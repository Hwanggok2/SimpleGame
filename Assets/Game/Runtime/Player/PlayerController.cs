using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SimpleGame
{
    public sealed partial class PlayerController : MonoBehaviour
    {
        public const float DefaultAttackRange = 0.72f;
        public const float EnemyPiercingHorizontalRadius = 1.5f;
        public const float EnemyPiercingVerticalRadius = 2f;
        public const float AimViewportPadding = 0.5f;
        public const float AimRayWidth = 0.08f;
        public const float AimEndpointSize = 0.42f;
        public const float AutoAttackInterval = 0.3f;
        public const float ModeOneEngagementInset = 0.02f;
        public const float AimAssistHalfWidth = 0.65f;
        public const float AimAssistRetentionWidthMultiplier = 1.35f;
        public const float MinimumCommandAimMagnitude = 0.01f;
        private const float EnemySelectionRadius =
            EnemyPiercingVerticalRadius;
        private const float MovementPassDirectionThreshold = 0.25f;
        private const float MovementPassLateralPadding = 0.05f;

        [SerializeField] private SpriteRenderer aimRayRenderer;
        [SerializeField] private SpriteRenderer aimEndpointRenderer;
        [SerializeField] private SpriteRenderer commandEndpointRenderer;
        [SerializeField] private SpriteRenderer commandArrowRenderer;

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
        private Vector2 rawAimDestination;
        private Vector2 aimDestination;
        private EnemyBase aimAssistEnemy;
        private uint aimAssistEnemyGeneration;
        private EnemyBase autoAttackEnemy;
        private uint autoAttackEnemyGeneration;
        private float nextAutoAttackAt;
        private EnemyBase lockedEnemy;
        private uint lockedEnemyGeneration;
        private bool lockedEnemyAllowsMovementPiercing;
        private float nextModeOneAttackAt;
        private Vector2 movementInput;
        private int remainingMovementPierces;
        private float movementPiercingRechargeAt =
            float.PositiveInfinity;
        private EnemyBase modeOnePassEnemy;
        private uint modeOnePassEnemyGeneration;
        private Vector2 modeOnePassDirection;
        private Vector2 modeOnePassStartPosition;
        private bool pathSeverPending;
        private Vector2 pathSeverStartPosition;
        private Vector2 pathSeverTargetPosition;
        private Vector2 pathSeverDirection;
        private float pathSeverClearance;
        private Vector2 commandMarkerDestination;
        private MobileControlMode controlMode =
            MobileControlMode.AimCommand;
        private bool autoAttackEnabled;
        private bool autoAttackRepeatCommandActive;
        private bool manualMovementHeld;
        private bool modeOneLockedCommandActive;
        private bool modeOneAttackPending;
        private bool commandMarkerVisible;
        private bool isAiming;
        private float autoAttackSpeedMultiplier = 1f;
        private readonly List<RaycastResult> uiRaycastResults = new();
        private readonly Dictionary<EnemyBase, uint>
            movementPiercedEnemyGenerations = new();

        public Vector2 AimInput => aimInput;
        public Vector2 RawAimDestination => rawAimDestination;
        public Vector2 AimDestination => aimDestination;
        public bool IsAiming => isAiming;
        public bool AutoAttackEnabled => autoAttackEnabled;
        public MobileControlMode ControlMode => controlMode;
        public bool ManualMovementHeld => manualMovementHeld;

        public void ConfigureAimVisuals(
            SpriteRenderer configuredRayRenderer,
            SpriteRenderer configuredEndpointRenderer,
            SpriteRenderer configuredCommandEndpointRenderer,
            SpriteRenderer configuredCommandArrowRenderer)
        {
            aimRayRenderer = configuredRayRenderer;
            aimEndpointRenderer = configuredEndpointRenderer;
            commandEndpointRenderer =
                configuredCommandEndpointRenderer;
            commandArrowRenderer = configuredCommandArrowRenderer;
            SetAimVisualsVisible(false);
            HideCommandMarker();
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

        public void SetAutoAttackSpeedMultiplier(float value)
        {
            autoAttackSpeedMultiplier = Mathf.Max(0.1f, value);
        }

        private float ResolvedAutoAttackInterval =>
            AutoAttackInterval / autoAttackSpeedMultiplier;

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
            bool hasManualMovementInput =
                HasActiveManualMovementInput();
            if (controlMode ==
                    MobileControlMode.DirectMoveAutoAim &&
                hasManualMovementInput)
            {
                if (!root.IsInputLocked)
                {
                    if (hasDestination)
                    {
                        CancelCommand();
                    }

                    EnemyBase pathTarget =
                        FindModeOneMovementPathTarget();
                    TickModeOneRangeAttackAgainst(pathTarget);
                    if (!root.IsInputLocked)
                    {
                        TickManualMovementTowards(pathTarget);
                    }
                }

                return;
            }

            TickCommand();
            if (controlMode ==
                MobileControlMode.DirectMoveAutoAim)
            {
                TickModeOneLockedTarget();
            }
            else
            {
                TickAutoAttack();
            }
        }

        private void LateUpdate()
        {
            RefreshAimVisuals();
            RefreshCommandMarkerVisuals();
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
            return TryIssueCommand(
                worldDestination,
                null,
                true);
        }

        private bool TryIssueCommand(
            Vector2 worldDestination,
            EnemyBase preferredEnemy,
            bool userInitiated,
            bool interceptPathEnemies = true,
            bool showCommandMarker = true,
            bool lockedTargetCommand = false,
            bool autoAttackRepeatCommand = false)
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

            bool replacesPendingAutoAttack =
                userInitiated && autoAttackRepeatCommandActive;
            modeOneLockedCommandActive = lockedTargetCommand;
            autoAttackRepeatCommandActive =
                autoAttackRepeatCommand;
            destination = worldDestination;
            commandOrigin = transform.position;
            SetIgnoredPathEnemy(null);
            if (userInitiated)
            {
                BeginMovementPiercingCommand();
            }

            EnemyBase directEnemy =
                preferredEnemy != null && preferredEnemy.IsAlive
                    ? preferredEnemy
                    : enemyWorld.FindEnemyNear(
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
            EnemyBase pathEnemy = interceptPathEnemies
                ? enemyWorld.FindFirstEnemyOnPath(
                    commandOrigin,
                    movementTarget,
                    EnemyWorldService.GetColliderRadius(root))
                : null;
            bool interceptedOnPath =
                pathEnemy != null && pathEnemy != directEnemy;
            EnemyBase selectedEnemy =
                SelectCommandEnemy(directEnemy, pathEnemy);
            if (userInitiated)
            {
                if (controlMode ==
                    MobileControlMode.DirectMoveAutoAim)
                {
                    SetLockedEnemy(
                        selectedEnemy,
                        selectedEnemy != null);
                }
                else
                {
                    SetAutoAttackTarget(selectedEnemy);
                }
            }

            if (showCommandMarker)
            {
                ShowCommandMarker(destination);
            }
            else
            {
                HideCommandMarker();
            }
            if (selectedEnemy != null &&
                hasDestination &&
                pendingEnemy == selectedEnemy)
            {
                if (lockedTargetCommand)
                {
                    pendingAttackCount = 1;
                }
                else if (!shieldApproachOnly)
                {
                    pendingAttackCount = replacesPendingAutoAttack
                        ? 1
                        : pendingAttackCount + 1;
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
            BeginCurrentMove();
            return true;
        }

        private void TickCommand()
        {
            if (!hasDestination)
            {
                return;
            }

            if (autoAttackRepeatCommandActive &&
                controlMode == MobileControlMode.AimCommand &&
                ResolveAutoAttackEnemy() == null)
            {
                CancelCommand();
                return;
            }

            if (modeOneLockedCommandActive &&
                (ResolveLockedEnemy() == null ||
                 pendingEnemy != lockedEnemy))
            {
                CancelCommand();
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
                    UpdatePathSever();
                    hasDestination = !reachedDestination;
                    if (reachedDestination)
                    {
                        SetIgnoredPathEnemy(null);
                        postKillEscapeActive = false;
                        autoAttackRepeatCommandActive = false;
                        HideCommandMarker();
                    }

                    return;
                }

                pendingAttackCount = 1;
                shieldApproachOnly = false;
                BeginCurrentMove();
            }

            float stoppingDistance = shieldApproachOnly
                ? pendingEnemy.Definition.ApproachRange
                : controlMode ==
                    MobileControlMode.DirectMoveAutoAim
                        ? GetModeOneEngagementRadius(pendingEnemy)
                        : attackRange;
            bool reached = root.Movement.StepTowards(
                pendingEnemy.transform.position,
                stoppingDistance,
                true);
            UpdatePathSever();
            if (!reached)
            {
                return;
            }

            if (shieldApproachOnly)
            {
                session.ShowHint(session.GetString(
                    GameStringIds.HintShieldInside,
                    "방패병의 안쪽에 도착했습니다. 다시 누르면 " +
                    "근접 공격합니다."));
                CancelCommand();
                return;
            }

            EnemyBase targetEnemy = pendingEnemy;
            bool movementPiercingRequested =
                IsPiercingTouchRequested(
                commandOrigin,
                targetEnemy.transform.position,
                destination);
            bool movementPiercingAvailable =
                movementPiercingRequested &&
                HasRemainingMovementPierces();
            bool attackExecuted = false;
            PlayerAttackExecution lastExecution = default;
            while (pendingAttackCount > 0 && targetEnemy.IsAlive)
            {
                pendingAttackCount--;
                attackExecuted = true;
                if (targetEnemy == ResolveAutoAttackEnemy())
                {
                    nextAutoAttackAt =
                        Time.time + ResolvedAutoAttackInterval;
                }

                if (targetEnemy == ResolveLockedEnemy())
                {
                    modeOneAttackPending = false;
                    nextModeOneAttackAt =
                        Time.time + ResolvedAutoAttackInterval;
                }

                if (ExecuteSingleAttack(
                        targetEnemy,
                        out lastExecution) ==
                    PlayerAttackReaction.Recoil)
                {
                    CancelCommand();
                    return;
                }
            }

            bool defeated = !targetEnemy.IsAlive;
            bool movementPiercingConsumed =
                !defeated &&
                attackExecuted &&
                movementPiercingAvailable &&
                lastExecution.PiercingAllowed &&
                TryConsumeMovementPierce();
            if (movementPiercingConsumed)
            {
                BeginPathSever(targetEnemy);
            }
            if (!ShouldContinueAfterPathAttack(
                defeated,
                movementPiercingConsumed,
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

        private PlayerAttackReaction ExecuteSingleAttack(
            EnemyBase targetEnemy,
            out PlayerAttackExecution execution)
        {
            bool critical = root.Critical.Roll();
            GameManager.Instance.PlaySoundEffect(
                GameAudioIds.PlayerAttack);
            root.PlayAttack(targetEnemy.transform.position);
            execution = root.AttackEnemy(
                targetEnemy,
                critical,
                root.CombatAbilities.PiercingLevel > 0);
            RetainModeOnePrimaryTarget(
                targetEnemy,
                execution.PiercingAllowed);

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
                session.ShowHint(session.GetString(
                    GameStringIds.HintShieldBypassSuccess,
                    "방패 우회 성공! 반동과 조작 불가를 " +
                    "무시했습니다."));
            }

            if (effectiveReaction == PlayerAttackReaction.Recoil)
            {
                root.ApplyFrontRecoil(targetEnemy.transform.position);
            }

            session.PlayCombatFeedback(
                execution.AnyDamageApplied,
                !targetEnemy.IsAlive,
                critical,
                effectiveReaction);
            return effectiveReaction;
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

        public bool BeginControlInput()
        {
            if (controlMode == MobileControlMode.AimCommand)
            {
                return BeginAim();
            }

            if (root == null ||
                session == null ||
                !session.IsPlaying ||
                !root.IsAlive ||
                root.IsInputLocked)
            {
                return false;
            }

            manualMovementHeld = true;
            movementInput = Vector2.zero;
            CancelCommand();
            BeginMovementPiercingCommand();
            return true;
        }

        public void SetControlInput(Vector2 normalizedInput)
        {
            if (controlMode == MobileControlMode.AimCommand)
            {
                SetAimInput(normalizedInput);
                return;
            }

            if (!manualMovementHeld)
            {
                return;
            }

            movementInput = Vector2.ClampMagnitude(
                normalizedInput,
                1f);
        }

        public void EndControlInput()
        {
            if (controlMode == MobileControlMode.AimCommand)
            {
                EndAim();
                return;
            }

            manualMovementHeld = false;
            movementInput = Vector2.zero;
            EndMovementPiercingCommand();
            root?.Movement.CancelMove();
        }

        public bool ExecuteControlAction()
        {
            return controlMode ==
                    MobileControlMode.DirectMoveAutoAim
                ? LockNearestVisibleEnemy()
                : ExecuteAimedCommand();
        }

        public void SetControlMode(MobileControlMode mode)
        {
            MobileControlMode resolvedMode = mode ==
                MobileControlMode.DirectMoveAutoAim
                    ? MobileControlMode.DirectMoveAutoAim
                    : MobileControlMode.AimCommand;
            if (controlMode == resolvedMode)
            {
                return;
            }

            CancelCommand();
            EndAim();
            manualMovementHeld = false;
            movementInput = Vector2.zero;
            EndMovementPiercingCommand();
            SetAutoAttackTarget(null);
            SetLockedEnemy(null, false);
            controlMode = resolvedMode;
            RefreshAimVisuals();
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
            SetAimAssistEnemy(null);
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
            rawAimDestination = transform.position;
            aimDestination = transform.position;
            SetAimAssistEnemy(null);
            SetAimVisualsVisible(false);
        }

        public bool ExecuteAimedCommand()
        {
            if (!isAiming ||
                !HasCommandAim(aimInput))
            {
                return false;
            }

            EnemyBase commandEnemy = enemyWorld != null
                ? enemyWorld.FindAimAssistTarget(
                    transform.position,
                    rawAimDestination,
                    AimAssistHalfWidth,
                    ResolveAimAssistEnemy(),
                    AimAssistRetentionWidthMultiplier)
                : null;
            SetAimAssistEnemy(commandEnemy);
            Vector2 commandDestination = commandEnemy != null
                ? commandEnemy.transform.position
                : rawAimDestination;
            aimDestination = commandDestination;
            return TryIssueCommand(
                commandDestination,
                commandEnemy,
                true);
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
            modeOneLockedCommandActive = false;
            autoAttackRepeatCommandActive = false;
            CancelPathSever();
            root?.Movement.CancelMove();
            HideCommandMarker();
        }

        public void SetAutoAttackEnabled(bool enabled)
        {
            autoAttackEnabled = enabled;
            if (!enabled)
            {
                if (autoAttackRepeatCommandActive)
                {
                    CancelCommand();
                }

                SetAutoAttackTarget(null);
            }
            else if (controlMode ==
                     MobileControlMode.DirectMoveAutoAim &&
                     ResolveLockedEnemy() != null)
            {
                nextModeOneAttackAt = Time.time;
            }
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
            bool movementPiercingConsumed,
            bool attackExecuted)
        {
            return targetDefeated ||
                (movementPiercingConsumed && attackExecuted);
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
