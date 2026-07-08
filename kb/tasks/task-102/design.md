# 설계 문서 — task-102

> Status: ready
> Inputs: `kb/concepts/project-brief.md`, `kb/concepts/demo-scope.md`, `kb/tasks/task-101/design.md`, Unity 게임 프로젝트 운영 규약(`AGENT.md`, `CLAUDE.md`, `QUICKREF.md`), 현재 `.gitignore`
> Outputs: `game/` 하위 Unity 6(6000.3.8f1) 2D URP 프로젝트 생성·검증 설계, 기본 패키지/프로젝트 설정/폴더 구조, 배치 모드 컴파일·EditMode smoke 검증 기준
> Next step: Claude가 이 설계를 구현한 뒤 `task-103`에서 ScriptableObject 6종과 초기 데이터 설계를 진행

## 목표 (Objective)

`Client is King` 개발을 시작할 수 있도록 리포 루트의 `game/`에 Unity 6 에디터 6000.3.8f1 기반 2D URP 프로젝트를 생성하고, 브리프의 픽셀아트·C#·검증 규약에 맞는 최소 기본 설정을 적용한다. 프로젝트가 새 환경에서 열리고 컴파일되는지 배치 모드로 확인해 이후 task가 게임 코드와 에셋을 안전하게 추가할 수 있는 기준선을 만든다.

## 범위 (Scope)

- 포함:
  - `game/` 하위에 Unity 6 프로젝트를 새로 생성한다. `game/ProjectSettings/ProjectVersion.txt`에는 `m_EditorVersion: 6000.3.8f1`가 기록되어야 한다.
  - 2D URP 기반 프로젝트가 되도록 `Packages/manifest.json`, URP 렌더 파이프라인 에셋, Graphics/Quality 설정을 정리한다.
  - 프로젝트 이름/Product Name은 `Client is King`, 기본 C# 네임스페이스는 `ClientIsKing`으로 맞춘다.
  - 향후 task가 사용할 1차 폴더 구조를 만든다: `Assets/Scripts/Runtime`, `Assets/Scripts/Editor`, `Assets/Tests/EditMode`, `Assets/Data`, `Assets/Scenes`, `Assets/Art/Placeholders`, `Assets/Settings`.
  - 기본 패키지는 URP, 2D Pixel Perfect, TextMeshPro, Unity Test Framework처럼 브리프의 아키텍처와 검증에 필요한 최소 범위만 포함한다.
  - Unity 프로젝트를 배치 모드로 열어 컴파일 게이트를 통과시키고, 가능하면 EditMode smoke test를 추가해 프로젝트 설정을 검증한다.
  - `kb/tasks/task-102/manifest.md`, `implementation-notes.md`, `kb/artifacts/task-102-summary.md`, `kb/index/status.md`는 구현 완료 기록으로 갱신하도록 설계에 포함한다.
- 제외:
  - 코어 데이터 모델(`IngredientDef`, `RecipeDef`, `GenreDef`, `CustomerArchetypeDef`, `SNSCampaignDef`, `GameEventDef`)과 초기 데이터 작성은 `task-103`으로 미룬다.
  - `MainMenu.unity`, `Shop.unity`, SceneBuilder, 하루 상태 머신, UI/프리팹 생성은 `task-104` 이후 범위다.
  - 경제·인벤토리·서빙·SNS·이벤트·저장 등 게임플레이 시스템은 구현하지 않는다.
  - Steam 연동, 직원 고용, 인테리어, 다점포, 미슐랭 트랙, 조리 미니게임 등 `demo-scope.md`의 주차장 기능은 구현하지 않는다.
  - Unity 캐시/빌드 산출물(`Library`, `Temp`, `Obj`, `Logs`, `UserSettings`, `Build*`)은 버전 관리하지 않는다.

## 제약 (Constraints)

- `kb/concepts/project-brief.md`가 Unity 버전, 프로젝트 위치, 아키텍처, 픽셀 표준의 SSOT다. 이 task는 브리프를 재정의하지 않고 프로젝트 기준선으로 반영한다.
- Unity 에디터는 6000.3.8f1을 사용한다. 해당 버전이 구현 환경에 없으면 다른 버전으로 대체하지 말고 `implementation-notes.md`에 차단 사유를 기록한 뒤 중단한다.
- Unity 프로젝트 위치는 반드시 `game/`이다. Unity가 리포 루트에 프로젝트 파일을 생성하지 않도록 생성 경로를 확인한다.
- `game/Assets/**/*.meta`, `game/ProjectSettings/`, `game/Packages/`는 재현에 필요한 버전 관리 대상이다. `.meta` 파일을 삭제하거나 ignore 처리하지 않는다.
- 브리프의 장래 아키텍처와 충돌하지 않도록 DI/ECS/이벤트버스 라이브러리, Input System 전환, 추가 렌더링 프레임워크는 도입하지 않는다.
- 템플릿이 생성한 샘플 씬(`SampleScene.unity`)은 커밋하지 않는다. 데모의 실제 씬은 `task-104`에서 `MainMenu.unity`와 `Shop.unity`만 생성한다.
- 이 task의 목표는 "프로젝트 생성과 검증 가능한 기준선"이다. 플레이 가능한 기능이나 도메인 모델을 먼저 넣어 task-103 이후의 책임을 침범하지 않는다.

## 구현 단계 (Implementation Steps)

1. 구현 시작 전 `game/`이 없거나 비어 있는지 확인한다. 이미 Unity 프로젝트가 있으면 덮어쓰지 말고 현재 내용과 충돌 여부를 `implementation-notes.md`에 기록한 뒤 사용자 확인을 요청한다.
2. Unity 6000.3.8f1 에디터 경로를 확인하고, Unity Hub/Editor 또는 CLI로 `game/`에 2D URP 프로젝트를 생성한다. 생성 후 `ProjectSettings/ProjectVersion.txt`의 에디터 버전을 확인한다.
3. `Packages/manifest.json`을 정리해 URP, 2D Pixel Perfect, TextMeshPro, Test Framework를 포함하고 불필요한 템플릿/샘플 의존성은 제거한다. Unity가 생성한 `packages-lock.json`도 함께 추적한다.
4. URP 렌더 파이프라인 에셋과 2D Renderer 에셋을 `Assets/Settings/Rendering/` 아래에 두고, Graphics/Quality 설정에서 해당 URP asset을 사용하도록 연결한다.
5. Product Name, 기본 네임스페이스, Windows Standalone 우선 개발에 필요한 최소 Player/Editor 설정을 적용한다. 픽셀아트 기준선으로 PPU 32, Point 필터, Pixel Perfect Camera 640x360을 이후 에셋/씬 task에서 적용할 수 있도록 설정명과 폴더를 준비한다.
6. `Assets/` 아래 1차 폴더 구조와 필요한 `.asmdef`/smoke test 파일을 만든다. smoke test는 프로젝트 설정이나 패키지 존재를 확인하는 수준으로 제한하고 게임 도메인 로직은 만들지 않는다.
7. 템플릿이 만든 샘플 씬, 샘플 에셋, tutorial artifact가 있으면 `.meta`와 함께 제거한다. 남기는 파일은 이후 task의 기준선으로 의미가 있는 프로젝트 설정·패키지·폴더·검증 파일로 제한한다.
8. Unity 배치 모드로 프로젝트를 열어 컴파일을 확인하고, smoke test를 만들었다면 EditMode 테스트를 실행한다. 캐시/로그/빌드 산출물이 Git 추적 대상에 잡히지 않는지 확인한다.
9. 구현 기록으로 `kb/tasks/task-102/manifest.md`, `implementation-notes.md`, `kb/artifacts/task-102-summary.md`를 갱신하고 `runtime/generate-status.py`로 status board를 재생성한다.

## 실행 계획 (Execution Plan)

- implement_model: claude-fable-5
- implement_effort: high
- routing_reason: M0 Unity 프로젝트 생성·설정·배치 검증 작업으로 파일 생성 범위가 넓지만 게임 로직은 없어 `project-brief.md`의 task-102 라우팅대로 fable-5/high가 적합하다.

| unit | 파일 범위 | depends_on | group |
|------|-----------|------------|-------|
| U1-editor-and-project-create | `game/ProjectSettings/ProjectVersion.txt`, Unity 생성 기본 구조 | 없음 | G1 |
| U2-packages-and-rendering | `game/Packages/manifest.json`, `game/Packages/packages-lock.json`, `game/Assets/Settings/Rendering/`, `game/ProjectSettings/GraphicsSettings.asset`, `game/ProjectSettings/QualitySettings.asset` | U1-editor-and-project-create | G2 |
| U3-project-identity-and-folders | `game/ProjectSettings/ProjectSettings.asset`, `game/Assets/Scripts/`, `game/Assets/Tests/`, `game/Assets/Data/`, `game/Assets/Scenes/`, `game/Assets/Art/Placeholders/`, 관련 `.meta` | U1-editor-and-project-create | G2 |
| U4-smoke-validation | `game/Assets/Tests/EditMode/`, 필요 시 `game/Assets/Scripts/Editor/` | U2-packages-and-rendering, U3-project-identity-and-folders | G3 |
| U5-cleanup-and-git-check | `game/Assets/` 샘플 제거, Git 추적/ignore 확인 | U2-packages-and-rendering, U3-project-identity-and-folders, U4-smoke-validation | G4 |
| U6-task-records | `kb/tasks/task-102/manifest.md`, `kb/tasks/task-102/implementation-notes.md`, `kb/artifacts/task-102-summary.md`, `kb/index/status.md` | U5-cleanup-and-git-check | G5 |

## 파일/모듈 영향 (Affected Files/Modules)

| 파일/모듈 | 변경 유형 | 설명 |
|-----------|-----------|------|
| `game/` | create | Unity 6 프로젝트 루트. 캐시/빌드 산출물은 제외하고 재현 가능한 프로젝트 파일만 추적 |
| `game/ProjectSettings/ProjectVersion.txt` | create | Unity 에디터 버전 `6000.3.8f1` 고정 확인 파일 |
| `game/ProjectSettings/*.asset` | create/modify | Product Name, 기본 네임스페이스, Graphics/Quality URP 연결, 최소 Player/Editor 설정 |
| `game/Packages/manifest.json` | create/modify | URP, 2D Pixel Perfect, TextMeshPro, Test Framework 등 기본 패키지 선언 |
| `game/Packages/packages-lock.json` | create | Unity 패키지 잠금 파일. 재현 가능한 패키지 해석을 위해 추적 |
| `game/Assets/Settings/Rendering/` | create | URP Render Pipeline Asset과 2D Renderer Asset 위치 |
| `game/Assets/Scripts/Runtime/` | create | 향후 런타임 C# 코드의 기준 폴더. 이 task에서는 도메인 로직을 넣지 않음 |
| `game/Assets/Scripts/Editor/` | create | 필요 시 프로젝트 설정 검증용 Editor 유틸리티 위치 |
| `game/Assets/Tests/EditMode/` | create | 프로젝트 설정 smoke test와 EditMode 테스트 어셈블리 위치 |
| `game/Assets/Data/` | create | task-103 이후 ScriptableObject 초기 데이터 위치 |
| `game/Assets/Scenes/` | create | task-104에서 `MainMenu.unity`, `Shop.unity`를 생성할 대상 폴더. 이 task에서는 샘플 씬을 남기지 않음 |
| `game/Assets/Art/Placeholders/` | create | task-112 이전 CC0/OFL 플레이스홀더 아트 위치 |
| `game/**/*.meta` | create/modify/delete | Unity 에셋·폴더와 함께 추적되는 메타 파일. 삭제/이동 시 에셋과 쌍으로 처리 |
| `kb/tasks/task-102/manifest.md` | modify | done-gate 통과를 위해 inputs/concepts_needed/related_files placeholder를 실제 값으로 교체 |
| `kb/tasks/task-102/implementation-notes.md` | create | Unity 버전, 생성 방법, 설정 결정, 검증 결과, 환경 이슈를 기록 |
| `kb/artifacts/task-102-summary.md` | create | task-102 완료 산출물 요약과 다음 단계 기록 |
| `kb/index/status.md` | modify | `runtime/generate-status.py`로 재생성되는 task 상태 보드 |

## 테스트 기준 (Test Criteria)

- [ ] `python runtime/validator/cli.py kb/tasks/task-102/design.md`가 0으로 통과한다.
- [ ] `game/ProjectSettings/ProjectVersion.txt`에 `m_EditorVersion: 6000.3.8f1`가 기록되어 있다.
- [ ] `game/Packages/manifest.json`에 URP, 2D Pixel Perfect, TextMeshPro, Test Framework 패키지가 포함되어 있고 `game/Packages/packages-lock.json`이 생성되어 있다.
- [ ] Unity 배치 모드 컴파일 게이트가 통과한다: `Unity.exe -batchmode -quit -projectPath game -logFile <temp-log>` 종료 코드가 0이고 로그에 C# compile error가 없다.
- [ ] smoke test를 추가한 경우 `Unity.exe -batchmode -quit -projectPath game -runTests -testPlatform EditMode -testResults <temp-results> -logFile <temp-log>`가 0으로 통과한다.
- [ ] `game/Assets/Scenes/SampleScene.unity` 같은 템플릿 샘플 씬이 추적 대상에 남아 있지 않다.
- [ ] `git status --short game`에 `Library`, `Temp`, `Obj`, `Logs`, `UserSettings`, `Build*` 등 Unity 캐시/빌드 산출물이 나타나지 않는다.
- [ ] `git check-ignore -v game/Library/test.asset game/Temp/test.tmp game/Obj/test.obj game/Logs/test.log game/UserSettings/EditorUserSettings.asset`가 모두 ignore 규칙을 보고한다.
- [ ] `git check-ignore game/Assets/Scripts/Runtime game/ProjectSettings/ProjectSettings.asset game/Packages/manifest.json`는 ignore 처리되지 않는다.
- [ ] 구현 완료 후 `python runtime/validator/cli.py --check-done task-102`와 `python runtime/generate-status.py --check`가 통과한다.

## 오픈 이슈 (Open Issues)

- 없음. 단, 구현 환경에 Unity 6000.3.8f1이 설치되어 있지 않으면 설계 문제가 아니라 구현 환경 차단으로 보고 `implementation-notes.md`에 기록한 뒤 중단한다.
