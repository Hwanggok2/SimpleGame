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
        public List<PlayerDefinition> PlayerDefinitions { get; } = new();
        public List<LevelUpCardDefinition> LevelUpCards { get; } = new();
        public List<GameStringEntry> GameStrings { get; } = new();
        public List<ImageDataDefinition> Images { get; } = new();
        public List<LobbyDifficultyDefinition> LobbyDifficulties { get; } =
            new();
        public int AccountExperienceScoreUnit { get; set; }
        public int AccountExperiencePerUnit { get; set; }
        public float CriticalChancePerCard { get; set; }
        public float MaximumCriticalChance { get; set; }
        public int InitialCardRerolls { get; set; }
        public int MaximumStoredCardRerolls { get; set; }
        public int BossRerollReward { get; set; }
    }

    public readonly struct GameDataImportSummary
    {
        public GameDataImportSummary(
            int enemyCount,
            int spawnCount,
            int playerLevelCount,
            int accountLevelCount,
            int stringCount,
            int imageCount,
            int lobbyDifficultyCount)
        {
            EnemyCount = enemyCount;
            SpawnCount = spawnCount;
            PlayerLevelCount = playerLevelCount;
            AccountLevelCount = accountLevelCount;
            StringCount = stringCount;
            ImageCount = imageCount;
            LobbyDifficultyCount = lobbyDifficultyCount;
        }

        public int EnemyCount { get; }
        public int SpawnCount { get; }
        public int PlayerLevelCount { get; }
        public int AccountLevelCount { get; }
        public int StringCount { get; }
        public int ImageCount { get; }
        public int LobbyDifficultyCount { get; }
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
                model,
                GameDifficulty.Normal);
            if (workbook.SheetNames.Any(name =>
                    string.Equals(
                        name,
                        "StageSpawnEasy",
                        StringComparison.OrdinalIgnoreCase)))
            {
                ParseStageSpawn(
                    workbook.ReadSheet("StageSpawnEasy"),
                    model,
                    GameDifficulty.Easy);
            }
            ParseLevelTable(
                workbook.ReadSheet("PlayerLevelExp"),
                model.PlayerLevels);
            ParseLevelTable(
                workbook.ReadSheet("AccountLevelExp"),
                model.AccountLevels);
            ParseGlobalBalance(
                workbook.ReadSheet("GlobalBalance"),
                model);
            ParsePlayerBalance(
                workbook.ReadSheet("PlayerBalance"),
                model);
            model.GameStrings.AddRange(ParseGameStrings(
                workbook.ReadSheet("GameString")));
            ParseImageData(
                workbook.ReadSheet("ImageData"),
                model);
            ParseLobbyDifficulties(
                workbook.ReadSheet("LobbyDifficulty"),
                model);
            ParseLevelUpCards(
                workbook.ReadSheet("LevelUpCard"),
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
                "Score",
                "BaseMaxHp",
                "HpGrowthRate",
                "LevelDifficultyOffset",
                "CombatProfileId",
                "ShowHpBar");
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
                    table.NonNegativeInt(row, "Score"),
                    table.PositiveFloat(row, "BaseMaxHp"),
                    table.NonNegativeFloat(row, "HpGrowthRate"),
                    table.NonNegativeInt(row, "LevelDifficultyOffset"),
                    table.RequiredText(row, "CombatProfileId"),
                    table.Boolean(row, "ShowHpBar")));
            }

            RequireDataRows(table);
        }

        private static void ParseStageSpawn(
            ExcelSheet sheet,
            GameDataExcelModel model,
            GameDifficulty difficulty)
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
                if (!StageSpawnEntry.TryParseWaveNumber(
                        waveId,
                        out _))
                {
                    throw table.Error(
                        row,
                        "WaveId",
                        $"expected WAVE_<positive number>, got '{waveId}'");
                }

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
                    table.PositiveInt(row, "EnemyLevel"),
                    difficulty);
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
                "MaximumCriticalChance",
                "InitialCardRerolls",
                "MaximumStoredCardRerolls",
                "BossRerollReward");
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
            model.InitialCardRerolls =
                table.NonNegativeInt(row, "InitialCardRerolls");
            model.MaximumStoredCardRerolls =
                table.NonNegativeInt(
                    row,
                    "MaximumStoredCardRerolls");
            model.BossRerollReward =
                table.NonNegativeInt(row, "BossRerollReward");
            if (model.MaximumCriticalChance <
                model.CriticalChancePerCard)
            {
                throw table.Error(
                    row,
                    "MaximumCriticalChance",
                    "must be greater than or equal to CriticalChancePerCard");
            }

            if (model.MaximumStoredCardRerolls <
                model.InitialCardRerolls)
            {
                throw table.Error(
                    row,
                    "MaximumStoredCardRerolls",
                    "must be greater than or equal to InitialCardRerolls");
            }
        }

        private static void ParsePlayerBalance(
            ExcelSheet sheet,
            GameDataExcelModel model)
        {
            var table = new ExcelTable(
                sheet,
                "PlayerId",
                "StartLevel",
                "BaseMaxHp",
                "BaseAttackPower",
                "AttackGrowthRate",
                "RearAttackMultiplier",
                "BaseMoveSpeed",
                "PathEnemyApproachSpeedMultiplier",
                "PostKillEscapeSpeedMultiplier",
                "MoveArrivalTolerance",
                "AttackRange",
                "BaseCriticalChance",
                "Enabled");
            var playerIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (ExcelRow row in table.DataRows)
            {
                string playerId = table.RequiredText(row, "PlayerId");
                ValidateIdentifier(sheet.Name, row, "PlayerId", playerId);
                if (!playerIds.Add(playerId))
                {
                    throw table.Error(
                        row,
                        "PlayerId",
                        $"duplicate PlayerId '{playerId}'");
                }

                float baseMoveSpeed = table.PositiveFloat(
                    row,
                    "BaseMoveSpeed");
                float approachMultiplier = table.PositiveFloat(
                    row,
                    "PathEnemyApproachSpeedMultiplier");
                float escapeMultiplier = table.PositiveFloat(
                    row,
                    "PostKillEscapeSpeedMultiplier");
                float arrivalTolerance = table.PositiveFloat(
                    row,
                    "MoveArrivalTolerance");
                if (approachMultiplier < 1f)
                {
                    throw table.Error(
                        row,
                        "PathEnemyApproachSpeedMultiplier",
                        "must be greater than or equal to 1");
                }

                if (escapeMultiplier < approachMultiplier)
                {
                    throw table.Error(
                        row,
                        "PostKillEscapeSpeedMultiplier",
                        "must be greater than or equal to " +
                        "PathEnemyApproachSpeedMultiplier");
                }

                model.PlayerDefinitions.Add(new PlayerDefinition(
                    playerId,
                    table.PositiveInt(row, "StartLevel"),
                    table.PositiveInt(row, "BaseMaxHp"),
                    table.PositiveFloat(row, "BaseAttackPower"),
                    table.NonNegativeFloat(
                        row,
                        "AttackGrowthRate"),
                    table.PositiveFloat(row, "RearAttackMultiplier"),
                    baseMoveSpeed,
                    approachMultiplier,
                    escapeMultiplier,
                    arrivalTolerance,
                    table.PositiveFloat(row, "AttackRange"),
                    table.Rate(row, "BaseCriticalChance"),
                    table.Boolean(row, "Enabled")));
            }

            RequireDataRows(table);
        }

        public static List<GameStringEntry> ParseGameStrings(
            ExcelSheet sheet)
        {
            var table = new ExcelTable(sheet, "StringId", "KoKR");
            var result = new List<GameStringEntry>();
            var stringIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (ExcelRow row in table.DataRows)
            {
                string stringId = table.RequiredText(row, "StringId");
                ValidateIdentifier(sheet.Name, row, "StringId", stringId);
                if (!stringIds.Add(stringId))
                {
                    throw table.Error(
                        row,
                        "StringId",
                        $"duplicate StringId '{stringId}'");
                }

                result.Add(new GameStringEntry(
                    stringId,
                    table.RequiredText(row, "KoKR")));
            }

            RequireDataRows(table);
            return result;
        }

        private static void ParseImageData(
            ExcelSheet sheet,
            GameDataExcelModel model)
        {
            var table = new ExcelTable(sheet, "Id", "FileName");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (ExcelRow row in table.DataRows)
            {
                string id = table.RequiredText(row, "Id");
                ValidateIdentifier(sheet.Name, row, "Id", id);
                if (!ids.Add(id))
                {
                    throw table.Error(
                        row,
                        "Id",
                        $"duplicate Id '{id}'");
                }

                string fileName = table.RequiredText(row, "FileName");
                string extension = Path.GetExtension(fileName);
                if (fileName.IndexOfAny(new[] { '/', '\\' }) >= 0 ||
                    Path.IsPathRooted(fileName) ||
                    !string.Equals(
                        Path.GetFileName(fileName),
                        fileName,
                        StringComparison.Ordinal) ||
                    (!string.Equals(
                         extension,
                         ".png",
                         StringComparison.OrdinalIgnoreCase) &&
                     !string.Equals(
                         extension,
                         ".jpg",
                         StringComparison.OrdinalIgnoreCase) &&
                     !string.Equals(
                         extension,
                         ".jpeg",
                         StringComparison.OrdinalIgnoreCase)))
                {
                    throw table.Error(
                        row,
                        "FileName",
                        "must be a PNG or JPG file name without a path");
                }

                model.Images.Add(new ImageDataDefinition(
                    id,
                    fileName,
                    null));
            }

            RequireDataRows(table);
        }

        private static void ParseLobbyDifficulties(
            ExcelSheet sheet,
            GameDataExcelModel model)
        {
            var table = new ExcelTable(
                sheet,
                "Id",
                "SortOrder",
                "RuntimeDifficulty",
                "StageId",
                "DurationMinutes",
                "EnemyCountReductionPercent",
                "EnemyLevelReductionPercent",
                "ImageId",
                "SelectedDifficultyImageId",
                "SelectedDifficultyImageScale",
                "NameKey",
                "ButtonDescriptionKey",
                "ObjectiveKey",
                "EffectDescriptionKey",
                "IsAvailable");
            var ids = new HashSet<LobbyDifficultyId>();
            var sortOrders = new HashSet<int>();
            foreach (ExcelRow row in table.DataRows)
            {
                LobbyDifficultyId id = table.EnumValue<
                    LobbyDifficultyId>(row, "Id");
                if (!ids.Add(id))
                {
                    throw table.Error(
                        row,
                        "Id",
                        $"duplicate Id '{id}'");
                }

                int sortOrder = table.PositiveInt(row, "SortOrder");
                if (!sortOrders.Add(sortOrder))
                {
                    throw table.Error(
                        row,
                        "SortOrder",
                        $"duplicate SortOrder '{sortOrder}'");
                }

                bool isAvailable = table.Boolean(row, "IsAvailable");
                string runtimeValue =
                    table.OptionalText(row, "RuntimeDifficulty");
                bool hasRuntimeDifficulty = !string.IsNullOrWhiteSpace(
                    runtimeValue);
                GameDifficulty runtimeDifficulty = GameDifficulty.Normal;
                if (hasRuntimeDifficulty &&
                    (!Enum.TryParse(
                         runtimeValue,
                         true,
                         out runtimeDifficulty) ||
                     !Enum.IsDefined(
                         typeof(GameDifficulty),
                         runtimeDifficulty)))
                {
                    throw table.Error(
                        row,
                        "RuntimeDifficulty",
                        $"unknown runtime difficulty '{runtimeValue}'");
                }

                if (isAvailable && !hasRuntimeDifficulty)
                {
                    throw table.Error(
                        row,
                        "RuntimeDifficulty",
                        "is required when IsAvailable is TRUE");
                }

                string stageId = table.OptionalText(row, "StageId");
                if (isAvailable && string.IsNullOrWhiteSpace(stageId))
                {
                    throw table.Error(
                        row,
                        "StageId",
                        "is required when IsAvailable is TRUE");
                }

                int enemyCountReduction = table.NonNegativeInt(
                    row,
                    "EnemyCountReductionPercent");
                int enemyLevelReduction = table.NonNegativeInt(
                    row,
                    "EnemyLevelReductionPercent");
                if (enemyCountReduction > 100 ||
                    enemyLevelReduction > 100)
                {
                    throw table.Error(
                        row,
                        "EnemyCountReductionPercent",
                        "reduction percentages must be between 0 and 100");
                }

                float selectedDifficultyImageScale =
                    table.PositiveFloat(
                        row,
                        "SelectedDifficultyImageScale");
                if (selectedDifficultyImageScale > 3f)
                {
                    throw table.Error(
                        row,
                        "SelectedDifficultyImageScale",
                        "must be greater than 0 and at most 3");
                }

                model.LobbyDifficulties.Add(
                    new LobbyDifficultyDefinition(
                        id,
                        sortOrder,
                        isAvailable,
                        hasRuntimeDifficulty,
                        runtimeDifficulty,
                        table.PositiveInt(row, "DurationMinutes"),
                        stageId,
                        enemyCountReduction,
                        enemyLevelReduction,
                        table.RequiredText(row, "ImageId"),
                        table.RequiredText(
                            row,
                            "SelectedDifficultyImageId"),
                        selectedDifficultyImageScale,
                        table.RequiredText(row, "NameKey"),
                        table.RequiredText(row, "ButtonDescriptionKey"),
                        table.RequiredText(row, "ObjectiveKey"),
                        table.RequiredText(
                            row,
                            "EffectDescriptionKey")));
            }

            RequireDataRows(table);
        }

        private static void ParseLevelUpCards(
            ExcelSheet sheet,
            GameDataExcelModel model)
        {
            var table = new ExcelTable(
                sheet,
                "CardId",
                "NameKey",
                "DescriptionKey",
                "EffectType",
                "TargetStat",
                "Operation",
                "Value",
                "MaxStack",
                "SelectionWeight",
                "MinPlayerLevel",
                "RequiredCardId",
                "FusionIngredientCardIds",
                "Rarity",
                "IconId",
                "Enabled");
            var cardIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (ExcelRow row in table.DataRows)
            {
                string cardId = table.RequiredText(row, "CardId");
                ValidateIdentifier(sheet.Name, row, "CardId", cardId);
                if (!cardIds.Add(cardId))
                {
                    throw table.Error(
                        row,
                        "CardId",
                        $"duplicate CardId '{cardId}'");
                }

                LevelUpCardEffectType effectType = table.EnumValue<
                    LevelUpCardEffectType>(row, "EffectType");
                PlayerStatId targetStat = table.EnumValue<PlayerStatId>(
                    row,
                    "TargetStat");
                StatOperation operation = table.EnumValue<StatOperation>(
                    row,
                    "Operation");
                model.LevelUpCards.Add(new LevelUpCardDefinition(
                    cardId,
                    table.RequiredText(row, "NameKey"),
                    table.RequiredText(row, "DescriptionKey"),
                    effectType,
                    targetStat,
                    operation,
                    table.NonNegativeFloat(row, "Value"),
                    table.PositiveInt(row, "MaxStack"),
                    table.NonNegativeInt(row, "SelectionWeight"),
                    table.PositiveInt(row, "MinPlayerLevel"),
                    table.OptionalText(row, "RequiredCardId"),
                    table.RequiredText(row, "Rarity"),
                    table.RequiredText(row, "IconId"),
                    table.Boolean(row, "Enabled"),
                    table.OptionalText(
                        row,
                        "FusionIngredientCardIds")));
            }

            RequireDataRows(table);
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

            var cardIds = new HashSet<string>(
                model.LevelUpCards.Select(value => value.CardId),
                StringComparer.Ordinal);
            foreach (LevelUpCardDefinition card in model.LevelUpCards)
            {
                if (!string.IsNullOrWhiteSpace(card.RequiredCardId) &&
                    (string.Equals(
                         card.CardId,
                         card.RequiredCardId,
                         StringComparison.Ordinal) ||
                     !cardIds.Contains(card.RequiredCardId)))
                {
                    throw new InvalidDataException(
                        $"LevelUpCard '{card.CardId}' has invalid " +
                        $"RequiredCardId '{card.RequiredCardId}'.");
                }

                IReadOnlyList<string> ingredientIds =
                    card.FusionIngredientCardIds;
                bool hasIngredientIds =
                    !string.IsNullOrWhiteSpace(
                        card.FusionIngredientCardIdsRaw);
                if (hasIngredientIds &&
                    card.FusionIngredientCardIdsRaw
                        .Split('|')
                        .Any(string.IsNullOrWhiteSpace))
                {
                    throw new InvalidDataException(
                        $"LevelUpCard '{card.CardId}' has an empty " +
                        "FusionIngredientCardIds entry.");
                }

                if (card.EffectType == LevelUpCardEffectType.Fusion)
                {
                    if (!hasIngredientIds || ingredientIds.Count < 2)
                    {
                        throw new InvalidDataException(
                            $"LevelUpCard '{card.CardId}' " +
                            "FusionIngredientCardIds must reference at " +
                            "least two ingredient cards.");
                    }
                }
                else if (hasIngredientIds)
                {
                    throw new InvalidDataException(
                        $"LevelUpCard '{card.CardId}' " +
                        "FusionIngredientCardIds must be empty for a " +
                        "non-fusion card.");
                }

                var uniqueIngredientIds = new HashSet<string>(
                    StringComparer.Ordinal);
                foreach (string ingredientId in ingredientIds)
                {
                    if (!uniqueIngredientIds.Add(ingredientId))
                    {
                        throw new InvalidDataException(
                            $"LevelUpCard '{card.CardId}' has duplicate " +
                            "FusionIngredientCardIds entry " +
                            $"'{ingredientId}'.");
                    }

                    if (string.Equals(
                            card.CardId,
                            ingredientId,
                            StringComparison.Ordinal))
                    {
                        throw new InvalidDataException(
                            $"LevelUpCard '{card.CardId}' cannot reference " +
                            "itself in FusionIngredientCardIds.");
                    }

                    if (!cardIds.Contains(ingredientId))
                    {
                        throw new InvalidDataException(
                            $"LevelUpCard '{card.CardId}' references " +
                            $"unknown FusionIngredientCardIds entry " +
                            $"'{ingredientId}'.");
                    }

                    LevelUpCardDefinition ingredient =
                        model.LevelUpCards.Find(value =>
                            string.Equals(
                                value.CardId,
                                ingredientId,
                                StringComparison.Ordinal));
                    if (ingredient.EffectType ==
                        LevelUpCardEffectType.Fusion)
                    {
                        throw new InvalidDataException(
                            $"LevelUpCard '{card.CardId}' cannot use " +
                            $"fusion card '{ingredientId}' in " +
                            "FusionIngredientCardIds.");
                    }
                }
            }

            var gameStringIds = new HashSet<string>(
                model.GameStrings.Select(value => value.StringId),
                StringComparer.Ordinal);
            foreach (string requiredStringId in GameStringIds.RequiredIds)
            {
                if (!gameStringIds.Contains(requiredStringId))
                {
                    throw new InvalidDataException(
                        $"GameString is missing required StringId " +
                        $"'{requiredStringId}'.");
                }
            }

            var imageIds = new HashSet<string>(
                model.Images.Select(value => value.Id),
                StringComparer.Ordinal);
            var lobbyIds = new HashSet<LobbyDifficultyId>();
            foreach (LobbyDifficultyDefinition difficulty in
                     model.LobbyDifficulties)
            {
                lobbyIds.Add(difficulty.Id);
                if (!imageIds.Contains(difficulty.ImageId))
                {
                    throw new InvalidDataException(
                        $"LobbyDifficulty '{difficulty.Id}' references " +
                        $"unknown ImageId '{difficulty.ImageId}'.");
                }

                if (!imageIds.Contains(
                        difficulty.SelectedDifficultyImageId))
                {
                    throw new InvalidDataException(
                        $"LobbyDifficulty '{difficulty.Id}' references " +
                        "unknown SelectedDifficultyImageId " +
                        $"'{difficulty.SelectedDifficultyImageId}'.");
                }

                RequireGameStringReference(
                    gameStringIds,
                    difficulty.Id.ToString(),
                    "NameKey",
                    difficulty.NameKey);
                RequireGameStringReference(
                    gameStringIds,
                    difficulty.Id.ToString(),
                    "ButtonDescriptionKey",
                    difficulty.ButtonDescriptionKey);
                RequireGameStringReference(
                    gameStringIds,
                    difficulty.Id.ToString(),
                    "ObjectiveKey",
                    difficulty.ObjectiveKey);
                RequireGameStringReference(
                    gameStringIds,
                    difficulty.Id.ToString(),
                    "EffectDescriptionKey",
                    difficulty.EffectDescriptionKey);
            }

            foreach (LobbyDifficultyId id in
                     Enum.GetValues(typeof(LobbyDifficultyId)))
            {
                if (!lobbyIds.Contains(id))
                {
                    throw new InvalidDataException(
                        $"LobbyDifficulty is missing required Id '{id}'.");
                }
            }

            foreach (LevelUpCardDefinition card in model.LevelUpCards)
            {
                RequireGameStringReference(
                    gameStringIds,
                    card.CardId,
                    "NameKey",
                    card.NameKey);
                RequireGameStringReference(
                    gameStringIds,
                    card.CardId,
                    "DescriptionKey",
                    card.DescriptionKey);
            }
        }

        private static void RequireGameStringReference(
            ISet<string> gameStringIds,
            string cardId,
            string columnName,
            string stringId)
        {
            if (gameStringIds.Contains(stringId))
            {
                return;
            }

            throw new InvalidDataException(
                $"LevelUpCard '{cardId}' references unknown GameString " +
                $"{columnName} '{stringId}'.");
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
            "Planning/GameData_10min_Balance.xlsx";

        public static string DefaultWorkbookPath =>
            Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                DefaultWorkbookRelativePath));

        [MenuItem("SimpleGame/데이터/엑셀 불러오기", false, 20)]
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

        [MenuItem("SimpleGame/데이터/엑셀 불러오기", true)]
        private static bool CanImportDefaultWorkbook()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem("SimpleGame/데이터/다른 엑셀 불러오기...", false, 21)]
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

        [MenuItem("SimpleGame/데이터/다른 엑셀 불러오기...", true)]
        private static bool CanImportSelectedWorkbook()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        public static GameDataImportSummary ImportFromPath(string path)
        {
            GameDataExcelModel data = GameDataExcelParser.Parse(path);
            GameDataManifest manifest = GameDataAssetBuilder.BuildAssets();
            ValidateUnityReferences(data, manifest);
            List<ImageDataDefinition> resolvedImages =
                ResolveImageAssets(data.Images);

            UnityEngine.Object[] generatedAssets =
            {
                manifest.EnemyBalance,
                manifest.StageSpawnSchedule,
                manifest.PlayerLevelExperience,
                manifest.AccountLevelExperience,
                manifest.GlobalBalance,
                manifest.PlayerBalance,
                manifest.LevelUpCards,
                manifest.GameStrings,
                manifest.ImageData,
                manifest.LobbyDifficulties
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
                data.MaximumCriticalChance,
                data.InitialCardRerolls,
                data.MaximumStoredCardRerolls,
                data.BossRerollReward);
            manifest.PlayerBalance.Configure(data.PlayerDefinitions);
            manifest.LevelUpCards.Configure(data.LevelUpCards);
            manifest.GameStrings.Configure(data.GameStrings);
            manifest.ImageData.Configure(resolvedImages);
            manifest.LobbyDifficulties.Configure(
                data.LobbyDifficulties);

            foreach (UnityEngine.Object asset in generatedAssets)
            {
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            return new GameDataImportSummary(
                data.EnemyDefinitions.Count,
                data.SpawnEntries.Count,
                data.PlayerLevels.Count,
                data.AccountLevels.Count,
                data.GameStrings.Count,
                data.Images.Count,
                data.LobbyDifficulties.Count);
        }

        private static void ImportFromMenu(string path)
        {
            try
            {
                GameDataImportSummary summary = ImportFromPath(path);
                string message =
                    $"엑셀 불러오기 완료: 적 {summary.EnemyCount}종, " +
                    $"스폰 {summary.SpawnCount}개, " +
                    $"플레이어 레벨 {summary.PlayerLevelCount}개, " +
                    $"계정 레벨 {summary.AccountLevelCount}개, " +
                    $"문자열 {summary.StringCount}개, " +
                    $"이미지 {summary.ImageCount}개, " +
                    $"로비 난이도 {summary.LobbyDifficultyCount}개.";
                Debug.Log($"{message}\nSource: {path}");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog(
                        "SimpleGame 데이터 불러오기",
                        message,
                        "확인");
                }
            }
            catch (Exception exception)
            {
                string message =
                    $"엑셀 불러오기에 실패했습니다. 기존 생성 데이터는 유지됩니다." +
                    $"\n\n{exception.Message}";
                Debug.LogError($"{message}\nSource: {path}");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog(
                        "SimpleGame 데이터 불러오기",
                        message,
                        "확인");
                }
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

        private static List<ImageDataDefinition> ResolveImageAssets(
            IEnumerable<ImageDataDefinition> definitions)
        {
            var result = new List<ImageDataDefinition>();
            foreach (ImageDataDefinition definition in definitions)
            {
                string assetPath =
                    $"Assets/Image/{definition.FileName}";
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    assetPath);
                if (sprite == null)
                {
                    throw new InvalidDataException(
                        $"ImageData '{definition.Id}' could not load a " +
                        $"Sprite at '{assetPath}'. Check FileName and " +
                        "texture import type.");
                }

                result.Add(new ImageDataDefinition(
                    definition.Id,
                    definition.FileName,
                    sprite));
            }

            return result;
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

        public string OptionalText(ExcelRow row, string columnName)
        {
            return Read(row, columnName).Trim();
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

        public float PositiveFloat(
            ExcelRow row,
            string columnName)
        {
            float value = ReadFloat(row, columnName);
            if (value <= 0f)
            {
                throw Error(row, columnName, "must be greater than 0");
            }

            return value;
        }

        public int OptionalNonNegativeInt(
            ExcelRow row,
            string columnName,
            int emptyValue)
        {
            string value = Read(row, columnName);
            if (string.IsNullOrWhiteSpace(value))
            {
                return emptyValue;
            }

            int parsed = ReadInt(row, columnName);
            if (parsed < 0)
            {
                throw Error(row, columnName, "must be 0 or greater");
            }

            return parsed;
        }

        public bool Boolean(ExcelRow row, string columnName)
        {
            string value = RequiredText(row, columnName);
            if (string.Equals(value, "1", StringComparison.Ordinal) ||
                string.Equals(
                    value,
                    "true",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(value, "0", StringComparison.Ordinal) ||
                string.Equals(
                    value,
                    "false",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            throw Error(
                row,
                columnName,
                $"'{value}' is not a boolean");
        }

        public T EnumValue<T>(ExcelRow row, string columnName)
            where T : struct, Enum
        {
            string value = RequiredText(row, columnName);
            if (Enum.TryParse(value, true, out T result) &&
                Enum.IsDefined(typeof(T), result))
            {
                return result;
            }

            throw Error(
                row,
                columnName,
                $"unknown {typeof(T).Name} '{value}'");
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
