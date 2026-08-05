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
        DifficultyHard,
        Retry,
        ReturnToLobby,
        Count
    }

    public enum HudTextId
    {
        Time,
        Hint,
        Count
    }

    public sealed partial class PrototypeHUDView : MonoBehaviour
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
        [SerializeField] private GameObject confirmationDialogPrefab;

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
        private Button difficultyHardButton;
        private TMP_Text gameOverTitle;
        private Button continueButton;
        private Button resultRetryButton;
        private Button resultLobbyButton;
        private Button pauseRetryButton;
        private Button pauseLobbyButton;
        private GameObject confirmationDialog;
        private TMP_Text confirmationMessage;
        private Button confirmationConfirmButton;
        private Button confirmationCancelButton;
        private HudButtonId pendingConfirmationAction = HudButtonId.Count;
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
        private ControlSettingsProfile controlSettingsProfile;
        private string difficultyStageName = string.Empty;
        private string difficultyStageDescription = string.Empty;
        private Slider bossHealthSlider;
        private TMP_Text bossHealthLabel;

        public GameObject CardSelectionPanelPrefab =>
            cardSelectionPanelPrefab;
        public GameObject PauseDetailsPanelPrefab =>
            pauseDetailsPanelPrefab;
        public GameObject GameOverPanelPrefab =>
            gameOverPanelPrefab;
        public GameObject DifficultySelectionPanelPrefab =>
            difficultySelectionPanelPrefab;
        public GameObject ConfirmationDialogPrefab =>
            confirmationDialogPrefab;
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
            GameObject configuredDifficultySelectionPanelPrefab,
            GameObject configuredConfirmationDialogPrefab = null)
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
            confirmationDialogPrefab = configuredConfirmationDialogPrefab;
        }

        public void Initialize()
        {
            Initialize(null);
        }

        public void Initialize(GameStringTable configuredGameStrings)
        {
            Initialize(configuredGameStrings, null);
        }

        public void Initialize(
            GameStringTable configuredGameStrings,
            ControlSettingsProfile configuredControlSettingsProfile)
        {
            gameStrings = configuredGameStrings;
            controlSettingsProfile = configuredControlSettingsProfile;
            ValidateConfiguration();
            EnsureBossHealthBar();
            cardChoicesInteractable = false;
            if (settingsButton != null)
            {
                settingsButton.transform.SetAsLastSibling();
            }

            CaptureControlBaseSizes();
            controlSettings = MobileControlSettingsStore.Load(
                controlSettingsProfile);
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
                continueButton?.gameObject.SetActive(true);
                SetResultNavigationPositions(0f, 260f);
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

        public void ShowClear(bool visible)
        {
            if (visible)
            {
                EnsureGameOverPanel();
                continueButton?.gameObject.SetActive(false);
                SetResultNavigationPositions(-140f, 140f);
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

        private void SetResultNavigationPositions(
            float retryX,
            float lobbyX)
        {
            if (resultRetryButton != null)
            {
                RectTransform rect = resultRetryButton.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(
                    retryX,
                    rect.anchoredPosition.y);
            }

            if (resultLobbyButton != null)
            {
                RectTransform rect = resultLobbyButton.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(
                    lobbyX,
                    rect.anchoredPosition.y);
            }
        }

        public void SetBossHealth(
            string bossName,
            int current,
            int maximum,
            bool visible)
        {
            EnsureBossHealthBar();
            if (bossHealthSlider == null || bossHealthLabel == null)
            {
                return;
            }

            bossHealthSlider.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            int safeMaximum = Mathf.Max(1, maximum);
            int safeCurrent = Mathf.Clamp(current, 0, safeMaximum);
            bossHealthSlider.SetValueWithoutNotify(
                (float)safeCurrent / safeMaximum);
            bossHealthLabel.text =
                $"{bossName}  {safeCurrent} / {safeMaximum}";
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
                BindControlSettingsNavigation();
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

        private void EnsureBossHealthBar()
        {
            if (bossHealthSlider != null || experienceSlider == null)
            {
                return;
            }

            GameObject bossBar = Instantiate(
                experienceSlider.gameObject,
                experienceSlider.transform.parent);
            bossBar.name = "BossHealthBar";
            bossHealthSlider = bossBar.GetComponent<Slider>();
            bossHealthLabel = bossBar.GetComponentInChildren<TMP_Text>(true);

            RectTransform rect = bossBar.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0f, -120f);
            rect.sizeDelta = new Vector2(0f, 36f);
            if (bossHealthLabel != null)
            {
                bossHealthLabel.name = "BossHealthLabel";
            }

            Image fill = bossHealthSlider != null &&
                bossHealthSlider.fillRect != null
                    ? bossHealthSlider.fillRect.GetComponent<Image>()
                    : null;
            if (fill != null)
            {
                fill.color = new Color(0.82f, 0.12f, 0.16f, 1f);
            }

            bossBar.SetActive(false);
        }
    }
}
