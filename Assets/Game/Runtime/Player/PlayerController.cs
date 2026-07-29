using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SimpleGame
{
    public sealed class PlayerController : MonoBehaviour
    {
        public const float DefaultAttackRange = 0.72f;
        public const float EnemyPiercingHorizontalRadius = 1.5f;
        public const float EnemyPiercingVerticalRadius = 2f;
        private const float EnemySelectionRadius =
            EnemyPiercingVerticalRadius;

        private PlayerRoot root;
        private PrototypeGameSession session;
        private EnemyWorldService enemyWorld;
        private Camera worldCamera;
        private EnemyBase pendingEnemy;
        private EnemyBase ignoredPathEnemy;
        private uint ignoredPathEnemyGeneration;
        private Vector2 commandOrigin;
        private Vector2 destination;
        private bool hasDestination;
        private bool shieldApproachOnly;
        private bool postKillEscapeActive;
        private int pendingAttackCount;
        private float attackRange = DefaultAttackRange;

        public void Configure(
            PlayerRoot playerRoot,
            PrototypeGameSession gameSession,
            EnemyWorldService configuredEnemyWorld,
            Camera camera,
            float configuredAttackRange)
        {
            root = playerRoot;
            session = gameSession;
            enemyWorld = configuredEnemyWorld;
            worldCamera = camera;
            SetAttackRange(configuredAttackRange);
        }

        public void SetAttackRange(float value)
        {
            attackRange = Mathf.Max(0.1f, value);
        }

        private void Update()
        {
            if (root == null ||
                session == null ||
                enemyWorld == null ||
                !session.IsPlaying ||
                !root.IsAlive)
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
            commandOrigin = transform.position;
            SetIgnoredPathEnemy(null);
            root.CombatAbilities.BeginPiercingCommand();
            EnemyBase directEnemy = enemyWorld.FindEnemyNear(
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

            Vector2 movementTarget = directEnemy != null
                ? directEnemy.transform.position
                : destination;
            EnemyBase pathEnemy = enemyWorld.FindFirstEnemyOnPath(
                commandOrigin,
                movementTarget,
                EnemyWorldService.GetColliderRadius(root));
            bool interceptedOnPath =
                pathEnemy != null && pathEnemy != directEnemy;
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
                pendingEnemy = enemyWorld.FindFirstEnemyOnPath(
                    transform.position,
                    destination,
                    EnemyWorldService.GetColliderRadius(root),
                    ResolveCurrentIgnoredPathEnemy());
                if (pendingEnemy == null)
                {
                    bool reachedDestination = root.Movement.StepTowards(
                        destination,
                        root.MoveArrivalTolerance);
                    hasDestination = !reachedDestination;
                    if (reachedDestination)
                    {
                        SetIgnoredPathEnemy(null);
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
            bool piercingRequested = IsPiercingTouchRequested(
                commandOrigin,
                targetEnemy.transform.position,
                destination);
            bool piercingReserved = false;
            bool attackExecuted = false;
            while (pendingAttackCount > 0 && targetEnemy.IsAlive)
            {
                pendingAttackCount--;
                if (!attackExecuted)
                {
                    piercingReserved =
                        piercingRequested &&
                        root.CombatAbilities.TryConsumePiercingTarget();
                }

                attackExecuted = true;
                bool critical = root.Critical.Roll();

                root.PlayAttack(targetEnemy.transform.position);
                PlayerAttackExecution execution =
                    root.AttackEnemy(
                        targetEnemy,
                        critical,
                        piercingReserved);
                if (piercingReserved &&
                    !execution.PiercingAllowed)
                {
                    root.CombatAbilities.RefundPiercingTarget();
                    piercingReserved = false;
                }

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
                    !targetEnemy.IsAlive,
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
            if (!ShouldContinueAfterPathAttack(
                defeated,
                piercingReserved,
                attackExecuted))
            {
                CancelCommand();
                return;
            }

            pendingEnemy = null;
            SetIgnoredPathEnemy(defeated
                ? null
                : targetEnemy);
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
            SetIgnoredPathEnemy(null);
            hasDestination = false;
            shieldApproachOnly = false;
            postKillEscapeActive = false;
            pendingAttackCount = 0;
            root?.Movement.CancelMove();
        }

        private EnemyBase ResolveCurrentIgnoredPathEnemy()
        {
            if (ignoredPathEnemy == null ||
                !ignoredPathEnemy.IsAlive ||
                ignoredPathEnemy.SpawnGeneration !=
                    ignoredPathEnemyGeneration)
            {
                SetIgnoredPathEnemy(null);
            }

            return ignoredPathEnemy;
        }

        private void SetIgnoredPathEnemy(EnemyBase enemy)
        {
            ignoredPathEnemy = enemy;
            ignoredPathEnemyGeneration =
                enemy != null ? enemy.SpawnGeneration : 0u;
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
            bool piercingReserved,
            bool attackExecuted)
        {
            return targetDefeated ||
                (piercingReserved && attackExecuted);
        }

        public static bool IsPiercingTouchRequested(
            Vector2 commandOrigin,
            Vector2 targetPosition,
            Vector2 destination)
        {
            Vector2 attackDirection =
                targetPosition - commandOrigin;
            if (attackDirection.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            bool destinationIsPastTarget = Vector2.Dot(
                destination - targetPosition,
                attackDirection) > 0f;
            Vector2 areaOffset =
                destination - targetPosition;
            float normalizedHorizontal =
                areaOffset.x / EnemyPiercingHorizontalRadius;
            float normalizedVertical =
                areaOffset.y / EnemyPiercingVerticalRadius;
            bool destinationIsOutsideArea =
                normalizedHorizontal * normalizedHorizontal +
                normalizedVertical * normalizedVertical > 1f;
            return destinationIsPastTarget &&
                destinationIsOutsideArea;
        }

        public static EnemyBase SelectCommandEnemy(
            EnemyBase directEnemy,
            EnemyBase pathEnemy)
        {
            return pathEnemy != null
                ? pathEnemy
                : directEnemy;
        }
    }
}
