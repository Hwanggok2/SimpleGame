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
        private GameObject gameOverPanel;
        private TMP_Text gameOverTitle;
        private Button continueButton;
        private bool cardChoicesInteractable;
        private bool hasCardRerollAlternative;
        private int remainingCardRerolls;
        private string pauseDetails = string.Empty;
        private string gameOverDetails = string.Empty;
        private bool commandControlsEnabled = true;

        public GameObject CardSelectionPanelPrefab =>
            cardSelectionPanelPrefab;
        public GameObject PauseDetailsPanelPrefab =>
            pauseDetailsPanelPrefab;
        public GameObject GameOverPanelPrefab =>
            gameOverPanelPrefab;
        public Button SettingsButton => settingsButton;
        public Button AttackButton => attackButton;
        public AimJoystickControl AimJoystick => aimJoystick;
        public bool CommandControlsEnabled =>
            commandControlsEnabled;

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
            GameObject configuredGameOverPanelPrefab)
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
        }

        public void Initialize()
        {
            ValidateConfiguration();
            cardChoicesInteractable = false;
            if (settingsButton != null)
            {
                settingsButton.transform.SetAsLastSibling();
            }

            SetCommandControlsEnabled(commandControlsEnabled);
        }

        public void InitializeAimControls(PlayerRoot player)
        {
            aimJoystick?.Initialize(player);
        }

        public void SetCommandControlsEnabled(bool enabled)
        {
            commandControlsEnabled = enabled;
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
                gameOverPanelPrefab == null)
            {
                Debug.LogError(
                    "PrototypeHUD prefab references are incomplete.",
                    this);
            }
        }
    }
}
