using NUnit.Framework;
using UnityEngine;

namespace SimpleGame.Tests
{
    public sealed class BossAttackPatternTests
    {
        [TestCase("GoblinBoss")]
        [TestCase("MushroomBoss")]
        [TestCase("FlyingEyeBoss")]
        [TestCase("SkeletonBoss")]
        public void Profiles_AlternateTwoDistinctAttackPatterns(
            string enemyId)
        {
            BossAttackPattern first = BossAttackPatterns.Get(
                enemyId,
                0);
            BossAttackPattern second = BossAttackPatterns.Get(
                enemyId,
                1);
            BossAttackPattern third = BossAttackPatterns.Get(
                enemyId,
                2);

            Assert.That(first.AnimationVariant, Is.EqualTo(1));
            Assert.That(second.AnimationVariant, Is.EqualTo(2));
            Assert.That(third.AnimationVariant, Is.EqualTo(1));
            Assert.That(
                second.IndicatorSize,
                Is.Not.EqualTo(first.IndicatorSize));
            Assert.That(
                third.IndicatorSize,
                Is.EqualTo(first.IndicatorSize));
        }

        [Test]
        public void ForwardBox_UsesTheSameLockedBoundsAsItsIndicator()
        {
            var pattern = new BossAttackPattern(
                1,
                BossAttackShape.ForwardBox,
                2f,
                1f);
            Vector2 origin = new(10f, 5f);
            Vector2 direction = Vector2.up;

            Assert.That(
                pattern.GetCenter(origin, direction),
                Is.EqualTo(new Vector2(10f, 6f)));
            Assert.That(
                pattern.IndicatorSize,
                Is.EqualTo(new Vector2(2f, 1f)));
            Assert.That(
                pattern.Contains(
                    origin,
                    direction,
                    new Vector2(10.49f, 7f)),
                Is.True);
            Assert.That(
                pattern.Contains(
                    origin,
                    direction,
                    new Vector2(10f, 4.99f)),
                Is.False);
            Assert.That(
                pattern.Contains(
                    origin,
                    direction,
                    new Vector2(10.51f, 6f)),
                Is.False);
        }

        [Test]
        public void CenteredBox_UsesHalfOfEachIndicatorDimension()
        {
            var pattern = new BossAttackPattern(
                2,
                BossAttackShape.CenteredBox,
                4f,
                2f);
            Vector2 origin = new(2f, 3f);

            Assert.That(
                pattern.GetCenter(origin, Vector2.right),
                Is.EqualTo(origin));
            Assert.That(
                pattern.Contains(
                    origin,
                    Vector2.right,
                    new Vector2(4f, 4f)),
                Is.True);
            Assert.That(
                pattern.Contains(
                    origin,
                    Vector2.right,
                    new Vector2(4.01f, 3f)),
                Is.False);
            Assert.That(
                pattern.Contains(
                    origin,
                    Vector2.right,
                    new Vector2(2f, 4.01f)),
                Is.False);
        }
    }
}
