# Manifest — task-105

> **Load**: 기본 로드 세트의 일부. 이 task 가 *실제로* 의존하는 것만 명시해 컨텍스트를 최소화한다.
> 여기에 없는 개념/파일은 기본적으로 열지 않는다.

- **task_id**: task-105
- **inputs**: kb/concepts/project-brief.md (코어 루프·매니저 규약 SSOT), kb/concepts/demo-scope.md, kb/tasks/task-103/implementation-notes.md (IngredientDef 시드·EditorInit 패턴), kb/tasks/task-104/implementation-notes.md (GameState·SceneBuilder·배치 게이트)
- **concepts_needed**: kb/concepts/project-brief.md, kb/concepts/demo-scope.md
- **related_files**: game/Assets/Scripts/Runtime/{Inventory,Economy}/ (Ops+Manager+타입), game/Assets/Scripts/Runtime/DayCycle/GameState.cs, game/Assets/Scripts/Runtime/UI/MarketPanelController.cs, game/Assets/Scripts/Editor/SceneBuilder.cs, game/Assets/Scenes/Shop.unity, game/Assets/Tests/EditMode/{Economy,Inventory}ManagerTests.cs·MarketPanelSceneTests.cs
- **notes**: 경제/인벤 핵심 규칙은 순수 Ops(EconomyOps/InventoryOps)에 있고 매니저는 thin wrapper. 시작 자금 = GameState.StartingCash(30000). 장보기 UI 는 SceneBuilder 재실행으로만 수정.
- **generated_by**: design=codex gpt-5.5/xhigh @codex 0.143.0, 2026-07-09 (fallback=none)
