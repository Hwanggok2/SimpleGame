using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    public sealed class HealthPickupSpawner : MonoBehaviour
    {
        public const float SpawnInterval = 20f;
        public const int MaximumActivePickups = 3;
        public const float SpawnEdgePadding = 1f;
        public const float MinimumPlayerDistance = 2.5f;
        private const int MaximumPositionAttempts = 8;

        [SerializeField] private PrototypeGameSession session;
        [SerializeField] private PlayerRoot player;
        [SerializeField] private PlayerWorldArea worldArea;
        [SerializeField] private HealthPickup pickupPrefab;
        [SerializeField] private Transform pickupRoot;

        private readonly List<HealthPickup> activePickups = new();
        private float nextSpawnAt = SpawnInterval;

        public int ActivePickupCount => activePickups.Count;
        internal bool IsSessionPlaying =>
            session != null && session.IsPlaying;

        public void Configure(
            PrototypeGameSession configuredSession,
            PlayerRoot configuredPlayer,
            PlayerWorldArea configuredWorldArea,
            HealthPickup configuredPrefab,
            Transform configuredRoot)
        {
            session = configuredSession;
            player = configuredPlayer;
            worldArea = configuredWorldArea;
            pickupPrefab = configuredPrefab;
            pickupRoot = configuredRoot;
        }

        private void Start()
        {
            nextSpawnAt =
                (session != null ? session.ElapsedTime : 0f) +
                SpawnInterval;
        }

        private void Update()
        {
            activePickups.RemoveAll(pickup => pickup == null);
            if (session == null ||
                !session.IsPlaying ||
                pickupPrefab == null ||
                player == null ||
                worldArea == null ||
                session.ElapsedTime < nextSpawnAt)
            {
                return;
            }

            nextSpawnAt = session.ElapsedTime + SpawnInterval;
            if (activePickups.Count >= MaximumActivePickups)
            {
                return;
            }

            SpawnPickup(FindSpawnPosition());
        }

        public static Vector2 CalculateSpawnPosition(
            Vector2 center,
            Vector2 extents,
            float edgePadding,
            Vector2 normalizedPosition)
        {
            Vector2 safeExtents = new(
                Mathf.Max(0f, extents.x - Mathf.Max(0f, edgePadding)),
                Mathf.Max(0f, extents.y - Mathf.Max(0f, edgePadding)));
            Vector2 normalized = new(
                Mathf.Clamp01(normalizedPosition.x),
                Mathf.Clamp01(normalizedPosition.y));
            return center + new Vector2(
                Mathf.Lerp(-safeExtents.x, safeExtents.x, normalized.x),
                Mathf.Lerp(-safeExtents.y, safeExtents.y, normalized.y));
        }

        internal void NotifyReleased(HealthPickup pickup)
        {
            activePickups.Remove(pickup);
        }

        private Vector2 FindSpawnPosition()
        {
            Vector2 center = player.transform.position;
            Vector2 extents = worldArea.GetSpawnExtents();
            Vector2 position = center;
            for (int attempt = 0;
                 attempt < MaximumPositionAttempts;
                 attempt++)
            {
                position = CalculateSpawnPosition(
                    center,
                    extents,
                    SpawnEdgePadding,
                    new Vector2(Random.value, Random.value));
                if (Vector2.Distance(position, center) >=
                    MinimumPlayerDistance)
                {
                    return position;
                }
            }

            return CalculateSpawnPosition(
                center,
                extents,
                SpawnEdgePadding,
                Vector2.one);
        }

        private void SpawnPickup(Vector2 position)
        {
            HealthPickup pickup = Instantiate(
                pickupPrefab,
                position,
                Quaternion.identity,
                pickupRoot);
            pickup.Configure(this);
            activePickups.Add(pickup);
        }
    }
}
