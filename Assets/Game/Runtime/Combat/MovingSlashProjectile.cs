using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    public sealed class MovingSlashProjectile : MonoBehaviour
    {
        private const float TravelSpeed = 14f;
        private const float TravelDistance = 7f;
        private const float BaseHalfLength = 0.8f;
        private const float BaseHitRadius = 0.38f;

        private readonly HashSet<EnemyBase> hitEnemies = new();
        private readonly List<HitCandidate> candidates = new();
        private PlayerRoot owner;
        private PrototypeGameSession session;
        private Vector2 direction;
        private Vector2 origin;
        private float sizeMultiplier;
        private float damageMultiplier;
        private int remainingHits;
        private LineRenderer line;
        private Material material;

        public static void Spawn(
            PlayerRoot owner,
            PrototypeGameSession session,
            Vector2 direction,
            int maximumHits,
            float sizeMultiplier,
            float damageMultiplier)
        {
            var projectileObject = new GameObject("MovingSlash");
            var projectile =
                projectileObject.AddComponent<MovingSlashProjectile>();
            projectile.Configure(
                owner,
                session,
                direction,
                maximumHits,
                sizeMultiplier,
                damageMultiplier);
        }

        private void Configure(
            PlayerRoot configuredOwner,
            PrototypeGameSession configuredSession,
            Vector2 configuredDirection,
            int maximumHits,
            float configuredSizeMultiplier,
            float configuredDamageMultiplier)
        {
            owner = configuredOwner;
            session = configuredSession;
            direction = configuredDirection.sqrMagnitude > 0.0001f
                ? configuredDirection.normalized
                : Vector2.right;
            remainingHits = Mathf.Max(1, maximumHits);
            sizeMultiplier = Mathf.Max(0.1f, configuredSizeMultiplier);
            damageMultiplier = Mathf.Max(0f, configuredDamageMultiplier);
            origin = (Vector2)owner.transform.position + direction * 0.45f;
            transform.position = origin;

            line = gameObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = 0.22f * sizeMultiplier;
            line.endWidth = line.startWidth;
            line.numCapVertices = 2;
            line.sortingOrder = 24;
            line.startColor = new Color(0.65f, 0.9f, 1f, 0.95f);
            line.endColor = line.startColor;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                material = new Material(shader);
                line.material = material;
            }

            RefreshVisual();
        }

        private void Update()
        {
            if (owner == null || session == null || !owner.IsAlive)
            {
                Destroy(gameObject);
                return;
            }

            Vector2 previous = transform.position;
            Vector2 next =
                previous + direction * TravelSpeed * Time.deltaTime;
            transform.position = next;
            RefreshVisual();
            HitEnemiesAlong(previous, next);

            if (remainingHits <= 0 ||
                Vector2.Distance(origin, next) >= TravelDistance)
            {
                Destroy(gameObject);
            }
        }

        private void HitEnemiesAlong(Vector2 start, Vector2 end)
        {
            candidates.Clear();
            foreach (EnemyBase enemy in session.Enemies)
            {
                if (enemy == null ||
                    !enemy.IsAlive ||
                    hitEnemies.Contains(enemy))
                {
                    continue;
                }

                float allowedDistance =
                    BaseHitRadius * sizeMultiplier +
                    PrototypeGameSession.GetColliderRadius(enemy);
                Vector2 enemyPosition = enemy.transform.position;
                float distance = CombatGeometry.DistancePointToSegment(
                    enemyPosition,
                    start,
                    end);
                if (distance > allowedDistance)
                {
                    continue;
                }

                candidates.Add(new HitCandidate(
                    enemy,
                    Vector2.Dot(enemyPosition - start, direction)));
            }

            candidates.Sort((left, right) =>
                left.Progress.CompareTo(right.Progress));
            foreach (HitCandidate candidate in candidates)
            {
                if (remainingHits <= 0)
                {
                    return;
                }

                hitEnemies.Add(candidate.Enemy);
                remainingHits--;
                owner.ApplySkillHit(
                    candidate.Enemy,
                    damageMultiplier);
            }
        }

        private void RefreshVisual()
        {
            Vector2 perpendicular = new(-direction.y, direction.x);
            float halfLength = BaseHalfLength * sizeMultiplier;
            Vector2 center = transform.position;
            line.SetPosition(0, center - perpendicular * halfLength);
            line.SetPosition(1, center + perpendicular * halfLength);
        }

        private void OnDestroy()
        {
            if (material != null)
            {
                Destroy(material);
            }
        }

        private readonly struct HitCandidate
        {
            public HitCandidate(EnemyBase enemy, float progress)
            {
                Enemy = enemy;
                Progress = progress;
            }

            public EnemyBase Enemy { get; }
            public float Progress { get; }
        }
    }
}
