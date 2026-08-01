using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame.Tests
{
    public sealed class PlayerHealthBarTests
    {
        [Test]
        public void HealthComponent_ReportsEveryHealthRatioChange()
        {
            var root = new GameObject("HealthOwner");
            try
            {
                HealthComponent health =
                    root.AddComponent<HealthComponent>();
                var changes = new List<Vector2Int>();
                health.Changed += (current, maximum) =>
                    changes.Add(new Vector2Int(current, maximum));

                health.Configure(100);
                health.ApplyDamage(25);
                health.Heal(10);
                health.IncreaseMaximum(20, true);
                health.RestoreFraction(0.5f);
                health.RestoreFull();

                Assert.That(changes, Is.EqualTo(new[]
                {
                    new Vector2Int(100, 100),
                    new Vector2Int(75, 100),
                    new Vector2Int(85, 100),
                    new Vector2Int(105, 120),
                    new Vector2Int(60, 120),
                    new Vector2Int(120, 120)
                }));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PlayerHealthBar_TracksRatioAndRemainsVisible()
        {
            var root = new GameObject("Player");
            var visual = new GameObject("HealthBarCanvas");
            var sliderObject = new GameObject(
                "HealthSlider",
                typeof(RectTransform),
                typeof(Slider));
            try
            {
                visual.transform.SetParent(root.transform, false);
                sliderObject.transform.SetParent(
                    visual.transform,
                    false);
                HealthComponent health =
                    root.AddComponent<HealthComponent>();
                PlayerHealthBar healthBar =
                    root.AddComponent<PlayerHealthBar>();
                Slider slider = sliderObject.GetComponent<Slider>();
                healthBar.Configure(visual, slider);
                visual.SetActive(false);

                health.Configure(100);
                healthBar.Bind(health);
                health.ApplyDamage(25);

                Assert.That(visual.activeSelf, Is.True);
                Assert.That(slider.value, Is.EqualTo(0.75f));

                health.IncreaseMaximum(50, false);
                Assert.That(slider.value, Is.EqualTo(0.5f));

                health.RestoreFull();
                Assert.That(slider.value, Is.EqualTo(1f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PlayerPrefab_AuthorsNumericFreeHealthBarBelowPlayer()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                SimpleGameEditor.CharacterAssetBuilder.PlayerPrefabPath);

            Assert.That(prefab, Is.Not.Null);
            PlayerHealthBar healthBar =
                prefab.GetComponent<PlayerHealthBar>();
            Assert.That(healthBar, Is.Not.Null);

            Transform canvasTransform =
                prefab.transform.Find("HealthBarCanvas");
            Assert.That(canvasTransform, Is.Not.Null);
            Assert.That(canvasTransform.localPosition.y, Is.LessThan(0f));
            Assert.That(
                canvasTransform.GetComponent<Canvas>().renderMode,
                Is.EqualTo(RenderMode.WorldSpace));

            Slider slider = canvasTransform.Find("HealthSlider")
                ?.GetComponent<Slider>();
            Assert.That(slider, Is.Not.Null);
            Assert.That(slider.value, Is.EqualTo(1f));
            Assert.That(
                canvasTransform.GetComponentInChildren<TMP_Text>(true),
                Is.Null);

            FieldInfo healthBarField = typeof(PlayerRoot).GetField(
                "healthBar",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(healthBarField, Is.Not.Null);
            Assert.That(
                healthBarField.GetValue(prefab.GetComponent<PlayerRoot>()),
                Is.SameAs(healthBar));
        }
    }
}
