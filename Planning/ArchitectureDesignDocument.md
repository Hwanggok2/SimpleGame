# SimpleGame 프로젝트 아키텍처 설계서

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
├─ Castle
│  └─ CastleRoot 및 기능 컴포넌트
└─ Enemies
   └─ EnemyBase 인스턴스

UI
├─ HUDView
├─ LevelUpView
├─ ContinueView
├─ GameOverView
└─ ClearView
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

`EnemyBase`, `PlayerRoot`, `CastleRoot`는 해당 Entity의 Facade이자 Coordinator다.

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
| `RangedEnemy` | 원거리 공격 취소와 플레이어 타깃 전환 |
| `ShieldEnemy` | 경로 차단, 정면 조작 불가, 직접 공격 없음 |
| `BossEnemy` | Castle 목표 유지, 고유 공격 주기, 넉백 저항 |

파생 클래스에는 해당 Enemy에서만 달라지는 규칙만 둔다.

### 6.3 Enemy 기능 컴포넌트

| 컴포넌트 | 책임 |
|---|---|
| `EnemyHealth` | 현재 체력 또는 남은 피해량, 피격, 사망 |
| `EnemyMovement` | 목표 위치 이동, 정지, 이동 재개 |
| `EnemyTargeting` | Castle 및 Player 목표 선택 |
| `EnemyFacing` | 바라보는 방향, 정면 및 후면 판정 |
| `EnemyAttackBase` | 공격 가능 여부, 준비, 판정, 쿨타임 |
| `EnemyStateMachine` | 행동 상태와 전환 |
| `EnemyVisual` | Animator, 피격 Flash, 사망 연출 |

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

정면, 후면, 레벨 차이와 치명타 판정은 Unity 컴포넌트와 분리된 순수 C# 클래스로 구현한다.

입력:

- Enemy 종류
- 플레이어 레벨
- Enemy 레벨
- 정면 또는 후면
- 치명타 여부

출력:

- 피해 가능 여부
- 적용 피해량
- 남은 필요 타격 수
- Enemy 행동 취소 가능 여부

이 클래스는 기획서의 타격 횟수 표를 기준으로 Edit Mode 테스트를 작성한다.

## 7. Enemy 상태 머신

일반 Enemy의 기본 상태:

```text
Spawn
  ↓
MoveToCastle
  ├─ Castle이 사거리 안 → AttackCastle
  ├─ Player에게 공격받음 → ChasePlayer
  └─ 사망 → Dead

ChasePlayer
  ├─ Player가 사거리 안 → AttackPlayer
  ├─ Player 사망 → MoveToCastle
  └─ 사망 → Dead

AttackPlayer
  ├─ 공격 종료 → ChasePlayer
  ├─ Player 사망 → MoveToCastle
  └─ 사망 → Dead
```

보스의 공격 상태:

```text
MoveToCastle
  ↓
AttackTelegraph 1.5초
  ↓
AttackActive 0.5초
  ↓
AttackRecovery 1초
  ↓
MoveToCastle
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
| `PlayerTargetSelector` | 터치 보정, 우선순위와 방패병 경로 검사 |
| `PlayerHealth` | 피격, 사망, 부활 |
| `PlayerLevel` | 인게임 경험치와 레벨 |
| `CriticalSystem` | 치명타 확률과 Roll |
| `PlayerStateMachine` | 이동, 공격, 조작 불가, 사망 상태 |

## 9. Castle 구조

```text
Castle
├─ CastleRoot
├─ CastleHealth
├─ CastleInvincibility
└─ CastleVisual
```

`CastleRoot`는 게임 오버와 광고 이어하기에 필요한 공통 API를 제공한다.

이어하기 처리 흐름:

```text
GameSession
  ↓ Continue 승인
CastleRoot.Restore(50%)
  ↓
CastleInvincibility.Activate(3초)
  ↓
PlayerRoot.Respawn(MaxHP)
  ↓
EnemyKnockbackService.PushToMapBounds()
```

광고 SDK의 성공 여부는 `GameSession` 또는 별도 광고 서비스가 처리하고 Castle은 광고 시스템을 직접 알지 않는다.

## 10. 이벤트와 직접 호출 기준

### 10.1 직접 호출

반드시 순서대로 실행되어야 하는 내부 행동은 직접 호출한다.

```text
StateMachine → Movement.Stop()
StateMachine → Attack.Execute()
PlayerCombat → CombatResolver.Resolve()
EnemyBase → Health.ApplyDamage()
```

### 10.2 이벤트

여러 외부 시스템에 알려야 하는 상태 변화만 이벤트로 발행한다.

- `HealthChanged`
- `Died`
- `TargetChanged`
- `PlayerLevelChanged`
- `CriticalChanceChanged`
- `ScoreChanged`
- `CastleDestroyed`
- `GameOver`

예:

```text
EnemyHealth.Died
├─ EnemyVisual
├─ ScoreSystem
├─ ExperienceSystem
└─ PoolService
```

전역 범용 EventBus는 사용하지 않는다. 발행자와 구독자를 쉽게 추적할 수 있는 타입이 명확한 C# 이벤트를 사용한다.

## 11. Definition과 런타임 상태

### 11.1 읽기 전용 Definition에 저장할 값

```text
EnemyDefinition
├─ EnemyType
├─ MoveSpeed
├─ AttackRange
├─ AttackDamage
├─ AttackCooldown
├─ Score
├─ Experience
└─ Prefab
```

`EnemyDefinition`은 게임 코드에서 사용하는 읽기 전용 설정 모델이다. 실제 저장 형태는 초기 프로토타입에서는 ScriptableObject일 수 있고, Excel 데이터 파이프라인이 완성된 뒤에는 `.bytes`를 읽어 생성한 런타임 데이터일 수 있다.

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

UI는 `View + Presenter + enum 인덱스 Registry` 방식으로 구성한다.

```text
GameSession
  ↓ 상태 이벤트
HUDPresenter
  ↓ 표시 요청
HUDView
  ↓ enum ID 조회
Button/TMP_Text
```

### 13.1 enum 규칙

화면마다 별도의 enum을 선언한다.

```csharp
public enum HudButtonId
{
    Pause = 0,
    Count = 1
}

public enum HudTextId
{
    Score = 0,
    PlayTime = 1,
    PlayerLevel = 2,
    CriticalChance = 3,
    Count = 4
}
```

`Button`이라는 enum 이름은 `UnityEngine.UI.Button`과 충돌하므로 사용하지 않는다.

`Count`는 배열 크기를 계산하기 위한 Sentinel이며 실제 UI GameObject와 연결하지 않는다. 자동 바인딩과 누락 검증에서도 `Count`는 제외한다.

### 13.2 Hierarchy 이름 규칙

UI GameObject 이름은 해당 enum 항목 이름과 동일하게 작성한다.

```text
HUD
├─ Buttons
│  └─ Pause
└─ Texts
   ├─ Score
   ├─ PlayTime
   ├─ PlayerLevel
   └─ CriticalChance
```

### 13.3 자동 바인딩

View 초기화 시 다음 순서로 Button과 TMP_Text를 수집한다.

1. 자식 Button 또는 TMP_Text 컴포넌트를 한 번 수집한다.
2. GameObject 이름을 대응하는 enum으로 변환한다.
3. enum 숫자를 배열 인덱스로 사용한다.
4. 해당 인덱스에 컴포넌트를 저장한다.
5. 중복, 누락, 잘못된 이름과 null을 검증한다.

```text
buttons[(int)HudButtonId.Pause] → Pause Button
texts[(int)HudTextId.Score] → Score TMP_Text
```

`GetComponentsInChildren` 결과 순서를 그대로 enum 인덱스와 연결하지 않는다. GameObject 이름을 enum으로 변환한 후 명시적인 인덱스에 저장한다.

### 13.4 View 사용 API

사용 코드는 다음 형태를 목표로 한다.

```csharp
Bind(HudButtonId.Pause, OnPauseClicked);
SetText(HudTextId.Score, score.ToString());
SetText(HudTextId.PlayerLevel, $"Lv. {level}");
```

enum 값 자체에 함수를 연결하는 것이 아니라 View의 `Bind()`가 enum 인덱스로 Button을 찾아 Listener를 등록한다.

### 13.5 UI 바인딩 검증

초기화 단계에서 다음 오류를 확인한다.

- enum에 대응하는 GameObject 누락
- 동일한 enum ID에 두 컴포넌트가 연결됨
- GameObject 이름을 enum으로 변환할 수 없음
- 잘못된 컴포넌트 타입
- 배열의 필수 인덱스가 null

오류를 조용히 무시하지 않고 화면 이름과 누락된 enum ID를 포함한 오류를 출력한다.

### 13.6 Listener 생명주기

- View가 활성화될 때 이벤트를 연결한다.
- View가 비활성화될 때 자신이 연결한 이벤트만 해제한다.
- View를 다시 열 때 중복 Listener가 등록되지 않도록 한다.
- Inspector에서 등록한 Listener가 있을 수 있으므로 모든 Listener를 일괄 제거하지 않는다.

### 13.7 Presenter

UI Registry는 게임 로직을 직접 호출하지 않는다.

```text
Pause Button
  ↓ HUDView
HUDPresenter.OnPauseClicked()
  ↓
GameSession.Pause()
```

게임 시스템도 enum Registry를 직접 사용하지 않는다.

권장:

```csharp
hudView.SetScore(score);
hudView.SetPlayerLevel(level);
```

`HudView` 내부에서만 다음과 같이 enum Registry를 사용한다.

```csharp
SetText(HudTextId.Score, score.ToString());
```

### 13.8 반복 UI

레벨업 카드처럼 같은 구조가 반복되는 UI는 enum 항목을 세 개 만들지 않는다.

권장:

```text
CardViews[0]
CardViews[1]
CardViews[2]
```

서로 역할이 다른 고정 UI는 enum으로 관리하고 동일 구조의 반복 항목은 배열 또는 리스트로 관리한다.

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

Enemy 및 Wave 데이터의 원본은 Excel로 관리할 예정이다.

```text
Excel
  ↓ 변환
검증 단계
  ↓
.bytes 또는 자동 생성 런타임 데이터
  ↓
DataRepository
  ↓
WaveSpawner / EnemyFactory
```

Excel, `.bytes`와 ScriptableObject를 동시에 수동 수정하지 않는다. 하나를 원본으로 정하고 나머지는 자동 생성 결과로 취급한다.

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
│  ├─ Castle/
│  ├─ Spawning/
│  ├─ Progression/
│  ├─ UI/
│  ├─ Save/
│  └─ Infrastructure/
├─ Data/
│  ├─ Enemies/
│  ├─ Cards/
│  └─ Waves/
├─ Prefabs/
└─ Tests/
   ├─ EditMode/
   └─ PlayMode/
```

### 17.1 Assembly Definition

초기에는 지나치게 세분화하지 않는다.

```text
SimpleGame.Runtime
SimpleGame.Tests.EditMode
SimpleGame.Tests.PlayMode
```

Editor 전용 도구가 추가되면 `SimpleGame.Editor`를 별도로 추가한다.

## 18. 테스트 기준

### 18.1 Edit Mode

Unity Scene 없이 검증할 규칙:

- 정면 및 후면 판정
- 원거리 및 근거리 레벨 차이 필요 타격 수
- 치명타 피해량
- 점수에서 계정 경험치 변환
- 인게임 필요 경험치
- 터치 타깃 우선순위
- Wave 데이터 검증

### 18.2 Play Mode

GameObject와 시간 흐름이 필요한 규칙:

- 공격 위치까지 이동한 후 피해 적용
- 방패병 경로 차단
- Enemy 목표 변경
- 플레이어 사망 및 부활
- Castle 광고 이어하기
- 보스 3초 공격 주기
- Object Pool 재사용
- UI enum 자동 바인딩과 Listener 중복 방지

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
| Castle을 무시하고 Player만 추적 | Targeting Strategy 또는 파생 Enemy 검토 |
| 다단계 고유 상태를 가진 보스 | `BossEnemy` 및 전용 상태 머신 |

## 21. 구현 순서

1. 폴더와 최소 Assembly Definition 구성
2. 순수 C# `CombatResolver`와 테스트
3. `EnemyBase` 및 공통 기능 컴포넌트
4. 일반 Enemy 상태 머신
5. `MeleeEnemy`와 `RangedEnemy`
6. Player 이동, 타깃 선택과 공격
7. `ShieldEnemy`
8. Castle과 게임 상태
9. 인게임 경험치와 치명타 카드
10. Boss 상태와 공격 주기
11. UI View, Presenter와 enum 자동 바인딩
12. Factory, Object Pool과 WaveSpawner
13. 저장, 계정 성장과 광고 연결

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
