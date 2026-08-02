using UnityEngine;

namespace SimpleGame
{
    public sealed partial class PlayerCombatAbilities
    {
        private void TrySpawnMovingSlash(Vector2 attackDirection)
        {
            if (movingSlashLevel <= 0 ||
                Random.value > CalculateMovingSlashChance(
                    movingSlashLevel))
            {
                return;
            }

            MovingSlashProjectile.Spawn(
                movingSlashPrefab,
                owner,
                enemyWorld,
                attackDirection,
                CalculateMovingSlashMaximumHits(movingSlashLevel),
                CalculateMovingSlashSize(movingSlashLevel),
                CalculateMovingSlashTravelDistance(movingSlashLevel),
                movingSlashDamageMultiplier);
        }

        public void TryScheduleSeverForCompletedMovementPierce(
            Vector2 piercingStartPosition,
            float piercingStartedAt)
        {
            if (owner == null || !HasSever || !TryReserveSever())
            {
                return;
            }

            StartCoroutine(SpawnSeverAfterDelay(
                piercingStartPosition,
                CalculateRemainingSeverDelay(
                    piercingStartedAt,
                    Time.time)));
        }

        public static float CalculateRemainingSeverDelay(
            float piercingStartedAt,
            float piercingCompletedAt)
        {
            float elapsed = Mathf.Max(
                0f,
                piercingCompletedAt - piercingStartedAt);
            return Mathf.Max(0f, SeverDelay - elapsed);
        }

        public static int CalculateStaticAdjacentTargetCount(int level)
        {
            return Mathf.Max(0, level) * 2 + 1;
        }

        public static float CalculateMovingSlashChance(int level)
        {
            return level <= 0
                ? 0f
                : Mathf.Clamp01(
                    (0.1f +
                     0.03f * (ClampMovingSlashLevel(level) - 1)) *
                    1.5f);
        }

        public static int CalculateMovingSlashMaximumHits(int level)
        {
            return level <= 0
                ? 0
                : ClampMovingSlashLevel(level) + 1;
        }

        public static float CalculateMovingSlashSize(int level)
        {
            return level <= 0
                ? 0f
                : 1f +
                    MovingSlashSizeGrowthPerLevel *
                    (ClampMovingSlashLevel(level) - 1);
        }

        public static float CalculateMovingSlashTravelDistance(int level)
        {
            return level <= 0
                ? 0f
                : MovingSlashBaseTravelDistance +
                    MovingSlashTravelGrowthPerLevel *
                    (ClampMovingSlashLevel(level) - 1);
        }

        public static float CalculateMovingSlashDamageMultiplier(
            int level,
            float baseDamageMultiplier =
                MovingSlashBaseDamageMultiplier)
        {
            return level <= 0
                ? 0f
                : Mathf.Max(0f, baseDamageMultiplier) +
                    MovingSlashDamageGrowthPerLevel *
                    (ClampMovingSlashLevel(level) - 1);
        }

        public static float CalculateShieldBypassChance(
            int level,
            float chancePerLevel = 0.1f)
        {
            return Mathf.Clamp01(
                Mathf.Max(0, level) *
                Mathf.Clamp01(chancePerLevel));
        }

        public static float CalculateFilthThrowDamageMultiplier(
            int level,
            float baseDamageMultiplier =
                FilthThrowBaseDamageMultiplier)
        {
            return level <= 0
                ? 0f
                : Mathf.Max(0f, baseDamageMultiplier) +
                    FilthThrowDamageGrowthPerLevel *
                    (ClampFilthThrowLevel(level) - 1);
        }

        public static float CalculateFilthThrowRadius(int level)
        {
            return level <= 0
                ? 0f
                : FilthThrowBaseRadius +
                    FilthThrowRadiusGrowthPerLevel *
                    (ClampFilthThrowLevel(level) - 1);
        }

        public static float CalculateFilthThrowInterval(int level)
        {
            return level <= 0
                ? float.PositiveInfinity
                : Mathf.Max(
                    0.1f,
                    FilthThrowBaseInterval -
                    FilthThrowIntervalReductionPerLevel *
                    (ClampFilthThrowLevel(level) - 1));
        }

        public static int CalculateFilthThrowCount(int level)
        {
            return level <= 0
                ? 0
                : ClampFilthThrowLevel(level);
        }

        public static int CalculateHitHealAmount(
            int level,
            int amountPerLevel = 2)
        {
            return Mathf.Max(0, level) *
                Mathf.Max(0, amountPerLevel);
        }

        private static int ClampMovingSlashLevel(int level)
        {
            return Mathf.Clamp(
                level,
                1,
                MovingSlashMaximumLevel);
        }

        private static int ClampFilthThrowLevel(int level)
        {
            return Mathf.Clamp(
                level,
                1,
                FilthThrowMaximumLevel);
        }

        private void Update()
        {
            if (owner == null ||
                enemyWorld == null ||
                worldCamera == null ||
                !owner.IsAlive ||
                Time.timeScale <= 0f)
            {
                return;
            }

            TryThrowFilth(
                filthThrowLevel,
                filthThrowBaseDamageMultiplier,
                ref nextFilthThrowAt,
                0,
                0f);
            if (hasStaticFilthFusion)
            {
                TryThrowFilth(
                    staticFilthLevelSnapshot,
                    staticFilthDamageSnapshot,
                    ref nextStaticFilthThrowAt,
                    staticFilthChargeSnapshot,
                    staticFilthChargeDamageSnapshot);
            }
        }

        private void TryThrowFilth(
            int level,
            float baseDamageMultiplier,
            ref float nextThrowAt,
            int fusionStaticLevel,
            float fusionStaticDamageMultiplier)
        {
            if (level <= 0 || Time.time < nextThrowAt)
            {
                return;
            }

            float halfHeight = worldCamera.orthographicSize;
            Vector2 cameraCenter = worldCamera.transform.position;
            Vector2 cameraHalfExtents = new(
                halfHeight * worldCamera.aspect,
                halfHeight);
            Rect visibleWorldBounds = Rect.MinMaxRect(
                cameraCenter.x - cameraHalfExtents.x,
                cameraCenter.y - cameraHalfExtents.y,
                cameraCenter.x + cameraHalfExtents.x,
                cameraCenter.y + cameraHalfExtents.y);
            EnemyBase firstTarget =
                enemyWorld.FindRandomLivingEnemyInBounds(
                    visibleWorldBounds,
                    Random.value);
            if (firstTarget == null)
            {
                return;
            }

            nextThrowAt =
                Time.time +
                CalculateFilthThrowInterval(level);
            float damageRadius =
                CalculateFilthThrowRadius(level);
            float damageMultiplier =
                CalculateFilthThrowDamageMultiplier(
                    level,
                    baseDamageMultiplier);
            int throwCount =
                CalculateFilthThrowCount(level);
            for (int index = 0; index < throwCount; index++)
            {
                EnemyBase target = index == 0
                    ? firstTarget
                    : enemyWorld.FindRandomLivingEnemyInBounds(
                        visibleWorldBounds,
                        Random.value);
                if (target == null)
                {
                    break;
                }

                FilthProjectile.Spawn(
                    filthProjectilePrefab,
                    owner,
                    enemyWorld,
                    target.transform.position,
                    damageMultiplier,
                    damageRadius,
                    fusionStaticLevel,
                    fusionStaticDamageMultiplier);
            }
        }

        public static bool CanTriggerHitHeal(
            bool targetDefeated,
            int level,
            float randomValue,
            float chance = HitHealChance)
        {
            return targetDefeated &&
                level > 0 &&
                randomValue < Mathf.Clamp01(chance);
        }

        public static int CalculateRemainingPiercingTargets(
            int level,
            int consumed)
        {
            return Mathf.Max(
                0,
                Mathf.Max(0, level) -
                Mathf.Max(0, consumed));
        }

        public static bool ShouldRefreshPiercingWindow(
            float currentTime,
            float windowEndsAt)
        {
            return currentTime >= windowEndsAt;
        }

        public static bool IsSeverCooldownReady(
            float currentTime,
            float nextAvailableTime)
        {
            return currentTime >= nextAvailableTime;
        }

        public static bool CanTriggerSever(
            bool hasSever,
            bool piercingAllowed,
            bool primaryDamaged)
        {
            return hasSever &&
                piercingAllowed &&
                primaryDamaged;
        }

        public bool RollShieldBypass()
        {
            return shieldBypassLevel > 0 &&
                Random.value < ShieldBypassChance;
        }
    }
}

