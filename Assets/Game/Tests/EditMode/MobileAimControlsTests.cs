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
        public void VisibleWorldBounds_UsesOrthographicCameraSize()
        {
            Rect bounds = PlayerController.CalculateVisibleWorldBounds(
                new Vector2(2f, -1f),
                5f,
                2f);

            Assert.That(bounds.xMin, Is.EqualTo(-8f));
            Assert.That(bounds.xMax, Is.EqualTo(12f));
            Assert.That(bounds.yMin, Is.EqualTo(-6f));
            Assert.That(bounds.yMax, Is.EqualTo(4f));
        }

        [TestCase(10f, 0f, 0f)]
        [TestCase(10f, 0.25f, 2.5f)]
        [TestCase(10f, 1f, 10f)]
        [TestCase(10f, 2f, 10f)]
        public void DirectionalMovement_ScalesSpeedWithPadMagnitude(
            float speed,
            float inputMagnitude,
            float expected)
        {
            Assert.That(
                PlayerMovement.CalculateDirectionalTargetSpeed(
                    speed,
                    inputMagnitude),
                Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void CircleSlide_InwardInputStopsAtAttackRange()
        {
            Vector2 result =
                PlayerMovement.CalculateCircleSlidePosition(
                    Vector2.right,
                    Vector2.right * 0.5f,
                    Vector2.zero,
                    1f);

            Assert.That(result, Is.EqualTo(Vector2.right));
        }

        [Test]
        public void CircleSlide_DiagonalInputKeepsTangentialMovement()
        {
            Vector2 result =
                PlayerMovement.CalculateCircleSlidePosition(
                    Vector2.right,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    1f);

            Assert.That(result.x, Is.LessThan(1f));
            Assert.That(result.y, Is.GreaterThan(0f));
            Assert.That(result.magnitude, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void CircleSlide_OutwardInputRemainsUnchanged()
        {
            var proposed = new Vector2(1.5f, 0.25f);

            Assert.That(
                PlayerMovement.CalculateCircleSlidePosition(
                    Vector2.right,
                    proposed,
                    Vector2.zero,
                    1f),
                Is.EqualTo(proposed));
        }

        [Test]
        public void CircleSlide_LargeStepCannotSweepThroughCircle()
        {
            Vector2 result =
                PlayerMovement.CalculateCircleSlidePosition(
                    new Vector2(-2f, 0f),
                    new Vector2(2f, 0f),
                    Vector2.zero,
                    1f);

            Assert.That(result.x, Is.EqualTo(-1f).Within(0.001f));
            Assert.That(result.magnitude, Is.GreaterThanOrEqualTo(1f));
        }

        [Test]
        public void CircleSlide_CloserEnemyDoesNotPushPlayerOutward()
        {
            Vector2 result =
                PlayerMovement.CalculateCircleSlidePosition(
                    Vector2.right * 0.8f,
                    Vector2.right * 0.6f,
                    Vector2.zero,
                    1.2f);

            Assert.That(
                result,
                Is.EqualTo(Vector2.right * 0.8f));
        }

        [Test]
        public void CircleSlide_CloserRadiusKeepsTangentialMovementOnArc()
        {
            Vector2 result =
                PlayerMovement.CalculateCircleSlidePosition(
                    Vector2.right * 0.8f,
                    new Vector2(0.8f, 0.2f),
                    Vector2.zero,
                    1.2f);

            Assert.That(result.x, Is.LessThan(0.8f));
            Assert.That(result.y, Is.GreaterThan(0f));
            Assert.That(
                result.magnitude,
                Is.EqualTo(0.8f).Within(0.001f));
        }

        [Test]
        public void CircleSlide_RepeatedTangentialMovementDoesNotDriftOutward()
        {
            Vector2 current = Vector2.right * 1.2f;
            for (int step = 0; step < 20; step++)
            {
                Vector2 tangent = new Vector2(
                    -current.y,
                    current.x).normalized;
                current = PlayerMovement.CalculateCircleSlidePosition(
                    current,
                    current + tangent * 0.16f,
                    Vector2.zero,
                    1.2f);
            }

            Assert.That(
                current.magnitude,
                Is.EqualTo(1.2f).Within(0.001f));
        }

        [Test]
        public void CircleSlide_CloserRadiusStillAllowsExplicitRetreat()
        {
            var proposed = Vector2.right * 0.9f;

            Assert.That(
                PlayerMovement.CalculateCircleSlidePosition(
                    Vector2.right * 0.8f,
                    proposed,
                    Vector2.zero,
                    1.2f),
                Is.EqualTo(proposed));
        }

        [Test]
        public void CircleSlide_CloserRadiusCannotSweepThroughEnemy()
        {
            Vector2 result =
                PlayerMovement.CalculateCircleSlidePosition(
                    Vector2.right * 0.8f,
                    Vector2.left * 1.2f,
                    Vector2.zero,
                    1.2f);

            Assert.That(result.x, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(result.magnitude, Is.EqualTo(0.8f).Within(0.001f));
        }

        [TestCase(1.2f, EnemyArchetype.Melee, 0.85f, 0.83f)]
        [TestCase(1.65f, EnemyArchetype.Melee, 0.95f, 0.93f)]
        [TestCase(1.2f, EnemyArchetype.Ranged, 2.25f, 1.18f)]
        [TestCase(1.2f, EnemyArchetype.Shield, 0f, 1.18f)]
        [TestCase(1.2f, EnemyArchetype.Boss, 0.85f, 1.18f)]
        public void ModeOneEngagementRadius_AllowsNormalEnemyAttack(
            float playerRange,
            EnemyArchetype archetype,
            float enemyRange,
            float expected)
        {
            Assert.That(
                PlayerController.CalculateModeOneEngagementRadius(
                    playerRange,
                    archetype,
                    enemyRange),
                Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void MovementPiercingBudget_RechargesOnlyAfterScheduledTime()
        {
            Assert.That(
                PlayerController.ShouldRefreshMovementPiercingBudget(
                    0,
                    10.39f,
                    10.4f),
                Is.False);
            Assert.That(
                PlayerController.ShouldRefreshMovementPiercingBudget(
                    0,
                    10.4f,
                    10.4f),
                Is.True);
            Assert.That(
                PlayerController.ShouldRefreshMovementPiercingBudget(
                    1,
                    11f,
                    10.4f),
                Is.False);
            Assert.That(
                PlayerController.ShouldRefreshMovementPiercingBudget(
                    0,
                    11f,
                    float.PositiveInfinity),
                Is.False);
        }

        [TestCase(1f, 0f, 1f, 0f, true)]
        [TestCase(1f, 0f, -1f, 0f, false)]
        [TestCase(1f, 0f, 1f, 1f, false)]
        public void ModeOnePass_StartsOnlyWhenMovementCrossesEnemy(
            float movementX,
            float movementY,
            float targetX,
            float targetY,
            bool expected)
        {
            Vector2 movement = new(movementX, movementY);
            Assert.That(
                PlayerController.CanStartModeOnePass(
                    Vector2.zero,
                    new Vector2(targetX, targetY),
                    movement,
                    0.6f),
                Is.EqualTo(expected));
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

        [TestCase(false, false, true, false, true, true, true)]
        [TestCase(false, false, true, false, false, true, false)]
        [TestCase(false, false, true, true, false, true, true)]
        [TestCase(true, false, true, true, true, true, false)]
        [TestCase(false, true, true, true, true, true, false)]
        [TestCase(false, false, false, true, true, true, false)]
        [TestCase(false, false, true, true, true, false, false)]
        public void ModeOneLockedTarget_RespectsManualPriorityAndOneShot(
            bool manualInputHeld,
            bool commandActive,
            bool hasTarget,
            bool autoAttackEnabled,
            bool oneShotPending,
            bool intervalElapsed,
            bool expected)
        {
            Assert.That(
                PlayerController.ShouldStartModeOneLockedTargetCommand(
                    manualInputHeld,
                    commandActive,
                    hasTarget,
                    autoAttackEnabled,
                    oneShotPending,
                    intervalElapsed),
                Is.EqualTo(expected));
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

        [Test]
        public void Joystick_CancelInputReleasesOwnedPointer()
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

                joystick.CancelInput();

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

        [Test]
        public void MobileControlSettings_ClampScaleAndSafeAreaPosition()
        {
            MobileControlSettings settings = MobileControlSettings.Default;
            settings.joystickScale = 0.2f;
            settings.joystickPosition = new Vector2(-1f, 2f);
            settings.attackScale = 3f;
            settings.attackPosition = new Vector2(2f, -1f);

            settings = MobileControlSettingsStore.Clamp(settings);

            Assert.That(
                settings.joystickScale,
                Is.EqualTo(MobileControlSettingsStore.MinimumScale));
            Assert.That(
                settings.attackScale,
                Is.EqualTo(MobileControlSettingsStore.MaximumScale));
            Assert.That(
                settings.joystickPosition,
                Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(
                settings.attackPosition,
                Is.EqualTo(new Vector2(1f, 0f)));

            Rect safeArea =
                MobileControlSettingsStore.CalculateSafeAreaInParent(
                    new Rect(-500f, -1000f, 1000f, 2000f),
                    new Rect(0f, 100f, 1000f, 1800f),
                    new Vector2(1000f, 2000f));
            Vector2 minimumCenter =
                MobileControlSettingsStore.CalculateControlCenter(
                    safeArea,
                    new Vector2(200f, 200f),
                    1.5f,
                    Vector2.zero);
            Vector2 maximumCenter =
                MobileControlSettingsStore.CalculateControlCenter(
                    safeArea,
                    new Vector2(200f, 200f),
                    1.5f,
                    Vector2.one);

            Assert.That(
                minimumCenter,
                Is.EqualTo(new Vector2(-350f, -750f)));
            Assert.That(
                maximumCenter,
                Is.EqualTo(new Vector2(350f, 750f)));
        }

        [Test]
        public void MobileControlSettings_PlayerPrefsRoundTrip()
        {
            MobileControlSettings original =
                MobileControlSettingsStore.Load();
            try
            {
                MobileControlSettings expected =
                    MobileControlSettings.Default;
                expected.controlsEnabled = false;
                expected.autoAttackEnabled = true;
                expected.controlMode =
                    MobileControlMode.DirectMoveAutoAim;
                expected.joystickScale = 1.25f;
                expected.joystickPosition = new Vector2(0.2f, 0.35f);
                expected.attackScale = 0.8f;
                expected.attackPosition = new Vector2(0.75f, 0.4f);

                MobileControlSettingsStore.Save(expected);
                MobileControlSettings actual =
                    MobileControlSettingsStore.Load();

                Assert.That(actual.controlsEnabled, Is.False);
                Assert.That(actual.autoAttackEnabled, Is.True);
                Assert.That(
                    actual.controlMode,
                    Is.EqualTo(
                        MobileControlMode.DirectMoveAutoAim));
                Assert.That(actual.joystickScale, Is.EqualTo(1.25f));
                Assert.That(
                    actual.joystickPosition,
                    Is.EqualTo(new Vector2(0.2f, 0.35f)));
                Assert.That(actual.attackScale, Is.EqualTo(0.8f));
                Assert.That(
                    actual.attackPosition,
                    Is.EqualTo(new Vector2(0.75f, 0.4f)));
            }
            finally
            {
                MobileControlSettingsStore.Save(original);
            }
        }

        [Test]
        public void MobileControlSettings_LegacyVersionKeepsLayoutAsModeTwo()
        {
            const string preferencesKey =
                "SimpleGame.MobileControls.v1";
            bool hadOriginal = PlayerPrefs.HasKey(preferencesKey);
            string original = hadOriginal
                ? PlayerPrefs.GetString(preferencesKey)
                : null;
            try
            {
                PlayerPrefs.SetString(
                    preferencesKey,
                    "{\"version\":1," +
                    "\"controlsEnabled\":false," +
                    "\"autoAttackEnabled\":true," +
                    "\"joystickScale\":1.25," +
                    "\"joystickPosition\":{\"x\":0.2,\"y\":0.35}," +
                    "\"attackScale\":0.8," +
                    "\"attackPosition\":{\"x\":0.75,\"y\":0.4}}");

                MobileControlSettings migrated =
                    MobileControlSettingsStore.Load();

                Assert.That(
                    migrated.version,
                    Is.EqualTo(MobileControlSettingsStore.CurrentVersion));
                Assert.That(
                    migrated.controlMode,
                    Is.EqualTo(MobileControlMode.AimCommand));
                Assert.That(migrated.controlsEnabled, Is.False);
                Assert.That(migrated.autoAttackEnabled, Is.True);
                Assert.That(migrated.joystickScale, Is.EqualTo(1.25f));
                Assert.That(
                    migrated.joystickPosition,
                    Is.EqualTo(new Vector2(0.2f, 0.35f)));
                Assert.That(migrated.attackScale, Is.EqualTo(0.8f));
                Assert.That(
                    migrated.attackPosition,
                    Is.EqualTo(new Vector2(0.75f, 0.4f)));
            }
            finally
            {
                if (hadOriginal)
                {
                    PlayerPrefs.SetString(preferencesKey, original);
                }
                else
                {
                    PlayerPrefs.DeleteKey(preferencesKey);
                }

                PlayerPrefs.Save();
            }
        }

        [Test]
        public void MobileControlSettings_InvalidModeFallsBackToModeTwo()
        {
            MobileControlSettings settings =
                MobileControlSettings.Default;
            settings.controlMode = (MobileControlMode)999;

            settings = MobileControlSettingsStore.Clamp(settings);

            Assert.That(
                settings.controlMode,
                Is.EqualTo(MobileControlMode.AimCommand));
        }

        [Test]
        public void ControlPositionInverse_AllowsFullScreenCrossing()
        {
            Rect safeArea = new(-540f, -960f, 1080f, 1920f);
            Vector2 baseSize = new(240f, 240f);
            const float scale = 1.25f;
            foreach (Vector2 expected in new[]
                     {
                         Vector2.zero,
                         new Vector2(0.95f, 0.15f),
                         Vector2.one
                     })
            {
                Vector2 center = MobileControlSettingsStore
                    .CalculateControlCenter(
                        safeArea,
                        baseSize,
                        scale,
                        expected);
                Vector2 actual = MobileControlSettingsStore
                    .CalculateNormalizedPosition(
                        safeArea,
                        baseSize,
                        scale,
                        center);
                Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.001f));
                Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.001f));
            }

            Vector2 crossedToRight =
                MobileControlSettingsStore
                    .CalculateControlCenter(
                        safeArea,
                        baseSize,
                        scale,
                        new Vector2(0.95f, 0.15f));
            Assert.That(crossedToRight.x, Is.GreaterThan(0f));

            Vector2 clamped =
                MobileControlSettingsStore
                    .CalculateNormalizedPosition(
                        safeArea,
                        baseSize,
                        scale,
                        new Vector2(9999f, -9999f));
            Assert.That(clamped, Is.EqualTo(new Vector2(1f, 0f)));
        }

        [Test]
        public void AutoAttack_UsesPointThreeSecondIntervalAndDefaultsOff()
        {
            Assert.That(
                PlayerController.AutoAttackInterval,
                Is.EqualTo(0.3f));
            Assert.That(
                MobileControlSettings.Default.autoAttackEnabled,
                Is.False);
            Assert.That(
                MobileControlSettings.Default.controlMode,
                Is.EqualTo(MobileControlMode.AimCommand));
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
