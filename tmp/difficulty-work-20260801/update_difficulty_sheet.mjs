import fs from "node:fs/promises";
import path from "node:path";
import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const root = "C:/Github/SimpleGame";
const source = path.join(root, "Planning/GameData_10min_Balance.xlsx");
const outputDir = path.join(root, "outputs/difficulty-selection-20260801");
const previewDir = path.join(root, "tmp/difficulty-work-20260801/previews-after");
await fs.mkdir(outputDir, { recursive: true });
await fs.mkdir(previewDir, { recursive: true });

const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const normal = workbook.worksheets.getItem("StageSpawn");
const normalValues = normal.getUsedRange().values;
const header = normalValues[0];
const bossIds = new Set([
  "GoblinBoss",
  "MushroomBoss",
  "FlyingEyeBoss",
  "SkeletonBoss",
]);

const easyValues = [header];
const nextIndexByWave = new Map();
for (const row of normalValues.slice(1)) {
  const spawnIndex = Number(row[3]);
  const enemyId = String(row[5]);
  if (!bossIds.has(enemyId) && spawnIndex % 4 === 0) {
    continue;
  }

  const waveId = String(row[1]);
  const nextIndex = (nextIndexByWave.get(waveId) ?? 0) + 1;
  nextIndexByWave.set(waveId, nextIndex);
  easyValues.push([
    row[0],
    row[1],
    row[2],
    nextIndex,
    row[4],
    row[5],
    Math.max(1, Math.ceil(Number(row[6]) * 0.8)),
  ]);
}

const easy = workbook.worksheets.add("StageSpawnEasy");
const easyRange = easy.getRange(`A1:G${easyValues.length}`);
easyRange.values = easyValues;
easy.getRange("A1:G1").copyFrom(normal.getRange("A1:G1"), "all");
easy.getRange(`A2:G${easyValues.length}`).copyFrom(
  normal.getRange(`A2:G${easyValues.length}`),
  "all",
);
easy.getRange(`A1:G${easyValues.length}`).values = easyValues;
easy.getRange(`A1:A${easyValues.length}`).format.columnWidth = 12;
easy.getRange(`B1:B${easyValues.length}`).format.columnWidth = 12;
easy.getRange(`C1:C${easyValues.length}`).format.columnWidth = 14;
easy.getRange(`D1:D${easyValues.length}`).format.columnWidth = 12;
easy.getRange(`E1:E${easyValues.length}`).format.columnWidth = 16;
easy.getRange(`F1:F${easyValues.length}`).format.columnWidth = 18;
easy.getRange(`G1:G${easyValues.length}`).format.columnWidth = 12;
easy.freezePanes.freezeRows(1);
easy.showGridLines = false;

const readme = workbook.worksheets.getItem("README");
readme.getRange("A22:F22").copyFrom(readme.getRange("A21:F21"), "all");
readme.getRange("A21:F21").copyFrom(readme.getRange("A20:F20"), "all");
readme.getRange("A20:F20").copyFrom(readme.getRange("A11:F11"), "all");
readme.getRange("A20:F20").values = [[
  "StageSpawnEasy",
  "쉬움 개별 스폰 명세",
  "보통 대비 일반 적 75%·적 레벨 80%",
  "예",
  "예",
  `${easyValues.length - 1}행`,
]];
readme.getRange("C5").values = [[
  `보통 3,283개 + 쉬움 ${easyValues.length - 1}개`,
]];
readme.getRange("F7").values = [[
  `보통 3,283마리 / 쉬움 ${easyValues.length - 1}마리`,
]];
readme.getRange("A22:F22").format.wrapText = true;

const keyCheck = await workbook.inspect({
  kind: "table",
  sheetId: "StageSpawnEasy",
  range: "A1:G20",
  include: "values,formulas",
  maxChars: 6000,
  tableMaxRows: 20,
  tableMaxCols: 7,
});
console.log(keyCheck.ndjson);
const tailStart = Math.max(2, easyValues.length - 29);
const tailCheck = await workbook.inspect({
  kind: "table",
  sheetId: "StageSpawnEasy",
  range: `A${tailStart}:G${easyValues.length}`,
  include: "values,formulas",
  maxChars: 6000,
  tableMaxRows: 30,
  tableMaxCols: 7,
});
console.log(tailCheck.ndjson);
const formulaErrors = await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
  options: { useRegex: true, maxResults: 300 },
  summary: "final formula error scan",
});
console.log(formulaErrors.ndjson);

for (const [name, range] of [
  ["README", "A1:F22"],
  ["StageSpawnEasy-top", "A1:G60"],
  ["StageSpawnEasy-tail", `A${tailStart}:G${easyValues.length}`],
]) {
  const sheetName = name.startsWith("StageSpawnEasy")
    ? "StageSpawnEasy"
    : name;
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

const outputPath = path.join(outputDir, "GameData_10min_Balance.xlsx");
const exported = await SpreadsheetFile.exportXlsx(workbook);
await exported.save(outputPath);
await fs.copyFile(outputPath, source);
console.log(JSON.stringify({
  normalSpawnCount: normalValues.length - 1,
  easySpawnCount: easyValues.length - 1,
  easyMaximumLevel: Math.max(...easyValues.slice(1).map((row) => Number(row[6]))),
  outputPath,
}));
