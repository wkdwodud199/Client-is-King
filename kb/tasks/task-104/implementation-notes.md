# 구현 노트 — task-104

> Status: done
> Inputs: kb/tasks/task-104/design.md
> Outputs: 씬 2종(MainMenu/Shop) + 하루 상태 머신 런타임 + SceneBuilder + EditMode 테스트 13종 추가
> Next step: task-105 설계 요청 (`runtime/codex-design.ps1 task-105 --auto`) — 경제·인벤토리 + 장보기 UI + EditMode 테스트

## 설계 대비 변경 사항

| 항목 | 설계 내용 | 실제 구현 | 변경 사유 |
|------|-----------|-----------|-----------|
| 씬 멱등 방식 | "기존 빌더 소유 오브젝트를 정리하고 재생성" | 빈 씬 전체 재생성 후 같은 경로 저장 (.meta 유지 → GUID 안정) | 이 시점 두 씬은 전체가 빌더 소유 — 전체 재생성이 더 결정론적이고 설계의 "수동 수정 대신 빌더 재실행" 철학과 일치 |
| 이벤트 발행 주체 | (설계가 GameManager/머신 중 미확정) | 순수 C# `DayPhaseMachine.Advance()` 가 직접 `GameEvents` 발행 | 씬 없이 이벤트 계약까지 EditMode 테스트 가능 — GameManager 는 얇은 래퍼 유지 (설계 제약) |
| .gitkeep 정리 | (설계 미언급) | `Assets/Scenes/.gitkeep`, `Assets/Data/.gitkeep` 제거 | 실콘텐츠(씬/시드 데이터)가 생겨 목적 소멸. `Art/Placeholders/.gitkeep` 는 유지 |
| SceneTemplateSettings.json | (설계 미언급) | 추적에 포함 | 씬 저장 시 Unity 가 생성한 ProjectSettings 파일 — 재현성 위해 추적 |

## 구현 결정 기록

1. **상태 머신은 순수 C#** (`ClientIsKing.DayCycle`) — `DayPhaseMachine.Advance()` 가 전환·day 증가·이벤트
   발행을 모두 담당하는 단일 진입점. `Next(phase)` 정적 함수가 순서 규칙의 단일 원천.
   null state → `ArgumentNullException`, day<1 → 1 보정 (설계 3단계 그대로).
2. **GameEvents 발행권 제한**: `RaiseDayPhaseChanged` 는 internal — 같은 어셈블리(상태 머신)만 발행 가능,
   구독은 public event. payload 는 readonly struct (Day 는 +1 반영 후 값).
3. **부트스트랩 이중화**: 두 씬 모두 GameManager 포함 — 어느 씬에서 시작해도 동작.
   중복은 `Awake` 의 Instance 가드가 제거 (`DontDestroyOnLoad` 원본 유지).
4. **UI 참조 주입은 EditorInit 패턴 재사용** (task-103 과 동일) — private `[SerializeField]` 유지,
   SceneBuilder 가 internal 주입. 버튼 리스너는 컨트롤러가 런타임에 스스로 연결 (persistent listener 회피).
5. **HUD 초기 동기화는 `Start()`** — GameManager.Awake 이후 시점 보장. 이후 갱신은 이벤트 구독.
6. **asmdef 참조 추가**: Runtime/Editor += `UnityEngine.UI`, `Unity.TextMeshPro` (TMP=ugui 2.0 병합,
   task-102 결정 계승). Tests += `ClientIsKing.Editor` (SceneBuilder 를 테스트에서 직접 실행).
7. **픽셀 표준 적용**: Shop 카메라 = URP 내장 `PixelPerfectCamera` (PPU 32, 640x360) +
   `GetUniversalAdditionalCameraData()` 호출로 URP 카메라 데이터를 씬에 직렬화.
   Canvas 는 ScaleWithScreenSize 640x360 (HUD 도 픽셀 기준 해상도 정렬).

## 발생한 이슈

- 없음. 신규 코드 8파일 + asmdef 3건이 **첫 배치 실행에서 컴파일 통과** — task-102 에서 확인한
  URP 2D 어셈블리 분리를 선반영한 덕분. TMP 도 ugui 2.0 기본 리소스로 문제 없이 동작.

## 테스트 결과

| 테스트 기준 (design.md 참조) | 결과 | 비고 |
|------------------------------|------|------|
| validator rc0 (design.md) | pass | 러너 게이트 |
| 배치 SceneBuilder.Apply exit 0 | pass | `[SceneBuilder] scenes built + build settings locked` |
| 배치 컴파일 게이트 exit 0 + `error CS` 없음 | pass | 명시적 2차 실행 |
| EditMode 테스트 exit 0 | pass | **26/26 통과** (기준선 6 + 데이터 7 + 상태 머신 7 + 씬 6) |
| 상태 머신: Market→Service→Settlement→Night→Market + Night→Market 만 day+1 | pass | `DayPhaseMachineTests` |
| DayPhaseChanged 정확히 1회 + payload(day/prev/current) | pass | Night→Market 시 day+1 반영 payload 별도 검증 |
| 씬 2종 존재 + Main Camera/EventSystem/Canvas | pass | `SceneBuilderTests` |
| Shop 카메라 PixelPerfectCamera PPU 32 / 640x360 | pass | |
| Shop HUD: day/phase 텍스트 + 진행 버튼 + 패널 4종 | pass | 초기 활성 = Market 만 |
| Build Settings = MainMenu, Shop 순서 2개만 | pass | Assets 하위 씬 하드캡 2 테스트 포함 |
| `git status` 캐시/빌드 미노출 | pass | -uall 기준 0건 |
| `--check-done` + `generate-status --check` | pass | 기록 완료 후 실행 (요약 문서 참조) |

## 산출물

- `game/Assets/Scripts/Runtime/DayCycle/` — DayPhase, GameState, DayPhaseMachine, DayPhaseChangedEventArgs, GameEvents
- `game/Assets/Scripts/Runtime/Managers/GameManager.cs` — 싱글턴 매니저 1호 (얇은 래퍼)
- `game/Assets/Scripts/Runtime/UI/{MainMenuController,PhaseHudController}.cs` — UI 연결 컨트롤러
- `game/Assets/Scripts/Editor/SceneBuilder.cs` — 씬 2종 코드 저작 배치 진입점
- `game/Assets/Scenes/{MainMenu,Shop}.unity` — 생성된 씬 (빌더 소유)
- `game/ProjectSettings/EditorBuildSettings.asset` — 씬 2개 고정
- `game/Assets/Tests/EditMode/{DayPhaseMachineTests,SceneBuilderTests}.cs` — 테스트 13종 추가
- asmdef 3건 갱신 (UI/TMP/Editor 참조)
- `kb/tasks/task-104/{manifest,implementation-notes}.md`, `kb/artifacts/task-104-summary.md` — 기록
