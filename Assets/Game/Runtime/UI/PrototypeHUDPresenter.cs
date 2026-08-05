using UnityEngine;

namespace SimpleGame
{
    public sealed class PrototypeHUDPresenter : MonoBehaviour
    {
        [SerializeField] private PrototypeHUDView view;
        private PrototypeGameSession session;
        private float nextRefreshAt;

        public void Configure(PrototypeHUDView hudView)
        {
            view = hudView;
        }

        public void Initialize(PrototypeGameSession gameSession)
        {
            session = gameSession;
            view.Initialize(
                session.GameStrings,
                session.ControlSettingsProfile);
            view.SetDifficultyContext(
                session.StageDisplayName,
                session.StageDescription);
            view.InitializeAimControls(session.Player);

            view.Bind(HudButtonId.CardChoice0, () => session.SelectCard(0));
            view.Bind(HudButtonId.CardChoice1, () => session.SelectCard(1));
            view.Bind(HudButtonId.CardChoice2, () => session.SelectCard(2));
            view.Bind(HudButtonId.CardReroll0, () => session.RerollCard(0));
            view.Bind(HudButtonId.CardReroll1, () => session.RerollCard(1));
            view.Bind(HudButtonId.CardReroll2, () => session.RerollCard(2));
            view.Bind(HudButtonId.Settings, session.TogglePause);
            view.Bind(HudButtonId.ContinueAd, session.SimulateRewardedContinue);
            view.Bind(
                HudButtonId.Attack,
                () => session.Player.ExecuteControlAction());
            view.Bind(
                HudButtonId.DifficultyEasy,
                () => session.SelectDifficulty(GameDifficulty.Easy));
            view.Bind(
                HudButtonId.DifficultyNormal,
                () => session.SelectDifficulty(GameDifficulty.Normal));
            view.Bind(
                HudButtonId.DifficultyHard,
                () => session.SelectDifficulty(GameDifficulty.Hard));
            view.Bind(HudButtonId.Retry, session.RestartRun);
            view.Bind(HudButtonId.ReturnToLobby, session.ReturnToLobby);

            session.HintChanged += OnHintChanged;
            session.CardSelectionVisibilityChanged += view.ShowCardSelection;
            session.CardChoicesChanged += view.SetCardChoices;
            session.CardChoiceInteractivityChanged +=
                view.SetCardChoicesInteractable;
            session.CardRerollStateChanged += view.SetCardRerollState;
            session.DifficultySelectionVisibilityChanged +=
                view.ShowDifficultySelection;
            session.PauseVisibilityChanged += view.ShowPauseDetails;
            session.PauseDetailsChanged += view.SetPauseDetails;
            session.GameOverVisibilityChanged += view.ShowGameOver;
            session.ClearVisibilityChanged += OnClearVisibilityChanged;
            view.SetCardRerollState(
                session.RemainingCardRerolls,
                false);
            Refresh();
        }

        private void Update()
        {
            if (session != null && Time.unscaledTime >= nextRefreshAt)
            {
                nextRefreshAt = Time.unscaledTime + 0.1f;
                Refresh();
            }
        }

        private void OnDestroy()
        {
            if (session == null)
            {
                return;
            }

            session.HintChanged -= OnHintChanged;
            if (view == null)
            {
                return;
            }

            session.CardSelectionVisibilityChanged -= view.ShowCardSelection;
            session.CardChoicesChanged -= view.SetCardChoices;
            session.CardChoiceInteractivityChanged -=
                view.SetCardChoicesInteractable;
            session.CardRerollStateChanged -= view.SetCardRerollState;
            session.DifficultySelectionVisibilityChanged -=
                view.ShowDifficultySelection;
            session.PauseVisibilityChanged -= view.ShowPauseDetails;
            session.PauseDetailsChanged -= view.SetPauseDetails;
            session.GameOverVisibilityChanged -= view.ShowGameOver;
            session.ClearVisibilityChanged -= OnClearVisibilityChanged;
            view.InitializeAimControls(null);
        }

        private void OnHintChanged(string message)
        {
            view.SetText(HudTextId.Hint, message);
        }

        private void OnClearVisibilityChanged(bool visible)
        {
            if (visible)
            {
                RefreshResultSummary(true);
            }

            view.ShowClear(visible);
        }

        private void Refresh()
        {
            view.SetText(
                HudTextId.Time,
                PrototypeGameSession.FormatElapsedTime(
                    session.ElapsedTime));
            if (session.Player.Progression.TryGetRequiredExperience(
                    out int requiredExperience))
            {
                view.SetExperience(
                    session.Player.Progression.Experience,
                    requiredExperience);
            }
            else
            {
                view.SetExperience(0, 0);
            }

            RefreshBossHealth();

            RefreshResultSummary(session.IsClear);
        }

        private void RefreshResultSummary(bool clear)
        {
            view.SetGameOverDetails(session.FormatString(
                clear
                    ? GameStringIds.HudClearSummaryFormat
                    : GameStringIds.HudGameOverSummaryFormat,
                clear
                    ? "클리어!\n점수 {0} / 계정 경험치 {1}"
                    : "게임 종료\n점수 {0} / 계정 경험치 {1}",
                session.Score,
                session.AccountExperience));
        }

        private void RefreshBossHealth()
        {
            EnemyBase visibleBoss = null;
            if (session.EnemyWorld != null)
            {
                foreach (EnemyBase enemy in session.EnemyWorld.Enemies)
                {
                    if (enemy != null &&
                        enemy.IsAlive &&
                        enemy.Archetype == EnemyArchetype.Boss)
                    {
                        visibleBoss = enemy;
                        break;
                    }
                }
            }

            if (visibleBoss == null)
            {
                view.SetBossHealth(string.Empty, 0, 1, false);
                return;
            }

            string fallbackName =
                PrototypeEnemyDefinitions.GetDisplayName(
                    visibleBoss.Definition.EnemyId,
                    visibleBoss.Archetype);
            string bossName = session.GetString(
                GameStringIds.EnemyName(
                    visibleBoss.Definition.EnemyId),
                fallbackName);
            view.SetBossHealth(
                bossName,
                visibleBoss.CurrentHealth,
                visibleBoss.MaxHealth,
                true);
        }
    }
}
