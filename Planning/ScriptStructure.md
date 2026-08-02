# SimpleGame 스크립트 구조와 책임

최종 갱신: 2026-08-02

이 문서는 `Assets/Game/Runtime`, `Assets/Game/Editor`, `Assets/Game/Tests/EditMode`의 C# 스크립트 배치와 책임을 설명하는 현재 기준 문서다. 런타임 동작은 `SimpleGame.Runtime`, 편집기 전용 생성·가져오기 도구는 `SimpleGame.Editor`, EditMode 검증은 `SimpleGame.Tests.EditMode` 어셈블리로 분리한다.

## 1. 폴더 구조

```text
Assets/Game/
├─ Runtime/
│  ├─ Combat/
│  │  ├─ CombatFeedbackResolver.cs
│  │  ├─ CombatGeometry.cs
│  │  ├─ CombatResolver.cs
│  │  ├─ FilthProjectile.cs
│  │  ├─ FlyingSwordController.cs
│  │  ├─ FlyingSwordController.Flight.cs
│  │  ├─ FlyingSwordController.Visuals.cs
│  │  ├─ MovingSlashProjectile.cs
│  │  └─ SlashTrailEffect.cs
│  ├─ Core/
│  │  ├─ CameraFollowController.cs
│  │  ├─ ComponentPrefabPool.cs
│  │  ├─ Direction2D.cs
│  │  ├─ EnemyWorldRecycler.cs
│  │  ├─ EnemyWorldService.cs
│  │  ├─ PauseDetailsData.cs
│  │  ├─ PlayerWorldArea.cs
│  │  ├─ PrototypeEnemyFactory.cs
│  │  ├─ PrototypeGameSession.cs
│  │  ├─ PrototypeGameSession.CardSelection.cs
│  │  ├─ PrototypeGameSession.Pause.cs
│  │  ├─ PrototypeGameSession.RunFlow.cs
│  │  ├─ PrototypeTypes.cs
│  │  ├─ WorldChunk.cs
│  │  └─ WorldChunkGrid.cs
│  ├─ Data/
│  │  ├─ CombatFeedbackProfile.cs
│  │  ├─ EnemyAssetCatalog.cs
│  │  ├─ EnemyBalanceTable.cs
│  │  ├─ GameDataManifest.cs
│  │  ├─ GameStringIds.cs
│  │  ├─ GameStringTable.cs
│  │  ├─ GlobalBalance.cs
│  │  ├─ LevelExperienceTable.cs
│  │  ├─ LevelUpCardTable.cs
│  │  ├─ PlayerBalanceTable.cs
│  │  ├─ SpawnPointRegistry.cs
│  │  ├─ StageSpawnController.cs
│  │  └─ StageSpawnSchedule.cs
│  ├─ Enemies/
│  │  ├─ BossAttackModule.cs
│  │  ├─ BossAttackPattern.cs
│  │  ├─ EnemyActor.cs
│  │  ├─ EnemyAttackModule.cs
│  │  ├─ EnemyBase.cs
│  │  ├─ EnemyFacing.cs
│  │  ├─ EnemyHealth.cs
│  │  ├─ EnemyHealthBar.cs
│  │  ├─ EnemyMovement.cs
│  │  └─ EnemyStateMachine.cs
│  ├─ Entities/
│  │  └─ HealthComponent.cs
│  ├─ Player/
│  │  ├─ CriticalSystem.cs
│  │  ├─ PlayerCombatAbilities.cs
│  │  ├─ PlayerCombatAbilities.Cards.cs
│  │  ├─ PlayerCombatAbilities.Skills.cs
│  │  ├─ PlayerController.cs
│  │  ├─ PlayerController.AimVisuals.cs
│  │  ├─ PlayerController.ModeOne.cs
│  │  ├─ PlayerHealthBar.cs
│  │  ├─ PlayerMovement.cs
│  │  ├─ PlayerProgression.cs
│  │  ├─ PlayerRoot.cs
│  │  └─ PlayerStats.cs
│  ├─ Presentation/
│  │  ├─ CameraShakeController.cs
│  │  ├─ CharacterSpriteAnimator.cs
│  │  ├─ CombatFeedbackController.cs
│  │  └─ DamagePopupView.cs
│  ├─ UI/
│  │  ├─ AimJoystickControl.cs
│  │  ├─ AttackCommandButton.cs
│  │  ├─ ControlLayoutDragSurface.cs
│  │  ├─ LevelUpCardView.cs
│  │  ├─ MobileControlSettings.cs
│  │  ├─ PrototypeHUDPresenter.cs
│  │  ├─ PrototypeHUDView.cs
│  │  ├─ PrototypeHUDView.ControlSettings.cs
│  │  ├─ PrototypeHUDView.Localization.cs
│  │  └─ PrototypeHUDView.Panels.cs
│  └─ World/
│     ├─ HealthPickup.cs
│     ├─ HealthPickupSpawner.cs
│     ├─ MushroomPoisonCloud.cs
│     └─ PoisonCloudSpawner.cs
├─ Editor/
│  ├─ CharacterAssetBuilder.cs
│  ├─ EditorAssetUtility.cs
│  ├─ GameDataAssetBuilder.cs
│  ├─ GameDataExcelImporter.cs
│  ├─ OpenXmlWorkbookReader.cs
│  ├─ PrototypeSceneBuilder.cs
│  └─ SourceTextureMemoryPostprocessor.cs
└─ Tests/EditMode/
   ├─ AssetMemoryPolicyTests.cs
   ├─ BossAttackPatternTests.cs
   ├─ CombatResolverTests.cs
   ├─ EnemyAssetCatalogTests.cs
   ├─ EnemyOverlapPolicyTests.cs
   ├─ EnemyPoolingTests.cs
   ├─ EnemyWorldServiceTests.cs
   ├─ FusionGameDataValidationTests.cs
   ├─ GameDataExcelImporterTests.cs
   ├─ GameDataTests.cs
   ├─ GameStringTableTests.cs
   ├─ GameStringUiTests.cs
   ├─ MobileAimControlsTests.cs
   ├─ PauseDetailsDataTests.cs
   ├─ Phase3GameplayTests.cs
   ├─ PlayerHealthBarTests.cs
   ├─ ProjectilePoolingTests.cs
   └─ SlashTrailEffectTests.cs
```

`MeleeEnemy`, `RangedEnemy`, `ShieldEnemy`, `BossEnemy`처럼 Archetype 값만 달랐던 빈 파생 클래스는 `EnemyActor` 하나로 통합한다. 프리팹이 직렬화한 `EnemyArchetype`과 `EnemyDefinition`이 종류를 결정하며, 실제 행동 차이는 `EnemyAttackModule`, `BossAttackModule` 같은 기능 컴포넌트와 데이터가 담당한다.

## 2. Runtime 스크립트 역할

### 2.1 Combat

| 스크립트 | 역할 |
|---|---|
| `CombatFeedbackResolver.cs` | 피해 적용·처치·치명타·정면 반동 결과에서 재생할 전투 피드백 우선순위를 순수 규칙으로 결정한다. |
| `CombatGeometry.cs` | 선분, 회전 사각형, 충돌 반경 등 공격 판정에 공통으로 쓰는 2D 기하 계산을 제공한다. |
| `CombatResolver.cs` | 공격 방향, 정면·후면, 방패 방어, 치명타와 최종 피해·반응을 계산하는 순수 전투 판정기다. |
| `FilthProjectile.cs` | Pool에서 대여한 오물 구체의 포물선 비행, 3초 장판, 0.5초 틱 피해와 장판별 Enemy·SpawnGeneration 최초 정전기 기록을 관리한다. |
| `FlyingSwordController.cs` | 이기어검의 직렬화 상태, 설정·레벨 API와 내부 슬롯 상태 모델을 소유하는 partial 본체다. 기존 1,028줄에서 207줄로 축소됐다. |
| `FlyingSwordController.Flight.cs` | 이기어검 발사, 비행 상태 갱신, 경로 관통·적중·재충전과 스폰 위치 선택을 담당한다. |
| `FlyingSwordController.Visuals.cs` | 이기어검 슬롯 시각 객체 구성, Prefab 참조 해석, 준비·공격 표시와 비활성·파괴 정리를 담당한다. |
| `MovingSlashProjectile.cs` | Pool에서 대여한 참격 투사체의 이동, 레벨별 크기·사거리·적중 상한, Enemy별 1회 피해와 종료 페이드를 처리한다. |
| `SlashTrailEffect.cs` | 절단 선분과 정전기 연결선을 표시하고 재사용 가능한 효과 Pool의 수명·Alpha를 관리한다. |

### 2.2 Core

| 스크립트 | 역할 |
|---|---|
| `CameraFollowController.cs` | 카메라가 Player를 부드럽게 추적하고 필요할 때 즉시 위치를 맞춘다. |
| `ComponentPrefabPool.cs` | Component Prefab 효과를 Prefab별 최대 16개의 비활성 인스턴스로 재사용하고 런타임 종료 시 정리하는 공용 Pool이다. |
| `Direction2D.cs` | 좌우 방향과 정면·후면 판정에 필요한 공통 2D 방향 보조 함수를 제공한다. |
| `EnemyWorldRecycler.cs` | Player 월드 재사용 경계를 벗어난 일반 Enemy를 반대편 Spawn 영역으로 재배치한다. |
| `EnemyWorldService.cs` | 살아 있는 Enemy 등록소이자 최근접·범위·경로·조준 보정·관통 검색과 2m Spatial Hash 기반 지상 Enemy 분리의 공용 Facade다. |
| `PauseDetailsData.cs` | Pause 화면의 Player 요약, 계정 요약, 스탯과 보유 카드 문자열을 구성하는 표시 모델이다. |
| `PlayerWorldArea.cs` | 카메라 기준 Spawn/Recycle 경계와 반대편·바깥쪽 재배치 위치를 계산한다. |
| `PrototypeEnemyFactory.cs` | Enemy Prefab을 생성·설정하고 Archetype/Prefab별 비활성 인스턴스를 제한된 Pool로 재사용한다. |
| `PrototypeGameSession.cs` | 세션 직렬화 상태, 데이터·HUD·월드 연결과 시작·Update·종료 수명주기를 소유하는 partial 본체다. 기존 952줄에서 251줄로 축소됐다. |
| `PrototypeGameSession.CardSelection.cs` | 카드 선택·리롤, 후보 발행, 융합 재료 소비와 선택 종료 흐름을 담당한다. |
| `PrototypeGameSession.Pause.cs` | Pause 전환과 Player·계정·스탯·보유 스킬 상세 문자열 구성을 담당한다. |
| `PrototypeGameSession.RunFlow.cs` | Enemy 처치, 전투 피드백, 난이도 선택, 이어하기, Player 사망·레벨업과 실행 흐름을 담당한다. |
| `PrototypeTypes.cs` | 공용 enum, 공격 결과 구조체, 성장 계산, Enemy Definition과 프로토타입 기본 데이터를 정의한다. |
| `WorldChunk.cs` | 단일 월드 청크 좌표와 월드 위치를 보관하고 재배치한다. |
| `WorldChunkGrid.cs` | 카메라 주변 3×3 청크를 재사용하며 무한 월드처럼 보이도록 중앙 좌표를 갱신한다. |

### 2.3 Data

| 스크립트 | 역할 |
|---|---|
| `CombatFeedbackProfile.cs` | 일반·처치·정면 반동·치명타 화면 흔들림의 강도와 지속 시간을 담는 수동 ScriptableObject다. |
| `EnemyAssetCatalog.cs` | Enemy ID별 Prefab 참조를 제공하고, Enemy Component 타입 교체 뒤 기존 참조가 비었을 때 직렬화된 Prefab 루트 `GameObject`에서 현재 `EnemyBase`를 다시 찾는 생성 에셋 카탈로그다. |
| `EnemyBalanceTable.cs` | Excel에서 가져온 Enemy Definition을 ID로 조회하는 ScriptableObject 테이블이다. |
| `GameDataManifest.cs` | Enemy·Player·레벨·카드·문자열·Spawn 등 생성 데이터 에셋의 단일 진입점이다. |
| `GameStringIds.cs` | 코드가 참조하는 `GameString` 식별자를 상수로 모아 오타와 하드코딩을 줄인다. |
| `GameStringTable.cs` | `StringId → KoKR` Dictionary를 지연 구성하고 문자열·format fallback을 제공한다. |
| `GlobalBalance.cs` | 계정 경험치 변환, 치명타, 리롤 등 전역 밸런스 값을 보관한다. |
| `LevelExperienceTable.cs` | Player/계정 레벨별 필요 경험치를 조회하는 공용 ScriptableObject 테이블이다. |
| `LevelUpCardTable.cs` | 카드 정의, 등급·선행·융합 재료·가중치 조건과 중복 없는 카드 추첨을 담당한다. |
| `PlayerBalanceTable.cs` | Player 기본 HP·공격력 성장·이동 속도·사거리·치명타 등 설정을 ID로 조회한다. |
| `SpawnPointRegistry.cs` | 씬의 Spawn Point를 ID로 등록하고 Spawn 데이터와 연결한다. |
| `StageSpawnController.cs` | 현재 난이도의 시간표를 따라 예정된 Enemy Spawn 항목을 순서대로 실행한다. |
| `StageSpawnSchedule.cs` | Stage/Wave/시간/위치/Enemy/레벨/난이도별 Spawn 원본 행을 저장하고 필터링한다. |

### 2.4 Enemies

| 스크립트 | 역할 |
|---|---|
| `BossAttackModule.cs` | Boss별 두 공격 패턴의 예고, 방향·위치 잠금, 판정과 순환 순서를 실행한다. |
| `BossAttackPattern.cs` | Boss 공격 모양과 크기, 중심·회전·포함 판정 및 Enemy ID별 패턴 데이터를 정의한다. |
| `EnemyActor.cs` | 모든 Enemy Prefab이 공유하는 `EnemyBase` 구현이며 직렬화된 Archetype을 반환한다. |
| `EnemyAttackModule.cs` | 일반 Enemy의 공격 예고, Windup/Active/Cooldown, Player 피해와 공격 애니메이션을 처리한다. |
| `EnemyBase.cs` | Definition, 레벨, 체력·이동·공격·표시 컴포넌트를 조율하고 피격·사망·Pool 세대를 관리하는 Enemy Facade다. Prefab의 빈 `DamagePopupAnchor` 월드 위치를 전역 팝업 시스템에 제공한다. |
| `EnemyFacing.cs` | 현재 바라보는 방향, 회전 지연과 공격 후 방향 잠금을 관리한다. |
| `EnemyHealth.cs` | float 기반 최대/현재 HP, 피해 적용, 사망과 Pool 재설정 이벤트를 관리한다. |
| `EnemyHealthBar.cs` | Enemy Health 변경을 World Space HP Bar와 레벨 라벨에 반영하고 표시 정책을 적용한다. |
| `EnemyMovement.cs` | 추적 이동, 정지와 넉백 이동을 Rigidbody2D에 적용한다. |
| `EnemyStateMachine.cs` | Enemy Archetype과 상태에 따라 추적·거리 유지·방패 대기·공격·Boss 패턴을 전환한다. |

### 2.5 Entities

| 스크립트 | 역할 |
|---|---|
| `HealthComponent.cs` | Player용 정수 HP, 피해 무적 시간, 회복·최대 HP 증가와 변경/사망 이벤트를 제공하며 `IPrototypeDamageTarget`을 정의한다. |

### 2.6 Player

| 스크립트 | 역할 |
|---|---|
| `CriticalSystem.cs` | 현재 치명타 확률을 제한하고 공격별 치명타 여부를 추첨한다. |
| `PlayerCombatAbilities.cs` | 직렬화된 스킬 상태와 일반·스킬 피해 실행을 소유하는 partial 본체다. 기존 1,163줄에서 594줄로 축소됐다. |
| `PlayerCombatAbilities.Cards.cs` | 카드 적용, 융합 생성·재료 초기화와 융합 Controller 정리를 담당한다. |
| `PlayerCombatAbilities.Skills.cs` | 참격·절단·오물 자동 발동, 스킬 수치 계산과 관통·방패 정책을 담당한다. |
| `PlayerController.cs` | 월드 Pointer 입력, 명령 발행·실행과 공통 조작 상태를 소유하는 partial 본체다. 기존 1,766줄에서 862줄로 축소됐다. |
| `PlayerController.AimVisuals.cs` | 조준선·끝점·확정 명령 마커와 조준 보정 대상 표시를 담당한다. |
| `PlayerController.ModeOne.cs` | 모드 1 이동 공격, 자동 조준·자동 공격, 교전 반경 선회와 이동 관통 상태를 담당한다. |
| `PlayerHealthBar.cs` | Player Prefab 아래 숫자 없는 상시 HP 비율 바를 `HealthComponent` 변경에 맞춰 갱신한다. |
| `PlayerMovement.cs` | 목적지 이동, 방향 입력 이동, 관통 경로 이동, 넉백과 입력 잠금을 실행한다. |
| `PlayerProgression.cs` | 인게임 경험치, 레벨과 다음 레벨 필요 경험치를 관리하고 레벨업 이벤트를 발행한다. |
| `PlayerRoot.cs` | Player의 체력·스탯·진행·이동·전투·컨트롤러를 구성하고 외부 시스템에 단일 API를 제공하는 Facade다. Prefab의 빈 `DamagePopupAnchor` 월드 위치를 전역 팝업 시스템에 제공한다. |
| `PlayerStats.cs` | 공격력·이동 속도·사거리·후면 배율 등 현재 스탯과 카드 강화 값을 계산·보관한다. |

### 2.7 Presentation

| 스크립트 | 역할 |
|---|---|
| `CameraShakeController.cs` | 요청된 강도·시간 중 현재보다 강한 카메라 흔들림을 재생하고 원래 위치로 복원한다. |
| `CharacterSpriteAnimator.cs` | Player/Enemy의 이동·공격·피격·방어·사망·좌우 방향을 Animator에 전달하며 중복 파라미터 쓰기와 불필요한 Update를 피한다. |
| `CombatFeedbackController.cs` | 전투 결과별 카메라 흔들림과 데미지 팝업을 한 곳에서 조율한다. Actor의 `DamagePopupAnchor` 위치에서 전역 팝업 Pool을 16개 Prewarm·최대 64개로 재사용하고, 연속 타격에는 순환 Stagger Offset을 적용하며 Prefab 누락 경고는 한 번만 출력한다. |
| `DamagePopupView.cs` | 실제 적용된 피해량을 World Space TMP로 표시한다. 일반·치명타·Player 피격에 Bold 크기 `3.1/3.8/3.35`와 각 색을 적용하고 정렬 순서 `220` 이상, 수명 `0.82초`, 상승 거리 `0.9`로 재생한 뒤 비활성화한다. |

### 2.8 UI

| 스크립트 | 역할 |
|---|---|
| `AimJoystickControl.cs` | Pointer 하나를 소유해 360도 조작 패드 입력을 정규화하고 Player 조준/모드 입력으로 전달한다. |
| `AttackCommandButton.cs` | 우측 공격 버튼의 PointerDown을 한 번의 공격 명령 Callback으로 전달한다. |
| `ControlLayoutDragSurface.cs` | 조작 편집 화면에서 좌·우 패드를 Safe Area 안에서 직접 끌어 pending 위치를 바꾼다. |
| `LevelUpCardView.cs` | 카드 이름·설명·등급 색상·리롤 상태를 표시하고 GameString을 적용한다. |
| `MobileControlSettings.cs` | 자동 공격, 모드, 좌·우 위치·크기의 기본값·Clamp·PlayerPrefs 저장과 Rect 변환을 담당한다. |
| `PrototypeHUDPresenter.cs` | HUD View의 버튼·표시와 GameSession/Player 동작을 바인딩한다. |
| `PrototypeHUDView.cs` | HUD 직렬화 참조, 초기화와 외부 표시 API를 소유하는 partial 본체다. 기존 1,526줄에서 415줄로 축소됐다. |
| `PrototypeHUDView.ControlSettings.cs` | 조작 설정 draft, 적용·폐기, 모드·자동 공격·크기·자유 배치 UI를 담당한다. |
| `PrototypeHUDView.Localization.cs` | GameString 기반 HUD·난이도·Pause·조작 문구 적용과 format fallback을 담당한다. |
| `PrototypeHUDView.Panels.cs` | 카드·난이도·Pause·GameOver 패널 참조 해석, 생성과 버튼 바인딩을 담당한다. |

### 2.9 World

| 스크립트 | 역할 |
|---|---|
| `HealthPickup.cs` | Player 접촉 시 최대 HP를 넘지 않는 회복을 적용하고 자신을 Spawner에 반환한다. |
| `HealthPickupSpawner.cs` | 플레이 중 20초 주기로 회복 오브젝트 위치·동시 상한·수명과 재사용을 관리한다. |
| `MushroomPoisonCloud.cs` | 버섯 보스 독 구름의 지속·0.5초 틱 Player 피해와 노출당 틱 수를 처리한다. |
| `PoisonCloudSpawner.cs` | 보스 공격이 예약한 위치에 독 구름을 지연 생성하고 Player 참조를 연결한다. |

## 3. Editor 스크립트 역할

| 스크립트 | 역할 |
|---|---|
| `CharacterAssetBuilder.cs` | 소스 Sprite에서 AnimationClip·AnimatorController·Player/Enemy·스킬·팝업 Prefab을 만들고 필수 Inspector 참조를 저장한다. Player `y=1.15`, 일반 Enemy `y=1.25`, Boss `y=1.8`의 빈 `DamagePopupAnchor`를 생성하며, 대상 9개 Prefab에 Anchor가 없을 때만 추가·연결하는 멱등 마이그레이션으로 기존 위치를 보존한다. 기존 오물 Prefab이 유효하면 수동 VFX 변경도 보존한다. |
| `EditorAssetUtility.cs` | Editor 생성 도구에서 폴더 생성 등 반복 에셋 작업을 공용화한다. |
| `GameDataAssetBuilder.cs` | Excel 모델을 런타임 ScriptableObject로 만들고 Manifest와 활성 씬에 연결·검증한다. `DamagePopup.prefab`도 명시적으로 로드·검증해 `CombatFeedbackController`에 연결한다. |
| `GameDataExcelImporter.cs` | Excel 각 시트를 강타입 모델로 파싱·교차 검증하고 생성 에셋 빌드를 시작한다. |
| `OpenXmlWorkbookReader.cs` | 외부 Excel 런타임 의존성 없이 `.xlsx` Open XML의 시트·행·셀을 읽는다. |
| `PrototypeSceneBuilder.cs` | Prototype Scene과 HUD/카드 UI Prefab을 생성·마이그레이션하고 시스템 참조를 연결한다. |
| `SourceTextureMemoryPostprocessor.cs` | SourceAssets Texture Import 설정이 프로젝트 메모리 정책을 따르도록 자동 보정한다. |

## 4. EditMode 테스트 스크립트 역할

| 스크립트 | 검증 범위 |
|---|---|
| `AssetMemoryPolicyTests.cs` | Source Texture 최대 크기·압축·읽기 설정 등 리소스 메모리 정책을 검증한다. |
| `BossAttackPatternTests.cs` | Boss 패턴 순서, 도형 중심·회전·범위와 보스별 설정을 검증한다. |
| `CombatResolverTests.cs` | 정면·후면, 방패, 치명타, 일격 처치와 피드백 우선순위, 데미지 팝업 숫자 형식·Pool 재사용을 검증한다. |
| `EnemyAssetCatalogTests.cs` | EnemyActor 교체 뒤 직접 Component 참조가 사라져도 Prefab 루트 fallback으로 현재 EnemyBase를 복구하는지 검증한다. |
| `EnemyOverlapPolicyTests.cs` | 비행/지상, 보스 등 Enemy별 겹침 허용과 분리 정책을 검증한다. |
| `EnemyPoolingTests.cs` | Factory의 Prefab별·전체 Pool 상한, 재사용과 SpawnGeneration 변경을 검증한다. |
| `EnemyWorldServiceTests.cs` | Enemy 등록, 범위·경로·조준·관통 검색과 충돌 반경·분리 규칙을 검증한다. |
| `FusionGameDataValidationTests.cs` | 융합 재료 ID·중복·자기 참조·MaxStack·등급 등 Import 규칙을 검증한다. |
| `GameDataExcelImporterTests.cs` | Excel 전체 파싱·생성 에셋·Prefab/Scene 직렬화 참조와 잘못된 데이터 거부를 검증한다. |
| `GameDataTests.cs` | 성장식, 카드 추첨·리롤·스킬 수치, Spawn과 난이도 등 데이터 기반 규칙을 검증한다. |
| `GameStringTableTests.cs` | 문자열 ID 조회, Dictionary 캐시, 누락·format 오류 fallback을 검증한다. |
| `GameStringUiTests.cs` | HUD·카드·난이도·Pause UI가 GameString을 실제 표시하는지 검증한다. |
| `MobileAimControlsTests.cs` | 조준 패드, 모드 1·2·숨기기, 자동 공격, pending 설정, 드래그·Safe Area와 관통 이동을 검증한다. |
| `PauseDetailsDataTests.cs` | Pause Player/계정/스탯/보유 카드 문구와 등급 정렬을 검증한다. |
| `Phase3GameplayTests.cs` | 스킬, 융합, 회복 오브젝트, 독 구름과 전투 연출의 수치·수명주기 회귀를 검증한다. |
| `PlayerHealthBarTests.cs` | Player Prefab HP Bar의 상시 표시, 비율 갱신과 숫자 텍스트 부재를 검증한다. |
| `ProjectilePoolingTests.cs` | 오물·참격 Prefab Pool의 최대 16개 상한, 직렬 재사용, 상태 초기화와 런타임 정리를 검증한다. |
| `SlashTrailEffectTests.cs` | 정전기 Arc가 비활성 인스턴스를 재사용하고 재사용 시 위치·색·수명을 초기화하는지 검증한다. |

## 5. 구조·최적화 적용 상태

### 적용됨

- Enemy 생성은 `PrototypeEnemyFactory`의 제한 Pool을 사용한다.
- 오물 장판은 자신이 소유한 `List<EnemyBase>`를 재사용하고 `FillEnemiesInRadius`의 비정렬·비할당 쿼리를 사용한다.
- `EnemyBase.CollisionRadius`는 `CircleCollider2D` 참조를 캐시한다.
- `CharacterSpriteAnimator`는 같은 Animator 값을 반복 기록하지 않고 Tint Pulse가 없는 인스턴스의 자체 Update를 끈다.
- Archetype 값만 달랐던 네 Enemy 태그 클래스를 `EnemyActor` 하나로 합치고, 연출 스크립트를 `Presentation`으로 분류한다.
- 데미지 팝업은 Actor Prefab의 빈 `DamagePopupAnchor`와 분리된 전역 World Space TMP를 16개 Prewarm·최대 64개의 제한 Pool로 재사용한다. Entity 소유 TMP는 동시 타격 덮어쓰기와 사망·Pool 반환 시 표시 절단 때문에 사용하지 않는다.
- 지상 Enemy 분리는 셀 크기 2m의 Uniform Spatial Hash와 재사용 후보 버퍼를 사용한다. 등록·이동 때 점유 셀을 갱신하고 분리 시 현재 반경과 겹치는 버킷만 방문한다.
- 오물·참격 투사체는 `ComponentPrefabPool<T>`를 공유하며 Prefab별 비활성 보관 상한은 16개다. 재사용 전 비행·타이머·Alpha·적중 이력을 초기화한다.
- `PlayerController`, `PlayerCombatAbilities`, `FlyingSwordController`, `PrototypeHUDView`, `PrototypeGameSession`은 기존 MonoBehaviour 하나와 기존 Update 수를 유지한 채 partial 책임 파일로 분리했다.
- `EnemyAssetCatalog`는 `EnemyActor` Component 교체로 오래된 Component 참조가 사라지는 경우를 위해 Prefab 루트 `GameObject` fallback을 함께 직렬화한다.

| partial 본체 | 분리 전 | 분리 후 본체 | 본체 줄 수 감소 |
|---|---:|---:|---:|
| `PlayerController.cs` | 1,766 | 862 | 51.2% |
| `PlayerCombatAbilities.cs` | 1,163 | 594 | 48.9% |
| `FlyingSwordController.cs` | 1,028 | 207 | 79.9% |
| `PrototypeHUDView.cs` | 1,526 | 415 | 72.8% |
| `PrototypeGameSession.cs` | 952 | 251 | 73.6% |

### Editor Mono 마이크로벤치마크 결과

| 대상과 조건 | 기존 | 변경 후 | 변화 |
|---|---:|---:|---:|
| Enemy 800, 간격 3, 전체 3 sweeps median | 1,141.060ms / pair check 3,835,200 | 5.194ms / pair check 0 + bucket 방문 10,800 | 시간 -99.54% |
| Enemy 800, 간격 0.55, 전체 3 sweeps median | 1,190.317ms / pair check 3,835,200 | 135.967ms / pair check 144,111 + bucket 방문 12,909 | 시간 -88.58%, pair check -96.24% |
| 오물 1,000회 직렬 Spawn·완료 | 23.807ms / 생성·파괴 1,000/1,000 | 2.221ms / 실행 중 생성·파괴 1/0, 최종 정리 1 | 시간 -90.67% |
| 참격 1,000회 직렬 Spawn·완료 | 14.531ms / 생성·파괴 1,000/1,000 | 2.115ms / 실행 중 생성·파괴 1/0, 최종 정리 1 | 시간 -85.44% |

- 각 sweep은 800마리 각각의 `SeparateEnemy`를 호출하고 호출 내부는 기존과 동일한 2-pass다. 기존 이론 pair check 3,835,200은 `800×799×2 pass×3 sweeps`다.
- 위 결과는 Unity Editor의 Mono 마이크로벤치마크다. 실제 빌드 전체 프레임 성능을 보장하는 수치가 아니며 Spatial Hash 개선 폭은 Enemy 밀도와 분포에 따라 달라진다.
- Spatial Hash 등록 메모리 벤치마크는 800마리에서 24,576B에서 208,896B로 증가했다. 인덱스 상주 오버헤드는 184,320B, +750%, 약 230B/Enemy이며 분리 warm 경로의 GC 할당은 0B다.
- 1,000회 직렬 실행에서 추적한 생성 할당 footprint는 오물 7,963B, 참격 2,601B로 기존 대비 약 99.9% 감소했다. 대신 비활성 인스턴스를 최대 16개 보관하므로 resident memory를 사용하며 프로젝트 전체 메모리 사용량이 99.9% 감소했다는 뜻은 아니다.
- partial 분리는 파일 책임과 변경 충돌을 줄이는 구조 개선이다. 컴파일 결과는 같은 타입·필드·MonoBehaviour·Update이므로 런타임 CPU와 메모리 절감은 0%다.

### 남은 측정 항목

- 일반 Enemy의 World Space Canvas·Slider·TMP 비용을 피격 시 제한 표시 또는 Sprite 기반 HP Bar로 줄이고 Animator Culling을 측정한다.
- 실제 빌드의 활성 Enemy 100/300/500마리에서 CPU Timeline, `GC.Alloc`, `Physics2D.Simulate`, `Animator.Update`, `Canvas.BuildBatch`의 median·p95를 별도로 기록한다.

`HealthComponent`와 `EnemyHealth`, `PlayerHealthBar`와 `EnemyHealthBar`는 이름은 비슷하지만 정수/실수 HP, 무적·회복, Pool 재설정, 표시 정책이 서로 달라 억지로 하나의 범용 클래스로 합치지 않는다. 공용화는 실제 동작과 수명주기가 같을 때만 진행한다.

## 6. 오물 투척 VFX 수정 방법

`Assets/Prefab/FilthProjectile.prefab`을 열면 구체와 장판 시각 요소를 직접 바꿀 수 있다.

- 구체: `Orb` 자식의 Sprite, Material, Color, Scale, Sorting 값을 수정한다.
- 장판: `DamageField/Outer`, `DamageField/Inner`의 Sprite, Color, Scale과 Material을 수정한다.
- 더 복잡한 표현은 해당 자식 아래에 `ParticleSystem`이나 `Animator`를 추가한다.
- `FilthProjectile`의 `orbRenderer`, `fieldVisual` 직렬화 참조는 유지해야 비행 종료 시 구체 숨김과 장판 표시가 정상 동작한다.
- `SimpleGame > Build Character Assets`는 유효한 기존 오물 Prefab을 보존한다. 기본 형태로 다시 만들려면 기존 Prefab을 별도로 백업한 뒤 재생성 정책에 맞춰 작업한다.

오물·정전기 융합은 장판 생성 시 Enemy 전체를 한 번 기록하는 방식이 아니다. 각 0.5초 틱마다 현재 반경 안 대상을 찾고, 그 장판에서 동일한 `Enemy + SpawnGeneration`이 처음 실제 피해를 받는 틱에 정전기를 한 번 발동한다. 따라서 3번째 틱에 처음 장판에 들어온 몬스터는 3번째 틱에서 정전기가 발동하고, 같은 장판에 재진입해도 같은 세대에는 다시 발동하지 않는다. 다른 장판과 Pool 재사용 후 새 SpawnGeneration은 각각 새로운 최초 피격으로 판정한다.
