using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame
{
    [DisallowMultipleComponent]
    public sealed class DifficultySelectionPanelView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [SerializeField] private TMP_Text easyButtonLabel;
        [SerializeField] private TMP_Text normalButtonLabel;
        [SerializeField] private TMP_Text hardButtonLabel;
        [SerializeField] private Button easyButton;
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;

        public event Action<GameDifficulty> DifficultyRequested;

        public bool IsConfigured =>
            titleLabel != null &&
            descriptionLabel != null &&
            easyButtonLabel != null &&
            normalButtonLabel != null &&
            hardButtonLabel != null &&
            easyButton != null &&
            normalButton != null &&
            hardButton != null;

        public void ConfigureReferences(
            TMP_Text configuredTitleLabel,
            TMP_Text configuredDescriptionLabel,
            TMP_Text configuredEasyButtonLabel,
            TMP_Text configuredNormalButtonLabel,
            TMP_Text configuredHardButtonLabel,
            Button configuredEasyButton,
            Button configuredNormalButton,
            Button configuredHardButton)
        {
            titleLabel = configuredTitleLabel;
            descriptionLabel = configuredDescriptionLabel;
            easyButtonLabel = configuredEasyButtonLabel;
            normalButtonLabel = configuredNormalButtonLabel;
            hardButtonLabel = configuredHardButtonLabel;
            easyButton = configuredEasyButton;
            normalButton = configuredNormalButton;
            hardButton = configuredHardButton;
        }

        private void Awake()
        {
            Bind(easyButton, GameDifficulty.Easy);
            Bind(normalButton, GameDifficulty.Normal);
            Bind(hardButton, GameDifficulty.Hard);
        }

        public void SetTitle(string value)
        {
            if (titleLabel != null)
            {
                titleLabel.text = value ?? string.Empty;
            }
        }

        public void SetDescription(string value)
        {
            if (descriptionLabel != null)
            {
                descriptionLabel.text = value ?? string.Empty;
            }
        }

        public void SetButtonText(GameDifficulty difficulty, string value)
        {
            TMP_Text label = difficulty switch
            {
                GameDifficulty.Easy => easyButtonLabel,
                GameDifficulty.Normal => normalButtonLabel,
                GameDifficulty.Hard => hardButtonLabel,
                _ => null
            };
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }

        private void Bind(Button button, GameDifficulty difficulty)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(
                () => DifficultyRequested?.Invoke(difficulty));
        }
    }
}
