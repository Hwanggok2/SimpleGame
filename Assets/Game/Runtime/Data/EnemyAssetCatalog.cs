using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    [Serializable]
    public sealed class EnemyAssetEntry
    {
        [SerializeField] private string enemyId;
        [SerializeField] private EnemyBase prefab;

        public EnemyAssetEntry(string enemyId, EnemyBase prefab)
        {
            this.enemyId = enemyId;
            this.prefab = prefab;
        }

        public string EnemyId => enemyId;
        public EnemyBase Prefab => prefab;
    }

    [CreateAssetMenu(
        fileName = "EnemyAssetCatalog",
        menuName = "SimpleGame/Data/Enemy Asset Catalog")]
    public sealed class EnemyAssetCatalog : ScriptableObject
    {
        [SerializeField] private List<EnemyAssetEntry> entries = new();

        public IReadOnlyList<EnemyAssetEntry> Entries => entries;

        public bool TryGetPrefab(string enemyId, out EnemyBase prefab)
        {
            foreach (EnemyAssetEntry entry in entries)
            {
                if (entry != null &&
                    string.Equals(
                        entry.EnemyId,
                        enemyId,
                        StringComparison.Ordinal))
                {
                    prefab = entry.Prefab;
                    return prefab != null;
                }
            }

            prefab = null;
            return false;
        }

        public void Configure(IEnumerable<EnemyAssetEntry> values)
        {
            entries = new List<EnemyAssetEntry>(values);
        }
    }
}
