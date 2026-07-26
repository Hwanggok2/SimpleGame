using TMPro;
using UnityEngine;

namespace SimpleGame
{
    [RequireComponent(typeof(HealthComponent))]
    public sealed class CastleRoot : MonoBehaviour, IPrototypeDamageTarget
    {
        [SerializeField] private HealthComponent health;
        [SerializeField] private SpriteRenderer castleVisual;
        [SerializeField] private TMP_Text castleLabel;

        public HealthComponent Health => health;
        public Transform TargetTransform => transform;
        public bool IsAlive => health != null && health.IsAlive;

        public void ConfigureVisuals(
            SpriteRenderer configuredVisual,
            TMP_Text configuredLabel)
        {
            castleVisual = configuredVisual;
            castleLabel = configuredLabel;
        }

        public void Configure(int maxHealth)
        {
            health = GetComponent<HealthComponent>();
            health.Configure(maxHealth);
            if (castleVisual == null || castleLabel == null)
            {
                Debug.LogError(
                    "Castle requires preconfigured visual and label components.",
                    this);
            }
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

    }
}
