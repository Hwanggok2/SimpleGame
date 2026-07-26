using System.Collections;
using UnityEngine;

namespace SimpleGame
{
    public sealed class EnemyMovement : MonoBehaviour
    {
        [SerializeField] private float speed = 0.7f;
        private CharacterSpriteAnimator characterAnimation;

        public void Configure(
            float moveSpeed,
            CharacterSpriteAnimator animation)
        {
            speed = Mathf.Max(0f, moveSpeed);
            characterAnimation = animation;
        }

        public void StepTowards(Vector2 target)
        {
            characterAnimation?.SetMoving(
                target - (Vector2)transform.position);
            transform.position = Vector2.MoveTowards(
                transform.position,
                target,
                speed * Time.deltaTime);
        }

        public void Knockback(Vector2 destination, float duration)
        {
            characterAnimation?.SetIdle();
            StopAllCoroutines();
            StartCoroutine(KnockbackRoutine(destination, duration));
        }

        public void Stop()
        {
            characterAnimation?.SetIdle();
        }

        public void StopImmediately()
        {
            StopAllCoroutines();
            Stop();
        }

        private IEnumerator KnockbackRoutine(Vector2 destination, float duration)
        {
            Vector2 start = transform.position;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                transform.position = Vector2.Lerp(start, destination, t);
                yield return null;
            }

            transform.position = destination;
        }
    }
}
