using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SimpleGame
{
    public sealed class PrototypeGameSession : MonoBehaviour
    {
        public const float CardChoiceInputDelay = 0.7f;

        [Header("Scene References")]
        [SerializeField] private PlayerRoot player;
        [SerializeField] private PrototypeEnemyFactory enemyFactory;
        [SerializeField] private EnemyWorldService enemyWorld;
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

        private readonly Dictionary<string, int> cardStacks = new(
            StringComparer.Ordinal);
        private readonly List<LevelUpCardDefinition> currentCardChoices =
            new();
        private GameRunState state = GameRunState.Playing;
        private int pendingCardSelections;
        private bool selectingStartingCards;
        private int continueCount;
        private GameRunState stateBeforePause = GameRunState.Playing;
        private bool cardChoicesInteractable;
        private float cardChoiceUnlockAt;

        public event Action<string> HintChanged;
        public event Action<bool> CardSelectionVisibilityChanged;
        public event Action<IReadOnlyList<LevelUpCardChoiceData>>
            CardChoicesChanged;
        public event Action<bool> CardChoiceInteractivityChanged;
        public event Action<bool> PauseVisibilityChanged;
        public event Action<string> PauseDetailsChanged;
        public event Action<bool> GameOverVisibilityChanged;

        public PlayerRoot Player => player;
        public int Score { get; private set; }
        public int AccountExperience =>
            gameData != null && gameData.GlobalBalance != null
                ? gameData.GlobalBalance.CalculateAccountExperience(Score)
                : 0;
        public float ElapsedTime { get; private set; }
        public bool IsPlaying => state == GameRunState.Playing;

        public static string FormatElapsedTime(float elapsedTime)
        {
            int totalSeconds = Mathf.Max(
                0,
                Mathf.FloorToInt(elapsedTime));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        public void ConfigureScene(
            PlayerRoot configuredPlayer,
            PrototypeEnemyFactory configuredFactory,
            Transform configuredEnemyRoot,
            Camera configuredCamera,
            CombatFeedbackController configuredCombatFeedback,
            EnemyWorldRecycler configuredEnemyRecycler,
            PrototypeHUDPresenter configuredPresenter,
            EnemyWorldService configuredEnemyWorld)
        {
            player = configuredPlayer;
            enemyFactory = configuredFactory;
            enemyWorld = configuredEnemyWorld;
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

        public void ConfigureHud(
            PrototypeHUDPresenter configuredPresenter)
        {
            hudPresenter = configuredPresenter;
        }

        private void Start()
        {
            Time.timeScale = 1f;
            if (gameData == null ||
                !gameData.IsConfigured ||
                stageSpawner == null ||
                enemyRecycler == null ||
                enemyWorld == null)
            {
                Debug.LogError(
                    "PrototypeGameSession requires GameDataManifest " +
                    "StageSpawnController, EnemyWorldRecycler, and " +
                    "EnemyWorldService.",
                    this);
                enabled = false;
                return;
            }

            EnsureCombatFeedback();
            player.Configure(
                this,
                enemyWorld,
                worldCamera,
                gameData.PlayerLevelExperience,
                gameData.GlobalBalance,
                gameData.PlayerBalance,
                stageSpawner.SpawnPoints);
            enemyFactory.ConfigureAssets(
                gameData.EnemyAssets,
                gameData.EnemyBalance);
            enemyFactory.Configure(this, enemyWorld, enemyRoot);
            enemyRecycler.Configure(
                this,
                enemyWorld,
                player.GetComponent<PlayerWorldArea>());
            hudPresenter.Initialize(this);

            player.Health.Depleted += OnPlayerDepleted;
            player.Progression.LevelUpCardRequested += OnPlayerLevelUp;
            stageSpawner.Begin(stageId);
            ShowHint("10분 동안 생존하세요. 빈 곳을 누르면 이동하고 적을 누르면 공격합니다.");
            QueueCardSelections(
                CalculateStartingCardSelectionCount(accountLevel),
                true);
        }

        private void Update()
        {
            if (Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }

            if (state == GameRunState.CardSelection &&
                !cardChoicesInteractable &&
                Time.unscaledTime >= cardChoiceUnlockAt)
            {
                SetCardChoicesInteractable(true);
            }

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

        public void OnEnemyDefeated(EnemyBase enemy)
        {
            enemyWorld.Unregister(enemy);
            Score += enemy.Definition.Score;
            player.Progression.AddExperience(enemy.Definition.KillExperience);
        }

        public void ShowHint(string message)
        {
            HintChanged?.Invoke(message);
        }

        public void PlayCombatFeedback(
            bool damageApplied,
            bool targetDefeated,
            bool critical,
            PlayerAttackReaction playerReaction)
        {
            combatFeedback.PlayResolvedAttack(
                damageApplied,
                targetDefeated,
                critical,
                playerReaction);
        }

        public void TogglePause()
        {
            if (state == GameRunState.Paused)
            {
                state = stateBeforePause;
                Time.timeScale =
                    state == GameRunState.CardSelection ? 0f : 1f;
                PauseVisibilityChanged?.Invoke(false);
                ShowHint(state == GameRunState.CardSelection
                    ? "레벨 업: 카드를 선택하세요."
                    : "게임을 재개했습니다.");
                return;
            }

            stateBeforePause = state;
            state = GameRunState.Paused;
            Time.timeScale = 0f;
            PauseDetailsChanged?.Invoke(BuildPauseDetails());
            PauseVisibilityChanged?.Invoke(true);
            ShowHint("일시 정지했습니다. ESC를 누르면 재개합니다.");
        }

        public void SelectCard(int choiceIndex)
        {
            if (state != GameRunState.CardSelection ||
                !cardChoicesInteractable ||
                pendingCardSelections <= 0 ||
                choiceIndex < 0 ||
                choiceIndex >= currentCardChoices.Count)
            {
                return;
            }

            LevelUpCardDefinition selected =
                currentCardChoices[choiceIndex];
            if (!player.ApplyCard(selected))
            {
                return;
            }

            cardStacks[selected.CardId] = GetCardStack(selected.CardId) + 1;
            pendingCardSelections--;
            if (pendingCardSelections > 0)
            {
                ArmCardChoiceDelay();
                RefreshCardChoices();
                if (TryFinishEmptyCardSelection())
                {
                    return;
                }

                ShowCardSelectionHint();
                return;
            }

            bool completedStartingCards = selectingStartingCards;
            selectingStartingCards = false;
            currentCardChoices.Clear();
            SetCardChoicesInteractable(false);
            CardSelectionVisibilityChanged?.Invoke(false);
            state = GameRunState.Playing;
            Time.timeScale = 1f;
            ShowHint(completedStartingCards
                ? "시작 카드를 선택했습니다. 게임을 시작합니다."
                : $"{selected.DisplayName} 카드를 획득했습니다.");
        }

        public void SimulateRewardedContinue()
        {
            if (state != GameRunState.GameOver || continueCount >= 2)
            {
                ShowHint("이어하기는 게임 종료 후 최대 두 번 사용할 수 있습니다.");
                return;
            }

            continueCount++;
            player.RestoreAfterContinue();
            enemyRecycler.RepositionAllNormalEnemies();

            state = GameRunState.Playing;
            GameOverVisibilityChanged?.Invoke(false);
            ShowHint(
                $"보상형 광고를 확인한 것으로 처리했습니다. " +
                $"이어하기 {continueCount}/2, 체력을 모두 회복했습니다.");
        }

        public void DebugDamagePlayer()
        {
            if (IsPlaying)
            {
                player.ReceiveDamage(10);
                ShowHint("시험 기능: 플레이어가 피해 10을 받았습니다.");
            }
        }

        public void DebugGrantPlayerExperience()
        {
            if (!IsPlaying)
            {
                return;
            }

            player.Progression.AddExperience(5);
            ShowHint("시험 기능: 플레이어 경험치가 5 증가했습니다.");
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
            currentCardChoices.Clear();
            SetCardChoicesInteractable(false);
            PauseVisibilityChanged?.Invoke(false);
            CardSelectionVisibilityChanged?.Invoke(false);
            GameOverVisibilityChanged?.Invoke(true);
            ShowHint("플레이어가 쓰러졌습니다. 이어하기로 다시 도전할 수 있습니다.");
        }

        private void OnPlayerLevelUp()
        {
            if (state == GameRunState.GameOver)
            {
                return;
            }

            player.Health.RestoreFull();
            foreach (EnemyBase enemy in enemyWorld.Enemies)
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
            ArmCardChoiceDelay();
            RefreshCardChoices();
            if (TryFinishEmptyCardSelection())
            {
                return;
            }

            CardSelectionVisibilityChanged?.Invoke(true);
            ShowCardSelectionHint();
        }

        private void RefreshCardChoices()
        {
            currentCardChoices.Clear();
            int unlockLevel = selectingStartingCards
                ? Mathf.Max(player.Progression.Level, accountLevel)
                : player.Progression.Level;
            currentCardChoices.AddRange(gameData.LevelUpCards.Draw(
                unlockLevel,
                GetCardStack,
                3));

            var choices = new List<LevelUpCardChoiceData>(
                currentCardChoices.Count);
            foreach (LevelUpCardDefinition choice in currentCardChoices)
            {
                choices.Add(new LevelUpCardChoiceData(
                    choice,
                    GetCardStack(choice.CardId)));
            }

            CardChoicesChanged?.Invoke(choices);
        }

        private void ArmCardChoiceDelay()
        {
            cardChoiceUnlockAt =
                Time.unscaledTime + CardChoiceInputDelay;
            SetCardChoicesInteractable(false);
        }

        private void SetCardChoicesInteractable(bool interactable)
        {
            cardChoicesInteractable = interactable;
            CardChoiceInteractivityChanged?.Invoke(interactable);
        }

        private string BuildPauseDetails()
        {
            var text = new StringBuilder();
            text.AppendLine("일시 정지");
            text.AppendLine("ESC를 누르면 게임을 재개합니다.");
            text.AppendLine();
            text.AppendLine(
                $"플레이어 레벨  {player.Progression.Level}    " +
                $"계정 레벨  {accountLevel}");
            text.AppendLine(
                $"점수  {Score}    계정 경험치  " +
                $"{AccountExperience}    생존 시간  " +
                FormatElapsedTime(ElapsedTime));
            text.AppendLine(
                $"체력  {player.Health.CurrentHealth}/" +
                $"{player.Health.MaxHealth}    " +
                $"공격력  {player.AttackPower:0.##}");
            text.AppendLine(
                $"치명타 확률  {player.Critical.Chance * 100f:0}%    " +
                $"이동 속도  {player.MoveSpeed:0.##}");
            text.AppendLine(
                $"공격 사거리  {player.AttackRange:0.##}    " +
                $"후면 피해  x{player.RearAttackMultiplier:0.##}");
            if (player.Progression.TryGetRequiredExperience(
                    out int requiredExperience))
            {
                text.AppendLine(
                    $"경험치  {player.Progression.Experience}/" +
                    $"{requiredExperience}    " +
                    $"다음 레벨까지  " +
                    $"{Mathf.Max(0, requiredExperience - player.Progression.Experience)}");
            }

            text.AppendLine();
            text.AppendLine("획득한 스킬");
            bool hasSelectedCard = false;
            foreach (LevelUpCardDefinition definition
                in gameData.LevelUpCards.Definitions)
            {
                int stack = GetCardStack(definition.CardId);
                if (stack <= 0)
                {
                    continue;
                }

                hasSelectedCard = true;
                text.Append("• ");
                text.Append(definition.DisplayName);
                text.Append("  레벨 ");
                text.Append(stack);
                text.Append('/');
                text.AppendLine(definition.MaxStack.ToString());
            }

            if (!hasSelectedCard)
            {
                text.AppendLine("없음");
            }

            return text.ToString();
        }

        private int GetCardStack(string cardId)
        {
            return cardStacks.TryGetValue(cardId, out int count)
                ? count
                : 0;
        }

        private void ShowCardSelectionHint()
        {
            string source = selectingStartingCards
                ? $"계정 레벨 {accountLevel} 시작 보너스"
                : "레벨 업";
            ShowHint(
                $"{source}: 카드를 선택하세요. " +
                $"남은 선택 {pendingCardSelections}회");
        }

        private bool TryFinishEmptyCardSelection()
        {
            if (currentCardChoices.Count > 0)
            {
                return false;
            }

            pendingCardSelections = 0;
            selectingStartingCards = false;
            SetCardChoicesInteractable(false);
            CardSelectionVisibilityChanged?.Invoke(false);
            state = GameRunState.Playing;
            Time.timeScale = 1f;
            ShowHint("모든 스킬이 최대 레벨에 도달했습니다.");
            return true;
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
                combatFeedback =
                    GetComponentInChildren<CombatFeedbackController>(
                        true);
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

    }
}
