import fs from "node:fs/promises";
import path from "node:path";
import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const workspace = "C:/Github/SimpleGame";
const inputPath = path.join(
  workspace,
  "Planning",
  "GameData_2min_Balance.xlsx"
);
const planningOutput = path.join(
  workspace,
  "Planning",
  "GameData_10min_Balance.xlsx"
);
const deliveryDir = path.join(
  workspace,
  "outputs",
  "10min-korean-balance"
);
const deliveryOutput = path.join(
  deliveryDir,
  "GameData_10min_Balance.xlsx"
);
const previewDir = path.join(
  workspace,
  "tmp",
  "artifact-work",
  "previews-after"
);

const input = await FileBlob.load(inputPath);
const workbook = await SpreadsheetFile.importXlsx(input);

const stageSheet = workbook.worksheets.getItem("StageSpawn");
const existingStageRows = stageSheet
  .getRange("A2:G251")
  .values
  .map((row) => row.slice());

const spawnPoints = [
  "TOP_01", "RIGHT_01", "BOTTOM_01", "LEFT_01",
  "TOP_02", "RIGHT_02", "BOTTOM_02", "LEFT_02",
  "TOP_03", "RIGHT_03", "BOTTOM_03", "LEFT_03",
  "TOP_04", "RIGHT_04", "BOTTOM_04", "LEFT_04",
  "TOP_05", "RIGHT_05", "BOTTOM_05", "LEFT_05",
  "TOP_06", "RIGHT_06", "BOTTOM_06", "LEFT_06",
  "TOP_07", "RIGHT_07", "BOTTOM_07", "LEFT_07",
  "TOP_08", "RIGHT_08", "BOTTOM_08", "LEFT_08",
];
const killExperience = {
  GoblinMelee: 1,
  GoblinRanged: 1,
  ShieldSkeleton: 2,
  GoblinBoss: 8,
};
const levelRequirements = Array.from(
  { length: 50 },
  (_, index) => index === 49 ? 0 : 8 + index * 2
);

function round(value, digits = 2) {
  const scale = 10 ** digits;
  return Math.round(value * scale) / scale;
}

function calculatePlayerLevel(totalExperience) {
  let level = 1;
  let remaining = Math.max(0, totalExperience);
  while (
    level < levelRequirements.length &&
    levelRequirements[level - 1] > 0 &&
    remaining >= levelRequirements[level - 1]
  ) {
    remaining -= levelRequirements[level - 1];
    level += 1;
  }
  return level;
}

function waveId(wave) {
  return `WAVE_${String(wave).padStart(2, "0")}`;
}

function waveNumber(id) {
  return Number(id.slice(id.lastIndexOf("_") + 1));
}

function distributeEnemyTypes(count, quotas, wave) {
  const types = [
    "GoblinMelee",
    "GoblinRanged",
    "ShieldSkeleton",
    "GoblinBoss",
  ];
  const used = Object.fromEntries(types.map((type) => [type, 0]));
  const result = [];
  for (let index = 0; index < count; index += 1) {
    let selected = null;
    let selectedScore = Number.NEGATIVE_INFINITY;
    for (let offset = 0; offset < types.length; offset += 1) {
      const type = types[(offset + wave) % types.length];
      if (used[type] >= quotas[type]) {
        continue;
      }
      const score =
        quotas[type] * (index + 1) / count -
        used[type];
      if (score > selectedScore) {
        selected = type;
        selectedScore = score;
      }
    }
    used[selected] += 1;
    result.push(selected);
  }
  return result;
}

let cumulativeExperience = existingStageRows.reduce(
  (sum, row) => sum + killExperience[row[5]],
  0
);
const stageRows = existingStageRows.slice();
let globalSpawnIndex = existingStageRows.length;

for (let wave = 13; wave <= 60; wave += 1) {
  const count = 40 + Math.ceil((wave - 12) / 3);
  const bossCount = wave % 12 === 0 ? 1 : 0;
  const shieldRatio = Math.min(0.22, 0.15 + (wave - 12) * 0.0015);
  const rangedRatio = Math.min(0.32, 0.25 + (wave - 12) * 0.0015);
  const shieldCount = Math.round(count * shieldRatio);
  const rangedCount = Math.round(count * rangedRatio);
  const meleeCount =
    count - shieldCount - rangedCount - bossCount;
  const quotas = {
    GoblinMelee: meleeCount,
    GoblinRanged: rangedCount,
    ShieldSkeleton: shieldCount,
    GoblinBoss: bossCount,
  };
  const enemies = distributeEnemyTypes(count, quotas, wave);
  const expectedPlayerLevel =
    calculatePlayerLevel(cumulativeExperience);
  const baseEnemyLevel = expectedPlayerLevel + 2;
  const startTime = 1 + (wave - 1) * 10;

  for (let index = 0; index < count; index += 1) {
    const enemyId = enemies[index];
    const spawnTime = round(
      startTime + (count === 1 ? 0 : 8.6 * index / (count - 1)),
      2
    );
    const levelBonus =
      (index + wave) % 9 === 0 ? 1 : 0;
    const bossBonus = enemyId === "GoblinBoss" ? 1 : 0;
    const enemyLevel =
      baseEnemyLevel + levelBonus + bossBonus;
    const point = spawnPoints[
      (globalSpawnIndex * 7 + wave * 3) %
      spawnPoints.length
    ];

    stageRows.push([
      "Stage01",
      waveId(wave),
      spawnTime,
      index + 1,
      point,
      enemyId,
      enemyLevel,
    ]);
    cumulativeExperience += killExperience[enemyId];
    globalSpawnIndex += 1;
  }
}

function replaceTable(
  sheet,
  clearRange,
  data,
  tableName
) {
  const existing = sheet.tables.items[0] ?? null;
  const style = existing?.style ?? "TableStyleMedium4";
  existing?.delete();
  sheet.getRange(clearRange).clear({ applyTo: "contents" });
  const rowCount = data.length;
  const colCount = data[0].length;
  const target = sheet.getRangeByIndexes(
    0,
    0,
    rowCount,
    colCount
  );
  target.values = data;
  const table = sheet.tables.add(target, true, tableName);
  table.style = style;
  return table;
}

const stageHeaders = [
  "StageId",
  "WaveId",
  "SpawnTimeSec",
  "SpawnIndex",
  "SpawnPointId",
  "EnemyId",
  "EnemyLevel",
];
replaceTable(
  stageSheet,
  "A1:G3000",
  [stageHeaders, ...stageRows],
  "StageSpawn10Min"
);
stageSheet.freezePanes.freezeRows(1);
stageSheet.getRange(`C2:C${stageRows.length + 1}`)
  .format.numberFormat = "0.00";
stageSheet.getRange(`D2:D${stageRows.length + 1}`)
  .format.numberFormat = "0";
stageSheet.getRange(`G2:G${stageRows.length + 1}`)
  .format.numberFormat = "0";

const playerLevelSheet =
  workbook.worksheets.getItem("PlayerLevelExp");
const playerLevelRows = levelRequirements.map(
  (required, index) => [
    index + 1,
    required,
    levelRequirements
      .slice(0, index + 1)
      .reduce((sum, value) => sum + value, 0),
  ]
);
replaceTable(
  playerLevelSheet,
  "A1:C100",
  [
    ["Level", "RequiredExp", "CumulativeExpToNext"],
    ...playerLevelRows,
  ],
  "PlayerLevelExp10Min"
);
playerLevelSheet.freezePanes.freezeRows(1);

const cardRows = [
  [
    "CRIT_CHANCE_UP",
    "CARD_CRIT_NAME",
    "치명타 강화",
    "치명타 확률이 5% 증가합니다. 최대 50%까지 적용됩니다.",
    "StatModifier",
    "CriticalChance",
    "Add",
    0.05,
    5,
    100,
    1,
    null,
    "일반",
    "ICON_CRIT",
    true,
  ],
  [
    "MAX_HP_UP",
    "CARD_HP_NAME",
    "체력 강화",
    "최대 체력과 현재 체력이 5 증가합니다.",
    "StatModifier",
    "MaxHp",
    "Add",
    5,
    5,
    100,
    1,
    null,
    "일반",
    "ICON_HP",
    true,
  ],
  [
    "MOVE_SPEED_UP",
    "CARD_SPEED_NAME",
    "이동 속도 강화",
    "이동 속도가 1 증가합니다. 최대 레벨에서는 목적지까지 약 0.1초에 이동합니다.",
    "StatModifier",
    "MoveSpeed",
    "Add",
    1,
    5,
    80,
    2,
    null,
    "희귀",
    "ICON_SPEED",
    true,
  ],
  [
    "ATTACK_RANGE_UP",
    "CARD_RANGE_NAME",
    "공격 범위 강화",
    "기본 공격 사거리가 0.15 증가합니다.",
    "StatModifier",
    "AttackRange",
    "Add",
    0.15,
    3,
    70,
    3,
    null,
    "희귀",
    "ICON_RANGE",
    true,
  ],
  [
    "PIERCING_UP",
    "CARD_PIERCING_NAME",
    "관통",
    "0.4초 판정창 동안 적 1명을 추가로 관통합니다. 반복 공격해도 판정창이 끝날 때까지 카드 레벨만큼만 추가 관통하며, 최대 5명입니다.",
    "UpgradeRank",
    "Piercing",
    "Add",
    1,
    5,
    90,
    2,
    null,
    "일반",
    "ICON_PIERCING",
    true,
  ],
  [
    "SEVER_TRAIL",
    "CARD_SEVER_NAME",
    "절단",
    "공격 0.5초 뒤 첫 공격 대상 위치부터 플레이어 현재 위치까지 참격을 만듭니다. 재사용 대기시간은 0.3초이며 구간의 적에게 기본 공격력의 2배 피해를 줍니다.",
    "UpgradeRank",
    "Sever",
    "Add",
    2,
    1,
    45,
    3,
    "PIERCING_UP",
    "영웅",
    "ICON_SEVER",
    true,
  ],
  [
    "HIT_HEAL",
    "CARD_HIT_HEAL_NAME",
    "흡혈",
    "적에게 피해를 줄 때마다 5% 확률로 체력을 카드 레벨당 2 회복합니다. 1/2/3레벨 회복량은 2/4/6입니다.",
    "UpgradeRank",
    "HitHeal",
    "Add",
    2,
    3,
    55,
    4,
    null,
    "희귀",
    "ICON_HIT_HEAL",
    true,
  ],
  [
    "STATIC_CHARGE",
    "CARD_STATIC_NAME",
    "정전기",
    "공격 대상과 주변 적에게 공격력의 0.75배 피해를 줍니다. 레벨마다 주변 대상이 2명 증가합니다.",
    "UpgradeRank",
    "StaticCharge",
    "Add",
    0.75,
    5,
    60,
    4,
    null,
    "희귀",
    "ICON_STATIC",
    true,
  ],
  [
    "MOVING_SLASH",
    "CARD_MOVING_SLASH_NAME",
    "참격",
    "이동 시 참격을 생성합니다. 레벨마다 확률 3%, 관통 1명, 크기 10%가 증가합니다.",
    "UpgradeRank",
    "MovingSlash",
    "Add",
    1.5,
    5,
    65,
    3,
    null,
    "희귀",
    "ICON_MOVING_SLASH",
    true,
  ],
  [
    "SHIELD_BYPASS",
    "CARD_SHIELD_BYPASS_NAME",
    "방패 우회",
    "방패병 정면 공격 시 반동과 0.5초 조작 불가를 무시할 확률이 레벨마다 10% 증가합니다.",
    "UpgradeRank",
    "ShieldBypass",
    "Add",
    0.1,
    3,
    55,
    3,
    null,
    "희귀",
    "ICON_SHIELD_BYPASS",
    true,
  ],
];
const cardHeaders = [
  "CardId",
  "NameKey",
  "DisplayName",
  "Description",
  "EffectType",
  "TargetStat",
  "Operation",
  "Value",
  "MaxStack",
  "SelectionWeight",
  "MinPlayerLevel",
  "RequiredCardId",
  "Rarity",
  "IconId",
  "Enabled",
];
const cardSheet = workbook.worksheets.getItem("LevelUpCard");
replaceTable(
  cardSheet,
  "A1:O30",
  [cardHeaders, ...cardRows],
  "LevelUpCard10Min"
);
cardSheet.freezePanes.freezeRows(1);
cardSheet.getRange("C1:C11").format.columnWidth = 18;
cardSheet.getRange("D1:D11").format.columnWidth = 58;
cardSheet.getRange("D2:D11").format.wrapText = true;
cardSheet.getRange("D2:D11").format.rowHeight = 44;
cardSheet.getRange("H2:H11").format.numberFormat = "0.00";

const grouped = new Map();
for (const row of stageRows) {
  const id = row[1];
  if (!grouped.has(id)) {
    grouped.set(id, []);
  }
  grouped.get(id).push(row);
}

let runningExperience = 0;
const waveSummaries = [];
for (let wave = 1; wave <= 60; wave += 1) {
  const id = waveId(wave);
  const rows = grouped.get(id);
  const counts = {
    GoblinMelee: 0,
    GoblinRanged: 0,
    ShieldSkeleton: 0,
    GoblinBoss: 0,
  };
  let waveExperience = 0;
  let levelTotal = 0;
  let minLevel = Number.POSITIVE_INFINITY;
  let maxLevel = 0;
  for (const row of rows) {
    counts[row[5]] += 1;
    waveExperience += killExperience[row[5]];
    levelTotal += row[6];
    minLevel = Math.min(minLevel, row[6]);
    maxLevel = Math.max(maxLevel, row[6]);
  }
  const expectedPlayerLevel =
    calculatePlayerLevel(runningExperience);
  runningExperience += waveExperience;
  const averageLevel = levelTotal / rows.length;
  waveSummaries.push({
    id,
    start: Math.min(...rows.map((row) => row[2])),
    count: rows.length,
    melee: counts.GoblinMelee,
    ranged: counts.GoblinRanged,
    shield: counts.ShieldSkeleton,
    boss: counts.GoblinBoss,
    waveExperience,
    cumulativeExperience: runningExperience,
    expectedPlayerLevel,
    minLevel,
    maxLevel,
    averageLevel,
    averageGap: averageLevel - expectedPlayerLevel,
  });
}

const wavePlanSheet = workbook.worksheets.getItem("WavePlan");
wavePlanSheet.getRange("A1:O1").unmerge();
wavePlanSheet.getRange("A1:O1").clear({ applyTo: "contents" });
wavePlanSheet.getRange("A1:O1").merge();
wavePlanSheet.getRange("A1").values = [[
  "60웨이브 / 10분 몬스터 밀도 계획",
]];
wavePlanSheet.getRange("A3:O3").unmerge();
wavePlanSheet.getRange("A3:O3").clear({ applyTo: "contents" });
wavePlanSheet.getRange("A3:O3").merge();
wavePlanSheet.getRange("A3").values = [[
  "기존 1~12웨이브 250마리를 유지하고, 이후 적 수 N(w)=40+⌈(w-12)/3⌉로 증가합니다. 마지막 스폰은 09:59.6이며 적 레벨은 예상 플레이어보다 높게 유지합니다.",
]];
const wavePlanHeaders = [
  "웨이브",
  "시작 초",
  "적 수",
  "근접",
  "원거리",
  "방패",
  "보스",
  "웨이브 경험치",
  "누적 경험치",
  "예상 플레이어 레벨",
  "최소 적 레벨",
  "최대 적 레벨",
  "평균 적 레벨",
  "평균 레벨 차이",
  "위치 규칙",
];
const wavePlanRows = waveSummaries.map((wave) => [
  wave.id,
  wave.start,
  wave.count,
  wave.melee,
  wave.ranged,
  wave.shield,
  wave.boss,
  wave.waveExperience,
  wave.cumulativeExperience,
  wave.expectedPlayerLevel,
  wave.minLevel,
  wave.maxLevel,
  round(wave.averageLevel, 1),
  round(wave.averageGap, 1),
  "상·우·하·좌 32개 지점을 순환하며 같은 위치 연속 생성을 피함",
]);
const oldWaveTable = wavePlanSheet.tables.items[0] ?? null;
const waveStyle = oldWaveTable?.style ?? "TableStyleMedium4";
oldWaveTable?.delete();
wavePlanSheet.getRange("A5:O100").clear({ applyTo: "contents" });
wavePlanSheet.getRange("A5:O65").values = [
  wavePlanHeaders,
  ...wavePlanRows,
];
const waveTable = wavePlanSheet.tables.add(
  "A5:O65",
  true,
  "WavePlan10Min"
);
waveTable.style = waveStyle;
for (let row = 6; row <= 65; row += 1) {
  wavePlanSheet.getRange(`C${row}`).formulas = [[
    `=COUNTIF('StageSpawn'!$B$2:$B$${stageRows.length + 1},A${row})`,
  ]];
  wavePlanSheet.getRange(`D${row}`).formulas = [[
    `=COUNTIFS('StageSpawn'!$B$2:$B$${stageRows.length + 1},A${row},'StageSpawn'!$F$2:$F$${stageRows.length + 1},"GoblinMelee")`,
  ]];
  wavePlanSheet.getRange(`E${row}`).formulas = [[
    `=COUNTIFS('StageSpawn'!$B$2:$B$${stageRows.length + 1},A${row},'StageSpawn'!$F$2:$F$${stageRows.length + 1},"GoblinRanged")`,
  ]];
  wavePlanSheet.getRange(`F${row}`).formulas = [[
    `=COUNTIFS('StageSpawn'!$B$2:$B$${stageRows.length + 1},A${row},'StageSpawn'!$F$2:$F$${stageRows.length + 1},"ShieldSkeleton")`,
  ]];
  wavePlanSheet.getRange(`G${row}`).formulas = [[
    `=COUNTIFS('StageSpawn'!$B$2:$B$${stageRows.length + 1},A${row},'StageSpawn'!$F$2:$F$${stageRows.length + 1},"GoblinBoss")`,
  ]];
  wavePlanSheet.getRange(`H${row}`).formulas = [[
    `=D${row}*'EnemyBalance'!$M$2+E${row}*'EnemyBalance'!$M$3+F${row}*'EnemyBalance'!$M$4+G${row}*'EnemyBalance'!$M$5`,
  ]];
  wavePlanSheet.getRange(`I${row}`).formulas = [[
    `=SUM($H$6:H${row})`,
  ]];
}
wavePlanSheet.freezePanes.freezeRows(5);
wavePlanSheet.getRange("O1:O65").format.columnWidth = 46;
wavePlanSheet.getRange("B6:B65").format.numberFormat = "0.00";
wavePlanSheet.getRange("M6:N65").format.numberFormat = "0.0";

const cardMathSheet = workbook.worksheets.getItem("CardMath");
cardMathSheet.tables.items[0]?.delete();
cardMathSheet.getRange("A1:L1").unmerge();
cardMathSheet.getRange("A1:M1").merge();
cardMathSheet.getRange("A16:M18").unmerge();
cardMathSheet.getRange("A16:M18").merge();
cardMathSheet.getRange("A1:M20").clear({ applyTo: "contents" });
cardMathSheet.getRange("A1").values = [[
  "카드 수식과 레벨별 효과",
]];
cardMathSheet.getRange("A3:B8").values = [
  ["입력", "값"],
  ["정전기 피해 배율", 0.75],
  ["절단 추가 피해 배율", 2],
  ["흡혈 레벨당 회복량", 2],
  ["회복 확률", 0.05],
  ["방패 우회 레벨당 확률", 0.1],
];
const cardMathHeaders = [
  "카드 레벨",
  "추가 관통",
  "절단 추가 피해",
  "기본+절단",
  "정전기 주변 대상",
  "정전기 주 대상 총배율",
  "정전기 주변 피해",
  "흡혈 기대 회복(최대 3레벨)",
  "이동 참격 확률",
  "이동 참격 최대 적중",
  "이동 참격 크기",
  "이동 참격 후면 피해",
  "방패 우회 확률",
];
const cardMathValues = Array.from(
  { length: 5 },
  (_, index) => {
    const level = index + 1;
    return [
      level,
      level,
      2,
      3,
      level * 2 + 1,
      1.75,
      0.75,
      Math.min(3, level) * 0.1,
      0.1 + 0.03 * index,
      level,
      1 + 0.1 * index,
      4.5,
      Math.min(3, level) * 0.1,
    ];
  }
);
cardMathSheet.getRange("A9:M14").values = [
  cardMathHeaders,
  ...cardMathValues,
];
const cardMathTable = cardMathSheet.tables.add(
  "A9:M14",
  true,
  "CardMath10Min"
);
cardMathTable.style = "TableStyleMedium4";
for (let row = 10; row <= 14; row += 1) {
  cardMathSheet.getRange(`B${row}`).formulas = [[`=A${row}`]];
  cardMathSheet.getRange(`C${row}`).formulas = [["=$B$5"]];
  cardMathSheet.getRange(`D${row}`).formulas = [[`=1+C${row}`]];
  cardMathSheet.getRange(`E${row}`).formulas = [[`=2*A${row}+1`]];
  cardMathSheet.getRange(`F${row}`).formulas = [["=1+$B$4"]];
  cardMathSheet.getRange(`G${row}`).formulas = [["=$B$4"]];
  cardMathSheet.getRange(`H${row}`).formulas = [[
    `=MIN(3,A${row})*$B$6*$B$7`,
  ]];
  cardMathSheet.getRange(`I${row}`).formulas = [[
    `=10%+3%*(A${row}-1)`,
  ]];
  cardMathSheet.getRange(`J${row}`).formulas = [[`=A${row}`]];
  cardMathSheet.getRange(`K${row}`).formulas = [[
    `=100%+10%*(A${row}-1)`,
  ]];
  cardMathSheet.getRange(`L${row}`).formulas = [["=1.5*3"]];
  cardMathSheet.getRange(`M${row}`).formulas = [[
    `=MIN(3,A${row})*$B$8`,
  ]];
}
cardMathSheet.getRange("A16").values = [[
  "관통: 길이 0.4초인 판정창에서 추가 관통 누적 수 C는 C≤L을 만족합니다. 반복 클릭은 창을 초기화하지 않으며 0.4초 뒤에만 예산 L이 복구됩니다.\n절단: 공격 1회당 최대 1개, 재사용 0.3초입니다. 0.5초 뒤 첫 공격점 p₀부터 현재 위치까지 선분 S를 만들고 겹친 적에게 2A_side 피해를 줍니다.\n흡혈: 확률은 5%로 고정하고 카드 레벨 L≤3에서 회복량은 2L, 타격당 기대 회복량은 0.1L HP입니다.\n방패 우회: 방패병 정면 공격 반동이 발생할 때만 확률을 굴립니다. 1/2/3레벨 확률은 10%/20%/30%입니다.",
]];
cardMathSheet.getRange("I10:I14").format.numberFormat = "0.0%";
cardMathSheet.getRange("K10:K14").format.numberFormat = "0%";
cardMathSheet.getRange("M10:M14").format.numberFormat = "0%";
cardMathSheet.getRange("B7:B8").format.numberFormat = "0.0%";
cardMathSheet.getRange("A1:M1").format = {
  fill: "#17652B",
  font: { bold: true, color: "#FFFFFF" },
};
cardMathSheet.getRange("A16:M18").format.wrapText = true;
cardMathSheet.getRange("A16:M18").format.rowHeight = 22;

const summarySheet =
  workbook.worksheets.getItem("BalanceSummary");
const drawingInspect = await workbook.inspect({
  kind: "drawing",
  sheetId: summarySheet.name,
  maxChars: 3000,
});
console.log(drawingInspect.ndjson);
summarySheet.deleteAllDrawings();
summarySheet.getRange("A1:L1").unmerge();
summarySheet.getRange("A1:L1").clear({ applyTo: "contents" });
summarySheet.getRange("A1:L1").merge();
summarySheet.getRange("A1").values = [[
  "SimpleGame 10분 밸런스 요약",
]];
summarySheet.getRange("A3").values = [["핵심 지표"]];
summarySheet.getRange("A4:A10").values = [
  ["플레이 시간(초)"],
  ["총 스폰 수"],
  ["총 획득 가능 경험치"],
  ["예상 최종 플레이어 레벨"],
  ["획득 가능한 카드 레벨 합계"],
  ["최대 평균 레벨 차이"],
  ["보스 수"],
];
summarySheet.getRange("B4:B10").formulas = [
  ["=600"],
  [`=COUNTA('StageSpawn'!$A$2:$A$${stageRows.length + 1})`],
  ["=MAX('WavePlan'!$I$6:$I$65)"],
  ["=MIN(50,MATCH(B6,'PlayerLevelExp'!$C$2:$C$51,1)+1)"],
  ["=38"],
  ["=MAX('WavePlan'!$N$6:$N$65)"],
  [`=COUNTIF('StageSpawn'!$F$2:$F$${stageRows.length + 1},"GoblinBoss")`],
];
summarySheet.getRange("D3:L3").unmerge();
summarySheet.getRange("D3:L3").clear({ applyTo: "contents" });
summarySheet.getRange("D3:L3").merge();
summarySheet.getRange("D3").values = [["핵심 수학 모델"]];
summarySheet.getRange("D4:L12").unmerge();
summarySheet.getRange("D4:L12").clear({ applyTo: "contents" });
const modelRows = [
  ["플레이어 공격력", "A(P)=기본 공격력 × 1.7^(P-1)"],
  ["적 체력", "H(E)=기본 체력 × 1.7^(max(1,E-보정)-1)"],
  ["적 공격력", "D(E)=ceil(기본 피해 × (1+0.05(E-1)))"],
  ["필요 경험치", "R(L)=6+2L, 50레벨 행은 0으로 종료"],
  ["10분 적 수", "1~12웨이브 유지, 이후 N(w)=40+ceil((w-12)/3)"],
  ["관통", "0.4초 판정창 내 추가 관통 누적 C≤카드 레벨 L"],
  ["정전기", "주 대상=기본+0.75A_side, 주변=0.75A_side, 수=2L+1"],
  ["절단", "공격당 1개·지연 0.5초·재사용 0.3초·선분 피해 2A_side"],
  ["방패 우회", "확률=min(30%,10%×L), 방패 반동에만 적용"],
];
for (let index = 0; index < modelRows.length; index += 1) {
  const row = 4 + index;
  summarySheet.getRange(`D${row}`).values = [[modelRows[index][0]]];
  summarySheet.getRange(`E${row}:L${row}`).merge();
  summarySheet.getRange(`E${row}`).values = [[modelRows[index][1]]];
}
summarySheet.getRange("A13:C25").clear({ applyTo: "contents" });
summarySheet.getRange("A13:C23").values = [
  ["시간", "1분간 적 수", "구간 끝 평균 적 레벨"],
  ...Array.from({ length: 10 }, (_, index) => {
    const minute = index + 1;
    const lastWave = waveSummaries[minute * 6 - 1];
    return [
      `${minute}분`,
      stageRows.filter(
        (row) =>
          row[2] > (minute - 1) * 60 &&
          row[2] <= minute * 60
      ).length,
      round(lastWave.averageLevel, 1),
    ];
  }),
];
for (let row = 14; row <= 23; row += 1) {
  const minute = row - 13;
  summarySheet.getRange(`B${row}`).formulas = [[
    `=COUNTIFS('StageSpawn'!$C$2:$C$${stageRows.length + 1},">${(minute - 1) * 60}",'StageSpawn'!$C$2:$C$${stageRows.length + 1},"<=${minute * 60}")`,
  ]];
  summarySheet.getRange(`C${row}`).formulas = [[
    `='WavePlan'!M${5 + minute * 6}`,
  ]];
}
const chart = summarySheet.charts.add(
  "line",
  summarySheet.getRange("A13:C23")
);
chart.title = "시간대별 적 수와 적 레벨 상승";
chart.titleTextStyle.fontSize = 13;
chart.hasLegend = true;
chart.xAxis = { axisType: "textAxis", textStyle: { fontSize: 9 } };
chart.yAxis = { numberFormatCode: "0" };
chart.setPosition("F13", "L26");
summarySheet.getRange("A27:L29").unmerge();
summarySheet.getRange("A27:L29").clear({ applyTo: "contents" });
summarySheet.getRange("A27:L29").merge();
summarySheet.getRange("A27").values = [[
  "밀도 의도: 첫 2분 250마리는 그대로 유지합니다. 이후 웨이브당 적 수는 3웨이브마다 1명씩 증가해 60웨이브 56마리, 전체 2,578마리가 됩니다. 적은 09:59.6까지 생성되며 예상 플레이어보다 평균 2레벨 이상 높게 배치합니다.\n생존 보정: 플레이어는 레벨업 즉시 현재 HP를 최대 HP까지 회복합니다. 흡혈은 5% 확률 고정, L≤3에서 회복량 2L로 설계합니다.",
]];
summarySheet.getRange("A27:L29").format.wrapText = true;

const readmeSheet = workbook.worksheets.getItem("README");
readmeSheet.getRange("A1:F1").unmerge();
readmeSheet.getRange("A1:F1").clear({ applyTo: "contents" });
readmeSheet.getRange("A1:F1").merge();
readmeSheet.getRange("A1").values = [[
  "SimpleGame 10분 밸런스 / 엑셀 → 유니티 데이터",
]];
readmeSheet.getRange("A4:F7").values = [
  [1, "이 파일의 입력값·스폰표를 수정하고 저장", "파란 입력 / 초록 수식", "예", "Planning/GameData_10min_Balance.xlsx", "2분 원본에서 확장"],
  [2, "유니티에서 엑셀 불러오기 실행", "콘솔에 스폰 2,578개", "예", "SimpleGame > 데이터 > 엑셀 불러오기", "오류 시 기존 스크립터블 오브젝트 유지"],
  [3, "에디트 모드 테스트 실행", "카드 수식·방패 우회·겹침 수학", "예", "SimpleGame.Tests.EditMode", "빌드 오류 0 확인"],
  [4, "10분 플레이 테스트", "지속 스폰 / 후반 적 레벨 우위", "예", "Stage01", "60웨이브 / 2,578마리"],
];
readmeSheet.getRange("E9:F9").values = [[
  "유니티 불러오기",
  "검증",
]];
readmeSheet.getRange("A10:F19").values = [
  ["EnemyBalance", "적 기본 수치", "체력·공격·경험치 점수", "예", "예", "4종"],
  ["StageSpawn", "개별 스폰 명세", "시간·종류·위치·레벨", "예", "예", "2,578행"],
  ["PlayerBalance", "플레이어 기본 수치", "공격 성장·후면·이동", "예", "예", "1종"],
  ["PlayerLevelExp", "레벨업 경험치", "R(L)=6+2L", "예", "예", "50레벨"],
  ["AccountLevelExp", "계정 레벨 경험치", "누적 계정 성장", "예", "예", "4레벨"],
  ["GlobalBalance", "공통 규칙", "치명타 상한 등", "예", "예", "1행"],
  ["LevelUpCard", "레벨업 카드", "10종 / 한국어 이름·설명 / 절단 선행 조건", "예", "예", "10행"],
  ["BalanceSummary", "핵심 기획", "핵심 지표·수학식·차트", "아니오", "아니오", "수식"],
  ["WavePlan", "웨이브 요약", "수·종류·경험치·레벨 차이", "아니오", "아니오", "60웨이브"],
  ["CardMath", "카드 수식", "관통 판정창·절단 재사용·레벨별 효과", "아니오", "아니오", "수식"],
];
readmeSheet.getRange("A20:F20").unmerge();
readmeSheet.getRange("A20:F20").clear({ applyTo: "contents" });
readmeSheet.getRange("A20:F20").merge();
readmeSheet.getRange("A20").values = [["밀도 설계 참고 자료"]];
readmeSheet.getRange("A21:F22").unmerge();
readmeSheet.getRange("A21:F22").clear({ applyTo: "contents" });
readmeSheet.getRange("A21:F22").merge();
readmeSheet.getRange("A21").values = [[
  "뱀파이어 서바이버즈의 지속 밀도 상승 구조를 참고하되, 클릭 이동·공격 구조에 맞춰 10초 단위 60웨이브로 재설계했습니다. 첫 2분 250마리는 보존하고 3~10분은 분당 249마리에서 333마리까지 증가합니다.",
]];

const errorScan = await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
  options: { useRegex: true, maxResults: 300 },
  summary: "최종 수식 오류 검사",
});
console.log(errorScan.ndjson);

for (const target of [
  ["StageSpawn", `A1:G${stageRows.length + 1}`],
  ["PlayerLevelExp", "A1:C51"],
  ["LevelUpCard", "A1:O11"],
  ["WavePlan", "A1:O65"],
  ["CardMath", "A1:M16"],
  ["BalanceSummary", "A1:L29"],
]) {
  const check = await workbook.inspect({
    kind: "table",
    range: `${target[0]}!${target[1]}`,
    include: "values,formulas",
    tableMaxRows: target[0] === "StageSpawn" ? 8 : 70,
    tableMaxCols: 15,
    tableMaxCellChars: 140,
  });
  console.log(check.ndjson);
}

await fs.mkdir(deliveryDir, { recursive: true });
await fs.mkdir(previewDir, { recursive: true });
const previewRanges = {
  README: "A1:F22",
  EnemyBalance: "A1:M5",
  StageSpawn: "A1:G40",
  PlayerBalance: "A1:V3",
  PlayerLevelExp: "A1:C51",
  AccountLevelExp: "A1:C30",
  GlobalBalance: "A1:B30",
  LevelUpCard: "A1:O11",
  WavePlan: "A1:O65",
  CardMath: "A1:M18",
  BalanceSummary: "A1:L29",
};
for (const sheet of workbook.worksheets.items) {
  const preview = await workbook.render({
    sheetName: sheet.name,
    range: previewRanges[sheet.name],
    scale: 1,
    format: "png",
  });
  const safeName = sheet.name.replaceAll(/[<>:"/\\|?*]/g, "_");
  await fs.writeFile(
    path.join(previewDir, `${safeName}.png`),
    new Uint8Array(await preview.arrayBuffer())
  );
}
const stageTail = await workbook.render({
  sheetName: "StageSpawn",
  range: `A${stageRows.length - 38}:G${stageRows.length + 1}`,
  scale: 1,
  format: "png",
});
await fs.writeFile(
  path.join(previewDir, "StageSpawn_tail.png"),
  new Uint8Array(await stageTail.arrayBuffer())
);

const output = await SpreadsheetFile.exportXlsx(workbook);
await output.save(planningOutput);
await output.save(deliveryOutput);

console.log(JSON.stringify({
  sourceRowsPreserved: existingStageRows.length,
  totalSpawns: stageRows.length,
  totalWaves: waveSummaries.length,
  finalSpawnTime: stageRows.at(-1)[2],
  totalExperience: runningExperience,
  expectedFinalPlayerLevel:
    calculatePlayerLevel(runningExperience),
  planningOutput,
  deliveryOutput,
}));
