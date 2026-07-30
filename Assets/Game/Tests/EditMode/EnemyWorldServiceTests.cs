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
