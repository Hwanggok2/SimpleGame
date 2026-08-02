using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SimpleGame.Tests
{
    public sealed class SlashTrailEffectTests
    {
        [Test]
        public void StaticArcPool_ReusesInactiveRendererAndMaterial()
        {
            DestroyStaticArcs();

            try
            {
                SlashTrailEffect.ShowStaticArc(
                    Vector2.zero,
                    Vector2.right);

                SlashTrailEffect first = FindOnlyStaticArc();
                LineRenderer firstLine =
                    first.GetComponent<LineRenderer>();
                Assert.That(firstLine, Is.Not.Null);
                Material firstMaterial = firstLine.sharedMaterial;
                Assert.That(firstMaterial, Is.Not.Null);

                first.gameObject.SetActive(false);
                SlashTrailEffect.ShowStaticArc(
                    Vector2.up,
                    new Vector2(3f, 4f));

                SlashTrailEffect reused = FindOnlyStaticArc();
                LineRenderer reusedLine =
                    reused.GetComponent<LineRenderer>();
                Assert.That(reused, Is.SameAs(first));
                Assert.That(reused.gameObject.activeSelf, Is.True);
                Assert.That(
                    reusedLine.sharedMaterial,
                    Is.SameAs(firstMaterial));
                Assert.That(
                    (Vector2)reusedLine.GetPosition(0),
                    Is.EqualTo(Vector2.up));
                Assert.That(
                    (Vector2)reusedLine.GetPosition(1),
                    Is.EqualTo(new Vector2(3f, 4f)));
            }
            finally
            {
                DestroyStaticArcs();
            }
        }

        private static SlashTrailEffect FindOnlyStaticArc()
        {
            var arcs = new List<SlashTrailEffect>();
            foreach (SlashTrailEffect effect in
                     Resources.FindObjectsOfTypeAll<SlashTrailEffect>())
            {
                if (effect != null &&
                    effect.gameObject.name == "StaticArc")
                {
                    arcs.Add(effect);
                }
            }

            Assert.That(arcs, Has.Count.EqualTo(1));
            return arcs[0];
        }

        private static void DestroyStaticArcs()
        {
            foreach (SlashTrailEffect effect in
                     Resources.FindObjectsOfTypeAll<SlashTrailEffect>())
            {
                if (effect != null &&
                    effect.gameObject.name == "StaticArc")
                {
                    Object.DestroyImmediate(effect.gameObject);
                }
            }
        }
    }
}
