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
            balance.Configure(5, 1, 0.1f, 0.7f);

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
                    1)
            });

            var entries = schedule.CopyStageEntries("Stage01");

            Assert.That(entries.Count, Is.EqualTo(2));
            Assert.That(entries[0].SpawnPointId, Is.EqualTo("TOP_01"));
            Assert.That(entries[1].SpawnPointId, Is.EqualTo("TOP_02"));

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
        public void PlayerMoveSpeed_ReachesFifteenAfterFiveUpgrades()
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
                stats.AddMoveSpeed(1f);
            }

            Assert.That(stats.MoveSpeed, Is.EqualTo(15f));
            Assert.That(
                stats.MoveSpeed * stats.PathEnemyApproachSpeedMultiplier,
                Is.EqualTo(16.5f).Within(0.001f));
            Assert.That(
                stats.MoveSpeed * stats.PostKillEscapeSpeedMultiplier,
                Is.EqualTo(18f).Within(0.001f));
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

        [TestCase(1f, 10f)]
        [TestCase(10f, 100f)]
        [TestCase(20f, 200f)]
        public void MaximumMoveSpeed_ReachesDestinationInPointOneSeconds(
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
                Is.EqualTo(0.1f).Within(0.0001f));
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

        [TestCase(1, 0.1f, 1, 1f)]
        [TestCase(5, 0.22f, 5, 1.4f)]
        public void MovingSlash_UpgradeMathMatchesDesign(
            int level,
            float expectedChance,
            int expectedHits,
            float expectedSize)
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
        [TestCase(1, 2)]
        [TestCase(2, 4)]
        [TestCase(3, 6)]
        public void HitHeal_AmountIncreasesTwoPerCardLevel(
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

        [TestCase(1, 0, 0.2f, 0.4f, false, true)]
        [TestCase(1, 1, 0.2f, 0.4f, false, false)]
        [TestCase(1, 0, 0.4f, 0.4f, false, false)]
        [TestCase(1, 1, 0.4f, 0.4f, true, true)]
        [TestCase(0, 0, 0.4f, 0.4f, true, false)]
        public void Piercing_CommandCannotReopenExpiredWindowByItself(
            int level,
            int consumed,
            float currentTime,
            float windowEndsAt,
            bool canOpenWindow,
            bool expected)
        {
            Assert.That(
                PlayerCombatAbilities.CanConsumePiercingTarget(
                    level,
                    consumed,
                    currentTime,
                    windowEndsAt,
                    canOpenWindow),
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
                PlayerCombatAbilities.SeverDelay,
                Is.EqualTo(0.15f).Within(0.0001f));
            Assert.That(
                PlayerCombatAbilities.SeverReuseCooldown,
                Is.EqualTo(0.1f).Within(0.0001f));
        }

        [TestCase(true, true, true, true)]
        [TestCase(true, false, true, false)]
        [TestCase(true, true, false, false)]
        [TestCase(false, true, true, false)]
        public void Sever_TriggersOnlyAfterSuccessfulPiercing(
            bool hasSever,
            bool piercingAllowed,
            bool primaryDamaged,
            bool expected)
        {
            Assert.That(
                PlayerCombatAbilities.CanTriggerSever(
                    hasSever,
                    piercingAllowed,
                    primaryDamaged),
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
                    directObject.AddComponent<MeleeEnemy>();
                EnemyBase pathEnemy =
                    pathObject.AddComponent<MeleeEnemy>();

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
                    enemyObject.AddComponent<MeleeEnemy>();
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
            health.Configure(5.1f);

            bool applied = health.Apply(new CombatResult(
                3f,
                5.1f,
                PlayerAttackReaction.None));

            Assert.That(applied, Is.True);
            Assert.That(health.MaxHealth, Is.EqualTo(5.1f).Within(0.001f));
            Assert.That(
                health.CurrentHealth,
                Is.EqualTo(2.1f).Within(0.001f));
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
