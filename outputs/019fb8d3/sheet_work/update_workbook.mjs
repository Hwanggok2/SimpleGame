import fs from "node:fs/promises";
import path from "node:path";
import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const sourcePath =
  "C:/Github/SimpleGame/Planning/GameData_10min_Balance.xlsx";
const outputPath =
  "C:/Github/SimpleGame/outputs/019fb8d3/GameData_10min_Balance.xlsx";
const previewDir =
  "C:/Github/SimpleGame/outputs/019fb8d3/sheet_work/previews_after";

const workbook = await SpreadsheetFile.importXlsx(
  await FileBlob.load(sourcePath),
);

const gameString = workbook.worksheets.getItem("GameString");
gameString.getRange("C11").values = [[
  "일반 공격은 0.4초 판정창마다 카드 레벨만큼 주 대상 뒤의 적에게 추가 피해를 줍니다. " +
    "이동 관통은 한 번의 연속 이동 입력마다 카드 레벨만큼 적을 지나갈 수 있습니다. " +
    "공격 관통 수와 이동 관통 수는 서로 별도로 소비합니다.",
]];
gameString.getRange("C13").values = [[
  "실제 이동 관통 0.3초 뒤 관통 시작 위치부터 현재 위치까지 검은 절단선을 만듭니다. " +
    "선은 0.1초 동안 사라지며 재사용 대기시간은 0.1초, 피해는 공격력의 2배입니다.",
]];
gameString.getRange("C19").values = [[
  "기본 공격 시 주 대상 방향으로 초승달 검기의 발동을 판정합니다. " +
    "방패에 막힌 공격도 판정하고 연속 발동할 수 있으며, 추가 피해로는 재발동하지 않습니다. " +
    "1~5레벨: 확률 15/19.5/24/28.5/33%, 피해 1.8/2.15/2.5/2.85/3.2배, " +
    "크기 100/115/130/145/160%, 사거리 6/7.5/9/10.5/12, 최대 타격 2/3/4/5/6.",
]];
gameString.getRange("A11:E11").format.autofitRows();
gameString.getRange("A13:E13").format.autofitRows();
gameString.getRange("A19:E19").format.autofitRows();

const cardMath = workbook.worksheets.getItem("CardMath");
cardMath.getRange("I9:L9").values = [[
  "참격 발동 확률",
  "참격 최대 적중",
  "참격 크기",
  "참격 후면 피해",
]];
const cardMathPhase3 = cardMath.tables.items.find(
  (table) => table.name === "CardMathPhase3",
);
if (!cardMathPhase3) {
  throw new Error("CardMathPhase3 table was not found.");
}
cardMathPhase3.syncColumnsFromSheet();
cardMath.getRange("I10:I14").formulas = [
  ["=(10%+3%*(A10-1))*1.5"],
  ["=(10%+3%*(A11-1))*1.5"],
  ["=(10%+3%*(A12-1))*1.5"],
  ["=(10%+3%*(A13-1))*1.5"],
  ["=(10%+3%*(A14-1))*1.5"],
];
cardMath.getRange("L10:L14").formulas = [
  ["=(LevelUpCard!$G$10+0.35*(A10-1))*3"],
  ["=(LevelUpCard!$G$10+0.35*(A11-1))*3"],
  ["=(LevelUpCard!$G$10+0.35*(A12-1))*3"],
  ["=(LevelUpCard!$G$10+0.35*(A13-1))*3"],
  ["=(LevelUpCard!$G$10+0.35*(A14-1))*3"],
];
cardMath.getRange("A16").values = [[
  "관통: 일반 공격 0.4초 판정창에서 추가 타깃 누적 C≤L, 이동은 한 번의 연속 입력마다 별도 L회. " +
    "절단: 실제 이동 관통 0.3초 뒤 선분에 2A_side 피해. " +
    "흡혈: 처치 시 5%, 회복 2L. " +
    "참격: 기본 공격마다 방패 방어 포함·쿨다운 없이 독립 판정, 추가 피해 재발동 없음, " +
    "p=1.5×[10%+3%(L-1)], H=L+1, S=1+15%(L-1), D=6+1.5(L-1), M=1.8+0.35(L-1). " +
    "방패 우회: 정면 반동에만 10%×L. " +
    "오물 투척: 화면 안 무작위 생존 적 대상, T=L, 틱 M=0.35+0.1(L-1), " +
    "반경 R=1.2×[1+10%(L-1)], 재사용 C=6-0.5(L-1), 장판당 6틱. " +
    "융합: 재료 전부 마스터+융합 미획득일 때 가중치 10, 획득 후 재료 스택/기본 레벨 0·융합 snapshot 유지.",
]];

const balanceSummary = workbook.worksheets.getItem("BalanceSummary");
balanceSummary.getRange("E9").values = [[
  "일반 공격: 0.4초 판정창 추가 타깃 누적 C≤L / 이동: 연속 입력마다 별도 L회",
]];
balanceSummary.getRange("G9:H10").values = [
  ["참격", "기본 공격마다 p=1.5×[10%+3%(L-1)], 쿨다운·연속 발동 제한 없음"],
  ["모드 1", "마지막 기본 공격 대상 유지·0.3초 공격·비관통 시 사거리 원주 이동"],
];
balanceSummary.getRange("A9:H10").format.autofitRows();

await fs.mkdir(path.dirname(outputPath), { recursive: true });
await fs.mkdir(previewDir, { recursive: true });

const output = await SpreadsheetFile.exportXlsx(workbook);
await output.save(outputPath);
const projectOutput = await SpreadsheetFile.exportXlsx(workbook);
await projectOutput.save(sourcePath);

for (const sheet of workbook.worksheets.items) {
  const safeName = sheet.name.replace(/[^A-Za-z0-9_-]+/g, "_");
  const preview = await workbook.render({
    sheetName: sheet.name,
    range: "A1:Z25",
    scale: 1,
    format: "png",
  });
  await fs.writeFile(
    path.join(previewDir, `${safeName}.png`),
    new Uint8Array(await preview.arrayBuffer()),
  );
}

const verification = await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
  options: { useRegex: true, maxResults: 100 },
  summary: "final formula error scan",
});
console.log(verification.ndjson);

for (const [sheetId, range] of [
  ["GameString", "A9:E20"],
  ["CardMath", "A9:L16"],
  ["LevelUpCard", "A8:N11"],
]) {
  const inspected = await workbook.inspect({
    kind: "region",
    sheetId,
    range,
    include: "values,formulas",
    maxChars: 16000,
    tableMaxRows: 20,
    tableMaxCols: 16,
  });
  console.log(`VERIFY ${sheetId} ${range}`);
  console.log(inspected.ndjson);
}
