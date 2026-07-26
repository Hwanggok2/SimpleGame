using UnityEngine;

namespace SimpleGame
{
    public sealed class PrototypeEnemyFactory : MonoBehaviour
    {
        [SerializeField] private Transform enemyRoot;
        [SerializeField] private PrototypeGameSession session;

        public void Configure(PrototypeGameSession gameSession, Transform root)
        {
            session = gameSession;
            enemyRoot = root;
        }

        public EnemyBase Spawn(EnemyArchetype archetype, int level, Vector2 position)
        {
            var gameObject = new GameObject($"{archetype}_Lv{level}");
            gameObject.transform.SetParent(enemyRoot, false);
            gameObject.transform.position = position;

            gameObject.AddComponent<EnemyHealth>();
            gameObject.AddComponent<EnemyFacing>();
            gameObject.AddComponent<EnemyMovement>();
            gameObject.AddComponent<EnemyStateMachine>();

            EnemyBase enemy = archetype switch
            {
                EnemyArchetype.Melee => gameObject.AddComponent<MeleeEnemy>(),
                EnemyArchetype.Ranged => gameObject.AddComponent<RangedEnemy>(),
                EnemyArchetype.Shield => gameObject.AddComponent<ShieldEnemy>(),
                EnemyArchetype.Boss => gameObject.AddComponent<BossEnemy>(),
                _ => null
            };

            if (archetype == EnemyArchetype.Boss)
            {
                gameObject.AddComponent<BossAttackModule>();
            }
            else if (archetype != EnemyArchetype.Shield)
            {
                gameObject.AddComponent<EnemyAttackModule>();
            }

            enemy.Configure(session, level);
            session.RegisterEnemy(enemy);
            return enemy;
        }
    }
}
