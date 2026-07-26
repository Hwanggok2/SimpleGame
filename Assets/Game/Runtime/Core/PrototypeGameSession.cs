using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    public sealed class PrototypeGameSession : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private PlayerRoot player;
        [SerializeField] private PrototypeEnemyFactory enemyFactory;
        [SerializeField] private Transform enemyRoot;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private CombatFeedbackController combatFeedback;
        [SerializeField] private EnemyWorldRecycler enemyRecycler;
        [SerializeField] private PrototypeHUDPresenter hudPresenter;
        [Header("Game Data")]
        [SerializeField] private string stageId = "Stage01";
        [SerializeField] private GameDataManifest gameData;
        [SerializeField] private StageSpawnController stageSpawner;
        [Header("Prototype Account")]
        [SerializeField, Min(1)] private int accountLevel = 1;

        private readonly List<EnemyBase> enemies = new();
        private GameRunState state = GameRunState.Playing;
        private int pendingCardSelections;
        private bool selectingStartingCards;
        private int continueCount;

        public event Action<string> HintChanged;
        public event Action<bool> CriticalCardVisibilityChanged;
        public event Action<bool> GameOverVisibilityChanged;

        public PlayerRoot Player => player;
        public IReadOnlyList<EnemyBase> Enemies => enemies;
        public int Score { get; private set; }
        public int AccountExperience =>
            gameData != null && gameData.GlobalBalance != null
                ? gameData.GlobalBalance.CalculateAccountExperience(Score)
                : 0;
        public float ElapsedTime { get; private set; }
        public bool IsPlaying => state == GameRunState.Playing;

        public void ConfigureScene(
            PlayerRoot configuredPlayer,
            PrototypeEnemyFactory configuredFactory,
            Transform configuredEnemyRoot,
            Camera configuredCamera,
            CombatFeedbackController configuredCombatFeedback,
            EnemyWorldRecycler configuredEnemyRecycler,
            PrototypeHUDPresenter configuredPresenter)
        {
            player = configuredPlayer;
            enemyFactory = configuredFactory;
            enemyRoot = configuredEnemyRoot;
            worldCamera = configuredCamera;
            combatFeedback = configuredCombatFeedback;
            enemyRecycler = configuredEnemyRecycler;
            hudPresenter = configuredPresenter;
        }

        public void ConfigureData(
            GameDataManifest configuredGameData,
            StageSpawnController configuredStageSpawner)
        {
            gameData = configuredGameData;
            stageSpawner = configuredStageSpawner;
        }

        private void Start()
        {
            Time.timeScale = 1f;
            if (gameData == null ||
                !gameData.IsConfigured ||
                stageSpawner == null ||
                enemyRecycler == null)
            {
                Debug.LogError(
                    "PrototypeGameSession requires GameDataManifest " +
                    "StageSpawnController, and EnemyWorldRecycler.",
                    this);
                enabled = false;
                return;
            }

            EnsureCombatFeedback();
            player.Configure(
                this,
                worldCamera,
                gameData.PlayerLevelExperience,
                gameData.GlobalBalance);
            enemyFactory.ConfigureAssets(
                gameData.EnemyAssets,
                gameData.EnemyBalance);
            enemyFactory.Configure(this, enemyRoot);
            enemyRecycler.Configure(this, player.GetComponent<PlayerWorldArea>());
            hudPresenter.Initialize(this);

            player.Health.Depleted += OnPlayerDepleted;
            player.Progression.LevelUpCardRequested += OnPlayerLevelUp;
            stageSpawner.Begin(stageId);
            ShowHint("Survive. Tap the field to move and tap enemies to attack.");
            QueueCardSelections(
                CalculateStartingCardSelectionCount(accountLevel),
                true);
        }

        private void Update()
        {
            if (state != GameRunState.GameOver &&
                player != null &&
                !player.IsAlive)
            {
                OnPlayerDepleted();
            }

            if (IsPlaying)
            {
                ElapsedTime += Time.deltaTime;
                stageSpawner.Tick(ElapsedTime);
            }
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            if (player != null && player.Health != null)
            {
                player.Health.Depleted -= OnPlayerDepleted;
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

        public EnemyBase FindFirstEnemyOnPath(Vector2 start, Vector2 destination)
        {
            Vector2 path = destination - start;
            float pathLengthSquared = path.sqrMagnitude;
            if (pathLengthSquared <= 0.0001f)
            {
                return null;
            }

            float playerRadius = GetColliderRadius(player);
            EnemyBase first = null;
            float firstProgress = float.MaxValue;
            foreach (EnemyBase enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                Vector2 enemyPosition = enemy.transform.position;
                float progress = Mathf.Clamp01(
                    Vector2.Dot(enemyPosition - start, path) /
                    pathLengthSquared);
                Vector2 closestPoint = start + path * progress;
                float combinedRadius =
                    playerRadius + GetColliderRadius(enemy);
                if (Vector2.SqrMagnitude(enemyPosition - closestPoint) >
                    combinedRadius * combinedRadius)
                {
                    continue;
                }

                if (progress < firstProgress)
                {
                    firstProgress = progress;
                    first = enemy;
                }
            }

            return first;
        }

        public void OnEnemyDefeated(EnemyBase enemy)
        {
            Score += enemy.Definition.Score;
            player.Progression.AddExperience(enemy.Definition.KillExperience);
        }

        public void ShowHint(string message)
        {
            HintChanged?.Invoke(message);
        }

        public void PlayCombatFeedback(
            bool damageApplied,
            bool critical,
            PlayerAttackReaction playerReaction)
        {
            combatFeedback.PlayResolvedAttack(
                damageApplied,
                critical,
                playerReaction);
        }

        public void TogglePause()
        {
            if (state == GameRunState.GameOver ||
                state == GameRunState.CardSelection)
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
            if (state != GameRunState.CardSelection ||
                pendingCardSelections <= 0)
            {
                return;
            }

            player.Critical.AddCard();
            pendingCardSelections--;
            if (pendingCardSelections > 0)
            {
                CriticalCardVisibilityChanged?.Invoke(false);
                CriticalCardVisibilityChanged?.Invoke(true);
                ShowCardSelectionHint();
                return;
            }

            bool completedStartingCards = selectingStartingCards;
            selectingStartingCards = false;
            CriticalCardVisibilityChanged?.Invoke(false);
            state = GameRunState.Playing;
            Time.timeScale = 1f;
            ShowHint(completedStartingCards
                ? "Starting cards selected. Game started."
                : "Critical chance +10% card applied.");
        }

        public void SimulateRewardedContinue()
        {
            if (state != GameRunState.GameOver || continueCount >= 2)
            {
                ShowHint("Continue is available after Game Over, up to two times.");
                return;
            }

            continueCount++;
            player.RestoreAfterContinue();
            enemyRecycler.RepositionAllNormalEnemies();

            state = GameRunState.Playing;
            GameOverVisibilityChanged?.Invoke(false);
            ShowHint(
                $"Rewarded ad simulated: Continue {continueCount}/2, " +
                "Player restored to full HP.");
        }

        public void DebugDamagePlayer()
        {
            if (IsPlaying)
            {
                player.ReceiveDamage(10);
                ShowHint("Debug: Player took 10 damage.");
            }
        }

        public void DebugGrantPlayerExperience()
        {
            if (!IsPlaying)
            {
                return;
            }

            player.Progression.AddExperience(5);
            ShowHint("Debug: Player EXP +5.");
        }

        private void OnPlayerDepleted()
        {
            if (state == GameRunState.GameOver)
            {
                return;
            }

            state = GameRunState.GameOver;
            pendingCardSelections = 0;
            selectingStartingCards = false;
            Time.timeScale = 1f;
            CriticalCardVisibilityChanged?.Invoke(false);
            GameOverVisibilityChanged?.Invoke(true);
            ShowHint(
                "Player defeated. CONTINUE simulates a successful rewarded ad.");
        }

        private void OnPlayerLevelUp()
        {
            if (state == GameRunState.GameOver)
            {
                return;
            }

            foreach (EnemyBase enemy in enemies)
            {
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.RefreshLevelLabel();
                }
            }

            QueueCardSelections(1, false);
        }

        public static int CalculateStartingCardSelectionCount(
            int currentAccountLevel)
        {
            return Mathf.Max(0, currentAccountLevel - 1);
        }

        private void QueueCardSelections(int count, bool startingCards)
        {
            if (count <= 0)
            {
                return;
            }

            pendingCardSelections += count;
            selectingStartingCards |= startingCards;
            state = GameRunState.CardSelection;
            Time.timeScale = 0f;
            CriticalCardVisibilityChanged?.Invoke(true);
            ShowCardSelectionHint();
        }

        private void ShowCardSelectionHint()
        {
            string source = selectingStartingCards
                ? $"ACCOUNT Lv.{accountLevel} START BONUS"
                : "LEVEL UP";
            ShowHint(
                $"{source}: select a card " +
                $"({pendingCardSelections} remaining).");
        }

        private void EnsureCombatFeedback()
        {
            CameraShakeController cameraShake =
                worldCamera.GetComponent<CameraShakeController>();
            if (cameraShake == null)
            {
                Debug.LogError(
                    "Main Camera requires CameraShakeController.",
                    worldCamera);
                return;
            }

            if (combatFeedback == null)
            {
                combatFeedback = GetComponent<CombatFeedbackController>();
            }

            if (combatFeedback == null)
            {
                Debug.LogError(
                    "PrototypeGameSession requires CombatFeedbackController.",
                    this);
                return;
            }

            combatFeedback.Configure(cameraShake, gameData.CombatFeedback);
        }

        private static float GetColliderRadius(Component owner)
        {
            CircleCollider2D circle = owner != null
                ? owner.GetComponent<CircleCollider2D>()
                : null;
            if (circle == null)
            {
                return 0f;
            }

            Vector3 scale = circle.transform.lossyScale;
            return circle.radius *
                Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        }
    }
}
