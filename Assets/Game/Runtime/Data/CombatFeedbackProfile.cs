using UnityEngine;

namespace SimpleGame
{
    [CreateAssetMenu(
        fileName = "CombatFeedbackProfile",
        menuName = "SimpleGame/Data/Combat Feedback Profile")]
    public sealed class CombatFeedbackProfile : ScriptableObject
    {
        [SerializeField, Min(0f)] private float normalHitStrength = 0.07f;
        [SerializeField, Min(0f)] private float normalHitDuration = 0.1f;
        [SerializeField, Min(0f)] private float defeatingHitStrength = 0.13f;
        [SerializeField, Min(0f)] private float defeatingHitDuration = 0.14f;
        [SerializeField, Min(0f)] private float frontRecoilStrength = 0.13f;
        [SerializeField, Min(0f)] private float frontRecoilDuration = 0.14f;
        [SerializeField, Min(0f)] private float criticalHitStrength = 0.22f;
        [SerializeField, Min(0f)] private float criticalHitDuration = 0.18f;

        public float NormalHitStrength => normalHitStrength;
        public float NormalHitDuration => normalHitDuration;
        public float DefeatingHitStrength => defeatingHitStrength;
        public float DefeatingHitDuration => defeatingHitDuration;
        public float FrontRecoilStrength => frontRecoilStrength;
        public float FrontRecoilDuration => frontRecoilDuration;
        public float CriticalHitStrength => criticalHitStrength;
        public float CriticalHitDuration => criticalHitDuration;

        public void Configure(
            float normalStrength,
            float normalDuration,
            float defeatingStrength,
            float defeatingDuration,
            float recoilStrength,
            float recoilDuration,
            float criticalStrength,
            float criticalDuration)
        {
            normalHitStrength = Mathf.Max(0f, normalStrength);
            normalHitDuration = Mathf.Max(0f, normalDuration);
            defeatingHitStrength = Mathf.Max(0f, defeatingStrength);
            defeatingHitDuration = Mathf.Max(0f, defeatingDuration);
            frontRecoilStrength = Mathf.Max(0f, recoilStrength);
            frontRecoilDuration = Mathf.Max(0f, recoilDuration);
            criticalHitStrength = Mathf.Max(0f, criticalStrength);
            criticalHitDuration = Mathf.Max(0f, criticalDuration);
        }
    }
}
