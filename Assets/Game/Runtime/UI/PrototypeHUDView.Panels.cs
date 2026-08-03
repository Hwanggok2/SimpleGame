using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame
{
    public sealed partial class PrototypeHUDView
    {
        private void ApplyPauseDetails()
        {
            if (playerOverviewLabel != null)
            {
                playerOverviewLabel.text = pauseDetails.PlayerOverview;
            }

            if (accountOverviewLabel != null)
            {
                accountOverviewLabel.text = pauseDetails.AccountOverview;
            }

            if (playerStatsLabel != null)
            {
                playerStatsLabel.text = pauseDetails.Stats;
            }

            if (acquiredSkillsLabel != null)
            {
                acquiredSkillsLabel.text = pauseDetails.Skills;
                RectTransform rect = acquiredSkillsLabel.rectTransform;
                rect.sizeDelta = new Vector2(
                    rect.sizeDelta.x,
                    Mathf.Max(0f, acquiredSkillsLabel.preferredHeight));
            }
        }

        private void EnsureCardSelectionPanel()
        {
            if (cardSelectionPanel != null ||
                cardSelectionPanelPrefab == null)
            {
                return;
            }

            cardSelectionPanel = InstantiatePopup(
                cardSelectionPanelPrefab);
            for (int index = 0;
                 index < cardChoiceButtons.Length;
                 index++)
            {
                string objectName =
                    ((HudButtonId)index).ToString();
                Transform choice =
                    cardSelectionPanel.transform.Find(objectName);
                if (choice == null)
                {
                    Debug.LogError(
                        $"Card selection prefab is missing {objectName}.",
                        cardSelectionPanel);
                    continue;
                }

                cardChoiceButtons[index] =
                    choice.GetComponent<Button>();
                cardChoiceViews[index] =
                    choice.GetComponent<LevelUpCardView>();
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
                        $"{objectName} requires a root Button, " +
                        "LevelUpCardView, and RerollButton.",
                        choice);
                }

                BindButton(
                    cardChoiceButtons[index],
                    buttonCallbacks[index]);
                BindButton(
                    cardRerollButtons[index],
                    buttonCallbacks[
                        (int)HudButtonId.CardReroll0 + index]);
            }

            SetTextAtPath(
                cardSelectionPanel.transform,
                "CardTitle",
                GameStringIds.UiCardSelectionTitle,
                "레벨 업\n카드를 선택하세요");
            ApplyCardRerollState();
        }

        private void EnsureDifficultySelectionPanel()
        {
            if (difficultySelectionPanel != null ||
                difficultySelectionPanelPrefab == null)
            {
                return;
            }

            difficultySelectionPanel = InstantiatePopup(
                difficultySelectionPanelPrefab);
            Transform easy = difficultySelectionPanel.transform.Find(
                HudButtonId.DifficultyEasy.ToString());
            Transform normal = difficultySelectionPanel.transform.Find(
                HudButtonId.DifficultyNormal.ToString());
            difficultyEasyButton = easy != null
                ? easy.GetComponent<Button>()
                : null;
            difficultyNormalButton = normal != null
                ? normal.GetComponent<Button>()
                : null;
            if (difficultyEasyButton == null ||
                difficultyNormalButton == null)
            {
                Debug.LogError(
                    "Difficulty selection prefab requires Easy and " +
                    "Normal buttons.",
                    difficultySelectionPanel);
                return;
            }

            BindButton(
                difficultyEasyButton,
                buttonCallbacks[(int)HudButtonId.DifficultyEasy]);
            BindButton(
                difficultyNormalButton,
                buttonCallbacks[(int)HudButtonId.DifficultyNormal]);
            ApplyDifficultyStrings();
        }

        private void EnsurePauseDetailsPanel()
        {
            if (pauseDetailsPanel != null ||
                pauseDetailsPanelPrefab == null)
            {
                return;
            }

            pauseDetailsPanel = InstantiatePopup(
                pauseDetailsPanelPrefab);
            Transform root = pauseDetailsPanel.transform;
            Transform settingsPageTransform = root.Find("SettingsPage");
            settingsPage = settingsPageTransform != null
                ? settingsPageTransform.gameObject
                : null;
            playerOverviewLabel = FindText(
                settingsPageTransform,
                "PlayerOverview");
            accountOverviewLabel = FindText(
                settingsPageTransform,
                "AccountOverview");
            playerStatsLabel = FindText(
                settingsPageTransform,
                "PlayerStats");
            acquiredSkillsLabel = FindText(
                settingsPageTransform,
                "SkillsPanel/Viewport/SkillsList");

            Transform settingsButtonTransform =
                root.Find("ControlSettingsButton");
            controlSettingsButton = settingsButtonTransform != null
                ? settingsButtonTransform.GetComponent<Button>()
                : null;
            Transform settingsPanelTransform =
                root.Find("ControlSettingsPanel");
            controlSettingsPanel = settingsPanelTransform != null
                ? settingsPanelTransform.gameObject
                : null;
            Transform autoAttackToggleTransform =
                settingsPanelTransform != null
                    ? settingsPanelTransform.Find("AutoAttackToggle")
                    : null;
            autoAttackToggle = autoAttackToggleTransform != null
                ? autoAttackToggleTransform.GetComponent<Toggle>()
                : null;
            autoAttackTrack = FindImage(
                autoAttackToggleTransform,
                "Track");
            autoAttackKnobImage = FindImage(
                autoAttackToggleTransform,
                "Track/Knob");
            autoAttackKnob = autoAttackKnobImage != null
                ? autoAttackKnobImage.rectTransform
                : null;
            autoAttackValueLabel = FindText(
                autoAttackToggleTransform,
                "Value");

            joystickSizeSlider = FindControlSlider(
                settingsPanelTransform,
                "JoystickSizeSlider");
            attackSizeSlider = FindControlSlider(
                settingsPanelTransform,
                "AttackSizeSlider");
            modeOneButton = FindControlButton(
                settingsPanelTransform,
                "ControlModeButtons/Mode1Button");
            modeTwoButton = FindControlButton(
                settingsPanelTransform,
                "ControlModeButtons/Mode2Button");
            hiddenModeButton = FindControlButton(
                settingsPanelTransform,
                "ControlModeButtons/HiddenButton");
            Button defaultsButton = FindControlButton(
                settingsPanelTransform,
                "ControlDefaultsButton");
            Button applyButton = FindControlButton(
                settingsPanelTransform,
                "ControlApplyButton");
            Transform dragSurfaceTransform = settingsPanelTransform != null
                ? settingsPanelTransform.Find("ControlDragSurface")
                : null;
            controlDragSurface = dragSurfaceTransform != null
                ? dragSurfaceTransform.GetComponent<ControlLayoutDragSurface>()
                : null;

            BindControlSettingsNavigation();

            if (settingsPage == null ||
                playerOverviewLabel == null ||
                accountOverviewLabel == null ||
                playerStatsLabel == null ||
                acquiredSkillsLabel == null ||
                controlSettingsButton == null ||
                controlSettingsPanel == null ||
                autoAttackToggle == null ||
                autoAttackTrack == null ||
                autoAttackKnob == null ||
                autoAttackValueLabel == null ||
                joystickSizeSlider == null ||
                attackSizeSlider == null ||
                modeOneButton == null ||
                modeTwoButton == null ||
                hiddenModeButton == null ||
                defaultsButton == null ||
                applyButton == null ||
                controlDragSurface == null)
            {
                Debug.LogError(
                    "Pause prefab settings references are incomplete.",
                    pauseDetailsPanel);
                return;
            }

            settingsButtonGroup = settingsButton.GetComponent<CanvasGroup>();
            if (settingsButtonGroup == null)
            {
                settingsButtonGroup =
                    settingsButton.gameObject.AddComponent<CanvasGroup>();
            }

            BindButton(defaultsButton, RestoreDefaultControlSettings);
            BindButton(applyButton, ApplyPendingControlSettings);
            BindButton(
                modeOneButton,
                () => SelectControlMode(
                    MobileControlMode.DirectMoveAutoAim));
            BindButton(
                modeTwoButton,
                () => SelectControlMode(MobileControlMode.AimCommand));
            BindButton(hiddenModeButton, SelectHiddenControlMode);
            autoAttackToggle.onValueChanged.RemoveAllListeners();
            autoAttackToggle.onValueChanged.AddListener(
                OnAutoAttackDraftChanged);
            BindControlSettingSliders();
            controlDragSurface.Configure(
                aimJoystick != null ? aimJoystick.TouchArea : null,
                attackButton != null
                    ? attackButton.GetComponent<RectTransform>()
                    : null,
                OnControlDragged);
            ApplyPauseStrings();
            ApplyPauseDetails();
            SetControlSettingsPageVisible(false);
        }

        private void BindControlSettingsNavigation()
        {
            if (controlSettingsButton == null)
            {
                return;
            }

            controlSettingsButton.interactable = true;
            controlSettingsButton.transform.SetAsLastSibling();
            BindButton(controlSettingsButton, ToggleControlSettingsPage);
        }

        private static TMP_Text FindText(Transform parent, string path)
        {
            Transform child = parent != null ? parent.Find(path) : null;
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }

        private static Image FindImage(Transform parent, string path)
        {
            Transform child = parent != null ? parent.Find(path) : null;
            return child != null ? child.GetComponent<Image>() : null;
        }

        private static Slider FindControlSlider(
            Transform parent,
            string name)
        {
            Transform child = parent != null ? parent.Find(name) : null;
            return child != null ? child.GetComponent<Slider>() : null;
        }

        private static Button FindControlButton(
            Transform parent,
            string name)
        {
            Transform child = parent != null ? parent.Find(name) : null;
            return child != null ? child.GetComponent<Button>() : null;
        }

        private void EnsureGameOverPanel()
        {
            if (gameOverPanel != null ||
                gameOverPanelPrefab == null)
            {
                return;
            }

            gameOverPanel = InstantiatePopup(gameOverPanelPrefab);
            Transform title =
                gameOverPanel.transform.Find("GameOverTitle");
            gameOverTitle =
                title != null ? title.GetComponent<TMP_Text>() : null;
            Transform continueTransform =
                gameOverPanel.transform.Find(
                    HudButtonId.ContinueAd.ToString());
            continueButton = continueTransform != null
                ? continueTransform.GetComponent<Button>()
                : null;
            if (gameOverTitle == null || continueButton == null)
            {
                Debug.LogError(
                    "Game-over prefab requires GameOverTitle and " +
                    "ContinueAd.",
                    gameOverPanel);
            }

            if (gameOverTitle != null &&
                string.IsNullOrWhiteSpace(gameOverDetails))
            {
                gameOverTitle.text = Text(
                    GameStringIds.UiGameOverTitle,
                    "게임 종료");
            }

            BindButton(
                continueButton,
                buttonCallbacks[(int)HudButtonId.ContinueAd]);
            SetButtonText(
                continueButton,
                GameStringIds.UiContinueButton,
                "이어하기");
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

            if (id == HudButtonId.DifficultyEasy)
            {
                BindButton(difficultyEasyButton, buttonCallbacks[index]);
                return;
            }

            if (id == HudButtonId.DifficultyNormal)
            {
                BindButton(difficultyNormalButton, buttonCallbacks[index]);
                return;
            }

            if (id == HudButtonId.ContinueAd)
            {
                BindButton(continueButton, buttonCallbacks[index]);
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
            UiTouchSoundPlayer touchSoundPlayer =
                GetComponent<UiTouchSoundPlayer>();
            if (touchSoundPlayer != null)
            {
                button.onClick.AddListener(touchSoundPlayer.Play);
            }

            if (callback != null)
            {
                button.onClick.AddListener(() => callback());
            }
        }
    }
}
