using UnityEngine;

namespace SimpleGame
{
    public sealed partial class PlayerController
    {
        public static bool HasClearedSeverPass(
            Vector2 playerPosition,
            Vector2 targetPosition,
            Vector2 passDirection,
            float clearance)
        {
            if (passDirection.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            Vector2 direction = passDirection.normalized;
            Vector2 targetOffset = playerPosition - targetPosition;
            float lateralDistance = Mathf.Abs(
                direction.x * targetOffset.y -
                direction.y * targetOffset.x);
            return lateralDistance <=
                    Mathf.Max(0f, clearance) +
                    MovementPassLateralPadding &&
                Vector2.Dot(targetOffset, direction) >=
                    Mathf.Max(0f, clearance);
        }

        private void BeginPathSever(EnemyBase target)
        {
            CancelPathSever();
            if (target == null ||
                root == null ||
                root.CombatAbilities == null ||
                !root.CombatAbilities.HasSever)
            {
                return;
            }

            pathSeverStartPosition = transform.position;
            pathSeverTargetPosition = target.transform.position;
            pathSeverDirection =
                destination - pathSeverStartPosition;
            if (pathSeverDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            pathSeverDirection.Normalize();
            pathSeverClearance =
                EnemyWorldService.GetColliderRadius(root) +
                EnemyWorldService.GetColliderRadius(target);
            pathSeverPending = true;
        }

        private void UpdatePathSever()
        {
            if (!pathSeverPending ||
                !HasClearedSeverPass(
                    transform.position,
                    pathSeverTargetPosition,
                    pathSeverDirection,
                    pathSeverClearance))
            {
                return;
            }

            Vector2 startPosition = pathSeverStartPosition;
            CancelPathSever();
            root?.CombatAbilities
                ?.TryTriggerSeverForCompletedMovementPierce(
                    startPosition);
        }

        private void CancelPathSever()
        {
            pathSeverPending = false;
            pathSeverStartPosition = Vector2.zero;
            pathSeverTargetPosition = Vector2.zero;
            pathSeverDirection = Vector2.zero;
            pathSeverClearance = 0f;
        }
    }
}
