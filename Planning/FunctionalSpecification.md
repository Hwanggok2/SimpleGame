# SimpleGame 기능 정의서

## 1. 문서 목적

이 문서는 `SimpleGame`에서 구현할 기능의 사용 시점, 처리 방식, 결과, 예외와 완료 판정 기준을 정의한다.

기능 구현자는 이 문서를 기준으로 클래스와 테스트를 작성하고, 기획 변경 시 기능 ID를 기준으로 변경 범위를 추적한다.

관련 문서:

- [GameDesignDocument.md](GameDesignDocument.md)
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
| GF-001 | 게임 세션 시작 | 시작 버튼 클릭 또는 Scene 진입 | GameBootstrap 초기화 완료, Player/Castle/맵 준비 | 저장된 계정 정보를 읽고 Player와 Castle을 초기화한 뒤 게임 타이머와 Wave 진행을 시작 | 게임 상태가 `Playing`으로 변경되고 입력과 Enemy 생성이 활성화 | 필수 데이터 또는 Entity 초기화 실패 시 플레이를 시작하지 않고 오류 표시 | 시작 연출, HUD 활성화 | `GameBootstrap`, `GameSession`, `SaveService` | 시작 후 타이머가 증가하고 Player 입력이 가능해야 함 | P0/부분 확정 |
| GF-002 | 게임 시간 진행 | 게임 상태가 `Playing`인 동안 | 현재 상태에서 게임 시간 진행이 허용됨 | 경과 시간을 누적하고 Wave와 최종 보스 출현 조건에 전달 | HUD 시간과 스폰 진행 시간이 갱신 | 일시정지 중에는 증가하지 않으며 레벨업 UI 중 정지 여부는 추후 결정 | HUD 플레이 시간 | `GameSession`, `WaveSpawner`, `HUDPresenter` | 60초 경과 시 HUD에 1분이 표시되고 Pause 중 값이 유지돼야 함 | P0/부분 확정 |
| GF-003 | 일시정지/재개 | Pause 버튼 클릭 | 게임 상태가 `Playing` 또는 `Paused` | Playing이면 시간, 입력, AI를 정지하고 Paused면 이전 상태로 복귀 | 게임 상태와 Pause UI 변경 | 게임 오버, 광고 이어하기, 클리어 중에는 실행하지 않음 | Pause 팝업 표시/숨김 | `GameSession`, `UIFlowCoordinator` | 정지 중 Player와 Enemy가 움직이지 않고 재개 후 정상 동작해야 함 | P1/부분 확정 |
| GF-004 | 최종 보스 출현 | 게임 경과 시간 10분 도달 | 게임이 종료되지 않았고 최종 보스가 아직 생성되지 않음 | 일반 Wave 진행을 조정하고 위/아래 보스 SpawnPoint 중 하나에서 최종 보스를 생성 | 최종 보스가 활성화되고 보스전 상태 진입 | 최종 보스 레벨, 생성 방향 선택 규칙과 기존 Enemy 처리 규칙은 미정 | WARNING, Boss HP UI | `GameSession`, `WaveSpawner`, `EnemyFactory`, `BossDefinition` | 10분 이전에는 생성되지 않고 10분 도달 시 한 번만 생성돼야 함 | P1/부분 확정 |
| GF-005 | 게임 클리어 | 최종 보스 사망 | 최종 보스전 진행 중 | Enemy 및 입력 진행을 정리하고 최종 점수와 보상을 확정 | 게임 상태가 `Clear`로 변경 | 보스 사망과 Castle 파괴가 같은 시점에 발생할 경우 우선순위 미정 | 클리어 화면, 최종 점수 | `GameSession`, `ScoreSystem`, `AccountProgression` | 최종 보스가 아닌 일반 보스 사망으로는 클리어되지 않아야 함 | P1/부분 확정 |
| GF-006 | 게임 오버 | Castle HP가 0이 됨 | Castle이 무적 상태가 아님 | 게임 진행과 입력을 정지하고 사용 가능한 광고 이어하기 횟수를 확인 | 이어하기 또는 종료 선택 상태 진입 | 광고 이어하기 가능 횟수가 없으면 바로 최종 결과 화면으로 진행 | Continue/GameOver UI | `GameSession`, `CastleHealth`, `ContinueView` | Castle HP 0에서 Enemy 이동과 게임 시간이 정지돼야 함 | P0/확정 |
| GF-007 | 최종 결과 확정 | 클리어, 이어하기 포기 또는 이어하기 불가 | 최종 점수 계산 완료 | 점수를 계정 경험치로 변환하고 저장 요청 | 계정 경험치 및 레벨 갱신, 결과 화면 표시 | 저장 실패 처리 방식은 미정 | 점수, 획득 계정 EXP, 계정 레벨 | `ScoreSystem`, `AccountProgression`, `SaveService` | 19점 획득 시 계정 EXP가 3 증가해야 함 | P1/부분 확정 |

## 5. 입력 및 타깃 선택 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| IN-001 | 월드 터치 입력 | 플레이 영역 터치 | 게임 상태 `Playing`, Player가 입력 가능 | 터치 화면 좌표를 월드 좌표로 변환하고 UI 터치 여부를 확인한 뒤 타깃 선택에 전달 | 이동 또는 공격 요청 생성 | UI 위 터치, 사망, 조작 불가, 일시정지 중에는 월드 명령을 생성하지 않음 | 터치 위치 피드백 | `InputReader`, `PlayerRoot` | UI 버튼 클릭이 Player 이동으로 이어지지 않아야 함 | P0/확정 |
| IN-002 | 입력 차단 | Player가 사망, 공격 불가, UI 전용 상태 또는 Pause 진입 | 현재 Player 상태 | 이동과 공격 요청을 무시 | Player 상태 유지 | 입력을 버릴지 버퍼링할지 중 현재는 버리는 방식으로 정의 | 조작 불가 표시 필요 여부 미정 | `PlayerStateMachine`, `InputReader` | 방패병 정면 공격 후 0.5초 동안 연속 터치해도 이동·공격하지 않아야 함 | P0/확정 |
| TS-001 | 터치 보정 후보 수집 | 유효한 월드 터치 발생 | 터치 월드 위치, 터치 보정 반경 | 터치 위치 주변의 살아 있는 Enemy를 수집 | 타깃 후보 목록 생성 | 보정 반경과 Collider 판정 방식은 데이터 미정 | 후보 또는 선택 타깃 강조 | `PlayerTargetSelector`, `PlayerData` | 보정 반경 밖 Enemy는 후보에 포함되지 않아야 함 | P0/부분 확정 |
| TS-002 | 타깃 우선순위 선택 | 후보 Enemy가 1개 이상 | Enemy 종류, Player와 Enemy 레벨 | 정의된 우선순위로 후보를 정렬하고 최상위 Enemy 선택 | 의도 타깃 1개 결정 | 같은 우선순위 Enemy가 여러 명일 때 거리/터치 근접도 기준은 미정 | 선택 타깃 표시 | `PlayerTargetSelector`, `CombatResolver` | 낮은 원거리와 방패병이 동시에 후보면 낮은 원거리가 선택돼야 함 | P0/부분 확정 |
| TS-003 | 경로상 방패병 차단 | 이동 또는 공격 목표 결정 후 | Player 위치, 목적지, 살아 있는 방패병 Collider | Player부터 목적지까지 경로를 검사하고 가장 먼저 막는 방패병을 찾음 | 목적지가 방패병 접근 위치로 변경 | 방패병이 여러 명일 때 Player와 가장 가까운 차단 대상을 사용 | 차단된 경로 또는 방패병 강조 | `PlayerTargetSelector`, `ShieldEnemy` | 일반 Enemy를 선택해도 경로에 방패병이 있으면 방패병 앞에서 멈춰야 함 | P0/확정 |

### 5.1 타깃 선택 우선순위

1. Player보다 레벨이 낮은 원거리 Enemy
2. Player와 레벨이 동일한 원거리 Enemy
3. Player보다 레벨이 낮은 근거리 Enemy
4. Player보다 레벨이 높은 원거리 Enemy
5. Player와 레벨이 동일한 근거리 Enemy
6. Player보다 레벨이 높은 근거리 Enemy
7. Boss
8. ShieldEnemy

경로를 가로막는 ShieldEnemy 판정은 위 선택 우선순위보다 먼저 적용되는 최종 차단 규칙이다.

## 6. Player 이동 및 공격 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| PM-001 | 빈 공간 이동 | Enemy 타깃 없이 월드 위치 터치 | Player 입력 가능, 유효한 플레이 영역 | 터치 위치를 목적지로 설정하고 바라보는 방향으로 이동 모션 재생 | 목적지에 도착 후 Idle 상태 전환 | 방패병이 경로를 막으면 방패병 접근 위치까지만 이동 | 이동 모션, 터치 마커 | `PlayerMovement`, `PlayerStateMachine` | 빈 공간 터치 후 Player가 허용 경계 안의 목적지에 도착해야 함 | P0/부분 확정 |
| PM-002 | 공격 위치 접근 | Enemy 공격 요청 | 타깃이 살아 있고 Player 입력 가능 | Enemy가 Player의 회색 공격 사거리 끝에 걸치는 지점을 계산하여 이동 | 공격 가능 위치 도착 후 공격 실행 | Enemy가 이미 사거리 안이면 이동 생략, 이동 중 타깃 사망 처리 미정 | 이동 모션, 타깃 강조 | `PlayerMovement`, `PlayerCombat` | 사거리 밖 공격 시 피해가 이동 완료 전 적용되지 않아야 함 | P0/부분 확정 |
| PM-003 | 일격 처치 후 터치 지점 이동 | 한 번에 처치 가능한 Enemy의 뒤쪽을 터치 | 경로 차단 없음, 타깃 1타 처치 가능 | 공격 위치를 지나 Enemy를 처치하고 원래 터치 지점까지 이동 | Enemy 사망, Player가 터치 위치 도착 | 터치 위치가 플레이 영역 밖이면 경계 안으로 보정 | 처치 이펙트와 연속 이동 | `PlayerMovement`, `PlayerCombat`, `CombatResolver` | 낮은 레벨 Enemy 뒤를 터치하면 Enemy 앞에서 멈추지 않아야 함 | P0/확정 |
| PC-001 | Player 공격 실행 | 공격 위치 도착 또는 사거리 내 Enemy 재터치 | Player가 공격 가능, 타깃 생존 | 정면/후면과 치명타를 판정하고 `CombatResolver` 결과를 Enemy에 적용 | Enemy 피해, 처치 또는 피해 무효 | 조작 불가, 사망, 공격 중복 요청 시 동작하지 않음 | 공격 모션, 타격 VFX/SFX | `PlayerCombat`, `EnemyBase`, `CombatResolver` | 동일 조건에서 기획 타격 수 표와 같은 결과가 나와야 함 | P0/확정 |
| PC-002 | 정면/후면 판정 | Player 공격 직전 | Enemy 바라보는 방향, Player 위치 | Enemy 기준 앞 180도는 정면, 뒤 180도는 후면으로 판정 | 공격 방향 결과 반환 | 정확히 측면 경계에 위치할 때 판정 기준은 구현 시 고정 필요 | 후면 공격 성공 표시 검토 | `EnemyFacing`, `CombatResolver` | Enemy의 뒤쪽 좌표에서는 후면, 앞쪽 좌표에서는 정면이어야 함 | P0/부분 확정 |
| PC-003 | 치명타 판정 | 유효한 Player 공격마다 | 현재 치명타 확률 0~70% | 공격당 한 번 난수를 판정하고 성공 시 방향별 치명타 피해 적용 | 정면은 후면 일반 1회분, 후면은 후면 일반 3회분 적용 | 치명타 확률은 70% 초과 불가 | 치명타 VFX, 강조 텍스트 또는 SFX | `CriticalSystem`, `CombatResolver` | 0%에서는 발생하지 않고 70% 이상으로 증가하지 않아야 함 | P0/확정 |
| PC-004 | 방패병 공격 후 조작 불가 | 조건을 만족하는 ShieldEnemy 정면 공격 성공 | ShieldEnemy가 Player보다 2레벨 이상 낮지 않음 | 공격 결과 적용 후 Player를 0.5초간 조작 불가 상태로 전환 | 0.5초 동안 이동·공격 불가 | ShieldEnemy 후면 공격 또는 2레벨 이상 낮은 ShieldEnemy 정면 공격에는 적용하지 않음 | 넉백/경직 모션, 입력 불가 피드백 | `PlayerStateMachine`, `ShieldEnemy` | 조건별로 조작 불가 적용 여부가 기획과 일치해야 함 | P0/확정 |

## 7. Player 생명 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| PH-001 | Player 피격 | Enemy 공격 판정과 Player가 겹침 | Player 생존, 피격 가능 | Enemy 공격력만큼 HP 감소 | HP 갱신 또는 사망 | 피격 무적 시간과 Player 최대 HP 미정 | HP Bar, 피격 Flash, 흔들림 | `PlayerHealth`, `EnemyAttackBase` | 공격 판정 밖에서는 HP가 감소하지 않아야 함 | P0/부분 확정 |
| PH-002 | Player 사망 | Player HP가 0 이하 | Player 생존 상태 | 입력과 공격을 차단하고 사망 상태로 전환, 일반 Enemy에 Player 사망 알림 | Player 비활성/사망 연출, 추적 Enemy가 Castle로 복귀 | Castle은 Player 사망으로 게임 오버되지 않음 | 사망 모션, 부활 대기 UI | `PlayerHealth`, `PlayerStateMachine`, `EnemyTargeting` | Player 사망 후 추적 중인 일반 Enemy 목표가 Castle이어야 함 | P0/확정 |
| PH-003 | Player 자동 부활 | 사망 후 부활 대기시간 종료 | Castle이 파괴되지 않았거나 게임이 계속 진행 중 | Castle 위치에서 Player 상태와 HP를 복구하고 입력 활성화 | Player가 게임에 복귀 | 부활 대기시간과 자동 부활 HP는 미정 | 부활 연출 | `PlayerRoot`, `PlayerHealth`, `GameSession` | 대기시간 전에는 입력할 수 없고 부활 후 입력 가능해야 함 | P0/부분 확정 |
| PH-004 | 광고 이어하기 Player 부활 | 광고 이어하기 성공 | GameOver 상태 | Player를 Castle 위치에서 최대 HP로 부활 | Player 생존 및 입력 대기 상태 | Castle 3초 무적과 GameSession 재개 순서를 따라야 함 | 부활 연출 | `PlayerRoot`, `GameSession` | 이어하기 후 Player HP가 최대치여야 함 | P1/확정 |

## 8. 일반 Enemy 공통 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| EN-001 | Enemy 생성 및 초기화 | WaveSpawner 생성 요청 | EnemyDefinition, 레벨, SpawnPoint, Pool 준비 | Pool에서 인스턴스를 가져와 상태, 타깃, 쿨타임, HP와 시각 요소 초기화 | Enemy가 Spawn 상태로 활성화 | 필수 Definition 또는 Prefab 누락 시 생성 실패를 기록 | Spawn 연출 선택 | `EnemyFactory`, `PoolService`, `EnemyBase` | 재사용 Enemy에 이전 타깃·HP·이벤트가 남아 있지 않아야 함 | P0/부분 확정 |
| EN-002 | Castle로 이동 | Enemy Spawn 완료 또는 Player 목표 해제 | Castle 생존, Enemy 이동 가능 | Castle 위치를 목표로 이동하고 진행 방향을 갱신 | Castle 공격 범위 진입 시 공격 상태 전환 | 이동속도, 회피 및 충돌 정책은 데이터 미정 | 이동 애니메이션 | `EnemyMovement`, `EnemyStateMachine` | 방해가 없으면 Enemy가 Castle 방향으로 이동해야 함 | P0/부분 확정 |
| EN-003 | Player로 목표 변경 | 일반 Enemy가 Player 공격을 받음 | Player 생존 | 현재 타깃을 Player로 변경하고 추적 상태로 전환 | Enemy가 Player를 추적 | Boss와 ShieldEnemy에는 각 고유 규칙 적용 | 타깃 변경 표시 선택 | `EnemyTargeting`, `EnemyStateMachine` | 일반 근거리·원거리 Enemy 피격 후 Player를 추적해야 함 | P0/확정 |
| EN-004 | Castle 목표 복귀 | 추적 대상 Player 사망 | 일반 Enemy 생존 | Player 참조를 해제하고 Castle을 목표로 설정 | Castle 이동 상태 복귀 | Boss는 원래부터 Castle 목표 유지 | 없음 | `EnemyTargeting`, `EnemyStateMachine` | Player 사망 후 일반 Enemy가 죽은 Player 위치에 머물지 않아야 함 | P0/확정 |
| EN-005 | 추적 방향 지연 갱신 | Player 추적 중 Player 위치 변경 | Enemy 생존, Player 추적 상태 | 0.7초 동안 기존 이동 방향을 유지한 후 Player 현재 위치로 방향 갱신 | Enemy 바라보는 방향과 이동 목표 갱신 | 연속 이동 시 타이머 재설정 또는 주기 갱신 방식은 구현 시 고정 필요 | 회전/방향 전환 애니메이션 | `EnemyFacing`, `EnemyTargeting` | Player가 방향을 바꿔도 Enemy가 즉시 회전하지 않아야 함 | P0/부분 확정 |
| EN-006 | Enemy 공격 실행 | 공격 대상이 사거리 안이고 쿨타임 완료 | Enemy 공격 가능, 대상 생존 | 공격 범위를 예고하고 공격 모션과 판정 실행 후 쿨타임 적용 | Player 또는 Castle HP 감소 | Enemy별 예고 시간, 공격력, 범위와 쿨타임 미정 | 공격 범위, 모션, SFX | `EnemyAttackBase`, `EnemyDefinition` | 사거리 밖 대상에게 피해가 적용되지 않아야 함 | P0/부분 확정 |
| EN-007 | Enemy 사망 | 누적 피해가 처치 기준 도달 | Enemy 생존 | 공격·이동·Collider를 중지하고 사망 이벤트 발행 후 Pool 반환 | 점수/경험치 지급, Enemy 제거 | ShieldEnemy 점수 없음, 최종 Boss는 클리어 연결 | 흰색 Flash, 사망 모션, VFX | `EnemyHealth`, `ScoreSystem`, `ExperienceSystem`, `PoolService` | 사망 이벤트와 보상이 한 번만 발생해야 함 | P0/확정 |

## 9. 원거리 및 근거리 Enemy 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| RA-001 | 원거리 Castle 공격 취소 | Castle 공격 모션 중 Player 공격 수신 | 원거리 Enemy의 현재 목표가 Castle | 현재 공격 모션과 판정을 취소하고 목표를 Player로 변경 | Player 추적 상태 전환 | 이미 목표가 Player면 공격 취소하지 않음 | 공격 취소 피드백 선택 | `RangedEnemy`, `RangedAttack`, `EnemyTargeting` | Castle 목표일 때만 공격이 취소돼야 함 | P0/확정 |
| RA-002 | 원거리 공격 | Player 또는 Castle이 공격 사거리 안 | 공격 쿨타임 완료 | 정의된 원거리 공격 Strategy 실행 | 대상 HP 감소 | 투사체/즉시 피해 방식, 속도, 충돌과 공격 수치는 미정 | 공격 예고, 투사체 또는 VFX | `RangedAttack`, `EnemyDefinition` | 선택된 공격 방식에 따라 한 번만 피해가 적용돼야 함 | P0/부분 확정 |
| ME-001 | 근거리 공격 | Player 또는 Castle이 근접 사거리 안 | 공격 쿨타임 완료 | 이동을 정지하고 근거리 공격 모션 후 범위 판정 | 대상 HP 감소 | 공격력, 범위, 예고와 쿨타임 미정 | 근거리 공격 모션과 범위 표시 | `MeleeAttack`, `EnemyDefinition` | 공격 범위 밖 대상은 피해를 받지 않아야 함 | P0/부분 확정 |

## 10. ShieldEnemy 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| SH-001 | Player 추적 및 경로 차단 | ShieldEnemy Spawn | Player 생존 | Player를 향해 이동하며 Player 이동·공격 경로의 장애물 역할 수행 | Player가 목표까지 바로 이동할 수 없음 | ShieldEnemy는 Player를 직접 공격하지 않음 | 방패 방향 표시 | `ShieldEnemy`, `EnemyMovement`, `PlayerTargetSelector` | ShieldEnemy가 사거리 안에서도 공격 행동을 하지 않아야 함 | P0/확정 |
| SH-002 | 원거리 터치 접근 | 하늘색 범위 밖 ShieldEnemy 또는 뒤쪽 목표 터치 | Player 입력 가능, ShieldEnemy가 경로 차단 | ShieldEnemy의 하늘색 범위 끝까지 이동하고 공격하지 않음 | Player가 ShieldEnemy 앞에서 정지 | 공격이 없으므로 0.5초 조작 불가도 적용하지 않음 | 차단 피드백 | `PlayerMovement`, `ShieldEnemy` | 첫 원거리 터치에서는 ShieldEnemy 타격 수가 감소하지 않아야 함 | P0/확정 |
| SH-003 | ShieldEnemy 정면 공격 | 하늘색 범위 안에서 ShieldEnemy 정면 터치 | Player 공격 가능 | 정면 타격을 누적하고 조건에 따라 0.5초 조작 불가 적용 | 3타 누적 시 처치 | ShieldEnemy가 Player보다 2레벨 이상 낮으면 조작 불가 없음 | 방패 충돌, Player 경직 | `ShieldEnemy`, `PlayerStateMachine` | 레벨 조건과 관계없이 정면 3타 처치, 경직 여부만 달라야 함 | P0/확정 |
| SH-004 | ShieldEnemy 후면 공격 | ShieldEnemy 후면 공격 성공 | Player 공격 가능 | 후면 공격 1회를 처치 피해로 적용 | 즉시 처치 | 레벨과 관계없이 동일 | 후면 처치 VFX | `ShieldEnemy`, `CombatResolver` | 모든 레벨의 ShieldEnemy가 후면 1타에 처치돼야 함 | P0/확정 |

## 11. Boss 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| BO-001 | Boss 생성 | 보스 Wave 또는 10분 최종 보스 Trigger | 위/아래 Boss SpawnPoint 준비 | Spawn 방향을 선택하고 WARNING 후 Boss 활성화 | Boss가 Castle 방향으로 이동 | 등장 WARNING 시간과 최종 Boss 레벨 미정 | WARNING, Boss HP Bar | `WaveSpawner`, `EnemyFactory`, `BossEnemy` | 좌우가 아닌 위/아래 지정 SpawnPoint에서 생성돼야 함 | P1/부분 확정 |
| BO-002 | Boss 목표 유지 | Boss가 Player 공격을 받음 | Boss 생존 | 피해만 적용하고 이동 목표는 Castle로 유지 | Castle 방향 이동 지속 | 공격 행동은 Player 공격으로 취소되지 않음 | 피격 연출 | `BossEnemy`, `EnemyTargeting` | 반복 피격 후에도 Boss 기본 목표가 Castle이어야 함 | P1/확정 |
| BO-003 | Boss 공격 주기 | Player가 Boss 공격 범위 안이고 새 주기 시작 가능 | Boss 생존 | 0~1.5초 이동하며 붉은 영역 표시, 1.5~2.0초 정지·공격, 2.0~3.0초 Castle로 이동 | 3초마다 조건부 공격 반복 | 영역 모양·크기와 예고 중 Player 이탈 처리 미정 | 붉은 영역, 공격 모션 | `BossAttack`, `BossStateMachine` | 구간별 이동/정지와 공격 판정 시간이 정의와 일치해야 함 | P1/부분 확정 |
| BO-004 | Boss 피해 처리 | Player가 Boss 공격 | 공격 방향과 치명타 결과 | HP 15 기준 정면 1, 후면 3, 정면 치명타 3, 후면 치명타 9 적용 | HP 0 이하에서 Boss 사망 | Boss 행동은 피격으로 취소하지 않음 | Boss HP Bar, 피격 VFX | `BossEnemy`, `CombatResolver` | 정면 일반 15회 또는 후면 일반 5회에 처치돼야 함 | P1/확정 |
| BO-005 | Boss 이어하기 넉백 | 광고 이어하기 성공 | Boss 생존 | 현재 위치에서 Castle→Boss 방향 맵 경계까지 남은 거리의 50% 이동 | Boss가 Castle에서 멀어짐 | 맵 밖으로 이동하지 않음 | 넉백 연출 | `EnemyKnockbackService`, `BossEnemy` | 일반 Enemy보다 짧고 정확히 남은 거리 절반만 이동해야 함 | P1/확정 |

### 11.1 Boss 공격 시간표

| 구간 | Boss 이동 | 붉은 공격 위치 | 공격 판정 |
|---|---|---|---|
| 0.0~1.5초 | Castle 방향 이동 | Boss 이동에 맞춰 이동 | 없음 |
| 1.5~2.0초 | 정지 | 공격 위치 유지 | 활성 |
| 2.0~3.0초 | Castle 방향 이동 | 비활성 | 없음 |

## 12. Castle 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| CA-001 | Castle 피격 | Enemy 공격 판정 명중 | Castle 생존, 무적 아님 | 공격력만큼 HP 감소 | HP 갱신 또는 Castle 파괴 | 무적 중 공격은 HP 감소 없음 | Castle HP Bar, 피격 연출 | `CastleHealth`, `EnemyAttackBase` | 3초 무적 중 같은 공격에 HP가 감소하지 않아야 함 | P0/부분 확정 |
| CA-002 | Castle 파괴 | HP가 0 이하 | 무적 아님 | Castle 파괴 이벤트를 한 번 발행 | 게임 오버 Trigger | 동시에 여러 공격을 받아도 중복 GameOver 금지 | 파괴 연출, GameOver UI | `CastleHealth`, `GameSession` | GameOver 이벤트가 한 번만 발생해야 함 | P0/확정 |
| CA-003 | 광고 이어하기 | ContinueAd 버튼 클릭 후 광고 성공 | 사용 횟수 2회 미만, GameOver 상태 | Castle 50% HP 복구, Player 최대 HP 부활, Castle 3초 무적, Enemy 넉백 후 게임 재개 | 사용 횟수 1 증가, Playing 복귀 | 광고 실패/취소와 재개 순서 미정 | 광고 UI, 부활 연출, 무적 표시 | `GameSession`, `AdService`, `CastleRoot`, `ContinueView` | 3회째 이어하기가 불가능하고 각 복구 값이 정확해야 함 | P1/부분 확정 |
| CA-004 | 일반 Enemy 이어하기 넉백 | 광고 이어하기 성공 | 일반 Enemy 생존 | Castle에서 Enemy를 향한 방향과 플레이 월드 경계 교점을 구해 Enemy 이동 | Enemy가 해당 방향 맵 가장자리로 이동 | Boss는 별도 50% 규칙 적용 | 넉백 VFX | `EnemyKnockbackService`, `MapBounds` | 좌우 Enemy가 상하 Enemy보다 짧게 이동할 수 있으나 모두 경계 안에 있어야 함 | P1/확정 |

## 13. 인게임 경험치 및 카드 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| PR-001 | 인게임 경험치 획득 | 일반 Enemy 또는 Boss 처치 | 보상 지급 가능한 Enemy 사망 | Enemy 레벨과 같은 경험치를 Player에 추가 | 경험치 Bar 갱신, 필요량 도달 시 레벨업 | ShieldEnemy 경험치 지급 여부는 기획서에 명시되지 않아 확인 필요 | 경험치 획득 피드백 | `ExperienceSystem`, `PlayerLevel` | 3레벨 Enemy 처치 시 EXP 3 증가해야 함 | P0/부분 확정 |
| PR-002 | 인게임 레벨업 | 누적 EXP가 현재 레벨 요구량 이상 | 현재 레벨 L, 요구량 L×10 | 필요 EXP를 차감하고 Player 레벨 1 증가 | 레벨업 카드 UI 요청 | 초과 EXP 유지, 다중 레벨업 처리와 UI Pause 여부 미정 | 레벨업 연출 | `PlayerLevel`, `LevelUpPresenter` | 1레벨에서 EXP 10 도달 시 2레벨이 돼야 함 | P0/부분 확정 |
| PR-003 | 능력 카드 3개 표시 | Player 레벨업 발생 | 카드 후보 Pool 준비 | 중복 허용 규칙에 따라 후보 3개를 구성해 표시 | Player가 카드 1개 선택 가능 | 치명타 외 카드와 최대치 카드 제외 규칙 미정 | LevelUpView, 카드 아이콘·설명 | `CardSelectionSystem`, `LevelUpView` | 정확히 3개의 선택 가능한 카드가 표시돼야 함 | P1/부분 확정 |
| PR-004 | 치명타 카드 선택 | 치명타 확률 증가 카드 클릭 | 현재 치명타 확률 70% 미만 | 치명타 확률을 10% 증가시키고 70%로 제한 | 확률 갱신 후 레벨업 UI 종료 | 70% 도달 후 카드 후보 제외 여부 미정 | 갱신된 치명타 확률 표시 | `CriticalSystem`, `LevelUpPresenter` | 6회 선택 시 60%, 7회 이상 선택해도 70%여야 함 | P0/확정 |

## 14. 점수 및 계정 성장 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| SC-001 | 일반 Enemy 처치 점수 | 일반 Enemy 사망 | 중복 보상 아님 | Enemy 레벨만큼 점수 추가 | 현재 점수 갱신 | ShieldEnemy는 점수 없음 | 점수 증가 표시 | `ScoreSystem`, `EnemyDefinition` | 4레벨 일반 Enemy 처치 시 4점 증가해야 함 | P0/확정 |
| SC-002 | Boss 처치 점수 | Boss 사망 | 중복 보상 아님 | Boss 레벨×2 점수 추가 | 현재 점수 갱신 | 최종 Boss도 동일 공식 적용 여부는 현재 동일하게 가정 | 보스 점수 표시 | `ScoreSystem`, `BossDefinition` | 5레벨 Boss 처치 시 10점 증가해야 함 | P1/부분 확정 |
| SC-003 | 생존 점수 | 생존 시간 증가 또는 게임 종료 | Castle 생존 시간 | 정의된 수식에 따라 시간 점수 계산 | 최종 점수에 합산 | 1분 3, 2분 5, 3분 7 예시만 존재하며 수식·누적 방식 미정 | HUD 또는 결과 화면 | `ScoreSystem`, `GameSession` | 수식 확정 전 구현 보류 | P1/미정 |
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
| UI-004 | HUD 상태 갱신 | HP, 점수, 시간, 레벨, 치명타 변경 이벤트 | HUD 활성화 | Presenter가 값을 표시 형식으로 변환하고 View의 의미 기반 메서드 호출 | 대응 TMP_Text와 Gauge 갱신 | Registry를 게임 시스템에서 직접 호출하지 않음 | HUD | `HUDPresenter`, `HUDView` | ScoreChanged 10 입력 시 HUD 점수가 10으로 표시돼야 함 | P1/확정 |
| UI-005 | 레벨업 UI | 레벨업 발생 | 카드 3개 준비 | LevelUpView 활성화, 카드 데이터 표시, 선택 Listener 연결 | 선택 결과 전달 후 UI 닫힘 | Pause 여부와 치명타 외 카드 미정 | 카드 3개 | `LevelUpPresenter`, `LevelUpView` | 카드 하나를 선택하면 효과가 한 번 적용되고 화면이 닫혀야 함 | P1/부분 확정 |
| UI-006 | 광고 이어하기 UI | Castle 파괴 | 남은 이어하기 횟수 계산 | ContinueAd와 포기 선택 표시, 남은 횟수 출력 | 광고 요청 또는 최종 결과 진행 | 광고 실패·취소·네트워크 오류 흐름 미정 | ContinueView | `ContinuePresenter`, `AdService` | 2회 사용 후 ContinueAd 버튼이 비활성 또는 미표시돼야 함 | P1/부분 확정 |

## 16. 데이터 및 Spawn 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| DA-001 | EnemyDefinition 로드 | GameBootstrap 초기화 | 검증된 `.bytes` 또는 프로토타입 Definition | ID별 Enemy 설정을 읽어 조회 가능한 Repository 구성 | EnemyFactory가 Definition 조회 가능 | 중복 ID, Prefab 누락, 음수 수치는 오류 처리 | 없음 | `DataRepository`, `EnemyDefinition` | 유효 ID 조회 성공, 잘못된 ID 조회 시 명확한 오류가 발생해야 함 | P0/부분 확정 |
| DA-002 | WaveData 로드 | GameBootstrap 초기화 | Excel에서 변환된 Wave 데이터 | 생성 시점 순으로 검증·정렬해 WaveSpawner에 제공 | 게임 시간 기반 Spawn 가능 | 실제 Wave 데이터와 포맷 미작성 | 없음 | `DataRepository`, `WaveData` | 시간, 종류, 레벨, 위치가 없는 행을 거부해야 함 | P1/미정 |
| SP-001 | 시간 기반 Enemy Spawn | 게임 시간이 Wave 행의 생성 시점에 도달 | WaveData, SpawnPoint, EnemyFactory | 종류, 레벨, 수량과 간격에 따라 생성 요청 | 지정 Enemy가 지정 위치에 생성 | SpawnPoint 없음, 동시 최대 수와 실패 재시도 규칙 미정 | Spawn 연출 선택 | `WaveSpawner`, `EnemyFactory`, `WaveData` | 같은 Wave 행이 중복 실행되지 않아야 함 | P1/미정 |
| PL-001 | Enemy Pool 재사용 | Enemy 생성 또는 사망 | Prefab별 Pool | 요청 시 비활성 인스턴스를 초기화해 반환하고 사망 후 초기화하여 보관 | 반복 Instantiate/Destroy 감소 | Pool 부족 시 확장 수와 최대치 미정 | 없음 | `PoolService`, `EnemyFactory` | 재사용 Enemy에 이전 HP·타깃·Listener가 남지 않아야 함 | P1/부분 확정 |

## 17. 저장 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| SV-001 | 계정 데이터 저장 | 게임 결과 확정 또는 중요 계정 변경 | 계정 레벨, EXP, 최고 점수, 클리어 정보 | 영구 저장 포맷으로 직렬화 | 다음 실행에서 복구 가능한 저장 데이터 생성 | 저장 위치, 암호화, 실패 복구와 버전 마이그레이션 미정 | 저장 실패 안내 필요 여부 미정 | `SaveService`, `AccountProgression` | 앱 재실행 후 계정 레벨과 EXP가 유지돼야 함 | P1/부분 확정 |
| SV-002 | 계정 데이터 로드 | 앱 또는 게임 초기화 | 저장 데이터 존재 여부 | 데이터 검증 후 계정 상태 복구, 없으면 기본값 생성 | 계정 성장과 시작 레벨 적용 준비 | 손상 데이터 처리 방식 미정 | 없음 | `SaveService`, `GameBootstrap` | 최초 실행은 기본값, 기존 사용자는 저장값으로 시작해야 함 | P1/부분 확정 |

## 18. 연출 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| FX-001 | 일반 타격 피드백 | 유효 공격 명중 | 대상 생존 또는 처치 직전 | 대상 흰색 Flash, 타격 이펙트, 모션과 흔들림 실행 | 명중 여부를 시각·청각적으로 전달 | 피해 무효 정면 공격은 별도 방어 피드백 필요 | Flash, VFX, SFX, Shake | `EnemyVisual`, `PlayerVisual` | 명중 공격과 피해 무효 공격이 시각적으로 구분돼야 함 | P1/부분 확정 |
| FX-002 | Enemy 공격 예고 | Enemy 공격 시작 전 | 공격 범위와 예고 시간 | 실제 공격 범위를 표시하고 판정 시점에 제거 또는 변경 | Player가 회피 가능 | 일반 Enemy 수치 미정 | 범위 표시 | `EnemyAttackBase`, `EnemyVisual` | 표시된 범위와 실제 판정 범위가 일치해야 함 | P1/부분 확정 |
| FX-003 | Boss 공격 예고 | Boss 공격 주기 0~1.5초 | Player가 공격 범위 안 | 붉은 공격 위치를 표시하고 Boss 이동에 맞춰 이동 | 1.5~2.0초 공격 위치 전달 | 영역 모양·크기, Player 이탈 처리 미정 | 붉은 범위 | `BossAttack`, `BossVisual` | 예고 영역이 1.5초 동안 Boss 이동을 따라야 함 | P1/부분 확정 |

## 19. 맵 및 경계 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| MP-001 | 플레이 영역 제한 | Player/Enemy 이동 목적지 계산 | 카메라 기준 플레이 가능 월드 경계 | 목적지를 경계 안으로 제한 | Entity가 맵 밖으로 나가지 않음 | Safe Area와 해상도별 월드 경계 계산 방식 미정 | 없음 | `MapBounds`, `PlayerMovement`, `EnemyMovement` | 1080×1920 기준 모든 이동 결과가 경계 내부여야 함 | P0/부분 확정 |
| MP-002 | 넉백 경계 교점 계산 | 광고 이어하기 Enemy 넉백 | Castle 위치, Enemy 위치, 월드 경계 | Castle→Enemy 방향 Ray와 경계의 교점을 계산 | 일반 Enemy 목적지 또는 Boss 절반 거리 반환 | Castle과 Enemy 위치가 같은 경우 대체 방향 필요 | 없음 | `MapBounds`, `EnemyKnockbackService` | 상하좌우 및 대각선 Enemy 모두 정확한 경계점을 계산해야 함 | P1/부분 확정 |

## 20. 광고 기능

| 기능 ID | 기능명 | 사용 시점/Trigger | 선행조건/입력 | 처리 방식 | 결과/출력 | 예외/제약 | UI/연출 | 담당 시스템/데이터 | 검증 기준 | 우선순위/상태 |
|---|---|---|---|---|---|---|---|---|---|---|
| AD-001 | 광고 이어하기 요청 | ContinueAd 버튼 클릭 | GameOver, 사용 횟수 2회 미만 | 보상형 광고를 요청하고 성공 결과만 GameSession에 전달 | 성공 시 CA-003 실행 | 광고 실패, 취소, No Fill과 네트워크 오류 정책 미정 | 광고 로딩/실패 UI | `AdService`, `ContinuePresenter` | 광고 성공 전에는 Castle과 Player가 부활하지 않아야 함 | P1/부분 확정 |
| AD-002 | 경험치 2배 광고 | 결과 화면 또는 정의된 시점 | 광고 시청 가능 | 광고 성공 시 대상 경험치를 2배 적용 | 경험치 보상 증가 | 인게임/계정 EXP 중 대상, 사용 시점과 횟수 미정 | 보상 광고 UI | `AdService`, `AccountProgression` 또는 `ExperienceSystem` | 규칙 확정 전 구현 보류 | P2/미정 |

## 21. 범위 제외 기능

| 기능 ID | 기능명 | 제외 사유 | 재검토 시점 | 상태 |
|---|---|---|---|---|
| EX-001 | 상하좌우 확장 맵 | 초기 고정 화면 전투 검증이 우선 | 핵심 전투와 Wave 완성 후 | 범위 제외 |
| EX-002 | 카메라 이동 대형 맵 | Enemy Spawn 및 경계 규칙 복잡도 증가 | 확장 맵 도입 시 | 범위 제외 |
| EX-003 | 공격력 업그레이드 | 현재 전투는 레벨 차이와 타격 횟수 중심 | 카드 시스템 확장 시 | 범위 제외 |
| EX-004 | 다수의 완성 카드 | 초기에는 치명타 카드 효과 검증 우선 | 기본 LevelUp UI 완성 후 | 범위 제외 |

## 22. 전투 판정 기준표

| Enemy와 Player의 레벨 관계 | 원거리 정면 | 원거리 후면 | 근거리 정면 | 근거리 후면 |
|---|---:|---:|---:|---:|
| Enemy가 낮음 | 1타 | 1타 | 1타 | 1타 |
| 동일 레벨 | 1타 | 1타 | 3타 | 1타 |
| Enemy가 1레벨 높음 | 3타 | 1타 | 피해 없음 | 2타 |
| Enemy가 2레벨 높음 | 피해 없음 | 2타 | 피해 없음 | 3타 |
| Enemy가 3레벨 높음 | 피해 없음 | 3타 | 피해 없음 | 4타 |
| Enemy가 N레벨 높음, N≥2 | 피해 없음 | N타 | 피해 없음 | N+1타 |

별도 규칙:

- ShieldEnemy는 정면 3타, 후면 1타다.
- Boss는 HP 15 기준 정면 1, 후면 3의 피해를 받는다.
- 정면 치명타는 해당 Enemy의 후면 일반 공격 1회분이다.
- 후면 치명타는 해당 Enemy의 후면 일반 공격 3회분이다.

## 23. 현재 미정으로 남은 핵심 항목

다음 항목은 기능 구조는 정의됐지만 최종 동작 또는 수치가 필요하다.

1. Player와 Castle 최대 HP
2. Player 자동 부활 대기시간과 부활 HP
3. 일반 Enemy별 이동속도, 공격력, 범위, 예고와 쿨타임
4. Player 이동속도, 공격 사거리, 접근 범위와 터치 보정 반경
5. 동일 우선순위 Enemy가 여러 명일 때 최종 선택 기준
6. Player 이동 중 타깃이 사망했을 때 처리
7. Boss 등장 WARNING 시간
8. Boss 공격 영역 모양·크기와 예고 중 Player 이탈 처리
9. 최종 Boss 레벨
10. 생존 점수 수식과 누적 방식
11. 인게임 레벨업 초과 EXP와 다중 레벨업 처리
12. ShieldEnemy 경험치 지급 여부
13. 치명타 확률 70% 도달 후 카드 후보 제외 여부
14. 치명타 외 능력 카드
15. 레벨업 UI 표시 중 게임 일시정지 여부
16. 계정 5레벨 이후 요구 EXP와 최대 레벨
17. 계정 레벨에서 Player 시작 레벨로 변환하는 규칙
18. 실제 Wave 및 Spawn 데이터
19. 광고 실패·취소·No Fill 처리
20. 광고 경험치 2배 대상과 적용 시점
21. 저장 포맷, 손상 복구와 버전 마이그레이션

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
