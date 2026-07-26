using System.Collections;
using UnityEngine;

namespace SimpleGame
{
    public sealed class PlayerMovement : MonoBehaviour
    {
        public const float DefaultMoveDuration = 0.1f;

        [SerializeField, Min(0.01f)]
        private float moveDuration = DefaultMoveDuration;

        private Vector2 moveStart;
        private float moveStartedAt;
        private bool isMoveActive;
        private CharacterSpriteAnimator characterAnimation;

        public void Configure(
            float duration,
            CharacterSpriteAnimator animation)
        {
            moveDuration = Mathf.Max(0.01f, duration);
            characterAnimation = animation;
        }

        public void BeginMove()
        {
            moveStart = transform.position;
            moveStartedAt = Time.time;
            isMoveActive = true;
        }

        public bool StepTowards(Vector2 destination, float stoppingDistance)
        {
            Vector2 current = transform.position;
            if (Vector2.Distance(current, destination) <= stoppingDistance)
            {
                isMoveActive = false;
                characterAnimation?.SetIdle();
                return true;
            }

            if (!isMoveActive)
            {
                BeginMove();
            }

            Vector2 startToDestination = destination - moveStart;
            Vector2 targetPosition = destination -
                startToDestination.normalized * stoppingDistance;
            float progress = Mathf.Clamp01(
                (Time.time - moveStartedAt) / moveDuration);
            Vector2 next = Vector2.Lerp(moveStart, targetPosition, progress);
            characterAnimation?.SetMoving(destination - current);
            transform.position = new Vector3(next.x, next.y, transform.position.z);

            bool reached = progress >= 1f ||
                Vector2.Distance(next, destination) <= stoppingDistance;
            if (reached)
            {
                isMoveActive = false;
                characterAnimation?.SetIdle();
            }

            return reached;
        }

        public void CancelMove()
        {
            isMoveActive = false;
            characterAnimation?.SetIdle();
        }

        public void Knockback(Vector2 destination, float duration)
        {
            CancelMove();
            StopAllCoroutines();
            StartCoroutine(KnockbackRoutine(destination, duration));
        }

        public void StopKnockback()
        {
            CancelMove();
            StopAllCoroutines();
        }

        private IEnumerator KnockbackRoutine(Vector2 destination, float duration)
        {
            Vector2 start = transform.position;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(elapsed / duration));
                transform.position = Vector2.Lerp(start, destination, t);
                yield return null;
            }

            transform.position = destination;
        }
    }
}
