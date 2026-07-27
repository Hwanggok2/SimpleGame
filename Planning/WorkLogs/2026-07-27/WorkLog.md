# 2026-07-27 작업 기록

## 1. 작업 범위

- Player·Enemy 공격력과 실제 HP 기반 전투 모델 정리
- Excel 기반 PlayerBalance·LevelUpCard 데이터 반영
- 레벨업 및 계정 시작 카드 3개 랜덤 추첨
- 모든 Enemy Prefab의 현재/최대 HP 표시
- Player 게임용 Prefab·AnimationClip·AnimatorController 경로 정리
- Enemy 공격 접근과 연속 처치 이동 시간 수정
- 기획서·기능 정의서·아키텍처·프로토타입 문서 동기화

## 2. 전투와 HP

- 방어도와 일반 Enemy 정면 피해 면역을 제거했다.
- Player 공격력과 Enemy 최대 HP는 레벨마다 동일한 `1.7` 배율로 성장한다.
- 정면 공격은 Player 공격력 1배, 후면은 3배, 치명타는 계산된 방향 피해의 3배다.
- 원거리 Enemy는 표시 레벨보다 1단계 낮은 HP 성장값을 사용한다.
- 낮은 레벨 Enemy의 방향 무관 일격 처치는 Enemy별 데이터로 유지한다.
- 모든 Enemy Prefab에 World Space HP Slider와 현재/최대 HP 숫자를 추가했다.

현재 주요 기본값:

| 대상 | 기본값 |
|---|---:|
| Player 최대 HP | 10 |
| Player 공격력 | 1 |
| 일반·방패 Enemy 기본 최대 HP | 3 |
| Boss 기본 최대 HP | 15 |
| 레벨 성장 배율 | 1.7 |
| 후면 공격 배율 | 3 |
| 치명타 피해 배율 | 3 |

## 3. 카드

- 레벨업 또는 계정 시작 보너스마다 조건을 만족하는 후보 중 카드 3개를 가중치로 추첨한다.
- 같은 선택 목록 안에서는 카드가 중복되지 않는다.
- 최대 중첩 전까지는 다음 선택에서 같은 카드가 다시 등장할 수 있다.
- 카드 선택 중에는 Player, Enemy, 애니메이션, 스폰과 게임 시간을 정지한다.
- 계정 레벨로 여러 장을 시작 선택할 때는 선택할 때마다 카드 목록을 새로 추첨한다.

현재 카드:

| 카드 ID | 효과 | 최대 중첩 | 최소 레벨 | 가중치 |
|---|---:|---:|---:|---:|
| `CRIT_CHANCE_UP` | 치명타 확률 +10% | 7 | 1 | 100 |
| `MAX_HP_UP` | 최대/현재 HP +10 | 5 | 1 | 100 |
| `MOVE_SPEED_UP` | 이동 강화 +1단계 | 3 | 2 | 80 |
| `ATTACK_RANGE_UP` | 공격 사거리 +0.2 | 3 | 3 | 70 |

## 4. Player 에셋

게임에서 사용하는 Player 에셋을 Enemy와 같은 `Assets/Game/Characters` 영역으로 이동했다.

- Prefab: `Assets/Game/Characters/Prefabs/Player/Player.prefab`
- AnimatorController: `Assets/Game/Characters/Animators/Player.controller`
- AnimationClip: `Assets/Game/Characters/Animations/Player`
- 원본 Sprite Sheet는 Enemy 원본과 동일하게 `Assets/Resources`에 유지한다.
- 기존 Scene Prefab 참조는 GUID를 유지해 새 경로를 계속 사용한다.

## 5. Enemy 접근 이동 문제

### 원인

이전 접근 로직은 Player가 실제로 멈출 Enemy 공격 사거리 끝 위치보다, Enemy 뒤에 터치한 최종 목적지까지의 긴 거리를 기준으로 이동 시간을 계산했다. 짧은 접근 거리를 긴 시간 동안 보간하므로 Enemy에게 가까워질 때만 느려 보였다.

### 수정

- 최초 Enemy 접근은 공격 사거리 끝의 실제 Player 도착 위치를 먼저 계산한다.
- 해당 실제 이동 거리에 일반 빈 공간 이동과 같은 시간 곡선을 적용한다.
- 기본 일반 이동은 가까운 기준 0.2초, 먼 기준 0.4초다.
- 이동 카드 3단계에서 가까운/먼 기준 모두 0.1초가 된다.
- Enemy가 이미 공격 사거리 안이면 이동하지 않고 즉시 공격한다.
- 일격 처치 후 다음 Enemy로 이어지는 연속 관통 구간만 적당 0.1초를 유지한다.
- 연속 처치 후 Enemy가 더 없으면 원래 터치 위치까지 일반 이동 시간으로 이동한다.

## 6. 데이터 파이프라인

`Planning/GameData.xlsx`의 7개 필수 시트를 Unity Editor importer가 검증한 뒤 Generated ScriptableObject로 갱신한다.

- EnemyBalance
- StageSpawn
- PlayerLevelExp
- AccountLevelExp
- GlobalBalance
- PlayerBalance
- LevelUpCard

현재 런타임은 Excel 또는 `.bytes`를 직접 읽지 않고 `GameDataManifest`가 참조하는 생성 SO를 사용한다.

## 7. 갱신 문서

- `Planning/GameDesignDocument.md`
- `Planning/FunctionalSpecification.md`
- `Planning/ArchitectureDesignDocument.md`
- `Planning/PrototypeImplementation.md`

과거의 0.1초 고정 이동, Resources Player Prefab 경로, 정면 피해 면역, 레벨만으로 계산한 처치 타수, 치명타 외 카드 미정 등의 내용을 현재 구현과 기획에 맞게 수정했다.

## 8. 검증

- 일반 5유닛 이동 시간: 0.3초
- 실제 공격 위치까지 5유닛인 Enemy 접근 시간: 0.3초
- 연속 처치 다음 Enemy 이동 시간: 0.1초
- 기존 Enemy 접근 0.1초 고정 상수 제거 확인
- Player Scene 인스턴스가 Game 폴더의 Player Prefab을 참조
- Unity EditMode 테스트: 40/40 통과
- Unity Console 오류: 0
- C# 빌드 오류: 0

## 9. 후속 결정

- 유효 카드가 최대 중첩으로 3개 미만이 될 때의 보충 규칙
- 공격력 증가 카드의 수치·중첩·최소 등장 레벨
- 최종 PlayerLevelExp 곡선
- 일반 Enemy·Boss 최종 전투 수치와 전체 Stage Spawn 일정
- 앱인토스 광고·저장·실기기 WebGL 검증

## 10. 속도 기반 이동 전환 계획

- 목적지 도달 시간 기반 이동을 Unity unit/sec 기준 이동 속도로 교체하는 계획을 작성했다.
- 초기 속도 20, 이동 카드당 +16·최대 5회, 경로 Enemy 접근 1.1배, 일격 처치 후 이탈 1.2배 규칙으로 최종 조정했다.
- 배율은 누적하지 않고 `Normal`, `EnemyApproach`, `PostKillEscape` 상태 중 하나만 적용한다.
- 기존 연속 처치 Enemy당 0.1초 고정 이동은 속도 기반 설계와 충돌하므로 제거 대상으로 분류했다.
- `PlayerBalance`, `LevelUpCard`, Excel Importer, Generated SO, 런타임 클래스와 테스트 변경 범위를 `Planning/PlayerMovementSpeedRefactorPlan.md`에 기록했다.

## 11. 속도 기반 이동 구현

- `PlayerMovement`를 목적지 시간 보간에서 `Vector2.MoveTowards` 기반 unit/sec 이동으로 교체했다.
- `PlayerBalance`에 기본 속도 20, 경로 Enemy 접근 1.1배, 처치 후 이탈 1.2배, 도착 허용 거리 0.08을 반영했다.
- `MOVE_SPEED_UP`을 `MoveSpeed +16`, 최대 5중첩으로 변경했다.
- 5중첩 시 일반 이동 속도는 100이며 10 unit 이동 시간이 0.1초가 된다.
- 기존 연속 처치 Enemy당 0.1초 고정값을 제거하고, 첫 처치 이후 남은 경로에서 1.2배 속도를 유지하도록 수정했다.
- Excel Importer 검증과 기본 Generated 데이터 생성 코드도 새 칼럼 구조에 맞췄다.
- `Planning/GameData.xlsx`를 가져와 Generated Player/Card SO를 갱신했다.
- Unity 런타임 데이터에서 기본 20, 접근 1.1배, 이탈 1.2배, 카드 +16/5중첩, 최대 속도 100, 10 unit 이동 0.1초를 확인했다.
- Unity EditMode 테스트 41/41, C# 빌드 오류 0, Unity Console 오류 0을 확인했다.
- Prototype Scene Play Mode 진입·종료 스모크 테스트에서도 Console 오류 0을 확인했다.

## 12. 이동 속도 재조정과 자연스러운 가감속

- Player 기본 이동 속도를 20에서 10으로 절반 조정했다.
- `MOVE_SPEED_UP` 증가량을 +16에서 +8로 절반 조정하고 최대 5중첩은 유지했다.
- 이동 순항 속도는 `10 → 18 → 26 → 34 → 42 → 50`이다.
- 이동 시작은 `SmoothDamp`로 가속하고 목적지 인근은 속도 비례 제동 거리와 `SmootherStep` 곡선으로 감속하도록 변경했다.
- 경로상의 일격 처치 Enemy를 연속 통과할 때는 운동량을 유지하고, 생존 Enemy·최종 목적지 도착·명령 취소·넉백에서는 운동량을 초기화한다.
- `Planning/GameData.xlsx`, Generated Player/Card SO, 기본 에셋 생성값, 테스트와 관련 기획·구조·기능 문서를 같은 값으로 동기화했다.
- C# 빌드 오류 0, Unity EditMode 테스트 41/41 통과, Play Mode 런타임 값 `10 / 1.1 / 1.2 / 0.06 / 0.05`와 Console 오류 0을 확인했다.
