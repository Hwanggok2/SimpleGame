using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace SimpleGame.Tests
{
    public sealed class Phase3GameplayTests
    {
        [Test]
        public void FlyingEyeDefinitions_AllowEnemyOverlap()
        {
            Assert.That(
                PrototypeEnemyDefinitions
                    .CreateFlyingEye()
                    .AllowsEnemyOverlap,
                Is.True);
            Assert.That(
                PrototypeEnemyDefinitions
                    .CreateFlyingEyeBoss()
                    .AllowsEnemyOverlap,
                Is.True);
            Assert.That(
                PrototypeEnemyDefinitions
                    .Create(EnemyArchetype.Melee)
                    .AllowsEnemyOverlap,
                Is.False);
        }

        [Test]
        public void SkeletonBoss_BlocksLivingFrontHitsLikeShield()
        {
            EnemyDefinition definition =
                PrototypeEnemyDefinitions.CreateSkeletonBoss();

            CombatResult front = CombatResolver.Resolve(
                definition,
                definition.CalculateMaxHealth(1),
                1f,
                3f,
                AttackSide.Front,
                false);
            CombatResult rear = CombatResolver.Resolve(
                definition,
                definition.CalculateMaxHealth(1),
                1f,
                3f,
                AttackSide.Rear,
                false);
            CombatResult critical = CombatResolver.Resolve(
                definition,
                definition.CalculateMaxHealth(1),
                1f,
                3f,
                AttackSide.Front,
                true);

            Assert.That(definition.Archetype, Is.EqualTo(
                EnemyArchetype.Boss));
            Assert.That(definition.BlocksFrontAttacks, Is.True);
            Assert.That(
                front.PlayerReaction,
                Is.EqualTo(PlayerAttackReaction.Recoil));
            Assert.That(
                rear.PlayerReaction,
                Is.EqualTo(PlayerAttackReaction.None));
            Assert.That(
                critical.PlayerReaction,
                Is.EqualTo(PlayerAttackReaction.None));
            Assert.That(
                CombatResolver.CanPiercePastTarget(
                    definition,
                    AttackSide.Front,
                    false),
                Is.False);
            Assert.That(
                CombatResolver.CanPiercePastTarget(
                    definition,
                    AttackSide.Rear,
                    false),
                Is.True);
        }

        [TestCase(0, 0f, 0f, 0)]
        [TestCase(1, 0.35f, 1.2f, 1)]
        [TestCase(2, 0.45f, 1.32f, 2)]
        [TestCase(3, 0.55f, 1.44f, 3)]
        [TestCase(4, 0.65f, 1.56f, 4)]
        [TestCase(5, 0.75f, 1.68f, 5)]
        [TestCase(6, 0.75f, 1.68f, 5)]
        public void FilthThrow_UpgradeMathMatchesDesign(
            int level,
            float expectedDamage,
            float expectedRadius,
            int expectedCount)
        {
            Assert.That(
                PlayerCombatAbilities
                    .CalculateFilthThrowDamageMultiplier(level),
                Is.EqualTo(expectedDamage).Within(0.0001f));
            Assert.That(
                PlayerCombatAbilities
                    .CalculateFilthThrowRadius(level),
                Is.EqualTo(expectedRadius).Within(0.0001f));
            Assert.That(
                PlayerCombatAbilities
                    .CalculateFilthThrowCount(level),
                Is.EqualTo(expectedCount));
        }

        [TestCase(1, 6f)]
        [TestCase(2, 5.5f)]
        [TestCase(3, 5f)]
        [TestCase(4, 4.5f)]
        [TestCase(5, 4f)]
        [TestCase(6, 4f)]
        public void FilthThrow_CooldownFallsByLevel(
            int level,
            float expected)
        {
            Assert.That(
                PlayerCombatAbilities
                    .CalculateFilthThrowInterval(level),
                Is.EqualTo(expected).Within(0.0001f));
        }

        [TestCase(0.49f, 0)]
        [TestCase(0.5f, 1)]
        [TestCase(2.99f, 5)]
        [TestCase(3f, 6)]
        [TestCase(5f, 6)]
        public void FilthField_TicksSixTimesOverThreeSeconds(
            float exposureDuration,
            int expectedTicks)
        {
            Assert.That(
                FilthProjectile.CalculateTickCount(
                    exposureDuration),
                Is.EqualTo(expectedTicks));
        }

        [Test]
        public void FilthProjectile_ArcStartsPeaksAndEndsCorrectly()
        {
            Vector2 start = Vector2.zero;
            Vector2 destination = new(2f, 0f);

            Assert.That(
                FilthProjectile.CalculateArcPosition(
                    start,
                    destination,
                    0f),
                Is.EqualTo(start));
            Assert.That(
                FilthProjectile.CalculateArcPosition(
                    start,
                    destination,
                    0.5f),
                Is.EqualTo(new Vector2(1f, FilthProjectile.ArcHeight)));
            Assert.That(
                FilthProjectile.CalculateArcPosition(
                    start,
                    destination,
                    1f),
                Is.EqualTo(destination));
        }

        [Test]
        public void FusionCard_RequiresEveryMasteredIngredientAndIsSingleUse()
        {
            LevelUpCardTable table =
                ScriptableObject.CreateInstance<LevelUpCardTable>();
            try
            {
                LevelUpCardDefinition sword = CreateCard(
                    "SWORD",
                    PlayerStatId.FlyingSwordCount,
                    2);
                LevelUpCardDefinition piercing = CreateCard(
                    "PIERCING",
                    PlayerStatId.Piercing,
                    3);
                LevelUpCardDefinition staticCharge = CreateCard(
                    "STATIC",
                    PlayerStatId.StaticCharge,
                    2);
                LevelUpCardDefinition firstFusion = CreateFusionCard(
                    "FUSION_ONE",
                    PlayerStatId.FlyingSwordPiercingFusion,
                    "SWORD|PIERCING");
                LevelUpCardDefinition otherFusion = CreateFusionCard(
                    "FUSION_TWO",
                    PlayerStatId.FlyingSwordStaticFusion,
                    "SWORD|STATIC");
                table.Configure(new[]
                {
                    sword,
                    piercing,
                    staticCharge,
                    firstFusion,
                    otherFusion
                });
                var stacks = new Dictionary<string, int>
                {
                    ["SWORD"] = 2,
                    ["PIERCING"] = 2,
                    ["STATIC"] = 2
                };
                var excludedBaseCards = new HashSet<string>
                {
                    "SWORD",
                    "PIERCING",
                    "STATIC",
                    "FUSION_TWO"
                };

                Assert.That(
                    table.HasEligibleCard(
                        1,
                        id => stacks.TryGetValue(id, out int value)
                            ? value
                            : 0,
                        excludedBaseCards),
                    Is.False);

                stacks["PIERCING"] = 3;
                Assert.That(
                    table.HasEligibleCard(
                        1,
                        id => stacks.TryGetValue(id, out int value)
                            ? value
                            : 0,
                        excludedBaseCards),
                    Is.True);

                stacks["FUSION_ONE"] = 1;
                Assert.That(
                    table.HasEligibleCard(
                        1,
                        id => stacks.TryGetValue(id, out int value)
                            ? value
                            : 0,
                        excludedBaseCards),
                    Is.False);

                stacks["SWORD"] = 0;
                var onlyOtherFusionExcluded = new HashSet<string>
                {
                    "PIERCING",
                    "STATIC",
                    "FUSION_ONE",
                    "FUSION_TWO"
                };
                List<LevelUpCardDefinition> resetIngredient = table.Draw(
                    1,
                    id => stacks.TryGetValue(id, out int value)
                        ? value
                        : 0,
                    1,
                    onlyOtherFusionExcluded);
                Assert.That(resetIngredient, Has.Count.EqualTo(1));
                Assert.That(resetIngredient[0].CardId, Is.EqualTo("SWORD"));
                Assert.That(
                    new LevelUpCardChoiceData(
                        resetIngredient[0],
                        stacks["SWORD"]).NextLevel,
                    Is.EqualTo(1));

                stacks["SWORD"] = 2;
                var onlyFirstFusionExcluded = new HashSet<string>
                {
                    "SWORD",
                    "PIERCING",
                    "STATIC",
                    "FUSION_ONE"
                };
                Assert.That(
                    table.HasEligibleCard(
                        1,
                        id => stacks.TryGetValue(id, out int value)
                            ? value
                            : 0,
                        onlyFirstFusionExcluded),
                    Is.True,
                    "A different unowned fusion must remain available.");
            }
            finally
            {
                Object.DestroyImmediate(table);
            }
        }

        [Test]
        public void FusionSelection_RemovesOnlyIngredientCardStacks()
        {
            var sessionObject = new GameObject("FusionStackSession");
            try
            {
                PrototypeGameSession session =
                    sessionObject.AddComponent<PrototypeGameSession>();
                FieldInfo stacksField = typeof(PrototypeGameSession)
                    .GetField(
                        "cardStacks",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo consumeMethod = typeof(PrototypeGameSession)
                    .GetMethod(
                        "ConsumeFusionIngredients",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(stacksField, Is.Not.Null);
                Assert.That(consumeMethod, Is.Not.Null);
                var stacks = (Dictionary<string, int>)
                    stacksField.GetValue(session);
                stacks["SWORD"] = 3;
                stacks["PIERCING"] = 5;
                stacks["UNRELATED"] = 2;

                consumeMethod.Invoke(session, new object[]
                {
                    CreateFusionCard(
                        "FUSION",
                        PlayerStatId.FlyingSwordPiercingFusion,
                        "SWORD|PIERCING")
                });

                Assert.That(stacks.ContainsKey("SWORD"), Is.False);
                Assert.That(stacks.ContainsKey("PIERCING"), Is.False);
                Assert.That(stacks["UNRELATED"], Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(sessionObject);
            }
        }

        [Test]
        public void FusionSnapshots_StayIndependentFromReacquiredBaseSkills()
        {
            var playerObject = new GameObject("FusionPlayer");
            var worldObject = new GameObject("FusionEnemyWorld");
            try
            {
                PlayerRoot owner = playerObject.AddComponent<PlayerRoot>();
                FlyingSwordController baseSwords =
                    playerObject.AddComponent<FlyingSwordController>();
                baseSwords.ConfigureVisuals(
                    CreateReadySwordVisuals(playerObject.transform),
                    CreateAttackTemplate(playerObject.transform));
                PlayerCombatAbilities abilities =
                    playerObject.GetComponent<PlayerCombatAbilities>();
                abilities.Configure(
                    owner,
                    worldObject.AddComponent<EnemyWorldService>(),
                    null,
                    null);

                LevelUpCardDefinition swordCount = CreateCard(
                    "FLYING_SWORD_COUNT",
                    PlayerStatId.FlyingSwordCount,
                    3);
                LevelUpCardDefinition swordHits = CreateCard(
                    "FLYING_SWORD_HITS",
                    PlayerStatId.FlyingSwordHitCount,
                    3);
                LevelUpCardDefinition piercing = CreateCard(
                    "PIERCING_UP",
                    PlayerStatId.Piercing,
                    5);
                LevelUpCardDefinition staticCharge = CreateCard(
                    "STATIC_CHARGE",
                    PlayerStatId.StaticCharge,
                    5,
                    0.75f);
                LevelUpCardDefinition filth = CreateCard(
                    "FILTH_THROW",
                    PlayerStatId.FilthThrow,
                    5,
                    0.35f);

                ApplyLevels(abilities, swordCount, 3);
                ApplyLevels(abilities, swordHits, 3);
                ApplyLevels(abilities, piercing, 5);
                Assert.That(
                    abilities.ApplyCard(CreateFusionCard(
                        "FUSION_FLYING_SWORD_PIERCING",
                        PlayerStatId.FlyingSwordPiercingFusion,
                        "FLYING_SWORD_COUNT|FLYING_SWORD_HITS|PIERCING_UP")),
                    Is.True);
                Assert.That(abilities.FlyingSwordCountLevel, Is.Zero);
                Assert.That(abilities.FlyingSwordHitCountLevel, Is.Zero);
                Assert.That(abilities.PiercingLevel, Is.Zero);
                Assert.That(
                    abilities.FlyingSwordPiercingCountSnapshot,
                    Is.EqualTo(3));
                Assert.That(
                    abilities.FlyingSwordPiercingHitsSnapshot,
                    Is.EqualTo(3));

                ApplyLevels(abilities, swordCount, 3);
                ApplyLevels(abilities, swordHits, 3);
                ApplyLevels(abilities, staticCharge, 5);
                Assert.That(
                    abilities.ApplyCard(CreateFusionCard(
                        "FUSION_FLYING_SWORD_STATIC",
                        PlayerStatId.FlyingSwordStaticFusion,
                        "FLYING_SWORD_COUNT|FLYING_SWORD_HITS|STATIC_CHARGE")),
                    Is.True);
                Assert.That(
                    abilities.HasFlyingSwordPiercingFusion,
                    Is.True);
                Assert.That(abilities.HasFlyingSwordStaticFusion, Is.True);
                Assert.That(
                    abilities.FlyingSwordStaticChargeSnapshot,
                    Is.EqualTo(5));

                ApplyLevels(abilities, staticCharge, 5);
                ApplyLevels(abilities, filth, 5);
                Assert.That(
                    abilities.ApplyCard(CreateFusionCard(
                        "FUSION_STATIC_FILTH",
                        PlayerStatId.StaticFilthFusion,
                        "STATIC_CHARGE|FILTH_THROW")),
                    Is.True);
                Assert.That(abilities.StaticChargeLevel, Is.Zero);
                Assert.That(abilities.FilthThrowLevel, Is.Zero);
                Assert.That(
                    abilities.StaticFilthChargeSnapshot,
                    Is.EqualTo(5));
                Assert.That(
                    abilities.StaticFilthLevelSnapshot,
                    Is.EqualTo(5));

                ApplyLevels(abilities, swordCount, 1);
                ApplyLevels(abilities, staticCharge, 1);
                ApplyLevels(abilities, filth, 1);
                Assert.That(abilities.FlyingSwordCountLevel, Is.EqualTo(1));
                Assert.That(abilities.StaticChargeLevel, Is.EqualTo(1));
                Assert.That(abilities.FilthThrowLevel, Is.EqualTo(1));
                Assert.That(
                    abilities.FlyingSwordPiercingCountSnapshot,
                    Is.EqualTo(3));
                Assert.That(
                    abilities.FlyingSwordStaticChargeSnapshot,
                    Is.EqualTo(5));
                Assert.That(
                    abilities.StaticFilthChargeSnapshot,
                    Is.EqualTo(5));
                Assert.That(
                    abilities.StaticFilthLevelSnapshot,
                    Is.EqualTo(5));

                Assert.That(
                    abilities.ApplyCard(CreateFusionCard(
                        "FUSION_STATIC_FILTH",
                        PlayerStatId.StaticFilthFusion,
                        "STATIC_CHARGE|FILTH_THROW")),
                    Is.False,
                    "The same fusion cannot be acquired twice.");
            }
            finally
            {
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(worldObject);
            }
        }

        [Test]
        public void FusionHitPolicies_AreUnlimitedAndGenerationAware()
        {
            Assert.That(
                FlyingSwordController.CalculateMaximumHits(3, true),
                Is.EqualTo(int.MaxValue));
            Assert.That(
                FlyingSwordController.CalculateMaximumHits(3, false),
                Is.EqualTo(5));
            Assert.That(
                FlyingSwordController.WasCurrentSpawnHit(
                    true,
                    7u,
                    7u),
                Is.True);
            Assert.That(
                FlyingSwordController.WasCurrentSpawnHit(
                    true,
                    7u,
                    8u),
                Is.False);
            Assert.That(
                PlayerCombatAbilities
                    .CalculateStaticAdjacentTargetCount(5),
                Is.EqualTo(11));
        }

        [Test]
        public void FusionFilth_StaticTriggersOncePerFieldAndEnemyGeneration()
        {
            Assert.That(
                FilthProjectile.ShouldTriggerStaticBurst(
                    5,
                    false,
                    0u,
                    4u),
                Is.True);
            Assert.That(
                FilthProjectile.ShouldTriggerStaticBurst(
                    5,
                    true,
                    4u,
                    4u),
                Is.False);
            Assert.That(
                FilthProjectile.ShouldTriggerStaticBurst(
                    5,
                    true,
                    4u,
                    5u),
                Is.True,
                "A pooled enemy's next spawn is a new target.");
            Assert.That(
                FilthProjectile.ShouldTriggerStaticBurst(
                    0,
                    false,
                    0u,
                    1u),
                Is.False);
        }

        [Test]
        public void FusionRarityAliases_HaveEpicAndLegendaryColors()
        {
            Color common = LevelUpCardView.ResolveRarityColor("일반");
            Color rare = LevelUpCardView.ResolveRarityColor("희귀");
            Color epic = LevelUpCardView.ResolveRarityColor("에픽");
            Color legendary =
                LevelUpCardView.ResolveRarityColor("레전더리");

            Assert.That(epic, Is.Not.EqualTo(common));
            Assert.That(epic, Is.Not.EqualTo(rare));
            Assert.That(legendary, Is.Not.EqualTo(common));
            Assert.That(legendary, Is.Not.EqualTo(epic));
        }

        private static LevelUpCardDefinition CreateCard(
            string cardId,
            PlayerStatId targetStat,
            int maxStack,
            float value = 1f)
        {
            return new LevelUpCardDefinition(
                cardId,
                cardId,
                cardId,
                "테스트 카드",
                LevelUpCardEffectType.UpgradeRank,
                targetStat,
                StatOperation.Add,
                value,
                maxStack,
                1,
                1,
                string.Empty,
                "희귀",
                "ICON_TEST",
                true);
        }

        private static LevelUpCardDefinition CreateFusionCard(
            string cardId,
            PlayerStatId targetStat,
            string ingredientCardIds)
        {
            return new LevelUpCardDefinition(
                cardId,
                cardId,
                cardId,
                "테스트 융합 카드",
                LevelUpCardEffectType.Fusion,
                targetStat,
                StatOperation.Add,
                1f,
                1,
                10,
                1,
                string.Empty,
                "레전더리",
                "ICON_FUSION_TEST",
                true,
                ingredientCardIds);
        }

        private static void ApplyLevels(
            PlayerCombatAbilities abilities,
            LevelUpCardDefinition card,
            int count)
        {
            for (int index = 0; index < count; index++)
            {
                Assert.That(abilities.ApplyCard(card), Is.True);
            }
        }

        private static SpriteRenderer[] CreateReadySwordVisuals(
            Transform parent)
        {
            var renderers = new SpriteRenderer[
                FlyingSwordController.MaximumSwordCount];
            for (int index = 0; index < renderers.Length; index++)
            {
                var visual = new GameObject($"ReadySword{index + 1}");
                visual.transform.SetParent(parent, false);
                renderers[index] = visual.AddComponent<SpriteRenderer>();
            }

            return renderers;
        }

        private static SpriteRenderer CreateAttackTemplate(
            Transform parent)
        {
            var template = new GameObject("Flying_Sword_Attack");
            template.transform.SetParent(parent, false);
            return template.AddComponent<SpriteRenderer>();
        }
    }
}
