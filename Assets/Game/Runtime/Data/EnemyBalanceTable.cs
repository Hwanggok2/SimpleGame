using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    [CreateAssetMenu(
        fileName = "EnemyBalanceTable",
        menuName = "SimpleGame/Data/Enemy Balance Table")]
    public sealed class EnemyBalanceTable : ScriptableObject
    {
        [SerializeField] private List<EnemyDefinition> definitions = new();

        public IReadOnlyList<EnemyDefinition> Definitions => definitions;

        public bool TryGet(string enemyId, out EnemyDefinition definition)
        {
            foreach (EnemyDefinition candidate in definitions)
            {
                if (candidate != null &&
                    string.Equals(
                        candidate.EnemyId,
                        enemyId,
                        StringComparison.Ordinal))
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = null;
            return false;
        }

        public void Configure(IEnumerable<EnemyDefinition> values)
        {
            definitions = new List<EnemyDefinition>(values);
        }
    }
}
