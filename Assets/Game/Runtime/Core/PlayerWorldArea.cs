using UnityEngine;

namespace SimpleGame
{
    public sealed class PlayerWorldArea : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField, Min(0f)] private float spawnPadding = 2f;
        [SerializeField, Min(0f)] private float recyclePadding = 6f;
        [SerializeField, Min(0f)] private float spawnJitter = 1.5f;

        public void Configure(Camera camera)
        {
            worldCamera = camera;
        }

        public bool IsOutsideRecycleArea(Vector2 worldPosition)
        {
            Vector2 offset = worldPosition - (Vector2)transform.position;
            Vector2 extents = GetRecycleExtents();
            return Mathf.Abs(offset.x) > extents.x ||
                Mathf.Abs(offset.y) > extents.y;
        }

        public Vector2 GetOppositeSpawnPosition(Vector2 currentPosition)
        {
            return CalculateOppositeSpawnPosition(
                transform.position,
                currentPosition,
                GetSpawnExtents(),
                Random.Range(-spawnJitter, spawnJitter));
        }

        public Vector2 GetOutwardSpawnPosition(Vector2 currentPosition)
        {
            return CalculateOutwardSpawnPosition(
                transform.position,
                currentPosition,
                GetSpawnExtents(),
                Random.Range(-spawnJitter, spawnJitter));
        }

        public Vector2 GetSpawnExtents()
        {
            Vector2 cameraExtents = GetCameraExtents();
            return cameraExtents + Vector2.one * spawnPadding;
        }

        public Vector2 GetRecycleExtents()
        {
            Vector2 spawnExtents = GetSpawnExtents();
            return new Vector2(
                Mathf.Max(
                    spawnExtents.x + 0.1f,
                    GetCameraExtents().x + recyclePadding),
                Mathf.Max(
                    spawnExtents.y + 0.1f,
                    GetCameraExtents().y + recyclePadding));
        }

        public static Vector2 CalculateOppositeSpawnPosition(
            Vector2 center,
            Vector2 currentPosition,
            Vector2 spawnExtents,
            float tangentOffset)
        {
            Vector2 direction = center - currentPosition;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector2.up;
            }

            direction.Normalize();
            float scale = 1f / Mathf.Max(
                Mathf.Abs(direction.x) / Mathf.Max(0.01f, spawnExtents.x),
                Mathf.Abs(direction.y) / Mathf.Max(0.01f, spawnExtents.y));
            Vector2 tangent = new(-direction.y, direction.x);
            Vector2 offset = direction * scale + tangent * tangentOffset;
            offset.x = Mathf.Clamp(
                offset.x,
                -spawnExtents.x,
                spawnExtents.x);
            offset.y = Mathf.Clamp(
                offset.y,
                -spawnExtents.y,
                spawnExtents.y);
            return center + offset;
        }

        public static Vector2 CalculateOutwardSpawnPosition(
            Vector2 center,
            Vector2 currentPosition,
            Vector2 spawnExtents,
            float tangentOffset)
        {
            Vector2 direction = currentPosition - center;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector2.up;
            }

            direction.Normalize();
            float scale = 1f / Mathf.Max(
                Mathf.Abs(direction.x) / Mathf.Max(0.01f, spawnExtents.x),
                Mathf.Abs(direction.y) / Mathf.Max(0.01f, spawnExtents.y));
            Vector2 tangent = new(-direction.y, direction.x);
            Vector2 offset = direction * scale + tangent * tangentOffset;
            offset.x = Mathf.Clamp(
                offset.x,
                -spawnExtents.x,
                spawnExtents.x);
            offset.y = Mathf.Clamp(
                offset.y,
                -spawnExtents.y,
                spawnExtents.y);
            Vector2 destination = center + offset;
            return Vector2.Dot(
                    destination - currentPosition,
                    direction) > 0f
                ? destination
                : currentPosition;
        }

        private Vector2 GetCameraExtents()
        {
            if (worldCamera == null || !worldCamera.orthographic)
            {
                return new Vector2(5.4f, 9.6f);
            }

            return new Vector2(
                worldCamera.orthographicSize * worldCamera.aspect,
                worldCamera.orthographicSize);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.7f);
            DrawBounds(GetSpawnExtents());
            Gizmos.color = new Color(1f, 0.35f, 0.2f, 0.7f);
            DrawBounds(GetRecycleExtents());
        }

        private void DrawBounds(Vector2 extents)
        {
            Gizmos.DrawWireCube(
                transform.position,
                new Vector3(extents.x * 2f, extents.y * 2f, 0f));
        }
    }
}
