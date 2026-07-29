import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const workbookPaths = process.argv.slice(2);
const ranges = [
  ["EnemyBalance", "A1:U2"],
  ["PlayerBalance", "A1:M2"],
  ["WavePlan", "A5:O6"],
  ["CardMath", "A9:M10"],
];

for (const workbookPath of workbookPaths) {
  const workbook = await SpreadsheetFile.importXlsx(
    await FileBlob.load(workbookPath),
  );
  console.log(`WORKBOOK=${workbookPath}`);
  for (const [sheetName, address] of ranges) {
    const sheet = workbook.worksheets.getItem(sheetName);
    console.log(`${sheetName}!${address}`);
    console.log(JSON.stringify(sheet.getRange(address).values));
  }
}
