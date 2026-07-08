# 설계 문서 — task-104

> Status: ready
> Inputs: `kb/concepts/project-brief.md`, `kb/concepts/demo-scope.md`, `kb/tasks/task-103/design.md`, `kb/tasks/task-103/implementation-notes.md`, 현재 Unity 프로젝트 기준선(`game/`)
> Outputs: `MainMenu.unity`/`Shop.unity` 2개 씬, 하루 상태 머신 런타임 골격, SceneBuilder 에디터 생성/검증 설계
> Next step: Claude가 이 설계를 구현한 뒤 `task-105`에서 경제·인벤토리, 장보기 UI, 관련 EditMode 테스트를 진행

## 목표 (Objective)

`Client is King`의 첫 플레이어블 골격을 만들기 위해 브리프가 고정한 2개 씬(`MainMenu.unity`, `Shop.unity`)과 하루 사이클 `Market → Service → Settlement → Night` 상태 머신을 추가한다. 씬과 기본 UI는 `SceneBuilder` 에디터 스크립트로 재현 가능하게 생성하고, 이후 task-105~107의 경제·서빙·정산 시스템이 붙을 수 있는 런타임 진입점을 마련한다.

## 범위 (Scope)

- 포함:
  - `game/Assets/Scenes/MainMenu.unity`와 `game/Assets/Scenes/Shop.unity`를 생성하고 Build Settings에 이 두 씬만 등록한다.
  - `SceneBuilder.Apply` 에디터 배치 진입점을 추가해 두 씬, 기본 카메라, Canvas/EventSystem, 최소 UI, 씬 전환 버튼, 상태 표시/진행 버튼을 멱등 생성한다.
  - Runtime에 `DayPhase`, `GameState`, `DayPhaseMachine`, `GameEvents`, `GameManager`를 추가해 하루 상태 전환의 단일 진입점을 만든다.
  - `MainMenu`에는 게임 시작 버튼과 제목만 둔다. 시작 버튼은 `Shop` 씬을 로드한다.
  - `Shop`에는 Pixel Perfect Camera 기준선, 현재 일차/phase 표시, 다음 phase로 진행하는 버튼, phase별 placeholder 패널만 둔다.
  - 상태 머신은 `Market → Service → Settlement → Night → Market` 순서로 전환하고 `Night → Market` 전환 때 day를 1 증가시킨다.
  - EditMode 테스트를 추가해 상태 전환 순서, day 증가, 이벤트 발행, SceneBuilder 산출물, Build Settings의 씬 하드캡을 검증한다.
  - 구현 완료 기록으로 `kb/tasks/task-104/manifest.md`, `implementation-notes.md`, `kb/artifacts/task-104-summary.md`, `kb/index/status.md`를 갱신하도록 포함한다.
- 제외:
  - 재료 구매 계산, 인벤토리 증감, 자금 계산, 장보기 UI 상세 동작은 `task-105` 범위다.
  - 조리 큐, 손님 생성, 서빙, 주문 처리, 수익 지급은 `task-106` 범위다.
  - 정산 수식, 하루 마감 보상/비용, 파산 판정은 `task-107` 범위다.
  - 장르 선택 UX와 장르별 배수 적용은 `task-108` 범위다.
  - SNS 마케팅 실행, 이벤트/장애물 실행, 저장/불러오기는 각각 `task-109`, `task-110`, `task-111`로 미룬다.
  - Steam 연동, 직원 고용, 인테리어, 다점포, 미슐랭 트랙, 조리 미니게임 등 `demo-scope.md`의 주차장 기능은 구현하지 않는다.
  - `MainMenu.unity`와 `Shop.unity` 외 추가 씬, 커스텀 아트, 폰트 패스, 사운드, 애니메이션은 만들지 않는다.

## 제약 (Constraints)

- `kb/concepts/project-brief.md`가 씬 수, 하루 사이클, 매니저 방향, 픽셀 표준의 SSOT다. 이 task는 브리프를 재정의하지 않는다.
- `kb/concepts/demo-scope.md`의 하드캡에 따라 Unity 씬은 `MainMenu.unity`와 `Shop.unity` 2개만 존재해야 한다.
- Unity 프로젝트 경로는 반드시 `game/`이며, 모든 runtime/editor/test 코드와 씬 asset은 `game/Assets/` 아래에 둔다.
- 씬과 `.meta` 파일은 모두 버전 관리 대상이다. `Library`, `Temp`, `Obj`, `Logs`, `UserSettings` 같은 Unity 캐시는 추적하지 않는다.
- 씬/프리팹/UI는 수동 에디터 작업이 아니라 `SceneBuilder` 코드로 저작한다. 구현자는 생성 후 수동으로 씬을 고치는 대신 빌더를 수정하고 재실행한다.
- 상태 머신은 씬 전환이 아니다. `Market`, `Service`, `Settlement`, `Night`는 `Shop` 씬 내부 phase로 표현한다.
- 상태 머신은 테스트 가능한 순수 C# 로직을 중심에 둔다. `GameManager` MonoBehaviour는 상태 보관, Unity scene/UI 연결, 이벤트 발행의 얇은 래퍼로 유지한다.
- `GameState`는 이번 task에서 day/phase 최소 필드만 가진다. 자금, 인벤토리, 통계, 저장 포맷 확장은 후속 task에서 추가한다.
- `Dictionary`, DI/ECS/이벤트버스 라이브러리, Input System 전환, 외부 UI 프레임워크를 도입하지 않는다.
- 픽셀 기준선은 task-102와 동일하게 PPU 32, reference resolution 640x360, URP 내장 `PixelPerfectCamera`를 사용한다.

## 구현 단계 (Implementation Steps)

1. Runtime에 `DayPhase` enum을 추가한다. 값은 `Market`, `Service`, `Settlement`, `Night` 4개로 제한하고, 표시 문자열이 필요하면 별도 helper에서 제공한다.
2. Runtime에 직렬화 가능한 순수 C# `GameState`를 추가한다. 초기값은 day 1, phase `Market`이며, Unity `JsonUtility` 확장을 고려해 public field 또는 `[SerializeField]` 기반 단순 구조를 사용한다.
3. Runtime에 `DayPhaseMachine`을 추가한다. 생성 시 `GameState`를 받고 `Advance()`가 다음 phase를 반환하며, `Night`에서 `Market`으로 넘어갈 때만 day를 증가시킨다. 잘못된 null state나 day 1 미만 입력은 방어적으로 보정하거나 명확한 예외로 처리한다.
4. Runtime에 `GameEvents` 정적 클래스를 추가한다. 최소 이벤트는 `DayPhaseChanged`이며 payload에는 day, previous phase, current phase를 담는다. 후속 시스템이 구독할 수 있게 C# `event`만 사용한다.
5. Runtime에 `GameManager` MonoBehaviour를 추가한다. singleton 인스턴스, 현재 `GameState`, `AdvancePhase()`, `StartNewGame()`, `LoadShopScene()` 정도의 얇은 API만 제공하고, 경제/인벤토리/서비스 로직은 넣지 않는다.
6. Runtime에 UI 연결용 작은 MonoBehaviour를 추가한다. `MainMenuController`는 시작 버튼에서 `GameManager.StartNewGame()`과 `LoadShopScene()`을 호출하고, `PhaseHudController`는 현재 day/phase 표시와 다음 phase 버튼만 갱신한다.
7. Editor에 `SceneBuilder.Apply`를 추가한다. `Assets/Scenes` 폴더를 보장하고, `MainMenu.unity`와 `Shop.unity`를 열거나 새로 만든 뒤 기존 빌더 소유 오브젝트를 정리하고 재생성한다.
8. `MainMenu` 씬을 생성한다. `GameManager` 부트스트랩 오브젝트, `Main Camera`, `EventSystem`, Canvas, 제목 텍스트, 시작 버튼을 배치한다. 버튼은 `MainMenuController`에 연결한다.
9. `Shop` 씬을 생성한다. `GameManager`, `Main Camera` + URP `PixelPerfectCamera`, `EventSystem`, Canvas, day/phase 텍스트, 다음 phase 버튼, `Market/Service/Settlement/Night` placeholder 패널을 배치한다. 패널은 현재 phase에 맞춰 표시/숨김만 수행한다.
10. `EditorBuildSettings.scenes`를 `Assets/Scenes/MainMenu.unity`, `Assets/Scenes/Shop.unity` 순서로 갱신한다. 다른 씬이 있으면 추가하지 말고 테스트에서 실패하도록 한다.
11. EditMode 테스트를 추가한다. 순수 상태 머신 테스트는 scene load 없이 실행하고, SceneBuilder 검증 테스트는 `SceneBuilder.Apply` 실행 후 scene asset 존재, Build Settings 순서, 필수 컴포넌트 존재, 씬 수 하드캡을 확인한다.
12. Unity 배치 모드에서 `SceneBuilder.Apply`, 컴파일 게이트, EditMode 테스트를 실행한다. 마지막으로 구현 기록 문서와 status board를 갱신한다.

## 실행 계획 (Execution Plan)

- implement_model: claude-fable-5
- implement_effort: xhigh
- routing_reason: M1의 씬 생성·런타임 상태 골격·에디터 빌더·검증을 함께 묶는 구조 작업이므로 `project-brief.md`의 task-104 라우팅대로 fable-5/xhigh가 적합하다.

| unit | 파일 범위 | depends_on | group |
|------|-----------|------------|-------|
| U1-day-state-runtime | `game/Assets/Scripts/Runtime/DayCycle/*.cs`, 관련 `.meta` | 없음 | G1 |
| U2-game-and-ui-controllers | `game/Assets/Scripts/Runtime/Managers/GameManager.cs`, `game/Assets/Scripts/Runtime/UI/*.cs`, 관련 `.meta` | U1-day-state-runtime | G2 |
| U3-scene-builder | `game/Assets/Scripts/Editor/SceneBuilder.cs`, `game/Assets/Scenes/MainMenu.unity`, `game/Assets/Scenes/Shop.unity`, `game/ProjectSettings/EditorBuildSettings.asset`, 관련 `.meta` | U2-game-and-ui-controllers | G3 |
| U4-editmode-tests | `game/Assets/Tests/EditMode/DayPhaseMachineTests.cs`, `game/Assets/Tests/EditMode/SceneBuilderTests.cs`, 필요 시 `ClientIsKing.Tests.EditMode.asmdef` | U1-day-state-runtime, U3-scene-builder | G4 |
| U5-validation-pass | Unity 배치 `SceneBuilder.Apply` 로그, 컴파일 로그, EditMode 결과 XML, `runtime/validator` 결과 | U3-scene-builder, U4-editmode-tests | G5 |
| U6-task-records | `kb/tasks/task-104/manifest.md`, `kb/tasks/task-104/implementation-notes.md`, `kb/artifacts/task-104-summary.md`, `kb/index/status.md` | U5-validation-pass | G6 |

## 파일/모듈 영향 (Affected Files/Modules)

| 파일/모듈 | 변경 유형 | 설명 |
|-----------|-----------|------|
| `game/Assets/Scripts/Runtime/DayCycle/DayPhase.cs` | create | 하루 phase enum 4종(`Market`, `Service`, `Settlement`, `Night`) 정의 |
| `game/Assets/Scripts/Runtime/DayCycle/GameState.cs` | create | day와 current phase만 담는 최소 런타임 상태 컨테이너 |
| `game/Assets/Scripts/Runtime/DayCycle/DayPhaseMachine.cs` | create | phase 전환 순서와 `Night → Market` day 증가 규칙을 담당하는 순수 C# 상태 머신 |
| `game/Assets/Scripts/Runtime/DayCycle/DayPhaseChangedEventArgs.cs` | create | `GameEvents.DayPhaseChanged` payload 타입 |
| `game/Assets/Scripts/Runtime/DayCycle/GameEvents.cs` | create | C# `event` 기반 런타임 이벤트 허브의 초기 골격 |
| `game/Assets/Scripts/Runtime/Managers/GameManager.cs` | create | 상태 머신을 보유하고 새 게임 시작, phase 진행, Shop 씬 로드를 제공하는 singleton MonoBehaviour |
| `game/Assets/Scripts/Runtime/UI/MainMenuController.cs` | create | MainMenu 시작 버튼을 `GameManager` API에 연결 |
| `game/Assets/Scripts/Runtime/UI/PhaseHudController.cs` | create | Shop 씬의 day/phase 표시와 다음 phase 버튼 갱신 |
| `game/Assets/Scripts/Runtime/ClientIsKing.Runtime.asmdef` | modify | 새 Runtime 폴더가 기존 Runtime 어셈블리에 포함되는지 유지한다. 별도 외부 참조는 원칙적으로 추가하지 않음 |
| `game/Assets/Scripts/Editor/SceneBuilder.cs` | create | 두 씬과 기본 UI를 멱등 생성하고 Build Settings를 갱신하는 배치 진입점 |
| `game/Assets/Scenes/MainMenu.unity` | create | 제목, 시작 버튼, 카메라, EventSystem, GameManager 부트스트랩을 포함하는 메인 메뉴 씬 |
| `game/Assets/Scenes/Shop.unity` | create | Pixel Perfect Camera, GameManager, phase HUD, phase placeholder 패널을 포함하는 단일 플레이 씬 |
| `game/ProjectSettings/EditorBuildSettings.asset` | modify | Build Settings 씬 목록을 `MainMenu`, `Shop` 2개로 고정 |
| `game/Assets/Tests/EditMode/DayPhaseMachineTests.cs` | create | phase 순서, day 증가, 초기값 보정을 검증하는 EditMode 테스트 |
| `game/Assets/Tests/EditMode/SceneBuilderTests.cs` | create | SceneBuilder 산출물, Build Settings 순서, 필수 컴포넌트, 씬 하드캡을 검증하는 EditMode 테스트 |
| `game/Assets/Tests/EditMode/ClientIsKing.Tests.EditMode.asmdef` | modify | 필요 시 SceneBuilder 검증에 필요한 Editor/Runtime 참조를 유지하거나 보강 |
| `game/**/*.meta` | create/modify | Unity가 생성하는 코드/씬/폴더 메타 파일. 관련 asset과 쌍으로 추적 |
| `kb/tasks/task-104/manifest.md` | modify | done-gate 통과를 위해 inputs/concepts_needed/related_files placeholder를 실제 값으로 교체 |
| `kb/tasks/task-104/implementation-notes.md` | create | 구현 결정, SceneBuilder 생성 방식, 상태 머신 API, 검증 결과, Unity 환경 이슈를 기록 |
| `kb/artifacts/task-104-summary.md` | create | task-104 완료 산출물 요약과 task-105 인계 사항 기록 |
| `kb/index/status.md` | modify | `runtime/generate-status.py`로 재생성되는 task 상태 보드 |

## 테스트 기준 (Test Criteria)

- [ ] `python runtime/validator/cli.py kb/tasks/task-104/design.md`가 0으로 통과한다.
- [ ] Unity 배치 모드에서 SceneBuilder가 성공한다: `Unity.exe -batchmode -quit -nographics -projectPath game -executeMethod ClientIsKing.EditorTools.SceneBuilder.Apply -logFile <temp-log>` 종료 코드가 0이다.
- [ ] 배치 컴파일 게이트가 통과한다: `Unity.exe -batchmode -quit -nographics -projectPath game -logFile <temp-log>` 종료 코드가 0이고 로그에 `error CS`가 없다.
- [ ] EditMode 테스트가 통과한다: `Unity.exe -batchmode -nographics -projectPath game -runTests -testPlatform EditMode -testResults <temp-results> -logFile <temp-log>` 종료 코드가 0이다.
- [ ] `DayPhaseMachineTests`가 `Market → Service → Settlement → Night → Market` 순서와 `Night → Market` 전환 시 day +1 규칙을 검증한다.
- [ ] `GameEvents.DayPhaseChanged`는 phase 전환 시 이전 phase, 현재 phase, day를 포함해 정확히 1회 발행된다.
- [ ] `Assets/Scenes/MainMenu.unity`와 `Assets/Scenes/Shop.unity`가 존재하고 두 씬 모두 `Main Camera`, `EventSystem`, Canvas를 포함한다.
- [ ] `Shop.unity`의 카메라는 URP `PixelPerfectCamera`를 포함하고 PPU 32, reference resolution 640x360으로 설정되어 있다.
- [ ] `Shop.unity`에는 day/phase 표시 텍스트, 다음 phase 버튼, 4개 phase placeholder 패널이 존재한다.
- [ ] `EditorBuildSettings.scenes`에는 `Assets/Scenes/MainMenu.unity`, `Assets/Scenes/Shop.unity`가 이 순서로만 등록되어 있다.
- [ ] `git status --short game`에 Unity 캐시/빌드 산출물(`Library`, `Temp`, `Obj`, `Logs`, `UserSettings`, `Build*`)이 나타나지 않는다.
- [ ] 구현 완료 후 `python runtime/validator/cli.py --check-done task-104`와 `python runtime/generate-status.py --check`가 통과한다.

## 오픈 이슈 (Open Issues)

- 없음. 이 task의 placeholder UI는 후속 task가 실제 경제·서빙·정산 UI로 교체할 전제이며, 지금은 상태 전환과 씬 재현성을 먼저 고정한다.
