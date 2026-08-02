# 2026-08-03 작업 기록

## Lobby 선택 난이도 이미지 개별 크기 보정

- UI `Image`가 고정 `RectTransform`을 사용해 Sprite의 Pixel Per Unit 변경만으로는 표시 크기가 달라지지 않는 원인을 확인했다.
- `LobbyDifficulty` 시트에 `SelectedDifficultyImageScale` 열을 추가했다.
- 초기 배율은 Easy `0.90`, Normal `1.10`, Hard `1.00`으로 설정했다.
- `LobbyDifficultyDefinition`과 Excel Importer가 배율을 저장하며 허용 범위는 0 초과 3 이하이다.
- `LobbyView`는 선택 난이도의 Sprite를 적용할 때 `SelectedDifficultyImage`의 `RectTransform.localScale`에 동일한 X/Y 배율을 적용한다.
- 현재 선택을 해제하면 이미지 Sprite와 표시 상태를 해제하고 배율을 `Vector3.one`으로 복원한다.
- 이후에는 Excel의 배율 숫자만 수정하고 데이터 Import를 다시 실행해 각 이미지 크기를 독립적으로 조정할 수 있다.

## Lobby 배경음악과 파일 잠금 정리

- `Assets/Music/harumachimusic-pastorale-idyllic-irish-harp-294840.mp3`를 `LobbyScreen.prefab/LobbyBgm`의 정적 `AudioSource`에 연결했다.
- `Play On Awake`, `Loop`, 2D 설정으로 Lobby 진입 즉시 재생하고 Lobby에 머무는 동안 반복한다. Battle로 이동하면 Lobby 씬 오브젝트와 함께 종료한다.
- 긴 배경음악의 메모리 부담을 줄이기 위해 MP3 Import를 `Streaming`, `Load In Background`, Preload Off로 변경했다.
- 기존 Legacy UI 마이그레이션이 사용자가 옮긴 난이도 이미지 계층을 보정할 수 있음을 검증 단계에서 발견했다. 해당 결과는 원본에 반영하지 않고, 음악만 추가하는 `MigrateLobbyMusic` 경로를 분리해 기존 Lobby UI를 보존했다.
- Lobby EditMode 테스트는 14개 전부 통과했다. 사용자가 옮긴 `SelectedDifficultyImage`의 위치를 허용하도록 테스트도 고정 경로 대신 이름 기반 조회로 변경했다.
- Git 활성 프로세스와 `.git/index.lock`, `.git/HEAD.lock`, `.git/config.lock`, `.git/packed-refs.lock`을 확인했으며 잠금 파일은 없었다. Excel 소유자 파일이 실수로 Commit되지 않도록 `~$*.xlsx`, `~$*.xlsm`을 `.gitignore`에 추가했다.
- `Planning/GameData_10min_Balance.xlsx`를 artifact-tool로 열어 15개 시트를 렌더링하고 수식 오류를 검사했다. 정상 export를 다시 열어 비교한 결과 값과 수식 snapshot이 일치했고 오류 셀은 0개였다.
- 검증된 정상 `.xlsx`로 원본을 교체하고, 2026-07-25에 남은 `Planning/2927e490-1763-485a-afdc-759a46a21c45.tmp` 임시 export 아카이브를 제거했다. Excel 소유자 파일은 0개임을 다시 확인했다.

## Lobby BGM 무음 수정

- `LobbyBgm/AudioSource`의 Clip, 볼륨, Loop와 프로젝트 오디오 활성 상태는 정상이었지만 `Lobby.unity`에 활성 `AudioListener`가 없어서 출력되지 않는 것을 확인했다.
- `Lobby.unity/Main Camera`에 정적 `AudioListener`를 추가했다. 새 Lobby 씬 생성 경로도 처음부터 Listener를 포함하도록 수정했다.
- 음악 전용 마이그레이션은 기존 씬에서는 Main Camera의 Listener만 추가하고 Lobby UI 프리팹은 변경하지 않는다.
- Lobby EditMode 테스트에 Main Camera Listener 검증을 추가했으며 14개 전부 통과했다. C# 빌드 오류는 0개다.
