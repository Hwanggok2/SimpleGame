using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace SimpleGame.Tests
{
    public sealed class EnemyAssetCatalogTests
    {
        [Test]
        public void TryGetPrefab_PrefersDirectComponentReference()
        {
            var directObject = new GameObject("DirectEnemy");
            var fallbackObject = new GameObject("FallbackEnemy");
            EnemyActor direct =
                directObject.AddComponent<EnemyActor>();
            fallbackObject.AddComponent<EnemyActor>();
            EnemyAssetCatalog catalog =
                ScriptableObject.CreateInstance<EnemyAssetCatalog>();

            try
            {
                var entry = new EnemyAssetEntry("GoblinMelee", direct);
                SetPrivateField(entry, "prefabObject", fallbackObject);
                catalog.Configure(new[] { entry });

                Assert.That(
                    catalog.TryGetPrefab(
                        "GoblinMelee",
                        out EnemyBase resolved),
                    Is.True);
                Assert.That(resolved, Is.SameAs(direct));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(directObject);
                Object.DestroyImmediate(fallbackObject);
            }
        }

        [Test]
        public void TryGetPrefab_UsesRootObjectWhenComponentReferenceIsLost()
        {
            var prefabObject = new GameObject("CatalogFallbackEnemy");
            EnemyActor enemy = prefabObject.AddComponent<EnemyActor>();
            EnemyAssetCatalog catalog =
                ScriptableObject.CreateInstance<EnemyAssetCatalog>();

            try
            {
                var entry = new EnemyAssetEntry("GoblinMelee", enemy);
                SetPrivateField<EnemyBase>(entry, "prefab", null);
                catalog.Configure(new[] { entry });

                Assert.That(
                    catalog.TryGetPrefab(
                        "GoblinMelee",
                        out EnemyBase resolved),
                    Is.True);
                Assert.That(resolved, Is.SameAs(enemy));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(prefabObject);
            }
        }

        [Test]
        public void TryGetPrefab_ReturnsFalseWhenEveryReferenceIsMissing()
        {
            EnemyAssetCatalog catalog =
                ScriptableObject.CreateInstance<EnemyAssetCatalog>();

            try
            {
                catalog.Configure(new[]
                {
                    new EnemyAssetEntry("GoblinMelee", null)
                });

                Assert.That(
                    catalog.TryGetPrefab(
                        "GoblinMelee",
                        out EnemyBase resolved),
                    Is.False);
                Assert.That(resolved, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(catalog);
            }
        }

        private static void SetPrivateField<T>(
            EnemyAssetEntry entry,
            string name,
            T value)
        {
            FieldInfo field = typeof(EnemyAssetEntry).GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(entry, value);
        }
    }
}
