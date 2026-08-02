using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame
{
    public sealed class LobbyDifficultyOptionView : MonoBehaviour
    {
        [SerializeField] private LobbyDifficultyId difficultyId;
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text descriptionLabel;

        public LobbyDifficultyId DifficultyId => difficultyId;
        public Button Button => button;

        public void Configure(
            LobbyDifficultyId configuredDifficultyId,
            Button configuredButton,
            Image configuredBackground,
            TMP_Text configuredTitleLabel,
            TMP_Text configuredDescriptionLabel)
        {
            difficultyId = configuredDifficultyId;
            button = configuredButton;
            background = configuredBackground;
            titleLabel = configuredTitleLabel;
            descriptionLabel = configuredDescriptionLabel;
        }

        public void SetContent(string title, string description)
        {
            if (titleLabel != null)
            {
                titleLabel.text = title ?? string.Empty;
            }

            if (descriptionLabel != null)
            {
                descriptionLabel.text = description ?? string.Empty;
            }
        }

        public void SetState(
            bool selected,
            bool available,
            Color selectedColor,
            Color normalColor)
        {
            if (background != null)
            {
                background.color = selected
                    ? selectedColor
                    : normalColor;
            }

            if (button != null)
            {
                button.interactable = available;
            }
        }
    }
}
