using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame
{
    public enum HudButtonId
    {
        CardChoice0,
        CardChoice1,
        CardChoice2,
        CardReroll0,
        CardReroll1,
        CardReroll2,
        Settings,
        ContinueAd,
        Attack,
        DifficultyEasy,
        DifficultyNormal,
        Count
    }

    public enum HudTextId
    {
        Time,
        Hint,
        Count
    }

    public sealed class PrototypeHUDView : MonoBehaviour
    {
        [Header("Persistent HUD")]
        [SerializeField] private TMP_Text timeLabel;
        [SerializeField] private TMP_Text hintLabel;
        [SerializeField] private Slider experienceSlider;
        [SerializeField] private TMP_Text experienceLabel;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button attackButton;
        [SerializeField] private AimJoystickControl aimJoystick;
        [Header("Transient UI")]
        [SerializeField] private Transform modalRoot;
        [SerializeField] private GameObject cardSelectionPanelPrefab;
        [SerializeField] private GameObject pauseDetailsPanelPrefab;
        [SerializeField] private GameObject gameOverPanelPrefab;
        [SerializeField] private GameObject difficultySelectionPanelPrefab;

        private readonly Action[] buttonCallbacks =
            new Action[(int)HudButtonId.Count];
        private readonly Button[] cardChoiceButtons = new Button[3];
        private readonly Button[] cardRerollButtons = new Button[3];
        private readonly LevelUpCardView[] cardChoiceViews =
            new LevelUpCardView[3];
        private GameObject cardSelectionPanel;
        private GameObject pauseDetailsPanel;
        private GameObject settingsPage;
        private TMP_Text playerOverviewLabel;
        private TMP_Text accountOverviewLabel;
        private TMP_Text playerStatsLabel;
        private TMP_Text acquiredSkillsLabel;
        private Toggle autoAttackToggle;
        private Image autoAttackTrack;
        private RectTransform autoAttackKnob;
        private Image autoAttackKnobImage;
        private TMP_Text autoAttackValueLabel;
        private Button controlSettingsButton;
        private GameObject controlSettingsPanel;
        private CanvasGroup settingsButtonGroup;
        private Slider joystickSizeSlider;
        private Slider attackSizeSlider;
        private Button modeOneButton;
        private Button modeTwoButton;
        private Button hiddenModeButton;
        private ControlLayoutDragSurface controlDragSurface;
        private GameObject gameOverPanel;
        private GameObject difficultySelectionPanel;
        private Button difficultyEasyButton;
        private Button difficultyNormalButton;
        private TMP_Text gameOverTitle;
        private Button continueButton;
        private bool cardChoicesInteractable;
        private bool hasCardRerollAlternative;
        private int remainingCardRerolls;
        private PauseDetailsData pauseDetails;
        private string gameOverDetails = string.Empty;
        private bool commandControlsEnabled = true;
        private bool editingControlSettings;
        private MobileControlSettings controlSettings;
        private MobileControlSettings pendingControlSettings;
        private Vector2 joystickBaseSize;
        private Vector2 attackBaseSize;
        private PlayerRoot aimControlsPlayer;
        private GameStringTable gameStrings;
        private string difficultyStageName = string.Empty;
        private string difficultyStageDescription = string.Empty;

        public GameObject CardSelectionPanelPrefab =>
            cardSelectionPanelPrefab;
        public GameObject PauseDetailsPanelPrefab =>
            pauseDetailsPanelPrefab;
        public GameObject GameOverPanelPrefab =>
            gameOverPanelPrefab;
        public GameObject DifficultySelectionPanelPrefab =>
            difficultySelectionPanelPrefab;
        public Button SettingsButton => settingsButton;
        public Button AttackButton => attackButton;
        public AimJoystickControl AimJoystick => aimJoystick;
        public bool CommandControlsEnabled =>
            commandControlsEnabled;
        public MobileControlSettings ControlSettings => controlSettings;

        public void Configure(
            TMP_Text configuredTimeLabel,
            TMP_Text configuredHintLabel,
            Slider configuredExperienceSlider,
            TMP_Text configuredExperienceLabel,
            Button configuredSettingsButton,
            Button configuredAttackButton,
            AimJoystickControl configuredAimJoystick,
            Transform configuredModalRoot,
            GameObject configuredCardSelectionPanelPrefab,
            GameObject configuredPauseDetailsPanelPrefab,
            GameObject configuredGameOverPanelPrefab,
            GameObject configuredDifficultySelectionPanelPrefab)
        {
            timeLabel = configuredTimeLabel;
            hintLabel = configuredHintLabel;
            experienceSlider = configuredExperienceSlider;
            experienceLabel = configuredExperienceLabel;
            settingsButton = configuredSettingsButton;
            attackButton = configuredAttackButton;
            aimJoystick = configuredAimJoystick;
            modalRoot = configuredModalRoot;
            cardSelectionPanelPrefab =
                configuredCardSelectionPanelPrefab;
            pauseDetailsPanelPrefab =
                configuredPauseDetailsPanelPrefab;
            gameOverPanelPrefab = configuredGameOverPanelPrefab;
            difficultySelectionPanelPrefab =
                configuredDifficultySelectionPanelPrefab;
        }

        public void Initialize()
        {
            Initialize(null);
        }

        public void Initialize(GameStringTable configuredGameStrings)
        {
            gameStrings = configuredGameStrings;
            ValidateConfiguration();
            cardChoicesInteractable = false;
            if (settingsButton != null)
            {
                settingsButton.transform.SetAsLastSibling();
            }

            CaptureControlBaseSizes();
            controlSettings = MobileControlSettingsStore.Load();
            pendingControlSettings = controlSettings;
            commandControlsEnabled = controlSettings.controlsEnabled;
            ApplyControlLayout(controlSettings);
            ApplyControlModePresentation(controlSettings.controlMode);
            ApplyCommandControlsVisibility(commandControlsEnabled);
            ApplyPersistentStrings();
        }

        public void SetDifficultyContext(
            string stageName,
            string stageDescription)
        {
            difficultyStageName = stageName ?? string.Empty;
            difficultyStageDescription = stageDescription ?? string.Empty;
            ApplyDifficultyStrings();
        }

        public void InitializeAimControls(PlayerRoot player)
        {
            aimControlsPlayer = player;
            aimJoystick?.Initialize(player);
            player?.SetControlMode(controlSettings.controlMode);
            player?.SetAutoAttackEnabled(
                controlSettings.autoAttackEnabled);
        }

        public void SetCommandControlsEnabled(bool enabled)
        {
            commandControlsEnabled = enabled;
            controlSettings.controlsEnabled = enabled;
            MobileControlSettingsStore.Save(controlSettings);
            ApplyCommandControlsVisibility(enabled);
        }

        public void SetAutoAttackEnabled(bool enabled)
        {
            controlSettings.autoAttackEnabled = enabled;
            pendingControlSettings.autoAttackEnabled = enabled;
            MobileControlSettingsStore.Save(controlSettings);
            aimControlsPlayer?.SetAutoAttackEnabled(enabled);
            autoAttackToggle?.SetIsOnWithoutNotify(enabled);
            RefreshAutoAttackSwitch();
        }

        private void ApplyCommandControlsVisibility(bool enabled)
        {
            if (aimJoystick != null &&
                aimJoystick.gameObject.activeSelf != enabled)
            {
                aimJoystick.gameObject.SetActive(enabled);
            }

            if (attackButton != null &&
                attackButton.gameObject.activeSelf != enabled)
            {
                attackButton.gameObject.SetActive(enabled);
            }

        }

        public void Bind(HudButtonId id, Action callback)
        {
            if (id == HudButtonId.Count)
            {
                return;
            }

            buttonCallbacks[(int)id] = callback;
            TryBindExistingButton(id);
        }

        public void SetText(HudTextId id, string value)
        {
            TMP_Text label = id switch
            {
                HudTextId.Time => timeLabel,
                HudTextId.Hint => hintLabel,
                _ => null
            };
            if (label != null)
            {
                label.text = value;
            }
        }

        public void ShowCardSelection(bool visible)
        {
            if (visible)
            {
                EnsureCardSelectionPanel();
            }

            if (cardSelectionPanel != null)
            {
                cardSelectionPanel.SetActive(visible);
            }
        }

        public void ShowDifficultySelection(bool visible)
        {
            if (visible)
            {
                EnsureDifficultySelectionPanel();
            }

            if (difficultySelectionPanel != null)
            {
                difficultySelectionPanel.SetActive(visible);
                if (visible)
                {
                    difficultySelectionPanel.transform.SetAsLastSibling();
                }
            }
        }

        public void SetCardChoicesInteractable(bool interactable)
        {
            cardChoicesInteractable = interactable;
            for (int index = 0;
                 index < cardChoiceButtons.Length;
                 index++)
            {
                Button button = cardChoiceButtons[index];
                if (button != null && button.gameObject.activeSelf)
                {
                    button.interactable = interactable;
                }
            }

            ApplyCardRerollState();
        }

        public void SetCardRerollState(
            int remainingRerolls,
            bool hasAlternative)
        {
            remainingCardRerolls = Mathf.Max(0, remainingRerolls);
            hasCardRerollAlternative = hasAlternative;
            ApplyCardRerollState();
        }

        public void SetCardChoices(
            IReadOnlyList<LevelUpCardChoiceData> choices)
        {
            EnsureCardSelectionPanel();
            for (int index = 0;
                 index < cardChoiceViews.Length;
                 index++)
            {
                Button button = cardChoiceButtons[index];
                if (button == null)
                {
                    continue;
                }

                bool visible = index < choices.Count;
                button.gameObject.SetActive(visible);
                button.interactable =
                    visible && cardChoicesInteractable;
                if (visible && cardChoiceViews[index] != null)
                {
                    cardChoiceViews[index].SetContent(choices[index]);
                }
            }

            ApplyCardRerollState();
        }

        public void ShowGameOver(bool visible)
        {
            if (visible)
            {
                EnsureGameOverPanel();
                if (gameOverTitle != null)
                {
                    gameOverTitle.text = gameOverDetails;
                }
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(visible);
            }
        }

        public void SetGameOverDetails(string value)
        {
            gameOverDetails = value;
            if (gameOverTitle != null)
            {
                gameOverTitle.text = value;
            }
        }

        public void SetExperience(int current, int required)
        {
            if (required <= 0)
            {
                experienceSlider.SetValueWithoutNotify(1f);
                experienceLabel.text = Text(
                    GameStringIds.HudMaxLevel,
                    "최대 레벨");
                return;
            }

            int clampedCurrent = Mathf.Clamp(current, 0, required);
            int remaining = Mathf.Max(0, required - clampedCurrent);
            experienceSlider.SetValueWithoutNotify(
                (float)clampedCurrent / required);
            experienceLabel.text = Format(
                GameStringIds.HudExperienceRemainingFormat,
                "다음 레벨까지 경험치 {0}",
                remaining);
        }

        public void SetPauseDetails(PauseDetailsData value)
        {
            pauseDetails = value;
            ApplyPauseDetails();
        }

        public void ShowPauseDetails(bool visible)
        {
            if (visible)
            {
                EnsurePauseDetailsPanel();
                pendingControlSettings = controlSettings;
                editingControlSettings = false;
                ApplyPauseDetails();
                RestoreAppliedControlPresentation();
                SetControlSettingsPageVisible(false);
            }
            else
            {
                DiscardControlSettingsDraft();
            }

            if (pauseDetailsPanel != null)
            {
                pauseDetailsPanel.SetActive(visible);
                if (visible)
                {
                    pauseDetailsPanel.transform.SetAsLastSibling();
                }
            }
        }

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

            BindButton(controlSettingsButton, ToggleControlSettingsPage);
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

        private void BindControlSettingSliders()
        {
            ConfigureControlSlider(
                joystickSizeSlider,
                MobileControlSettingsStore.MinimumScale,
                MobileControlSettingsStore.MaximumScale,
                OnJoystickSizeChanged);
            ConfigureControlSlider(
                attackSizeSlider,
                MobileControlSettingsStore.MinimumScale,
                MobileControlSettingsStore.MaximumScale,
                OnAttackSizeChanged);
        }

        private static void ConfigureControlSlider(
            Slider slider,
            float minimum,
            float maximum,
            UnityEngine.Events.UnityAction<float> callback)
        {
            slider.minValue = minimum;
            slider.maxValue = maximum;
            slider.wholeNumbers = false;
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(callback);
        }

        private void ToggleControlSettingsPage()
        {
            if (editingControlSettings)
            {
                CloseControlSettingsPage();
            }
            else
            {
                OpenControlSettingsPage();
            }
        }

        private void OpenControlSettingsPage()
        {
            editingControlSettings = true;
            SynchronizeControlSettingsUi();
            ApplyControlLayout(pendingControlSettings);
            ApplyControlModePresentation(
                pendingControlSettings.controlMode);
            ApplyCommandControlsVisibility(
                pendingControlSettings.controlsEnabled);
            SetControlSettingsPageVisible(true);
        }

        private void CloseControlSettingsPage()
        {
            editingControlSettings = false;
            RestoreAppliedControlPresentation();
            SetControlSettingsPageVisible(false);
        }

        private void RestoreDefaultControlSettings()
        {
            pendingControlSettings = MobileControlSettings.Default;
            SynchronizeControlSettingsUi();
            PreviewPendingControlSettings();
        }

        private void ApplyPendingControlSettings()
        {
            MobileControlMode previousMode =
                controlSettings.controlMode;
            controlSettings = MobileControlSettingsStore.Clamp(
                pendingControlSettings);
            commandControlsEnabled = controlSettings.controlsEnabled;
            pendingControlSettings = controlSettings;
            MobileControlSettingsStore.Save(controlSettings);
            editingControlSettings = false;
            if (previousMode != controlSettings.controlMode ||
                !commandControlsEnabled)
            {
                aimJoystick?.CancelInput();
            }

            aimControlsPlayer?.SetControlMode(
                controlSettings.controlMode);
            aimControlsPlayer?.SetAutoAttackEnabled(
                controlSettings.autoAttackEnabled);
            RestoreAppliedControlPresentation();
            SetControlSettingsPageVisible(false);
        }

        private void DiscardControlSettingsDraft()
        {
            pendingControlSettings = controlSettings;
            editingControlSettings = false;
            RestoreAppliedControlPresentation();
            SetControlSettingsPageVisible(false);
        }

        private void RestoreAppliedControlPresentation()
        {
            ApplyControlModePresentation(
                controlSettings.controlMode);
            ApplyControlLayout(controlSettings);
            ApplyCommandControlsVisibility(
                controlSettings.controlsEnabled);
        }

        private void SetControlSettingsPageVisible(bool visible)
        {
            settingsPage?.SetActive(!visible);
            controlSettingsPanel?.SetActive(visible);
            if (settingsButton != null)
            {
                settingsButton.interactable = !visible;
            }

            if (settingsButtonGroup != null)
            {
                settingsButtonGroup.alpha = visible ? 0.35f : 1f;
                settingsButtonGroup.interactable = !visible;
                settingsButtonGroup.blocksRaycasts = !visible;
            }

            controlDragSurface?.SetDragEnabled(
                visible && pendingControlSettings.controlsEnabled);
        }

        private void SynchronizeControlSettingsUi()
        {
            joystickSizeSlider.SetValueWithoutNotify(
                pendingControlSettings.joystickScale);
            attackSizeSlider.SetValueWithoutNotify(
                pendingControlSettings.attackScale);
            autoAttackToggle.SetIsOnWithoutNotify(
                pendingControlSettings.autoAttackEnabled);
            RefreshControlSettingLabels();
            ApplyControlModePresentation(
                pendingControlSettings.controlMode);
        }

        private void OnJoystickSizeChanged(float value)
        {
            pendingControlSettings.joystickScale = value;
            PreviewPendingControlSettings();
        }

        private void OnAutoAttackDraftChanged(bool enabled)
        {
            pendingControlSettings.autoAttackEnabled = enabled;
            RefreshAutoAttackSwitch();
        }

        private void SelectControlMode(MobileControlMode mode)
        {
            pendingControlSettings.controlsEnabled = true;
            pendingControlSettings.controlMode = mode;
            PreviewPendingControlSettings();
        }

        private void SelectHiddenControlMode()
        {
            pendingControlSettings.controlsEnabled = false;
            PreviewPendingControlSettings();
        }

        private void OnAttackSizeChanged(float value)
        {
            pendingControlSettings.attackScale = value;
            PreviewPendingControlSettings();
        }

        private void PreviewPendingControlSettings()
        {
            pendingControlSettings = MobileControlSettingsStore.Clamp(
                pendingControlSettings);
            RefreshControlSettingLabels();
            ApplyControlLayout(pendingControlSettings);
            ApplyControlModePresentation(
                pendingControlSettings.controlMode);
            ApplyCommandControlsVisibility(
                pendingControlSettings.controlsEnabled);
            controlDragSurface?.SetDragEnabled(
                editingControlSettings &&
                pendingControlSettings.controlsEnabled);
        }

        private void RefreshControlSettingLabels()
        {
            SetControlSettingLabel(
                joystickSizeSlider,
                pendingControlSettings.joystickScale);
            SetControlSettingLabel(
                attackSizeSlider,
                pendingControlSettings.attackScale);
            RefreshControlModeButtons();
            RefreshAutoAttackSwitch();
        }

        private static void SetControlSettingLabel(
            Slider slider,
            float value)
        {
            Transform valueTransform = slider.transform.Find("Value");
            TMP_Text label = valueTransform != null
                ? valueTransform.GetComponent<TMP_Text>()
                : null;
            if (label != null)
            {
                label.text = $"{Mathf.RoundToInt(value * 100f)}%";
            }
        }

        private void RefreshControlModeButtons()
        {
            bool hidden = !pendingControlSettings.controlsEnabled;
            SetControlModeButtonSelected(
                modeOneButton,
                !hidden && pendingControlSettings.controlMode ==
                    MobileControlMode.DirectMoveAutoAim);
            SetControlModeButtonSelected(
                modeTwoButton,
                !hidden && pendingControlSettings.controlMode ==
                    MobileControlMode.AimCommand);
            SetControlModeButtonSelected(hiddenModeButton, hidden);
        }

        private static void SetControlModeButtonSelected(
            Button button,
            bool selected)
        {
            Image image = button != null
                ? button.GetComponent<Image>()
                : null;
            if (image != null)
            {
                image.color = selected
                    ? new Color(0.16f, 0.56f, 0.92f, 0.98f)
                    : new Color(0.23f, 0.27f, 0.31f, 0.94f);
            }
        }

        private void RefreshAutoAttackSwitch()
        {
            if (autoAttackToggle == null)
            {
                return;
            }

            bool enabled = pendingControlSettings.autoAttackEnabled;
            autoAttackToggle.SetIsOnWithoutNotify(enabled);
            if (autoAttackTrack != null)
            {
                autoAttackTrack.color = enabled
                    ? new Color(0.16f, 0.56f, 0.92f, 1f)
                    : new Color(0.34f, 0.36f, 0.39f, 1f);
            }

            if (autoAttackKnob != null)
            {
                Vector2 position = autoAttackKnob.anchoredPosition;
                position.x = enabled ? 32f : -32f;
                autoAttackKnob.anchoredPosition = position;
            }

            if (autoAttackKnobImage != null)
            {
                autoAttackKnobImage.color = enabled
                    ? new Color(0.52f, 0.78f, 1f, 1f)
                    : new Color(0.62f, 0.63f, 0.65f, 1f);
            }

            if (autoAttackValueLabel != null)
            {
                autoAttackValueLabel.text = enabled
                    ? Text(GameStringIds.UiAutoAttackOn, "On")
                    : Text(GameStringIds.UiAutoAttackOff, "Off");
            }
        }

        private void OnControlDragged(
            ControlLayoutDragTarget target,
            Vector2 screenPoint,
            Camera eventCamera)
        {
            RectTransform control = target ==
                ControlLayoutDragTarget.Joystick
                    ? aimJoystick?.TouchArea
                    : attackButton != null
                        ? attackButton.GetComponent<RectTransform>()
                        : null;
            RectTransform parent = control != null
                ? control.parent as RectTransform
                : null;
            if (parent == null ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent,
                    screenPoint,
                    eventCamera,
                    out Vector2 localPoint))
            {
                return;
            }

            Vector2 baseSize = target == ControlLayoutDragTarget.Joystick
                ? joystickBaseSize
                : attackBaseSize;
            float scale = target == ControlLayoutDragTarget.Joystick
                ? pendingControlSettings.joystickScale
                : pendingControlSettings.attackScale;
            Rect safeArea = MobileControlSettingsStore
                .CalculateSafeAreaInParent(
                    parent.rect,
                    Screen.safeArea,
                    new Vector2(Screen.width, Screen.height));
            Vector2 normalized = MobileControlSettingsStore
                .CalculateNormalizedPosition(
                    safeArea,
                    baseSize,
                    scale,
                    localPoint);
            if (target == ControlLayoutDragTarget.Joystick)
            {
                pendingControlSettings.joystickPosition = normalized;
            }
            else
            {
                pendingControlSettings.attackPosition = normalized;
            }

            PreviewPendingControlSettings();
        }

        private void ApplyControlModePresentation(
            MobileControlMode mode)
        {
            TMP_Text label = attackButton != null
                ? attackButton.GetComponentInChildren<TMP_Text>(true)
                : null;
            if (label != null)
            {
                label.text = mode ==
                    MobileControlMode.DirectMoveAutoAim
                        ? Text(
                            GameStringIds.UiAutoAimButton,
                            "자동 조준")
                        : Text(
                            GameStringIds.UiAttackButton,
                            "공격");
            }
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

        private void ApplyPersistentStrings()
        {
            SetButtonText(
                settingsButton,
                GameStringIds.UiSettingsButton,
                "설정");
            ApplyControlModePresentation(controlSettings.controlMode);
        }

        private void ApplyDifficultyStrings()
        {
            if (difficultySelectionPanel == null)
            {
                return;
            }

            Transform root = difficultySelectionPanel.transform;
            SetTextAtPath(
                root,
                "DifficultyTitle",
                GameStringIds.UiDifficultyTitle,
                "난이도 선택");

            Transform descriptionTransform = root.Find(
                "DifficultyDescription");
            TMP_Text description = descriptionTransform != null
                ? descriptionTransform.GetComponent<TMP_Text>()
                : null;
            if (description != null)
            {
                description.text = Format(
                    GameStringIds.UiDifficultyStageFormat,
                    "{0}\n{1}\n난이도는 이번 게임의 적 수와 " +
                    "적 레벨에 적용됩니다.",
                    difficultyStageName,
                    difficultyStageDescription);
            }

            string optionFallback = "{0}\n{1}";
            SetButtonText(
                difficultyEasyButton,
                Format(
                    GameStringIds.UiDifficultyOptionFormat,
                    optionFallback,
                    Text(
                        GameStringIds.DifficultyEasyName,
                        "쉬움"),
                    Text(
                        GameStringIds.DifficultyEasyDescription,
                        "적 수 75% · 적 레벨 80%")));
            SetButtonText(
                difficultyNormalButton,
                Format(
                    GameStringIds.UiDifficultyOptionFormat,
                    optionFallback,
                    Text(
                        GameStringIds.DifficultyNormalName,
                        "보통"),
                    Text(
                        GameStringIds.DifficultyNormalDescription,
                        "현재 밸런스")));
        }

        private void ApplyPauseStrings()
        {
            if (pauseDetailsPanel == null)
            {
                return;
            }

            Transform root = pauseDetailsPanel.transform;
            SetTextAtPath(
                root,
                "SettingsPage/PlayerStatsTitle",
                GameStringIds.PauseStatsTitle,
                "현재 스탯");
            SetTextAtPath(
                root,
                "SettingsPage/SkillsTitle",
                GameStringIds.PauseSkillsTitle,
                "획득한 스킬");
            SetTextAtPath(
                root,
                "ControlSettingsPanel/AutoAttackToggle/Label",
                GameStringIds.UiAutoAttack,
                "자동 공격");
            SetTextAtPath(
                root,
                "ControlSettingsButton/Label",
                GameStringIds.UiControlButton,
                "조작");
            SetTextAtPath(
                root,
                "ControlSettingsPanel/ControlSettingsTitle",
                GameStringIds.UiControlTitle,
                "조작 패널 설정");
            SetTextAtPath(
                root,
                "ControlSettingsPanel/JoystickSizeSlider/Label",
                GameStringIds.UiJoystickSize,
                "왼쪽 조이스틱 크기");
            SetTextAtPath(
                root,
                "ControlSettingsPanel/AttackSizeSlider/Label",
                GameStringIds.UiAttackSize,
                "오른쪽 공격 버튼 크기");
            SetTextAtPath(
                root,
                "ControlSettingsPanel/ControlModeLabel",
                GameStringIds.UiControlMode,
                "조작 모드");
            SetTextAtPath(
                root,
                "ControlSettingsPanel/ControlModeButtons/Mode1Button/Label",
                GameStringIds.ControlModeOneName,
                "모드 1");
            SetTextAtPath(
                root,
                "ControlSettingsPanel/ControlModeButtons/Mode2Button/Label",
                GameStringIds.ControlModeTwoName,
                "모드 2");
            SetTextAtPath(
                root,
                "ControlSettingsPanel/ControlModeButtons/HiddenButton/Label",
                GameStringIds.ControlModeHiddenName,
                "숨기기");
            SetTextAtPath(
                root,
                "ControlSettingsPanel/ControlDefaultsButton/Label",
                GameStringIds.UiDefaults,
                "기본값");
            SetTextAtPath(
                root,
                "ControlSettingsPanel/ControlApplyButton/Label",
                GameStringIds.UiApply,
                "적용");
            RefreshControlSettingLabels();
        }

        private string Text(string stringId, string fallback)
        {
            return gameStrings != null
                ? gameStrings.Get(stringId, fallback)
                : fallback;
        }

        private string Format(
            string stringId,
            string fallbackTemplate,
            params object[] arguments)
        {
            if (gameStrings != null)
            {
                return gameStrings.Format(
                    stringId,
                    fallbackTemplate,
                    arguments);
            }

            try
            {
                return string.Format(
                    fallbackTemplate,
                    arguments ?? Array.Empty<object>());
            }
            catch (FormatException)
            {
                return fallbackTemplate;
            }
        }

        private void SetTextAtPath(
            Transform root,
            string path,
            string stringId,
            string fallback)
        {
            Transform child = root != null ? root.Find(path) : null;
            TMP_Text label = child != null
                ? child.GetComponent<TMP_Text>()
                : null;
            if (label != null)
            {
                label.text = Text(stringId, fallback);
            }
        }

        private static void SetButtonText(Button button, string value)
        {
            TMP_Text label = button != null
                ? button.GetComponentInChildren<TMP_Text>(true)
                : null;
            if (label != null)
            {
                label.text = value;
            }
        }

        private void SetButtonText(
            Button button,
            string stringId,
            string fallback)
        {
            SetButtonText(button, Text(stringId, fallback));
        }

        private static void BindButton(
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

        private void CaptureControlBaseSizes()
        {
            RectTransform joystickRect = aimJoystick != null
                ? aimJoystick.TouchArea
                : null;
            RectTransform attackRect = attackButton != null
                ? attackButton.GetComponent<RectTransform>()
                : null;
            joystickBaseSize = GetControlBaseSize(joystickRect);
            attackBaseSize = GetControlBaseSize(attackRect);
        }

        private static Vector2 GetControlBaseSize(RectTransform control)
        {
            if (control == null)
            {
                return Vector2.zero;
            }

            Vector2 size = control.rect.size;
            if (size.x <= 0f || size.y <= 0f)
            {
                size = control.sizeDelta;
            }

            return new Vector2(
                Mathf.Abs(size.x),
                Mathf.Abs(size.y));
        }

        private void ApplyControlLayout(MobileControlSettings settings)
        {
            RectTransform joystickRect = aimJoystick != null
                ? aimJoystick.TouchArea
                : null;
            RectTransform attackRect = attackButton != null
                ? attackButton.GetComponent<RectTransform>()
                : null;
            ApplyControlLayout(
                joystickRect,
                joystickBaseSize,
                settings.joystickScale,
                settings.joystickPosition);
            ApplyControlLayout(
                attackRect,
                attackBaseSize,
                settings.attackScale,
                settings.attackPosition);
        }

        private static void ApplyControlLayout(
            RectTransform control,
            Vector2 baseSize,
            float scale,
            Vector2 normalizedPosition)
        {
            RectTransform parent = control != null
                ? control.parent as RectTransform
                : null;
            if (parent == null || baseSize.x <= 0f || baseSize.y <= 0f)
            {
                return;
            }

            MobileControlSettingsStore.Apply(
                control,
                baseSize,
                scale,
                normalizedPosition,
                parent.rect,
                Screen.safeArea,
                new Vector2(Screen.width, Screen.height));
        }

        private void OnRectTransformDimensionsChange()
        {
            if (joystickBaseSize.x <= 0f || attackBaseSize.x <= 0f)
            {
                return;
            }

            ApplyControlLayout(
                editingControlSettings
                    ? pendingControlSettings
                    : controlSettings);
        }

        private void ValidateConfiguration()
        {
            if (timeLabel == null ||
                hintLabel == null ||
                experienceSlider == null ||
                experienceLabel == null ||
                settingsButton == null ||
                attackButton == null ||
                attackButton.GetComponent<AttackCommandButton>() == null ||
                aimJoystick == null ||
                modalRoot == null ||
                cardSelectionPanelPrefab == null ||
                pauseDetailsPanelPrefab == null ||
                gameOverPanelPrefab == null ||
                difficultySelectionPanelPrefab == null)
            {
                Debug.LogError(
                    "PrototypeHUD prefab references are incomplete.",
                    this);
            }
        }
    }
}
