# Manifest — task-108

> **Load**: 기본 로드 세트의 일부. 이 task 가 *실제로* 의존하는 것만 명시해 컨텍스트를 최소화한다.
> 여기에 없는 개념/파일은 기본적으로 열지 않는다.

- **task_id**: task-108
- **inputs**: kb/concepts/project-brief.md (화면 연출 최소 기준 — 로드맵 v2 신설 절), kb/concepts/demo-scope.md (CC0/OFL 하드캡), kb/tasks/task-107/implementation-notes.md (M1 루프·패널 구조)
- **concepts_needed**: kb/concepts/project-brief.md, kb/concepts/demo-scope.md
- **related_files**: game/Assets/Art/Placeholders/** (스프라이트 10종+provenance), game/Assets/Scripts/Editor/{PlaceholderArtBuilder,SceneBuilder}.cs, game/Assets/Scripts/Runtime/Presentation/ (6파일), game/Assets/Scripts/Runtime/DayCycle/GameEvents.cs, game/Assets/Scripts/Runtime/UI/{ServicePanel,SettlementPanel}Controller.cs, game/Assets/Scenes/Shop.unity, game/Assets/Tests/EditMode/{PlaceholderArt,ShopPresentationScene,ServicePresentationEvent,SettlementPresentation}Tests.cs
- **notes**: 표현 이벤트는 UI 계층만 발행(Ops 불변). 무대/패널 배치: 무대 상단 밴드(y20~180), 패널 하단(0,-80 · 480×200). 스프라이트는 PlaceholderArtBuilder 재실행으로만 수정. 트윈은 Play 모드 전용(EditMode 는 즉시 스냅).
- **generated_by**: design=codex gpt-5.5/xhigh @codex 0.143.0, 2026-07-09 (fallback=none)
