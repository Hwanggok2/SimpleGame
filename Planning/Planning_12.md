# 5차 작업 결과 — 오물 다중 투척, 조작 패드 설정, 최적화 감사

## 1. 범위

이번 묶음은 다음 세 가지를 함께 처리한다.

1. `FILTH_THROW` 레벨이 오를 때 한 번에 던지는 구체 수도 증가한다.
2. Pause 설정에서 좌측 조준 패드와 우측 공격 버튼을 함께 켜고 끌 수 있다.
3. 기능 증가로 비용이 커지는 경로를 먼저 최적화하고, 프로젝트 전체의 다음 최적화 우선순위를 정리한다.

요청된 `포니테일` 스킬은 현재 Codex 스킬 목록과 저장소 어디에도 존재하지 않아 실행할 수 없었다. 대신 작은 변경, 명시적 수용 기준, 측정 가능한 최적화를 우선하는 보수적 리팩터링 지침으로 진행했다.

## 2. 오물 투척 확정안

| 레벨 | 동시 투척 수 | 틱 피해 배율 | 반경 | 재사용 |
|---:|---:|---:|---:|---:|
| 1 | 1 | 0.35 | 1.20 | 6.0초 |
| 2 | 2 | 0.45 | 1.32 | 5.5초 |
| 3 | 3 | 0.55 | 1.44 | 5.0초 |
| 4 | 4 | 0.65 | 1.56 | 4.5초 |
| 5 | 5 | 0.75 | 1.68 | 4.0초 |

- 한 번의 쿨다운에 현재 레벨 수만큼 같은 프레임에 투척한다.
- 각 구체는 화면 안의 살아 있는 Enemy 중 서로 독립된 무작위 대상을 뽑고, 선택 순간 위치로 날아간다.
- 화면 안에 살아 있는 Enemy가 없으면 쿨다운을 소비하지 않고 다시 시도한다.
- 구체와 장판의 겹침을 허용하며, 겹친 장판 피해도 각각 적용한다.
- 기존 0.45초 포물선, 3초 장판, 0.5초 간격 6틱 규칙은 유지한다.
- 원본 엑셀 `LevelUpCard` 설명, `CardMath`의 `오물 투척 수` 수식 열, README와 BalanceSummary를 함께 갱신한다.

## 3. 조작 패드 설정

- `PauseDetailsPanel.prefab` 하단에 기본 ON인 `조작 패드 표시` Toggle을 둔다.
- OFF는 `AimJoystickControl`을 먼저 비활성화한다. 따라서 `OnDisable` 경로가 Pointer 소유권, 정규화 입력, Knob, 월드 레이와 끝점을 즉시 해제한다.
- 이어서 우측 공격 버튼을 숨긴다. 이미 발행된 이동·공격 명령과 기존 월드 직접 터치는 취소하거나 비활성화하지 않는다.
- ON은 조이스틱과 공격 버튼을 모두 복구한다.
- 설정은 현재 실행 중인 한 판에만 유지하며 앱 재실행 이후까지 저장하지 않는다.

## 4. 이번에 적용한 최적화

### 4.1 오물 장판 비할당 반경 수집

기존 장판은 0.5초마다 전체 Enemy 후보 List와 결과 List를 새로 만들고, 모든 대상을 공격하면서도 거리 정렬을 수행했다. 레벨 5는 한 번에 장판이 최대 5개라 이 비용이 그대로 다섯 배가 된다.

`EnemyWorldService.FillEnemiesInRadius(center, radius, output)`가 호출자가 보유한 List를 비우고 살아 있는 범위 대상만 채우도록 바꿨다. `FilthProjectile`은 자신의 List 하나를 전체 수명 동안 재사용한다.

### 4.2 Enemy 충돌 반경 컴포넌트 캐시

지상 Enemy 분리는 움직이는 모든 Enemy가 매 프레임 전체 Enemy를 두 번 훑는다. 각 비교에서 반복되던 `GetComponent<CircleCollider2D>`를 `EnemyBase.CollisionRadius`의 캐시로 제거했다. Scale을 반영한 최종 반경 계산은 유지하므로 크기 변경 동작은 보존한다.

### 4.3 Animator 중복 쓰기와 불필요한 Update 제거

`CharacterSpriteAnimator`는 Motion과 FaceLeft의 마지막 값을 캐시하고 실제 값이 바뀔 때만 `Animator.SetInteger/SetBool`을 호출한다. 색상 Pulse 속도가 0인 일반 캐릭터는 이 Adapter의 `LateUpdate`를 비활성화한다. 외부에서 호출하는 이동·공격·피격·사망 메서드는 그대로 사용할 수 있다.

## 5. 프로젝트 구조 감사

현재 10분 Spawn 원본은 3,283개, 평균 약 5.48개/초, 순간 최대 15개/초이며 활성 Enemy 상한은 없다. 따라서 코드 줄 수 자체보다 Enemy 수 증가에 대한 확장성이 우선이다.

| 우선순위 | 영역 | 현재 문제 | 권장 구조 |
|---:|---|---|---|
| 1 | Enemy 분리 | 이동 Enemy마다 2-pass 전체 순회, O(N²) | `EnemyWorldService` 내부 Uniform Spatial Hash |
| 2 | Enemy UI·애니메이션 | Enemy별 World Space Canvas·Slider·TMP, AlwaysAnimate | 보스 상시/일반 피격 시 HP 표시, Sprite HP Bar, Animator Culling |
| 3 | 전투 이펙트 | 정전기 Arc마다 GameObject·LineRenderer·Material 생성/파괴 | 정전기 Arc → 오물 → 이동 참격 순 Pool |
| 4 | `PlayerCombatAbilities` | 카드 상태·일반 공격·여러 스킬·쿨다운·Spawn이 한 클래스에 집중 | MonoBehaviour 하나 아래 일반 C# Ability 모듈 |
| 5 | `FlyingSwordController` | 슬롯 상태·이동·판정·시각 생성 혼합 | 순수 Slot Model과 Renderer/Collision 분리 |
| 6 | `PrototypeGameSession` | Run 상태·카드·보상·점수·Pause 집중 | `RunState`, `CardSelectionService`, `RewardService` |
| 7 | Editor Builder | Character/Scene Builder가 각각 대형 파일이고 반복 생성 코드 존재 | Character·Effect·World, Scene·HUD Builder로 분리 |

Spatial Hash, 중앙 AI Tick, Physics2D Collider 제거는 영향 범위가 넓어 이번에 함께 넣지 않는다. 기존 brute-force 결과와 무작위 배치 비교 테스트, Enemy 100/300/500마리 Stress 측정 후 단계적으로 교체한다.

## 6. 검증 결과

- `dotnet build SimpleGame.sln --no-restore`: 오류 0.
- Unity EditMode: 288/288 통과.
- Play smoke: `FILTH_THROW` 5레벨에서 같은 발동에 `FilthProjectile` 5개가 서로 다른 위치에 존재함을 확인.
- Pause Toggle smoke: OFF에서 좌·우 Control 비활성, ON에서 둘 다 복구.
- 엑셀 11개 시트를 모두 렌더링하고 핵심 값·수식 및 Formula Error 0건을 확인.
