using UnityEngine;

namespace SimpleGame
{
    public sealed class RangedArrowProjectile : MonoBehaviour
    {
        public const float DefaultSpeed = 11f;
        public const float DefaultHitRadius = 0.34f;

        private PlayerRoot target;
        private Vector2 direction = Vector2.right;
        private int damage;
        private float remainingDistance;
        [SerializeField, Min(0.1f)]
        [Tooltip("Arrow travel speed in world units per second.")]
        private float speed = DefaultSpeed;
        private float hitRadius = DefaultHitRadius;
        private bool launched;

        public float Speed => Mathf.Max(0.1f, speed);

        public void Launch(
            PlayerRoot configuredTarget,
            Vector2 origin,
            Vector2 travelDirection,
            int configuredDamage,
            float maximumDistance)
        {
            target = configuredTarget;
            transform.position = origin;
            direction = travelDirection.sqrMagnitude > 0.0001f
                ? travelDirection.normalized
                : Vector2.right;
            damage = Mathf.Max(0, configuredDamage);
            remainingDistance = Mathf.Max(0f, maximumDistance);
            launched = remainingDistance > 0f;
            transform.rotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        private void Update()
        {
            if (!launched || remainingDistance <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            Vector2 start = transform.position;
            float stepDistance = Mathf.Min(
                Speed * Time.deltaTime,
                remainingDistance);
            Vector2 end = start + direction * stepDistance;
            transform.position = end;
            remainingDistance -= stepDistance;

            if (target != null &&
                target.IsAlive &&
                SegmentHitsCircle(
                    start,
                    end,
                    target.transform.position,
                    hitRadius))
            {
                target.ReceiveDamage(damage);
                launched = false;
                Destroy(gameObject);
                return;
            }

            if (remainingDistance <= 0f)
            {
                launched = false;
                Destroy(gameObject);
            }
        }

        public static bool SegmentHitsCircle(
            Vector2 start,
            Vector2 end,
            Vector2 center,
            float radius)
        {
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            float progress = lengthSquared > 0.0001f
                ? Mathf.Clamp01(
                    Vector2.Dot(center - start, segment) /
                    lengthSquared)
                : 0f;
            Vector2 closest = start + segment * progress;
            float safeRadius = Mathf.Max(0f, radius);
            return (center - closest).sqrMagnitude <=
                safeRadius * safeRadius;
        }
    }
}
