using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SimpleGame.Tests
{
    public sealed class MobileAimControlsTests
    {
        [TestCase(0f, 0f, 100f, 0f, 0f)]
        [TestCase(50f, 0f, 100f, 0.5f, 0f)]
        [TestCase(30f, 40f, 100f, 0.3f, 0.4f)]
        [TestCase(200f, 0f, 100f, 1f, 0f)]
        [TestCase(0f, -200f, 100f, 0f, -1f)]
        public void JoystickOffset_NormalizesAndClamps(
            float x,
            float y,
            float radius,
            float expectedX,
            float expectedY)
        {
            Vector2 normalized =
                AimJoystickControl.NormalizePadOffset(
                    new Vector2(x, y),
                    radius);

            Assert.That(normalized.x, Is.EqualTo(expectedX).Within(0.001f));
            Assert.That(normalized.y, Is.EqualTo(expectedY).Within(0.001f));
        }

        [Test]
        public void JoystickOffset_ZeroRadiusReturnsNeutral()
        {
            Assert.That(
                AimJoystickControl.NormalizePadOffset(
                    new Vector2(20f, 10f),
                    0f),
                Is.EqualTo(Vector2.zero));
        }

        [TestCase(0f, 0f)]
        [TestCase(0.25f, 2f)]
        [TestCase(0.5f, 4f)]
        [TestCase(1f, 8f)]
        [TestCase(2f, 8f)]
        public void AimPoint_LengthTracksInputMagnitude(
            float inputMagnitude,
            float expectedDistance)
        {
            Vector2 origin = new(3f, -2f);
            Vector2 point = PlayerController.CalculateAimPoint(
                origin,
                Vector2.right * inputMagnitude,
                8f);

            Assert.That(
                Vector2.Distance(origin, point),
                Is.EqualTo(expectedDistance).Within(0.001f));
        }

        [Test]
        public void MaximumAimDistance_StopsAtVisibleViewportEdge()
        {
            Vector2 player = Vector2.zero;
            Vector2 camera = Vector2.zero;
            Vector2 halfExtents = new(5f, 10f);

            Assert.That(
                PlayerController.CalculateMaximumAimDistance(
                    player,
                    camera,
                    halfExtents,
                    Vector2.right),
                Is.EqualTo(4.5f).Within(0.001f));
            Assert.That(
                PlayerController.CalculateMaximumAimDistance(
                    player,
                    camera,
                    halfExtents,
                    Vector2.up),
                Is.EqualTo(9.5f).Within(0.001f));
            Assert.That(
                PlayerController.CalculateMaximumAimDistance(
                    new Vector2(1f, 0f),
                    camera,
                    halfExtents,
                    Vector2.right),
                Is.EqualTo(3.5f).Within(0.001f));
        }

        [Test]
        public void NeutralAim_DoesNotCreateAttackCommand()
        {
            Assert.That(
                PlayerController.HasCommandAim(Vector2.zero),
                Is.False);
            Assert.That(
                PlayerController.HasCommandAim(
                    Vector2.right *
                    PlayerController.MinimumCommandAimMagnitude),
                Is.True);
        }

        [Test]
        public void AttackButton_DispatchesOnPointerDownWithoutDuplicates()
        {
            var buttonObject = new GameObject("Attack");
            try
            {
                AttackCommandButton button =
                    buttonObject.AddComponent<
                        AttackCommandButton>();
                int firstCallbackCount = 0;
                int replacementCallbackCount = 0;
                button.Bind(() => firstCallbackCount++);
                button.Bind(() => replacementCallbackCount++);

                button.OnPointerDown(null);

                Assert.That(firstCallbackCount, Is.Zero);
                Assert.That(replacementCallbackCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(buttonObject);
            }
        }

        [Test]
        public void Joystick_ReinitializesAfterPlayerIsDestroyed()
        {
            var playerObject = new GameObject("Player");
            var joystickObject = new GameObject("AimJoystick");
            try
            {
                PlayerRoot player =
                    playerObject.AddComponent<PlayerRoot>();
                var serializedPlayer = new SerializedObject(player);
                serializedPlayer.FindProperty("controller")
                    .objectReferenceValue =
                    playerObject.GetComponent<PlayerController>();
                serializedPlayer.ApplyModifiedPropertiesWithoutUndo();
                AimJoystickControl joystick =
                    joystickObject.AddComponent<AimJoystickControl>();
                joystick.Initialize(player);
                Object.DestroyImmediate(playerObject);

                Assert.DoesNotThrow(() => joystick.Initialize(null));
            }
            finally
            {
                if (playerObject != null)
                {
                    Object.DestroyImmediate(playerObject);
                }

                Object.DestroyImmediate(joystickObject);
            }
        }

        [Test]
        public void UiPress_IsBlockedByImmediateGraphicRaycast()
        {
            var eventSystemObject = new GameObject(
                "EventSystem",
                typeof(EventSystem));
            var canvasObject = new GameObject(
                "Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(ImmediateGraphicRaycaster));
            var graphicObject = new GameObject(
                "Graphic",
                typeof(RectTransform),
                typeof(Image));
            ImmediateGraphicRaycaster raycaster = null;
            try
            {
                EventSystem eventSystem =
                    eventSystemObject.GetComponent<EventSystem>();
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                raycaster =
                    canvasObject.GetComponent<
                        ImmediateGraphicRaycaster>();
                raycaster.Target = graphicObject;
                List<BaseRaycaster> registeredRaycasters =
                    RaycasterManager.GetRaycasters();
                if (!registeredRaycasters.Contains(raycaster))
                {
                    registeredRaycasters.Add(raycaster);
                }

                var results = new List<RaycastResult>();

                Assert.That(
                    PlayerController.IsScreenPointOverUi(
                        eventSystem,
                        new Vector2(320f, 240f),
                        42,
                        results),
                    Is.True);
                Assert.That(results, Is.Empty);
            }
            finally
            {
                if (raycaster != null)
                {
                    RaycasterManager.GetRaycasters().Remove(raycaster);
                }

                Object.DestroyImmediate(graphicObject);
                Object.DestroyImmediate(canvasObject);
                Object.DestroyImmediate(eventSystemObject);
            }
        }

        [TestCase("OnApplicationFocus", false)]
        [TestCase("OnApplicationPause", true)]
        public void Joystick_ReleasesOwnedPointerWhenAppIsInterrupted(
            string message,
            bool state)
        {
            var joystickObject = new GameObject("AimJoystick");
            try
            {
                AimJoystickControl joystick =
                    joystickObject.AddComponent<AimJoystickControl>();
                FieldInfo activePointerField =
                    typeof(AimJoystickControl).GetField(
                        "activePointerId",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);
                Assert.That(activePointerField, Is.Not.Null);
                activePointerField.SetValue(joystick, 17);
                Assert.That(joystick.IsHeld, Is.True);

                MethodInfo interruptionHandler =
                    typeof(AimJoystickControl).GetMethod(
                        message,
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);
                Assert.That(interruptionHandler, Is.Not.Null);
                interruptionHandler.Invoke(
                    joystick,
                    new object[] { state });

                Assert.That(joystick.IsHeld, Is.False);
                Assert.That(
                    joystick.NormalizedInput,
                    Is.EqualTo(Vector2.zero));
            }
            finally
            {
                Object.DestroyImmediate(joystickObject);
            }
        }
    }

    public sealed class ImmediateGraphicRaycaster :
        GraphicRaycaster
    {
        public GameObject Target { get; set; }

        public override bool IsActive()
        {
            return true;
        }

        public override void Raycast(
            PointerEventData eventData,
            List<RaycastResult> resultAppendList)
        {
            resultAppendList.Add(
                new RaycastResult
                {
                    gameObject = Target,
                    module = this,
                    distance = 0f,
                    index = resultAppendList.Count,
                    screenPosition = eventData.position
                });
        }
    }
}
