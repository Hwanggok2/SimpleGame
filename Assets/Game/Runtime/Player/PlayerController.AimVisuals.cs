using UnityEngine;

namespace SimpleGame
{
    public sealed partial class PlayerController
    {
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
            if (controlMode ==
                MobileControlMode.DirectMoveAutoAim)
            {
                EnemyBase lockedTarget = ResolveLockedEnemy();
                rawAimDestination = lockedTarget != null
                    ? lockedTarget.transform.position
                    : playerPosition;
                aimDestination = rawAimDestination;
                SetAimAssistEnemy(null);
                if (lockedTarget == null)
                {
                    SetAimVisualsVisible(false);
                    return;
                }

                DrawAimLine(playerPosition, aimDestination);
                return;
            }

            if (!isAiming ||
                worldCamera == null)
            {
                rawAimDestination = playerPosition;
                aimDestination = playerPosition;
                SetAimAssistEnemy(null);
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
            rawAimDestination = CalculateAimPoint(
                playerPosition,
                aimInput,
                maximumDistance);
            EnemyBase assistedEnemy =
                HasCommandAim(aimInput) && enemyWorld != null
                    ? enemyWorld.FindAimAssistTarget(
                        playerPosition,
                        rawAimDestination,
                        AimAssistHalfWidth,
                        ResolveAimAssistEnemy(),
                        AimAssistRetentionWidthMultiplier)
                    : null;
            SetAimAssistEnemy(assistedEnemy);
            aimDestination = assistedEnemy != null
                ? assistedEnemy.transform.position
                : rawAimDestination;
            DrawAimLine(playerPosition, aimDestination);
        }

        private void DrawAimLine(
            Vector2 playerPosition,
            Vector2 targetPosition)
        {
            SetAimVisualsVisible(true);

            Vector2 offset =
                targetPosition - playerPosition;
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
                if (aimRayRenderer.drawMode ==
                    SpriteDrawMode.Tiled)
                {
                    ray.localScale = Vector3.one;
                    aimRayRenderer.size = new Vector2(
                        length,
                        AimRayWidth);
                }
                else
                {
                    ray.localScale = new Vector3(
                        length,
                        AimRayWidth,
                        1f);
                }
            }

            if (aimEndpointRenderer != null)
            {
                Transform endpoint =
                    aimEndpointRenderer.transform;
                endpoint.position = targetPosition;
                float pulse =
                    0.9f +
                    0.1f *
                    Mathf.Sin(Time.unscaledTime * 7f);
                endpoint.localScale =
                    Vector3.one *
                    AimEndpointSize *
                    pulse;
                endpoint.rotation = Quaternion.identity;
            }
        }

        private void RefreshCommandMarkerVisuals()
        {
            if (!commandMarkerVisible)
            {
                return;
            }

            Vector2 markerPosition = commandMarkerDestination;
            if (pendingEnemy != null && pendingEnemy.IsAlive)
            {
                markerPosition = pendingEnemy.transform.position;
            }

            if (commandEndpointRenderer != null)
            {
                Transform endpoint = commandEndpointRenderer.transform;
                endpoint.position = markerPosition;
                endpoint.rotation = Quaternion.identity;
            }

            if (commandArrowRenderer != null)
            {
                Transform arrow = commandArrowRenderer.transform;
                arrow.position = markerPosition;
                Vector2 direction =
                    markerPosition - (Vector2)transform.position;
                arrow.rotation = direction.sqrMagnitude > 0.0001f
                    ? Quaternion.Euler(
                        0f,
                        0f,
                        Mathf.Atan2(direction.y, direction.x) *
                        Mathf.Rad2Deg)
                    : Quaternion.identity;
            }
        }

        private void ShowCommandMarker(Vector2 markerDestination)
        {
            commandMarkerDestination = markerDestination;
            commandMarkerVisible = true;
            if (commandEndpointRenderer != null)
            {
                commandEndpointRenderer.enabled = true;
            }

            if (commandArrowRenderer != null)
            {
                commandArrowRenderer.enabled = true;
            }

            RefreshCommandMarkerVisuals();
        }

        private void HideCommandMarker()
        {
            commandMarkerVisible = false;
            if (commandEndpointRenderer != null)
            {
                commandEndpointRenderer.enabled = false;
            }

            if (commandArrowRenderer != null)
            {
                commandArrowRenderer.enabled = false;
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

        private EnemyBase ResolveAimAssistEnemy()
        {
            if (aimAssistEnemy == null ||
                !aimAssistEnemy.IsAlive ||
                aimAssistEnemy.SpawnGeneration !=
                    aimAssistEnemyGeneration)
            {
                SetAimAssistEnemy(null);
            }

            return aimAssistEnemy;
        }

        private void SetAimAssistEnemy(EnemyBase enemy)
        {
            aimAssistEnemy = enemy;
            aimAssistEnemyGeneration =
                enemy != null ? enemy.SpawnGeneration : 0u;
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
    }
}

