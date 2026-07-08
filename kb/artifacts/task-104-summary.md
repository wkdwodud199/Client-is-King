# 산출물 요약 — task-104

> Status: done
> Inputs: kb/tasks/task-104/implementation-notes.md
> Outputs: 이 요약 문서
> Next step: task-105 설계 요청 (경제·인벤토리 + 장보기 UI + EditMode 테스트, M1)

## 작업 요약

- **Task ID**: task-104
- **제목**: 씬 2종 + 하루 상태 머신 + SceneBuilder
- **완료일**: 2026-07-09

## 산출물 목록

| 산출물 | 경로 | 설명 |
|--------|------|------|
| 하루 상태 머신 | `game/Assets/Scripts/Runtime/DayCycle/` | 순수 C# — Market→Service→Settlement→Night 순환, Night→Market 만 day+1, 전환마다 이벤트 1회 |
| 이벤트 허브 골격 | `.../DayCycle/GameEvents.cs` | C# event 기반 (발행 internal, 구독 public) — 라이브러리 금지 규약 준수 |
| GameManager | `.../Managers/GameManager.cs` | 싱글턴 8종 중 1호 — 상태 보관·phase 진행·씬 로드 얇은 래퍼 |
| UI 컨트롤러 | `.../UI/{MainMenu,PhaseHud}Controller.cs` | 시작 버튼 연결 · day/phase HUD + 패널 4종 토글 |
| SceneBuilder | `game/Assets/Scripts/Editor/SceneBuilder.cs` | 씬 2종 + UI 를 코드 저작하는 멱등 배치 진입점 (수동 씬 편집 금지) |
| 씬 2종 | `game/Assets/Scenes/{MainMenu,Shop}.unity` | Shop 은 URP PixelPerfectCamera (PPU 32, 640x360) — 씬 하드캡 2 고정 |
| 테스트 13종 추가 | `game/Assets/Tests/EditMode/` | 상태 머신 7 + SceneBuilder 산출물 6 — **총 26/26 통과** |
| task 기록 | `kb/tasks/task-104/`, `kb/artifacts/task-104-summary.md` | manifest(provenance)·구현 노트·요약 |

## 주요 결정

- **상태 머신 = 순수 C# 단일 진입점** (전환·day·이벤트 발행) — GameManager 는 얇은 래퍼로 유지.
- **씬은 전체 재생성 멱등 방식** — 두 씬 전체가 빌더 소유, 수정은 빌더 재실행으로만.
- **부트스트랩 이중화** — 두 씬 모두 GameManager 포함, Awake 가드가 중복 제거 (어느 씬에서 시작해도 동작).
- 참조 주입은 task-103 의 **EditorInit 패턴 재사용**, 버튼 리스너는 런타임 자가 연결.
- `Scenes/.gitkeep`·`Data/.gitkeep` 제거 (실콘텐츠 생성으로 목적 소멸).

## 검증

- SceneBuilder.Apply exit 0 (첫 실행 통과) · 컴파일 게이트 exit 0 · **EditMode 26/26** · 캐시 누출 0건

## 관련 문서

- 설계: `kb/tasks/task-104/design.md`
- 구현 노트: `kb/tasks/task-104/implementation-notes.md`
