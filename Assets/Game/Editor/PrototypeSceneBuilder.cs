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
        private const string ScenePath = "Assets/Scenes/PrototypeScene.unity";
        private const string WorldTilePath = "Assets/Game/World/Tiles";
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
            PrototypeEnemyFactory factory = systems.AddComponent<PrototypeEnemyFactory>();
            factory.ConfigureAssets(
                gameData.EnemyAssets,
                gameData.EnemyBalance);
            CombatFeedbackController combatFeedback =
                systems.AddComponent<CombatFeedbackController>();
            combatFeedback.Configure(
                camera.GetComponent<CameraShakeController>(),
                gameData.CombatFeedback);
            PrototypeGameSession session = systems.AddComponent<PrototypeGameSession>();
            EnemyWorldRecycler enemyRecycler =
                systems.AddComponent<EnemyWorldRecycler>();
            StageSpawnController stageSpawner =
                systems.AddComponent<StageSpawnController>();

            var entities = new GameObject("Entities");
            Transform enemyRoot = new GameObject("Enemies").transform;
            enemyRoot.SetParent(entities.transform, false);

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
            PrototypeHUDPresenter presenter = CreateHud();

            session.ConfigureScene(
                player,
                factory,
                enemyRoot,
                camera,
                combatFeedback,
                enemyRecycler,
                presenter);
            session.ConfigureData(gameData, stageSpawner);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = systems;
            Debug.Log($"Prototype scene created: {ScenePath}");
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
                6,
                points);
            CreateHorizontalSpawnGroup(
                root.transform,
                "BottomSpawn",
                "BOTTOM",
                -11f,
                -4.8f,
                4.8f,
                6,
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
            EnsureAssetFolder("Assets/Game/World");
            EnsureAssetFolder(WorldTilePath);
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
                    "Assets/Resources/PNG/" +
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

        private static void EnsureAssetFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int separator = path.LastIndexOf('/');
            string parent = path[..separator];
            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(
                parent,
                path[(separator + 1)..]);
        }

        private static PrototypeHUDPresenter CreateHud()
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

            var eventSystemObject = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));

            GameObject topPanel = CreatePanel(
                canvasObject.transform,
                "TopPanel",
                new Color(0.04f, 0.07f, 0.08f, 0.9f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -270f),
                Vector2.zero);

            CreateText(topPanel.transform, HudTextId.Score, "SCORE 0", 34, 20f, -48f);
            CreateText(topPanel.transform, HudTextId.Time, "TIME 0.0", 34, 20f, -92f);
            CreateText(topPanel.transform, HudTextId.PlayerLevel, "PLAYER Lv.1", 30, 20f, -136f);
            CreateText(topPanel.transform, HudTextId.CriticalChance, "CRIT 0%", 30, 20f, -176f);
            CreateText(topPanel.transform, HudTextId.PlayerHp, "PLAYER HP 10/10", 28, 20f, -216f);

            GameObject hintPanel = CreatePanel(
                canvasObject.transform,
                "HintPanel",
                new Color(0f, 0f, 0f, 0.72f),
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                Vector2.zero,
                new Vector2(0f, 150f));
            CreateStretchText(
                hintPanel.transform,
                HudTextId.Hint,
                "Tap the field to move. Tap an enemy to attack.",
                29);

            GameObject buttonPanel = CreatePanel(
                canvasObject.transform,
                "DebugButtons",
                new Color(0f, 0f, 0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(15f, 165f),
                new Vector2(-15f, 305f));
            CreateButton(buttonPanel.transform, HudButtonId.Pause, "PAUSE", 10f);
            CreateButton(buttonPanel.transform, HudButtonId.DamagePlayer, "PLAYER -10", 225f);
            CreateButton(buttonPanel.transform, HudButtonId.GrantXp, "PLAYER XP +5", 440f);
            CreateButton(buttonPanel.transform, HudButtonId.ContinueAd, "CONTINUE (AD TEST)", 655f, 390f);

            GameObject criticalPanel = CreatePanel(
                canvasObject.transform,
                "CriticalCardPanel",
                new Color(0.04f, 0.02f, 0.09f, 0.94f),
                new Vector2(0.12f, 0.34f),
                new Vector2(0.88f, 0.66f),
                Vector2.zero,
                Vector2.zero);
            CreatePanelTitle(criticalPanel.transform, "LEVEL UP\nSELECT A CARD", 46);
            CreateButton(
                criticalPanel.transform,
                HudButtonId.CriticalCard,
                "CRITICAL CHANCE +10%\n(REPEATABLE / MAX 70%)",
                120f,
                540f,
                -245f,
                140f);

            GameObject gameOverPanel = CreatePanel(
                canvasObject.transform,
                "GameOverPanel",
                new Color(0.12f, 0.01f, 0.01f, 0.94f),
                new Vector2(0.12f, 0.38f),
                new Vector2(0.88f, 0.62f),
                Vector2.zero,
                Vector2.zero);
            CreateStretchText(
                gameOverPanel.transform,
                HudTextId.GameOverTitle,
                "GAME OVER",
                48);

            var hudView = canvasObject.AddComponent<PrototypeHUDView>();
            hudView.Configure(
                canvasObject.transform,
                canvasObject.transform,
                criticalPanel,
                gameOverPanel);

            var presenter = canvasObject.AddComponent<PrototypeHUDPresenter>();
            presenter.Configure(hudView);
            return presenter;
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

        private static void CreateText(
            Transform parent,
            HudTextId id,
            string value,
            float fontSize,
            float x,
            float y)
        {
            TMP_Text label = CreateTextObject(parent, id.ToString(), value, fontSize);
            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(520f, 40f);
            label.alignment = TextAlignmentOptions.Left;
        }

        private static void CreateStretchText(
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

        private static Button CreateButton(
            Transform parent,
            HudButtonId id,
            string labelText,
            float x,
            float width = 200f,
            float y = 0f,
            float height = 110f)
        {
            var buttonObject = new GameObject(
                id.ToString(),
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);

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
