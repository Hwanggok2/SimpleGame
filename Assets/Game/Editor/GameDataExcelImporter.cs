using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SimpleGame;
using UnityEditor;
using UnityEngine;

namespace SimpleGameEditor
{
    public sealed class GameDataExcelModel
    {
        public List<EnemyDefinition> EnemyDefinitions { get; } = new();
        public List<StageSpawnEntry> SpawnEntries { get; } = new();
        public List<LevelExperienceRow> PlayerLevels { get; } = new();
        public List<LevelExperienceRow> AccountLevels { get; } = new();
        public int AccountExperienceScoreUnit { get; set; }
        public int AccountExperiencePerUnit { get; set; }
        public float CriticalChancePerCard { get; set; }
        public float MaximumCriticalChance { get; set; }
    }

    public readonly struct GameDataImportSummary
    {
        public GameDataImportSummary(
            int enemyCount,
            int spawnCount,
            int playerLevelCount,
            int accountLevelCount)
        {
            EnemyCount = enemyCount;
            SpawnCount = spawnCount;
            PlayerLevelCount = playerLevelCount;
            AccountLevelCount = accountLevelCount;
        }

        public int EnemyCount { get; }
        public int SpawnCount { get; }
        public int PlayerLevelCount { get; }
        public int AccountLevelCount { get; }
    }

    public static class GameDataExcelParser
    {
        private static readonly Regex IdentifierPattern =
            new("^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.CultureInvariant);
        private static readonly Regex SpawnPointPattern =
            new(
                "^(LEFT|RIGHT|TOP|BOTTOM)_[0-9]{2}$",
                RegexOptions.CultureInvariant);

        public static GameDataExcelModel Parse(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    "Excel workbook was not found.",
                    path);
            }

            if (!string.Equals(
                    Path.GetExtension(path),
                    ".xlsx",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "Only .xlsx workbooks are supported.");
            }

            using var workbook = new OpenXmlWorkbookReader(path);
            var model = new GameDataExcelModel();
            ParseEnemyBalance(
                workbook.ReadSheet("EnemyBalance"),
                model);
            ParseStageSpawn(
                workbook.ReadSheet("StageSpawn"),
                model);
            ParseLevelTable(
                workbook.ReadSheet("PlayerLevelExp"),
                model.PlayerLevels);
            ParseLevelTable(
                workbook.ReadSheet("AccountLevelExp"),
                model.AccountLevels);
            ParseGlobalBalance(
                workbook.ReadSheet("GlobalBalance"),
                model);
            ValidateReferences(model);
            return model;
        }

        private static void ParseEnemyBalance(
            ExcelSheet sheet,
            GameDataExcelModel model)
        {
            var table = new ExcelTable(
                sheet,
                "EnemyId",
                "Archetype",
                "MoveSpeed",
                "AttackRange",
                "AttackDamage",
                "AttackWindup",
                "AttackActiveDuration",
                "AttackCooldown",
                "AttackAreaRadius",
                "ApproachRange",
                "FacingTurnDelay",
                "PostAttackFacingLock",
                "KillExperience",
                "Score");
            var enemyIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (ExcelRow row in table.DataRows)
            {
                string enemyId = table.RequiredText(row, "EnemyId");
                ValidateIdentifier(sheet.Name, row, "EnemyId", enemyId);
                if (!enemyIds.Add(enemyId))
                {
                    throw table.Error(
                        row,
                        "EnemyId",
                        $"duplicate EnemyId '{enemyId}'");
                }

                string archetypeValue =
                    table.RequiredText(row, "Archetype");
                if (!Enum.TryParse(
                        archetypeValue,
                        true,
                        out EnemyArchetype archetype) ||
                    !Enum.IsDefined(typeof(EnemyArchetype), archetype))
                {
                    throw table.Error(
                        row,
                        "Archetype",
                        $"unknown archetype '{archetypeValue}'");
                }

                model.EnemyDefinitions.Add(new EnemyDefinition(
                    enemyId,
                    archetype,
                    table.NonNegativeFloat(row, "MoveSpeed"),
                    table.NonNegativeFloat(row, "AttackRange"),
                    table.NonNegativeInt(row, "AttackDamage"),
                    table.NonNegativeFloat(row, "AttackWindup"),
                    table.NonNegativeFloat(row, "AttackActiveDuration"),
                    table.NonNegativeFloat(row, "AttackCooldown"),
                    table.NonNegativeFloat(row, "AttackAreaRadius"),
                    table.NonNegativeFloat(row, "ApproachRange"),
                    table.NonNegativeFloat(row, "FacingTurnDelay"),
                    table.NonNegativeFloat(row, "PostAttackFacingLock"),
                    table.NonNegativeInt(row, "KillExperience"),
                    table.NonNegativeInt(row, "Score")));
            }

            RequireDataRows(table);
        }

        private static void ParseStageSpawn(
            ExcelSheet sheet,
            GameDataExcelModel model)
        {
            var table = new ExcelTable(
                sheet,
                "StageId",
                "WaveId",
                "SpawnTimeSec",
                "SpawnIndex",
                "SpawnPointId",
                "EnemyId",
                "EnemyLevel");
            var runtimeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (ExcelRow row in table.DataRows)
            {
                string stageId = table.RequiredText(row, "StageId");
                string waveId = table.RequiredText(row, "WaveId");
                string spawnPointId =
                    table.RequiredText(row, "SpawnPointId");
                string enemyId = table.RequiredText(row, "EnemyId");
                ValidateIdentifier(sheet.Name, row, "StageId", stageId);
                ValidateIdentifier(sheet.Name, row, "WaveId", waveId);
                ValidateIdentifier(sheet.Name, row, "EnemyId", enemyId);
                if (!model.EnemyDefinitions.Any(definition =>
                        string.Equals(
                            definition.EnemyId,
                            enemyId,
                            StringComparison.Ordinal)))
                {
                    throw table.Error(
                        row,
                        "EnemyId",
                        $"unknown EnemyId '{enemyId}'");
                }

                if (!SpawnPointPattern.IsMatch(spawnPointId))
                {
                    throw table.Error(
                        row,
                        "SpawnPointId",
                        $"invalid spawn point '{spawnPointId}'");
                }

                var entry = new StageSpawnEntry(
                    stageId,
                    waveId,
                    table.NonNegativeFloat(row, "SpawnTimeSec"),
                    table.PositiveInt(row, "SpawnIndex"),
                    spawnPointId,
                    enemyId,
                    table.PositiveInt(row, "EnemyLevel"));
                if (!runtimeIds.Add(entry.RuntimeId))
                {
                    throw table.Error(
                        row,
                        "SpawnIndex",
                        $"duplicate runtime id '{entry.RuntimeId}'");
                }

                model.SpawnEntries.Add(entry);
            }

            RequireDataRows(table);
        }

        private static void ParseLevelTable(
            ExcelSheet sheet,
            List<LevelExperienceRow> destination)
        {
            var table = new ExcelTable(sheet, "Level", "RequiredExp");
            var levels = new HashSet<int>();
            foreach (ExcelRow row in table.DataRows)
            {
                int level = table.PositiveInt(row, "Level");
                if (!levels.Add(level))
                {
                    throw table.Error(
                        row,
                        "Level",
                        $"duplicate level '{level}'");
                }

                destination.Add(new LevelExperienceRow(
                    level,
                    table.NonNegativeInt(row, "RequiredExp")));
            }

            RequireDataRows(table);
            destination.Sort((left, right) =>
                left.Level.CompareTo(right.Level));
            for (int index = 0; index < destination.Count; index++)
            {
                int expectedLevel = index + 1;
                if (destination[index].Level != expectedLevel)
                {
                    throw new InvalidDataException(
                        $"{sheet.Name} levels must be continuous from 1. " +
                        $"Expected {expectedLevel}, found " +
                        $"{destination[index].Level}.");
                }
            }
        }

        private static void ParseGlobalBalance(
            ExcelSheet sheet,
            GameDataExcelModel model)
        {
            var table = new ExcelTable(
                sheet,
                "AccountExpScoreUnit",
                "AccountExpPerUnit",
                "CriticalChancePerCard",
                "MaximumCriticalChance");
            if (table.DataRows.Count != 1)
            {
                throw new InvalidDataException(
                    $"{sheet.Name} must contain exactly one data row.");
            }

            ExcelRow row = table.DataRows[0];
            model.AccountExperienceScoreUnit =
                table.PositiveInt(row, "AccountExpScoreUnit");
            model.AccountExperiencePerUnit =
                table.NonNegativeInt(row, "AccountExpPerUnit");
            model.CriticalChancePerCard =
                table.Rate(row, "CriticalChancePerCard");
            model.MaximumCriticalChance =
                table.Rate(row, "MaximumCriticalChance");
            if (model.MaximumCriticalChance <
                model.CriticalChancePerCard)
            {
                throw table.Error(
                    row,
                    "MaximumCriticalChance",
                    "must be greater than or equal to CriticalChancePerCard");
            }
        }

        private static void ValidateReferences(GameDataExcelModel model)
        {
            var enemyIds = new HashSet<string>(
                model.EnemyDefinitions.Select(value => value.EnemyId),
                StringComparer.Ordinal);
            foreach (StageSpawnEntry entry in model.SpawnEntries)
            {
                if (!enemyIds.Contains(entry.EnemyId))
                {
                    throw new InvalidDataException(
                        $"StageSpawn references unknown EnemyId " +
                        $"'{entry.EnemyId}' in {entry.RuntimeId}.");
                }
            }
        }

        private static void ValidateIdentifier(
            string sheetName,
            ExcelRow row,
            string columnName,
            string value)
        {
            if (!IdentifierPattern.IsMatch(value))
            {
                throw new InvalidDataException(
                    $"{sheetName} row {row.RowNumber}, {columnName}: " +
                    $"'{value}' must start with a letter and contain only " +
                    "letters, numbers, or underscores.");
            }
        }

        private static void RequireDataRows(ExcelTable table)
        {
            if (table.DataRows.Count == 0)
            {
                throw new InvalidDataException(
                    $"{table.SheetName} contains no data rows.");
            }
        }
    }

    public static class GameDataExcelImporter
    {
        public const string DefaultWorkbookRelativePath =
            "Planning/GameData.xlsx";

        public static string DefaultWorkbookPath =>
            Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                DefaultWorkbookRelativePath));

        [MenuItem("SimpleGame/Data/Import Excel", false, 20)]
        public static void ImportDefaultWorkbook()
        {
            string path = DefaultWorkbookPath;
            if (!File.Exists(path))
            {
                path = EditorUtility.OpenFilePanel(
                    "Select SimpleGame data workbook",
                    Path.GetDirectoryName(DefaultWorkbookPath),
                    "xlsx");
                if (string.IsNullOrWhiteSpace(path))
                {
                    return;
                }
            }

            ImportFromMenu(path);
        }

        [MenuItem("SimpleGame/Data/Import Excel", true)]
        private static bool CanImportDefaultWorkbook()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem("SimpleGame/Data/Import Excel From...", false, 21)]
        public static void ImportSelectedWorkbook()
        {
            string path = EditorUtility.OpenFilePanel(
                "Select SimpleGame data workbook",
                Path.GetDirectoryName(DefaultWorkbookPath),
                "xlsx");
            if (!string.IsNullOrWhiteSpace(path))
            {
                ImportFromMenu(path);
            }
        }

        [MenuItem("SimpleGame/Data/Import Excel From...", true)]
        private static bool CanImportSelectedWorkbook()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        public static GameDataImportSummary ImportFromPath(string path)
        {
            GameDataExcelModel data = GameDataExcelParser.Parse(path);
            GameDataManifest manifest = GameDataAssetBuilder.BuildAssets();
            ValidateUnityReferences(data, manifest);

            UnityEngine.Object[] generatedAssets =
            {
                manifest.EnemyBalance,
                manifest.StageSpawnSchedule,
                manifest.PlayerLevelExperience,
                manifest.AccountLevelExperience,
                manifest.GlobalBalance
            };
            Undo.RecordObjects(generatedAssets, "Import Game Data from Excel");

            manifest.EnemyBalance.Configure(data.EnemyDefinitions);
            manifest.StageSpawnSchedule.Configure(data.SpawnEntries);
            manifest.PlayerLevelExperience.Configure(data.PlayerLevels);
            manifest.AccountLevelExperience.Configure(data.AccountLevels);
            manifest.GlobalBalance.Configure(
                data.AccountExperienceScoreUnit,
                data.AccountExperiencePerUnit,
                data.CriticalChancePerCard,
                data.MaximumCriticalChance);

            foreach (UnityEngine.Object asset in generatedAssets)
            {
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            return new GameDataImportSummary(
                data.EnemyDefinitions.Count,
                data.SpawnEntries.Count,
                data.PlayerLevels.Count,
                data.AccountLevels.Count);
        }

        private static void ImportFromMenu(string path)
        {
            try
            {
                GameDataImportSummary summary = ImportFromPath(path);
                string message =
                    $"Excel import complete: {summary.EnemyCount} enemies, " +
                    $"{summary.SpawnCount} spawns, " +
                    $"{summary.PlayerLevelCount} player levels, " +
                    $"{summary.AccountLevelCount} account levels.";
                Debug.Log($"{message}\nSource: {path}");
                EditorUtility.DisplayDialog(
                    "SimpleGame Data Import",
                    message,
                    "OK");
            }
            catch (Exception exception)
            {
                string message =
                    $"Excel import failed. Existing generated data was kept." +
                    $"\n\n{exception.Message}";
                Debug.LogError($"{message}\nSource: {path}");
                EditorUtility.DisplayDialog(
                    "SimpleGame Data Import",
                    message,
                    "OK");
            }
        }

        private static void ValidateUnityReferences(
            GameDataExcelModel data,
            GameDataManifest manifest)
        {
            foreach (EnemyDefinition definition in data.EnemyDefinitions)
            {
                if (!manifest.EnemyAssets.TryGetPrefab(
                        definition.EnemyId,
                        out EnemyBase prefab))
                {
                    throw new InvalidDataException(
                        $"EnemyId '{definition.EnemyId}' has no prefab " +
                        "mapping in EnemyAssetCatalog.");
                }

                if (prefab.Archetype != definition.Archetype)
                {
                    throw new InvalidDataException(
                        $"EnemyId '{definition.EnemyId}' uses archetype " +
                        $"{definition.Archetype}, but its prefab uses " +
                        $"{prefab.Archetype}.");
                }
            }

            SpawnPointRegistry registry =
                UnityEngine.Object.FindAnyObjectByType<SpawnPointRegistry>(
                    FindObjectsInactive.Include);
            if (registry == null)
            {
                return;
            }

            foreach (string spawnPointId in data.SpawnEntries
                         .Select(entry => entry.SpawnPointId)
                         .Distinct(StringComparer.Ordinal))
            {
                if (!registry.TryGet(spawnPointId, out _))
                {
                    throw new InvalidDataException(
                        $"SpawnPointId '{spawnPointId}' does not exist in " +
                        "the active scene SpawnPointRegistry.");
                }
            }
        }
    }

    public sealed class ExcelTable
    {
        private readonly Dictionary<string, int> columns;

        public ExcelTable(
            ExcelSheet sheet,
            params string[] requiredColumns)
        {
            SheetName = sheet.Name;
            ExcelRow header = sheet.Rows.FirstOrDefault(row => !row.IsEmpty);
            if (header == null)
            {
                throw new InvalidDataException(
                    $"{sheet.Name} is empty.");
            }

            columns = new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < header.Cells.Count; index++)
            {
                string name = header.GetCell(index);
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (!columns.TryAdd(name, index))
                {
                    throw new InvalidDataException(
                        $"{sheet.Name} row {header.RowNumber}: " +
                        $"duplicate column '{name}'.");
                }
            }

            foreach (string requiredColumn in requiredColumns)
            {
                if (!columns.ContainsKey(requiredColumn))
                {
                    throw new InvalidDataException(
                        $"{sheet.Name} is missing required column " +
                        $"'{requiredColumn}'.");
                }
            }

            DataRows = sheet.Rows
                .Where(row => row.RowNumber > header.RowNumber && !row.IsEmpty)
                .ToList();
        }

        public string SheetName { get; }
        public IReadOnlyList<ExcelRow> DataRows { get; }

        public string RequiredText(ExcelRow row, string columnName)
        {
            string value = Read(row, columnName);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw Error(row, columnName, "value is required");
            }

            return value;
        }

        public int PositiveInt(ExcelRow row, string columnName)
        {
            int value = ReadInt(row, columnName);
            if (value < 1)
            {
                throw Error(row, columnName, "must be at least 1");
            }

            return value;
        }

        public int NonNegativeInt(ExcelRow row, string columnName)
        {
            int value = ReadInt(row, columnName);
            if (value < 0)
            {
                throw Error(row, columnName, "must be 0 or greater");
            }

            return value;
        }

        public float NonNegativeFloat(
            ExcelRow row,
            string columnName)
        {
            float value = ReadFloat(row, columnName);
            if (value < 0f)
            {
                throw Error(row, columnName, "must be 0 or greater");
            }

            return value;
        }

        public float Rate(ExcelRow row, string columnName)
        {
            float value = ReadFloat(row, columnName);
            if (value < 0f || value > 1f)
            {
                throw Error(
                    row,
                    columnName,
                    "must be between 0 and 1 (10% = 0.1)");
            }

            return value;
        }

        public InvalidDataException Error(
            ExcelRow row,
            string columnName,
            string message)
        {
            return new InvalidDataException(
                $"{SheetName} row {row.RowNumber}, {columnName}: {message}.");
        }

        private string Read(ExcelRow row, string columnName)
        {
            return row.GetCell(columns[columnName]);
        }

        private int ReadInt(ExcelRow row, string columnName)
        {
            string value = RequiredText(row, columnName);
            if (!double.TryParse(
                    value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double number) ||
                double.IsNaN(number) ||
                double.IsInfinity(number) ||
                Math.Abs(number - Math.Round(number)) > 0.000001d ||
                number < int.MinValue ||
                number > int.MaxValue)
            {
                throw Error(row, columnName, $"'{value}' is not an integer");
            }

            return (int)Math.Round(number);
        }

        private float ReadFloat(ExcelRow row, string columnName)
        {
            string value = RequiredText(row, columnName);
            if (!float.TryParse(
                    value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float number) ||
                float.IsNaN(number) ||
                float.IsInfinity(number))
            {
                throw Error(row, columnName, $"'{value}' is not a number");
            }

            return number;
        }
    }
}
