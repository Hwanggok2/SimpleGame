using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame
{
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text valueLabel;
        private EnemyHealth health;

        public void Configure(
            GameObject configuredVisualRoot,
            Slider configuredSlider,
            TMP_Text configuredValueLabel)
        {
            visualRoot = configuredVisualRoot;
            slider = configuredSlider;
            valueLabel = configuredValueLabel;
        }

        public void Bind(EnemyHealth configuredHealth, bool visible)
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

            SetVisible(visible);
        }

        public void SetVisible(bool visible)
        {
            if (visualRoot != null)
            {
                visualRoot.SetActive(visible);
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

            if (valueLabel != null)
            {
                valueLabel.text = $"{current}/{maximum}";
            }
        }
    }
}
