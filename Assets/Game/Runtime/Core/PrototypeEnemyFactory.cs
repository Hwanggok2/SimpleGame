using UnityEngine;

namespace SimpleGame
{
    public sealed class PrototypeEnemyFactory : MonoBehaviour
    {
        [SerializeField] private Transform enemyRoot;
        [SerializeField] private PrototypeGameSession session;
        [SerializeField] private EnemyWorldService enemyWorld;
        [SerializeField] private EnemyAssetCatalog assetCatalog;
        [SerializeField] private EnemyBalanceTable balanceTable;

        public void ConfigureAssets(
            EnemyAssetCatalog configuredAssetCatalog,
            EnemyBalanceTable configuredBalanceTable)
        {
            assetCatalog = configuredAssetCatalog;
            balanceTable = configuredBalanceTable;
        }

        public void Configure(
            PrototypeGameSession gameSession,
            EnemyWorldService configuredEnemyWorld,
            Transform root)
        {
            session = gameSession;
            enemyWorld = configuredEnemyWorld;
            enemyRoot = root;
        }

        public EnemyBase Spawn(
            string enemyId,
            int level,
            int waveNumber,
            Vector2 position)
        {
            if (session == null ||
                enemyWorld == null ||
                enemyRoot == null)
            {
                Debug.LogError(
                    "PrototypeEnemyFactory must be configured with " +
                    "a session, EnemyWorldService, and enemy root " +
                    "before spawning.",
                    this);
                return null;
            }

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

            float radius =
                EnemyWorldService.GetColliderRadius(prefab);
            Vector2 spawnPosition =
                enemyWorld.FindOpenEnemyPosition(position, radius);
            EnemyBase enemy = Instantiate(
                prefab,
                spawnPosition,
                Quaternion.identity,
                enemyRoot);
            enemy.name =
                $"{enemyId}_W{waveNumber:00}_Lv{level}";

            enemy.Configure(
                session,
                enemyWorld,
                level,
                waveNumber,
                definition);
            enemyWorld.Register(enemy);
            return enemy;
        }
    }
}
