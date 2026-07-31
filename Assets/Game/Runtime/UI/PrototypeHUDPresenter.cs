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
            view.Initialize();
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
                () => session.Player.ExecuteAimedCommand());
            view.Bind(
                HudButtonId.DifficultyEasy,
                () => session.SelectDifficulty(GameDifficulty.Easy));
            view.Bind(
                HudButtonId.DifficultyNormal,
                () => session.SelectDifficulty(GameDifficulty.Normal));

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
            view.InitializeAimControls(null);
        }

        private void OnHintChanged(string message)
        {
            view.SetText(HudTextId.Hint, message);
        }

        private void Refresh()
        {
            view.SetText(
                HudTextId.Time,
                PrototypeGameSession.FormatElapsedTime(
                    session.ElapsedTime));
            view.SetText(
                HudTextId.PlayerHp,
                $"체력  {session.Player.Health.CurrentHealth}/{session.Player.Health.MaxHealth}");
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

            view.SetGameOverDetails(
                $"게임 종료\n점수 {session.Score} / " +
                $"계정 경험치 {session.AccountExperience}");
        }
    }
}
