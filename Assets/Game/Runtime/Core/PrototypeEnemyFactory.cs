using UnityEngine;

namespace SimpleGame
{
    public sealed class PrototypeEnemyFactory : MonoBehaviour
    {
        [SerializeField] private Transform enemyRoot;
        [SerializeField] private PrototypeGameSession session;
        [SerializeField] private MeleeEnemy meleePrefab;
        [SerializeField] private RangedEnemy rangedPrefab;
        [SerializeField] private ShieldEnemy shieldPrefab;
        [SerializeField] private BossEnemy bossPrefab;

        public void ConfigurePrefabs(
            MeleeEnemy melee,
            RangedEnemy ranged,
            ShieldEnemy shield,
            BossEnemy boss)
        {
            meleePrefab = melee;
            rangedPrefab = ranged;
            shieldPrefab = shield;
            bossPrefab = boss;
        }

        public void Configure(PrototypeGameSession gameSession, Transform root)
        {
            session = gameSession;
            enemyRoot = root;
        }

        public EnemyBase Spawn(EnemyArchetype archetype, int level, Vector2 position)
        {
            EnemyBase prefab = archetype switch
            {
                EnemyArchetype.Melee => meleePrefab,
                EnemyArchetype.Ranged => rangedPrefab,
                EnemyArchetype.Shield => shieldPrefab,
                EnemyArchetype.Boss => bossPrefab,
                _ => null
            };

            if (prefab == null)
            {
                Debug.LogError($"Enemy prefab is not assigned: {archetype}", this);
                return null;
            }

            EnemyBase enemy = Instantiate(
                prefab,
                position,
                Quaternion.identity,
                enemyRoot);
            enemy.name = $"{archetype}_Lv{level}";

            enemy.Configure(session, level);
            session.RegisterEnemy(enemy);
            return enemy;
        }
    }
}
