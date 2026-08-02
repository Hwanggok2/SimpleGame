using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    public enum LobbyDifficultyId
    {
        Easy,
        Normal,
        Hard
    }

    [Serializable]
    public sealed class LobbyDifficultyDefinition
    {
        [SerializeField] private LobbyDifficultyId id;
        [SerializeField, Min(1)] private int sortOrder;
        [SerializeField] private bool isAvailable;
        [SerializeField] private bool hasRuntimeDifficulty;
        [SerializeField] private GameDifficulty runtimeDifficulty;
        [SerializeField, Min(1)] private int durationMinutes;
        [SerializeField] private string stageId;
        [SerializeField, Min(0)] private int enemyCountReductionPercent;
        [SerializeField, Min(0)] private int enemyLevelReductionPercent;
        [SerializeField] private string imageId;
        [SerializeField] private string selectedDifficultyImageId;
        [SerializeField, Min(0.1f)]
        private float selectedDifficultyImageScale = 1f;
        [SerializeField] private string nameKey;
        [SerializeField] private string buttonDescriptionKey;
        [SerializeField] private string objectiveKey;
        [SerializeField] private string effectDescriptionKey;

        public LobbyDifficultyDefinition(
            LobbyDifficultyId id,
            int sortOrder,
            bool isAvailable,
            bool hasRuntimeDifficulty,
            GameDifficulty runtimeDifficulty,
            int durationMinutes,
            string stageId,
            int enemyCountReductionPercent,
            int enemyLevelReductionPercent,
            string imageId,
            string selectedDifficultyImageId,
            float selectedDifficultyImageScale,
            string nameKey,
            string buttonDescriptionKey,
            string objectiveKey,
            string effectDescriptionKey)
        {
            this.id = id;
            this.sortOrder = sortOrder;
            this.isAvailable = isAvailable;
            this.hasRuntimeDifficulty = hasRuntimeDifficulty;
            this.runtimeDifficulty = runtimeDifficulty;
            this.durationMinutes = durationMinutes;
            this.stageId = stageId ?? string.Empty;
            this.enemyCountReductionPercent = enemyCountReductionPercent;
            this.enemyLevelReductionPercent = enemyLevelReductionPercent;
            this.imageId = imageId ?? string.Empty;
            this.selectedDifficultyImageId =
                selectedDifficultyImageId ?? string.Empty;
            this.selectedDifficultyImageScale =
                selectedDifficultyImageScale;
            this.nameKey = nameKey ?? string.Empty;
            this.buttonDescriptionKey = buttonDescriptionKey ?? string.Empty;
            this.objectiveKey = objectiveKey ?? string.Empty;
            this.effectDescriptionKey =
                effectDescriptionKey ?? string.Empty;
        }

        public LobbyDifficultyId Id => id;
        public int SortOrder => sortOrder;
        public bool IsAvailable => isAvailable;
        public int DurationMinutes => durationMinutes;
        public string StageId => stageId;
        public int EnemyCountReductionPercent =>
            enemyCountReductionPercent;
        public int EnemyLevelReductionPercent =>
            enemyLevelReductionPercent;
        public string ImageId => imageId;
        public string SelectedDifficultyImageId =>
            selectedDifficultyImageId;
        public float SelectedDifficultyImageScale =>
            selectedDifficultyImageScale;
        public string NameKey => nameKey;
        public string ButtonDescriptionKey => buttonDescriptionKey;
        public string ObjectiveKey => objectiveKey;
        public string EffectDescriptionKey => effectDescriptionKey;

        public bool TryGetRuntimeDifficulty(out GameDifficulty difficulty)
        {
            difficulty = runtimeDifficulty;
            return isAvailable && hasRuntimeDifficulty;
        }
    }

    [CreateAssetMenu(
        fileName = "LobbyDifficultyTable",
        menuName = "SimpleGame/Data/Lobby Difficulty Table")]
    public sealed class LobbyDifficultyTable : ScriptableObject
    {
        [SerializeField]
        private List<LobbyDifficultyDefinition> definitions = new();

        public IReadOnlyList<LobbyDifficultyDefinition> Definitions =>
            definitions;

        public void Configure(IEnumerable<LobbyDifficultyDefinition> values)
        {
            definitions = values != null
                ? new List<LobbyDifficultyDefinition>(values)
                : new List<LobbyDifficultyDefinition>();
        }

        public bool TryGet(
            LobbyDifficultyId id,
            out LobbyDifficultyDefinition definition)
        {
            foreach (LobbyDifficultyDefinition value in definitions)
            {
                if (value.Id == id)
                {
                    definition = value;
                    return true;
                }
            }

            definition = null;
            return false;
        }
    }
}
