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
        ContinueAd,
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
        [Header("Transient UI")]
        [SerializeField] private Transform modalRoot;
        [SerializeField] private GameObject cardSelectionPanelPrefab;
        [SerializeField] private GameObject pauseDetailsPanelPrefab;
        [SerializeField] private GameObject gameOverPanelPrefab;

        private readonly Action[] buttonCallbacks =
            new Action[(int)HudButtonId.Count];
        private readonly Button[] cardChoiceButtons = new Button[3];
        private readonly LevelUpCardView[] cardChoiceViews =
            new LevelUpCardView[3];
        private GameObject cardSelectionPanel;
        private GameObject pauseDetailsPanel;
        private TMP_Text pauseDetailsLabel;
        private GameObject gameOverPanel;
        private TMP_Text gameOverTitle;
        private Button continueButton;
        private bool cardChoicesInteractable;
        private string pauseDetails = string.Empty;
        private string gameOverDetails = string.Empty;

        public GameObject CardSelectionPanelPrefab =>
            cardSelectionPanelPrefab;
        public GameObject PauseDetailsPanelPrefab =>
            pauseDetailsPanelPrefab;
        public GameObject GameOverPanelPrefab =>
            gameOverPanelPrefab;

        public void Configure(
            TMP_Text configuredTimeLabel,
            TMP_Text configuredPlayerHpLabel,
            TMP_Text configuredHintLabel,
            Slider configuredExperienceSlider,
            TMP_Text configuredExperienceLabel,
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
                BindButton(
                    cardChoiceButtons[index],
                    buttonCallbacks[index]);
            }
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

            if (id == HudButtonId.ContinueAd)
            {
                BindButton(continueButton, buttonCallbacks[index]);
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
