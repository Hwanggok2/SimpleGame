using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    [Serializable]
    public sealed class StageSpawnEntry
    {
        [SerializeField] private string stageId;
        [SerializeField] private string waveId;
        [SerializeField, Min(0f)] private float spawnTimeSec;
        [SerializeField, Min(1)] private int spawnIndex;
        [SerializeField] private string spawnPointId;
        [SerializeField] private string enemyId;
        [SerializeField, Min(1)] private int enemyLevel = 1;

        public StageSpawnEntry(
            string stageId,
            string waveId,
            float spawnTimeSec,
            int spawnIndex,
            string spawnPointId,
            string enemyId,
            int enemyLevel)
        {
            this.stageId = stageId;
            this.waveId = waveId;
            this.spawnTimeSec = Mathf.Max(0f, spawnTimeSec);
            this.spawnIndex = Mathf.Max(1, spawnIndex);
            this.spawnPointId = spawnPointId;
            this.enemyId = enemyId;
            this.enemyLevel = Mathf.Max(1, enemyLevel);
        }

        public string StageId => stageId;
        public string WaveId => waveId;
        public float SpawnTimeSec => spawnTimeSec;
        public int SpawnIndex => spawnIndex;
        public string SpawnPointId => spawnPointId;
        public string EnemyId => enemyId;
        public int EnemyLevel => enemyLevel;
        public int WaveNumber =>
            TryParseWaveNumber(waveId, out int result)
                ? result
                : 1;
        public string RuntimeId =>
            $"{stageId}_{waveId}_{spawnIndex:000}";

        public static bool TryParseWaveNumber(
            string value,
            out int waveNumber)
        {
            const string prefix = "WAVE_";
            waveNumber = 0;
            return !string.IsNullOrWhiteSpace(value) &&
                value.StartsWith(
                    prefix,
                    StringComparison.Ordinal) &&
                int.TryParse(
                    value.Substring(prefix.Length),
                    out waveNumber) &&
                waveNumber > 0;
        }
    }

    [CreateAssetMenu(
        fileName = "StageSpawnSchedule",
        menuName = "SimpleGame/Data/Stage Spawn Schedule")]
    public sealed class StageSpawnSchedule : ScriptableObject
    {
        [SerializeField] private List<StageSpawnEntry> entries = new();

        public IReadOnlyList<StageSpawnEntry> Entries => entries;

        public void Configure(IEnumerable<StageSpawnEntry> values)
        {
            entries = new List<StageSpawnEntry>(values);
        }

        public List<StageSpawnEntry> CopyStageEntries(string stageId)
        {
            var result = new List<StageSpawnEntry>();
            foreach (StageSpawnEntry entry in entries)
            {
                if (entry != null &&
                    string.Equals(
                        entry.StageId,
                        stageId,
                        StringComparison.Ordinal))
                {
                    result.Add(entry);
                }
            }

            result.Sort(CompareEntries);
            return result;
        }

        private static int CompareEntries(
            StageSpawnEntry left,
            StageSpawnEntry right)
        {
            int timeOrder = left.SpawnTimeSec.CompareTo(right.SpawnTimeSec);
            if (timeOrder != 0)
            {
                return timeOrder;
            }

            int waveOrder = string.CompareOrdinal(left.WaveId, right.WaveId);
            return waveOrder != 0
                ? waveOrder
                : left.SpawnIndex.CompareTo(right.SpawnIndex);
        }
    }
}
