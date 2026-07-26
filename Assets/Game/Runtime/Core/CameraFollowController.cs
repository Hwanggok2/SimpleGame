using UnityEngine;

namespace SimpleGame
{
    [DefaultExecutionOrder(-100)]
    public sealed class CameraFollowController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(0f)] private float smoothTime = 0.08f;

        private Vector3 velocity;

        public void Configure(Transform followTarget)
        {
            target = followTarget;
            SnapToTarget();
        }

        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            transform.position = new Vector3(
                target.position.x,
                target.position.y,
                transform.position.z);
            velocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 destination = new(
                target.position.x,
                target.position.y,
                transform.position.z);
            transform.position = smoothTime <= 0f
                ? destination
                : Vector3.SmoothDamp(
                    transform.position,
                    destination,
                    ref velocity,
                    smoothTime);
        }
    }
}
