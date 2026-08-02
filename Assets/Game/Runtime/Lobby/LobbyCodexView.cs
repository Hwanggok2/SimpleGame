using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame
{
    public enum LobbyCodexTab
    {
        Enemy,
        Skill
    }

    public sealed class LobbyCodexView : MonoBehaviour
    {
        private const int EntriesPerPage = 9;
        private static readonly string[] SkillCardIds =
        {
            "MOVING_SLASH",
            "PIERCING_UP",
            "SEVER_TRAIL",
            "FLYING_SWORD_COUNT",
            "STATIC_CHARGE",
            "FILTH_THROW",
            "FUSION_FLYING_SWORD_STATIC",
            "FUSION_FLYING_SWORD_PIERCING",
            "FUSION_STATIC_FILTH"
        };
        private static readonly Color SelectedTabColor =
            new(0.27f, 0.84f, 0.52f, 1f);
        private static readonly Color NormalTabColor =
            new(0.89f, 0.91f, 0.84f, 1f);

        [Header("Data")]
        [SerializeField] private GameDataManifest gameData;
        [Header("Shell")]
        [SerializeField] private Button outsideButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button enemyTabButton;
        [SerializeField] private Button skillTabButton;
        [SerializeField] private TMP_Text enemyTabLabel;
        [SerializeField] private TMP_Text skillTabLabel;
        [Header("Content")]
        [SerializeField] private GameObject enemyContent;
        [SerializeField] private GameObject skillContent;
        [Header("Enemy Grid")]
        [SerializeField] private LobbyCodexEntryView[] enemyEntries;
        [SerializeField] private Button enemyPreviousButton;
        [SerializeField] private Button enemyNextButton;
        [SerializeField] private TMP_Text enemyPageLabel;
        [Header("Skill Grid")]
        [SerializeField] private LobbyCodexEntryView[] skillEntries;
        [SerializeField] private Button skillPreviousButton;
        [SerializeField] private Button skillNextButton;
        [SerializeField] private TMP_Text skillPageLabel;
        [Header("Overlays")]
        [SerializeField] private LobbyCodexDetailView detailView;

        private readonly List<CodexItem> enemyItems = new();
        private readonly List<CodexItem> skillItems = new();
        private int enemyPage;
        private int skillPage;
        private bool initialized;

        public bool IsOpen => gameObject.activeSelf;
        public LobbyCodexTab CurrentTab { get; private set; }
        public IReadOnlyList<LobbyCodexEntryView> EnemyEntries =>
            enemyEntries;
        public IReadOnlyList<LobbyCodexEntryView> SkillEntries =>
            skillEntries;

        public void Configure(
            GameDataManifest configuredGameData,
            Button configuredOutsideButton,
            Button configuredCloseButton,
            Button configuredEnemyTabButton,
            Button configuredSkillTabButton,
            TMP_Text configuredEnemyTabLabel,
            TMP_Text configuredSkillTabLabel,
            GameObject configuredEnemyContent,
            GameObject configuredSkillContent,
            LobbyCodexEntryView[] configuredEnemyEntries,
            Button configuredEnemyPreviousButton,
            Button configuredEnemyNextButton,
            TMP_Text configuredEnemyPageLabel,
            LobbyCodexEntryView[] configuredSkillEntries,
            Button configuredSkillPreviousButton,
            Button configuredSkillNextButton,
            TMP_Text configuredSkillPageLabel,
            LobbyCodexDetailView configuredDetailView)
        {
            gameData = configuredGameData;
            outsideButton = configuredOutsideButton;
            closeButton = configuredCloseButton;
            enemyTabButton = configuredEnemyTabButton;
            skillTabButton = configuredSkillTabButton;
            enemyTabLabel = configuredEnemyTabLabel;
            skillTabLabel = configuredSkillTabLabel;
            enemyContent = configuredEnemyContent;
            skillContent = configuredSkillContent;
            enemyEntries = configuredEnemyEntries;
            enemyPreviousButton = configuredEnemyPreviousButton;
            enemyNextButton = configuredEnemyNextButton;
            enemyPageLabel = configuredEnemyPageLabel;
            skillEntries = configuredSkillEntries;
            skillPreviousButton = configuredSkillPreviousButton;
            skillNextButton = configuredSkillNextButton;
            skillPageLabel = configuredSkillPageLabel;
            detailView = configuredDetailView;
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            Bind(outsideButton, Close);
            Bind(closeButton, Close);
            Bind(enemyTabButton, () => SelectTab(LobbyCodexTab.Enemy));
            Bind(skillTabButton, () => SelectTab(LobbyCodexTab.Skill));
            Bind(enemyPreviousButton, () => ChangeEnemyPage(-1));
            Bind(enemyNextButton, () => ChangeEnemyPage(1));
            Bind(skillPreviousButton, () => ChangeSkillPage(-1));
            Bind(skillNextButton, () => ChangeSkillPage(1));
            LocalizeShell();
            BuildEnemyItems();
            BuildSkillItems();
            detailView?.Initialize();
            RefreshEnemyPage();
            RefreshSkillPage();
            gameObject.SetActive(false);
        }

        public void Open(LobbyCodexTab tab = LobbyCodexTab.Enemy)
        {
            if (!initialized)
            {
                Initialize();
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            detailView?.Hide();
            SelectTab(tab);
        }

        public void Close()
        {
            detailView?.Hide();
            gameObject.SetActive(false);
        }

        public void SelectTab(LobbyCodexTab tab)
        {
            CurrentTab = tab;
            SetActive(enemyContent, tab == LobbyCodexTab.Enemy);
            SetActive(skillContent, tab == LobbyCodexTab.Skill);
            SetTabSelected(enemyTabButton, tab == LobbyCodexTab.Enemy);
            SetTabSelected(skillTabButton, tab == LobbyCodexTab.Skill);
            detailView?.Hide();
        }

        private void LocalizeShell()
        {
            SetText(
                enemyTabLabel,
                GameStringIds.UiCodexEnemyTab,
                "적");
            SetText(
                skillTabLabel,
                GameStringIds.UiCodexSkillTab,
                "스킬");
        }

        private void BuildEnemyItems()
        {
            enemyItems.Clear();
            IReadOnlyList<EnemyDefinition> definitions =
                gameData?.EnemyBalance?.Definitions;
            if (definitions == null)
            {
                return;
            }

            foreach (EnemyDefinition definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                Sprite sprite = null;
                if (gameData.EnemyAssets != null &&
                    gameData.EnemyAssets.TryGetPrefab(
                        definition.EnemyId,
                        out EnemyBase prefab))
                {
                    SpriteRenderer renderer = prefab
                        .GetComponentInChildren<SpriteRenderer>(true);
                    sprite = renderer != null ? renderer.sprite : null;
                }

                string name = Text(
                    GameStringIds.EnemyName(definition.EnemyId),
                    PrototypeEnemyDefinitions.GetDisplayName(
                        definition.EnemyId,
                        definition.Archetype));
                string description = Text(
                    GameStringIds.EnemyDescription(
                        definition.EnemyId),
                    name);
                enemyItems.Add(new CodexItem(
                    name,
                    description,
                    sprite,
                    true));
            }
        }

        private void BuildSkillItems()
        {
            skillItems.Clear();
            IReadOnlyList<LevelUpCardDefinition> definitions =
                gameData?.LevelUpCards?.Definitions;
            if (definitions == null)
            {
                return;
            }

            foreach (string cardId in SkillCardIds)
            {
                LevelUpCardDefinition definition = FindCard(
                    definitions,
                    cardId);
                if (definition == null)
                {
                    continue;
                }

                string name = cardId == "FLYING_SWORD_COUNT"
                    ? Text(
                        GameStringIds.CodexFlyingSwordName,
                        "이기어검")
                    : definition.ResolveDisplayName(gameData.GameStrings);
                string description = cardId == "FLYING_SWORD_COUNT"
                    ? Text(
                        GameStringIds.CodexFlyingSwordDescription,
                        definition.ResolveDescription(
                            gameData.GameStrings))
                    : definition.ResolveDescription(gameData.GameStrings);
                skillItems.Add(new CodexItem(
                    name,
                    description,
                    null,
                    false));
            }
        }

        private static LevelUpCardDefinition FindCard(
            IReadOnlyList<LevelUpCardDefinition> definitions,
            string cardId)
        {
            foreach (LevelUpCardDefinition definition in definitions)
            {
                if (definition != null &&
                    string.Equals(
                        definition.CardId,
                        cardId,
                        StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        private void ChangeEnemyPage(int delta)
        {
            enemyPage = ClampPage(enemyPage + delta, enemyItems.Count);
            RefreshEnemyPage();
        }

        private void ChangeSkillPage(int delta)
        {
            skillPage = ClampPage(skillPage + delta, skillItems.Count);
            RefreshSkillPage();
        }

        private void RefreshEnemyPage()
        {
            RefreshPage(
                enemyItems,
                enemyEntries,
                enemyPage,
                enemyPreviousButton,
                enemyNextButton,
                enemyPageLabel);
        }

        private void RefreshSkillPage()
        {
            RefreshPage(
                skillItems,
                skillEntries,
                skillPage,
                skillPreviousButton,
                skillNextButton,
                skillPageLabel);
        }

        private void RefreshPage(
            IReadOnlyList<CodexItem> items,
            LobbyCodexEntryView[] entries,
            int page,
            Button previous,
            Button next,
            TMP_Text pageLabel)
        {
            if (entries != null)
            {
                for (int slot = 0; slot < entries.Length; slot++)
                {
                    LobbyCodexEntryView entry = entries[slot];
                    if (entry == null)
                    {
                        continue;
                    }

                    int itemIndex = page * EntriesPerPage + slot;
                    if (itemIndex >= items.Count)
                    {
                        entry.SetEmpty();
                        continue;
                    }

                    CodexItem item = items[itemIndex];
                    entry.SetContent(
                        item.Name,
                        item.Sprite,
                        item.ShowImage,
                        () => detailView?.Show(
                            item.Name,
                            item.Description,
                            item.Sprite,
                            item.ShowImage));
                }
            }

            int pageCount = CalculatePageCount(items.Count);
            if (previous != null)
            {
                previous.interactable = page > 0;
            }

            if (next != null)
            {
                next.interactable = page + 1 < pageCount;
            }

            if (pageLabel != null)
            {
                pageLabel.text = Format(
                    GameStringIds.UiCodexPageFormat,
                    "{0} / {1}",
                    page + 1,
                    pageCount);
            }
        }

        private static int ClampPage(int page, int itemCount)
        {
            return Mathf.Clamp(page, 0, CalculatePageCount(itemCount) - 1);
        }

        private static int CalculatePageCount(int itemCount)
        {
            return Mathf.Max(
                1,
                Mathf.CeilToInt((float)itemCount / EntriesPerPage));
        }

        private static void Bind(
            Button button,
            UnityEngine.Events.UnityAction callback)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(callback);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        private static void SetTabSelected(Button button, bool selected)
        {
            Image image = button != null
                ? button.GetComponent<Image>()
                : null;
            if (image != null)
            {
                image.color = selected
                    ? SelectedTabColor
                    : NormalTabColor;
            }
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

        private sealed class CodexItem
        {
            public CodexItem(
                string name,
                string description,
                Sprite sprite,
                bool showImage)
            {
                Name = name;
                Description = description;
                Sprite = sprite;
                ShowImage = showImage;
            }

            public string Name { get; }
            public string Description { get; }
            public Sprite Sprite { get; }
            public bool ShowImage { get; }
        }
    }
}
