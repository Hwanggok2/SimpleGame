import fs from "node:fs/promises";
import path from "node:path";
import {
  FileBlob,
  SpreadsheetFile,
} from "@oai/artifact-tool";

const projectRoot = "C:/Github/SimpleGame";
const sourcePath = path.join(
  projectRoot,
  "outputs",
  "sever-delay-20260729",
  "GameData_10min_Balance.xlsx",
);
const outputDir = path.join(
  projectRoot,
  "outputs",
  "workbook-repair-20260730",
);
const outputPath = path.join(
  outputDir,
  "GameData_10min_Balance.xlsx",
);
const previewDir = path.join(outputDir, "previews");

const workbook = await SpreadsheetFile.importXlsx(
  await FileBlob.load(sourcePath),
);

const repairs = [
  {
    sheetName: "EnemyBalance",
    tableName: "EnemyBalanceTable",
    range: "A1:S5",
  },
  {
    sheetName: "PlayerBalance",
    tableName: "PlayerBalanceTable",
    range: "A1:M2",
  },
  {
    sheetName: "WavePlan",
    tableName: "WavePlan10Min",
    range: "A5:O65",
  },
  {
    sheetName: "CardMath",
    tableName: "CardMath10Min",
    range: "A9:M14",
  },
];

for (const repair of repairs) {
  const sheet = workbook.worksheets.getItem(repair.sheetName);
  const existing = sheet.tables.items.find(
    (table) => table.name === repair.tableName,
  );
  if (!existing) {
    throw new Error(
      `Missing table ${repair.tableName} on ${repair.sheetName}.`,
    );
  }

  const settings = {
    style: existing.style,
    showHeaders: existing.showHeaders,
    showTotals: existing.showTotals,
    showBandedColumns: existing.showBandedColumns,
    showFilterButton: existing.showFilterButton,
  };
  existing.delete();

  const replacement = sheet.tables.add(
    repair.range,
    true,
    repair.tableName,
  );
  replacement.style = settings.style || "TableStyleMedium4";
  replacement.showHeaders = settings.showHeaders;
  replacement.showTotals = settings.showTotals;
  replacement.showBandedColumns = settings.showBandedColumns;
  replacement.showFilterButton = settings.showFilterButton;
}

const verification = await workbook.inspect({
  kind: "table",
  sheetId: "LevelUpCard",
  range: "A1:O13",
  include: "values,formulas",
  tableMaxRows: 13,
  tableMaxCols: 15,
  maxChars: 12000,
});
console.log("---LEVEL_UP_CARD---");
console.log(verification.ndjson);

const errorScan = await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
  options: {
    useRegex: true,
    maxResults: 300,
  },
  summary: "Formula error scan after table repair",
});
console.log("---ERROR_SCAN---");
console.log(errorScan.ndjson);

const previewRanges = {
  README: "A1:F21",
  EnemyBalance: "A1:S5",
  StageSpawn: "A1:G30",
  PlayerBalance: "A1:M2",
  PlayerLevelExp: "A1:C51",
  AccountLevelExp: "A1:B5",
  GlobalBalance: "A1:D2",
  LevelUpCard: "A1:O13",
  WavePlan: "A1:S65",
  CardMath: "A1:M16",
  BalanceSummary: "A1:E27",
};

await fs.mkdir(previewDir, { recursive: true });
const sheets = workbook.worksheets.items;
for (let index = 0; index < sheets.length; index += 1) {
  const sheet = sheets[index];
  const preview = await workbook.render({
    sheetName: sheet.name,
    range: previewRanges[sheet.name],
    scale: 0.8,
    format: "png",
  });
  const safeName = sheet.name.replace(/[<>:"/\\|?*]/g, "_");
  await fs.writeFile(
    path.join(previewDir, `${index + 1}-${safeName}.png`),
    new Uint8Array(await preview.arrayBuffer()),
  );
}

await fs.mkdir(outputDir, { recursive: true });
const output = await SpreadsheetFile.exportXlsx(workbook);
await output.save(outputPath);
console.log(`OUTPUT=${outputPath}`);
