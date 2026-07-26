using UnityEngine;

namespace SimpleGame
{
    public sealed class MapBounds : MonoBehaviour
    {
        [SerializeField] private Vector2 min = new(-5.4f, -9.2f);
        [SerializeField] private Vector2 max = new(5.4f, 9.2f);

        public Vector2 Min => min;
        public Vector2 Max => max;

        public void Configure(Vector2 newMin, Vector2 newMax)
        {
            min = newMin;
            max = newMax;
        }

        public Vector2 Clamp(Vector2 position)
        {
            return new Vector2(
                Mathf.Clamp(position.x, min.x, max.x),
                Mathf.Clamp(position.y, min.y, max.y));
        }

        public Vector2 GetBoundaryPoint(Vector2 origin, Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector2.up;
            }

            direction.Normalize();
            float distance = float.PositiveInfinity;

            if (Mathf.Abs(direction.x) > 0.0001f)
            {
                float xEdge = direction.x > 0f ? max.x : min.x;
                float xDistance = (xEdge - origin.x) / direction.x;
                if (xDistance >= 0f)
                {
                    distance = Mathf.Min(distance, xDistance);
                }
            }

            if (Mathf.Abs(direction.y) > 0.0001f)
            {
                float yEdge = direction.y > 0f ? max.y : min.y;
                float yDistance = (yEdge - origin.y) / direction.y;
                if (yDistance >= 0f)
                {
                    distance = Mathf.Min(distance, yDistance);
                }
            }

            if (float.IsPositiveInfinity(distance))
            {
                return Clamp(origin);
            }

            return Clamp(origin + direction * distance);
        }
    }
}
