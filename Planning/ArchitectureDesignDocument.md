# SimpleGame 프로젝트 아키텍처 설계서

- 최종 갱신: 2026-07-27

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

### 6.2 Enemy 파생 클래스

| 클래스 | 차별화되는 핵심 규칙 |
|---|---|
| `MeleeEnemy` | 근거리 레벨 차이 공격 규칙과 근접 행동 |
| `RangedEnemy` | 발사 전 자유 조준, 발사 후 1초 방향 고정과 2초 쿨타임 |
| `ShieldEnemy` | Player 추격, 하늘색 범위 끝 정지, 0.8초 Shield 방향 고정, 조건부 정면 반동 |
| `BossEnemy` | Player 목표 유지, 고유 3초 공격 주기, 일반 Enemy 재배치 제외 |

파생 클래스에는 해당 Enemy에서만 달라지는 규칙만 둔다.

### 6.3 Enemy 기능 컴포넌트

| 컴포넌트 | 책임 |
|---|---|
| `EnemyHealth` | 현재 체력 또는 남은 피해량, 피격, 사망 |
| `EnemyMovement` | 목표 위치 이동, 정지 거리 판정, 이동 재개 |
| `EnemyTargeting` | 생존한 Player 목표 유지 |
| `EnemyFacing` | 바라보는 방향, 0.5초 좌우 전환 지연, 정면 및 후면 판정 |
| `EnemyAttackBase` | 공격 가능 여부, 준비, 판정, 쿨타임 |
| `EnemyStateMachine` | 행동 상태와 전환 |
| `EnemyVisual` | SpriteRenderer, 피격 Flash, 사망 연출 |
| `CharacterSpriteAnimator` | 저장된 AnimatorController에 Motion·FaceLeft·Attack·Hurt 파라미터 전달 |

### 6.4 공격 Strategy

```text
EnemyAttackBase
├─ MeleeAttack
├─ RangedAttack
└─ BossAttack
```

`RangedAttack`은 다음을 담당한다.

- 공격 범위 확인
- 공격 준비
- 투사체 생성 또는 공격 판정
- 공격 취소
- 쿨타임

`RangedEnemy`는 원거리 공격 세부 구현을 알 필요 없이 `EnemyAttackBase`의 공통 API를 사용한다.

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

Player와 Enemy의 Animation·Animator 자산은 `Assets/Game/Characters` 아래에서 관리한다. 프로젝트의 모든 Prefab은 `Assets/Prefab`에 모으고 원본 Sprite Sheet만 `Assets/Resources`에 유지한다.

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

Assets/Resources/
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
└─ EnemyWorldRecycler
```

- `CameraFollowController`가 Player 실제 월드 좌표를 추적한다.
- `CameraShakeController`는 추적 위치 위에 일시적인 오프셋만 합성하고 종료 시 추적 위치로 복귀한다.
- `WorldChunkGrid`는 Player가 속한 청크를 중심으로 3×3 좌표를 유지한다.
- `WorldChunk` 9개는 삭제·생성하지 않고 멀어진 행·열을 진행 방향 앞으로 재배치한다.
- 지형 원본은 현재 4종 Tile을 사용하며 활성 인스턴스 수와 원본 종류 수를 구분한다.
- `PlayerWorldArea`는 카메라보다 큰 Spawn 경계와 그보다 큰 재배치 경계를 계산한다.
- `EnemyWorldRecycler`는 `GameSession`에 등록된 일반 Enemy만 검사하고 청크와 독립된 `EnemyRoot` 안에서 반대편 Spawn 경계로 재배치한다.
- Tag 문자열은 보조 필터로만 사용할 수 있으며 청크·Enemy 재배치 책임은 명시적인 컴포넌트가 소유한다.

이어하기 처리 흐름:

```text
GameSession
  ↓ Continue 승인
PlayerRoot.RestoreAfterContinue(MaxHP)
  ↓
EnemyWorldRecycler.RepositionAllNormalEnemies()
  ↓
Playing 복귀
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
  └─ LevelUpCard
          ↓ 가져오기·검증
Assets/Game/Data/Generated/*.asset
          ↓
GameDataManifest
          ↓
GameSession / StageSpawnController / EnemyFactory

Unity 수동 설정
  ├─ EnemyAssetCatalog (EnemyId ↔ Prefab)
  └─ CombatFeedbackProfile (화면 흔들림)
          ↓
GameDataManifest

PrototypeScene
  ├─ SpawnPointRegistry (Player 기준 SpawnPointId ↔ Transform)
  ├─ WorldChunkGrid (3×3 Tilemap)
  └─ PlayerWorldArea (Spawn/재배치 경계)
```

### 15.1 자동 생성 ScriptableObject

`Assets/Game/Data/Generated` 아래 에셋은 Excel 값의 Unity 런타임 표현이다.

- `EnemyBalanceTable`: EnemyId별 전투 수치, 보상과 Archetype
- `StageSpawnSchedule`: StageId, WaveId, 시간, 순번, SpawnPointId, EnemyId, 레벨
- `PlayerLevelExperience`: 플레이어 레벨별 다음 레벨 필요 EXP
- `AccountLevelExperience`: 계정 레벨별 다음 레벨 필요 EXP
- `GlobalBalance`: 점수→계정 EXP 환산식과 치명타 공통값
- `PlayerBalanceTable`: Player 기본 HP·공격력·성장률·기본 이동 속도·상황별 속도 배율·도착 허용 거리·사거리
- `LevelUpCardTable`: 카드 효과, 중첩, 가중치, 최소 등장 레벨과 활성 여부

이 에셋들은 수동 편집하지 않고 `Planning/GameData.xlsx` 가져오기 결과로만
취급한다. Excel 저장 후 Unity 메뉴 `SimpleGame > Data > Import Excel`을
실행하면 Editor 전용 importer가 7개 필수 시트를 모두 메모리에서 검증한 뒤
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

## 17. 권장 폴더 구조

```text
Assets/Game/
├─ Runtime/
│  ├─ Core/
│  ├─ Combat/
│  ├─ Player/
│  ├─ Enemies/
│  │  ├─ Common/
│  │  ├─ Melee/
│  │  ├─ Ranged/
│  │  ├─ Shield/
│  │  └─ Boss/
│  ├─ World/
│  ├─ Spawning/
│  ├─ Progression/
│  ├─ Presentation/
│  ├─ UI/
│  ├─ Save/
│  └─ Infrastructure/
├─ Data/
│  ├─ Generated/
│  ├─ Catalogs/
│  └─ Profiles/
├─ Characters/
│  ├─ Animations/
│  ├─ Animators/
│  └─ Shared/
└─ Tests/
   ├─ EditMode/
   └─ PlayMode/
```

모든 `.prefab` 에셋은 종류와 관계없이 `Assets/Prefab` 바로 아래에서 관리한다.

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
