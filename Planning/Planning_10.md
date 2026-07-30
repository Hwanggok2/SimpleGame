# Planning_10

- 문서 버전: 10.0
- 작성일: 2026-07-30
- 기준 버전: [Planning_9.md](Planning_9.md)
- 기능 기준: [FunctionalSpecification.md](FunctionalSpecification.md)
- 밸런스 원본: [GameData_10min_Balance.xlsx](GameData_10min_Balance.xlsx)
- 상태: 3차 작업 묶음 확정 사양 및 수용 기준

## 1. 이번 작업 범위

이번 묶음은 다음 세 영역을 확정한다.

1. Flying Eye 일반형·보스형 추가와 비행형 Enemy 겹침 허용
2. 네 보스의 고정 등장 순서, Attack1·Attack2 교대 패턴, SkeletonBoss 정면 방패
3. 신규 카드 스킬 `오물 투척`

좌측 조준 조이스틱과 우측 공격 버튼은 사양만 유지하며 다음 작업 묶음에서 구현한다.

## 2. Flying Eye

### 2.1 일반형

| 항목 | 값 |
|---|---:|
| EnemyId | `FlyingEye` |
| Archetype | `Melee` |
| 기본 이동 속도 | 0.95 |
| 공격 거리 | 0.95 |
| 기본 공격 피해 | 1 |
| 공격 선딜레이 | 0.65초 |
| 공격 재사용 시간 | 1.8초 |
| 처치 경험치 | 2 |
| 처치 점수 | 8 |
| 기본 HP | 3 |
| 레벨당 HP 증가 | 0.75 |
| CombatProfileId | `FlyingMelee` |

- WAVE_25부터 기존 `GoblinMelee` 일부를 `FlyingEye`로 교체한다.
- WAVE_25~36에서는 각 Wave의 10번째 `GoblinMelee`마다 교체한다.
- WAVE_37~60에서는 각 Wave의 8번째 `GoblinMelee`마다 교체한다.
- WAVE_48의 기존 Boss 행은 일반 `FlyingEye`로 교체해 보스 수를 늘리지 않는다.
- 전체 일정에서 일반 `FlyingEye`는 186회 Spawn한다.

### 2.2 보스형

| 항목 | 값 |
|---|---:|
| EnemyId | `FlyingEyeBoss` |
| Archetype | `Boss` |
| 기본 이동 속도 | 0.88 |
| 공격 거리 | 3.6 |
| 기본 공격 피해 | 4 |
| 공격 선딜레이 | 0.9초 |
| 공격 활성 시간 | 0.5초 |
| 공격 재사용 시간 | 2.5초 |
| 공격 영역 기준 반경 | 1.6 |
| 처치 경험치 | 12 |
| 처치 점수 | 90 |
| 기본 HP | 20 |
| 레벨당 HP 증가 | 4 |
| CombatProfileId | `BossFlyingEye` |

- WAVE_36에서 세 번째 보스로 정확히 한 번 Spawn한다.
- 일반형과 동일한 Flight, Attack1, Attack2, Take Hit, Death 리소스를 사용한다.

### 2.3 겹침 정책

- `FlyingEye`와 `FlyingEyeBoss`는 생성, 이동, 이어하기 밀어내기 후 위치 보정에서 다른 Enemy와 겹칠 수 있다.
- 새 비행형 Enemy는 요청 Spawn 좌표를 그대로 사용한다.
- 지상형 Enemy도 기존 비행형 Enemy를 빈자리 탐색과 밀어내기 대상으로 취급하지 않는다.
- 비행형끼리도 서로 밀어내지 않는다.
- 지상형 Enemy끼리의 기존 생성 위치 탐색과 이동 중 분리 규칙은 그대로 유지한다.
- 겹침 허용은 이동과 배치 정책만 바꾸며 피격, 공격 대상 탐색, 보상, 레벨 성장에는 영향을 주지 않는다.

## 3. 보스 등장 순서

10분 Stage의 Boss는 다음 네 종류가 각각 한 번씩 등장한다.

| 순서 | Wave | EnemyId | Spawn 시각 | Enemy 레벨 |
|---:|---|---|---:|---:|
| 1 | WAVE_12 | `GoblinBoss` | 115.78초 | 11 |
| 2 | WAVE_24 | `MushroomBoss` | 234.34초 | 20 |
| 3 | WAVE_36 | `FlyingEyeBoss` | 354.58초 | 31 |
| 4 | WAVE_60 | `SkeletonBoss` | 594.29초 | 52 |

- WAVE_48의 기존 `GoblinBoss` 행은 일반 `FlyingEye`로 바꾼다.
- Boss 처치 시 카드 선택 1회와 리롤권 1회 충전 규칙은 네 보스에 공통 적용한다.
- `MushroomBoss`의 사망 1초 후 5초 독구름 규칙은 그대로 유지한다.

## 4. 보스 공통 2패턴

### 4.1 실행 규칙

- 모든 보스는 Attack1과 Attack2 리소스를 모두 사용한다.
- 첫 공격은 Attack1이며 이후 Attack2, Attack1 순서로 생존하는 동안 계속 교대한다.
- 공격을 시작할 때 Boss 위치와 Player 방향을 고정하고 해당 패턴의 붉은 예고 영역을 표시한다.
- 선딜레이 동안 Boss는 정지한다.
- 선딜레이 종료 시 해당 Attack 애니메이션을 재생하고 예고 영역 안의 Player에게 피해를 한 번 적용한다.
- 공격 활성 시간이 끝나면 예고 영역을 끄고 재사용 시간이 끝날 때까지 Player 추적을 재개한다.
- 패턴 전환은 실제 공격 판정이 실행된 뒤에만 진행한다.

### 4.2 패턴별 공격 범위

`전방 직사각형`은 Boss 위치에서 바라보는 방향 앞으로 뻗는다. `중심 정사각형`은 Boss를 중심으로 전 방향을 덮는다.

| Boss | 패턴 | 모션 | 형태 | 길이 | 폭 |
|---|---|---|---|---:|---:|
| GoblinBoss | Attack1 | Attack1 | 전방 직사각형 | 2.2 | 2.4 |
| GoblinBoss | Attack2 | Attack2 | 전방 직사각형 | 3.3 | 1.0 |
| MushroomBoss | Attack1 | Attack1 | 전방 직사각형 | 1.9 | 1.1 |
| MushroomBoss | Attack2 | Attack2 | 전방 직사각형 | 2.7 | 1.5 |
| FlyingEyeBoss | Attack1 | Attack1 | 전방 직사각형 | 3.6 | 0.9 |
| FlyingEyeBoss | Attack2 | Attack2 | 중심 정사각형 | 3.2 | 3.2 |
| SkeletonBoss | Attack1 | Attack1 | 전방 직사각형 | 2.3 | 2.0 |
| SkeletonBoss | Attack2 | Attack2 | 전방 직사각형 | 3.0 | 1.5 |

### 4.3 보스별 공격 시간

| Boss | 선딜레이 | 활성 시간 | 한 주기 종료 시각 |
|---|---:|---:|---:|
| GoblinBoss | 1.5초 | 0.5초 | 3.0초 |
| MushroomBoss | 1.3초 | 0.5초 | 2.8초 |
| FlyingEyeBoss | 0.9초 | 0.5초 | 2.5초 |
| SkeletonBoss | 1.2초 | 0.5초 | 3.0초 |

`한 주기 종료 시각`은 공격 시작부터 다음 공격을 시작할 수 있을 때까지의 총 시간이다.

## 5. SkeletonBoss

| 항목 | 값 |
|---|---:|
| EnemyId | `SkeletonBoss` |
| Archetype | `Boss` |
| 기본 이동 속도 | 0.8 |
| 기본 공격 피해 | 4 |
| 처치 경험치 | 15 |
| 처치 점수 | 120 |
| 기본 HP | 22 |
| 레벨당 HP 증가 | 4.2 |
| CombatProfileId | `BossSkeleton` |

- 최종 보스는 WAVE_60의 `SkeletonBoss`다.
- Archetype은 `Boss`지만 정면 방패 판정은 `ShieldSkeleton`과 동일하게 활성화한다.
- 생존 가능한 비치명 일반 정면 공격은 방패에 막히며 Player 반동과 입력 잠금 규칙을 적용한다.
- 후면 공격과 치명타는 정면 방패를 우회한다.
- 정면 공격으로 해당 타격에 처치할 수 있으면 공격과 관통을 허용한다.
- 방패 우회 카드가 성공한 경우 기존 우회 규칙을 그대로 적용한다.

## 6. 신규 스킬: 오물 투척

### 6.1 카드 데이터

| 열 | 값 |
|---|---|
| CardId | `FILTH_THROW` |
| NameKey | `CARD_FILTH_THROW_NAME` |
| DisplayName | 오물 투척 |
| EffectType | `UpgradeRank` |
| TargetStat | `FilthThrow` |
| Operation | `Add` |
| Value | 0.35 |
| MaxStack | 5 |
| SelectionWeight | 60 |
| MinPlayerLevel | 3 |
| RequiredCardId | 없음 |
| Rarity | 희귀 |
| IconId | `ICON_FILTH_THROW` |
| Enabled | TRUE |

### 6.2 발동과 연출

- 최초 카드 획득 0.25초 후 첫 투척을 실행한다.
- 이후 현재 스킬 레벨의 재사용 시간마다 자동으로 투척한다.
- 목표는 현재 카메라 화면 안의 무작위 위치다.
- 장판 반경과 0.25 unit의 시각 여백까지 화면 안에 남도록 목표 좌표를 제한한다.
- 구체는 0.45초 동안 Player 위치에서 목표 위치로 이동한다.
- 구체의 포물선 높이는 1.4 unit이며 착지 시 구체를 숨기고 장판을 표시한다.
- 전용 원화가 없는 프로토타입에서는 갈색 구체와 반투명 올리브색 장판을 사용한다.
- Pause와 카드 선택처럼 `Time.timeScale`이 0인 동안에는 재사용 시간과 장판 시간이 진행되지 않는다.

### 6.3 레벨별 밸런스

| 레벨 | 1틱 피해 배율 | 장판 반경 | 재사용 시간 | 6틱 완전 적중 피해 |
|---:|---:|---:|---:|---:|
| 1 | 공격력의 0.35배 | 1.20 | 6.0초 | 공격력의 2.10배 |
| 2 | 공격력의 0.45배 | 1.35 | 5.5초 | 공격력의 2.70배 |
| 3 | 공격력의 0.55배 | 1.50 | 5.0초 | 공격력의 3.30배 |
| 4 | 공격력의 0.65배 | 1.65 | 4.5초 | 공격력의 3.90배 |
| 5 | 공격력의 0.75배 | 1.80 | 4.0초 | 공격력의 4.50배 |

- 장판 지속 시간은 전 레벨 3초다.
- 피해 주기는 전 레벨 0.5초이며 최대 6회 판정한다.
- 각 Tick 시점에 장판 안에서 생존 중인 모든 Enemy를 공격한다.
- 장판에 늦게 들어온 Enemy는 다음 Tick부터 피해를 받고, 나간 Enemy는 피해를 받지 않는다.
- 후면에 있는 Enemy에는 기존 스킬 후면 배율을 적용한다.
- 레벨 5 이후 계산값은 레벨 5 값으로 고정한다.

## 7. XLSX 데이터 확정

### 7.1 행 수

| 시트 | 데이터 행 수 | 변경 |
|---|---:|---|
| EnemyBalance | 8 | `FlyingEye`, `FlyingEyeBoss`, `SkeletonBoss` 추가 |
| StageSpawn | 3,283 | EnemyId만 교체하며 총 Spawn 수 유지 |
| LevelUpCard | 13 | `FILTH_THROW` 1행 추가 |

### 7.2 StageSpawn 최종 분포

| EnemyId | Spawn 수 |
|---|---:|
| GoblinMelee | 1,844 |
| GoblinRanged | 861 |
| ShieldSkeleton | 388 |
| FlyingEye | 186 |
| GoblinBoss | 1 |
| MushroomBoss | 1 |
| FlyingEyeBoss | 1 |
| SkeletonBoss | 1 |
| 합계 | 3,283 |

- 전체 60 Wave와 마지막 Spawn 시각 599.60초는 유지한다.
- `WavePlan`의 일반·보스 수와 경험치 수식은 8개 EnemyId를 모두 포함한다.
- `BalanceSummary`와 `README`는 Enemy 8종, 카드 13종, Boss 4회로 표시한다.
- Excel Import 후 `EnemyBalanceTable`, `EnemyAssetCatalog`, `StageSpawnSchedule`, `LevelUpCardTable`이 같은 ID 집합을 가져야 한다.

## 8. 수용 기준

### 8.1 Flying Eye

- 일반형과 보스형 Prefab, Animator, Balance, Catalog 조회가 모두 성공한다.
- 두 Prefab 모두 Attack1과 Attack2 리소스를 가진다.
- 비행형이 포함된 Enemy 쌍은 생성과 이동 과정에서 서로 밀어내지 않는다.
- 지상형끼리는 기존 최소 간격을 유지한다.
- StageSpawn에서 일반 `FlyingEye`가 186회, `FlyingEyeBoss`가 1회다.

### 8.2 보스

- Boss Spawn 순서가 Goblin, Mushroom, Flying Eye, Skeleton이며 각 1회다.
- 네 보스 모두 Attack1 → Attack2 → Attack1 순으로 교대한다.
- 각 모션의 붉은 예고 영역 크기와 실제 피해 판정이 같은 패턴 데이터를 사용한다.
- 선딜레이 전에 피해가 발생하지 않고 활성 구간 진입 시 한 번만 피해가 발생한다.
- `SkeletonBoss`의 비치명 정면 일반 공격은 막히고 후면·치명타·처치 타격은 허용된다.

### 8.3 오물 투척

- 카드가 최대 5레벨까지 선택되고 레벨별 피해, 반경, 재사용 시간이 표와 일치한다.
- 목표 장판 전체가 카메라 화면 안에 생성된다.
- 투사체가 시작점, 포물선 정점, 착지점 순으로 이동한다.
- 장판은 0.49초에 피해가 없고 0.5초에 첫 피해, 3초에 여섯 번째이자 마지막 피해를 준다.
- Player 사망 시 진행 중인 투사체와 장판이 제거된다.

### 8.4 데이터와 회귀

- EnemyBalance 8행, LevelUpCard 13행, StageSpawn 3,283행을 Import한다.
- 중복 EnemyId, 누락 Prefab, Archetype 불일치가 없다.
- 기존 이어하기, 회복 오브젝트, Mushroom 독구름, 참격, 보스 처치 보상이 유지된다.
- Unity 컴파일 오류가 없고 전체 EditMode 테스트가 통과한다.
- XLSX 수식 오류가 없으며 전체 시트의 핵심 값과 레이아웃을 확인한다.

## 9. 다음 작업 묶음: 조준 조이스틱과 공격 버튼

다음 묶음에는 아래에서 확정한 모바일 조작만 남긴다.

- 좌측 하단에 원형 360도 조준 조이스틱을 배치한다.
- 좌측 조이스틱 입력은 Player를 이동시키지 않는다.
- 조이스틱 방향으로 Player부터 조준 지점까지 보이는 Raycast를 표시한다.
- Raycast 끝에는 기존 터치 목적지와 구분 가능한 터치 이펙트를 표시한다.
- 조이스틱 중심에서 입력점까지의 거리가 짧으면 Raycast도 짧고, 가장자리까지 밀면 최대 거리까지 길어진다.
- 조이스틱을 움직이는 동안에는 방향과 거리를 계속 갱신한다.
- 좌측 조이스틱에서 손을 뗀 순간 Raycast 끝과 터치 이펙트를 Player 위치로 되돌린다.
- 우측 하단 공격 버튼을 누르면 현재 Raycast 끝의 터치 이펙트 위치를 목적지로 사용해 기존 터치와 같은 이동 및 공격 명령을 실행한다.
- 공격 버튼 또는 월드 터치 명령 자체는 저장된 조준 위치를 Player 위치로 초기화하지 않는다.
- 조준 위치 초기화 조건은 좌측 조이스틱 입력 종료다.

다음 묶음의 수용 기준은 `조이스틱 조준 중 Player 정지`, `입력 크기와 Raycast 길이 비례`, `공격 버튼으로 현재 조준점 이동·공격`, `좌측 손 떼기 시 Player 위치 복귀` 네 항목을 핵심으로 한다.
