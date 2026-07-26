using System.Collections;
using UnityEngine;

namespace SimpleGame
{
    public sealed class EnemyMovement : MonoBehaviour
    {
        [SerializeField] private float speed = 0.7f;

        public void Configure(float moveSpeed)
        {
            speed = Mathf.Max(0f, moveSpeed);
        }

        public void StepTowards(Vector2 target)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                target,
                speed * Time.deltaTime);
        }

        public void Knockback(Vector2 destination, float duration)
        {
            StopAllCoroutines();
            StartCoroutine(KnockbackRoutine(destination, duration));
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
