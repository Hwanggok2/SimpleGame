using System.IO;
using System.Linq;
using NUnit.Framework;
using SimpleGameEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SimpleGame.Tests
{
    public sealed class LobbyTests
    {
        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(
                LobbyDifficultySelectionStore.PreferencesKey);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(
                LobbyDifficultySelectionStore.PreferencesKey);
        }

        [Test]
        public void SelectionStore_FirstEntryHasNoDefault()
        {
            Assert.That(
                LobbyDifficultySelectionStore.TryLoad(out _),
                Is.False);
        }

        [Test]
        public void SelectionStore_RoundTripsPlayableDifficulty()
        {
            LobbyDifficultySelectionStore.Save(
                LobbyDifficultyId.Normal);

            Assert.That(
                LobbyDifficultySelectionStore.TryLoad(
                    out LobbyDifficultyId loaded),
                Is.True);
            Assert.That(loaded, Is.EqualTo(LobbyDifficultyId.Normal));
        }

        [Test]
        public void SelectionStore_IgnoresCorruptAndAcceptsHardValues()
        {
            PlayerPrefs.SetString(
                LobbyDifficultySelectionStore.PreferencesKey,
                "Broken");
            Assert.That(
                LobbyDifficultySelectionStore.TryLoad(
                    out LobbyDifficultyId hard),
                Is.True);
            Assert.That(hard, Is.EqualTo(LobbyDifficultyId.Hard));

            PlayerPrefs.SetString(
                LobbyDifficultySelectionStore.PreferencesKey,
                LobbyDifficultyId.Hard.ToString());
            Assert.That(
                LobbyDifficultySelectionStore.TryLoad(out _),
                Is.False);
        }

        [Test]
        public void GeneratedLobbyData_UsesImportedSpritesAndPlayableHard()
        {
            GameDataManifest manifest =
                AssetDatabase.LoadAssetAtPath<GameDataManifest>(
                    GameDataAssetBuilder.ManifestPath);

            Assert.That(manifest, Is.Not.Null);
            Assert.That(manifest.ImageData, Is.Not.Null);
            Assert.That(manifest.LobbyDifficulties, Is.Not.Null);
            Assert.That(
                manifest.ImageData.Definitions,
                Has.Count.EqualTo(6));
            Assert.That(
                manifest.ImageData.Definitions.All(value =>
                    value.Sprite != null),
                Is.True);
            Assert.That(
                manifest.LobbyDifficulties.TryGet(
                    LobbyDifficultyId.Hard,
                    out LobbyDifficultyDefinition hard),
                Is.True);
            Assert.That(hard.IsAvailable, Is.True);
            Assert.That(
                hard.TryGetRuntimeDifficulty(
                    out GameDifficulty runtimeDifficulty),
                Is.True);
            Assert.That(runtimeDifficulty, Is.EqualTo(GameDifficulty.Hard));
        }

        [Test]
        public void LobbyPrefab_FirstEntryIsUnselectedAndNavigationIsReady()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                LobbySceneBuilder.LobbyPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            GameObject instance =
                Object.Instantiate(prefab);
            try
            {
                Assert.That(
                    instance.GetComponent<CanvasScaler>().screenMatchMode,
                    Is.EqualTo(CanvasScaler.ScreenMatchMode.Expand));
                LobbyView view = instance.GetComponent<LobbyView>();
                view.Initialize();

                Assert.That(view.HasSelection, Is.False);
                Assert.That(view.CanEnter, Is.False);
                Assert.That(
                    instance.transform.Find("DifficultyPreview")
                        .gameObject.activeSelf,
                    Is.False);
                LobbyDifficultyOptionView[] options =
                    instance.GetComponentsInChildren<
                        LobbyDifficultyOptionView>(true);
                Assert.That(options, Has.Length.EqualTo(3));
                LobbyDifficultyOptionView hard = options.Single(value =>
                    value.DifficultyId == LobbyDifficultyId.Hard);
                Assert.That(hard.Button.interactable, Is.False);
                Assert.That(
                    instance.transform.Find("TraitsButton")
                        .GetComponent<Button>().interactable,
                    Is.False);
                Assert.That(
                    instance.transform.Find("CollectionButton")
                        .GetComponent<Button>().interactable,
                    Is.True);
                Assert.That(
                    instance.transform.Find("SettingsButton")
                        .GetComponent<Button>().interactable,
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void LobbyCodex_UsesEnemyAndSkillTabsOnly()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                LobbySceneBuilder.LobbyPrefabPath);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                LobbyView lobby = instance.GetComponent<LobbyView>();
                lobby.Initialize();
                LobbyCodexView codex = lobby.CodexView;
                Assert.That(codex, Is.Not.Null);
                Assert.That(codex.gameObject.activeSelf, Is.False);

                instance.transform.Find("CollectionButton")
                    .GetComponent<Button>().onClick.Invoke();
                Assert.That(codex.IsOpen, Is.True);
                Assert.That(
                    codex.CurrentTab,
                    Is.EqualTo(LobbyCodexTab.Enemy));
                Assert.That(codex.EnemyEntries.Count, Is.EqualTo(9));
                Assert.That(
                    codex.EnemyEntries.Count(value =>
                        value.Button.interactable),
                    Is.EqualTo(8));

                codex.EnemyEntries[0].Button.onClick.Invoke();
                Transform detail = codex.transform.Find(
                    "Window/DetailOverlay");
                Assert.That(detail, Is.Not.Null);
                Assert.That(detail.gameObject.activeSelf, Is.True);
                detail.GetComponent<Button>().onClick.Invoke();
                Assert.That(detail.gameObject.activeSelf, Is.False);

                Assert.That(
                    codex.transform.Find("Window/ControlsTab"),
                    Is.Null);
                Assert.That(
                    codex.transform.Find("Window/TraitsTab"),
                    Is.Null);
                Assert.That(
                    codex.transform.Find("Window/SkillTab"),
                    Is.Not.Null);

                codex.transform.Find("OutsideCloseArea")
                    .GetComponent<Button>().onClick.Invoke();
                Assert.That(codex.IsOpen, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void ControlSettingsPanel_IsSharedByBattleAndLobby()
        {
            GameObject shared = AssetDatabase.LoadAssetAtPath<GameObject>(
                PrototypeSceneBuilder.ControlSettingsPanelPrefabPath);
            GameObject pause = AssetDatabase.LoadAssetAtPath<GameObject>(
                PrototypeSceneBuilder.PauseDetailsPanelPrefabPath);
            GameObject settings = AssetDatabase.LoadAssetAtPath<GameObject>(
                LobbySceneBuilder.SettingsPrefabPath);
            Assert.That(shared, Is.Not.Null);
            Assert.That(pause, Is.Not.Null);
            Assert.That(settings, Is.Not.Null);

            string sharedGuid = AssetDatabase.AssetPathToGUID(
                PrototypeSceneBuilder.ControlSettingsPanelPrefabPath);
            string pauseYaml = File.ReadAllText(
                Path.GetFullPath(
                    PrototypeSceneBuilder.PauseDetailsPanelPrefabPath));
            string settingsYaml = File.ReadAllText(
                Path.GetFullPath(LobbySceneBuilder.SettingsPrefabPath));
            StringAssert.Contains(sharedGuid, pauseYaml);
            StringAssert.Contains(sharedGuid, settingsYaml);
        }

        [Test]
        public void EnsureControlSettingsPanelPrefab_DoesNotRewriteUserPrefab()
        {
            string pausePath = Path.GetFullPath(
                PrototypeSceneBuilder.PauseDetailsPanelPrefabPath);
            string before = File.ReadAllText(pausePath);

            GameObject shared =
                PrototypeSceneBuilder.EnsureControlSettingsPanelPrefab();

            Assert.That(shared, Is.Not.Null);
            Assert.That(File.ReadAllText(pausePath), Is.EqualTo(before));
        }

        [Test]
        public void LobbyPrefab_SelectsAndRestoresLastDifficulty()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                LobbySceneBuilder.LobbyPrefabPath);
            GameObject firstInstance = Object.Instantiate(prefab);
            try
            {
                LobbyView firstView =
                    firstInstance.GetComponent<LobbyView>();
                firstView.Initialize();
                firstView.SelectDifficulty(LobbyDifficultyId.Easy);
                Assert.That(firstView.HasSelection, Is.True);
                Assert.That(firstView.CanEnter, Is.True);
                Assert.That(
                    firstView.SelectedDifficulty,
                    Is.EqualTo(LobbyDifficultyId.Easy));
                Assert.That(
                    firstInstance.transform.Find("DifficultyPreview")
                        .gameObject.activeSelf,
                    Is.True);
                Image representativeImage = firstInstance.transform.Find(
                        "DifficultyPreview/RepresentativeImage")
                    .GetComponent<Image>();
                Image selectedDifficultyImage =
                    firstInstance.GetComponentsInChildren<Image>(true)
                        .Single(value =>
                            value.name == "SelectedDifficultyImage");
                Assert.That(representativeImage.enabled, Is.True);
                Assert.That(
                    representativeImage.sprite.name,
                    Is.EqualTo("LobbyDifficulty_Easy"));
                Assert.That(selectedDifficultyImage.enabled, Is.True);
                Assert.That(
                    selectedDifficultyImage.sprite.name,
                    Is.EqualTo("Easy_Text"));
                Assert.That(
                    selectedDifficultyImage.rectTransform.localScale,
                    Is.EqualTo(new Vector3(0.9f, 0.9f, 1f)));

                firstView.SelectDifficulty(LobbyDifficultyId.Easy);
                Assert.That(firstView.HasSelection, Is.False);
                Assert.That(firstView.CanEnter, Is.False);
                Assert.That(
                    firstInstance.transform.Find("DifficultyPreview")
                        .gameObject.activeSelf,
                    Is.False);
                Assert.That(
                    selectedDifficultyImage.rectTransform.localScale,
                    Is.EqualTo(Vector3.one));
            }
            finally
            {
                Object.DestroyImmediate(firstInstance);
            }

            GameObject restoredInstance = Object.Instantiate(prefab);
            try
            {
                LobbyView restoredView =
                    restoredInstance.GetComponent<LobbyView>();
                restoredView.Initialize();
                Assert.That(restoredView.HasSelection, Is.True);
                Assert.That(
                    restoredView.SelectedDifficulty,
                    Is.EqualTo(LobbyDifficultyId.Easy));

                restoredView.SelectDifficulty(LobbyDifficultyId.Hard);
                Assert.That(
                    restoredView.SelectedDifficulty,
                    Is.EqualTo(LobbyDifficultyId.Easy));
            }
            finally
            {
                Object.DestroyImmediate(restoredInstance);
            }
        }

        [Test]
        public void LobbySettings_UsesBattleStyleControlPageToggle()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                LobbySceneBuilder.LobbyPrefabPath);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                LobbyView lobby = instance.GetComponent<LobbyView>();
                lobby.Initialize();
                LobbySettingsView settings = lobby.SettingsView;
                Assert.That(settings, Is.Not.Null);
                Assert.That(settings.IsOpen, Is.False);

                instance.transform.Find("SettingsButton")
                    .GetComponent<Button>().onClick.Invoke();
                Assert.That(settings.IsOpen, Is.True);
                Assert.That(
                    settings.transform.Find("Window/SettingsPage")
                        .gameObject.activeSelf,
                    Is.True);

                Button controls = settings.transform.Find(
                        "Window/ControlSettingsButton")
                    .GetComponent<Button>();
                controls.onClick.Invoke();
                Assert.That(settings.IsEditingControlSettings, Is.True);
                Assert.That(
                    settings.transform.Find("Window/SettingsPage")
                        .gameObject.activeSelf,
                    Is.False);
                Assert.That(
                    settings.transform.Find("Window/ControlSettingsPage")
                        .gameObject.activeSelf,
                    Is.True);

                controls.onClick.Invoke();
                Assert.That(settings.IsEditingControlSettings, Is.False);
                Assert.That(
                    settings.transform.Find("Window/SettingsPage")
                        .gameObject.activeSelf,
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void LobbyBuilder_DoesNotRewriteExistingUiOrScene()
        {
            string[] paths =
            {
                LobbySceneBuilder.LobbyPrefabPath,
                LobbySceneBuilder.CodexPrefabPath,
                LobbySceneBuilder.SettingsPrefabPath,
                LobbySceneBuilder.LobbyScenePath
            };
            string[] before = paths
                .Select(path => File.ReadAllText(Path.GetFullPath(path)))
                .ToArray();

            LobbySceneBuilder.Build();

            for (int index = 0; index < paths.Length; index++)
            {
                Assert.That(
                    File.ReadAllText(Path.GetFullPath(paths[index])),
                    Is.EqualTo(before[index]),
                    paths[index]);
            }
        }

        [Test]
        public void LobbyScene_IsFirstBuildSceneAndUsesStaticPrefab()
        {
            EditorBuildSettingsScene[] settings =
                EditorBuildSettings.scenes;
            Assert.That(settings, Has.Length.GreaterThanOrEqualTo(2));
            Assert.That(
                settings[0].path,
                Is.EqualTo(LobbySceneBuilder.LobbyScenePath));
            Assert.That(settings[0].enabled, Is.True);
            Assert.That(
                settings[1].path,
                Is.EqualTo(PrototypeSceneBuilder.BattleScenePath));
            Assert.That(settings[1].enabled, Is.True);

            Scene scene = EditorSceneManager.OpenScene(
                LobbySceneBuilder.LobbyScenePath,
                OpenSceneMode.Additive);
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                Assert.That(
                    roots.Any(value => value.name == "Main Camera"),
                    Is.True);
                GameObject mainCamera = roots.Single(value =>
                    value.name == "Main Camera");
                Assert.That(
                    mainCamera.GetComponent<AudioListener>(),
                    Is.Not.Null);
                Assert.That(
                    roots.Any(value => value.name == "EventSystem"),
                    Is.True);
                GameObject lobby = roots.Single(value =>
                    value.name == "LobbyScreen");
                Assert.That(
                    PrefabUtility.IsPartOfPrefabInstance(lobby),
                    Is.True);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            string sceneYaml = File.ReadAllText(
                Path.GetFullPath(LobbySceneBuilder.LobbyScenePath));
            string prefabGuid = AssetDatabase.AssetPathToGUID(
                LobbySceneBuilder.LobbyPrefabPath);
            StringAssert.Contains($"guid: {prefabGuid}", sceneYaml);
        }

        [Test]
        public void LobbyMusic_IsStaticLoopingAndStreamed()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                LobbySceneBuilder.LobbyPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            Transform music = prefab.transform.Find(
                LobbySceneBuilder.LobbyMusicObjectName);
            Assert.That(music, Is.Not.Null);
            AudioSource source = music.GetComponent<AudioSource>();
            Assert.That(source, Is.Not.Null);
            Assert.That(source.clip, Is.Not.Null);
            Assert.That(
                AssetDatabase.GetAssetPath(source.clip),
                Is.EqualTo(LobbySceneBuilder.LobbyMusicClipPath));
            Assert.That(source.playOnAwake, Is.True);
            Assert.That(source.loop, Is.True);
            Assert.That(source.spatialBlend, Is.EqualTo(0f));

            AudioImporter importer = AssetImporter.GetAtPath(
                LobbySceneBuilder.LobbyMusicClipPath) as AudioImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(
                importer.defaultSampleSettings.loadType,
                Is.EqualTo(AudioClipLoadType.Streaming));
            Assert.That(importer.loadInBackground, Is.True);
            Assert.That(
                importer.defaultSampleSettings.preloadAudioData,
                Is.False);
        }

        [Test]
        public void LobbyImages_AreSingleSpritesWithoutCpuCopiesOrMipmaps()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { "Assets/Image" });
            string[] expectedNames =
            {
                "LobbyDifficulty_Easy",
                "LobbyDifficulty_Normal",
                "LobbyDifficulty_Hard",
                "Easy_Text",
                "Normal_Text",
                "Hard_Text"
            };
            string[] relevantGuids = guids
                .Where(guid => expectedNames.Contains(
                    Path.GetFileNameWithoutExtension(
                        AssetDatabase.GUIDToAssetPath(guid))))
                .ToArray();
            Assert.That(relevantGuids, Has.Length.EqualTo(6));
            foreach (string guid in relevantGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer =
                    AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(
                    importer.textureType,
                    Is.EqualTo(TextureImporterType.Sprite),
                    path);
                Assert.That(
                    importer.spriteImportMode,
                    Is.EqualTo(SpriteImportMode.Single),
                    path);
                Assert.That(importer.isReadable, Is.False, path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
            }
        }
    }
}
