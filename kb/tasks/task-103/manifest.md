# Manifest — task-103

> **Load**: 기본 로드 세트의 일부. 이 task 가 *실제로* 의존하는 것만 명시해 컨텍스트를 최소화한다.
> 여기에 없는 개념/파일은 기본적으로 열지 않는다.

- **task_id**: task-103
- **inputs**: kb/concepts/project-brief.md (SO 6종·하드캡 SSOT), kb/concepts/demo-scope.md (하드캡/주차장), kb/tasks/task-102/implementation-notes.md (asmdef 경계·배치 게이트 규약)
- **concepts_needed**: kb/concepts/project-brief.md, kb/concepts/demo-scope.md
- **related_files**: game/Assets/Scripts/Runtime/Data/ (SO 6종+DataTypes), game/Assets/Scripts/Runtime/AssemblyInfo.cs, game/Assets/Scripts/Editor/InitialDataBuilder.cs, game/Assets/Data/Definitions/ (시드 39개), game/Assets/Tests/EditMode/DataDefinitionTests.cs
- **notes**: 시드 데이터는 InitialDataBuilder.Apply 로만 재생성 (수동 편집 시 다음 Apply 가 덮어씀). SO 필드 주입은 internal EditorInit + InternalsVisibleTo(ClientIsKing.Editor).
- **generated_by**: design=codex gpt-5.5/xhigh @codex 0.143.0, 2026-07-09 (fallback=none)
