using System;
using UnityEngine;

namespace SimpleGame
{
    public sealed class CharacterSpriteAnimator : MonoBehaviour
    {
        public const string MotionParameter = "Motion";
        public const string FaceLeftParameter = "FaceLeft";
        public const string AttackParameter = "Attack";
        public const string Attack2Parameter = "Attack2";
        public const string HurtParameter = "Hurt";
        public const string DeathParameter = "Death";

        private static readonly int MotionId = Animator.StringToHash(MotionParameter);
        private static readonly int FaceLeftId =
            Animator.StringToHash(FaceLeftParameter);
        private static readonly int AttackId = Animator.StringToHash(AttackParameter);
        private static readonly int Attack2Id =
            Animator.StringToHash(Attack2Parameter);
        private static readonly int HurtId = Animator.StringToHash(HurtParameter);
        private static readonly int DeathId = Animator.StringToHash(DeathParameter);
        private const float MinimumDeathDuration = 0.55f;

        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color baseTint = Color.white;
        [SerializeField] private Color pulseTint = Color.white;
        [SerializeField, Min(0f)] private float tintPulseSpeed;
        private float deathDuration = MinimumDeathDuration;
        private int motionState = int.MinValue;
        private int faceLeftState = -1;

        public bool IsConfigured =>
            animator != null &&
            animator.runtimeAnimatorController != null &&
            spriteRenderer != null;

        public void Configure(
            Animator targetAnimator,
            SpriteRenderer targetRenderer)
        {
            animator = targetAnimator;
            spriteRenderer = targetRenderer;
            motionState = int.MinValue;
            faceLeftState = -1;
            CacheDeathDuration();
        }

        public void ConfigureTintPulse(Color color, float speed)
        {
            pulseTint = color;
            tintPulseSpeed = Mathf.Max(0f, speed);
            enabled = tintPulseSpeed > 0f;
            ApplyTint(0f);
        }

        public void SetMoving(Vector2 direction)
        {
            Face(direction);
            SetMotion(1);
        }

        public void SetIdle()
        {
            SetMotion(0);
        }

        public void SetGuard(Vector2 direction)
        {
            Face(direction);
            SetMotion(2);
        }

        public void Face(Vector2 direction)
        {
            if (spriteRenderer == null || Mathf.Abs(direction.x) <= 0.01f)
            {
                return;
            }

            int requestedFaceLeft = direction.x < 0f ? 1 : 0;
            if (animator == null ||
                faceLeftState == requestedFaceLeft)
            {
                return;
            }

            animator.SetBool(
                FaceLeftId,
                requestedFaceLeft == 1);
            faceLeftState = requestedFaceLeft;
        }

        public void PlayAttack(Vector2 direction)
        {
            PlayAttack(direction, 1);
        }

        public void PlayAttack(
            Vector2 direction,
            int animationVariant)
        {
            Face(direction);
            if (animator == null)
            {
                return;
            }

            if (animationVariant == 2)
            {
                animator.ResetTrigger(AttackId);
                animator.SetTrigger(Attack2Id);
                return;
            }

            animator.ResetTrigger(Attack2Id);
            animator.SetTrigger(AttackId);
        }

        public void PlayHurt(Vector2 direction)
        {
            Face(direction);
            animator?.SetTrigger(HurtId);
        }

        public float PlayDeath(Vector2 direction)
        {
            Face(direction);
            SetMotion(0);
            if (animator != null)
            {
                animator.ResetTrigger(AttackId);
                animator.ResetTrigger(Attack2Id);
                animator.ResetTrigger(HurtId);
                animator.SetTrigger(DeathId);
            }

            return deathDuration;
        }

        private void LateUpdate()
        {
            if (tintPulseSpeed <= 0f)
            {
                return;
            }

            float blend = 0.5f +
                0.5f * Mathf.Sin(Time.time * tintPulseSpeed);
            ApplyTint(blend);
        }

        private void OnDisable()
        {
            ApplyTint(0f);
        }

        private void ApplyTint(float blend)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(
                    baseTint,
                    pulseTint,
                    Mathf.Clamp01(blend));
            }
        }

        public void Revive()
        {
            motionState = int.MinValue;
            faceLeftState = -1;
            if (animator == null)
            {
                return;
            }

            animator.ResetTrigger(AttackId);
            animator.ResetTrigger(Attack2Id);
            animator.ResetTrigger(HurtId);
            animator.ResetTrigger(DeathId);
            animator.SetInteger(MotionId, 0);
            animator.SetBool(FaceLeftId, false);
            motionState = 0;
            faceLeftState = 0;
            animator.Play("Base Layer.Idle", 0, 0f);
            animator.Update(0f);
        }

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }

            enabled = tintPulseSpeed > 0f;
            CacheDeathDuration();
        }

        private void SetMotion(int state)
        {
            if (animator == null || motionState == state)
            {
                return;
            }

            animator.SetInteger(MotionId, state);
            motionState = state;
        }

        private void CacheDeathDuration()
        {
            deathDuration = MinimumDeathDuration;
            if (animator?.runtimeAnimatorController == null)
            {
                return;
            }

            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name.EndsWith("_Death", StringComparison.Ordinal))
                {
                    deathDuration = Mathf.Max(MinimumDeathDuration, clip.length);
                    return;
                }
            }
        }
    }
}
