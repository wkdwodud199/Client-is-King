# 산출물 요약 — task-110

> Status: done
> Inputs: kb/tasks/task-110/implementation-notes.md
> Outputs: 이 요약 문서 — 장르 선택 시스템(도메인 수학·경제/서비스·매니저 배선·UI·SceneBuilder·밸런스 guard·PlayMode) 완료 요약과 인계
> Next step: **Codex 640×360 시각 승인 + 코드 리뷰 → 오너 승인 → 커밋 → task-111(SNS)**

## 작업 요약

- **Task ID**: task-110
- **제목**: 장르 선택 시스템 — 국밥/분식/면류/제네럴리스트 전문 분야, 결정론적 수요 예측, 경제/서비스 배수 적용
- **완료일**: 2026-07-11 (커밋 완료 `9265da5`; Codex 코드리뷰·640×360 시각승인·수동 smoke 대기)

## 산출물 목록

| 산출물 | 경로 | 설명 |
|--------|------|------|
| 장르 도메인 Ops | `game/Assets/Scripts/Runtime/Genre/{GenreSelectionResult,GenreDemandPlan,GenreSelectionOps}.cs` | 순수 C#, RoundHalfUp/FNV-1a/milli-weight/plan 생성·주문 pick 결정론 수학 |
| 경제/서비스 genre overload | `Runtime/Economy/EconomyOps.cs`, `Runtime/Service/ServiceOps.cs` | 장르 배수 적용 원가/판매가, neutral overload 하위호환 보존 |
| 매니저 배선 | `Runtime/Managers/GameManager.cs`, `Runtime/Service/ServiceManager.cs`, `Runtime/Economy/EconomyManager.cs` | genre catalog·`CanAdvancePhase`·원자적 `AdvancePhase`·plan 검증 |
| UI | `Runtime/UI/{MarketPanelController,PhaseHudController,ServicePanelController,SettlementPanelController}.cs` | 장르 선택 modal, 구매 잠금, HUD badge, 1인가/예상총액, 정산 원인 문구 |
| 데이터/씬 | `Editor/InitialDataBuilder.cs`, `Editor/SceneBuilder.cs`, `Scenes/{MainMenu,Shop}.unity` | 국밥(1.15,1.20,0.95)·분식(0.85,0.80,1.05)·면류(0.95,1.00,0.95)·제네럴리스트(1,1,1) 확정, 씬 오브젝트 생성 |
| 밸런스 테스트 | `Tests/EditMode/GenreBalanceTests.cs` | 100-day 재유도, ±1%·max/min 1.10·생존 guard·4축 비지배 |
| 씬/UI 회귀 테스트 | `Tests/EditMode/{MarketPanelScene,ServicePanelScene,SettlementPanelScene,SceneBuilder,EconomyManager,ServiceOps}Tests.cs` | 구조적+상태(GenreFlow) fixture 분리, 총 152/152 |
| 테스트 인프라 | `Tests/EditMode/TestSceneSupport.cs` | 배치 EditMode 의 Awake/Start/OnEnable 미보장 우회 헬퍼(테스트 전용) |
| PlayMode | `Tests/PlayMode/{ClientIsKing.Tests.PlayMode.asmdef,GenrePersistencePlayModeTests.cs}` | MainMenu→Shop persistent instance 생존, UI 없이 AdvancePhase 원자성, 2/2 pass |
| task 기록 | `kb/tasks/task-110/`, 이 요약 | manifest·design·implementation-notes |

## 주요 결정 / 이탈

- **결정론 수학 고정**: `RoundHalfUp(x)=floor(x+0.5)`, FNV-1a(offset 2166136261/prime 16777619) known vector
  `gukbap|1|0`→2190636514 확정. milli-weight/roll/누적선택 전부 정수 연산으로 재현 가능.
- **밸런스 guard 1회차 통과**: 100-day 재유도 max/min=1.0628(≤1.10), 4장르 평균이 설계값 ±0.05% 이내 — 재밸런싱·
  에스컬레이션 불필요.
- **씬 테스트 fixture 분리**: 배치 EditMode 의 `OpenScene`/`AddComponent` 가 `Awake/Start/OnEnable` 동기 호출을
  보장하지 않는 문제를 발견 — `TestSceneSupport`(리플렉션 기반 singleton 강제 동기화 + lifecycle 강제 호출)로
  우회하고, 구조적 테스트와 상태 변경 테스트를 별도 fixture 클래스로 분리해 상호 오염을 막았다. production
  코드는 무변경.
- **neutral overload 전량 보존** — 기존 EconomyOps/ServiceOps 공개 API 그대로 컴파일·통과.

## 검증

- 컴파일 exit 0(error CS 0, 수 회 반복) · **EditMode 152/152**(2회 연속) · **PlayMode 2/2**(2회 연속) · 씬 멱등성
  (오브젝트 총수 동일 + persistent listener 0) · `git status --short game` 에 금지 디렉터리 없음 · 신규 파일 전부 `.meta` 존재.
- M1~M1.5 기존 루프 규칙·수치·GameState 필드·씬 2개 하드캡 불변.

## 미결(Codex/오너 게이트)

- 640×360 원본 캡처 시각 승인 — 미수행(self-approve 금지).
- 수동 Play smoke(60초 내 첫 선택) — 미수행.
- Codex 코드 리뷰 — 미수행.

## 관련 문서

- 설계: `kb/tasks/task-110/design.md`
- 구현 노트: `kb/tasks/task-110/implementation-notes.md` (그룹1~3 상세, 밸런스 재유도 실측값, 씬 격리 트러블슈팅 포함)
