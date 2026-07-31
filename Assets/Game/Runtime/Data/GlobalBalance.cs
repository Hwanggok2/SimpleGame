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
        [SerializeField, Range(0f, 1f)] private float criticalChancePerCard = 0.05f;
        [SerializeField, Range(0f, 1f)] private float maximumCriticalChance = 0.5f;
        [SerializeField, Min(0)] private int initialCardRerolls = 5;
        [SerializeField, Min(0)] private int maximumStoredCardRerolls = 9;
        [SerializeField, Min(0)] private int bossRerollReward = 1;

        public float CriticalChancePerCard => criticalChancePerCard;
        public float MaximumCriticalChance => maximumCriticalChance;
        public int InitialCardRerolls => initialCardRerolls;
        public int MaximumStoredCardRerolls => maximumStoredCardRerolls;
        public int BossRerollReward => bossRerollReward;

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
            float criticalMaximum,
            int configuredInitialCardRerolls,
            int configuredMaximumStoredCardRerolls,
            int configuredBossRerollReward)
        {
            accountExperienceScoreUnit = Mathf.Max(1, scoreUnit);
            accountExperiencePerUnit = Mathf.Max(0, experiencePerUnit);
            criticalChancePerCard = Mathf.Clamp01(criticalIncrease);
            maximumCriticalChance = Mathf.Clamp01(criticalMaximum);
            initialCardRerolls =
                Mathf.Max(0, configuredInitialCardRerolls);
            maximumStoredCardRerolls = Mathf.Max(
                initialCardRerolls,
                configuredMaximumStoredCardRerolls);
            bossRerollReward =
                Mathf.Max(0, configuredBossRerollReward);
        }
    }
}
