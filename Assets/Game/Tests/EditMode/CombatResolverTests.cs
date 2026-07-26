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
