# Manifest — task-106

> **Load**: 기본 로드 세트의 일부. 이 task 가 *실제로* 의존하는 것만 명시해 컨텍스트를 최소화한다.
> 여기에 없는 개념/파일은 기본적으로 열지 않는다.

- **task_id**: task-106
- **inputs**: kb/concepts/project-brief.md (코어 루프 SSOT), kb/concepts/demo-scope.md, kb/tasks/task-105/implementation-notes.md (Ops/thin 매니저·EditorInit·InventoryOps 불변 계약)
- **concepts_needed**: kb/concepts/project-brief.md, kb/concepts/demo-scope.md
- **related_files**: game/Assets/Scripts/Runtime/Service/ (5파일), game/Assets/Scripts/Runtime/DayCycle/GameState.cs, game/Assets/Scripts/Runtime/UI/ServicePanelController.cs, game/Assets/Scripts/Editor/SceneBuilder.cs, game/Assets/Scenes/Shop.unity, game/Assets/Tests/EditMode/{ServiceOps,ServicePanelScene}Tests.cs
- **notes**: 주문 생성은 시드 없는 결정론 산식(day+인덱스). 판매가 = BasePrice×party (배수는 task-108+). 등급 혼합 금지. 당일 통계 리셋/마감은 task-107 소관. IVT 에 Tests.EditMode 추가됨 (fixture 용).
- **generated_by**: design=codex gpt-5.5/xhigh @codex 0.143.0, 2026-07-09 (fallback=none)
