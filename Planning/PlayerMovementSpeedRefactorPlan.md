# Player 이동 속도 기반 전환 구현 계획

- 작성일: 2026-07-27
- 상태: 구현 및 자동 검증 완료
- 대상: Player 일반 이동, Enemy 경로 공격 이동, 일격 처치 관통 이동, 이동 속도 카드, Excel Importer

## 1. 목표

현재의 “목적지까지 몇 초 안에 도달”하는 시간 기반 이동을 제거하고, Unity 월드 단위/초 기준의 이동 속도로 통일한다.

- Player 초기 이동 속도: `10`
- 이동 속도 카드 1회 선택: 현재 이동 속도에 `+8`
- 이동 속도 카드 최대 중첩: `5`
- Player와 터치 위치 사이에 Enemy가 있으면 접근 중 속도: 현재 이동 속도의 `1.1배`
- 해당 Enemy를 일격 처치한 뒤 원래 터치 위치로 빠져나가는 속도: 현재 이동 속도의 `1.2배`
- 배율은 누적하지 않는다. `1.1 × 1.2`가 아니라 이동 상태에 따라 `1.0`, `1.1`, `1.2` 중 하나만 적용한다.

계산식은 다음과 같다.

```text
현재 기본 이동 속도 = BaseMoveSpeed + 이동 카드 누적 증가량
일반 이동 속도 = 현재 기본 이동 속도 × 1.0
Enemy 접근 속도 = 현재 기본 이동 속도 × 1.1
처치 후 이탈 속도 = 현재 기본 이동 속도 × 1.2
도착 시간 = 실제 이동 거리 ÷ 적용 이동 속도
```

초기 상태와 이동 카드 5회 획득 상태의 실제 값은 다음과 같다.

| 카드 중첩 | 일반 이동 | Enemy 접근 | 처치 후 이탈 |
|---:|---:|---:|---:|
| 0 | 10.0 | 11.0 | 12.0 |
| 1 | 18.0 | 19.8 | 21.6 |
| 2 | 26.0 | 28.6 | 31.2 |
| 3 | 34.0 | 37.4 | 40.8 |
| 4 | 42.0 | 46.2 | 50.4 |
| 5 | 50.0 | 55.0 | 60.0 |

## 2. 이동 상태와 전환 규칙

### 일반 이동

- 터치 경로에 Enemy가 없으면 `Normal`, 배율 `1.0`을 사용한다.
- 새 위치를 연속 터치하면 현재 위치에서 새 목적지로 즉시 방향과 목적지를 갱신한다.
- 목적지 도착, 명령 취소, 넉백, 사망 시 이동 상태를 초기화한다.

### Enemy 접근

- Player와 터치 위치를 잇는 선분에서 첫 번째 Enemy를 찾으면 `EnemyApproach`, 배율 `1.1`을 사용한다.
- 터치한 Enemy 자체도 경로의 끝점에 있는 Enemy로 판단하여 같은 규칙을 적용한다.
- Enemy가 이미 공격 사거리 안이면 이동하지 않고 즉시 공격한다.
- 일격 처치가 불가능한 Enemy는 공격 사거리 끝까지 접근하여 공격한 뒤 정지한다.

### 처치 후 이탈

- 경로상의 Enemy가 실제로 사망했을 때만 `PostKillEscape`, 배율 `1.2`로 전환한다.
- 사망하지 않은 Enemy를 공격했을 때는 이탈 배율을 적용하지 않는다.
- 여러 일격 처치 Enemy가 연속으로 있으면 첫 처치 이후 남은 경로에서 `1.2`를 유지한다.
- 배율은 적 수에 따라 증가하지 않으며 `1.2`가 상한이다.
- 다음 경로 Enemy가 일격 처치되지 않으면 공격 사거리 끝에서 공격하고 정지한다.
- Enemy가 더 없으면 원래 터치 위치까지 `1.2`로 이동한 뒤 `Normal`로 돌아간다.
- 새 터치 입력, 반동 넉백, 입력 잠금, 사망, 카드 선택 UI 진입 시 이탈 상태를 해제한다.

기존의 “연속 처치 Enemy 한 마리당 0.1초 이동” 규칙은 속도 기반 이동과 동시에 유지할 수 없으므로 제거한다. 이후 연속 처치 구간의 이동 시간도 `거리 ÷ 현재 속도`로 결정한다.

## 3. Excel 변경 계획

현재 `PlayerBalance`는 이동 시간을 나타내는 5개 칼럼을 사용한다. 이를 속도 기반 칼럼으로 교체한다.

### PlayerBalance

삭제할 칼럼:

- `MoveNearDurationBaseSec`
- `MoveFarDurationBaseSec`
- `MoveNearDurationMaxUpgradeSec`
- `MoveFarDurationMaxUpgradeSec`
- `MoveUpgradeMaxStack`

추가할 칼럼:

| 칼럼 | 자료형 | 초기값 | 설명 |
|---|---|---:|---|
| `BaseMoveSpeed` | float | 10 | Player 기본 이동 속도, Unity unit/sec |
| `PathEnemyApproachSpeedMultiplier` | float | 1.1 | 경로 Enemy 접근 속도 배율 |
| `PostKillEscapeSpeedMultiplier` | float | 1.2 | 경로 Enemy 처치 후 이탈 속도 배율 |
| `MoveArrivalTolerance` | float | 0.08 | 목적지 도착으로 판정할 허용 거리 |

이동 카드 증가량과 최대 중첩은 `LevelUpCard`가 이미 보유한 `Value`, `MaxStack`을 단일 기준으로 사용한다. 같은 값을 `PlayerBalance`에도 중복 저장하지 않는다.

변경 후 `LightBandit`의 이동 관련 데이터:

```text
BaseMoveSpeed = 10
PathEnemyApproachSpeedMultiplier = 1.1
PostKillEscapeSpeedMultiplier = 1.2
MoveArrivalTolerance = 0.08
```

### LevelUpCard

`MOVE_SPEED_UP` 행을 다음과 같이 변경한다.

| 칼럼 | 기존 | 변경 |
|---|---|---|
| `EffectType` | `UpgradeRank` | `StatModifier` |
| `TargetStat` | `MoveDuration` | `MoveSpeed` |
| `Operation` | `Add` | `Add` |
| `Value` | 1 | 8 |
| `MaxStack` | 3 | 5 |

`MinPlayerLevel`, `SelectionWeight`, `Rarity`, `IconId`, `Enabled`는 현재 값을 유지한다.

### Importer 검증

- `BaseMoveSpeed > 0`
- `PathEnemyApproachSpeedMultiplier >= 1`
- `PostKillEscapeSpeedMultiplier >= PathEnemyApproachSpeedMultiplier`
- `MoveArrivalTolerance > 0`
- `MOVE_SPEED_UP.Value > 0`
- 기존 시간 기반 칼럼이 남았거나 새 필수 칼럼이 없으면 시트·행·열을 포함한 오류를 표시한다.
- Import가 성공하면 기존 `PlayerBalanceTable.asset`, `LevelUpCardTable.asset`, `GameDataManifest.asset` 참조를 유지한 채 값을 갱신한다.

## 4. 코드 변경 계획

### 데이터 모델

- `PlayerDefinition`의 이동 시간 필드와 보간 메서드를 제거한다.
- `BaseMoveSpeed`, 접근/이탈 배율, 도착 허용 거리를 추가한다.
- `PlayerStatId.MoveDuration`을 `PlayerStatId.MoveSpeed`로 교체한다.
- 이동 카드의 최대 중첩은 카드 선택 시스템이 관리하고 `PlayerStats`는 실제 누적 속도 보너스만 보관한다.

### PlayerStats

- `moveUpgradeRank` 대신 `moveSpeedBonus`를 저장한다.
- 현재 속도는 `BaseMoveSpeed + moveSpeedBonus`로 계산한다.
- `AddMoveSpeed(float amount)`를 제공한다.
- 공격력·공격 사거리 등 기존 책임은 유지한다.

### PlayerMovement

- 시간 보간용 `moveStart`, `moveStartedAt`, `activeMoveDuration`을 제거한다.
- 프레임마다 다음 식으로 이동한다.

```csharp
transform.position = Vector2.MoveTowards(
    transform.position,
    targetPosition,
    effectiveSpeed * Time.deltaTime);
```

- 이동 시작은 `SmoothDamp`로 짧게 가속하고 목적지 인근은 현재 순항 속도에 비례한 제동 거리에서 `SmootherStep`으로 감속한다.
- 일격 처치 후 다음 Enemy로 이어지는 구간은 운동량을 유지하며, 생존 Enemy·최종 목적지 도착·명령 취소·넉백에서는 운동량을 초기화한다.
- 도착 허용 거리 안에서는 목적지에 스냅하고 Idle로 전환한다.
- 넉백은 공격 이동과 별도 시간 기반 연출로 유지한다.

### PlayerController

- `SweepEnemyMoveDuration = 0.1f`와 관련 분기를 제거한다.
- 현재 이동 상태를 `Normal`, `EnemyApproach`, `PostKillEscape`로 명시한다.
- 경로 탐색과 공격 결과에 따라 상태만 전환하고, 실제 좌표 이동은 `PlayerMovement`에 위임한다.
- 일격 처치가 연속되어도 `PostKillEscape` 배율을 유지하며 중첩하지 않는다.
- 새 입력과 취소 조건에서 반드시 `Normal`로 복귀시킨다.

### PlayerRoot와 카드 적용

- `MOVE_SPEED_UP` 선택 시 `PlayerStats.AddMoveSpeed(card.Value)`를 호출한다.
- 변경된 현재 속도를 `PlayerMovement`에 즉시 반영한다.
- 카드 선택 전후 Player 위치나 현재 이동 목적지는 변경하지 않는다.

### Editor와 생성 SO

- `GameDataExcelImporter`의 `PlayerBalance` 필수 칼럼과 검증을 변경한다.
- `GameDataAssetBuilder`의 기본 Player/Card 생성값도 같은 구조로 변경한다.
- Excel Import를 실행해 Generated SO를 다시 생성한다.

## 5. 이동감을 경쾌하게 만드는 권장 기법

게임플레이 위치는 일정 속도를 유지하고, 체감 연출은 Player의 Visual 자식에만 적용하는 방식이 안전하다.

1. **짧은 출발 Lean**
   - 이동 시작 시 Visual을 진행 방향으로 2~4도 기울이고 0.08초 안에 복귀시킨다.
   - 실제 Collider와 Root 좌표에는 영향을 주지 않는다.

2. **속도 연동 애니메이션**
   - 걷기 애니메이션 재생 속도를 `현재 속도 ÷ 기본 속도`에 따라 조절한다.
   - 지나치게 빨라지지 않도록 약 `0.9~1.35` 범위로 제한한다.

3. **처치 후 이탈 전용 잔상**
   - `PostKillEscape` 상태에서만 짧은 잔상 또는 작은 먼지 효과를 사용한다.
   - 일반 이동, Enemy 접근, 처치 후 이탈의 차이를 숫자 UI 없이도 느낄 수 있다.

4. **목적지 스냅과 즉시 재지정**
   - 도착 직전 감속 대신 작은 허용 거리에서 스냅한다.
   - 연속 터치 시 코루틴을 쌓지 않고 기존 목적지를 즉시 교체한다.

5. **카메라 추적 지연 제한**
   - 카메라는 Player보다 약간 부드럽게 따라오되 지연을 과하게 주지 않는다.
   - 권장 시작값은 약 `0.08~0.12초`이며, 공격/이탈 중 추가 지연을 만들지 않는다.

화면 전체 `Time.timeScale`을 멈추는 Hit Stop은 연속 클릭과 Enemy 이동까지 끊을 수 있으므로 초기 구현에서는 제외한다. 필요하면 이후 실제 타격 애니메이션과 Visual 자식에만 적용하는 국소 Hit Pause를 별도 실험한다.

## 6. 체감 속도 주의 사항

현재 Prototype Scene 카메라의 Orthographic Size는 `10`이므로 세로 화면 높이는 약 20 Unity unit이다. 기본 순항 속도 `10`이라면:

- 5 unit 이동: 순항 기준 약 0.5초
- 10 unit 이동: 순항 기준 약 1초
- 세로 화면 전체 20 unit 이동: 순항 기준 약 2초

5회 강화하면 일반 순항 속도는 50이므로 가감속을 제외한 10 unit 환산 시간은 0.2초다. 같은 상태에서 Enemy 접근은 55, 처치 후 이탈은 60이다. 실제 도착 시간은 시작 가속과 목적지 감속 때문에 환산 시간보다 조금 길다.

## 7. 테스트 계획

### EditMode

- 카드 중첩 0~5에서 현재 기본 속도가 각각 `10`, `18`, `26`, `34`, `42`, `50`인지 검증
- 초기 상태에서 일반/접근/이탈 순항 속도가 `10`, `11`, `12`인지 검증
- 5중첩 상태에서 `50`, `55`, `60`인지 검증
- 배율이 서로 곱해지거나 Enemy 수에 따라 누적되지 않는지 검증
- Excel의 새 칼럼과 `MoveSpeed` 카드가 정상 파싱되는지 검증
- 0 이하 속도와 잘못된 배율을 Importer가 거부하는지 검증

### PlayMode 또는 수동 검증

- 빈 공간 터치 시 거리에 비례한 일정 속도로 이동
- Enemy 뒤 터치 시 접근 구간 `1.1배`
- 일격 처치 직후 원래 목적지까지 `1.2배`
- 여러 일격 처치 Enemy를 통과해도 `1.2배` 상한 유지
- 생존 Enemy를 공격하면 공격 사거리 끝에서 정지
- Enemy가 이미 공격 사거리 안이면 이동 없이 공격
- 이동 중 연속 터치, 카드 선택, 넉백, 사망 후 상태 초기화
- 애니메이션 방향과 공격 시점이 실제 이동/공격 판정과 일치

## 8. 구현 순서

1. 기획서·기능 정의서·구조 문서에서 시간 기반 이동 규칙을 속도 기반 규칙으로 갱신
2. `GameData.xlsx`의 `PlayerBalance`, `LevelUpCard` 칼럼과 값을 변경
3. `PlayerDefinition`, `PlayerStatId`, `PlayerStats` 데이터 구조 변경
4. `PlayerMovement`를 `MoveTowards + unit/sec` 방식으로 교체
5. `PlayerController`에 세 가지 이동 상태와 전환 규칙 적용
6. `PlayerRoot` 카드 적용과 Editor Importer/기본 에셋 생성 코드 변경
7. Excel Import 실행 및 Generated SO 갱신
8. EditMode 테스트 추가·수정 후 전체 테스트와 C# 빌드 실행
9. Prototype Scene에서 연속 터치·단일/다중 Enemy 관통·생존 Enemy 정지 확인
10. 실제 화면 체감에 따라 `BaseMoveSpeed`만 Excel에서 1차 튜닝

## 9. 완료 기준

- 이동 시간이 더 이상 목적지 도달 시간이나 고정 0.1초로 결정되지 않는다.
- 일반 이동, Enemy 접근, 처치 후 이탈 모두 같은 속도 기반 이동기를 사용한다.
- 카드 선택 후 현재 속도가 즉시 `+8`되고 최대 5중첩이 지켜진다.
- 시작 가속과 목적지 감속이 적용되고, 연속 일격 처치 구간에서는 운동량이 유지된다.
- 상태 배율이 정확히 `1.0 / 1.1 / 1.2`로 전환되고 누적되지 않는다.
- Excel 수정 후 `SimpleGame > Data > Import Excel`만으로 SO가 갱신된다.
- 자동 테스트, C# 빌드, Unity Console 검증을 모두 통과한다.
