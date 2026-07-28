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
            float targetMaxHealth,
            float playerAttackPower,
            float rearAttackMultiplier,
            AttackSide side,
            bool critical)
        {
            float safeTargetMaxHealth =
                Mathf.Max(1f, targetMaxHealth);
            float damage = Mathf.Max(0f, playerAttackPower) *
                (side == AttackSide.Rear
                    ? Mathf.Max(1f, rearAttackMultiplier)
                    : 1f);
            if (critical)
            {
                damage *= 3f;
            }

            bool causesRecoil = side == AttackSide.Front &&
                definition.Archetype == EnemyArchetype.Shield &&
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
            float targetMaxHealth,
            float playerAttackPower,
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
                ? Mathf.CeilToInt(
                    result.TargetMaxHealth / result.Damage)
                : int.MaxValue;
        }
    }
}
