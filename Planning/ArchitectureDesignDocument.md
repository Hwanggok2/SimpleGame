# SimpleGame 프로젝트 아키텍처 설계서

- 최종 갱신: 2026-08-02

## 1. 문서 목적

이 문서는 `SimpleGame`의 게임 스크립트를 구현할 때 사용할 클래스 구조, 디자인 패턴, 의존 방향과 UI 바인딩 규칙을 정의한다.

핵심 목표는 다음과 같다.

- 공통 로직을 재사용하여 유사한 스크립트의 중복 생성을 방지한다.
- 클래스와 컴포넌트의 역할을 명확하게 구분한다.
- 부모 클래스가 모든 기능을 직접 구현하는 God Object가 되지 않도록 한다.
- Enemy 종류나 공격 방식이 추가돼도 기존 코드를 대규모로 수정하지 않는다.
- GameObject, UI와 데이터의 연결 관계를 추적할 수 있게 만든다.
- 전투 판정처럼 중요한 규칙을 Unity 실행 환경과 분리하여 테스트할 수 있게 한다.

게임 규칙은 [GameDesignDocument.md](GameDesignDocument.md)를 기준으로 한다.

### 1.1 최종 출시 대상과 플랫폼 경계

최종 제품은 앱인토스에서 실행되는 Unity WebGL 게임이다. Unity Editor와 일반 브라우저는 개발 환경으로만 취급한다.

- Unity 버전은 현재 Unity 6을 유지한다.
- 배포는 앱인토스 공식 Unity SDK의 WebGL 빌드와 `.ait` 패키징 흐름을 사용한다.
- SDK 버전은 설치 시점의 검증된 릴리스 태그로 고정한다.
- 게임 규칙, 전투, 이동과 상태 머신은 앱인토스 SDK를 참조하지 않는다.
- 광고, 가시성, 계정·기기 정보와 공유처럼 호스트 기능이 필요한 코드만 플랫폼 경계에서 SDK를 호출한다.
- SDK API는 `async/await` 기반으로 호출하고 Unity 메인 스레드를 동기 대기로 막지 않는다.
- WebGL 전용 조건부 컴파일은 플랫폼 경계 내부에만 둔다.
- 화면이 가려지거나 백그라운드로 전환되면 게임 시간과 오디오를 정지하고, 이벤트 구독은 비활성화 시 반드시 해제한다.
- 보상형 광고 보상은 광고 표시 성공이 아니라 `userEarnedReward` 이벤트를 받은 뒤 한 번만 지급한다.
- 매 프레임 할당, 불필요한 `Resources` 상주, 런타임 대량 생성과 WebGL에서 지원되지 않는 스레드·파일 시스템 의존을 피한다.

현재 프로젝트에는 앱인토스 Unity SDK가 설치되어 있지 않다. 앱 ID, 아이콘 URL과 광고 그룹 ID가 준비되면 공식 SDK를 설치하고 플랫폼 경계를 구현한다. 그 전에는 `SimulateRewardedContinue`처럼 이름에 테스트 용도가 드러나는 로컬 대체 흐름만 사용한다.

공식 기준:

- [Unity SDK 연동](https://developers-apps-in-toss.toss.im/unity/sdk/getting-started.html)
- [빌드 프로필](https://developers-apps-in-toss.toss.im/unity/sdk/build-profiles.html)
- [화면 가시성 처리](https://developers-apps-in-toss.toss.im/unity/sdk/visibility.html)
- [Unity 인앱 광고](https://developers-apps-in-toss.toss.im/unity/porting-tutorials/iaa.html)

## 2. 채택할 기본 구조

프로젝트의 기본 아키텍처는 다음 패턴을 조합한다.

> 얕은 상속 + 컴포넌트 조합 + 부모 Facade/Coordinator + 상태 머신 + 제한적인 Observer + 데이터 기반 설정

각 패턴의 역할은 다음과 같다.

| 패턴 | 프로젝트에서의 역할 |
|---|---|
| 얕은 상속 | Enemy, Attack처럼 명확한 공통 계약과 생명주기 공유 |
| 컴포넌트 조합 | 이동, 체력, 타깃, 공격처럼 독립적인 기능 재사용 |
| Facade/Coordinator | 기능 컴포넌트 참조, 초기화, 외부 API와 생명주기 관리 |
| 상태 머신 | Enemy와 Boss의 행동 순서 및 상태 전환 관리 |
| Observer | HP, 사망, 점수, 레벨처럼 외부에 알려야 하는 상태 변경 |
| Strategy | 근거리, 원거리, 보스처럼 교체 가능한 공격 알고리즘 |
| ScriptableObject | Prefab, 연출 등 프로젝트 에셋 참조가 필요한 읽기 전용 설정 |
| Factory/Object Pool | Enemy, 투사체와 VFX의 생성 및 재사용 |
| Presenter/View | 게임 상태와 UI 컴포넌트 분리 |
| enum UI Registry | UI 이름 규칙을 이용한 Button 및 TMP_Text 자동 바인딩 |

정식 MVC 프레임워크와 UniRx는 초기 구조에 도입하지 않는다.

## 3. 설계 원칙

### 3.1 상속은 `is-a` 관계에 사용한다

올바른 관계:

```text
RangedEnemy is an Enemy
RangedAttack is an EnemyAttack
BossEnemy is an Enemy
```

잘못된 관계:

```text
RangedAttack is an Enemy
EnemyHealth is an Enemy
```

따라서 클래스는 다음처럼 구성한다.

```text
EnemyBase
├─ MeleeEnemy
├─ RangedEnemy
├─ ShieldEnemy
└─ BossEnemy

EnemyAttackBase
├─ MeleeAttack
├─ RangedAttack
└─ BossAttack
```

### 3.2 조합은 `has-a` 관계에 사용한다

```text
Enemy has EnemyHealth
Enemy has EnemyMovement
Enemy has EnemyTargeting
Enemy has EnemyAttack
Enemy has EnemyStateMachine
```

### 3.3 단순 수치 차이는 데이터로 처리한다

다음 차이만 존재한다면 새로운 클래스를 만들지 않는다.

- 이동속도
- 공격력
- 공격 사거리
- 공격 쿨타임
- 레벨
- 점수
- 경험치
- Sprite 또는 Animator

같은 `RangedEnemy`와 `RangedAttack`을 사용하고 `EnemyDefinition` 데이터만 다르게 설정한다.

### 3.4 실제 알고리즘이 달라질 때만 기능을 분리한다

새로운 기능 클래스가 필요한 예:

- 일반 투사체 공격
- 독 상태를 적용하는 투사체 공격
- 범위 공격
- 돌진 공격
- 보스의 경고 영역 공격

단순히 공격력이나 색상만 다르면 클래스를 추가하지 않는다.

### 3.5 상속 깊이는 얕게 유지한다

권장:

```text
EnemyBase
└─ RangedEnemy
```

피해야 할 구조:

```text
EnemyBase
└─ GroundEnemy
   └─ RangedEnemy
      └─ EliteRangedEnemy
         └─ PoisonEliteRangedEnemy
```

Enemy 종류 아래에서 다시 변하는 공격 방식은 상속 트리를 늘리지 않고 컴포넌트와 데이터로 처리한다.

## 4. 전체 시스템 구조

```text
GameBootstrap
├─ GameSession
├─ InputReader
├─ WaveSpawner
├─ EnemyFactory
├─ PoolService
├─ ScoreSystem
├─ ExperienceSystem
├─ AccountProgression
├─ SaveService
└─ UIFlowCoordinator

World
├─ Player
│  └─ PlayerRoot 및 기능 컴포넌트
├─ WorldGrid
│  └─ 3×3 WorldChunk/Tilemap 인스턴스
└─ EnemyRoot
   └─ 청크와 독립된 EnemyBase 인스턴스

UI
├─ HUDView
├─ LevelUpView
├─ ContinueView
├─ GameOverView
└─ ClearView

Presentation
├─ CameraFollowController
├─ CombatFeedbackController
└─ CameraShakeController
```

### 4.1 의존 방향

```text
UI View
  ↑
Presenter
  ↑
GameSession 및 게임 시스템
  ↑
Entity Facade
  ↑
기능 컴포넌트
  ↑
순수 계산 로직 및 데이터
```

하위 계층은 자신을 사용하는 상위 계층을 직접 참조하지 않는다.

예:

- `EnemyHealth`는 `ScoreSystem`을 모른다.
- `ScoreSystem`이 Enemy 사망 이벤트를 구독한다.
- `HUDView`는 `GameSession`을 직접 조작하지 않는다.
- `HUDPresenter`가 View의 입력을 받아 `GameSession`에 전달한다.

## 5. Entity Facade/Coordinator

### 5.1 부모의 역할

`EnemyBase`, `PlayerRoot`는 해당 Entity의 Facade이자 Coordinator다.

부모가 담당한다.

- 필수 기능 컴포넌트 참조 보유
- 생성 시 초기화 순서 관리
- 외부 시스템에 공통 API 제공
- 기능 컴포넌트 연결
- 활성화 및 비활성화 생명주기 관리
- Object Pool 반환 준비
- 필수 참조 검증

부모가 직접 담당하지 않는다.

- 경로 이동 계산
- 피해량 계산
- 투사체 이동
- 애니메이션 세부 재생
- UI 텍스트 갱신
- 점수와 경험치 직접 증가
- 모든 상태의 세부 행동

### 5.2 기능 컴포넌트 위치

논리 컴포넌트는 가급적 Entity의 루트 GameObject에 같이 부착한다.

```text
Enemy
├─ EnemyBase
├─ EnemyHealth
├─ EnemyMovement
├─ EnemyTargeting
├─ EnemyFacing
├─ EnemyAttackBase 파생 컴포넌트
└─ EnemyStateMachine
```

별도 Transform이 필요한 요소만 자식 GameObject로 둔다.

```text
Enemy
├─ VisualRoot
├─ AttackOrigin
├─ Hitbox
└─ UIAnchor
```

기능을 분리한다는 이유만으로 빈 자식 GameObject를 만들지 않는다.

## 6. Enemy 클래스 구조

### 6.1 EnemyBase

`EnemyBase`는 모든 Enemy가 공유하는 외부 계약과 초기화 흐름을 정의한다.

예상 책임:

- `EnemyDefinition`과 런타임 레벨 수신
- 기능 컴포넌트 초기화
- 외부 공격 요청 수신
- 활성화 및 Pool 반환
- 상태 머신 시작

개념적인 형태:

```csharp
public abstract class EnemyBase : MonoBehaviour
{
    protected EnemyHealth Health;
    protected EnemyMovement Movement;
    protected EnemyTargeting Targeting;
    protected EnemyFacing Facing;
    protected EnemyAttackBase Attack;
    protected EnemyStateMachine StateMachine;

    public virtual void Initialize(
        EnemyDefinition definition,
        int level,
        EnemyServices services)
    {
        // 컴포넌트별 초기화 순서를 관리한다.
    }

    public void ReceiveAttack(AttackContext context)
    {
        // Facing과 CombatResolver를 이용해 결과를 각 담당 기능에 전달한다.
    }
}
```

이 코드는 구조를 설명하기 위한 예시이며 실제 구현 시 필요한 최소 API만 작성한다.

### 6.2 Enemy 공용 구현과 Archetype

현재 모든 Enemy Prefab은 `EnemyBase`의 공용 구현인 `EnemyActor`를 사용한다. `EnemyActor`는 직렬화된 `EnemyArchetype`만 제공하며, 종류별 실제 규칙은 `EnemyDefinition`, `EnemyStateMachine`과 공격 기능 컴포넌트가 결정한다.

| Archetype | 데이터·조합으로 달라지는 핵심 규칙 |
|---|---|
| `Melee` | 근거리 레벨 차이 공격 규칙과 근접 행동 |
| `Ranged` | 발사 전 자유 조준, 발사 후 방향 고정과 쿨타임 |
| `Shield` | Player 추격, 하늘색 범위 끝 정지, Shield 방향 고정, 조건부 정면 반동 |
| `Boss` | Player 목표 유지, 보스 패턴 순환, 일반 Enemy 재배치 제외 |

알고리즘을 추가하지 않고 Archetype 값만 반환하던 네 파생 클래스는 제거한다. 새 종류가 기존 흐름을 공유하면 Definition과 Prefab 조합을 추가하고, 실제로 다른 알고리즘이 필요할 때만 기능 컴포넌트를 추가한다.

### 6.3 Enemy 기능 컴포넌트

| 컴포넌트 | 책임 |
|---|---|
| `EnemyHealth` | float 기반 현재·최대 체력, 피해, 사망과 Pool 재설정 이벤트 |
| `EnemyMovement` | 목표 위치 이동, 정지와 넉백 |
| `EnemyFacing` | 바라보는 방향, 좌우 전환 지연과 공격 후 방향 잠금 |
| `EnemyAttackModule` | 일반 Enemy의 준비, 예고, 판정과 쿨타임 |
| `BossAttackModule` | Boss별 두 패턴의 예고, 위치·방향 잠금과 판정 |
| `EnemyStateMachine` | Archetype별 추적·거리 유지·방패 대기·공격 상태 전환 |
| `EnemyHealthBar` | 체력 비율과 레벨 라벨 표시 |
| `CharacterSpriteAnimator` | 저장된 AnimatorController에 Motion·FaceLeft·Attack·Hurt·Death 상태 전달 |

### 6.4 공격 모듈

```text
EnemyBase
├─ EnemyAttackModule  ── 일반 Melee/Ranged/Shield 공격 시간표
└─ BossAttackModule   ── BossAttackPatterns 기반 두 패턴 순환
```

현재 일반 Enemy는 하나의 `EnemyAttackModule`이 Definition의 사거리·준비·판정·쿨타임과 Archetype 조건을 사용한다. Boss는 도형과 순환 방식이 달라 `BossAttackModule`로 분리한다. 공격 종류가 늘어나 현재 Module의 조건문이 독립 알고리즘 여러 개로 커질 때 Strategy 분리를 다시 검토한다.

### 6.5 CombatResolver

정면, 후면, 레벨별 공격력·최대 HP, 일격 처치 예외와 치명타 판정은 Unity 컴포넌트와 분리된 순수 C# 클래스로 구현한다.

입력:

- `EnemyDefinition`
- 플레이어 레벨
- Enemy 레벨
- Player 공격력
- 후면 공격 배율
- 정면 또는 후면
- 치명타 여부

출력:

- 적용 피해량
- Enemy 최대 HP
- Player 반동 유형: 없음 또는 방패 반동

방어도와 일반 Enemy 정면 피해 면역은 사용하지 않는다. `EnemyDefinition.CalculateMaxHealth()`와 `PlayerStats.GetAttackPower()`가 같은 성장 배율을 사용하고, `CombatResolver`는 정면 1배·후면 3배·치명타 3배를 조합한다. 낮은 레벨 Enemy의 방향 무관 일격 처치는 `OneHitPlayerLevelAdvantage` 데이터로 별도 처리한다. 위험도 색상은 최대 HP를 비치명타 피해로 나눈 실제 필요 타수를 사용한다.

## 7. Enemy 상태 머신

일반 Enemy의 기본 상태:

```text
Spawn
  ↓
ChasePlayer
  ├─ Player가 사거리 안 → AttackTelegraph
  ├─ 재배치 경계 통과 → Reposition
  └─ 사망 → Dead

AttackTelegraph
  ├─ 실제 공격 판정 → AttackRecovery
  ├─ Player 사망 → Hold
  └─ 사망 → Dead

AttackRecovery
  ├─ 방향 고정 종료 → ChasePlayer
  └─ 사망 → Dead
```

방패병은 공격 상태 대신 다음 흐름을 사용한다.

```text
Spawn
  ↓
ChasePlayer
  ├─ Player가 하늘색 범위 끝에 도달 → HoldApproachBoundary
  ├─ Player 사망 → Hold
  └─ 사망 → Dead

HoldApproachBoundary
  ├─ Player가 하늘색 범위 밖으로 이동 → ChasePlayer
  ├─ Player가 반대편에 0.8초 유지 → ApplyPendingFacing
  ├─ Player 사망 → Hold
  └─ 사망 → Dead
```

하늘색 범위는 방패병 중심의 접근 판정 범위다. `EnemyMovement`는 Player와 방패병 사이의 거리가 이 범위의 반경에 도달하면 이동을 정지하며, Player가 멀어지면 다시 이동한다.

보스의 공격 상태:

```text
ChasePlayer
  ↓
AttackTelegraph 1.5초
  ↓
AttackActive 0.5초
  ↓
AttackRecovery 1초
  ↓
ChasePlayer
```

초기 구현은 명시적인 enum 상태와 전환으로 시작한다. 상태마다 독립 데이터와 로직이 커질 때만 상태별 클래스로 분리한다.

## 8. Player 구조

```text
Player
├─ PlayerRoot
├─ PlayerHealth
├─ PlayerMovement
├─ PlayerCombat
├─ PlayerTargetSelector
├─ PlayerLevel
├─ CriticalSystem
├─ PlayerStateMachine
│
├─ VisualRoot
├─ AttackOrigin
└─ UIAnchor
```

### 8.1 주요 책임

| 컴포넌트 | 책임 |
|---|---|
| `PlayerRoot` | 외부 API, 초기화, 기능 연결 |
| `PlayerMovement` | 터치 위치 및 공격 위치 이동 |
| `PlayerCombat` | 공격 요청과 `CombatResolver` 연결 |
| `PlayerTargetSelector` | 터치 보정, 우선순위와 모든 Enemy의 Collider 기반 경로 가로채기 검사 |
| `PlayerHealth` | 피격, 사망, 부활 |
| `PlayerLevel` | 인게임 경험치와 레벨 |
| `PlayerStats` | PlayerDefinition 기반 공격력·사거리·이동 강화 단계 계산 |
| `CriticalSystem` | 치명타 확률과 Roll |
| `PlayerStateMachine` | 이동, 공격, 조작 불가, 사망 상태 |
| `CharacterSpriteAnimator` | Player/Enemy 공용 Animator 파라미터 Adapter. Motion·방향·Attack·Hurt·Death 상태를 전달하며 Sprite 프레임이나 Controller를 런타임 생성하지 않음 |

### 8.2 공격 위치 계산

`PlayerCombat`은 공격 실행 전에 `PlayerMovement`에 공격 위치 계산을 요청한다.

```text
Enemy가 회색 공격 사거리 밖
  → Enemy가 Player 공격 사거리 끝에 위치하는 지점 계산
  → PlayerMovement.MoveTo(공격 위치)
  → 도착 후 공격

Enemy가 회색 공격 사거리 안
  → Player 이동 생략
  → 현재 위치에서 즉시 공격
```

두 Collider가 겹쳐 있어도 Enemy가 회색 공격 사거리 안이라면 분리 이동이나 위치 보정을 수행하지 않는다. 공격 위치 계산은 거리 조건만 결정하고 물리 충돌 해결을 대신하지 않는다.

`PlayerMovement`는 목적지 도달 시간을 저장하지 않고 `Vector2.MoveTowards`로 `현재 이동 속도 × 상태 배율 × Time.deltaTime`만큼 이동한다. 기본 속도는 `PlayerBalance.BaseMoveSpeed = 10`이다. 시작 시 `SmoothDamp`로 현재 속도를 순항 속도까지 올리고, 목적지 인근의 제동 거리는 현재 순항 속도에 비례해 계산한 뒤 `SmootherStep`으로 감속한다. `MoveArrivalTolerance = 0.08` 안에서는 도착 처리한다.

`PlayerController`는 이동 상황에 따라 배율만 조정한다. 빈 공간 이동은 1.0배, 터치 경로의 첫 Enemy 접근은 1.1배, 해당 Enemy를 실제 처치한 뒤 남은 경로는 1.2배다. 여러 Enemy를 처치해도 1.2배를 유지하며 배율을 서로 곱하지 않는다. 일격 처치 후 다음 Enemy로 이어질 때는 현재 운동량을 유지하고, 생존 Enemy를 만나거나 목적지에 도착하면 운동량을 초기화한다. 새 입력·넉백·사망·목적지 도착 시 처치 후 이탈 상태를 해제하며 넉백 시간은 이 속도 규칙과 분리한다.

`PlayerController`는 Enemy 클릭마다 공격 요청을 한 개 생성하고 Player 공격 쿨타임을 두지 않는다. 같은 Enemy에게 접근하는 동안 들어온 추가 클릭은 이동 시작 시간을 초기화하지 않고 요청 수만 누적한다. 공격 위치에 도착하면 누적된 요청을 각각 독립된 공격으로 처리한다. Enemy가 이미 회색 공격 사거리 안이라면 이동 명령 없이 해당 클릭의 공격을 즉시 처리한다.

`PlayerMovement`와 `EnemyMovement`는 이동 상태와 방향을 `CharacterSpriteAnimator`에 전달한다. 공격 모듈은 실제 공격 판정 시점에 Attack Trigger, 체력 모듈을 조정하는 Facade는 실제 피해 적용 시점에 Hurt Trigger를 전달한다. `CharacterSpriteAnimator`는 Sprite 배열을 로드하거나 프레임을 직접 교체하지 않고 Prefab에 직렬화된 Unity `Animator`의 파라미터만 변경한다.

방패병도 별도 애니메이터 클래스를 만들지 않고 같은 Adapter를 사용한다. `EnemyStateMachine`은 하늘색 범위 밖에서 Motion=Move(Walk), 범위 안에서 Motion=Guard(Shield)를 요청한다. Attack/Hurt 상태는 AnimatorController의 Exit Time 이후 현재 Motion 값에 맞는 Idle·Move·Guard 상태로 복귀한다.

Player와 Enemy의 Animation·Animator 자산은 `Assets/Game/Characters` 아래에서 관리한다. 프로젝트의 모든 Prefab은 `Assets/Prefab`에 모으고 원본 Sprite Sheet만 `Assets/SourceAssets`에 유지한다.

```text
Assets/Game/Characters/
├─ Animations/
│  ├─ Player/       # Player Clip 10종
│  ├─ Common/       # Enemy 방향별 Facing Clip
│  ├─ Goblin/
│  └─ Skeleton/
├─ Animators/       # Player, Goblin, Skeleton Controller
└─ Shared/          # 직렬화 가능한 PrototypeSquare Sprite

Assets/Prefab/      # Player, Enemy, UI, Map, Effect 등 모든 Prefab

Assets/SourceAssets/
├─ Bandits - Pixel Art/Sprites/LightBandit.png
└─ Monsters Creatures Fantasy/Sprites/      # Goblin, Skeleton 원본
```

`PrototypeEnemyFactory`는 런타임에 GameObject와 컴포넌트를 조립하지 않고 직렬화된 Enemy Prefab별 지연 생성 Pool을 사용한다. 같은 Source Prefab의 비활성 인스턴스를 우선 재사용하고 Pool이 비었을 때만 Instantiate하며, 사망 애니메이션 뒤 상태를 초기화해 반환한다. 각 Prefab은 고정된 컴포넌트, SpriteRenderer, AnimatorController 참조를 Inspector에서 확인할 수 있어야 한다.

Player와 Enemy Prefab의 `Rigidbody2D`, `Collider2D`, Animator, 공격 범위, 공격 예고, 방향 마커와 레벨 라벨은 모두 Prefab에 직렬화한다. 모든 Enemy Prefab에는 `EnemyHealthBar`, World Space Canvas, Slider와 현재/최대 HP 라벨도 직렬화한다. `CameraFollowController`, `CameraShakeController`, `PlayerWorldArea`, `EnemyWorldRecycler`, `WorldChunkGrid`와 9개 Tilemap 청크도 Scene에 미리 저장한다. 런타임 코드는 `AddComponent`나 고정 자식 GameObject 생성을 수행하지 않고 저장된 참조의 값과 활성 상태만 변경한다.

LightBandit 원본 Sprite는 X Scale `+1`에서 왼쪽을 향하고 Goblin·Skeleton 원본 Sprite는 X Scale `+1`에서 오른쪽을 향한다. 따라서 동일한 `FaceLeft` 파라미터를 사용하되 Player Facing Clip은 Left `+1`/Right `-1`, Enemy Facing Clip은 Left `-1`/Right `+1`로 분리한다. 이 차이는 AnimationClip과 AnimatorController 자산에서 해결하며 런타임 방향 반전 코드를 추가하지 않는다.

### 8.3 Player 공격 반동

`PlayerController`는 `CombatResolver` 결과를 적용한 뒤 Player 반동 조건을 확인한다. 현재 반동은 일격 처치 예외가 적용되지 않는 `ShieldEnemy`를 정면에서 비치명타로 공격한 경우에만 발생한다.

반동 조건이면 다음 순서를 직접 호출한다.

```text
PlayerCombat
  → PlayerMovement.Knockback(OppositeFromEnemy)
  → PlayerStateMachine.LockInput(0.5초)
  → CombatFeedbackController.PlayRecoilShake()
```

Player보다 2레벨 이상 낮은 방패병은 일격 처치되므로 반동이 발생하지 않는다. 방패병 정면 치명타도 3배 피해만 적용하고 반동을 발생시키지 않는다. 반동 넉백 거리와 이동 시간은 전투 피드백 설정과 PlayerRoot 상수에서 관리하며 추후 데이터화한다.

## 9. 카메라와 무한 월드 구조

```text
Player
├─ PlayerRoot
└─ PlayerWorldArea

Main Camera
├─ CameraFollowController
└─ CameraShakeController

WorldGrid
├─ WorldChunkGrid
└─ WorldChunk × 9
   ├─ Tilemap
   └─ TilemapRenderer

PrototypeSystems
├─ EnemyWorldRecycler
├─ HealthPickupSpawner
└─ PoisonCloudSpawner

Entities
├─ HealthPickups
└─ PoisonClouds
```

- `CameraFollowController`가 Player 실제 월드 좌표를 추적한다.
- `CameraShakeController`는 추적 위치 위에 일시적인 오프셋만 합성하고 종료 시 추적 위치로 복귀한다.
- `WorldChunkGrid`는 Player가 속한 청크를 중심으로 3×3 좌표를 유지한다.
- `WorldChunk` 9개는 삭제·생성하지 않고 멀어진 행·열을 진행 방향 앞으로 재배치한다.
- 지형 원본은 현재 4종 Tile을 사용하며 활성 인스턴스 수와 원본 종류 수를 구분한다.
- `PlayerWorldArea`는 카메라보다 큰 Spawn 경계와 그보다 큰 재배치 경계를 계산한다.
- `EnemyWorldRecycler`는 `GameSession`에 등록된 일반 Enemy만 검사하고 청크와 독립된 `EnemyRoot` 안에서 반대편 Spawn 경계로 재배치한다.
- `HealthPickupSpawner`는 `GameSession.ElapsedTime`을 기준으로 20초마다 `PlayerWorldArea` 안의 무작위 위치를 선택한다. 생성 개수는 3개로 제한하고 각 `HealthPickup`이 45초 수명을 직접 관리한다.
- `HealthPickup`은 Trigger 체류 중 `HealthComponent.Heal(5)`가 실제로 1 이상 회복했을 때만 자신을 제거한다.
- `PoisonCloudSpawner`는 MushroomBoss 사망 좌표를 값으로 복사해 Enemy Pool 회수와 분리한다. 1초 지연 뒤 독립된 `MushroomPoisonCloud` 인스턴스를 생성한다.
- `MushroomPoisonCloud`는 Player와의 거리로 반경 진입을 검사하고 연속 노출 시간을 0.5초 단위로 소비하므로 별도 물리 Layer나 Enemy Collider에 의존하지 않는다.
- Tag 문자열은 보조 필터로만 사용할 수 있으며 청크·Enemy 재배치 책임은 명시적인 컴포넌트가 소유한다.

이어하기 처리 흐름:

```text
GameSession
  ↓ Continue 승인
PlayerRoot.RestoreAfterContinue(MaxHP)
  ↓
EnemyWorldRecycler.PushAwayAllNormalEnemies()
  ↓
일반 Enemy 현재 HP 50% 피해 + Hurt
  ↓
0.4초 동안 현재 방향 바깥쪽 Spawn 경계로 밀어내기
  (Boss 제외, 매 프레임 Enemy 분리 유지)
  ↓
Playing 복귀
```

Boss 사망 보상 흐름:

```text
EnemyBase.BeginDeath()
  ↓
PrototypeGameSession.OnEnemyDefeated()
  ├─ 점수·EXP 지급
  ├─ Boss: 공유 리롤 +1(최대 3) + 카드 선택 1회 Queue
  └─ MushroomBoss: 사망 좌표를 PoisonCloudSpawner에 전달
```

## 10. 이벤트와 직접 호출 기준

### 10.1 직접 호출

반드시 순서대로 실행되어야 하는 내부 행동은 직접 호출한다.

```text
StateMachine → Movement.Stop()
StateMachine → Attack.Execute()
PlayerCombat → CombatResolver.Resolve()
EnemyBase → Health.ApplyDamage()
PlayerCombat → PlayerMovement.Knockback()
PlayerCombat → PlayerStateMachine.LockInput()
```

### 10.2 이벤트

여러 외부 시스템에 알려야 하는 상태 변화만 이벤트로 발행한다.
아래 목록은 소비자가 생겼을 때 도입할 이벤트 후보이며, 구독자가 없는 이벤트를 미리 선언하지 않는다.

- `HealthChanged`
- `Died`
- `TargetChanged`
- `PlayerLevelChanged`
- `CriticalChanceChanged`
- `ScoreChanged`
- `GameOver`
- `PlayerAttackResolved`

예:

```text
EnemyHealth.Died
├─ EnemyVisual
├─ ScoreSystem
├─ ExperienceSystem
└─ PoolService
```

`PlayerAttackResolved`는 공격 결과를 시각·청각 시스템에 전달하는 타입이 명확한 이벤트다. `CombatFeedbackController`는 이 결과를 받아 일반 타격, 치명타와 정면 반동을 구분하고 `CameraShakeController`에 서로 다른 흔들림 설정을 요청한다. 한 공격에서 여러 조건이 겹치면 흔들림을 합산하지 않고 `치명타 > 정면 반동 > 일반 타격` 우선순위에 따라 가장 큰 흔들림 하나만 요청한다. 피해 무효 정면 공격 중 반동 조건에 해당하지 않는 결과에는 화면 흔들림을 재생하지 않는다.

전역 범용 EventBus는 사용하지 않는다. 발행자와 구독자를 쉽게 추적할 수 있는 타입이 명확한 C# 이벤트를 사용한다.

## 11. Definition과 런타임 상태

### 11.1 읽기 전용 Definition에 저장할 값

```text
EnemyDefinition
├─ EnemyType
├─ MoveSpeed
├─ ApproachRange
├─ AttackRange
├─ AttackDamage
├─ AttackWindup / ActiveDuration
├─ AttackCooldown
├─ FacingTurnDelay
├─ PostAttackFacingLock
├─ Score
├─ Experience
├─ BaseMaxHp
├─ HpGrowthMultiplier
├─ LevelDifficultyOffset
├─ OneHitPlayerLevelAdvantage
├─ CombatProfileId
└─ ShowHpBar
```

```text
PlayerDefinition
├─ StartLevel
├─ BaseMaxHp
├─ BaseAttackPower
├─ AttackGrowthMultiplier
├─ RearAttackMultiplier
├─ BaseMoveSpeed
├─ PathEnemyApproachSpeedMultiplier
├─ PostKillEscapeSpeedMultiplier
├─ MoveArrivalTolerance
├─ AttackRange
└─ BaseCriticalChance

LevelUpCardDefinition
├─ CardId / NameKey
├─ EffectType
├─ TargetStat / Operation / Value
├─ MaxStack / SelectionWeight
├─ MinPlayerLevel
├─ Rarity / IconId
└─ Enabled

CombatFeedbackDefinition
├─ NormalHitShake
├─ CriticalHitShake
└─ FrontRecoilShake
```

치명타 흔들림은 일반 타격보다 강한 값으로 검증한다. 구체적인 진폭, 주파수와 지속시간은 `CombatFeedbackDefinition`의 밸런스 값으로 둔다.

`EnemyDefinition`, `PlayerDefinition`, `LevelUpCardDefinition`은 게임 코드에서 사용하는 읽기 전용 설정 모델이다. 현재 저장 형태는 Excel importer가 생성한 ScriptableObject이며 WebGL 런타임에는 Excel 파서가 포함되지 않는다.

도메인 로직은 가능한 한 구체적인 저장 형태보다 `EnemyDefinition`이 제공하는 값에 의존한다. ScriptableObject를 사용하더라도 Excel을 원본으로 정한 뒤에는 생성 결과로만 취급하며 수동으로 양쪽을 수정하지 않는다.

### 11.2 Entity 인스턴스에 저장할 값

```text
EnemyRuntimeState
├─ CurrentLevel
├─ CurrentHealth
├─ CurrentTarget
├─ CurrentState
└─ CooldownRemaining
```

Definition 또는 ScriptableObject에는 현재 HP, 현재 타깃이나 남은 쿨타임처럼 플레이 중 변경되는 값을 저장하지 않는다.

## 12. Factory와 Object Pool

`WaveSpawner`는 Prefab을 직접 생성하지 않고 `EnemyFactory`에 생성을 요청한다.

```text
WaveSpawner
  ↓ Spawn 요청
EnemyFactory
  ↓ Definition 확인
PoolService
  ↓ 재사용 또는 생성
EnemyBase.Initialize()
```

Pool에서 다시 꺼낼 때 초기화해야 하는 값:

- 현재 HP
- 현재 레벨
- 현재 타깃
- 상태 머신 상태
- 공격 쿨타임
- 이동 속도
- 치명타 또는 피격 상태
- 이벤트 구독
- Animator 상태
- Collider 활성 상태

Pool로 반환할 때 자신이 등록한 이벤트를 반드시 해제한다.

## 13. UI 아키텍처

UI는 `View + Presenter + 프리팹 생명주기` 방식으로 구성한다.

```text
GameSession
  ↓ 상태 이벤트
HUDPresenter
  ↓ 표시 요청
HUDView
  ├─ Persistent HUD
  └─ Modal Prefab Instance
```

### 13.1 상시 UI와 일시 UI

씬에는 `PrototypeHUD.prefab` 인스턴스 하나만 둔다. 이 프리팹은 시간, 경험치, HP, 안내 문구처럼 플레이 중 계속 필요한 UI와 빈 `ModalRoot`를 가진다.

레벨업, ESC 상세 정보, 게임오버처럼 특정 상태에서만 표시되는 UI는 각각 독립된 프리팹으로 관리하며 씬 파일에 배치하지 않는다.

```text
PrototypeHUD.prefab
├─ TopPanel
├─ HintPanel
└─ ModalRoot
   ├─ CardSelectionPanel.prefab  (필요 시)
   ├─ PauseDetailsPanel.prefab   (필요 시)
   └─ GameOverPanel.prefab       (필요 시)
```

### 13.2 생성과 재사용

`PrototypeHUDView`는 일시 UI 프리팹 참조를 직렬화한다.

1. 상태 이벤트에서 표시 요청을 받는다.
2. 해당 인스턴스가 없으면 `ModalRoot` 아래에 생성한다.
3. 최신 표시 데이터와 Listener를 적용한다.
4. 인스턴스를 활성화한다.
5. 닫을 때 파괴하지 않고 비활성화해 다음 표시에서 재사용한다.

이 방식은 초기 씬 계층을 단순하게 유지하면서 반복 생성 비용과 Listener 중복을 방지한다.

### 13.3 View와 Presenter 책임

- `PrototypeHUDPresenter`: `GameSession` 이벤트 구독, 표시용 문자열 구성, Callback 전달
- `PrototypeHUDView`: 프리팹 생성·표시·숨김, Text 반영, Button Listener 연결
- Popup 프리팹: 레이아웃과 Graphic 컴포넌트 보유
- `GameSession`: UI 계층과 프리팹을 직접 참조하지 않음

### 13.4 버튼과 반복 UI

카드 선택 Button Callback은 프리팹 생성 전에도 View에 저장할 수 있어야 한다. `CardChoice0~2`가 생성되면 저장된 Callback을 연결한다.

동일 구조의 카드 3장은 `LevelUpCardView[]`와 `Button[]`로 관리한다. 서로 역할이 다른 `ContinueAd`는 별도 참조로 관리한다.

Listener를 연결할 때 View가 기존 런타임 Listener를 정리한 뒤 현재 Callback을 한 번만 등록한다.

### 13.5 구성 검증

HUD 초기화 단계에서 다음 참조를 검증한다.

- 시간, HP, 안내, 경험치 Slider와 Label
- `ModalRoot`
- CardSelection, PauseDetails, GameOver 프리팹

Popup 생성 단계에서는 다음 자식 요소를 검증한다.

- `CardChoice0~2`
- `PauseDetails`
- `GameOverTitle`
- `ContinueAd`

누락은 조용히 무시하지 않고 대상 프리팹 이름을 포함한 오류로 출력한다.

### 13.6 씬 정리 원칙

- ESC로 대체된 Pause 버튼을 씬에 두지 않는다.
- 개발 시험용 Button을 플레이 UI에 두지 않는다.
- ESC 상세 정보에서만 사용하는 점수, 레벨, 치명타 Text를 상시 HUD에 숨겨 두지 않는다.
- 일시 UI는 씬에 비활성 상태로 미리 배치하지 않는다.

## 14. 입력 구조

설치된 Unity Input System을 사용한다.

```text
InputAction
  ↓
InputReader
  ↓
PlayerTargetSelector
  ↓
PlayerStateMachine
```

`InputReader`는 화면 터치 또는 포인터 입력을 게임 명령으로 변환한다. UI 입력과 월드 입력의 차단 여부는 `UIFlowCoordinator`와 Player 입력 상태가 결정한다.

Player 컴포넌트가 Input System API를 여러 곳에서 직접 구독하지 않도록 한다.

## 15. 데이터 흐름

수치와 반복 행 데이터는 Excel을 원본으로 관리하고, Unity 에셋 참조와 연출
프로필은 수동 ScriptableObject로 관리한다. 현재 런타임은 `.bytes`를 직접
읽지 않고, 검증된 ScriptableObject를 통해 데이터에 접근한다.

```text
Excel 원본
  ├─ EnemyBalance
  ├─ StageSpawn
  ├─ PlayerLevelExp
  ├─ AccountLevelExp
  ├─ GlobalBalance
  ├─ PlayerBalance
  ├─ LevelUpCard
  ├─ GameString
  ├─ ImageData
  └─ LobbyDifficulty
          ↓ 가져오기·검증
Assets/Game/Data/Generated/*.asset
          ↓
GameDataManifest
          ↓
LobbyView / GameSession / StageSpawnController / EnemyFactory

Unity 수동 설정
  ├─ EnemyAssetCatalog (EnemyId ↔ Prefab)
  └─ CombatFeedbackProfile (화면 흔들림)
          ↓
GameDataManifest

Battle Scene
  ├─ SpawnPointRegistry (Player 기준 SpawnPointId ↔ Transform)
  ├─ WorldChunkGrid (3×3 Tilemap)
  └─ PlayerWorldArea (Spawn/재배치 경계)
```

### 15.1 자동 생성 ScriptableObject

`Assets/Game/Data/Generated` 아래 에셋은 Excel 값의 Unity 런타임 표현이다.

- `EnemyBalanceTable`: EnemyId별 전투 수치, 보상과 Archetype. 현재 GoblinMelee, GoblinRanged, ShieldSkeleton, GoblinBoss, MushroomBoss 5종
- `StageSpawnSchedule`: StageId, WaveId, 시간, 순번, SpawnPointId, EnemyId, 레벨
- `PlayerLevelExperience`: 플레이어 레벨별 다음 레벨 필요 EXP
- `AccountLevelExperience`: 계정 레벨별 다음 레벨 필요 EXP
- `GlobalBalance`: 점수→계정 EXP 환산식과 치명타 공통값
- `PlayerBalanceTable`: Player 기본 HP·공격력·성장률·기본 이동 속도·상황별 속도 배율·도착 허용 거리·사거리
- `LevelUpCardTable`: 카드 효과, 중첩, 가중치, 최소 등장 레벨과 활성 여부
- `GameStringTable`: 사용자 표시 문자열 ID와 한국어 문구
- `ImageDataTable`: 이미지 ID, 원본 파일명과 Import 시 해석한 Sprite 참조
- `LobbyDifficultyTable`: Lobby 표시 순서·사용 가능 여부·시간·보정 안내값·이미지와 문자열 Key·실행 난이도 연결

이 에셋들은 수동 편집하지 않고 `Planning/GameData_10min_Balance.xlsx` 가져오기 결과로만
취급한다. Excel 저장 후 Unity 메뉴 `SimpleGame > Data > Import Excel`을
실행하면 Editor 전용 importer가 필수 시트를 모두 메모리에서 검증한 뒤
정상일 때만 기존 에셋에 일괄 적용한다. 오류가 하나라도 있으면 기존 정상
에셋은 변경되지 않는다.

`.xlsx`는 Editor에서만 Open XML로 읽으며 런타임 빌드에는 Excel 파서가
포함되지 않는다. 현재 단계에서는 `.bytes`를 추가 생성하지 않고, 가져온
ScriptableObject를 Player 빌드에서도 그대로 읽는다. `Build Prototype Scene`
또는 `Create or Update Data Assets`는 누락된 데이터 에셋만 초기값으로
생성하며 이미 가져온 Excel 값은 덮어쓰지 않는다.

### 15.2 수동 ScriptableObject

Unity Object 참조와 Unity에서 직접 조정하는 연출값은 Excel에 넣지 않는다.

- `EnemyAssetCatalog`: 안정적인 EnemyId와 실제 Enemy Prefab을 연결
- `CombatFeedbackProfile`: 일반 타격, 방패 반동, 치명타 화면 흔들림 강도와 시간

데이터 생성 명령을 다시 실행해도 기존 수동 SO 값은 덮어쓰지 않는다.

### 15.3 Scene 데이터

스폰 위치의 좌표는 Scene에서 디자이너가 Player 기준 로컬 좌표로 편집한다. `SpawnPointRegistry`가
`LEFT_01`, `RIGHT_01`, `TOP_01`, `BOTTOM_01` 같은 ID를 실제 Transform에
연결한다. Spawn Transform은 Player를 따라가므로 장시간 이동 후에도 현재 카메라 주변에서 생성된다. Excel에는 좌표 대신 `SpawnPointId`만 기록한다.

### 15.4 진입점과 검증

`GameDataManifest`는 생성 데이터와 수동 카탈로그를 한 곳에 모으는 런타임
진입점이다. 게임 시작 전 다음 항목을 검증한다.

- 중복 Spawn RuntimeId
- 존재하지 않는 EnemyId 또는 Prefab
- Enemy Archetype과 Prefab 불일치
- 존재하지 않는 SpawnPointId
- 비어 있는 플레이어·계정 EXP 테이블

`.bytes`는 Toss WebGL의 초기 용량·로딩 측정 결과 실제 이점이 확인될 때만
추가한다. 추가하더라도 Excel을 원본으로 유지하고 SO와 `.bytes`를 동시에
수동 수정하지 않는다.

## 16. 의존성 연결

초기 단계에서는 별도의 DI 프레임워크를 사용하지 않는다.

- 같은 Entity 내부: 부모 Facade가 컴포넌트 참조 보유
- Prefab 내부 고정 참조: Inspector 참조
- 생성 시 결정되는 값: `Initialize()`로 전달
- 게임 공용 시스템: `GameBootstrap`이 생성하고 명시적으로 전달
- 변경 알림: 타입이 명확한 C# 이벤트

매 프레임 `Find`, `GetComponent`, `GetComponentInChildren`로 의존성을 탐색하지 않는다. 필요한 검색은 초기화 시 한 번만 수행하고 결과를 보관한다.

## 17. 현재 폴더 구조

```text
Assets/Game/
├─ Runtime/
│  ├─ Core/
│  ├─ Combat/
│  ├─ Data/
│  ├─ Player/
│  ├─ Enemies/
│  ├─ Entities/
│  ├─ World/
│  ├─ Presentation/
│  └─ UI/
├─ Editor/
├─ Data/
│  ├─ Generated/
│  ├─ Catalogs/
│  └─ Profiles/
├─ Characters/
│  ├─ Animations/
│  ├─ Animators/
│  └─ Shared/
└─ Tests/
   └─ EditMode/
```

모든 `.prefab` 에셋은 `Assets/Prefab`을 루트로 관리한다. 한 화면을 이루는 관련 UI는 `Assets/Prefab/UI/<화면명>`처럼 기능별 하위 폴더에 모으고, 현재 Lobby의 정적 화면은 `Assets/Prefab/UI/Lobby/LobbyScreen.prefab`에 둔다.
폴더 하나에 스크립트가 과도하게 늘어나기 전에는 Archetype별 빈 하위 폴더나 아직 구현하지 않은 Save·Infrastructure 폴더를 미리 만들지 않는다. 전체 파일명과 역할은 `Planning/ScriptStructure.md`에서 관리한다.

### 17.1 Assembly Definition

초기에는 지나치게 세분화하지 않는다.

```text
SimpleGame.Runtime
SimpleGame.Editor
SimpleGame.Tests.EditMode
SimpleGame.Tests.PlayMode
```

`SimpleGame.Editor`에는 Excel importer와 Scene/Prefab 생성 도구만 두며
WebGL Player 빌드에는 포함하지 않는다.

## 18. 테스트 기준

### 18.1 Edit Mode

Unity Scene 없이 검증할 규칙:

- 정면 및 후면 판정
- Player 공격력·Enemy 최대 HP 성장식과 종류별 레벨 보정
- 원거리·근거리·방패·Boss의 실제 필요 타격 수
- 치명타 피해량
- 점수에서 계정 경험치 변환
- 인게임 필요 경험치
- 터치 타깃 우선순위
- 공격 위치 계산: 사거리 밖 접근과 사거리 안 이동 생략
- 방패병 정면 일반 공격의 Player 반동 조건
- Wave 데이터 검증
- 카드 최소 레벨·가중치·최대 중첩과 목록 내 중복 방지

### 18.2 Play Mode

GameObject와 시간 흐름이 필요한 규칙:

- 공격 위치까지 이동한 후 피해 적용
- 최초 Enemy 접근이 강화된 현재 속도의 1.1배인지 확인
- 연속 일격 처치 시 남은 경로에서 1.2배를 유지하고 누적하지 않는지 확인
- 회색 공격 사거리 안에서 Collider가 겹쳐도 이동하지 않는지 확인
- 방패병 추격 및 하늘색 범위 끝 정지
- 방패병 경로 차단
- Player 반동 넉백과 0.5초 입력 차단
- 일반 타격, 치명타와 정면 반동별 화면 흔들림
- Enemy 목표 변경
- Player 추적과 0.5초 좌우 방향 전환 지연
- 근거리 공격 예고 방향 고정과 판정 후 해제
- 원거리 발사 전 조준, 발사 후 1초 방향 고정과 2초 쿨타임
- Shield 0.8초 방향 고정과 예약 전환
- 플레이어 사망 게임 오버와 광고 부활
- 카메라 추적과 CameraShake 합성
- 3×3 Tilemap 청크 재배치
- 일반 Enemy 반대편 Spawn 경계 재배치
- 보스 3초 공격 주기
- 레벨업 HP 2 회복과 필드 회복 오브젝트 HP 5 회복
- 회복 오브젝트 20초 생성·3개 상한·45초 만료
- Boss 카드 선택 1회와 리롤 1회 충전
- MushroomBoss 사망 1초 지연·5초 독구름·0.5초당 피해 1
- Object Pool 재사용
- UI enum 자동 바인딩과 Listener 중복 방지
- Enemy HP Slider와 현재/최대 숫자 갱신

## 19. 피해야 할 구조

- 하나의 `GameManager`가 모든 시스템 직접 처리
- `EnemyBase`가 이동, 공격, HP와 UI를 전부 구현
- 한 가지 수치 차이마다 새로운 Enemy 클래스 생성
- 기능 하나마다 빈 자식 GameObject 생성
- 깊은 Enemy 상속 트리
- 구현체가 하나뿐인 모든 기능에 인터페이스 생성
- 전역 EventBus를 통한 모든 행동 전달
- 모든 시스템의 Singleton화
- ScriptableObject에 현재 HP와 쿨타임 저장
- UI enum 이름으로 매번 Transform 검색
- `GetComponentsInChildren` 반환 순서를 enum 순서로 가정
- 화면 하나에 모든 게임 UI enum 사용
- UI Registry에서 게임 로직 직접 호출
- Pool 반환 시 이벤트와 타이머 초기화 누락

## 20. 클래스 추가 판단 기준

새 클래스 또는 컴포넌트를 만들기 전에 다음 순서로 판단한다.

1. 기존 데이터 값 변경만으로 표현 가능한가?
2. 기존 컴포넌트 조합으로 표현 가능한가?
3. 기존 기능 클래스의 명확한 파생 구현인가?
4. Entity 전체의 상태 흐름이 실제로 다른가?

예:

| 요구사항 | 처리 |
|---|---|
| 공격력이 높은 원거리 Enemy | 데이터만 추가 |
| 독 투사체를 사용하는 원거리 Enemy | `PoisonProjectileAttack` 추가 |
| 같은 독 공격이지만 Sprite만 다름 | 데이터만 추가 |
| 일반 Enemy가 Player만 추적 | 공통 `EnemyStateMachine` 규칙 |
| 다단계 고유 상태를 가진 보스 | `BossEnemy` 및 전용 상태 머신 |

## 21. 구현 순서

1. 폴더와 최소 Assembly Definition 구성
2. 순수 C# `CombatResolver`와 테스트
3. `EnemyBase` 및 공통 기능 컴포넌트
4. 일반 Enemy 상태 머신
5. `MeleeEnemy`와 `RangedEnemy`
6. Player 이동, 타깃 선택과 공격
7. `ShieldEnemy`
8. Player 사망 게임 오버와 게임 상태
9. 인게임 경험치와 치명타 카드
10. Boss 상태와 공격 주기
11. UI View, Presenter와 enum 자동 바인딩
12. Factory, Object Pool과 WaveSpawner
13. 카메라 추적, 3×3 청크와 Enemy 재배치
14. 저장, 계정 성장과 광고 연결

각 단계는 현재 단계에 필요한 최소 클래스만 추가하며 이후 기능을 예상해 불필요한 범용 프레임워크를 먼저 만들지 않는다.

## 22. 최종 결정

이 프로젝트에서는 다음 기준을 사용한다.

- 공통 생명주기와 외부 API는 얕은 부모 클래스에 둔다.
- 실제 기능은 재사용 가능한 컴포넌트가 소유한다.
- 부모는 자식 또는 같은 Entity의 기능 컴포넌트 참조와 초기화를 관리한다.
- 상태 머신이 행동 순서를 조율한다.
- 교체 가능한 알고리즘만 Strategy 파생 클래스로 분리한다.
- 수치 차이는 데이터로 처리한다.
- 외부 알림에는 제한적인 C# 이벤트를 사용한다.
- UI는 화면별 enum과 GameObject 이름 규칙으로 초기화 시 자동 바인딩한다.
- UI Button 이벤트는 `Bind(enumId, callback)` 형태로 등록한다.
- 반복 UI는 enum이 아니라 배열 또는 리스트로 관리한다.
- 실행 흐름을 숨기는 전역 EventBus, UniRx와 과도한 추상화는 초기 단계에서 사용하지 않는다.

## 23. 3차 콘텐츠 아키텍처 결정

### 23.1 기능 속성 기반 Enemy 예외

- Flying Eye를 위해 새 `EnemyArchetype`이나 물리 Layer를 추가하지 않는다.
- `EnemyDefinition.AllowsEnemyOverlap`은 EnemyId에서 파생되는 읽기 전용 기능 속성이다.
- `EnemyWorldService`는 새로 들어오는 Enemy와 기존 Enemy 양쪽의 속성을 확인한다. 어느 한쪽이라도 겹침 허용이면 생성 위치 회피와 수동 분리를 생략한다.
- `PrototypeEnemyFactory.Spawn`, `EnemyBase.Reposition`, 이어하기 밀치기는 같은 정책 인자를 전달한다.
- 공격 경로·근접 검색·범위 수집은 이 속성을 보지 않으므로 비행형도 정상 피격 대상이다.

### 23.2 데이터 기반 보스 패턴

```text
BossEnemy
  └─ BossAttackModule
       ├─ BossAttackPatterns.Get(enemyId, sequence)
       ├─ BossAttackPattern(variant, shape, length, width)
       ├─ lockedOrigin / lockedDirection
       └─ CharacterSpriteAnimator.PlayAttack(direction, variant)
```

- 네 보스의 별도 파생 클래스를 만들지 않고 `EnemyDefinition.EnemyId`로 두 패턴의 데이터만 선택한다.
- `BossAttackShape`은 `ForwardBox`와 `CenteredBox` 두 종류이며, `GetCenter`, 회전, `Contains`가 경고와 실제 판정의 단일 기준이다.
- `nextPatternSequence`는 실제 공격이 발동될 때만 증가한다. Windup 취소는 현재 패턴을 재시도하고 Pool의 `Configure`는 Attack1로 초기화한다.
- Animator Controller는 기존 `Attack`과 별도 `Attack2` Trigger/State를 가진다. `CharacterAssetBuilder.EnsureControllerStates`가 기존 Controller에도 파라미터·클립·전이를 보강한다.

### 23.3 방패 기능 분리

- `SkeletonBoss`는 `EnemyArchetype.Boss`를 유지한다.
- `EnemyDefinition.BlocksFrontAttacks`가 `ShieldSkeleton`과 `SkeletonBoss`의 전면 방어 공통 기능을 표현한다.
- `CombatResolver`, 관통 판정, 반동과 방패 우회는 이 기능 속성을 사용한다.
- Shield 전용 접근 대기·방향 잠금·표시 범위는 계속 `EnemyArchetype.Shield`만 사용한다.

### 23.4 오물 투척 수명주기

```text
PlayerCombatAbilities.Update
  └─ CalculateFilthThrowCount(level) = clamp(level, 1, 5)
       └─ FindRandomLivingEnemyInBounds(camera bounds)
            └─ 같은 프레임에 레벨 수만큼 독립 목표 선택·Spawn
            └─ FilthProjectile
                 ├─ Throw 0.45초
                 └─ Field 3초
                      └─ 0.5초마다 FillEnemiesInRadius(reused List)
                           └─ PlayerRoot.ApplySkillHit
```

- 별도 Scene 시스템이나 Spawner를 추가하지 않는다. 자동 발동 주기와 카드 레벨은 기존 `PlayerCombatAbilities`가 소유한다.
- `PlayerRoot.Configure`가 이미 받은 World Camera를 전달하므로 `Camera.main` 전역 검색에 의존하지 않는다.
- `EnemyWorldService.FindRandomLivingEnemyInBounds`는 등록된 Enemy 목록에서 화면 안 생존 후보 수를 센 뒤 난수 인덱스 하나를 다시 순회해 선택하므로 별도 후보 List를 할당하지 않는다. 후보가 없으면 쿨다운을 소비하지 않고 다음 갱신에서 재시도한다.
- 투사체 하나가 선택 순간 Enemy 위치까지의 포물선 비행과 장판 상태를 모두 담당하며 장판은 매 틱 현재 Enemy 목록을 다시 수집한다. 같은 쿨다운에 생성된 구체는 목표와 장판을 서로 독립적으로 소유한다.
- 모든 범위 대상을 공격하는 장판에는 거리 정렬이 필요 없다. `FilthProjectile`이 보유한 List를 `EnemyWorldService.FillEnemiesInRadius`에 넘겨 비우고 다시 채워 틱마다 후보·결과 List를 생성하거나 정렬하지 않는다.
- 목표 위치, 포물선, 레벨별 투척 수·피해·반경·재사용, 틱 수는 순수 정적 함수로 분리해 EditMode에서 검증한다.

## 24. 4차 모바일 조작 아키텍처 결정

### 24.1 입력 흐름과 명령 스냅샷

```text
AimJoystickControl(pointerId)
  ├─ PointerDown → PlayerController.BeginAim
  │                  └─ 기존 명령과 독립된 조준 상태 시작
  ├─ Drag → SetAimInput
  │           ├─ 방향·크기 → Viewport 기반 RawAimDestination
  │           └─ EnemyWorldService.FindAimAssistTarget
  │                └─ 표시용 AimDestination만 Enemy에 고정
  └─ PointerUp → EndAim
                  └─ 가이드만 해제

AttackCommandButton.PointerDown
  └─ PrototypeHUDView / PrototypeHUDPresenter
       └─ PlayerController.ExecuteAimedCommand
            └─ 원본 통로 후보 재검사
                 └─ 고정 Enemy 현재 위치 또는 RawAimDestination 스냅샷
                 └─ TryIssueCommand
```

- `AimJoystickControl`은 최초 PointerId 하나만 소유하고 다른 Pointer의 Drag·PointerUp을 무시한다. 공격 버튼은 다른 Pointer로 동시에 사용할 수 있다.
- 앱 포커스를 잃거나 모바일 앱이 일시정지되면 PointerUp 누락에 대비해 조이스틱 소유권·입력·가이드를 즉시 해제한다.
- `BeginAim`과 `SetAimInput`은 조준 상태와 표시만 갱신하며 기존 명령을 취소하거나 이동·공격 API를 호출하지 않는다.
- `EnemyWorldService.FindAimAssistTarget`은 등록된 Enemy를 추가 할당 없이 순회해 원본 선분의 전방·거리·통로 폭을 검사한다. 중앙선 각도 오차를 우선하고 진행 거리를 동률 기준으로 사용하며, 넓은 유지 폭과 작은 점수 허용치로 미세 입력에서 대상이 깜빡이지 않게 한다.
- `PlayerController`는 `RawAimDestination`과 표시용 `AimDestination`을 분리한다. 공격 직전에 후보를 다시 검사하고 유효한 잠금이 있으면 Enemy 현재 위치를, 없으면 원본 끝점을 기존 `TryIssueCommand`에 전달한다. 따라서 조준 입력과 월드 직접 터치는 동일한 이동·타깃·공격 파이프라인을 공유한다.
- 공격 버튼과 월드 직접 터치는 `EndAim`을 호출하지 않는다. 이후 조이스틱을 움직이면 다음 명령의 조준점만 바뀌고 현재 명령의 목적지는 유지된다.
- `EndAim`은 입력과 끝점을 Player 위치로 초기화하고 Renderer를 숨기지만 `CancelCommand`를 호출하지 않는다.

### 24.2 화면 좌표와 월드 가이드

- 패드 로컬 오프셋을 반경으로 나누고 벡터 길이를 1로 Clamp해 360도 정규화 입력을 만든다.
- `PlayerController.CalculateMaximumAimDistance`는 현재 Player 좌표에서 카메라 직교 Viewport 경계와 조준 방향의 교점을 구한다. 가로·세로 반경은 각각 0.5 월드 단위 줄인 안전 경계를 사용한다.
- 끝점은 `playerPosition + normalizedInput × maximumDistance`로 계산하므로 입력 크기와 레이 길이가 선형이다.
- 표시 레이와 끝점은 조준 보정 대상이 있을 때만 그 Enemy 위치를 사용한다. 원본 끝점은 항상 조이스틱 입력으로 계산되어 보정 후보 탐색 범위와 사용자의 의도를 유지한다.
- `CharacterAssetBuilder`가 Player Prefab에 하늘색 `AimRay`와 45도 회전한 `AimEndpoint` SpriteRenderer를 저장한다. `PlayerController`는 위치·회전·길이·Pulse와 표시 여부만 변경한다.

### 24.3 HUD 구성과 책임

- `PrototypeSceneBuilder`는 `1080×1920` 기준 `CommandControls` 아래 좌하단 `AimJoystickControl`과 우하단 `AttackCommandButton`을 생성한다.
- `PrototypeHUDView`는 두 Control 참조를 보관하고 Player를 조이스틱에 연결한다.
- `PrototypeHUDPresenter`는 `HudButtonId.Attack`을 `PlayerRoot.ExecuteAimedCommand`에 바인딩한다.
- 공격 버튼은 Unity `Button.onClick` 완료 시점 대신 `AttackCommandButton.IPointerDownHandler`를 사용한다. View의 기존 Callback 저장·교체 규칙을 유지해 중복 발행을 막는다.
- `PauseDetailsPanel.prefab`의 `ControlPadToggle`은 `PrototypeHUDView.SetCommandControlsEnabled`에 한 번 연결된다. OFF는 조이스틱을 먼저 비활성화해 `OnDisable → EndAim`을 보장한 다음 공격 버튼을 숨기고, ON은 두 Control을 복구한다. 기본 ON인 실행 중 상태만 보관하며 `PlayerPrefs` 영구 저장은 하지 않는다.
- 기존 `PlayerController`의 월드 직접 터치 경로는 삭제하지 않는다. `Touchscreen.touches`의 신규 Press를 모두 순회하므로 첫 손가락이 조이스틱을 점유한 동안 두 번째 손가락의 월드 터치도 처리한다. 새 Pointer를 읽은 프레임에는 `IsPointerOverGameObject`의 갱신 순서에 의존하지 않고 현재 화면 좌표로 `EventSystem.RaycastAll`을 즉시 실행해 `GraphicRaycaster` UI 입력을 월드 명령에서 차단한다.

### 24.4 검증 경계

- EditMode에서는 패드 입력 정규화·Clamp, 입력 크기별 거리, Viewport 0.5 여백, 중립 입력 거부, Pointer Down Callback 단일 발행과 동일 프레임 UI Graphic 차단을 검증한다.
- 에셋 검증에서는 Player Prefab의 비활성 `AimRay`·`AimEndpoint`, HUD의 좌우 Control, Pause 설정 Toggle, `PrototypeHUDView` 참조와 기준 위치를 확인한다.
- PlayMode에서는 조준 시작·드래그 중 기존 명령 유지, 드래그만으로 새 이동·공격 미발생, 명령 스냅샷, 해제 후 명령 유지, 두 손가락 Pointer 소유권과 기존 월드 터치 회귀를 확인한다.
- Toggle OFF/ON 시 좌·우 Control 상태와 View 상태가 일치하고 OFF 전 조준이 해제되는지 확인한다.

## 25. 성능 최적화 경계

### 25.1 즉시 적용한 저위험 최적화

- `EnemyBase.CollisionRadius`가 `CircleCollider2D` 참조를 캐시한다. `EnemyWorldService`의 경로·분리·검색에서 반복 `GetComponent`를 피하되 Transform Scale을 반영한 실제 반경 계산은 매 요청마다 유지한다.
- 오물 장판은 호출자가 소유한 List를 채우는 무정렬 반경 쿼리를 사용한다. 레벨 5의 `5개 장판 × 장판당 6틱`에서도 쿼리용 List 할당과 전체 거리 정렬을 만들지 않는다.
- `CharacterSpriteAnimator`는 마지막 Motion·FaceLeft 값을 기억해 값이 바뀔 때만 Animator 파라미터를 기록한다. `tintPulseSpeed=0`이면 해당 Adapter의 `LateUpdate`를 비활성화하며 공격·피격·사망처럼 외부에서 직접 호출되는 메서드는 계속 동작한다.

### 25.2 측정 후 적용한 구조 개선과 남은 항목

- 지상 Enemy 분리는 `EnemyWorldService` 외부 API를 유지한 채 2m Uniform Spatial Hash로 교체했다. 등록·이동 시 점유 셀을 갱신하고 분리 반경과 겹치는 버킷만 검사한다.
- 일반 Enemy의 World Space Canvas·Slider·TMP 체력바는 피격 직후 제한 시간만 표시하거나 SpriteRenderer 기반 바로 바꾸고, Animator Culling을 검토한다.
- 정전기 Arc에 이어 오물·이동 참격도 Prefab별 최대 16개의 공용 Component Pool을 적용했다. 재사용 시 타이머·Alpha·타격 이력을 초기화한다.
- `PlayerController`, `PlayerCombatAbilities`, `FlyingSwordController`, `PrototypeHUDView`, `PrototypeGameSession`은 런타임 Update 수를 늘리지 않는 partial 파일로 책임을 분리했다.
- Editor Mono 마이크로벤치마크는 구현 전후 알고리즘과 반복 생성 비용 비교에 사용했다. 실제 빌드의 활성 Enemy 100/300/500마리 CPU Timeline, `Physics2D.Simulate`, `Animator.Update`, `Canvas.BuildBatch` median·p95는 별도 후속 측정으로 남긴다.

## 26. 전투 피드백과 스크립트 구조 정리

### 26.1 현재 Runtime 책임 폴더

- `Core`: 실행 조율, 월드 Registry/Factory/Pool과 여러 기능을 연결하는 Facade
- `Combat`: 저장 상태를 소유하지 않는 전투 판정과 Player 스킬 투사체·전투 효과
- `Data`: Excel Import 결과와 수동 Profile ScriptableObject
- `Enemies`, `Player`, `Entities`: Entity Facade와 수명주기가 있는 기능 컴포넌트
- `Presentation`: 카메라 흔들림, Animator Adapter, 전투 피드백과 데미지 팝업
- `UI`: HUD View/Presenter와 모바일 조작 UI
- `World`: 회복 오브젝트와 환경 공격

전체 파일 트리와 스크립트별 단일 책임은 `Planning/ScriptStructure.md`를 현재 기준으로 삼는다.

### 26.2 Enemy 공용 타입

```text
EnemyActor(serialized EnemyArchetype)
  └─ EnemyBase
       ├─ EnemyHealth
       ├─ EnemyMovement
       ├─ EnemyFacing
       ├─ EnemyStateMachine
       ├─ EnemyAttackModule (필요한 Prefab)
       └─ BossAttackModule (Boss Prefab)
```

- `MeleeEnemy`, `RangedEnemy`, `ShieldEnemy`, `BossEnemy`는 Archetype 상수 하나만 반환하고 동작을 추가하지 않아 `EnemyActor`로 통합한다.
- Enemy별 행동은 `EnemyDefinition`, 직렬화된 Archetype과 조합된 공격 컴포넌트가 결정한다. 새 종류도 알고리즘이 같다면 파생 클래스가 아니라 데이터와 Prefab 조합을 추가한다.
- `HealthComponent`와 `EnemyHealth`는 정수 HP·무적·회복 대 float HP·Pool 재설정처럼 수명주기가 다르므로 현재 분리를 유지한다. 공용화는 이름이 아니라 동일한 정책과 변경 이유를 기준으로 한다.

### 26.3 피해 팝업 흐름

```text
EnemyBase.ReceivePlayerAttack / PlayerRoot.ReceiveDamage
  └─ 실제 HP 감소량(before - after) 계산
       └─ Actor Prefab의 DamagePopupAnchor 월드 위치
            └─ PrototypeGameSession
                 └─ CombatFeedbackController
                      └─ 전역 DamagePopupView Pool (Prewarm 16 / 최대 64)
                           └─ 상승 + Alpha Fade + 비활성 반환
```

- 피해를 적용하는 가장 낮은 공통 진입점 두 곳에서 팝업을 요청하므로 일반 공격, 모든 Player 스킬, Enemy·Boss 공격과 독 피해를 함께 포괄한다.
- Player와 8개 Enemy Prefab은 TMP가 아니라 편집 가능한 빈 `DamagePopupAnchor`만 소유한다. 기본 로컬 높이는 Player `1.15`, 일반 Enemy `1.25`, Boss `1.8`이다.
- 실제 World Space TMP는 피격 Entity의 자식이 아닌 전투 피드백 Root에서 재생해 Enemy가 사망 즉시 Pool로 돌아가도 `0.82초`의 표시 수명을 유지한다. `0.9`만큼 상승하며 순환 Stagger Offset으로 동시 타격의 완전한 중첩을 피한다.
- `DamagePopupView`는 새 Manager를 만들지 않고 기존 `CombatFeedbackController`가 소유한다. 16개를 미리 준비하고 최대 64개로 제한해 비활성 인스턴스를 재사용한다.
- Entity마다 TMP를 상주시켜 한 Text를 갱신하는 구조는 동시 타격이 서로 덮이고 Entity 사망·비활성화와 팝업 수명이 결합되므로 채택하지 않는다.
- 표시 스타일은 일반·치명타·Player 피격 순으로 Bold 크기 `3.1/3.8/3.35`, Renderer 정렬 순서 `220` 이상이다. 실제 감소량이 0 이하면 생성하지 않는다.
- `GameDataAssetBuilder`는 `DamagePopup.prefab`을 명시적으로 로드해 `CombatFeedbackController.Configure`에 전달한다. 런타임 참조가 없을 때는 동일 경고를 반복하지 않고 한 번만 기록한다.
- `CharacterAssetBuilder.MigrateDamagePopupAnchors`는 Player와 8개 Enemy Prefab만 `LoadPrefabContents`로 열어 Anchor가 없을 때만 생성·연결한다. 이미 연결된 Anchor와 기획자가 바꾼 위치를 보존하므로 반복 실행해도 결과가 변하지 않는다.

### 26.4 처치 피드백 단일 경로

```text
모든 피해 원천
  └─ EnemyBase.BeginDeath (생성 개체당 한 번)
       └─ PrototypeGameSession.OnEnemyDefeated
            └─ CombatFeedbackController.PlayDefeatingHit
                 └─ CameraShakeController.Play
```

- 절단·정전기·참격·오물처럼 `ApplySkillHit`를 통과하는 공격도 공통 사망 경로에서 처치 흔들림을 받는다.
- 공격 원천마다 흔들림 코드를 추가하지 않아 누락과 이중 호출을 동시에 막는다.
- 한 프레임에 여러 요청이 와도 CameraShake는 강도를 누적하지 않고 더 강한 활성 요청 정책을 유지한다.

### 26.5 전투 효과 Pool과 오물 VFX 소유권

- 정전기 Arc는 `SlashTrailEffect`의 비활성 인스턴스 재사용 Pool을 사용하고 LineRenderer와 Material을 적중마다 생성·파괴하지 않는다. 재사용 시 위치, 폭, 색·Alpha와 수명 상태를 모두 초기화한다.
- 데미지 팝업도 같은 원칙으로 별도 제한 Pool을 사용한다. 서로 표현과 컴포넌트가 달라 하나의 억지 범용 Pool 타입으로 합치지 않고, 소유 Facade와 재사용 정책만 통일한다.
- 오물 장판은 기존 호출자 소유 List 재사용을 유지한다. 오물·이동 참격 투사체는 측정 후 `ComponentPrefabPool<T>`를 적용하고 Prefab별 비활성 인스턴스를 최대 16개 보관한다.
- `FilthProjectile.prefab`의 구체·장판 시각 자산은 기획자가 소유한다. Builder는 유효한 기존 Prefab을 보존하고, 런타임은 `orbRenderer`, `fieldVisual` 참조를 통해 상태만 전환한다.

## 27. Spatial Index·투사체 Pool·partial 모듈화

### 27.1 2m Uniform Spatial Hash

```text
EnemyWorldService
  ├─ enemies: 전체 등록 순서와 기존 Query API
  ├─ spatialEntries: Enemy → SpawnGeneration + 점유 CellRange
  ├─ separationBuckets: Cell → List<EnemyBase>
  ├─ bucketListPool: 비어 있는 Bucket List 재사용
  └─ separationCandidates: 분리 호출 중 후보 중복 제거·재사용
```

- 셀 크기는 2m다. Enemy의 위치와 충돌 반경으로 최소·최대 셀을 계산해 경계에 걸친 개체도 모든 점유 버킷에 등록한다.
- 등록·해제와 이동 후 셀 범위가 바뀔 때만 버킷을 갱신한다. 빈 버킷의 List는 `bucketListPool`로 반환하며, 분리 중 후보 버퍼도 호출마다 새로 할당하지 않는다.
- 분리 정책, SpawnGeneration 검사와 겹침 허용 규칙은 기존 구현을 유지한다. Spatial Hash는 후보 수집 방식만 교체한다.

| Editor Mono, Enemy 800·전체 3 sweeps | 기존 O(N²) | 2m Spatial Hash | 변화 |
|---|---:|---:|---:|
| 간격 3 median | 1,141.060ms | 5.194ms | -99.54% |
| 간격 3 pair/bucket | pair 3,835,200 | pair 0 + bucket 10,800 | pair 제거 |
| 간격 0.55 median | 1,190.317ms | 135.967ms | -88.58% |
| 간격 0.55 pair/bucket | pair 3,835,200 | pair 144,111 + bucket 12,909 | pair -96.24% |

- 등록 메모리는 24,576B에서 208,896B로 증가했다. 인덱스 오버헤드는 184,320B, +750%, 약 230B/Enemy이며 warm 분리 경로의 GC 할당은 0B다.
- 각 sweep은 800마리 각각의 `SeparateEnemy` 호출이며 호출 내부는 2-pass다. 기존 pair check 3,835,200은 `800×799×2 pass×3 sweeps`로 계산된다.
- 이는 Unity Editor Mono 마이크로벤치마크다. 실제 빌드의 전체 프레임 성능과 동일하지 않고, 후보가 한 셀에 몰리는 정도에 따라 개선 폭이 달라진다.

### 27.2 공용 Component Prefab Pool

```text
FilthProjectile.Spawn ─────┐
                           ├─ ComponentPrefabPool<T>
MovingSlashProjectile.Spawn┘    ├─ Prefab별 inactive Stack
                                ├─ 상한 16
                                ├─ Rent / Return
                                └─ Runtime 종료 시 DestroyAll
```

- Pool은 Prefab을 Key로 분리해 잘못된 외형·직렬화 참조의 인스턴스가 섞이지 않게 한다. 비활성 상한 16을 넘긴 반환 개체는 보관하지 않는다.
- 투사체는 반환 직전에 시각·충돌 상태를 끄고, 대여 후 Configure에서 소유자·월드·목표, 비행/장판 시간, Alpha, Enemy·SpawnGeneration 적중 이력을 초기화한다.
- 1,000회 직렬 Spawn·완료 Editor Mono 벤치마크에서 오물은 23.807→2.221ms(-90.67%), 참격은 14.531→2.115ms(-85.44%)였다. 실행 중 생성/파괴는 1,000/1,000→1/0회이며 런타임 종료 최종 정리가 1회다.
- 추적한 생성 할당 footprint는 오물 7,963B, 참격 2,601B로 기존 대비 약 99.9% 감소했다. 이 값은 반복 생성 allocation traffic 기준이다. inactive Pool의 resident memory가 남으므로 전역 메모리 절감률로 해석하지 않는다.

### 27.3 partial 책임 경계

```text
PlayerController
  ├─ PlayerController.cs (공통 입력·명령·직렬화, 1,766→862줄)
  ├─ PlayerController.ModeOne.cs
  └─ PlayerController.AimVisuals.cs

PlayerCombatAbilities
  ├─ PlayerCombatAbilities.cs (공통 상태·피해, 1,163→594줄)
  ├─ PlayerCombatAbilities.Cards.cs
  └─ PlayerCombatAbilities.Skills.cs

FlyingSwordController
  ├─ FlyingSwordController.cs (상태·설정·Slot, 1,028→207줄)
  ├─ FlyingSwordController.Flight.cs
  └─ FlyingSwordController.Visuals.cs

PrototypeHUDView
  ├─ PrototypeHUDView.cs (참조·공개 표시 API, 1,526→415줄)
  ├─ PrototypeHUDView.ControlSettings.cs
  ├─ PrototypeHUDView.Localization.cs
  └─ PrototypeHUDView.Panels.cs

PrototypeGameSession
  ├─ PrototypeGameSession.cs (상태·연결·수명주기, 952→251줄)
  ├─ PrototypeGameSession.CardSelection.cs
  ├─ PrototypeGameSession.Pause.cs
  └─ PrototypeGameSession.RunFlow.cs
```

- partial 파일은 컴파일 시 기존 단일 타입으로 합쳐진다. 직렬화 필드, 공개 API, MonoBehaviour 수와 Update 수가 동일하므로 이 분리 자체의 런타임 CPU·메모리 절감은 0%다.
- 목적은 책임별 코드 탐색, 변경 검토와 병합 충돌 범위를 줄이면서 Unity Prefab·Scene 직렬화 호환을 유지하는 것이다.

### 27.4 EnemyAssetCatalog Component 교체 호환

- `EnemyActor` 통합은 Prefab 루트의 기존 `MeleeEnemy` 등 Component를 새 Component로 교체한다. ScriptableObject가 구 Component를 직접 가리키던 참조는 이 과정에서 비어 있을 수 있다.
- `EnemyAssetEntry`는 직접 `EnemyBase` 참조와 Prefab 루트 `GameObject`를 함께 저장한다. 직접 참조가 없을 때 루트의 현재 `EnemyBase`를 `GetComponent`로 복구한다.
- Builder도 Prefab을 `GameObject`로 먼저 로드한 뒤 현재 `EnemyBase`를 읽는다. 루트까지 없거나 현재 EnemyBase가 없으면 기존처럼 명시적으로 조회 실패한다.

## 28. Lobby 씬·표시 데이터 아키텍처

### 28.1 앱 진입과 씬 경계

```text
Build Settings
  0. Lobby
     └─ LobbyScreen.prefab
          ├─ LobbyView
          └─ LobbyDifficultyOptionView × 3
                 ↓ 쉬움/보통 선택 저장 + 입장하기
  1. Battle
     └─ PrototypeGameSession
          ├─ 저장 Lobby ID → GameDifficulty 변환
          └─ 기존 SelectDifficulty 실행
```

- `Lobby`가 제품의 첫 씬이고 `Battle`이 실제 전투 씬이다. 전환 문자열은 `Battle` 하나로 고정한다.
- `Battle`을 편집기에서 직접 실행할 수 있도록, 유효한 Lobby 저장값이 없을 때만 기존 인게임 난이도 모달을 보여준다. 정상 Lobby 경로는 선택 시 저장값을 만든 뒤 Battle을 열므로 같은 난이도를 다시 묻지 않는다.
- 어려움은 Lobby 전용 `LobbyDifficultyId`에는 존재하지만 런타임 `GameDifficulty`에는 연결하지 않는다. UI 준비 상태가 Spawn 데이터 존재를 암시하지 않도록 `IsAvailable=false`, 실행 난이도 없음으로 직렬화한다.

### 28.2 런타임 조립 없는 Lobby UI

- 고정 계층은 `Assets/Prefab/UI/Lobby/LobbyScreen.prefab`에 저장한다. `Lobby.unity`에는 Main Camera, EventSystem과 이 프리팹 인스턴스만 둔다.
- `LobbySceneBuilder`는 Editor 전용 최초 생성·대상 한정 마이그레이션 도구다. 기존 Lobby Prefab과 Scene이 있으면 `Build`는 다시 저장하지 않는다. 런타임 `LobbyView`는 저장된 참조에 Listener와 표시값만 적용한다.
- 상단 `도감`은 적 탭으로 도감을 열고 `설정`은 별도 설정 Prefab을 연다. `특성`은 상단 Placeholder 버튼만 유지하며 도감과 연결하지 않는다.
- 최초 미선택 상태는 enum 기본값으로 표현하지 않고 `hasSelection`과 PlayerPrefs 키 존재 여부로 구분한다. 키가 없거나 현재 선택을 해제하면 모든 난이도 배경을 회색으로 두고 `DifficultyPreview` 전체와 입장 버튼을 비활성화한다. 같은 난이도 버튼 재입력은 현재 선택만 해제하고 다음 진입 복원용 마지막 저장 ID는 유지한다.

### 28.3 문자열·수치·이미지 데이터 흐름

```text
GameData_10min_Balance.xlsx
  ├─ GameString ───────────────→ GameStringTable
  ├─ LobbyDifficulty ──────────→ LobbyDifficultyTable
  └─ ImageData (Id, FileName)
             + Assets/Image/<FileName>
                    ↓ Editor Import에서 Sprite 해석
              ImageDataTable
                       ↓
                GameDataManifest
                       ↓
                   LobbyView
```

- `ImageData`에는 Unity 경로나 Object 참조 대신 `Id`, `FileName`만 둔다. Importer는 경로 구분자·상위 경로 이탈을 거부하고 고정 폴더 `Assets/Image`의 Sprite를 생성 SO에 직접 연결한다. Player 빌드는 PNG 파일을 경로로 읽지 않는다.
- `LobbyDifficulty`는 전투 Spawn 시트가 아니라 표시 메타데이터다. 시간, 보정 설명 수치, 이미지 ID와 이름·버튼 설명·목표·효과 GameString Key를 한 행으로 연결한다.
- 선택 저장은 `LobbyDifficultySelectionStore` 한 곳이 PlayerPrefs Key와 유효성 검사를 소유한다. 저장값 손상, 정의 누락, 어려움 값은 선택 없음으로 축소한다.

### 28.4 알려진 데이터 불일치

- 쉬움의 Lobby 표시 시간은 5분이지만 현재 `StageSpawnEasy`는 10분·60웨이브 데이터다. Lobby 메타데이터 변경은 전투 일정 변경을 자동으로 만들지 않는다.
- 실제 쉬움 전투가 5분이 되려면 Spawn 시간표, 보스 출현과 관련 검증값을 별도 밸런스 작업에서 함께 변경해야 한다. 그 전까지 Battle은 기존 10분 스폰 데이터를 사용한다.

## 29. Lobby 도감과 공용 조작 프리팹 아키텍처

### 29.1 정적 Prefab 계층

```text
LobbyScreen.prefab
├─ LobbyBgm (AudioSource: Play On Awake, Loop, 2D)
├─ LobbyCodexPanel.prefab
│  ├─ EnemyContent
│  │  ├─ LobbyCodexEntry.prefab × 9
│  │  └─ LobbyCodexDetail.prefab
│  └─ SkillContent
│     ├─ LobbyCodexEntry.prefab × 9
│     └─ LobbyCodexDetail.prefab
└─ LobbySettingsPanel.prefab
   ├─ SettingsPage
   ├─ ControlSettingsButton
   └─ ControlSettingsPage
      └─ Assets/Prefab/UI/Shared/ControlSettingsPanel.prefab

PauseDetailsPanel.prefab
└─ Assets/Prefab/UI/Shared/ControlSettingsPanel.prefab
```

- 도감의 고정 창, 적·스킬 탭, 카드 18개와 상세 Overlay는 Prefab에 저장한다. 런타임에는 `LobbyCodexView`가 기존 참조의 활성 상태, 문구, Sprite와 Listener만 갱신한다. 조작·특성 탭과 콘텐츠는 도감 계층에 두지 않는다.
- Battle 일시정지 화면과 `LobbySettingsPanel.prefab`은 동일한 `ControlSettingsPanel.prefab`을 중첩 Prefab으로 사용한다. 조작 UI의 배치나 항목을 바꿀 때 공용 Prefab 하나를 수정하면 두 화면에 같은 구조가 반영된다.
- 공용화 최초 마이그레이션은 기존 `PauseDetailsPanel.prefab/ControlSettingsPanel`을 `SaveAsPrefabAssetAndConnect`로 추출한다. 기존 계층을 코드 기본값으로 재생성하지 않으며, 공용 Prefab이나 Pause Prefab이 이미 존재하면 `Ensure` 경로는 해당 자산을 덮어쓰지 않는다.
- 도감 바깥 터치와 X는 전체 창을 닫고, 상세 Overlay 터치는 상세만 닫는다. 창 내부의 일반 터치가 바깥 닫기 영역으로 전파되지 않도록 Window가 Raycast를 차단한다.

### 29.2 데이터 흐름과 페이지 정책

```text
EnemyBalance + EnemyAssetCatalog + GameStringTable
                         └─→ Enemy 3×3 Page ─→ Detail Overlay

LevelUpCardTable + GameStringTable
                         └─→ Skill 3×3 Page ─→ Detail Overlay
```

- 적 카드는 `EnemyBalance`의 정의 순서와 `EnemyAssetCatalog` Sprite를 결합한다. 설명은 적 ID별 GameString Key로 조회하며 현재 8종 뒤의 1개 슬롯은 비활성 빈 칸이다.
- 스킬 카드는 기본 스킬 6종과 융합 3종을 고정 순서로 보여준다. 이기어검은 두 성장 카드 대신 하나의 도감 항목으로 통합한다. 현재 스킬 Icon 자산이 없으므로 카드와 상세 이미지 참조를 비워 둔다.
- 페이지 단위는 9개이며 좌우 버튼은 페이지 경계를 넘지 못한다. 미래 항목 추가 시 정적 Slot을 재사용해 다음 페이지 데이터만 바인딩한다.
- 탭명, 페이지 표기, 통합 이기어검 문구와 적 설명은 `GameStringTable`에서 읽어 Excel 재임포트만으로 교체할 수 있다.

### 29.3 Lobby 조작 설정 상태

- `LobbySettingsView`는 상단 설정 버튼으로 별도 설정 창을 열고, 내부 조작 버튼으로 `SettingsPage`와 `ControlSettingsPage`를 전환한다. 이 구조는 Battle Pause의 기본 설정 화면↔조작 설정 화면 전환과 같다.
- `LobbyControlSettingsView`는 `MobileControlSettingsStore`의 적용값과 별도의 편집 초안을 유지한다. 조작 화면에서 기본 설정 화면으로 돌아가도 초안은 남고, 적용 버튼을 눌렀을 때만 저장한 뒤 기본 화면으로 돌아간다.
- 설정 창 전체를 적용 없이 닫으면 초안을 버린다. 다음에 설정 창을 열면 마지막으로 적용한 모드, 자동 공격, 크기와 좌우 패드 위치를 다시 불러온다.
- 숨기기 모드에서는 미리보기 패드를 숨기며 모드 1·2로 돌아오면 마지막 적용 위치를 복원한다. 실제 인게임 조작 로직은 기존 `MobileControlSettingsStore` 소비 경로를 그대로 사용한다.

### 29.4 난이도 이미지 참조 구조

- `ImageData`는 이미지 ID와 `Assets/Image` 아래의 파일명을 연결하며, Import 결과인 `ImageDataTable`은 런타임 파일 검색 대신 Sprite 직접 참조를 가진다.
- `LobbyDifficulty.ImageId`는 기존 전투 대표 이미지(`LobbyDifficulty_Easy/Normal/Hard.png`) 전용이다.
- `LobbyDifficulty.SelectedDifficultyImageId`는 현재 선택 난이도 표시 이미지(`Easy_Text/Normal_Text/Hard_Text.png`) 전용이다.
- `LobbyDifficulty.SelectedDifficultyImageScale`은 하나의 `SelectedDifficultyImage`가 Sprite를 교체하는 구조에서도 난이도별 시각 크기를 다르게 보정한다. `LobbyView`는 선택마다 `RectTransform.localScale`에 균일 배율을 적용하고 선택 해제 시 `Vector3.one`으로 복원한다.
- `LobbyView`는 난이도 선택 시 두 ID를 각각 `RepresentativeImage`, `SelectedDifficultyImage`에 적용하고 선택 해제 시 두 Sprite 참조와 Image 표시를 모두 해제한다.
- 사용자가 수정한 Lobby UI를 보호하기 위해 일반 `Build()`는 기존 Prefab을 재생성하지 않는다. 누락된 `SelectedDifficultyImage` 마이그레이션도 해당 자식과 직렬화 참조만 보완하며 기존 RectTransform과 다른 UI 계층은 보존한다.

### 29.5 Lobby 음악과 작업 파일 안정성

- `LobbyBgm`은 `LobbyScreen.prefab`에 직렬화된 정적 `AudioSource`다. `Play On Awake`, `Loop`, `spatialBlend=0`을 사용하고 `Lobby.unity/Main Camera`의 정적 `AudioListener`로 출력한다. 런타임 생성이나 전역 영속 오브젝트를 사용하지 않으므로 Lobby 진입과 함께 재생되고 Lobby 씬을 벗어나면 자연스럽게 정리된다.
- Lobby의 긴 MP3는 `AudioClipLoadType.Streaming`, `Load In Background=true`, `Preload Audio Data=false`로 Import한다. 압축 음원 전체를 시작 시점에 PCM으로 해제해 상주시키는 대신 재생 스트림을 사용한다.
- `LobbySceneBuilder.MigrateLobbyMusic`은 기존 UI 마이그레이션과 분리되어 `LobbyBgm`, 해당 AudioImporter 설정과 Lobby Main Camera의 누락된 `AudioListener`만 보완한다. 사용자가 편집한 난이도 이미지와 설정 패널 계층은 다시 생성하거나 이동하지 않는다.
- Excel 원본을 갱신할 때는 artifact-tool로 기존 `.xlsx`를 Import한 뒤 값·수식 snapshot과 오류 검사를 통과한 정상 export만 원본에 반영한다. Excel 소유자 파일 `~$*.xlsx`, `~$*.xlsm`과 일반 `.tmp`는 Git 추적에서 제외한다.
- Commit 전에는 활성 Git 프로세스와 `.git/index.lock`, `.git/HEAD.lock`, `.git/config.lock`, `.git/packed-refs.lock`을 확인한다. 잠금 파일은 활성 Git 작업이 없고 stale임이 확인된 경우에만 제거한다.
