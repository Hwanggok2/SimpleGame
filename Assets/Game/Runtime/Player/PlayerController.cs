using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SimpleGame
{
    public sealed class PlayerController : MonoBehaviour
    {
        private PlayerRoot root;
        private PrototypeGameSession session;
        private Camera worldCamera;
        private MapBounds mapBounds;
        private EnemyBase pendingEnemy;
        private Vector2 destination;
        private bool hasDestination;
        private float nextAttackTime;

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
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Vector2 screenPosition;
            bool pressed;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                pressed = true;
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
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

            Vector3 world = worldCamera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, -worldCamera.transform.position.z));
            destination = mapBounds.Clamp(world);
            pendingEnemy = session.FindEnemyNear(destination, 1.1f);
            hasDestination = true;
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

            float stoppingDistance = pendingEnemy.Archetype == EnemyArchetype.Shield
                ? 2.25f
                : 0.72f;
            bool reached = root.Movement.StepTowards(
                pendingEnemy.transform.position,
                stoppingDistance);
            if (!reached || Time.time < nextAttackTime)
            {
                return;
            }

            if (pendingEnemy.Archetype == EnemyArchetype.Shield &&
                Vector2.Distance(transform.position, pendingEnemy.transform.position) > 1f)
            {
                session.ShowHint("Shield approach reached. Tap the Shield again for a close attack.");
                hasDestination = false;
                return;
            }

            AttackSide side = CombatResolver.GetAttackSide(
                pendingEnemy.Facing.Direction,
                pendingEnemy.transform.position,
                transform.position);
            bool critical = root.Critical.Roll();
            CombatResult result = CombatResolver.Resolve(
                pendingEnemy.Archetype,
                root.Progression.Level,
                pendingEnemy.Level,
                side,
                critical);

            pendingEnemy.ReceivePlayerAttack(result, root, side, critical);
            nextAttackTime = Time.time + 0.28f;
            if (pendingEnemy.Archetype == EnemyArchetype.Shield &&
                side == AttackSide.Front &&
                pendingEnemy.Level > root.Progression.Level - 2)
            {
                root.LockInput(0.5f);
            }

            if (!pendingEnemy.IsAlive)
            {
                pendingEnemy = null;
                hasDestination = true;
            }
        }
    }
}
