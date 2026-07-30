import fs from "node:fs/promises";
import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const workbookPath =
  "C:/Github/SimpleGame/Planning/GameData_10min_Balance.xlsx";
const previewDir =
  "C:/Users/JW/AppData/Local/Temp/simplegame-spreadsheet-previews";

const input = await FileBlob.load(workbookPath);
const workbook = await SpreadsheetFile.importXlsx(input);

const sheets = await workbook.inspect({
  kind: "sheet",
  include: "id,name",
  maxChars: 4000,
});
console.log(sheets.ndjson);

const matches = await workbook.inspect({
  kind: "match",
  searchTerm: "FILTH_THROW|오물 투척",
  options: { useRegex: true, maxResults: 30 },
  summary: "filth throw cells",
});
console.log(matches.ndjson);

for (const [sheetId, range] of [
  ["LevelUpCard", "A13:O14"],
  ["CardMath", "M9:P16"],
]) {
  const table = await workbook.inspect({
    kind: "table",
    sheetId,
    range,
    include: "values,formulas",
    tableMaxRows: 20,
    tableMaxCols: 20,
    maxChars: 6000,
  });
  console.log(table.ndjson);
}

for (const [sheetId, range] of [
  ["LevelUpCard", "D14"],
  ["CardMath", "P9:P14"],
]) {
  const style = await workbook.inspect({
    kind: "computedStyle",
    sheetId,
    range,
    maxChars: 3000,
  });
  console.log(style.ndjson);
}

await fs.mkdir(previewDir, { recursive: true });
for (const sheetName of ["LevelUpCard", "CardMath"]) {
  const preview = await workbook.render({
    sheetName,
    autoCrop: "all",
    scale: 1,
    format: "png",
  });
  await fs.writeFile(
    `${previewDir}/${sheetName}.png`,
    new Uint8Array(await preview.arrayBuffer()),
  );
}
