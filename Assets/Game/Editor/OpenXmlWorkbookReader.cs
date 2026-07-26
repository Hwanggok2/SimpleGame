using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace SimpleGameEditor
{
    public sealed class ExcelRow
    {
        private readonly IReadOnlyList<string> cells;

        public ExcelRow(int rowNumber, IReadOnlyList<string> cells)
        {
            RowNumber = rowNumber;
            this.cells = cells;
        }

        public int RowNumber { get; }
        public IReadOnlyList<string> Cells => cells;

        public string GetCell(int columnIndex)
        {
            return columnIndex >= 0 && columnIndex < cells.Count
                ? cells[columnIndex]?.Trim() ?? string.Empty
                : string.Empty;
        }

        public bool IsEmpty => cells.All(string.IsNullOrWhiteSpace);
    }

    public sealed class ExcelSheet
    {
        public ExcelSheet(string name, IReadOnlyList<ExcelRow> rows)
        {
            Name = name;
            Rows = rows;
        }

        public string Name { get; }
        public IReadOnlyList<ExcelRow> Rows { get; }
    }

    public sealed class OpenXmlWorkbookReader : IDisposable
    {
        private static readonly XNamespace SpreadsheetNamespace =
            "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace DocumentRelationshipNamespace =
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly XNamespace PackageRelationshipNamespace =
            "http://schemas.openxmlformats.org/package/2006/relationships";

        private readonly FileStream stream;
        private readonly ZipArchive archive;
        private readonly IReadOnlyList<string> sharedStrings;
        private readonly Dictionary<string, string> sheetParts;

        public OpenXmlWorkbookReader(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(
                    "Excel workbook path is empty.",
                    nameof(path));
            }

            stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            try
            {
                archive = new ZipArchive(
                    stream,
                    ZipArchiveMode.Read,
                    false);
                sharedStrings = ReadSharedStrings();
                sheetParts = ReadSheetParts();
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        public IReadOnlyCollection<string> SheetNames => sheetParts.Keys;

        public ExcelSheet ReadSheet(string sheetName)
        {
            string actualName = sheetParts.Keys.FirstOrDefault(candidate =>
                string.Equals(
                    candidate,
                    sheetName,
                    StringComparison.OrdinalIgnoreCase));
            if (actualName == null)
            {
                throw new InvalidDataException(
                    $"Required sheet '{sheetName}' was not found. " +
                    $"Available sheets: {string.Join(", ", SheetNames)}");
            }

            XDocument document = ReadXml(sheetParts[actualName]);
            var rows = new List<ExcelRow>();
            foreach (XElement rowElement in document
                         .Descendants(SpreadsheetNamespace + "row"))
            {
                int rowNumber = ReadPositiveInteger(
                    rowElement.Attribute("r")?.Value,
                    rows.Count + 1);
                var values = new SortedDictionary<int, string>();
                foreach (XElement cell in rowElement.Elements(
                             SpreadsheetNamespace + "c"))
                {
                    string reference = cell.Attribute("r")?.Value;
                    int columnIndex = GetColumnIndex(reference);
                    values[columnIndex] = ReadCellValue(
                        actualName,
                        reference,
                        cell);
                }

                int columnCount = values.Count == 0
                    ? 0
                    : values.Keys.Max() + 1;
                var cells = Enumerable
                    .Repeat(string.Empty, columnCount)
                    .ToArray();
                foreach (KeyValuePair<int, string> value in values)
                {
                    cells[value.Key] = value.Value;
                }

                rows.Add(new ExcelRow(rowNumber, cells));
            }

            return new ExcelSheet(actualName, rows);
        }

        public void Dispose()
        {
            archive.Dispose();
            stream.Dispose();
        }

        private IReadOnlyList<string> ReadSharedStrings()
        {
            ZipArchiveEntry entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
            {
                return Array.Empty<string>();
            }

            XDocument document = ReadXml(entry);
            return document
                .Descendants(SpreadsheetNamespace + "si")
                .Select(item => string.Concat(
                    item.Descendants(SpreadsheetNamespace + "t")
                        .Select(text => text.Value)))
                .ToList();
        }

        private Dictionary<string, string> ReadSheetParts()
        {
            XDocument workbook = ReadXml("xl/workbook.xml");
            XDocument relationships =
                ReadXml("xl/_rels/workbook.xml.rels");
            var targets = relationships
                .Descendants(PackageRelationshipNamespace + "Relationship")
                .ToDictionary(
                    relation => relation.Attribute("Id")?.Value ??
                        string.Empty,
                    relation => relation.Attribute("Target")?.Value ??
                        string.Empty,
                    StringComparer.Ordinal);
            var result = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

            foreach (XElement sheet in workbook.Descendants(
                         SpreadsheetNamespace + "sheet"))
            {
                string name = sheet.Attribute("name")?.Value;
                string relationshipId = sheet.Attribute(
                    DocumentRelationshipNamespace + "id")?.Value;
                if (string.IsNullOrWhiteSpace(name) ||
                    string.IsNullOrWhiteSpace(relationshipId) ||
                    !targets.TryGetValue(
                        relationshipId,
                        out string target))
                {
                    throw new InvalidDataException(
                        "Workbook contains an invalid worksheet reference.");
                }

                result.Add(
                    name,
                    ResolvePartPath("xl/workbook.xml", target));
            }

            return result;
        }

        private string ReadCellValue(
            string sheetName,
            string reference,
            XElement cell)
        {
            if (cell.Element(SpreadsheetNamespace + "f") != null)
            {
                throw new InvalidDataException(
                    $"{sheetName}!{reference} contains a formula. " +
                    "Game data cells must contain fixed values.");
            }

            string type = cell.Attribute("t")?.Value;
            if (string.Equals(type, "inlineStr", StringComparison.Ordinal))
            {
                return string.Concat(
                    cell.Descendants(SpreadsheetNamespace + "t")
                        .Select(text => text.Value));
            }

            string raw = cell.Element(SpreadsheetNamespace + "v")?.Value ??
                string.Empty;
            if (string.Equals(type, "s", StringComparison.Ordinal))
            {
                if (!int.TryParse(
                        raw,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int index) ||
                    index < 0 ||
                    index >= sharedStrings.Count)
                {
                    throw new InvalidDataException(
                        $"{sheetName}!{reference} has an invalid shared string.");
                }

                return sharedStrings[index];
            }

            if (string.Equals(type, "b", StringComparison.Ordinal))
            {
                return raw == "1" ? "TRUE" : "FALSE";
            }

            if (string.Equals(type, "e", StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"{sheetName}!{reference} contains Excel error '{raw}'.");
            }

            return raw;
        }

        private XDocument ReadXml(string partPath)
        {
            ZipArchiveEntry entry = archive.GetEntry(partPath);
            if (entry == null)
            {
                throw new InvalidDataException(
                    $"Excel part is missing: {partPath}");
            }

            return ReadXml(entry);
        }

        private static XDocument ReadXml(ZipArchiveEntry entry)
        {
            using Stream entryStream = entry.Open();
            return XDocument.Load(entryStream, LoadOptions.None);
        }

        private static int GetColumnIndex(string cellReference)
        {
            if (string.IsNullOrWhiteSpace(cellReference))
            {
                throw new InvalidDataException(
                    "Worksheet contains a cell without a reference.");
            }

            int result = 0;
            int letterCount = 0;
            foreach (char value in cellReference)
            {
                if (!char.IsLetter(value))
                {
                    break;
                }

                result = result * 26 +
                    (char.ToUpperInvariant(value) - 'A' + 1);
                letterCount++;
            }

            if (letterCount == 0)
            {
                throw new InvalidDataException(
                    $"Invalid cell reference: {cellReference}");
            }

            return result - 1;
        }

        private static int ReadPositiveInteger(
            string value,
            int fallback)
        {
            return int.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int result) &&
                result > 0
                ? result
                : fallback;
        }

        private static string ResolvePartPath(
            string sourcePart,
            string target)
        {
            string normalizedTarget = target.Replace('\\', '/');
            if (normalizedTarget.StartsWith("/", StringComparison.Ordinal))
            {
                return normalizedTarget.TrimStart('/');
            }

            string sourceDirectory = sourcePart.Substring(
                0,
                sourcePart.LastIndexOf('/', sourcePart.Length - 1));
            var parts = new List<string>();
            foreach (string part in
                     $"{sourceDirectory}/{normalizedTarget}".Split('/'))
            {
                if (part == "..")
                {
                    if (parts.Count == 0)
                    {
                        throw new InvalidDataException(
                            $"Invalid Excel relationship target: {target}");
                    }

                    parts.RemoveAt(parts.Count - 1);
                }
                else if (part != "." && part.Length > 0)
                {
                    parts.Add(part);
                }
            }

            return string.Join("/", parts);
        }
    }
}
