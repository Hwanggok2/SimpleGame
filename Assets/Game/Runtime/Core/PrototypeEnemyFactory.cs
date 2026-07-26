using UnityEngine;

namespace SimpleGame
{
    public sealed class PrototypeEnemyFactory : MonoBehaviour
    {
        [SerializeField] private Transform enemyRoot;
        [SerializeField] private PrototypeGameSession session;
        [SerializeField] private EnemyAssetCatalog assetCatalog;
        [SerializeField] private EnemyBalanceTable balanceTable;

        public void ConfigureAssets(
            EnemyAssetCatalog configuredAssetCatalog,
            EnemyBalanceTable configuredBalanceTable)
        {
            assetCatalog = configuredAssetCatalog;
            balanceTable = configuredBalanceTable;
        }

        public void Configure(PrototypeGameSession gameSession, Transform root)
        {
            session = gameSession;
            enemyRoot = root;
        }

        public EnemyBase Spawn(string enemyId, int level, Vector2 position)
        {
            if (balanceTable == null ||
                !balanceTable.TryGet(enemyId, out EnemyDefinition definition))
            {
                Debug.LogError($"Enemy balance not found: {enemyId}", this);
                return null;
            }

            if (assetCatalog == null ||
                !assetCatalog.TryGetPrefab(enemyId, out EnemyBase prefab))
            {
                Debug.LogError($"Enemy prefab is not assigned: {enemyId}", this);
                return null;
            }

            if (prefab.Archetype != definition.Archetype)
            {
                Debug.LogError(
                    $"Enemy archetype mismatch: {enemyId} " +
                    $"({definition.Archetype} data / {prefab.Archetype} prefab)",
                    this);
                return null;
            }

            EnemyBase enemy = Instantiate(
                prefab,
                position,
                Quaternion.identity,
                enemyRoot);
            enemy.name = $"{enemyId}_Lv{level}";

            enemy.Configure(session, level, definition);
            session.RegisterEnemy(enemy);
            return enemy;
        }
    }
}
