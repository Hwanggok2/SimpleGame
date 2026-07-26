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
    }
}
