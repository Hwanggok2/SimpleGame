using UnityEngine;

namespace SimpleGame
{
    [CreateAssetMenu(
        fileName = "GlobalBalance",
        menuName = "SimpleGame/Data/Global Balance")]
    public sealed class GlobalBalance : ScriptableObject
    {
        [SerializeField, Min(1)] private int accountExperienceScoreUnit = 5;
        [SerializeField, Min(0)] private int accountExperiencePerUnit = 1;
        [SerializeField, Range(0f, 1f)] private float criticalChancePerCard = 0.1f;
        [SerializeField, Range(0f, 1f)] private float maximumCriticalChance = 0.7f;

        public float CriticalChancePerCard => criticalChancePerCard;
        public float MaximumCriticalChance => maximumCriticalChance;

        public int CalculateAccountExperience(int score)
        {
            return Mathf.Max(0, score) /
                Mathf.Max(1, accountExperienceScoreUnit) *
                Mathf.Max(0, accountExperiencePerUnit);
        }

        public void Configure(
            int scoreUnit,
            int experiencePerUnit,
            float criticalIncrease,
            float criticalMaximum)
        {
            accountExperienceScoreUnit = Mathf.Max(1, scoreUnit);
            accountExperiencePerUnit = Mathf.Max(0, experiencePerUnit);
            criticalChancePerCard = Mathf.Clamp01(criticalIncrease);
            maximumCriticalChance = Mathf.Clamp01(criticalMaximum);
        }
    }
}
