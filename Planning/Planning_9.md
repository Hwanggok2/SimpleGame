# Planning_9

- 문서 버전: 9.0
- 작성일: 2026-07-30
- 기준 버전: [Planning_8.md](Planning_8.md)
- 기능 기준: [FunctionalSpecification.md](FunctionalSpecification.md)
- 밸런스 원본: [GameData_10min_Balance.xlsx](GameData_10min_Balance.xlsx)
- 상태: 2차 작업 묶음 구현 및 데이터 반영

## 1. 이번 작업 범위

이번 묶음은 전체 추가 요청 중 다음 세 항목을 구현한다.

1. 레벨업 회복 변경과 필드 회복 오브젝트
2. 보스 처치 카드·리롤 보상과 기본 이동 속도 조정
3. MushroomBoss 전용 등장과 사망 독구름

비행형 일반·보스, 네 보스의 두 공격 모션 패턴, 최종 Skeleton 정면 방패, 오물 투척과 조작 패드는 다음 작업 묶음으로 유지한다.

## 2. 회복 규칙

### 2.1 레벨업

- 레벨업 1회마다 현재 HP를 2 회복한다.
- 최대 HP를 넘지 않으며 완전 회복하지 않는다.
- 한 번의 EXP 지급으로 여러 레벨이 오르면 레벨업 횟수마다 HP 2 회복과 카드 선택 1회를 누적한다.
- 이어하기의 최대 HP 부활 규칙은 그대로 유지한다.

### 2.2 필드 회복 오브젝트

| 항목 | 값 |
|---|---:|
| 최초/반복 생성 간격 | 20초 |
| 획득 회복량 | HP 5 |
| 동시 최대 수 | 3개 |
| 미획득 수명 | 45초 |
| Player 최소 생성 거리 | 2.5 unit |
| 위치 재추첨 | 최대 8회 |

- `Playing` 시간만 생성 타이머에 반영한다.
- 일시정지, 카드 선택, 게임 오버 중에는 미획득 수명도 감소하지 않는다.
- Player 주변 재사용 월드의 Spawn 경계 안에서 무작위 생성한다.
- 최대 HP 상태에서는 접촉해도 소비하지 않고, 이후 피해를 받은 채 계속 겹치면 획득할 수 있다.

## 3. 보스 공통 보상과 속도

- Boss 사망 시 기존 카드 UI를 열어 후보 3장 중 1장을 선택한다.
- 한 판 공유 리롤을 1회 충전하되 최대 3회를 넘지 않는다.
- Boss EXP로 레벨업이 동시에 발생하면 레벨업 카드와 보스 카드 선택을 모두 누적한다.

| EnemyId | 기본 이동 속도 |
|---|---:|
| GoblinBoss | 0.75 |
| MushroomBoss | 0.72 |

두 값 모두 Enemy 레벨당 1.25% 이동 속도 증가와 160% 상한을 적용한다.

## 4. MushroomBoss

- `MushroomBoss`는 `Boss` Archetype과 공통 Boss 공격 모듈을 재사용한다.
- Mushroom 전용 Idle, Run, Attack1, Take Hit, Death 리소스로 별도 Animator와 Prefab을 생성한다.
- 현재 10분 일정에서는 두 번째 보스인 WAVE_24에 한 번만 등장하며 일반 Enemy로는 생성하지 않는다.

### 4.1 독구름

| 항목 | 값 |
|---|---:|
| 사망 후 생성 지연 | 1초 |
| 지속 시간 | 5초 |
| 피해 반경 | 1.6 unit |
| 피해 주기 | 0.5초 |
| 주기당 피해 | 1 |

- Enemy가 사망 애니메이션 뒤 Pool로 돌아가도 위치를 잃지 않도록 사망 순간 좌표를 `PoisonCloudSpawner`에 값으로 전달한다.
- Player가 반경 밖으로 나오면 연속 노출 시간을 초기화한다.
- 카드 선택과 Pause의 `TimeScale=0` 동안 지연·지속·피해 시간이 함께 멈춘다.
- 5초 내내 머물면 총 10회, 최대 피해 10을 받는다.

## 5. 데이터 배치

- `EnemyBalance`는 5종으로 확장한다.
- WAVE_24의 기존 GoblinBoss 한 행을 MushroomBoss로 교체한다.
- 전체 Spawn 3,283행과 나머지 Spawn 시각·위치·레벨은 유지한다.
- 이번 단계에서 WAVE_12는 GoblinBoss, WAVE_24는 MushroomBoss다.
- WAVE_36 이후 FlyingEyeBoss와 최종 SkeletonBoss 배치는 해당 Prefab과 두 공격 패턴 구현 시 함께 교체한다.

## 6. 완료 조건

- 레벨업 후 HP가 정확히 2 증가하고 이어하기는 최대 HP로 부활한다.
- 회복 오브젝트가 20초 간격, 최대 3개로 생성되고 HP 5를 회복한다.
- Boss 처치 시 카드 선택 1회와 리롤 1회가 지급된다.
- GoblinBoss와 MushroomBoss의 기본 이동 속도가 각각 0.75와 0.72다.
- MushroomBoss가 WAVE_24에만 한 번 등장한다.
- MushroomBoss 사망 1초 뒤 독구름이 생성되고 5초 동안 내부 Player에게 0.5초마다 피해 1을 준다.
- Excel Import, Unity 컴파일과 EditMode 테스트가 통과한다.
