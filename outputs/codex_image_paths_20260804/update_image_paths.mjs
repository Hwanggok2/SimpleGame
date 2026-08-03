import fs from "node:fs/promises";
import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const projectRoot = "C:/Github/SimpleGame";
const workbookPath = `${projectRoot}/Planning/GameData_10min_Balance.xlsx`;
const outputDir = `${projectRoot}/outputs/codex_image_paths_20260804`;
const mode = process.argv[2] ?? "inspect";

const input = await FileBlob.load(workbookPath);
const workbook = await SpreadsheetFile.importXlsx(input);

if (mode === "update") {
  const sheet = workbook.worksheets.getItem("ImageData");
  sheet.getRange("B5:B10").values = [
    ["Background/LobbyDifficulty_Easy.png"],
    ["Background/LobbyDifficulty_Normal.png"],
    ["Background/LobbyDifficulty_Hard.png"],
    ["UI/Easy_Text.png"],
    ["UI/Normal_Text.png"],
    ["UI/Hard_Text.png"],
  ];
  sheet.getRange("A1:A10").format.columnWidth = 42;
  sheet.getRange("B1:B10").format.columnWidth = 48;

  const output = await SpreadsheetFile.exportXlsx(workbook);
  await output.save(`${outputDir}/GameData_10min_Balance.xlsx`);
  await output.save(workbookPath);
}

const inspection = await workbook.inspect({
  kind: "table",
  sheetId: "ImageData",
  range: "A1:B10",
  include: "values,formulas",
  tableMaxRows: 10,
  tableMaxCols: 2,
  maxChars: 4000,
});
console.log(inspection.ndjson);

const errors = await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
  options: { useRegex: true, maxResults: 50 },
  summary: "formula error scan",
});
console.log(errors.ndjson);

const preview = await workbook.render({
  sheetName: "ImageData",
  range: "A1:B10",
  scale: 2,
  format: "png",
});
const previewBytes = new Uint8Array(await preview.arrayBuffer());
await fs.writeFile(
  `${outputDir}/ImageData_${mode}.png`,
  previewBytes,
);
