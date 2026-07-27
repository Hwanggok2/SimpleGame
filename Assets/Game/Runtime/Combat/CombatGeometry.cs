using UnityEngine;

namespace SimpleGame
{
    public static class CombatGeometry
    {
        public static float DistancePointToSegment(
            Vector2 point,
            Vector2 start,
            Vector2 end)
        {
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= 0.0001f)
            {
                return Vector2.Distance(point, start);
            }

            float progress = Mathf.Clamp01(
                Vector2.Dot(point - start, segment) / lengthSquared);
            return Vector2.Distance(point, start + segment * progress);
        }

        public static bool IsAheadAlongPath(
            Vector2 point,
            Vector2 start,
            Vector2 end)
        {
            Vector2 path = end - start;
            return path.sqrMagnitude > 0.0001f &&
                Vector2.Dot(point - start, path) > 0f;
        }

        public static bool OverlapsSegment(
            Vector2 center,
            float radius,
            Vector2 start,
            Vector2 end,
            float halfWidth)
        {
            return DistancePointToSegment(center, start, end) <=
                Mathf.Max(0f, radius) + Mathf.Max(0f, halfWidth);
        }

        public static Vector2 PushOutside(
            Vector2 position,
            int ownerId,
            Vector2 obstaclePosition,
            int obstacleId,
            float minimumDistance)
        {
            Vector2 offset = position - obstaclePosition;
            if (offset.sqrMagnitude <= 0.0001f)
            {
                int hash = unchecked(ownerId * 397) ^ obstacleId;
                float angle = (hash & 1023) / 1024f * Mathf.PI * 2f;
                offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }

            float distance = offset.magnitude;
            return distance >= minimumDistance
                ? position
                : obstaclePosition +
                    offset.normalized * minimumDistance;
        }
    }
}
