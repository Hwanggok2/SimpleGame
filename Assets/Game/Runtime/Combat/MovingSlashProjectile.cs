using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    public sealed class MovingSlashProjectile : MonoBehaviour
    {
        public const int MaximumInactivePoolSize = 16;
        public const string AnimationResourcePath =
            "Effects/MovingSlash_Crescent_6f";
        public const int AnimationFrameCount = 6;
        public const float PlayerSpeedMultiplier = 3f;
        public const float DefaultTravelDistance =
            PlayerCombatAbilities.MovingSlashBaseTravelDistance;
        public const float MaximumActiveDuration = 1.5f;
        public const float FadeOutDuration = 0.1f;

        private const float BaseVisualScale = 1.6f;
        private const float BaseHitRadius = 0.38f;

        private readonly Dictionary<EnemyBase, uint> hitEnemyGenerations =
            new();
        private readonly List<HitCandidate> candidates = new();
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] animationFrames;
        private PlayerRoot owner;
        private EnemyWorldService enemyWorld;
        private Vector2 direction;
        private Vector2 origin;
        private float travelSpeed;
        private float sizeMultiplier;
        private float travelDistance;
        private float damageMultiplier;
        private float activeElapsed;
        private int remainingHits;
        private bool isFading;
        private float fadeElapsed;
        private int fadeStartFrame;
        private Color baseRendererColor;
        private bool hasBaseRendererColor;

        public static void Spawn(
            MovingSlashProjectile prefab,
            PlayerRoot owner,
            EnemyWorldService enemyWorld,
            Vector2 direction,
            int maximumHits,
            float sizeMultiplier,
            float travelDistance,
            float damageMultiplier)
        {
            if (prefab == null)
            {
                Debug.LogError(
                    "Moving slash prefab is not assigned.",
                    owner);
                return;
            }

            MovingSlashProjectile projectile =
                ComponentPrefabPool<MovingSlashProjectile>.Acquire(
                    prefab,
                    MaximumInactivePoolSize);
            projectile.name = "MovingSlash";
            projectile.Configure(
                owner,
                enemyWorld,
                direction,
                maximumHits,
                sizeMultiplier,
                travelDistance,
                damageMultiplier);
        }

        public void ConfigureVisuals(
            SpriteRenderer configuredRenderer,
            Sprite[] configuredFrames)
        {
            spriteRenderer = configuredRenderer;
            animationFrames =
                configuredFrames ?? new Sprite[0];
            hasBaseRendererColor = false;
        }

        private void Configure(
            PlayerRoot configuredOwner,
            EnemyWorldService configuredEnemyWorld,
            Vector2 configuredDirection,
            int maximumHits,
            float configuredSizeMultiplier,
            float configuredTravelDistance,
            float configuredDamageMultiplier)
        {
            owner = configuredOwner;
            enemyWorld = configuredEnemyWorld;
            direction = configuredDirection.sqrMagnitude > 0.0001f
                ? configuredDirection.normalized
                : Vector2.right;
            travelDistance = Mathf.Max(
                0f,
                configuredTravelDistance);
            travelSpeed = CalculateTravelSpeed(
                owner.MoveSpeed,
                travelDistance,
                owner.Movement.IsMaximumSpeedActive);
            remainingHits = Mathf.Max(1, maximumHits);
            sizeMultiplier = Mathf.Max(0.1f, configuredSizeMultiplier);
            damageMultiplier = Mathf.Max(0f, configuredDamageMultiplier);
            activeElapsed = 0f;
            isFading = false;
            fadeElapsed = 0f;
            fadeStartFrame = 0;
            hitEnemyGenerations.Clear();
            candidates.Clear();
            origin = (Vector2)owner.transform.position + direction * 0.45f;
            transform.position = origin;

            if (spriteRenderer == null ||
                animationFrames == null ||
                animationFrames.Length != AnimationFrameCount)
            {
                Debug.LogError(
                    "Moving slash prefab requires a SpriteRenderer and " +
                    $"{AnimationFrameCount} animation frames.",
                    this);
                Recycle();
                return;
            }

            if (!hasBaseRendererColor)
            {
                baseRendererColor = spriteRenderer.color;
                hasBaseRendererColor = true;
            }

            spriteRenderer.color = baseRendererColor;

            spriteRenderer.sortingOrder = 24;
            transform.rotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(direction.y, direction.x) *
                    Mathf.Rad2Deg);
            transform.localScale =
                Vector3.one * BaseVisualScale * sizeMultiplier;
            RefreshVisual(0f);
        }

        private void Update()
        {
            if (owner == null ||
                enemyWorld == null ||
                !owner.IsAlive)
            {
                Recycle();
                return;
            }

            if (isFading)
            {
                fadeElapsed += Time.deltaTime;
                RefreshFadeVisual();
                if (fadeElapsed >= FadeOutDuration)
                {
                    Recycle();
                }

                return;
            }

            activeElapsed += Time.deltaTime;
            Vector2 previous = transform.position;
            Vector2 destination =
                origin + direction * travelDistance;
            Vector2 next = Vector2.MoveTowards(
                previous,
                destination,
                travelSpeed * Time.deltaTime);
            transform.position = next;
            float distanceTravelled =
                Vector2.Distance(origin, next);

            RefreshVisual(distanceTravelled);
            HitEnemiesAlong(previous, next);
            if (ShouldBeginFade(
                remainingHits,
                distanceTravelled,
                travelDistance,
                activeElapsed))
            {
                BeginFade(distanceTravelled);
            }
        }

        private void HitEnemiesAlong(Vector2 start, Vector2 end)
        {
            candidates.Clear();
            foreach (EnemyBase enemy in enemyWorld.Enemies)
            {
                if (enemy == null ||
                    !enemy.IsAlive ||
                    HasHitCurrentSpawn(enemy))
                {
                    continue;
                }

                float allowedDistance =
                    BaseHitRadius * sizeMultiplier +
                    EnemyWorldService.GetColliderRadius(enemy);
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

                hitEnemyGenerations[candidate.Enemy] =
                    candidate.Enemy.SpawnGeneration;
                remainingHits--;
                owner.ApplySkillHit(
                    candidate.Enemy,
                    damageMultiplier);
            }
        }

        public static float CalculateTravelSpeed(
            float playerMoveSpeed,
            float travelDistance,
            bool maximumSpeedActive)
        {
            return maximumSpeedActive
                ? PlayerMovement.CalculateMaximumTravelSpeed(
                    travelDistance)
                : Mathf.Max(0f, playerMoveSpeed) *
                    PlayerSpeedMultiplier;
        }

        public static bool ShouldBeginFade(
            int remainingHits,
            float distanceTravelled,
            float travelDistance,
            float activeElapsed)
        {
            return remainingHits <= 0 ||
                travelDistance <= 0f ||
                distanceTravelled >= travelDistance - 0.0001f ||
                activeElapsed >= MaximumActiveDuration;
        }

        public static int CalculateAnimationFrameIndex(
            float distanceTravelled,
            float travelDistance = DefaultTravelDistance,
            int frameCount = AnimationFrameCount)
        {
            if (frameCount <= 1 || travelDistance <= 0f)
            {
                return 0;
            }

            float progress = Mathf.Clamp01(
                Mathf.Max(0f, distanceTravelled) /
                travelDistance);
            return Mathf.Min(
                frameCount - 1,
                Mathf.FloorToInt(progress * frameCount));
        }

        public static float CalculateFadeAlpha(
            float elapsed,
            float duration = FadeOutDuration)
        {
            if (duration <= 0f)
            {
                return 0f;
            }

            return 1f - Mathf.Clamp01(
                Mathf.Max(0f, elapsed) / duration);
        }

        private void RefreshVisual(float distanceTravelled)
        {
            if (spriteRenderer == null ||
                animationFrames == null ||
                animationFrames.Length == 0)
            {
                return;
            }

            int frameIndex = CalculateAnimationFrameIndex(
                distanceTravelled,
                travelDistance,
                animationFrames.Length);
            SetVisualFrame(frameIndex);
        }

        private void BeginFade(float distanceTravelled)
        {
            isFading = true;
            fadeElapsed = 0f;
            fadeStartFrame = CalculateAnimationFrameIndex(
                distanceTravelled,
                travelDistance,
                animationFrames.Length);
        }

        private void RefreshFadeVisual()
        {
            float progress = Mathf.Clamp01(
                fadeElapsed / FadeOutDuration);
            int remainingFrameCount =
                animationFrames.Length - fadeStartFrame;
            int frameOffset = Mathf.Min(
                remainingFrameCount - 1,
                Mathf.FloorToInt(
                    progress * remainingFrameCount));
            SetVisualFrame(fadeStartFrame + frameOffset);

            Color color = spriteRenderer.color;
            color.a = CalculateFadeAlpha(
                fadeElapsed,
                FadeOutDuration);
            spriteRenderer.color = color;
        }

        private void SetVisualFrame(int frameIndex)
        {
            spriteRenderer.sprite =
                animationFrames[
                    Mathf.Clamp(
                        frameIndex,
                        0,
                        animationFrames.Length - 1)];
        }

        private bool HasHitCurrentSpawn(EnemyBase enemy)
        {
            return hitEnemyGenerations.TryGetValue(
                    enemy,
                    out uint generation) &&
                generation == enemy.SpawnGeneration;
        }

        private void Recycle()
        {
            hitEnemyGenerations.Clear();
            candidates.Clear();
            owner = null;
            enemyWorld = null;
            direction = Vector2.zero;
            origin = Vector2.zero;
            travelSpeed = 0f;
            sizeMultiplier = 0f;
            travelDistance = 0f;
            damageMultiplier = 0f;
            activeElapsed = 0f;
            remainingHits = 0;
            isFading = false;
            fadeElapsed = 0f;
            fadeStartFrame = 0;
            if (spriteRenderer != null && hasBaseRendererColor)
            {
                spriteRenderer.color = baseRendererColor;
            }

            ComponentPrefabPool<MovingSlashProjectile>.Release(this);
        }

        private void OnDestroy()
        {
            ComponentPrefabPool<MovingSlashProjectile>.Forget(this);
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
