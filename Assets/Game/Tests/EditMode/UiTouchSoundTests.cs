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
        public void BattleSettingsButton_PlaysTouchSoundWhenBound()
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
                FieldInfo sourceField = typeof(UiTouchSoundPlayer)
                    .GetField(
                        "source",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(sourceField, Is.Not.Null);
                sourceField.SetValue(
                    soundPlayer,
                    instance.AddComponent<AudioSource>());
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
                    Is.EqualTo(Time.frameCount));
            }
            finally
            {
                Object.DestroyImmediate(instance);
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
