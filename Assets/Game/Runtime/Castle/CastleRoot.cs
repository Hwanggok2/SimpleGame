using UnityEngine;

namespace SimpleGame
{
    [RequireComponent(typeof(HealthComponent))]
    public sealed class CastleRoot : MonoBehaviour, IPrototypeDamageTarget
    {
        [SerializeField] private HealthComponent health;

        public HealthComponent Health => health;
        public Transform TargetTransform => transform;
        public bool IsAlive => health != null && health.IsAlive;

        public void Configure(int maxHealth)
        {
            health = GetComponent<HealthComponent>();
            health.Configure(maxHealth);
            BuildVisual();
        }

        public void ReceiveDamage(int amount)
        {
            health.ApplyDamage(amount);
        }

        public void RestoreAfterContinue()
        {
            health.RestoreFraction(0.5f);
            health.MakeInvulnerable(3f);
        }

        private void BuildVisual()
        {
            if (transform.Find("CastleVisual") != null)
            {
                return;
            }

            PrototypeVisualFactory.CreateSprite(
                transform,
                "CastleVisual",
                new Color(0.72f, 0.72f, 0.78f),
                new Vector2(2.1f, 1.8f),
                10);
            PrototypeVisualFactory.CreateWorldLabel(
                transform,
                "CASTLE",
                new Vector3(0f, 1.2f, 0f),
                3f,
                20);
        }
    }
}
