using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame
{
    public enum HudButtonId
    {
        Pause,
        CriticalCard,
        ContinueAd,
        DamagePlayer,
        GrantXp,
        Count
    }

    public enum HudTextId
    {
        Score,
        Time,
        PlayerLevel,
        CriticalChance,
        PlayerHp,
        Hint,
        GameOverTitle,
        Count
    }

    public sealed class PrototypeHUDView : MonoBehaviour
    {
        [SerializeField] private Transform textRoot;
        [SerializeField] private Transform buttonRoot;
        [SerializeField] private GameObject criticalCardPanel;
        [SerializeField] private GameObject gameOverPanel;

        private TMP_Text[] texts;
        private Button[] buttons;

        public void Configure(
            Transform configuredTextRoot,
            Transform configuredButtonRoot,
            GameObject cardPanel,
            GameObject overPanel)
        {
            textRoot = configuredTextRoot;
            buttonRoot = configuredButtonRoot;
            criticalCardPanel = cardPanel;
            gameOverPanel = overPanel;
        }

        public void Initialize()
        {
            texts = new TMP_Text[(int)HudTextId.Count];
            buttons = new Button[(int)HudButtonId.Count];

            foreach (TMP_Text label in textRoot.GetComponentsInChildren<TMP_Text>(true))
            {
                if (Enum.TryParse(label.gameObject.name, out HudTextId id) &&
                    id != HudTextId.Count)
                {
                    texts[(int)id] = label;
                }
            }

            foreach (Button button in buttonRoot.GetComponentsInChildren<Button>(true))
            {
                if (Enum.TryParse(button.gameObject.name, out HudButtonId id) &&
                    id != HudButtonId.Count)
                {
                    buttons[(int)id] = button;
                }
            }

            ValidateBindings();
            ShowCriticalCard(false);
            ShowGameOver(false);
        }

        public void Bind(HudButtonId id, Action callback)
        {
            Button button = buttons[(int)id];
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => callback());
        }

        public void SetText(HudTextId id, string value)
        {
            TMP_Text label = texts[(int)id];
            if (label != null)
            {
                label.text = value;
            }
        }

        public void ShowCriticalCard(bool visible)
        {
            criticalCardPanel.SetActive(visible);
        }

        public void ShowGameOver(bool visible)
        {
            gameOverPanel.SetActive(visible);
        }

        private void ValidateBindings()
        {
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] == null)
                {
                    Debug.LogError($"HUD Text binding missing: {(HudTextId)i}", this);
                }
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null)
                {
                    Debug.LogError($"HUD Button binding missing: {(HudButtonId)i}", this);
                }
            }
        }
    }
}
