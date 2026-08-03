using System;
using System.Collections;
using UnityEngine;

namespace SimpleGame
{
    public sealed class EnemyMovement : MonoBehaviour
    {
        [SerializeField] private float speed = 0.7f;
        private CharacterSpriteAnimator characterAnimation;

        public bool IsKnockbackActive { get; private set; }

        public void Configure(
            float moveSpeed,
            CharacterSpriteAnimator animation)
        {
            StopAllCoroutines();
            IsKnockbackActive = false;
            speed = Mathf.Max(0f, moveSpeed);
            characterAnimation = animation;
            characterAnimation?.SetIdle();
        }

        public void StepTowards(
            Vector2 target,
            Vector2 facingDirection,
            float speedMultiplier = 1f)
        {
            characterAnimation?.SetMoving(facingDirection);
            transform.position = Vector2.MoveTowards(
                transform.position,
                target,
                speed * Mathf.Max(0f, speedMultiplier) * Time.deltaTime);
        }

        public void Knockback(
            Vector2 destination,
            float duration,
            Action positionUpdated = null)
        {
            characterAnimation?.SetIdle();
            StopAllCoroutines();
            IsKnockbackActive = true;
            if (duration <= 0f)
            {
                transform.position = destination;
                positionUpdated?.Invoke();
                IsKnockbackActive = false;
                return;
            }

            StartCoroutine(KnockbackRoutine(
                destination,
                duration,
                positionUpdated));
        }

        public void Stop()
        {
            characterAnimation?.SetIdle();
        }

        public void StopImmediately()
        {
            StopAllCoroutines();
            IsKnockbackActive = false;
            Stop();
        }

        private IEnumerator KnockbackRoutine(
            Vector2 destination,
            float duration,
            Action positionUpdated)
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
                positionUpdated?.Invoke();
                yield return null;
            }

            transform.position = destination;
            positionUpdated?.Invoke();
            IsKnockbackActive = false;
            characterAnimation?.SetIdle();
        }
    }
}
