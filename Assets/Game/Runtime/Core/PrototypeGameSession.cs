using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SimpleGame
{
    public sealed class PrototypeGameSession : MonoBehaviour
    {
        public const float CardChoiceInputDelay = 0.7f;
        public const int DefaultInitialCardRerolls = 5;
        public const int DefaultMaximumStoredCardRerolls = 9;
        public const int DefaultBossRerollReward = 1;
        public const int LevelUpHealAmount = 2;

        [Header("Scene References")]
        [SerializeField] private PlayerRoot player;
        [SerializeField] private PrototypeEnemyFactory enemyFactory;
        [SerializeField] private EnemyWorldService enemyWorld;
        [SerializeField] private Transform enemyRoot;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private CombatFeedbackController combatFeedback;
        [SerializeField] private EnemyWorldRecycler enemyRecycler;
        [SerializeField] private PrototypeHUDPresenter hudPresenter;
        [SerializeField] private PoisonCloudSpawner poisonCloudSpawner;
        [Header("Game Data")]
        [SerializeField] private string stageId = "Stage01";
        [SerializeField] private GameDataManifest gameData;
        [SerializeField] private StageSpawnController stageSpawner;
        [Header("Prototype Account")]
        [SerializeField, Min(1)] private int accountLevel = 1;

        private readonly Dictionary<string, int> cardStacks = new(
            StringComparer.Ordinal);
        private readonly List<LevelUpCardDefinition> currentCardChoices =
            new();
        private readonly HashSet<string> currentCardHistory = new(
            StringComparer.Ordinal);
        private GameRunState state = GameRunState.DifficultySelection;
        private int pendingCardSelections;
        private int pendingBossRewardSelections;
        private bool selectingStartingCards;
        private int remainingCardRerolls =
            DefaultInitialCardRerolls;
        private int continueCount;
        private GameRunState stateBeforePause = GameRunState.Playing;
        private bool cardChoicesInteractable;
        private float cardChoiceUnlockAt;

        public event Action<string> HintChanged;
        public event Action<bool> CardSelectionVisibilityChanged;
        public event Action<IReadOnlyList<LevelUpCardChoiceData>>
            CardChoicesChanged;
        public event Action<bool> CardChoiceInteractivityChanged;
        public event Action<int, bool> CardRerollStateChanged;
        public event Action<bool> DifficultySelectionVisibilityChanged;
        public event Action<bool> PauseVisibilityChanged;
        public event Action<PauseDetailsData> PauseDetailsChanged;
        public event Action<bool> GameOverVisibilityChanged;

        public PlayerRoot Player => player;
        public int Score { get; private set; }
        public int AccountExperience =>
            gameData != null && gameData.GlobalBalance != null
                ? gameData.GlobalBalance.CalculateAccountExperience(Score)
                : 0;
        public float ElapsedTime { get; private set; }
        public bool IsPlaying => state == GameRunState.Playing;
        public int RemainingCardRerolls => remainingCardRerolls;
        public GameDifficulty Difficulty { get; private set; } =
            GameDifficulty.Normal;
        public GameStringTable GameStrings =>
            gameData != null ? gameData.GameStrings : null;
        public string StageDisplayName => GetString(
            GameStringIds.StageName(stageId),
            stageId);
        public string StageDescription => GetString(
            GameStringIds.StageDescription(stageId),
            string.Empty);

        public static string FormatElapsedTime(float elapsedTime)
        {
            int totalSeconds = Mathf.Max(
                0,
                Mathf.FloorToInt(elapsedTime));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        public string GetString(string stringId, string fallback = null)
        {
            return GameStrings != null
                ? GameStrings.Get(stringId, fallback)
                : fallback ?? $"[{stringId}]";
        }

        public string FormatString(
            string stringId,
            string fallbackTemplate,
            params object[] arguments)
        {
            if (GameStrings != null)
            {
                return GameStrings.Format(
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

        public void ConfigureScene(
            PlayerRoot configuredPlayer,
            PrototypeEnemyFactory configuredFactory,
            Transform configuredEnemyRoot,
            Camera configuredCamera,
            CombatFeedbackController configuredCombatFeedback,
            EnemyWorldRecycler configuredEnemyRecycler,
            PrototypeHUDPresenter configuredPresenter,
            EnemyWorldService configuredEnemyWorld)
        {
            player = configuredPlayer;
            enemyFactory = configuredFactory;
            enemyWorld = configuredEnemyWorld;
            enemyRoot = configuredEnemyRoot;
            worldCamera = configuredCamera;
            combatFeedback = configuredCombatFeedback;
            enemyRecycler = configuredEnemyRecycler;
            hudPresenter = configuredPresenter;
        }

        public void ConfigureData(
            GameDataManifest configuredGameData,
            StageSpawnController configuredStageSpawner)
        {
            gameData = configuredGameData;
            stageSpawner = configuredStageSpawner;
        }

        public void ConfigureHud(
            PrototypeHUDPresenter configuredPresenter)
        {
            hudPresenter = configuredPresenter;
        }

        public void ConfigureWorldRewards(
            PoisonCloudSpawner configuredPoisonCloudSpawner)
        {
            poisonCloudSpawner = configuredPoisonCloudSpawner;
        }

        private void Start()
        {
            Time.timeScale = 0f;
            remainingCardRerolls = GetInitialCardRerolls();
            if (gameData == null ||
                !gameData.IsConfigured ||
                stageSpawner == null ||
                enemyRecycler == null ||
                enemyWorld == null)
            {
                Debug.LogError(
                    "PrototypeGameSession requires GameDataManifest " +
                    "StageSpawnController, EnemyWorldRecycler, and " +
                    "EnemyWorldService.",
                    this);
                enabled = false;
                return;
            }

            EnsureCombatFeedback();
            player.Configure(
                this,
                enemyWorld,
                worldCamera,
                gameData.PlayerLevelExperience,
                gameData.GlobalBalance,
                gameData.PlayerBalance,
                stageSpawner.SpawnPoints);
            enemyFactory.ConfigureAssets(
                gameData.EnemyAssets,
                gameData.EnemyBalance);
            enemyFactory.Configure(this, enemyWorld, enemyRoot);
            enemyRecycler.Configure(
                this,
                enemyWorld,
                player.GetComponent<PlayerWorldArea>());
            hudPresenter.Initialize(this);

            player.Health.Depleted += OnPlayerDepleted;
            player.Progression.LevelUpCardRequested += OnPlayerLevelUp;
            state = GameRunState.DifficultySelection;
            DifficultySelectionVisibilityChanged?.Invoke(true);
            ShowHint(GetString(
                GameStringIds.HintSelectDifficulty,
                "난이도를 선택하면 게임을 시작합니다."));
        }

        private void Update()
        {
            if (Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }

            if (state == GameRunState.CardSelection &&
                !cardChoicesInteractable &&
                Time.unscaledTime >= cardChoiceUnlockAt)
            {
                SetCardChoicesInteractable(true);
            }

            if (state != GameRunState.GameOver &&
                player != null &&
                !player.IsAlive)
            {
                OnPlayerDepleted();
            }

            if (IsPlaying)
            {
                ElapsedTime += Time.deltaTime;
                stageSpawner.Tick(ElapsedTime);
            }
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            if (player != null && player.Health != null)
            {
                player.Health.Depleted -= OnPlayerDepleted;
            }

            if (player != null && player.Progression != null)
            {
                player.Progression.LevelUpCardRequested -= OnPlayerLevelUp;
            }
        }

        public void OnEnemyDefeated(EnemyBase enemy)
        {
            bool isBoss = enemy.Archetype == EnemyArchetype.Boss;
            bool isMushroomBoss =
                PrototypeEnemyDefinitions.IsMushroomBoss(
                    enemy.Definition.EnemyId);
            Vector2 defeatedPosition = enemy.transform.position;
            enemyWorld.Unregister(enemy);
            Score += enemy.Definition.Score;
            player.Progression.AddExperience(enemy.Definition.KillExperience);
            if (isMushroomBoss)
            {
                poisonCloudSpawner?.Schedule(defeatedPosition);
            }

            if (isBoss)
            {
                remainingCardRerolls =
                    CalculateBossRewardRerolls(
                        remainingCardRerolls,
                        GetBossRerollReward(),
                        GetMaximumStoredCardRerolls());
                QueueCardSelections(1, false, true);
            }
        }

        public void ShowHint(string message)
        {
            HintChanged?.Invoke(message);
        }

        public void PlayCombatFeedback(
            bool damageApplied,
            bool targetDefeated,
            bool critical,
            PlayerAttackReaction playerReaction)
        {
            combatFeedback.PlayResolvedAttack(
                damageApplied,
                targetDefeated,
                critical,
                playerReaction);
        }

        public void TogglePause()
        {
            if (state == GameRunState.DifficultySelection)
            {
                return;
            }

            if (state == GameRunState.Paused)
            {
                state = stateBeforePause;
                Time.timeScale =
                    state == GameRunState.CardSelection ? 0f : 1f;
                PauseVisibilityChanged?.Invoke(false);
                ShowHint(state == GameRunState.CardSelection
                    ? GetString(
                        GameStringIds.HintResumeCardSelection,
                        "레벨 업: 카드를 선택하세요.")
                    : GetString(
                        GameStringIds.HintGameResumed,
                        "게임을 재개했습니다."));
                return;
            }

            stateBeforePause = state;
            state = GameRunState.Paused;
            Time.timeScale = 0f;
            PauseDetailsChanged?.Invoke(BuildPauseDetails());
            PauseVisibilityChanged?.Invoke(true);
            ShowHint(GetString(
                GameStringIds.HintGamePaused,
                "일시 정지했습니다. ESC를 누르면 재개합니다."));
        }

        public void SelectDifficulty(GameDifficulty difficulty)
        {
            if (state != GameRunState.DifficultySelection)
            {
                return;
            }

            Difficulty = difficulty;
            ElapsedTime = 0f;
            state = GameRunState.Playing;
            Time.timeScale = 1f;
            stageSpawner.Begin(stageId, difficulty);
            DifficultySelectionVisibilityChanged?.Invoke(false);
            ShowHint(FormatString(
                GameStringIds.HintGameStartedFormat,
                "{0} 난이도로 시작합니다. 왼쪽 조이스틱으로 " +
                "조준하고 공격 버튼을 누르세요.",
                ResolveDifficultyDisplayName(difficulty)));
            QueueCardSelections(
                CalculateStartingCardSelectionCount(accountLevel),
                true);
        }

        public static string GetDifficultyDisplayName(
            GameDifficulty difficulty)
        {
            return difficulty == GameDifficulty.Easy
                ? "쉬움"
                : "보통";
        }

        public string ResolveDifficultyDisplayName(
            GameDifficulty difficulty)
        {
            return GetString(
                GameStringIds.DifficultyName(difficulty),
                GetDifficultyDisplayName(difficulty));
        }

        public void SelectCard(int choiceIndex)
        {
            if (state != GameRunState.CardSelection ||
                !cardChoicesInteractable ||
                pendingCardSelections <= 0 ||
                choiceIndex < 0 ||
                choiceIndex >= currentCardChoices.Count)
            {
                return;
            }

            LevelUpCardDefinition selected =
                currentCardChoices[choiceIndex];
            if (!player.ApplyCard(selected))
            {
                return;
            }

            ConsumeFusionIngredients(selected);
            cardStacks[selected.CardId] = GetCardStack(selected.CardId) + 1;
            if (pendingBossRewardSelections > 0)
            {
                pendingBossRewardSelections--;
            }

            pendingCardSelections--;
            if (pendingCardSelections > 0)
            {
                ArmCardChoiceDelay();
                RefreshCardChoices();
                if (TryFinishEmptyCardSelection())
                {
                    return;
                }

                ShowCardSelectionHint();
                return;
            }

            bool completedStartingCards = selectingStartingCards;
            selectingStartingCards = false;
            currentCardChoices.Clear();
            currentCardHistory.Clear();
            SetCardChoicesInteractable(false);
            CardSelectionVisibilityChanged?.Invoke(false);
            state = GameRunState.Playing;
            Time.timeScale = 1f;
            ShowHint(completedStartingCards
                ? GetString(
                    GameStringIds.HintStartingCardSelected,
                    "시작 카드를 선택했습니다. 게임을 시작합니다.")
                : FormatString(
                    GameStringIds.HintCardAcquiredFormat,
                    "{0} 카드를 획득했습니다.",
                    selected.ResolveDisplayName(GameStrings)));
        }

        public void RerollCard(int choiceIndex)
        {
            if (state != GameRunState.CardSelection ||
                !cardChoicesInteractable ||
                pendingCardSelections <= 0 ||
                remainingCardRerolls <= 0 ||
                choiceIndex < 0 ||
                choiceIndex >= currentCardChoices.Count)
            {
                return;
            }

            HashSet<string> excludedCardIds =
                BuildRerollExcludedCardIds();
            currentCardHistory.UnionWith(excludedCardIds);
            List<LevelUpCardDefinition> replacements =
                gameData.LevelUpCards.Draw(
                    GetCardUnlockLevel(),
                    GetCardStack,
                    1,
                    excludedCardIds);
            if (replacements.Count == 0)
            {
                PublishCardRerollState();
                return;
            }

            currentCardChoices[choiceIndex] = replacements[0];
            currentCardHistory.Add(replacements[0].CardId);
            remainingCardRerolls--;
            PublishCardChoices();
            PublishCardRerollState();
        }

        public void SimulateRewardedContinue()
        {
            if (state != GameRunState.GameOver || continueCount >= 2)
            {
                ShowHint(GetString(
                    GameStringIds.HintContinueLimit,
                    "이어하기는 게임 종료 후 최대 두 번 사용할 수 있습니다."));
                return;
            }

            continueCount++;
            player.RestoreAfterContinue();
            enemyRecycler.PushAwayAllNormalEnemies();

            state = GameRunState.Playing;
            GameOverVisibilityChanged?.Invoke(false);
            ShowHint(FormatString(
                GameStringIds.HintContinueSuccessFormat,
                "보상형 광고를 확인한 것으로 처리했습니다. " +
                "이어하기 {0}/2, 체력을 모두 회복했습니다.",
                continueCount));
        }

        public void DebugDamagePlayer()
        {
            if (IsPlaying)
            {
                player.ReceiveDamage(10);
                ShowHint(GetString(
                    GameStringIds.HintDebugDamage,
                    "시험 기능: 플레이어가 피해 10을 받았습니다."));
            }
        }

        public void DebugGrantPlayerExperience()
        {
            if (!IsPlaying)
            {
                return;
            }

            player.Progression.AddExperience(5);
            ShowHint(GetString(
                GameStringIds.HintDebugExperience,
                "시험 기능: 플레이어 경험치가 5 증가했습니다."));
        }

        private void OnPlayerDepleted()
        {
            if (state == GameRunState.GameOver)
            {
                return;
            }

            state = GameRunState.GameOver;
            pendingCardSelections = 0;
            pendingBossRewardSelections = 0;
            selectingStartingCards = false;
            Time.timeScale = 1f;
            currentCardChoices.Clear();
            currentCardHistory.Clear();
            SetCardChoicesInteractable(false);
            PauseVisibilityChanged?.Invoke(false);
            CardSelectionVisibilityChanged?.Invoke(false);
            GameOverVisibilityChanged?.Invoke(true);
            ShowHint(GetString(
                GameStringIds.HintPlayerDefeated,
                "플레이어가 쓰러졌습니다. 이어하기로 다시 도전할 수 있습니다."));
        }

        private void OnPlayerLevelUp()
        {
            if (state == GameRunState.GameOver)
            {
                return;
            }

            player.Health.Heal(LevelUpHealAmount);
            foreach (EnemyBase enemy in enemyWorld.Enemies)
            {
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.RefreshLevelLabel();
                }
            }

            QueueCardSelections(1, false);
        }

        public static int CalculateStartingCardSelectionCount(
            int currentAccountLevel)
        {
            return Mathf.Max(0, currentAccountLevel - 1);
        }

        public static int CalculateBossRewardRerolls(
            int currentRerolls,
            int reward = DefaultBossRerollReward,
            int maximumStored = DefaultMaximumStoredCardRerolls)
        {
            return Mathf.Clamp(
                currentRerolls + Mathf.Max(0, reward),
                0,
                Mathf.Max(0, maximumStored));
        }

        private int GetInitialCardRerolls()
        {
            return gameData?.GlobalBalance != null
                ? gameData.GlobalBalance.InitialCardRerolls
                : DefaultInitialCardRerolls;
        }

        private int GetMaximumStoredCardRerolls()
        {
            return gameData?.GlobalBalance != null
                ? gameData.GlobalBalance.MaximumStoredCardRerolls
                : DefaultMaximumStoredCardRerolls;
        }

        private int GetBossRerollReward()
        {
            return gameData?.GlobalBalance != null
                ? gameData.GlobalBalance.BossRerollReward
                : DefaultBossRerollReward;
        }

        private void QueueCardSelections(
            int count,
            bool startingCards,
            bool bossReward = false)
        {
            if (count <= 0)
            {
                return;
            }

            pendingCardSelections += count;
            if (bossReward)
            {
                pendingBossRewardSelections += count;
            }

            selectingStartingCards |= startingCards;
            state = GameRunState.CardSelection;
            Time.timeScale = 0f;
            ArmCardChoiceDelay();
            RefreshCardChoices();
            if (TryFinishEmptyCardSelection())
            {
                return;
            }

            CardSelectionVisibilityChanged?.Invoke(true);
            ShowCardSelectionHint();
        }

        private void RefreshCardChoices()
        {
            currentCardChoices.Clear();
            currentCardHistory.Clear();
            currentCardChoices.AddRange(gameData.LevelUpCards.Draw(
                GetCardUnlockLevel(),
                GetCardStack,
                3));
            foreach (LevelUpCardDefinition choice in currentCardChoices)
            {
                currentCardHistory.Add(choice.CardId);
            }

            PublishCardChoices();
            PublishCardRerollState();
        }

        private void PublishCardChoices()
        {
            var choices = new List<LevelUpCardChoiceData>(
                currentCardChoices.Count);
            foreach (LevelUpCardDefinition choice in currentCardChoices)
            {
                choices.Add(new LevelUpCardChoiceData(
                    choice,
                    GetCardStack(choice.CardId),
                    GameStrings));
            }

            CardChoicesChanged?.Invoke(choices);
        }

        private void PublishCardRerollState()
        {
            bool hasAlternative =
                remainingCardRerolls > 0 &&
                currentCardChoices.Count > 0 &&
                gameData.LevelUpCards.HasEligibleCard(
                    GetCardUnlockLevel(),
                    GetCardStack,
                    BuildRerollExcludedCardIds());
            CardRerollStateChanged?.Invoke(
                remainingCardRerolls,
                hasAlternative);
        }

        private int GetCardUnlockLevel()
        {
            return selectingStartingCards
                ? Mathf.Max(player.Progression.Level, accountLevel)
                : player.Progression.Level;
        }

        private HashSet<string> BuildRerollExcludedCardIds()
        {
            var cardIds = new HashSet<string>(
                currentCardHistory,
                StringComparer.Ordinal);
            foreach (LevelUpCardDefinition choice
                     in currentCardChoices)
            {
                cardIds.Add(choice.CardId);
            }

            return cardIds;
        }

        private void ArmCardChoiceDelay()
        {
            cardChoiceUnlockAt =
                Time.unscaledTime + CardChoiceInputDelay;
            SetCardChoicesInteractable(false);
        }

        private void SetCardChoicesInteractable(bool interactable)
        {
            cardChoicesInteractable = interactable;
            CardChoiceInteractivityChanged?.Invoke(interactable);
        }

        private PauseDetailsData BuildPauseDetails()
        {
            string playerOverview = FormatString(
                GameStringIds.PausePlayerOverviewFormat,
                "플레이어 레벨: {0}\n난이도: {1}\n" +
                "점수: {2}\n생존 시간: {3}",
                player.Progression.Level,
                ResolveDifficultyDisplayName(Difficulty),
                Score,
                FormatElapsedTime(ElapsedTime));

            return new PauseDetailsData(
                playerOverview,
                BuildAccountOverview(),
                BuildPauseStats(),
                BuildPauseSkills());
        }

        private string BuildAccountOverview()
        {
            int earnedExperience = AccountExperience;
            if (gameData != null &&
                gameData.AccountLevelExperience != null &&
                gameData.AccountLevelExperience.TryGetRequiredExperience(
                    accountLevel,
                    out int requiredExperience) &&
                requiredExperience > 0)
            {
                return FormatString(
                    GameStringIds.PauseAccountOverviewFormat,
                    "계정 레벨: {0}\n이번 게임 획득 경험치: {1}\n" +
                    "다음 계정 레벨 진행도: {2}/{3}",
                    accountLevel,
                    earnedExperience,
                    Mathf.Min(earnedExperience, requiredExperience),
                    requiredExperience);
            }

            return FormatString(
                GameStringIds.PauseAccountMaxFormat,
                "계정 레벨: {0}\n이번 게임 획득 경험치: {1}\n" +
                "다음 계정 레벨 진행도: MAX",
                accountLevel,
                earnedExperience);
        }

        private string BuildPauseStats()
        {
            var text = new StringBuilder();
            text.AppendLine(FormatString(
                GameStringIds.PauseVitalsFormat,
                "체력  {0}/{1}    공격력  {2:0.##}",
                player.Health.CurrentHealth,
                player.Health.MaxHealth,
                player.AttackPower));
            text.AppendLine(FormatString(
                GameStringIds.PauseMobilityFormat,
                "치명타 확률  {0:0}%    이동 속도  {1:0.##}",
                player.Critical.Chance * 100f,
                player.MoveSpeed));
            text.Append(FormatString(
                GameStringIds.PauseCombatFormat,
                "공격 사거리  {0:0.##}    후면 피해  x{1:0.##}",
                player.AttackRange,
                player.RearAttackMultiplier));
            return text.ToString();
        }

        private string BuildPauseSkills()
        {
            var text = new StringBuilder();
            bool hasSelectedCard = false;
            for (int rarityOrder = 0; rarityOrder <= 4; rarityOrder++)
            {
                foreach (LevelUpCardDefinition definition
                    in gameData.LevelUpCards.Definitions)
                {
                    int stack = GetCardStack(definition.CardId);
                    if (stack <= 0 ||
                        GetRaritySortOrder(definition.Rarity) != rarityOrder)
                    {
                        continue;
                    }

                    hasSelectedCard = true;
                    text.Append("• ");
                    text.Append(definition.ResolveDisplayName(GameStrings));
                    text.AppendLine(FormatString(
                        GameStringIds.PauseSkillLevelFormat,
                        "  레벨 {0}/{1}",
                        stack,
                        definition.MaxStack));
                }
            }

            if (!hasSelectedCard)
            {
                text.Append(GetString(
                    GameStringIds.CommonNone,
                    "없음"));
            }

            return text.ToString().TrimEnd();
        }

        private static int GetRaritySortOrder(string rarity)
        {
            string value = rarity?.Trim();
            if (string.Equals(value, "일반", StringComparison.Ordinal) ||
                string.Equals(value, "Common", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            if (string.Equals(value, "희귀", StringComparison.Ordinal) ||
                string.Equals(value, "레어", StringComparison.Ordinal) ||
                string.Equals(value, "Rare", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (string.Equals(value, "에픽", StringComparison.Ordinal) ||
                string.Equals(value, "영웅", StringComparison.Ordinal) ||
                string.Equals(value, "Epic", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Hero", StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            if (string.Equals(value, "레전더리", StringComparison.Ordinal) ||
                string.Equals(value, "전설", StringComparison.Ordinal) ||
                string.Equals(value, "Legendary", StringComparison.OrdinalIgnoreCase))
            {
                return 3;
            }

            return 4;
        }

        private int GetCardStack(string cardId)
        {
            return cardStacks.TryGetValue(cardId, out int count)
                ? count
                : 0;
        }

        private void ConsumeFusionIngredients(
            LevelUpCardDefinition selected)
        {
            if (selected == null ||
                selected.EffectType != LevelUpCardEffectType.Fusion)
            {
                return;
            }

            foreach (string ingredientCardId
                     in selected.FusionIngredientCardIds)
            {
                cardStacks.Remove(ingredientCardId);
            }
        }

        private void ShowCardSelectionHint()
        {
            if (pendingBossRewardSelections > 0)
            {
                ShowHint(FormatString(
                    GameStringIds.HintCardSelectBossFormat,
                    "보스 처치 보상: 카드를 선택하세요. 남은 선택 {0}회",
                    pendingCardSelections));
                return;
            }

            if (selectingStartingCards)
            {
                ShowHint(FormatString(
                    GameStringIds.HintCardSelectAccountFormat,
                    "계정 레벨 {0} 시작 보너스: 카드를 선택하세요. " +
                    "남은 선택 {1}회",
                    accountLevel,
                    pendingCardSelections));
                return;
            }

            ShowHint(FormatString(
                GameStringIds.HintCardSelectLevelUpFormat,
                "레벨 업: 카드를 선택하세요. 남은 선택 {0}회",
                pendingCardSelections));
        }

        private bool TryFinishEmptyCardSelection()
        {
            if (currentCardChoices.Count > 0)
            {
                return false;
            }

            pendingCardSelections = 0;
            pendingBossRewardSelections = 0;
            selectingStartingCards = false;
            currentCardHistory.Clear();
            SetCardChoicesInteractable(false);
            CardSelectionVisibilityChanged?.Invoke(false);
            state = GameRunState.Playing;
            Time.timeScale = 1f;
            ShowHint(GetString(
                GameStringIds.HintAllSkillsMaxed,
                "모든 스킬이 최대 레벨에 도달했습니다."));
            return true;
        }

        private void EnsureCombatFeedback()
        {
            CameraShakeController cameraShake =
                worldCamera.GetComponent<CameraShakeController>();
            if (cameraShake == null)
            {
                Debug.LogError(
                    "Main Camera requires CameraShakeController.",
                    worldCamera);
                return;
            }

            if (combatFeedback == null)
            {
                combatFeedback =
                    GetComponentInChildren<CombatFeedbackController>(
                        true);
            }

            if (combatFeedback == null)
            {
                Debug.LogError(
                    "PrototypeGameSession requires CombatFeedbackController.",
                    this);
                return;
            }

            combatFeedback.Configure(cameraShake, gameData.CombatFeedback);
        }

    }
}
