using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame
{
    public enum CardSelectionSlot
    {
        First,
        Second,
        Third
    }

    [DisallowMultipleComponent]
    public sealed class CardSelectionPanelView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private LevelUpCardView[] choices =
            new LevelUpCardView[3];
        [SerializeField] private Button[] choiceButtons = new Button[3];

        public bool IsConfigured =>
            titleLabel != null &&
            choices != null &&
            choices.Length == 3 &&
            choices[0] != null &&
            choices[1] != null &&
            choices[2] != null &&
            choiceButtons != null &&
            choiceButtons.Length == 3 &&
            choiceButtons[0] != null &&
            choiceButtons[1] != null &&
            choiceButtons[2] != null;

        public void ConfigureReferences(
            TMP_Text configuredTitleLabel,
            LevelUpCardView first,
            LevelUpCardView second,
            LevelUpCardView third,
            Button firstButton,
            Button secondButton,
            Button thirdButton)
        {
            titleLabel = configuredTitleLabel;
            choices = new[] { first, second, third };
            choiceButtons = new[]
            {
                firstButton,
                secondButton,
                thirdButton
            };
        }

        public void SetTitle(string value)
        {
            if (titleLabel != null)
            {
                titleLabel.text = value ?? string.Empty;
            }
        }

        public LevelUpCardView GetChoice(CardSelectionSlot slot)
        {
            int index = (int)slot;
            return choices != null && index >= 0 && index < choices.Length
                ? choices[index]
                : null;
        }

        public Button GetChoiceButton(CardSelectionSlot slot)
        {
            int index = (int)slot;
            return choiceButtons != null &&
                   index >= 0 &&
                   index < choiceButtons.Length
                ? choiceButtons[index]
                : null;
        }
    }
}
