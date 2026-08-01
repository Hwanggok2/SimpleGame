using NUnit.Framework;
using UnityEngine;

namespace SimpleGame.Tests
{
    public sealed class GameStringTableTests
    {
        [Test]
        public void TryGet_UsesExactOrdinalId()
        {
            GameStringTable table = CreateTable(
                new GameStringEntry("HUD_TITLE", "Title"));

            try
            {
                Assert.That(
                    table.TryGet("HUD_TITLE", out string text),
                    Is.True);
                Assert.That(text, Is.EqualTo("Title"));
                Assert.That(
                    table.TryGet("hud_title", out _),
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(table);
            }
        }

        [Test]
        public void Get_UsesStoredThenCallerThenIdFallback()
        {
            GameStringTable table = CreateTable(
                new GameStringEntry("KNOWN", "Stored"));

            try
            {
                Assert.That(
                    table.Get("KNOWN", "Caller fallback"),
                    Is.EqualTo("Stored"));
                Assert.That(
                    table.Get("MISSING", "Caller fallback"),
                    Is.EqualTo("Caller fallback"));
                Assert.That(
                    table.Get("MISSING"),
                    Is.EqualTo("[MISSING]"));
            }
            finally
            {
                Object.DestroyImmediate(table);
            }
        }

        [Test]
        public void Format_UsesFallbackAndNeverLeaksFormatException()
        {
            GameStringTable table = CreateTable(
                new GameStringEntry("VALID", "HP {0}/{1}"),
                new GameStringEntry("BROKEN", "HP {0"));

            try
            {
                Assert.That(
                    table.Format("VALID", "Fallback {0}", 3, 5),
                    Is.EqualTo("HP 3/5"));
                Assert.That(
                    table.Format("MISSING", "Fallback {0}", 3),
                    Is.EqualTo("Fallback 3"));
                Assert.That(
                    table.Format("BROKEN", "Fallback {0}", 3),
                    Is.EqualTo("Fallback 3"));
                Assert.That(
                    table.Format("BROKEN", "Fallback {0", 3),
                    Is.EqualTo("HP {0"));
            }
            finally
            {
                Object.DestroyImmediate(table);
            }
        }

        [Test]
        public void Configure_InvalidatesLazyLookup()
        {
            GameStringTable table = CreateTable(
                new GameStringEntry("VALUE", "Before"));

            try
            {
                Assert.That(table.Get("VALUE"), Is.EqualTo("Before"));

                table.Configure(new[]
                {
                    new GameStringEntry("VALUE", "After")
                });

                Assert.That(table.Get("VALUE"), Is.EqualTo("After"));
                Assert.That(table.Entries, Has.Count.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(table);
            }
        }

        private static GameStringTable CreateTable(
            params GameStringEntry[] entries)
        {
            GameStringTable table =
                ScriptableObject.CreateInstance<GameStringTable>();
            table.Configure(entries);
            return table;
        }
    }
}
