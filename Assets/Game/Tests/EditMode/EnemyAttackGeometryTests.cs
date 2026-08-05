using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame.Tests.EditMode
{
    public sealed class EnemyAttackGeometryTests
    {
        [Test]
        public void MeleeArea_IncludesFrontAndSide_ButExcludesRear()
        {
            Assert.That(
                EnemyAttackModule.IsInsideMeleeArea(
                    Vector2.zero,
                    Vector2.right,
                    new Vector2(0.8f, 0f),
                    1f),
                Is.True);
            Assert.That(
                EnemyAttackModule.IsInsideMeleeArea(
                    Vector2.zero,
                    Vector2.right,
                    new Vector2(0f, 0.7f),
                    1f),
                Is.True);
            Assert.That(
                EnemyAttackModule.IsInsideMeleeArea(
                    Vector2.zero,
                    Vector2.right,
                    new Vector2(-0.1f, 0f),
                    1f),
                Is.False);
        }

        [Test]
        public void ArrowCollision_UsesTravelSegment()
        {
            Assert.That(
                RangedArrowProjectile.SegmentHitsCircle(
                    Vector2.zero,
                    new Vector2(2f, 0f),
                    new Vector2(1f, 0.2f),
                    0.25f),
                Is.True);
            Assert.That(
                RangedArrowProjectile.SegmentHitsCircle(
                    Vector2.zero,
                    new Vector2(2f, 0f),
                    new Vector2(1f, 0.5f),
                    0.25f),
                Is.False);
        }

        [Test]
        public void BossDash_UsesRequestedSpeedDamageAndStun()
        {
            Assert.That(
                BossAttackModule.DefaultDashSpeedMultiplier,
                Is.EqualTo(5f));
            Assert.That(BossAttackModule.DashDamage, Is.EqualTo(100));
            Assert.That(BossAttackModule.DashStunDuration, Is.EqualTo(0.6f));

            string[] bossPrefabPaths =
            {
                "Assets/Prefab/BossEnemy.prefab",
                "Assets/Prefab/MushroomBoss.prefab",
                "Assets/Prefab/FlyingEyeBoss.prefab",
                "Assets/Prefab/SkeletonBoss.prefab"
            };
            foreach (string path in bossPrefabPaths)
            {
                BossAttackModule attack = AssetDatabase
                    .LoadAssetAtPath<GameObject>(path)
                    ?.GetComponent<BossAttackModule>();
                Assert.That(attack, Is.Not.Null, path);
                Assert.That(
                    attack.DashSpeedMultiplier,
                    Is.EqualTo(5f),
                    path);
            }
        }

        [Test]
        public void RangedEnemyPrefab_ReferencesArrowProjectilePrefab()
        {
            GameObject arrow = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefab/RangedArrow.prefab");
            GameObject rangedEnemy =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefab/RangedEnemy.prefab");
            Assert.That(
                arrow?.GetComponent<RangedArrowProjectile>(),
                Is.Not.Null);
            Assert.That(
                arrow?.GetComponent<SpriteRenderer>()?.sprite,
                Is.Not.Null);
            Assert.That(
                arrow.transform.localScale.x,
                Is.EqualTo(0.22f).Within(0.001f));
            Assert.That(
                arrow.GetComponent<RangedArrowProjectile>().Speed,
                Is.EqualTo(11f));
            EnemyAttackModule attack =
                rangedEnemy?.GetComponent<EnemyAttackModule>();
            Assert.That(attack, Is.Not.Null);
            var serializedAttack = new SerializedObject(attack);
            Assert.That(
                serializedAttack.FindProperty("projectilePrefab")
                    .objectReferenceValue,
                Is.Not.Null);
        }

        [Test]
        public void RangedEnemyPrefab_RangeVisualSizeControlsAttackRange()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefab/RangedEnemy.prefab");
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                EnemyAttackModule attack =
                    instance.GetComponent<EnemyAttackModule>();
                Transform range =
                    instance.transform.Find("RangedAttackRange");

                Assert.That(range, Is.Not.Null);
                Assert.That(
                    range.GetComponent<SpriteRenderer>()?.sprite,
                    Is.Not.Null);
                Assert.That(
                    attack.AttackRange,
                    Is.EqualTo(2.25f).Within(0.001f));

                range.localScale = Vector3.one * 6f;
                Assert.That(
                    attack.AttackRange,
                    Is.EqualTo(3f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void Hud_CreatesBossHealthBarBelowExperienceBar()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefab/PrototypeHUD.prefab");
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                PrototypeHUDView view =
                    instance.GetComponent<PrototypeHUDView>();
                view.Initialize();
                view.SetBossHealth("Test Boss", 30, 40, true);

                Transform bossBar =
                    instance.transform.Find("TopPanel/BossHealthBar");
                Assert.That(bossBar, Is.Not.Null);
                Assert.That(bossBar.gameObject.activeSelf, Is.True);
                Assert.That(
                    bossBar.GetComponent<Slider>().value,
                    Is.EqualTo(0.75f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
