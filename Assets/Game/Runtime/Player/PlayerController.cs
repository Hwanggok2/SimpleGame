using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SimpleGame
{
    public sealed class PlayerController : MonoBehaviour
    {
        public const float AttackRange = 0.72f;
        private const float EnemySelectionRadius = 1.5f;

        private PlayerRoot root;
        private PrototypeGameSession session;
        private Camera worldCamera;
        private MapBounds mapBounds;
        private EnemyBase pendingEnemy;
        private Vector2 destination;
        private bool hasDestination;
        private bool shieldApproachOnly;
        private int pendingAttackCount;

        public void Configure(
            PlayerRoot playerRoot,
            PrototypeGameSession gameSession,
            Camera camera,
            MapBounds bounds)
        {
            root = playerRoot;
            session = gameSession;
            worldCamera = camera;
            mapBounds = bounds;
        }

        private void Update()
        {
            if (root == null || session == null || !session.IsPlaying || !root.IsAlive)
            {
                return;
            }

            ReadPointer();
            TickCommand();
        }

        private void ReadPointer()
        {
            Vector2 screenPosition;
            bool pressed;
            int pointerId = -1;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                pressed = true;
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                pointerId = Touchscreen.current.primaryTouch.touchId.ReadValue();
            }
            else
            {
                pressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
                screenPosition = Mouse.current != null
                    ? Mouse.current.position.ReadValue()
                    : Vector2.zero;
            }

            if (!pressed || root.IsInputLocked)
            {
                return;
            }

            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject(pointerId))
            {
                return;
            }

            Vector3 world = worldCamera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, -worldCamera.transform.position.z));
            destination = mapBounds.Clamp(world);
            EnemyBase selectedEnemy = session.FindEnemyNear(
                destination,
                EnemySelectionRadius);
            if (selectedEnemy != null &&
                hasDestination &&
                pendingEnemy == selectedEnemy)
            {
                if (!shieldApproachOnly)
                {
                    pendingAttackCount++;
                }

                return;
            }

            pendingEnemy = selectedEnemy;
            pendingAttackCount = pendingEnemy == null ? 0 : 1;
            shieldApproachOnly = pendingEnemy != null &&
                pendingEnemy.Archetype == EnemyArchetype.Shield &&
                Vector2.Distance(transform.position, pendingEnemy.transform.position) >
                    pendingEnemy.Definition.ApproachRange;
            hasDestination = true;
            root.Movement.BeginMove();
        }

        private void TickCommand()
        {
            if (!hasDestination)
            {
                return;
            }

            if (pendingEnemy == null || !pendingEnemy.IsAlive)
            {
                hasDestination = !root.Movement.StepTowards(destination, 0.08f);
                return;
            }

            float stoppingDistance = shieldApproachOnly
                ? pendingEnemy.Definition.ApproachRange
                : AttackRange;
            bool reached = root.Movement.StepTowards(
                pendingEnemy.transform.position,
                stoppingDistance);
            if (!reached)
            {
                return;
            }

            if (shieldApproachOnly)
            {
                session.ShowHint("Shield approach reached. Tap the Shield again for a close attack.");
                CancelCommand();
                return;
            }

            EnemyBase targetEnemy = pendingEnemy;
            while (pendingAttackCount > 0 && targetEnemy.IsAlive)
            {
                pendingAttackCount--;
                AttackSide side = CombatResolver.GetAttackSide(
                    targetEnemy.Facing.Direction,
                    targetEnemy.transform.position,
                    transform.position);
                bool critical = root.Critical.Roll();
                CombatResult result = CombatResolver.Resolve(
                    targetEnemy.Archetype,
                    root.Progression.Level,
                    targetEnemy.Level,
                    side,
                    critical);

                root.PlayAttack(targetEnemy.transform.position);
                bool damageApplied = targetEnemy.ReceivePlayerAttack(
                    result,
                    root,
                    side,
                    critical);
                if (result.PlayerReaction == PlayerAttackReaction.Recoil)
                {
                    root.ApplyFrontRecoil(targetEnemy.transform.position);
                }

                session.PlayCombatFeedback(
                    damageApplied,
                    critical,
                    result.PlayerReaction);

                if (result.PlayerReaction == PlayerAttackReaction.Recoil)
                {
                    CancelCommand();
                    return;
                }
            }

            bool defeated = !targetEnemy.IsAlive;
            if (!defeated)
            {
                CancelCommand();
                return;
            }

            pendingEnemy = null;
            shieldApproachOnly = false;
            pendingAttackCount = 0;
            hasDestination = true;
            root.Movement.BeginMove();
        }

        public void CancelCommand()
        {
            pendingEnemy = null;
            hasDestination = false;
            shieldApproachOnly = false;
            pendingAttackCount = 0;
            root?.Movement.CancelMove();
        }
    }
}
