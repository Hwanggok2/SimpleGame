using UnityEngine;

namespace SimpleGame
{
    public sealed class EnemyWorldRecycler : MonoBehaviour
    {
        private const float CheckInterval = 0.2f;
        public const float ContinuePushDuration = 0.4f;
        public const float ContinueDamageFraction = 0.5f;

        [SerializeField] private PrototypeGameSession session;
        [SerializeField] private EnemyWorldService enemyWorld;
        [SerializeField] private PlayerWorldArea worldArea;
        private float nextCheckAt;

        public void Configure(
            PrototypeGameSession gameSession,
            EnemyWorldService configuredEnemyWorld,
            PlayerWorldArea area)
        {
            session = gameSession;
            enemyWorld = configuredEnemyWorld;
            worldArea = area;
        }

        public void PushAwayAllNormalEnemies()
        {
            if (session == null ||
                enemyWorld == null ||
                worldArea == null)
            {
                return;
            }

            for (int index = enemyWorld.Enemies.Count - 1;
                 index >= 0;
                 index--)
            {
                EnemyBase enemy = enemyWorld.Enemies[index];
                if (CanReposition(enemy))
                {
                    Vector2 position =
                        worldArea.GetOutwardSpawnPosition(
                            enemy.transform.position);
                    enemy.ApplyContinuePush(
                        position,
                        session.Player.transform.position,
                        CalculateContinueDamage(
                            enemy.CurrentHealth),
                        ContinuePushDuration);
                }
            }
        }

        public static int CalculateContinueDamage(
            int currentHealth)
        {
            return CombatResolver.RoundDamage(
                Mathf.Max(0, currentHealth) *
                ContinueDamageFraction);
        }

        private void Update()
        {
            if (session == null ||
                enemyWorld == null ||
                worldArea == null ||
                !session.IsPlaying ||
                Time.time < nextCheckAt)
            {
                return;
            }

            nextCheckAt = Time.time + CheckInterval;
            foreach (EnemyBase enemy in enemyWorld.Enemies)
            {
                if (CanReposition(enemy) &&
                    worldArea.IsOutsideRecycleArea(
                        enemy.transform.position))
                {
                    Reposition(enemy);
                }
            }
        }

        private static bool CanReposition(EnemyBase enemy)
        {
            return enemy != null &&
                enemy.IsAlive &&
                enemy.Archetype != EnemyArchetype.Boss;
        }

        private void Reposition(EnemyBase enemy)
        {
            Vector2 position = worldArea.GetOppositeSpawnPosition(
                enemy.transform.position);
            enemy.Reposition(
                position,
                session.Player.transform.position);
        }
    }
}
