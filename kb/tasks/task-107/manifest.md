# Manifest — task-107

> **Load**: 기본 로드 세트의 일부. 이 task 가 *실제로* 의존하는 것만 명시해 컨텍스트를 최소화한다.
> 여기에 없는 개념/파일은 기본적으로 열지 않는다.

- **task_id**: task-107
- **inputs**: kb/concepts/project-brief.md (코어 루프·M1 게이트 SSOT), kb/concepts/demo-scope.md, kb/tasks/task-106/implementation-notes.md (매출 cash 반영 시점·통계 필드·Ops 패턴)
- **concepts_needed**: kb/concepts/project-brief.md, kb/concepts/demo-scope.md
- **related_files**: game/Assets/Scripts/Runtime/Settlement/ (3파일), game/Assets/Scripts/Runtime/DayCycle/GameState.cs, game/Assets/Scripts/Runtime/Economy/EconomyOps.cs (지출 추적), game/Assets/Scripts/Runtime/Managers/GameManager.cs (파산 게이트), game/Assets/Scripts/Runtime/UI/{PhaseHud,SettlementPanel,NightPanel}Controller.cs, game/Assets/Scripts/Editor/SceneBuilder.cs, game/Assets/Tests/EditMode/{SettlementOps,FirstPlayableLoop,SettlementPanelScene}Tests.cs
- **notes**: 정산 cash delta 는 운영비(12,000, SettlementOps.DailyOperatingCost)뿐 — 매출/재료비는 각 시점에 이미 반영(이중 반영 금지). 정산은 day 당 1회 멱등. 파산 시 cash 0 고정 + AdvancePhase 차단. GameManager 의 DDOL 은 Play 모드 가드(EditMode 테스트 가능).
- **generated_by**: design=codex gpt-5.5/xhigh @codex 0.143.0, 2026-07-09 (fallback=none)
