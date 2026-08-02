using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame
{
    public sealed class LobbyCodexDetailView : MonoBehaviour
    {
        [SerializeField] private Button dismissButton;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text descriptionLabel;

        public void Configure(
            Button configuredDismissButton,
            Image configuredIcon,
            TMP_Text configuredNameLabel,
            TMP_Text configuredDescriptionLabel)
        {
            dismissButton = configuredDismissButton;
            icon = configuredIcon;
            nameLabel = configuredNameLabel;
            descriptionLabel = configuredDescriptionLabel;
        }

        public void Initialize()
        {
            if (dismissButton != null)
            {
                dismissButton.onClick.RemoveAllListeners();
                dismissButton.onClick.AddListener(Hide);
            }

            Hide();
        }

        public void Show(
            string displayName,
            string description,
            Sprite sprite,
            bool showImage)
        {
            if (nameLabel != null)
            {
                nameLabel.text = displayName ?? string.Empty;
            }

            if (descriptionLabel != null)
            {
                descriptionLabel.text = description ?? string.Empty;
            }

            if (icon != null)
            {
                icon.sprite = sprite;
                icon.color = Color.white;
                icon.enabled = showImage && sprite != null;
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
