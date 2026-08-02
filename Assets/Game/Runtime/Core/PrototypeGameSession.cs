using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SimpleGame
{
    public sealed partial class PrototypeGameSession : MonoBehaviour
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
            if (LobbyDifficultySelectionStore.TryLoad(
                    out LobbyDifficultyId lobbyDifficulty) &&
                gameData.LobbyDifficulties.TryGet(
                    lobbyDifficulty,
                    out LobbyDifficultyDefinition definition) &&
                definition.TryGetRuntimeDifficulty(
                    out GameDifficulty runtimeDifficulty))
            {
                SelectDifficulty(runtimeDifficulty);
                return;
            }

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
    }
}
