using NUnit.Framework;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame.Tests
{
    public sealed class UiTouchSoundTests
    {
        [Test]
        public void Eligibility_IncludesUiAndExcludesCombatControls()
        {
            GameObject buttonObject = CreateButton("Settings");
            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(buttonObject.transform, false);
            GameObject attackObject = CreateButton("Attack");
            attackObject.AddComponent<AttackCommandButton>();
            GameObject movementObject = CreateButton("Movement");
            movementObject.AddComponent<AimJoystickControl>();
            GameObject enemyObject = new("Enemy");

            try
            {
                Assert.That(
                    UiTouchSoundPlayer.ShouldPlayFor(labelObject),
                    Is.True);
                Assert.That(
                    UiTouchSoundPlayer.ShouldPlayFor(attackObject),
                    Is.False);
                Assert.That(
                    UiTouchSoundPlayer.ShouldPlayFor(movementObject),
                    Is.False);
                Assert.That(
                    UiTouchSoundPlayer.ShouldPlayFor(enemyObject),
                    Is.False);

                buttonObject.GetComponent<Button>().interactable = false;
                Assert.That(
                    UiTouchSoundPlayer.ShouldPlayFor(labelObject),
                    Is.True,
                    "Visible placeholder UI such as Traits still clicks.");
            }
            finally
            {
                Object.DestroyImmediate(buttonObject);
                Object.DestroyImmediate(attackObject);
                Object.DestroyImmediate(movementObject);
                Object.DestroyImmediate(enemyObject);
            }
        }

        [TestCase("Assets/Prefab/UI/Lobby/LobbyScreen.prefab")]
        [TestCase("Assets/Prefab/PrototypeHUD.prefab")]
        public void ScreenPrefab_HasTouchSoundClip(string prefabPath)
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null);

            UiTouchSoundPlayer player =
                prefab.GetComponent<UiTouchSoundPlayer>();
            Assert.That(player, Is.Not.Null);
            Assert.That(player.TouchClip, Is.Not.Null);
            Assert.That(
                AssetDatabase.GetAssetPath(player.TouchClip),
                Is.EqualTo("Assets/Music/Effect/Touch.mp3"));
        }

        [Test]
        public void BattleButtonBinding_DoesNotAddSecondTouchSoundPath()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefab/PrototypeHUD.prefab");
            GameObject instance = Object.Instantiate(prefab);

            try
            {
                PrototypeHUDView view =
                    instance.GetComponent<PrototypeHUDView>();
                UiTouchSoundPlayer soundPlayer =
                    instance.GetComponent<UiTouchSoundPlayer>();
                view.Initialize();
                view.Bind(HudButtonId.Settings, () => { });

                view.SettingsButton.onClick.Invoke();

                FieldInfo lastPlayedFrame = typeof(UiTouchSoundPlayer)
                    .GetField(
                        "lastPlayedFrame",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(lastPlayedFrame, Is.Not.Null);
                Assert.That(
                    (int)lastPlayedFrame.GetValue(soundPlayer),
                    Is.EqualTo(-1));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void AudioSettingsProfile_ExposesSceneMusicAndSoundEffects()
        {
            AudioSettingsProfile profile =
                ScriptableObject.CreateInstance<AudioSettingsProfile>();
            AudioClip lobbyMusic = AudioClip.Create(
                "LobbyMusic",
                1,
                1,
                44100,
                false);
            AudioClip battleMusic = AudioClip.Create(
                "BattleMusic",
                1,
                1,
                44100,
                false);
            AudioClip touch = AudioClip.Create(
                "Touch",
                1,
                1,
                44100,
                false);
            AudioClip attack = AudioClip.Create(
                "Attack",
                1,
                1,
                44100,
                false);
            AudioClip enemyDeath = AudioClip.Create(
                "EnemyDeath",
                1,
                1,
                44100,
                false);

            try
            {
                profile.Configure(
                    new[]
                    {
                        new AudioClipDefinition(
                            GameManager.LobbySceneName,
                            lobbyMusic,
                            0.5f),
                        new AudioClipDefinition(
                            GameManager.BattleSceneName,
                            battleMusic,
                            0.75f)
                    },
                    new[]
                    {
                        new AudioClipDefinition(
                            GameAudioIds.UiTouch,
                            touch,
                            0.4f),
                        new AudioClipDefinition(
                            GameAudioIds.PlayerAttack,
                            attack,
                            0.6f),
                        new AudioClipDefinition(
                            GameAudioIds.EnemyDeath("GoblinMelee"),
                            enemyDeath,
                            0.7f)
                    });

                Assert.That(
                    profile.TryGetBackgroundMusic(
                        GameManager.LobbySceneName,
                        out AudioClipDefinition lobby),
                    Is.True);
                Assert.That(lobby.Clip, Is.SameAs(lobbyMusic));
                Assert.That(lobby.Volume, Is.EqualTo(0.5f));
                Assert.That(
                    profile.TryGetBackgroundMusic(
                        GameManager.BattleSceneName,
                        out AudioClipDefinition battle),
                    Is.True);
                Assert.That(battle.Clip, Is.SameAs(battleMusic));
                Assert.That(
                    profile.TryGetSoundEffect(
                        GameAudioIds.UiTouch,
                        out AudioClipDefinition uiTouch),
                    Is.True);
                Assert.That(uiTouch.Clip, Is.SameAs(touch));
                Assert.That(
                    profile.TryGetSoundEffect(
                        GameAudioIds.PlayerAttack,
                        out AudioClipDefinition playerAttack),
                    Is.True);
                Assert.That(playerAttack.Clip, Is.SameAs(attack));
                Assert.That(
                    profile.TryGetSoundEffect(
                        GameAudioIds.EnemyDeath("GoblinMelee"),
                        out AudioClipDefinition death),
                    Is.True);
                Assert.That(death.Clip, Is.SameAs(enemyDeath));
                Assert.That(death.Volume, Is.EqualTo(0.7f));
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(lobbyMusic);
                Object.DestroyImmediate(battleMusic);
                Object.DestroyImmediate(touch);
                Object.DestroyImmediate(attack);
                Object.DestroyImmediate(enemyDeath);
            }
        }

        [TestCase("GoblinMelee", "EnemyDeath/GoblinMelee")]
        [TestCase("SkeletonBoss", "EnemyDeath/SkeletonBoss")]
        public void EnemyDeathSoundEffectId_IsUniquePerEnemy(
            string enemyId,
            string expected)
        {
            Assert.That(GameAudioIds.EnemyDeath(enemyId), Is.EqualTo(expected));
        }

        [Test]
        public void GameManager_OwnsAudioWithoutFramePolling()
        {
            var managerObject = new GameObject("TestManager");
            try
            {
                GameManager manager =
                    managerObject.AddComponent<GameManager>();

                Assert.That(GameManager.RootName, Is.EqualTo("Manager"));
                Assert.That(manager.UiAudioSource, Is.Not.Null);
                Assert.That(
                    manager.UiAudioSource.playOnAwake,
                    Is.False);
                Assert.That(
                    typeof(GameManager).GetMethod(
                        "Update",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic),
                    Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(managerObject);
            }
        }

        private static GameObject CreateButton(string name)
        {
            return new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
        }
    }
}
