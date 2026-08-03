using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace SimpleGame.Tests
{
    public sealed class EnemyWorldServiceTests
    {
        [Test]
        public void Register_DeduplicatesAndUnregistersEnemy()
        {
            var serviceObject = new GameObject("EnemyWorld");
            var enemyObject = new GameObject("Enemy");
            try
            {
                EnemyWorldService service =
                    serviceObject.AddComponent<EnemyWorldService>();
                EnemyBase enemy =
                    enemyObject.AddComponent<EnemyActor>();

                service.Register(enemy);
                service.Register(enemy);

                Assert.That(service.Enemies, Has.Count.EqualTo(1));

                service.Unregister(enemy);

                Assert.That(service.Enemies, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(enemyObject);
                Object.DestroyImmediate(serviceObject);
            }
        }

        [Test]
        public void FindFirstEnemyOnPath_SelectsNearestAndHonorsIgnore()
        {
            var serviceObject = new GameObject("EnemyWorld");
            EnemyBase nearEnemy = null;
            EnemyBase farEnemy = null;
            EnemyBase offPathEnemy = null;
            try
            {
                EnemyWorldService service =
                    serviceObject.AddComponent<EnemyWorldService>();
                nearEnemy = CreateLiveEnemy(
                    "NearEnemy",
                    new Vector2(2f, 0f));
                farEnemy = CreateLiveEnemy(
                    "FarEnemy",
                    new Vector2(4f, 0f));
                offPathEnemy = CreateLiveEnemy(
                    "OffPathEnemy",
                    new Vector2(1f, 2f));
                service.Register(farEnemy);
                service.Register(offPathEnemy);
                service.Register(nearEnemy);

                EnemyBase first = service.FindFirstEnemyOnPath(
                    Vector2.zero,
                    new Vector2(6f, 0f),
                    0.3f);
                EnemyBase afterIgnore = service.FindFirstEnemyOnPath(
                    Vector2.zero,
                    new Vector2(6f, 0f),
                    0.3f,
                    nearEnemy);
                var ignoredGenerations =
                    new Dictionary<EnemyBase, uint>
                    {
                        [nearEnemy] = nearEnemy.SpawnGeneration
                    };
                EnemyBase afterGenerationIgnore =
                    service.FindFirstEnemyOnPath(
                        Vector2.zero,
                        new Vector2(6f, 0f),
                        0.3f,
                        null,
                        ignoredGenerations);

                Assert.That(first, Is.SameAs(nearEnemy));
                Assert.That(afterIgnore, Is.SameAs(farEnemy));
                Assert.That(
                    afterGenerationIgnore,
                    Is.SameAs(farEnemy));
            }
            finally
            {
                DestroyEnemy(offPathEnemy);
                DestroyEnemy(farEnemy);
                DestroyEnemy(nearEnemy);
                Object.DestroyImmediate(serviceObject);
            }
        }

        [Test]
        public void FindRandomLivingEnemyInBounds_SelectsOnlyEligibleEnemies()
        {
            var serviceObject = new GameObject("EnemyWorld");
            EnemyBase firstLiving = null;
            EnemyBase deadEnemy = null;
            EnemyBase secondLiving = null;
            EnemyBase outsideEnemy = null;
            try
            {
                EnemyWorldService service =
                    serviceObject.AddComponent<EnemyWorldService>();
                firstLiving = CreateLiveEnemy(
                    "FirstLiving",
                    Vector2.zero);
                deadEnemy = new GameObject("DeadEnemy")
                    .AddComponent<EnemyActor>();
                secondLiving = CreateLiveEnemy(
                    "SecondLiving",
                    Vector2.one);
                outsideEnemy = CreateLiveEnemy(
                    "Outside",
                    new Vector2(3f, 0f));
                service.Register(firstLiving);
                service.Register(deadEnemy);
                service.Register(outsideEnemy);
                service.Register(secondLiving);
                var bounds = new Rect(-1f, -1f, 3f, 3f);

                Assert.That(
                    service.FindRandomLivingEnemyInBounds(
                        bounds,
                        0f),
                    Is.SameAs(firstLiving));
                Assert.That(
                    service.FindRandomLivingEnemyInBounds(
                        bounds,
                        0.999f),
                    Is.SameAs(secondLiving));
                Assert.That(
                    service.FindRandomLivingEnemyInBounds(
                        bounds,
                        1f),
                    Is.SameAs(secondLiving));
            }
            finally
            {
                DestroyEnemy(outsideEnemy);
                DestroyEnemy(secondLiving);
                DestroyEnemy(deadEnemy);
                DestroyEnemy(firstLiving);
                Object.DestroyImmediate(serviceObject);
            }
        }

        [Test]
        public void FindRandomLivingEnemyInBounds_ReturnsNullWithoutCandidate()
        {
            var serviceObject = new GameObject("EnemyWorld");
            EnemyBase deadEnemy = null;
            EnemyBase outsideEnemy = null;
            try
            {
                EnemyWorldService service =
                    serviceObject.AddComponent<EnemyWorldService>();
                deadEnemy = new GameObject("DeadEnemy")
                    .AddComponent<EnemyActor>();
                outsideEnemy = CreateLiveEnemy(
                    "Outside",
                    new Vector2(3f, 0f));
                service.Register(deadEnemy);
                service.Register(outsideEnemy);

                Assert.That(
                    service.FindRandomLivingEnemyInBounds(
                        new Rect(-1f, -1f, 2f, 2f),
                        0.5f),
                    Is.Null);
            }
            finally
            {
                DestroyEnemy(outsideEnemy);
                DestroyEnemy(deadEnemy);
                Object.DestroyImmediate(serviceObject);
            }
        }

        [Test]
        public void FindNearestLivingEnemyInBounds_IgnoresDeadAndOutside()
        {
            var serviceObject = new GameObject("EnemyWorld");
            EnemyBase nearest = null;
            EnemyBase farther = null;
            EnemyBase outside = null;
            EnemyBase dead = null;
            try
            {
                EnemyWorldService service =
                    serviceObject.AddComponent<EnemyWorldService>();
                nearest = CreateLiveEnemy(
                    "Nearest",
                    new Vector2(1f, 0f));
                farther = CreateLiveEnemy(
                    "Farther",
                    new Vector2(2f, 0f));
                outside = CreateLiveEnemy(
                    "Outside",
                    new Vector2(4f, 0f));
                dead = new GameObject("Dead")
                    .AddComponent<EnemyActor>();
                service.Register(farther);
                service.Register(outside);
                service.Register(dead);
                service.Register(nearest);

                EnemyBase selected =
                    service.FindNearestLivingEnemyInBounds(
                        Vector2.zero,
                        new Rect(-2.5f, -2.5f, 5f, 5f));

                Assert.That(selected, Is.SameAs(nearest));
            }
            finally
            {
                DestroyEnemy(dead);
                DestroyEnemy(outside);
                DestroyEnemy(farther);
                DestroyEnemy(nearest);
                Object.DestroyImmediate(serviceObject);
            }
        }

        [Test]
        public void CollectPiercingTargets_UsesDistanceOrderAndLimit()
        {
            var serviceObject = new GameObject("EnemyWorld");
            EnemyBase primary = null;
            EnemyBase nearBehind = null;
            EnemyBase farBehind = null;
            try
            {
                EnemyWorldService service =
                    serviceObject.AddComponent<EnemyWorldService>();
                primary = CreateLiveEnemy(
                    "Primary",
                    new Vector2(1f, 0f));
                nearBehind = CreateLiveEnemy(
                    "NearBehind",
                    new Vector2(2f, 0f));
                farBehind = CreateLiveEnemy(
                    "FarBehind",
                    new Vector2(3f, 0f));
                service.Register(farBehind);
                service.Register(primary);
                service.Register(nearBehind);

                var targets = service.CollectPiercingTargets(
                    Vector2.zero,
                    primary,
                    1,
                    4.5f,
                    0.42f);

                Assert.That(targets, Has.Count.EqualTo(2));
                Assert.That(targets[0], Is.SameAs(primary));
                Assert.That(targets[1], Is.SameAs(nearBehind));
            }
            finally
            {
                DestroyEnemy(farBehind);
                DestroyEnemy(nearBehind);
                DestroyEnemy(primary);
                Object.DestroyImmediate(serviceObject);
            }
        }

        [Test]
        public void FillEnemiesInRadius_ClearsAndReusesCallerBuffer()
        {
            var serviceObject = new GameObject("EnemyWorld");
            EnemyBase nearEnemy = null;
            EnemyBase edgeEnemy = null;
            EnemyBase outsideEnemy = null;
            try
            {
                EnemyWorldService service =
                    serviceObject.AddComponent<EnemyWorldService>();
                nearEnemy = CreateLiveEnemy(
                    "NearEnemy",
                    new Vector2(0.5f, 0f));
                edgeEnemy = CreateLiveEnemy(
                    "EdgeEnemy",
                    new Vector2(1.5f, 0f));
                outsideEnemy = CreateLiveEnemy(
                    "OutsideEnemy",
                    new Vector2(1.51f, 0f));
                service.Register(outsideEnemy);
                service.Register(edgeEnemy);
                service.Register(nearEnemy);
                var buffer = new List<EnemyBase>
                {
                    outsideEnemy
                };

                service.FillEnemiesInRadius(
                    Vector2.zero,
                    1.5f,
                    buffer);

                Assert.That(
                    buffer,
                    Is.EquivalentTo(new[]
                    {
                        nearEnemy,
                        edgeEnemy
                    }));
                Assert.That(
                    buffer.Contains(outsideEnemy),
                    Is.False);
            }
            finally
            {
                DestroyEnemy(outsideEnemy);
                DestroyEnemy(edgeEnemy);
                DestroyEnemy(nearEnemy);
                Object.DestroyImmediate(serviceObject);
            }
        }

        [Test]
        public void AimAssist_SelectsOnlyLiveEnemyInsideRawAimCorridor()
        {
            var serviceObject = new GameObject("EnemyWorld");
            EnemyBase inside = null;
            EnemyBase outside = null;
            EnemyBase beyond = null;
            try
            {
                EnemyWorldService service =
                    serviceObject.AddComponent<EnemyWorldService>();
                inside = CreateLiveEnemy(
                    "Inside",
                    new Vector2(3f, 0.4f));
                outside = CreateLiveEnemy(
                    "Outside",
                    new Vector2(2f, 1.2f));
                beyond = CreateLiveEnemy(
                    "Beyond",
                    new Vector2(7f, 0f));
                service.Register(outside);
                service.Register(beyond);
                service.Register(inside);

                EnemyBase target = service.FindAimAssistTarget(
                    Vector2.zero,
                    new Vector2(6f, 0f),
                    0.65f);

                Assert.That(target, Is.SameAs(inside));
            }
            finally
            {
                DestroyEnemy(beyond);
                DestroyEnemy(outside);
                DestroyEnemy(inside);
                Object.DestroyImmediate(serviceObject);
            }
        }

        [Test]
        public void AimAssist_PrioritizesAimAlignmentThenDistance()
        {
            var serviceObject = new GameObject("EnemyWorld");
            EnemyBase nearOffAxis = null;
            EnemyBase farAligned = null;
            try
            {
                EnemyWorldService service =
                    serviceObject.AddComponent<EnemyWorldService>();
                nearOffAxis = CreateLiveEnemy(
                    "NearOffAxis",
                    new Vector2(2f, 0.4f));
                farAligned = CreateLiveEnemy(
                    "FarAligned",
                    new Vector2(4f, 0.05f));
                service.Register(nearOffAxis);
                service.Register(farAligned);

                EnemyBase target = service.FindAimAssistTarget(
                    Vector2.zero,
                    new Vector2(6f, 0f),
                    0.65f);

                Assert.That(target, Is.SameAs(farAligned));
            }
            finally
            {
                DestroyEnemy(farAligned);
                DestroyEnemy(nearOffAxis);
                Object.DestroyImmediate(serviceObject);
            }
        }

        [Test]
        public void AimAssist_RetainsPreferredTargetAcrossSmallAimChanges()
        {
            var serviceObject = new GameObject("EnemyWorld");
            EnemyBase preferred = null;
            EnemyBase challenger = null;
            try
            {
                EnemyWorldService service =
                    serviceObject.AddComponent<EnemyWorldService>();
                preferred = CreateLiveEnemy(
                    "Preferred",
                    new Vector2(3f, 0.15f));
                challenger = CreateLiveEnemy(
                    "Challenger",
                    new Vector2(3.1f, 0f));
                service.Register(preferred);
                service.Register(challenger);

                EnemyBase withoutRetention =
                    service.FindAimAssistTarget(
                        Vector2.zero,
                        new Vector2(6f, 0f),
                        0.65f);
                EnemyBase withRetention =
                    service.FindAimAssistTarget(
                        Vector2.zero,
                        new Vector2(6f, 0f),
                        0.65f,
                        preferred,
                        1.35f);

                Assert.That(
                    withoutRetention,
                    Is.SameAs(challenger));
                Assert.That(withRetention, Is.SameAs(preferred));
            }
            finally
            {
                DestroyEnemy(challenger);
                DestroyEnemy(preferred);
                Object.DestroyImmediate(serviceObject);
            }
        }

        [Test]
        public void AimAssist_VisibleEndpointSnapsButRawEndpointIsPreserved()
        {
            var serviceObject = new GameObject("EnemyWorld");
            var playerObject = new GameObject("Player");
            var cameraObject = new GameObject("Camera");
            var sessionObject = new GameObject("Session");
            EnemyBase enemy = null;
            try
            {
                EnemyWorldService service =
                    serviceObject.AddComponent<EnemyWorldService>();
                enemy = CreateLiveEnemy(
                    "Target",
                    new Vector2(3f, 0f));
                service.Register(enemy);

                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.aspect = 2f;
                cameraObject.transform.position =
                    new Vector3(0f, 0f, -10f);

                PlayerRoot player =
                    playerObject.AddComponent<PlayerRoot>();
                PlayerController controller =
                    playerObject.GetComponent<PlayerController>();
                HealthComponent health =
                    playerObject.GetComponent<HealthComponent>();
                health.Configure(10);
                SetPrivateField(player, "health", health);
                SetPrivateField(
                    player,
                    "movement",
                    playerObject.GetComponent<PlayerMovement>());
                SetPrivateField(
                    player,
                    "stats",
                    playerObject.GetComponent<PlayerStats>());
                SetPrivateField(
                    player,
                    "combatAbilities",
                    playerObject.GetComponent<PlayerCombatAbilities>());
                PrototypeGameSession session =
                    sessionObject.AddComponent<PrototypeGameSession>();
                SetPrivateField(session, "state", GameRunState.Playing);
                controller.Configure(
                    player,
                    session,
                    service,
                    camera,
                    PlayerController.DefaultAttackRange);

                Assert.That(controller.BeginAim(), Is.True);
                controller.SetAimInput(Vector2.right);

                Assert.That(
                    controller.RawAimDestination.x,
                    Is.GreaterThan(enemy.transform.position.x));
                Assert.That(
                    controller.AimDestination,
                    Is.EqualTo((Vector2)enemy.transform.position));
                Assert.That(controller.ExecuteAimedCommand(), Is.True);
                Assert.That(
                    GetPrivateField<Vector2>(controller, "destination"),
                    Is.EqualTo((Vector2)enemy.transform.position));
            }
            finally
            {
                DestroyEnemy(enemy);
                Object.DestroyImmediate(sessionObject);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(serviceObject);
            }
        }

        [Test]
        public void ModeOne_MissingLockDoesNotClearRangeAttackCooldown()
        {
            var playerObject = new GameObject("Player");
            try
            {
                PlayerRoot player =
                    playerObject.AddComponent<PlayerRoot>();
                PlayerController controller =
                    playerObject.GetComponent<PlayerController>();
                const float scheduledAttackAt = 12.5f;
                SetPrivateField(
                    controller,
                    "nextModeOneAttackAt",
                    scheduledAttackAt);

                MethodInfo resolveLockedEnemy =
                    typeof(PlayerController).GetMethod(
                        "ResolveLockedEnemy",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);
                Assert.That(resolveLockedEnemy, Is.Not.Null);
                Assert.That(
                    resolveLockedEnemy.Invoke(controller, null),
                    Is.Null);
                Assert.That(
                    GetPrivateField<float>(
                        controller,
                        "nextModeOneAttackAt"),
                    Is.EqualTo(scheduledAttackAt));
                Assert.That(player, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void ModeOneMovement_AttacksWithoutAutoAimOrAutoAttackSetting()
        {
            var serviceObject = new GameObject("EnemyWorld");
            var playerObject = new GameObject("Player");
            var sessionObject = new GameObject("Session");
            EnemyBase enemy = null;
            CombatFeedbackProfile feedbackProfile = null;
            try
            {
                EnemyWorldService service =
                    serviceObject.AddComponent<EnemyWorldService>();
                PrototypeGameSession session =
                    sessionObject.AddComponent<PrototypeGameSession>();
                SetPrivateField(session, "state", GameRunState.Playing);
                feedbackProfile =
                    ScriptableObject.CreateInstance<CombatFeedbackProfile>();
                CombatFeedbackController feedback =
                    sessionObject.AddComponent<CombatFeedbackController>();
                feedback.Configure(
                    sessionObject.AddComponent<CameraShakeController>(),
                    feedbackProfile);
                SetPrivateField(session, "combatFeedback", feedback);

                enemy = CreateLiveEnemy(
                    "InRangeTarget",
                    new Vector2(0.5f, 0f));
                enemy.gameObject.AddComponent<CharacterSpriteAnimator>();
                UnityEngine.TestTools.LogAssert.Expect(
                    LogType.Error,
                    "Melee prefab has no configured Animator or " +
                    "SpriteRenderer.");
                UnityEngine.TestTools.LogAssert.Expect(
                    LogType.Error,
                    "Melee prefab requires marker and level visuals.");
                enemy.Configure(
                    null,
                    session,
                    service,
                    1,
                    1,
                    PrototypeEnemyDefinitions.Create(
                        EnemyArchetype.Melee));
                service.Register(enemy);

                PlayerController controller = ConfigureController(
                    playerObject,
                    sessionObject,
                    service,
                    null);
                PlayerRoot player =
                    playerObject.GetComponent<PlayerRoot>();
                SetPrivateField(
                    player,
                    "critical",
                    playerObject.GetComponent<CriticalSystem>());
                SetPrivateField(
                    player,
                    "progression",
                    playerObject.GetComponent<PlayerProgression>());
                SetPrivateField(
                    player,
                    "characterAnimation",
                    playerObject.AddComponent<CharacterSpriteAnimator>());
                player.CombatAbilities.Configure(
                    player,
                    service,
                    null,
                    null);

                controller.SetControlMode(
                    MobileControlMode.DirectMoveAutoAim);
                controller.SetAutoAttackEnabled(false);
                Assert.That(controller.BeginControlInput(), Is.True);
                Assert.That(controller.AutoAttackEnabled, Is.False);
                Assert.That(
                    GetPrivateField<EnemyBase>(
                        controller,
                        "lockedEnemy"),
                    Is.Null,
                    "The auto-aim button was not used.");

                float fullHealth = enemy.CurrentHealth;
                InvokePrivate(controller, "TickModeOneRangeAttack");
                Assert.That(
                    enemy.CurrentHealth,
                    Is.EqualTo(fullHealth),
                    "Holding the pad at its neutral center is not movement.");

                controller.SetControlInput(Vector2.right);
                InvokePrivate(controller, "TickModeOneRangeAttack");
                float healthAfterFirstAttack = enemy.CurrentHealth;
                Assert.That(healthAfterFirstAttack, Is.LessThan(fullHealth));
                Assert.That(
                    GetPrivateField<EnemyBase>(
                        controller,
                        "lockedEnemy"),
                    Is.SameAs(enemy));

                InvokePrivate(controller, "TickModeOneRangeAttack");
                Assert.That(
                    enemy.CurrentHealth,
                    Is.EqualTo(healthAfterFirstAttack),
                    "The intrinsic movement attack still uses the 0.3 " +
                    "second interval.");
            }
            finally
            {
                DestroyEnemy(enemy);
                Object.DestroyImmediate(feedbackProfile);
                Object.DestroyImmediate(sessionObject);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(serviceObject);
            }
        }

        [Test]
        public void ModeOneMovement_PathEnemyOverridesOutOfRangeLockOnHit()
        {
            var serviceObject = new GameObject("EnemyWorld");
            var playerObject = new GameObject("Player");
            var sessionObject = new GameObject("Session");
            EnemyBase lockedEnemy = null;
            EnemyBase pathEnemy = null;
            CombatFeedbackProfile feedbackProfile = null;
            try
            {
                EnemyWorldService service =
                    serviceObject.AddComponent<EnemyWorldService>();
                PrototypeGameSession session =
                    sessionObject.AddComponent<PrototypeGameSession>();
                SetPrivateField(session, "state", GameRunState.Playing);
                feedbackProfile =
                    ScriptableObject.CreateInstance<CombatFeedbackProfile>();
                CombatFeedbackController feedback =
                    sessionObject.AddComponent<CombatFeedbackController>();
                feedback.Configure(
                    sessionObject.AddComponent<CameraShakeController>(),
                    feedbackProfile);
                SetPrivateField(session, "combatFeedback", feedback);

                lockedEnemy = CreateLiveEnemy(
                    "PreviousTarget",
                    new Vector2(-2f, 0f));
                pathEnemy = CreateLiveEnemy(
                    "PathTarget",
                    new Vector2(0.5f, 0f));
                pathEnemy.gameObject.AddComponent<CharacterSpriteAnimator>();
                UnityEngine.TestTools.LogAssert.Expect(
                    LogType.Error,
                    "Melee prefab has no configured Animator or " +
                    "SpriteRenderer.");
                UnityEngine.TestTools.LogAssert.Expect(
                    LogType.Error,
                    "Melee prefab requires marker and level visuals.");
                pathEnemy.Configure(
                    null,
                    session,
                    service,
                    1,
                    1,
                    PrototypeEnemyDefinitions.Create(
                        EnemyArchetype.Melee));
                service.Register(lockedEnemy);
                service.Register(pathEnemy);

                PlayerController controller = ConfigureController(
                    playerObject,
                    sessionObject,
                    service,
                    null);
                PlayerRoot player =
                    playerObject.GetComponent<PlayerRoot>();
                SetPrivateField(
                    player,
                    "critical",
                    playerObject.GetComponent<CriticalSystem>());
                SetPrivateField(
                    player,
                    "progression",
                    playerObject.GetComponent<PlayerProgression>());
                SetPrivateField(
                    player,
                    "characterAnimation",
                    playerObject.AddComponent<CharacterSpriteAnimator>());
                player.CombatAbilities.Configure(
                    player,
                    service,
                    null,
                    null);

                controller.SetControlMode(
                    MobileControlMode.DirectMoveAutoAim);
                Assert.That(controller.BeginControlInput(), Is.True);
                controller.SetControlInput(Vector2.right);
                SetPrivateField(controller, "lockedEnemy", lockedEnemy);
                SetPrivateField(
                    controller,
                    "lockedEnemyGeneration",
                    lockedEnemy.SpawnGeneration);
                SetPrivateField(controller, "nextModeOneAttackAt", 0f);

                float pathHealth = pathEnemy.CurrentHealth;
                float lockedHealth = lockedEnemy.CurrentHealth;
                InvokePrivate(controller, "TickModeOneRangeAttack");

                Assert.That(pathEnemy.CurrentHealth, Is.LessThan(pathHealth));
                Assert.That(lockedEnemy.CurrentHealth, Is.EqualTo(lockedHealth));
                Assert.That(
                    GetPrivateField<EnemyBase>(
                        controller,
                        "lockedEnemy"),
                    Is.SameAs(pathEnemy),
                    "The last actually hit path enemy becomes the lock.");
            }
            finally
            {
                DestroyEnemy(pathEnemy);
                DestroyEnemy(lockedEnemy);
                Object.DestroyImmediate(feedbackProfile);
                Object.DestroyImmediate(sessionObject);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(serviceObject);
            }
        }

        [Test]
        public void ModeOne_NeutralHeldPadDoesNotBlockAutoAimCommand()
        {
            var serviceObject = new GameObject("EnemyWorld");
            var playerObject = new GameObject("Player");
            var cameraObject = new GameObject("Camera");
            var sessionObject = new GameObject("Session");
            EnemyBase enemy = null;
            try
            {
                EnemyWorldService service =
                    serviceObject.AddComponent<EnemyWorldService>();
                enemy = CreateLiveEnemy(
                    "NearestTarget",
                    new Vector2(3f, 0f));
                service.Register(enemy);

                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.aspect = 2f;
                cameraObject.transform.position =
                    new Vector3(0f, 0f, -10f);

                PlayerController controller = ConfigureController(
                    playerObject,
                    sessionObject,
                    service,
                    camera);
                controller.SetControlMode(
                    MobileControlMode.DirectMoveAutoAim);
                Assert.That(controller.BeginControlInput(), Is.True);
                Assert.That(controller.ManualMovementHeld, Is.True);

                Assert.That(controller.ExecuteControlAction(), Is.True);

                Assert.That(
                    GetPrivateField<bool>(controller, "hasDestination"),
                    Is.True);
                Assert.That(
                    GetPrivateField<EnemyBase>(controller, "pendingEnemy"),
                    Is.SameAs(enemy));
                Assert.That(
                    GetPrivateField<bool>(
                        playerObject.GetComponent<PlayerMovement>(),
                        "isMoveActive"),
                    Is.True);
            }
            finally
            {
                DestroyEnemy(enemy);
                Object.DestroyImmediate(sessionObject);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(serviceObject);
            }
        }

        [Test]
        public void MovementPiercing_RechargesWhilePadRemainsHeld()
        {
            var serviceObject = new GameObject("EnemyWorld");
            var playerObject = new GameObject("Player");
            var sessionObject = new GameObject("Session");
            EnemyBase passedEnemy = null;
            try
            {
                EnemyWorldService service =
                    serviceObject.AddComponent<EnemyWorldService>();
                PlayerController controller = ConfigureController(
                    playerObject,
                    sessionObject,
                    service,
                    null);
                PlayerCombatAbilities abilities =
                    playerObject.GetComponent<PlayerCombatAbilities>();
                SetPrivateField(abilities, "piercingLevel", 2);
                controller.SetControlMode(
                    MobileControlMode.DirectMoveAutoAim);
                Assert.That(controller.BeginControlInput(), Is.True);

                Assert.That(
                    InvokePrivateWithResult<bool>(
                        controller,
                        "TryConsumeMovementPierce"),
                    Is.True);
                Assert.That(
                    InvokePrivateWithResult<bool>(
                        controller,
                        "TryConsumeMovementPierce"),
                    Is.True);
                Assert.That(
                    GetPrivateField<int>(
                        controller,
                        "remainingMovementPierces"),
                    Is.Zero);

                passedEnemy = CreateLiveEnemy(
                    "AlreadyPassed",
                    Vector2.right);
                Dictionary<EnemyBase, uint> passed =
                    GetPrivateField<Dictionary<EnemyBase, uint>>(
                        controller,
                        "movementPiercedEnemyGenerations");
                passed[passedEnemy] = passedEnemy.SpawnGeneration;
                SetPrivateField(
                    controller,
                    "movementPiercingRechargeAt",
                    Time.time - 0.01f);

                Assert.That(
                    InvokePrivateWithResult<bool>(
                        controller,
                        "HasRemainingMovementPierces"),
                    Is.True);
                Assert.That(
                    GetPrivateField<int>(
                        controller,
                        "remainingMovementPierces"),
                    Is.EqualTo(2));
                Assert.That(
                    passed.ContainsKey(passedEnemy),
                    Is.True,
                    "Recharge must not consume the same spawn twice.");

                controller.EndControlInput();
                Assert.That(
                    GetPrivateField<int>(
                        controller,
                        "remainingMovementPierces"),
                    Is.Zero);
                Assert.That(passed, Is.Empty);
            }
            finally
            {
                DestroyEnemy(passedEnemy);
                Object.DestroyImmediate(sessionObject);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(serviceObject);
            }
        }

        [Test]
        public void ModeOne_RepeatedAutoAimStaysOneShotAndManualInputWins()
        {
            var serviceObject = new GameObject("EnemyWorld");
            var playerObject = new GameObject("Player");
            var cameraObject = new GameObject("Camera");
            var sessionObject = new GameObject("Session");
            EnemyBase enemy = null;
            try
            {
                EnemyWorldService service =
                    serviceObject.AddComponent<EnemyWorldService>();
                enemy = CreateLiveEnemy(
                    "NearestTarget",
                    new Vector2(3f, 0f));
                service.Register(enemy);

                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.aspect = 2f;
                cameraObject.transform.position =
                    new Vector3(0f, 0f, -10f);

                PlayerRoot player =
                    playerObject.AddComponent<PlayerRoot>();
                PlayerController controller =
                    playerObject.GetComponent<PlayerController>();
                HealthComponent health =
                    playerObject.GetComponent<HealthComponent>();
                health.Configure(10);
                SetPrivateField(player, "health", health);
                SetPrivateField(
                    player,
                    "movement",
                    playerObject.GetComponent<PlayerMovement>());
                SetPrivateField(
                    player,
                    "stats",
                    playerObject.GetComponent<PlayerStats>());
                SetPrivateField(
                    player,
                    "combatAbilities",
                    playerObject.GetComponent<PlayerCombatAbilities>());
                PrototypeGameSession session =
                    sessionObject.AddComponent<PrototypeGameSession>();
                SetPrivateField(session, "state", GameRunState.Playing);
                controller.Configure(
                    player,
                    session,
                    service,
                    camera,
                    PlayerController.DefaultAttackRange);
                controller.SetControlMode(
                    MobileControlMode.DirectMoveAutoAim);

                Assert.That(controller.ExecuteControlAction(), Is.True);
                Assert.That(
                    GetPrivateField<EnemyBase>(
                        controller,
                        "lockedEnemy"),
                    Is.SameAs(enemy));
                Assert.That(
                    GetPrivateField<bool>(
                        controller,
                        "hasDestination"),
                    Is.True);
                Assert.That(controller.ExecuteControlAction(), Is.True);
                Assert.That(controller.ExecuteControlAction(), Is.True);
                Assert.That(
                    GetPrivateField<int>(
                        controller,
                        "pendingAttackCount"),
                    Is.EqualTo(1));

                Assert.That(controller.BeginControlInput(), Is.True);
                controller.SetControlInput(Vector2.right * 0.5f);

                Assert.That(controller.ManualMovementHeld, Is.True);
                Assert.That(
                    GetPrivateField<bool>(
                        controller,
                        "hasDestination"),
                    Is.False);
                Assert.That(
                    GetPrivateField<EnemyBase>(
                        controller,
                        "lockedEnemy"),
                    Is.SameAs(enemy));

                controller.EndControlInput();
                MethodInfo tickLockedTarget =
                    typeof(PlayerController).GetMethod(
                        "TickModeOneLockedTarget",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);
                Assert.That(tickLockedTarget, Is.Not.Null);
                tickLockedTarget.Invoke(controller, null);

                Assert.That(controller.ManualMovementHeld, Is.False);
                Assert.That(
                    GetPrivateField<bool>(
                        controller,
                        "hasDestination"),
                    Is.True);

                Assert.That(
                    controller.TryIssueCommand(
                        new Vector2(-2f, 0f)),
                    Is.True);
                Assert.That(
                    GetPrivateField<EnemyBase>(
                        controller,
                        "lockedEnemy"),
                    Is.Null);
                Assert.That(controller.ExecuteControlAction(), Is.True);
                Assert.That(
                    GetPrivateField<EnemyBase>(
                        controller,
                        "lockedEnemy"),
                    Is.SameAs(enemy));

                FieldInfo generationField =
                    typeof(EnemyBase).GetField(
                        "<SpawnGeneration>k__BackingField",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);
                Assert.That(generationField, Is.Not.Null);
                generationField.SetValue(
                    enemy,
                    enemy.SpawnGeneration + 1u);
                MethodInfo resolveLockedEnemy =
                    typeof(PlayerController).GetMethod(
                        "ResolveLockedEnemy",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);
                Assert.That(resolveLockedEnemy, Is.Not.Null);
                Assert.That(
                    resolveLockedEnemy.Invoke(controller, null),
                    Is.Null);
                Assert.That(
                    GetPrivateField<EnemyBase>(
                        controller,
                        "lockedEnemy"),
                    Is.Null);
                MethodInfo tickCommand =
                    typeof(PlayerController).GetMethod(
                        "TickCommand",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);
                Assert.That(tickCommand, Is.Not.Null);
                tickCommand.Invoke(controller, null);
                Assert.That(
                    GetPrivateField<bool>(
                        controller,
                        "hasDestination"),
                    Is.False);
            }
            finally
            {
                DestroyEnemy(enemy);
                Object.DestroyImmediate(sessionObject);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(serviceObject);
            }
        }

        [Test]
        public void ModeOne_DisablingAutoAttackCancelsOnlyRepeatCommand()
        {
            var serviceObject = new GameObject("EnemyWorld");
            var playerObject = new GameObject("Player");
            var cameraObject = new GameObject("Camera");
            var sessionObject = new GameObject("Session");
            EnemyBase enemy = null;
            try
            {
                EnemyWorldService service =
                    serviceObject.AddComponent<EnemyWorldService>();
                enemy = CreateLiveEnemy(
                    "NearestTarget",
                    new Vector2(3f, 0f));
                service.Register(enemy);

                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.aspect = 2f;
                cameraObject.transform.position =
                    new Vector3(0f, 0f, -10f);

                PlayerController controller = ConfigureController(
                    playerObject,
                    sessionObject,
                    service,
                    camera);
                controller.SetControlMode(
                    MobileControlMode.DirectMoveAutoAim);
                controller.SetAutoAttackEnabled(true);

                Assert.That(controller.BeginControlInput(), Is.True);
                Assert.That(controller.ExecuteControlAction(), Is.True);
                controller.EndControlInput();
                InvokePrivate(controller, "TickModeOneLockedTarget");

                Assert.That(
                    GetPrivateField<bool>(
                        controller,
                        "hasDestination"),
                    Is.True);
                Assert.That(
                    GetPrivateField<bool>(
                        controller,
                        "autoAttackRepeatCommandActive"),
                    Is.False);

                controller.SetAutoAttackEnabled(false);

                Assert.That(
                    GetPrivateField<bool>(
                        controller,
                        "hasDestination"),
                    Is.True,
                    "The pending right-button one-shot is user-owned.");

                controller.CancelCommand();
                SetPrivateField(
                    controller,
                    "modeOneAttackPending",
                    false);
                controller.SetAutoAttackEnabled(true);
                SetPrivateField(
                    controller,
                    "nextModeOneAttackAt",
                    0f);
                InvokePrivate(controller, "TickModeOneLockedTarget");

                Assert.That(
                    GetPrivateField<bool>(
                        controller,
                        "hasDestination"),
                    Is.True);
                Assert.That(
                    GetPrivateField<bool>(
                        controller,
                        "autoAttackRepeatCommandActive"),
                    Is.True);

                controller.SetAutoAttackEnabled(false);

                Assert.That(
                    GetPrivateField<bool>(
                        controller,
                        "hasDestination"),
                    Is.False);
                Assert.That(
                    GetPrivateField<EnemyBase>(
                        controller,
                        "lockedEnemy"),
                    Is.SameAs(enemy));
            }
            finally
            {
                DestroyEnemy(enemy);
                Object.DestroyImmediate(sessionObject);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(serviceObject);
            }
        }

        [Test]
        public void ModeTwo_DisablingAutoAttackCancelsOnlyRepeatCommand()
        {
            var serviceObject = new GameObject("EnemyWorld");
            var playerObject = new GameObject("Player");
            var sessionObject = new GameObject("Session");
            EnemyBase enemy = null;
            try
            {
                EnemyWorldService service =
                    serviceObject.AddComponent<EnemyWorldService>();
                enemy = CreateLiveEnemy(
                    "CommandTarget",
                    new Vector2(3f, 0f));
                service.Register(enemy);

                PlayerController controller = ConfigureController(
                    playerObject,
                    sessionObject,
                    service,
                    null);
                FieldInfo generationField =
                    typeof(EnemyBase).GetField(
                        "<SpawnGeneration>k__BackingField",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);
                Assert.That(generationField, Is.Not.Null);
                controller.SetAutoAttackEnabled(true);

                Assert.That(
                    controller.TryIssueCommand(
                        enemy.transform.position),
                    Is.True);
                Assert.That(
                    GetPrivateField<bool>(
                        controller,
                        "autoAttackRepeatCommandActive"),
                    Is.False);

                generationField.SetValue(
                    enemy,
                    enemy.SpawnGeneration + 1u);
                InvokePrivate(controller, "TickCommand");
                Assert.That(
                    GetPrivateField<bool>(
                        controller,
                        "hasDestination"),
                    Is.True,
                    "Target generation does not revoke a user command.");

                controller.SetAutoAttackEnabled(false);

                Assert.That(
                    GetPrivateField<bool>(
                        controller,
                        "hasDestination"),
                    Is.True,
                    "The original touch command is user-owned.");

                controller.CancelCommand();
                controller.SetAutoAttackEnabled(true);
                Assert.That(
                    controller.TryIssueCommand(
                        enemy.transform.position),
                    Is.True);
                controller.CancelCommand();
                SetPrivateField(
                    controller,
                    "nextAutoAttackAt",
                    0f);
                InvokePrivate(controller, "TickAutoAttack");

                Assert.That(
                    GetPrivateField<bool>(
                        controller,
                        "hasDestination"),
                    Is.True);
                Assert.That(
                    GetPrivateField<bool>(
                        controller,
                        "autoAttackRepeatCommandActive"),
                    Is.True);

                controller.SetAutoAttackEnabled(false);

                Assert.That(
                    GetPrivateField<bool>(
                        controller,
                        "hasDestination"),
                    Is.False);

                controller.SetAutoAttackEnabled(true);
                Assert.That(
                    controller.TryIssueCommand(
                        enemy.transform.position),
                    Is.True);
                controller.CancelCommand();
                SetPrivateField(
                    controller,
                    "nextAutoAttackAt",
                    0f);
                InvokePrivate(controller, "TickAutoAttack");
                generationField.SetValue(
                    enemy,
                    enemy.SpawnGeneration + 1u);

                InvokePrivate(controller, "TickCommand");

                Assert.That(
                    GetPrivateField<bool>(
                        controller,
                        "hasDestination"),
                    Is.False,
                    "A stale repeat command must not retarget its path.");
            }
            finally
            {
                DestroyEnemy(enemy);
                Object.DestroyImmediate(sessionObject);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(serviceObject);
            }
        }

        [Test]
        public void ModeTwo_ManualAttackReplacesPendingAutoRepeat()
        {
            var serviceObject = new GameObject("EnemyWorld");
            var playerObject = new GameObject("Player");
            var sessionObject = new GameObject("Session");
            EnemyBase enemy = null;
            try
            {
                EnemyWorldService service =
                    serviceObject.AddComponent<EnemyWorldService>();
                enemy = CreateLiveEnemy(
                    "CommandTarget",
                    new Vector2(3f, 0f));
                service.Register(enemy);

                PlayerController controller = ConfigureController(
                    playerObject,
                    sessionObject,
                    service,
                    null);
                controller.SetAutoAttackEnabled(true);
                Assert.That(
                    controller.TryIssueCommand(
                        enemy.transform.position),
                    Is.True);
                controller.CancelCommand();
                SetPrivateField(
                    controller,
                    "nextAutoAttackAt",
                    0f);
                InvokePrivate(controller, "TickAutoAttack");

                Assert.That(
                    GetPrivateField<bool>(
                        controller,
                        "autoAttackRepeatCommandActive"),
                    Is.True);
                Assert.That(
                    GetPrivateField<int>(
                        controller,
                        "pendingAttackCount"),
                    Is.EqualTo(1));

                Assert.That(
                    controller.TryIssueCommand(
                        enemy.transform.position),
                    Is.True);
                Assert.That(
                    GetPrivateField<bool>(
                        controller,
                        "autoAttackRepeatCommandActive"),
                    Is.False);
                Assert.That(
                    GetPrivateField<int>(
                        controller,
                        "pendingAttackCount"),
                    Is.EqualTo(1),
                    "Manual input replaces rather than stacks with the " +
                    "pending automatic hit.");

                Assert.That(
                    controller.TryIssueCommand(
                        enemy.transform.position),
                    Is.True);
                Assert.That(
                    GetPrivateField<int>(
                        controller,
                        "pendingAttackCount"),
                    Is.EqualTo(2),
                    "Consecutive manual inputs still have no attack " +
                    "cooldown.");
            }
            finally
            {
                DestroyEnemy(enemy);
                Object.DestroyImmediate(sessionObject);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(serviceObject);
            }
        }

        private static PlayerController ConfigureController(
            GameObject playerObject,
            GameObject sessionObject,
            EnemyWorldService service,
            Camera camera)
        {
            PlayerRoot player =
                playerObject.AddComponent<PlayerRoot>();
            PlayerController controller =
                playerObject.GetComponent<PlayerController>();
            HealthComponent health =
                playerObject.GetComponent<HealthComponent>();
            health.Configure(10);
            SetPrivateField(player, "health", health);
            SetPrivateField(
                player,
                "movement",
                playerObject.GetComponent<PlayerMovement>());
            SetPrivateField(
                player,
                "stats",
                playerObject.GetComponent<PlayerStats>());
            SetPrivateField(
                player,
                "combatAbilities",
                playerObject.GetComponent<PlayerCombatAbilities>());
            PrototypeGameSession session =
                sessionObject.GetComponent<PrototypeGameSession>() ??
                sessionObject.AddComponent<PrototypeGameSession>();
            SetPrivateField(session, "state", GameRunState.Playing);
            controller.Configure(
                player,
                session,
                service,
                camera,
                PlayerController.DefaultAttackRange);
            return controller;
        }

        private static void InvokePrivate(object target, string name)
        {
            MethodInfo method = target.GetType().GetMethod(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, null);
        }

        private static T InvokePrivateWithResult<T>(
            object target,
            string name)
        {
            MethodInfo method = target.GetType().GetMethod(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (T)method.Invoke(target, null);
        }

        private static void SetPrivateField(
            object target,
            string name,
            object value)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (T)field.GetValue(target);
        }

        private static EnemyBase CreateLiveEnemy(
            string name,
            Vector2 position)
        {
            var enemyObject = new GameObject(name);
            enemyObject.transform.position = position;
            EnemyBase enemy =
                enemyObject.AddComponent<EnemyActor>();
            EnemyHealth health =
                enemyObject.GetComponent<EnemyHealth>();
            health.Configure(10);
            FieldInfo healthField = typeof(EnemyBase).GetField(
                "health",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(healthField, Is.Not.Null);
            healthField.SetValue(enemy, health);

            CircleCollider2D collider =
                enemyObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.3f;
            return enemy;
        }

        private static void DestroyEnemy(EnemyBase enemy)
        {
            if (enemy != null)
            {
                Object.DestroyImmediate(enemy.gameObject);
            }
        }
    }
}
