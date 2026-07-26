using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    public sealed class PrototypeGameSession : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private MapBounds mapBounds;
        [SerializeField] private PlayerRoot player;
        [SerializeField] private CastleRoot castle;
        [SerializeField] private PrototypeEnemyFactory enemyFactory;
        [SerializeField] private Transform enemyRoot;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private PrototypeHUDPresenter hudPresenter;

        private readonly List<EnemyBase> enemies = new();
        private GameRunState state = GameRunState.Playing;
        private int continueCount;
        private int observedPlayerLevel;

        public event Action<string> HintChanged;
        public event Action<bool> CriticalCardVisibilityChanged;
        public event Action<bool> GameOverVisibilityChanged;

        public MapBounds Bounds => mapBounds;
        public PlayerRoot Player => player;
        public CastleRoot Castle => castle;
        public int Score { get; private set; }
        public int AccountExperience => Score / 5;
        public float ElapsedTime { get; private set; }
        public bool IsPlaying => state == GameRunState.Playing;

        public void ConfigureScene(
            MapBounds configuredBounds,
            PlayerRoot configuredPlayer,
            CastleRoot configuredCastle,
            PrototypeEnemyFactory configuredFactory,
            Transform configuredEnemyRoot,
            Camera configuredCamera,
            PrototypeHUDPresenter configuredPresenter)
        {
            mapBounds = configuredBounds;
            player = configuredPlayer;
            castle = configuredCastle;
            enemyFactory = configuredFactory;
            enemyRoot = configuredEnemyRoot;
            worldCamera = configuredCamera;
            hudPresenter = configuredPresenter;
        }

        private void Start()
        {
            Time.timeScale = 1f;
            castle.Configure(30);
            player.Configure(this, worldCamera, mapBounds);
            enemyFactory.Configure(this, enemyRoot);
            hudPresenter.Initialize(this);

            castle.Health.Depleted += OnCastleDepleted;
            player.Progression.LevelUpCardRequested += OnPlayerLevelUp;
            observedPlayerLevel = player.Progression.Level;
            SpawnPrototypeSet();
            ShowHint("Tap the field to move. Tap an enemy to test the combat rules.");
        }

        private void Update()
        {
            if (state != GameRunState.GameOver && castle != null && !castle.IsAlive)
            {
                OnCastleDepleted();
            }

            if (player != null && player.Progression.Level > observedPlayerLevel)
            {
                OnPlayerLevelUp();
            }

            if (IsPlaying)
            {
                ElapsedTime += Time.deltaTime;
            }
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            if (castle != null && castle.Health != null)
            {
                castle.Health.Depleted -= OnCastleDepleted;
            }

            if (player != null && player.Progression != null)
            {
                player.Progression.LevelUpCardRequested -= OnPlayerLevelUp;
            }
        }

        public void RegisterEnemy(EnemyBase enemy)
        {
            if (!enemies.Contains(enemy))
            {
                enemies.Add(enemy);
            }
        }

        public EnemyBase FindEnemyNear(Vector2 position, float radius)
        {
            EnemyBase nearest = null;
            float nearestDistance = radius;
            foreach (EnemyBase enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                float distance = Vector2.Distance(position, enemy.transform.position);
                if (distance <= nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        public void OnEnemyDefeated(EnemyBase enemy)
        {
            int score = enemy.Archetype == EnemyArchetype.Boss ? 25 : 5;
            Score += score;
            player.Progression.AddExperience(enemy.Archetype == EnemyArchetype.Boss ? 5 : 2);
        }

        public void ShowHint(string message)
        {
            HintChanged?.Invoke(message);
        }

        public void TogglePause()
        {
            if (state == GameRunState.GameOver)
            {
                return;
            }

            state = state == GameRunState.Paused
                ? GameRunState.Playing
                : GameRunState.Paused;
            Time.timeScale = state == GameRunState.Paused ? 0f : 1f;
            ShowHint(state == GameRunState.Paused ? "PAUSED" : "RESUMED");
        }

        public void SelectCriticalCard()
        {
            player.Critical.AddCard();
            CriticalCardVisibilityChanged?.Invoke(false);
            if (state == GameRunState.Paused)
            {
                TogglePause();
            }

            ShowHint("Critical chance +10% card applied.");
        }

        public void SimulateRewardedContinue()
        {
            if (state != GameRunState.GameOver || continueCount >= 2)
            {
                ShowHint("Continue is available after Game Over, up to two times.");
                return;
            }

            continueCount++;
            castle.RestoreAfterContinue();
            player.RestoreAfterContinue();
            foreach (EnemyBase enemy in enemies)
            {
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.ApplyContinueKnockback(mapBounds, castle.transform.position);
                }
            }

            state = GameRunState.Playing;
            GameOverVisibilityChanged?.Invoke(false);
            ShowHint($"Rewarded ad simulated: Continue {continueCount}/2, Castle invulnerable for 3s.");
        }

        public void DebugDamageCastle()
        {
            if (IsPlaying)
            {
                castle.ReceiveDamage(10);
                ShowHint("Debug: Castle took 10 damage.");
            }
        }

        public void DebugGrantPlayerExperience()
        {
            player.Progression.AddExperience(5);
            ShowHint("Debug: Player EXP +5.");
        }

        private void OnCastleDepleted()
        {
            if (state == GameRunState.GameOver)
            {
                return;
            }

            state = GameRunState.GameOver;
            Time.timeScale = 1f;
            GameOverVisibilityChanged?.Invoke(true);
            ShowHint("Castle destroyed. CONTINUE simulates a successful rewarded ad.");
        }

        private void OnPlayerLevelUp()
        {
            observedPlayerLevel = player.Progression.Level;
            if (state == GameRunState.GameOver)
            {
                return;
            }

            state = GameRunState.Paused;
            Time.timeScale = 0f;
            CriticalCardVisibilityChanged?.Invoke(true);
            ShowHint("LEVEL UP: select the repeatable Critical +10% card.");
        }

        private void SpawnPrototypeSet()
        {
            enemyFactory.Spawn(EnemyArchetype.Ranged, 1, new Vector2(-3.7f, 6.8f));
            enemyFactory.Spawn(EnemyArchetype.Melee, 1, new Vector2(3.7f, 6.5f));
            enemyFactory.Spawn(EnemyArchetype.Shield, 1, new Vector2(0f, 4.6f));
            enemyFactory.Spawn(EnemyArchetype.Ranged, 3, new Vector2(-3.8f, -6.2f));
            enemyFactory.Spawn(EnemyArchetype.Melee, 2, new Vector2(3.8f, -5.9f));
            enemyFactory.Spawn(EnemyArchetype.Boss, 1, new Vector2(0f, 8.1f));
        }
    }
}
