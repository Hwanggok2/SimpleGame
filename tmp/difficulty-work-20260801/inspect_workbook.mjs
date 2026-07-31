import fs from "node:fs/promises";
import path from "node:path";
import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const root = "C:/Github/SimpleGame";
const source = path.join(root, "Planning/GameData_10min_Balance.xlsx");
const previewDir = path.join(root, "tmp/difficulty-work-20260801/previews-before");
await fs.mkdir(previewDir, { recursive: true });

const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const summary = await workbook.inspect({
  kind: "sheet,table",
  include: "id,name,range,values,formulas",
  maxChars: 12000,
  tableMaxRows: 8,
  tableMaxCols: 12,
  tableMaxCellChars: 120,
});
console.log(summary.ndjson);

const sheets = workbook.worksheets.items;
for (const sheet of sheets) {
  const renderRanges = sheet.name === "StageSpawn"
    ? ["A1:G60", "A3255:G3284"]
    : [sheet.getUsedRange().address];
  let part = 0;
  for (const range of renderRanges) {
  const rendered = await workbook.render({
    sheetName: sheet.name,
    range,
    scale: 0.7,
    format: "png",
  });
  const safeName = sheet.name.replace(/[^A-Za-z0-9_-]/g, "_");
  await fs.writeFile(
    path.join(previewDir, `${safeName}-${part}.png`),
    new Uint8Array(await rendered.arrayBuffer()),
  );
    part += 1;
  }
}

const stage = workbook.worksheets.getItem("StageSpawn");
const stageDetails = await workbook.inspect({
  kind: "region,computedStyle,table",
  sheetId: stage.name,
  range: "A1:G25",
  include: "values,formulas",
  maxChars: 14000,
  tableMaxRows: 25,
  tableMaxCols: 10,
});
console.log(stageDetails.ndjson);
