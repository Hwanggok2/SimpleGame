using UnityEngine;

namespace SimpleGame
{
    public sealed class CombatFeedbackController : MonoBehaviour
    {
        [SerializeField] private CameraShakeController cameraShake;
        [SerializeField] private CombatFeedbackProfile profile;

        public void Configure(
            CameraShakeController configuredCameraShake,
            CombatFeedbackProfile configuredProfile = null)
        {
            cameraShake = configuredCameraShake;
            if (configuredProfile != null)
            {
                profile = configuredProfile;
            }
        }

        public void PlayResolvedAttack(
            bool damageApplied,
            bool critical,
            PlayerAttackReaction playerReaction)
        {
            CombatFeedbackLevel feedback = CombatFeedbackResolver.Resolve(
                damageApplied,
                critical,
                playerReaction);
            if (cameraShake == null || profile == null)
            {
                Debug.LogError(
                    "Combat feedback requires CameraShakeController and profile.",
                    this);
                return;
            }

            switch (feedback)
            {
                case CombatFeedbackLevel.NormalHit:
                    cameraShake.Play(
                        profile.NormalHitStrength,
                        profile.NormalHitDuration);
                    break;
                case CombatFeedbackLevel.FrontRecoil:
                    cameraShake.Play(
                        profile.FrontRecoilStrength,
                        profile.FrontRecoilDuration);
                    break;
                case CombatFeedbackLevel.CriticalHit:
                    cameraShake.Play(
                        profile.CriticalHitStrength,
                        profile.CriticalHitDuration);
                    break;
            }
        }
    }
}
