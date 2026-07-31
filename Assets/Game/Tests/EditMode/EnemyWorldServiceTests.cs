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
                    enemyObject.AddComponent<MeleeEnemy>();

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

                Assert.That(first, Is.SameAs(nearEnemy));
                Assert.That(afterIgnore, Is.SameAs(farEnemy));
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
                    .AddComponent<MeleeEnemy>();
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
                    .AddComponent<MeleeEnemy>();
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
                enemyObject.AddComponent<MeleeEnemy>();
            EnemyHealth health =
                enemyObject.GetComponent<EnemyHealth>();
            health.Configure(10f);
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
