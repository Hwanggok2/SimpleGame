using UnityEngine;

namespace SimpleGame
{
    public sealed class MushroomPoisonCloud : MonoBehaviour
    {
        public const float SpawnDelay = 1f;
        public const float Duration = 5f;
        public const float TickInterval = 0.5f;
        public const int DamagePerTick = 1;
        public const float DamageRadius = 1.6f;

        private PlayerRoot player;
        private float remainingDuration;
        private float exposureTime;

        public void Configure(PlayerRoot configuredPlayer)
        {
            player = configuredPlayer;
            remainingDuration = Duration;
            exposureTime = 0f;
        }

        private void Update()
        {
            float activeDeltaTime = Mathf.Min(
                Time.deltaTime,
                Mathf.Max(0f, remainingDuration));
            remainingDuration -= Time.deltaTime;

            if (player == null ||
                !player.IsAlive ||
                Vector2.Distance(
                    transform.position,
                    player.transform.position) > DamageRadius)
            {
                exposureTime = 0f;
                ReleaseIfExpired();
                return;
            }

            exposureTime += activeDeltaTime;
            int tickCount = CalculateTickCount(exposureTime);
            if (tickCount > 0)
            {
                exposureTime -= tickCount * TickInterval;
                for (int index = 0;
                     index < tickCount && player.IsAlive;
                     index++)
                {
                    player.ReceiveDamage(DamagePerTick);
                }
            }

            ReleaseIfExpired();
        }

        public static int CalculateTickCount(float exposureDuration)
        {
            return Mathf.Max(
                0,
                Mathf.FloorToInt(
                    (Mathf.Max(0f, exposureDuration) + 0.0001f) /
                    TickInterval));
        }

        private void ReleaseIfExpired()
        {
            if (remainingDuration <= 0f)
            {
                Release();
            }
        }

        private void Release()
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }
    }
}
