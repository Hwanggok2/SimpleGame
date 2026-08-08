using UnityEngine;

namespace SimpleGame
{
    public sealed partial class PrototypeGameSession
    {
        public void OnEnemyDefeated(
            EnemyBase enemy,
            bool critical = false)
        {
            GameManager.Instance.PlaySoundEffect(
                GameAudioIds.EnemyDeath(enemy.Definition.EnemyId));
            combatFeedback?.PlayDefeatingHit(critical);
            bool isBoss = enemy.Archetype == EnemyArchetype.Boss;
            bool isMushroomBoss =
                PrototypeEnemyDefinitions.IsMushroomBoss(
                    enemy.Definition.EnemyId);
            Vector2 defeatedPosition = enemy.transform.position;
            enemyWorld.Unregister(enemy);
            Score += enemy.Definition.Score;
            int experience = Mathf.Max(
                0,
                Mathf.RoundToInt(
                    enemy.Definition.KillExperience *
                    (activeDifficulty != null
                        ? activeDifficulty.EnemyExperienceMultiplier
                        : 1f)));
            player.Progression.AddExperience(experience);
            if (isMushroomBoss)
            {
                poisonCloudSpawner?.Schedule(defeatedPosition);
            }

            if (activeDifficulty != null &&
                string.Equals(
                    enemy.Definition.EnemyId,
                    activeDifficulty.FinalBossId,
                    System.StringComparison.Ordinal))
            {
                BeginClear(defeatedPosition);
                return;
            }

            if (isBoss)
            {
                remainingCardRerolls =
                    CalculateBossRewardRerolls(
                        remainingCardRerolls,
                        GetBossRerollReward(),
                        GetMaximumStoredCardRerolls());
                QueueCardSelections(1, false, true);
            }
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

        public void ShowDamagePopup(
            Vector3 worldPosition,
            int amount,
            DamagePopupStyle style)
        {
            combatFeedback?.ShowDamagePopup(
                worldPosition,
                amount,
                style);
        }

        public void SelectDifficulty(GameDifficulty difficulty)
        {
            if (state != GameRunState.DifficultySelection)
            {
                return;
            }

            Difficulty = difficulty;
            gameData.LobbyDifficulties.TryGet(
                difficulty,
                out activeDifficulty);
            ElapsedTime = 0f;
            state = GameRunState.Playing;
            Time.timeScale = 1f;
            stageSpawner.Begin(stageId, difficulty);
            player.ApplyDifficultyModifiers(
                activeDifficulty != null
                    ? activeDifficulty.PlayerMaxHealthMultiplier
                    : 1f,
                activeDifficulty != null
                    ? activeDifficulty.AutoAttackSpeedMultiplier
                    : 1f);
            DifficultySelectionVisibilityChanged?.Invoke(false);
            ShowHint(FormatString(
                GameStringIds.HintGameStartedFormat,
                "{0} 난이도로 시작합니다. 왼쪽 조이스틱으로 " +
                "조준하고 공격 버튼을 누르세요.",
                ResolveDifficultyDisplayName(difficulty)));
            QueueCardSelections(
                CalculateStartingCardSelectionCount(accountLevel),
                true);
        }

        public static string GetDifficultyDisplayName(
            GameDifficulty difficulty)
        {
            return difficulty switch
            {
                GameDifficulty.Easy => "쉬움",
                GameDifficulty.Normal => "보통",
                GameDifficulty.Hard => "어려움",
                _ => difficulty.ToString()
            };
        }

        public string ResolveDifficultyDisplayName(
            GameDifficulty difficulty)
        {
            return GetString(
                GameStringIds.DifficultyName(difficulty),
                GetDifficultyDisplayName(difficulty));
        }

        public void SimulateRewardedContinue()
        {
            if (state != GameRunState.GameOver || continueCount >= 2)
            {
                ShowHint(GetString(
                    GameStringIds.HintContinueLimit,
                    "이어하기는 게임 종료 후 최대 두 번 사용할 수 있습니다."));
                return;
            }

            continueCount++;
            player.RestoreAfterContinue();
            enemyRecycler.PushAwayAllNormalEnemies();

            state = GameRunState.Playing;
            GameOverVisibilityChanged?.Invoke(false);
            ShowHint(FormatString(
                GameStringIds.HintContinueSuccessFormat,
                "보상형 광고를 확인한 것으로 처리했습니다. " +
                "이어하기 {0}/2, 체력을 모두 회복했습니다.",
                continueCount));
        }

        public void DebugDamagePlayer()
        {
            if (IsPlaying)
            {
                player.ReceiveDamage(100);
                ShowHint(GetString(
                    GameStringIds.HintDebugDamage,
                    "시험 기능: 플레이어가 피해 100을 받았습니다."));
            }
        }

        public void DebugGrantPlayerExperience()
        {
            if (!IsPlaying)
            {
                return;
            }

            player.Progression.AddExperience(5);
            ShowHint(GetString(
                GameStringIds.HintDebugExperience,
                "시험 기능: 플레이어 경험치가 5 증가했습니다."));
        }

        private void OnPlayerDepleted()
        {
            if (state == GameRunState.GameOver ||
                state == GameRunState.Clear)
            {
                return;
            }

            state = GameRunState.GameOver;
            pendingCardSelections = 0;
            pendingBossRewardSelections = 0;
            selectingStartingCards = false;
            Time.timeScale = 1f;
            currentCardChoices.Clear();
            currentCardHistory.Clear();
            SetCardChoicesInteractable(false);
            PauseVisibilityChanged?.Invoke(false);
            CardSelectionVisibilityChanged?.Invoke(false);
            GameOverVisibilityChanged?.Invoke(true);
            ShowHint(GetString(
                GameStringIds.HintPlayerDefeated,
                "플레이어가 쓰러졌습니다. 이어하기로 다시 도전할 수 있습니다."));
        }

        private void OnPlayerLevelUp()
        {
            if (state == GameRunState.GameOver ||
                state == GameRunState.Clear)
            {
                return;
            }

            player.Health.Heal(LevelUpHealAmount);
            foreach (EnemyBase enemy in enemyWorld.Enemies)
            {
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.RefreshLevelLabel();
                }
            }

            QueueCardSelections(1, false);
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
