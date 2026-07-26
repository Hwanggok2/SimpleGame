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

            health.Configure(10);
            movement.Configure(4f);
            controller.Configure(this, session, worldCamera, mapBounds);
            BuildVisual();
        }

        public void ReceiveDamage(int amount)
        {
            health.ApplyDamage(amount);
        }

        public void RestoreAfterContinue()
        {
            gameObject.SetActive(true);
            health.RestoreFull();
            IsInputLocked = false;
        }

        public void LockInput(float seconds)
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(LockRoutine(seconds));
            }
        }

        private IEnumerator LockRoutine(float seconds)
        {
            IsInputLocked = true;
            yield return new WaitForSeconds(seconds);
            IsInputLocked = false;
        }

        private void BuildVisual()
        {
            if (transform.Find("PlayerVisual") != null)
            {
                return;
            }

            PrototypeVisualFactory.CreateSprite(
                transform,
                "PlayerVisual",
                new Color(0.12f, 0.85f, 0.95f),
                new Vector2(0.75f, 0.75f),
                30);
            PrototypeVisualFactory.CreateWorldLabel(
                transform,
                "PLAYER",
                new Vector3(0f, 0.72f, 0f),
                2.5f,
                35);
        }
    }
}
