using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame
{
    public sealed class PlayerHealthBar : MonoBehaviour
    {
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private Slider slider;
        private HealthComponent health;

        public void Configure(
            GameObject configuredVisualRoot,
            Slider configuredSlider)
        {
            visualRoot = configuredVisualRoot;
            slider = configuredSlider;
        }

        public void Bind(HealthComponent configuredHealth)
        {
            if (health != null)
            {
                health.Changed -= Refresh;
            }

            health = configuredHealth;
            if (health != null)
            {
                health.Changed += Refresh;
                Refresh(health.CurrentHealth, health.MaxHealth);
            }

            if (visualRoot != null)
            {
                visualRoot.SetActive(true);
            }
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Changed -= Refresh;
            }
        }

        private void Refresh(int current, int maximum)
        {
            if (slider != null)
            {
                slider.value = maximum > 0
                    ? (float)current / maximum
                    : 0f;
            }
        }
    }
}
