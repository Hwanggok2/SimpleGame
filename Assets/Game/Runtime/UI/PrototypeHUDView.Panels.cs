using System;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame
{
    public sealed partial class PrototypeHUDView
    {
        private void ApplyPauseDetails()
        {
            pauseDetailsPanel?.SetDetails(pauseDetails);
        }

        private void EnsureCardSelectionPanel()
        {
            if (cardSelectionPanel != null ||
                cardSelectionPanelPrefab == null)
            {
                return;
            }

            GameObject instance = InstantiatePopup(cardSelectionPanelPrefab);
            cardSelectionPanel =
                instance.GetComponent<CardSelectionPanelView>();
            if (cardSelectionPanel == null ||
                !cardSelectionPanel.IsConfigured)
            {
                Debug.LogError(
                    "Card selection prefab requires a configured " +
                    "CardSelectionPanelView.",
                    instance);
                return;
            }

            for (int index = 0;
                 index < cardChoiceButtons.Length;
                 index++)
            {
                CardSelectionSlot slot = (CardSelectionSlot)index;
                cardChoiceButtons[index] =
                    cardSelectionPanel.GetChoiceButton(slot);
                cardChoiceViews[index] =
                    cardSelectionPanel.GetChoice(slot);
                cardChoiceViews[index]?.ConfigureStrings(gameStrings);
                cardRerollButtons[index] =
                    cardChoiceViews[index] != null
                        ? cardChoiceViews[index].RerollButton
                        : null;
                if (cardChoiceButtons[index] == null ||
                    cardChoiceViews[index] == null ||
                    cardRerollButtons[index] == null)
                {
                    Debug.LogError(
                        $"Card slot {slot} requires a root Button, " +
                        "LevelUpCardView, and RerollButton.",
                        instance);
                }

                BindButton(
                    cardChoiceButtons[index],
                    buttonCallbacks[index]);
                BindButton(
                    cardRerollButtons[index],
                    buttonCallbacks[
                        (int)HudButtonId.CardReroll0 + index]);
            }

            cardSelectionPanel.SetTitle(Text(
                GameStringIds.UiCardSelectionTitle,
                "레벨 업\n카드를 선택하세요"));
            ApplyCardRerollState();
        }

        private void EnsureDifficultySelectionPanel()
        {
            if (difficultySelectionPanel != null ||
                difficultySelectionPanelPrefab == null)
            {
                return;
            }

            GameObject instance = InstantiatePopup(
                difficultySelectionPanelPrefab);
            difficultySelectionPanel =
                instance.GetComponent<DifficultySelectionPanelView>();
            if (difficultySelectionPanel == null ||
                !difficultySelectionPanel.IsConfigured)
            {
                Debug.LogError(
                    "Difficulty selection prefab requires a configured " +
                    "DifficultySelectionPanelView.",
                    instance);
                return;
            }

            difficultySelectionPanel.DifficultyRequested +=
                OnDifficultyRequested;
            ApplyDifficultyStrings();
        }

        private void OnDifficultyRequested(GameDifficulty difficulty)
        {
            HudButtonId id = difficulty switch
            {
                GameDifficulty.Easy => HudButtonId.DifficultyEasy,
                GameDifficulty.Normal => HudButtonId.DifficultyNormal,
                GameDifficulty.Hard => HudButtonId.DifficultyHard,
                _ => HudButtonId.Count
            };
            if (id != HudButtonId.Count)
            {
                buttonCallbacks[(int)id]?.Invoke();
            }
        }

        private void EnsurePauseDetailsPanel()
        {
            if (pauseDetailsPanel != null ||
                pauseDetailsPanelPrefab == null)
            {
                return;
            }

            GameObject instance = InstantiatePopup(pauseDetailsPanelPrefab);
            pauseDetailsPanel =
                instance.GetComponent<PauseDetailsPanelView>();
            if (pauseDetailsPanel == null ||
                !pauseDetailsPanel.IsConfigured ||
                !pauseDetailsPanel.ControlSettings.IsConfigured)
            {
                Debug.LogError(
                    "Pause prefab requires configured " +
                    "PauseDetailsPanelView and ControlSettingsPanelView " +
                    "components.",
                    instance);
                return;
            }

            controlSettingsPanel = pauseDetailsPanel.ControlSettings;
            controlSettingsPanel.Initialize();
            pauseDetailsPanel.ActionRequested +=
                OnPauseDetailsActionRequested;
            controlSettingsPanel.ActionRequested +=
                OnControlSettingsActionRequested;
            controlSettingsPanel.AutoAttackChanged +=
                OnAutoAttackDraftChanged;
            controlSettingsPanel.JoystickSizeChanged +=
                OnJoystickSizeChanged;
            controlSettingsPanel.AttackSizeChanged +=
                OnAttackSizeChanged;

            controlSettingsPanel.ConfigureDragSurface(
                aimJoystick != null ? aimJoystick.TouchArea : null,
                attackButton != null
                    ? attackButton.GetComponent<RectTransform>()
                    : null,
                OnControlDragged);
            ApplyPauseStrings();
            ApplyPauseDetails();
            SetControlSettingsPageVisible(false);
        }

        private void OnPauseDetailsActionRequested(
            PauseDetailsAction action)
        {
            switch (action)
            {
                case PauseDetailsAction.Retry:
                    RequestNavigationConfirmation(HudButtonId.Retry);
                    break;
                case PauseDetailsAction.ReturnToLobby:
                    RequestNavigationConfirmation(
                        HudButtonId.ReturnToLobby);
                    break;
            }
        }

        private void EnsureGameOverPanel()
        {
            if (gameOverPanel != null ||
                gameOverPanelPrefab == null)
            {
                return;
            }

            GameObject instance = InstantiatePopup(gameOverPanelPrefab);
            gameOverPanel = instance.GetComponent<ResultPanelView>();
            if (gameOverPanel == null || !gameOverPanel.IsConfigured)
            {
                Debug.LogError(
                    "Game-over prefab requires a configured " +
                    "ResultPanelView.",
                    instance);
                return;
            }

            gameOverPanel.ActionRequested += OnResultActionRequested;
            if (string.IsNullOrWhiteSpace(gameOverDetails))
            {
                gameOverPanel.SetSummary(Text(
                    GameStringIds.UiGameOverTitle,
                    "게임 종료"));
            }

            gameOverPanel.SetButtonText(
                ResultPanelAction.Continue,
                Text(GameStringIds.UiContinueButton, "이어하기"));
            gameOverPanel.SetButtonText(
                ResultPanelAction.Retry,
                Text(GameStringIds.UiRetryButton, "다시하기"));
            gameOverPanel.SetButtonText(
                ResultPanelAction.ReturnToLobby,
                Text(GameStringIds.UiReturnLobbyButton, "로비로 이동"));
        }

        private void OnResultActionRequested(ResultPanelAction action)
        {
            switch (action)
            {
                case ResultPanelAction.Continue:
                    buttonCallbacks[(int)HudButtonId.ContinueAd]?.Invoke();
                    break;
                case ResultPanelAction.Retry:
                    RequestNavigationConfirmation(HudButtonId.Retry);
                    break;
                case ResultPanelAction.ReturnToLobby:
                    RequestNavigationConfirmation(
                        HudButtonId.ReturnToLobby);
                    break;
            }
        }

        private void RequestNavigationConfirmation(HudButtonId action)
        {
            if (action != HudButtonId.Retry &&
                action != HudButtonId.ReturnToLobby)
            {
                return;
            }

            EnsureConfirmationDialog();
            if (confirmationDialog == null)
            {
                return;
            }

            pendingConfirmationAction = action;
            confirmationDialog.SetMessage(action == HudButtonId.Retry
                ? Text(
                    GameStringIds.UiConfirmRestartMessage,
                    "다시 하시겠습니까?")
                : Text(
                    GameStringIds.UiConfirmLobbyMessage,
                    "로비로 나가시겠습니까?"));
            confirmationDialog.gameObject.SetActive(true);
            confirmationDialog.transform.SetAsLastSibling();
        }

        private void EnsureConfirmationDialog()
        {
            if (confirmationDialog != null ||
                confirmationDialogPrefab == null)
            {
                return;
            }

            GameObject instance = InstantiatePopup(
                confirmationDialogPrefab);
            confirmationDialog =
                instance.GetComponent<ConfirmationDialogView>();
            if (confirmationDialog == null ||
                !confirmationDialog.IsConfigured)
            {
                Debug.LogError(
                    "Confirmation prefab requires a configured " +
                    "ConfirmationDialogView.",
                    instance);
                return;
            }

            confirmationDialog.Confirmed += ConfirmNavigation;
            confirmationDialog.Cancelled += CancelNavigation;
            confirmationDialog.SetButtonTexts(
                Text(GameStringIds.UiConfirmButton, "확인"),
                Text(GameStringIds.UiCancel, "취소"));
        }

        private void ConfirmNavigation()
        {
            HudButtonId action = pendingConfirmationAction;
            CancelNavigation();
            if (action != HudButtonId.Count)
            {
                buttonCallbacks[(int)action]?.Invoke();
            }
        }

        private void CancelNavigation()
        {
            pendingConfirmationAction = HudButtonId.Count;
            confirmationDialog?.gameObject.SetActive(false);
        }

        private GameObject InstantiatePopup(GameObject prefab)
        {
            GameObject instance = Instantiate(
                prefab,
                modalRoot,
                false);
            instance.name = prefab.name;
            instance.SetActive(false);
            return instance;
        }

        private void TryBindExistingButton(HudButtonId id)
        {
            int index = (int)id;
            if (index >= (int)HudButtonId.CardChoice0 &&
                index <= (int)HudButtonId.CardChoice2)
            {
                BindButton(
                    cardChoiceButtons[index],
                    buttonCallbacks[index]);
                return;
            }

            if (index >= (int)HudButtonId.CardReroll0 &&
                index <= (int)HudButtonId.CardReroll2)
            {
                int rerollIndex =
                    index - (int)HudButtonId.CardReroll0;
                BindButton(
                    cardRerollButtons[rerollIndex],
                    buttonCallbacks[index]);
                return;
            }

            if (id == HudButtonId.Settings)
            {
                BindButton(settingsButton, buttonCallbacks[index]);
                return;
            }

            if (id == HudButtonId.ControlSettings)
            {
                BindButton(controlSettingsButton, buttonCallbacks[index]);
                return;
            }

            if (id == HudButtonId.Attack)
            {
                AttackCommandButton commandButton =
                    attackButton != null
                        ? attackButton.GetComponent<
                            AttackCommandButton>()
                        : null;
                commandButton?.Bind(buttonCallbacks[index]);
                return;
            }

        }

        private void ApplyCardRerollState()
        {
            for (int index = 0;
                 index < cardChoiceViews.Length;
                 index++)
            {
                LevelUpCardView view = cardChoiceViews[index];
                if (view == null)
                {
                    continue;
                }

                bool cardVisible =
                    cardChoiceButtons[index] != null &&
                    cardChoiceButtons[index].gameObject.activeSelf;
                view.SetRerollState(
                    remainingCardRerolls,
                    cardVisible &&
                    cardChoicesInteractable &&
                    hasCardRerollAlternative);
            }
        }

        private void BindButton(
            Button button,
            Action callback)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            if (callback != null)
            {
                button.onClick.AddListener(() => callback());
            }
        }
    }
}
