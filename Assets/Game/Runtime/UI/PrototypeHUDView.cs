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
        PlayerHp,
        Hint,
        Count
    }

    public sealed class PrototypeHUDView : MonoBehaviour
    {
        [Header("Persistent HUD")]
        [SerializeField] private TMP_Text timeLabel;
        [SerializeField] private TMP_Text playerHpLabel;
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
        private TMP_Text pauseDetailsLabel;
        private Toggle commandControlsToggle;
        private Toggle autoAttackToggle;
        private Button controlSettingsButton;
        private GameObject controlSettingsPanel;
        private Image pauseDetailsBackground;
        private Image controlSettingsBackground;
        private Color pauseDetailsBackgroundColor;
        private Color controlSettingsBackgroundColor;
        private Slider joystickSizeSlider;
        private Slider joystickHorizontalSlider;
        private Slider joystickVerticalSlider;
        private Slider attackSizeSlider;
        private Slider attackHorizontalSlider;
        private Slider attackVerticalSlider;
        private GameObject gameOverPanel;
        private GameObject difficultySelectionPanel;
        private Button difficultyEasyButton;
        private Button difficultyNormalButton;
        private TMP_Text gameOverTitle;
        private Button continueButton;
        private bool cardChoicesInteractable;
        private bool hasCardRerollAlternative;
        private int remainingCardRerolls;
        private string pauseDetails = string.Empty;
        private string gameOverDetails = string.Empty;
        private bool commandControlsEnabled = true;
        private bool editingControlSettings;
        private MobileControlSettings controlSettings;
        private MobileControlSettings pendingControlSettings;
        private Vector2 joystickBaseSize;
        private Vector2 attackBaseSize;
        private PlayerRoot aimControlsPlayer;

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
            TMP_Text configuredPlayerHpLabel,
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
            playerHpLabel = configuredPlayerHpLabel;
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
            ApplyCommandControlsVisibility(commandControlsEnabled);
        }

        public void InitializeAimControls(PlayerRoot player)
        {
            aimControlsPlayer = player;
            aimJoystick?.Initialize(player);
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

            commandControlsToggle?.SetIsOnWithoutNotify(enabled);
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
                HudTextId.PlayerHp => playerHpLabel,
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
                experienceLabel.text = "최대 레벨";
                return;
            }

            int clampedCurrent = Mathf.Clamp(current, 0, required);
            int remaining = Mathf.Max(0, required - clampedCurrent);
            experienceSlider.SetValueWithoutNotify(
                (float)clampedCurrent / required);
            experienceLabel.text =
                $"다음 레벨까지 경험치 {remaining}";
        }

        public void SetPauseDetails(string value)
        {
            pauseDetails = value;
            if (pauseDetailsLabel != null)
            {
                pauseDetailsLabel.text = value;
            }
        }

        public void ShowPauseDetails(bool visible)
        {
            if (visible)
            {
                EnsurePauseDetailsPanel();
                if (pauseDetailsLabel != null)
                {
                    pauseDetailsLabel.text = pauseDetails;
                }
            }
            else if (editingControlSettings)
            {
                CancelControlSettings();
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
            Transform label =
                pauseDetailsPanel.transform.Find("PauseDetails");
            pauseDetailsLabel =
                label != null ? label.GetComponent<TMP_Text>() : null;
            if (pauseDetailsLabel == null)
            {
                Debug.LogError(
                    "Pause prefab is missing PauseDetails text.",
                    pauseDetailsPanel);
            }

            Transform toggle =
                pauseDetailsPanel.transform.Find(
                    "ControlPadToggle");
            commandControlsToggle =
                toggle != null
                    ? toggle.GetComponent<Toggle>()
                    : null;
            if (commandControlsToggle == null)
            {
                Debug.LogError(
                    "Pause prefab is missing ControlPadToggle.",
                    pauseDetailsPanel);
                return;
            }

            commandControlsToggle.SetIsOnWithoutNotify(
                commandControlsEnabled);
            commandControlsToggle.onValueChanged.AddListener(
                SetCommandControlsEnabled);

            Transform autoAttackToggleTransform =
                pauseDetailsPanel.transform.Find(
                    "AutoAttackToggle");
            autoAttackToggle = autoAttackToggleTransform != null
                ? autoAttackToggleTransform.GetComponent<Toggle>()
                : null;
            if (autoAttackToggle == null)
            {
                Debug.LogError(
                    "Pause prefab is missing AutoAttackToggle.",
                    pauseDetailsPanel);
                return;
            }

            autoAttackToggle.SetIsOnWithoutNotify(
                controlSettings.autoAttackEnabled);
            autoAttackToggle.onValueChanged.AddListener(
                SetAutoAttackEnabled);

            Transform settingsButtonTransform =
                pauseDetailsPanel.transform.Find(
                    "ControlSettingsButton");
            controlSettingsButton = settingsButtonTransform != null
                ? settingsButtonTransform.GetComponent<Button>()
                : null;
            Transform settingsPanelTransform =
                pauseDetailsPanel.transform.Find(
                    "ControlSettingsPanel");
            controlSettingsPanel = settingsPanelTransform != null
                ? settingsPanelTransform.gameObject
                : null;
            if (controlSettingsButton == null ||
                controlSettingsPanel == null)
            {
                Debug.LogError(
                    "Pause prefab is missing control settings UI.",
                    pauseDetailsPanel);
                return;
            }

            pauseDetailsBackground =
                pauseDetailsPanel.GetComponent<Image>();
            controlSettingsBackground =
                controlSettingsPanel.GetComponent<Image>();
            if (pauseDetailsBackground != null)
            {
                pauseDetailsBackgroundColor =
                    pauseDetailsBackground.color;
            }

            if (controlSettingsBackground != null)
            {
                controlSettingsBackgroundColor =
                    controlSettingsBackground.color;
            }

            joystickSizeSlider = FindControlSlider(
                settingsPanelTransform,
                "JoystickSizeSlider");
            joystickHorizontalSlider = FindControlSlider(
                settingsPanelTransform,
                "JoystickHorizontalSlider");
            joystickVerticalSlider = FindControlSlider(
                settingsPanelTransform,
                "JoystickVerticalSlider");
            attackSizeSlider = FindControlSlider(
                settingsPanelTransform,
                "AttackSizeSlider");
            attackHorizontalSlider = FindControlSlider(
                settingsPanelTransform,
                "AttackHorizontalSlider");
            attackVerticalSlider = FindControlSlider(
                settingsPanelTransform,
                "AttackVerticalSlider");
            Button defaultsButton = FindControlButton(
                settingsPanelTransform,
                "ControlDefaultsButton");
            Button cancelButton = FindControlButton(
                settingsPanelTransform,
                "ControlCancelButton");
            Button applyButton = FindControlButton(
                settingsPanelTransform,
                "ControlApplyButton");
            if (joystickSizeSlider == null ||
                joystickHorizontalSlider == null ||
                joystickVerticalSlider == null ||
                attackSizeSlider == null ||
                attackHorizontalSlider == null ||
                attackVerticalSlider == null ||
                defaultsButton == null ||
                cancelButton == null ||
                applyButton == null)
            {
                Debug.LogError(
                    "Control settings panel references are incomplete.",
                    pauseDetailsPanel);
                return;
            }

            BindButton(controlSettingsButton, OpenControlSettings);
            BindButton(defaultsButton, RestoreDefaultControlSettings);
            BindButton(cancelButton, CancelControlSettings);
            BindButton(applyButton, ApplyPendingControlSettings);
            BindControlSettingSliders();
            ArrangeControlSettingsPanel();
            SetControlSettingsPageVisible(false);
        }

        private static Slider FindControlSlider(
            Transform parent,
            string name)
        {
            Transform child = parent.Find(name);
            return child != null ? child.GetComponent<Slider>() : null;
        }

        private static Button FindControlButton(
            Transform parent,
            string name)
        {
            Transform child = parent.Find(name);
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
                joystickHorizontalSlider,
                0f,
                1f,
                OnJoystickHorizontalChanged);
            ConfigureControlSlider(
                joystickVerticalSlider,
                0f,
                1f,
                OnJoystickVerticalChanged);
            ConfigureControlSlider(
                attackSizeSlider,
                MobileControlSettingsStore.MinimumScale,
                MobileControlSettingsStore.MaximumScale,
                OnAttackSizeChanged);
            ConfigureControlSlider(
                attackHorizontalSlider,
                0f,
                1f,
                OnAttackHorizontalChanged);
            ConfigureControlSlider(
                attackVerticalSlider,
                0f,
                1f,
                OnAttackVerticalChanged);
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

        private void OpenControlSettings()
        {
            pendingControlSettings = controlSettings;
            editingControlSettings = true;
            SynchronizeControlSettingSliders();
            ApplyControlLayout(pendingControlSettings);
            ApplyCommandControlsVisibility(true);
            SetControlSettingsPageVisible(true);
        }

        private void RestoreDefaultControlSettings()
        {
            bool controlsEnabled = commandControlsEnabled;
            bool autoAttackEnabled =
                controlSettings.autoAttackEnabled;
            pendingControlSettings = MobileControlSettings.Default;
            pendingControlSettings.controlsEnabled = controlsEnabled;
            pendingControlSettings.autoAttackEnabled =
                autoAttackEnabled;
            SynchronizeControlSettingSliders();
            ApplyControlLayout(pendingControlSettings);
        }

        private void ApplyPendingControlSettings()
        {
            controlSettings = MobileControlSettingsStore.Clamp(
                pendingControlSettings);
            controlSettings.controlsEnabled = commandControlsEnabled;
            pendingControlSettings = controlSettings;
            MobileControlSettingsStore.Save(controlSettings);
            editingControlSettings = false;
            ApplyControlLayout(controlSettings);
            ApplyCommandControlsVisibility(commandControlsEnabled);
            SetControlSettingsPageVisible(false);
        }

        private void CancelControlSettings()
        {
            pendingControlSettings = controlSettings;
            editingControlSettings = false;
            ApplyControlLayout(controlSettings);
            ApplyCommandControlsVisibility(commandControlsEnabled);
            SetControlSettingsPageVisible(false);
        }

        private void SetControlSettingsPageVisible(bool visible)
        {
            pauseDetailsLabel?.gameObject.SetActive(!visible);
            commandControlsToggle?.gameObject.SetActive(!visible);
            autoAttackToggle?.gameObject.SetActive(!visible);
            controlSettingsButton?.gameObject.SetActive(!visible);
            controlSettingsPanel?.SetActive(visible);
            SetPreviewBackgroundAlpha(visible);
        }

        private void SetPreviewBackgroundAlpha(bool previewVisible)
        {
            if (pauseDetailsBackground != null)
            {
                Color color = pauseDetailsBackgroundColor;
                if (previewVisible)
                {
                    color.a = 0.12f;
                }

                pauseDetailsBackground.color = color;
            }

            if (controlSettingsBackground != null)
            {
                Color color = controlSettingsBackgroundColor;
                if (previewVisible)
                {
                    color.a = 0.45f;
                }

                controlSettingsBackground.color = color;
            }
        }

        private void SynchronizeControlSettingSliders()
        {
            joystickSizeSlider.SetValueWithoutNotify(
                pendingControlSettings.joystickScale);
            joystickHorizontalSlider.SetValueWithoutNotify(
                pendingControlSettings.joystickPosition.x);
            joystickVerticalSlider.SetValueWithoutNotify(
                pendingControlSettings.joystickPosition.y);
            attackSizeSlider.SetValueWithoutNotify(
                pendingControlSettings.attackScale);
            attackHorizontalSlider.SetValueWithoutNotify(
                pendingControlSettings.attackPosition.x);
            attackVerticalSlider.SetValueWithoutNotify(
                pendingControlSettings.attackPosition.y);
            RefreshControlSettingLabels();
        }

        private void OnJoystickSizeChanged(float value)
        {
            pendingControlSettings.joystickScale = value;
            PreviewPendingControlSettings();
        }

        private void OnJoystickHorizontalChanged(float value)
        {
            pendingControlSettings.joystickPosition.x = value;
            PreviewPendingControlSettings();
        }

        private void OnJoystickVerticalChanged(float value)
        {
            pendingControlSettings.joystickPosition.y = value;
            PreviewPendingControlSettings();
        }

        private void OnAttackSizeChanged(float value)
        {
            pendingControlSettings.attackScale = value;
            PreviewPendingControlSettings();
        }

        private void OnAttackHorizontalChanged(float value)
        {
            pendingControlSettings.attackPosition.x = value;
            PreviewPendingControlSettings();
        }

        private void OnAttackVerticalChanged(float value)
        {
            pendingControlSettings.attackPosition.y = value;
            PreviewPendingControlSettings();
        }

        private void PreviewPendingControlSettings()
        {
            pendingControlSettings = MobileControlSettingsStore.Clamp(
                pendingControlSettings);
            RefreshControlSettingLabels();
            ApplyControlLayout(pendingControlSettings);
        }

        private void RefreshControlSettingLabels()
        {
            SetControlSettingLabel(
                joystickSizeSlider,
                pendingControlSettings.joystickScale);
            SetControlSettingLabel(
                joystickHorizontalSlider,
                pendingControlSettings.joystickPosition.x);
            SetControlSettingLabel(
                joystickVerticalSlider,
                pendingControlSettings.joystickPosition.y);
            SetControlSettingLabel(
                attackSizeSlider,
                pendingControlSettings.attackScale);
            SetControlSettingLabel(
                attackHorizontalSlider,
                pendingControlSettings.attackPosition.x);
            SetControlSettingLabel(
                attackVerticalSlider,
                pendingControlSettings.attackPosition.y);
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

        private void ArrangeControlSettingsPanel()
        {
            RectTransform panelRect = controlSettingsPanel != null
                ? controlSettingsPanel.GetComponent<RectTransform>()
                : null;
            if (panelRect == null ||
                panelRect.rect.width <= 0f ||
                panelRect.rect.height <= 0f)
            {
                return;
            }

            Vector2 panelSize = panelRect.rect.size;
            float sliderWidth =
                MobileControlSettingsStore
                    .CalculateSettingsSliderWidth(panelSize);
            ArrangeControlSlider(
                joystickSizeSlider,
                panelSize,
                false,
                0,
                sliderWidth);
            ArrangeControlSlider(
                joystickHorizontalSlider,
                panelSize,
                false,
                1,
                sliderWidth);
            ArrangeControlSlider(
                joystickVerticalSlider,
                panelSize,
                false,
                2,
                sliderWidth);
            ArrangeControlSlider(
                attackSizeSlider,
                panelSize,
                true,
                0,
                sliderWidth);
            ArrangeControlSlider(
                attackHorizontalSlider,
                panelSize,
                true,
                1,
                sliderWidth);
            ArrangeControlSlider(
                attackVerticalSlider,
                panelSize,
                true,
                2,
                sliderWidth);
        }

        private static void ArrangeControlSlider(
            Slider slider,
            Vector2 panelSize,
            bool attackControl,
            int row,
            float width)
        {
            if (slider == null)
            {
                return;
            }

            RectTransform rect =
                slider.GetComponent<RectTransform>();
            rect.anchoredPosition =
                MobileControlSettingsStore
                    .CalculateSettingsSliderPosition(
                        panelSize,
                        attackControl,
                        row);
            rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
            Transform labelTransform = slider.transform.Find("Label");
            RectTransform labelRect = labelTransform != null
                ? labelTransform.GetComponent<RectTransform>()
                : null;
            if (labelRect != null)
            {
                labelRect.sizeDelta = new Vector2(
                    Mathf.Max(140f, width - 140f),
                    labelRect.sizeDelta.y);
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

            BindButton(
                continueButton,
                buttonCallbacks[(int)HudButtonId.ContinueAd]);
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
            ArrangeControlSettingsPanel();
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
                playerHpLabel == null ||
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
