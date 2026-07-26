using System;
using UnityEngine;

namespace SimpleGame
{
    public sealed class CharacterSpriteAnimator : MonoBehaviour
    {
        public const string MotionParameter = "Motion";
        public const string FaceLeftParameter = "FaceLeft";
        public const string AttackParameter = "Attack";
        public const string HurtParameter = "Hurt";
        public const string DeathParameter = "Death";

        private static readonly int MotionId = Animator.StringToHash(MotionParameter);
        private static readonly int FaceLeftId =
            Animator.StringToHash(FaceLeftParameter);
        private static readonly int AttackId = Animator.StringToHash(AttackParameter);
        private static readonly int HurtId = Animator.StringToHash(HurtParameter);
        private static readonly int DeathId = Animator.StringToHash(DeathParameter);
        private const float MinimumDeathDuration = 0.55f;

        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        private float deathDuration = MinimumDeathDuration;

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
            CacheDeathDuration();
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

            animator?.SetBool(FaceLeftId, direction.x < 0f);
        }

        public void PlayAttack(Vector2 direction)
        {
            Face(direction);
            animator?.SetTrigger(AttackId);
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
                animator.ResetTrigger(HurtId);
                animator.SetTrigger(DeathId);
            }

            return deathDuration;
        }

        public void Revive()
        {
            if (animator == null)
            {
                return;
            }

            animator.ResetTrigger(AttackId);
            animator.ResetTrigger(HurtId);
            animator.ResetTrigger(DeathId);
            animator.SetInteger(MotionId, 0);
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

            CacheDeathDuration();
        }

        private void SetMotion(int state)
        {
            animator?.SetInteger(MotionId, state);
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
