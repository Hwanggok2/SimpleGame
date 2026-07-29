using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SimpleGame.Tests
{
    public sealed class EnemyPoolingTests
    {
        private const string ManifestPath =
            "Assets/Game/Data/GameDataManifest.asset";

        [TestCase("GoblinMelee")]
        [TestCase("GoblinRanged")]
        [TestCase("ShieldSkeleton")]
        [TestCase("GoblinBoss")]
        public void Recycle_ReusesPrefabAndResetsEnemyState(
            string enemyId)
        {
            using var context = new PoolContext();
            EnemyBase first = context.Factory.Spawn(
                enemyId,
                2,
                3,
                Vector2.zero);
            Assert.That(first, Is.Not.Null);
            uint firstGeneration = first.SpawnGeneration;

            first.FaceTowardsImmediate(Vector2.left * 10f);
            EnemyAttackModule attack = first.Attack;
            if (attack != null)
            {
                SetPrivateField(
                    attack,
                    "nextReadyAt",
                    float.MaxValue);
                SetPrivateField(attack, "windingUp", true);
            }

            BossAttackModule bossAttack = first.BossAttack;
            if (bossAttack != null)
            {
                SetPrivateField(
                    bossAttack,
                    "cycleStartedAt",
                    Time.time);
                SetPrivateField(
                    bossAttack,
                    "damageApplied",
                    true);
            }

            EnemyStateMachine stateMachine =
                first.GetComponent<EnemyStateMachine>();
            SetPrivateField(stateMachine, "shielding", true);
            SetPrivateField(stateMachine, "pendingShieldSign", -1);
            SetPrivateField(
                stateMachine,
                "shieldDirectionLockedUntil",
                float.MaxValue);

            context.Factory.Recycle(first);

            Assert.That(first.gameObject.activeSelf, Is.False);
            Assert.That(context.World.Enemies, Is.Empty);
            Assert.That(
                context.Factory.InactiveInstanceCount,
                Is.EqualTo(1));

            EnemyBase reused = context.Factory.Spawn(
                enemyId,
                4,
                5,
                new Vector2(3f, 2f));

            Assert.That(reused, Is.SameAs(first));
            Assert.That(reused.gameObject.activeSelf, Is.True);
            Assert.That(reused.IsAlive, Is.True);
            Assert.That(reused.CurrentHealth, Is.EqualTo(reused.MaxHealth));
            Assert.That(
                reused.GetComponent<Collider2D>().enabled,
                Is.True);
            Assert.That(reused.Level, Is.EqualTo(4));
            Assert.That(reused.WaveNumber, Is.EqualTo(5));
            Assert.That(
                reused.SpawnGeneration,
                Is.Not.EqualTo(firstGeneration));
            Assert.That(
                reused.Facing.Direction.x,
                Is.EqualTo(0f).Within(0.0001f));
            Assert.That(
                reused.Facing.Direction.y,
                Is.EqualTo(-1f).Within(0.0001f));
            Assert.That(context.World.Enemies, Has.Count.EqualTo(1));
            Assert.That(
                context.Factory.InactiveInstanceCount,
                Is.Zero);
            Assert.That(
                context.Factory.ManagedInstanceCount,
                Is.EqualTo(1));

            Animator animator = reused.GetComponent<Animator>();
            Assert.That(
                animator.GetBool(
                    CharacterSpriteAnimator.FaceLeftParameter),
                Is.False);

            if (reused.Attack != null)
            {
                Assert.That(reused.Attack.CanStart, Is.True);
            }

            if (reused.BossAttack != null)
            {
                Assert.That(
                    GetPrivateField<float>(
                        reused.BossAttack,
                        "cycleStartedAt"),
                    Is.EqualTo(-1f));
                Assert.That(
                    GetPrivateField<bool>(
                        reused.BossAttack,
                        "damageApplied"),
                    Is.False);
            }

            Assert.That(
                GetPrivateField<bool>(
                    stateMachine,
                    "shielding"),
                Is.False);
            Assert.That(
                GetPrivateField<int>(
                    stateMachine,
                    "pendingShieldSign"),
                Is.Zero);
            Assert.That(
                GetPrivateField<float>(
                    stateMachine,
                    "shieldDirectionLockedUntil"),
                Is.Zero);
        }

        [Test]
        public void Recycle_RespectsPerPrefabInactiveLimit()
        {
            using var context = new PoolContext();
            SetPrivateField(
                context.Factory,
                "maximumInactivePerPrefab",
                1);

            EnemyBase first = context.Factory.Spawn(
                "GoblinMelee",
                1,
                1,
                Vector2.zero);
            EnemyBase second = context.Factory.Spawn(
                "GoblinMelee",
                1,
                1,
                Vector2.right * 3f);

            context.Factory.Recycle(first);
            context.Factory.Recycle(second);

            Assert.That(
                context.Factory.InactiveInstanceCount,
                Is.EqualTo(1));
            Assert.That(
                context.Factory.ManagedInstanceCount,
                Is.EqualTo(1));

            EnemyBase reused = context.Factory.Spawn(
                "GoblinMelee",
                1,
                2,
                Vector2.up * 2f);
            Assert.That(reused, Is.SameAs(first));
            Assert.That(
                context.Factory.InactiveInstanceCount,
                Is.Zero);
            Assert.That(
                context.Factory.ManagedInstanceCount,
                Is.EqualTo(1));
        }

        [Test]
        public void Spawn_DoesNotReuseDifferentSourcePrefab()
        {
            using var context = new PoolContext();
            EnemyBase melee = context.Factory.Spawn(
                "GoblinMelee",
                1,
                1,
                Vector2.zero);
            context.Factory.Recycle(melee);

            EnemyBase ranged = context.Factory.Spawn(
                "GoblinRanged",
                1,
                1,
                Vector2.right);

            Assert.That(ranged, Is.Not.SameAs(melee));
            Assert.That(ranged.Archetype, Is.EqualTo(
                EnemyArchetype.Ranged));
            Assert.That(
                context.Factory.InactiveInstanceCount,
                Is.EqualTo(1));
            Assert.That(
                context.Factory.ManagedInstanceCount,
                Is.EqualTo(2));
        }

        [Test]
        public void PlayerCommand_ReusedEnemyIsNotIgnoredAsPreviousSpawn()
        {
            using var context = new PoolContext();
            var controllerObject = new GameObject(
                "PlayerControllerTest");
            try
            {
                PlayerController controller =
                    controllerObject.AddComponent<PlayerController>();
                EnemyBase firstSpawn = context.Factory.Spawn(
                    "GoblinMelee",
                    1,
                    1,
                    Vector2.right * 2f);
                uint firstGeneration = firstSpawn.SpawnGeneration;
                SetPrivateField(
                    controller,
                    "ignoredPathEnemy",
                    firstSpawn);
                SetPrivateField(
                    controller,
                    "ignoredPathEnemyGeneration",
                    firstGeneration);

                Assert.That(
                    InvokePrivateMethod<EnemyBase>(
                        controller,
                        "ResolveCurrentIgnoredPathEnemy"),
                    Is.SameAs(firstSpawn));

                context.Factory.Recycle(firstSpawn);
                EnemyBase reused = context.Factory.Spawn(
                    "GoblinMelee",
                    1,
                    2,
                    Vector2.right * 2f);
                Assert.That(reused, Is.SameAs(firstSpawn));
                Assert.That(
                    reused.SpawnGeneration,
                    Is.Not.EqualTo(firstGeneration));

                EnemyBase ignored =
                    InvokePrivateMethod<EnemyBase>(
                        controller,
                        "ResolveCurrentIgnoredPathEnemy");
                EnemyBase pathEnemy =
                    context.World.FindFirstEnemyOnPath(
                        Vector2.zero,
                        Vector2.right * 4f,
                        0.1f,
                        ignored);

                Assert.That(ignored, Is.Null);
                Assert.That(pathEnemy, Is.SameAs(reused));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    controllerObject);
            }
        }

        private static void SetPrivateField<T>(
            object owner,
            string name,
            T value)
        {
            FieldInfo field = owner.GetType().GetField(
                name,
                BindingFlags.Instance |
                BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(owner, value);
        }

        private static T GetPrivateField<T>(
            object owner,
            string name)
        {
            FieldInfo field = owner.GetType().GetField(
                name,
                BindingFlags.Instance |
                BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            return (T)field.GetValue(owner);
        }

        private static T InvokePrivateMethod<T>(
            object owner,
            string name)
        {
            MethodInfo method = owner.GetType().GetMethod(
                name,
                BindingFlags.Instance |
                BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, name);
            return (T)method.Invoke(owner, null);
        }

        private sealed class PoolContext : IDisposable
        {
            private readonly GameObject root;

            public PoolContext()
            {
                root = new GameObject("EnemyPoolTest");
                PrototypeGameSession session =
                    root.AddComponent<PrototypeGameSession>();
                World = root.AddComponent<EnemyWorldService>();
                Factory = root.AddComponent<PrototypeEnemyFactory>();

                var enemyRoot = new GameObject("Enemies");
                enemyRoot.transform.SetParent(root.transform, false);

                GameDataManifest manifest =
                    AssetDatabase.LoadAssetAtPath<GameDataManifest>(
                        ManifestPath);
                Assert.That(manifest, Is.Not.Null);
                Factory.ConfigureAssets(
                    manifest.EnemyAssets,
                    manifest.EnemyBalance);
                Factory.Configure(
                    session,
                    World,
                    enemyRoot.transform);
            }

            public PrototypeEnemyFactory Factory { get; }
            public EnemyWorldService World { get; }

            public void Dispose()
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }
    }
}
