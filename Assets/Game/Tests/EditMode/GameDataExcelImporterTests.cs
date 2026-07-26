using System;
using System.IO;
using NUnit.Framework;
using SimpleGameEditor;
using UnityEngine;

namespace SimpleGame.Tests
{
    public sealed class GameDataExcelImporterTests
    {
        [Test]
        public void Parser_ParsesProjectWorkbook()
        {
            string path = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                GameDataExcelImporter.DefaultWorkbookRelativePath));

            GameDataExcelModel model = GameDataExcelParser.Parse(path);

            Assert.That(model.EnemyDefinitions, Has.Count.EqualTo(4));
            Assert.That(model.SpawnEntries, Has.Count.EqualTo(15));
            Assert.That(model.PlayerLevels, Has.Count.EqualTo(20));
            Assert.That(model.AccountLevels, Has.Count.EqualTo(4));
            Assert.That(model.AccountExperienceScoreUnit, Is.EqualTo(5));
            Assert.That(model.CriticalChancePerCard, Is.EqualTo(0.1f));
            Assert.That(model.MaximumCriticalChance, Is.EqualTo(0.7f));
        }

        [Test]
        public void ExcelTable_RejectsFractionalInteger()
        {
            var sheet = new ExcelSheet(
                "Levels",
                new[]
                {
                    new ExcelRow(1, new[] { "Level", "RequiredExp" }),
                    new ExcelRow(2, new[] { "1.5", "10" })
                });
            var table = new ExcelTable(
                sheet,
                "Level",
                "RequiredExp");

            InvalidDataException exception = Assert.Throws<
                InvalidDataException>(() =>
                table.PositiveInt(table.DataRows[0], "Level"));

            StringAssert.Contains("Levels row 2, Level", exception.Message);
        }

        [Test]
        public void ExcelTable_RejectsMissingRequiredColumn()
        {
            var sheet = new ExcelSheet(
                "Levels",
                new[]
                {
                    new ExcelRow(1, new[] { "Level" }),
                    new ExcelRow(2, new[] { "1" })
                });

            InvalidDataException exception = Assert.Throws<
                InvalidDataException>(() =>
                new ExcelTable(sheet, "Level", "RequiredExp"));

            StringAssert.Contains("RequiredExp", exception.Message);
        }
    }
}
