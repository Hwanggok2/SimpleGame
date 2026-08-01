using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace SimpleGame.Tests
{
    public sealed class PauseDetailsDataTests
    {
        [Test]
        public void PauseDetails_AreSectionedAndSkillsUseRarityOrder()
        {
            var playerObject = new GameObject("PauseDetailsPlayer");
            var sessionObject = new GameObject("PauseDetailsSession");
            LevelExperienceTable playerLevels =
                ScriptableObject.CreateInstance<LevelExperienceTable>();
            LevelExperienceTable accountLevels =
                ScriptableObject.CreateInstance<LevelExperienceTable>();
            GlobalBalance globalBalance =
                ScriptableObject.CreateInstance<GlobalBalance>();
            LevelUpCardTable cards =
                ScriptableObject.CreateInstance<LevelUpCardTable>();
            GameStringTable strings =
                ScriptableObject.CreateInstance<GameStringTable>();
            GameDataManifest manifest =
                ScriptableObject.CreateInstance<GameDataManifest>();

            try
            {
                playerLevels.Configure(new[]
                {
                    new LevelExperienceRow(4, 10)
                });
                accountLevels.Configure(new[]
                {
                    new LevelExperienceRow(3, 20)
                });
                globalBalance.Configure(5, 2, 0.1f, 1f, 5, 9, 1);

                PlayerRoot player =
                    playerObject.AddComponent<PlayerRoot>();
                HealthComponent health =
                    playerObject.GetComponent<HealthComponent>();
                PlayerProgression progression =
                    playerObject.GetComponent<PlayerProgression>();
                CriticalSystem critical =
                    playerObject.GetComponent<CriticalSystem>();
                PlayerStats stats =
                    playerObject.GetComponent<PlayerStats>();
                health.Configure(20);
                health.ApplyDamage(7);
                progression.Configure(playerLevels);
                SetPrivateField(progression, "level", 4);
                critical.Configure(0.1f, 1f);
                critical.Add(0.25f);
                stats.Configure(new PlayerDefinition(
                    "TestPlayer",
                    1,
                    20,
                    2f,
                    0.5f,
                    3f,
                    10f,
                    1.1f,
                    1.2f,
                    0.08f,
                    1.5f,
                    0f,
                    true));
                SetPrivateField(player, "health", health);
                SetPrivateField(player, "progression", progression);
                SetPrivateField(player, "critical", critical);
                SetPrivateField(player, "stats", stats);

                cards.Configure(new[]
                {
                    CreateCard("LEGENDARY", "레전더리", false),
                    CreateCard("RARE_ONE", "희귀", false),
                    CreateCard("COMMON_ONE", "일반", true),
                    CreateCard("EPIC", "에픽", false),
                    CreateCard("RARE_TWO", "Rare", false),
                    CreateCard("COMMON_TWO", "Common", true),
                    CreateCard("CONSUMED", "일반", false)
                });
                strings.Configure(new[]
                {
                    new GameStringEntry(
                        GameStringIds.DifficultyEasyName,
                        "쉬움"),
                    new GameStringEntry(
                        GameStringIds.PausePlayerOverviewFormat,
                        "P:{0}|{1}|{2}|{3}"),
                    new GameStringEntry(
                        GameStringIds.PauseAccountOverviewFormat,
                        "A:{0}|{1}|{2}|{3}"),
                    new GameStringEntry(
                        GameStringIds.PauseAccountMaxFormat,
                        "MAX:{0}|{1}"),
                    new GameStringEntry(
                        GameStringIds.PauseVitalsFormat,
                        "V:{0}/{1}|{2:0.##}"),
                    new GameStringEntry(
                        GameStringIds.PauseMobilityFormat,
                        "M:{0:0}|{1:0.##}"),
                    new GameStringEntry(
                        GameStringIds.PauseCombatFormat,
                        "C:{0:0.##}|{1:0.##}"),
                    new GameStringEntry(
                        GameStringIds.PauseSkillLevelFormat,
                        "@{0}/{1}"),
                    new GameStringEntry(GameStringIds.CommonNone, "NONE")
                });
                manifest.Configure(
                    null,
                    null,
                    playerLevels,
                    accountLevels,
                    globalBalance,
                    null,
                    cards,
                    null,
                    null,
                    strings);

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
                SetPrivateField(session, "accountLevel", 3);
                SetPrivateField(session, "state", GameRunState.Playing);
                SetPrivateField(session, "<Score>k__BackingField", 30);
                SetPrivateField(
                    session,
                    "<ElapsedTime>k__BackingField",
                    125.9f);
                SetPrivateField(
                    session,
                    "<Difficulty>k__BackingField",
                    GameDifficulty.Easy);
                Dictionary<string, int> stacks =
                    GetPrivateField<Dictionary<string, int>>(
                        session,
                        "cardStacks");
                stacks["LEGENDARY"] = 1;
                stacks["RARE_ONE"] = 2;
                stacks["COMMON_ONE"] = 3;
                stacks["EPIC"] = 1;
                stacks["RARE_TWO"] = 1;
                stacks["COMMON_TWO"] = 2;

                PauseDetailsData received = default;
                bool wasPublished = false;
                session.PauseDetailsChanged += value =>
                {
                    received = value;
                    wasPublished = true;
                };

                session.TogglePause();

                Assert.That(wasPublished, Is.True);
                Assert.That(
                    received.PlayerOverview,
                    Is.EqualTo("P:4|쉬움|30|02:05"));
                Assert.That(
                    received.AccountOverview,
                    Is.EqualTo("A:3|12|12|20"));
                StringAssert.Contains("V:13/20|", received.Stats);
                StringAssert.Contains("M:25|10", received.Stats);
                StringAssert.Contains("C:1.5|3", received.Stats);
                Assert.That(
                    received.Skills.Contains("CONSUMED"),
                    Is.False,
                    "A fusion ingredient removed from cardStacks must " +
                    "not appear in the acquired-card list.");
                AssertInOrder(
                    received.Skills,
                    "COMMON_ONE",
                    "COMMON_TWO",
                    "RARE_ONE",
                    "RARE_TWO",
                    "EPIC",
                    "LEGENDARY");
            }
            finally
            {
                Time.timeScale = 1f;
                Object.DestroyImmediate(sessionObject);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(manifest);
                Object.DestroyImmediate(strings);
                Object.DestroyImmediate(cards);
                Object.DestroyImmediate(globalBalance);
                Object.DestroyImmediate(accountLevels);
                Object.DestroyImmediate(playerLevels);
            }
        }

        private static LevelUpCardDefinition CreateCard(
            string cardId,
            string rarity,
            bool statUpgrade)
        {
            return new LevelUpCardDefinition(
                cardId,
                cardId,
                cardId,
                "Test description",
                statUpgrade
                    ? LevelUpCardEffectType.StatModifier
                    : LevelUpCardEffectType.UpgradeRank,
                statUpgrade
                    ? PlayerStatId.MaxHp
                    : PlayerStatId.Piercing,
                StatOperation.Add,
                1f,
                5,
                1,
                1,
                string.Empty,
                rarity,
                "ICON_TEST",
                true);
        }

        private static void AssertInOrder(
            string text,
            params string[] values)
        {
            int previous = -1;
            foreach (string value in values)
            {
                int current = text.IndexOf(
                    value,
                    System.StringComparison.Ordinal);
                Assert.That(current, Is.GreaterThan(previous), value);
                previous = current;
            }
        }

        private static T GetPrivateField<T>(
            object target,
            string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
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
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
