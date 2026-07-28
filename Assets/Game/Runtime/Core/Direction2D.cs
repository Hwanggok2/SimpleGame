using UnityEngine;

namespace SimpleGame
{
    public static class Direction2D
    {
        public const float DefaultHorizontalDeadZone = 0.01f;

        public static int GetHorizontalSign(
            Vector2 direction,
            float deadZone = DefaultHorizontalDeadZone)
        {
            if (Mathf.Abs(direction.x) <= Mathf.Max(0f, deadZone))
            {
                return 0;
            }

            return direction.x < 0f ? -1 : 1;
        }
    }
}
