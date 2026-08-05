using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame
{
    [DisallowMultipleComponent]
    public sealed class ConfirmationDialogView : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private TMP_Text confirmButtonLabel;
        [SerializeField] private TMP_Text cancelButtonLabel;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        public event Action Confirmed;
        public event Action Cancelled;

        public bool IsConfigured =>
            messageLabel != null &&
            confirmButtonLabel != null &&
            cancelButtonLabel != null &&
            confirmButton != null &&
            cancelButton != null;

        public void ConfigureReferences(
            TMP_Text configuredMessageLabel,
            TMP_Text configuredConfirmButtonLabel,
            TMP_Text configuredCancelButtonLabel,
            Button configuredConfirmButton,
            Button configuredCancelButton)
        {
            messageLabel = configuredMessageLabel;
            confirmButtonLabel = configuredConfirmButtonLabel;
            cancelButtonLabel = configuredCancelButtonLabel;
            confirmButton = configuredConfirmButton;
            cancelButton = configuredCancelButton;
        }

        private void Awake()
        {
            Bind(confirmButton, () => Confirmed?.Invoke());
            Bind(cancelButton, () => Cancelled?.Invoke());
        }

        public void SetMessage(string value)
        {
            if (messageLabel != null)
            {
                messageLabel.text = value ?? string.Empty;
            }
        }

        public void SetButtonTexts(string confirm, string cancel)
        {
            SetButtonText(confirmButtonLabel, confirm);
            SetButtonText(cancelButtonLabel, cancel);
        }

        private static void Bind(Button button, Action callback)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => callback?.Invoke());
        }

        private static void SetButtonText(TMP_Text label, string value)
        {
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }
    }
}
