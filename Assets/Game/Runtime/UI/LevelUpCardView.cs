using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class LevelUpCardView : MonoBehaviour
    {
        [SerializeField] private Image rarityFrame;
        [SerializeField] private Image innerBackground;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text skillText;
        [SerializeField] private Outline rarityOutline;
        [SerializeField] private Shadow rarityGlow;
        [SerializeField] private Button rerollButton;
        [SerializeField] private TMP_Text rerollLabel;

        private Color glowColor;
        private float glowStrength;

        public Image RarityFrame => rarityFrame;
        public Image InnerBackground => innerBackground;
        public TMP_Text TitleText => titleText;
        public TMP_Text SkillText => skillText;
        public Button RerollButton => rerollButton;
        public TMP_Text RerollLabel => rerollLabel;

        public void ConfigureReferences(
            Image configuredRarityFrame,
            Image configuredInnerBackground,
            TMP_Text configuredTitleText,
            TMP_Text configuredSkillText,
            Outline configuredRarityOutline,
            Shadow configuredRarityGlow,
            Button configuredRerollButton,
            TMP_Text configuredRerollLabel)
        {
            rarityFrame = configuredRarityFrame;
            innerBackground = configuredInnerBackground;
            titleText = configuredTitleText;
            skillText = configuredSkillText;
            rarityOutline = configuredRarityOutline;
            rarityGlow = configuredRarityGlow;
            rerollButton = configuredRerollButton;
            rerollLabel = configuredRerollLabel;
        }

        public void SetContent(LevelUpCardChoiceData choice)
        {
            if (titleText != null)
            {
                titleText.text = choice.HeaderText;
            }

            if (skillText != null)
            {
                skillText.text = choice.Description;
            }

            ApplyRarity(choice.Rarity);
        }

        public void SetRerollState(
            int remainingRerolls,
            bool interactable)
        {
            int remaining = Mathf.Max(0, remainingRerolls);
            if (rerollLabel != null)
            {
                rerollLabel.text = $"교체 {remaining}";
            }

            if (rerollButton != null)
            {
                rerollButton.interactable =
                    interactable && remaining > 0;
            }
        }

        public static Color ResolveRarityColor(string rarity)
        {
            return rarity?.Trim() switch
            {
                "희귀" or "Rare" =>
                    new Color(0.12f, 0.55f, 0.95f, 0.96f),
                "영웅" or "Epic" or "Hero" =>
                    new Color(0.64f, 0.28f, 0.92f, 0.96f),
                "전설" or "Legendary" =>
                    new Color(1f, 0.61f, 0.12f, 0.96f),
                _ => new Color(0.34f, 0.43f, 0.51f, 0.96f)
            };
        }

        private void ApplyRarity(string rarity)
        {
            Color frameColor = ResolveRarityColor(rarity);
            if (rarityFrame != null)
            {
                rarityFrame.color = frameColor;
            }

            if (rarityOutline != null)
            {
                Color outlineColor = frameColor;
                outlineColor.a = 0.9f;
                rarityOutline.effectColor = outlineColor;
            }

            glowColor = frameColor;
            glowStrength = ResolveGlowStrength(rarity);
            UpdateGlow();
        }

        private void Update()
        {
            UpdateGlow();
        }

        private void UpdateGlow()
        {
            if (rarityGlow == null)
            {
                return;
            }

            float pulse =
                0.5f +
                0.5f * Mathf.Sin(
                    Time.unscaledTime * 2.6f +
                    transform.GetSiblingIndex() * 0.7f);
            Color color = glowColor;
            color.a = Mathf.Lerp(0.12f, 0.38f, pulse) *
                glowStrength;
            rarityGlow.effectColor = color;
        }

        private static float ResolveGlowStrength(string rarity)
        {
            return rarity?.Trim() switch
            {
                "희귀" or "Rare" => 0.72f,
                "영웅" or "Epic" or "Hero" => 0.9f,
                "전설" or "Legendary" => 1f,
                _ => 0.45f
            };
        }
    }
}
