using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    [System.Serializable]
    public sealed class LevelExperienceRow
    {
        [SerializeField, Min(1)] private int level = 1;
        [SerializeField, Min(0)] private int requiredExperienceToNext;

        public LevelExperienceRow(int level, int requiredExperienceToNext)
        {
            this.level = Mathf.Max(1, level);
            this.requiredExperienceToNext =
                Mathf.Max(0, requiredExperienceToNext);
        }

        public int Level => level;
        public int RequiredExperienceToNext => requiredExperienceToNext;
    }

    [CreateAssetMenu(
        fileName = "LevelExperienceTable",
        menuName = "SimpleGame/Data/Level Experience Table")]
    public sealed class LevelExperienceTable : ScriptableObject
    {
        [SerializeField] private List<LevelExperienceRow> rows = new();

        public IReadOnlyList<LevelExperienceRow> Rows => rows;

        public bool TryGetRequiredExperience(int level, out int required)
        {
            foreach (LevelExperienceRow row in rows)
            {
                if (row != null && row.Level == level)
                {
                    required = row.RequiredExperienceToNext;
                    return true;
                }
            }

            required = 0;
            return false;
        }

        public void Configure(IEnumerable<LevelExperienceRow> values)
        {
            rows = new List<LevelExperienceRow>(values);
        }
    }
}
