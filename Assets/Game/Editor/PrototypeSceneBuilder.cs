using System;
using System.Collections.Generic;
using SimpleGame;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace SimpleGameEditor
{
    public static class PrototypeSceneBuilder
    {
        public const string LevelUpCardPrefabPath =
            CharacterAssetBuilder.PrefabRootPath +
            "/LevelUpCard.prefab";
        public const string PrototypeHudPrefabPath =
            CharacterAssetBuilder.PrefabRootPath +
            "/PrototypeHUD.prefab";
        public const string CardSelectionPanelPrefabPath =
            CharacterAssetBuilder.PrefabRootPath +
            "/CardSelectionPanel.prefab";
        public const string PauseDetailsPanelPrefabPath =
            CharacterAssetBuilder.PrefabRootPath +
            "/PauseDetailsPanel.prefab";
        public const string GameOverPanelPrefabPath =
            CharacterAssetBuilder.PrefabRootPath +
            "/GameOverPanel.prefab";
        public const string DifficultySelectionPanelPrefabPath =
            CharacterAssetBuilder.PrefabRootPath +
            "/DifficultySelectionPanel.prefab";
        private const string ScenePath = "Assets/Scenes/PrototypeScene.unity";
        private const string WorldTilePath = "Assets/Game/World/Tiles";
        private const float LevelUpCardWidth = 300f;
        private const float LevelUpCardHeight =
            LevelUpCardWidth * 1920f / 1080f;
        private const int ChunkCellCount = 8;
        private const float WorldCellSize = 2.56f;

        [MenuItem("SimpleGame/Build Prototype Scene %#b")]
        public static void Build()
        {
            CharacterAssetBuilder.Build();
            GameDataManifest gameData = GameDataAssetBuilder.BuildAssets();
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            Camera camera = CreateCamera();
            CreateMainLight();

            var systems = new GameObject("PrototypeSystems");
            PrototypeGameSession session =
                systems.AddComponent<PrototypeGameSession>();

            GameObject enemySystems =
                GetOrCreateSystemGroup(systems.transform, "EnemyWorld");
            EnemyWorldService enemyWorld =
                enemySystems.AddComponent<EnemyWorldService>();
            PrototypeEnemyFactory factory =
                enemySystems.AddComponent<PrototypeEnemyFactory>();
            factory.ConfigureAssets(
                gameData.EnemyAssets,
                gameData.EnemyBalance);
            EnemyWorldRecycler enemyRecycler =
                enemySystems.AddComponent<EnemyWorldRecycler>();

            GameObject combatSystems =
                GetOrCreateSystemGroup(systems.transform, "Combat");
            CombatFeedbackController combatFeedback =
                combatSystems.AddComponent<CombatFeedbackController>();
            combatFeedback.Configure(
                camera.GetComponent<CameraShakeController>(),
                gameData.CombatFeedback);

            GameObject spawningSystems =
                GetOrCreateSystemGroup(systems.transform, "Spawning");
            StageSpawnController stageSpawner =
                spawningSystems.AddComponent<StageSpawnController>();
            HealthPickupSpawner healthPickupSpawner =
                spawningSystems.AddComponent<HealthPickupSpawner>();
            PoisonCloudSpawner poisonCloudSpawner =
                spawningSystems.AddComponent<PoisonCloudSpawner>();

            var entities = new GameObject("Entities");
            Transform enemyRoot = new GameObject("Enemies").transform;
            enemyRoot.SetParent(entities.transform, false);
            Transform pickupRoot =
                new GameObject("HealthPickups").transform;
            pickupRoot.SetParent(entities.transform, false);
            Transform cloudRoot =
                new GameObject("PoisonClouds").transform;
            cloudRoot.SetParent(entities.transform, false);

            PlayerRoot player = CreatePlayer(entities.transform);
            PlayerWorldArea worldArea =
                player.gameObject.AddComponent<PlayerWorldArea>();
            worldArea.Configure(camera);
            camera.GetComponent<CameraFollowController>()
                .Configure(player.transform);
            CreateWorldChunks(player.transform);
            SpawnPointRegistry spawnPoints =
                CreateDefaultSpawnPoints(player.transform);
            stageSpawner.Configure(gameData, spawnPoints, factory);
            HealthPickup healthPickupPrefab =
                LoadPrefabComponent<HealthPickup>(
                    CharacterAssetBuilder.HealthPickupPrefabPath);
            MushroomPoisonCloud poisonCloudPrefab =
                LoadPrefabComponent<MushroomPoisonCloud>(
                    CharacterAssetBuilder.PoisonCloudPrefabPath);
            healthPickupSpawner.Configure(
                session,
                player,
                worldArea,
                healthPickupPrefab,
                pickupRoot);
            poisonCloudSpawner.Configure(
                session,
                player,
                poisonCloudPrefab,
                cloudRoot);
            PrototypeHUDPresenter presenter = CreateHud();

            session.ConfigureScene(
                player,
                factory,
                enemyRoot,
                camera,
                combatFeedback,
                enemyRecycler,
                presenter,
                enemyWorld);
            session.ConfigureData(gameData, stageSpawner);
            session.ConfigureWorldRewards(poisonCloudSpawner);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = systems;
            Debug.Log($"Prototype scene created: {ScenePath}");
        }

        internal static GameObject GetOrCreateSystemGroup(
            Transform parent,
            string name,
            bool registerUndo = false)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var group = new GameObject(name);
            group.transform.SetParent(parent, false);
            if (registerUndo)
            {
                Undo.RegisterCreatedObjectUndo(
                    group,
                    $"Create {name} system group");
            }

            return group;
        }

        [MenuItem("SimpleGame/Build Level Up Card Prefab")]
        public static void BuildLevelUpCardPrefab()
        {
            ConfigureLevelUpCardPrefabAsset();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"Level-up card prefab updated: {LevelUpCardPrefabPath}");
        }

        [MenuItem("SimpleGame/Update Card Selection UI")]
        public static void UpdateCardSelectionUi()
        {
            EnsureUiPrefabAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "Level-up card and card-selection prefab references " +
                "are up to date.");
        }

        [MenuItem("SimpleGame/Migrate Scene UI To Prefabs")]
        public static void MigrateSceneUiToPrefabs()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    $"Open {ScenePath} before migrating its UI.");
            }

            PrototypeGameSession session =
                UnityEngine.Object.FindAnyObjectByType<
                    PrototypeGameSession>(
                    FindObjectsInactive.Include);
            if (session == null)
            {
                throw new InvalidOperationException(
                    "PrototypeGameSession was not found.");
            }

            GameObject existingHud = GameObject.Find("PrototypeHUD");
            if (existingHud != null)
            {
                UnityEngine.Object.DestroyImmediate(existingHud);
            }

            PrototypeHUDPresenter presenter = CreateHud();
            session.ConfigureHud(presenter);
            EditorUtility.SetDirty(session);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Scene UI migrated: persistent HUD is a prefab " +
                "instance and transient panels are runtime-only.");
        }

        private static Camera CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<CameraFollowController>();
            cameraObject.AddComponent<CameraShakeController>();
            camera.orthographic = true;
            camera.orthographicSize = 10f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.08f, 0.07f);
            return camera;
        }

        private static void CreateMainLight()
        {
            var lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
        }

        private static PlayerRoot CreatePlayer(Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CharacterAssetBuilder.PlayerPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Player prefab not found: {CharacterAssetBuilder.PlayerPrefabPath}");
            }

            var playerObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            playerObject.name = "Player";
            playerObject.transform.SetParent(parent, false);
            playerObject.transform.position = Vector3.zero;
            return playerObject.GetComponent<PlayerRoot>();
        }

        private static T LoadPrefabComponent<T>(string path)
            where T : Component
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(path);
            T component = prefab != null
                ? prefab.GetComponent<T>()
                : null;
            if (component == null)
            {
                throw new InvalidOperationException(
                    $"Prefab component {typeof(T).Name} not found: {path}");
            }

            return component;
        }

        private static SpawnPointRegistry CreateDefaultSpawnPoints(
            Transform player)
        {
            var root = new GameObject("SpawnTransform");
            root.transform.SetParent(player, false);
            var points = new System.Collections.Generic.List<Transform>();
            CreateVerticalSpawnGroup(
                root.transform,
                "LeftSpawn",
                "LEFT",
                -7f,
                -8.5f,
                8.5f,
                8,
                points);
            CreateVerticalSpawnGroup(
                root.transform,
                "RightSpawn",
                "RIGHT",
                7f,
                -8.5f,
                8.5f,
                8,
                points);
            CreateHorizontalSpawnGroup(
                root.transform,
                "TopSpawn",
                "TOP",
                11f,
                -4.8f,
                4.8f,
                8,
                points);
            CreateHorizontalSpawnGroup(
                root.transform,
                "BottomSpawn",
                "BOTTOM",
                -11f,
                -4.8f,
                4.8f,
                8,
                points);

            SpawnPointRegistry registry =
                root.AddComponent<SpawnPointRegistry>();
            registry.Configure(points);
            return registry;
        }

        private static void CreateVerticalSpawnGroup(
            Transform root,
            string groupName,
            string idPrefix,
            float x,
            float minY,
            float maxY,
            int count,
            System.Collections.Generic.ICollection<Transform> output)
        {
            var group = new GameObject(groupName);
            group.transform.SetParent(root, false);
            for (int index = 0; index < count; index++)
            {
                float progress = count == 1
                    ? 0.5f
                    : index / (count - 1f);
                var point = new GameObject($"{idPrefix}_{index + 1:00}");
                point.transform.SetParent(group.transform, false);
                point.transform.localPosition =
                    new Vector3(x, Mathf.Lerp(maxY, minY, progress), 0f);
                output.Add(point.transform);
            }
        }

        private static void CreateHorizontalSpawnGroup(
            Transform root,
            string groupName,
            string idPrefix,
            float y,
            float minX,
            float maxX,
            int count,
            System.Collections.Generic.ICollection<Transform> output)
        {
            var group = new GameObject(groupName);
            group.transform.SetParent(root, false);
            for (int index = 0; index < count; index++)
            {
                float progress = count == 1
                    ? 0.5f
                    : index / (count - 1f);
                var point = new GameObject($"{idPrefix}_{index + 1:00}");
                point.transform.SetParent(group.transform, false);
                point.transform.localPosition =
                    new Vector3(Mathf.Lerp(minX, maxX, progress), y, 0f);
                output.Add(point.transform);
            }
        }

        private static WorldChunkGrid CreateWorldChunks(Transform player)
        {
            Tile[] variants = CreateWorldTiles();
            var worldObject = new GameObject(
                "WorldGrid",
                typeof(Grid),
                typeof(WorldChunkGrid));
            Grid grid = worldObject.GetComponent<Grid>();
            grid.cellSize =
                new Vector3(WorldCellSize, WorldCellSize, 0f);

            var chunks = new List<WorldChunk>(9);
            int variantIndex = 0;
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    var chunkObject = new GameObject(
                        $"MapChunk_{x}_{y}",
                        typeof(Tilemap),
                        typeof(TilemapRenderer),
                        typeof(WorldChunk));
                    chunkObject.transform.SetParent(
                        worldObject.transform,
                        false);

                    Tilemap tilemap = chunkObject.GetComponent<Tilemap>();
                    Tile ground = variants[variantIndex % variants.Length];
                    variantIndex++;
                    int half = ChunkCellCount / 2;
                    for (int cellY = -half;
                         cellY < half;
                         cellY++)
                    {
                        for (int cellX = -half;
                             cellX < half;
                             cellX++)
                        {
                            tilemap.SetTile(
                                new Vector3Int(cellX, cellY, 0),
                                ground);
                        }
                    }

                    TilemapRenderer renderer =
                        chunkObject.GetComponent<TilemapRenderer>();
                    renderer.sortingOrder = -200;
                    WorldChunk chunk =
                        chunkObject.GetComponent<WorldChunk>();
                    chunk.Place(
                        new Vector2Int(x, y),
                        Vector2.one *
                            (ChunkCellCount * WorldCellSize));
                    chunks.Add(chunk);
                }
            }

            WorldChunkGrid chunkGrid =
                worldObject.GetComponent<WorldChunkGrid>();
            chunkGrid.Configure(
                player,
                Vector2.one * (ChunkCellCount * WorldCellSize),
                chunks);
            return chunkGrid;
        }

        private static Tile[] CreateWorldTiles()
        {
            EditorAssetUtility.EnsureFolder("Assets/Game/World");
            EditorAssetUtility.EnsureFolder(WorldTilePath);
            var result = new Tile[4];
            for (int index = 0; index < result.Length; index++)
            {
                int sourceIndex = index + 1;
                string tilePath =
                    $"{WorldTilePath}/Ground_{sourceIndex:00}.asset";
                Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, tilePath);
                }

                string spritePath =
                    "Assets/SourceAssets/PNG/" +
                    $"Top-Down Simple Summer_Ground {sourceIndex:00}.png";
                tile.sprite =
                    AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                tile.colliderType = Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);
                result[index] = tile;
            }

            AssetDatabase.SaveAssets();
            return result;
        }

        private static PrototypeHUDPresenter CreateHud()
        {
            EnsureUiPrefabAssets();
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrototypeHudPrefabPath);
            var instance =
                (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "PrototypeHUD";
            EnsureEventSystem();
            return instance.GetComponent<PrototypeHUDPresenter>();
        }

        private static void EnsureUiPrefabAssets()
        {
            EditorAssetUtility.EnsureFolder(
                CharacterAssetBuilder.PrefabRootPath);
            ConfigureLevelUpCardPrefabAsset();

            GameObject cardSelectionPrefab =
                LoadOrCreatePrefab(
                    CardSelectionPanelPrefabPath,
                    CreateCardSelectionPanelPrefab);
            GameObject pausePrefab =
                LoadOrCreatePrefab(
                    PauseDetailsPanelPrefabPath,
                    CreatePauseDetailsPanelPrefab);
            Transform controlSettingsPanel = pausePrefab.transform.Find(
                "ControlSettingsPanel");
            Image controlSettingsBackground =
                controlSettingsPanel != null
                    ? controlSettingsPanel.GetComponent<Image>()
                    : null;
            if (pausePrefab.transform.Find(
                    "ControlPadToggle") == null ||
                pausePrefab.transform.Find(
                    "AutoAttackToggle") == null ||
                pausePrefab.transform.Find(
                    "ControlSettingsButton") == null ||
                controlSettingsPanel == null ||
                controlSettingsBackground == null ||
                !Mathf.Approximately(
                    controlSettingsBackground.color.a,
                    0.45f))
            {
                pausePrefab = CreatePauseDetailsPanelPrefab();
            }

            GameObject gameOverPrefab =
                LoadOrCreatePrefab(
                    GameOverPanelPrefabPath,
                    CreateGameOverPanelPrefab);
            GameObject difficultySelectionPrefab =
                LoadOrCreatePrefab(
                    DifficultySelectionPanelPrefabPath,
                    CreateDifficultySelectionPanelPrefab);

            GameObject hudPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrototypeHudPrefabPath);
            PrototypeHUDView existingHudView =
                hudPrefab != null
                    ? hudPrefab.GetComponent<PrototypeHUDView>()
                    : null;
            if (existingHudView == null ||
                existingHudView.SettingsButton == null ||
                existingHudView.AttackButton == null ||
                existingHudView.AttackButton.GetComponent<
                    AttackCommandButton>() == null ||
                existingHudView.AimJoystick == null ||
                existingHudView.DifficultySelectionPanelPrefab == null)
            {
                CreatePrototypeHudPrefab(
                    cardSelectionPrefab,
                    pausePrefab,
                    gameOverPrefab,
                    difficultySelectionPrefab);
            }

            AssetDatabase.SaveAssets();
        }

        private static GameObject LoadOrCreatePrefab(
            string path,
            Func<GameObject> create)
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab != null ? prefab : create();
        }

        private static GameObject CreatePrototypeHudPrefab(
            GameObject cardSelectionPrefab,
            GameObject pausePrefab,
            GameObject gameOverPrefab,
            GameObject difficultySelectionPrefab)
        {
            var canvasObject = new GameObject(
                "PrototypeHUD",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject topPanel = CreatePanel(
                canvasObject.transform,
                "TopPanel",
                new Color(0.04f, 0.07f, 0.08f, 0.9f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -142f),
                Vector2.zero);
            TMP_Text timeLabel = CreateTextObject(
                topPanel.transform,
                HudTextId.Time.ToString(),
                "00:00",
                34f);
            ConfigureTimeLabel(timeLabel);
            TMP_Text hpLabel = CreateTextObject(
                topPanel.transform,
                HudTextId.PlayerHp.ToString(),
                "체력 10/10",
                26f);
            ConfigureHpLabel(hpLabel);
            Slider experienceSlider = CreateExperienceSlider(
                topPanel.transform,
                out TMP_Text experienceLabel);

            GameObject hintPanel = CreatePanel(
                canvasObject.transform,
                "HintPanel",
                new Color(0f, 0f, 0f, 0.72f),
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                Vector2.zero,
                new Vector2(0f, 150f));
            TMP_Text hintLabel = CreateStretchText(
                hintPanel.transform,
                HudTextId.Hint,
                "왼쪽 조이스틱으로 조준하고 오른쪽 공격 버튼을 누르세요.",
                29f);
            hintPanel.GetComponent<Image>().raycastTarget = false;
            hintLabel.raycastTarget = false;

            var commandControlsObject = new GameObject(
                "CommandControls",
                typeof(RectTransform));
            commandControlsObject.transform.SetParent(
                canvasObject.transform,
                false);
            StretchRect(
                commandControlsObject.GetComponent<RectTransform>());
            AimJoystickControl aimJoystick =
                CreateAimJoystick(commandControlsObject.transform);
            Button attackButton =
                CreateAttackButton(commandControlsObject.transform);

            var modalRootObject = new GameObject(
                "ModalRoot",
                typeof(RectTransform));
            modalRootObject.transform.SetParent(
                canvasObject.transform,
                false);
            StretchRect(
                modalRootObject.GetComponent<RectTransform>());

            Button settingsButton = CreateButtonVisual(
                HudButtonId.Settings.ToString(),
                "설정");
            settingsButton.transform.SetParent(
                canvasObject.transform,
                false);
            RectTransform settingsRect =
                settingsButton.GetComponent<RectTransform>();
            settingsRect.anchorMin = Vector2.one;
            settingsRect.anchorMax = Vector2.one;
            settingsRect.pivot = Vector2.one;
            settingsRect.anchoredPosition =
                new Vector2(-18f, -16f);
            settingsRect.sizeDelta = new Vector2(112f, 64f);

            var hudView =
                canvasObject.AddComponent<PrototypeHUDView>();
            hudView.Configure(
                timeLabel,
                hpLabel,
                hintLabel,
                experienceSlider,
                experienceLabel,
                settingsButton,
                attackButton,
                aimJoystick,
                modalRootObject.transform,
                cardSelectionPrefab,
                pausePrefab,
                gameOverPrefab,
                difficultySelectionPrefab);
            var presenter =
                canvasObject.AddComponent<PrototypeHUDPresenter>();
            presenter.Configure(hudView);

            return SaveTemporaryPrefab(
                canvasObject,
                PrototypeHudPrefabPath);
        }

        private static GameObject CreateCardSelectionPanelPrefab()
        {
            GameObject panel = CreatePanel(
                null,
                "CardSelectionPanel",
                new Color(0.04f, 0.02f, 0.09f, 0.94f),
                new Vector2(0.04f, 0.3f),
                new Vector2(0.96f, 0.7f),
                Vector2.zero,
                Vector2.zero);
            CreatePanelTitle(
                panel.transform,
                "레벨 업\n카드를 선택하세요",
                46f);
            CreateCardChoiceButtons(panel.transform);
            return SaveTemporaryPrefab(
                panel,
                CardSelectionPanelPrefabPath);
        }

        private static GameObject CreatePauseDetailsPanelPrefab()
        {
            GameObject panel = CreatePanel(
                null,
                "PauseDetailsPanel",
                new Color(0.025f, 0.035f, 0.045f, 0.96f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            TMP_Text label = CreateTextObject(
                panel.transform,
                "PauseDetails",
                "일시 정지",
                31f);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(70f, 410f);
            labelRect.offsetMax = new Vector2(-70f, -70f);
            label.alignment = TextAlignmentOptions.TopLeft;
            CreateControlPadToggle(panel.transform);
            CreateAutoAttackToggle(panel.transform);
            CreateControlSettingsButton(panel.transform);
            CreateControlSettingsPanel(panel.transform);
            return SaveTemporaryPrefab(
                panel,
                PauseDetailsPanelPrefabPath);
        }

        private static Toggle CreateControlPadToggle(
            Transform parent)
        {
            var toggleObject = new GameObject(
                "ControlPadToggle",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Toggle));
            toggleObject.transform.SetParent(parent, false);

            RectTransform rect =
                toggleObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 72f);
            rect.sizeDelta = new Vector2(480f, 86f);

            Image background = toggleObject.GetComponent<Image>();
            background.color =
                new Color(0.08f, 0.24f, 0.31f, 0.96f);
            background.raycastTarget = true;

            var checkmarkObject = new GameObject(
                "Checkmark",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            checkmarkObject.transform.SetParent(
                toggleObject.transform,
                false);
            RectTransform checkmarkRect =
                checkmarkObject.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0f, 0.5f);
            checkmarkRect.anchorMax = new Vector2(0f, 0.5f);
            checkmarkRect.pivot = new Vector2(0.5f, 0.5f);
            checkmarkRect.anchoredPosition = new Vector2(48f, 0f);
            checkmarkRect.sizeDelta = new Vector2(46f, 46f);
            Image checkmark =
                checkmarkObject.GetComponent<Image>();
            checkmark.sprite = LoadBuiltinCircleSprite();
            checkmark.color =
                new Color(0.25f, 0.88f, 1f, 1f);
            checkmark.raycastTarget = false;

            TMP_Text label = CreateTextObject(
                toggleObject.transform,
                "Label",
                "조작 패드 표시",
                30f);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(90f, 8f);
            labelRect.offsetMax = new Vector2(-18f, -8f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;

            Toggle toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = checkmark;
            toggle.isOn = true;
            return toggle;
        }

        private static Toggle CreateAutoAttackToggle(
            Transform parent)
        {
            var toggleObject = new GameObject(
                "AutoAttackToggle",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Toggle));
            toggleObject.transform.SetParent(parent, false);

            RectTransform rect =
                toggleObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 170f);
            rect.sizeDelta = new Vector2(480f, 86f);

            Image background = toggleObject.GetComponent<Image>();
            background.color =
                new Color(0.08f, 0.24f, 0.31f, 0.96f);
            background.raycastTarget = true;

            var checkmarkObject = new GameObject(
                "Checkmark",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            checkmarkObject.transform.SetParent(
                toggleObject.transform,
                false);
            RectTransform checkmarkRect =
                checkmarkObject.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0f, 0.5f);
            checkmarkRect.anchorMax = new Vector2(0f, 0.5f);
            checkmarkRect.pivot = new Vector2(0.5f, 0.5f);
            checkmarkRect.anchoredPosition = new Vector2(48f, 0f);
            checkmarkRect.sizeDelta = new Vector2(46f, 46f);
            Image checkmark = checkmarkObject.GetComponent<Image>();
            checkmark.sprite = LoadBuiltinCircleSprite();
            checkmark.color = new Color(1f, 0.36f, 0.22f, 1f);
            checkmark.raycastTarget = false;

            TMP_Text label = CreateTextObject(
                toggleObject.transform,
                "Label",
                "자동 공격",
                30f);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(90f, 8f);
            labelRect.offsetMax = new Vector2(-18f, -8f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;

            Toggle toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = checkmark;
            toggle.isOn = false;
            return toggle;
        }

        private static void CreateControlSettingsButton(Transform parent)
        {
            Button button = CreateButtonVisual(
                "ControlSettingsButton",
                "조작");
            button.transform.SetParent(parent, false);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 268f);
            rect.sizeDelta = new Vector2(480f, 86f);
        }

        private static void CreateControlSettingsPanel(Transform parent)
        {
            GameObject panel = CreatePanel(
                parent,
                "ControlSettingsPanel",
                new Color(0.025f, 0.035f, 0.045f, 0.45f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            TMP_Text title = CreateTextObject(
                panel.transform,
                "ControlSettingsTitle",
                "조작 패널 설정",
                42f);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -55f);
            titleRect.sizeDelta = new Vector2(0f, 90f);
            title.alignment = TextAlignmentOptions.Center;
            title.fontStyle = FontStyles.Bold;

            CreateControlSettingsSlider(
                panel.transform,
                "JoystickSizeSlider",
                "왼쪽 조이스틱 크기",
                MobileControlSettingsStore.MinimumScale,
                MobileControlSettingsStore.MaximumScale,
                1f,
                -245f);
            CreateControlSettingsSlider(
                panel.transform,
                "JoystickHorizontalSlider",
                "왼쪽 조이스틱 가로 위치",
                0f,
                1f,
                MobileControlSettings.Default.joystickPosition.x,
                -445f);
            CreateControlSettingsSlider(
                panel.transform,
                "JoystickVerticalSlider",
                "왼쪽 조이스틱 세로 위치",
                0f,
                1f,
                MobileControlSettings.Default.joystickPosition.y,
                -645f);
            CreateControlSettingsSlider(
                panel.transform,
                "AttackSizeSlider",
                "오른쪽 공격 버튼 크기",
                MobileControlSettingsStore.MinimumScale,
                MobileControlSettingsStore.MaximumScale,
                1f,
                -895f);
            CreateControlSettingsSlider(
                panel.transform,
                "AttackHorizontalSlider",
                "오른쪽 공격 버튼 가로 위치",
                0f,
                1f,
                MobileControlSettings.Default.attackPosition.x,
                -1095f);
            CreateControlSettingsSlider(
                panel.transform,
                "AttackVerticalSlider",
                "오른쪽 공격 버튼 세로 위치",
                0f,
                1f,
                MobileControlSettings.Default.attackPosition.y,
                -1295f);

            CreateControlSettingsActionButton(
                panel.transform,
                "ControlDefaultsButton",
                "기본값",
                -280f);
            CreateControlSettingsActionButton(
                panel.transform,
                "ControlCancelButton",
                "취소",
                0f);
            CreateControlSettingsActionButton(
                panel.transform,
                "ControlApplyButton",
                "적용",
                280f);
            panel.SetActive(false);
        }

        private static Slider CreateControlSettingsSlider(
            Transform parent,
            string objectName,
            string labelText,
            float minimum,
            float maximum,
            float value,
            float y)
        {
            var sliderObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Slider));
            sliderObject.transform.SetParent(parent, false);
            RectTransform rect =
                sliderObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(760f, 44f);

            Image background = sliderObject.GetComponent<Image>();
            background.color = new Color(0.08f, 0.18f, 0.22f, 1f);

            var handleObject = new GameObject(
                "Handle",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            handleObject.transform.SetParent(sliderObject.transform, false);
            RectTransform handleRect =
                handleObject.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0f, 0.5f);
            handleRect.anchorMax = new Vector2(0f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(58f, 58f);
            Image handleImage = handleObject.GetComponent<Image>();
            handleImage.sprite = LoadBuiltinCircleSprite();
            handleImage.color = new Color(0.25f, 0.88f, 1f, 1f);

            TMP_Text label = CreateTextObject(
                sliderObject.transform,
                "Label",
                labelText,
                28f);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 22f);
            labelRect.sizeDelta = new Vector2(620f, 54f);
            label.alignment = TextAlignmentOptions.MidlineLeft;

            TMP_Text valueLabel = CreateTextObject(
                sliderObject.transform,
                "Value",
                $"{Mathf.RoundToInt(value * 100f)}%",
                28f);
            RectTransform valueRect = valueLabel.rectTransform;
            valueRect.anchorMin = new Vector2(1f, 1f);
            valueRect.anchorMax = new Vector2(1f, 1f);
            valueRect.pivot = new Vector2(1f, 0f);
            valueRect.anchoredPosition = new Vector2(0f, 22f);
            valueRect.sizeDelta = new Vector2(130f, 54f);
            valueLabel.alignment = TextAlignmentOptions.MidlineRight;

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = minimum;
            slider.maxValue = maximum;
            slider.value = value;
            slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            return slider;
        }

        private static void CreateControlSettingsActionButton(
            Transform parent,
            string objectName,
            string labelText,
            float x)
        {
            Button button = CreateButtonVisual(objectName, labelText);
            button.transform.SetParent(parent, false);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(x, 72f);
            rect.sizeDelta = new Vector2(230f, 86f);
        }

        private static GameObject CreateGameOverPanelPrefab()
        {
            GameObject panel = CreatePanel(
                null,
                "GameOverPanel",
                new Color(0.12f, 0.01f, 0.01f, 0.94f),
                new Vector2(0.12f, 0.38f),
                new Vector2(0.88f, 0.62f),
                Vector2.zero,
                Vector2.zero);
            TMP_Text title = CreateTextObject(
                panel.transform,
                "GameOverTitle",
                "게임 종료",
                48f);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 0.28f);
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(30f, 15f);
            titleRect.offsetMax = new Vector2(-30f, -20f);
            title.alignment = TextAlignmentOptions.Center;

            Button continueButton = CreateButtonVisual(
                HudButtonId.ContinueAd.ToString(),
                "이어하기");
            continueButton.transform.SetParent(panel.transform, false);
            RectTransform buttonRect =
                continueButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, 28f);
            buttonRect.sizeDelta = new Vector2(400f, 100f);
            return SaveTemporaryPrefab(
                panel,
                GameOverPanelPrefabPath);
        }

        private static GameObject CreateDifficultySelectionPanelPrefab()
        {
            GameObject panel = CreatePanel(
                null,
                "DifficultySelectionPanel",
                new Color(0.025f, 0.035f, 0.045f, 0.97f),
                new Vector2(0.08f, 0.24f),
                new Vector2(0.92f, 0.76f),
                Vector2.zero,
                Vector2.zero);
            TMP_Text title = CreateTextObject(
                panel.transform,
                "DifficultyTitle",
                "난이도 선택",
                52f);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -70f);
            titleRect.sizeDelta = new Vector2(0f, 90f);
            title.alignment = TextAlignmentOptions.Center;
            title.fontStyle = FontStyles.Bold;

            TMP_Text description = CreateTextObject(
                panel.transform,
                "DifficultyDescription",
                "난이도는 이번 게임의 적 수와 적 레벨에 적용됩니다.",
                28f);
            RectTransform descriptionRect = description.rectTransform;
            descriptionRect.anchorMin = new Vector2(0.08f, 0.58f);
            descriptionRect.anchorMax = new Vector2(0.92f, 0.76f);
            descriptionRect.offsetMin = Vector2.zero;
            descriptionRect.offsetMax = Vector2.zero;
            description.alignment = TextAlignmentOptions.Center;

            CreateDifficultyButton(
                panel.transform,
                HudButtonId.DifficultyEasy,
                "쉬움\n적 수 75% · 적 레벨 80%",
                90f,
                new Color(0.12f, 0.5f, 0.32f, 0.98f));
            CreateDifficultyButton(
                panel.transform,
                HudButtonId.DifficultyNormal,
                "보통\n현재 밸런스",
                -90f,
                new Color(0.18f, 0.38f, 0.68f, 0.98f));
            return SaveTemporaryPrefab(
                panel,
                DifficultySelectionPanelPrefabPath);
        }

        private static void CreateDifficultyButton(
            Transform parent,
            HudButtonId id,
            string labelText,
            float y,
            Color color)
        {
            Button button = CreateButtonVisual(id.ToString(), labelText);
            button.transform.SetParent(parent, false);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(620f, 138f);
            button.GetComponent<Image>().color = color;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            label.fontSize = 30f;
            label.fontStyle = FontStyles.Bold;
        }

        private static GameObject SaveTemporaryPrefab(
            GameObject root,
            string path)
        {
            GameObject prefab =
                PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>(
                    FindObjectsInactive.Include) != null)
            {
                return;
            }

            _ = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
        }

        private static void ConfigureTimeLabel(TMP_Text label)
        {
            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -8f);
            rect.sizeDelta = new Vector2(300f, 42f);
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
        }

        private static void ConfigureHpLabel(TMP_Text label)
        {
            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-18f, -102f);
            rect.sizeDelta = new Vector2(300f, 34f);
            label.alignment = TextAlignmentOptions.Right;
        }

        private static Slider CreateExperienceSlider(
            Transform parent,
            out TMP_Text experienceLabel)
        {
            var sliderObject = new GameObject(
                "ExperienceBar",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Slider));
            sliderObject.transform.SetParent(parent, false);
            RectTransform sliderRect =
                sliderObject.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0f, 1f);
            sliderRect.anchorMax = new Vector2(1f, 1f);
            sliderRect.offsetMin = new Vector2(0f, -98f);
            sliderRect.offsetMax = new Vector2(0f, -54f);
            sliderObject.GetComponent<Image>().color =
                new Color(0.08f, 0.11f, 0.12f, 0.98f);

            var fillObject = new GameObject(
                "Fill",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            fillObject.transform.SetParent(
                sliderObject.transform,
                false);
            RectTransform fillRect =
                fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            Image fillImage = fillObject.GetComponent<Image>();
            fillImage.color =
                new Color(0.15f, 0.82f, 0.72f, 1f);

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.interactable = false;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.fillRect = fillRect;
            slider.targetGraphic = fillImage;

            experienceLabel = CreateTextObject(
                sliderObject.transform,
                "ExperienceLabel",
                "다음 레벨까지 경험치 10",
                25f);
            StretchRect(experienceLabel.rectTransform);
            experienceLabel.fontStyle = FontStyles.Bold;
            experienceLabel.alignment =
                TextAlignmentOptions.Center;
            return slider;
        }

        private static void StretchRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameObject CreatePanel(
            Transform parent,
            string name,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            var panel = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private static TMP_Text CreateStretchText(
            Transform parent,
            HudTextId id,
            string value,
            float fontSize)
        {
            TMP_Text label = CreateTextObject(parent, id.ToString(), value, fontSize);
            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(25f, 15f);
            rect.offsetMax = new Vector2(-25f, -15f);
            label.alignment = TextAlignmentOptions.Center;
            return label;
        }

        private static TMP_Text CreateTextObject(
            Transform parent,
            string name,
            string value,
            float fontSize)
        {
            var textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TMP_Text label = textObject.GetComponent<TMP_Text>();
            label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                CharacterAssetBuilder.DefaultFontPath);
            label.text = value;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.raycastTarget = false;
            label.enableWordWrapping = true;
            return label;
        }

        private static void CreatePanelTitle(Transform parent, string value, float fontSize)
        {
            TMP_Text label = CreateTextObject(parent, "CardTitle", value, fontSize);
            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -35f);
            rect.sizeDelta = new Vector2(0f, 150f);
            label.alignment = TextAlignmentOptions.Center;
        }

        private static void CreateCardChoiceButtons(Transform parent)
        {
            GameObject prefab = ConfigureLevelUpCardPrefabAsset();
            CreateCardChoiceButton(
                parent,
                prefab,
                HudButtonId.CardChoice0,
                20f);
            CreateCardChoiceButton(
                parent,
                prefab,
                HudButtonId.CardChoice1,
                350f);
            CreateCardChoiceButton(
                parent,
                prefab,
                HudButtonId.CardChoice2,
                680f);
        }

        private static GameObject ConfigureLevelUpCardPrefabAsset()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                LevelUpCardPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Level-up card prefab not found: " +
                    LevelUpCardPrefabPath);
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(
                LevelUpCardPrefabPath);
            try
            {
                Image frame = contents.GetComponent<Image>();
                Button rootButton = contents.GetComponent<Button>();
                Transform inner = contents.transform.Find(
                    "LevelUpCard_In");
                Transform title = inner?.Find("Label");
                Transform skill = contents.transform.Find(
                    "Panel/Skill_Text");
                if (frame == null ||
                    rootButton == null ||
                    inner == null ||
                    title == null ||
                    skill == null)
                {
                    throw new InvalidOperationException(
                        "LevelUpCard prefab requires root Image/Button, " +
                        "LevelUpCard_In/Label, and Panel/Skill_Text.");
                }

                Image innerBackground = inner.GetComponent<Image>();
                TMP_Text titleText = title.GetComponent<TMP_Text>();
                TMP_Text skillText = skill.GetComponent<TMP_Text>();
                if (innerBackground == null ||
                    titleText == null ||
                    skillText == null)
                {
                    throw new InvalidOperationException(
                        "LevelUpCard visual references are incomplete.");
                }

                Transform rerollTransform =
                    contents.transform.Find("RerollButton");
                Button rerollButton;
                if (rerollTransform == null)
                {
                    rerollButton = CreateButtonVisual(
                        "RerollButton",
                        $"교체 {PrototypeGameSession.DefaultInitialCardRerolls}");
                    rerollButton.transform.SetParent(
                        contents.transform,
                        false);
                    rerollTransform = rerollButton.transform;
                }
                else
                {
                    rerollButton =
                        rerollTransform.GetComponent<Button>();
                }

                Image rerollImage =
                    rerollTransform.GetComponent<Image>();
                Transform rerollLabelTransform =
                    rerollTransform.Find("Label");
                TMP_Text rerollLabel =
                    rerollLabelTransform != null
                        ? rerollLabelTransform.GetComponent<TMP_Text>()
                        : null;
                if (rerollButton == null ||
                    rerollImage == null ||
                    rerollLabel == null)
                {
                    throw new InvalidOperationException(
                        "LevelUpCard RerollButton requires " +
                        "Button/Image and Label text.");
                }

                RectTransform rerollRect =
                    rerollTransform.GetComponent<RectTransform>();
                rerollRect.anchorMin = Vector2.one;
                rerollRect.anchorMax = Vector2.one;
                rerollRect.pivot = Vector2.one;
                rerollRect.anchoredPosition =
                    new Vector2(-10f, -10f);
                rerollRect.sizeDelta = new Vector2(96f, 58f);
                rerollButton.targetGraphic = rerollImage;
                rerollLabel.text =
                    $"교체 {PrototypeGameSession.DefaultInitialCardRerolls}";
                rerollLabel.enableAutoSizing = true;
                rerollLabel.fontSizeMin = 14f;
                rerollLabel.fontSizeMax = 22f;
                rerollTransform.SetAsLastSibling();

                Button innerButton = inner.GetComponent<Button>();
                if (innerButton != null)
                {
                    UnityEngine.Object.DestroyImmediate(innerButton);
                }

                foreach (Graphic graphic in
                         contents.GetComponentsInChildren<Graphic>(true))
                {
                    graphic.raycastTarget =
                        graphic == frame ||
                        graphic == rerollImage;
                }

                Outline outline =
                    contents.GetComponent<Outline>() ??
                    contents.AddComponent<Outline>();
                outline.effectDistance = new Vector2(3f, -3f);
                outline.useGraphicAlpha = true;

                Shadow glow = null;
                foreach (Shadow effect in
                         contents.GetComponents<Shadow>())
                {
                    if (effect.GetType() == typeof(Shadow))
                    {
                        glow = effect;
                        break;
                    }
                }

                glow ??= contents.AddComponent<Shadow>();
                glow.effectDistance = new Vector2(0f, -7f);
                glow.useGraphicAlpha = true;

                RectTransform rect =
                    contents.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(
                    LevelUpCardWidth,
                    LevelUpCardHeight);

                RectTransform descriptionPanel =
                    skill.parent.GetComponent<RectTransform>();
                descriptionPanel.anchoredPosition =
                    new Vector2(0f, -155f);
                descriptionPanel.sizeDelta =
                    new Vector2(0f, 190f);
                titleText.enableAutoSizing = true;
                titleText.fontSizeMin = 18f;
                titleText.fontSizeMax = 26f;
                skillText.enableAutoSizing = true;
                skillText.fontSizeMin = 14f;
                skillText.fontSizeMax = 20f;
                skillText.color = new Color(
                    0.16f,
                    0.18f,
                    0.21f,
                    1f);

                LevelUpCardView cardView =
                    contents.GetComponent<LevelUpCardView>() ??
                    contents.AddComponent<LevelUpCardView>();
                cardView.ConfigureReferences(
                    frame,
                    innerBackground,
                    titleText,
                    skillText,
                    outline,
                    glow,
                    rerollButton,
                    rerollLabel);
                rootButton.targetGraphic = frame;
                PrefabUtility.SaveAsPrefabAsset(
                    contents,
                    LevelUpCardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(
                LevelUpCardPrefabPath);
        }

        private static Button CreateCardChoiceButton(
            Transform parent,
            GameObject prefab,
            HudButtonId id,
            float x)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(
                prefab,
                parent);
            instance.name = id.ToString();
            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(x, -70f);
            return instance.GetComponent<Button>();
        }

        private static AimJoystickControl CreateAimJoystick(
            Transform parent)
        {
            var joystickObject = new GameObject(
                "AimJoystick",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(AimJoystickControl));
            joystickObject.transform.SetParent(parent, false);

            RectTransform joystickRect =
                joystickObject.GetComponent<RectTransform>();
            joystickRect.anchorMin = Vector2.zero;
            joystickRect.anchorMax = Vector2.zero;
            joystickRect.pivot = new Vector2(0.5f, 0.5f);
            joystickRect.anchoredPosition =
                new Vector2(178f, 315f);
            joystickRect.sizeDelta = new Vector2(280f, 280f);

            Image joystickImage =
                joystickObject.GetComponent<Image>();
            joystickImage.sprite = LoadBuiltinCircleSprite();
            joystickImage.color =
                new Color(0.04f, 0.16f, 0.22f, 0.62f);
            joystickImage.raycastTarget = true;

            var knobObject = new GameObject(
                "Knob",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            knobObject.transform.SetParent(
                joystickObject.transform,
                false);
            RectTransform knobRect =
                knobObject.GetComponent<RectTransform>();
            knobRect.anchorMin = new Vector2(0.5f, 0.5f);
            knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.pivot = new Vector2(0.5f, 0.5f);
            knobRect.anchoredPosition = Vector2.zero;
            knobRect.sizeDelta = new Vector2(104f, 104f);
            Image knobImage = knobObject.GetComponent<Image>();
            knobImage.sprite = LoadBuiltinCircleSprite();
            knobImage.color =
                new Color(0.25f, 0.88f, 1f, 0.88f);
            knobImage.raycastTarget = false;

            AimJoystickControl control =
                joystickObject.GetComponent<AimJoystickControl>();
            control.Configure(joystickRect, knobRect);
            return control;
        }

        private static Button CreateAttackButton(Transform parent)
        {
            Button attackButton = CreateButtonVisual(
                HudButtonId.Attack.ToString(),
                "공격");
            attackButton.transform.SetParent(parent, false);
            attackButton.gameObject.AddComponent<
                AttackCommandButton>();

            RectTransform rect =
                attackButton.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition =
                new Vector2(-168f, 315f);
            rect.sizeDelta = new Vector2(224f, 224f);

            Image image = attackButton.GetComponent<Image>();
            image.sprite = LoadBuiltinCircleSprite();
            image.color =
                new Color(0.88f, 0.22f, 0.12f, 0.92f);
            image.raycastTarget = true;

            TMP_Text label =
                attackButton.GetComponentInChildren<TMP_Text>();
            label.fontSize = 38f;
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;
            return attackButton;
        }

        private static Sprite LoadBuiltinCircleSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/Knob.psd");
        }

        private static Button CreateButtonVisual(
            string objectName,
            string labelText)
        {
            var buttonObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.12f, 0.42f, 0.62f, 0.96f);

            TMP_Text label = CreateTextObject(
                buttonObject.transform,
                "Label",
                labelText,
                26f);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 8f);
            labelRect.offsetMax = new Vector2(-8f, -8f);
            label.alignment = TextAlignmentOptions.Center;
            return buttonObject.GetComponent<Button>();
        }
    }
}
