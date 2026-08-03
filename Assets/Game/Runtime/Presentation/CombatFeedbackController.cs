using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    public sealed class CombatFeedbackController : MonoBehaviour
    {
        private const int DamagePopupPrewarmCount = 16;
        private const int MaximumDamagePopupCount = 64;
        private static readonly Vector3[] DamagePopupOffsets =
        {
            Vector3.zero,
            new(-0.16f, 0.06f, 0f),
            new(0.16f, 0.12f, 0f),
            new(-0.08f, 0.18f, 0f),
            new(0.08f, 0.24f, 0f)
        };

        [SerializeField] private CameraShakeController cameraShake;
        [SerializeField] private CombatFeedbackProfile profile;
        [SerializeField] private DamagePopupView damagePopupPrefab;

        private readonly List<DamagePopupView> damagePopupPool = new();
        private int replacementPopupIndex;
        private int popupOffsetIndex;
        private bool missingDamagePopupReported;

        public bool HasConfiguredDamagePopup => damagePopupPrefab != null;

        public void Configure(
            CameraShakeController configuredCameraShake,
            CombatFeedbackProfile configuredProfile = null,
            DamagePopupView configuredDamagePopupPrefab = null)
        {
            cameraShake = configuredCameraShake;
            if (configuredProfile != null)
            {
                profile = configuredProfile;
            }

            if (configuredDamagePopupPrefab != null)
            {
                damagePopupPrefab = configuredDamagePopupPrefab;
                missingDamagePopupReported = false;
            }

            if (Application.isPlaying)
            {
                EnsureDamagePopupPool();
            }
        }

        public void PlayResolvedAttack(
            bool damageApplied,
            bool targetDefeated,
            bool critical,
            PlayerAttackReaction playerReaction)
        {
            if (targetDefeated)
            {
                return;
            }

            CombatFeedbackLevel feedback = CombatFeedbackResolver.Resolve(
                damageApplied,
                false,
                critical,
                playerReaction);
            Play(feedback);
        }

        public void PlayDefeatingHit(bool critical = false)
        {
            Play(
                critical
                    ? CombatFeedbackLevel.CriticalHit
                    : CombatFeedbackLevel.DefeatingHit);
        }

        public void ShowDamagePopup(
            Vector3 worldPosition,
            int amount,
            DamagePopupStyle style)
        {
            if (amount <= 0)
            {
                return;
            }

            if (damagePopupPrefab == null)
            {
                if (!missingDamagePopupReported)
                {
                    Debug.LogWarning(
                        "Damage popup prefab is not configured.",
                        this);
                    missingDamagePopupReported = true;
                }

                return;
            }

            DamagePopupView popup = AcquireDamagePopup();
            Vector3 offset = DamagePopupOffsets[
                popupOffsetIndex % DamagePopupOffsets.Length];
            popupOffsetIndex++;
            popup.Play(worldPosition + offset, amount, style);
        }

        private void Awake()
        {
            EnsureDamagePopupPool();
        }

        private void Play(CombatFeedbackLevel feedback)
        {
            if (feedback == CombatFeedbackLevel.None)
            {
                return;
            }

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
                case CombatFeedbackLevel.DefeatingHit:
                    cameraShake.Play(
                        profile.DefeatingHitStrength,
                        profile.DefeatingHitDuration);
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

        private void EnsureDamagePopupPool()
        {
            if (!Application.isPlaying || damagePopupPrefab == null)
            {
                return;
            }

            while (damagePopupPool.Count < DamagePopupPrewarmCount)
            {
                CreateDamagePopup();
            }
        }

        private DamagePopupView AcquireDamagePopup()
        {
            for (int index = 0; index < damagePopupPool.Count; index++)
            {
                DamagePopupView popup = damagePopupPool[index];
                if (popup != null && !popup.IsPlaying)
                {
                    return popup;
                }
            }

            if (damagePopupPool.Count < MaximumDamagePopupCount)
            {
                return CreateDamagePopup();
            }

            replacementPopupIndex %= damagePopupPool.Count;
            DamagePopupView replacement =
                damagePopupPool[replacementPopupIndex];
            replacementPopupIndex++;
            return replacement;
        }

        private DamagePopupView CreateDamagePopup()
        {
            DamagePopupView popup = Instantiate(
                damagePopupPrefab,
                transform);
            popup.name = "DamagePopup";
            popup.Stop();
            damagePopupPool.Add(popup);
            return popup;
        }
    }
}
