using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    [DisallowMultipleComponent]
    public sealed class EnemyWorldService : MonoBehaviour
    {
        private const float EnemySeparationPadding = 0.08f;
        private const int SeparationPassCount = 2;
        private const int SpawnPositionAttemptCount = 32;

        private readonly List<EnemyBase> enemies = new();

        public IReadOnlyList<EnemyBase> Enemies => enemies;

        public void Register(EnemyBase enemy)
        {
            if (enemy != null && !enemies.Contains(enemy))
            {
                enemies.Add(enemy);
            }
        }

        public void Unregister(EnemyBase enemy)
        {
            enemies.Remove(enemy);
        }

        public EnemyBase FindEnemyNear(Vector2 position, float radius)
        {
            EnemyBase nearest = null;
            float nearestDistance = radius;
            foreach (EnemyBase enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                float distance = Vector2.Distance(
                    position,
                    enemy.transform.position);
                if (distance <= nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        public EnemyBase FindRandomLivingEnemyInBounds(
            Rect worldBounds,
            float randomValue)
        {
            int livingCount = 0;
            foreach (EnemyBase enemy in enemies)
            {
                if (enemy != null &&
                    enemy.IsAlive &&
                    worldBounds.Contains(enemy.transform.position))
                {
                    livingCount++;
                }
            }

            if (livingCount <= 0)
            {
                return null;
            }

            int selectedIndex = Mathf.Min(
                Mathf.FloorToInt(
                    Mathf.Clamp01(randomValue) * livingCount),
                livingCount - 1);
            foreach (EnemyBase enemy in enemies)
            {
                if (enemy == null ||
                    !enemy.IsAlive ||
                    !worldBounds.Contains(enemy.transform.position))
                {
                    continue;
                }

                if (selectedIndex <= 0)
                {
                    return enemy;
                }

                selectedIndex--;
            }

            return null;
        }

        public EnemyBase FindFirstEnemyOnPath(
            Vector2 start,
            Vector2 destination,
            float moverRadius,
            EnemyBase ignoredEnemy = null)
        {
            Vector2 path = destination - start;
            float pathLengthSquared = path.sqrMagnitude;
            if (pathLengthSquared <= 0.0001f)
            {
                return null;
            }

            EnemyBase first = null;
            float firstProgress = float.MaxValue;
            foreach (EnemyBase enemy in enemies)
            {
                if (enemy == null ||
                    enemy == ignoredEnemy ||
                    !enemy.IsAlive)
                {
                    continue;
                }

                Vector2 enemyPosition = enemy.transform.position;
                if (!CombatGeometry.IsAheadAlongPath(
                    enemyPosition,
                    start,
                    destination))
                {
                    continue;
                }

                float progress = Mathf.Clamp01(
                    Vector2.Dot(enemyPosition - start, path) /
                    pathLengthSquared);
                Vector2 closestPoint = start + path * progress;
                float combinedRadius =
                    Mathf.Max(0f, moverRadius) +
                    GetColliderRadius(enemy);
                if (Vector2.SqrMagnitude(enemyPosition - closestPoint) >
                    combinedRadius * combinedRadius)
                {
                    continue;
                }

                if (progress < firstProgress)
                {
                    firstProgress = progress;
                    first = enemy;
                }
            }

            return first;
        }

        public EnemyBase FindAimAssistTarget(
            Vector2 start,
            Vector2 destination,
            float halfWidth,
            EnemyBase preferredEnemy = null,
            float retentionWidthMultiplier = 1f)
        {
            Vector2 path = destination - start;
            float pathLength = path.magnitude;
            if (pathLength <= 0.0001f)
            {
                return null;
            }

            Vector2 direction = path / pathLength;
            float safeHalfWidth = Mathf.Max(0f, halfWidth);
            EnemyBase best = null;
            float bestScore = float.MaxValue;
            foreach (EnemyBase enemy in enemies)
            {
                if (!TryGetAimAssistScore(
                        enemy,
                        start,
                        direction,
                        pathLength,
                        safeHalfWidth,
                        out float score))
                {
                    continue;
                }

                if (score < bestScore)
                {
                    best = enemy;
                    bestScore = score;
                }
            }

            const float retentionScoreTolerance = 0.08f;
            float retentionWidth = safeHalfWidth *
                Mathf.Max(1f, retentionWidthMultiplier);
            if (preferredEnemy != null &&
                enemies.Contains(preferredEnemy) &&
                TryGetAimAssistScore(
                    preferredEnemy,
                    start,
                    direction,
                    pathLength,
                    retentionWidth,
                    out float preferredScore) &&
                (best == null ||
                 best == preferredEnemy ||
                 preferredScore <=
                    bestScore + retentionScoreTolerance))
            {
                return preferredEnemy;
            }

            return best;
        }

        public List<EnemyBase> CollectPiercingTargets(
            Vector2 start,
            EnemyBase primary,
            int additionalTargetCount,
            float reachAfterPrimary,
            float halfWidth)
        {
            var result = new List<EnemyBase>();
            if (primary == null || !primary.IsAlive)
            {
                return result;
            }

            result.Add(primary);
            if (additionalTargetCount <= 0)
            {
                return result;
            }

            Vector2 direction =
                (Vector2)primary.transform.position - start;
            float primaryDistance = direction.magnitude;
            if (primaryDistance <= 0.0001f)
            {
                return result;
            }

            direction /= primaryDistance;
            var candidates = new List<ProjectedEnemy>();
            foreach (EnemyBase enemy in enemies)
            {
                if (enemy == null ||
                    enemy == primary ||
                    !enemy.IsAlive)
                {
                    continue;
                }

                Vector2 offset =
                    (Vector2)enemy.transform.position - start;
                float progress = Vector2.Dot(offset, direction);
                if (progress <= primaryDistance + 0.01f ||
                    progress > primaryDistance + reachAfterPrimary)
                {
                    continue;
                }

                Vector2 closest = start + direction * progress;
                float allowedDistance =
                    halfWidth + GetColliderRadius(enemy);
                if (Vector2.Distance(
                        enemy.transform.position,
                        closest) <= allowedDistance)
                {
                    candidates.Add(new ProjectedEnemy(enemy, progress));
                }
            }

            candidates.Sort((left, right) =>
                left.Progress.CompareTo(right.Progress));
            int count = Mathf.Min(
                additionalTargetCount,
                candidates.Count);
            for (int index = 0; index < count; index++)
            {
                result.Add(candidates[index].Enemy);
            }

            return result;
        }

        public List<EnemyBase> CollectNearestEnemies(
            Vector2 center,
            float radius,
            int maximumCount,
            ISet<EnemyBase> excluded)
        {
            var candidates = new List<ProjectedEnemy>();
            float radiusSquared = radius * radius;
            foreach (EnemyBase enemy in enemies)
            {
                if (enemy == null ||
                    !enemy.IsAlive ||
                    (excluded != null && excluded.Contains(enemy)))
                {
                    continue;
                }

                float distanceSquared = Vector2.SqrMagnitude(
                    (Vector2)enemy.transform.position - center);
                if (distanceSquared <= radiusSquared)
                {
                    candidates.Add(new ProjectedEnemy(
                        enemy,
                        distanceSquared));
                }
            }

            candidates.Sort((left, right) =>
                left.Progress.CompareTo(right.Progress));
            int count = Mathf.Min(
                Mathf.Max(0, maximumCount),
                candidates.Count);
            var result = new List<EnemyBase>(count);
            for (int index = 0; index < count; index++)
            {
                result.Add(candidates[index].Enemy);
            }

            return result;
        }

        public void FillEnemiesInRadius(
            Vector2 center,
            float radius,
            List<EnemyBase> results)
        {
            if (results == null)
            {
                return;
            }

            results.Clear();
            float safeRadius = Mathf.Max(0f, radius);
            float radiusSquared = safeRadius * safeRadius;
            foreach (EnemyBase enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                float distanceSquared = Vector2.SqrMagnitude(
                    (Vector2)enemy.transform.position - center);
                if (distanceSquared <= radiusSquared)
                {
                    results.Add(enemy);
                }
            }
        }

        public List<EnemyBase> CollectEnemiesAlongSegment(
            Vector2 start,
            Vector2 end,
            float halfWidth)
        {
            var result = new List<EnemyBase>();
            foreach (EnemyBase enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                if (CombatGeometry.OverlapsSegment(
                        enemy.transform.position,
                        GetColliderRadius(enemy),
                        start,
                        end,
                        halfWidth))
                {
                    result.Add(enemy);
                }
            }

            return result;
        }

        public Vector2 FindOpenEnemyPosition(
            Vector2 requestedPosition,
            float radius,
            EnemyBase ignoredEnemy = null,
            bool incomingAllowsEnemyOverlap = false)
        {
            if (incomingAllowsEnemyOverlap)
            {
                return requestedPosition;
            }

            float safeRadius = Mathf.Max(0.1f, radius);
            for (int attempt = 0;
                 attempt < SpawnPositionAttemptCount;
                 attempt++)
            {
                Vector2 candidate = requestedPosition;
                if (attempt > 0)
                {
                    float ring = 1f + (attempt - 1) / 8;
                    float angle = attempt * 2.39996323f;
                    candidate += new Vector2(
                        Mathf.Cos(angle),
                        Mathf.Sin(angle)) *
                        safeRadius * 2.15f * ring;
                }

                if (IsEnemyPositionOpen(
                        candidate,
                        safeRadius,
                        ignoredEnemy))
                {
                    return candidate;
                }
            }

            return requestedPosition;
        }

        public void SeparateEnemy(EnemyBase mover)
        {
            if (mover == null ||
                !mover.IsAlive ||
                mover.AllowsEnemyOverlap)
            {
                return;
            }

            Vector2 resolved = mover.transform.position;
            float moverRadius = GetColliderRadius(mover);
            for (int pass = 0; pass < SeparationPassCount; pass++)
            {
                foreach (EnemyBase other in enemies)
                {
                    if (other == null ||
                        other == mover ||
                        !other.IsAlive ||
                        other.AllowsEnemyOverlap)
                    {
                        continue;
                    }

                    float minimumDistance =
                        moverRadius +
                        GetColliderRadius(other) +
                        EnemySeparationPadding;
                    resolved = CombatGeometry.PushOutside(
                        resolved,
                        mover.GetInstanceID(),
                        other.transform.position,
                        other.GetInstanceID(),
                        minimumDistance);
                }
            }

            mover.transform.position = new Vector3(
                resolved.x,
                resolved.y,
                mover.transform.position.z);
        }

        public static float GetColliderRadius(Component owner)
        {
            if (owner is EnemyBase enemy)
            {
                return enemy.CollisionRadius;
            }

            CircleCollider2D circle = owner != null
                ? owner.GetComponent<CircleCollider2D>()
                : null;
            if (circle == null)
            {
                return 0f;
            }

            Vector3 scale = circle.transform.lossyScale;
            return circle.radius *
                Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        }

        private bool IsEnemyPositionOpen(
            Vector2 position,
            float radius,
            EnemyBase ignoredEnemy)
        {
            foreach (EnemyBase enemy in enemies)
            {
                if (enemy == null ||
                    enemy == ignoredEnemy ||
                    !enemy.IsAlive ||
                    enemy.AllowsEnemyOverlap)
                {
                    continue;
                }

                float minimumDistance =
                    radius +
                    GetColliderRadius(enemy) +
                    EnemySeparationPadding;
                if (Vector2.SqrMagnitude(
                        position -
                        (Vector2)enemy.transform.position) <
                    minimumDistance * minimumDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryGetAimAssistScore(
            EnemyBase enemy,
            Vector2 start,
            Vector2 direction,
            float pathLength,
            float halfWidth,
            out float score)
        {
            score = float.MaxValue;
            if (enemy == null || !enemy.IsAlive)
            {
                return false;
            }

            Vector2 offset =
                (Vector2)enemy.transform.position - start;
            float distanceAlongPath = Vector2.Dot(
                offset,
                direction);
            if (distanceAlongPath <= 0f ||
                distanceAlongPath > pathLength)
            {
                return false;
            }

            float distanceFromPath = Mathf.Abs(
                direction.x * offset.y -
                direction.y * offset.x);
            float allowedDistance =
                halfWidth + GetColliderRadius(enemy);
            if (distanceFromPath > allowedDistance)
            {
                return false;
            }

            float angularError = distanceFromPath /
                Mathf.Max(0.5f, distanceAlongPath);
            float distanceTieBreaker =
                distanceAlongPath / pathLength * 0.05f;
            score = angularError + distanceTieBreaker;
            return true;
        }

        private readonly struct ProjectedEnemy
        {
            public ProjectedEnemy(EnemyBase enemy, float progress)
            {
                Enemy = enemy;
                Progress = progress;
            }

            public EnemyBase Enemy { get; }
            public float Progress { get; }
        }
    }
}
