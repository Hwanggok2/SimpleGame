using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame
{
    public enum ResultPanelAction
    {
        Continue,
        Retry,
        ReturnToLobby
    }

    [DisallowMultipleComponent]
    public sealed class ResultPanelView : MonoBehaviour
    {
        [SerializeField] private TMP_Text summaryLabel;
        [SerializeField] private TMP_Text continueButtonLabel;
        [SerializeField] private TMP_Text retryButtonLabel;
        [SerializeField] private TMP_Text returnToLobbyButtonLabel;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button returnToLobbyButton;

        public event Action<ResultPanelAction> ActionRequested;

        public bool IsConfigured =>
            summaryLabel != null &&
            continueButtonLabel != null &&
            retryButtonLabel != null &&
            returnToLobbyButtonLabel != null &&
            continueButton != null &&
            retryButton != null &&
            returnToLobbyButton != null;

        public void ConfigureReferences(
            TMP_Text configuredSummaryLabel,
            TMP_Text configuredContinueButtonLabel,
            TMP_Text configuredRetryButtonLabel,
            TMP_Text configuredReturnToLobbyButtonLabel,
            Button configuredContinueButton,
            Button configuredRetryButton,
            Button configuredReturnToLobbyButton)
        {
            summaryLabel = configuredSummaryLabel;
            continueButtonLabel = configuredContinueButtonLabel;
            retryButtonLabel = configuredRetryButtonLabel;
            returnToLobbyButtonLabel =
                configuredReturnToLobbyButtonLabel;
            continueButton = configuredContinueButton;
            retryButton = configuredRetryButton;
            returnToLobbyButton = configuredReturnToLobbyButton;
        }

        private void Awake()
        {
            Bind(continueButton, ResultPanelAction.Continue);
            Bind(retryButton, ResultPanelAction.Retry);
            Bind(returnToLobbyButton, ResultPanelAction.ReturnToLobby);
        }

        public void SetSummary(string value)
        {
            if (summaryLabel != null)
            {
                summaryLabel.text = value ?? string.Empty;
            }
        }

        public void SetButtonText(ResultPanelAction action, string value)
        {
            TMP_Text label = action switch
            {
                ResultPanelAction.Continue => continueButtonLabel,
                ResultPanelAction.Retry => retryButtonLabel,
                ResultPanelAction.ReturnToLobby =>
                    returnToLobbyButtonLabel,
                _ => null
            };
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }

        public void SetClearMode(bool clear)
        {
            continueButton?.gameObject.SetActive(!clear);
            SetButtonPosition(retryButton, clear ? -140f : 0f);
            SetButtonPosition(returnToLobbyButton, clear ? 140f : 260f);
        }

        private void Bind(Button button, ResultPanelAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(
                () => ActionRequested?.Invoke(action));
        }

        private static void SetButtonPosition(Button button, float x)
        {
            RectTransform rect = button != null
                ? button.GetComponent<RectTransform>()
                : null;
            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(
                    x,
                    rect.anchoredPosition.y);
            }
        }
    }
}
