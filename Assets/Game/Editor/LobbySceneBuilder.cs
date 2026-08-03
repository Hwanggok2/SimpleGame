using System;
using System.Collections.Generic;
using System.IO;
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
    public static class LobbySceneBuilder
    {
        public const string LobbyScenePath = "Assets/Scenes/Lobby.unity";
        public const string LobbyPrefabPath =
            "Assets/Prefab/UI/Lobby/LobbyScreen.prefab";
        public const string CodexPrefabPath =
            "Assets/Prefab/UI/Lobby/LobbyCodexPanel.prefab";
        public const string CodexEntryPrefabPath =
            "Assets/Prefab/UI/Lobby/LobbyCodexEntry.prefab";
        public const string CodexDetailPrefabPath =
            "Assets/Prefab/UI/Lobby/LobbyCodexDetail.prefab";
        public const string SettingsPrefabPath =
            "Assets/Prefab/UI/Lobby/LobbySettingsPanel.prefab";
        public const string LobbyMusicClipPath =
            "Assets/Music/harumachimusic-pastorale-idyllic-irish-harp-294840.mp3";
        public const string TouchEffectClipPath =
            "Assets/Music/Effect/Touch.mp3";
        public const string LobbyMusicObjectName = "LobbyBgm";

        private const string ImageRoot = "Assets/Image";
        private const string PrefabRoot = "Assets/Prefab/UI/Lobby";
        private const string KoreanFontPath =
            "Assets/Font/Pretendard-Regular SDF.asset";
        private static readonly Color BackgroundColor =
            new(0.96f, 0.96f, 0.96f, 1f);
        private static readonly Color PanelColor =
            new(0.91f, 0.91f, 0.91f, 1f);
        private static readonly Color BannerColor =
            new(0.20f, 0.21f, 0.19f, 0.92f);
        private static readonly Color PlaceholderButtonColor =
            new(0.70f, 0.64f, 0.96f, 1f);
        private static readonly Color OptionColor =
            new(0.43f, 0.43f, 0.43f, 1f);
        private static readonly Color SelectedColor =
            new(0.18f, 0.64f, 0.34f, 1f);
        private static readonly Color NormalTabColor =
            new(0.89f, 0.91f, 0.84f, 1f);

        [MenuItem("SimpleGame/Build Lobby Scene")]
        public static void Build()
        {
            EnsurePlaceholderImages();
            ConfigureLobbyMusicImporter();
            GameDataExcelImporter.ImportFromPath(
                GameDataExcelImporter.DefaultWorkbookPath);
            GameDataManifest manifest =
                AssetDatabase.LoadAssetAtPath<GameDataManifest>(
                    GameDataAssetBuilder.ManifestPath);
            if (manifest == null || !manifest.IsConfigured)
            {
                throw new InvalidOperationException(
                    "GameDataManifest is not configured after import.");
            }

            GameObject controlSettingsPrefab =
                PrototypeSceneBuilder.EnsureControlSettingsPanelPrefab();
            GameObject codexPrefab = EnsureCodexPrefabs(manifest);
            GameObject settingsPrefab = EnsureSettingsPrefab(
                controlSettingsPrefab);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(
                    LobbyPrefabPath) == null)
            {
                BuildLobbyPrefab(manifest, codexPrefab, settingsPrefab);
            }

            if (!File.Exists(Path.GetFullPath(LobbyScenePath)))
            {
                BuildLobbyScene();
            }
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"Lobby UI built as a prefab and saved to " +
                $"{LobbyScenePath}.");
        }

        [MenuItem("SimpleGame/Migrate Legacy Lobby UI")]
        public static void MigrateLegacyLobbyUi()
        {
            bool musicImporterChanged = ConfigureLobbyMusicImporter();
            GameDataManifest manifest =
                AssetDatabase.LoadAssetAtPath<GameDataManifest>(
                    GameDataAssetBuilder.ManifestPath);
            GameObject controlSettingsPrefab =
                PrototypeSceneBuilder.EnsureControlSettingsPanelPrefab();
            GameObject settingsPrefab = EnsureSettingsPrefab(
                controlSettingsPrefab);
            bool codexChanged = MigrateCodexPrefab(manifest);
            bool lobbyChanged = MigrateLobbyPrefab(settingsPrefab);
            if (musicImporterChanged || codexChanged || lobbyChanged)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log(
                musicImporterChanged || codexChanged || lobbyChanged
                    ? "Legacy Lobby UI migrated without rebuilding the " +
                      "Lobby scene."
                    : "Lobby UI already uses the current prefab structure.");
        }

        [MenuItem("SimpleGame/Migrate Lobby Music")]
        public static void MigrateLobbyMusic()
        {
            bool importerChanged = ConfigureLobbyMusicImporter();
            bool prefabChanged = MigrateLobbyMusicPrefab();
            bool sceneChanged = MigrateLobbyAudioListener();
            if (importerChanged || prefabChanged || sceneChanged)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log(
                importerChanged || prefabChanged || sceneChanged
                    ? "Lobby music added without rebuilding the Lobby UI."
                    : "Lobby music is already configured.");
        }

        private static bool MigrateLobbyAudioListener()
        {
            if (!File.Exists(Path.GetFullPath(LobbyScenePath)))
            {
                return false;
            }

            Scene scene = SceneManager.GetSceneByPath(LobbyScenePath);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            if (!wasLoaded)
            {
                scene = EditorSceneManager.OpenScene(
                    LobbyScenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                Camera mainCamera = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Camera candidate = root.GetComponent<Camera>();
                    if (candidate == null)
                    {
                        continue;
                    }

                    if (root.CompareTag("MainCamera") ||
                        root.name == "Main Camera")
                    {
                        mainCamera = candidate;
                        break;
                    }
                }

                if (mainCamera == null)
                {
                    throw new InvalidOperationException(
                        "Lobby Main Camera was not found.");
                }
                if (mainCamera.GetComponent<AudioListener>() != null)
                {
                    return false;
                }

                mainCamera.gameObject.AddComponent<AudioListener>();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                return true;
            }
            finally
            {
                if (!wasLoaded && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static bool MigrateLobbyMusicPrefab()
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(
                LobbyPrefabPath);
            if (asset == null)
            {
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(
                LobbyPrefabPath);
            try
            {
                if (!EnsureLobbyMusic(root.transform))
                {
                    return false;
                }

                PrefabUtility.SaveAsPrefabAsset(root, LobbyPrefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool MigrateCodexPrefab(GameDataManifest manifest)
        {
            GameObject asset =
                AssetDatabase.LoadAssetAtPath<GameObject>(CodexPrefabPath);
            if (asset == null)
            {
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(
                CodexPrefabPath);
            try
            {
                Transform window = root.transform.Find("Window");
                string[] obsoletePaths =
                {
                    "ControlsTab",
                    "TraitsTab",
                    "ControlsContent",
                    "TraitsContent"
                };
                bool changed = false;
                foreach (string path in obsoletePaths)
                {
                    Transform obsolete = window?.Find(path);
                    if (obsolete == null)
                    {
                        continue;
                    }

                    UnityEngine.Object.DestroyImmediate(
                        obsolete.gameObject);
                    changed = true;
                }

                if (!changed)
                {
                    return false;
                }

                Button enemyTab = FindComponent<Button>(
                    window,
                    "EnemyTab");
                Button skillTab = FindComponent<Button>(
                    window,
                    "SkillTab");
                SetAnchoredPosition(enemyTab, new Vector2(-95f, -44f));
                SetAnchoredPosition(skillTab, new Vector2(95f, -44f));
                GameObject enemyContent = window.Find("EnemyContent")
                    .gameObject;
                GameObject skillContent = window.Find("SkillContent")
                    .gameObject;
                LobbyCodexView view = root.GetComponent<LobbyCodexView>();
                view.Configure(
                    manifest,
                    root.transform.Find("OutsideCloseArea")
                        .GetComponent<Button>(),
                    FindComponent<Button>(window, "CloseButton"),
                    enemyTab,
                    skillTab,
                    FindComponent<TMP_Text>(
                        enemyTab.transform,
                        "Label"),
                    FindComponent<TMP_Text>(
                        skillTab.transform,
                        "Label"),
                    enemyContent,
                    skillContent,
                    enemyContent.GetComponentsInChildren<
                        LobbyCodexEntryView>(true),
                    FindComponent<Button>(
                        enemyContent.transform,
                        "PreviousPageButton"),
                    FindComponent<Button>(
                        enemyContent.transform,
                        "NextPageButton"),
                    FindComponent<TMP_Text>(
                        enemyContent.transform,
                        "PageLabel"),
                    skillContent.GetComponentsInChildren<
                        LobbyCodexEntryView>(true),
                    FindComponent<Button>(
                        skillContent.transform,
                        "PreviousPageButton"),
                    FindComponent<Button>(
                        skillContent.transform,
                        "NextPageButton"),
                    FindComponent<TMP_Text>(
                        skillContent.transform,
                        "PageLabel"),
                    FindComponent<LobbyCodexDetailView>(
                        window,
                        "DetailOverlay"));
                PrefabUtility.SaveAsPrefabAsset(root, CodexPrefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool MigrateLobbyPrefab(GameObject settingsPrefab)
        {
            GameObject asset =
                AssetDatabase.LoadAssetAtPath<GameObject>(LobbyPrefabPath);
            if (asset == null || settingsPrefab == null)
            {
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(
                LobbyPrefabPath);
            try
            {
                bool changed = EnsureLobbyMusic(root.transform);
                Transform settings = root.transform.Find(
                    "LobbySettingsPanel");
                if (settings == null)
                {
                    var instance =
                        (GameObject)PrefabUtility.InstantiatePrefab(
                            settingsPrefab,
                            root.transform);
                    instance.name = "LobbySettingsPanel";
                    instance.SetActive(false);
                    root.GetComponent<LobbyView>().SetSettingsView(
                        instance.GetComponent<LobbySettingsView>());
                    changed = true;
                }

                Transform preview = root.transform.Find(
                    "DifficultyPreview");
                Image selectedDifficultyImage = FindComponent<Image>(
                    preview,
                    "SelectedDifficultyImage");
                if (preview != null && selectedDifficultyImage == null)
                {
                    selectedDifficultyImage =
                        CreateSelectedDifficultyImage(preview);
                    changed = true;
                }

                var serializedView = new SerializedObject(
                    root.GetComponent<LobbyView>());
                SerializedProperty selectedImageProperty =
                    serializedView.FindProperty(
                        "selectedDifficultyImage");
                if (selectedImageProperty != null &&
                    selectedImageProperty.objectReferenceValue !=
                    selectedDifficultyImage)
                {
                    selectedImageProperty.objectReferenceValue =
                        selectedDifficultyImage;
                    serializedView.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }

                if (!changed)
                {
                    return false;
                }

                PrefabUtility.SaveAsPrefabAsset(root, LobbyPrefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static T FindComponent<T>(Transform root, string path)
            where T : Component
        {
            return root != null
                ? root.Find(path)?.GetComponent<T>()
                : null;
        }

        private static void SetAnchoredPosition(
            Component component,
            Vector2 position)
        {
            RectTransform rect = component != null
                ? component.GetComponent<RectTransform>()
                : null;
            if (rect != null)
            {
                rect.anchoredPosition = position;
            }
        }

        private static void EnsurePlaceholderImages()
        {
            EditorAssetUtility.EnsureFolder(ImageRoot);
            EditorAssetUtility.EnsureFolder(ImageRoot + "/Background");
            var sources = new Dictionary<string, string>
            {
                ["Background/LobbyDifficulty_Easy.png"] =
                    "Assets/Screenshots/PrototypeScene_PlayMode.png",
                ["Background/LobbyDifficulty_Normal.png"] =
                    "Assets/Screenshots/PrototypeScene_Final.png",
                ["Background/LobbyDifficulty_Hard.png"] =
                    "Assets/Screenshots/PrototypeScene_PlayMode-1.png"
            };

            foreach (KeyValuePair<string, string> pair in sources)
            {
                string destination = $"{ImageRoot}/{pair.Key}";
                if (AssetDatabase.LoadMainAssetAtPath(destination) == null)
                {
                    if (!AssetDatabase.CopyAsset(pair.Value, destination))
                    {
                        throw new InvalidOperationException(
                            $"Could not create placeholder image: " +
                            $"{destination}");
                    }
                }

                AssetDatabase.ImportAsset(
                    destination,
                    ImportAssetOptions.ForceUpdate);
            }
        }

        private static void BuildLobbyPrefab(
            GameDataManifest manifest,
            GameObject codexPrefab,
            GameObject settingsPrefab)
        {
            EditorAssetUtility.EnsureFolder(PrefabRoot);
            GameObject root = CreateLobbyScreen(
                manifest,
                codexPrefab,
                settingsPrefab);
            PrefabUtility.SaveAsPrefabAsset(root, LobbyPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static GameObject EnsureCodexPrefabs(
            GameDataManifest manifest)
        {
            GameObject existing =
                AssetDatabase.LoadAssetAtPath<GameObject>(CodexPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            EditorAssetUtility.EnsureFolder(PrefabRoot);
            GameObject entryPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    CodexEntryPrefabPath) ??
                BuildCodexEntryPrefab();
            GameObject detailPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    CodexDetailPrefabPath) ??
                BuildCodexDetailPrefab();
            GameObject codex = CreateCodexPanel(
                manifest,
                entryPrefab,
                detailPrefab);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
                codex,
                CodexPrefabPath);
            UnityEngine.Object.DestroyImmediate(codex);
            return prefab;
        }

        private static GameObject BuildCodexEntryPrefab()
        {
            var root = new GameObject(
                "LobbyCodexEntry",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(LobbyCodexEntryView));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(245f, 290f);
            Image background = root.GetComponent<Image>();
            background.color = new Color(0.20f, 0.22f, 0.23f, 1f);

            GameObject iconBackdrop = CreatePanel(
                root.transform,
                "IconBackdrop",
                new Color(0.14f, 0.15f, 0.16f, 1f),
                new Vector2(0.08f, 0.22f),
                new Vector2(0.92f, 0.94f),
                Vector2.zero,
                Vector2.zero);
            iconBackdrop.GetComponent<Image>().raycastTarget = false;
            iconBackdrop.AddComponent<RectMask2D>();
            GameObject iconObject = CreatePanel(
                iconBackdrop.transform,
                "Icon",
                Color.white,
                new Vector2(0.04f, 0.04f),
                new Vector2(0.96f, 0.96f),
                Vector2.zero,
                Vector2.zero);
            Image icon = iconObject.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.rectTransform.localScale = new Vector3(2.2f, 2.2f, 1f);

            TMP_Text label = CreateText(
                root.transform,
                "Name",
                string.Empty,
                28f,
                TextAlignmentOptions.Center,
                Color.white);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = new Vector2(1f, 0.23f);
            labelRect.offsetMin = new Vector2(8f, 4f);
            labelRect.offsetMax = new Vector2(-8f, -4f);
            label.fontStyle = FontStyles.Bold;

            root.GetComponent<LobbyCodexEntryView>().Configure(
                root.GetComponent<Button>(),
                icon,
                label);
            GameObject prefab =
                PrefabUtility.SaveAsPrefabAsset(
                    root,
                    CodexEntryPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildCodexDetailPrefab()
        {
            var root = new GameObject(
                "LobbyCodexDetail",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(LobbyCodexDetailView));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect, 0f, 0f);
            Image overlay = root.GetComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.58f);

            GameObject card = CreatePanel(
                root.transform,
                "DetailCard",
                new Color(0.12f, 0.13f, 0.14f, 0.98f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(620f, 930f));
            card.GetComponent<Image>().raycastTarget = false;

            GameObject iconBackdrop = CreatePanel(
                card.transform,
                "IconBackdrop",
                new Color(0.06f, 0.065f, 0.07f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -245f),
                new Vector2(350f, 350f));
            iconBackdrop.GetComponent<Image>().raycastTarget = false;
            iconBackdrop.AddComponent<RectMask2D>();
            GameObject iconObject = CreatePanel(
                iconBackdrop.transform,
                "Icon",
                Color.white,
                new Vector2(0.08f, 0.08f),
                new Vector2(0.92f, 0.92f),
                Vector2.zero,
                Vector2.zero);
            Image icon = iconObject.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.rectTransform.localScale = new Vector3(2.2f, 2.2f, 1f);

            TMP_Text nameLabel = CreateText(
                card.transform,
                "Name",
                string.Empty,
                38f,
                TextAlignmentOptions.Center,
                Color.white);
            ConfigureTopRect(
                nameLabel.rectTransform,
                new Vector2(0f, -475f),
                new Vector2(540f, 78f));
            nameLabel.fontStyle = FontStyles.Bold;

            TMP_Text descriptionLabel = CreateText(
                card.transform,
                "Description",
                string.Empty,
                27f,
                TextAlignmentOptions.TopLeft,
                new Color(0.95f, 0.94f, 0.86f, 1f));
            RectTransform descriptionRect =
                descriptionLabel.rectTransform;
            descriptionRect.anchorMin = new Vector2(0f, 0f);
            descriptionRect.anchorMax = new Vector2(1f, 0.43f);
            descriptionRect.offsetMin = new Vector2(46f, 42f);
            descriptionRect.offsetMax = new Vector2(-46f, -20f);

            root.GetComponent<LobbyCodexDetailView>().Configure(
                root.GetComponent<Button>(),
                icon,
                nameLabel,
                descriptionLabel);
            root.SetActive(false);
            GameObject prefab =
                PrefabUtility.SaveAsPrefabAsset(
                    root,
                    CodexDetailPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject EnsureSettingsPrefab(
            GameObject controlSettingsPrefab)
        {
            GameObject existing =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    SettingsPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            EditorAssetUtility.EnsureFolder(PrefabRoot);
            GameObject root = CreateSettingsPanel(controlSettingsPrefab);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
                root,
                SettingsPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateSettingsPanel(
            GameObject controlSettingsPrefab)
        {
            var root = new GameObject(
                "LobbySettingsPanel",
                typeof(RectTransform),
                typeof(LobbySettingsView));
            Stretch(root.GetComponent<RectTransform>(), 0f, 0f);

            GameObject outside = CreatePanel(
                root.transform,
                "OutsideCloseArea",
                new Color(0f, 0f, 0f, 0.42f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            Button outsideButton = outside.AddComponent<Button>();

            GameObject window = CreatePanel(
                root.transform,
                "Window",
                new Color(0.77f, 0.78f, 0.79f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -150f),
                new Vector2(920f, 1540f));
            RectTransform windowRect =
                window.GetComponent<RectTransform>();
            windowRect.pivot = new Vector2(0.5f, 1f);
            windowRect.anchoredPosition = new Vector2(0f, -150f);

            Button controlButton = CreateButton(
                window.transform,
                "ControlSettingsButton",
                "조작",
                new Vector2(0f, -44f),
                new Vector2(170f, 78f),
                NormalTabColor,
                out TMP_Text controlLabel);
            Button closeButton = CreateButton(
                window.transform,
                "CloseButton",
                "×",
                new Vector2(415f, -44f),
                new Vector2(72f, 72f),
                new Color(0.76f, 0.35f, 0.35f, 1f),
                out TMP_Text closeLabel);
            closeLabel.fontSize = 44f;

            GameObject settingsPage = CreateContentGroup(
                window.transform,
                "SettingsPage");
            TMP_Text title = CreateText(
                settingsPage.transform,
                "SettingsTitle",
                "설정",
                50f,
                TextAlignmentOptions.Center,
                new Color(0.08f, 0.08f, 0.08f, 1f));
            ConfigureTopRect(
                title.rectTransform,
                new Vector2(0f, -180f),
                new Vector2(780f, 86f));
            title.fontStyle = FontStyles.Bold;

            GameObject controlsContent = CreateContentGroup(
                window.transform,
                "ControlSettingsPage");
            LobbyControlSettingsView controlSettingsView =
                CreateLobbyControlSettings(
                    controlsContent,
                    controlSettingsPrefab);
            controlsContent.SetActive(false);

            root.GetComponent<LobbySettingsView>().Configure(
                outsideButton,
                closeButton,
                controlButton,
                title,
                controlLabel,
                settingsPage,
                controlsContent,
                controlSettingsView);
            root.SetActive(false);
            return root;
        }

        private static GameObject CreateCodexPanel(
            GameDataManifest manifest,
            GameObject entryPrefab,
            GameObject detailPrefab)
        {
            var root = new GameObject(
                "LobbyCodexPanel",
                typeof(RectTransform),
                typeof(LobbyCodexView));
            Stretch(root.GetComponent<RectTransform>(), 0f, 0f);

            GameObject outside = CreatePanel(
                root.transform,
                "OutsideCloseArea",
                new Color(0f, 0f, 0f, 0.42f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            Button outsideButton = outside.AddComponent<Button>();

            GameObject window = CreatePanel(
                root.transform,
                "Window",
                new Color(0.77f, 0.78f, 0.79f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -150f),
                new Vector2(920f, 1540f));
            RectTransform windowRect =
                window.GetComponent<RectTransform>();
            windowRect.pivot = new Vector2(0.5f, 1f);
            windowRect.anchoredPosition = new Vector2(0f, -150f);

            Button enemyTab = CreateButton(
                window.transform,
                "EnemyTab",
                "적",
                new Vector2(-95f, -44f),
                new Vector2(170f, 78f),
                SelectedColor,
                out TMP_Text enemyTabLabel);
            Button skillTab = CreateButton(
                window.transform,
                "SkillTab",
                "스킬",
                new Vector2(95f, -44f),
                new Vector2(170f, 78f),
                NormalTabColor,
                out TMP_Text skillTabLabel);
            Button closeButton = CreateButton(
                window.transform,
                "CloseButton",
                "×",
                new Vector2(415f, -44f),
                new Vector2(72f, 72f),
                new Color(0.76f, 0.35f, 0.35f, 1f),
                out TMP_Text closeLabel);
            closeLabel.fontSize = 44f;

            GameObject enemyContent = CreateContentGroup(
                window.transform,
                "EnemyContent");
            CreateCodexGrid(
                enemyContent.transform,
                entryPrefab,
                out LobbyCodexEntryView[] enemyEntries,
                out Button enemyPrevious,
                out Button enemyNext,
                out TMP_Text enemyPageLabel);

            GameObject skillContent = CreateContentGroup(
                window.transform,
                "SkillContent");
            CreateCodexGrid(
                skillContent.transform,
                entryPrefab,
                out LobbyCodexEntryView[] skillEntries,
                out Button skillPrevious,
                out Button skillNext,
                out TMP_Text skillPageLabel);
            skillContent.SetActive(false);

            var detailInstance =
                (GameObject)PrefabUtility.InstantiatePrefab(detailPrefab);
            detailInstance.name = "DetailOverlay";
            detailInstance.transform.SetParent(window.transform, false);
            Stretch(
                detailInstance.GetComponent<RectTransform>(),
                0f,
                0f);
            detailInstance.SetActive(false);
            LobbyCodexDetailView detailView =
                detailInstance.GetComponent<LobbyCodexDetailView>();

            root.GetComponent<LobbyCodexView>().Configure(
                manifest,
                outsideButton,
                closeButton,
                enemyTab,
                skillTab,
                enemyTabLabel,
                skillTabLabel,
                enemyContent,
                skillContent,
                enemyEntries,
                enemyPrevious,
                enemyNext,
                enemyPageLabel,
                skillEntries,
                skillPrevious,
                skillNext,
                skillPageLabel,
                detailView);
            root.SetActive(false);
            return root;
        }

        private static GameObject CreateContentGroup(
            Transform parent,
            string objectName)
        {
            var group = new GameObject(objectName, typeof(RectTransform));
            group.transform.SetParent(parent, false);
            RectTransform rect = group.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(18f, 18f);
            rect.offsetMax = new Vector2(-18f, -105f);
            return group;
        }

        private static void CreateCodexGrid(
            Transform parent,
            GameObject entryPrefab,
            out LobbyCodexEntryView[] entries,
            out Button previous,
            out Button next,
            out TMP_Text pageLabel)
        {
            entries = new LobbyCodexEntryView[9];
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    int index = row * 3 + column;
                    var instance =
                        (GameObject)PrefabUtility.InstantiatePrefab(
                            entryPrefab);
                    instance.name = $"Entry{index + 1}";
                    instance.transform.SetParent(parent, false);
                    RectTransform rect =
                        instance.GetComponent<RectTransform>();
                    ConfigureTopRect(
                        rect,
                        new Vector2(
                            (column - 1) * 270f,
                            -235f - row * 320f),
                        new Vector2(245f, 290f));
                    entries[index] =
                        instance.GetComponent<LobbyCodexEntryView>();
                }
            }

            previous = CreateButton(
                parent,
                "PreviousPageButton",
                "<",
                new Vector2(-415f, -555f),
                new Vector2(64f, 120f),
                new Color(0.42f, 0.44f, 0.46f, 0.96f),
                out TMP_Text previousLabel);
            previousLabel.fontSize = 35f;
            next = CreateButton(
                parent,
                "NextPageButton",
                ">",
                new Vector2(415f, -555f),
                new Vector2(64f, 120f),
                new Color(0.42f, 0.44f, 0.46f, 0.96f),
                out TMP_Text nextLabel);
            nextLabel.fontSize = 35f;
            pageLabel = CreateText(
                parent,
                "PageLabel",
                "1 / 1",
                26f,
                TextAlignmentOptions.Center,
                new Color(0.14f, 0.14f, 0.14f, 1f));
            ConfigureTopRect(
                pageLabel.rectTransform,
                new Vector2(0f, -1245f),
                new Vector2(240f, 54f));
        }

        private static LobbyControlSettingsView
            CreateLobbyControlSettings(
                GameObject controlsContent,
                GameObject controlSettingsPrefab)
        {
            var shared =
                (GameObject)PrefabUtility.InstantiatePrefab(
                    controlSettingsPrefab);
            shared.name = "ControlSettingsPanel";
            shared.transform.SetParent(controlsContent.transform, false);
            Stretch(shared.GetComponent<RectTransform>(), 0f, 0f);
            shared.SetActive(true);

            GameObject previewAreaObject = CreatePanel(
                controlsContent.transform,
                "ControlPreviewArea",
                new Color(0.06f, 0.08f, 0.09f, 0.30f),
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(22f, 190f),
                new Vector2(-22f, 500f));
            previewAreaObject.GetComponent<Image>().raycastTarget = false;
            RectTransform previewArea =
                previewAreaObject.GetComponent<RectTransform>();
            RectTransform joystick = CreateControlPreview(
                previewArea,
                "JoystickPreview",
                new Color(0.38f, 0.42f, 0.48f, 0.92f),
                "",
                154f,
                true);
            RectTransform attack = CreateControlPreview(
                previewArea,
                "AttackPreview",
                new Color(0.56f, 0.22f, 0.22f, 0.94f),
                "공격",
                176f,
                false);

            LobbyControlSettingsView view =
                controlsContent.AddComponent<LobbyControlSettingsView>();
            view.Configure(shared, previewArea, joystick, attack);
            return view;
        }

        private static RectTransform CreateControlPreview(
            RectTransform parent,
            string objectName,
            Color color,
            string labelText,
            float size,
            bool addInnerKnob)
        {
            var root = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            Image image = root.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/Knob.psd");
            image.color = color;
            image.raycastTarget = false;

            if (addInnerKnob)
            {
                GameObject knob = CreatePanel(
                    root.transform,
                    "Knob",
                    new Color(0.30f, 0.45f, 0.92f, 1f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(size * 0.48f, size * 0.48f));
                Image knobImage = knob.GetComponent<Image>();
                knobImage.sprite =
                    AssetDatabase.GetBuiltinExtraResource<Sprite>(
                        "UI/Skin/Knob.psd");
                knobImage.raycastTarget = false;
            }
            else
            {
                TMP_Text label = CreateText(
                    root.transform,
                    "Label",
                    labelText,
                    28f,
                    TextAlignmentOptions.Center,
                    Color.white);
                Stretch(label.rectTransform, 8f, 8f);
            }

            return rect;
        }

        private static GameObject CreateLobbyScreen(
            GameDataManifest manifest,
            GameObject codexPrefab,
            GameObject settingsPrefab)
        {
            var root = new GameObject(
                "LobbyScreen",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(LobbyView));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.Expand;

            root.AddComponent<UiTouchSoundPlayer>().Configure(
                AssetDatabase.LoadAssetAtPath<AudioClip>(
                    TouchEffectClipPath));

            EnsureLobbyMusic(root.transform);

            CreatePanel(
                root.transform,
                "Background",
                BackgroundColor,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            CreatePanel(
                root.transform,
                "HeaderLine",
                new Color(0.15f, 0.15f, 0.15f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(290f, -84f),
                new Vector2(620f, 4f));

            Button traitsButton = CreateButton(
                root.transform,
                "TraitsButton",
                "특성",
                new Vector2(-160f, -50f),
                new Vector2(120f, 64f),
                PlaceholderButtonColor,
                out TMP_Text traitsLabel);
            Button collectionButton = CreateButton(
                root.transform,
                "CollectionButton",
                "도감",
                new Vector2(0f, -50f),
                new Vector2(120f, 64f),
                PlaceholderButtonColor,
                out TMP_Text collectionLabel);
            Button settingsButton = CreateButton(
                root.transform,
                "SettingsButton",
                "설정",
                new Vector2(160f, -50f),
                new Vector2(120f, 64f),
                PlaceholderButtonColor,
                out TMP_Text settingsLabel);

            if (codexPrefab == null)
            {
                throw new InvalidOperationException(
                    $"Lobby codex prefab was not found: " +
                    CodexPrefabPath);
            }

            var codexInstance =
                (GameObject)PrefabUtility.InstantiatePrefab(codexPrefab);
            codexInstance.name = "LobbyCodexPanel";
            codexInstance.transform.SetParent(root.transform, false);
            codexInstance.SetActive(false);
            LobbyCodexView codexView =
                codexInstance.GetComponent<LobbyCodexView>();

            if (settingsPrefab == null)
            {
                throw new InvalidOperationException(
                    $"Lobby settings prefab was not found: " +
                    SettingsPrefabPath);
            }

            var settingsInstance =
                (GameObject)PrefabUtility.InstantiatePrefab(
                    settingsPrefab);
            settingsInstance.name = "LobbySettingsPanel";
            settingsInstance.transform.SetParent(root.transform, false);
            settingsInstance.SetActive(false);
            LobbySettingsView settingsView =
                settingsInstance.GetComponent<LobbySettingsView>();

            GameObject previewPanel = CreatePanel(
                root.transform,
                "DifficultyPreview",
                PanelColor,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -150f),
                new Vector2(960f, 760f));
            RectTransform previewRect =
                previewPanel.GetComponent<RectTransform>();
            previewRect.pivot = new Vector2(0.5f, 1f);
            previewPanel.SetActive(false);

            GameObject objectiveBanner = CreatePanel(
                previewPanel.transform,
                "ObjectiveBanner",
                BannerColor,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -128f),
                Vector2.zero);
            TMP_Text objectiveLabel = CreateText(
                objectiveBanner.transform,
                "ObjectiveLabel",
                "난이도를 선택해 주세요.",
                34f,
                TextAlignmentOptions.Center,
                Color.white);
            Stretch(objectiveLabel.rectTransform, 28f, 18f);

            GameObject imageObject = CreatePanel(
                previewPanel.transform,
                "RepresentativeImage",
                new Color(0.84f, 0.84f, 0.84f, 1f),
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(24f, 116f),
                new Vector2(-24f, -140f));
            Image previewImage = imageObject.GetComponent<Image>();
            previewImage.preserveAspect = true;
            previewImage.enabled = false;

            Image selectedDifficultyImage =
                CreateSelectedDifficultyImage(previewPanel.transform);

            GameObject effectBanner = CreatePanel(
                previewPanel.transform,
                "EffectBanner",
                BannerColor,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 116f));
            TMP_Text effectLabel = CreateText(
                effectBanner.transform,
                "EffectLabel",
                string.Empty,
                29f,
                TextAlignmentOptions.Center,
                new Color(0.92f, 0.93f, 0.56f, 1f));
            Stretch(effectLabel.rectTransform, 28f, 14f);

            GameObject selectionPanel = CreatePanel(
                root.transform,
                "DifficultySelection",
                new Color(0.98f, 0.98f, 0.97f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -960f),
                new Vector2(960f, 830f));
            RectTransform selectionRect =
                selectionPanel.GetComponent<RectTransform>();
            selectionRect.pivot = new Vector2(0.5f, 1f);

            TMP_Text difficultyTitle = CreateText(
                selectionPanel.transform,
                "DifficultyTitle",
                "난이도 선택",
                50f,
                TextAlignmentOptions.Center,
                new Color(0.08f, 0.08f, 0.08f, 1f));
            ConfigureTopRect(
                difficultyTitle.rectTransform,
                new Vector2(0f, -62f),
                new Vector2(820f, 86f));
            difficultyTitle.fontStyle = FontStyles.Bold;

            var options = new LobbyDifficultyOptionView[3];
            options[0] = CreateDifficultyOption(
                selectionPanel.transform,
                LobbyDifficultyId.Easy,
                new Vector2(0f, -190f));
            options[1] = CreateDifficultyOption(
                selectionPanel.transform,
                LobbyDifficultyId.Normal,
                new Vector2(0f, -355f));
            options[2] = CreateDifficultyOption(
                selectionPanel.transform,
                LobbyDifficultyId.Hard,
                new Vector2(0f, -520f));

            Button enterButton = CreateButton(
                selectionPanel.transform,
                "EnterButton",
                "입장하기",
                new Vector2(330f, -700f),
                new Vector2(230f, 94f),
                new Color(0.42f, 0.84f, 0.85f, 1f),
                out TMP_Text enterLabel);
            enterButton.interactable = false;
            enterLabel.fontSize = 38f;
            enterLabel.fontStyle = FontStyles.Bold;

            LobbyView lobbyView = root.GetComponent<LobbyView>();
            lobbyView.Configure(
                manifest,
                traitsButton,
                collectionButton,
                settingsButton,
                traitsLabel,
                collectionLabel,
                settingsLabel,
                objectiveLabel,
                previewImage,
                selectedDifficultyImage,
                effectLabel,
                difficultyTitle,
                options,
                enterButton,
                enterLabel,
                codexView);
            lobbyView.SetSettingsView(settingsView);
            return root;
        }

        private static bool ConfigureLobbyMusicImporter()
        {
            AudioImporter importer =
                AssetImporter.GetAtPath(LobbyMusicClipPath) as AudioImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    $"Lobby music clip was not found: " +
                    LobbyMusicClipPath);
            }

            AudioImporterSampleSettings settings =
                importer.defaultSampleSettings;
            bool changed =
                settings.loadType != AudioClipLoadType.Streaming ||
                !importer.loadInBackground ||
                settings.preloadAudioData;
            if (!changed)
            {
                return false;
            }

            settings.loadType = AudioClipLoadType.Streaming;
            settings.preloadAudioData = false;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = true;
            importer.SaveAndReimport();
            return true;
        }

        private static bool EnsureLobbyMusic(Transform root)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                LobbyMusicClipPath);
            if (clip == null)
            {
                throw new InvalidOperationException(
                    $"Lobby music clip was not found: " +
                    LobbyMusicClipPath);
            }

            Transform musicTransform = root.Find(LobbyMusicObjectName);
            bool changed = false;
            if (musicTransform == null)
            {
                var musicObject = new GameObject(
                    LobbyMusicObjectName,
                    typeof(AudioSource));
                musicObject.transform.SetParent(root, false);
                musicTransform = musicObject.transform;
                changed = true;
            }

            AudioSource source =
                musicTransform.GetComponent<AudioSource>();
            if (source == null)
            {
                source = musicTransform.gameObject.AddComponent<AudioSource>();
                changed = true;
            }

            if (source.clip != clip)
            {
                source.clip = clip;
                changed = true;
            }
            if (!source.playOnAwake)
            {
                source.playOnAwake = true;
                changed = true;
            }
            if (!source.loop)
            {
                source.loop = true;
                changed = true;
            }
            if (!Mathf.Approximately(source.spatialBlend, 0f))
            {
                source.spatialBlend = 0f;
                changed = true;
            }

            return changed;
        }

        private static Image CreateSelectedDifficultyImage(
            Transform parent)
        {
            GameObject imageObject = CreatePanel(
                parent,
                "SelectedDifficultyImage",
                Color.white,
                Vector2.one,
                Vector2.one,
                new Vector2(-184f, -244f),
                new Vector2(320f, 200f));
            Image image = imageObject.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        private static LobbyDifficultyOptionView CreateDifficultyOption(
            Transform parent,
            LobbyDifficultyId id,
            Vector2 anchoredPosition)
        {
            var optionObject = new GameObject(
                $"{id}Option",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(LobbyDifficultyOptionView));
            optionObject.transform.SetParent(parent, false);
            RectTransform rect =
                optionObject.GetComponent<RectTransform>();
            ConfigureTopRect(
                rect,
                anchoredPosition,
                new Vector2(780f, 138f));
            Image background = optionObject.GetComponent<Image>();
            background.color = OptionColor;
            Button button = optionObject.GetComponent<Button>();

            TMP_Text title = CreateText(
                optionObject.transform,
                "Title",
                id.ToString(),
                35f,
                TextAlignmentOptions.Center,
                Color.white);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 0.48f);
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(20f, 0f);
            titleRect.offsetMax = new Vector2(-20f, -5f);
            title.fontStyle = FontStyles.Bold;

            TMP_Text description = CreateText(
                optionObject.transform,
                "Description",
                string.Empty,
                23f,
                TextAlignmentOptions.Center,
                new Color(0.94f, 0.94f, 0.67f, 1f));
            RectTransform descriptionRect = description.rectTransform;
            descriptionRect.anchorMin = Vector2.zero;
            descriptionRect.anchorMax = new Vector2(1f, 0.52f);
            descriptionRect.offsetMin = new Vector2(20f, 8f);
            descriptionRect.offsetMax = new Vector2(-20f, 0f);

            LobbyDifficultyOptionView view =
                optionObject.GetComponent<LobbyDifficultyOptionView>();
            view.Configure(
                id,
                button,
                background,
                title,
                description);
            return view;
        }

        private static void BuildLobbyScene()
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;

            _ = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(LobbyPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Lobby prefab was not found: {LobbyPrefabPath}");
            }

            PrefabUtility.InstantiatePrefab(prefab, scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, LobbyScenePath);
        }

        private static void UpdateBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new(LobbyScenePath, true),
                new(PrototypeSceneBuilder.BattleScenePath, true)
            };
            foreach (EditorBuildSettingsScene existing in
                     EditorBuildSettings.scenes)
            {
                if (string.Equals(
                        existing.path,
                        LobbyScenePath,
                        StringComparison.Ordinal) ||
                    string.Equals(
                        existing.path,
                        PrototypeSceneBuilder.BattleScenePath,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                scenes.Add(existing);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static GameObject CreatePanel(
            Transform parent,
            string objectName,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            var panel = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            if (anchorMin == anchorMax)
            {
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = offsetMin;
                rect.sizeDelta = offsetMax;
            }
            else
            {
                rect.offsetMin = offsetMin;
                rect.offsetMax = offsetMax;
            }
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private static Button CreateButton(
            Transform parent,
            string objectName,
            string labelText,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color,
            out TMP_Text label)
        {
            var buttonObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect =
                buttonObject.GetComponent<RectTransform>();
            ConfigureTopRect(rect, anchoredPosition, size);
            buttonObject.GetComponent<Image>().color = color;
            label = CreateText(
                buttonObject.transform,
                "Label",
                labelText,
                31f,
                TextAlignmentOptions.Center,
                new Color(0.07f, 0.07f, 0.07f, 1f));
            Stretch(label.rectTransform, 8f, 6f);
            return buttonObject.GetComponent<Button>();
        }

        private static TMP_Text CreateText(
            Transform parent,
            string objectName,
            string value,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color)
        {
            var textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text =
                textObject.GetComponent<TextMeshProUGUI>();
            TMP_FontAsset font =
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    KoreanFontPath);
            if (font == null)
            {
                throw new InvalidOperationException(
                    $"Korean TMP font was not found: {KoreanFontPath}");
            }

            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        private static void ConfigureTopRect(
            RectTransform rect,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void Stretch(
            RectTransform rect,
            float horizontalPadding,
            float verticalPadding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(
                horizontalPadding,
                verticalPadding);
            rect.offsetMax = new Vector2(
                -horizontalPadding,
                -verticalPadding);
        }
    }
}
