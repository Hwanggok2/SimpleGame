using System.Collections;
using TMPro;
using UnityEngine;

namespace SimpleGame
{
    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(CriticalSystem))]
    [RequireComponent(typeof(PlayerProgression))]
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerRoot : MonoBehaviour, IPrototypeDamageTarget
    {
        [SerializeField] private HealthComponent health;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private CriticalSystem critical;
        [SerializeField] private PlayerProgression progression;
        [SerializeField] private PlayerController controller;
        [SerializeField] private CharacterSpriteAnimator characterAnimation;
        [SerializeField] private SpriteRenderer attackRangeRenderer;
        [SerializeField] private TMP_Text levelLabel;

        private const float FrontRecoilDistance = 1.2f;
        private const float FrontRecoilMoveDuration = 0.18f;
        private const float FrontRecoilInputLockDuration = 0.5f;

        private MapBounds mapBounds;
        private Coroutine inputLockRoutine;

        public HealthComponent Health => health;
        public PlayerMovement Movement => movement;
        public CriticalSystem Critical => critical;
        public PlayerProgression Progression => progression;
        public Transform TargetTransform => transform;
        public bool IsAlive => health != null && health.IsAlive;
        public bool IsInputLocked { get; private set; }

        public void ConfigureVisuals(
            SpriteRenderer configuredAttackRange,
            TMP_Text configuredLevelLabel)
        {
            attackRangeRenderer = configuredAttackRange;
            levelLabel = configuredLevelLabel;
        }

        public void Configure(
            PrototypeGameSession session,
            Camera worldCamera,
            MapBounds mapBounds,
            LevelExperienceTable experienceTable,
            GlobalBalance globalBalance)
        {
            health = GetComponent<HealthComponent>();
            movement = GetComponent<PlayerMovement>();
            critical = GetComponent<CriticalSystem>();
            progression = GetComponent<PlayerProgression>();
            controller = GetComponent<PlayerController>();
            characterAnimation = GetComponent<CharacterSpriteAnimator>();
            if (characterAnimation == null)
            {
                Debug.LogError(
                    "Player prefab requires CharacterSpriteAnimator.",
                    this);
                return;
            }

            this.mapBounds = mapBounds;

            health.Configure(10);
            progression.Configure(experienceTable);
            critical.Configure(
                globalBalance.CriticalChancePerCard,
                globalBalance.MaximumCriticalChance);
            BuildVisual();
            movement.Configure(
                PlayerMovement.DefaultMoveDuration,
                characterAnimation);
            controller.Configure(this, session, worldCamera, mapBounds);
        }

        public void ReceiveDamage(int amount)
        {
            if (!health.ApplyDamage(amount))
            {
                return;
            }

            if (health.IsAlive)
            {
                characterAnimation.PlayHurt(Vector2.zero);
                return;
            }

            controller.CancelCommand();
            movement.StopKnockback();
            IsInputLocked = true;
            characterAnimation.PlayDeath(Vector2.zero);
        }

        public void PlayAttack(Vector2 targetPosition)
        {
            characterAnimation.PlayAttack(
                targetPosition - (Vector2)transform.position);
        }

        public void RestoreAfterContinue()
        {
            gameObject.SetActive(true);
            health.RestoreFull();
            movement.StopKnockback();
            controller.CancelCommand();
            if (inputLockRoutine != null)
            {
                StopCoroutine(inputLockRoutine);
                inputLockRoutine = null;
            }

            IsInputLocked = false;
            characterAnimation.Revive();
        }

        public void ApplyFrontRecoil(Vector2 enemyPosition)
        {
            Vector2 direction = (Vector2)transform.position - enemyPosition;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector2.down;
            }

            Vector2 destination = mapBounds.Clamp(
                (Vector2)transform.position +
                direction.normalized * FrontRecoilDistance);
            controller.CancelCommand();
            movement.Knockback(destination, FrontRecoilMoveDuration);
            characterAnimation.PlayHurt(
                enemyPosition - (Vector2)transform.position);
            LockInput(FrontRecoilInputLockDuration);
        }

        public void LockInput(float seconds)
        {
            if (gameObject.activeInHierarchy)
            {
                if (inputLockRoutine != null)
                {
                    StopCoroutine(inputLockRoutine);
                }

                inputLockRoutine = StartCoroutine(LockRoutine(seconds));
            }
        }

        private IEnumerator LockRoutine(float seconds)
        {
            IsInputLocked = true;
            yield return new WaitForSeconds(seconds);
            IsInputLocked = false;
            inputLockRoutine = null;
        }

        private void BuildVisual()
        {
            if (attackRangeRenderer == null || levelLabel == null)
            {
                Debug.LogError(
                    "Player prefab requires preconfigured range and level visuals.",
                    this);
                return;
            }

            attackRangeRenderer.transform.localScale =
                Vector3.one * PlayerController.AttackRange * 2f;
            levelLabel.text = "PLAYER";

            if (!characterAnimation.IsConfigured)
            {
                Debug.LogError(
                    "Player prefab has no configured Animator or SpriteRenderer.",
                    this);
            }
        }
    }
}
