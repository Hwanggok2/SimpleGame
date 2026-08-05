using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace SimpleGame.Tests
{
    public sealed class GameDataTests
    {
        [Test]
        public void GlobalBalance_UsesFloorForAccountExperience()
        {
            GlobalBalance balance =
                ScriptableObject.CreateInstance<GlobalBalance>();
            balance.Configure(5, 1, 0.1f, 0.7f, 5, 9, 1);

            Assert.That(balance.CalculateAccountExperience(19), Is.EqualTo(3));

            Object.DestroyImmediate(balance);
        }

        [Test]
        public void StageSchedule_FiltersAndSortsByTimeThenIndex()
        {
            StageSpawnSchedule schedule =
                ScriptableObject.CreateInstance<StageSpawnSchedule>();
            schedule.Configure(new[]
            {
                new StageSpawnEntry(
                    "Stage01",
                    "WAVE_02",
                    20f,
                    2,
                    "TOP_02",
                    "GoblinMelee",
                    1),
                new StageSpawnEntry(
                    "Stage02",
                    "WAVE_01",
                    1f,
                    1,
                    "LEFT_01",
                    "GoblinMelee",
                    1),
                new StageSpawnEntry(
                    "Stage01",
                    "WAVE_01",
                    1f,
                    1,
                    "TOP_01",
                    "GoblinMelee",
                    1),
                new StageSpawnEntry(
                    "Stage01",
                    "WAVE_01",
                    1f,
                    1,
                    "BOTTOM_01",
                    "GoblinMelee",
                    1,
                    GameDifficulty.Easy)
            });

            var entries = schedule.CopyStageEntries("Stage01");
            var easyEntries = schedule.CopyStageEntries(
                "Stage01",
                GameDifficulty.Easy);

            Assert.That(entries.Count, Is.EqualTo(2));
            Assert.That(entries[0].SpawnPointId, Is.EqualTo("TOP_01"));
            Assert.That(entries[1].SpawnPointId, Is.EqualTo("TOP_02"));
            Assert.That(easyEntries, Has.Count.EqualTo(1));
            Assert.That(
                easyEntries[0].SpawnPointId,
                Is.EqualTo("BOTTOM_01"));

            Object.DestroyImmediate(schedule);
        }

        [TestCase("WAVE_01", 1)]
        [TestCase("WAVE_08", 8)]
        [TestCase("WAVE_60", 60)]
        public void StageSpawnEntry_ParsesWaveNumber(
            string waveId,
            int expected)
        {
            var entry = new StageSpawnEntry(
                "Stage01",
                waveId,
                1f,
                1,
                "TOP_01",
                "GoblinMelee",
                1);

            Assert.That(entry.WaveNumber, Is.EqualTo(expected));
        }

        [Test]
        public void LevelTable_ReturnsConfiguredRequirement()
        {
            LevelExperienceTable table =
                ScriptableObject.CreateInstance<LevelExperienceTable>();
            table.Configure(new[]
            {
                new LevelExperienceRow(1, 5),
                new LevelExperienceRow(2, 7)
            });

            Assert.That(
                table.TryGetRequiredExperience(2, out int required),
                Is.True);
            Assert.That(required, Is.EqualTo(7));
            Assert.That(
                table.TryGetRequiredExperience(3, out _),
                Is.False);

            Object.DestroyImmediate(table);
        }

        [Test]
        public void PrototypeEnemyIds_AreStableForExcelMapping()
        {
            Assert.That(
                PrototypeEnemyDefinitions.GetEnemyId(EnemyArchetype.Melee),
                Is.EqualTo("GoblinMelee"));
            Assert.That(
                PrototypeEnemyDefinitions.GetEnemyId(EnemyArchetype.Shield),
                Is.EqualTo("ShieldSkeleton"));
            Assert.That(
                PrototypeEnemyDefinitions.GetEnemyId(EnemyArchetype.Boss),
                Is.EqualTo("GoblinBoss"));
            Assert.That(
                PrototypeEnemyDefinitions
                    .Create(EnemyArchetype.Shield)
                    .FacingTurnDelay,
                Is.EqualTo(0.5f));
            Assert.That(
                PrototypeEnemyDefinitions
                    .Create(EnemyArchetype.Melee)
                    .FacingTurnDelay,
                Is.EqualTo(0.5f));
            Assert.That(
                PrototypeEnemyDefinitions
                    .Create(EnemyArchetype.Ranged)
                    .PostAttackFacingLock,
                Is.EqualTo(1f));
            Assert.That(
                PrototypeEnemyDefinitions
                    .Create(EnemyArchetype.Ranged)
                    .AttackCooldown,
                Is.EqualTo(2f));
        }

        [Test]
        public void ShieldFacing_WaitsBeforeTurningToTheOppositeSide()
        {
            var owner = new GameObject("ShieldFacingTest");
            EnemyFacing facing = owner.AddComponent<EnemyFacing>();
            facing.Configure(0.5f);

            facing.Face(Vector2.right, 0f);
            facing.Face(Vector2.left, 0.1f);
            Assert.That(facing.Direction.x, Is.GreaterThan(0f));

            facing.Face(Vector2.left, 0.59f);
            Assert.That(facing.Direction.x, Is.GreaterThan(0f));

            facing.Face(Vector2.left, 0.6f);
            Assert.That(facing.Direction.x, Is.LessThan(0f));

            Object.DestroyImmediate(owner);
        }

        [TestCase(1, 0)]
        [TestCase(2, 1)]
        [TestCase(3, 2)]
        public void StartingCardCount_IsAccountLevelMinusOne(
            int accountLevel,
            int expectedSelections)
        {
            Assert.That(
                PrototypeGameSession.CalculateStartingCardSelectionCount(
                    accountLevel),
                Is.EqualTo(expectedSelections));
        }

        [Test]
        public void CardRerollBudget_StartsAtFiveAndStoresNine()
        {
            Assert.That(
                PrototypeGameSession.DefaultInitialCardRerolls,
                Is.EqualTo(5));
            Assert.That(
                PrototypeGameSession.DefaultMaximumStoredCardRerolls,
                Is.EqualTo(9));
            Assert.That(
                PrototypeGameSession.DefaultBossRerollReward,
                Is.EqualTo(1));
        }

        [TestCase(0, 1)]
        [TestCase(5, 6)]
        [TestCase(8, 9)]
        [TestCase(9, 9)]
        public void BossReward_AddsOneRerollWithoutExceedingBudget(
            int currentRerolls,
            int expectedRerolls)
        {
            Assert.That(
                PrototypeGameSession.CalculateBossRewardRerolls(
                    currentRerolls),
                Is.EqualTo(expectedRerolls));
        }

        [Test]
        public void LevelUpCardTable_DrawsThreeDistinctEligibleCards()
        {
            LevelUpCardTable table =
                ScriptableObject.CreateInstance<LevelUpCardTable>();
            table.Configure(new[]
            {
                CreateCard("A", 1),
                CreateCard("B", 1),
                CreateCard("C", 2),
                CreateCard("D", 3)
            });

            var choices = table.Draw(3, _ => 0, 3);

            Assert.That(choices, Has.Count.EqualTo(3));
            Assert.That(
                new System.Collections.Generic.HashSet<string>(
                    System.Linq.Enumerable.Select(
                        choices,
                        choice => choice.CardId)),
                Has.Count.EqualTo(3));
            Object.DestroyImmediate(table);
        }

        [Test]
        public void LevelUpCardTable_RerollExcludesEveryVisibleCard()
        {
            LevelUpCardTable table =
                ScriptableObject.CreateInstance<LevelUpCardTable>();
            table.Configure(new[]
            {
                CreateCard("A", 1),
                CreateCard("B", 1),
                CreateCard("C", 1),
                CreateCard("D", 1)
            });
            var visibleCardIds = new HashSet<string>
            {
                "A",
                "B",
                "C"
            };

            var replacement = table.Draw(
                1,
                _ => 0,
                1,
                visibleCardIds);

            Assert.That(replacement, Has.Count.EqualTo(1));
            Assert.That(replacement[0].CardId, Is.EqualTo("D"));
            Assert.That(
                table.HasEligibleCard(
                    1,
                    _ => 0,
                    visibleCardIds),
                Is.True);

            visibleCardIds.Add("D");
            Assert.That(
                table.Draw(1, _ => 0, 1, visibleCardIds),
                Is.Empty);
            Assert.That(
                table.HasEligibleCard(
                    1,
                    _ => 0,
                    visibleCardIds),
                Is.False);
            Object.DestroyImmediate(table);
        }

        [Test]
        public void GameSession_RerollsOneSlotAndConsumesSharedBudget()
        {
            LevelUpCardTable table =
                ScriptableObject.CreateInstance<LevelUpCardTable>();
            GameDataManifest manifest =
                ScriptableObject.CreateInstance<GameDataManifest>();
            var playerObject = new GameObject("RerollPlayer");
            var sessionObject = new GameObject("RerollSession");
            playerObject.SetActive(false);
            sessionObject.SetActive(false);
            try
            {
                LevelUpCardDefinition cardA = CreateCard("A", 1);
                LevelUpCardDefinition cardB = CreateCard("B", 1);
                LevelUpCardDefinition cardC = CreateCard("C", 1);
                LevelUpCardDefinition cardD = CreateCard("D", 1);
                LevelUpCardDefinition cardE = CreateCard("E", 1);
                LevelUpCardDefinition cardF = CreateCard("F", 1);
                LevelUpCardDefinition cardG = CreateCard("G", 1);
                LevelUpCardDefinition cardH = CreateCard("H", 1);
                table.Configure(new[] { cardA, cardB, cardC });
                manifest.Configure(
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    table,
                    null,
                    null);

                PlayerRoot player =
                    playerObject.AddComponent<PlayerRoot>();
                SetPrivateField(
                    player,
                    "progression",
                    playerObject.GetComponent<PlayerProgression>());
                PrototypeGameSession session =
                    sessionObject.AddComponent<PrototypeGameSession>();
                session.ConfigureScene(
                    player,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null);
                session.ConfigureData(manifest, null);
                SetPrivateField(
                    session,
                    "state",
                    GameRunState.CardSelection);
                SetPrivateField(
                    session,
                    "pendingCardSelections",
                    1);
                SetPrivateField(
                    session,
                    "cardChoicesInteractable",
                    true);
                List<LevelUpCardDefinition> choices =
                    GetPrivateField<List<LevelUpCardDefinition>>(
                        session,
                        "currentCardChoices");
                choices.Add(cardA);
                choices.Add(cardB);
                choices.Add(cardC);

                session.RerollCard(0);
                Assert.That(choices[0].CardId, Is.EqualTo("A"));
                Assert.That(session.RemainingCardRerolls, Is.EqualTo(5));

                table.Configure(new[]
                {
                    cardA,
                    cardB,
                    cardC,
                    cardD,
                    cardE,
                    cardF,
                    cardG,
                    cardH
                });
                var shownCardIds = new HashSet<string>
                {
                    "A",
                    "B",
                    "C"
                };
                for (int reroll = 0; reroll < 5; reroll++)
                {
                    session.RerollCard(0);
                    Assert.That(
                        shownCardIds.Add(choices[0].CardId),
                        Is.True,
                        "A reroll must not return a card already shown " +
                        "during the current selection.");
                }

                Assert.That(session.RemainingCardRerolls, Is.Zero);
                string cardAfterBudgetSpent = choices[0].CardId;

                session.RerollCard(0);
                Assert.That(
                    choices[0].CardId,
                    Is.EqualTo(cardAfterBudgetSpent));
                Assert.That(session.RemainingCardRerolls, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(sessionObject);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(manifest);
                Object.DestroyImmediate(table);
            }
        }

        [Test]
        public void LevelUpCardTable_RequiresPrerequisiteCard()
        {
            LevelUpCardTable table =
                ScriptableObject.CreateInstance<LevelUpCardTable>();
            table.Configure(new[]
            {
                CreateCard("PIERCING_UP", 1),
                CreateCard("SEVER_TRAIL", 1, "PIERCING_UP")
            });

            var locked = table.Draw(1, _ => 0, 2);
            var unlocked = table.Draw(
                1,
                cardId => cardId == "PIERCING_UP" ? 1 : 0,
                2);

            Assert.That(
                System.Linq.Enumerable.Any(
                    locked,
                    card => card.CardId == "SEVER_TRAIL"),
                Is.False);
            Assert.That(
                System.Linq.Enumerable.Any(
                    unlocked,
                    card => card.CardId == "SEVER_TRAIL"),
                Is.True);
            Object.DestroyImmediate(table);
        }

        [Test]
        public void PlayerMoveSpeed_IncreasesLinearlyByHalfPerUpgrade()
        {
            var owner = new GameObject("PlayerMoveSpeedTest");
            PlayerStats stats = owner.AddComponent<PlayerStats>();
            stats.Configure(new PlayerDefinition(
                "LightBandit",
                1,
                10,
                1f,
                0.65f,
                3f,
                10f,
                1.1f,
                1.2f,
                0.08f,
                1.2f,
                0f,
                true));

            Assert.That(stats.MoveSpeed, Is.EqualTo(10f));
            for (int index = 0; index < 5; index++)
            {
                stats.AddMoveSpeed(0.5f);
            }

            Assert.That(stats.MoveSpeed, Is.EqualTo(12.5f));
            Assert.That(
                stats.MoveSpeed * stats.PathEnemyApproachSpeedMultiplier,
                Is.EqualTo(13.75f).Within(0.001f));
            Assert.That(
                stats.MoveSpeed * stats.PostKillEscapeSpeedMultiplier,
                Is.EqualTo(15f).Within(0.001f));
            Object.DestroyImmediate(owner);
        }

        [TestCase(1, 3)]
        [TestCase(3, 7)]
        [TestCase(5, 11)]
        public void StaticCharge_TargetCountMatchesDesign(
            int level,
            int expectedTargets)
        {
            Assert.That(
                PlayerCombatAbilities
                    .CalculateStaticAdjacentTargetCount(level),
                Is.EqualTo(expectedTargets));
        }

        [TestCase(0f, "00:00")]
        [TestCase(59.9f, "00:59")]
        [TestCase(810f, "13:30")]
        public void GameSession_FormatsElapsedTime(
            float elapsed,
            string expected)
        {
            Assert.That(
                PrototypeGameSession.FormatElapsedTime(elapsed),
                Is.EqualTo(expected));
        }

        [TestCase(1f, 6.666667f)]
        [TestCase(10f, 66.66667f)]
        [TestCase(20f, 133.3333f)]
        public void MaximumMoveSpeed_ReachesDestinationInPointOneFiveSeconds(
            float distance,
            float expectedSpeed)
        {
            float speed =
                PlayerMovement.CalculateMaximumTravelSpeed(distance);

            Assert.That(
                speed,
                Is.EqualTo(expectedSpeed).Within(0.001f));
            Assert.That(
                distance / speed,
                Is.EqualTo(0.15f).Within(0.0001f));
        }

        [TestCase(0, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 4)]
        [TestCase(3, 5)]
        [TestCase(4, 5)]
        public void FlyingSword_MaximumHitsStartsAtTwoAndCapsAtFive(
            int level,
            int expected)
        {
            Assert.That(
                FlyingSwordController.CalculateMaximumHits(level),
                Is.EqualTo(expected));
        }

        [TestCase(0.099f, 0.1f, false)]
        [TestCase(0.1f, 0.1f, true)]
        public void FlyingSword_LaunchIntervalOpensAtPointOneSeconds(
            float currentTime,
            float nextLaunchAt,
            bool expected)
        {
            Assert.That(
                FlyingSwordController.IsLaunchReady(
                    currentTime,
                    nextLaunchAt),
                Is.EqualTo(expected));
        }

        [TestCase(0.299f, 0.3f, false)]
        [TestCase(0.3f, 0.3f, true)]
        public void FlyingSword_SlotCooldownOpensAtPointThreeSeconds(
            float currentTime,
            float readyAt,
            bool expected)
        {
            Assert.That(
                FlyingSwordController.IsSlotReady(
                    currentTime,
                    readyAt),
                Is.EqualTo(expected));
        }

        [Test]
        public void FlyingSword_ThreeSlotsSustainPointOneSecondCadence()
        {
            Assert.That(
                FlyingSwordController.RechargeDuration,
                Is.EqualTo(
                    FlyingSwordController.LaunchInterval *
                    FlyingSwordController.MaximumSwordCount)
                    .Within(0.0001f));
            Assert.That(
                FlyingSwordController.PostTargetTravelDuration,
                Is.EqualTo(0.1f).Within(0.0001f));
        }

        [TestCase(0.1f, 1f)]
        [TestCase(0.05f, 0.5f)]
        [TestCase(0f, 0f)]
        [TestCase(-0.1f, 0f)]
        public void FlyingSword_FadeAlphaFallsLinearlyAfterPrimaryHit(
            float remainingDuration,
            float expectedAlpha)
        {
            Assert.That(
                FlyingSwordController.CalculateFadeAlpha(
                    remainingDuration),
                Is.EqualTo(expectedAlpha).Within(0.0001f));
        }

        [TestCase(0, 0f, 0, 0f, 0f, 0f)]
        [TestCase(1, 0.15f, 2, 1f, 6f, 1.8f)]
        [TestCase(2, 0.195f, 3, 1.15f, 7.5f, 2.15f)]
        [TestCase(3, 0.24f, 4, 1.3f, 9f, 2.5f)]
        [TestCase(4, 0.285f, 5, 1.45f, 10.5f, 2.85f)]
        [TestCase(5, 0.33f, 6, 1.6f, 12f, 3.2f)]
        [TestCase(6, 0.33f, 6, 1.6f, 12f, 3.2f)]
        public void MovingSlash_UpgradeMathMatchesDesign(
            int level,
            float expectedChance,
            int expectedHits,
            float expectedSize,
            float expectedDistance,
            float expectedDamage)
        {
            Assert.That(
                PlayerCombatAbilities.CalculateMovingSlashChance(level),
                Is.EqualTo(expectedChance).Within(0.0001f));
            Assert.That(
                PlayerCombatAbilities
                    .CalculateMovingSlashMaximumHits(level),
                Is.EqualTo(expectedHits));
            Assert.That(
                PlayerCombatAbilities.CalculateMovingSlashSize(level),
                Is.EqualTo(expectedSize).Within(0.0001f));
            Assert.That(
                PlayerCombatAbilities
                    .CalculateMovingSlashTravelDistance(level),
                Is.EqualTo(expectedDistance).Within(0.0001f));
            Assert.That(
                PlayerCombatAbilities
                    .CalculateMovingSlashDamageMultiplier(level),
                Is.EqualTo(expectedDamage).Within(0.0001f));
        }

        [TestCase(10f, 6f, false, 30f)]
        [TestCase(15f, 12f, false, 45f)]
        [TestCase(15f, 6f, true, 40f)]
        [TestCase(15f, 12f, true, 80f)]
        public void MovingSlash_SpeedUsesItsOwnTravelDistance(
            float playerMoveSpeed,
            float travelDistance,
            bool maximumSpeedActive,
            float expectedSpeed)
        {
            Assert.That(
                MovingSlashProjectile.CalculateTravelSpeed(
                    playerMoveSpeed,
                    travelDistance,
                    maximumSpeedActive),
                Is.EqualTo(expectedSpeed).Within(0.0001f));
        }

        [TestCase(0f, 6f, 0)]
        [TestCase(0.99f, 6f, 0)]
        [TestCase(1f, 6f, 1)]
        [TestCase(5.99f, 6f, 5)]
        [TestCase(6f, 6f, 5)]
        [TestCase(12f, 12f, 5)]
        public void MovingSlash_AnimationTracksTravelProgress(
            float distanceTravelled,
            float travelDistance,
            int expectedFrame)
        {
            Assert.That(
                MovingSlashProjectile.CalculateAnimationFrameIndex(
                    distanceTravelled,
                    travelDistance),
                Is.EqualTo(expectedFrame));
        }

        [TestCase(1, 5f, 6f, 0.5f, false)]
        [TestCase(0, 1f, 6f, 0.1f, true)]
        [TestCase(1, 6f, 6f, 0.1f, true)]
        [TestCase(1, 1f, 6f, 1.5f, true)]
        public void MovingSlash_FadeHasAForcedLifetimeLimit(
            int remainingHits,
            float distanceTravelled,
            float travelDistance,
            float activeElapsed,
            bool expected)
        {
            Assert.That(
                MovingSlashProjectile.ShouldBeginFade(
                    remainingHits,
                    distanceTravelled,
                    travelDistance,
                    activeElapsed),
                Is.EqualTo(expected));
        }

        [TestCase(0f, 1f)]
        [TestCase(0.05f, 0.5f)]
        [TestCase(0.1f, 0f)]
        [TestCase(1f, 0f)]
        public void MovingSlash_FadeAlphaDecreasesLinearly(
            float elapsed,
            float expectedAlpha)
        {
            Assert.That(
                MovingSlashProjectile.CalculateFadeAlpha(elapsed),
                Is.EqualTo(expectedAlpha).Within(0.0001f));
        }

        [TestCase(0, 0f)]
        [TestCase(1, 0.1f)]
        [TestCase(2, 0.2f)]
        [TestCase(3, 0.3f)]
        public void ShieldBypass_ChanceIncreasesTenPercentPerLevel(
            int level,
            float expected)
        {
            Assert.That(
                PlayerCombatAbilities.CalculateShieldBypassChance(level),
                Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void LevelUpCard_RarityColorsAreVisuallyDistinct()
        {
            Color common =
                LevelUpCardView.ResolveRarityColor("일반");
            Color rare =
                LevelUpCardView.ResolveRarityColor("희귀");
            Color hero =
                LevelUpCardView.ResolveRarityColor("영웅");

            Assert.That(rare, Is.Not.EqualTo(common));
            Assert.That(hero, Is.Not.EqualTo(common));
            Assert.That(hero, Is.Not.EqualTo(rare));
        }

        [TestCase(0, 0)]
        [TestCase(1, 20)]
        [TestCase(2, 40)]
        [TestCase(3, 60)]
        public void HitHeal_AmountIncreasesTwentyPerCardLevel(
            int level,
            int expected)
        {
            Assert.That(
                PlayerCombatAbilities.CalculateHitHealAmount(level),
                Is.EqualTo(expected));
            Assert.That(
                PlayerCombatAbilities.HitHealChance,
                Is.EqualTo(0.05f).Within(0.0001f));
        }

        [TestCase(true, 1, 0.049f, true)]
        [TestCase(true, 1, 0.05f, false)]
        [TestCase(false, 1, 0f, false)]
        [TestCase(true, 0, 0f, false)]
        public void HitHeal_TriggersOnlyAfterEnemyDefeat(
            bool targetDefeated,
            int level,
            float randomValue,
            bool expected)
        {
            Assert.That(
                PlayerCombatAbilities.CanTriggerHitHeal(
                    targetDefeated,
                    level,
                    randomValue),
                Is.EqualTo(expected));
        }

        [TestCase(1, 0, 1)]
        [TestCase(1, 1, 0)]
        [TestCase(5, 1, 4)]
        [TestCase(5, 5, 0)]
        public void Piercing_WindowBudgetPreservesCardLevelDifference(
            int level,
            int consumed,
            int expectedRemaining)
        {
            Assert.That(
                PlayerCombatAbilities
                    .CalculateRemainingPiercingTargets(
                        level,
                        consumed),
                Is.EqualTo(expectedRemaining));
            Assert.That(
                PlayerCombatAbilities.PiercingWindowDuration,
                Is.EqualTo(0.4f).Within(0.0001f));
        }

        [TestCase(0.39f, 0.4f, false)]
        [TestCase(0.4f, 0.4f, true)]
        public void Piercing_WindowRefreshesOnlyAfterExpiration(
            float currentTime,
            float windowEndsAt,
            bool expected)
        {
            Assert.That(
                PlayerCombatAbilities.ShouldRefreshPiercingWindow(
                    currentTime,
                    windowEndsAt),
                Is.EqualTo(expected));
        }

        [TestCase(0.099f, 0.1f, false)]
        [TestCase(0.1f, 0.1f, true)]
        public void Sever_CooldownDiscardsEarlyTrigger(
            float currentTime,
            float nextAvailableTime,
            bool expected)
        {
            Assert.That(
                PlayerCombatAbilities.IsSeverCooldownReady(
                    currentTime,
                    nextAvailableTime),
                Is.EqualTo(expected));
            Assert.That(
                PlayerCombatAbilities.SeverReuseCooldown,
                Is.EqualTo(0.1f).Within(0.0001f));
        }

        [TestCase(0.9f, 0f, false)]
        [TestCase(1f, 0f, true)]
        [TestCase(2f, 0f, true)]
        [TestCase(2f, 1.1f, false)]
        public void Sever_TriggersWhenPlayerClearsPiercedEnemy(
            float playerX,
            float playerY,
            bool expected)
        {
            Assert.That(
                PlayerController.HasClearedSeverPass(
                    new Vector2(playerX, playerY),
                    Vector2.zero,
                    Vector2.right,
                    1f),
                Is.EqualTo(expected));
        }

        [TestCase(0f, 1f)]
        [TestCase(0.05f, 0.5f)]
        [TestCase(0.1f, 0f)]
        [TestCase(0.2f, 0f)]
        public void Sever_VisualFadeMatchesFlyingSword(
            float elapsed,
            float expectedAlpha)
        {
            Assert.That(
                SlashTrailEffect.CalculateFadeAlpha(
                    elapsed,
                    PlayerCombatAbilities.SeverTrailFadeDuration),
                Is.EqualTo(expectedAlpha).Within(0.0001f));
            Assert.That(
                PlayerCombatAbilities.SeverTrailFadeDuration,
                Is.EqualTo(
                    FlyingSwordController.PostTargetTravelDuration)
                    .Within(0.0001f));
        }

        [Test]
        public void SeverVisualPool_SameFrameShowsKeepIndependentSegments()
        {
            DestroySeverTrailObjects();
            var templateObject =
                new GameObject("SeverVisualPoolTemplate");
            SpriteRenderer template =
                templateObject.AddComponent<SpriteRenderer>();
            var texture = new Texture2D(8, 8);
            var pixels = new Color[64];
            for (int index = 0; index < pixels.Length; index++)
            {
                pixels[index] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 8f, 8f),
                new Vector2(0.5f, 0.5f),
                8f,
                0u,
                SpriteMeshType.FullRect,
                new Vector4(2f, 0f, 2f, 0f));
            template.sprite = sprite;
            template.drawMode = SpriteDrawMode.Sliced;
            template.size = new Vector2(1f, 0.9f);
            templateObject.SetActive(false);

            Vector2[] starts =
            {
                Vector2.zero,
                new(0f, 1f),
                new(-2f, -2f)
            };
            Vector2[] ends =
            {
                new(2f, 0f),
                new(0f, 4f),
                new(2f, 2f)
            };

            try
            {
                for (int index = 0; index < starts.Length; index++)
                {
                    SlashTrailEffect.Show(
                        template,
                        starts[index],
                        ends[index],
                        10f);
                }

                var activeEffects = new List<SlashTrailEffect>();
                var instanceIds = new HashSet<int>();
                foreach (SlashTrailEffect effect in
                         Resources.FindObjectsOfTypeAll<
                             SlashTrailEffect>())
                {
                    if (effect == null ||
                        effect.gameObject.name != "SeverTrail" ||
                        !effect.gameObject.activeSelf)
                    {
                        continue;
                    }

                    activeEffects.Add(effect);
                    instanceIds.Add(effect.GetInstanceID());
                }

                Assert.That(activeEffects, Has.Count.EqualTo(3));
                Assert.That(instanceIds, Has.Count.EqualTo(3));
                for (int index = 0; index < starts.Length; index++)
                {
                    Vector2 expectedMidpoint =
                        (starts[index] + ends[index]) * 0.5f;
                    float expectedLength =
                        Vector2.Distance(starts[index], ends[index]);
                    bool found = false;
                    foreach (SlashTrailEffect effect in activeEffects)
                    {
                        if (Vector2.Distance(
                                effect.transform.position,
                                expectedMidpoint) > 0.0001f)
                        {
                            continue;
                        }

                        SpriteRenderer renderer =
                            effect.GetComponent<SpriteRenderer>();
                        Assert.That(renderer.drawMode,
                            Is.EqualTo(SpriteDrawMode.Sliced));
                        Assert.That(
                            renderer.size.x,
                            Is.EqualTo(expectedLength)
                                .Within(0.0001f));
                        Assert.That(
                            effect.transform.localScale,
                            Is.EqualTo(Vector3.one));
                        LineRenderer line =
                            effect.GetComponent<LineRenderer>();
                        Assert.That(line, Is.Not.Null);
                        Assert.That(
                            (Vector2)line.GetPosition(0),
                            Is.EqualTo(starts[index]));
                        Assert.That(
                            (Vector2)line.GetPosition(1),
                            Is.EqualTo(ends[index]));
                        found = true;
                        break;
                    }

                    Assert.That(
                        found,
                        Is.True,
                        $"Missing sever segment {index}.");
                }
            }
            finally
            {
                DestroySeverTrailObjects();
                Object.DestroyImmediate(templateObject);
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void Sever_DamagesEnemiesOverlappingItsSegment()
        {
            Assert.That(
                CombatGeometry.OverlapsSegment(
                    new Vector2(2f, 0.4f),
                    0.25f,
                    Vector2.zero,
                    new Vector2(4f, 0f),
                    PlayerCombatAbilities.SeverHalfWidth),
                Is.True);
            Assert.That(
                CombatGeometry.OverlapsSegment(
                    new Vector2(2f, 0.5f),
                    0.25f,
                    Vector2.zero,
                    new Vector2(4f, 0f),
                    PlayerCombatAbilities.SeverHalfWidth),
                Is.False);
            Assert.That(
                CombatGeometry.OverlapsSegment(
                    new Vector2(4.5f, 0f),
                    0.25f,
                    Vector2.zero,
                    new Vector2(4f, 0f),
                    PlayerCombatAbilities.SeverHalfWidth),
                Is.False);
        }

        [Test]
        public void EnemySeparation_PushesOverlappingPositionOutside()
        {
            Vector2 resolved = CombatGeometry.PushOutside(
                Vector2.zero,
                1,
                Vector2.zero,
                2,
                1.25f);

            Assert.That(
                Vector2.Distance(resolved, Vector2.zero),
                Is.EqualTo(1.25f).Within(0.001f));
        }

        [TestCase(2f, 0f, 1f, 0f, true)]
        [TestCase(-2f, 0f, 1f, 0f, false)]
        [TestCase(0f, -2f, 1f, 0f, false)]
        public void PlayerCommand_OnlySelectsEnemiesInInputDirection(
            float destinationX,
            float destinationY,
            float enemyX,
            float enemyY,
            bool expected)
        {
            Assert.That(
                PlayerController.IsTargetInCommandDirection(
                    Vector2.zero,
                    new Vector2(destinationX, destinationY),
                    new Vector2(enemyX, enemyY)),
                Is.EqualTo(expected));
        }

        [TestCase(true, false, false, true)]
        [TestCase(false, true, true, true)]
        [TestCase(false, true, false, false)]
        [TestCase(false, false, true, false)]
        [TestCase(false, false, false, false)]
        public void PlayerCommand_ContinuesOnlyAfterKillOrExecutedPiercingAttack(
            bool targetDefeated,
            bool piercingReserved,
            bool attackExecuted,
            bool expected)
        {
            Assert.That(
                PlayerController.ShouldContinueAfterPathAttack(
                    targetDefeated,
                    piercingReserved,
                    attackExecuted),
                Is.EqualTo(expected));
        }

        [TestCase(1f, 0f, 2.49f, 0f, false)]
        [TestCase(1f, 0f, 2.5f, 0f, false)]
        [TestCase(1f, 0f, 2.51f, 0f, true)]
        [TestCase(0f, 1f, 0f, 2.99f, false)]
        [TestCase(0f, 1f, 0f, 3f, false)]
        [TestCase(0f, 1f, 0f, 3.01f, true)]
        [TestCase(1f, 0f, -1f, 0f, false)]
        [TestCase(1f, 0f, 1.75f, 1f, false)]
        [TestCase(1f, 0f, 1.75f, 1.8f, true)]
        public void PlayerCommand_PiercesOnlyBeyondEnemyArea(
            float targetX,
            float targetY,
            float destinationX,
            float destinationY,
            bool expected)
        {
            Assert.That(
                PlayerController.EnemyPiercingHorizontalRadius,
                Is.EqualTo(1.5f));
            Assert.That(
                PlayerController.EnemyPiercingVerticalRadius,
                Is.EqualTo(2f));
            Assert.That(
                PlayerController.IsPiercingTouchRequested(
                    Vector2.zero,
                    new Vector2(targetX, targetY),
                    new Vector2(
                        destinationX,
                        destinationY)),
                Is.EqualTo(expected));
        }

        [Test]
        public void PlayerCommand_PathEnemyTakesPriorityOverDirectTouch()
        {
            var directObject = new GameObject("DirectEnemy");
            var pathObject = new GameObject("PathEnemy");
            try
            {
                EnemyBase directEnemy =
                    directObject.AddComponent<EnemyActor>();
                EnemyBase pathEnemy =
                    pathObject.AddComponent<EnemyActor>();

                Assert.That(
                    PlayerController.SelectCommandEnemy(
                        directEnemy,
                        pathEnemy),
                    Is.SameAs(pathEnemy));
                Assert.That(
                    PlayerController.SelectCommandEnemy(
                        directEnemy,
                        null),
                    Is.SameAs(directEnemy));
            }
            finally
            {
                Object.DestroyImmediate(directObject);
                Object.DestroyImmediate(pathObject);
            }
        }

        [Test]
        public void EnemyVisuals_DisableLegacyFacingMarker()
        {
            var enemyObject = new GameObject("Enemy");
            var markerObject = new GameObject("FacingMarker");
            markerObject.transform.SetParent(enemyObject.transform);
            try
            {
                EnemyBase enemy =
                    enemyObject.AddComponent<EnemyActor>();
                SpriteRenderer marker =
                    markerObject.AddComponent<SpriteRenderer>();
                marker.enabled = true;

                enemy.ConfigureVisuals(
                    null,
                    marker,
                    null,
                    null);

                Assert.That(marker.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(enemyObject);
            }
        }

        [TestCase(EnemyArchetype.Melee)]
        [TestCase(EnemyArchetype.Ranged)]
        [TestCase(EnemyArchetype.Shield)]
        [TestCase(EnemyArchetype.Boss)]
        public void EnemyActor_UsesConfiguredArchetype(
            EnemyArchetype archetype)
        {
            var enemyObject = new GameObject("Enemy");
            try
            {
                EnemyActor enemy =
                    enemyObject.AddComponent<EnemyActor>();

                enemy.ConfigureArchetype(archetype);

                Assert.That(enemy.Archetype, Is.EqualTo(archetype));
            }
            finally
            {
                Object.DestroyImmediate(enemyObject);
            }
        }

        [Test]
        public void Health_HealAddsWithoutExceedingMaximum()
        {
            var owner = new GameObject("HealTest");
            HealthComponent health = owner.AddComponent<HealthComponent>();
            health.Configure(10);
            health.ApplyDamage(7);

            Assert.That(health.Heal(5), Is.EqualTo(5));
            Assert.That(health.CurrentHealth, Is.EqualTo(8));
            Assert.That(health.Heal(5), Is.EqualTo(2));
            Assert.That(health.CurrentHealth, Is.EqualTo(10));
            Object.DestroyImmediate(owner);
        }

        [Test]
        public void HealthPickup_HealsFiveAndIsNotSpentAtFullHealth()
        {
            var playerObject = new GameObject("PickupPlayer");
            var pickupObject = new GameObject("HealthPickup");
            try
            {
                HealthComponent health =
                    playerObject.AddComponent<HealthComponent>();
                PlayerRoot player =
                    playerObject.AddComponent<PlayerRoot>();
                SetPrivateField(player, "health", health);
                health.Configure(10);

                HealthPickup pickup =
                    pickupObject.AddComponent<HealthPickup>();
                pickup.Configure(null);
                Assert.That(pickup.TryCollect(player), Is.False);
                Assert.That(pickupObject != null, Is.True);

                health.ApplyDamage(7);
                Assert.That(pickup.TryCollect(player), Is.True);
                Assert.That(health.CurrentHealth, Is.EqualTo(8));
            }
            finally
            {
                if (pickupObject != null)
                {
                    Object.DestroyImmediate(pickupObject);
                }

                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void LevelUpAndWorldHealingAmountsMatchDesign()
        {
            Assert.That(
                PrototypeGameSession.LevelUpHealAmount,
                Is.EqualTo(20));
            Assert.That(HealthPickup.HealAmount, Is.EqualTo(50));
            Assert.That(HealthPickup.Lifetime, Is.EqualTo(45f));
            Assert.That(HealthPickupSpawner.SpawnInterval, Is.EqualTo(20f));
            Assert.That(
                HealthPickupSpawner.MaximumActivePickups,
                Is.EqualTo(3));
        }

        [TestCase(0f, 0f, -6f, -11f)]
        [TestCase(0.5f, 0.5f, 0f, 0f)]
        [TestCase(1f, 1f, 6f, 11f)]
        public void HealthPickupSpawn_StaysInsidePaddedWorldArea(
            float normalizedX,
            float normalizedY,
            float expectedX,
            float expectedY)
        {
            Vector2 position =
                HealthPickupSpawner.CalculateSpawnPosition(
                    Vector2.zero,
                    new Vector2(7f, 12f),
                    1f,
                    new Vector2(normalizedX, normalizedY));

            Assert.That(
                position.x,
                Is.EqualTo(expectedX).Within(0.001f));
            Assert.That(
                position.y,
                Is.EqualTo(expectedY).Within(0.001f));
        }

        [TestCase(0.49f, 0)]
        [TestCase(0.5f, 1)]
        [TestCase(1f, 2)]
        [TestCase(5f, 10)]
        public void MushroomPoison_TicksEveryHalfSecond(
            float exposureDuration,
            int expectedTicks)
        {
            Assert.That(
                MushroomPoisonCloud.CalculateTickCount(
                    exposureDuration),
                Is.EqualTo(expectedTicks));
            Assert.That(
                MushroomPoisonCloud.DamagePerTick,
                Is.EqualTo(10));
            Assert.That(
                MushroomPoisonCloud.Duration,
                Is.EqualTo(5f));
            Assert.That(
                MushroomPoisonCloud.SpawnDelay,
                Is.EqualTo(1f));
        }

        [Test]
        public void Health_RestoreFullFillsMissingHealth()
        {
            var owner = new GameObject("RestoreFullTest");
            HealthComponent health = owner.AddComponent<HealthComponent>();
            health.Configure(10);
            health.ApplyDamage(7);

            health.RestoreFull();

            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth));
            Object.DestroyImmediate(owner);
        }

        [Test]
        public void EnemyHealth_ReportsCurrentAndMaximumHealth()
        {
            var owner = new GameObject("EnemyHealthTest");
            EnemyHealth health = owner.AddComponent<EnemyHealth>();
            health.Configure(5);

            bool applied = health.Apply(new CombatResult(
                3,
                5,
                PlayerAttackReaction.None));

            Assert.That(applied, Is.True);
            Assert.That(health.MaxHealth, Is.EqualTo(5));
            Assert.That(health.CurrentHealth, Is.EqualTo(2));
            Object.DestroyImmediate(owner);
        }

        [Test]
        public void WorldArea_RepositionsEnemyOnOppositeInnerBoundary()
        {
            Vector2 result =
                PlayerWorldArea.CalculateOppositeSpawnPosition(
                    Vector2.zero,
                    new Vector2(20f, 0f),
                    new Vector2(7f, 12f),
                    0f);

            Assert.That(result.x, Is.EqualTo(-7f).Within(0.001f));
            Assert.That(result.y, Is.EqualTo(0f).Within(0.001f));
        }

        [TestCase(10, 5)]
        [TestCase(3, 2)]
        [TestCase(-1, 0)]
        public void Continue_DamagesHalfOfCurrentEnemyHealth(
            int currentHealth,
            int expectedDamage)
        {
            Assert.That(
                EnemyWorldRecycler.CalculateContinueDamage(
                    currentHealth),
                Is.EqualTo(expectedDamage));
        }

        [TestCase(2f, 0f, 7f, 0f)]
        [TestCase(-2f, 0f, -7f, 0f)]
        [TestCase(0f, 2f, 0f, 12f)]
        public void WorldArea_ContinuePushesTowardSameSideBoundary(
            float currentX,
            float currentY,
            float expectedX,
            float expectedY)
        {
            Vector2 result =
                PlayerWorldArea.CalculateOutwardSpawnPosition(
                    Vector2.zero,
                    new Vector2(currentX, currentY),
                    new Vector2(7f, 12f),
                    0f);

            Assert.That(
                result.x,
                Is.EqualTo(expectedX).Within(0.001f));
            Assert.That(
                result.y,
                Is.EqualTo(expectedY).Within(0.001f));
        }

        [Test]
        public void WorldArea_ContinueDoesNotPullDistantEnemyInward()
        {
            Vector2 current = new(20f, 0f);
            Vector2 result =
                PlayerWorldArea.CalculateOutwardSpawnPosition(
                    Vector2.zero,
                    current,
                    new Vector2(7f, 12f),
                    0f);

            Assert.That(result, Is.EqualTo(current));
        }

        [Test]
        public void FacingImmediate_BypassesPendingTurnDelay()
        {
            var owner = new GameObject("ImmediateFacingTest");
            EnemyFacing facing = owner.AddComponent<EnemyFacing>();
            facing.Configure(0.5f);
            facing.Face(Vector2.right, 0f);
            facing.Face(Vector2.left, 0.1f);

            facing.FaceImmediate(Vector2.left);

            Assert.That(facing.Direction.x, Is.LessThan(0f));
            Object.DestroyImmediate(owner);
        }

        private static T GetPrivateField<T>(
            object target,
            string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (T)field.GetValue(target);
        }

        private static void SetPrivateField(
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private static void DestroySeverTrailObjects()
        {
            foreach (SlashTrailEffect effect in
                     Resources.FindObjectsOfTypeAll<SlashTrailEffect>())
            {
                if (effect != null &&
                    effect.gameObject.name == "SeverTrail")
                {
                    Object.DestroyImmediate(effect.gameObject);
                }
            }
        }

        private static LevelUpCardDefinition CreateCard(
            string cardId,
            int minimumLevel,
            string requiredCardId = "")
        {
            return new LevelUpCardDefinition(
                cardId,
                cardId,
                cardId,
                "테스트 카드 설명",
                LevelUpCardEffectType.StatModifier,
                PlayerStatId.MaxHp,
                StatOperation.Add,
                1f,
                3,
                1,
                minimumLevel,
                requiredCardId,
                "Common",
                "ICON",
                true);
        }
    }
}
