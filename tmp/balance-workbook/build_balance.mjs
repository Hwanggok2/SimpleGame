import fs from "node:fs/promises";
import path from "node:path";
import {
  FileBlob,
  SpreadsheetFile,
} from "@oai/artifact-tool";

const workspace = "C:/Github/SimpleGame";
const sourcePath = path.join(
  workspace,
  "Planning",
  "GameData_2min_Balance.xlsx",
);
const workDir = path.join(workspace, "tmp", "balance-workbook");
const outputDir = path.join(
  workspace,
  "outputs",
  "enemy-balance-20260727",
);
const outputPath = path.join(
  outputDir,
  "SimpleGame_2min_Balance.xlsx",
);
await fs.mkdir(workDir, { recursive: true });
await fs.mkdir(outputDir, { recursive: true });

const input = await FileBlob.load(sourcePath);
const workbook = await SpreadsheetFile.importXlsx(input);

const green = "#2F8F1F";
const darkGreen = "#155E28";
const lightGreen = "#E9F7EA";
const mint = "#D6F2DC";
const navy = "#183153";
const blue = "#2563EB";
const amber = "#F59E0B";
const red = "#DC2626";
const gray = "#64748B";
const lightGray = "#E2E8F0";

function getOrAddSheet(name) {
  return workbook.worksheets.getOrAdd(name);
}

function resetSheet(sheet) {
  sheet.deleteAllDrawings();
  for (const table of [...sheet.tables.items]) {
    table.delete();
  }
  const used = sheet.getUsedRange();
  if (used) {
    used.clear({ applyTo: "all" });
  }
  sheet.showGridLines = false;
  sheet.freezePanes.unfreeze();
}

function setBaseFont(sheet, range) {
  sheet.getRange(range).format.font = {
    name: "Aptos",
    size: 10,
    color: "#17202A",
  };
  sheet.getRange(range).format.verticalAlignment = "center";
}

function styleTitle(sheet, range, text) {
  sheet.getRange(range).merge();
  sheet.getRange(range).values = [[text]];
  sheet.getRange(range).format = {
    fill: darkGreen,
    font: {
      name: "Aptos Display",
      size: 18,
      bold: true,
      color: "#FFFFFF",
    },
    verticalAlignment: "center",
    horizontalAlignment: "left",
  };
  sheet.getRange(range).format.rowHeight = 34;
}

function restyleTitle(sheet, range) {
  sheet.getRange(range).format = {
    fill: darkGreen,
    font: {
      name: "Aptos Display",
      size: 18,
      bold: true,
      color: "#FFFFFF",
    },
    verticalAlignment: "center",
    horizontalAlignment: "left",
  };
  sheet.getRange(range).format.rowHeight = 34;
}

function styleSection(sheet, range) {
  sheet.getRange(range).format = {
    fill: green,
    font: {
      name: "Aptos",
      size: 10,
      bold: true,
      color: "#FFFFFF",
    },
    verticalAlignment: "center",
    horizontalAlignment: "center",
    wrapText: true,
    borders: {
      preset: "outside",
      style: "thin",
      color: green,
    },
  };
}

function writeDataSheet(
  sheetName,
  headers,
  rows,
  tableName,
  widths,
  numberFormats = {},
) {
  const sheet = getOrAddSheet(sheetName);
  resetSheet(sheet);
  const rowCount = rows.length + 1;
  const columnCount = headers.length;
  const endColumn = columnName(columnCount);
  sheet.getRangeByIndexes(0, 0, rowCount, columnCount).values = [
    headers,
    ...rows,
  ];
  const rangeAddress = `A1:${endColumn}${rowCount}`;
  const table = sheet.tables.add(rangeAddress, true, tableName);
  table.style = "TableStyleMedium4";
  setBaseFont(sheet, rangeAddress);
  sheet.getRange(`A1:${endColumn}1`).format = {
    fill: green,
    font: {
      name: "Aptos",
      size: 10,
      bold: true,
      color: "#FFFFFF",
    },
    horizontalAlignment: "center",
    verticalAlignment: "center",
    wrapText: true,
  };
  sheet.getRange(`A1:${endColumn}1`).format.rowHeight = 30;
  sheet.freezePanes.freezeRows(1);
  widths.forEach((width, index) => {
    sheet.getRangeByIndexes(0, index, rowCount, 1)
      .format.columnWidth = width;
  });
  for (const [column, numberFormat] of Object.entries(numberFormats)) {
    sheet.getRange(`${column}2:${column}${rowCount}`)
      .format.numberFormat = numberFormat;
  }
  return sheet;
}

function columnName(columnCount) {
  let value = columnCount;
  let result = "";
  while (value > 0) {
    value--;
    result =
      String.fromCharCode(65 + (value % 26)) +
      result;
    value = Math.floor(value / 26);
  }
  return result;
}

const enemyRows = [
  [
    "GoblinMelee", "Melee", 0.85, 0.85, 2, 0.55, 0, 1.5, 0, 0,
    0.5, 0, 1, 5, 3, 1.7, 0, 3, 1, "StandardMelee", true,
  ],
  [
    "GoblinRanged", "Ranged", 0.6, 2.25, 2, 0.8, 0, 2, 0, 0,
    0.5, 1, 1, 7, 3, 1.7, 1, 3, 0, "StandardRanged", true,
  ],
  [
    "ShieldSkeleton", "Shield", 0.7, 0, 0, 0, 0, 0, 0, 2.25,
    0.5, 0, 2, 8, 3, 1.7, 0, 3, 2, "Shield", true,
  ],
  [
    "GoblinBoss", "Boss", 0.5, 2.6, 4, 1.5, 0.5, 3, 1.35, 0,
    0.5, 0, 8, 50, 15, 1.7, 0, 3, null, "Boss", true,
  ],
];

const enemyHeaders = [
  "EnemyId", "Archetype", "MoveSpeed", "AttackRange", "AttackDamage",
  "AttackWindup", "AttackActiveDuration", "AttackCooldown",
  "AttackAreaRadius", "ApproachRange", "FacingTurnDelay",
  "PostAttackFacingLock", "KillExperience", "Score", "BaseMaxHp",
  "HpGrowthMultiplier", "LevelDifficultyOffset",
  "RearDamageMultiplier", "OneHitPlayerLevelAdvantage",
  "CombatProfileId", "ShowHpBar",
];

const waveCompositions = [
  [6, 0, 0, 0],
  [7, 0, 1, 0],
  [7, 3, 0, 0],
  [8, 3, 1, 0],
  [9, 4, 2, 0],
  [11, 5, 2, 0],
  [13, 6, 2, 0],
  [14, 7, 3, 0],
  [16, 8, 4, 0],
  [18, 9, 5, 0],
  [20, 10, 6, 0],
  [22, 11, 6, 1],
];

function calculateWaveCount(waveNumber) {
  return 4 +
    2 * waveNumber +
    Math.floor(((waveNumber - 1) ** 2) / 10);
}

const waveTypes = waveCompositions.map((composition, index) => {
  const waveNumber = index + 1;
  const expectedCount = calculateWaveCount(waveNumber);
  const actualCount = composition.reduce(
    (total, value) => total + value,
    0,
  );
  if (actualCount !== expectedCount) {
    throw new Error(
      `Wave ${waveNumber}: expected ${expectedCount}, found ${actualCount}.`,
    );
  }

  const enemyIds = [
    "GoblinMelee",
    "GoblinRanged",
    "ShieldSkeleton",
  ];
  const remaining = composition.slice(0, 3);
  const types = [];
  while (remaining.some((value) => value > 0)) {
    for (let typeIndex = 0; typeIndex < enemyIds.length; typeIndex++) {
      if (remaining[typeIndex] > 0) {
        types.push(enemyIds[typeIndex]);
        remaining[typeIndex]--;
      }
    }
  }

  if (composition[3] > 0) {
    types.push("GoblinBoss");
  }

  return types;
});
const waveBaseLevels = [1, 2, 3, 4, 6, 7, 8, 10, 11, 12, 13, 15];
const spawnPoints = [];
for (let index = 1; index <= 8; index++) {
  spawnPoints.push(`LEFT_${String(index).padStart(2, "0")}`);
  spawnPoints.push(`RIGHT_${String(index).padStart(2, "0")}`);
  if (index <= 6) {
    spawnPoints.push(`TOP_${String(index).padStart(2, "0")}`);
    spawnPoints.push(`BOTTOM_${String(index).padStart(2, "0")}`);
  }
}

const spawnRows = [];
for (let waveIndex = 0; waveIndex < waveTypes.length; waveIndex++) {
  const waveId = `WAVE_${String(waveIndex + 1).padStart(2, "0")}`;
  const types = waveTypes[waveIndex];
  const spawnInterval = types.length > 1
    ? 8.6 / (types.length - 1)
    : 0;
  for (let index = 0; index < types.length; index++) {
    const enemyId = types[index];
    let level = waveBaseLevels[waveIndex];
    if (enemyId === "GoblinRanged") {
      level++;
    }
    if (index % 6 === 0 && enemyId !== "GoblinBoss") {
      level++;
    }
    if (enemyId === "GoblinBoss") {
      level = waveBaseLevels[waveIndex];
    }
    const spawnPoint =
      spawnPoints[(waveIndex * 5 + index * 3) % spawnPoints.length];
    spawnRows.push([
      "Stage01",
      waveId,
      Number((
        1 + waveIndex * 10 + index * spawnInterval
      ).toFixed(2)),
      index + 1,
      spawnPoint,
      enemyId,
      level,
    ]);
  }
}

if (spawnRows.length !== 250) {
  throw new Error(`Expected 250 spawns, found ${spawnRows.length}.`);
}
const spawnLastRow = spawnRows.length + 1;

const playerLevelRows = Array.from({ length: 20 }, (_, index) => {
  const level = index + 1;
  return [level, 6 + level * 2, null];
});

const cardHeaders = [
  "CardId", "NameKey", "EffectType", "TargetStat", "Operation", "Value",
  "MaxStack", "SelectionWeight", "MinPlayerLevel", "RequiredCardId",
  "Rarity", "IconId", "Enabled",
];
const cardRows = [
  ["CRIT_CHANCE_UP", "CARD_CRIT_NAME", "StatModifier", "CriticalChance", "Add", 0.05, 5, 100, 1, "", "Common", "ICON_CRIT", true],
  ["MAX_HP_UP", "CARD_HP_NAME", "StatModifier", "MaxHp", "Add", 5, 5, 100, 1, "", "Common", "ICON_HP", true],
  ["MOVE_SPEED_UP", "CARD_SPEED_NAME", "StatModifier", "MoveSpeed", "Add", 1, 5, 80, 2, "", "Rare", "ICON_SPEED", true],
  ["ATTACK_RANGE_UP", "CARD_RANGE_NAME", "StatModifier", "AttackRange", "Add", 0.15, 3, 70, 3, "", "Rare", "ICON_RANGE", true],
  ["PIERCING_UP", "CARD_PIERCING_NAME", "UpgradeRank", "Piercing", "Add", 1, 5, 90, 2, "", "Common", "ICON_PIERCING", true],
  ["SEVER_TRAIL", "CARD_SEVER_NAME", "UpgradeRank", "Sever", "Add", 1.5, 1, 45, 3, "PIERCING_UP", "Epic", "ICON_SEVER", true],
  ["HIT_HEAL", "CARD_HIT_HEAL_NAME", "UpgradeRank", "HitHeal", "Add", 5, 1, 55, 4, "", "Rare", "ICON_HIT_HEAL", true],
  ["STATIC_CHARGE", "CARD_STATIC_NAME", "UpgradeRank", "StaticCharge", "Add", 0.75, 5, 60, 4, "", "Rare", "ICON_STATIC", true],
  ["MOVING_SLASH", "CARD_MOVING_SLASH_NAME", "UpgradeRank", "MovingSlash", "Add", 1.5, 5, 65, 3, "", "Rare", "ICON_MOVING_SLASH", true],
];

const enemySheet = writeDataSheet(
  "EnemyBalance",
  enemyHeaders,
  enemyRows,
  "EnemyBalanceTable",
  [18, 12, 11, 12, 13, 13, 17, 14, 15, 14, 15, 18, 14, 10, 12, 17, 18, 18, 23, 18, 11],
  {
    C: "0.00", D: "0.00", F: "0.00", G: "0.00", H: "0.00",
    I: "0.00", J: "0.00", K: "0.00", L: "0.00", O: "0.000",
    P: "0.000",
  },
);

const stageSheet = writeDataSheet(
  "StageSpawn",
  [
    "StageId", "WaveId", "SpawnTimeSec", "SpawnIndex",
    "SpawnPointId", "EnemyId", "EnemyLevel",
  ],
  spawnRows,
  "StageSpawnTable",
  [12, 12, 14, 12, 15, 19, 12],
  { C: "0.00", D: "0", G: "0" },
);
stageSheet.getRange(`A2:G${spawnLastRow}`).conditionalFormats.add(
  "Custom",
  {
    formula: "=MOD(ROW(),2)=0",
    format: { fill: "#F4FBF4" },
  },
);

writeDataSheet(
  "PlayerBalance",
  [
    "PlayerId", "StartLevel", "BaseMaxHp", "BaseAttackPower",
    "AttackGrowthMultiplier", "RearAttackMultiplier", "BaseMoveSpeed",
    "PathEnemyApproachSpeedMultiplier",
    "PostKillEscapeSpeedMultiplier", "MoveArrivalTolerance",
    "AttackRange", "BaseCriticalChance", "Enabled",
  ],
  [["LightBandit", 1, 10, 1, 1.7, 3, 10, 1.1, 1.2, 0.08, 1.2, 0, true]],
  "PlayerBalanceTable",
  [18, 12, 12, 16, 20, 19, 16, 31, 31, 22, 14, 20, 11],
  {
    D: "0.000", E: "0.000", F: "0.000", G: "0.00", H: "0.00",
    I: "0.00", J: "0.00", K: "0.00", L: "0.0%",
  },
);

const playerLevelSheet = writeDataSheet(
  "PlayerLevelExp",
  ["Level", "RequiredExp", "CumulativeExpToNext"],
  playerLevelRows,
  "PlayerLevelExpTable",
  [11, 16, 23],
  { A: "0", B: "0", C: "0" },
);
playerLevelSheet.getRange("C2").formulas = [["=SUM($B$2:B2)"]];
playerLevelSheet.getRange("C2:C21").fillDown();
playerLevelSheet.getRange("C2:C21").format.font = {
  name: "Aptos",
  size: 10,
  color: darkGreen,
};

writeDataSheet(
  "AccountLevelExp",
  ["Level", "RequiredExp"],
  [[1, 40], [2, 150], [3, 300], [4, 500]],
  "AccountLevelExpTable",
  [11, 16],
  { A: "0", B: "0" },
);

writeDataSheet(
  "GlobalBalance",
  [
    "AccountExpScoreUnit", "AccountExpPerUnit",
    "CriticalChancePerCard", "MaximumCriticalChance",
  ],
  [[5, 1, 0.05, 0.5]],
  "GlobalBalanceTable",
  [23, 21, 24, 26],
  { A: "0", B: "0", C: "0.0%", D: "0.0%" },
);

const cardSheet = writeDataSheet(
  "LevelUpCard",
  cardHeaders,
  cardRows,
  "LevelUpCardTable",
  [22, 24, 16, 18, 12, 11, 11, 17, 17, 20, 12, 22, 11],
  { F: "0.00", G: "0", H: "0", I: "0" },
);
cardSheet.getRange("J2:J10").format.font = {
  name: "Aptos",
  size: 10,
  color: blue,
};

const readme = getOrAddSheet("README");
resetSheet(readme);
styleTitle(readme, "A1:F1", "SimpleGame 2분 밸런스 / Excel → Unity 데이터");
readme.getRange("A3:F3").values = [[
  "순서", "작업", "검증 포인트", "필수", "메뉴/ID", "비고",
]];
styleSection(readme, "A3:F3");
const readmeRows = [
  [1, "이 파일의 입력값·스폰표를 수정하고 저장", "파란 입력 / 초록 수식", "Y", "Planning/GameData_2min_Balance.xlsx", "출력본과 동일한 구조"],
  [2, "Unity에서 Excel Import 실행", "Console에 250 spawns", "Y", "SimpleGame > Data > Import Excel", "오류 시 기존 SO 유지"],
  [3, "EditMode 테스트 실행", "카드 수식·선행 조건·겹침 수학", "Y", "SimpleGame.Tests.EditMode", "빌드 오류 0 확인"],
  [4, "2분 플레이 테스트", "웨이브마다 적 수 증가 / 후반 적 레벨 우위", "Y", "Stage01", "12웨이브 / 250마리"],
];
readme.getRange("A4:F7").values = readmeRows;
readme.getRange("A9:F9").values = [[
  "시트", "역할", "핵심 내용", "수정 권장", "Unity Import", "검증",
]];
styleSection(readme, "A9:F9");
const sheetDescriptions = [
  ["EnemyBalance", "적 기본 수치", "HP·공격·EXP·점수", "Y", "Y", "4종"],
  ["StageSpawn", "개별 스폰 명세", "시간·종류·위치·레벨", "Y", "Y", "250행"],
  ["PlayerBalance", "플레이어 기본 수치", "공격 성장·후면·이동", "Y", "Y", "1종"],
  ["PlayerLevelExp", "레벨업 경험치", "R(L)=6+2L", "Y", "Y", "20레벨"],
  ["GlobalBalance", "공통 규칙", "치명타 상한 등", "Y", "Y", "1행"],
  ["LevelUpCard", "레벨업 카드", "9종 / 절단 선행 조건", "Y", "Y", "9행"],
  ["BalanceSummary", "핵심 기획", "KPI·수학식·차트", "N", "N", "수식"],
  ["WavePlan", "웨이브 요약", "수·종류·XP·레벨 격차", "N", "N", "12웨이브"],
  ["CardMath", "신규 카드 수식", "레벨 1~5 효과", "N", "N", "수식"],
];
readme.getRange("A10:F18").values = sheetDescriptions;
readme.getRange("A20:F20").merge();
readme.getRange("A20:F20").values = [["밀도 설계 참고 자료"]];
styleSection(readme, "A20:F20");
readme.getRange("A21:F22").merge();
readme.getRange("A21:F22").values = [[
  "Vampire Survivors의 웨이브는 최소 적 수와 스폰 간격을 사용합니다. " +
  "이 구조만 참고해 2분 클릭 전투용 증가식으로 축소했습니다.\n" +
  "https://vampire-survivors.fandom.com/wiki/Enemies",
]];
readme.getRange("A21:F22").format = {
  fill: "#F8FAFC",
  font: { name: "Aptos", size: 10, color: navy },
  wrapText: true,
  verticalAlignment: "center",
  borders: { preset: "outside", style: "thin", color: lightGray },
};
setBaseFont(readme, "A1:F22");
readme.getRange("A4:F7").format.wrapText = true;
readme.getRange("A10:F18").format.wrapText = true;
readme.getRange("A4:F7").format.rowHeight = 30;
readme.getRange("A10:F18").format.rowHeight = 28;
[17, 27, 36, 13, 31, 24].forEach((width, index) => {
  readme.getRangeByIndexes(0, index, 22, 1).format.columnWidth = width;
});
readme.freezePanes.freezeRows(3);

const wavePlan = getOrAddSheet("WavePlan");
resetSheet(wavePlan);
styleTitle(wavePlan, "A1:O1", "12웨이브 / 120초 몬스터 밀도 계획");
wavePlan.getRange("A3:O3").merge();
wavePlan.getRange("A3:O3").values = [[
  "적 수 N(w)=4+2w+floor((w-1)^2/10). 10초마다 최소 밀도를 높이고 스폰 간격을 줄입니다. 플레이어 레벨은 이전 웨이브까지 모든 적 처치를 가정합니다.",
]];
wavePlan.getRange("A3:O3").format = {
  fill: lightGreen,
  font: { name: "Aptos", size: 10, color: darkGreen, italic: true },
  wrapText: true,
  verticalAlignment: "center",
};
wavePlan.getRange("A5:O5").values = [[
  "WaveId", "StartSec", "Count", "Melee", "Ranged", "Shield", "Boss",
  "WaveXP", "CumulativeXP", "ExpectedPlayerLvStart",
  "MinEnemyLv", "MaxEnemyLv", "AvgEnemyLv", "AvgLevelGap", "PositionRule",
]];
styleSection(wavePlan, "A5:O5");
const waveRows = Array.from({ length: 12 }, (_, index) => [
  `WAVE_${String(index + 1).padStart(2, "0")}`,
  1 + index * 10,
  null, null, null, null, null, null, null, null, null, null, null, null,
  "4방향 스폰포인트 순환 / 개별 위치는 StageSpawn 참조",
]);
wavePlan.getRange("A6:O17").values = waveRows;
for (let row = 6; row <= 17; row++) {
  wavePlan.getRange(`C${row}`).formulas = [[
    `=COUNTIF('StageSpawn'!$B$2:$B$${spawnLastRow},A${row})`,
  ]];
  wavePlan.getRange(`D${row}`).formulas = [[
    `=COUNTIFS('StageSpawn'!$B$2:$B$${spawnLastRow},A${row},'StageSpawn'!$F$2:$F$${spawnLastRow},"GoblinMelee")`,
  ]];
  wavePlan.getRange(`E${row}`).formulas = [[
    `=COUNTIFS('StageSpawn'!$B$2:$B$${spawnLastRow},A${row},'StageSpawn'!$F$2:$F$${spawnLastRow},"GoblinRanged")`,
  ]];
  wavePlan.getRange(`F${row}`).formulas = [[
    `=COUNTIFS('StageSpawn'!$B$2:$B$${spawnLastRow},A${row},'StageSpawn'!$F$2:$F$${spawnLastRow},"ShieldSkeleton")`,
  ]];
  wavePlan.getRange(`G${row}`).formulas = [[
    `=COUNTIFS('StageSpawn'!$B$2:$B$${spawnLastRow},A${row},'StageSpawn'!$F$2:$F$${spawnLastRow},"GoblinBoss")`,
  ]];
  wavePlan.getRange(`H${row}`).formulas = [[
    `=D${row}*'EnemyBalance'!$M$2+E${row}*'EnemyBalance'!$M$3+F${row}*'EnemyBalance'!$M$4+G${row}*'EnemyBalance'!$M$5`,
  ]];
  wavePlan.getRange(`I${row}`).formulas = [[
    `=SUM($H$6:H${row})`,
  ]];
  const previousXp = row === 6 ? "0" : `I${row - 1}`;
  wavePlan.getRange(`J${row}`).formulas = [[
    `=1+COUNTIF('PlayerLevelExp'!$C$2:$C$21,"<="&${previousXp})`,
  ]];
  wavePlan.getRange(`K${row}`).formulas = [[
    `=MINIFS('StageSpawn'!$G$2:$G$${spawnLastRow},'StageSpawn'!$B$2:$B$${spawnLastRow},A${row})`,
  ]];
  wavePlan.getRange(`L${row}`).formulas = [[
    `=MAXIFS('StageSpawn'!$G$2:$G$${spawnLastRow},'StageSpawn'!$B$2:$B$${spawnLastRow},A${row})`,
  ]];
  wavePlan.getRange(`M${row}`).formulas = [[
    `=AVERAGEIF('StageSpawn'!$B$2:$B$${spawnLastRow},A${row},'StageSpawn'!$G$2:$G$${spawnLastRow})`,
  ]];
  wavePlan.getRange(`N${row}`).formulas = [[`=M${row}-J${row}`]];
}
const waveTable = wavePlan.tables.add("A5:O17", true, "WavePlanTable");
waveTable.style = "TableStyleMedium4";
setBaseFont(wavePlan, "A1:O17");
wavePlan.getRange("A6:O17").format.rowHeight = 25;
wavePlan.getRange("O6:O17").format.wrapText = true;
wavePlan.getRange("B6:N17").format.numberFormat = "0";
wavePlan.getRange("M6:N17").format.numberFormat = "0.0";
wavePlan.getRange("N6:N17").conditionalFormats.add("colorScale", {
  colors: ["#DCFCE7", "#FEF3C7", "#FECACA"],
  thresholds: ["min", "50%", "max"],
});
[13, 11, 9, 9, 9, 9, 9, 11, 16, 23, 13, 13, 14, 16, 46]
  .forEach((width, index) => {
    wavePlan.getRangeByIndexes(0, index, 17, 1)
      .format.columnWidth = width;
  });
wavePlan.freezePanes.freezeRows(5);

const cardMath = getOrAddSheet("CardMath");
resetSheet(cardMath);
styleTitle(cardMath, "A1:L1", "신규 카드 수식 및 레벨별 효과");
cardMath.getRange("A3:B7").values = [
  ["입력", "값"],
  ["정전기 피해 배율", null],
  ["절단 추가 피해 배율", null],
  ["히트 회복량", null],
  ["히트 회복 확률", 0.05],
];
styleSection(cardMath, "A3:B3");
cardMath.getRange("B4").formulas = [["='LevelUpCard'!F9"]];
cardMath.getRange("B5").formulas = [["='LevelUpCard'!F7"]];
cardMath.getRange("B6").formulas = [["='LevelUpCard'!F8"]];
cardMath.getRange("A9:L9").values = [[
  "CardLv", "PierceAdditional", "SeverBonus", "Normal+Sever",
  "StaticAdjacent", "StaticPrimaryTotal", "StaticAdjacentDamage",
  "ExpectedHealPerHit", "MoveSlashChance", "MoveSlashMaxHits",
  "MoveSlashSize", "MoveSlashRearDamage",
]];
styleSection(cardMath, "A9:L9");
for (let level = 1; level <= 5; level++) {
  const row = 9 + level;
  cardMath.getRange(`A${row}`).values = [[level]];
  cardMath.getRange(`B${row}`).formulas = [[`=A${row}`]];
  cardMath.getRange(`C${row}`).formulas = [["=$B$5"]];
  cardMath.getRange(`D${row}`).formulas = [["=1+C10"]];
  cardMath.getRange(`E${row}`).formulas = [[`=2*A${row}+1`]];
  cardMath.getRange(`F${row}`).formulas = [["=1+$B$4"]];
  cardMath.getRange(`G${row}`).formulas = [["=$B$4"]];
  cardMath.getRange(`H${row}`).formulas = [["=$B$6*$B$7"]];
  cardMath.getRange(`I${row}`).formulas = [[
    `=10%+3%*(A${row}-1)`,
  ]];
  cardMath.getRange(`J${row}`).formulas = [[`=A${row}`]];
  cardMath.getRange(`K${row}`).formulas = [[
    `=1+10%*(A${row}-1)`,
  ]];
  cardMath.getRange(`L${row}`).formulas = [[
    `='LevelUpCard'!F10*'PlayerBalance'!F2`,
  ]];
}
const cardMathTable = cardMath.tables.add(
  "A9:L14",
  true,
  "CardMathTable",
);
cardMathTable.style = "TableStyleMedium4";
cardMath.getRange("A16:L16").merge();
cardMath.getRange("A16:L16").values = [[
  "정전기: 주 대상은 기본 공격 + 0.75A_side, 주변은 0.75A_side. 절단: 기본 공격과 별도로 1.5A_side 추가. 최대 이동속도 Lv.5에서는 v=이동거리/0.1초로 계산. 치명타는 기본 공격에만 적용하고 모든 신규 스킬에는 후면 배율을 적용.",
]];
cardMath.getRange("A16:L16").format = {
  fill: lightGreen,
  font: { name: "Aptos", size: 10, color: darkGreen },
  wrapText: true,
  rowHeight: 38,
  verticalAlignment: "center",
};
setBaseFont(cardMath, "A1:L16");
cardMath.getRange("B4:B7").format.numberFormat = "0.00";
cardMath.getRange("B7").format.numberFormat = "0.0%";
cardMath.getRange("C10:D14").format.numberFormat = "0.00x";
cardMath.getRange("F10:H14").format.numberFormat = "0.00";
cardMath.getRange("I10:I14").format.numberFormat = "0.0%";
cardMath.getRange("K10:K14").format.numberFormat = "0%";
cardMath.getRange("L10:L14").format.numberFormat = "0.00x";
[11, 18, 13, 16, 18, 20, 22, 21, 19, 20, 17, 23]
  .forEach((width, index) => {
    cardMath.getRangeByIndexes(0, index, 16, 1)
      .format.columnWidth = width;
  });
cardMath.freezePanes.freezeRows(9);

const summary = getOrAddSheet("BalanceSummary");
resetSheet(summary);
styleTitle(summary, "A1:L1", "SimpleGame 2분 밸런스 요약");
summary.getRange("A3:B3").values = [["KPI", "값"]];
styleSection(summary, "A3:B3");
summary.getRange("A4:A10").values = [
  ["플레이 시간(초)"],
  ["총 스폰 수"],
  ["총 획득 가능 XP"],
  ["예상 최종 플레이어 레벨"],
  ["레벨업 카드 횟수"],
  ["최대 평균 레벨 격차"],
  ["보스 수"],
];
summary.getRange("B4").values = [[120]];
summary.getRange("B5").formulas = [[
  `=COUNTA('StageSpawn'!$A$2:$A$${spawnLastRow})`,
]];
summary.getRange("B6").formulas = [["=SUM('WavePlan'!$H$6:$H$17)"]];
summary.getRange("B7").formulas = [[
  '=1+COUNTIF(\'PlayerLevelExp\'!$C$2:$C$21,"<="&B6)',
]];
summary.getRange("B8").formulas = [["=B7-1"]];
summary.getRange("B9").formulas = [["=MAX('WavePlan'!$N$6:$N$17)"]];
summary.getRange("B10").formulas = [[
  `=COUNTIF('StageSpawn'!$F$2:$F$${spawnLastRow},"GoblinBoss")`,
]];
summary.getRange("A4:B10").format = {
  fill: "#F8FAFC",
  font: { name: "Aptos", size: 11, color: navy },
  borders: {
    insideHorizontal: { style: "thin", color: lightGray },
    bottom: { style: "thin", color: lightGray },
    left: { style: "thin", color: lightGray },
    right: { style: "thin", color: lightGray },
  },
  verticalAlignment: "center",
};
summary.getRange("B4:B10").format.font = {
  name: "Aptos Display",
  size: 14,
  bold: true,
  color: darkGreen,
};
summary.getRange("B9").format.numberFormat = "0.0";

summary.getRange("D3:L3").merge();
summary.getRange("D3:L3").values = [["핵심 수학 모델"]];
styleSection(summary, "D3:L3");
const formulas = [
  ["플레이어 공격", "A(P) = BaseAttack × 1.7^(P−1)"],
  ["적 체력", "H(E) = BaseHP × 1.7^(max(1,E−Offset)−1)"],
  ["적 공격", "D(E) = ceil(BaseDamage × (1 + 0.05(E−1)))"],
  ["필요 경험치", "R(L) = 6 + 2L, 누적 C(L) = Σ R(k)"],
  ["정전기", "주 대상 = 기본 + 0.75A_side, 주변 = 0.75A_side, 수 = 2L+1"],
  ["절단", "관통 경로 대상마다 기본 피해 + 1.5A_side"],
  ["이동 참격", "확률 = 10% + 3%(L−1), 크기 = 1 + 10%(L−1), 총 타격 = L"],
  ["후면 규칙", "A_side = A(P) × 3 (후면), 신규 스킬에도 동일 적용"],
];
summary.getRange("D4:E11").values = formulas;
summary.getRange("D4:D11").format = {
  fill: lightGreen,
  font: { name: "Aptos", size: 10, bold: true, color: darkGreen },
  verticalAlignment: "center",
};
summary.getRange("E4:L11").merge(true);
for (let row = 4; row <= 11; row++) {
  summary.getRange(`E${row}:L${row}`).values = [[formulas[row - 4][1]]];
}
summary.getRange("E4:L11").format = {
  fill: "#FFFFFF",
  font: { name: "Aptos", size: 10, color: navy },
  wrapText: true,
  verticalAlignment: "center",
};
summary.getRange("D4:L11").format.borders = {
  preset: "all",
  style: "thin",
  color: lightGray,
};
summary.getRange("D4:L11").format.rowHeight = 28;

summary.getRange("A13:C13").values = [[
  "Wave", "EnemyCount", "AvgEnemyLevel",
]];
styleSection(summary, "A13:C13");
for (let index = 0; index < 12; index++) {
  const row = 14 + index;
  const sourceRow = 6 + index;
  summary.getRange(`A${row}:C${row}`).formulas = [[
    `='WavePlan'!A${sourceRow}`,
    `='WavePlan'!C${sourceRow}`,
    `='WavePlan'!M${sourceRow}`,
  ]];
}
const chart = summary.charts.add(
  "line",
  summary.getRange("A13:C25"),
);
chart.title = "웨이브별 적 수와 평균 적 레벨";
chart.hasLegend = true;
chart.xAxis = { axisType: "textAxis", textStyle: { fontSize: 9 } };
chart.yAxis = { numberFormatCode: "0.0", min: 0 };
chart.setPosition("E13", "L26");

summary.getRange("A27:L29").merge();
summary.getRange("A27:L29").values = [[
  "밀도 의도: Vampire Survivors의 시간 구간별 최소 적 밀도와 짧아지는 스폰 간격을 참고하되 클릭 전투에 맞게 축소했습니다. N(w)=4+2w+floor((w-1)^2/10)으로 W1 6마리에서 W12 40마리까지 증가합니다. 일반 적 EXP를 낮춰 전부 처치해도 최종 레벨은 약 14로 제한하며, 후반 적 레벨은 예상 플레이어보다 높게 유지합니다.",
]];
summary.getRange("A27:L29").format = {
  fill: "#FFF7ED",
  font: { name: "Aptos", size: 10, color: "#9A3412" },
  wrapText: true,
  verticalAlignment: "center",
  borders: { preset: "outside", style: "thin", color: amber },
};
setBaseFont(summary, "A1:L29");
summary.getRange("C14:C25").format.numberFormat = "0.0";
summary.getRange("A1:A29").format.columnWidth = 24;
summary.getRange("B1:B29").format.columnWidth = 16;
summary.getRange("C1:C29").format.columnWidth = 16;
summary.getRange("D1:D29").format.columnWidth = 18;
for (const column of ["E", "F", "G", "H", "I", "J", "K", "L"]) {
  summary.getRange(`${column}1:${column}29`).format.columnWidth = 13;
}
summary.freezePanes.freezeRows(1);

summary.getRange("A27:L29").values = [[
  "밀도 의도: Vampire Survivors의 시간 구간별 최소 적 밀도와 짧아지는 스폰 간격을 참고하되 클릭 전투에 맞게 축소했습니다. N(w)=4+2w+floor((w-1)^2/10)으로 W1 6마리에서 W12 40마리까지 증가합니다. 일반 적 EXP를 낮춰 전부 처치해도 최종 레벨은 약 14로 제한하며, 후반 적 레벨은 예상 플레이어보다 높게 유지합니다.",
]];
restyleTitle(readme, "A1:F1");
restyleTitle(wavePlan, "A1:O1");
restyleTitle(cardMath, "A1:L1");
restyleTitle(summary, "A1:L1");

function assetHeader(scriptGuid, name, editorClass) {
  return [
    "%YAML 1.1",
    "%TAG !u! tag:unity3d.com,2011:",
    "--- !u!114 &11400000",
    "MonoBehaviour:",
    "  m_ObjectHideFlags: 0",
    "  m_CorrespondingSourceObject: {fileID: 0}",
    "  m_PrefabInstance: {fileID: 0}",
    "  m_PrefabAsset: {fileID: 0}",
    "  m_GameObject: {fileID: 0}",
    "  m_Enabled: 1",
    "  m_EditorHideFlags: 0",
    `  m_Script: {fileID: 11500000, guid: ${scriptGuid}, type: 3}`,
    `  m_Name: ${name}`,
    `  m_EditorClassIdentifier: SimpleGame.Runtime::SimpleGame.${editorClass}`,
  ];
}

function formatUnityNumber(value) {
  return Number.isInteger(value)
    ? String(value)
    : value.toFixed(2).replace(/0+$/, "").replace(/\.$/, "");
}

const generatedDir = path.join(
  workspace,
  "Assets",
  "Game",
  "Data",
  "Generated",
);

const stageYaml = assetHeader(
  "cf74568244184064ca2a8ce836bc0bfd",
  "StageSpawnSchedule",
  "StageSpawnSchedule",
);
stageYaml.push("  entries:");
for (const row of spawnRows) {
  stageYaml.push(
    `  - stageId: ${row[0]}`,
    `    waveId: ${row[1]}`,
    `    spawnTimeSec: ${formatUnityNumber(row[2])}`,
    `    spawnIndex: ${row[3]}`,
    `    spawnPointId: ${row[4]}`,
    `    enemyId: ${row[5]}`,
    `    enemyLevel: ${row[6]}`,
  );
}
await fs.writeFile(
  path.join(generatedDir, "StageSpawnSchedule.asset"),
  `${stageYaml.join("\n")}\n`,
  "utf8",
);

const enemyYaml = assetHeader(
  "0d070aa5ecdaa5d47bd17924c3b7d0d0",
  "EnemyBalanceTable",
  "EnemyBalanceTable",
);
enemyYaml.push("  definitions:");
for (const row of enemyRows) {
  enemyYaml.push(
    `  - enemyId: ${row[0]}`,
    `    archetype: ${["Melee", "Ranged", "Shield", "Boss"].indexOf(row[1])}`,
    `    moveSpeed: ${row[2]}`,
    `    attackRange: ${row[3]}`,
    `    attackDamage: ${row[4]}`,
    `    attackWindup: ${row[5]}`,
    `    attackActiveDuration: ${row[6]}`,
    `    attackCooldown: ${row[7]}`,
    `    attackAreaRadius: ${row[8]}`,
    `    approachRange: ${row[9]}`,
    `    facingTurnDelay: ${row[10]}`,
    `    postAttackFacingLock: ${row[11]}`,
    `    killExperience: ${row[12]}`,
    `    score: ${row[13]}`,
    `    baseMaxHp: ${row[14]}`,
    `    hpGrowthMultiplier: ${row[15]}`,
    `    levelDifficultyOffset: ${row[16]}`,
    `    oneHitPlayerLevelAdvantage: ${row[18] ?? -1}`,
    `    combatProfileId: ${row[19]}`,
    `    showHpBar: ${row[20] ? 1 : 0}`,
  );
}
await fs.writeFile(
  path.join(generatedDir, "EnemyBalanceTable.asset"),
  `${enemyYaml.join("\n")}\n`,
  "utf8",
);

const levelYaml = assetHeader(
  "55973c792c1d8fc44be8a79bdef85db7",
  "PlayerLevelExperience",
  "LevelExperienceTable",
);
levelYaml.push("  rows:");
for (const row of playerLevelRows) {
  levelYaml.push(
    `  - level: ${row[0]}`,
    `    requiredExperienceToNext: ${row[1]}`,
  );
}
await fs.writeFile(
  path.join(generatedDir, "PlayerLevelExperience.asset"),
  `${levelYaml.join("\n")}\n`,
  "utf8",
);

const targetStatIds = new Map([
  ["CriticalChance", 0],
  ["MaxHp", 1],
  ["MoveSpeed", 2],
  ["AttackRange", 3],
  ["Piercing", 4],
  ["Sever", 5],
  ["HitHeal", 6],
  ["StaticCharge", 7],
  ["MovingSlash", 8],
]);
const cardYaml = assetHeader(
  "c16a655742233a54b882ebf89bf6717b",
  "LevelUpCardTable",
  "LevelUpCardTable",
);
cardYaml.push("  definitions:");
for (const row of cardRows) {
  cardYaml.push(
    `  - cardId: ${row[0]}`,
    `    nameKey: ${row[1]}`,
    `    effectType: ${row[2] === "UpgradeRank" ? 1 : 0}`,
    `    targetStat: ${targetStatIds.get(row[3])}`,
    "    operation: 0",
    `    value: ${row[5]}`,
    `    maxStack: ${row[6]}`,
    `    selectionWeight: ${row[7]}`,
    `    minPlayerLevel: ${row[8]}`,
    `    requiredCardId: ${row[9]}`,
    `    rarity: ${row[10]}`,
    `    iconId: ${row[11]}`,
    `    enabled: ${row[12] ? 1 : 0}`,
  );
}
await fs.writeFile(
  path.join(generatedDir, "LevelUpCardTable.asset"),
  `${cardYaml.join("\n")}\n`,
  "utf8",
);

const globalYaml = assetHeader(
  "14e1375fe700ee84e97b989bd1245422",
  "GlobalBalance",
  "GlobalBalance",
);
globalYaml.push(
  "  accountExperienceScoreUnit: 5",
  "  accountExperiencePerUnit: 1",
  "  criticalChancePerCard: 0.05",
  "  maximumCriticalChance: 0.5",
);
await fs.writeFile(
  path.join(generatedDir, "GlobalBalance.asset"),
  `${globalYaml.join("\n")}\n`,
  "utf8",
);

const outputWorkbook = await SpreadsheetFile.exportXlsx(workbook);
await outputWorkbook.save(outputPath);
await fs.copyFile(outputPath, sourcePath);

const checks = {};
checks.summary = (await workbook.inspect({
  kind: "table",
  range: "BalanceSummary!A1:L29",
  include: "values,formulas",
  tableMaxRows: 29,
  tableMaxCols: 12,
  maxChars: 12000,
})).ndjson;
checks.wavePlan = (await workbook.inspect({
  kind: "table",
  range: "WavePlan!A5:N17",
  include: "values,formulas",
  tableMaxRows: 13,
  tableMaxCols: 14,
  maxChars: 12000,
})).ndjson;
checks.errors = (await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
  options: { useRegex: true, maxResults: 300 },
  summary: "final formula error scan",
})).ndjson;
console.log(JSON.stringify(checks, null, 2));

for (const sheet of workbook.worksheets.items) {
  const preview = await workbook.render({
    sheetName: sheet.name,
    autoCrop: "all",
    scale: 1,
    format: "png",
  });
  const safeName = sheet.name.replace(/[^a-zA-Z0-9_-]+/g, "_");
  await fs.writeFile(
    path.join(workDir, `after-${safeName}.png`),
    new Uint8Array(await preview.arrayBuffer()),
  );
}

console.log(outputPath);
