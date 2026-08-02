using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame
{
    public sealed class LobbyCodexEntryView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameLabel;

        public Button Button => button;
        public Image Icon => icon;
        public TMP_Text NameLabel => nameLabel;

        public void Configure(
            Button configuredButton,
            Image configuredIcon,
            TMP_Text configuredNameLabel)
        {
            button = configuredButton;
            icon = configuredIcon;
            nameLabel = configuredNameLabel;
        }

        public void SetContent(
            string displayName,
            Sprite sprite,
            bool showImage,
            Action selected)
        {
            gameObject.SetActive(true);
            if (nameLabel != null)
            {
                nameLabel.text = displayName ?? string.Empty;
            }

            if (icon != null)
            {
                icon.sprite = sprite;
                icon.color = Color.white;
                icon.enabled = showImage && sprite != null;
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = selected != null;
                if (selected != null)
                {
                    button.onClick.AddListener(() => selected());
                }
            }
        }

        public void SetEmpty()
        {
            SetContent(string.Empty, null, false, null);
        }
    }
}
