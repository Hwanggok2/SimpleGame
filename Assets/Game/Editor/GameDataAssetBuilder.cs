using System;
using System.Collections.Generic;
using System.Linq;
using SimpleGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SimpleGameEditor
{
    public static class GameDataAssetBuilder
    {
        public const string ManifestPath =
            "Assets/Game/Data/GameDataManifest.asset";

        private const string DataPath = "Assets/Game/Data";
        private const string CatalogPath = DataPath + "/Catalogs";
        private const string GeneratedPath = DataPath + "/Generated";
        private const string ProfilePath = DataPath + "/Profiles";
        private const string EnemyCatalogPath =
            CatalogPath + "/EnemyAssetCatalog.asset";
        private const string FeedbackProfilePath =
            ProfilePath + "/CombatFeedbackProfile.asset";
        private const string EnemyBalancePath =
            GeneratedPath + "/EnemyBalanceTable.asset";
        private const string SpawnSchedulePath =
            GeneratedPath + "/StageSpawnSchedule.asset";
        private const string PlayerLevelPath =
            GeneratedPath + "/PlayerLevelExperience.asset";
        private const string AccountLevelPath =
            GeneratedPath + "/AccountLevelExperience.asset";
        private const string GlobalBalancePath =
            GeneratedPath + "/GlobalBalance.asset";
        private const string PlayerBalancePath =
            GeneratedPath + "/PlayerBalanceTable.asset";
        private const string LevelUpCardPath =
            GeneratedPath + "/LevelUpCardTable.asset";
        private const string GameStringPath =
            GeneratedPath + "/GameStringTable.asset";

        [MenuItem("SimpleGame/Data/Create or Update Data Assets")]
        public static void BuildAndWireActiveScene()
        {
            GameDataManifest manifest = BuildAssets();
            WireActiveScene(manifest, true);
            ValidateActiveData();
        }

        [MenuItem("SimpleGame/Data/Validate Active Data")]
        public static void ValidateActiveData()
        {
            GameDataManifest manifest =
                AssetDatabase.LoadAssetAtPath<GameDataManifest>(ManifestPath);
            SpawnPointRegistry registry =
                UnityEngine.Object.FindAnyObjectByType<SpawnPointRegistry>(
                    FindObjectsInactive.Include);
            if (manifest == null || !manifest.IsConfigured || registry == null)
            {
                throw new InvalidOperationException(
                    "GameDataManifest or SpawnPointRegistry is not configured.");
            }

            var runtimeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (StageSpawnEntry entry in manifest.StageSpawnSchedule.Entries)
            {
                if (!runtimeIds.Add(entry.RuntimeId))
                {
                    throw new InvalidOperationException(
                        $"Duplicate spawn id: {entry.RuntimeId}");
                }

                if (!manifest.EnemyBalance.TryGet(
                        entry.EnemyId,
                        out EnemyDefinition definition))
                {
                    throw new InvalidOperationException(
                        $"Enemy balance not found: {entry.EnemyId}");
                }

                if (!manifest.EnemyAssets.TryGetPrefab(
                        entry.EnemyId,
                        out EnemyBase prefab) ||
                    prefab.Archetype != definition.Archetype)
                {
                    throw new InvalidOperationException(
                        $"Enemy prefab mapping is invalid: {entry.EnemyId}");
                }

                if (!registry.TryGet(entry.SpawnPointId, out _))
                {
                    throw new InvalidOperationException(
                        $"Spawn point not found: {entry.SpawnPointId}");
                }
            }

            if (manifest.PlayerLevelExperience.Rows.Count == 0 ||
                manifest.AccountLevelExperience.Rows.Count == 0 ||
                manifest.PlayerBalance.Definitions.Count == 0 ||
                manifest.LevelUpCards.Definitions.Count == 0)
            {
                throw new InvalidOperationException(
                    "Player, card, or level EXP data is empty.");
            }

            PrototypeGameSession session =
                UnityEngine.Object.FindAnyObjectByType<PrototypeGameSession>(
                    FindObjectsInactive.Include);
            StageSpawnController spawner =
                UnityEngine.Object.FindAnyObjectByType<StageSpawnController>(
                    FindObjectsInactive.Include);
            Debug.Log(
                $"Game data valid: {manifest.EnemyBalance.Definitions.Count} " +
                $"enemies, {manifest.StageSpawnSchedule.Entries.Count} spawns, " +
                $"{registry.SpawnPoints.Count} points. " +
                $"Runtime={Application.isPlaying}, " +
                $"SessionEnabled={session != null && session.enabled}, " +
                $"Elapsed={(session?.ElapsedTime ?? 0f):0.00}, " +
                $"Pending={spawner?.PendingCount ?? 0}.");
        }

        public static GameDataManifest BuildAssets()
        {
            EditorAssetUtility.EnsureFolder(DataPath);
            EditorAssetUtility.EnsureFolder(CatalogPath);
            EditorAssetUtility.EnsureFolder(GeneratedPath);
            EditorAssetUtility.EnsureFolder(ProfilePath);

            EnemyBalanceTable enemyBalance =
                CreateOrLoad<EnemyBalanceTable>(
                    EnemyBalancePath,
                    out bool enemyBalanceCreated);
            EnemyDefinition[] requiredEnemyDefinitions =
                CreateRequiredEnemyDefinitions();
            if (enemyBalanceCreated ||
                enemyBalance.Definitions.Count == 0)
            {
                enemyBalance.Configure(requiredEnemyDefinitions);
            }
            else
            {
                var definitions = new List<EnemyDefinition>(
                    enemyBalance.Definitions);
                foreach (EnemyDefinition required in
                         requiredEnemyDefinitions)
                {
                    if (!definitions.Any(existing =>
                            string.Equals(
                                existing.EnemyId,
                                required.EnemyId,
                                StringComparison.Ordinal)))
                    {
                        definitions.Add(required);
                    }
                }

                enemyBalance.Configure(definitions);
            }

            StageSpawnSchedule spawnSchedule =
                CreateOrLoad<StageSpawnSchedule>(
                    SpawnSchedulePath,
                    out bool spawnScheduleCreated);
            if (spawnScheduleCreated || spawnSchedule.Entries.Count == 0)
            {
                spawnSchedule.Configure(CreatePrototypeSpawnRows());
            }

            LevelExperienceTable playerLevels =
                CreateOrLoad<LevelExperienceTable>(
                    PlayerLevelPath,
                    out bool playerLevelsCreated);
            if (playerLevelsCreated || playerLevels.Rows.Count == 0)
            {
                playerLevels.Configure(
                    Enumerable.Range(
                        1,
                        ProgressionCurve.MaximumPlayerLevel)
                        .Select(level =>
                            new LevelExperienceRow(
                                level,
                                ProgressionCurve
                                    .CalculateRequiredExperience(level))));
            }

            LevelExperienceTable accountLevels =
                CreateOrLoad<LevelExperienceTable>(
                    AccountLevelPath,
                    out bool accountLevelsCreated);
            if (accountLevelsCreated || accountLevels.Rows.Count == 0)
            {
                accountLevels.Configure(new[]
                {
                    new LevelExperienceRow(1, 40),
                    new LevelExperienceRow(2, 60),
                    new LevelExperienceRow(3, 100),
                    new LevelExperienceRow(4, 200)
                });
            }

            GlobalBalance globalBalance =
                CreateOrLoad<GlobalBalance>(
                    GlobalBalancePath,
                    out bool globalBalanceCreated);
            if (globalBalanceCreated)
            {
                globalBalance.Configure(
                    5,
                    1,
                    0.05f,
                    0.5f,
                    5,
                    9,
                    1);
            }

            PlayerBalanceTable playerBalance =
                CreateOrLoad<PlayerBalanceTable>(
                    PlayerBalancePath,
                    out bool playerBalanceCreated);
            if (playerBalanceCreated ||
                playerBalance.Definitions.Count == 0)
            {
                playerBalance.Configure(new[]
                {
                    CreateDefaultPlayerDefinition()
                });
            }

            LevelUpCardTable levelUpCards =
                CreateOrLoad<LevelUpCardTable>(
                    LevelUpCardPath,
                    out bool levelUpCardsCreated);
            if (levelUpCardsCreated ||
                levelUpCards.Definitions.Count == 0)
            {
                levelUpCards.Configure(CreateDefaultLevelUpCards());
            }

            GameStringTable gameStrings =
                CreateOrLoad<GameStringTable>(
                    GameStringPath,
                    out _);

            EnemyAssetCatalog enemyCatalog =
                CreateOrLoad<EnemyAssetCatalog>(
                    EnemyCatalogPath,
                    out bool enemyCatalogCreated);
            EnemyAssetEntry[] requiredEnemyAssets =
                CreateRequiredEnemyAssetEntries();
            if (enemyCatalogCreated || enemyCatalog.Entries.Count == 0)
            {
                enemyCatalog.Configure(requiredEnemyAssets);
            }
            else
            {
                var entries = new List<EnemyAssetEntry>(
                    enemyCatalog.Entries);
                foreach (EnemyAssetEntry required in requiredEnemyAssets)
                {
                    if (!enemyCatalog.TryGetPrefab(
                            required.EnemyId,
                            out _))
                    {
                        entries.Add(required);
                    }
                }

                enemyCatalog.Configure(entries);
            }

            CombatFeedbackProfile feedback =
                CreateOrLoad<CombatFeedbackProfile>(
                    FeedbackProfilePath,
                    out bool feedbackCreated);
            if (feedbackCreated)
            {
                feedback.Configure(
                    0.07f,
                    0.1f,
                    0.13f,
                    0.14f,
                    0.13f,
                    0.14f,
                    0.22f,
                    0.18f);
            }

            GameDataManifest manifest =
                CreateOrLoad<GameDataManifest>(ManifestPath);
            manifest.Configure(
                enemyBalance,
                spawnSchedule,
                playerLevels,
                accountLevels,
                globalBalance,
                playerBalance,
                levelUpCards,
                enemyCatalog,
                feedback,
                gameStrings);

            MarkDirty(
                enemyBalance,
                spawnSchedule,
                playerLevels,
                accountLevels,
                globalBalance,
                playerBalance,
                levelUpCards,
                gameStrings,
                enemyCatalog,
                feedback,
                manifest);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return manifest;
        }

        public static void WireActiveScene(
            GameDataManifest manifest,
            bool saveScene)
        {
            PrototypeEnemyFactory factory =
                UnityEngine.Object.FindAnyObjectByType<PrototypeEnemyFactory>(
                    FindObjectsInactive.Include);
            PrototypeGameSession session =
                UnityEngine.Object.FindAnyObjectByType<PrototypeGameSession>(
                    FindObjectsInactive.Include);
            CombatFeedbackController feedback =
                UnityEngine.Object.FindAnyObjectByType<CombatFeedbackController>(
                    FindObjectsInactive.Include);
            CameraShakeController cameraShake =
                UnityEngine.Object.FindAnyObjectByType<CameraShakeController>(
                    FindObjectsInactive.Include);
            GameObject spawnRoot = GameObject.Find("SpawnTransform");
            if (factory == null ||
                session == null ||
                feedback == null ||
                cameraShake == null ||
                spawnRoot == null)
            {
                throw new InvalidOperationException(
                    "Active scene requires Prototype systems, camera, " +
                    "and SpawnTransform before data wiring.");
            }

            Transform misspelledLeft = spawnRoot.transform.Find("LeftSpwan");
            if (misspelledLeft != null)
            {
                misspelledLeft.name = "LeftSpawn";
            }

            SpawnPointRegistry registry =
                spawnRoot.GetComponent<SpawnPointRegistry>();
            if (registry == null)
            {
                registry = Undo.AddComponent<SpawnPointRegistry>(spawnRoot);
            }

            List<Transform> spawnPoints = spawnRoot
                .GetComponentsInChildren<Transform>(true)
                .Where(transform => IsSpawnPointId(transform.name))
                .OrderBy(transform => transform.name, StringComparer.Ordinal)
                .ToList();
            registry.Configure(spawnPoints);

            StageSpawnController stageSpawner =
                UnityEngine.Object.FindAnyObjectByType<StageSpawnController>(
                    FindObjectsInactive.Include);
            if (stageSpawner == null)
            {
                GameObject spawningSystems =
                    PrototypeSceneBuilder.GetOrCreateSystemGroup(
                        session.transform,
                        "Spawning",
                        true);
                stageSpawner = Undo.AddComponent<StageSpawnController>(
                    spawningSystems);
            }

            factory.ConfigureAssets(
                manifest.EnemyAssets,
                manifest.EnemyBalance);
            GameObject damagePopupObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    CharacterAssetBuilder.DamagePopupPrefabPath);
            DamagePopupView damagePopup = damagePopupObject != null
                ? damagePopupObject.GetComponent<DamagePopupView>()
                : null;
            if (damagePopup == null)
            {
                throw new InvalidOperationException(
                    "Damage popup prefab is missing or invalid: " +
                    CharacterAssetBuilder.DamagePopupPrefabPath);
            }

            feedback.Configure(
                cameraShake,
                manifest.CombatFeedback,
                damagePopup);
            stageSpawner.Configure(manifest, registry, factory);
            session.ConfigureData(manifest, stageSpawner);

            MarkDirty(factory, feedback, registry, stageSpawner, session);
            EditorSceneManager.MarkSceneDirty(
                EditorSceneManager.GetActiveScene());
            if (saveScene)
            {
                EditorSceneManager.SaveOpenScenes();
            }

            Debug.Log(
                $"Game data assets updated and {spawnPoints.Count} " +
                "spawn points wired.");
        }

        private static EnemyDefinition[]
            CreateRequiredEnemyDefinitions()
        {
            return new[]
            {
                PrototypeEnemyDefinitions.Create(
                    EnemyArchetype.Melee),
                PrototypeEnemyDefinitions.Create(
                    EnemyArchetype.Ranged),
                PrototypeEnemyDefinitions.Create(
                    EnemyArchetype.Shield),
                PrototypeEnemyDefinitions.Create(
                    EnemyArchetype.Boss),
                PrototypeEnemyDefinitions.CreateMushroomBoss(),
                PrototypeEnemyDefinitions.CreateFlyingEye(),
                PrototypeEnemyDefinitions.CreateFlyingEyeBoss(),
                PrototypeEnemyDefinitions.CreateSkeletonBoss()
            };
        }

        private static EnemyAssetEntry[]
            CreateRequiredEnemyAssetEntries()
        {
            return new[]
            {
                CreateEnemyAssetEntry(
                    EnemyArchetype.Melee,
                    CharacterAssetBuilder.MeleePrefabPath),
                CreateEnemyAssetEntry(
                    EnemyArchetype.Ranged,
                    CharacterAssetBuilder.RangedPrefabPath),
                CreateEnemyAssetEntry(
                    EnemyArchetype.Shield,
                    CharacterAssetBuilder.ShieldPrefabPath),
                CreateEnemyAssetEntry(
                    EnemyArchetype.Boss,
                    CharacterAssetBuilder.BossPrefabPath),
                CreateEnemyAssetEntry(
                    PrototypeEnemyDefinitions.MushroomBossId,
                    CharacterAssetBuilder.MushroomBossPrefabPath),
                CreateEnemyAssetEntry(
                    PrototypeEnemyDefinitions.FlyingEyeId,
                    CharacterAssetBuilder.FlyingEyePrefabPath),
                CreateEnemyAssetEntry(
                    PrototypeEnemyDefinitions.FlyingEyeBossId,
                    CharacterAssetBuilder.FlyingEyeBossPrefabPath),
                CreateEnemyAssetEntry(
                    PrototypeEnemyDefinitions.SkeletonBossId,
                    CharacterAssetBuilder.SkeletonBossPrefabPath)
            };
        }

        private static List<StageSpawnEntry> CreatePrototypeSpawnRows()
        {
            var result = new List<StageSpawnEntry>();
            for (int index = 1; index <= 6; index++)
            {
                result.Add(new StageSpawnEntry(
                    "Stage01",
                    "WAVE_01",
                    1f,
                    index,
                    $"TOP_{index:00}",
                    "GoblinMelee",
                    1));
            }

            result.Add(new StageSpawnEntry(
                "Stage01",
                "WAVE_01",
                1f,
                7,
                "RIGHT_03",
                "ShieldSkeleton",
                1));

            for (int index = 1; index <= 6; index++)
            {
                result.Add(new StageSpawnEntry(
                    "Stage01",
                    "WAVE_01",
                    1f,
                    index + 7,
                    $"BOTTOM_{index:00}",
                    "GoblinMelee",
                    1));
            }

            result.Add(new StageSpawnEntry(
                "Stage01",
                "WAVE_01",
                1f,
                14,
                "LEFT_03",
                "ShieldSkeleton",
                1));
            result.Add(new StageSpawnEntry(
                "Stage01",
                "WAVE_05",
                120f,
                15,
                "TOP_03",
                "GoblinBoss",
                5));
            result.Add(new StageSpawnEntry(
                "Stage01",
                "WAVE_24",
                234.34f,
                15,
                "BOTTOM_05",
                PrototypeEnemyDefinitions.MushroomBossId,
                20));
            return result;
        }

        private static PlayerDefinition CreateDefaultPlayerDefinition()
        {
            return new PlayerDefinition(
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
                true);
        }

        private static IEnumerable<LevelUpCardDefinition>
            CreateDefaultLevelUpCards()
        {
            return new[]
            {
                new LevelUpCardDefinition(
                    "CRIT_CHANCE_UP",
                    "CARD_CRIT_NAME",
                    "치명타 강화",
                    "치명타 확률이 5% 증가합니다. 최대 50%까지 적용됩니다.",
                    LevelUpCardEffectType.StatModifier,
                    PlayerStatId.CriticalChance,
                    StatOperation.Add,
                    0.05f,
                    5,
                    100,
                    1,
                    string.Empty,
                    "일반",
                    "ICON_CRIT",
                    true),
                new LevelUpCardDefinition(
                    "MAX_HP_UP",
                    "CARD_HP_NAME",
                    "체력 강화",
                    "최대 체력과 현재 체력이 5 증가합니다.",
                    LevelUpCardEffectType.StatModifier,
                    PlayerStatId.MaxHp,
                    StatOperation.Add,
                    5f,
                    5,
                    100,
                    1,
                    string.Empty,
                    "일반",
                    "ICON_HP",
                    true),
                new LevelUpCardDefinition(
                    "MOVE_SPEED_UP",
                    "CARD_SPEED_NAME",
                    "이동 속도 강화",
                    "이동 속도가 1 증가합니다. 최대 레벨에서는 목적지까지 약 0.15초에 이동합니다.",
                    LevelUpCardEffectType.StatModifier,
                    PlayerStatId.MoveSpeed,
                    StatOperation.Add,
                    1f,
                    5,
                    80,
                    2,
                    string.Empty,
                    "일반",
                    "ICON_SPEED",
                    true),
                new LevelUpCardDefinition(
                    "ATTACK_RANGE_UP",
                    "CARD_RANGE_NAME",
                    "공격 범위 강화",
                    "기본 공격 사거리가 0.15 증가합니다.",
                    LevelUpCardEffectType.StatModifier,
                    PlayerStatId.AttackRange,
                    StatOperation.Add,
                    0.15f,
                    3,
                    70,
                    3,
                    string.Empty,
                    "일반",
                    "ICON_RANGE",
                    true),
                new LevelUpCardDefinition(
                    "PIERCING_UP",
                    "CARD_PIERCING_NAME",
                    "관통",
                    "일반 공격은 0.4초 판정창마다 카드 레벨만큼 주 대상 뒤의 적에게 추가 피해를 줍니다. 이동 관통은 카드 레벨만큼 적을 지나간 뒤 0.4초 후 횟수가 재충전되며, 이동 입력을 유지하는 중에도 충전됩니다. 공격 관통 수와 이동 관통 수는 서로 별도로 소비합니다.",
                    LevelUpCardEffectType.UpgradeRank,
                    PlayerStatId.Piercing,
                    StatOperation.Add,
                    1f,
                    5,
                    90,
                    2,
                    string.Empty,
                    "희귀",
                    "ICON_PIERCING",
                    true),
                new LevelUpCardDefinition(
                    "SEVER_TRAIL",
                    "CARD_SEVER_NAME",
                    "절단",
                    "실제 이동 관통 0.3초 뒤 관통 시작 위치부터 현재 위치까지 검은 절단선을 만듭니다. 선은 0.1초 동안 사라지며 재사용 대기시간은 0.1초, 피해는 공격력의 2배입니다.",
                    LevelUpCardEffectType.UpgradeRank,
                    PlayerStatId.Sever,
                    StatOperation.Add,
                    2f,
                    1,
                    45,
                    3,
                    "PIERCING_UP",
                    "에픽",
                    "ICON_SEVER",
                    true),
                new LevelUpCardDefinition(
                    "HIT_HEAL",
                    "CARD_HIT_HEAL_NAME",
                    "흡혈",
                    "적을 처치할 때마다 5% 확률로 체력을 카드 레벨당 2 회복합니다. 1/2/3레벨 회복량은 2/4/6입니다.",
                    LevelUpCardEffectType.UpgradeRank,
                    PlayerStatId.HitHeal,
                    StatOperation.Add,
                    2f,
                    3,
                    55,
                    4,
                    string.Empty,
                    "희귀",
                    "ICON_HIT_HEAL",
                    true),
                new LevelUpCardDefinition(
                    "STATIC_CHARGE",
                    "CARD_STATIC_NAME",
                    "정전기",
                    "공격 대상과 주변 적에게 공격력의 0.75배 피해를 줍니다. 레벨마다 주변 대상이 2명 증가합니다.",
                    LevelUpCardEffectType.UpgradeRank,
                    PlayerStatId.StaticCharge,
                    StatOperation.Add,
                    0.75f,
                    5,
                    60,
                    4,
                    string.Empty,
                    "희귀",
                    "ICON_STATIC",
                    true),
                new LevelUpCardDefinition(
                    "MOVING_SLASH",
                    "CARD_MOVING_SLASH_NAME",
                    "참격",
                    "기본 공격 시 주 대상 방향으로 초승달 검기의 발동을 판정합니다. 방패에 막힌 공격도 판정하고 연속 발동할 수 있으며, 추가 피해로는 재발동하지 않습니다. 1~5레벨: 확률 15/19.5/24/28.5/33%, 피해 1.8/2.15/2.5/2.85/3.2배, 크기 100/115/130/145/160%, 사거리 6/7.5/9/10.5/12, 최대 타격 2/3/4/5/6.",
                    LevelUpCardEffectType.UpgradeRank,
                    PlayerStatId.MovingSlash,
                    StatOperation.Add,
                    1.8f,
                    5,
                    65,
                    3,
                    string.Empty,
                    "희귀",
                    "ICON_MOVING_SLASH",
                    true),
                new LevelUpCardDefinition(
                    "FILTH_THROW",
                    "CARD_FILTH_THROW_NAME",
                    "오물 투척",
                    "화면 안의 무작위 생존 적에게 오물 구체를 던집니다. 착탄 지점은 3초 동안 0.5초마다 피해를 주며, 레벨마다 투척 수·피해·범위가 증가하고 재사용 대기시간이 감소합니다. 1~5레벨 투척 수: 1/2/3/4/5, 반경: 1.2/1.32/1.44/1.56/1.68.",
                    LevelUpCardEffectType.UpgradeRank,
                    PlayerStatId.FilthThrow,
                    StatOperation.Add,
                    PlayerCombatAbilities
                        .FilthThrowBaseDamageMultiplier,
                    PlayerCombatAbilities
                        .FilthThrowMaximumLevel,
                    60,
                    3,
                    string.Empty,
                    "희귀",
                    "ICON_FILTH_THROW",
                    true),
                new LevelUpCardDefinition(
                    "SHIELD_BYPASS",
                    "CARD_SHIELD_BYPASS_NAME",
                    "방패 우회",
                    "방패병 정면 공격 시 반동과 0.5초 조작 불가를 무시할 확률이 레벨마다 10% 증가합니다.",
                    LevelUpCardEffectType.UpgradeRank,
                    PlayerStatId.ShieldBypass,
                    StatOperation.Add,
                    0.1f,
                    3,
                    55,
                    3,
                    string.Empty,
                    "희귀",
                    "ICON_SHIELD_BYPASS",
                    true),
                new LevelUpCardDefinition(
                    "FLYING_SWORD_COUNT",
                    "CARD_FLYING_SWORD_COUNT_NAME",
                    "이기어검 수 증가",
                    "플레이어가 적에게 피해를 주면 준비된 이기어검 1개가 무작위 적 Spawn 위치에서 날아갑니다. 레벨마다 검 1개가 활성화되며 검마다 0.3초 후 재충전됩니다.",
                    LevelUpCardEffectType.UpgradeRank,
                    PlayerStatId.FlyingSwordCount,
                    StatOperation.Add,
                    1f,
                    3,
                    60,
                    3,
                    string.Empty,
                    "희귀",
                    "ICON_FLYING_SWORD_COUNT",
                    true),
                new LevelUpCardDefinition(
                    "FLYING_SWORD_HITS",
                    "CARD_FLYING_SWORD_HITS_NAME",
                    "이기어검 타격 수 증가",
                    "이기어검 한 자루가 타격할 수 있는 적이 1명 증가합니다. 기본 2명이며 최대 5명입니다.",
                    LevelUpCardEffectType.UpgradeRank,
                    PlayerStatId.FlyingSwordHitCount,
                    StatOperation.Add,
                    1f,
                    3,
                    50,
                    4,
                    "FLYING_SWORD_COUNT",
                    "희귀",
                    "ICON_FLYING_SWORD_HITS",
                    true),
                new LevelUpCardDefinition(
                    "FUSION_FLYING_SWORD_PIERCING",
                    "CARD_FUSION_FLYING_SWORD_PIERCING_NAME",
                    "이기어검·관통 융합",
                    "이기어검이 경로상의 모든 적을 관통합니다.",
                    LevelUpCardEffectType.Fusion,
                    PlayerStatId.FlyingSwordPiercingFusion,
                    StatOperation.Add,
                    1f,
                    1,
                    10,
                    1,
                    string.Empty,
                    "레전더리",
                    "ICON_FLYING_SWORD_COUNT",
                    true,
                    "FLYING_SWORD_COUNT|FLYING_SWORD_HITS|PIERCING_UP"),
                new LevelUpCardDefinition(
                    "FUSION_FLYING_SWORD_STATIC",
                    "CARD_FUSION_FLYING_SWORD_STATIC_NAME",
                    "이기어검·정전기 융합",
                    "이기어검에 적중한 각 적을 중심으로 정전기가 발생합니다.",
                    LevelUpCardEffectType.Fusion,
                    PlayerStatId.FlyingSwordStaticFusion,
                    StatOperation.Add,
                    1f,
                    1,
                    10,
                    1,
                    string.Empty,
                    "레전더리",
                    "ICON_STATIC",
                    true,
                    "FLYING_SWORD_COUNT|FLYING_SWORD_HITS|STATIC_CHARGE"),
                new LevelUpCardDefinition(
                    "FUSION_STATIC_FILTH",
                    "CARD_FUSION_STATIC_FILTH_NAME",
                    "정전기·오물 투척 융합",
                    "오물에 처음 적중한 각 적을 중심으로 정전기가 발생합니다.",
                    LevelUpCardEffectType.Fusion,
                    PlayerStatId.StaticFilthFusion,
                    StatOperation.Add,
                    1f,
                    1,
                    10,
                    1,
                    string.Empty,
                    "레전더리",
                    "ICON_FILTH_THROW",
                    true,
                    "STATIC_CHARGE|FILTH_THROW")
            };
        }

        private static EnemyAssetEntry CreateEnemyAssetEntry(
            EnemyArchetype archetype,
            string prefabPath)
        {
            return CreateEnemyAssetEntry(
                PrototypeEnemyDefinitions.GetEnemyId(archetype),
                prefabPath);
        }

        private static EnemyAssetEntry CreateEnemyAssetEntry(
            string enemyId,
            string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                prefabPath);
            EnemyBase enemy = prefab != null
                ? prefab.GetComponent<EnemyBase>()
                : null;
            if (enemy == null)
            {
                throw new InvalidOperationException(
                    $"Enemy prefab not found: {prefabPath}");
            }

            return new EnemyAssetEntry(
                enemyId,
                enemy);
        }

        private static bool IsSpawnPointId(string value)
        {
            string[] prefixes = { "LEFT_", "RIGHT_", "TOP_", "BOTTOM_" };
            return prefixes.Any(prefix =>
                value.StartsWith(prefix, StringComparison.Ordinal));
        }

        private static T CreateOrLoad<T>(string path)
            where T : ScriptableObject
        {
            return CreateOrLoad<T>(path, out _);
        }

        private static T CreateOrLoad<T>(string path, out bool created)
            where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                created = false;
                return asset;
            }

            AssetDatabase.DeleteAsset(path);
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            created = true;
            return asset;
        }

        private static void MarkDirty(params UnityEngine.Object[] values)
        {
            foreach (UnityEngine.Object value in values)
            {
                EditorUtility.SetDirty(value);
            }
        }

    }
}
