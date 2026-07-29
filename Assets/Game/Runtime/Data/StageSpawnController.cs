using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    public sealed class StageSpawnController : MonoBehaviour
    {
        [SerializeField] private GameDataManifest gameData;
        [SerializeField] private SpawnPointRegistry spawnPoints;
        [SerializeField] private PrototypeEnemyFactory enemyFactory;

        private List<StageSpawnEntry> activeEntries = new();
        private int nextEntryIndex;

        public int PendingCount =>
            Mathf.Max(0, activeEntries.Count - nextEntryIndex);
        public SpawnPointRegistry SpawnPoints => spawnPoints;

        public void Configure(
            GameDataManifest configuredGameData,
            SpawnPointRegistry configuredSpawnPoints,
            PrototypeEnemyFactory configuredEnemyFactory)
        {
            gameData = configuredGameData;
            spawnPoints = configuredSpawnPoints;
            enemyFactory = configuredEnemyFactory;
        }

        public void Begin(string stageId)
        {
            nextEntryIndex = 0;
            activeEntries = gameData != null &&
                gameData.StageSpawnSchedule != null
                ? gameData.StageSpawnSchedule.CopyStageEntries(stageId)
                : new List<StageSpawnEntry>();
        }

        public void Tick(float elapsedTime)
        {
            while (nextEntryIndex < activeEntries.Count &&
                activeEntries[nextEntryIndex].SpawnTimeSec <= elapsedTime)
            {
                Spawn(activeEntries[nextEntryIndex]);
                nextEntryIndex++;
            }
        }

        private void Spawn(StageSpawnEntry entry)
        {
            if (enemyFactory == null)
            {
                Debug.LogError(
                    $"EnemyFactory is not configured for {entry.RuntimeId}.",
                    this);
                return;
            }

            if (spawnPoints == null ||
                !spawnPoints.TryGet(entry.SpawnPointId, out Transform point))
            {
                Debug.LogError(
                    $"Spawn point not found for {entry.RuntimeId}: " +
                    entry.SpawnPointId,
                    this);
                return;
            }

            enemyFactory.Spawn(
                entry.EnemyId,
                entry.EnemyLevel,
                entry.WaveNumber,
                point.position);
        }
    }
}
