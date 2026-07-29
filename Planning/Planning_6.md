# Planning_6

- 문서 버전: 6.0
- 작성일: 2026-07-27
- 기준 버전: [Planning_5.md](Planning_5.md)
- 기능 기준: [FunctionalSpecification.md](FunctionalSpecification.md)
- 상태: 구현 반영 완료

## 1. 버전 관리 규칙

- `Planning_5.md`와 그 이전 문서는 당시 결정 기록으로 유지한다.
- 이 문서는 v5 전체 사양을 상속하고 레벨업 카드 UI의 프리팹화 변경만 정의한다.
- 다음 기획 변경은 `Planning_7.md`로 작성한다.

## 2. 변경 목적

기존 레벨업 카드 3장은 `PrototypeSceneBuilder`가 각각 별도 Button 오브젝트로 생성했다. 카드 모양과 폰트를 한 에셋에서 관리할 수 있도록 공통 프리팹으로 전환한다.

## 3. 레벨업 카드 프리팹

- 에셋 경로: `Assets/Prefab/LevelUpCard.prefab`
- 루트 오브젝트: `LevelUpCard`
- 필수 컴포넌트: `RectTransform`, `CanvasRenderer`, `Image`, `Button`
- 필수 자식: `Label`
- 텍스트 컴포넌트: `TextMeshProUGUI`
- 기본 폰트: `Pretendard-Regular SDF`
- 기본 크기: `230×250`
- 기본 배경색: RGBA `(0.12, 0.42, 0.62, 0.96)`

## 4. 씬 배치 규칙

`CardSelectionPanel` 아래에 같은 프리팹을 3개 배치한다.

| 인스턴스 이름 | X 위치 | Y 위치 | 런타임 선택 인덱스 |
|---|---:|---:|---:|
| `CardChoice0` | 20 | -245 | 0 |
| `CardChoice1` | 260 | -245 | 1 |
| `CardChoice2` | 500 | -245 | 2 |

인스턴스 이름은 `PrototypeHUDView`의 enum 자동 바인딩 규칙 때문에 유지한다. 각 인스턴스의 표시 문자열과 활성 상태는 기존처럼 런타임 데이터로 변경한다.

## 5. 생성 및 갱신

- `SimpleGame/Build Level Up Card Prefab`: 프리팹만 다시 생성한다.
- `SimpleGame/Update Card Selection UI`: 현재 PrototypeScene의 카드 3장을 프리팹 인스턴스로 교체하고 씬을 저장한다.
- `SimpleGame/Build Prototype Scene`: 프리팹 생성과 카드 인스턴스 배치를 모두 수행한다.

## 6. 완료 조건

- `LevelUpCard.prefab`이 독립 에셋으로 존재한다.
- 프리팹에 Button, Image, TMP Label과 Pretendard 폰트가 연결돼 있다.
- PrototypeScene의 `CardChoice0~2`가 모두 같은 프리팹을 참조한다.
- 기존 0.7초 입력 잠금과 카드 선택 콜백이 유지된다.
- Unity 빌드와 EditMode 테스트가 통과한다.
