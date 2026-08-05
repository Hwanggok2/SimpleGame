using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame
{
    public sealed class LobbyView : MonoBehaviour
    {
        public const string BattleSceneName = GameManager.BattleSceneName;

        [Header("Data")]
        [SerializeField] private GameDataManifest gameData;
        [Header("Navigation")]
        [SerializeField] private Button traitsButton;
        [SerializeField] private Button collectionButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private TMP_Text traitsLabel;
        [SerializeField] private TMP_Text collectionLabel;
        [SerializeField] private TMP_Text settingsLabel;
        [SerializeField] private LobbyCodexView codexView;
        [SerializeField] private LobbySettingsView settingsView;
        [Header("Difficulty Preview")]
        [SerializeField] private TMP_Text objectiveLabel;
        [SerializeField] private Image previewImage;
        [SerializeField] private Image selectedDifficultyImage;
        [SerializeField] private TMP_Text effectLabel;
        [SerializeField] private TMP_Text difficultyTitleLabel;
        [Header("Difficulty Selection")]
        [SerializeField]
        private LobbyDifficultyOptionView[] difficultyOptions;
        [SerializeField] private Button enterButton;
        [SerializeField] private TMP_Text enterButtonLabel;
        [SerializeField] private Color selectedColor =
            new(0.18f, 0.64f, 0.34f, 1f);
        [SerializeField] private Color normalColor =
            new(0.43f, 0.43f, 0.43f, 1f);

        private bool initialized;
        private bool hasSelection;
        private LobbyDifficultyId selectedDifficulty;

        public bool HasSelection => hasSelection;
        public LobbyDifficultyId SelectedDifficulty => selectedDifficulty;
        public bool CanEnter => enterButton != null && enterButton.interactable;
        public LobbyCodexView CodexView => codexView;
        public LobbySettingsView SettingsView => settingsView;

        public void Configure(
            GameDataManifest configuredGameData,
            Button configuredTraitsButton,
            Button configuredCollectionButton,
            Button configuredSettingsButton,
            TMP_Text configuredTraitsLabel,
            TMP_Text configuredCollectionLabel,
            TMP_Text configuredSettingsLabel,
            TMP_Text configuredObjectiveLabel,
            Image configuredPreviewImage,
            Image configuredSelectedDifficultyImage,
            TMP_Text configuredEffectLabel,
            TMP_Text configuredDifficultyTitleLabel,
            LobbyDifficultyOptionView[] configuredDifficultyOptions,
            Button configuredEnterButton,
            TMP_Text configuredEnterButtonLabel,
            LobbyCodexView configuredCodexView)
        {
            gameData = configuredGameData;
            traitsButton = configuredTraitsButton;
            collectionButton = configuredCollectionButton;
            settingsButton = configuredSettingsButton;
            traitsLabel = configuredTraitsLabel;
            collectionLabel = configuredCollectionLabel;
            settingsLabel = configuredSettingsLabel;
            objectiveLabel = configuredObjectiveLabel;
            previewImage = configuredPreviewImage;
            selectedDifficultyImage = configuredSelectedDifficultyImage;
            effectLabel = configuredEffectLabel;
            difficultyTitleLabel = configuredDifficultyTitleLabel;
            difficultyOptions = configuredDifficultyOptions;
            enterButton = configuredEnterButton;
            enterButtonLabel = configuredEnterButtonLabel;
            codexView = configuredCodexView;
        }

        public void SetSettingsView(LobbySettingsView configuredSettingsView)
        {
            settingsView = configuredSettingsView;
        }

        private void Awake()
        {
            GameManager.RegisterLobby(this);
        }

        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            GameManager.UnregisterLobby(this);
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            LocalizeStaticLabels();
            ConfigureNavigationButtons();
            ConfigureDifficultyButtons();

            if (enterButton != null)
            {
                enterButton.onClick.AddListener(EnterBattle);
            }

            if (LobbyDifficultySelectionStore.TryLoad(
                    out LobbyDifficultyId savedDifficulty) &&
                IsAvailable(savedDifficulty))
            {
                ApplySelection(savedDifficulty, false);
            }
            else
            {
                ClearSelection();
            }
        }

        public void SelectDifficulty(LobbyDifficultyId difficultyId)
        {
            if (!initialized)
            {
                Initialize();
            }

            if (!IsAvailable(difficultyId))
            {
                return;
            }

            if (hasSelection && selectedDifficulty == difficultyId)
            {
                ClearSelection();
                return;
            }

            ApplySelection(difficultyId, true);
        }

        public void EnterBattle()
        {
            if (!TryGetSelectedDefinition(
                    out LobbyDifficultyDefinition definition) ||
                !definition.TryGetRuntimeDifficulty(out _))
            {
                return;
            }

            GameManager.Instance.LoadBattle();
        }

        private void ConfigureNavigationButtons()
        {
            codexView?.Initialize();
            settingsView?.Initialize(
                gameData?.GameStrings,
                gameData?.ControlSettings);
            SetPlaceholderButton(traitsButton);
            ConfigureCodexButton(
                collectionButton,
                LobbyCodexTab.Enemy);
            ConfigureSettingsButton();
        }

        private void ConfigureSettingsButton()
        {
            if (settingsButton == null)
            {
                return;
            }

            settingsButton.onClick.RemoveAllListeners();
            settingsButton.interactable = settingsView != null;
            if (settingsView != null)
            {
                settingsButton.onClick.AddListener(settingsView.Toggle);
            }
        }

        private void ConfigureCodexButton(
            Button button,
            LobbyCodexTab tab)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.interactable = codexView != null;
            if (codexView != null)
            {
                button.onClick.AddListener(() => codexView.Open(tab));
            }
        }

        private static void SetPlaceholderButton(Button button)
        {
            if (button != null)
            {
                button.interactable = false;
            }
        }

        private void ConfigureDifficultyButtons()
        {
            if (difficultyOptions == null)
            {
                return;
            }

            foreach (LobbyDifficultyOptionView option in difficultyOptions)
            {
                if (option == null)
                {
                    continue;
                }

                LobbyDifficultyId optionId = option.DifficultyId;
                bool available = TryGetDefinition(
                    optionId,
                    out LobbyDifficultyDefinition definition) &&
                    definition.IsAvailable;
                if (definition != null)
                {
                    string title = Format(
                        GameStringIds.UiLobbyDifficultyLabelFormat,
                        "{0} - {1}분",
                        Text(definition.NameKey, optionId.ToString()),
                        definition.DurationMinutes);
                    string description = Text(
                        definition.ButtonDescriptionKey,
                        string.Empty);
                    if (!available)
                    {
                        description = string.IsNullOrWhiteSpace(description)
                            ? Text(
                                GameStringIds.UiLobbyUnavailable,
                                "준비 중")
                            : $"{description}\n" +
                              Text(
                                  GameStringIds.UiLobbyUnavailable,
                                  "준비 중");
                    }

                    option.SetContent(title, description);
                }

                option.SetState(
                    false,
                    available,
                    selectedColor,
                    normalColor);
                if (available && option.Button != null)
                {
                    option.Button.onClick.AddListener(
                        () => SelectDifficulty(optionId));
                }
            }
        }

        private void ClearSelection()
        {
            hasSelection = false;
            SetDifficultyPreviewVisible(false);
            if (difficultyOptions != null)
            {
                foreach (LobbyDifficultyOptionView option in
                         difficultyOptions)
                {
                    if (option != null)
                    {
                        option.SetState(
                            false,
                            IsAvailable(option.DifficultyId),
                            selectedColor,
                            normalColor);
                    }
                }
            }

            if (objectiveLabel != null)
            {
                objectiveLabel.text = Text(
                    GameStringIds.UiLobbySelectPrompt,
                    "난이도를 선택해 주세요.");
            }

            if (effectLabel != null)
            {
                effectLabel.text = string.Empty;
            }

            if (previewImage != null)
            {
                previewImage.sprite = null;
                previewImage.enabled = false;
            }

            if (selectedDifficultyImage != null)
            {
                selectedDifficultyImage.sprite = null;
                selectedDifficultyImage.enabled = false;
                selectedDifficultyImage.rectTransform.localScale =
                    Vector3.one;
            }

            if (enterButton != null)
            {
                enterButton.interactable = false;
            }
        }

        private void ApplySelection(
            LobbyDifficultyId difficultyId,
            bool save)
        {
            if (!TryGetDefinition(
                    difficultyId,
                    out LobbyDifficultyDefinition definition) ||
                !definition.TryGetRuntimeDifficulty(out _))
            {
                return;
            }

            selectedDifficulty = difficultyId;
            hasSelection = true;
            SetDifficultyPreviewVisible(true);
            if (difficultyOptions != null)
            {
                foreach (LobbyDifficultyOptionView option in
                         difficultyOptions)
                {
                    if (option != null)
                    {
                        option.SetState(
                            option.DifficultyId == difficultyId,
                            IsAvailable(option.DifficultyId),
                            selectedColor,
                            normalColor);
                    }
                }
            }

            if (objectiveLabel != null)
            {
                objectiveLabel.text = Format(
                    definition.ObjectiveKey,
                    "{0}분 동안 생성되는 적을 처치하세요.",
                    definition.DurationMinutes);
            }

            if (effectLabel != null)
            {
                effectLabel.text = Format(
                    definition.EffectDescriptionKey,
                    string.Empty,
                    definition.EnemyCountReductionPercent,
                    definition.EnemyLevelReductionPercent);
            }

            if (previewImage != null)
            {
                Sprite sprite = null;
                bool foundSprite =
                    gameData != null &&
                    gameData.ImageData != null &&
                    gameData.ImageData.TryGet(
                        definition.ImageId,
                        out sprite);
                previewImage.enabled = foundSprite;
                previewImage.sprite = sprite;
            }

            ApplyImage(
                selectedDifficultyImage,
                definition.SelectedDifficultyImageId,
                definition.SelectedDifficultyImageScale);

            if (enterButton != null)
            {
                enterButton.interactable = true;
            }

            if (save)
            {
                LobbyDifficultySelectionStore.Save(difficultyId);
            }
        }

        private void SetDifficultyPreviewVisible(bool visible)
        {
            GameObject preview = previewImage != null
                ? previewImage.transform.parent?.gameObject
                : objectiveLabel?.transform.parent?.parent?.gameObject;
            if (preview != null && preview.activeSelf != visible)
            {
                preview.SetActive(visible);
            }
        }

        private void ApplyImage(
            Image target,
            string imageId,
            float scale)
        {
            if (target == null)
            {
                return;
            }

            Sprite sprite = null;
            bool foundSprite =
                gameData != null &&
                gameData.ImageData != null &&
                gameData.ImageData.TryGet(imageId, out sprite);
            target.sprite = sprite;
            target.enabled = foundSprite;
            float safeScale = Mathf.Max(0.1f, scale);
            target.rectTransform.localScale = new Vector3(
                safeScale,
                safeScale,
                1f);
        }

        private bool IsAvailable(LobbyDifficultyId difficultyId)
        {
            return TryGetDefinition(
                    difficultyId,
                    out LobbyDifficultyDefinition definition) &&
                definition.TryGetRuntimeDifficulty(out _);
        }

        private bool TryGetSelectedDefinition(
            out LobbyDifficultyDefinition definition)
        {
            if (!hasSelection)
            {
                definition = null;
                return false;
            }

            return TryGetDefinition(selectedDifficulty, out definition);
        }

        private bool TryGetDefinition(
            LobbyDifficultyId difficultyId,
            out LobbyDifficultyDefinition definition)
        {
            if (gameData != null &&
                gameData.LobbyDifficulties != null &&
                gameData.LobbyDifficulties.TryGet(
                    difficultyId,
                    out definition))
            {
                return true;
            }

            definition = null;
            return false;
        }

        private void LocalizeStaticLabels()
        {
            SetText(
                traitsLabel,
                GameStringIds.UiLobbyTraitsButton,
                "특성");
            SetText(
                collectionLabel,
                GameStringIds.UiLobbyCollectionButton,
                "도감");
            SetText(
                settingsLabel,
                GameStringIds.UiSettingsButton,
                "설정");
            SetText(
                difficultyTitleLabel,
                GameStringIds.UiDifficultyTitle,
                "난이도 선택");
            SetText(
                enterButtonLabel,
                GameStringIds.UiLobbyEnterButton,
                "입장하기");
        }

        private void SetText(
            TMP_Text label,
            string stringId,
            string fallback)
        {
            if (label != null)
            {
                label.text = Text(stringId, fallback);
            }
        }

        private string Text(string stringId, string fallback)
        {
            return gameData != null && gameData.GameStrings != null
                ? gameData.GameStrings.Get(stringId, fallback)
                : fallback;
        }

        private string Format(
            string stringId,
            string fallback,
            params object[] arguments)
        {
            return gameData != null && gameData.GameStrings != null
                ? gameData.GameStrings.Format(
                    stringId,
                    fallback,
                    arguments)
                : string.Format(fallback, arguments);
        }
    }
}
