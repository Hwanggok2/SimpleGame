using System;
using UnityEngine;

namespace SimpleGame
{
    public sealed class PlayerProgression : MonoBehaviour
    {
        [SerializeField] private int level = 1;
        [SerializeField] private int experience;
        [SerializeField] private LevelExperienceTable experienceTable;

        public event Action LevelUpCardRequested;

        public int Level => level;
        public int Experience => experience;

        public void Configure(LevelExperienceTable configuredExperienceTable)
        {
            experienceTable = configuredExperienceTable;
        }

        public bool TryGetRequiredExperience(out int required)
        {
            if (experienceTable != null &&
                experienceTable.TryGetRequiredExperience(level, out required))
            {
                return true;
            }

            required = 0;
            return false;
        }

        public void AddExperience(int amount)
        {
            experience += Mathf.Max(0, amount);
            while (true)
            {
                if (!TryGetRequiredExperience(out int required))
                {
                    Debug.LogError(
                        $"Player EXP row not found for level {level}.",
                        this);
                    return;
                }

                if (required <= 0 || experience < required)
                {
                    return;
                }

                experience -= required;
                level++;
                LevelUpCardRequested?.Invoke();
            }
        }

    }
}
