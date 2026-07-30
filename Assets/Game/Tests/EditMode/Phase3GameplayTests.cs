using NUnit.Framework;
using UnityEngine;

namespace SimpleGame.Tests
{
    public sealed class Phase3GameplayTests
    {
        [Test]
        public void FlyingEyeDefinitions_AllowEnemyOverlap()
        {
            Assert.That(
                PrototypeEnemyDefinitions
                    .CreateFlyingEye()
                    .AllowsEnemyOverlap,
                Is.True);
            Assert.That(
                PrototypeEnemyDefinitions
                    .CreateFlyingEyeBoss()
                    .AllowsEnemyOverlap,
                Is.True);
            Assert.That(
                PrototypeEnemyDefinitions
                    .Create(EnemyArchetype.Melee)
                    .AllowsEnemyOverlap,
                Is.False);
        }

        [Test]
        public void SkeletonBoss_BlocksLivingFrontHitsLikeShield()
        {
            EnemyDefinition definition =
                PrototypeEnemyDefinitions.CreateSkeletonBoss();

            CombatResult front = CombatResolver.Resolve(
                definition,
                definition.CalculateMaxHealth(1),
                1f,
                3f,
                AttackSide.Front,
                false);
            CombatResult rear = CombatResolver.Resolve(
                definition,
                definition.CalculateMaxHealth(1),
                1f,
                3f,
                AttackSide.Rear,
                false);
            CombatResult critical = CombatResolver.Resolve(
                definition,
                definition.CalculateMaxHealth(1),
                1f,
                3f,
                AttackSide.Front,
                true);

            Assert.That(definition.Archetype, Is.EqualTo(
                EnemyArchetype.Boss));
            Assert.That(definition.BlocksFrontAttacks, Is.True);
            Assert.That(
                front.PlayerReaction,
                Is.EqualTo(PlayerAttackReaction.Recoil));
            Assert.That(
                rear.PlayerReaction,
                Is.EqualTo(PlayerAttackReaction.None));
            Assert.That(
                critical.PlayerReaction,
                Is.EqualTo(PlayerAttackReaction.None));
            Assert.That(
                CombatResolver.CanPiercePastTarget(
                    definition,
                    AttackSide.Front,
                    false),
                Is.False);
            Assert.That(
                CombatResolver.CanPiercePastTarget(
                    definition,
                    AttackSide.Rear,
                    false),
                Is.True);
        }

        [TestCase(0, 0f, 0f, 0)]
        [TestCase(1, 0.35f, 1.2f, 1)]
        [TestCase(2, 0.45f, 1.35f, 2)]
        [TestCase(3, 0.55f, 1.5f, 3)]
        [TestCase(4, 0.65f, 1.65f, 4)]
        [TestCase(5, 0.75f, 1.8f, 5)]
        [TestCase(6, 0.75f, 1.8f, 5)]
        public void FilthThrow_UpgradeMathMatchesDesign(
            int level,
            float expectedDamage,
            float expectedRadius,
            int expectedCount)
        {
            Assert.That(
                PlayerCombatAbilities
                    .CalculateFilthThrowDamageMultiplier(level),
                Is.EqualTo(expectedDamage).Within(0.0001f));
            Assert.That(
                PlayerCombatAbilities
                    .CalculateFilthThrowRadius(level),
                Is.EqualTo(expectedRadius).Within(0.0001f));
            Assert.That(
                PlayerCombatAbilities
                    .CalculateFilthThrowCount(level),
                Is.EqualTo(expectedCount));
        }

        [TestCase(1, 6f)]
        [TestCase(2, 5.5f)]
        [TestCase(3, 5f)]
        [TestCase(4, 4.5f)]
        [TestCase(5, 4f)]
        [TestCase(6, 4f)]
        public void FilthThrow_CooldownFallsByLevel(
            int level,
            float expected)
        {
            Assert.That(
                PlayerCombatAbilities
                    .CalculateFilthThrowInterval(level),
                Is.EqualTo(expected).Within(0.0001f));
        }

        [TestCase(0.49f, 0)]
        [TestCase(0.5f, 1)]
        [TestCase(2.99f, 5)]
        [TestCase(3f, 6)]
        [TestCase(5f, 6)]
        public void FilthField_TicksSixTimesOverThreeSeconds(
            float exposureDuration,
            int expectedTicks)
        {
            Assert.That(
                FilthProjectile.CalculateTickCount(
                    exposureDuration),
                Is.EqualTo(expectedTicks));
        }

        [Test]
        public void FilthProjectile_ArcStartsPeaksAndEndsCorrectly()
        {
            Vector2 start = Vector2.zero;
            Vector2 destination = new(2f, 0f);

            Assert.That(
                FilthProjectile.CalculateArcPosition(
                    start,
                    destination,
                    0f),
                Is.EqualTo(start));
            Assert.That(
                FilthProjectile.CalculateArcPosition(
                    start,
                    destination,
                    0.5f),
                Is.EqualTo(new Vector2(1f, FilthProjectile.ArcHeight)));
            Assert.That(
                FilthProjectile.CalculateArcPosition(
                    start,
                    destination,
                    1f),
                Is.EqualTo(destination));
        }

        [Test]
        public void FilthTarget_LeavesRadiusInsideScreenBounds()
        {
            Vector2 cameraCenter = new(2f, -1f);
            Vector2 halfExtents = new(8f, 5f);
            const float radius = 1.8f;

            Vector2 minimum =
                FilthProjectile.CalculateTargetPosition(
                    cameraCenter,
                    halfExtents,
                    radius,
                    Vector2.zero);
            Vector2 maximum =
                FilthProjectile.CalculateTargetPosition(
                    cameraCenter,
                    halfExtents,
                    radius,
                    Vector2.one);

            Assert.That(
                minimum.x - radius,
                Is.GreaterThan(cameraCenter.x - halfExtents.x));
            Assert.That(
                minimum.y - radius,
                Is.GreaterThan(cameraCenter.y - halfExtents.y));
            Assert.That(
                maximum.x + radius,
                Is.LessThan(cameraCenter.x + halfExtents.x));
            Assert.That(
                maximum.y + radius,
                Is.LessThan(cameraCenter.y + halfExtents.y));
        }
    }
}
