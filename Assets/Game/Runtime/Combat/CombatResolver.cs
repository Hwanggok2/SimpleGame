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
            EnemyArchetype archetype,
            int playerLevel,
            int enemyLevel,
            AttackSide side,
            bool critical)
        {
            GetDamageModel(
                archetype,
                playerLevel,
                enemyLevel,
                out int durability,
                out int frontDamage,
                out int rearDamage);

            int damage = side == AttackSide.Front ? frontDamage : rearDamage;
            if (critical)
            {
                damage = side == AttackSide.Front
                    ? rearDamage
                    : rearDamage * 3;
            }

            bool causesRecoil = side == AttackSide.Front &&
                ((archetype == EnemyArchetype.Shield &&
                    enemyLevel >= playerLevel - 1) ||
                 ((archetype == EnemyArchetype.Melee ||
                   archetype == EnemyArchetype.Ranged) &&
                  damage == 0));

            return new CombatResult(
                damage,
                durability,
                causesRecoil
                    ? PlayerAttackReaction.Recoil
                    : PlayerAttackReaction.None);
        }

        public static EnemyThreatLevel GetThreatLevel(
            EnemyArchetype archetype,
            int playerLevel,
            int enemyLevel)
        {
            CombatResult front = Resolve(
                archetype,
                playerLevel,
                enemyLevel,
                AttackSide.Front,
                false);
            CombatResult rear = Resolve(
                archetype,
                playerLevel,
                enemyLevel,
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

        private static int GetRequiredHitCount(CombatResult result)
        {
            return result.Damage > 0
                ? Mathf.CeilToInt(
                    (float)result.RequiredDurability / result.Damage)
                : int.MaxValue;
        }

        private static void GetDamageModel(
            EnemyArchetype archetype,
            int playerLevel,
            int enemyLevel,
            out int durability,
            out int frontDamage,
            out int rearDamage)
        {
            if (archetype == EnemyArchetype.Boss)
            {
                durability = 15;
                frontDamage = 1;
                rearDamage = 3;
                return;
            }

            if (archetype == EnemyArchetype.Shield)
            {
                durability = 3;
                frontDamage = 1;
                rearDamage = 3;
                return;
            }

            int levelDifference = enemyLevel - playerLevel;
            if (levelDifference < 0)
            {
                durability = 1;
                frontDamage = 1;
                rearDamage = 1;
                return;
            }

            if (archetype == EnemyArchetype.Ranged)
            {
                if (levelDifference == 0)
                {
                    durability = 1;
                    frontDamage = 1;
                    rearDamage = 1;
                    return;
                }

                if (levelDifference == 1)
                {
                    durability = 3;
                    frontDamage = 1;
                    rearDamage = 3;
                    return;
                }

                durability = levelDifference;
                frontDamage = 0;
                rearDamage = 1;
                return;
            }

            if (levelDifference == 0)
            {
                durability = 3;
                frontDamage = 1;
                rearDamage = 3;
                return;
            }

            durability = levelDifference + 1;
            frontDamage = 0;
            rearDamage = 1;
        }
    }
}
