using UnityEngine;

namespace SimpleGame
{
    [CreateAssetMenu(
        fileName = "GameDataManifest",
        menuName = "SimpleGame/Data/Game Data Manifest")]
    public sealed class GameDataManifest : ScriptableObject
    {
        [Header("Generated from Excel")]
        [SerializeField] private EnemyBalanceTable enemyBalance;
        [SerializeField] private StageSpawnSchedule stageSpawnSchedule;
        [SerializeField] private LevelExperienceTable playerLevelExperience;
        [SerializeField] private LevelExperienceTable accountLevelExperience;
        [SerializeField] private GlobalBalance globalBalance;
        [SerializeField] private PlayerBalanceTable playerBalance;
        [SerializeField] private LevelUpCardTable levelUpCards;
        [SerializeField] private GameStringTable gameStrings;
        [SerializeField] private ImageDataTable imageData;
        [SerializeField] private LobbyDifficultyTable lobbyDifficulties;

        [Header("Unity Asset Catalogs")]
        [SerializeField] private EnemyAssetCatalog enemyAssets;
        [SerializeField] private CombatFeedbackProfile combatFeedback;

        public EnemyBalanceTable EnemyBalance => enemyBalance;
        public StageSpawnSchedule StageSpawnSchedule => stageSpawnSchedule;
        public LevelExperienceTable PlayerLevelExperience =>
            playerLevelExperience;
        public LevelExperienceTable AccountLevelExperience =>
            accountLevelExperience;
        public GlobalBalance GlobalBalance => globalBalance;
        public PlayerBalanceTable PlayerBalance => playerBalance;
        public LevelUpCardTable LevelUpCards => levelUpCards;
        public GameStringTable GameStrings => gameStrings;
        public ImageDataTable ImageData => imageData;
        public LobbyDifficultyTable LobbyDifficulties => lobbyDifficulties;
        public EnemyAssetCatalog EnemyAssets => enemyAssets;
        public CombatFeedbackProfile CombatFeedback => combatFeedback;

        public bool IsConfigured =>
            enemyBalance != null &&
            stageSpawnSchedule != null &&
            playerLevelExperience != null &&
            accountLevelExperience != null &&
            globalBalance != null &&
            playerBalance != null &&
            levelUpCards != null &&
            gameStrings != null &&
            imageData != null &&
            lobbyDifficulties != null &&
            enemyAssets != null &&
            combatFeedback != null;

        public void Configure(
            EnemyBalanceTable configuredEnemyBalance,
            StageSpawnSchedule configuredStageSpawnSchedule,
            LevelExperienceTable configuredPlayerLevelExperience,
            LevelExperienceTable configuredAccountLevelExperience,
            GlobalBalance configuredGlobalBalance,
            PlayerBalanceTable configuredPlayerBalance,
            LevelUpCardTable configuredLevelUpCards,
            EnemyAssetCatalog configuredEnemyAssets,
            CombatFeedbackProfile configuredCombatFeedback,
            GameStringTable configuredGameStrings = null,
            ImageDataTable configuredImageData = null,
            LobbyDifficultyTable configuredLobbyDifficulties = null)
        {
            enemyBalance = configuredEnemyBalance;
            stageSpawnSchedule = configuredStageSpawnSchedule;
            playerLevelExperience = configuredPlayerLevelExperience;
            accountLevelExperience = configuredAccountLevelExperience;
            globalBalance = configuredGlobalBalance;
            playerBalance = configuredPlayerBalance;
            levelUpCards = configuredLevelUpCards;
            gameStrings = configuredGameStrings;
            imageData = configuredImageData;
            lobbyDifficulties = configuredLobbyDifficulties;
            enemyAssets = configuredEnemyAssets;
            combatFeedback = configuredCombatFeedback;
        }
    }
}
