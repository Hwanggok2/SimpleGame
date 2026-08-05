using System;
using System.Text;
using UnityEngine;

namespace SimpleGame
{
    public sealed partial class PrototypeGameSession
    {
        public void TogglePause()
        {
            if (state == GameRunState.DifficultySelection ||
                state == GameRunState.GameOver ||
                state == GameRunState.Clear)
            {
                return;
            }

            if (state == GameRunState.Paused)
            {
                state = stateBeforePause;
                Time.timeScale =
                    state == GameRunState.CardSelection ? 0f : 1f;
                PauseVisibilityChanged?.Invoke(false);
                ShowHint(state == GameRunState.CardSelection
                    ? GetString(
                        GameStringIds.HintResumeCardSelection,
                        "레벨 업: 카드를 선택하세요.")
                    : GetString(
                        GameStringIds.HintGameResumed,
                        "게임을 재개했습니다."));
                return;
            }

            stateBeforePause = state;
            state = GameRunState.Paused;
            Time.timeScale = 0f;
            PauseDetailsChanged?.Invoke(BuildPauseDetails());
            PauseVisibilityChanged?.Invoke(true);
            ShowHint(GetString(
                GameStringIds.HintGamePaused,
                "일시 정지했습니다. ESC를 누르면 재개합니다."));
        }

        private PauseDetailsData BuildPauseDetails()
        {
            string playerOverview = FormatString(
                GameStringIds.PausePlayerOverviewFormat,
                "플레이어 레벨: {0}\n난이도: {1}\n" +
                "점수: {2}\n생존 시간: {3}",
                player.Progression.Level,
                ResolveDifficultyDisplayName(Difficulty),
                Score,
                FormatElapsedTime(ElapsedTime));

            return new PauseDetailsData(
                playerOverview,
                BuildAccountOverview(),
                BuildPauseStats(),
                BuildPauseSkills());
        }

        private string BuildAccountOverview()
        {
            int earnedExperience = AccountExperience;
            if (gameData != null &&
                gameData.AccountLevelExperience != null &&
                gameData.AccountLevelExperience.TryGetRequiredExperience(
                    accountLevel,
                    out int requiredExperience) &&
                requiredExperience > 0)
            {
                return FormatString(
                    GameStringIds.PauseAccountOverviewFormat,
                    "계정 레벨: {0}\n이번 게임 획득 경험치: {1}\n" +
                    "다음 계정 레벨 진행도: {2}/{3}",
                    accountLevel,
                    earnedExperience,
                    Mathf.Min(earnedExperience, requiredExperience),
                    requiredExperience);
            }

            return FormatString(
                GameStringIds.PauseAccountMaxFormat,
                "계정 레벨: {0}\n이번 게임 획득 경험치: {1}\n" +
                "다음 계정 레벨 진행도: MAX",
                accountLevel,
                earnedExperience);
        }

        private string BuildPauseStats()
        {
            var text = new StringBuilder();
            text.AppendLine(FormatString(
                GameStringIds.PauseVitalsFormat,
                "체력  {0}/{1}    공격력  {2:0.##}",
                player.Health.CurrentHealth,
                player.Health.MaxHealth,
                player.AttackPower));
            text.AppendLine(FormatString(
                GameStringIds.PauseMobilityFormat,
                "치명타 확률  {0:0}%    이동 속도  {1:0.##}",
                player.Critical.Chance * 100f,
                player.MoveSpeed));
            text.Append(FormatString(
                GameStringIds.PauseCombatFormat,
                "공격 사거리  {0:0.##}    후면 피해  x{1:0.##}",
                player.AttackRange,
                player.RearAttackMultiplier));
            return text.ToString();
        }

        private string BuildPauseSkills()
        {
            var text = new StringBuilder();
            bool hasSelectedCard = false;
            for (int rarityOrder = 0; rarityOrder <= 4; rarityOrder++)
            {
                foreach (LevelUpCardDefinition definition
                    in gameData.LevelUpCards.Definitions)
                {
                    int stack = GetCardStack(definition.CardId);
                    if (stack <= 0 ||
                        GetRaritySortOrder(definition.Rarity) != rarityOrder)
                    {
                        continue;
                    }

                    hasSelectedCard = true;
                    text.Append("• ");
                    text.Append(definition.ResolveDisplayName(GameStrings));
                    text.AppendLine(FormatString(
                        GameStringIds.PauseSkillLevelFormat,
                        "  레벨 {0}/{1}",
                        stack,
                        definition.MaxStack));
                }
            }

            if (!hasSelectedCard)
            {
                text.Append(GetString(
                    GameStringIds.CommonNone,
                    "없음"));
            }

            return text.ToString().TrimEnd();
        }

        private static int GetRaritySortOrder(string rarity)
        {
            string value = rarity?.Trim();
            if (string.Equals(value, "일반", StringComparison.Ordinal) ||
                string.Equals(value, "Common", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            if (string.Equals(value, "희귀", StringComparison.Ordinal) ||
                string.Equals(value, "레어", StringComparison.Ordinal) ||
                string.Equals(value, "Rare", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (string.Equals(value, "에픽", StringComparison.Ordinal) ||
                string.Equals(value, "영웅", StringComparison.Ordinal) ||
                string.Equals(value, "Epic", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Hero", StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            if (string.Equals(value, "레전더리", StringComparison.Ordinal) ||
                string.Equals(value, "전설", StringComparison.Ordinal) ||
                string.Equals(value, "Legendary", StringComparison.OrdinalIgnoreCase))
            {
                return 3;
            }

            return 4;
        }
    }
}
