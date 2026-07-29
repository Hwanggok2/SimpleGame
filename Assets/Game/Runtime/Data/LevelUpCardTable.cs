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
        {
            DisplayName = definition.DisplayName;
            Description = definition.Description;
            Rarity = definition.Rarity;
            MaxLevel = definition.MaxStack;
            NextLevel = Mathf.Min(
                definition.MaxStack,
                Mathf.Max(0, currentStack) + 1);
        }

        public string DisplayName { get; }
        public string Description { get; }
        public string Rarity { get; }
        public int NextLevel { get; }
        public int MaxLevel { get; }
        public string HeaderText =>
            $"{DisplayName}\n{Rarity} · 획득 후 레벨 " +
            $"{NextLevel}/{MaxLevel}";
    }

    [Serializable]
    public sealed class LevelUpCardDefinition
    {
        [SerializeField] private string cardId;
        [SerializeField] private string nameKey;
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
            bool enabled)
        {
            this.cardId = cardId;
            this.nameKey = nameKey;
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
        }

        public string CardId => cardId;
        public string NameKey => nameKey;
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
        public string DisplayName =>
            string.IsNullOrWhiteSpace(displayName)
                ? nameKey
                : displayName;

        public string GetDisplayText(int currentStack)
        {
            int nextStack = Mathf.Min(maxStack, currentStack + 1);
            string body = string.IsNullOrWhiteSpace(description)
                ? nameKey
                : description;
            return $"{DisplayName}\n{body}\n" +
                $"{rarity} · 획득 후 레벨 {nextStack}/{maxStack}";
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

        private static bool IsEligible(
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
                getStackCount(definition.CardId) <
                    definition.MaxStack &&
                (excludedCardIds == null ||
                 !excludedCardIds.Contains(definition.CardId));
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
