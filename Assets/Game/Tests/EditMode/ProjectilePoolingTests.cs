using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace SimpleGame.Tests
{
    public sealed class ProjectilePoolingTests
    {
        [SetUp]
        public void SetUp()
        {
            ComponentPrefabPool<FilthProjectile>.Clear();
            ComponentPrefabPool<MovingSlashProjectile>.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            ComponentPrefabPool<FilthProjectile>.Clear();
            ComponentPrefabPool<MovingSlashProjectile>.Clear();
        }

        [Test]
        public void FilthPool_ReusesOneInstanceAcrossOneHundredUses()
        {
            FilthProjectile prefab = CreateFilthPrefab();
            FilthProjectile first = null;
            try
            {
                for (int index = 0; index < 100; index++)
                {
                    FilthProjectile instance =
                        ComponentPrefabPool<FilthProjectile>.Acquire(
                            prefab,
                            FilthProjectile.MaximumInactivePoolSize);
                    first ??= instance;
                    Assert.That(instance, Is.SameAs(first));
                    InvokeRecycle(instance);
                }

                PrefabPoolDiagnostics diagnostics =
                    ComponentPrefabPool<FilthProjectile>
                        .GetDiagnostics(prefab);
                Assert.That(diagnostics.CreatedCount, Is.EqualTo(1));
                Assert.That(diagnostics.ReusedCount, Is.EqualTo(99));
                Assert.That(diagnostics.ReleasedCount, Is.EqualTo(100));
                Assert.That(diagnostics.DiscardedCount, Is.Zero);
                Assert.That(diagnostics.ManagedCount, Is.EqualTo(1));
                Assert.That(diagnostics.InactiveCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(prefab.gameObject);
            }
        }

        [Test]
        public void MovingSlashPool_ReusesOneInstanceAcrossOneHundredUses()
        {
            MovingSlashProjectile prefab = CreateMovingSlashPrefab();
            MovingSlashProjectile first = null;
            try
            {
                for (int index = 0; index < 100; index++)
                {
                    MovingSlashProjectile instance =
                        ComponentPrefabPool<MovingSlashProjectile>.Acquire(
                            prefab,
                            MovingSlashProjectile.MaximumInactivePoolSize);
                    first ??= instance;
                    Assert.That(instance, Is.SameAs(first));
                    InvokeRecycle(instance);
                }

                PrefabPoolDiagnostics diagnostics =
                    ComponentPrefabPool<MovingSlashProjectile>
                        .GetDiagnostics(prefab);
                Assert.That(diagnostics.CreatedCount, Is.EqualTo(1));
                Assert.That(diagnostics.ReusedCount, Is.EqualTo(99));
                Assert.That(diagnostics.ReleasedCount, Is.EqualTo(100));
                Assert.That(diagnostics.DiscardedCount, Is.Zero);
                Assert.That(diagnostics.ManagedCount, Is.EqualTo(1));
                Assert.That(diagnostics.InactiveCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(prefab.gameObject);
            }
        }

        [Test]
        public void ProjectileRecycle_ClearsReferencesCollectionsAndVisuals()
        {
            FilthProjectile filthPrefab = CreateFilthPrefab();
            MovingSlashProjectile slashPrefab =
                CreateMovingSlashPrefab();
            var supportRoot = new GameObject("ProjectilePoolSupport");
            try
            {
                PlayerRoot owner =
                    supportRoot.AddComponent<PlayerRoot>();
                EnemyWorldService world =
                    supportRoot.AddComponent<EnemyWorldService>();
                var enemyObject = new GameObject("PoolEnemy");
                enemyObject.transform.SetParent(
                    supportRoot.transform,
                    false);
                EnemyActor enemy =
                    enemyObject.AddComponent<EnemyActor>();

                FilthProjectile filth =
                    ComponentPrefabPool<FilthProjectile>.Acquire(
                        filthPrefab,
                        FilthProjectile.MaximumInactivePoolSize);
                SetPrivateField(filth, "owner", owner);
                SetPrivateField(filth, "enemyWorld", world);
                SetPrivateField(filth, "fieldElapsed", 2f);
                GetPrivateField<List<EnemyBase>>(
                    filth,
                    "damageTargets").Add(enemy);
                GetPrivateField<Dictionary<EnemyBase, uint>>(
                    filth,
                    "staticTriggeredEnemyGenerations")[enemy] = 3u;
                GameObject fieldVisual =
                    GetPrivateField<GameObject>(filth, "fieldVisual");
                SpriteRenderer orbRenderer =
                    GetPrivateField<SpriteRenderer>(
                        filth,
                        "orbRenderer");
                fieldVisual.SetActive(true);
                orbRenderer.gameObject.SetActive(false);

                InvokeRecycle(filth);

                Assert.That(
                    GetPrivateField<PlayerRoot>(filth, "owner"),
                    Is.Null);
                Assert.That(
                    GetPrivateField<EnemyWorldService>(
                        filth,
                        "enemyWorld"),
                    Is.Null);
                Assert.That(
                    GetPrivateField<List<EnemyBase>>(
                        filth,
                        "damageTargets"),
                    Is.Empty);
                Assert.That(
                    GetPrivateField<Dictionary<EnemyBase, uint>>(
                        filth,
                        "staticTriggeredEnemyGenerations"),
                    Is.Empty);
                Assert.That(fieldVisual.activeSelf, Is.False);
                Assert.That(orbRenderer.gameObject.activeSelf, Is.True);

                MovingSlashProjectile slash =
                    ComponentPrefabPool<MovingSlashProjectile>.Acquire(
                        slashPrefab,
                        MovingSlashProjectile.MaximumInactivePoolSize);
                SpriteRenderer slashRenderer =
                    slash.GetComponent<SpriteRenderer>();
                Color originalColor = slashRenderer.color;
                SetPrivateField(slash, "hasBaseRendererColor", true);
                SetPrivateField(slash, "baseRendererColor", originalColor);
                SetPrivateField(slash, "owner", owner);
                SetPrivateField(slash, "enemyWorld", world);
                SetPrivateField(slash, "isFading", true);
                SetPrivateField(slash, "fadeElapsed", 0.08f);
                GetPrivateField<Dictionary<EnemyBase, uint>>(
                    slash,
                    "hitEnemyGenerations")[enemy] = 5u;
                IList candidates =
                    GetPrivateField<IList>(slash, "candidates");
                candidates.Add(CreateHitCandidate(enemy));
                Color fadedColor = originalColor;
                fadedColor.a = 0.1f;
                slashRenderer.color = fadedColor;

                InvokeRecycle(slash);

                Assert.That(
                    GetPrivateField<PlayerRoot>(slash, "owner"),
                    Is.Null);
                Assert.That(
                    GetPrivateField<EnemyWorldService>(
                        slash,
                        "enemyWorld"),
                    Is.Null);
                Assert.That(
                    GetPrivateField<Dictionary<EnemyBase, uint>>(
                        slash,
                        "hitEnemyGenerations"),
                    Is.Empty);
                Assert.That(candidates, Is.Empty);
                Assert.That(
                    GetPrivateField<bool>(slash, "isFading"),
                    Is.False);
                Assert.That(slashRenderer.color, Is.EqualTo(originalColor));
            }
            finally
            {
                Object.DestroyImmediate(supportRoot);
                Object.DestroyImmediate(filthPrefab.gameObject);
                Object.DestroyImmediate(slashPrefab.gameObject);
            }
        }

        [Test]
        public void Pool_DiscardsInstancesBeyondPrefabLimit()
        {
            FilthProjectile prefab = CreateFilthPrefab();
            try
            {
                FilthProjectile first =
                    ComponentPrefabPool<FilthProjectile>.Acquire(prefab, 2);
                FilthProjectile second =
                    ComponentPrefabPool<FilthProjectile>.Acquire(prefab, 2);
                FilthProjectile third =
                    ComponentPrefabPool<FilthProjectile>.Acquire(prefab, 2);

                InvokeRecycle(first);
                InvokeRecycle(second);
                InvokeRecycle(third);

                PrefabPoolDiagnostics diagnostics =
                    ComponentPrefabPool<FilthProjectile>
                        .GetDiagnostics(prefab);
                Assert.That(diagnostics.CreatedCount, Is.EqualTo(3));
                Assert.That(diagnostics.ReleasedCount, Is.EqualTo(3));
                Assert.That(diagnostics.DiscardedCount, Is.EqualTo(1));
                Assert.That(diagnostics.ManagedCount, Is.EqualTo(2));
                Assert.That(diagnostics.InactiveCount, Is.EqualTo(2));
                Assert.That(diagnostics.MaximumInactive, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(prefab.gameObject);
            }
        }

        [Test]
        public void Pool_TrimsExistingInactiveInstancesWhenLimitShrinks()
        {
            FilthProjectile prefab = CreateFilthPrefab();
            try
            {
                FilthProjectile first =
                    ComponentPrefabPool<FilthProjectile>.Acquire(prefab, 3);
                FilthProjectile second =
                    ComponentPrefabPool<FilthProjectile>.Acquire(prefab, 3);
                FilthProjectile third =
                    ComponentPrefabPool<FilthProjectile>.Acquire(prefab, 3);
                InvokeRecycle(first);
                InvokeRecycle(second);
                InvokeRecycle(third);

                FilthProjectile reused =
                    ComponentPrefabPool<FilthProjectile>.Acquire(prefab, 1);
                InvokeRecycle(reused);

                PrefabPoolDiagnostics diagnostics =
                    ComponentPrefabPool<FilthProjectile>
                        .GetDiagnostics(prefab);
                Assert.That(diagnostics.MaximumInactive, Is.EqualTo(1));
                Assert.That(diagnostics.InactiveCount, Is.EqualTo(1));
                Assert.That(diagnostics.ManagedCount, Is.EqualTo(1));
                Assert.That(diagnostics.DiscardedCount, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(prefab.gameObject);
            }
        }

        private static FilthProjectile CreateFilthPrefab()
        {
            var root = new GameObject("FilthPoolPrefab");
            FilthProjectile projectile =
                root.AddComponent<FilthProjectile>();
            var orb = new GameObject("Orb");
            orb.transform.SetParent(root.transform, false);
            SpriteRenderer orbRenderer =
                orb.AddComponent<SpriteRenderer>();
            var field = new GameObject("Field");
            field.transform.SetParent(root.transform, false);
            field.SetActive(false);
            projectile.ConfigureVisuals(orbRenderer, field);
            return projectile;
        }

        private static MovingSlashProjectile CreateMovingSlashPrefab()
        {
            var root = new GameObject("MovingSlashPoolPrefab");
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.color = new Color(0.8f, 0.9f, 1f, 0.75f);
            MovingSlashProjectile projectile =
                root.AddComponent<MovingSlashProjectile>();
            projectile.ConfigureVisuals(
                renderer,
                new Sprite[MovingSlashProjectile.AnimationFrameCount]);
            return projectile;
        }

        private static object CreateHitCandidate(EnemyBase enemy)
        {
            System.Type candidateType =
                typeof(MovingSlashProjectile).GetNestedType(
                    "HitCandidate",
                    BindingFlags.NonPublic);
            Assert.That(candidateType, Is.Not.Null);
            return System.Activator.CreateInstance(
                candidateType,
                enemy,
                1f);
        }

        private static void InvokeRecycle(object projectile)
        {
            MethodInfo method = projectile.GetType().GetMethod(
                "Recycle",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(projectile, null);
        }

        private static void SetPrivateField<T>(
            object owner,
            string name,
            T value)
        {
            FieldInfo field = owner.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(owner, value);
        }

        private static T GetPrivateField<T>(
            object owner,
            string name)
        {
            FieldInfo field = owner.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            return (T)field.GetValue(owner);
        }
    }
}
