using UnityEngine;

namespace SimpleGame
{
    public sealed partial class PlayerController
    {
        private bool HasActiveManualMovementInput()
        {
            return manualMovementHeld &&
                HasCommandAim(movementInput);
        }

        private EnemyBase FindModeOneMovementPathTarget()
        {
            if (enemyWorld == null ||
                root == null ||
                !HasCommandAim(movementInput))
            {
                return null;
            }

            float lookaheadDistance = Mathf.Max(
                attackRange,
                attackRange +
                root.MoveSpeed * Mathf.Max(0f, Time.deltaTime));
            Vector2 direction = movementInput.normalized;
            return enemyWorld.FindFirstEnemyOnPath(
                transform.position,
                (Vector2)transform.position +
                    direction * lookaheadDistance,
                EnemyWorldService.GetColliderRadius(root),
                null,
                movementPiercedEnemyGenerations);
        }

        private void TickManualMovementTowards(
            EnemyBase pathTarget)
        {
            RefreshMovementPiercingBudget();
            EnemyBase lockedTarget = ResolveLockedEnemy();
            EnemyBase target = pathTarget != null &&
                    pathTarget.IsAlive &&
                    !HasPassedEnemyDuringCurrentMovement(pathTarget)
                ? pathTarget
                : lockedTarget;
            if (target == null ||
                HasPassedEnemyDuringCurrentMovement(target))
            {
                CancelModeOnePassCandidate();
                root.Movement.StepInDirection(movementInput);
                return;
            }

            bool canStartPass =
                target == lockedTarget &&
                lockedEnemyAllowsMovementPiercing &&
                HasRemainingMovementPierces() &&
                CanStartModeOnePass(
                    transform.position,
                    target.transform.position,
                    movementInput,
                    EnemyWorldService.GetColliderRadius(root) +
                    EnemyWorldService.GetColliderRadius(target));
            if (modeOnePassEnemy == null && canStartPass)
            {
                modeOnePassEnemy = target;
                modeOnePassEnemyGeneration =
                    target.SpawnGeneration;
                modeOnePassDirection = movementInput.normalized;
                modeOnePassStartPosition = transform.position;
            }

            if (ResolveModeOnePassEnemy() == target)
            {
                root.Movement.StepInDirection(movementInput);
                UpdateModeOnePass();
                return;
            }

            root.Movement.StepInDirectionAroundCircle(
                movementInput,
                target.transform.position,
                GetModeOneEngagementRadius(target));
        }

        private void TickModeOneRangeAttack()
        {
            TickModeOneRangeAttackAgainst(
                FindModeOneMovementPathTarget());
        }

        private void TickModeOneRangeAttackAgainst(
            EnemyBase pathTarget)
        {
            if (!HasCommandAim(movementInput) ||
                Time.time < nextModeOneAttackAt)
            {
                return;
            }

            EnemyBase lockedTarget = ResolveLockedEnemy();
            EnemyBase target = IsEnemyInAttackRange(pathTarget)
                ? pathTarget
                : null;
            if (target == null &&
                IsEnemyInAttackRange(lockedTarget))
            {
                target = lockedTarget;
            }

            if (target == null)
            {
                target = enemyWorld.FindEnemyNear(
                    transform.position,
                    attackRange);
            }

            if (target == null)
            {
                return;
            }

            nextModeOneAttackAt =
                Time.time + ResolvedAutoAttackInterval;
            if (target == lockedTarget)
            {
                modeOneAttackPending = false;
            }

            ExecuteSingleAttack(
                target,
                out _);
        }

        private bool IsEnemyInAttackRange(EnemyBase enemy)
        {
            return enemy != null &&
                enemy.IsAlive &&
                Vector2.Distance(
                    transform.position,
                    enemy.transform.position) <= attackRange;
        }

        private float GetModeOneEngagementRadius(EnemyBase enemy)
        {
            if (enemy == null || enemy.Definition == null)
            {
                return CalculateModeOneEngagementRadius(
                    attackRange,
                    EnemyArchetype.Shield,
                    0f);
            }

            return CalculateModeOneEngagementRadius(
                attackRange,
                enemy.Archetype,
                enemy.Definition.AttackRange);
        }

        public static float CalculateModeOneEngagementRadius(
            float playerAttackRange,
            EnemyArchetype enemyArchetype,
            float enemyAttackRange)
        {
            float playerRange = Mathf.Max(
                0f,
                playerAttackRange);
            float playerInset = Mathf.Min(
                ModeOneEngagementInset,
                playerRange * 0.1f);
            float safePlayerRange = Mathf.Max(
                0f,
                playerRange - playerInset);
            if ((enemyArchetype != EnemyArchetype.Melee &&
                 enemyArchetype != EnemyArchetype.Ranged) ||
                enemyAttackRange <= 0f)
            {
                return safePlayerRange;
            }

            float safeEnemyRange = Mathf.Max(
                0f,
                enemyAttackRange);
            float inset = Mathf.Min(
                ModeOneEngagementInset,
                safeEnemyRange * 0.1f);
            return Mathf.Min(
                safePlayerRange,
                safeEnemyRange - inset);
        }

        private void TickModeOneLockedTarget()
        {
            EnemyBase target = ResolveLockedEnemy();
            if (!ShouldStartModeOneLockedTargetCommand(
                    HasActiveManualMovementInput(),
                    hasDestination,
                    target != null,
                    autoAttackEnabled,
                    modeOneAttackPending,
                    Time.time >= nextModeOneAttackAt))
            {
                return;
            }

            bool autoAttackRepeatCommand =
                autoAttackEnabled && !modeOneAttackPending;
            TryIssueCommand(
                target.transform.position,
                target,
                false,
                false,
                false,
                true,
                autoAttackRepeatCommand);
        }

        public static bool ShouldStartModeOneLockedTargetCommand(
            bool manualInputHeld,
            bool commandActive,
            bool hasTarget,
            bool autoAttackEnabled,
            bool oneShotPending,
            bool intervalElapsed)
        {
            return !manualInputHeld &&
                !commandActive &&
                hasTarget &&
                (autoAttackEnabled || oneShotPending) &&
                intervalElapsed;
        }

        private bool LockNearestVisibleEnemy()
        {
            if (root == null ||
                session == null ||
                enemyWorld == null ||
                worldCamera == null ||
                !session.IsPlaying ||
                !root.IsAlive ||
                root.IsInputLocked)
            {
                return false;
            }

            float halfHeight = worldCamera.orthographicSize;
            Rect visibleBounds = CalculateVisibleWorldBounds(
                worldCamera.transform.position,
                halfHeight,
                worldCamera.aspect);
            EnemyBase target =
                enemyWorld.FindNearestLivingEnemyInBounds(
                    transform.position,
                    visibleBounds);
            SetLockedEnemy(target, target != null);
            if (target == null)
            {
                CancelCommand();
                return false;
            }

            if (HasActiveManualMovementInput())
            {
                return true;
            }

            return TryIssueCommand(
                target.transform.position,
                target,
                false,
                false,
                false,
                true);
        }

        public static Rect CalculateVisibleWorldBounds(
            Vector2 cameraCenter,
            float orthographicHalfHeight,
            float aspect)
        {
            float halfHeight = Mathf.Max(
                0f,
                orthographicHalfHeight);
            float halfWidth = halfHeight * Mathf.Max(0f, aspect);
            return Rect.MinMaxRect(
                cameraCenter.x - halfWidth,
                cameraCenter.y - halfHeight,
                cameraCenter.x + halfWidth,
                cameraCenter.y + halfHeight);
        }

        private void TickAutoAttack()
        {
            EnemyBase target = ResolveAutoAttackEnemy();
            if (!autoAttackEnabled ||
                target == null ||
                Time.time < nextAutoAttackAt ||
                (hasDestination && pendingEnemy == target))
            {
                return;
            }

            TryIssueCommand(
                target.transform.position,
                target,
                false,
                autoAttackRepeatCommand: true);
        }

        private EnemyBase ResolveAutoAttackEnemy()
        {
            if (autoAttackEnemy == null ||
                !autoAttackEnemy.IsAlive ||
                autoAttackEnemy.SpawnGeneration !=
                    autoAttackEnemyGeneration)
            {
                SetAutoAttackTarget(null);
            }

            return autoAttackEnemy;
        }

        private void SetAutoAttackTarget(EnemyBase enemy)
        {
            autoAttackEnemy =
                autoAttackEnabled && enemy != null && enemy.IsAlive
                    ? enemy
                    : null;
            autoAttackEnemyGeneration = autoAttackEnemy != null
                ? autoAttackEnemy.SpawnGeneration
                : 0u;
            nextAutoAttackAt = autoAttackEnemy != null
                ? Time.time + ResolvedAutoAttackInterval
                : 0f;
        }

        private EnemyBase ResolveLockedEnemy()
        {
            if (lockedEnemy == null)
            {
                return null;
            }

            if (!lockedEnemy.IsAlive ||
                lockedEnemy.SpawnGeneration != lockedEnemyGeneration)
            {
                lockedEnemy = null;
                lockedEnemyGeneration = 0u;
                lockedEnemyAllowsMovementPiercing = false;
                modeOneAttackPending = false;
                nextModeOneAttackAt = 0f;
                CancelModeOnePassCandidate();
            }

            return lockedEnemy;
        }

        private void SetLockedEnemy(
            EnemyBase enemy,
            bool attackPending)
        {
            CancelModeOnePassCandidate();
            lockedEnemy = enemy != null && enemy.IsAlive
                ? enemy
                : null;
            lockedEnemyGeneration = lockedEnemy != null
                ? lockedEnemy.SpawnGeneration
                : 0u;
            lockedEnemyAllowsMovementPiercing = false;
            modeOneAttackPending =
                lockedEnemy != null && attackPending;
            nextModeOneAttackAt = lockedEnemy != null
                ? Time.time
                : 0f;
            RefreshAimVisuals();
        }

        private void RetainModeOnePrimaryTarget(
            EnemyBase enemy,
            bool movementPiercingAllowed)
        {
            if (controlMode !=
                MobileControlMode.DirectMoveAutoAim)
            {
                return;
            }

            EnemyBase retained = enemy != null && enemy.IsAlive
                ? enemy
                : null;
            if (lockedEnemy != retained ||
                (retained != null &&
                 lockedEnemyGeneration != retained.SpawnGeneration))
            {
                CancelModeOnePassCandidate();
            }

            lockedEnemy = retained;
            lockedEnemyGeneration = retained != null
                ? retained.SpawnGeneration
                : 0u;
            lockedEnemyAllowsMovementPiercing =
                retained != null && movementPiercingAllowed;
            modeOneAttackPending = false;
            RefreshAimVisuals();
        }

        private void BeginMovementPiercingCommand()
        {
            remainingMovementPierces = root != null &&
                root.CombatAbilities != null
                    ? root.CombatAbilities.PiercingLevel
                    : 0;
            movementPiercingRechargeAt =
                float.PositiveInfinity;
            movementPiercedEnemyGenerations.Clear();
            CancelModeOnePassCandidate();
        }

        private void EndMovementPiercingCommand()
        {
            remainingMovementPierces = 0;
            movementPiercingRechargeAt =
                float.PositiveInfinity;
            movementPiercedEnemyGenerations.Clear();
            CancelModeOnePassCandidate();
        }

        private bool HasRemainingMovementPierces()
        {
            RefreshMovementPiercingBudget();
            return remainingMovementPierces > 0 &&
                root != null &&
                root.CombatAbilities != null &&
                root.CombatAbilities.PiercingLevel > 0;
        }

        private bool TryConsumeMovementPierce()
        {
            if (!HasRemainingMovementPierces())
            {
                return false;
            }

            remainingMovementPierces--;
            if (remainingMovementPierces <= 0)
            {
                movementPiercingRechargeAt =
                    Time.time +
                    PlayerCombatAbilities.PiercingWindowDuration;
            }

            return true;
        }

        private void RefreshMovementPiercingBudget()
        {
            if (!ShouldRefreshMovementPiercingBudget(
                    remainingMovementPierces,
                    Time.time,
                    movementPiercingRechargeAt))
            {
                return;
            }

            int currentLevel = root != null &&
                root.CombatAbilities != null
                    ? root.CombatAbilities.PiercingLevel
                    : 0;
            remainingMovementPierces = Mathf.Max(
                0,
                currentLevel);
            movementPiercingRechargeAt =
                float.PositiveInfinity;
        }

        public static bool ShouldRefreshMovementPiercingBudget(
            int remainingPierces,
            float currentTime,
            float rechargeAt)
        {
            return remainingPierces <= 0 &&
                !float.IsInfinity(rechargeAt) &&
                currentTime >= rechargeAt;
        }

        private bool HasPassedEnemyDuringCurrentMovement(
            EnemyBase enemy)
        {
            if (enemy == null ||
                !movementPiercedEnemyGenerations.TryGetValue(
                    enemy,
                    out uint generation))
            {
                return false;
            }

            if (generation == enemy.SpawnGeneration)
            {
                return true;
            }

            movementPiercedEnemyGenerations.Remove(enemy);
            return false;
        }

        public static bool CanStartModeOnePass(
            Vector2 playerPosition,
            Vector2 targetPosition,
            Vector2 movement,
            float combinedCollisionRadius)
        {
            if (movement.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            Vector2 direction = movement.normalized;
            Vector2 targetOffset = targetPosition - playerPosition;
            float distanceAhead = Vector2.Dot(
                targetOffset,
                direction);
            if (distanceAhead <= 0f)
            {
                return false;
            }

            float lateralDistance = Mathf.Abs(
                direction.x * targetOffset.y -
                direction.y * targetOffset.x);
            return lateralDistance <=
                Mathf.Max(0f, combinedCollisionRadius);
        }

        private EnemyBase ResolveModeOnePassEnemy()
        {
            if (modeOnePassEnemy == null ||
                !modeOnePassEnemy.IsAlive ||
                modeOnePassEnemy.SpawnGeneration !=
                    modeOnePassEnemyGeneration)
            {
                CancelModeOnePassCandidate();
                return null;
            }

            if (movementInput.sqrMagnitude > 0.0001f &&
                Vector2.Dot(
                    movementInput.normalized,
                    modeOnePassDirection) <
                    MovementPassDirectionThreshold)
            {
                CancelModeOnePassCandidate();
                return null;
            }

            return modeOnePassEnemy;
        }

        private void UpdateModeOnePass()
        {
            EnemyBase target = ResolveModeOnePassEnemy();
            if (target == null)
            {
                return;
            }

            Vector2 targetOffset =
                (Vector2)transform.position -
                (Vector2)target.transform.position;
            float lateralDistance = Mathf.Abs(
                modeOnePassDirection.x * targetOffset.y -
                modeOnePassDirection.y * targetOffset.x);
            float combinedRadius =
                EnemyWorldService.GetColliderRadius(root) +
                EnemyWorldService.GetColliderRadius(target);
            if (lateralDistance >
                combinedRadius + MovementPassLateralPadding)
            {
                CancelModeOnePassCandidate();
                return;
            }

            float clearedDistance = Vector2.Dot(
                targetOffset,
                modeOnePassDirection);
            if (clearedDistance < combinedRadius)
            {
                return;
            }

            if (TryConsumeMovementPierce())
            {
                movementPiercedEnemyGenerations[target] =
                    target.SpawnGeneration;
                root.CombatAbilities
                    .TryTriggerSeverForCompletedMovementPierce(
                        modeOnePassStartPosition);
            }

            CancelModeOnePassCandidate();
        }

        private void CancelModeOnePassCandidate()
        {
            modeOnePassEnemy = null;
            modeOnePassEnemyGeneration = 0u;
            modeOnePassDirection = Vector2.zero;
            modeOnePassStartPosition = Vector2.zero;
        }
    }
}
