# Planning_8

- 문서 버전: 8.0
- 작성일: 2026-07-28
- 기준 버전: [Planning_7.md](Planning_7.md)
- 기능 기준: [FunctionalSpecification.md](FunctionalSpecification.md)
- 상태: 구현 반영 완료

## 1. 버전 관리 규칙

- 이 문서는 v7 전체 사양을 상속하고 UI 프리팹과 생명주기 변경만 정의한다.
- `Planning_7.md`와 이전 문서는 당시 결정 기록으로 유지한다.
- 다음 기획 변경은 `Planning_9.md`로 작성한다.

## 2. UI 생명주기

UI는 표시 시점에 따라 다음 두 종류로 나눈다.

| 구분 | UI | 배치 방식 |
|---|---|---|
| 상시 | 시간, 경험치 Bar, HP, 안내 문구 | `PrototypeHUD.prefab`을 씬에 1개 배치 |
| 일시 | 레벨업 카드 선택 | `CardSelectionPanel.prefab`을 레벨업 시 생성 |
| 일시 | ESC 상세 정보 | `PauseDetailsPanel.prefab`을 일시정지 시 생성 |
| 일시 | 게임오버와 이어하기 | `GameOverPanel.prefab`을 사망 시 생성 |

일시 UI는 씬 파일에 미리 배치하지 않는다. 첫 표시 요청 시 `ModalRoot` 아래에 생성하고 이후에는 비활성 상태로 재사용한다.

## 3. 씬 정리

다음 UI는 씬과 상시 HUD 프리팹에서 제거한다.

- `DebugButtons`
- `Pause` 버튼: ESC 입력으로 대체
- `DamagePlayer`, `GrantXp`: 개발용 공개 메서드는 유지하되 화면 버튼은 제거
- `Score`, `PlayerLevel`, `CriticalChance`: ESC 상세 정보에서만 표시
- 씬에 미리 배치된 `CardSelectionPanel`, `PauseDetailsPanel`, `GameOverPanel`

`ContinueAd`는 디버그 버튼 영역에서 `GameOverPanel.prefab` 내부로 이동한다.

## 4. 프리팹 구성

```text
PrototypeHUD.prefab
├─ TopPanel
│  ├─ Time
│  ├─ PlayerHp
│  └─ ExperienceBar
├─ HintPanel
└─ ModalRoot

CardSelectionPanel.prefab
├─ CardTitle
└─ CardChoice0~2
   └─ LevelUpCard.prefab

PauseDetailsPanel.prefab
└─ PauseDetails

GameOverPanel.prefab
├─ GameOverTitle
└─ ContinueAd
```

## 5. 데이터와 입력 흐름

`PrototypeHUDPresenter`는 게임 상태를 `PrototypeHUDView`에 전달한다. View는 일시 UI의 프리팹 참조와 버튼 Callback을 보관한다.

1. 상태 이벤트가 발생한다.
2. View가 대응하는 프리팹 인스턴스를 확인한다.
3. 없으면 `ModalRoot` 아래에 생성한다.
4. 최신 Text와 카드 데이터를 적용한다.
5. UI를 활성화한다.
6. 닫을 때는 비활성화하고 다음 호출에서 재사용한다.

카드 선택의 0.7초 입력 잠금과 ESC의 `TimeScale=0` 동작은 기존 사양을 유지한다.

## 6. 완료 조건

- 씬의 `PrototypeHUD`가 프리팹 인스턴스다.
- 씬 파일에 일시 UI와 `DebugButtons`가 없다.
- 상시 HUD에는 시간, 경험치, HP, 안내 문구만 존재한다.
- 레벨업 시 카드 선택 프리팹과 카드 3장이 생성된다.
- ESC 입력 시 Pause 프리팹이 생성되고 현재 스킬 정보가 표시된다.
- 사망 시 GameOver 프리팹이 생성되고 이어하기 버튼이 동작한다.
- 모든 UI가 `Pretendard-Regular SDF`를 사용한다.
- EditMode 테스트와 Play Mode 전체 흐름 검증이 통과한다.
