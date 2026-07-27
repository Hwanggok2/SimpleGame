# SimpleGame 기능 정의서

- 최종 갱신: 2026-07-28

## 1. 문서 목적

이 문서는 `SimpleGame`에서 구현할 기능의 사용 시점, 처리 방식, 결과, 예외와 완료 판정 기준을 정의한다.

기능 구현자는 이 문서를 기준으로 클래스와 테스트를 작성하고, 기획 변경 시 기능 ID를 기준으로 변경 범위를 추적한다.

관련 문서:

- [Planning_8.md](Planning_8.md)
- [Planning_7.md](Planning_7.md) — v8 이전 확정본
- [Planning_6.md](Planning_6.md) — v7 이전 확정본
- [Planning_5.md](Planning_5.md) — v6 이전 확정본
- [Planning_4.md](Planning_4.md) — v5 이전 확정본
- [GameDesignDocument.md](GameDesignDocument.md) — v4 이전 통합본
- [ArchitectureDesignDocument.md](ArchitectureDesignDocument.md)

## 2. 기능 정의서 권장 칼럼

실무 기능 정의서에는 일반적으로 다음 칼럼을 사용한다.

| 칼럼 | 설명 |
|---|---|
| 기능 ID | 요구사항, 코드, 테스트와 이슈를 연결하는 고유 식별자 |
| 기능명 | 사용자가 인식할 수 있는 기능 이름 |
| 사용 시점/Trigger | 클릭, 상태 변경, 시간 경과처럼 기능이 시작되는 조건 |
| 선행조건/입력 | 기능 실행 전에 만족해야 할 상태와 필요한 입력값 |
| 처리 방식 | 기능이 수행하는 순서와 핵심 판정 |
| 결과/출력 | 성공했을 때 변경되는 상태와 반환 결과 |
| 예외/제약 | 실행하지 않는 조건, 대체 흐름과 경계 상황 |
| UI/연출 | 사용자에게 표시할 텍스트, 애니메이션, 이펙트와 사운드 |
| 담당 시스템/데이터 | 구현 책임 클래스와 참조하는 데이터 |
| 검증 기준 | 기능 완료 여부를 판단할 재현 가능한 조건 |
| 우선순위/정의 상태 | P0/P1/P2와 확정/부분 확정/미정 상태 |

프로젝트 관리 도구로 옮길 때는 다음 칼럼도 추가할 수 있다.

- 담당자
- 작업 상태
- 목표 버전
- 관련 이슈 또는 티켓
- 변경 일자
- 기획 승인자

## 3. 표기 기준

### 3.1 우선순위

| 우선순위 | 의미 |
|---|---|
| P0 | 핵심 전투 프로토타입 또는 게임 진행에 반드시 필요 |
| P1 | MVP 완성에 필요 |
| P2 | 후속 콘텐츠, 편의성 또는 폴리싱 |

### 3.2 정의 상태

| 상태 | 의미 |
|---|---|
| 확정 | 현재 정보로 기능과 테스트를 구현할 수 있음 |
| 부분 확정 | 구조는 구현할 수 있지만 일부 수치 또는 세부 동작이 미정 |
| 미정 | 핵심 규칙 또는 데이터 결정이 필요 |
| 범위 제외 | 초기 버전에서는 구현하지 않음 |

## 4. 게임 진행 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| GF-001 | 게임 세션 시작 | 시작 버튼 클릭 또는 Scene 진입 | GameBootstrap 초기화 완료, Player·카메라·월드 청크 준비 | 저장된 계정 정보를 읽고 Player, 3×3 활성 청크와 Enemy 스폰 시스템을 초기화한 뒤 게임 타이머를 시작 | 게임 상태가 `Playing`으로 변경되고 입력과 Enemy 생성이 활성화 | 필수 데이터 또는 Entity 초기화 실패 시 플레이를 시작하지 않고 오류 표시 | 시작 연출, HUD 활성화 | `GameBootstrap`, `GameSession`, `WorldChunkGrid`, `SaveService` | 시작 후 타이머가 증가하고 Player 입력과 카메라 추적이 가능해야 함 | P0/구현 |
| GF-002 | 게임 시간 진행 | 게임 상태가 `Playing`인 동안 | 현재 상태에서 게임 시간 진행이 허용됨 | 경과 시간을 누적하고 Wave와 최종 보스 출현 조건에 전달한다. HUD 표시는 `MM:SS`로 내림 처리한다. | 최상단 생존 시간과 스폰 진행 시간이 갱신 | 수동 Pause와 카드 선택 중에는 증가하지 않음 | 최상단 `13:30` 형식 생존 시간 | `PrototypeGameSession`, `StageSpawnController`, `PrototypeHUDPresenter` | 59.9초는 `00:59`, 810초는 `13:30`으로 표시되고 정지 중 값이 유지돼야 함 | P0/구현 |
| GF-003 | ESC 일시정지/재개 | ESC 입력 | 현재 게임 상태와 직전 상태 | 현재 상태를 `Paused`로 보관하고 TimeScale, Player 입력, Enemy AI, 애니메이션, 스폰과 시간을 정지한다. 다시 ESC를 누르면 직전 상태로 복귀한다. | 전체 게임 정지와 Pause 상세 패널 표시/숨김 | 카드 선택 중 Pause하면 재개 시 다시 카드 선택 상태와 TimeScale 0을 유지 | 현재 능력치, 점수, 레벨, 경험치, 획득 스킬과 스킬 레벨 | `PrototypeGameSession`, `PrototypeHUDView`, `PrototypeHUDPresenter` | Playing과 CardSelection 상태에서 ESC를 눌렀을 때 모두 정지되고, 재개 시 각각 원래 상태로 복귀해야 함 | P0/구현 |
| GF-004 | 최종 보스 출현 | 게임 경과 시간 10분 도달 | 게임이 종료되지 않았고 최종 보스가 아직 생성되지 않음 | 일반 Wave 진행을 조정하고 Player 기준 위/아래 Spawn 경계 중 하나에서 최종 보스를 생성 | 최종 보스가 활성화되고 보스전 상태 진입 | 최종 보스 레벨과 생성 방향 선택 규칙은 미정, Boss는 거리 기반 자동 재배치 제외 | WARNING, Boss HP UI | `GameSession`, `WaveSpawner`, `EnemyFactory`, `BossDefinition` | 10분 이전에는 생성되지 않고 10분 도달 시 한 번만 생성돼야 함 | P1/부분 확정 |
| GF-005 | 게임 클리어 | 최종 보스 사망 | 최종 보스전 진행 중 | Enemy 및 입력 진행을 정리하고 최종 점수와 보상을 확정 | 게임 상태가 `Clear`로 변경 | 보스 사망과 Player 사망이 같은 시점에 발생할 경우 우선순위 미정 | 클리어 화면, 최종 점수 | `GameSession`, `ScoreSystem`, `AccountProgression` | 최종 보스가 아닌 일반 보스 사망으로는 클리어되지 않아야 함 | P1/부분 확정 |
| GF-006 | 게임 오버 | Player HP가 0이 됨 | Player가 생존 상태였음 | 게임 진행과 입력을 정지하고 사용 가능한 광고 이어하기 횟수를 확인 | 이어하기 또는 종료 선택 상태 진입 | 광고 이어하기 가능 횟수가 없으면 바로 최종 결과 화면으로 진행 | Continue/GameOver UI | `GameSession`, `PlayerRoot`, `HealthComponent`, `ContinueView` | Player HP 0에서 Enemy 이동·스폰·게임 시간이 정지돼야 함 | P0/구현 |
| GF-007 | 최종 결과 확정 | 클리어, 이어하기 포기 또는 이어하기 불가 | 최종 점수 계산 완료 | 점수를 계정 경험치로 변환하고 저장 요청 | 계정 경험치 및 레벨 갱신, 결과 화면 표시 | 저장 실패 처리 방식은 미정 | 점수, 획득 계정 EXP, 계정 레벨 | `ScoreSystem`, `AccountProgression`, `SaveService` | 19점 획득 시 계정 EXP가 3 증가해야 함 | P1/부분 확정 |

## 5. 입력 및 타깃 선택 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| IN-001 | 월드 터치 입력 | 플레이 영역 터치 | 게임 상태 `Playing`, Player가 입력 가능 | 터치 화면 좌표를 월드 좌표로 변환하고 UI 터치 여부를 확인한 뒤 타깃 선택에 전달 | 이동 또는 공격 요청 생성 | UI 위 터치, 사망, 조작 불가, 일시정지 중에는 월드 명령을 생성하지 않음 | 터치 위치 피드백 | `InputReader`, `PlayerRoot` | UI 버튼 클릭이 Player 이동으로 이어지지 않아야 함 | P0/확정 |
| IN-002 | 입력 차단 | Player가 사망, 공격 불가, UI 전용 상태 또는 Pause 진입 | 현재 Player 상태 | 이동과 공격 요청을 무시 | Player 상태 유지 | 입력을 버릴지 버퍼링할지 중 현재는 버리는 방식으로 정의 | 조작 불가 표시 필요 여부 미정 | `PlayerStateMachine`, `InputReader` | PC-004 반동 발생 후 0.5초 동안 연속 터치해도 이동·공격하지 않아야 함 | P0/확정 |
| TS-001 | 직접 터치 Enemy 보정 | 유효한 월드 터치 발생 | 터치 월드 위치, 보정 반경 1.5, 살아 있는 Enemy | 터치 위치에서 가장 가까운 Enemy를 찾고 Player→터치 진행 방향 앞에 있는지 검사한다. | 직접 공격 의도 타깃 1개 결정 | 반경 밖, 사망 상태, 입력 반대 방향 Enemy는 제외 | 선택 타깃 강조 | `PlayerController`, `PrototypeGameSession` | 같은 화면에서 Enemy를 연속 터치하면 관통 보유 여부와 관계없이 해당 Enemy가 공격 대상으로 선택돼야 함 | P0/구현 |
| TS-002 | 직접 터치 우선 선택 | 직접 터치 후보와 경로상 Enemy가 동시에 존재 | 직접 후보, 경로 후보 | 직접 터치한 Enemy를 경로상의 다른 Enemy보다 우선 선택한다. | 밀집 상황에서도 사용자가 누른 Enemy로 공격 명령 생성 | 직접 후보가 없을 때만 경로 후보 사용 | 별도 표시 없음 | `PlayerController.SelectCommandEnemy` | 직접 후보 A와 경로 후보 B가 함께 있으면 A가 선택되고, 직접 후보가 없으면 B가 선택돼야 함 | P0/구현 |
| TS-003 | 빈 공간 경로 Enemy 가로채기 | Enemy가 아닌 빈 공간 또는 Enemy 뒤쪽 빈 공간 터치 | Player 위치·Collider, 목적지, 살아 있는 Enemy Collider | Player부터 목적지까지 선분을 검사하고 Collider가 가장 먼저 겹치는 Enemy를 공격 대상으로 전환한다. | 경로 Enemy를 공격한 뒤 조건에 따라 정지 또는 이동 계속 | 여러 Enemy가 겹치면 경로 진행률이 가장 작은 대상을 사용. 직접 터치 Enemy가 있으면 TS-002가 우선 | 가로챈 Enemy 강조 | `PlayerController`, `PrototypeGameSession`, `EnemyBase` | Enemy 뒤 빈 공간을 터치하면 관통이 없어도 첫 Enemy를 공격하고, 생존하면 사거리 끝에서 정지해야 함 | P0/구현 |

### 5.1 타깃 선택 우선순위

1. 보정 반경 안에서 직접 터치한 Enemy
2. 직접 Enemy가 없을 때 Player→목적지 경로에서 가장 먼저 겹치는 Enemy
3. 두 조건 모두 없을 때 빈 공간 이동

Enemy 종류와 레벨 기반의 과거 우선순위 목록은 현재 직접 조작 의도와 충돌하므로 폐기한다.

## 6. Player 이동 및 공격 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| PM-001 | 빈 공간 이동 | Enemy 타깃 없이 월드 위치 터치 | Player 입력 가능, 카메라 화면 안의 유효한 월드 좌표 | `Vector2.MoveTowards`로 이동하되 시작은 `SmoothDamp`로 가속하고 목적지 인근은 `SmootherStep`으로 감속한다. 기본 순항 속도는 10 unit/sec이며 이동 속도 최대 레벨에서는 명령 거리 `d`에 대해 `v_max=d/0.1`을 사용한다. | 목적지 도착 허용 거리 0.08 안에서 Idle 전환, 최대 레벨은 터치 위치에 약 0.1초 도착 | 방패병이 경로를 막으면 방패병 접근 위치까지만 이동. 고정 맵 경계 Clamp는 적용하지 않음 | 부드러운 가속·감속, 이동 모션, 터치 마커, 카메라 이동 | `PlayerMovement`, `PlayerStats`, `CameraFollowController` | 일반 상태는 기본 순항 속도 10에 수렴하고 이동 속도 5레벨은 거리 1/10/20을 각각 약 0.1초에 이동해야 함 | P0/구현 |
| PM-002 | 최초 공격 위치 접근 | Enemy 공격 요청 또는 Enemy 뒤쪽 터치 | 타깃이 살아 있고 Player 입력 가능 | Enemy 중심이 회색 공격 사거리 끝에 오도록 이동하며 현재 순항 속도의 1.1배를 적용한다 | 공격 가능 위치 도착 후 공격 실행 | Enemy가 이미 사거리 안이면 Collider가 겹쳐도 이동·분리 보정을 하지 않음. 1.1배와 다른 상태 배율은 곱하지 않음 | 이동 모션, 타깃 강조 | `PlayerMovement`, `PlayerController`, `PlayerStats` | 기본 속도 10에서 접근 순항 속도가 11이고 이동 완료 전에는 피해가 없어야 함 | P0/구현 |
| PM-003 | 경로상 Enemy 연속 공격과 후속 이동 | Enemy 뒤쪽의 빈 공간 또는 다른 목표 터치 | Player 입력 가능, 이동 경로와 살아 있는 Enemy Collider가 겹침 | 첫 Enemy는 1.1배로 접근한다. Enemy가 사망하면 1.2배로 남은 경로를 계속 검사한다. Enemy가 생존해도 관통 카드가 있으면 해당 Enemy를 이번 경로에서 제외하고 목적지 이동과 다음 경로 검사를 계속한다. | 일격 처치 또는 관통이 허용된 Enemy를 공격하며 이동하고, 관통이 없는 상태에서 생존 Enemy를 만나면 정지 | 1.2배는 실제 처치 후에만 적용하고 적 수에 따라 중첩하지 않음. 관통 추가 타깃 수는 PC-005 판정창 예산을 사용 | 공격·피격·Death 모션, 처치 후 빠른 이탈 | `PlayerController`, `PlayerMovement`, `PlayerCombatAbilities`, `PrototypeGameSession` | 관통 0레벨은 생존 Enemy 뒤에서 정지하고, 관통 1레벨 이상은 공격 후 같은 명령의 목적지 이동을 계속해야 함 | P0/구현 |
| PC-001 | Player 공격 실행 | Enemy 유효 클릭마다 | Player가 공격 가능, 타깃 생존 | 클릭마다 공격 요청 1회를 생성한다. 사거리 밖 요청은 접근 후 처리하고, 접근 중 같은 Enemy에 연속 입력된 요청은 누적하여 도착 후 각각 정면/후면과 치명타를 판정한다 | 클릭 횟수와 같은 수의 Enemy 피해·피해 무효 판정 | 별도 Player 자동 공격 쿨타임 없음. 조작 불가·사망 중 입력은 무시하며 정면 반동 발생 시 남은 누적 요청을 취소 | 공격 모션, 타격 VFX/SFX | `PlayerController`, `EnemyBase`, `CombatResolver` | 동일 Enemy를 빠르게 N회 클릭하면 반동·사망 예외 전까지 공격 판정도 N회 발생해야 함 | P0/확정 |
| PC-002 | 정면/후면 판정 | Player 공격 직전 | Enemy 바라보는 방향, Player 위치 | Enemy 기준 앞 180도는 정면, 뒤 180도는 후면으로 판정 | 공격 방향 결과 반환 | 정확히 측면 경계에 위치할 때 판정 기준은 구현 시 고정 필요 | 후면 공격 성공 표시 검토 | `EnemyFacing`, `CombatResolver` | Enemy의 뒤쪽 좌표에서는 후면, 앞쪽 좌표에서는 정면이어야 함 | P0/부분 확정 |
| PC-003 | 치명타 판정 | 유효한 Player 공격마다 | 현재 치명타 확률 0~70% | 공격당 한 번 난수를 판정하고 성공 시 정면/후면 계산 피해에 3배를 적용 | 치명타 피해와 피드백 발생 | 치명타 확률은 70% 초과 불가. 일격 처치 예외 대상은 이미 최대 HP 피해이므로 추가 배율을 적용하지 않음 | 치명타 VFX, 강조 텍스트 또는 SFX | `CriticalSystem`, `CombatResolver` | 0%에서는 발생하지 않고, 일반 피해 대비 3배이며, 확률은 70% 이상으로 증가하지 않아야 함 | P0/구현 |
| PC-004 | 방패병 정면 반동 | ShieldEnemy를 정면 일반 공격해 `Recoil` 결과가 발생 | Player·ShieldEnemy 생존 | 방패 우회가 실패하면 Player를 Enemy 반대 방향으로 넉백하고 0.5초간 조작 불가 상태로 전환 | 반동 이동, 0.5초 동안 이동·공격 불가 | 조작 불가는 ShieldEnemy에만 적용한다. 일반·원거리·Boss는 Player보다 레벨이 높아도 조작 불가 없음. 후면 공격과 방패 우회 성공에는 반동 없음 | 화면 흔들림, 넉백/경직 모션, 입력 불가 피드백 | `PlayerController`, `PlayerMovement`, `PlayerRoot`, `PlayerCombatAbilities` | 방패병 정면 공격에서만 반동이 발생하고 다른 Enemy 정면 공격에서는 Player 입력이 유지돼야 함 | P0/구현 |
| PC-005 | 관통 판정창 예산 | 관통 카드 보유 상태에서 일반 공격 실행 | 관통 레벨 `L=1..5`, 현재 0.4초 판정창의 소비량 `C` | 주 대상 외 경로상의 Enemy를 가까운 순서로 수집하되 한 판정창에서 누적 추가 관통 수가 `C≤L`이 되도록 제한한다. 반복 클릭은 판정창을 초기화하지 않는다. | 0.4초마다 카드 레벨만큼 추가 관통 예산 복구 | 주 대상은 예산에서 제외. 창이 끝나기 전에는 이미 소비한 수만큼 차감. 무한 관통 혼합 스킬은 후속 버전 범위 | 관통 타격 연출 | `PlayerCombatAbilities`, `PrototypeGameSession` | L1 상태에서 0.4초 안에 여러 번 클릭해도 추가 관통은 총 1명이고, 0.4초가 지난 뒤에만 1명 예산이 복구돼야 함 | P0/구현 |

## 7. Player 생명 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| PH-001 | Player 피격 | Enemy 공격 판정과 Player가 겹침 | Player 생존, 피격 가능 | Enemy 공격력만큼 HP 감소 | HP 갱신 또는 사망 | 기본 최대 HP는 10이며 일반 피격 무적 시간은 미정 | HUD HP, 피격 Flash, 흔들림 | `HealthComponent`, `PlayerRoot`, `EnemyAttackModule` | 공격 판정 밖에서는 HP가 감소하지 않고 HUD 수치가 실제 HP와 일치해야 함 | P0/구현 |
| PH-002 | Player 사망 및 게임 오버 | Player HP가 0 이하 | Player 생존 상태 | 입력·이동·공격을 차단하고 Death 상태로 전환한 뒤 GameSession에 게임 오버를 전달 | Player 사망 연출과 GameOver UI 표시, Enemy·스폰·게임 시간 정지 | 자동 부활은 사용하지 않음 | Death 모션, GameOver UI | `PlayerRoot`, `HealthComponent`, `GameSession` | Player HP가 0이 된 프레임에 GameOver 상태가 한 번만 발생해야 함 | P0/구현 |
| PH-003 | 광고 이어하기 Player 부활 | 광고 이어하기 보상 성공 | GameOver 상태, 이어하기 사용 횟수 2회 미만 | 현재 Player 위치에서 최대 HP로 부활시키고 일반 Enemy를 Spawn 경계로 재배치 | Player 생존·입력·게임 시간 재개 | Boss 재배치 여부와 부활 직후 무적 시간은 미정 | 부활 연출 | `PlayerRoot`, `GameSession`, `EnemyWorldRecycler` | 이어하기 후 Player HP가 최대치이고 일반 Enemy가 카메라 밖 Spawn 경계에 있어야 함 | P1/부분 구현 |

## 8. 일반 Enemy 공통 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| EN-001 | Enemy 생성 및 초기화 | StageSpawner 생성 요청 | EnemyDefinition, 레벨, SpawnPoint 준비 | Prefab 인스턴스에 상태와 타깃을 초기화하고 `BaseMaxHp × HpGrowthMultiplier^(유효 레벨-1)`로 현재/최대 HP를 설정 | Enemy가 Spawn 상태로 활성화 | 필수 Definition 또는 Prefab 누락 시 생성 실패를 기록 | Spawn 연출, HP Bar | `PrototypeEnemyFactory`, `EnemyBase`, `EnemyHealth` | 생성 레벨과 난이도 보정값에 맞는 최대 HP가 설정되고 이전 상태가 남지 않아야 함 | P0/구현 |
| EN-002 | Player 추적 이동 | Enemy Spawn 또는 공격 종료 | Player 생존, Enemy 이동 가능 | Player의 현재 위치를 목표로 이동하고 진행 방향을 갱신 | 공격 범위 진입 시 공격 준비 상태 전환 | 이동속도, 회피 및 Enemy 간 충돌 정책은 데이터 미정 | 이동 애니메이션 | `EnemyMovement`, `EnemyStateMachine` | 방해가 없으면 모든 Enemy가 Player 방향으로 이동해야 함 | P0/구현 |
| EN-003 | 좌우 방향 지연 전환 | Player 추적 중 Player가 Enemy 반대편으로 이동 | Enemy 생존, 방향 잠금 상태 아님 | 반대편 상태가 0.5초 유지될 때만 좌우 방향을 변경하고, 그 전에 복귀하면 예약 전환을 취소 | 추적 이동은 계속되며 바라보는 방향만 지연 갱신 | 상하 이동은 마지막 유효 좌우 방향 유지 | 방향 전환 애니메이션 | `EnemyFacing`, `EnemyStateMachine` | 좌우 변경 후 0.49초에는 기존 방향, 0.5초 이후에는 새 방향이어야 함 | P0/구현 |
| EN-004 | 공격 방향 잠금과 해제 | 일반 근거리 Enemy 공격 예고 시작 및 실제 판정 종료 | 공격 대상이 Player, Enemy 공격 가능 | 예고 시작 방향을 실제 공격 판정까지 고정하고 판정 종료 후 Player 방향 갱신을 허용 | 공격 중 방향 흔들림 방지, 종료 후 추적 복귀 | 원거리 Enemy는 RA-001의 조준 규칙을 사용 | 공격 예고, 방향 고정 | `EnemyAttackModule`, `EnemyFacing`, `EnemyStateMachine` | 예고 중 Player가 반대편으로 가도 방향이 유지되고 판정 후에는 전환 가능해야 함 | P0/구현 |
| EN-005 | 화면 밖 일반 Enemy 재배치 | 일반 Enemy가 PlayerWorldArea의 재배치 경계를 벗어남 | Enemy 생존, Boss 아님 | 공격·이동 상태를 취소하고 Player 기준 반대 방향의 더 작은 Spawn 경계로 이동한 뒤 추적 상태를 초기화 | Enemy 종류·레벨·누적 피해를 유지한 채 전투에 복귀 | 사망 중, 화면 안, Boss는 제외. Spawn 경계는 재배치 경계보다 작아야 함 | 화면 밖 처리로 별도 연출 없음 | `PlayerWorldArea`, `EnemyWorldRecycler`, `EnemyBase` | 재배치 직후 Enemy가 다시 재배치 조건에 걸리지 않고 원래 반대편에서 Player를 추적해야 함 | P0/구현 |
| EN-006 | Enemy 공격 실행 | Player가 사거리 안이고 쿨타임 완료 | Enemy 공격 가능, Player 생존 | 공격 범위를 예고하고 공격 모션과 판정 실행 후 쿨타임 적용 | Player HP 감소 | Enemy별 예고 시간, 공격력, 범위와 쿨타임은 데이터로 관리 | 공격 범위, 모션, SFX | `EnemyAttackModule`, `EnemyDefinition` | 사거리 밖 Player에게 피해가 적용되지 않아야 함 | P0/부분 구현 |
| EN-007 | Enemy 사망 | 현재 HP가 0 이하 | Enemy 생존 | 공격·이동·Collider를 즉시 중지하고 사망 이벤트를 한 번 발행한 뒤 Death 클립 종료 후 비활성화 | 점수/경험치 지급, 공격 대상 및 경로 차단에서 즉시 제외 | Death 재생 중 추가 피격 불가. ShieldEnemy 점수 없음, 최종 Boss는 클리어 연결 | 캐릭터별 Death 모션, VFX, HP Bar 숨김 | `EnemyHealth`, `EnemyBase`, `CharacterSpriteAnimator`, `PrototypeGameSession` | 사망 보상은 한 번만 발생하고 HP Bar가 숨겨진 뒤 Death 클립 종료 후 비활성화돼야 함 | P0/구현 |
| EN-008 | 이름·레벨 위험도 색상 표시 | Enemy 생성 또는 Player 레벨 상승 | Enemy 최대 HP, Player 공격력, 방향 배율, 일격 처치 예외 | 비치명타 기준 필요 타수를 계산한다. 방향 무관 1타면 초록색, 정면 3타·후면 1타면 흰색, 그 외에는 빨간색을 적용한다 | `EnemyId Lv.N` 라벨과 현재 위험도 색상 표시 | 치명타 확률과 현재 HP는 색상 계산에 반영하지 않으며 Boss는 빨간색으로 표시 | Enemy 머리 위 TMP 라벨 | `EnemyBase`, `CombatResolver`, `PlayerStats` | Player 레벨 상승 직후 모든 생존 Enemy 라벨이 실제 HP/공격력 관계에 맞게 갱신돼야 함 | P0/구현 |
| EN-009 | Enemy HP 표시 | Enemy 생성, 피해, 회복 또는 사망 | Enemy Prefab에 `EnemyHealthBar`, Slider와 숫자 라벨 연결 | `EnemyHealth.Changed`를 구독해 Slider 비율과 현재/최대 HP 숫자를 갱신하고 사망 시 숨긴다 | 모든 Enemy의 남은 체력을 월드 공간에서 확인 | 소수점은 최대 한 자리까지 표시하며 `ShowHpBar=false`인 Definition은 숨김 | 월드 공간 HP Slider, `현재/최대` 라벨 | `EnemyHealth`, `EnemyHealthBar`, Enemy Prefab | 4종 Enemy Prefab 모두 Slider를 가지고 피해 직후 비율·숫자가 일치해야 함 | P0/구현 |
| EN-010 | Enemy 겹침 방지 | Enemy 생성·재배치·추적 이동 | 대상 Collider 반경, 다른 생존 Enemy 위치 | 생성·재배치 시 최대 32개 후보를 나선형으로 검사해 열린 위치를 선택한다. 이동 중에는 두 번의 분리 패스로 `r₁+r₂+0.08` 이상이 되도록 겹친 위치를 밀어낸다. | Enemy가 같은 좌표에 포개지지 않고 밀집 대형을 형성 | 32개 후보가 모두 막히면 요청 위치를 사용. 사망 Enemy는 분리 대상에서 제외 | 별도 디버그 표시 없음 | `PrototypeGameSession.FindOpenEnemyPosition`, `SeparateEnemy`, `CombatGeometry` | 동일 SpawnPoint에 여러 Enemy를 생성해도 Collider 중심 간 거리가 최소 분리 거리 이상이어야 함 | P0/구현 |
| EN-011 | 레거시 방향 마커 숨김 | Enemy Visual 구성 또는 활성화 | Prefab의 `FacingMarker` 참조 | 노란색 `FacingMarker` Renderer를 항상 비활성화한다. 실제 정면·후면 계산은 `EnemyFacing.Direction`을 사용한다. | Enemy 아래 노란 막대가 표시되지 않음 | 마커 제거가 전후면 판정, 애니메이션 방향 또는 Collider에 영향을 주지 않아야 함 | 노란 마커 없음 | `EnemyBase`, `CharacterAssetBuilder`, `EnemyFacing` | Visual 구성 직후 `FacingMarker.enabled=false`이고 정면·후면 테스트는 기존과 동일하게 통과해야 함 | P1/구현 |

## 9. 원거리 및 근거리 Enemy 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| RA-001 | 원거리 조준·발사·방향 고정 | Player가 공격 사거리 안이고 쿨타임 완료 | 원거리 Enemy와 Player 생존 | 발사 전까지 Player를 따라 자유롭게 조준하고 발사 순간 공격 판정 후 1초간 이동·방향을 고정한다 | 발사 후 총 2초간 재공격 불가, 1~2초에는 이동·방향 전환 가능 | 방향 고정 1초는 총 쿨타임 2초에 포함. 투사체 방식은 미정이며 현재 프로토타입은 즉시 판정 | 공격 예고, 발사 모션·VFX | `EnemyAttackModule`, `EnemyFacing`, `EnemyDefinition` | 발사 전 조준은 Player를 따라가고 발사 후 1초 동안 방향이 변하지 않으며 2초 전 재공격하지 않아야 함 | P0/구현 |
| RA-002 | 원거리 공격 피해 | 조준 완료 후 발사 시점 | Player가 판정 거리 안 | 정의된 원거리 공격 Strategy를 한 번 실행 | Player HP 감소 | 투사체/즉시 피해 방식, 속도와 충돌은 미정 | 투사체 또는 VFX | `EnemyAttackModule`, 향후 `RangedAttack` | 한 번의 발사에서 피해가 한 번만 적용돼야 함 | P0/부분 구현 |
| ME-001 | 근거리 공격 | Player가 근접 사거리 안 | 공격 쿨타임 완료 | 이동을 정지하고 예고 시작 방향을 고정한 뒤 근거리 공격 모션과 범위 판정 | Player HP 감소, 판정 종료 후 추적·방향 갱신 재개 | 공격력, 범위와 예고 수치는 데이터로 관리 | 근거리 공격 모션과 범위 표시 | `EnemyAttackModule`, `EnemyDefinition` | 예고 중 반대편으로 이동한 Player를 따라 방향이 뒤집히지 않아야 함 | P0/구현 |

## 10. ShieldEnemy 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| SH-001 | Player 추적 및 0.8초 Shield 방향 고정 | ShieldEnemy Spawn, Player가 하늘색 범위 밖으로 이동하거나 범위 안에 진입 | Player 생존, Skeleton 애니메이션 로드 완료 | 범위 밖에서는 Player를 향해 Walk로 이동한다. 범위 안에 들어오면 현재 방향으로 최소 0.8초 Shield를 유지하고, 그동안 Player가 반대편으로 이동하면 방향을 예약해 유지 시간 종료 후 적용한다 | Player와 하늘색 범위 경계를 유지하며 이동·공격 경로 차단 | 예약 중 Player가 원래 방향으로 복귀하면 전환 취소. Shield 중 새 반대 방향이 생기면 해당 시점부터 0.8초 유지 후 전환. 범위 밖이면 Walk 복귀 | Skeleton Walk·Shield·Hit, 방패 방향, 하늘색 범위 표시 | `ShieldEnemy`, `EnemyFacing`, `EnemyMovement`, `EnemyStateMachine`, `CharacterSpriteAnimator` | Shield 진입·반대편 변경 후 0.79초에는 기존 방향, 0.8초 이후에는 예약 방향이어야 함 | P0/구현 |
| SH-002 | 원거리 터치 접근 | 하늘색 범위 밖 ShieldEnemy 또는 뒤쪽 목표 터치 | Player 입력 가능, ShieldEnemy가 경로 차단 | ShieldEnemy의 하늘색 범위 끝까지 이동하고 공격하지 않음 | Player가 ShieldEnemy 앞에서 정지 | 공격이 없으므로 0.5초 조작 불가도 적용하지 않음 | 차단 피드백 | `PlayerMovement`, `ShieldEnemy` | 첫 원거리 터치에서는 ShieldEnemy 타격 수가 감소하지 않아야 함 | P0/확정 |
| SH-003 | ShieldEnemy 정면 공격 | 하늘색 범위 안에서 ShieldEnemy 정면 터치 | Player 공격 가능 | Player 공격력 1배 피해를 적용한다. 일격 처치 대상이 아니고 치명타가 아니면 Player 넉백·0.5초 조작 불가·화면 흔들림을 함께 적용 | 실제 HP 감소, 조건부 Player 반동 | Player보다 2레벨 이상 낮으면 최대 HP 피해로 즉시 처치하고 반동 없음. 치명타는 3배 피해를 주되 반동 없음 | HP Bar, 방패 충돌, 화면 흔들림, Player 넉백/경직 | `ShieldEnemy`, `PlayerController`, `EnemyHealth`, `CombatResolver` | 동일 레벨 기준 정면 일반 3타이며, 일격 처치·치명타가 아닌 정면 공격에서만 반동이 발생해야 함 | P0/구현 |
| SH-004 | ShieldEnemy 후면 공격 | ShieldEnemy 후면 공격 성공 | Player 공격 가능 | Player 공격력 3배 피해를 현재 HP에 적용 | HP 감소 또는 처치 | 동일 레벨은 1타지만 고레벨 방패병은 HP 성장식에 따라 여러 타격이 필요 | HP Bar, 후면 타격 VFX | `ShieldEnemy`, `EnemyHealth`, `CombatResolver` | 동일 레벨은 후면 1타, 1레벨 높은 방패병은 후면 2타가 필요해야 함 | P0/구현 |

## 11. Boss 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| BO-001 | Boss 생성 | 보스 Wave 또는 10분 최종 보스 Trigger | Player 기준 위/아래 Boss SpawnPoint 준비 | Spawn 방향을 선택하고 WARNING 후 Boss 활성화 | Boss가 Player 방향으로 이동 | 등장 WARNING 시간과 최종 Boss 레벨 미정, 거리 기반 자동 재배치 제외 | WARNING, Boss HP Bar | `WaveSpawner`, `EnemyFactory`, `BossEnemy` | 좌우가 아닌 위/아래 지정 Spawn 경계에서 생성돼야 함 | P1/부분 확정 |
| BO-002 | Boss Player 목표 유지 | Boss가 생성되거나 Player 공격을 받음 | Boss 생존, Player 생존 | 피해와 무관하게 이동 목표를 Player로 유지 | Player 방향 이동 지속 | 공격 행동은 Player 공격으로 취소되지 않음 | 피격 연출 | `BossEnemy`, `EnemyStateMachine` | 반복 피격 후에도 Boss 기본 목표가 Player여야 함 | P1/구현 |
| BO-003 | Boss 공격 주기 | Player가 Boss 공격 범위 안이고 새 주기 시작 가능 | Boss 생존 | 0~1.5초 Player 방향 이동·붉은 영역 표시, 1.5~2.0초 정지·공격, 2.0~3.0초 Player 추적 이동 | 3초마다 조건부 공격 반복 | 영역 모양·크기와 예고 중 Player 이탈 처리 미정 | 붉은 영역, 공격 모션 | `BossAttack`, `BossStateMachine` | 구간별 이동/정지와 공격 판정 시간이 정의와 일치해야 함 | P1/부분 구현 |
| BO-004 | Boss 피해 처리 | Player가 Boss 공격 | Player 공격력, Boss 최대 HP, 방향과 치명타 결과 | Boss 기본 HP 15와 레벨별 1.7배 성장식을 사용하고 정면 1배, 후면 3배, 치명타 추가 3배 피해를 적용 | HP 0 이하에서 Boss 사망 | Boss 행동은 피격으로 취소하지 않으며 일격 처치 예외 없음 | Boss HP Slider·현재/최대 숫자, 피격 VFX | `BossEnemy`, `EnemyHealth`, `CombatResolver` | 동일 레벨 기준 정면 일반 15회 또는 후면 일반 5회에 처치돼야 함 | P1/구현 |
| BO-005 | Boss 이어하기 처리 | 광고 이어하기 성공 | Boss 생존 | 현재는 Boss 위치를 유지하고 Player만 부활 | Boss 전투 상태 유지 | Boss 재배치·거리 보정 규칙은 미정 | 필요 시 별도 연출 | `GameSession`, `BossEnemy` | 일반 Enemy 거리 재배치에 Boss가 포함되지 않아야 함 | P1/미정 |

### 11.1 Boss 공격 시간표

| 구간 | Boss 이동 | 붉은 공격 위치 | 공격 판정 |
|---|---|---|---|
| 0.0~1.5초 | Player 방향 이동 | Boss 이동에 맞춰 이동 | 없음 |
| 1.5~2.0초 | 정지 | 공격 위치 유지 | 활성 |
| 2.0~3.0초 | Player 방향 이동 | 비활성 | 없음 |

## 12. 카메라와 무한맵 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| WM-001 | 카메라 Player 추적 | Player가 이동 | Main Camera와 Player 참조 유효 | 카메라 X/Y를 Player 위치로 부드럽게 이동하고 Z를 유지 | Player 주변 월드가 계속 화면에 표시 | CameraShake와 위치 갱신 순서가 충돌하지 않아야 함 | 부드러운 카메라 이동 | `CameraFollowController`, `CameraShakeController` | 이동 후 카메라가 Player를 추적하며 흔들림 종료 후 추적 위치로 정확히 복귀해야 함 | P0/구현 |
| WM-002 | 3×3 활성 Tilemap 청크 유지 | 게임 시작 또는 Player가 청크 경계 통과 | 9개 WorldChunk, 동일한 청크 크기 | Player가 속한 중심 좌표 주변 3×3 좌표를 계산하고 멀어진 청크만 반대쪽 빈 좌표로 이동 | 항상 9개 청크로 카메라 주변 월드 유지 | 청크를 Instantiate/Destroy하지 않으며 한 좌표에 둘 이상 배치 금지 | 연결된 지형 표시 | `WorldChunkGrid`, `WorldChunk`, Unity `Tilemap` | 어느 방향으로 이동해도 활성 청크 수가 9개이고 중심 주변 좌표가 중복 없이 유지돼야 함 | P0/구현 |
| WM-003 | 4종 맵 원본 반복 사용 | 3×3 청크 Scene 생성 | Ground Tile 원본 4종 | 9개 청크에 네 지형 원본을 순환 배치하고 재배치 시 해당 청크의 지형을 유지 | 최소 네 가지 지형 패턴 반복 | 최종 장식·장애물과 이음새 제약은 미정 | Ground Tilemap | `PrototypeSceneBuilder`, `WorldChunkGrid` | Scene에서 9개 Tilemap과 4종 Ground Tile 에셋을 확인할 수 있어야 함 | P0/구현 |
| WM-004 | Spawn·재배치 이중 경계 | 카메라 크기 또는 Player 위치 변경 | PlayerWorldArea, 직교 카메라 | 카메라 바깥에 Spawn 경계를, 그보다 크게 재배치 경계를 계산 | 재배치된 Enemy가 즉시 재재배치되지 않음 | 화면 비율에 따라 X/Y 경계를 별도로 계산 | Scene Gizmo에서만 경계 표시 | `PlayerWorldArea` | 모든 화면 비율에서 `카메라 < Spawn < 재배치` 순서를 유지해야 함 | P0/구현 |

## 13. 인게임 경험치 및 카드 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| PR-001 | 인게임 경험치 획득 | Enemy 사망 | 보상 지급 가능한 Enemy 사망 | `EnemyDefinition.KillExperience`를 Player에 추가 | 경험치 갱신, 필요량 도달 시 레벨업 | 현재 GoblinMelee/Ranged 2, Shield 0, Boss 5이며 레벨과 독립 | 경험치 획득 피드백 | `PrototypeGameSession`, `PlayerProgression`, `EnemyDefinition` | 현재 데이터에서 GoblinMelee 처치 시 EXP 2가 증가해야 함 | P0/구현 |
| PR-002 | 인게임 레벨업 | 누적 EXP가 현재 레벨 요구량 이상 | `PlayerLevelExperience`의 현재 레벨 행 | 필요 EXP를 차감하고 Player 레벨을 1 올린 뒤 현재 HP를 최대 HP까지 회복하고 카드 선택을 요청 | HP 완전 회복, 레벨업 카드 UI 표시 | 현재 프로토타입 요구량은 `R(L)=6+2L`이며 1~50레벨까지 정의. 50레벨 행의 0은 상한을 뜻하고, 초과 EXP는 유지한다. 연속 레벨업은 각 레벨업 이벤트마다 회복과 카드 선택 요청을 1회씩 처리한다. | 레벨업 연출과 HP HUD 즉시 갱신 | `PlayerProgression`, `LevelExperienceTable`, `PrototypeGameSession`, `HealthComponent` | HP가 감소한 상태에서 1레벨 EXP 8 도달 시 2레벨, `CurrentHP=MaxHP`, 카드 선택 1회가 함께 성립해야 함 | P0/구현 |
| PR-003 | 능력 카드 3개 추첨 | Player 레벨업 또는 계정 시작 보너스 | `LevelUpCardTable`, 현재 레벨과 카드별 중첩 수 | Enabled·최소 레벨·최대 중첩 조건을 통과한 후보에서 가중치 추첨하며 한 목록 안에서는 같은 카드를 제거해 중복 없이 3개를 구성 | Player가 카드 1개 선택 가능 | 최대 중첩에 도달한 카드는 후보 제외. 계정 시작 보너스는 계정 레벨을 잠금 해제 기준에 반영 | 카드 3개와 현재 중첩 수 | `LevelUpCardTable`, `PrototypeGameSession`, `PrototypeHUDView` | 현재 데이터 기준 레벨업마다 서로 다른 선택지 3개가 표시되고 추가한 카드도 최소 레벨 이후 등장해야 함 | P0/구현 |
| PR-004 | 치명타 강화 카드 | `CRIT_CHANCE_UP` 선택 | 현재 중첩 5 미만 | 치명타 확률을 5%p 증가시키고 최대 50%로 제한 | 치명타 확률 즉시 갱신 | 최대 5중첩, 최소 레벨 1, 가중치 100 | 카드 이름·설명·현재 레벨 | `CriticalSystem`, `PlayerRoot`, `LevelUpCardTable` | 5회 선택 후 치명타 확률이 25%p 증가하고 추가 후보에서 제외돼야 함 | P0/구현 |
| PR-005 | 체력 강화 카드 | `MAX_HP_UP` 선택 | 현재 중첩 5 미만 | 최대 HP와 현재 HP를 각각 5 증가 | 생존 여유 증가, HUD HP 즉시 갱신 | 최대 5중첩, 최소 레벨 1, 가중치 100 | HP 증가 표시 | `HealthComponent`, `PlayerRoot`, `LevelUpCardTable` | 피해 상태에서 선택해도 최대/현재 HP가 각각 정확히 5 증가해야 함 | P0/구현 |
| PR-006 | 이동 속도 강화 카드 | `MOVE_SPEED_UP` 선택 | Player 레벨 2 이상, 현재 중첩 5 미만 | 선택할 때마다 현재 이동 속도를 1 증가시키고, 5레벨 도달 시 PM-001의 거리 기반 최대 속도 모드를 활성화한다. | 최대 레벨에서 현재 터치 거리와 관계없이 약 0.1초 도착 | 접근 1.1배와 처치 후 이탈 1.2배는 현재 이동 상태에 적용. 가중치 80 | 카드 효과와 현재 레벨 | `PlayerStats`, `PlayerMovement`, `PlayerRoot` | 5레벨에서 거리 1/10/20의 목적지에 각각 약 0.1초 안에 도착해야 함 | P0/구현 |
| PR-007 | 공격 범위 강화 카드 | `ATTACK_RANGE_UP` 선택 | Player 레벨 3 이상, 현재 중첩 3 미만 | Player 공격 사거리를 0.15 증가시키고 공격 위치 계산을 갱신 | 더 먼 위치에서 이동 없이 공격 가능 | 최대 3중첩, 가중치 70 | 회색 공격 범위 확대 | `PlayerStats`, `PlayerController`, `PlayerRoot` | 3중첩 후 기본 사거리보다 0.45 증가해야 함 | P0/구현 |
| PR-008 | 관통 카드 | `PIERCING_UP` 선택 | Player 레벨 2 이상, 현재 중첩 5 미만 | 관통 레벨을 1 증가시키고 PC-005의 0.4초 판정창 추가 타깃 상한 `L`을 갱신 | 카드 레벨만큼 추가 Enemy 관통 | 최대 5레벨, 가중치 90. 주 대상은 관통 수에서 제외 | 관통 타격 연출 | `PlayerCombatAbilities`, `LevelUpCardTable` | L1/L5에서 한 판정창의 추가 관통 상한이 각각 1/5여야 함 | P0/구현 |
| PR-009 | 절단 카드 | `SEVER_TRAIL` 선택 | 관통 카드 1레벨 이상, Player 레벨 3 이상, 절단 미보유 | 성공한 기본 공격에서 첫 공격 위치 `p₀`를 저장하고 0.5초 뒤 Player 현재 위치 `P(t+0.5)`까지 선분을 생성한다. 선분과 겹친 모든 Enemy에게 `2×A_side` 피해를 준다. | 일반 공격 피해와 절단 피해가 함께 적용 | 공격당 최대 1개, 재사용 0.3초. 쿨타임 중 요청은 버림. 최대 1레벨, 가중치 45 | 0.32초간 하늘색 긴 참격 | `PlayerCombatAbilities`, `SlashTrailEffect`, `CombatGeometry` | 같은 공격에서 절단이 2개 생성되지 않고, 0.5초 뒤 선분의 모든 겹친 Enemy가 2배 피해를 받아야 함 | P0/구현 |
| PR-010 | 흡혈 카드 | `HIT_HEAL` 선택 | Player 레벨 4 이상, 현재 중첩 3 미만 | Enemy에게 실제 피해가 적용될 때마다 5% 확률로 `V(L)=2L`만큼 HP를 회복한다. 카드를 다시 선택할 때마다 `L`이 1 증가한다. | L1/L2/L3 성공 회복량 2/4/6, 타격당 기대 회복량 `E[V]=0.05×2L=0.1L` | 확률은 레벨과 무관하게 5% 고정. 최대 HP를 초과하지 않음. 직접·관통·스킬 명중 각각 성공 타격 단위로 판정. 최대 3레벨, 가중치 55 | 회복 수치 또는 효과 | `PlayerCombatAbilities`, `HealthComponent`, `LevelUpCardTable` | L1/L2/L3에서 성공 시 각각 HP 2/4/6을 회복하고, 실패 확률 경계와 카드 레벨이 달라도 판정 확률은 5%여야 함 | P0/구현 |
| PR-011 | 정전기 카드 | `STATIC_CHARGE` 선택 | Player 레벨 4 이상, 현재 중첩 5 미만 | 주 대상은 기본 방향 피해 `A_side`에 `0.75A_side`를 추가한다. 주변 `N(L)=2L+1`명은 각각 `0.75A_side` 피해를 받는다. | L1~L5 주변 대상 수 3/5/7/9/11 | 탐색 반경 3.2 안의 가까운 Enemy, 직접 공격·관통 대상은 주변 목록에서 제외. 최대 5레벨, 가중치 60 | 주 대상에서 주변으로 이어지는 폭 0.035의 얇은 하늘색 선 | `PlayerCombatAbilities`, `SlashTrailEffect`, `PrototypeGameSession` | L5에서 주 대상 총 피해가 `1.75A_side`, 주변 최대 11명이 각각 `0.75A_side` 피해를 받아야 함 | P0/구현 |
| PR-012 | 참격 카드 | `MOVING_SLASH` 선택 | Player 레벨 3 이상, 현재 중첩 5 미만, 이동 명령 발생 | 이동 방향으로 참격 투사체를 생성한다. 확률 `p(L)=10%+3%(L-1)`, 최대 적중 `H(L)=L`, 크기 `S(L)=1+0.1(L-1)`, 피해 `1.5A_side`를 적용한다. | L1~L5 확률 10/13/16/19/22%, 최대 적중 1~5, 크기 100~140% | 같은 Enemy는 투사체당 한 번만 피해. 후면 공격 배율 적용 시 `4.5A`. 최대 5레벨, 가중치 65 | 이동 방향으로 날아가는 하늘색 참격 | `PlayerCombatAbilities`, `MovingSlashProjectile` | L1은 첫 Enemy 명중 후 사라지고, L5는 최대 5명까지 관통하며 후면 피해가 4.5배여야 함 | P0/구현 |
| PR-013 | 방패 우회 카드 | `SHIELD_BYPASS` 선택 | Player 레벨 3 이상, 현재 중첩 3 미만 | 방패병 정면 공격의 반동이 발생할 때 확률 `p(L)=min(30%,10%×L)`을 판정해 성공 시 넉백과 0.5초 조작 불가를 모두 무시한다. | L1/L2/L3 우회 확률 10/20/30% | ShieldEnemy 정면 반동에만 적용. 일반 Enemy와 Boss에는 우회 판정 자체를 하지 않음. 가중치 55 | 우회 성공 안내 | `PlayerCombatAbilities`, `PlayerController` | 같은 난수 조건에서 L1/L2/L3의 성공 경계가 0.1/0.2/0.3이고 우회 시 입력 잠금이 없어야 함 | P0/구현 |

## 14. 점수 및 계정 성장 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| SC-001 | 일반 Enemy 처치 점수 | 일반 Enemy 사망 | 중복 보상 아님 | `EnemyDefinition.Score`만큼 점수 추가 | 현재 점수 갱신 | 현재 GoblinMelee/Ranged 5, Shield 0이며 레벨과 독립 | 점수 증가 표시 | `PrototypeGameSession`, `EnemyDefinition` | GoblinMelee 처치 시 현재 데이터 기준 5점 증가해야 함 | P0/구현 |
| SC-002 | Boss 처치 점수 | Boss 사망 | 중복 보상 아님 | `EnemyDefinition.Score`만큼 점수 추가 | 현재 점수 갱신 | 현재 GoblinBoss 25 | 보스 점수 표시 | `PrototypeGameSession`, `EnemyDefinition` | GoblinBoss 처치 시 현재 데이터 기준 25점 증가해야 함 | P1/구현 |
| SC-003 | 생존 점수 | 생존 시간 증가 또는 게임 종료 | Player 생존 시간 | 정의된 수식에 따라 시간 점수 계산 | 최종 점수에 합산 | 1분 3, 2분 5, 3분 7 예시만 존재하며 수식·누적 방식 미정 | HUD 또는 결과 화면 | `ScoreSystem`, `GameSession` | 수식 확정 전 구현 보류 | P1/미정 |
| AC-001 | 계정 경험치 변환 | 최종 결과 확정 | 최종 점수 | `floor(최종 점수÷5)` 계산, 나머지 점수 폐기 | 계정 EXP 증가 | 음수 점수 없음 | 결과 화면 획득 EXP | `AccountProgression` | 19점은 EXP 3, 20점은 EXP 4가 돼야 함 | P1/확정 |
| AC-002 | 계정 레벨업 | 계정 EXP가 요구량 도달 | 계정 레벨별 요구 EXP | 요구 EXP를 차감하고 계정 레벨 증가 | 다음 게임 시작 레벨 성장에 사용 | 5레벨 이후 요구량, 최대 레벨과 시작 레벨 변환 규칙 미정 | 계정 레벨업 표시 | `AccountProgression`, `SaveService` | 1레벨 EXP 40 도달 시 2레벨이 돼야 함 | P1/부분 확정 |

### 14.1 계정 레벨 요구 경험치

| 계정 레벨 | 필요 계정 EXP |
|---|---:|
| 1→2 | 40 |
| 2→3 | 60 |
| 3→4 | 100 |
| 4→5 | 200 |

## 15. UI 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| UI-001 | Button enum 자동 바인딩 | View 초기화 | 화면별 `ButtonId`, 자식 Button | Button GameObject 이름을 enum으로 변환하고 enum 인덱스 배열에 저장 | `Bind(ButtonId, callback)` 사용 가능 | `Count`는 제외, 중복·누락·잘못된 이름은 오류 | 없음 | `UIViewBase`, 화면별 View | Hierarchy 순서가 바뀌어도 같은 이름 Button이 올바른 ID에 연결돼야 함 | P0/확정 |
| UI-002 | TMP_Text enum 자동 바인딩 | View 초기화 | 화면별 `TextId`, 자식 TMP_Text | TMP_Text GameObject 이름을 enum으로 변환하고 enum 인덱스 배열에 저장 | `SetText(TextId, value)` 사용 가능 | `Count` 제외, 같은 ID 중복 연결 금지 | 없음 | `UIViewBase`, 화면별 View | enum에 정의된 필수 Text가 누락되면 화면과 ID가 포함된 오류가 발생해야 함 | P0/확정 |
| UI-003 | Button Listener 연결 | View 활성화 또는 Presenter 초기화 | Button 바인딩 완료, Callback | enum 인덱스로 Button을 찾고 Click Listener 등록 | 클릭 시 지정 Callback 1회 실행 | 재활성화 시 중복 등록 금지, 비활성화 시 자신이 등록한 Listener만 해제 | Button 클릭 상태 | 화면별 View/Presenter | 화면을 세 번 열고 닫아도 한 번 클릭에 Callback이 한 번만 실행돼야 함 | P0/확정 |
| UI-004 | 상단 전투 HUD | HP, 시간 또는 경험치 변경 | `PrototypeHUD.prefab` 활성화 | 최상단 중앙에 `MM:SS` 생존 시간을 표시하고, 그 아래 화면 좌우를 채우는 경험치 Slider와 남은 경험치 문구를 배치한다. HP는 경험치 바 오른쪽 아래에 표시한다. 안내 문구와 함께 상시 UI만 HUD 프리팹에 포함한다. | 전투 중 필요한 시간·경험치·HP·안내만 상시 노출 | 점수, Player 레벨, 치명타 확률과 상세 능력치는 HUD 계층에서 제거하고 UI-007에서만 표시 | 시간, 전체 폭 EXP Bar, 우측 HP, 하단 안내 | `PrototypeHUDView`, `PrototypeHUDPresenter`, `PrototypeHUD.prefab` | 씬의 HUD가 프리팹 인스턴스이고 점수·레벨·치명타와 DebugButtons가 계층에 없어야 함 | P0/구현 |
| UI-005 | 레벨업·시작 카드 UI | 레벨업 또는 계정 레벨 시작 보너스 | 추첨된 카드 3개, `CardSelectionPanel.prefab`, `LevelUpCard.prefab`, 시트의 `DisplayName`·`Description`·`Rarity` | `CardSelection` 상태로 전환해 TimeScale을 0으로 만들고 씬에 없는 카드 선택 프리팹을 `ModalRoot` 아래에 생성한다. 공통 카드 프리팹 3장에 이름·획득 후 레벨과 설명을 분리 표시한다. `Skill_Text`에는 시트 `Description`을 그대로 넣고 밝은 설명 패널 위에 진한 중성색 글자를 사용한다. 카드 폭은 300, 높이는 `300×1920/1080=533.33`이며 `LevelUpCard_In` 색은 유지한다. 바깥 `LevelUpCard`는 등급별 프레임 색을 적용하고 Outline과 unscaled time 기반의 약한 Glow를 표시한다. 창이 나타난 뒤 0.7초 동안 선택을 잠근다. | 등급 색으로 카드 가치 식별, 설명 가독성 확보, 오입력 방지 후 선택 완료 시 재개 | 닫을 때 비활성화하고 다음 레벨업에서 인스턴스를 재사용한다. 후보가 3개 미만이 되는 보충 규칙은 추후 확정 | 1080×1920 기준 세로형 카드 3장, 등급 테두리·은은한 발광 | `LevelUpCardView`, `PrototypeGameSession`, `PrototypeHUDPresenter`, `PrototypeHUDView` | 씬에 CardSelectionPanel이 없고 레벨업 시 프리팹 인스턴스와 카드 3장이 생성되며 0.69초 입력은 무시돼야 함 | P0/구현 |
| UI-006 | 광고 이어하기 UI | Player 사망 게임 오버 | 남은 이어하기 횟수 계산, `GameOverPanel.prefab` | 씬에 없는 GameOver 프리팹을 생성하고 결과 Text와 `ContinueAd` 버튼을 표시한다. 이어하기 선택 시 기존 보상형 광고 모의 흐름을 실행한다. | 광고 요청 또는 Player 부활 | `ContinueAd`는 DebugButtons가 아니라 GameOver 프리팹에만 존재한다. 실제 광고 실패·취소·네트워크 오류 흐름은 추후 확정 | 중앙 게임오버 패널과 이어하기 버튼 | `PrototypeGameSession`, `PrototypeHUDView` | 사망 전 씬에 패널이 없고 사망 시 생성되며 ContinueAd 선택 시 Player가 부활해야 함 | P1/부분 구현 |
| UI-007 | ESC 상세 정보 패널 | GF-003 Pause 진입 | Player·계정·카드 데이터 로드 완료, `PauseDetailsPanel.prefab` | 현재 Player/계정 레벨, 점수, 계정 EXP, 생존 시간, HP, 공격력, 치명타, 이동 속도, 사거리, 후면 배율, 현재/필요 EXP와 획득 카드별 레벨을 문자열로 구성한다. 씬에 없는 Pause 프리팹을 `ModalRoot` 아래에 생성하고 내용을 표시한다. | 상시 HUD에서 숨긴 모든 상세 정보 확인 | 획득 카드가 없으면 `없음` 표시. ESC 재입력 시 패널을 비활성화하고 재사용 | 전체 화면 반투명 Pause 패널 | `PrototypeGameSession.BuildPauseDetails`, `PrototypeHUDView` | 씬에 PauseDetailsPanel이 없고 ESC 입력 후 생성되며 모든 카드 이름과 `현재/최대 레벨`이 표시돼야 함 | P0/구현 |
| UI-008 | 한국어 기본 폰트 | HUD, 카드, Enemy 라벨 프리팹 생성 | `Pretendard-Regular SDF` 에셋 존재 | 모든 TMP_Text 프리팹에 기본 폰트를 지정하고 카드·HUD·상태 안내 문구를 한국어로 표시한다. | 한글 글리프 누락과 영문 임시 문구 제거 | 식별자와 코드용 CardId·EnemyId는 영문 유지 가능 | `Pretendard-Regular SDF` | `PrototypeHUDView`, `PrototypeSceneBuilder`, `CharacterAssetBuilder` | HUD와 일시 UI 프리팹의 TMP_Text가 모두 Pretendard SDF를 참조하고 사용자 노출 문구가 한국어여야 함 | P1/구현 |
| UI-009 | UI 프리팹 생명주기 | 씬 로드 및 UI 표시 이벤트 | HUD·CardSelection·PauseDetails·GameOver 프리팹 | 상시 HUD 프리팹 1개만 씬에 배치하고 일시 UI는 첫 표시 요청 시 생성한 뒤 비활성 상태로 재사용한다. 씬에서 `DebugButtons`, 미사용 상시 Text와 일시 패널을 제거한다. | 씬 계층 단순화와 UI별 독립 편집 | Popup 프리팹 참조 누락 시 View가 명시적 오류를 출력한다 | `PrototypeHUD/ModalRoot` | `PrototypeHUDView`, `PrototypeSceneBuilder` | 씬 YAML에 DebugButtons와 일시 패널 이름이 없고 Play Mode에서 세 일시 UI가 각각 필요 시 생성돼야 함 | P0/구현 |

## 16. 데이터 및 Spawn 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| DA-001 | Enemy 밸런스 로드 | GameSession 초기화 | `GameDataManifest`, `EnemyBalanceTable`, `EnemyAssetCatalog` | EnemyId로 수치 Definition과 Prefab을 각각 조회하고 Archetype 일치 여부 검증 | EnemyFactory가 동일 ID의 수치와 Prefab으로 Enemy 생성 가능 | 중복·누락 ID, Prefab 누락, Archetype 불일치는 실행 전 오류 처리 | 없음 | `GameDataManifest`, `EnemyBalanceTable`, `EnemyAssetCatalog`, `EnemyFactory` | 4종 EnemyId 조회 성공, 잘못된 ID와 잘못된 Prefab 매핑은 검증에서 거부돼야 함 | P0/구현 |
| DA-002 | 10분 스폰 일정 로드 | Stage 시작 | Excel 변환 결과인 `StageSpawnSchedule` | StageId 행을 시간, WaveId, SpawnIndex 순으로 정렬해 60개 Wave·2,578개 Spawn을 준비한다. 마지막 생성 시점은 599.6초다. | 10분 전체 게임 시간 기반 Spawn 준비 완료 | 동일 Stage·Wave·SpawnIndex 중복 불가, 필수 문자열과 음수 시간은 importer에서 거부 | 없음 | `GameDataManifest`, `StageSpawnSchedule`, `StageSpawnController` | 첫 2분 누적 250마리, 전체 2,578마리이며 마지막 행 시간이 599.6초여야 함 | P0/구현 |
| DA-003 | Player·레벨·카드 밸런스 로드 | Player 초기화, 레벨업 또는 점수 정산 | Player/Account EXP, GlobalBalance, PlayerBalance, LevelUpCardTable | Player 기본 HP·공격력·성장률·기본 이동 속도·접근/이탈 배율·도착 허용 거리·사거리와 PR-004~013 카드 규칙을 Manifest에서 전달 | Player 전투·이동·카드와 계정 EXP가 데이터값을 사용 | 없는 PlayerId, 비연속 EXP 레벨, 잘못된 카드 Stat/중첩/가중치·선행 카드 참조는 importer에서 거부 | 없음 | `GameDataManifest`, `PlayerBalanceTable`, `LevelUpCardTable`, `PlayerProgression` | 기본 속도 10, 접근 1.1배, 이탈 1.2배와 10종 카드의 값·최대 레벨·선행 조건이 로드돼야 함 | P0/구현 |
| DA-004 | Excel→Generated SO 가져오기 | `Planning/GameData_10min_Balance.xlsx` 저장 후 `SimpleGame > 데이터 > 엑셀 불러오기` 실행 | `EnemyBalance`, `StageSpawn`, `PlayerLevelExp`, `AccountLevelExp`, `GlobalBalance`, `PlayerBalance`, `LevelUpCard` 시트 | 열 이름·숫자 범위·ID 참조·중복·레벨 연속성·Unity Prefab 및 활성 Scene SpawnPoint 참조를 전부 검증한 뒤 `Assets/Game/Data/Generated` 에셋을 일괄 갱신 | 검증된 10분 데이터가 기존 Manifest 참조를 통해 다음 실행부터 즉시 반영 | `.xlsx`만 지원하고 수식 셀은 허용하지 않음. 오류가 하나라도 있으면 기존 정상 SO를 변경하지 않음 | Editor 완료/실패 Dialog와 Console 로그 | `GameDataExcelImporter`, `OpenXmlWorkbookReader`, `GameDataAssetBuilder` | 정상 파일은 7개 필수 시트와 2,578개 Spawn을 같은 SO에 반영하고 오류 파일은 기존 SO를 보존해야 함 | P0/구현 |
| SP-001 | 시간 기반 Enemy Spawn | 게임 시간이 Spawn 행의 생성 시점에 도달 | `StageSpawnSchedule`, Player 기준 32개 SpawnPoint, `EnemyFactory` | SpawnPointId를 Player 주변 경계 Transform으로 변환하고 지정 EnemyId·레벨로 생성 요청한 뒤 EN-010으로 열린 위치를 찾는다. | 지정 Enemy가 현재 Player 주변의 지정 방향에 한 번 생성 | SpawnPoint 또는 EnemyId 누락 시 오류를 기록하고 해당 행만 건너뜀 | Spawn 연출 선택 | `StageSpawnController`, `SpawnPointRegistry`, `PrototypeEnemyFactory` | Player가 원점에서 멀어진 뒤에도 현재 Player 주변에 생성되고 같은 위치에 겹쳐 생성되지 않아야 함 | P0/구현 |
| SP-002 | 시간 증가형 몬스터 밀도 | Wave 13 이후 각 Wave 시작 | Wave 번호 `w`, 10초 Wave 간격 | 1~12 Wave의 기존 250마리를 유지하고 이후 Wave 수를 `N(w)=40+ceil((w-12)/3)`로 증가시킨다. 근접·원거리·방패 비율과 보스 Wave를 섞고 Enemy 평균 레벨을 예상 Player보다 약 2 이상 높게 둔다. | 1분별 Spawn 수 69/181/249/261/273/285/297/309/321/333 | 보스는 12 Wave 간격, SpawnPoint는 같은 위치 연속 사용을 피하고 32개를 순환 | 후반으로 갈수록 높은 화면 밀도 | `GameData_10min_Balance.xlsx`, `StageSpawnSchedule` | W60은 56마리이고 전체 2,578마리, 평균 레벨 차이 최대 2.5 이하여야 함 | P0/구현 |
| PL-001 | Enemy Pool 재사용 | Enemy 생성 또는 사망 | Prefab별 Pool | 요청 시 비활성 인스턴스를 초기화해 반환하고 사망 후 초기화하여 보관 | 반복 Instantiate/Destroy 감소 | Pool 부족 시 확장 수와 최대치 미정 | 없음 | `PoolService`, `EnemyFactory` | 재사용 Enemy에 이전 HP·타깃·Listener가 남지 않아야 함 | P1/부분 확정 |

## 17. 저장 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| SV-001 | 계정 데이터 저장 | 게임 결과 확정 또는 중요 계정 변경 | 계정 레벨, EXP, 최고 점수, 클리어 정보 | 영구 저장 포맷으로 직렬화 | 다음 실행에서 복구 가능한 저장 데이터 생성 | 저장 위치, 암호화, 실패 복구와 버전 마이그레이션 미정 | 저장 실패 안내 필요 여부 미정 | `SaveService`, `AccountProgression` | 앱 재실행 후 계정 레벨과 EXP가 유지돼야 함 | P1/부분 확정 |
| SV-002 | 계정 데이터 로드 | 앱 또는 게임 초기화 | 저장 데이터 존재 여부 | 데이터 검증 후 계정 상태 복구, 없으면 기본값 생성 | 계정 성장과 시작 레벨 적용 준비 | 손상 데이터 처리 방식 미정 | 없음 | `SaveService`, `GameBootstrap` | 최초 실행은 기본값, 기존 사용자는 저장값으로 시작해야 함 | P1/부분 확정 |

## 18. 연출 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| FX-001 | 일반 타격 피드백 | Player 공격으로 Enemy에게 실제 피해 적용 | 대상 생존 또는 처치 직전 | 대상 흰색 Flash, 타격 이펙트, 모션과 일반 강도의 화면 흔들림 실행 | 명중 여부를 시각·청각적으로 전달 | 치명타는 더 강한 화면 흔들림 사용. 피해 무효 공격은 PC-004 반동 조건일 때만 화면 흔들림 적용. 여러 조건이 겹치면 가장 큰 흔들림 하나만 적용 | Flash, VFX, SFX, Camera Shake | `EnemyVisual`, `CombatFeedbackController`, `CameraShakeController` | 일반 명중보다 치명타 화면 흔들림이 강하고, 치명타·반동 동시 발생 시 치명타 흔들림만 재생되며, 피해가 없는 비반동 공격에는 화면 흔들림이 없어야 함 | P1/부분 확정 |
| FX-002 | Enemy 공격 예고 | Enemy 공격 시작 전 | 공격 범위와 예고 시간 | 실제 공격 범위를 표시하고 판정 시점에 제거 또는 변경 | Player가 회피 가능 | 일반 Enemy 수치 미정 | 범위 표시 | `EnemyAttackBase`, `EnemyVisual` | 표시된 범위와 실제 판정 범위가 일치해야 함 | P1/부분 확정 |
| FX-003 | Boss 공격 예고 | Boss 공격 주기 0~1.5초 | Player가 공격 범위 안 | 붉은 공격 위치를 표시하고 Boss 이동에 맞춰 이동 | 1.5~2.0초 공격 위치 전달 | 영역 모양·크기, Player 이탈 처리 미정 | 붉은 범위 | `BossAttack`, `BossVisual` | 예고 영역이 1.5초 동안 Boss 이동을 따라야 함 | P1/부분 확정 |
| FX-004 | 정면 공격 반동 피드백 | PC-004 정면 반동 결과 발생 | Player와 Enemy 생존, 반동 방향 계산 가능 | Player 넉백·0.5초 조작 불가와 동시에 정면 반동용 화면 흔들림 재생 | 공격이 막혔거나 방패에 반사된 감각 전달 | Player보다 2레벨 이상 낮은 ShieldEnemy에는 반동 피드백을 적용하지 않음. 치명타와 동시에 발생하면 더 큰 치명타 흔들림만 재생 | 반동 Camera Shake, 방어 VFX/SFX | `CombatFeedbackController`, `CameraShakeController`, `PlayerMovement`, `PlayerStateMachine` | 반동 조건과 화면 흔들림·넉백·입력 차단의 발생 시점이 일치하고, 동시 흔들림은 중첩되지 않아야 함 | P1/부분 확정 |
| FX-005 | 캐릭터 상태 애니메이션 | Player 또는 Enemy의 이동·공격·피격·방어·사망 상태 변경 | Character Prefab에 SpriteRenderer·Animator·Controller 연결 완료 | Player와 Goblin은 정지 시 Idle·이동 시 Move, Skeleton 방패병은 추격 시 Walk·범위 안 정지 시 Shield를 재생한다. 실제 공격 판정 시 Attack, 생존 피격 시 Hurt, 사망 시 Death Trigger를 전달하고 Facing Layer는 좌우 Clip을 전환한다 | 현재 행동·방향과 일치하는 Animator 상태 표시 | 세 리소스 모두 좌우 측면형으로 상하 전용 프레임이 없음. LightBandit 원본은 X Scale +1이 왼쪽, Goblin·Skeleton 원본은 X Scale +1이 오른쪽이므로 Player·Enemy 방향 Clip을 분리한다. Attack·Hurt 종료 후 Motion 값에 맞는 지속 상태로 복귀 | 게임용 Player·Enemy Prefab·Clip·Controller는 모두 `Assets/Game/Characters` 아래, 원본 Sprite Sheet만 `Assets/Resources` 아래 | `CharacterSpriteAnimator`, Unity `Animator`, `PlayerMovement`, `EnemyMovement`, `EnemyStateMachine`, 공격·피격 모듈 | Player Prefab이 `Prefabs/Player/Player.prefab`과 `Animators/Player.controller`를 사용하고 모든 캐릭터 상태가 정확히 전환돼야 함 | P0/구현 |
| FX-006 | 고정 컴포넌트 Inspector 구성 | Scene 또는 Character Prefab 로드 | Rigidbody2D·Collider2D·범위·경고·라벨·피드백 참조 저장 완료 | 런타임은 저장된 컴포넌트의 값과 활성 상태만 변경하고 `AddComponent`나 고정 자식 생성을 수행하지 않음 | Prefab과 Scene Inspector에서 전체 고정 구성을 확인 가능 | 일회성 투사체·타격 이펙트는 추후 Pool 대상이며 본 규칙의 고정 자식에 포함하지 않음 | Collider Gizmo, 범위·경고 Renderer | Character Prefab, `PrototypeScene`, 각 Root/Module의 SerializeField | Player·Enemy에 Rigidbody2D와 Collider2D가 각 1개이고, 필수 참조 누락 및 런타임 생성 코드가 없어야 함 | P0/구현 |
| FX-007 | 정전기 연결선 | PR-011의 주변 피해가 실제 적용 | 주 대상과 주변 대상 위치 | 주 대상에서 각 주변 대상까지 폭 0.035, 지속 0.16초의 하늘색 LineRenderer를 만들고 알파를 감소시킨다. | 정전기 전파 대상을 즉시 식별 | 피해가 적용되지 않은 대상에는 선을 생성하지 않음 | 얇은 하늘색 전기선 | `SlashTrailEffect.ShowStaticArc` | 정전기 피해 대상 수와 표시된 선 수가 일치하고 선이 0.16초 뒤 사라져야 함 | P1/구현 |
| FX-008 | 절단·이동 참격 | PR-009 또는 PR-012 발동 | 시작·끝 위치 또는 이동 방향 | 절단은 첫 공격점부터 현재 Player까지 긴 선을, 이동 참격은 이동 방향에 수직인 선을 이동시키며 표시한다. | 범위와 진행 방향을 시각적으로 전달 | 절단과 이동 참격은 서로 독립된 효과이며 같은 Enemy에 각각 피해 가능 | 하늘색 절단선과 투사체 참격 | `SlashTrailEffect`, `MovingSlashProjectile` | 절단 선분과 이동 참격 충돌 범위가 화면 표시와 일치해야 함 | P1/구현 |

## 19. 맵 및 경계 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| MP-001 | 무제한 월드 좌표 이동 | Player 이동 목적지·넉백 계산 | 터치 월드 좌표 또는 반동 방향 | 고정 MapBounds Clamp 없이 목적지와 반동 위치를 계산 | Player가 단일 화면 경계 밖으로 계속 이동 가능 | 지나치게 큰 좌표에서 Floating Origin이 필요한 시점은 추후 성능 측정 | 카메라 추적 | `PlayerController`, `PlayerMovement`, `CameraFollowController` | 기존 고정 맵 경계를 넘은 위치도 정상 이동·카메라 추적돼야 함 | P0/구현 |
| MP-002 | 일반 Enemy 반대편 재배치 위치 계산 | Enemy가 재배치 경계 밖 | Player 중심, Enemy 현재 방향, Spawn X/Y 반경 | Player→Enemy 방향의 반대 벡터와 Spawn 사각 경계 교점을 계산하고 접선 방향 오프셋 적용 | 카메라 밖 반대편 위치 반환 | Player와 Enemy 좌표가 같으면 기본 방향 사용 | 없음 | `PlayerWorldArea`, `EnemyWorldRecycler` | 오른쪽 밖 Enemy는 왼쪽 Spawn 경계, 위쪽 밖 Enemy는 아래 Spawn 경계로 이동해야 함 | P0/구현 |

## 20. 광고 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| AD-001 | 광고 이어하기 요청 | ContinueAd 버튼 클릭 | Player 사망 GameOver, 사용 횟수 2회 미만, 앱인토스 광고 지원 환경 | `AIT.LoadFullScreenAd()`의 `loaded` 이후 `AIT.ShowFullScreenAd()`를 호출하고 `userEarnedReward` 이벤트만 GameSession에 전달 | 보상 이벤트 수신 시 Player 최대 HP 부활과 일반 Enemy 재배치 실행 | 표시·노출·닫힘만으로 보상하지 않음. 실패, 취소, No Fill과 중복 콜백 정책은 구현 전 확정 | 광고 로딩/실패 UI | 앱인토스 `AdService`, `ContinuePresenter` | `userEarnedReward` 수신 전에는 Player가 부활하지 않고, 한 요청에서 보상이 한 번만 지급되어야 함 | P1/부분 확정 |
| AD-002 | 경험치 2배 광고 | 결과 화면 또는 정의된 시점 | 광고 시청 가능 | 광고 성공 시 대상 경험치를 2배 적용 | 경험치 보상 증가 | 인게임/계정 EXP 중 대상, 사용 시점과 횟수 미정 | 보상 광고 UI | `AdService`, `AccountProgression` 또는 `ExperienceSystem` | 규칙 확정 전 구현 보류 | P2/미정 |

### 20.1 앱인토스 출시 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| AIT-001 | WebGL 패키징 | 앱인토스 개발·배포 빌드 | 공식 Unity SDK, 앱 ID, 아이콘 URL | SDK 빌드 프로필로 WebGL을 생성하고 `.ait`로 패키징 | 샌드박스 또는 배포 가능한 패키지 | 앱 ID·아이콘 누락 시 빌드 중단. Production은 Development Build와 Mock Bridge 비활성 | 앱인토스 로딩 화면 | AIT Configuration, Build Profile | Dev Server, Production Server와 Build & Package가 각각 목적에 맞게 완료되어야 함 | P0/미구현 |
| AIT-002 | 화면 가시성 대응 | 토스 화면 전환 또는 앱 백그라운드 진입 | 앱인토스 Unity SDK | 가려지면 게임 시간·입력·오디오를 정지하고 다시 보이면 이전 진행 가능 상태를 복원 | 백그라운드 중 진행·오디오 없음 | GameOver·레벨업·수동 Pause 상태를 무조건 Playing으로 덮어쓰지 않음 | 필요 시 일시정지 표시 | 플랫폼 가시성 처리, `GameSession` | 숨김 상태에서 전투 시간이 흐르지 않고 복귀 후 기존 상태가 보존되어야 함 | P0/미구현 |
| AIT-003 | WebGL 성능 검증 | Production 패키지 후보 생성 | 실제 모바일 토스 앱 또는 샌드박스 | 초기 로딩 시간, FPS, 메모리, 프레임 스톨과 GC를 측정 | 출시 기준 충족 여부 | Editor 수치만으로 승인하지 않음 | 로딩 진행률 | AIT 자동 메트릭, 프로파일링 결과 | 저사양 대상 기기 기준과 허용 수치를 확정한 뒤 통과해야 함 | P0/부분 확정 |

## 21. 범위 제외 기능

| 기능 ID | 기능명 | 제외 사유 | 재검토 시점 | 상태 |
|---|---|---|---|---|
| EX-001 | Floating Origin | 현재 3×3 청크 재배치만으로 초기 플레이 검증 가능 | 장시간 플레이 좌표 정밀도 문제가 측정될 때 | 범위 제외 |
| EX-002 | 청크별 완성형 랜덤 장애물·상호작용 | 우선 Ground Tilemap 재활용과 전투 검증이 필요 | 청크 이음새와 이동 안정성 검증 후 | 범위 제외 |
| EX-003 | 공격력 증가 카드 | HP·공격력 성장 기반은 구현했지만 카드 데이터는 아직 없음 | 전투 밸런스 검증 후 | 범위 제외 |
| EX-004 | 카드 등급별 별도 연출·아이콘 현지화 | 현재 10종 카드의 기능과 한국어 텍스트 우선 | 최종 UI 아트 적용 시 | 범위 제외 |

## 22. 전투 판정 기준표

공통 계산:

- Player 공격력: `1 × 1.7^(PlayerLevel-1)`
- Enemy 최대 HP: `BaseMaxHp × 1.7^(max(1, EnemyLevel-LevelDifficultyOffset)-1)`
- 정면 피해: `PlayerAttackPower`
- 후면 피해: `PlayerAttackPower × 3`
- 치명타 피해: 위 방향 피해 `× 3`
- 방어도 및 일반 Enemy 정면 피해 면역: 없음

| Enemy와 Player의 레벨 관계 | 원거리 정면/후면 | 근거리 정면/후면 |
|---|---:|---:|
| Enemy가 낮음 | 1타/1타 | 1타/1타 |
| 동일 레벨 | 1타/1타 | 3타/1타 |
| Enemy가 1레벨 높음 | 3타/1타 | 6타/2타 |
| Enemy가 2레벨 높음 | 6타/2타 | 9타/3타 |
| Enemy가 3레벨 높음 | 9타/3타 | 15타/5타 |

별도 규칙:

- 원거리는 표시 레벨보다 1단계 낮은 HP 성장값을 사용한다.
- ShieldEnemy는 동일 레벨 기준 정면 3타/후면 1타이며 고레벨일수록 HP 성장식에 따라 타수가 증가한다.
- Boss 기본 HP는 15이고 동일 레벨 기준 정면 15타/후면 5타다.
- 원거리 동일 이하, 근거리 1레벨 이상 낮음, 방패병 2레벨 이상 낮음에는 방향 무관 일격 처치 예외를 적용한다.

스킬 계산:

- 관통: 길이 0.4초의 판정창에서 추가 관통 누적 `C≤L`, `L∈[1,5]`
- 절단: `t+0.5`에 선분 `[p₀, P(t+0.5)]` 생성, 재사용 0.3초, 겹친 Enemy 피해 `2A_side`
- 흡혈: `L∈[1,3]`, 성공 회복량 `V(L)=2L`, 성공 확률 5% 고정, 타격당 기대 회복량 `E[V]=0.05×2L=0.1L HP`
- 정전기: 주 대상 `1.75A_side`, 주변 `N(L)=2L+1`명에게 각각 `0.75A_side`
- 이동 참격: `p(L)=0.10+0.03(L-1)`, `H(L)=L`, `S(L)=1+0.1(L-1)`, 피해 `1.5A_side`
- 방패 우회: `p(L)=min(0.30,0.10L)`, ShieldEnemy 정면 반동에만 적용

## 23. 현재 미정으로 남은 핵심 항목

다음 항목은 기능 구조는 정의됐지만 최종 동작 또는 수치가 필요하다.

1. 광고 이어하기 직후 Player 무적 시간
2. 일반 Enemy별 최종 이동속도, 공격력, 범위와 예고 수치
3. 하늘색 접근 범위와 최종 터치 보정 반경
4. Boss 등장 WARNING 시간
5. Boss 공격 영역 모양·크기와 예고 중 Player 이탈 처리
6. 최종 Boss 레벨과 거리 제한·재배치 여부
7. 생존 점수 수식과 누적 방식
8. 50레벨 이후 Player 레벨 상한 확장 여부와 다중 레벨업 처리
9. 계정 5레벨 이후 요구 EXP와 최대 레벨
10. 실제 계정 레벨·경험치 저장 위치와 Toss 계정 데이터 연동 방식
11. 최대 중첩으로 유효 카드가 3개 미만이 될 때의 보충 규칙
12. 공격력 증가 카드의 증가량·중첩·등장 레벨
13. 무한 관통을 제공할 혼합 스킬의 조합 조건·등장 레벨·피해 보정
14. 치명타 카드 최대 5중첩의 실효 상한 25%와 GlobalBalance 상한 50% 사이의 후속 확장 규칙
15. 광고 실패·취소·No Fill 처리
16. 광고 경험치 2배 대상과 적용 시점
17. 저장 포맷, 손상 복구와 버전 마이그레이션
18. Player 반동 넉백 거리와 이동 시간
19. 일반 타격, 치명타와 정면 반동별 최종 화면 흔들림 수치
20. 4종 맵 원본의 최종 장식·장애물 구성과 연결 규칙
21. 일반 Enemy 재배치 시 누적 피해 유지의 최종 밸런스
22. 후반 2,578마리 밀도에서 동시 활성 Enemy 상한과 풀링 확장 정책

## 24. 기능 완료 조건

기능은 다음 조건을 모두 충족해야 완료로 처리한다.

- 기능 ID에 대응하는 구현 코드가 존재한다.
- 정상 흐름과 예외 흐름이 구현돼 있다.
- 관련 이벤트가 중복 발행되지 않는다.
- Pool 재사용 후 이전 상태가 남지 않는다.
- 기능 정의서의 검증 기준을 재현할 테스트 또는 확인 절차가 있다.
- 미정 수치를 임의로 확정값처럼 구현하지 않는다.
- UI 기능은 enum 바인딩 누락과 Listener 중복을 검증한다.
- 전투 계산 기능은 기획 타격 횟수 표와 일치하는 Edit Mode 테스트를 가진다.
