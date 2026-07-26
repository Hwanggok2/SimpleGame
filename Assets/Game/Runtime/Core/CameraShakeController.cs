using UnityEngine;

namespace SimpleGame
{
    public sealed class CameraShakeController : MonoBehaviour
    {
        private Vector3 restingLocalPosition;
        private float activeStrength;
        private float shakeEndsAt;

        private void Awake()
        {
            restingLocalPosition = transform.localPosition;
        }

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
            transform.localPosition = restingLocalPosition +
                new Vector3(offset.x, offset.y, 0f);
        }

        private void OnDisable()
        {
            ResetPosition();
        }

        private void ResetPosition()
        {
            if (activeStrength <= 0f)
            {
                return;
            }

            transform.localPosition = restingLocalPosition;
            activeStrength = 0f;
            shakeEndsAt = 0f;
        }
    }
}
