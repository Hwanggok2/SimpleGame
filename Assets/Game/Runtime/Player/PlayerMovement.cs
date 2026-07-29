using System.Collections;
using UnityEngine;

namespace SimpleGame
{
    public sealed class PlayerMovement : MonoBehaviour
    {
        public const float DefaultMoveSpeed = 10f;
        public const float DefaultArrivalTolerance = 0.08f;
        public const float MaximumSpeedTravelDuration = 0.15f;
        private const float MinimumSpeedFactor = 0.15f;

        [SerializeField, Min(0.01f)]
        private float moveSpeed = DefaultMoveSpeed;
        [SerializeField, Min(0.01f)]
        private float accelerationSmoothTime = 0.06f;
        [SerializeField, Min(0.01f)]
        private float decelerationSmoothTime = 0.05f;

        private float activeSpeedMultiplier = 1f;
        private float currentMoveSpeed;
        private float speedSmoothVelocity;
        private Vector2 activeDirection;
        private bool isMoveActive;
        private bool maximumSpeedActive;
        private float maximumTravelSpeed;
        private CharacterSpriteAnimator characterAnimation;

        public bool IsMaximumSpeedActive => maximumSpeedActive;

        public void Configure(
            float configuredMoveSpeed,
            CharacterSpriteAnimator animation)
        {
            SetMoveSpeed(configuredMoveSpeed);
            characterAnimation = animation;
        }

        public void SetMoveSpeed(float value)
        {
            moveSpeed = Mathf.Max(0.01f, value);
        }

        public void SetMaximumSpeedActive(bool active)
        {
            maximumSpeedActive = active;
            if (!active)
            {
                maximumTravelSpeed = 0f;
            }
        }

        public void BeginMove(Vector2 destination)
        {
            BeginMove(destination, 1f);
        }

        public void BeginMove(
            Vector2 destination,
            float speedMultiplier)
        {
            Vector2 direction =
                destination - (Vector2)transform.position;
            if (currentMoveSpeed > 0.01f &&
                activeDirection.sqrMagnitude > 0.0001f &&
                Vector2.Dot(
                    direction.normalized,
                    activeDirection) < 0f)
            {
                ResetMomentum();
            }

            activeSpeedMultiplier = Mathf.Max(0.01f, speedMultiplier);
            activeDirection = direction.normalized;
            isMoveActive = true;
            maximumTravelSpeed = maximumSpeedActive
                ? CalculateMaximumTravelSpeed(direction.magnitude) *
                    activeSpeedMultiplier
                : 0f;
            characterAnimation?.SetMoving(direction);
        }

        public bool StepTowards(
            Vector2 destination,
            float stoppingDistance,
            bool preserveMomentumOnReach = false)
        {
            Vector2 current = transform.position;
            if (Vector2.Distance(current, destination) <= stoppingDistance)
            {
                CompleteMove(preserveMomentumOnReach);
                return true;
            }

            if (!isMoveActive)
            {
                BeginMove(destination);
            }

            Vector2 direction = destination - current;
            activeDirection = direction.normalized;
            Vector2 targetPosition = destination -
                direction.normalized * stoppingDistance;
            float targetSpeed = maximumSpeedActive
                ? maximumTravelSpeed
                : moveSpeed * activeSpeedMultiplier;
            float brakingDistance = Mathf.Max(
                DefaultArrivalTolerance,
                targetSpeed * decelerationSmoothTime);
            float brakingProgress = Mathf.Clamp01(
                Vector2.Distance(current, targetPosition) /
                brakingDistance);
            float brakingFactor = Mathf.Lerp(
                MinimumSpeedFactor,
                1f,
                SmootherStep(brakingProgress));
            float desiredSpeed = targetSpeed * brakingFactor;
            if (maximumSpeedActive)
            {
                currentMoveSpeed = targetSpeed;
            }
            else
            {
                float smoothTime = desiredSpeed >= currentMoveSpeed
                    ? accelerationSmoothTime
                    : decelerationSmoothTime;
                currentMoveSpeed = Mathf.SmoothDamp(
                    currentMoveSpeed,
                    desiredSpeed,
                    ref speedSmoothVelocity,
                    smoothTime);
            }
            Vector2 next = Vector2.MoveTowards(
                current,
                targetPosition,
                currentMoveSpeed * Time.deltaTime);
            characterAnimation?.SetMoving(direction);
            transform.position = new Vector3(next.x, next.y, transform.position.z);

            bool reached = next == targetPosition ||
                Vector2.Distance(next, destination) <= stoppingDistance;
            if (reached)
            {
                CompleteMove(preserveMomentumOnReach);
            }

            return reached;
        }

        public void CancelMove()
        {
            isMoveActive = false;
            ResetMomentum();
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

        private void CompleteMove(bool preserveMomentum)
        {
            isMoveActive = false;
            if (!preserveMomentum)
            {
                ResetMomentum();
            }

            characterAnimation?.SetIdle();
        }

        private void ResetMomentum()
        {
            currentMoveSpeed = 0f;
            speedSmoothVelocity = 0f;
            activeDirection = Vector2.zero;
            maximumTravelSpeed = 0f;
        }

        public static float CalculateMaximumTravelSpeed(float distance)
        {
            return Mathf.Max(0f, distance) /
                MaximumSpeedTravelDuration;
        }

        private static float SmootherStep(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }
    }
}
