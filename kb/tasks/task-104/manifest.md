# Manifest — task-104

> **Load**: 기본 로드 세트의 일부. 이 task 가 *실제로* 의존하는 것만 명시해 컨텍스트를 최소화한다.
> 여기에 없는 개념/파일은 기본적으로 열지 않는다.

- **task_id**: task-104
- **inputs**: kb/concepts/project-brief.md (하루 사이클·씬 하드캡·매니저 규약 SSOT), kb/concepts/demo-scope.md (씬 2개 하드캡), kb/tasks/task-103/implementation-notes.md (asmdef 경계·EditorInit 패턴)
- **concepts_needed**: kb/concepts/project-brief.md, kb/concepts/demo-scope.md
- **related_files**: game/Assets/Scripts/Runtime/DayCycle/ (상태 머신 5파일), game/Assets/Scripts/Runtime/Managers/GameManager.cs, game/Assets/Scripts/Runtime/UI/ (컨트롤러 2종), game/Assets/Scripts/Editor/SceneBuilder.cs, game/Assets/Scenes/{MainMenu,Shop}.unity, game/ProjectSettings/EditorBuildSettings.asset, game/Assets/Tests/EditMode/{DayPhaseMachine,SceneBuilder}Tests.cs
- **notes**: 씬은 SceneBuilder.Apply 로만 재생성 (수동 편집 금지 — 전체가 빌더 소유, 재실행 시 덮어씀). UI 컨트롤러 참조 주입은 EditorInit 패턴.
- **generated_by**: design=codex gpt-5.5/xhigh @codex 0.143.0, 2026-07-09 (fallback=none)
