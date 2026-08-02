using UnityEngine;

namespace SimpleGame
{
    [DefaultExecutionOrder(100)]
    public sealed class CameraShakeController : MonoBehaviour
    {
        private Vector3 lastOffset;
        private float activeStrength;
        private float shakeEndsAt;

        public void Play(float strength, float duration)
        {
            if (Time.unscaledTime >= shakeEndsAt)
            {
                ResetPosition();
            }

            if (strength < activeStrength)
            {
                return;
            }

            activeStrength = Mathf.Max(0f, strength);
            shakeEndsAt = Time.unscaledTime + Mathf.Max(0f, duration);
        }

        private void Update()
        {
            transform.localPosition -= lastOffset;
            lastOffset = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (activeStrength <= 0f || Time.unscaledTime >= shakeEndsAt)
            {
                ResetPosition();
                return;
            }

            float time = Time.unscaledTime;
            Vector2 offset = new Vector2(
                Mathf.Sin(time * 73f),
                Mathf.Sin(time * 91f)) * activeStrength;
            lastOffset = new Vector3(offset.x, offset.y, 0f);
            transform.localPosition += lastOffset;
        }

        private void OnDisable()
        {
            ResetPosition();
        }

        private void ResetPosition()
        {
            transform.localPosition -= lastOffset;
            lastOffset = Vector3.zero;
            activeStrength = 0f;
            shakeEndsAt = 0f;
        }
    }
}
