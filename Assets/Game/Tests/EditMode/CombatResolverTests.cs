using NUnit.Framework;
using UnityEngine;

namespace SimpleGame.Tests
{
    public sealed class CombatResolverTests
    {
        [TestCase(EnemyArchetype.Ranged, 1, 1, AttackSide.Front, 1, 1)]
        [TestCase(EnemyArchetype.Ranged, 1, 2, AttackSide.Front, 1, 3)]
        [TestCase(EnemyArchetype.Ranged, 1, 3, AttackSide.Front, 0, 2)]
        [TestCase(EnemyArchetype.Ranged, 1, 3, AttackSide.Rear, 1, 2)]
        [TestCase(EnemyArchetype.Melee, 1, 1, AttackSide.Front, 1, 3)]
        [TestCase(EnemyArchetype.Melee, 1, 1, AttackSide.Rear, 3, 3)]
        [TestCase(EnemyArchetype.Melee, 1, 2, AttackSide.Front, 0, 2)]
        [TestCase(EnemyArchetype.Melee, 1, 2, AttackSide.Rear, 1, 2)]
        public void Resolve_MatchesDesignTable(
            EnemyArchetype archetype,
            int playerLevel,
            int enemyLevel,
            AttackSide side,
            int expectedDamage,
            int expectedDurability)
        {
            CombatResult result = CombatResolver.Resolve(
                archetype,
                playerLevel,
                enemyLevel,
                side,
                false);

            Assert.That(result.Damage, Is.EqualTo(expectedDamage));
            Assert.That(result.RequiredDurability, Is.EqualTo(expectedDurability));
        }

        [Test]
        public void Resolve_RearCritical_UsesThreeRearHits()
        {
            CombatResult result = CombatResolver.Resolve(
                EnemyArchetype.Boss,
                1,
                5,
                AttackSide.Rear,
                true);

            Assert.That(result.Damage, Is.EqualTo(9));
            Assert.That(result.RequiredDurability, Is.EqualTo(15));
        }

        [TestCase(EnemyArchetype.Shield, 3, 1, AttackSide.Front, false)]
        [TestCase(EnemyArchetype.Shield, 2, 1, AttackSide.Front, true)]
        [TestCase(EnemyArchetype.Shield, 1, 1, AttackSide.Front, true)]
        [TestCase(EnemyArchetype.Shield, 1, 2, AttackSide.Front, true)]
        [TestCase(EnemyArchetype.Shield, 1, 2, AttackSide.Rear, false)]
        [TestCase(EnemyArchetype.Melee, 1, 2, AttackSide.Front, true)]
        [TestCase(EnemyArchetype.Ranged, 1, 3, AttackSide.Front, true)]
        [TestCase(EnemyArchetype.Melee, 1, 1, AttackSide.Front, false)]
        [TestCase(EnemyArchetype.Ranged, 1, 2, AttackSide.Front, false)]
        [TestCase(EnemyArchetype.Boss, 1, 5, AttackSide.Front, false)]
        public void Resolve_AssignsFrontRecoilOnlyToDesignedCases(
            EnemyArchetype archetype,
            int playerLevel,
            int enemyLevel,
            AttackSide side,
            bool expectedRecoil)
        {
            CombatResult result = CombatResolver.Resolve(
                archetype,
                playerLevel,
                enemyLevel,
                side,
                false);

            Assert.That(
                result.PlayerReaction == PlayerAttackReaction.Recoil,
                Is.EqualTo(expectedRecoil));
        }

        [Test]
        public void Resolve_CriticalFrontHitBypassesDamageImmunityAndRecoil()
        {
            CombatResult result = CombatResolver.Resolve(
                EnemyArchetype.Melee,
                1,
                2,
                AttackSide.Front,
                true);

            Assert.That(result.Damage, Is.GreaterThan(0));
            Assert.That(result.PlayerReaction, Is.EqualTo(PlayerAttackReaction.None));
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
                CombatFeedbackResolver.Resolve(damageApplied, critical, reaction),
                Is.EqualTo(expected));
        }

        [Test]
        public void ShieldDefinition_UsesCyanApproachRange()
        {
            Assert.That(
                PrototypeEnemyDefinitions.Create(EnemyArchetype.Shield).ApproachRange,
                Is.EqualTo(2.25f));
        }

        [Test]
        public void GetAttackSide_UsesEnemyFacing()
        {
            Assert.That(
                CombatResolver.GetAttackSide(Vector2.up, Vector2.zero, Vector2.up),
                Is.EqualTo(AttackSide.Front));
            Assert.That(
                CombatResolver.GetAttackSide(Vector2.up, Vector2.zero, Vector2.down),
                Is.EqualTo(AttackSide.Rear));
        }
    }
}
