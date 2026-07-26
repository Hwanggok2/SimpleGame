using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    public sealed class SpawnPointRegistry : MonoBehaviour
    {
        [SerializeField] private List<Transform> spawnPoints = new();

        public IReadOnlyList<Transform> SpawnPoints => spawnPoints;

        public bool TryGet(string spawnPointId, out Transform spawnPoint)
        {
            foreach (Transform candidate in spawnPoints)
            {
                if (candidate != null &&
                    string.Equals(
                        candidate.name,
                        spawnPointId,
                        StringComparison.Ordinal))
                {
                    spawnPoint = candidate;
                    return true;
                }
            }

            spawnPoint = null;
            return false;
        }

        public void Configure(IEnumerable<Transform> values)
        {
            spawnPoints = new List<Transform>(values);
        }
    }
}
