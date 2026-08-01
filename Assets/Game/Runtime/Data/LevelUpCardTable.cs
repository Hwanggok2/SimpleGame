using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    public readonly struct LevelUpCardChoiceData
    {
        public LevelUpCardChoiceData(
            LevelUpCardDefinition definition,
            int currentStack)
            : this(definition, currentStack, null)
        {
        }

        public LevelUpCardChoiceData(
            LevelUpCardDefinition definition,
            int currentStack,
            GameStringTable strings)
        {
            DisplayName = definition.ResolveDisplayName(strings);
            Description = definition.ResolveDescription(strings);
            Rarity = definition.Rarity;
            MaxLevel = definition.MaxStack;
            NextLevel = Mathf.Min(
                definition.MaxStack,
                Mathf.Max(0, currentStack) + 1);
            string fallback =
                $"{DisplayName}\n{Rarity} · 획득 후 레벨 " +
                $"{NextLevel}/{MaxLevel}";
            HeaderText = strings != null
                ? strings.Format(
                    GameStringIds.CardHeaderFormat,
                    fallback,
                    DisplayName,
                    Rarity,
                    NextLevel,
                    MaxLevel)
                : fallback;
        }

        public string DisplayName { get; }
        public string Description { get; }
        public string Rarity { get; }
        public int NextLevel { get; }
        public int MaxLevel { get; }
        public string HeaderText { get; }
    }

    [Serializable]
    public sealed class LevelUpCardDefinition
    {
        [SerializeField] private string cardId;
        [SerializeField] private string nameKey;
        [SerializeField] private string descriptionKey;
        [SerializeField] private string displayName;
        [SerializeField] private string description;
        [SerializeField] private LevelUpCardEffectType effectType;
        [SerializeField] private PlayerStatId targetStat;
        [SerializeField] private StatOperation operation;
        [SerializeField] private float value;
        [SerializeField] private int maxStack;
        [SerializeField] private int selectionWeight;
        [SerializeField] private int minPlayerLevel;
        [SerializeField] private string requiredCardId;
        [SerializeField] private string rarity;
        [SerializeField] private string iconId;
        [SerializeField] private bool enabled;
        [SerializeField] private string fusionIngredientCardIds;

        public LevelUpCardDefinition(
            string cardId,
            string nameKey,
            string displayName,
            string description,
            LevelUpCardEffectType effectType,
            PlayerStatId targetStat,
            StatOperation operation,
            float value,
            int maxStack,
            int selectionWeight,
            int minPlayerLevel,
            string requiredCardId,
            string rarity,
            string iconId,
            bool enabled,
            string fusionIngredientCardIds = "")
        {
            this.cardId = cardId;
            this.nameKey = nameKey;
            descriptionKey = string.Empty;
            this.displayName = displayName;
            this.description = description;
            this.effectType = effectType;
            this.targetStat = targetStat;
            this.operation = operation;
            this.value = value;
            this.maxStack = maxStack;
            this.selectionWeight = selectionWeight;
            this.minPlayerLevel = minPlayerLevel;
            this.requiredCardId = requiredCardId;
            this.rarity = rarity;
            this.iconId = iconId;
            this.enabled = enabled;
            this.fusionIngredientCardIds =
                fusionIngredientCardIds ?? string.Empty;
        }

        public LevelUpCardDefinition(
            string cardId,
            string nameKey,
            string descriptionKey,
            LevelUpCardEffectType effectType,
            PlayerStatId targetStat,
            StatOperation operation,
            float value,
            int maxStack,
            int selectionWeight,
            int minPlayerLevel,
            string requiredCardId,
            string rarity,
            string iconId,
            bool enabled,
            string fusionIngredientCardIds = "")
            : this(
                cardId,
                nameKey,
                string.Empty,
                string.Empty,
                effectType,
                targetStat,
                operation,
                value,
                maxStack,
                selectionWeight,
                minPlayerLevel,
                requiredCardId,
                rarity,
                iconId,
                enabled,
                fusionIngredientCardIds)
        {
            this.descriptionKey = descriptionKey;
        }

        public string CardId => cardId;
        public string NameKey => nameKey;
        public string DescriptionKey => descriptionKey;
        public string Description => description;
        public LevelUpCardEffectType EffectType => effectType;
        public PlayerStatId TargetStat => targetStat;
        public StatOperation Operation => operation;
        public float Value => value;
        public int MaxStack => maxStack;
        public int SelectionWeight => selectionWeight;
        public int MinPlayerLevel => minPlayerLevel;
        public string RequiredCardId => requiredCardId;
        public string Rarity => rarity;
        public string IconId => iconId;
        public bool Enabled => enabled;
        public string FusionIngredientCardIdsRaw =>
            fusionIngredientCardIds ?? string.Empty;
        public IReadOnlyList<string> FusionIngredientCardIds
        {
            get
            {
                if (string.IsNullOrWhiteSpace(
                        fusionIngredientCardIds))
                {
                    return Array.Empty<string>();
                }

                string[] rawIds = fusionIngredientCardIds.Split('|');
                var parsedIds = new List<string>(rawIds.Length);
                foreach (string rawId in rawIds)
                {
                    string parsedId = rawId.Trim();
                    if (!string.IsNullOrWhiteSpace(parsedId))
                    {
                        parsedIds.Add(parsedId);
                    }
                }

                return parsedIds;
            }
        }
        public string DisplayName =>
            string.IsNullOrWhiteSpace(displayName)
                ? nameKey
                : displayName;

        public string ResolveDisplayName(GameStringTable strings)
        {
            return strings != null
                ? strings.Get(NameKey, DisplayName)
                : DisplayName;
        }

        public string ResolveDescription(GameStringTable strings)
        {
            string fallback = string.IsNullOrWhiteSpace(description)
                ? descriptionKey
                : description;
            return strings != null
                ? strings.Get(descriptionKey, fallback)
                : fallback;
        }

        public string GetDisplayText(
            int currentStack,
            GameStringTable strings = null)
        {
            int nextStack = Mathf.Min(maxStack, currentStack + 1);
            string resolvedName = ResolveDisplayName(strings);
            string body = ResolveDescription(strings);
            string headerFallback =
                $"{resolvedName}\n{rarity} · 획득 후 레벨 " +
                $"{nextStack}/{maxStack}";
            string header = strings != null
                ? strings.Format(
                    GameStringIds.CardHeaderFormat,
                    headerFallback,
                    resolvedName,
                    rarity,
                    nextStack,
                    maxStack)
                : headerFallback;
            return $"{header}\n{body}";
        }
    }

    [CreateAssetMenu(
        fileName = "LevelUpCardTable",
        menuName = "SimpleGame/Data/Level Up Card Table")]
    public sealed class LevelUpCardTable : ScriptableObject
    {
        [SerializeField]
        private List<LevelUpCardDefinition> definitions = new();

        public IReadOnlyList<LevelUpCardDefinition> Definitions =>
            definitions;

        public List<LevelUpCardDefinition> Draw(
            int unlockLevel,
            Func<string, int> getStackCount,
            int count,
            ISet<string> excludedCardIds = null)
        {
            var candidates = new List<LevelUpCardDefinition>();
            foreach (LevelUpCardDefinition definition in definitions)
            {
                if (IsEligible(
                        definition,
                        unlockLevel,
                        getStackCount,
                        excludedCardIds))
                {
                    candidates.Add(definition);
                }
            }

            var result = new List<LevelUpCardDefinition>(
                Mathf.Min(count, candidates.Count));
            while (result.Count < count && candidates.Count > 0)
            {
                int totalWeight = 0;
                foreach (LevelUpCardDefinition candidate in candidates)
                {
                    totalWeight += Mathf.Max(0, candidate.SelectionWeight);
                }

                int selectedIndex = totalWeight > 0
                    ? FindWeightedIndex(
                        candidates,
                        UnityEngine.Random.Range(0, totalWeight))
                    : UnityEngine.Random.Range(0, candidates.Count);
                result.Add(candidates[selectedIndex]);
                candidates.RemoveAt(selectedIndex);
            }

            return result;
        }

        public bool HasEligibleCard(
            int unlockLevel,
            Func<string, int> getStackCount,
            ISet<string> excludedCardIds = null)
        {
            foreach (LevelUpCardDefinition definition in definitions)
            {
                if (IsEligible(
                        definition,
                        unlockLevel,
                        getStackCount,
                        excludedCardIds))
                {
                    return true;
                }
            }

            return false;
        }

        public void Configure(IEnumerable<LevelUpCardDefinition> values)
        {
            definitions = new List<LevelUpCardDefinition>(values);
        }

        private bool IsEligible(
            LevelUpCardDefinition definition,
            int unlockLevel,
            Func<string, int> getStackCount,
            ISet<string> excludedCardIds)
        {
            return definition != null &&
                definition.Enabled &&
                definition.MinPlayerLevel <= unlockLevel &&
                (string.IsNullOrWhiteSpace(
                     definition.RequiredCardId) ||
                 getStackCount(definition.RequiredCardId) > 0) &&
                HasMasteredFusionIngredients(
                    definition,
                    getStackCount) &&
                getStackCount(definition.CardId) <
                    definition.MaxStack &&
                (excludedCardIds == null ||
                 !excludedCardIds.Contains(definition.CardId));
        }

        private bool HasMasteredFusionIngredients(
            LevelUpCardDefinition definition,
            Func<string, int> getStackCount)
        {
            if (definition.EffectType != LevelUpCardEffectType.Fusion)
            {
                return true;
            }

            IReadOnlyList<string> ingredientIds =
                definition.FusionIngredientCardIds;
            if (ingredientIds.Count == 0)
            {
                return false;
            }

            foreach (string ingredientId in ingredientIds)
            {
                LevelUpCardDefinition ingredient = definitions.Find(
                    value => value != null &&
                        string.Equals(
                            value.CardId,
                            ingredientId,
                            StringComparison.Ordinal));
                if (ingredient == null ||
                    getStackCount(ingredientId) < ingredient.MaxStack)
                {
                    return false;
                }
            }

            return true;
        }

        private static int FindWeightedIndex(
            IReadOnlyList<LevelUpCardDefinition> candidates,
            int roll)
        {
            for (int index = 0; index < candidates.Count; index++)
            {
                roll -= Mathf.Max(0, candidates[index].SelectionWeight);
                if (roll < 0)
                {
                    return index;
                }
            }

            return candidates.Count - 1;
        }
    }
}
