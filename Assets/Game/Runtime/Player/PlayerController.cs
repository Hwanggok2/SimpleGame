using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SimpleGame
{
    public sealed class PlayerController : MonoBehaviour
    {
        public const float DefaultAttackRange = 0.72f;
        public const float EnemyPiercingHorizontalRadius = 1.5f;
        public const float EnemyPiercingVerticalRadius = 2f;
        public const float AimViewportPadding = 0.5f;
        public const float AimRayWidth = 0.08f;
        public const float AimEndpointSize = 0.42f;
        public const float MinimumCommandAimMagnitude = 0.01f;
        private const float EnemySelectionRadius =
            EnemyPiercingVerticalRadius;

        [SerializeField] private SpriteRenderer aimRayRenderer;
        [SerializeField] private SpriteRenderer aimEndpointRenderer;

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
        private Vector2 aimInput;
        private Vector2 aimDestination;
        private bool isAiming;
        private readonly List<RaycastResult> uiRaycastResults = new();

        public Vector2 AimInput => aimInput;
        public Vector2 AimDestination => aimDestination;
        public bool IsAiming => isAiming;

        public void ConfigureAimVisuals(
            SpriteRenderer configuredRayRenderer,
            SpriteRenderer configuredEndpointRenderer)
        {
            aimRayRenderer = configuredRayRenderer;
            aimEndpointRenderer = configuredEndpointRenderer;
            SetAimVisualsVisible(false);
        }

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
            EndAim();
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

        private void LateUpdate()
        {
            RefreshAimVisuals();
        }

        private void ReadPointer()
        {
            if (root.IsInputLocked)
            {
                return;
            }

            bool touchPressed = false;
            if (Touchscreen.current != null)
            {
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (!touch.press.wasPressedThisFrame)
                    {
                        continue;
                    }

                    touchPressed = true;
                    TryIssueScreenPointerCommand(
                        touch.position.ReadValue(),
                        touch.touchId.ReadValue());
                }
            }

            if (touchPressed ||
                Mouse.current == null ||
                !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            TryIssueScreenPointerCommand(
                Mouse.current.position.ReadValue(),
                -1);
        }

        private void TryIssueScreenPointerCommand(
            Vector2 screenPosition,
            int pointerId)
        {
            if (IsScreenPointOverUi(
                    EventSystem.current,
                    screenPosition,
                    pointerId,
                    uiRaycastResults))
            {
                return;
            }

            Vector3 world = worldCamera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, -worldCamera.transform.position.z));
            TryIssueCommand(world);
        }

        public static bool IsScreenPointOverUi(
            EventSystem eventSystem,
            Vector2 screenPosition,
            int pointerId,
            List<RaycastResult> results)
        {
            if (eventSystem == null || results == null)
            {
                return false;
            }

            results.Clear();
            var pointerData = new PointerEventData(eventSystem)
            {
                position = screenPosition,
                pointerId = pointerId
            };
            eventSystem.RaycastAll(pointerData, results);
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].module is GraphicRaycaster)
                {
                    results.Clear();
                    return true;
                }
            }

            results.Clear();
            return false;
        }

        public bool TryIssueCommand(Vector2 worldDestination)
        {
            if (root == null ||
                session == null ||
                enemyWorld == null ||
                !session.IsPlaying ||
                !root.IsAlive ||
                root.IsInputLocked)
            {
                return false;
            }

            destination = worldDestination;
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

                return true;
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
            return true;
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
                    targetEnemy.Definition.BlocksFrontAttacks;
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

        public bool BeginAim()
        {
            if (root == null ||
                session == null ||
                !session.IsPlaying ||
                !root.IsAlive ||
                root.IsInputLocked)
            {
                return false;
            }

            isAiming = true;
            aimInput = Vector2.zero;
            RefreshAimVisuals();
            return true;
        }

        public void SetAimInput(Vector2 normalizedInput)
        {
            if (!isAiming)
            {
                return;
            }

            aimInput = Vector2.ClampMagnitude(
                normalizedInput,
                1f);
            RefreshAimVisuals();
        }

        public void EndAim()
        {
            isAiming = false;
            aimInput = Vector2.zero;
            aimDestination = transform.position;
            SetAimVisualsVisible(false);
        }

        public bool ExecuteAimedCommand()
        {
            if (!isAiming ||
                !HasCommandAim(aimInput))
            {
                return false;
            }

            Vector2 commandDestination = aimDestination;
            return TryIssueCommand(commandDestination);
        }

        public static bool HasCommandAim(Vector2 normalizedInput)
        {
            return normalizedInput.magnitude >=
                MinimumCommandAimMagnitude;
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

        public static Vector2 CalculateAimPoint(
            Vector2 playerPosition,
            Vector2 normalizedInput,
            float maximumDistance)
        {
            return playerPosition +
                Vector2.ClampMagnitude(normalizedInput, 1f) *
                Mathf.Max(0f, maximumDistance);
        }

        public static float CalculateMaximumAimDistance(
            Vector2 playerPosition,
            Vector2 cameraCenter,
            Vector2 cameraHalfExtents,
            Vector2 direction,
            float padding = AimViewportPadding)
        {
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return 0f;
            }

            Vector2 normalized = direction.normalized;
            Vector2 safeHalfExtents = new(
                Mathf.Max(0f, cameraHalfExtents.x - padding),
                Mathf.Max(0f, cameraHalfExtents.y - padding));
            float horizontalDistance = DistanceToViewportEdge(
                playerPosition.x,
                cameraCenter.x,
                safeHalfExtents.x,
                normalized.x);
            float verticalDistance = DistanceToViewportEdge(
                playerPosition.y,
                cameraCenter.y,
                safeHalfExtents.y,
                normalized.y);
            return Mathf.Max(
                0f,
                Mathf.Min(
                    horizontalDistance,
                    verticalDistance));
        }

        private static float DistanceToViewportEdge(
            float playerCoordinate,
            float cameraCoordinate,
            float halfExtent,
            float direction)
        {
            if (Mathf.Abs(direction) <= 0.0001f)
            {
                return float.PositiveInfinity;
            }

            float boundary =
                cameraCoordinate +
                Mathf.Sign(direction) * halfExtent;
            return Mathf.Max(
                0f,
                (boundary - playerCoordinate) / direction);
        }

        private void RefreshAimVisuals()
        {
            Vector2 playerPosition = transform.position;
            if (!isAiming ||
                worldCamera == null)
            {
                aimDestination = playerPosition;
                SetAimVisualsVisible(false);
                return;
            }

            float halfHeight = worldCamera.orthographicSize;
            float maximumDistance =
                CalculateMaximumAimDistance(
                    playerPosition,
                    worldCamera.transform.position,
                    new Vector2(
                        halfHeight * worldCamera.aspect,
                        halfHeight),
                    aimInput);
            aimDestination = CalculateAimPoint(
                playerPosition,
                aimInput,
                maximumDistance);
            SetAimVisualsVisible(true);

            Vector2 offset =
                aimDestination - playerPosition;
            float length = offset.magnitude;
            if (aimRayRenderer != null)
            {
                Transform ray = aimRayRenderer.transform;
                ray.position =
                    playerPosition + offset * 0.5f;
                ray.rotation = Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Atan2(offset.y, offset.x) *
                    Mathf.Rad2Deg);
                ray.localScale = new Vector3(
                    length,
                    AimRayWidth,
                    1f);
            }

            if (aimEndpointRenderer != null)
            {
                Transform endpoint =
                    aimEndpointRenderer.transform;
                endpoint.position = aimDestination;
                float pulse =
                    0.9f +
                    0.1f *
                    Mathf.Sin(Time.unscaledTime * 7f);
                endpoint.localScale =
                    Vector3.one *
                    AimEndpointSize *
                    pulse;
            }
        }

        private void SetAimVisualsVisible(bool visible)
        {
            if (aimRayRenderer != null)
            {
                aimRayRenderer.enabled = visible;
            }

            if (aimEndpointRenderer != null)
            {
                aimEndpointRenderer.enabled = visible;
            }
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
