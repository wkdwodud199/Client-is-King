# 설계 문서 — task-101

> Status: ready
> Inputs: `kb/concepts/project-brief.md`, 루트 운영 규약 문서(`AGENT.md`, `AGENTS.md`, `CLAUDE.md`, `QUICKREF.md`), 현재 `.gitignore`, 현재 README 문서
> Outputs: 게임 프로젝트용 리포 규약 확장, Unity 6용 ignore 규칙, 데모 스코프 가드 문서화 설계
> Next step: Claude가 이 설계를 구현한 뒤 `task-102`에서 Unity 6 프로젝트 생성/기본 세팅을 진행

## 목표 (Objective)

`Client is King` 게임 개발을 시작하기 전에 저장소 운영 규약을 Unity 프로젝트 구조에 맞게 확장하고, Unity 생성물/캐시가 Git에 들어가지 않도록 `.gitignore`를 보강한다. 또한 데모 범위와 주차장 항목을 `kb/concepts/demo-scope.md`로 고정해 이후 task가 브리프 밖 기능을 무심코 구현하지 않도록 한다.

## 범위 (Scope)

- 포함:
  - 루트 운영 규약 문서에 게임 프로젝트 오버레이를 추가한다: Unity 프로젝트 위치는 `game/`, task 문서는 `kb/`, 구현 기록은 `implementation-notes.md`/artifact summary에 남긴다.
  - `.gitignore`에 Unity 6 프로젝트용 표준 ignore 규칙을 추가하되, `Assets/**/*.meta`, `ProjectSettings/`, `Packages/`처럼 버전 관리가 필요한 파일은 무시하지 않는다.
  - `kb/concepts/demo-scope.md`를 새로 작성해 데모 포함 범위, 하드캡, 제외/주차장 항목, 범위 변경 절차를 명시한다.
  - README 한/영 문서에는 이 저장소가 CWC 협업 런타임 위에서 Unity 게임 프로젝트를 담는다는 짧은 위치 설명과 핵심 경로만 보강한다.
  - 구현 완료 기록을 위해 `manifest.md`, `implementation-notes.md`, `kb/artifacts/task-101-summary.md`, 생성형 status board를 규약에 맞게 갱신한다.
- 제외:
  - Unity 프로젝트 생성, `game/` 디렉터리 초기화, 씬/스크립트/에셋 생성은 `task-102` 이후로 미룬다.
  - `runtime/validator/`, 러너 스크립트, CI 워크플로우의 동작 변경은 하지 않는다.
  - Steam 연동, 직원 고용, 인테리어, 다점포, 미슐랭 트랙, 조리 미니게임 등 데모 제외 기능은 구현하지 않고 주차장으로만 기록한다.

## 제약 (Constraints)

- `kb/concepts/project-brief.md`가 게임 컨셉과 로드맵의 SSOT이므로, task-101은 브리프 내용을 재정의하지 않고 운영 문서와 스코프 문서로 연결한다.
- Unity ignore 규칙은 `game/` 하위 프로젝트를 전제로 작성하되, 향후 Unity가 루트에 생성될 경우에도 위험한 캐시/빌드 산출물이 추적되지 않도록 일반 Unity 패턴을 함께 고려한다.
- `.meta` 파일을 무시하면 Unity 참조가 깨지므로 절대 ignore 대상에 넣지 않는다.
- 문서 변경은 기존 CWC 협업 규약을 삭제하거나 축소하지 않고, 게임 프로젝트에 필요한 추가 규약을 덧붙이는 방식으로 한다.
- 이 task의 스코프 가드는 문서 기반 정책이다. 새로운 validator/CI 강제 규칙은 요구하지 않는다.

## 구현 단계 (Implementation Steps)

1. `project-brief.md`의 Unity 6, `game/`, 데모 하드캡, 주차장 항목을 기준으로 운영 문서에 추가할 게임 프로젝트 규약을 정리한다.
2. `AGENT.md`, `AGENTS.md`, `CLAUDE.md`, `QUICKREF.md`에 게임 프로젝트 경로와 역할별 파일 소유권을 보강한다. 특히 Codex는 설계 시 `design.md`만, Claude는 구현 시 설계 범위에 지정된 게임/문서 파일만 수정한다는 경계를 유지한다.
3. `.gitignore`에 Unity 캐시/빌드/IDE 산출물 ignore 규칙을 추가한다. `game/Library/`, `game/Temp/`, `game/Obj/`, `game/Logs/`, `game/UserSettings/`, `game/Build*/`, Unity 생성 솔루션/프로젝트 파일, crash/memory capture 파일을 포함한다.
4. `kb/concepts/demo-scope.md`를 작성한다. 포함 범위, 제외/주차장, 하드캡, 범위 변경 승인 규칙, 이후 design.md에서 참조해야 하는 기준을 표로 정리한다.
5. `README.md`와 `README.en.md`에는 기존 CWC 설명을 유지하면서 `Client is King` 게임 프로젝트 오버레이, Unity 버전, 주요 경로(`game/`, `kb/`, `runtime/`)를 짧게 추가한다.
6. 완료 기록으로 `manifest.md`의 placeholder를 실제 입력/개념/관련 파일로 교체하고, `implementation-notes.md`, `kb/artifacts/task-101-summary.md`, `kb/index/status.md`를 규약대로 갱신한다.
7. 문서 검증, ignore 규칙 확인, 기존 테스트 스위트를 실행해 변경이 CWC 런타임을 깨지 않는지 확인한다.

## 실행 계획 (Execution Plan)

- implement_model: claude-fable-5
- implement_effort: medium
- routing_reason: M0 문서/ignore 정비 작업이며 코드·Unity 에셋 구현이 없어 `project-brief.md`의 task-101 라우팅대로 fable-5/medium이 적합하다.

| unit | 파일 범위 | depends_on | group |
|------|-----------|------------|-------|
| U1-repo-conventions | `AGENT.md`, `AGENTS.md`, `CLAUDE.md`, `QUICKREF.md` | 없음 | G1 |
| U2-unity-ignore | `.gitignore` | 없음 | G1 |
| U3-scope-doc | `kb/concepts/demo-scope.md`, 필요 시 README의 scope 링크 | 없음 | G1 |
| U4-readme-overlay | `README.md`, `README.en.md` | U1-repo-conventions, U3-scope-doc | G2 |
| U5-task-records | `kb/tasks/task-101/manifest.md`, `kb/tasks/task-101/implementation-notes.md`, `kb/artifacts/task-101-summary.md`, `kb/index/status.md` | U1-repo-conventions, U2-unity-ignore, U3-scope-doc, U4-readme-overlay | G3 |

## 파일/모듈 영향 (Affected Files/Modules)

| 파일/모듈 | 변경 유형 | 설명 |
|-----------|-----------|------|
| `AGENT.md` | modify | 공통 규약에 Unity 게임 프로젝트 경로, scope guard 참조, `game/`/`kb/` 파일 배치 규칙을 보강 |
| `AGENTS.md` | modify | Codex 설계자 규약에 게임 task 설계 시 `project-brief.md`와 `demo-scope.md`를 기준으로 범위 판단하도록 보강 |
| `CLAUDE.md` | modify | Claude 구현자 규약에 Unity 파일 소유권, `.meta` 보존, 구현 범위 이탈 시 기록/중단 기준을 보강 |
| `QUICKREF.md` | modify | routine fast-path에서 게임 task가 확인해야 할 핵심 경로와 scope guard를 요약 |
| `.gitignore` | modify | Unity 6 프로젝트 캐시, 임시 파일, 빌드 산출물, IDE 생성물을 무시하도록 추가 |
| `kb/concepts/demo-scope.md` | create | 데모 포함 범위, 하드캡, 주차장 기능, 범위 변경 절차를 기록하는 스코프 가드 문서 |
| `README.md` | modify | 한국어 README에 `Client is King` 게임 프로젝트 오버레이와 주요 경로를 추가 |
| `README.en.md` | modify | 영어 README에 동일한 게임 프로젝트 오버레이와 주요 경로를 추가 |
| `kb/tasks/task-101/manifest.md` | modify | done-gate 통과를 위해 inputs/concepts_needed/related_files placeholder를 실제 값으로 교체 |
| `kb/tasks/task-101/implementation-notes.md` | create | 구현 결정, 변경 파일, 검증 결과를 기록 |
| `kb/artifacts/task-101-summary.md` | create | task-101 완료 산출물 요약을 기록 |
| `kb/index/status.md` | modify | `runtime/generate-status.py`로 재생성되는 task 상태 보드 |

## 테스트 기준 (Test Criteria)

- [ ] `python runtime/validator/cli.py kb/tasks/task-101/design.md`가 0으로 통과한다.
- [ ] `.gitignore` 확인: `git check-ignore -v game/Library/test.asset game/Temp/test.tmp game/Obj/test.obj game/Logs/test.log game/UserSettings/EditorUserSettings.asset game/Builds/test.exe`가 모두 ignore 규칙을 보고한다.
- [ ] `.gitignore` 역확인: `git check-ignore game/Assets/Scripts/Foo.cs game/Assets/Scripts/Foo.cs.meta game/ProjectSettings/ProjectSettings.asset game/Packages/manifest.json`는 ignore 처리되지 않는다.
- [ ] `kb/concepts/demo-scope.md`에 데모 포함 범위, 하드캡, 제외/주차장, 범위 변경 절차가 모두 존재한다.
- [ ] `python -m pytest tests/validator tests/context_budget tests/status_board tests/runtime -v`가 통과한다.
- [ ] 가능한 환경에서는 `./tests/run-smoke.sh` 및 `Invoke-Pester tests/pester -Output Detailed -CI`가 통과한다.
- [ ] 구현 완료 후 `python runtime/validator/cli.py --check-done task-101`와 `python runtime/generate-status.py --check`가 통과한다.

## 오픈 이슈 (Open Issues)

- 없음. 이 task는 Unity 프로젝트를 만들지 않는 선행 정비 작업이며, 실제 Unity 생성/검증은 `task-102`에서 다룬다.
