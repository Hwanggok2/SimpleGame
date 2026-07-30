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
                0f,
                0f);

            Assert.That(
                (Vector2)mover.transform.position,
                Is.EqualTo(Vector2.zero));
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

            EnemyBase enemy = owner.AddComponent<MeleeEnemy>();
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
            SetPrivateProperty(enemy, "Definition", definition);
            SetPrivateField(enemy, "health", health);
            SetPrivateField(enemy, "facing", facing);
            SetPrivateField(enemy, "movement", movement);
            SetPrivateField(enemy, "stateMachine", stateMachine);
            SetPrivateField(enemy, "characterAnimation", animation);
            SetPrivateField(enemy, "enemyWorld", service);

            health.Configure(10f);
            facing.Configure(0f);
            movement.Configure(0f, animation);
            stateMachine.Configure(enemy);
            service.Register(enemy);
            return enemy;
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
