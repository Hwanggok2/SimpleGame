using UnityEngine;

namespace SimpleGame
{
    public sealed class EnemyFacing : MonoBehaviour
    {
        [SerializeField] private Vector2 direction = Vector2.down;
        [SerializeField, Min(0f)] private float turnDelay;

        private int horizontalSign;
        private int pendingHorizontalSign;
        private float turnRequestedAt;

        public Vector2 Direction => direction;

        public void Configure(float configuredTurnDelay)
        {
            turnDelay = Mathf.Max(0f, configuredTurnDelay);
            horizontalSign = GetHorizontalSign(direction);
            pendingHorizontalSign = 0;
        }

        public void Face(Vector2 targetPosition)
        {
            Face(targetPosition, Time.time);
        }

        public void FaceImmediate(Vector2 targetPosition)
        {
            Vector2 next = targetPosition - (Vector2)transform.position;
            if (next.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector2 desiredDirection = next.normalized;
            SetDirection(
                desiredDirection,
                GetHorizontalSign(desiredDirection));
        }

        public void Face(Vector2 targetPosition, float currentTime)
        {
            Vector2 next = targetPosition - (Vector2)transform.position;
            if (next.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector2 desiredDirection = next.normalized;
            int desiredHorizontalSign = GetHorizontalSign(desiredDirection);
            if (turnDelay <= 0f ||
                horizontalSign == 0 ||
                desiredHorizontalSign == 0 ||
                desiredHorizontalSign == horizontalSign)
            {
                SetDirection(desiredDirection, desiredHorizontalSign);
                return;
            }

            if (pendingHorizontalSign != desiredHorizontalSign)
            {
                pendingHorizontalSign = desiredHorizontalSign;
                turnRequestedAt = currentTime;
                return;
            }

            if (currentTime - turnRequestedAt >= turnDelay)
            {
                SetDirection(desiredDirection, desiredHorizontalSign);
            }
        }

        private void SetDirection(Vector2 value, int nextHorizontalSign)
        {
            direction = value;
            if (nextHorizontalSign != 0)
            {
                horizontalSign = nextHorizontalSign;
            }

            pendingHorizontalSign = 0;
        }

        private static int GetHorizontalSign(Vector2 value)
        {
            if (Mathf.Abs(value.x) <= 0.01f)
            {
                return 0;
            }

            return value.x < 0f ? -1 : 1;
        }
    }
}
