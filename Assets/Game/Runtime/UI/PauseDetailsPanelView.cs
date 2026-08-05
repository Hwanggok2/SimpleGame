using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame
{
    public enum PauseDetailsAction
    {
        Retry,
        ReturnToLobby
    }

    public enum PauseDetailsTextId
    {
        PlayerOverview,
        AccountOverview,
        PlayerStats,
        AcquiredSkills,
        PlayerStatsTitle,
        SkillsTitle,
        RetryButton,
        ReturnToLobbyButton
    }

    [DisallowMultipleComponent]
    public sealed class PauseDetailsPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject settingsPage;
        [SerializeField] private ControlSettingsPanelView controlSettings;
        [SerializeField] private TMP_Text playerOverviewLabel;
        [SerializeField] private TMP_Text accountOverviewLabel;
        [SerializeField] private TMP_Text playerStatsLabel;
        [SerializeField] private TMP_Text acquiredSkillsLabel;
        [SerializeField] private TMP_Text playerStatsTitleLabel;
        [SerializeField] private TMP_Text skillsTitleLabel;
        [SerializeField] private TMP_Text retryButtonLabel;
        [SerializeField] private TMP_Text returnToLobbyButtonLabel;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button returnToLobbyButton;

        public event Action<PauseDetailsAction> ActionRequested;

        public bool IsConfigured =>
            settingsPage != null &&
            controlSettings != null &&
            retryButton != null &&
            returnToLobbyButton != null &&
            playerOverviewLabel != null &&
            accountOverviewLabel != null &&
            playerStatsLabel != null &&
            acquiredSkillsLabel != null &&
            playerStatsTitleLabel != null &&
            skillsTitleLabel != null &&
            retryButtonLabel != null &&
            returnToLobbyButtonLabel != null;

        public ControlSettingsPanelView ControlSettings => controlSettings;

        public void ConfigureReferences(
            GameObject configuredSettingsPage,
            ControlSettingsPanelView configuredControlSettings,
            TMP_Text configuredPlayerOverviewLabel,
            TMP_Text configuredAccountOverviewLabel,
            TMP_Text configuredPlayerStatsLabel,
            TMP_Text configuredAcquiredSkillsLabel,
            TMP_Text configuredPlayerStatsTitleLabel,
            TMP_Text configuredSkillsTitleLabel,
            TMP_Text configuredRetryButtonLabel,
            TMP_Text configuredReturnToLobbyButtonLabel,
            Button configuredRetryButton,
            Button configuredReturnToLobbyButton)
        {
            settingsPage = configuredSettingsPage;
            controlSettings = configuredControlSettings;
            playerOverviewLabel = configuredPlayerOverviewLabel;
            accountOverviewLabel = configuredAccountOverviewLabel;
            playerStatsLabel = configuredPlayerStatsLabel;
            acquiredSkillsLabel = configuredAcquiredSkillsLabel;
            playerStatsTitleLabel = configuredPlayerStatsTitleLabel;
            skillsTitleLabel = configuredSkillsTitleLabel;
            retryButtonLabel = configuredRetryButtonLabel;
            returnToLobbyButtonLabel =
                configuredReturnToLobbyButtonLabel;
            retryButton = configuredRetryButton;
            returnToLobbyButton = configuredReturnToLobbyButton;
        }

        private void Awake()
        {
            Bind(retryButton, PauseDetailsAction.Retry);
            Bind(returnToLobbyButton, PauseDetailsAction.ReturnToLobby);
        }

        public void SetDetails(PauseDetailsData details)
        {
            SetText(PauseDetailsTextId.PlayerOverview, details.PlayerOverview);
            SetText(PauseDetailsTextId.AccountOverview, details.AccountOverview);
            SetText(PauseDetailsTextId.PlayerStats, details.Stats);
            SetText(PauseDetailsTextId.AcquiredSkills, details.Skills);
            if (acquiredSkillsLabel != null)
            {
                RectTransform rect = acquiredSkillsLabel.rectTransform;
                rect.sizeDelta = new Vector2(
                    rect.sizeDelta.x,
                    Mathf.Max(0f, acquiredSkillsLabel.preferredHeight));
            }
        }

        public void SetControlSettingsVisible(bool visible)
        {
            settingsPage?.SetActive(!visible);
            retryButton?.gameObject.SetActive(!visible);
            returnToLobbyButton?.gameObject.SetActive(!visible);
            if (controlSettings != null)
            {
                controlSettings.gameObject.SetActive(visible);
            }
        }

        public void SetText(PauseDetailsTextId id, string value)
        {
            TMP_Text label = id switch
            {
                PauseDetailsTextId.PlayerOverview => playerOverviewLabel,
                PauseDetailsTextId.AccountOverview => accountOverviewLabel,
                PauseDetailsTextId.PlayerStats => playerStatsLabel,
                PauseDetailsTextId.AcquiredSkills => acquiredSkillsLabel,
                PauseDetailsTextId.PlayerStatsTitle => playerStatsTitleLabel,
                PauseDetailsTextId.SkillsTitle => skillsTitleLabel,
                PauseDetailsTextId.RetryButton => retryButtonLabel,
                PauseDetailsTextId.ReturnToLobbyButton =>
                    returnToLobbyButtonLabel,
                _ => null
            };
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }

        private void Bind(Button button, PauseDetailsAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(
                () => ActionRequested?.Invoke(action));
        }

    }
}
