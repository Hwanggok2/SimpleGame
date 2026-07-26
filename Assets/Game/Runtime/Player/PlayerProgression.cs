using System;
using UnityEngine;

namespace SimpleGame
{
    public sealed class PlayerProgression : MonoBehaviour
    {
        [SerializeField] private int level = 1;
        [SerializeField] private int experience;

        public event Action<int> LevelChanged;
        public event Action LevelUpCardRequested;

        public int Level => level;
        public int Experience => experience;

        public void AddExperience(int amount)
        {
            experience += Mathf.Max(0, amount);
            int required = RequiredExperience(level);
            while (experience >= required)
            {
                experience -= required;
                level++;
                LevelChanged?.Invoke(level);
                LevelUpCardRequested?.Invoke();
                required = RequiredExperience(level);
            }
        }

        private static int RequiredExperience(int currentLevel)
        {
            // Temporary prototype balance until the player progression table is fixed.
            return 3 + currentLevel * 2;
        }
    }
}
