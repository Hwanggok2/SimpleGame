# 2026-08-02 작업 기록

## 작업 목표

- 모드 1에서 이동만으로 접근했을 때 한 번에 3회 이상 피해가 발생하는 원인을 확인하고 수정한다.
- 참격을 이동 입력이 아닌 기본 공격 기반 확률 스킬로 바꾼다.
- 관통의 공격 효과와 이동 효과를 분리하고 모드 1의 비관통 이동을 보완한다.
- 마지막 기본 공격 대상을 모드 1 잠금 대상으로 유지한다.
- 모드 1 수동 이동의 경로 우선 대상, 양측 교전 가능 반경, 중립 입력 우선순위와 이동 관통 재충전 규칙을 실제 구현에 맞춘다.
- 원거리 고블린·방패병을 상대로 원주 이동 중 공격과 관통이 간헐적으로 끊기고, Enemy 접근 후 Player가 바깥으로 밀리는 문제를 수정한다.
- 변경 규칙을 Excel, GameString, 기획서와 기능 정의서에 동기화한다.

## 원인 확인과 수정

### 모드 1 이동 중 다중 피해

- 원인은 `PlayerController.ResolveLockedEnemy()`가 잠금 대상이 없는 정상 상태에서도 `nextModeOneAttackAt`을 0으로 초기화하던 로직이었다.
- 이동 중 자동 공격은 공격 직후 다음 허용 시각을 `현재 시각 + 0.3초`로 예약했지만, 다음 프레임의 잠금 조회가 이를 다시 0으로 만들어 프레임마다 공격할 수 있었다.
- 잠금이 null인 조회 경로에서는 공격 예약 시각을 변경하지 않도록 수정했다.
- 잠금 대상이 실제로 사망했거나 `SpawnGeneration`이 달라진 경우에만 잠금과 관련 상태를 정리한다.
- 잠금이 없는 상태에서 조회를 반복해도 예약 시각이 유지되는 회귀 테스트를 추가했다.

### 모드 1 이동 공격 선행조건 수정

- 모드 1 좌측 패드의 사거리 공격은 모드 고유 기능인데도 설정의 `자동 공격` On/Off에 묶여 있었다. 기본값 Off에서는 우측 자동 조준을 누르지 않은 이동 공격이 대상 탐색 전에 반환되는 것이 원인이었다.
- 비영점 좌측 패드 입력 중에는 자동 공격 설정과 우측 자동 조준 사용 여부와 무관하게 사거리 안 Enemy를 0.3초 간격으로 공격하도록 분리했다.
- 패드 중앙을 누르기만 한 중립 입력은 이동 공격을 만들지 않고, Pointer 소유 상태만 남았다는 이유로 잠긴 대상의 자동 접근도 차단하지 않는다.
- 최소 입력 임계값 이상의 실제 방향 입력만 자동 접근보다 우선한다. 자동 공격 설정은 유효한 방향 입력이 없을 때 지정 대상을 자동 추적·반복 공격할지만 계속 제어한다.

## 모드 1 대상 잠금과 이동

- 기본 공격이 실제 실행된 뒤 주 대상이 살아 있으면 해당 Enemy를 마지막 타격 대상으로 잠근다.
- 방패에 막힌 기본 공격도 마지막 대상과 참격 발동 판정에 포함한다.
- 관통 후속 타격과 정전기·오물·이기어검·참격 등 추가 피해는 잠금을 바꾸지 않는다.
- 우측 자동 조준 또는 월드 직접 터치로 새 대상을 지정하면 기존 잠금을 즉시 덮어쓴다.
- 자동 접근 중에는 살아 있는 잠금 대상이 사거리 밖에 있다는 이유만으로 주변 Enemy로 교체하지 않는다.
- 실제 방향 이동 중에는 진행 경로상의 첫 Enemy를 기존 잠금보다 먼저 공격·이동 대상으로 사용한다. 경로 대상은 탐색만으로 잠금을 바꾸지 않고 실제 기본 공격 뒤에만 새 마지막 타격 잠금이 된다.

### 교전 반경 원주 이동

- 기존에는 Player 공격 사거리 1.2를 그대로 원주 반경으로 사용해 공격 시작 사거리가 0.85인 근접 고블린과 0.95인 플라잉 아이가 공격을 시작하지 못했다.
- Player 안전 반경을 `playerAttackRange-min(0.02, playerAttackRange×0.1)`로 두고, 일반 Melee/Ranged는 Enemy 안전 반경과의 작은 값을 사용해 Player와 Enemy가 모두 안정적으로 공격 가능한 위치에서 선회하도록 했다.
- 일반 공격 사거리를 사용하지 않는 Shield/Boss, 공격 사거리 0 이하 또는 Definition 누락 대상도 Player 안전 반경을 사용한다. Player 기본 사거리 1.2 기준 Ranged/Shield/Boss 교전 반경은 1.18이다.
- 실제 방향 이동의 경로상 첫 Enemy는 잠금보다 먼저 원주 제약 대상이 되며, 실제 기본 공격 전에는 교전 반경 안쪽으로 통과하지 않는다.
- 정확한 안쪽 입력은 교전 반경에서 멈추고, 대각선·접선 입력은 안쪽 성분을 제거한 뒤 같은 원주에 재투영한다. 바깥 입력은 수정하지 않으며 대상 사망·세대 변경 시 제약을 해제한다.

### 원거리·방패병 공격과 관통 지연

- 원거리 고블린의 긴 공격 사거리 자체는 원인이 아니었다. Ranged와 Shield의 기존 교전 반경이 Player 최대 공격 사거리 1.2와 정확히 같았고, 공격 판정도 `distance≤1.2`라 경계 오차에 취약했다.
- 기존 원주 이동은 접선 성분을 직선으로 더하고 원주에 재투영하지 않았다. 반경 1.2, 이동 속도 10, 60fps에서는 순수 접선 한 프레임만으로 거리가 약 1.2115가 되어 첫 공격 뒤 다음 공격 시점에는 사거리 밖으로 벗어났다.
- 공격이 끊기면 마지막 타격 대상의 이동 관통 허용 상태도 갱신되지 않아 원거리 고블린의 공격 관통과 이동 관통이 함께 불안정하게 느껴졌다. 관통 타깃 수집·0.4초 예산·주 대상 뒤 판정 파이프라인에는 별도 결함이 없음을 확인했다.
- 프레임 제약 반경을 `min(설정 교전 반경, 현재 Enemy와의 거리)`로 계산한다. Enemy가 다가와 거리가 가까워지면 그 거리를 보존하고, 안쪽·접선 입력은 현재 반경 원주에 재투영하며, 사용자의 명시적인 바깥 입력만 자유롭게 허용한다.
- 모드 1 잠금 대상 자동 접근도 같은 안전 교전 반경을 사용한다. 방패 정면 공격의 반동·0.5초 입력 잠금과 생존 가능한 방패 정면의 관통 차단은 기존 의도대로 유지한다.

## 관통 규칙 분리

- 공격 관통은 기존 0.4초 판정창을 유지한다. 판정창마다 카드 레벨만큼 주 대상 뒤의 Enemy에게 추가 기본 공격 피해를 적용한다.
- 이동 관통은 공격 관통과 별도 예산으로 분리했다.
- 좌측 패드 방향 입력 또는 월드 이동 명령을 시작할 때 카드 레벨만큼 이동 통과 예산을 부여한다.
- Enemy의 반대편 충돌 반경까지 실제로 통과했을 때 이동 횟수 1을 소비한다.
- 예산을 모두 소비하면 소진 시점부터 0.4초 뒤, 같은 이동 입력이 유지 중일 때 카드 레벨만큼 재충전한다.
- 같은 연속 입력에서 이미 지나간 동일 Enemy와 동일 SpawnGeneration은 예산 재충전 뒤에도 다시 소비하지 않고 경로 방해 대상으로 재선택하지 않는다.
- 모드 1에서는 경로상의 첫 Enemy를 실제로 기본 공격하고 마지막 타격 잠금과 관통 허용 결과가 갱신된 뒤에만 해당 Enemy를 지나갈 수 있다. 공격 전 경로 대상과 방패 정면 차단은 교전 반경 원주 이동으로 처리한다.
- 모드 1 이동 관통은 후보 시작 위치·시각을 저장하되, Enemy 반대편까지 실제로 통과하고 횟수를 소비한 뒤에만 절단을 확정한다. 관통이 취소되면 절단도 취소되며, 완료 시에는 시작 기준 0.3초 중 남은 시간만 적용한다.

## 자동·수동 공격 경합 보완

- 모드 2·숨기기에서 자동 반복 명령이 같은 Enemy로 접근 중일 때 수동 입력이 들어오면 대기 자동 공격을 수동 공격 1회로 교체한다.
- 자동 공격과 수동 공격을 한 프레임에 2회로 합산하지 않고, 다음 자동 공격은 수동 입력 0.3초 뒤로 미룬다.
- 이후의 연속 수동 입력은 기존 무제한 입력 장점을 유지하도록 각각 누적한다.

## 참격 변경

- 이동 명령과 좌측 패드 이동 시작에 있던 참격 발동 지점을 제거했다.
- 유효한 기본 공격마다 주 대상 방향으로 참격을 한 번 판정한다.
- 방패에 막힌 기본 공격도 판정한다.
- 별도 쿨다운과 연속 발동 제한은 두지 않았다.
- 관통 후속 타격과 스킬 추가 피해는 참격을 재귀 발동하지 않는다.
- 기존 피해·크기·사거리·최대 적중 수는 유지하고 발동 확률만 1.5배로 높였다.

| 레벨 | 발동 확률 | 피해 배율 | 최대 적중 |
|---:|---:|---:|---:|
| 1 | 15% | 1.8배 | 2 |
| 2 | 19.5% | 2.15배 | 3 |
| 3 | 24% | 2.5배 | 4 |
| 4 | 28.5% | 2.85배 | 5 |
| 5 | 33% | 3.2배 | 6 |

## 데이터와 문서 동기화

- `GameData_10min_Balance.xlsx`의 `GameString`에서 관통·절단·참격 설명을 새 규칙으로 변경했다.
- `CardMath`의 참격 확률을 15/19.5/24/28.5/33%로 변경하고 열 이름을 `참격` 기준으로 정리했다.
- `CardMath` 후면 피해 수식이 `LevelUpCard!H10`의 최대 중첩 수를 참조해 15배부터 계산되던 오류를 `LevelUpCard!G10`의 1.8 피해 배율 참조로 수정했다. 후면 피해는 5.4/6.45/7.5/8.55/9.6배로 계산된다.
- `BalanceSummary`에 공격/이동 관통 분리, 참격 발동식과 당시 모드 1 원주 이동 규칙을 추가했다. 후속 교전 반경·입력 우선순위는 기획서와 기능 정의서에 반영했고, 0.4초 이동 관통 재충전 규칙은 `GameString`과 `CardMath`에도 반영해 데이터 재임포트 시 설명이 유지되도록 했다.
- Excel 수식 오류 검색 결과 `#REF!`, `#DIV/0!`, `#VALUE!`, `#NAME?`, `#N/A`는 0건이다.
- `GameStringTable.asset`과 `GameDataAssetBuilder` fallback 문구를 Excel과 동일하게 맞췄다.
- `GameDesignDocument.md` 22장·25장과 `FunctionalSpecification.md`의 PM-003, PC-006, PR-008, IN-009~IN-016 관련 규칙을 실제 구현에 맞게 갱신했다.

## 검증

- `dotnet build SimpleGame.sln --no-restore`: 성공, 오류 0건.
- 메인 편집기가 프로젝트를 사용 중이어서 검증용 복제 프로젝트에서 관련 테스트 70개와 Unity EditMode 전체 테스트를 실행했다: 관련 70개 통과, 전체 359개 통과, 실패 0개.
- 원주 재투영과 안전 반경 후속 수정 뒤 `MobileAimControlsTests` 55개와 Unity EditMode 전체 364개를 다시 실행했다: 55/55, 364/364 통과, 실패·건너뜀 0개.
- 참격 레벨별 확률·피해 회귀 테스트 갱신.
- 모드 1 null 잠금이 공격 예약 시각을 지우지 않는 회귀 테스트 추가.
- 사거리 원주 이동의 정면 정지·대각선 접선 이동·바깥 이동 테스트 추가.
- 이동 방향이 Enemy Collider 경로와 교차할 때만 모드 1 이동 관통을 시작하는 테스트 추가.
- 자동 반복 접근 중 같은 대상의 수동 입력이 대기 공격을 2회로 늘리지 않고 1회로 교체하는 회귀 테스트 추가.
- 실제 이동 관통 완료 시 절단이 시작 기준 0.3초의 남은 지연만 사용하는 테스트 추가.
- 자동 공격 Off·자동 조준 미사용 상태에서 비영점 좌측 패드 입력이 사거리 공격을 실행하고, 중립 입력과 0.3초 이내 재호출은 공격하지 않는 회귀 테스트 추가.
- 기존 잠금이 사거리 밖이어도 실제 이동 경로상의 Enemy를 먼저 공격하고, 실제 타격 뒤에만 잠금을 교체하는 회귀 테스트 추가.
- 중립 또는 잔류 패드 접촉이 우측 자동 조준의 자동 접근 명령을 막지 않는 회귀 테스트 추가.
- 큰 프레임 이동도 교전 반경 원을 한 번에 가로지르지 못하는 sweep-through 회귀 테스트 추가.
- Ranged/Shield/Boss의 Player 안전 반경 1.18, Enemy가 가까워진 거리의 보존, 반복 접선 이동의 반경 드리프트 방지, 명시적 후퇴 허용과 가까운 반경 sweep-through 차단 회귀 테스트 추가.
- 이동 관통 예산이 입력을 유지한 채 소진 0.4초 뒤 재충전되고 동일 SpawnGeneration 통과 이력은 보존되는 회귀 테스트 추가.
- Excel 전체 13개 시트를 렌더링해 표 구조와 변경 셀의 잘림 여부를 확인했다.
- 후속 회귀 검증 기준에는 경로상 첫 Enemy의 잠금 우선 공격·공격 전 통과 금지, Melee/Ranged 교전 반경과 Shield/Boss 예외, 중립·잔류 held 상태의 자동 접근 유지, 이동 관통 예산 소진 0.4초 뒤 재충전과 동일 SpawnGeneration 중복 제외를 포함했다.

## 주요 변경 파일

- `Assets/Game/Runtime/Player/PlayerController.cs`
- `Assets/Game/Runtime/Player/PlayerMovement.cs`
- `Assets/Game/Runtime/Player/PlayerCombatAbilities.cs`
- `Assets/Game/Runtime/Player/PlayerRoot.cs`
- `Assets/Game/Tests/EditMode/EnemyWorldServiceTests.cs`
- `Assets/Game/Tests/EditMode/MobileAimControlsTests.cs`
- `Assets/Game/Tests/EditMode/GameDataTests.cs`
- `Assets/Game/Tests/EditMode/GameDataExcelImporterTests.cs`
- `Planning/GameData_10min_Balance.xlsx`
- `Planning/GameDesignDocument.md`
- `Planning/FunctionalSpecification.md`

## 10차 전투 피드백·구조 정리

### 기존 최적화 적용 상태 감사

- 기존 작업에서 계획한 Enemy 제한 Pool, 오물 장판의 호출자 소유 List·비정렬 반경 쿼리, Enemy Collider 캐시, Animator 중복 파라미터 쓰기 방지가 실제 코드에 유지되고 있음을 확인했다.
- Archetype 상수만 반환하던 `MeleeEnemy`, `RangedEnemy`, `ShieldEnemy`, `BossEnemy` 네 클래스를 직렬화된 Archetype의 `EnemyActor` 하나로 통합하고 Enemy Prefab 8개와 Builder·테스트 참조를 이전했다.
- 호출되지 않던 `PlayerController.TickManualMovement()`를 제거했다. 이름만 유사하고 정책이 다른 `HealthComponent/EnemyHealth`, Player/Enemy HP Bar는 억지로 공용화하지 않았다.
- 카메라 흔들림, 캐릭터 Animator Adapter와 전투 피드백을 `Runtime/Presentation`으로 이동하고 신규 `DamagePopupView`도 같은 책임 폴더에 배치했다.

### 오물·정전기 융합 최초 피격

- 기존 `FilthProjectile`이 장판마다 `Enemy + SpawnGeneration` Dictionary를 소유하고 각 0.5초 틱의 현재 범위 대상을 검사하는 구조임을 확인했다.
- 장판의 3번째 틱에 처음 들어온 Enemy도 해당 틱에서 정전기가 발동하고, 같은 장판·같은 세대에는 이후 재발동하지 않는 순차 회귀 테스트를 추가했다.
- 다른 장판은 각각 한 번 발동하고 Pool에서 세대가 바뀐 Enemy는 새 생성 개체로 다시 한 번 발동하는 기존 규칙을 유지했다.

### 데미지 팝업과 공통 처치 흔들림

- `EnemyBase.ReceivePlayerAttack`과 `PlayerRoot.ReceiveDamage`에서 피격 전후 HP 차이를 계산해 실제 감소량만 팝업으로 표시한다. Enemy 일반 피해는 흰색, 치명타는 노란 강조색, Player 피격은 빨간색이다.
- Player와 8개 Enemy Prefab에는 TMP 대신 편집 가능한 빈 `DamagePopupAnchor`를 연결했다. 기본 높이는 Player `y=1.15`, 일반 Enemy `y=1.25`, Boss `y=1.8`이며 기획자가 Prefab에서 위치를 바꿀 수 있다.
- 실제 World Space TMP는 `CombatFeedbackController`가 16개 미리 준비하고 최대 64개까지 전역 Pool로 재사용한다. 수명이 끝나면 비활성화하며 Enemy가 사망·반환돼도 피격 Entity와 독립된 Feedback Root에서 표시된다. Entity별 TMP 방식은 동시 타격 숫자 덮어쓰기와 사망·비활성화 시 표시 절단 때문에 채택하지 않았다.
- 일반·치명타·Player 피격은 Bold 크기 `3.1/3.8/3.35`, 정렬 순서 `220` 이상을 사용한다. `0.82초` 동안 `0.9` 상승·Fade하고 순환 Stagger Offset으로 연속 숫자의 완전한 중첩을 피한다. 실제 감소량이 0 이하면 팝업을 생성하지 않는다.
- 팝업 Prefab이 누락되면 같은 경고를 반복하지 않고 한 번만 출력한다. `GameDataAssetBuilder`가 `DamagePopup.prefab`을 명시적으로 로드·검증해 `CombatFeedbackController`에 전달하도록 연결 경로를 고정했다.
- `CharacterAssetBuilder.MigrateDamagePopupAnchors`는 대상 9개 Prefab만 열어 Anchor가 없을 때 생성·연결한다. 이미 존재하는 Anchor와 사용자가 조정한 위치를 보존하므로 반복 실행해도 추가 변경이 없는 멱등 마이그레이션이다.
- 실제 저장된 Player·Enemy Prefab을 검증 복제본에서 다시 임포트한 뒤 Unity EditMode 전체 테스트 `410/410`이 통과했다. 실제 `DamagePopup.prefab` 재생 시 TMP 메시가 생성되고 카메라 Viewport 안에 놓이는 것과 Anchor 마이그레이션 재실행 결과 `0 prefab(s)`도 확인했다.
- 모든 Enemy 사망이 통과하는 `PrototypeGameSession.OnEnemyDefeated`에서 처치 흔들림을 호출하도록 통합했다. 개별 공격 피드백은 처치 시 흔들림을 넘겨 공통 경로와 중복되지 않는다.
- 이 공통 사망 경로를 통해 절단, 정전기, 참격, 오물 투척 처치도 처치 화면 흔들림을 받는다.

### 전투 효과와 VFX 편집

- 정전기 Arc의 GameObject·LineRenderer·Material을 적중마다 생성·파괴하던 구조를 비활성 인스턴스 Pool로 바꿨다. 재사용 시 위치·색·Alpha·수명 상태를 다시 설정하고 런타임 종료 시 Pool과 Material을 정리한다.
- `CharacterAssetBuilder`는 유효한 기존 `FilthProjectile.prefab`을 덮어쓰지 않는다. `Orb`, `DamageField/Outer`, `DamageField/Inner`의 Sprite·Material·Color·Scale을 직접 바꾸거나 자식 ParticleSystem/Animator를 추가할 수 있다.
- `orbRenderer`, `fieldVisual` 직렬화 참조는 비행 구체와 장판 상태 전환에 사용하므로 유지해야 한다.

### 문서화

- `Planning/ScriptStructure.md`를 추가해 Runtime·Editor·EditMode 전체 C# 파일의 폴더 트리와 스크립트별 역할을 정리했다.
- 기획서 26장, 기능 정의서 31장, 아키텍처 설계서 26장에 오물 최초 피격, 데미지 팝업, 처치 흔들림, 공용 Enemy 타입, Presentation 폴더, Pool과 오물 VFX 편집 규칙을 동기화했다.

### 추가·이동·정리된 주요 스크립트

- 추가: `Assets/Game/Runtime/Enemies/EnemyActor.cs`
- 추가: `Assets/Game/Runtime/Presentation/DamagePopupView.cs`
- 이동: `CameraShakeController.cs`, `CharacterSpriteAnimator.cs`, `CombatFeedbackController.cs` → `Assets/Game/Runtime/Presentation`
- 제거: `MeleeEnemy.cs`, `RangedEnemy.cs`, `ShieldEnemy.cs`, `BossEnemy.cs`
- 최적화: `Assets/Game/Runtime/Combat/SlashTrailEffect.cs`
- 회귀 테스트: `CombatResolverTests.cs`, `GameDataExcelImporterTests.cs`, `Phase3GameplayTests.cs`, `SlashTrailEffectTests.cs`

### 10차 검증 결과

- 메인 Unity 편집기가 프로젝트를 사용 중이어서 `tmp/ui-validation-20260801` 복제본에서 Character Asset과 Prototype Scene을 재생성하고 컴파일 오류가 없음을 확인했다.
- 첫 전체 검사에서 기존 `PauseDetailsPanel.prefab`의 계정 정보 정렬값이 Builder의 `TopRight` 규칙과 달라 1건이 실패했다. Builder가 오래된 프리팹도 재생성하도록 마이그레이션 조건을 보강하고 자산 정렬값을 맞췄다.
- 최종 Unity EditMode 전체 테스트 결과는 384개 통과, 실패 0개다. 데미지 팝업 숫자 형식·비활성 인스턴스 재사용, 오물 장판 3번째 틱 최초 진입, 정전기 Arc 인스턴스·Material 재사용, EnemyActor Prefab 종류 보존을 포함한다.
- 프리팹 보존 검토에서 Component 타입을 경로에서 직접 로드하던 부분을 `GameObject` 로드 후 `GetComponent`로 수정했다. `CharacterAssetBuilder.Build()` 실행 전후 `FilthProjectile.prefab`과 `DamagePopup.prefab`의 SHA-256이 각각 동일함을 확인해 수동 VFX가 덮어써지지 않는 것도 검증했다.

## 11차 밀집 전투·투사체·대형 스크립트 최적화

### Enemy 분리 Spatial Hash

- `EnemyWorldService`의 지상 Enemy 분리 후보 수집을 전체 O(N²) 순회에서 셀 크기 2m의 Uniform Spatial Hash로 교체했다. Enemy 등록·해제·이동 시 점유 셀을 갱신하고 분리 반경과 겹치는 버킷만 방문한다.
- 버킷 List와 분리 후보 버퍼를 재사용해 warm 분리 경로의 GC 할당을 0B로 유지했다.
- Unity Editor Mono 마이크로벤치마크의 800마리·전체 3 sweeps에서 sparse 간격 3 median은 1,141.060→5.194ms(-99.54%), pair check는 3,835,200→0회, bucket 방문은 10,800회였다. 각 sweep은 모든 Enemy의 `SeparateEnemy`를 호출하고 호출 내부는 2-pass이며 기존 check 산식은 `800×799×2×3`이다.
- dense 간격 0.55 median은 1,190.317→135.967ms(-88.58%), pair check는 3,835,200→144,111회(-96.24%), bucket 방문은 12,909회였다.
- 등록 메모리는 24,576→208,896B로 늘었다. 인덱스 상주 오버헤드는 184,320B(+750%, 약 230B/Enemy)이며 속도와 메모리의 명시적 tradeoff다.
- 위 수치는 Editor 마이크로벤치마크이고 실제 빌드 전체 프레임의 보편적 개선율이 아니다. Enemy 밀도와 한 셀에 모이는 후보 수에 따라 차이가 난다.

### 오물·이동 참격 투사체 Pool

- `ComponentPrefabPool<T>`를 추가하고 오물·이동 참격을 Prefab별 최대 16개의 비활성 인스턴스로 재사용한다. 비행·장판 시간, Alpha, 적중 Enemy·SpawnGeneration 기록을 대여 시 초기화한다.
- 1,000회 직렬 Spawn·완료 Editor Mono 벤치마크에서 오물은 23.807→2.221ms(-90.67%), 참격은 14.531→2.115ms(-85.44%)였다.
- 실행 중 생성/파괴는 1,000/1,000→1/0회로 줄고 런타임 종료 최종 정리만 1회 발생했다.
- 추적한 생성 할당 footprint는 오물 7,963B, 참격 2,601B로 기존 대비 약 99.9% 감소했다. 이는 반복 allocation traffic 수치이며 최대 16개의 resident Pool 메모리가 남으므로 게임 전체 메모리 절감률은 아니다.
- 통합 리뷰에서 씬 종료 후 fake-null 인스턴스가 비활성 Stack에 남는 경로와 실행 중 보관 상한을 낮췄을 때 기존 인스턴스가 즉시 정리되지 않는 경로를 확인했다. 씬 정리 시 Stack·파괴된 Prefab 키까지 제거하고 상한 변경 시 초과분을 즉시 정리하도록 보강했다.

### 대형 MonoBehaviour partial 분리

| 본체 | 분리 전 | 분리 후 | 추가 partial 책임 |
|---|---:|---:|---|
| `PlayerController.cs` | 1,766줄 | 862줄 | `ModeOne`, `AimVisuals` |
| `PlayerCombatAbilities.cs` | 1,163줄 | 594줄 | `Cards`, `Skills` |
| `FlyingSwordController.cs` | 1,028줄 | 207줄 | `Flight`, `Visuals` |
| `PrototypeHUDView.cs` | 1,526줄 | 415줄 | `ControlSettings`, `Localization`, `Panels` |
| `PrototypeGameSession.cs` | 952줄 | 251줄 | `CardSelection`, `Pause`, `RunFlow` |

- 모두 같은 partial 타입으로 분리해 기존 직렬화/API/MonoBehaviour/Update 수를 유지했다. 구조 분리 자체의 런타임 CPU·메모리 절감은 0%이며 코드 탐색과 변경 충돌 범위를 줄이는 목적이다.

### EnemyAssetCatalog 호환 보강

- 개별 Enemy 파생 Component를 `EnemyActor`로 교체할 때 기존 Catalog의 직접 Component 참조가 사라질 수 있어 `EnemyAssetEntry`가 Prefab 루트 `GameObject`를 함께 보관하도록 했다.
- 직접 `EnemyBase` 참조가 없으면 Prefab 루트에서 현재 Component를 다시 찾아 반환한다. Builder도 GameObject를 먼저 로드한 뒤 현재 EnemyBase를 해석한다.
- `EnemyAssetCatalogTests`에 직접 참조 우선, Component 교체 뒤 루트 fallback과 완전 누락 실패 회귀를 추가했다.

### 문서 동기화

- `ScriptStructure.md`의 실제 파일 트리와 partial·Pool·Catalog 테스트 역할을 갱신했다.
- 기획서 27장, 기능 정의서 32장, 아키텍처 설계서 27장에 알고리즘, 메모리 tradeoff, 측정 조건과 Editor 마이크로벤치마크 한계를 반영했다.

### 11차 최종 검증 결과

- Unity EditMode 전체 테스트는 Pool 상한 축소와 Catalog 3경로 회귀 테스트까지 포함해 399/399 통과, 실패 0개다.

## 12차 Lobby 진입 화면·난이도 표시 데이터

### 씬 흐름과 정적 UI

- 앱 첫 씬을 `Lobby`, 전투 씬을 `Battle`로 정의하고 Build Settings도 이 순서를 기준으로 관리한다.
- Lobby의 고정 화면은 런타임에서 조립하지 않고 `Assets/Prefab/UI/Lobby/LobbyScreen.prefab`에 저장한다. `Lobby.unity`에는 Main Camera, EventSystem과 이 프리팹 인스턴스를 둔다.
- 상단 `특성`, `도감`, `설정`은 후속 화면 구성이 확정될 때까지 비활성 Placeholder로 만들었다.
- `LobbyView`는 직렬화된 Button·Image·TMP 참조의 내용과 상태만 바꾸며, 입장 가능한 난이도가 선택되면 `Battle` 씬을 연다.

### 최초 미선택과 선택 복원

- PlayerPrefs 키가 없는 최초 Lobby는 난이도를 자동 선택하지 않는다. 세 버튼을 모두 회색으로 표시하고 대표 이미지를 숨기며 `입장하기`를 비활성화한다.
- 쉬움 또는 보통을 선택하면 해당 버튼을 녹색으로 바꾸고 마지막 선택을 저장한다. 다음 접속에는 유효한 마지막 선택을 복원한다.
- 저장값이 손상되었거나 현재 정의와 맞지 않으면 미선택 상태로 안전하게 돌아간다.
- 어려움은 버튼·문구·대표 이미지 연결만 준비한 UI-only 상태다. `GameDifficulty`와 Spawn 데이터에는 추가하지 않았고 선택·저장·입장을 비활성화했다.

### ImageData·LobbyDifficulty·GameString

- Excel에 `ImageData` 시트를 추가해 `Id`, `FileName`을 기록하고, Editor Importer가 `Assets/Image/<FileName>`을 Sprite로 해석해 `ImageDataTable.asset`에 직접 참조하도록 구성했다.
- `LobbyDifficulty`는 표시 순서, 사용 가능 여부, 실행 난이도 연결, 표시 시간, 적 수·레벨 보정 안내값, 이미지 ID와 문자열 Key를 관리하는 UI 메타데이터다.
- 상단 메뉴, 난이도명·버튼 보조 설명·목표·효과·입장 문구는 `GameString`으로 관리해 시트 수정과 재Import만으로 갱신할 수 있게 했다.
- 대표 이미지는 `Assets/Image`에서 관리하며 Read/Write와 Mipmap을 끄는 Texture Import 정책 대상으로 포함했다.

### Battle 연동과 fallback

- 정상 흐름은 `Lobby → Battle`이며, Battle 세션은 저장된 Lobby 난이도를 기존 `GameDifficulty.Easy/Normal`과 `SelectDifficulty` 흐름으로 연결한다.
- 개발 중 `Battle` 씬을 직접 열었는데 유효한 저장 난이도가 없으면 기존 인게임 난이도 모달을 fallback으로 유지한다.

### 알려진 불일치와 후속 작업

- 새 Lobby의 쉬움 UI는 기획대로 5분을 표시하지만 기존 `StageSpawnEasy`는 여전히 60웨이브·10분 데이터다. 현재 실제 쉬움 전투는 10분 일정으로 실행된다.
- 스폰 시간표와 보스 출현을 5분에 맞추는 작업은 별도 밸런스 변경으로 연기했다. 이번 작업의 `LobbyDifficulty.DurationMinutes=5`는 UI 표시 메타데이터이며 Stage 데이터 변경이 아니다.

### 문서 동기화

- `GameDesignDocument.md` 28장에 Lobby UX, 선택 저장, 이미지·문구 데이터와 5분/10분 불일치를 반영했다.
- `FunctionalSpecification.md` 33장에 정적 UI, 선택 복원, Battle 전달, ImageData와 LobbyDifficulty Import 기능을 정의했다.
- `ArchitectureDesignDocument.md` 28장에 `Lobby → Battle` 씬 경계, 정적 Prefab과 데이터 흐름을 기록하고 Prefab 폴더 정책을 기능별 하위 폴더 허용으로 갱신했다.
- `ScriptStructure.md`에 Lobby Runtime·Editor·테스트 파일과 각 책임, 자산 배치를 추가했다.

## 13차 Lobby 도감·공용 조작 UI

### 도감 창과 탭

- Lobby 상단 `도감`은 적 탭, `특성`은 특성 탭으로 같은 `LobbyCodexPanel.prefab`을 연다. `설정`은 계속 비활성 Placeholder로 유지했다.
- 적·조작·스킬·특성 콘텐츠는 모두 Prefab 안에 미리 만든 뒤 선택 탭 하나만 활성화한다. X와 창 바깥 터치는 도감 전체를 닫고 상세 Overlay 터치는 상세만 닫는다.
- 적과 스킬은 각각 정적 카드 9개를 한 페이지로 사용한다. 좌우 버튼은 페이지 경계에서 비활성화되며 빈 Slot은 입력과 표시를 함께 끈다.

### 적·스킬 상세와 GameString

- 적 도감은 현재 8개 `EnemyBalance` 정의에 `EnemyAssetCatalog` Sprite, 이름과 적별 GameString 설명을 결합했다. 카드 선택 시 확대 이미지·이름·설명을 표시한다.
- 스킬 도감은 참격, 관통, 절단, 이기어검, 정전기, 오물 투척과 융합 3종을 표시한다. 이기어검은 두 성장 카드를 하나의 도감 항목으로 통합했다.
- 스킬 이미지 자산은 아직 없으므로 카드와 상세 이미지를 의도적으로 비워 두었다. 추후 Sprite를 데이터에 연결할 수 있는 자리만 유지했다.
- Excel `GameString`에 도감 탭·페이지·특성 준비 문구, 통합 이기어검 문구와 적 8종 설명 등 16개 ID를 추가했다. 데이터 임포트 후 `GameStringTable.asset`에 반영했다.

### 공용 조작 Prefab과 특성 UI

- 기존 Battle 일시정지 조작 설정 계층을 `Assets/Prefab/UI/Shared/ControlSettingsPanel.prefab`으로 분리했다. `PauseDetailsPanel.prefab`과 Lobby 조작 탭이 같은 중첩 Prefab을 사용한다.
- 최초 공용화 과정에서 Builder 기본값으로 기존 사용자 수정 조작 UI를 덮어쓴 문제를 확인했다. `PauseDetailsPanel.prefab`을 21:21 커밋의 사용자 수정본으로 복구하고, 해당 계층 자체를 Unity `SaveAsPrefabAssetAndConnect`로 추출하는 방식으로 교체했다.
- 자동 Builder에서는 기존 `PauseDetailsPanel.prefab`과 `ControlSettingsPanel.prefab`의 스타일·계층 검사 후 재생성하는 경로를 제거했다. 자산이 이미 존재하면 보존하고, 공용 Prefab이 없을 때만 기존 Pause의 계층을 마이그레이션한다.
- Lobby에서는 적용 전 편집값을 초안으로 유지한다. 다른 도감 탭을 오가면 초안이 남지만 도감 전체를 적용 없이 닫으면 버리고, 다음 진입에는 마지막 적용값을 복원한다.
- 모드, 자동 공격, 좌우 크기와 패드 드래그, 기본값, 적용을 기존 저장소에 연결했다. 숨기기 모드에서는 미리보기 패드도 숨긴다.
- 특성 탭은 2×3 빈 Slot과 준비 중 GameString을 가진 UI-only 상태로 만들었다. 특성 데이터와 성장 효과는 추가하지 않았다.

### 검증

- 메인 작업 폴더의 Unity Editor를 중단하지 않고 격리 복제본에서 Prefab과 씬을 재생성했다.
- Unity EditMode 전체 테스트는 `420/420` 통과, 실패 0건이다.
- 1080×1920 렌더로 적 목록·상세·스킬·조작·특성 탭을 확인했다. 적 Sprite는 카드 안에서 읽히도록 별도 아이콘 배경과 확대 비율을 적용하고, 페이지 화살표의 폰트 미지원 문자를 `<`, `>`로 교체했다.
- 갱신한 Excel 산출물의 수식 오류 토큰은 0건이며 GameString 추가 행을 렌더로 확인했다.
- 복구 전 원본과 공용화 후 Prefab을 Unity에서 재귀 비교해 계층, 활성 상태, RectTransform, 이미지 색·Sprite, 텍스트, 버튼·토글·슬라이더 값이 모두 동일함(`CONTROL_PREFAB_EXACT_MATCH`)을 확인했다. 공용 참조와 Builder 비덮어쓰기 회귀 테스트도 각각 통과했다.

## 14차 Lobby 난이도 해제·도감 축소·설정 분리

### 난이도 선택 해제

- 현재 선택된 쉬움 또는 보통 버튼을 다시 누르면 현재 Lobby 선택을 해제한다. 모든 난이도 버튼을 회색으로 되돌리고 `DifficultyPreview` 전체와 `입장하기`를 비활성화한다.
- 선택 해제는 이번 화면의 선택만 없앤다. 마지막으로 저장한 난이도 ID는 유지하므로 다음 Lobby 진입에는 기존 규칙대로 마지막 난이도를 기본 선택한다.
- `LobbyScreen.prefab`의 `DifficultyPreview`도 기본 비활성으로 저장해 초기화 전 한 프레임 노출을 방지했다.

### 도감과 설정 화면 분리

- `LobbyCodexPanel.prefab`에서는 `조작`, `특성` 탭과 두 콘텐츠 계층을 제거하고 `적`, `스킬`만 유지했다.
- 상단 `특성` 버튼은 Lobby 바깥에 그대로 두되 도감과 연결하지 않는 비활성 Placeholder로 유지한다.
- 상단 `설정`은 새 `LobbySettingsPanel.prefab`을 연다. 기본 설정 화면의 `조작` 버튼은 Battle Pause와 동일하게 기본 화면과 공용 `ControlSettingsPanel.prefab` 화면을 전환한다.
- 조작 화면에서 `조작` 버튼으로 돌아가도 draft는 유지한다. 설정 창 전체를 적용 없이 닫으면 폐기하고, `적용`하면 저장한 뒤 기본 설정 화면으로 돌아간다.

### Lobby UI 수동 편집 보존

- `LobbySceneBuilder.Build()`는 `LobbyCodexPanel`, `LobbySettingsPanel`, `LobbyScreen`, `Lobby.unity`가 이미 존재하면 다시 생성하거나 저장하지 않는다. 앞으로 Unity에서 수정한 Lobby UI와 Scene Override를 Builder가 덮어쓰지 않는다.
- 이번 변경은 격리 복사본에서 기존 `LobbyCodexPanel`의 구형 네 노드만 제거하고 `LobbyScreen`에 새 설정 Prefab 인스턴스만 추가하는 대상 한정 마이그레이션으로 적용했다.
- 적용 전후 SHA-256 비교에서 `Lobby.unity`와 공용 `ControlSettingsPanel.prefab`은 동일했다. Lobby 테스트 13개 중 변경 관련 12개는 통과했고, 격리 복사본의 Generated Sprite 참조 1건만 환경 차이로 실패했다. 빌더 재실행 전후 Lobby UI·Scene 파일 동일성 테스트는 통과했다.

## 15차 Lobby 난이도 이미지 매핑 분리

- 이미지 변경이 반영되지 않은 원인은 저장된 `GameData_10min_Balance.xlsx`의 기존 `ImageData`가 대표 이미지 파일명을 유지했고, 생성된 `ImageDataTable.asset`도 재Import 전 직접 Sprite 참조를 계속 사용했기 때문이다.
- 기존 `LOBBY_DIFFICULTY_EASY/NORMAL/HARD -> LobbyDifficulty_Easy/Normal/Hard.png` 매핑은 올바른 대표 이미지 용도이므로 유지했다.
- `LOBBY_SELECTED_DIFFICULTY_EASY/NORMAL/HARD -> Easy_Text/Normal_Text/Hard_Text.png` 행을 `ImageData`에 추가했다.
- `LobbyDifficulty`에 `SelectedDifficultyImageId` 열을 추가해 대표 이미지와 선택 난이도 표시 이미지를 독립적으로 교체할 수 있게 했다.
- `DifficultyPreview`에는 `RepresentativeImage`와 별도의 `SelectedDifficultyImage`를 연결하고, 난이도 선택·해제 시 두 이미지를 함께 갱신·해제하도록 했다.
- 기존 Lobby UI를 다시 생성하지 않고 누락된 이미지 자식과 직렬화 참조만 추가하는 대상 한정 마이그레이션으로 구성했다.
