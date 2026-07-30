using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    public sealed class PrototypeEnemyFactory : MonoBehaviour
    {
        private const int DefaultMaximumInactivePerPrefab = 64;

        [SerializeField] private Transform enemyRoot;
        [SerializeField] private PrototypeGameSession session;
        [SerializeField] private EnemyWorldService enemyWorld;
        [SerializeField] private EnemyAssetCatalog assetCatalog;
        [SerializeField] private EnemyBalanceTable balanceTable;
        [SerializeField, Min(0)]
        private int maximumInactivePerPrefab =
            DefaultMaximumInactivePerPrefab;

        private readonly Dictionary<EnemyBase, Stack<EnemyBase>>
            inactiveByPrefab = new();
        private readonly Dictionary<EnemyBase, EnemyBase>
            sourcePrefabByInstance = new();
        private readonly HashSet<EnemyBase> inactiveInstances = new();

        public int ManagedInstanceCount =>
            sourcePrefabByInstance.Count;
        public int InactiveInstanceCount =>
            inactiveInstances.Count;

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
                enemyWorld.FindOpenEnemyPosition(
                    position,
                    radius,
                    incomingAllowsEnemyOverlap:
                        definition.AllowsEnemyOverlap);
            EnemyBase enemy = Acquire(prefab, spawnPosition);
            enemy.name =
                $"{enemyId}_W{waveNumber:00}_Lv{level}";

            enemy.Configure(
                this,
                session,
                enemyWorld,
                level,
                waveNumber,
                definition);
            enemyWorld.Register(enemy);
            return enemy;
        }

        public void Recycle(EnemyBase enemy)
        {
            if (enemy == null ||
                !sourcePrefabByInstance.TryGetValue(
                    enemy,
                    out EnemyBase sourcePrefab) ||
                !inactiveInstances.Add(enemy))
            {
                return;
            }

            enemyWorld?.Unregister(enemy);
            enemy.PrepareForPool();
            enemy.gameObject.SetActive(false);
            if (enemyRoot != null)
            {
                enemy.transform.SetParent(enemyRoot, true);
            }

            if (!inactiveByPrefab.TryGetValue(
                    sourcePrefab,
                    out Stack<EnemyBase> inactive))
            {
                inactive = new Stack<EnemyBase>();
                inactiveByPrefab.Add(sourcePrefab, inactive);
            }

            int maximumInactive =
                Mathf.Max(0, maximumInactivePerPrefab);
            if (inactive.Count >= maximumInactive)
            {
                inactiveInstances.Remove(enemy);
                sourcePrefabByInstance.Remove(enemy);
                if (Application.isPlaying)
                {
                    Destroy(enemy.gameObject);
                }
                else
                {
                    DestroyImmediate(enemy.gameObject);
                }

                return;
            }

            inactive.Push(enemy);
        }

        private EnemyBase Acquire(
            EnemyBase prefab,
            Vector2 spawnPosition)
        {
            EnemyBase enemy = null;
            if (inactiveByPrefab.TryGetValue(
                    prefab,
                    out Stack<EnemyBase> inactive))
            {
                while (inactive.Count > 0 && enemy == null)
                {
                    EnemyBase candidate = inactive.Pop();
                    inactiveInstances.Remove(candidate);
                    if (candidate == null)
                    {
                        sourcePrefabByInstance.Remove(candidate);
                        continue;
                    }

                    enemy = candidate;
                }
            }

            if (enemy == null)
            {
                enemy = Instantiate(
                    prefab,
                    spawnPosition,
                    Quaternion.identity,
                    enemyRoot);
                sourcePrefabByInstance[enemy] = prefab;
                return enemy;
            }

            enemy.transform.SetParent(enemyRoot, false);
            enemy.transform.SetPositionAndRotation(
                spawnPosition,
                Quaternion.identity);
            enemy.transform.localScale = prefab.transform.localScale;
            enemy.gameObject.SetActive(true);
            return enemy;
        }

        private void OnDestroy()
        {
            inactiveByPrefab.Clear();
            sourcePrefabByInstance.Clear();
            inactiveInstances.Clear();
        }
    }
}
