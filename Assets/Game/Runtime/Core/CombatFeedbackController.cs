using UnityEngine;

namespace SimpleGame
{
    public sealed class CombatFeedbackController : MonoBehaviour
    {
        [SerializeField] private CameraShakeController cameraShake;
        [SerializeField] private float normalHitStrength = 0.07f;
        [SerializeField] private float normalHitDuration = 0.1f;
        [SerializeField] private float frontRecoilStrength = 0.13f;
        [SerializeField] private float frontRecoilDuration = 0.14f;
        [SerializeField] private float criticalHitStrength = 0.22f;
        [SerializeField] private float criticalHitDuration = 0.18f;

        public CombatFeedbackLevel LastPlayed { get; private set; }

        public void Configure(CameraShakeController configuredCameraShake)
        {
            cameraShake = configuredCameraShake;
        }

        public void PlayResolvedAttack(
            bool damageApplied,
            bool critical,
            PlayerAttackReaction playerReaction)
        {
            LastPlayed = CombatFeedbackResolver.Resolve(
                damageApplied,
                critical,
                playerReaction);

            switch (LastPlayed)
            {
                case CombatFeedbackLevel.NormalHit:
                    cameraShake.Play(normalHitStrength, normalHitDuration);
                    break;
                case CombatFeedbackLevel.FrontRecoil:
                    cameraShake.Play(frontRecoilStrength, frontRecoilDuration);
                    break;
                case CombatFeedbackLevel.CriticalHit:
                    cameraShake.Play(criticalHitStrength, criticalHitDuration);
                    break;
            }
        }
    }
}
