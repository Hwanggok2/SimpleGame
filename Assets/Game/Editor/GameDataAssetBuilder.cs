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
                manifest.AccountLevelExperience.Rows.Count == 0)
            {
                throw new InvalidOperationException(
                    "Player or account level EXP table is empty.");
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
            EnsureFolder(DataPath);
            EnsureFolder(CatalogPath);
            EnsureFolder(GeneratedPath);
            EnsureFolder(ProfilePath);

            EnemyBalanceTable enemyBalance =
                CreateOrLoad<EnemyBalanceTable>(EnemyBalancePath);
            enemyBalance.Configure(Enum.GetValues(typeof(EnemyArchetype))
                .Cast<EnemyArchetype>()
                .Select(PrototypeEnemyDefinitions.Create));

            StageSpawnSchedule spawnSchedule =
                CreateOrLoad<StageSpawnSchedule>(SpawnSchedulePath);
            spawnSchedule.Configure(CreatePrototypeSpawnRows());

            LevelExperienceTable playerLevels =
                CreateOrLoad<LevelExperienceTable>(PlayerLevelPath);
            playerLevels.Configure(
                Enumerable.Range(1, 20)
                    .Select(level =>
                        new LevelExperienceRow(level, 3 + level * 2)));

            LevelExperienceTable accountLevels =
                CreateOrLoad<LevelExperienceTable>(AccountLevelPath);
            accountLevels.Configure(new[]
            {
                new LevelExperienceRow(1, 40),
                new LevelExperienceRow(2, 60),
                new LevelExperienceRow(3, 100),
                new LevelExperienceRow(4, 200)
            });

            GlobalBalance globalBalance =
                CreateOrLoad<GlobalBalance>(GlobalBalancePath);
            globalBalance.Configure(5, 1, 0.1f, 0.7f);

            EnemyAssetCatalog enemyCatalog =
                CreateOrLoad<EnemyAssetCatalog>(
                    EnemyCatalogPath,
                    out bool enemyCatalogCreated);
            if (enemyCatalogCreated || enemyCatalog.Entries.Count == 0)
            {
                enemyCatalog.Configure(new[]
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
                        CharacterAssetBuilder.BossPrefabPath)
                });
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
                enemyCatalog,
                feedback);

            MarkDirty(
                enemyBalance,
                spawnSchedule,
                playerLevels,
                accountLevels,
                globalBalance,
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
                factory.GetComponent<StageSpawnController>();
            if (stageSpawner == null)
            {
                stageSpawner =
                    Undo.AddComponent<StageSpawnController>(factory.gameObject);
            }

            factory.ConfigureAssets(
                manifest.EnemyAssets,
                manifest.EnemyBalance);
            feedback.Configure(cameraShake, manifest.CombatFeedback);
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
            return result;
        }

        private static EnemyAssetEntry CreateEnemyAssetEntry(
            EnemyArchetype archetype,
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
                PrototypeEnemyDefinitions.GetEnemyId(archetype),
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

        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Split('/').Skip(1))
            {
                string next = $"{current}/{part}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, part);
                }

                current = next;
            }
        }
    }
}
