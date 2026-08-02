using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    public sealed partial class FlyingSwordController
    {
        public void HandlePrimaryHit(EnemyBase primary)
        {
            if (primary == null ||
                owner == null ||
                enemyWorld == null ||
                swordCountLevel <= 0 ||
                !owner.IsAlive)
            {
                return;
            }

            foreach (SwordSlot slot in slots)
            {
                if (slot.State == SwordState.Approaching &&
                    (slot.PrimaryTarget == null ||
                     !slot.PrimaryTarget.IsAlive) &&
                    BeginFlight(slot, primary))
                {
                    nextLaunchAt = Time.time + LaunchInterval;
                    return;
                }
            }

            if (!IsLaunchReady(Time.time, nextLaunchAt))
            {
                return;
            }

            foreach (SwordSlot slot in slots)
            {
                RefreshSlotAvailability(slot, Time.time);
                if (slot.State == SwordState.Ready &&
                    BeginFlight(slot, primary))
                {
                    nextLaunchAt = Time.time + LaunchInterval;
                    return;
                }
            }
        }

        public static int CalculateMaximumHits(int level)
        {
            return BaseHitCount +
                Mathf.Clamp(level, 0, MaximumHitUpgradeLevel);
        }

        public static int CalculateMaximumHits(
            int level,
            bool piercesEntirePath)
        {
            return piercesEntirePath
                ? int.MaxValue
                : CalculateMaximumHits(level);
        }

        public static bool WasCurrentSpawnHit(
            bool hasRecordedGeneration,
            uint recordedGeneration,
            uint currentGeneration)
        {
            return hasRecordedGeneration &&
                recordedGeneration == currentGeneration;
        }

        public static bool IsLaunchReady(
            float currentTime,
            float nextAvailableTime)
        {
            return currentTime >= nextAvailableTime;
        }

        public static bool IsSlotReady(
            float currentTime,
            float readyAt)
        {
            return currentTime >= readyAt;
        }

        public static float CalculateFadeAlpha(
            float remainingDuration)
        {
            return Mathf.Clamp01(
                remainingDuration /
                PostTargetTravelDuration);
        }

        private void Update()
        {
            if (owner == null || enemyWorld == null)
            {
                HideAllVisuals();
                return;
            }

            if (!owner.IsAlive)
            {
                CancelFlights();
                HideAllVisuals();
                return;
            }

            float currentTime = Time.time;
            foreach (SwordSlot slot in slots)
            {
                RefreshSlotAvailability(slot, currentTime);
                switch (slot.State)
                {
                    case SwordState.Approaching:
                        TickApproaching(slot);
                        break;
                    case SwordState.Passing:
                        TickPassing(slot);
                        break;
                }
            }
        }

        private void LateUpdate()
        {
            if (owner != null &&
                enemyWorld != null &&
                owner.IsAlive)
            {
                RefreshReadyIndicators();
            }
        }

        private bool BeginFlight(
            SwordSlot slot,
            EnemyBase primary)
        {
            Vector2 targetPosition = primary.transform.position;
            if (!TryGetRandomSpawnPosition(
                    targetPosition,
                    out Vector2 start))
            {
                return false;
            }

            Vector2 path = targetPosition - start;
            float distance = path.magnitude;
            Vector2 direction = distance > Mathf.Epsilon
                ? path / distance
                : Vector2.right;
            float speed =
                PlayerMovement.CalculateMaximumTravelSpeed(distance);

            slot.PrimaryTarget = primary;
            slot.TargetPosition = targetPosition;
            slot.AttackOrigin = start;
            slot.Direction = direction;
            slot.Speed = speed;
            slot.RemainingPassDuration =
                PostTargetTravelDuration;
            slot.PiercesEntirePath = piercesEntirePath;
            slot.StaticChargeLevel = staticChargeLevel;
            slot.StaticDamageMultiplier = staticDamageMultiplier;
            slot.RemainingHits = CalculateMaximumHits(
                hitCountLevel,
                slot.PiercesEntirePath);
            slot.ReadyAt = Time.time + RechargeDuration;
            slot.HitEnemyGenerations.Clear();
            slot.State = SwordState.Approaching;

            slot.Transform.position = new Vector3(
                start.x,
                start.y,
                owner.transform.position.z);
            FaceDirection(slot.Transform, direction);
            slot.IndicatorVisual?.SetActive(false);
            RestoreAttackColor(slot);
            slot.AttackVisual.SetActive(true);
            RefreshAttackVisual(slot);
            return true;
        }

        private void TickApproaching(SwordSlot slot)
        {
            Vector2 previous = slot.Transform.position;
            float distanceToTarget = Vector2.Distance(
                previous,
                slot.TargetPosition);
            float frameDistance = slot.Speed * Time.deltaTime;
            if (frameDistance + 0.0001f < distanceToTarget)
            {
                Vector2 next =
                    previous + slot.Direction * frameDistance;
                if (slot.PiercesEntirePath)
                {
                    HitSecondaryEnemies(slot, previous, next);
                }

                SetWorldPosition(
                    slot,
                    next);
                RefreshAttackVisual(slot);
                return;
            }

            SetWorldPosition(slot, slot.TargetPosition);
            RefreshAttackVisual(slot);
            if (slot.PiercesEntirePath)
            {
                HitSecondaryEnemies(
                    slot,
                    previous,
                    slot.TargetPosition);
            }

            HitPrimary(slot);
            slot.State = SwordState.Passing;

            float overflow = Mathf.Max(
                0f,
                frameDistance - distanceToTarget);
            float overflowDuration = slot.Speed > 0f
                ? overflow / slot.Speed
                : Time.deltaTime;
            AdvancePassing(slot, overflowDuration);
        }

        private void TickPassing(SwordSlot slot)
        {
            AdvancePassing(
                slot,
                Time.deltaTime);
        }

        private void AdvancePassing(
            SwordSlot slot,
            float requestedDuration)
        {
            float duration = Mathf.Min(
                Mathf.Max(0f, requestedDuration),
                slot.RemainingPassDuration);
            float distance = slot.Speed * duration;
            Vector2 previous = slot.Transform.position;
            Vector2 next =
                previous + slot.Direction * distance;
            SetWorldPosition(slot, next);
            HitSecondaryEnemies(slot, previous, next);
            slot.RemainingPassDuration =
                Mathf.Max(
                    0f,
                    slot.RemainingPassDuration - duration);
            RefreshAttackVisual(
                slot,
                CalculateFadeAlpha(
                    slot.RemainingPassDuration));

            if (slot.RemainingPassDuration <= 0.0001f)
            {
                FinishFlight(slot);
            }
        }

        private void HitPrimary(SwordSlot slot)
        {
            EnemyBase primary = slot.PrimaryTarget;
            if (primary == null ||
                !primary.IsAlive ||
                slot.RemainingHits <= 0 ||
                HasHitCurrentSpawn(slot, primary))
            {
                return;
            }

            RememberHit(slot, primary);
            if (ApplySwordHit(slot, primary))
            {
                slot.RemainingHits--;
            }
        }

        private void HitSecondaryEnemies(
            SwordSlot slot,
            Vector2 start,
            Vector2 end)
        {
            hitCandidates.Clear();
            if (slot.RemainingHits <= 0 ||
                Vector2.SqrMagnitude(end - start) <= 0.0000001f)
            {
                return;
            }

            IReadOnlyList<EnemyBase> enemies = enemyWorld.Enemies;
            for (int index = 0; index < enemies.Count; index++)
            {
                EnemyBase enemy = enemies[index];
                if (enemy == null ||
                    !enemy.IsAlive ||
                    HasHitCurrentSpawn(slot, enemy))
                {
                    continue;
                }

                Vector2 enemyPosition = enemy.transform.position;
                float progress = Vector2.Dot(
                    enemyPosition - start,
                    slot.Direction);
                if (progress <= 0f)
                {
                    continue;
                }

                float allowedDistance =
                    HitHalfWidth +
                    EnemyWorldService.GetColliderRadius(enemy);
                if (CombatGeometry.DistancePointToSegment(
                        enemyPosition,
                        start,
                        end) > allowedDistance)
                {
                    continue;
                }

                hitCandidates.Add(new HitCandidate(
                    enemy,
                    progress));
            }

            hitCandidates.Sort((left, right) =>
                left.Progress.CompareTo(right.Progress));
            foreach (HitCandidate candidate in hitCandidates)
            {
                if (slot.RemainingHits <= 0)
                {
                    break;
                }

                EnemyBase enemy = candidate.Enemy;
                if (enemy == null ||
                    !enemy.IsAlive ||
                    HasHitCurrentSpawn(slot, enemy))
                {
                    continue;
                }

                RememberHit(slot, enemy);
                if (ApplySwordHit(slot, enemy))
                {
                    slot.RemainingHits--;
                }
            }

            hitCandidates.Clear();
        }

        private static bool HasHitCurrentSpawn(
            SwordSlot slot,
            EnemyBase enemy)
        {
            bool hasRecordedGeneration =
                slot.HitEnemyGenerations.TryGetValue(
                    enemy,
                    out uint generation);
            return WasCurrentSpawnHit(
                hasRecordedGeneration,
                generation,
                enemy.SpawnGeneration);
        }

        private static void RememberHit(
            SwordSlot slot,
            EnemyBase enemy)
        {
            slot.HitEnemyGenerations[enemy] =
                enemy.SpawnGeneration;
        }

        private bool ApplySwordHit(
            SwordSlot slot,
            EnemyBase enemy)
        {
            bool damageApplied = owner.ApplySkillHitWithStaticBurst(
                enemy,
                DamageMultiplier,
                slot.StaticChargeLevel,
                slot.StaticDamageMultiplier);
            if (damageApplied)
            {
                enemy.Session?.PlayCombatFeedback(
                    true,
                    !enemy.IsAlive,
                    false,
                    PlayerAttackReaction.None);
            }

            return damageApplied;
        }

        private void FinishFlight(SwordSlot slot)
        {
            slot.State = SwordState.Cooling;
            slot.PrimaryTarget = null;
            slot.HitEnemyGenerations.Clear();
            slot.AttackVisual.SetActive(false);
            RestoreAttackColor(slot);
        }

        private void RefreshSlotAvailability(
            SwordSlot slot,
            float currentTime)
        {
            if (slot.State == SwordState.Cooling &&
                IsSlotReady(currentTime, slot.ReadyAt))
            {
                slot.State = SwordState.Ready;
            }
        }

        private bool TryGetRandomSpawnPosition(
            Vector2 targetPosition,
            out Vector2 position)
        {
            IReadOnlyList<Transform> candidates =
                spawnPoints != null
                    ? spawnPoints.SpawnPoints
                    : null;
            int count = candidates?.Count ?? 0;
            if (count <= 0)
            {
                position = default;
                return false;
            }

            int startIndex = Random.Range(0, count);
            Transform fallback = null;
            for (int offset = 0; offset < count; offset++)
            {
                Transform candidate =
                    candidates[(startIndex + offset) % count];
                if (candidate == null)
                {
                    continue;
                }

                fallback ??= candidate;
                Vector2 candidatePosition = candidate.position;
                if (Vector2.Distance(
                        candidatePosition,
                        targetPosition) <= MinimumPathDistance)
                {
                    continue;
                }

                position = candidatePosition;
                return true;
            }

            if (fallback != null)
            {
                position = fallback.position;
                return true;
            }

            position = default;
            return false;
        }
    }
}

