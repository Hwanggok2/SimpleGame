import fs from "node:fs/promises";
import path from "node:path";
import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const root = "C:/Github/SimpleGame";
const source = path.join(root, "Planning/GameData_10min_Balance.xlsx");
const outputPath = path.join(
  root,
  "outputs/difficulty-selection-20260801/GameData_10min_Balance.xlsx",
);
const previewDir = path.join(root, "tmp/difficulty-work-20260801/previews-after");
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const easy = workbook.worksheets.getItem("StageSpawnEasy");
const used = easy.getUsedRange();
const rowCount = used.values.length;
const header = easy.getRange("A1:G1");
header.format = {
  fill: "#2F8F1F",
  font: { bold: true, color: "#FFFFFF" },
  wrapText: true,
  horizontalAlignment: "center",
};
const body = easy.getRange(`A2:G${rowCount}`);
body.format = {
  fill: "#F4FBF4",
  font: { color: "#17202A" },
  wrapText: false,
};
easy.getRange(`C2:C${rowCount}`).format.numberFormat = "0.00";
easy.getRange(`D2:D${rowCount}`).format.numberFormat = "0";
easy.getRange(`G2:G${rowCount}`).format.numberFormat = "0";

const readme = workbook.worksheets.getItem("README");
readme.getRange("A20:F20").unmerge();
readme.getRange("A20:F20").copyFrom(readme.getRange("A11:F11"), "all");
readme.getRange("A20:F20").values = [[
  "StageSpawnEasy",
  "쉬움 개별 스폰 명세",
  "보통 대비 일반 적 75%·적 레벨 80%",
  "예",
  "예",
  `${rowCount - 1}행`,
]];

for (const [name, sheetName, range] of [
  ["README", "README", "A1:F22"],
  ["StageSpawnEasy-top", "StageSpawnEasy", "A1:G60"],
  ["StageSpawnEasy-tail", "StageSpawnEasy", `A${rowCount - 29}:G${rowCount}`],
]) {
  const rendered = await workbook.render({
    sheetName,
    range,
    scale: 0.9,
    format: "png",
  });
  await fs.writeFile(
    path.join(previewDir, `${name}.png`),
    new Uint8Array(await rendered.arrayBuffer()),
  );
}

const errors = await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
  options: { useRegex: true, maxResults: 300 },
});
console.log(errors.ndjson);
const exported = await SpreadsheetFile.exportXlsx(workbook);
await exported.save(outputPath);
await fs.copyFile(outputPath, source);
