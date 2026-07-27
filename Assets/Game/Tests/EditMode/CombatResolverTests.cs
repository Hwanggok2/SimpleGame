using NUnit.Framework;
using UnityEngine;

namespace SimpleGame.Tests
{
    public sealed class CombatResolverTests
    {
        [TestCase(
            EnemyArchetype.Melee,
            1,
            1,
            AttackSide.Front,
            1f,
            3f)]
        [TestCase(
            EnemyArchetype.Melee,
            1,
            1,
            AttackSide.Rear,
            3f,
            3f)]
        [TestCase(
            EnemyArchetype.Melee,
            1,
            2,
            AttackSide.Front,
            1f,
            5.1f)]
        [TestCase(
            EnemyArchetype.Melee,
            1,
            2,
            AttackSide.Rear,
            3f,
            5.1f)]
        [TestCase(
            EnemyArchetype.Ranged,
            1,
            1,
            AttackSide.Front,
            3f,
            3f)]
        [TestCase(
            EnemyArchetype.Ranged,
            1,
            2,
            AttackSide.Front,
            1f,
            3f)]
        [TestCase(
            EnemyArchetype.Ranged,
            1,
            3,
            AttackSide.Rear,
            3f,
            5.1f)]
        public void Resolve_UsesAttackPowerAndScaledHealth(
            EnemyArchetype archetype,
            int playerLevel,
            int enemyLevel,
            AttackSide side,
            float expectedDamage,
            float expectedMaxHealth)
        {
            EnemyDefinition definition =
                PrototypeEnemyDefinitions.Create(archetype);
            CombatResult result = CombatResolver.Resolve(
                definition,
                playerLevel,
                enemyLevel,
                PlayerAttack(playerLevel),
                3f,
                side,
                false);

            Assert.That(
                result.Damage,
                Is.EqualTo(expectedDamage).Within(0.001f));
            Assert.That(
                result.TargetMaxHealth,
                Is.EqualTo(expectedMaxHealth).Within(0.001f));
        }

        [Test]
        public void Resolve_CriticalMultipliesDamageWithoutArmor()
        {
            EnemyDefinition definition =
                PrototypeEnemyDefinitions.Create(EnemyArchetype.Melee);

            CombatResult front = CombatResolver.Resolve(
                definition,
                1,
                2,
                1f,
                3f,
                AttackSide.Front,
                true);
            CombatResult rear = CombatResolver.Resolve(
                definition,
                1,
                2,
                1f,
                3f,
                AttackSide.Rear,
                true);

            Assert.That(front.Damage, Is.EqualTo(3f));
            Assert.That(rear.Damage, Is.EqualTo(9f));
        }

        [TestCase(3, 1, false)]
        [TestCase(2, 1, true)]
        [TestCase(1, 1, true)]
        [TestCase(1, 2, true)]
        public void Resolve_ShieldFrontRecoilUsesOneHitException(
            int playerLevel,
            int enemyLevel,
            bool expectedRecoil)
        {
            EnemyDefinition definition =
                PrototypeEnemyDefinitions.Create(EnemyArchetype.Shield);
            CombatResult result = CombatResolver.Resolve(
                definition,
                playerLevel,
                enemyLevel,
                PlayerAttack(playerLevel),
                3f,
                AttackSide.Front,
                false);

            Assert.That(
                result.PlayerReaction == PlayerAttackReaction.Recoil,
                Is.EqualTo(expectedRecoil));
        }

        [Test]
        public void Resolve_CriticalShieldFrontHitBypassesRecoil()
        {
            EnemyDefinition definition =
                PrototypeEnemyDefinitions.Create(EnemyArchetype.Shield);
            CombatResult result = CombatResolver.Resolve(
                definition,
                1,
                2,
                1f,
                3f,
                AttackSide.Front,
                true);

            Assert.That(result.Damage, Is.EqualTo(3f));
            Assert.That(
                result.PlayerReaction,
                Is.EqualTo(PlayerAttackReaction.None));
        }

        [TestCase(EnemyArchetype.Melee)]
        [TestCase(EnemyArchetype.Ranged)]
        [TestCase(EnemyArchetype.Boss)]
        public void Resolve_HigherLevelNonShieldNeverLocksPlayerInput(
            EnemyArchetype archetype)
        {
            EnemyDefinition definition =
                PrototypeEnemyDefinitions.Create(archetype);
            CombatResult result = CombatResolver.Resolve(
                definition,
                1,
                20,
                1f,
                3f,
                AttackSide.Front,
                false);

            Assert.That(
                result.PlayerReaction,
                Is.EqualTo(PlayerAttackReaction.None));
        }

        [TestCase(true, true, PlayerAttackReaction.Recoil, CombatFeedbackLevel.CriticalHit)]
        [TestCase(false, false, PlayerAttackReaction.Recoil, CombatFeedbackLevel.FrontRecoil)]
        [TestCase(true, false, PlayerAttackReaction.None, CombatFeedbackLevel.NormalHit)]
        [TestCase(false, false, PlayerAttackReaction.None, CombatFeedbackLevel.None)]
        public void FeedbackResolver_SelectsOnlyTheLargestFeedback(
            bool damageApplied,
            bool critical,
            PlayerAttackReaction reaction,
            CombatFeedbackLevel expected)
        {
            Assert.That(
                CombatFeedbackResolver.Resolve(
                    damageApplied,
                    critical,
                    reaction),
                Is.EqualTo(expected));
        }

        [Test]
        public void ShieldDefinition_UsesCyanApproachRange()
        {
            Assert.That(
                PrototypeEnemyDefinitions
                    .Create(EnemyArchetype.Shield)
                    .ApproachRange,
                Is.EqualTo(2.25f));
        }

        [TestCase(1, 2)]
        [TestCase(10, 3)]
        [TestCase(14, 4)]
        public void EnemyAttackDamage_ScalesGraduallyWithLevel(
            int level,
            int expectedDamage)
        {
            Assert.That(
                PrototypeEnemyDefinitions
                    .Create(EnemyArchetype.Melee)
                    .CalculateAttackDamage(level),
                Is.EqualTo(expectedDamage));
        }

        [Test]
        public void GetAttackSide_UsesEnemyFacing()
        {
            Assert.That(
                CombatResolver.GetAttackSide(
                    Vector2.up,
                    Vector2.zero,
                    Vector2.up),
                Is.EqualTo(AttackSide.Front));
            Assert.That(
                CombatResolver.GetAttackSide(
                    Vector2.up,
                    Vector2.zero,
                    Vector2.down),
                Is.EqualTo(AttackSide.Rear));
        }

        [TestCase(
            EnemyArchetype.Melee,
            2,
            1,
            EnemyThreatLevel.OneHit)]
        [TestCase(
            EnemyArchetype.Ranged,
            1,
            1,
            EnemyThreatLevel.OneHit)]
        [TestCase(
            EnemyArchetype.Melee,
            1,
            1,
            EnemyThreatLevel.ThreeFrontOneRear)]
        [TestCase(
            EnemyArchetype.Ranged,
            1,
            2,
            EnemyThreatLevel.ThreeFrontOneRear)]
        [TestCase(
            EnemyArchetype.Melee,
            1,
            2,
            EnemyThreatLevel.Dangerous)]
        [TestCase(
            EnemyArchetype.Ranged,
            1,
            3,
            EnemyThreatLevel.Dangerous)]
        public void ThreatLevel_UsesCurrentHitCounts(
            EnemyArchetype archetype,
            int playerLevel,
            int enemyLevel,
            EnemyThreatLevel expected)
        {
            EnemyDefinition definition =
                PrototypeEnemyDefinitions.Create(archetype);
            Assert.That(
                CombatResolver.GetThreatLevel(
                    definition,
                    playerLevel,
                    enemyLevel,
                    PlayerAttack(playerLevel),
                    3f),
                Is.EqualTo(expected));
        }

        private static float PlayerAttack(int level)
        {
            return Mathf.Pow(1.7f, level - 1);
        }
    }
}
