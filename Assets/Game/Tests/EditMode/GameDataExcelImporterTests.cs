using System;
using System.IO;
using System.Linq;
using TMPro;
using NUnit.Framework;
using SimpleGameEditor;
using UnityEditor;
using UnityEditor.Animations;
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
            StageSpawnEntry[] normalSpawns = model.SpawnEntries
                .Where(entry =>
                    entry.Difficulty == GameDifficulty.Normal)
                .ToArray();
            StageSpawnEntry[] easySpawns = model.SpawnEntries
                .Where(entry =>
                    entry.Difficulty == GameDifficulty.Easy)
                .ToArray();

            Assert.That(model.EnemyDefinitions, Has.Count.EqualTo(8));
            Assert.That(normalSpawns, Has.Length.EqualTo(3283));
            Assert.That(easySpawns, Has.Length.EqualTo(2487));
            Assert.That(
                normalSpawns.Max(entry => entry.EnemyLevel),
                Is.EqualTo(52));
            Assert.That(
                easySpawns.Max(entry => entry.EnemyLevel),
                Is.EqualTo(42));
            Assert.That(
                normalSpawns.Max(entry => entry.WaveNumber),
                Is.EqualTo(60));
            Assert.That(
                normalSpawns
                    .Where(entry => entry.WaveNumber == 5)
                    .All(entry =>
                        ProgressionCurve
                            .CalculateWaveHealthMultiplier(
                                entry.WaveNumber) == 1.2f),
                Is.True);
            Assert.That(
                normalSpawns
                    .Where(entry => entry.WaveNumber == 8)
                    .All(entry =>
                        ProgressionCurve
                            .CalculateWaveHealthMultiplier(
                                entry.WaveNumber) == 1.5f),
                Is.True);
            Assert.That(
                normalSpawns
                    .Count(entry => entry.SpawnTimeSec < 60f),
                Is.EqualTo(43));
            Assert.That(
                normalSpawns.Count(entry =>
                    entry.SpawnTimeSec >= 540f &&
                    entry.SpawnTimeSec < 600f),
                Is.EqualTo(699));
            Assert.That(model.PlayerLevels, Has.Count.EqualTo(50));
            Assert.That(model.AccountLevels, Has.Count.EqualTo(4));
            Assert.That(model.PlayerDefinitions, Has.Count.EqualTo(1));
            Assert.That(model.LevelUpCards, Has.Count.EqualTo(13));
            Assert.That(model.AccountExperienceScoreUnit, Is.EqualTo(5));
            Assert.That(model.CriticalChancePerCard, Is.EqualTo(0.05f));
            Assert.That(model.MaximumCriticalChance, Is.EqualTo(0.5f));
            Assert.That(model.InitialCardRerolls, Is.EqualTo(5));
            Assert.That(
                model.MaximumStoredCardRerolls,
                Is.EqualTo(9));
            Assert.That(model.BossRerollReward, Is.EqualTo(1));
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

            EnemyDefinition goblinBoss =
                model.EnemyDefinitions.Single(
                    enemy => enemy.EnemyId ==
                        PrototypeEnemyDefinitions.GoblinBossId);
            EnemyDefinition mushroomBoss =
                model.EnemyDefinitions.Single(
                    enemy => enemy.EnemyId ==
                        PrototypeEnemyDefinitions.MushroomBossId);
            EnemyDefinition flyingEye =
                model.EnemyDefinitions.Single(
                    enemy => enemy.EnemyId ==
                        PrototypeEnemyDefinitions.FlyingEyeId);
            EnemyDefinition flyingEyeBoss =
                model.EnemyDefinitions.Single(
                    enemy => enemy.EnemyId ==
                        PrototypeEnemyDefinitions.FlyingEyeBossId);
            EnemyDefinition skeletonBoss =
                model.EnemyDefinitions.Single(
                    enemy => enemy.EnemyId ==
                        PrototypeEnemyDefinitions.SkeletonBossId);
            Assert.That(goblinBoss.MoveSpeed, Is.EqualTo(0.75f));
            Assert.That(mushroomBoss.Archetype, Is.EqualTo(
                EnemyArchetype.Boss));
            Assert.That(mushroomBoss.MoveSpeed, Is.EqualTo(0.72f));
            Assert.That(flyingEye.AllowsEnemyOverlap, Is.True);
            Assert.That(
                flyingEyeBoss.Archetype,
                Is.EqualTo(EnemyArchetype.Boss));
            Assert.That(flyingEyeBoss.AllowsEnemyOverlap, Is.True);
            Assert.That(skeletonBoss.BlocksFrontAttacks, Is.True);
            Assert.That(
                normalSpawns.Count(entry =>
                    entry.EnemyId ==
                        PrototypeEnemyDefinitions.FlyingEyeId),
                Is.EqualTo(186));
            Assert.That(
                normalSpawns.Single(entry =>
                    entry.WaveNumber == 24 &&
                    entry.EnemyId ==
                        PrototypeEnemyDefinitions.MushroomBossId),
                Is.Not.Null);
            string[] bossOrder = normalSpawns
                .Where(entry =>
                    entry.EnemyId ==
                        PrototypeEnemyDefinitions.GoblinBossId ||
                    entry.EnemyId ==
                        PrototypeEnemyDefinitions.MushroomBossId ||
                    entry.EnemyId ==
                        PrototypeEnemyDefinitions.FlyingEyeBossId ||
                    entry.EnemyId ==
                        PrototypeEnemyDefinitions.SkeletonBossId)
                .OrderBy(entry => entry.SpawnTimeSec)
                .Select(entry => entry.EnemyId)
                .ToArray();
            CollectionAssert.AreEqual(
                new[]
                {
                    PrototypeEnemyDefinitions.GoblinBossId,
                    PrototypeEnemyDefinitions.MushroomBossId,
                    PrototypeEnemyDefinitions.FlyingEyeBossId,
                    PrototypeEnemyDefinitions.SkeletonBossId
                },
                bossOrder);

            LevelUpCardDefinition speedCard = model.LevelUpCards.Find(
                card => card.CardId == "MOVE_SPEED_UP");
            Assert.That(speedCard, Is.Not.Null);
            Assert.That(
                speedCard.TargetStat,
                Is.EqualTo(PlayerStatId.MoveSpeed));
            Assert.That(speedCard.Value, Is.EqualTo(1f));
            Assert.That(speedCard.MaxStack, Is.EqualTo(5));
            StringAssert.Contains(
                "약 0.15초",
                speedCard.Description);

            LevelUpCardDefinition movingSlashCard =
                model.LevelUpCards.Find(
                    card => card.CardId == "MOVING_SLASH");
            Assert.That(movingSlashCard, Is.Not.Null);
            StringAssert.Contains(
                "초승달 검기",
                movingSlashCard.Description);
            StringAssert.Contains(
                "피해·크기·사거리·최대 타격 수",
                movingSlashCard.Description);
            StringAssert.Contains(
                "최대 타격 2/3/4/5/6",
                movingSlashCard.Description);
            Assert.That(movingSlashCard.Value, Is.EqualTo(1.8f));

            LevelUpCardDefinition severCard = model.LevelUpCards.Find(
                card => card.CardId == "SEVER_TRAIL");
            Assert.That(severCard, Is.Not.Null);
            Assert.That(
                severCard.RequiredCardId,
                Is.EqualTo("PIERCING_UP"));
            Assert.That(severCard.Value, Is.EqualTo(2f));
            StringAssert.Contains(
                "실제 관통 0.3초 뒤",
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

            LevelUpCardDefinition filthThrowCard =
                model.LevelUpCards.Find(
                    card => card.CardId == "FILTH_THROW");
            Assert.That(filthThrowCard, Is.Not.Null);
            Assert.That(
                filthThrowCard.TargetStat,
                Is.EqualTo(PlayerStatId.FilthThrow));
            Assert.That(filthThrowCard.Value, Is.EqualTo(0.35f));
            Assert.That(filthThrowCard.MaxStack, Is.EqualTo(5));
            Assert.That(filthThrowCard.SelectionWeight, Is.EqualTo(60));
            StringAssert.Contains(
                "투척 수 1/2/3/4/5",
                filthThrowCard.Description);
            StringAssert.Contains(
                "3초 동안 0.5초마다",
                filthThrowCard.Description);
        }

        [Test]
        public void WorkbookReader_RejectsOutOfSyncTableHeaders()
        {
            string path = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "outputs",
                "phase4-balance-20260730-019fb294",
                "GameData_10min_Balance.xlsx"));

            InvalidDataException exception = Assert.Throws<
                InvalidDataException>(() =>
            {
                using var reader = new OpenXmlWorkbookReader(path);
            });

            StringAssert.Contains("WavePlan", exception.Message);
            StringAssert.Contains("TableColumn", exception.Message);
        }

        [Test]
        public void GeneratedCards_ContainFlyingSwordUpgrades()
        {
            LevelUpCardTable table =
                AssetDatabase.LoadAssetAtPath<LevelUpCardTable>(
                    "Assets/Game/Data/Generated/" +
                    "LevelUpCardTable.asset");

            Assert.That(table, Is.Not.Null);
            Assert.That(table.Definitions, Has.Count.EqualTo(13));

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

            LevelUpCardDefinition speedCard =
                table.Definitions.Single(
                    card => card.CardId == "MOVE_SPEED_UP");
            StringAssert.Contains(
                "약 0.15초",
                speedCard.Description);

            LevelUpCardDefinition movingSlashCard =
                table.Definitions.Single(
                    card => card.CardId == "MOVING_SLASH");
            StringAssert.Contains(
                "초승달 검기",
                movingSlashCard.Description);
            StringAssert.Contains(
                "피해·크기·사거리·최대 타격 수",
                movingSlashCard.Description);
            StringAssert.Contains(
                "최대 타격 2/3/4/5/6",
                movingSlashCard.Description);
            Assert.That(movingSlashCard.Value, Is.EqualTo(1.8f));

            LevelUpCardDefinition severCard =
                table.Definitions.Single(
                    card => card.CardId == "SEVER_TRAIL");
            StringAssert.Contains(
                "실제 관통 0.3초 뒤",
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

            LevelUpCardDefinition filthThrowCard =
                table.Definitions.Single(
                    card => card.CardId == "FILTH_THROW");
            Assert.That(
                filthThrowCard.TargetStat,
                Is.EqualTo(PlayerStatId.FilthThrow));
            Assert.That(filthThrowCard.Value, Is.EqualTo(0.35f));
            Assert.That(filthThrowCard.MaxStack, Is.EqualTo(5));
            StringAssert.Contains(
                "투척 수 1/2/3/4/5",
                filthThrowCard.Description);
        }

        [Test]
        public void MovingSlashCrescentSheet_HasSixPixelArtFrames()
        {
            string assetPath =
                $"Assets/SourceAssets/" +
                $"{MovingSlashProjectile.AnimationResourcePath}.png";
            TextureImporter importer =
                AssetImporter.GetAtPath(assetPath) as TextureImporter;
            Texture2D texture =
                AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            Sprite[] frames =
                AssetDatabase.LoadAllAssetsAtPath(assetPath)
                    .OfType<Sprite>()
                    .OrderBy(frame => frame.name)
                    .ToArray();

            Assert.That(importer, Is.Not.Null);
            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.width, Is.EqualTo(384));
            Assert.That(texture.height, Is.EqualTo(64));
            Assert.That(
                importer.textureType,
                Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(
                importer.spriteImportMode,
                Is.EqualTo(SpriteImportMode.Multiple));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(
                importer.textureCompression,
                Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(
                importer.spritePixelsPerUnit,
                Is.EqualTo(64f).Within(0.0001f));
            Assert.That(
                frames,
                Has.Length.EqualTo(
                    MovingSlashProjectile.AnimationFrameCount));

            for (int index = 0; index < frames.Length; index++)
            {
                Assert.That(
                    frames[index].name,
                    Is.EqualTo($"MovingSlash_Crescent_{index}"));
                Assert.That(frames[index].rect.width, Is.EqualTo(64f));
                Assert.That(frames[index].rect.height, Is.EqualTo(64f));
            }
        }

        [Test]
        public void AllPrefabAssets_AreCentralizedUnderPrefabRoot()
        {
            string[] prefabPaths = AssetDatabase.FindAssets(
                    "t:Prefab",
                    new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(
                    ".prefab",
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();

            Assert.That(prefabPaths, Has.Length.GreaterThanOrEqualTo(40));
            Assert.That(
                prefabPaths,
                Is.All.StartsWith(
                    CharacterAssetBuilder.PrefabRootPath + "/"));
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

            MovingSlashProjectile movingSlashPrefab =
                serializedAbilities
                    .FindProperty("movingSlashPrefab")
                    .objectReferenceValue as MovingSlashProjectile;
            Assert.That(movingSlashPrefab, Is.Not.Null);
            Assert.That(
                AssetDatabase.GetAssetPath(movingSlashPrefab),
                Is.EqualTo(CharacterAssetBuilder.MovingSlashPrefabPath));

            var serializedMovingSlash =
                new SerializedObject(movingSlashPrefab);
            SpriteRenderer movingSlashRenderer =
                serializedMovingSlash
                    .FindProperty("spriteRenderer")
                    .objectReferenceValue as SpriteRenderer;
            SerializedProperty movingSlashFrames =
                serializedMovingSlash.FindProperty("animationFrames");
            Assert.That(movingSlashRenderer, Is.Not.Null);
            Assert.That(
                movingSlashRenderer.flipX,
                Is.True,
                "The crescent's solid edge must face its local +X travel " +
                "direction.");
            Assert.That(
                movingSlashFrames.arraySize,
                Is.EqualTo(
                    MovingSlashProjectile.AnimationFrameCount));
            Assert.That(
                movingSlashPrefab.GetComponent<LineRenderer>(),
                Is.Null);

            FilthProjectile filthProjectilePrefab =
                serializedAbilities
                    .FindProperty("filthProjectilePrefab")
                    .objectReferenceValue as FilthProjectile;
            Assert.That(filthProjectilePrefab, Is.Not.Null);
            Assert.That(
                AssetDatabase.GetAssetPath(filthProjectilePrefab),
                Is.EqualTo(
                    CharacterAssetBuilder
                        .FilthProjectilePrefabPath));
            var serializedFilth =
                new SerializedObject(filthProjectilePrefab);
            Assert.That(
                serializedFilth
                    .FindProperty("orbRenderer")
                    .objectReferenceValue,
                Is.Not.Null);
            Assert.That(
                serializedFilth
                    .FindProperty("fieldVisual")
                    .objectReferenceValue,
                Is.Not.Null);

            PlayerController playerController =
                prefab.GetComponent<PlayerController>();
            Assert.That(playerController, Is.Not.Null);
            var serializedPlayerController =
                new SerializedObject(playerController);
            SpriteRenderer aimRay =
                serializedPlayerController
                    .FindProperty("aimRayRenderer")
                    .objectReferenceValue as SpriteRenderer;
            SpriteRenderer aimEndpoint =
                serializedPlayerController
                    .FindProperty("aimEndpointRenderer")
                    .objectReferenceValue as SpriteRenderer;
            SpriteRenderer commandEndpoint =
                serializedPlayerController
                    .FindProperty("commandEndpointRenderer")
                    .objectReferenceValue as SpriteRenderer;
            SpriteRenderer commandArrow =
                serializedPlayerController
                    .FindProperty("commandArrowRenderer")
                    .objectReferenceValue as SpriteRenderer;
            Assert.That(aimRay, Is.Not.Null);
            Assert.That(aimRay.name, Is.EqualTo("AimRay"));
            Assert.That(aimRay.enabled, Is.False);
            Assert.That(aimRay.drawMode, Is.EqualTo(SpriteDrawMode.Tiled));
            Assert.That(aimRay.color, Is.EqualTo(Color.white));
            Assert.That(
                AssetDatabase.GetAssetPath(aimRay.sprite),
                Is.EqualTo(CharacterAssetBuilder.AimDashAssetPath));
            Assert.That(aimEndpoint, Is.Not.Null);
            Assert.That(
                aimEndpoint.name,
                Is.EqualTo("AimEndpoint"));
            Assert.That(aimEndpoint.enabled, Is.False);
            Assert.That(
                AssetDatabase.GetAssetPath(aimEndpoint.sprite),
                Is.EqualTo(CharacterAssetBuilder.AimEllipseAssetPath));
            Assert.That(commandEndpoint, Is.Not.Null);
            Assert.That(commandEndpoint.enabled, Is.False);
            Assert.That(
                AssetDatabase.GetAssetPath(commandEndpoint.sprite),
                Is.EqualTo(CharacterAssetBuilder.AimEllipseAssetPath));
            Assert.That(commandArrow, Is.Not.Null);
            Assert.That(commandArrow.enabled, Is.False);
            Assert.That(commandArrow.color.a, Is.EqualTo(0.5f));
            Assert.That(commandArrow.color.r, Is.EqualTo(1f));
            Assert.That(
                AssetDatabase.GetAssetPath(commandArrow.sprite),
                Is.EqualTo(CharacterAssetBuilder.AimArrowAssetPath));
        }

        [Test]
        public void WorldRewardAndMushroomPrefabs_AreConfigured()
        {
            GameObject pickupPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    CharacterAssetBuilder.HealthPickupPrefabPath);
            GameObject cloudPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    CharacterAssetBuilder.PoisonCloudPrefabPath);
            GameObject mushroomPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    CharacterAssetBuilder.MushroomBossPrefabPath);
            GameObject flyingEyePrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    CharacterAssetBuilder.FlyingEyePrefabPath);
            GameObject flyingEyeBossPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    CharacterAssetBuilder.FlyingEyeBossPrefabPath);
            GameObject skeletonBossPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    CharacterAssetBuilder.SkeletonBossPrefabPath);
            GameObject filthPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    CharacterAssetBuilder.FilthProjectilePrefabPath);
            EnemyAssetCatalog catalog =
                AssetDatabase.LoadAssetAtPath<EnemyAssetCatalog>(
                    "Assets/Game/Data/Catalogs/EnemyAssetCatalog.asset");

            Assert.That(pickupPrefab, Is.Not.Null);
            Assert.That(
                pickupPrefab.GetComponent<HealthPickup>(),
                Is.Not.Null);
            CircleCollider2D pickupCollider =
                pickupPrefab.GetComponent<CircleCollider2D>();
            Assert.That(pickupCollider, Is.Not.Null);
            Assert.That(pickupCollider.isTrigger, Is.True);
            Assert.That(
                pickupPrefab.GetComponentsInChildren<SpriteRenderer>(),
                Has.Length.GreaterThanOrEqualTo(3));

            Assert.That(cloudPrefab, Is.Not.Null);
            Assert.That(
                cloudPrefab.GetComponent<MushroomPoisonCloud>(),
                Is.Not.Null);

            Assert.That(mushroomPrefab, Is.Not.Null);
            BossEnemy mushroomBoss =
                mushroomPrefab.GetComponent<BossEnemy>();
            Assert.That(mushroomBoss, Is.Not.Null);
            Assert.That(
                mushroomPrefab.GetComponent<Animator>(),
                Is.Not.Null);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(
                catalog.TryGetPrefab(
                    PrototypeEnemyDefinitions.MushroomBossId,
                    out EnemyBase mappedPrefab),
                Is.True);
            Assert.That(mappedPrefab, Is.SameAs(mushroomBoss));
            Assert.That(flyingEyePrefab, Is.Not.Null);
            Assert.That(
                flyingEyePrefab.GetComponent<MeleeEnemy>(),
                Is.Not.Null);
            Assert.That(flyingEyeBossPrefab, Is.Not.Null);
            Assert.That(
                flyingEyeBossPrefab.GetComponent<BossEnemy>(),
                Is.Not.Null);
            Assert.That(skeletonBossPrefab, Is.Not.Null);
            Assert.That(
                skeletonBossPrefab.GetComponent<BossEnemy>(),
                Is.Not.Null);
            Assert.That(filthPrefab, Is.Not.Null);
            Assert.That(
                filthPrefab.GetComponent<FilthProjectile>(),
                Is.Not.Null);
            Assert.That(
                catalog.TryGetPrefab(
                    PrototypeEnemyDefinitions.FlyingEyeId,
                    out _),
                Is.True);
            Assert.That(
                catalog.TryGetPrefab(
                    PrototypeEnemyDefinitions.FlyingEyeBossId,
                    out _),
                Is.True);
            Assert.That(
                catalog.TryGetPrefab(
                    PrototypeEnemyDefinitions.SkeletonBossId,
                    out _),
                Is.True);
        }

        [Test]
        public void BossPrefabs_HaveAttack2ControllersAndBossModules()
        {
            (string prefabPath, string controllerPath)[] bossAssets =
            {
                (
                    CharacterAssetBuilder.BossPrefabPath,
                    CharacterAssetBuilder.RootPath +
                    "/Animators/Goblin.controller"),
                (
                    CharacterAssetBuilder.MushroomBossPrefabPath,
                    CharacterAssetBuilder.RootPath +
                    "/Animators/Mushroom.controller"),
                (
                    CharacterAssetBuilder.FlyingEyeBossPrefabPath,
                    CharacterAssetBuilder.RootPath +
                    "/Animators/FlyingEye.controller"),
                (
                    CharacterAssetBuilder.SkeletonBossPrefabPath,
                    CharacterAssetBuilder.RootPath +
                    "/Animators/Skeleton.controller")
            };

            foreach (
                (string prefabPath, string controllerPath) in
                bossAssets)
            {
                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        prefabPath);
                Assert.That(
                    prefab,
                    Is.Not.Null,
                    $"Missing boss prefab: {prefabPath}");
                Assert.That(
                    prefab.GetComponent<BossEnemy>(),
                    Is.Not.Null,
                    prefabPath);
                Assert.That(
                    prefab.GetComponent<BossAttackModule>(),
                    Is.Not.Null,
                    prefabPath);

                Animator animator = prefab.GetComponent<Animator>();
                Assert.That(animator, Is.Not.Null, prefabPath);
                Assert.That(
                    AssetDatabase.GetAssetPath(
                        animator.runtimeAnimatorController),
                    Is.EqualTo(controllerPath),
                    prefabPath);

                AnimatorController controller =
                    AssetDatabase.LoadAssetAtPath<AnimatorController>(
                        controllerPath);
                Assert.That(
                    controller,
                    Is.Not.Null,
                    $"Missing boss animator: {controllerPath}");
                Assert.That(
                    controller.parameters.Any(parameter =>
                        parameter.name ==
                            CharacterSpriteAnimator.Attack2Parameter &&
                        parameter.type ==
                            AnimatorControllerParameterType.Trigger),
                    Is.True,
                    controllerPath);

                AnimatorState attack2 = controller.layers[0]
                    .stateMachine.states
                    .Select(child => child.state)
                    .SingleOrDefault(state => state.name == "Attack2");
                Assert.That(attack2, Is.Not.Null, controllerPath);
                Assert.That(
                    attack2.motion,
                    Is.Not.Null,
                    controllerPath);
            }
        }

        [Test]
        public void LevelUpCardPrefab_HasReusableButtonAndKoreanFont()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PrototypeSceneBuilder.LevelUpCardPrefabPath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.name, Is.EqualTo("LevelUpCard"));
            Assert.That(prefab.GetComponent<Button>(), Is.Not.Null);
            LevelUpCardView cardView =
                prefab.GetComponent<LevelUpCardView>();
            Assert.That(cardView, Is.Not.Null);
            Assert.That(cardView.RerollButton, Is.Not.Null);
            Assert.That(cardView.RerollLabel, Is.Not.Null);
            Assert.That(
                cardView.RerollButton.transform.parent,
                Is.EqualTo(prefab.transform));
            Assert.That(
                cardView.RerollButton.name,
                Is.EqualTo("RerollButton"));
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
            GameObject difficulty =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrototypeSceneBuilder
                        .DifficultySelectionPanelPrefabPath);

            Assert.That(hud, Is.Not.Null);
            Assert.That(cardSelection, Is.Not.Null);
            Assert.That(pause, Is.Not.Null);
            Assert.That(gameOver, Is.Not.Null);
            Assert.That(difficulty, Is.Not.Null);
            Assert.That(hud.transform.Find("TopPanel"), Is.Not.Null);
            Transform hintPanel = hud.transform.Find("HintPanel");
            Assert.That(hintPanel, Is.Not.Null);
            Assert.That(
                hintPanel.GetComponent<Image>().raycastTarget,
                Is.False);
            Transform commandControls =
                hud.transform.Find("CommandControls");
            Assert.That(commandControls, Is.Not.Null);
            Transform joystick =
                commandControls.Find("AimJoystick");
            Transform attack =
                commandControls.Find(
                    HudButtonId.Attack.ToString());
            Assert.That(joystick, Is.Not.Null);
            Assert.That(attack, Is.Not.Null);
            Assert.That(hud.transform.Find("ModalRoot"), Is.Not.Null);
            Transform settings = hud.transform.Find(
                HudButtonId.Settings.ToString());
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.GetComponent<Button>(), Is.Not.Null);
            Assert.That(
                settings.GetSiblingIndex(),
                Is.EqualTo(hud.transform.childCount - 1));
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
                view.SettingsButton,
                Is.EqualTo(settings.GetComponent<Button>()));
            Assert.That(
                view.AimJoystick,
                Is.EqualTo(
                    joystick.GetComponent<
                        AimJoystickControl>()));
            Assert.That(
                view.AttackButton,
                Is.EqualTo(attack.GetComponent<Button>()));
            Assert.That(
                attack.GetComponent<AttackCommandButton>(),
                Is.Not.Null);
            Assert.That(
                joystick.GetComponent<Image>().raycastTarget,
                Is.True);
            Assert.That(
                joystick.Find("Knob")
                    .GetComponent<Image>()
                    .raycastTarget,
                Is.False);
            RectTransform joystickRect =
                joystick.GetComponent<RectTransform>();
            RectTransform attackRect =
                attack.GetComponent<RectTransform>();
            Assert.That(
                joystickRect.anchorMin,
                Is.EqualTo(Vector2.zero));
            Assert.That(
                attackRect.anchorMin,
                Is.EqualTo(new Vector2(1f, 0f)));
            Assert.That(
                attack.GetComponent<Image>().raycastTarget,
                Is.True);
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
                AssetDatabase.GetAssetPath(
                    view.DifficultySelectionPanelPrefab),
                Is.EqualTo(
                    PrototypeSceneBuilder
                        .DifficultySelectionPanelPrefabPath));

            Assert.That(
                cardSelection.GetComponentsInChildren<
                    LevelUpCardView>(true).Length,
                Is.EqualTo(3));
            foreach (LevelUpCardView cardView in
                     cardSelection.GetComponentsInChildren<
                         LevelUpCardView>(true))
            {
                Assert.That(cardView.RerollButton, Is.Not.Null);
                Assert.That(cardView.RerollLabel, Is.Not.Null);
            }
            Assert.That(
                pause.transform.Find("PauseDetails"),
                Is.Not.Null);
            Transform controlPadToggle =
                pause.transform.Find("ControlPadToggle");
            Assert.That(controlPadToggle, Is.Not.Null);
            Toggle toggle =
                controlPadToggle.GetComponent<Toggle>();
            Assert.That(toggle, Is.Not.Null);
            Assert.That(toggle.isOn, Is.True);
            Assert.That(toggle.graphic, Is.Not.Null);
            Assert.That(
                controlPadToggle.Find("Label")
                    .GetComponent<TMP_Text>()
                    .text,
                Is.EqualTo("조작 패드 표시"));
            Transform autoAttackToggle =
                pause.transform.Find("AutoAttackToggle");
            Assert.That(autoAttackToggle, Is.Not.Null);
            Assert.That(
                autoAttackToggle.GetComponent<Toggle>().isOn,
                Is.False);
            Assert.That(
                autoAttackToggle.Find("Label")
                    .GetComponent<TMP_Text>()
                    .text,
                Is.EqualTo("자동 공격"));
            Transform controlSettingsButton =
                pause.transform.Find("ControlSettingsButton");
            Transform controlSettingsPanel =
                pause.transform.Find("ControlSettingsPanel");
            Assert.That(controlSettingsButton, Is.Not.Null);
            Assert.That(
                controlSettingsButton.GetComponent<Button>(),
                Is.Not.Null);
            Assert.That(controlSettingsPanel, Is.Not.Null);
            Assert.That(controlSettingsPanel.gameObject.activeSelf, Is.False);
            Assert.That(
                controlSettingsPanel.GetComponent<Image>().color.a,
                Is.LessThanOrEqualTo(0.5f));
            foreach (string sliderName in new[]
                     {
                         "JoystickSizeSlider",
                         "JoystickHorizontalSlider",
                         "JoystickVerticalSlider",
                         "AttackSizeSlider",
                         "AttackHorizontalSlider",
                         "AttackVerticalSlider"
                     })
            {
                Assert.That(
                    controlSettingsPanel.Find(sliderName)
                        ?.GetComponent<Slider>(),
                    Is.Not.Null,
                    sliderName);
            }

            foreach (string buttonName in new[]
                     {
                         "ControlDefaultsButton",
                         "ControlCancelButton",
                         "ControlApplyButton"
                     })
            {
                Assert.That(
                    controlSettingsPanel.Find(buttonName)
                        ?.GetComponent<Button>(),
                    Is.Not.Null,
                    buttonName);
            }
            Assert.That(
                gameOver.transform.Find("GameOverTitle"),
                Is.Not.Null);
            Assert.That(
                gameOver.transform.Find("ContinueAd")
                    .GetComponent<Button>(),
                Is.Not.Null);
            Assert.That(
                difficulty.transform.Find(
                    HudButtonId.DifficultyEasy.ToString())
                    ?.GetComponent<Button>(),
                Is.Not.Null);
            Assert.That(
                difficulty.transform.Find(
                    HudButtonId.DifficultyNormal.ToString())
                    ?.GetComponent<Button>(),
                Is.Not.Null);
        }

        [Test]
        public void PrototypeHud_UpdatesAndBindsSharedCardRerolls()
        {
            MobileControlSettings originalControlSettings =
                MobileControlSettingsStore.Load();
            MobileControlSettingsStore.Save(
                MobileControlSettings.Default);
            GameObject hudPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrototypeSceneBuilder.PrototypeHudPrefabPath);
            GameObject hud = UnityEngine.Object.Instantiate(hudPrefab);
            try
            {
                PrototypeHUDView view =
                    hud.GetComponent<PrototypeHUDView>();
                int settingsClicks = 0;
                int rerollClicks = 0;
                int attackPresses = 0;
                view.Initialize();
                view.Bind(
                    HudButtonId.Settings,
                    () => settingsClicks++);
                view.Bind(
                    HudButtonId.CardReroll0,
                    () => rerollClicks++);
                view.Bind(
                    HudButtonId.Attack,
                    () => attackPresses++);
                view.ShowCardSelection(true);
                view.SetCardRerollState(2, true);
                view.SetCardChoicesInteractable(true);
                view.ShowPauseDetails(true);

                Transform controlPadToggle =
                    hud.transform.Find(
                        "ModalRoot/PauseDetailsPanel/" +
                        "ControlPadToggle");
                Assert.That(controlPadToggle, Is.Not.Null);
                Toggle toggle =
                    controlPadToggle.GetComponent<Toggle>();
                Assert.That(toggle.isOn, Is.True);
                Assert.That(view.CommandControlsEnabled, Is.True);

                toggle.isOn = false;
                Assert.That(view.CommandControlsEnabled, Is.False);
                Assert.That(
                    MobileControlSettingsStore.Load().controlsEnabled,
                    Is.False);
                Assert.That(
                    view.AimJoystick.gameObject.activeSelf,
                    Is.False);
                Assert.That(
                    view.AttackButton.gameObject.activeSelf,
                    Is.False);

                Transform controlSettingsButton =
                    hud.transform.Find(
                        "ModalRoot/PauseDetailsPanel/" +
                        "ControlSettingsButton");
                Transform controlSettingsPanel =
                    hud.transform.Find(
                        "ModalRoot/PauseDetailsPanel/" +
                        "ControlSettingsPanel");
                Assert.That(controlSettingsButton, Is.Not.Null);
                Assert.That(controlSettingsPanel, Is.Not.Null);
                Image pauseBackground =
                    controlPadToggle.parent.GetComponent<Image>();
                float mainBackgroundAlpha = pauseBackground.color.a;

                controlSettingsButton.GetComponent<Button>()
                    .onClick.Invoke();
                Assert.That(view.CommandControlsEnabled, Is.False);
                Assert.That(
                    MobileControlSettingsStore.Load().controlsEnabled,
                    Is.False);
                Assert.That(
                    view.AimJoystick.gameObject.activeSelf,
                    Is.True);
                Assert.That(
                    view.AttackButton.gameObject.activeSelf,
                    Is.True);
                Assert.That(
                    pauseBackground.color.a,
                    Is.LessThan(0.2f));
                Assert.That(
                    controlSettingsPanel.GetComponent<Image>().color.a,
                    Is.LessThan(0.5f));
                controlSettingsPanel.Find("ControlCancelButton")
                    .GetComponent<Button>()
                    .onClick.Invoke();
                Assert.That(
                    view.AimJoystick.gameObject.activeSelf,
                    Is.False);
                Assert.That(
                    view.AttackButton.gameObject.activeSelf,
                    Is.False);
                Assert.That(
                    pauseBackground.color.a,
                    Is.EqualTo(mainBackgroundAlpha).Within(0.001f));

                toggle.isOn = true;
                Assert.That(view.CommandControlsEnabled, Is.True);
                Assert.That(
                    MobileControlSettingsStore.Load().controlsEnabled,
                    Is.True);
                Assert.That(
                    view.AimJoystick.gameObject.activeSelf,
                    Is.True);
                Assert.That(
                    view.AttackButton.gameObject.activeSelf,
                    Is.True);

                controlSettingsButton.GetComponent<Button>()
                    .onClick.Invoke();
                Assert.That(
                    controlSettingsPanel.gameObject.activeSelf,
                    Is.True);
                Assert.That(
                    controlPadToggle.gameObject.activeSelf,
                    Is.False);

                Slider joystickSize = controlSettingsPanel.Find(
                        "JoystickSizeSlider")
                    .GetComponent<Slider>();
                joystickSize.value = 1.5f;
                Assert.That(
                    view.AimJoystick.transform.localScale.x,
                    Is.EqualTo(1.5f).Within(0.001f));
                controlSettingsPanel.Find("ControlCancelButton")
                    .GetComponent<Button>()
                    .onClick.Invoke();
                Assert.That(
                    view.AimJoystick.transform.localScale.x,
                    Is.EqualTo(1f).Within(0.001f));

                controlSettingsButton.GetComponent<Button>()
                    .onClick.Invoke();
                Slider attackSize = controlSettingsPanel.Find(
                        "AttackSizeSlider")
                    .GetComponent<Slider>();
                attackSize.value = 0.7f;
                controlSettingsPanel.Find("ControlApplyButton")
                    .GetComponent<Button>()
                    .onClick.Invoke();
                Assert.That(
                    view.AttackButton.transform.localScale.x,
                    Is.EqualTo(0.7f).Within(0.001f));
                Assert.That(
                    MobileControlSettingsStore.Load().attackScale,
                    Is.EqualTo(0.7f).Within(0.001f));

                controlSettingsButton.GetComponent<Button>()
                    .onClick.Invoke();
                joystickSize.value = 1.5f;
                controlSettingsPanel.Find("ControlDefaultsButton")
                    .GetComponent<Button>()
                    .onClick.Invoke();
                Assert.That(
                    joystickSize.value,
                    Is.EqualTo(1f).Within(0.001f));
                Assert.That(
                    attackSize.value,
                    Is.EqualTo(1f).Within(0.001f));
                controlSettingsPanel.Find("ControlCancelButton")
                    .GetComponent<Button>()
                    .onClick.Invoke();
                Assert.That(
                    view.AttackButton.transform.localScale.x,
                    Is.EqualTo(0.7f).Within(0.001f));

                Transform panel = hud.transform.Find(
                    "ModalRoot/CardSelectionPanel");
                Assert.That(panel, Is.Not.Null);
                LevelUpCardView[] cards =
                    panel.GetComponentsInChildren<
                        LevelUpCardView>(true);
                Assert.That(cards, Has.Length.EqualTo(3));
                foreach (LevelUpCardView card in cards)
                {
                    Assert.That(
                        card.RerollLabel.text,
                        Is.EqualTo("교체 2"));
                    Assert.That(
                        card.RerollButton.interactable,
                        Is.True);
                }

                cards[0].RerollButton.onClick.Invoke();
                view.SettingsButton.onClick.Invoke();
                view.AttackButton
                    .GetComponent<AttackCommandButton>()
                    .OnPointerDown(null);
                view.AttackButton.onClick.Invoke();
                Assert.That(rerollClicks, Is.EqualTo(1));
                Assert.That(settingsClicks, Is.EqualTo(1));
                Assert.That(attackPresses, Is.EqualTo(1));

                view.SetCardRerollState(0, true);
                foreach (LevelUpCardView card in cards)
                {
                    Assert.That(
                        card.RerollLabel.text,
                        Is.EqualTo("교체 0"));
                    Assert.That(
                        card.RerollButton.interactable,
                        Is.False);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(hud);
                MobileControlSettingsStore.Save(
                    originalControlSettings);
            }
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
