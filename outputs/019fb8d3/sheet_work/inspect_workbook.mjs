import fs from "node:fs/promises";
import path from "node:path";
import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const workbookPath = "C:/Github/SimpleGame/Planning/GameData_10min_Balance.xlsx";
const outputDir = "C:/Github/SimpleGame/outputs/019fb8d3/sheet_work/previews_before";
await fs.mkdir(outputDir, { recursive: true });

const workbook = await SpreadsheetFile.importXlsx(
  await FileBlob.load(workbookPath),
);

if (process.argv.includes("--cells")) {
  const cardMath = workbook.worksheets.getItem("CardMath");
  for (const address of ["I10:I14", "L10:L14"]) {
    const range = cardMath.getRange(address);
    console.log(`FORMULAS CardMath ${address}`);
    console.log(JSON.stringify(range.formulas));
    console.log(`VALUES CardMath ${address}`);
    console.log(JSON.stringify(range.values));
  }

  process.exit(0);
}

if (process.argv.includes("--balance")) {
  const inspected = await workbook.inspect({
    kind: "region",
    sheetId: "BalanceSummary",
    range: "A1:L15",
    include: "values,formulas",
    maxChars: 16000,
    tableMaxRows: 20,
    tableMaxCols: 16,
  });
  console.log(inspected.ndjson);
  process.exit(0);
}

if (process.argv.includes("--tables")) {
  const sheet = workbook.worksheets.getItem("CardMath");
  for (const table of sheet.tables.items) {
    console.log(JSON.stringify({
      name: table.name,
      style: table.style,
      showHeaders: table.showHeaders,
      showTotals: table.showTotals,
      showBandedColumns: table.showBandedColumns,
      showFilterButton: table.showFilterButton,
      headerAddress: table.getHeaderRowRange().address,
      dataAddress: table.getDataRows().address,
      methods: Object.getOwnPropertyNames(Object.getPrototypeOf(table)),
    }));
  }
  process.exit(0);
}

const summary = await workbook.inspect({
  kind: "workbook,sheet,table",
  maxChars: 12000,
  tableMaxRows: 4,
  tableMaxCols: 8,
  tableMaxCellChars: 100,
});
console.log("SUMMARY");
console.log(summary.ndjson);

for (const sheet of workbook.worksheets.items) {
  const safeName = sheet.name.replace(/[^A-Za-z0-9_-]+/g, "_");
  const preview = await workbook.render({
    sheetName: sheet.name,
    range: "A1:Z25",
    scale: 1,
    format: "png",
  });
  await fs.writeFile(
    path.join(outputDir, `${safeName}.png`),
    new Uint8Array(await preview.arrayBuffer()),
  );
}

for (const [sheetId, range] of [
  ["GameString", "A1:D25"],
  ["CardMath", "A1:L16"],
  ["LevelUpCard", "A1:N20"],
]) {
  const inspected = await workbook.inspect({
    kind: "region,computedStyle",
    sheetId,
    range,
    maxChars: 12000,
    tableMaxRows: 30,
    tableMaxCols: 16,
  });
  console.log(`DETAIL ${sheetId} ${range}`);
  console.log(inspected.ndjson);
}
