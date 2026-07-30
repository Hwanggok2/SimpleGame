using UnityEngine;

namespace SimpleGame
{
    public sealed class HealthPickup : MonoBehaviour
    {
        public const int HealAmount = 5;
        public const float Lifetime = 45f;

        private HealthPickupSpawner owner;
        private float remainingLifetime;
        private bool released;

        public void Configure(HealthPickupSpawner configuredOwner)
        {
            owner = configuredOwner;
            remainingLifetime = Lifetime;
            released = false;
        }

        private void Update()
        {
            if (released ||
                (owner != null && !owner.IsSessionPlaying))
            {
                return;
            }

            remainingLifetime -= Time.deltaTime;
            if (remainingLifetime <= 0f)
            {
                Release();
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryCollect(other.GetComponentInParent<PlayerRoot>());
        }

        public bool TryCollect(PlayerRoot player)
        {
            if (released ||
                player == null ||
                player.Health == null ||
                player.Health.Heal(HealAmount) <= 0)
            {
                return false;
            }

            Release();
            return true;
        }

        private void Release()
        {
            if (released)
            {
                return;
            }

            released = true;
            owner?.NotifyReleased(this);
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
