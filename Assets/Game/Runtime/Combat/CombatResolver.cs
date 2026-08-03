using UnityEngine;

namespace SimpleGame
{
    public static class CombatResolver
    {
        public static AttackSide GetAttackSide(
            Vector2 enemyFacing,
            Vector2 enemyPosition,
            Vector2 attackerPosition)
        {
            Vector2 facing = enemyFacing.sqrMagnitude > 0.0001f
                ? enemyFacing.normalized
                : Vector2.up;
            Vector2 toAttacker = (attackerPosition - enemyPosition).normalized;
            return Vector2.Dot(facing, toAttacker) >= 0f
                ? AttackSide.Front
                : AttackSide.Rear;
        }

        public static CombatResult Resolve(
            EnemyDefinition definition,
            int targetMaxHealth,
            int playerAttackPower,
            float rearAttackMultiplier,
            AttackSide side,
            bool critical)
        {
            int safeTargetMaxHealth = Mathf.Max(1, targetMaxHealth);
            float rawDamage = Mathf.Max(0, playerAttackPower) *
                (side == AttackSide.Rear
                    ? Mathf.Max(1f, rearAttackMultiplier)
                    : 1f);
            if (critical)
            {
                rawDamage *= 3f;
            }

            int damage = RoundDamage(rawDamage);

            bool causesRecoil = side == AttackSide.Front &&
                definition.BlocksFrontAttacks &&
                damage < safeTargetMaxHealth &&
                !critical;

            return new CombatResult(
                damage,
                safeTargetMaxHealth,
                causesRecoil
                    ? PlayerAttackReaction.Recoil
                    : PlayerAttackReaction.None);
        }

        public static EnemyThreatLevel GetThreatLevel(
            EnemyDefinition definition,
            int targetMaxHealth,
            int playerAttackPower,
            float rearAttackMultiplier)
        {
            CombatResult front = Resolve(
                definition,
                targetMaxHealth,
                playerAttackPower,
                rearAttackMultiplier,
                AttackSide.Front,
                false);
            CombatResult rear = Resolve(
                definition,
                targetMaxHealth,
                playerAttackPower,
                rearAttackMultiplier,
                AttackSide.Rear,
                false);

            int frontHits = GetRequiredHitCount(front);
            int rearHits = GetRequiredHitCount(rear);
            if (frontHits == 1 && rearHits == 1)
            {
                return EnemyThreatLevel.OneHit;
            }

            return frontHits == 3 && rearHits == 1
                ? EnemyThreatLevel.ThreeFrontOneRear
                : EnemyThreatLevel.Dangerous;
        }

        public static bool CanPiercePastTarget(
            EnemyDefinition definition,
            AttackSide side,
            bool targetDefeatedByAttack)
        {
            return definition == null ||
                !definition.BlocksFrontAttacks ||
                side != AttackSide.Front ||
                targetDefeatedByAttack;
        }

        public static bool CanPiercePastTarget(
            EnemyArchetype archetype,
            AttackSide side,
            bool targetDefeatedByAttack)
        {
            return archetype != EnemyArchetype.Shield ||
                side != AttackSide.Front ||
                targetDefeatedByAttack;
        }

        private static int GetRequiredHitCount(CombatResult result)
        {
            return result.Damage > 0
                ? (result.TargetMaxHealth + result.Damage - 1) /
                    result.Damage
                : int.MaxValue;
        }

        public static int RoundDamage(float rawDamage)
        {
            return ProgressionCurve.RoundPositiveStat(rawDamage);
        }
    }
}
