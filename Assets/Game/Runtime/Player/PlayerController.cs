using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SimpleGame
{
    public sealed class PlayerController : MonoBehaviour
    {
        public const float DefaultAttackRange = 0.72f;
        private const float EnemySelectionRadius = 1.5f;

        private PlayerRoot root;
        private PrototypeGameSession session;
        private Camera worldCamera;
        private EnemyBase pendingEnemy;
        private EnemyBase ignoredPathEnemy;
        private Vector2 destination;
        private bool hasDestination;
        private bool shieldApproachOnly;
        private bool postKillEscapeActive;
        private int pendingAttackCount;
        private float attackRange = DefaultAttackRange;

        public void Configure(
            PlayerRoot playerRoot,
            PrototypeGameSession gameSession,
            Camera camera,
            float configuredAttackRange)
        {
            root = playerRoot;
            session = gameSession;
            worldCamera = camera;
            SetAttackRange(configuredAttackRange);
        }

        public void SetAttackRange(float value)
        {
            attackRange = Mathf.Max(0.1f, value);
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
            destination = world;
            Vector2 commandOrigin = transform.position;
            ignoredPathEnemy = null;
            EnemyBase directEnemy = session.FindEnemyNear(
                destination,
                EnemySelectionRadius);
            if (directEnemy != null &&
                !IsTargetInCommandDirection(
                    commandOrigin,
                    destination,
                    directEnemy.transform.position))
            {
                directEnemy = null;
            }

            EnemyBase pathEnemy = session.FindFirstEnemyOnPath(
                commandOrigin,
                destination);
            bool interceptedOnPath =
                directEnemy == null && pathEnemy != null;
            EnemyBase selectedEnemy =
                SelectCommandEnemy(directEnemy, pathEnemy);
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
            postKillEscapeActive = false;
            shieldApproachOnly = pendingEnemy != null &&
                !interceptedOnPath &&
                pendingEnemy.Archetype == EnemyArchetype.Shield &&
                Vector2.Distance(transform.position, pendingEnemy.transform.position) >
                    pendingEnemy.Definition.ApproachRange;
            hasDestination = true;
            root.TrySpawnMovingSlash(
                destination - (Vector2)transform.position);
            BeginCurrentMove();
        }

        private void TickCommand()
        {
            if (!hasDestination)
            {
                return;
            }

            if (pendingEnemy == null || !pendingEnemy.IsAlive)
            {
                pendingEnemy = session.FindFirstEnemyOnPath(
                    transform.position,
                    destination,
                    ignoredPathEnemy);
                if (pendingEnemy == null)
                {
                    bool reachedDestination = root.Movement.StepTowards(
                        destination,
                        root.MoveArrivalTolerance);
                    hasDestination = !reachedDestination;
                    if (reachedDestination)
                    {
                        ignoredPathEnemy = null;
                        postKillEscapeActive = false;
                    }

                    return;
                }

                pendingAttackCount = 1;
                shieldApproachOnly = false;
                BeginCurrentMove();
            }

            float stoppingDistance = shieldApproachOnly
                ? pendingEnemy.Definition.ApproachRange
                : attackRange;
            bool reached = root.Movement.StepTowards(
                pendingEnemy.transform.position,
                stoppingDistance,
                true);
            if (!reached)
            {
                return;
            }

            if (shieldApproachOnly)
            {
                session.ShowHint("방패병의 안쪽에 도착했습니다. 다시 누르면 근접 공격합니다.");
                CancelCommand();
                return;
            }

            EnemyBase targetEnemy = pendingEnemy;
            while (pendingAttackCount > 0 && targetEnemy.IsAlive)
            {
                pendingAttackCount--;
                bool critical = root.Critical.Roll();

                root.PlayAttack(targetEnemy.transform.position);
                PlayerAttackExecution execution =
                    root.AttackEnemy(targetEnemy, critical);
                bool shieldRecoil =
                    execution.PrimaryResult.PlayerReaction ==
                        PlayerAttackReaction.Recoil &&
                    targetEnemy.Archetype ==
                        EnemyArchetype.Shield;
                bool bypassedShield =
                    shieldRecoil &&
                    root.CombatAbilities.RollShieldBypass();
                PlayerAttackReaction effectiveReaction =
                    shieldRecoil && !bypassedShield
                        ? PlayerAttackReaction.Recoil
                        : PlayerAttackReaction.None;
                if (bypassedShield)
                {
                    session.ShowHint(
                        "방패 우회 성공! 반동과 조작 불가를 무시했습니다.");
                }

                if (effectiveReaction ==
                    PlayerAttackReaction.Recoil)
                {
                    root.ApplyFrontRecoil(targetEnemy.transform.position);
                }

                session.PlayCombatFeedback(
                    execution.AnyDamageApplied,
                    critical,
                    effectiveReaction);

                if (effectiveReaction ==
                    PlayerAttackReaction.Recoil)
                {
                    CancelCommand();
                    return;
                }
            }

            bool defeated = !targetEnemy.IsAlive;
            bool hasPiercing =
                root.CombatAbilities.PiercingLevel > 0;
            if (!ShouldContinueAfterPathAttack(
                defeated,
                hasPiercing))
            {
                CancelCommand();
                return;
            }

            pendingEnemy = null;
            ignoredPathEnemy = defeated
                ? null
                : targetEnemy;
            shieldApproachOnly = false;
            pendingAttackCount = 0;
            postKillEscapeActive = defeated;
            hasDestination = true;
            root.Movement.BeginMove(
                destination,
                defeated
                    ? root.PostKillEscapeSpeedMultiplier
                    : 1f);
        }

        private void BeginCurrentMove()
        {
            if (pendingEnemy == null)
            {
                float multiplier = postKillEscapeActive
                    ? root.PostKillEscapeSpeedMultiplier
                    : 1f;
                root.Movement.BeginMove(destination, multiplier);
                return;
            }

            float speedMultiplier = postKillEscapeActive
                ? root.PostKillEscapeSpeedMultiplier
                : root.PathEnemyApproachSpeedMultiplier;
            root.Movement.BeginMove(
                pendingEnemy.transform.position,
                speedMultiplier);
        }

        public void CancelCommand()
        {
            pendingEnemy = null;
            ignoredPathEnemy = null;
            hasDestination = false;
            shieldApproachOnly = false;
            postKillEscapeActive = false;
            pendingAttackCount = 0;
            root?.Movement.CancelMove();
        }

        public static bool IsTargetInCommandDirection(
            Vector2 origin,
            Vector2 destination,
            Vector2 target)
        {
            Vector2 commandDirection = destination - origin;
            if (commandDirection.sqrMagnitude <= 0.0001f)
            {
                return true;
            }

            Vector2 targetOffset = target - origin;
            if (targetOffset.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            return CombatGeometry.IsAheadAlongPath(
                target,
                origin,
                destination);
        }

        public static bool ShouldContinueAfterPathAttack(
            bool targetDefeated,
            bool hasPiercing)
        {
            return targetDefeated || hasPiercing;
        }

        public static EnemyBase SelectCommandEnemy(
            EnemyBase directEnemy,
            EnemyBase pathEnemy)
        {
            return directEnemy != null
                ? directEnemy
                : pathEnemy;
        }
    }
}
