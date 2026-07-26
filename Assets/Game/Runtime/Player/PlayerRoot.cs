using System.Collections;
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

        public void Configure(PrototypeGameSession session, Camera worldCamera, MapBounds mapBounds)
        {
            health = GetComponent<HealthComponent>();
            movement = GetComponent<PlayerMovement>();
            critical = GetComponent<CriticalSystem>();
            progression = GetComponent<PlayerProgression>();
            controller = GetComponent<PlayerController>();
            characterAnimation = GetComponent<CharacterSpriteAnimator>();
            if (characterAnimation == null)
            {
                characterAnimation = gameObject.AddComponent<CharacterSpriteAnimator>();
            }

            this.mapBounds = mapBounds;

            health.Configure(10);
            BuildVisual();
            movement.Configure(
                PlayerMovement.DefaultMoveDuration,
                characterAnimation);
            controller.Configure(this, session, worldCamera, mapBounds);
        }

        public void ReceiveDamage(int amount)
        {
            if (health.ApplyDamage(amount))
            {
                characterAnimation.PlayHurt(Vector2.zero);
            }
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
            if (transform.Find("PlayerAttackRange") == null)
            {
                PrototypeVisualFactory.CreateSprite(
                    transform,
                    "PlayerAttackRange",
                    new Color(0.55f, 0.58f, 0.62f, 0.2f),
                    Vector2.one * PlayerController.AttackRange * 2f,
                    5);
            }

            Transform visualTransform = transform.Find("PlayerVisual");
            if (visualTransform == null)
            {
                visualTransform = new GameObject("PlayerVisual").transform;
                visualTransform.SetParent(transform, false);
            }

            SpriteRenderer renderer =
                visualTransform.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer =
                    visualTransform.gameObject.AddComponent<SpriteRenderer>();
            }
            renderer.color = Color.white;
            renderer.sortingOrder = 30;
            visualTransform.localScale = Vector3.one * 1.65f;
            if (!characterAnimation.ConfigureLightBandit(renderer))
            {
                Debug.LogWarning(
                    "LightBandit sprites were not found under Resources.",
                    this);
                renderer.sprite = PrototypeVisualFactory.SquareSprite;
                renderer.color = new Color(0.12f, 0.85f, 0.95f);
                visualTransform.localScale = Vector3.one * 0.75f;
            }

            if (transform.Find("LevelLabel") == null)
            {
                PrototypeVisualFactory.CreateWorldLabel(
                    transform,
                    "PLAYER",
                    new Vector3(0f, 0.72f, 0f),
                    2.5f,
                    35);
            }
        }
    }
}
