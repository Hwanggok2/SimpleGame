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

            view.Bind(HudButtonId.Pause, session.TogglePause);
            view.Bind(HudButtonId.CriticalCard, session.SelectCriticalCard);
            view.Bind(HudButtonId.ContinueAd, session.SimulateRewardedContinue);
            view.Bind(HudButtonId.DamageCastle, session.DebugDamageCastle);
            view.Bind(HudButtonId.GrantXp, session.DebugGrantPlayerExperience);

            session.HintChanged += OnHintChanged;
            session.CriticalCardVisibilityChanged += view.ShowCriticalCard;
            session.GameOverVisibilityChanged += view.ShowGameOver;
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
            session.CriticalCardVisibilityChanged -= view.ShowCriticalCard;
            session.GameOverVisibilityChanged -= view.ShowGameOver;
        }

        private void OnHintChanged(string message)
        {
            view.SetText(HudTextId.Hint, message);
        }

        private void Refresh()
        {
            view.SetText(
                HudTextId.Score,
                $"SCORE {session.Score}   ACCOUNT EXP {session.AccountExperience}");
            view.SetText(HudTextId.Time, $"TIME {session.ElapsedTime:0.0}");
            view.SetText(
                HudTextId.PlayerLevel,
                $"PLAYER Lv.{session.Player.Progression.Level}  XP {session.Player.Progression.Experience}");
            view.SetText(
                HudTextId.CriticalChance,
                $"CRIT {session.Player.Critical.Chance * 100f:0}%");
            view.SetText(
                HudTextId.PlayerHp,
                $"PLAYER HP {session.Player.Health.CurrentHealth}/{session.Player.Health.MaxHealth}");
            view.SetText(
                HudTextId.CastleHp,
                $"CASTLE HP {session.Castle.Health.CurrentHealth}/{session.Castle.Health.MaxHealth}");
            view.SetText(
                HudTextId.GameOverTitle,
                $"GAME OVER\nScore {session.Score} / Account EXP {session.AccountExperience}");
        }
    }
}
