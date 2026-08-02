using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    public sealed partial class PrototypeGameSession
    {
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

            ConsumeFusionIngredients(selected);
            cardStacks[selected.CardId] = GetCardStack(selected.CardId) + 1;
            if (pendingBossRewardSelections > 0)
            {
                pendingBossRewardSelections--;
            }

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
            currentCardHistory.Clear();
            SetCardChoicesInteractable(false);
            CardSelectionVisibilityChanged?.Invoke(false);
            state = GameRunState.Playing;
            Time.timeScale = 1f;
            ShowHint(completedStartingCards
                ? GetString(
                    GameStringIds.HintStartingCardSelected,
                    "시작 카드를 선택했습니다. 게임을 시작합니다.")
                : FormatString(
                    GameStringIds.HintCardAcquiredFormat,
                    "{0} 카드를 획득했습니다.",
                    selected.ResolveDisplayName(GameStrings)));
        }

        public void RerollCard(int choiceIndex)
        {
            if (state != GameRunState.CardSelection ||
                !cardChoicesInteractable ||
                pendingCardSelections <= 0 ||
                remainingCardRerolls <= 0 ||
                choiceIndex < 0 ||
                choiceIndex >= currentCardChoices.Count)
            {
                return;
            }

            HashSet<string> excludedCardIds =
                BuildRerollExcludedCardIds();
            currentCardHistory.UnionWith(excludedCardIds);
            List<LevelUpCardDefinition> replacements =
                gameData.LevelUpCards.Draw(
                    GetCardUnlockLevel(),
                    GetCardStack,
                    1,
                    excludedCardIds);
            if (replacements.Count == 0)
            {
                PublishCardRerollState();
                return;
            }

            currentCardChoices[choiceIndex] = replacements[0];
            currentCardHistory.Add(replacements[0].CardId);
            remainingCardRerolls--;
            PublishCardChoices();
            PublishCardRerollState();
        }

        public static int CalculateStartingCardSelectionCount(
            int currentAccountLevel)
        {
            return Mathf.Max(0, currentAccountLevel - 1);
        }

        public static int CalculateBossRewardRerolls(
            int currentRerolls,
            int reward = DefaultBossRerollReward,
            int maximumStored = DefaultMaximumStoredCardRerolls)
        {
            return Mathf.Clamp(
                currentRerolls + Mathf.Max(0, reward),
                0,
                Mathf.Max(0, maximumStored));
        }

        private int GetInitialCardRerolls()
        {
            return gameData?.GlobalBalance != null
                ? gameData.GlobalBalance.InitialCardRerolls
                : DefaultInitialCardRerolls;
        }

        private int GetMaximumStoredCardRerolls()
        {
            return gameData?.GlobalBalance != null
                ? gameData.GlobalBalance.MaximumStoredCardRerolls
                : DefaultMaximumStoredCardRerolls;
        }

        private int GetBossRerollReward()
        {
            return gameData?.GlobalBalance != null
                ? gameData.GlobalBalance.BossRerollReward
                : DefaultBossRerollReward;
        }

        private void QueueCardSelections(
            int count,
            bool startingCards,
            bool bossReward = false)
        {
            if (count <= 0)
            {
                return;
            }

            pendingCardSelections += count;
            if (bossReward)
            {
                pendingBossRewardSelections += count;
            }

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
            currentCardHistory.Clear();
            currentCardChoices.AddRange(gameData.LevelUpCards.Draw(
                GetCardUnlockLevel(),
                GetCardStack,
                3));
            foreach (LevelUpCardDefinition choice in currentCardChoices)
            {
                currentCardHistory.Add(choice.CardId);
            }

            PublishCardChoices();
            PublishCardRerollState();
        }

        private void PublishCardChoices()
        {
            var choices = new List<LevelUpCardChoiceData>(
                currentCardChoices.Count);
            foreach (LevelUpCardDefinition choice in currentCardChoices)
            {
                choices.Add(new LevelUpCardChoiceData(
                    choice,
                    GetCardStack(choice.CardId),
                    GameStrings));
            }

            CardChoicesChanged?.Invoke(choices);
        }

        private void PublishCardRerollState()
        {
            bool hasAlternative =
                remainingCardRerolls > 0 &&
                currentCardChoices.Count > 0 &&
                gameData.LevelUpCards.HasEligibleCard(
                    GetCardUnlockLevel(),
                    GetCardStack,
                    BuildRerollExcludedCardIds());
            CardRerollStateChanged?.Invoke(
                remainingCardRerolls,
                hasAlternative);
        }

        private int GetCardUnlockLevel()
        {
            return selectingStartingCards
                ? Mathf.Max(player.Progression.Level, accountLevel)
                : player.Progression.Level;
        }

        private HashSet<string> BuildRerollExcludedCardIds()
        {
            var cardIds = new HashSet<string>(
                currentCardHistory,
                StringComparer.Ordinal);
            foreach (LevelUpCardDefinition choice
                     in currentCardChoices)
            {
                cardIds.Add(choice.CardId);
            }

            return cardIds;
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

        private int GetCardStack(string cardId)
        {
            return cardStacks.TryGetValue(cardId, out int count)
                ? count
                : 0;
        }

        private void ConsumeFusionIngredients(
            LevelUpCardDefinition selected)
        {
            if (selected == null ||
                selected.EffectType != LevelUpCardEffectType.Fusion)
            {
                return;
            }

            foreach (string ingredientCardId
                     in selected.FusionIngredientCardIds)
            {
                cardStacks.Remove(ingredientCardId);
            }
        }

        private void ShowCardSelectionHint()
        {
            if (pendingBossRewardSelections > 0)
            {
                ShowHint(FormatString(
                    GameStringIds.HintCardSelectBossFormat,
                    "보스 처치 보상: 카드를 선택하세요. 남은 선택 {0}회",
                    pendingCardSelections));
                return;
            }

            if (selectingStartingCards)
            {
                ShowHint(FormatString(
                    GameStringIds.HintCardSelectAccountFormat,
                    "계정 레벨 {0} 시작 보너스: 카드를 선택하세요. " +
                    "남은 선택 {1}회",
                    accountLevel,
                    pendingCardSelections));
                return;
            }

            ShowHint(FormatString(
                GameStringIds.HintCardSelectLevelUpFormat,
                "레벨 업: 카드를 선택하세요. 남은 선택 {0}회",
                pendingCardSelections));
        }

        private bool TryFinishEmptyCardSelection()
        {
            if (currentCardChoices.Count > 0)
            {
                return false;
            }

            pendingCardSelections = 0;
            pendingBossRewardSelections = 0;
            selectingStartingCards = false;
            currentCardHistory.Clear();
            SetCardChoicesInteractable(false);
            CardSelectionVisibilityChanged?.Invoke(false);
            state = GameRunState.Playing;
            Time.timeScale = 1f;
            ShowHint(GetString(
                GameStringIds.HintAllSkillsMaxed,
                "모든 스킬이 최대 레벨에 도달했습니다."));
            return true;
        }
    }
}
