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
        public EnemyAssetCatalog EnemyAssets => enemyAssets;
        public CombatFeedbackProfile CombatFeedback => combatFeedback;

        public bool IsConfigured =>
            enemyBalance != null &&
            stageSpawnSchedule != null &&
            playerLevelExperience != null &&
            accountLevelExperience != null &&
            globalBalance != null &&
            enemyAssets != null &&
            combatFeedback != null;

        public void Configure(
            EnemyBalanceTable configuredEnemyBalance,
            StageSpawnSchedule configuredStageSpawnSchedule,
            LevelExperienceTable configuredPlayerLevelExperience,
            LevelExperienceTable configuredAccountLevelExperience,
            GlobalBalance configuredGlobalBalance,
            EnemyAssetCatalog configuredEnemyAssets,
            CombatFeedbackProfile configuredCombatFeedback)
        {
            enemyBalance = configuredEnemyBalance;
            stageSpawnSchedule = configuredStageSpawnSchedule;
            playerLevelExperience = configuredPlayerLevelExperience;
            accountLevelExperience = configuredAccountLevelExperience;
            globalBalance = configuredGlobalBalance;
            enemyAssets = configuredEnemyAssets;
            combatFeedback = configuredCombatFeedback;
        }
    }
}
