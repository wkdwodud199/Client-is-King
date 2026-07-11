# Manifest — task-110

> **Load**: 기본 로드 세트의 일부. 이 task 가 *실제로* 의존하는 것만 명시해 컨텍스트를 최소화한다.
> 여기에 없는 개념/파일은 기본적으로 열지 않는다.

- **task_id**: task-110
- **inputs**: `kb/tasks/task-110/design.md`(구현 계약 SSOT), `kb/concepts/project-brief.md`, `kb/concepts/demo-scope.md`, `kb/concepts/development-priority.md`, `kb/tasks/task-109/`(아트 프로토타입 기준선), 현재 Unity 기준선 `game/`
- **concepts_needed**: `kb/concepts/project-brief.md`(로드맵·하드캡 SSOT), `kb/concepts/demo-scope.md`(구현 하드캡), `kb/concepts/development-priority.md`(첫 의미 있는 선택·B급 처리 근거). `art-direction.md`는 task-110 무관(아트 프로토타입 전용).
- **related_files**: `game/Assets/Scripts/Runtime/DayCycle/{GameState,GameEvents}.cs`; 신규 `game/Assets/Scripts/Runtime/Genre/{GenreSelectionResult,GenreDemandPlan,GenreSelectionOps}.cs`; `game/Assets/Scripts/Runtime/Economy/{EconomyOps,EconomyManager}.cs`; `game/Assets/Scripts/Runtime/Service/{ServiceOps,ServiceManager}.cs`; `game/Assets/Scripts/Runtime/Managers/GameManager.cs`; `game/Assets/Scripts/Runtime/UI/{MarketPanelController,PhaseHudController,ServicePanelController,SettlementPanelController}.cs`; `game/Assets/Scripts/Editor/{InitialDataBuilder,SceneBuilder}.cs`; `game/Assets/Data/Definitions/Genres/*.asset`; `game/Assets/Tests/EditMode/{GenreSelectionOps,GenreBalance,EconomyManager,ServiceOps,MarketPanelScene,ServicePanelScene,SettlementPanelScene,SceneBuilder,FirstPlayableLoop}Tests.cs`; 신규 `game/Assets/Tests/PlayMode/{ClientIsKing.Tests.PlayMode.asmdef,GenrePersistencePlayModeTests.cs}`
- **notes**: 이 turn은 **task-110 구현 계약(design.md H절·실행계획·영향파일·테스트기준)만** 구현한다. 제품 GDD v0.9(A~G 절)·PPTX(J 절)·최종 아트는 비실행 설계다. 결정론 필수(FNV-1a `genreId|day|orderIndex`, offset 2166136261/prime 16777619, known vector `gukbap|1|0`=2190636514; `RoundHalfUp(x)=floor(x+0.5)`). 기존 public API·97 EditMode 기준선은 neutral overload로 보존. 씬 2개 하드캡·SceneBuilder 멱등 유지. task-110 UI는 B급 숨기고 C급만(B급 데이터/Ops는 보존). Codex 소유 게이트(코드 리뷰·640×360 시각 승인)는 Claude가 self-approve 하지 않는다.
