using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace SimpleGame.Tests
{
    public sealed class EnemyOverlapPolicyTests
    {
        private readonly List<GameObject> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    Object.DestroyImmediate(createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void FindOpenPosition_OverlapAllowedIncomingUsesRequestedPosition()
        {
            EnemyWorldService service = CreateService();
            CreateLiveEnemy(
                service,
                "Ground",
                "GoblinMelee",
                Vector2.zero);

            Vector2 resolved = service.FindOpenEnemyPosition(
                Vector2.zero,
                0.3f,
                incomingAllowsEnemyOverlap: true);

            Assert.That(resolved, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void FindOpenPosition_GroundEnemyIgnoresExistingFlyingEnemy()
        {
            EnemyWorldService service = CreateService();
            CreateLiveEnemy(
                service,
                "Flying",
                PrototypeEnemyDefinitions.FlyingEyeId,
                Vector2.zero);

            Vector2 resolved = service.FindOpenEnemyPosition(
                Vector2.zero,
                0.3f);

            Assert.That(resolved, Is.EqualTo(Vector2.zero));
        }

        [TestCase(
            PrototypeEnemyDefinitions.FlyingEyeId,
            "GoblinMelee")]
        [TestCase(
            PrototypeEnemyDefinitions.FlyingEyeBossId,
            "GoblinMelee")]
        [TestCase(
            "GoblinMelee",
            PrototypeEnemyDefinitions.FlyingEyeId)]
        public void SeparateEnemy_DoesNotSeparatePairContainingFlyingEnemy(
            string moverId,
            string otherId)
        {
            EnemyWorldService service = CreateService();
            EnemyBase mover = CreateLiveEnemy(
                service,
                "Mover",
                moverId,
                Vector2.zero);
            CreateLiveEnemy(
                service,
                "Other",
                otherId,
                Vector2.zero);

            service.SeparateEnemy(mover);

            Assert.That(
                (Vector2)mover.transform.position,
                Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void SeparateEnemy_StillSeparatesTwoGroundEnemies()
        {
            EnemyWorldService service = CreateService();
            EnemyBase mover = CreateLiveEnemy(
                service,
                "Mover",
                "GoblinMelee",
                Vector2.zero);
            EnemyBase other = CreateLiveEnemy(
                service,
                "Other",
                "GoblinRanged",
                Vector2.right * 0.1f);

            service.SeparateEnemy(mover);

            Assert.That(
                Vector2.Distance(
                    mover.transform.position,
                    other.transform.position),
                Is.EqualTo(0.68f).Within(0.001f));
        }

        [TestCase(
            PrototypeEnemyDefinitions.FlyingEyeId,
            "GoblinMelee")]
        [TestCase(
            "GoblinMelee",
            PrototypeEnemyDefinitions.FlyingEyeId)]
        public void Reposition_AllowsEitherEnemyToSharePosition(
            string moverId,
            string otherId)
        {
            EnemyWorldService service = CreateService();
            EnemyBase mover = CreateLiveEnemy(
                service,
                "Mover",
                moverId,
                Vector2.right * 2f);
            CreateLiveEnemy(
                service,
                "Other",
                otherId,
                Vector2.zero);

            mover.Reposition(Vector2.zero, Vector2.right);

            Assert.That(
                (Vector2)mover.transform.position,
                Is.EqualTo(Vector2.zero));
        }

        [TestCase(
            PrototypeEnemyDefinitions.FlyingEyeId,
            "GoblinMelee")]
        [TestCase(
            "GoblinMelee",
            PrototypeEnemyDefinitions.FlyingEyeId)]
        public void ContinuePush_AllowsEitherEnemyToShareDestination(
            string moverId,
            string otherId)
        {
            EnemyWorldService service = CreateService();
            EnemyBase mover = CreateLiveEnemy(
                service,
                "Mover",
                moverId,
                Vector2.right * 2f);
            CreateLiveEnemy(
                service,
                "Other",
                otherId,
                Vector2.zero);

            mover.ApplyContinuePush(
                Vector2.zero,
                Vector2.left,
                0,
                0f);

            Assert.That(
                (Vector2)mover.transform.position,
                Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void SeparateEnemy_SpatialHashMatchesLegacyTwoPassResult()
        {
            EnemyWorldService service = CreateService();
            EnemyBase mover = CreateLiveEnemy(
                service,
                "Mover",
                "GoblinMelee",
                Vector2.zero);
            EnemyBase[] others =
            {
                CreateLiveEnemy(
                    service,
                    "Right",
                    "GoblinRanged",
                    new Vector2(0.1f, 0f)),
                CreateLiveEnemy(
                    service,
                    "Left",
                    "GoblinMelee",
                    new Vector2(-0.18f, 0.08f)),
                CreateLiveEnemy(
                    service,
                    "Above",
                    "GoblinRanged",
                    new Vector2(0.04f, 0.24f)),
                CreateLiveEnemy(
                    service,
                    "Far",
                    "GoblinMelee",
                    new Vector2(8f, 8f))
            };
            Vector2 expected = ResolveWithLegacyFullScan(
                mover,
                others);

            service.SeparateEnemy(mover);

            Assert.That(
                Vector2.Distance(
                    mover.transform.position,
                    expected),
                Is.LessThan(0.0001f));
        }

        [Test]
        public void SeparateEnemy_DetectsOverlapAcrossCellBoundary()
        {
            EnemyWorldService service = CreateService();
            EnemyBase mover = CreateLiveEnemy(
                service,
                "Mover",
                "GoblinMelee",
                new Vector2(1.95f, 0f));
            EnemyBase other = CreateLiveEnemy(
                service,
                "Other",
                "GoblinRanged",
                new Vector2(2.05f, 0f));

            service.SeparateEnemy(mover);

            Assert.That(
                Vector2.Distance(
                    mover.transform.position,
                    other.transform.position),
                Is.EqualTo(0.68f).Within(0.001f));
        }

        [Test]
        public void SeparateEnemy_LargeColliderOccupiesEveryCoveredCell()
        {
            EnemyWorldService service = CreateService();
            EnemyBase mover = CreateLiveEnemy(
                service,
                "Mover",
                "GoblinMelee",
                new Vector2(0.05f, 0f));
            EnemyBase boss = CreateLiveEnemy(
                service,
                "Boss",
                "GoblinMelee",
                new Vector2(1.45f, 0f));
            boss.GetComponent<CircleCollider2D>().radius = 1.2f;
            service.NotifyPositionChanged(boss);

            service.SeparateEnemy(mover);

            Assert.That(
                Vector2.Distance(
                    mover.transform.position,
                    boss.transform.position),
                Is.EqualTo(1.58f).Within(0.001f));
        }

        [Test]
        public void Reposition_MovesEnemyBetweenSpatialBuckets()
        {
            EnemyWorldService service = CreateService();
            EnemyBase other = CreateLiveEnemy(
                service,
                "Other",
                "GoblinRanged",
                Vector2.zero);
            other.Reposition(
                new Vector2(10.1f, 0f),
                Vector2.zero);
            EnemyBase mover = CreateLiveEnemy(
                service,
                "Mover",
                "GoblinMelee",
                new Vector2(10f, 0f));

            service.SeparateEnemy(mover);

            Assert.That(
                Vector2.Distance(
                    mover.transform.position,
                    other.transform.position),
                Is.EqualTo(0.68f).Within(0.001f));
        }

        [Test]
        public void Knockback_MovesEnemyBetweenSpatialBuckets()
        {
            EnemyWorldService service = CreateService();
            EnemyBase pushed = CreateLiveEnemy(
                service,
                "Pushed",
                "GoblinRanged",
                Vector2.zero);
            pushed.ApplyContinuePush(
                new Vector2(20.1f, 0f),
                Vector2.left,
                0,
                0f);
            EnemyBase mover = CreateLiveEnemy(
                service,
                "Mover",
                "GoblinMelee",
                new Vector2(20f, 0f));

            service.SeparateEnemy(mover);

            Assert.That(
                Vector2.Distance(
                    mover.transform.position,
                    pushed.transform.position),
                Is.EqualTo(0.68f).Within(0.001f));
        }

        [Test]
        public void SeparateEnemy_SparseWorldChecksOnlyLocalCandidates()
        {
            EnemyWorldService service = CreateService();
            EnemyBase mover = CreateLiveEnemy(
                service,
                "Mover",
                "GoblinMelee",
                Vector2.zero);
            CreateLiveEnemy(
                service,
                "Near",
                "GoblinRanged",
                new Vector2(0.1f, 0f));
            const int farEnemyCount = 100;
            for (int index = 0; index < farEnemyCount; index++)
            {
                CreateLiveEnemy(
                    service,
                    $"Far_{index:000}",
                    "GoblinMelee",
                    new Vector2(10f + index * 3f, 5f));
            }

            service.SeparateEnemy(mover);

            int legacyCheckCount =
                (service.Enemies.Count - 1) * 2;
            Assert.That(legacyCheckCount, Is.EqualTo(202));
            Assert.That(
                service.LastSeparationCandidateCheckCount,
                Is.LessThanOrEqualTo(4));
            Assert.That(
                1f -
                service.LastSeparationCandidateCheckCount /
                (float)legacyCheckCount,
                Is.GreaterThan(0.98f));
        }

        [Test]
        public void Unregister_RemovesEnemyFromSpatialIndex()
        {
            EnemyWorldService service = CreateService();
            EnemyBase enemy = CreateLiveEnemy(
                service,
                "PooledEnemy",
                "GoblinMelee",
                Vector2.zero);
            Assert.That(service.TrackedSpatialEntryCount, Is.EqualTo(1));
            Assert.That(service.ActiveSpatialBucketCount, Is.GreaterThan(0));

            service.Unregister(enemy);

            Assert.That(service.TrackedSpatialEntryCount, Is.Zero);
            Assert.That(service.ActiveSpatialBucketCount, Is.Zero);
        }

        private EnemyWorldService CreateService()
        {
            var owner = new GameObject("EnemyWorld");
            createdObjects.Add(owner);
            return owner.AddComponent<EnemyWorldService>();
        }

        private EnemyBase CreateLiveEnemy(
            EnemyWorldService service,
            string name,
            string enemyId,
            Vector2 position)
        {
            var owner = new GameObject(name);
            createdObjects.Add(owner);
            owner.transform.position = position;

            EnemyActor enemy = owner.AddComponent<EnemyActor>();
            EnemyHealth health = owner.GetComponent<EnemyHealth>();
            EnemyFacing facing = owner.GetComponent<EnemyFacing>();
            EnemyMovement movement = owner.GetComponent<EnemyMovement>();
            EnemyStateMachine stateMachine =
                owner.GetComponent<EnemyStateMachine>();
            CharacterSpriteAnimator animation =
                owner.AddComponent<CharacterSpriteAnimator>();
            CircleCollider2D collider =
                owner.AddComponent<CircleCollider2D>();
            collider.radius = 0.3f;

            EnemyDefinition definition = CreateDefinition(enemyId);
            enemy.ConfigureArchetype(definition.Archetype);
            SetPrivateProperty(enemy, "Definition", definition);
            SetPrivateField(enemy, "health", health);
            SetPrivateField(enemy, "facing", facing);
            SetPrivateField(enemy, "movement", movement);
            SetPrivateField(enemy, "stateMachine", stateMachine);
            SetPrivateField(enemy, "characterAnimation", animation);
            SetPrivateField(enemy, "enemyWorld", service);

            health.Configure(10);
            facing.Configure(0f);
            movement.Configure(0f, animation);
            stateMachine.Configure(enemy);
            service.Register(enemy);
            return enemy;
        }

        private static Vector2 ResolveWithLegacyFullScan(
            EnemyBase mover,
            IReadOnlyList<EnemyBase> others)
        {
            Vector2 resolved = mover.transform.position;
            float moverRadius = mover.CollisionRadius;
            for (int pass = 0; pass < 2; pass++)
            {
                foreach (EnemyBase other in others)
                {
                    resolved = CombatGeometry.PushOutside(
                        resolved,
                        mover.GetInstanceID(),
                        other.transform.position,
                        other.GetInstanceID(),
                        moverRadius +
                        other.CollisionRadius +
                        0.08f);
                }
            }

            return resolved;
        }

        private static EnemyDefinition CreateDefinition(string enemyId)
        {
            EnemyArchetype archetype =
                enemyId == PrototypeEnemyDefinitions.FlyingEyeBossId
                    ? EnemyArchetype.Boss
                    : EnemyArchetype.Melee;
            return new EnemyDefinition(
                enemyId,
                archetype,
                0f,
                1f,
                1,
                0f,
                0f,
                1f,
                0f,
                0f,
                0f,
                0f,
                0,
                0,
                10f,
                0f,
                0,
                "Test",
                true);
        }

        private static void SetPrivateField(
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field = target.GetType().BaseType?.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static void SetPrivateProperty(
            object target,
            string propertyName,
            object value)
        {
            PropertyInfo property = target.GetType().BaseType?.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            MethodInfo setter = property?.GetSetMethod(true);
            Assert.That(setter, Is.Not.Null, propertyName);
            setter.Invoke(target, new[] { value });
        }
    }
}
