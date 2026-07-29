using System;
using System.IO;
using System.Linq;
using TMPro;
using NUnit.Framework;
using SimpleGameEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame.Tests
{
    public sealed class GameDataExcelImporterTests
    {
        [Test]
        public void Parser_ParsesProjectWorkbook()
        {
            string path = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                GameDataExcelImporter.DefaultWorkbookRelativePath));

            GameDataExcelModel model = GameDataExcelParser.Parse(path);

            Assert.That(model.EnemyDefinitions, Has.Count.EqualTo(4));
            Assert.That(model.SpawnEntries, Has.Count.EqualTo(3283));
            Assert.That(
                model.SpawnEntries.Max(entry => entry.EnemyLevel),
                Is.EqualTo(52));
            Assert.That(
                model.SpawnEntries.Max(entry => entry.WaveNumber),
                Is.EqualTo(60));
            Assert.That(
                model.SpawnEntries
                    .Where(entry => entry.WaveNumber == 5)
                    .All(entry =>
                        ProgressionCurve
                            .CalculateWaveHealthMultiplier(
                                entry.WaveNumber) == 1.2f),
                Is.True);
            Assert.That(
                model.SpawnEntries
                    .Where(entry => entry.WaveNumber == 8)
                    .All(entry =>
                        ProgressionCurve
                            .CalculateWaveHealthMultiplier(
                                entry.WaveNumber) == 1.5f),
                Is.True);
            Assert.That(
                model.SpawnEntries
                    .Count(entry => entry.SpawnTimeSec < 60f),
                Is.EqualTo(43));
            Assert.That(
                model.SpawnEntries.Count(entry =>
                    entry.SpawnTimeSec >= 540f &&
                    entry.SpawnTimeSec < 600f),
                Is.EqualTo(699));
            Assert.That(model.PlayerLevels, Has.Count.EqualTo(50));
            Assert.That(model.AccountLevels, Has.Count.EqualTo(4));
            Assert.That(model.PlayerDefinitions, Has.Count.EqualTo(1));
            Assert.That(model.LevelUpCards, Has.Count.EqualTo(12));
            Assert.That(model.AccountExperienceScoreUnit, Is.EqualTo(5));
            Assert.That(model.CriticalChancePerCard, Is.EqualTo(0.05f));
            Assert.That(model.MaximumCriticalChance, Is.EqualTo(0.5f));
            Assert.That(
                model.PlayerDefinitions[0].BaseMoveSpeed,
                Is.EqualTo(10f));
            Assert.That(
                model.PlayerDefinitions[0]
                    .PathEnemyApproachSpeedMultiplier,
                Is.EqualTo(1.1f));
            Assert.That(
                model.PlayerDefinitions[0]
                    .PostKillEscapeSpeedMultiplier,
                Is.EqualTo(1.2f));

            LevelUpCardDefinition speedCard = model.LevelUpCards.Find(
                card => card.CardId == "MOVE_SPEED_UP");
            Assert.That(speedCard, Is.Not.Null);
            Assert.That(
                speedCard.TargetStat,
                Is.EqualTo(PlayerStatId.MoveSpeed));
            Assert.That(speedCard.Value, Is.EqualTo(1f));
            Assert.That(speedCard.MaxStack, Is.EqualTo(5));

            LevelUpCardDefinition severCard = model.LevelUpCards.Find(
                card => card.CardId == "SEVER_TRAIL");
            Assert.That(severCard, Is.Not.Null);
            Assert.That(
                severCard.RequiredCardId,
                Is.EqualTo("PIERCING_UP"));
            Assert.That(severCard.Value, Is.EqualTo(2f));
            StringAssert.Contains(
                "실제 관통 0.15초 뒤",
                severCard.Description);
            StringAssert.Contains(
                "재사용 대기시간은 0.1초",
                severCard.Description);

            LevelUpCardDefinition hitHealCard = model.LevelUpCards.Find(
                card => card.CardId == "HIT_HEAL");
            Assert.That(hitHealCard, Is.Not.Null);
            Assert.That(hitHealCard.DisplayName, Is.EqualTo("흡혈"));
            Assert.That(hitHealCard.Value, Is.EqualTo(2f));
            Assert.That(hitHealCard.MaxStack, Is.EqualTo(3));
            StringAssert.Contains(
                "적을 처치할 때마다",
                hitHealCard.Description);

            LevelUpCardDefinition bypassCard = model.LevelUpCards.Find(
                card => card.CardId == "SHIELD_BYPASS");
            Assert.That(bypassCard, Is.Not.Null);
            Assert.That(
                bypassCard.DisplayName,
                Is.EqualTo("방패 우회"));
            StringAssert.Contains("0.5초", bypassCard.Description);
            Assert.That(
                bypassCard.TargetStat,
                Is.EqualTo(PlayerStatId.ShieldBypass));
            Assert.That(bypassCard.Value, Is.EqualTo(0.1f));
            Assert.That(bypassCard.MaxStack, Is.EqualTo(3));

            LevelUpCardDefinition flyingSwordCountCard =
                model.LevelUpCards.Find(
                    card => card.CardId == "FLYING_SWORD_COUNT");
            Assert.That(flyingSwordCountCard, Is.Not.Null);
            Assert.That(
                flyingSwordCountCard.TargetStat,
                Is.EqualTo(PlayerStatId.FlyingSwordCount));
            Assert.That(flyingSwordCountCard.MaxStack, Is.EqualTo(3));

            LevelUpCardDefinition flyingSwordHitCountCard =
                model.LevelUpCards.Find(
                    card => card.CardId == "FLYING_SWORD_HITS");
            Assert.That(flyingSwordHitCountCard, Is.Not.Null);
            Assert.That(
                flyingSwordHitCountCard.TargetStat,
                Is.EqualTo(PlayerStatId.FlyingSwordHitCount));
            Assert.That(flyingSwordHitCountCard.MaxStack, Is.EqualTo(3));
            Assert.That(
                flyingSwordHitCountCard.RequiredCardId,
                Is.EqualTo("FLYING_SWORD_COUNT"));
        }

        [Test]
        public void GeneratedCards_ContainFlyingSwordUpgrades()
        {
            LevelUpCardTable table =
                AssetDatabase.LoadAssetAtPath<LevelUpCardTable>(
                    "Assets/Game/Data/Generated/" +
                    "LevelUpCardTable.asset");

            Assert.That(table, Is.Not.Null);
            Assert.That(table.Definitions, Has.Count.EqualTo(12));

            LevelUpCardDefinition countCard =
                table.Definitions.Single(
                    card => card.CardId == "FLYING_SWORD_COUNT");
            Assert.That(
                countCard.TargetStat,
                Is.EqualTo(PlayerStatId.FlyingSwordCount));
            Assert.That(countCard.MaxStack, Is.EqualTo(3));

            LevelUpCardDefinition hitCard =
                table.Definitions.Single(
                    card => card.CardId == "FLYING_SWORD_HITS");
            Assert.That(
                hitCard.TargetStat,
                Is.EqualTo(PlayerStatId.FlyingSwordHitCount));
            Assert.That(hitCard.MaxStack, Is.EqualTo(3));
            Assert.That(
                hitCard.RequiredCardId,
                Is.EqualTo("FLYING_SWORD_COUNT"));

            LevelUpCardDefinition severCard =
                table.Definitions.Single(
                    card => card.CardId == "SEVER_TRAIL");
            StringAssert.Contains(
                "실제 관통 0.15초 뒤",
                severCard.Description);
            StringAssert.Contains(
                "재사용 대기시간은 0.1초",
                severCard.Description);

            LevelUpCardDefinition hitHealCard =
                table.Definitions.Single(
                    card => card.CardId == "HIT_HEAL");
            StringAssert.Contains(
                "적을 처치할 때마다",
                hitHealCard.Description);
        }

        [Test]
        public void PlayerPrefab_HasReusableSkillVisuals()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CharacterAssetBuilder.PlayerPrefabPath);

            Assert.That(prefab, Is.Not.Null);
            FlyingSwordController controller =
                prefab.GetComponent<FlyingSwordController>();
            Assert.That(controller, Is.Not.Null);

            var serializedController =
                new SerializedObject(controller);
            SerializedProperty readyVisuals =
                serializedController.FindProperty(
                    "readySwordVisuals");
            Assert.That(readyVisuals, Is.Not.Null);
            Assert.That(
                readyVisuals.arraySize,
                Is.EqualTo(FlyingSwordController.MaximumSwordCount));

            Vector3[] expectedPositions =
            {
                new(0.123f, 0.717f, 0f),
                new(-0.208f, 0.544f, 0f),
                new(0.171f, 0.376f, 1f)
            };
            for (int index = 0;
                 index < readyVisuals.arraySize;
                 index++)
            {
                SpriteRenderer readyVisual =
                    readyVisuals
                        .GetArrayElementAtIndex(index)
                        .objectReferenceValue as SpriteRenderer;
                Assert.That(readyVisual, Is.Not.Null);
                Assert.That(
                    Vector3.Distance(
                        readyVisual.transform.localPosition,
                        expectedPositions[index]),
                    Is.LessThan(0.0001f));
            }

            SpriteRenderer attackTemplate =
                serializedController
                    .FindProperty("attackVisualTemplate")
                    .objectReferenceValue as SpriteRenderer;
            Assert.That(attackTemplate, Is.Not.Null);
            Assert.That(
                attackTemplate.name,
                Is.EqualTo("Flying_Sword_Attack"));
            Assert.That(
                attackTemplate.gameObject.activeSelf,
                Is.False);

            PlayerCombatAbilities combatAbilities =
                prefab.GetComponent<PlayerCombatAbilities>();
            Assert.That(combatAbilities, Is.Not.Null);
            var serializedAbilities =
                new SerializedObject(combatAbilities);
            SpriteRenderer cutting =
                serializedAbilities
                    .FindProperty("severTrailVisual")
                    .objectReferenceValue as SpriteRenderer;
            Assert.That(cutting, Is.Not.Null);
            Assert.That(cutting.name, Is.EqualTo("cutting"));
            Assert.That(cutting.sprite, Is.Not.Null);
            Assert.That(cutting.gameObject.activeSelf, Is.False);
            Assert.That(cutting.sortingOrder, Is.EqualTo(100));
            Assert.That(cutting.color.r, Is.LessThan(0.1f));
            Assert.That(cutting.color.g, Is.LessThan(0.1f));
            Assert.That(cutting.color.b, Is.LessThan(0.1f));
        }

        [Test]
        public void LevelUpCardPrefab_HasReusableButtonAndKoreanFont()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PrototypeSceneBuilder.LevelUpCardPrefabPath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.name, Is.EqualTo("LevelUpCard"));
            Assert.That(prefab.GetComponent<Button>(), Is.Not.Null);
            Assert.That(
                prefab.GetComponent<LevelUpCardView>(),
                Is.Not.Null);
            RectTransform rect = prefab.GetComponent<RectTransform>();
            Assert.That(rect.sizeDelta.x, Is.EqualTo(300f));
            Assert.That(
                rect.sizeDelta.y,
                Is.EqualTo(300f * 1920f / 1080f)
                    .Within(0.01f));
            Transform inner = prefab.transform.Find("LevelUpCard_In");
            Assert.That(inner, Is.Not.Null);
            Assert.That(inner.GetComponent<Button>(), Is.Null);
            Transform skill =
                prefab.transform.Find("Panel/Skill_Text");
            Assert.That(skill, Is.Not.Null);
            TMP_Text skillText = skill.GetComponent<TMP_Text>();
            Assert.That(skillText, Is.Not.Null);
            Assert.That(skillText.color.r, Is.LessThan(0.25f));
            Assert.That(skillText.color.g, Is.LessThan(0.25f));
            Assert.That(skillText.color.b, Is.LessThan(0.25f));
            TMP_Text label = prefab.GetComponentInChildren<TMP_Text>(true);
            Assert.That(label, Is.Not.Null);
            Assert.That(
                AssetDatabase.GetAssetPath(label.font),
                Is.EqualTo(CharacterAssetBuilder.DefaultFontPath));
        }

        [Test]
        public void PrototypeUi_UsesPersistentAndTransientPrefabs()
        {
            GameObject hud = AssetDatabase.LoadAssetAtPath<GameObject>(
                PrototypeSceneBuilder.PrototypeHudPrefabPath);
            GameObject cardSelection =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrototypeSceneBuilder.CardSelectionPanelPrefabPath);
            GameObject pause = AssetDatabase.LoadAssetAtPath<GameObject>(
                PrototypeSceneBuilder.PauseDetailsPanelPrefabPath);
            GameObject gameOver =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrototypeSceneBuilder.GameOverPanelPrefabPath);

            Assert.That(hud, Is.Not.Null);
            Assert.That(cardSelection, Is.Not.Null);
            Assert.That(pause, Is.Not.Null);
            Assert.That(gameOver, Is.Not.Null);
            Assert.That(hud.transform.Find("TopPanel"), Is.Not.Null);
            Assert.That(hud.transform.Find("HintPanel"), Is.Not.Null);
            Assert.That(hud.transform.Find("ModalRoot"), Is.Not.Null);
            Assert.That(hud.transform.Find("DebugButtons"), Is.Null);
            Assert.That(
                hud.transform.Find("CardSelectionPanel"),
                Is.Null);
            Assert.That(hud.transform.Find("PauseDetailsPanel"), Is.Null);
            Assert.That(hud.transform.Find("GameOverPanel"), Is.Null);

            PrototypeHUDView view =
                hud.GetComponent<PrototypeHUDView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(
                AssetDatabase.GetAssetPath(
                    view.CardSelectionPanelPrefab),
                Is.EqualTo(
                    PrototypeSceneBuilder
                        .CardSelectionPanelPrefabPath));
            Assert.That(
                AssetDatabase.GetAssetPath(
                    view.PauseDetailsPanelPrefab),
                Is.EqualTo(
                    PrototypeSceneBuilder
                        .PauseDetailsPanelPrefabPath));
            Assert.That(
                AssetDatabase.GetAssetPath(view.GameOverPanelPrefab),
                Is.EqualTo(
                    PrototypeSceneBuilder.GameOverPanelPrefabPath));

            Assert.That(
                cardSelection.GetComponentsInChildren<
                    LevelUpCardView>(true).Length,
                Is.EqualTo(3));
            Assert.That(
                pause.transform.Find("PauseDetails"),
                Is.Not.Null);
            Assert.That(
                gameOver.transform.Find("GameOverTitle"),
                Is.Not.Null);
            Assert.That(
                gameOver.transform.Find("ContinueAd")
                    .GetComponent<Button>(),
                Is.Not.Null);
        }

        [Test]
        public void PrototypeScene_ContainsOnlyPersistentHudPrefab()
        {
            string scenePath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "Scenes",
                "PrototypeScene.unity"));
            string sceneYaml = File.ReadAllText(scenePath);
            string hudGuid = AssetDatabase.AssetPathToGUID(
                PrototypeSceneBuilder.PrototypeHudPrefabPath);

            StringAssert.Contains($"guid: {hudGuid}", sceneYaml);
            StringAssert.DoesNotContain(
                "m_Name: DebugButtons",
                sceneYaml);
            StringAssert.DoesNotContain(
                "m_Name: CardSelectionPanel",
                sceneYaml);
            StringAssert.DoesNotContain(
                "m_Name: PauseDetailsPanel",
                sceneYaml);
            StringAssert.DoesNotContain(
                "m_Name: GameOverPanel",
                sceneYaml);
            StringAssert.DoesNotContain("m_Name: Score", sceneYaml);
            StringAssert.DoesNotContain(
                "m_Name: PlayerLevel",
                sceneYaml);
            StringAssert.DoesNotContain(
                "m_Name: CriticalChance",
                sceneYaml);
        }

        [Test]
        public void LevelUpCardChoiceData_UsesSheetDescriptionAndNextLevel()
        {
            var definition = new LevelUpCardDefinition(
                "TEST_CARD",
                "TEST_NAME",
                "시험 카드",
                "시트에서 가져온 스킬 설명",
                LevelUpCardEffectType.UpgradeRank,
                PlayerStatId.HitHeal,
                StatOperation.Add,
                2f,
                3,
                1,
                1,
                string.Empty,
                "희귀",
                "ICON_TEST",
                true);

            var choice = new LevelUpCardChoiceData(definition, 1);

            Assert.That(
                choice.Description,
                Is.EqualTo("시트에서 가져온 스킬 설명"));
            Assert.That(choice.NextLevel, Is.EqualTo(2));
            Assert.That(choice.MaxLevel, Is.EqualTo(3));
            StringAssert.Contains("희귀", choice.HeaderText);
        }

        [Test]
        public void ExcelTable_RejectsFractionalInteger()
        {
            var sheet = new ExcelSheet(
                "Levels",
                new[]
                {
                    new ExcelRow(1, new[] { "Level", "RequiredExp" }),
                    new ExcelRow(2, new[] { "1.5", "10" })
                });
            var table = new ExcelTable(
                sheet,
                "Level",
                "RequiredExp");

            InvalidDataException exception = Assert.Throws<
                InvalidDataException>(() =>
                table.PositiveInt(table.DataRows[0], "Level"));

            StringAssert.Contains("Levels row 2, Level", exception.Message);
        }

        [Test]
        public void ExcelTable_RejectsMissingRequiredColumn()
        {
            var sheet = new ExcelSheet(
                "Levels",
                new[]
                {
                    new ExcelRow(1, new[] { "Level" }),
                    new ExcelRow(2, new[] { "1" })
                });

            InvalidDataException exception = Assert.Throws<
                InvalidDataException>(() =>
                new ExcelTable(sheet, "Level", "RequiredExp"));

            StringAssert.Contains("RequiredExp", exception.Message);
        }
    }
}
