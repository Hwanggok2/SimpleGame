import fs from "node:fs/promises";
import path from "node:path";
import {
  FileBlob,
  SpreadsheetFile,
} from "@oai/artifact-tool";

const projectRoot = "C:/Github/SimpleGame";
const inputPath = path.join(
  projectRoot,
  "Planning",
  "GameData_10min_Balance.xlsx",
);
const outputDir = path.join(
  projectRoot,
  "outputs",
  "sever-delay-20260729",
);
const mode = process.argv[2] ?? "inspect";
const previewDir = path.join(
  outputDir,
  mode === "edit" ? "previews-after" : "previews-before",
);

const input = await FileBlob.load(inputPath);
const workbook = await SpreadsheetFile.importXlsx(input);

const sheets = await workbook.inspect({
  kind: "sheet",
  include: "id,name",
  maxChars: 12000,
});
console.log("---SHEETS---");
console.log(sheets.ndjson);

const matches = await workbook.inspect({
  kind: "match",
  searchTerm: "SEVER_TRAIL|0\\.15",
  options: {
    useRegex: true,
    maxResults: 200,
  },
  summary: "Sever timing source cells",
});
console.log("---MATCHES---");
console.log(matches.ndjson);

const sheetItems = workbook.worksheets.items;
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

async function renderSheets(directory) {
  await fs.mkdir(directory, { recursive: true });
  for (let index = 0; index < sheetItems.length; index += 1) {
    const sheet = sheetItems[index];
    const preview = await workbook.render({
      sheetName: sheet.name,
      range: previewRanges[sheet.name],
      scale: 0.8,
      format: "png",
    });
    const safeName = sheet.name.replace(/[<>:"/\\|?*]/g, "_");
    await fs.writeFile(
      path.join(directory, `${index + 1}-${safeName}.png`),
      new Uint8Array(await preview.arrayBuffer()),
    );
  }
}

if (mode !== "edit") {
  await renderSheets(previewDir);
}

if (mode === "edit") {
  const updates = [
    {
      sheetName: "LevelUpCard",
      address: "D7",
      from: "0.15초 뒤",
      to: "0.3초 뒤",
    },
    {
      sheetName: "CardMath",
      address: "A16",
      from: "0.15초 뒤 관통 시작점",
      to: "0.3초 뒤 관통 시작점",
    },
    {
      sheetName: "BalanceSummary",
      address: "E11",
      from: "지연 0.15초",
      to: "지연 0.3초",
    },
  ];
  let updatedCells = 0;
  for (const update of updates) {
    const sheet = workbook.worksheets.getItem(update.sheetName);
    const cell = sheet.getRange(update.address);
    const value = cell.values[0]?.[0];
    if (typeof value !== "string" ||
      !value.includes(update.from)) {
      throw new Error(
        `${update.sheetName}!${update.address} did not contain ` +
        `"${update.from}".`,
      );
    }

    cell.values = [[value.replace(update.from, update.to)]];
    updatedCells += 1;
  }

  if (updatedCells !== updates.length) {
    throw new Error("Not every sever timing source cell was updated.");
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
  console.log("---VERIFICATION---");
  console.log(verification.ndjson);

  const errorScan = await workbook.inspect({
    kind: "match",
    searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
    options: {
      useRegex: true,
      maxResults: 300,
    },
    summary: "Final formula error scan",
  });
  console.log("---ERROR_SCAN---");
  console.log(errorScan.ndjson);

  await renderSheets(previewDir);
  await fs.mkdir(outputDir, { recursive: true });
  const output = await SpreadsheetFile.exportXlsx(workbook);
  const outputPath = path.join(
    outputDir,
    "GameData_10min_Balance.xlsx",
  );
  await output.save(outputPath);
  await fs.copyFile(outputPath, inputPath);
  console.log(`UPDATED_CELLS=${updatedCells}`);
  console.log(`OUTPUT=${outputPath}`);
}
