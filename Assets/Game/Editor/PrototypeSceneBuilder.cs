using SimpleGame;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SimpleGameEditor
{
    public static class PrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/PrototypeScene.unity";

        [MenuItem("SimpleGame/Build Prototype Scene")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            Camera camera = CreateCamera();
            CreateMainLight();

            var systems = new GameObject("PrototypeSystems");
            MapBounds bounds = systems.AddComponent<MapBounds>();
            bounds.Configure(new Vector2(-5.4f, -9.2f), new Vector2(5.4f, 9.2f));

            PrototypeArenaVisual arena = systems.AddComponent<PrototypeArenaVisual>();
            arena.Configure(bounds);
            PrototypeEnemyFactory factory = systems.AddComponent<PrototypeEnemyFactory>();
            PrototypeGameSession session = systems.AddComponent<PrototypeGameSession>();

            var entities = new GameObject("Entities");
            Transform enemyRoot = new GameObject("Enemies").transform;
            enemyRoot.SetParent(entities.transform, false);

            PlayerRoot player = CreatePlayer(entities.transform);
            CastleRoot castle = CreateCastle(entities.transform);
            PrototypeHUDPresenter presenter = CreateHud();

            session.ConfigureScene(
                bounds,
                player,
                castle,
                factory,
                enemyRoot,
                camera,
                presenter);

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
            var playerObject = new GameObject("Player");
            playerObject.transform.SetParent(parent, false);
            playerObject.transform.position = new Vector3(0f, -2.8f, 0f);
            playerObject.AddComponent<HealthComponent>();
            playerObject.AddComponent<PlayerMovement>();
            playerObject.AddComponent<CriticalSystem>();
            playerObject.AddComponent<PlayerProgression>();
            playerObject.AddComponent<PlayerController>();
            return playerObject.AddComponent<PlayerRoot>();
        }

        private static CastleRoot CreateCastle(Transform parent)
        {
            var castleObject = new GameObject("Castle");
            castleObject.transform.SetParent(parent, false);
            castleObject.transform.position = Vector3.zero;
            castleObject.AddComponent<HealthComponent>();
            return castleObject.AddComponent<CastleRoot>();
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
            CreateText(topPanel.transform, HudTextId.CastleHp, "CASTLE HP 30/30", 28, 540f, -216f);

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
            CreateButton(buttonPanel.transform, HudButtonId.DamageCastle, "CASTLE -10", 225f);
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
