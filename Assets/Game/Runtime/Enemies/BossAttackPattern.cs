using UnityEngine;

namespace SimpleGame
{
    public enum BossAttackShape
    {
        ForwardBox,
        CenteredBox
    }

    public readonly struct BossAttackPattern
    {
        public BossAttackPattern(
            int animationVariant,
            BossAttackShape shape,
            float length,
            float width)
        {
            AnimationVariant = Mathf.Clamp(animationVariant, 1, 2);
            Shape = shape;
            Length = Mathf.Max(0.1f, length);
            Width = Mathf.Max(0.1f, width);
        }

        public int AnimationVariant { get; }
        public BossAttackShape Shape { get; }
        public float Length { get; }
        public float Width { get; }
        public Vector2 IndicatorSize => new(Length, Width);
        public float EngagementRange =>
            Shape == BossAttackShape.ForwardBox
                ? Length
                : Mathf.Max(Length, Width) * 0.5f;

        public Vector2 GetCenter(
            Vector2 origin,
            Vector2 direction)
        {
            return Shape == BossAttackShape.ForwardBox
                ? origin + NormalizeDirection(direction) * (Length * 0.5f)
                : origin;
        }

        public float GetRotationDegrees(Vector2 direction)
        {
            Vector2 normalized = NormalizeDirection(direction);
            return Mathf.Atan2(normalized.y, normalized.x) *
                Mathf.Rad2Deg;
        }

        public bool Contains(
            Vector2 origin,
            Vector2 direction,
            Vector2 position)
        {
            Vector2 forward = NormalizeDirection(direction);
            Vector2 side = new(-forward.y, forward.x);
            Vector2 offset = position - origin;
            float forwardDistance = Vector2.Dot(offset, forward);
            float sideDistance = Mathf.Abs(Vector2.Dot(offset, side));

            if (Shape == BossAttackShape.ForwardBox)
            {
                return forwardDistance >= 0f &&
                    forwardDistance <= Length &&
                    sideDistance <= Width * 0.5f;
            }

            return Mathf.Abs(forwardDistance) <= Length * 0.5f &&
                sideDistance <= Width * 0.5f;
        }

        private static Vector2 NormalizeDirection(Vector2 direction)
        {
            return direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector2.right;
        }
    }

    public static class BossAttackPatterns
    {
        public static BossAttackPattern Get(
            string enemyId,
            int sequenceIndex)
        {
            bool secondary = (sequenceIndex & 1) != 0;
            return enemyId switch
            {
                PrototypeEnemyDefinitions.MushroomBossId => secondary
                    ? new BossAttackPattern(
                        2,
                        BossAttackShape.ForwardBox,
                        2.7f,
                        1.5f)
                    : new BossAttackPattern(
                        1,
                        BossAttackShape.ForwardBox,
                        1.9f,
                        1.1f),
                PrototypeEnemyDefinitions.FlyingEyeBossId => secondary
                    ? new BossAttackPattern(
                        2,
                        BossAttackShape.CenteredBox,
                        3.2f,
                        3.2f)
                    : new BossAttackPattern(
                        1,
                        BossAttackShape.ForwardBox,
                        3.6f,
                        0.9f),
                PrototypeEnemyDefinitions.SkeletonBossId => secondary
                    ? new BossAttackPattern(
                        2,
                        BossAttackShape.ForwardBox,
                        3f,
                        1.5f)
                    : new BossAttackPattern(
                        1,
                        BossAttackShape.ForwardBox,
                        2.3f,
                        2f),
                _ => secondary
                    ? new BossAttackPattern(
                        2,
                        BossAttackShape.ForwardBox,
                        3.3f,
                        1f)
                    : new BossAttackPattern(
                        1,
                        BossAttackShape.ForwardBox,
                        2.2f,
                        2.4f)
            };
        }
    }
}
