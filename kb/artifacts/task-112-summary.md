# 산출물 요약 — task-112

> Status: done
> Inputs: kb/tasks/task-112/implementation-notes.md
> Outputs: 이 요약 문서 — 이벤트/장애물 시스템(결정론 도메인 수학·수요/서비스/경제/정산 배선·UI·SceneBuilder·밸런스 guard·PlayMode) 완료 요약과 인계
> Next step: **Codex 코드 리뷰 + 640×360 시각 승인 + 수동 Play smoke + 오너 이벤트 시드 재확정 승인 → 통과 시 장르+SNS+이벤트 3일 수직 슬라이스 통합 게이트 → task-113(저장/불러오기)**

## 작업 요약

- **Task ID**: task-112
- **제목**: 이벤트/장애물 시스템 — 재료값 폭등·위생 점검·임대료 인상·단체 손님 4종, 전날 예고→당일 적용
  인과 사슬, 결정론 FNV-1a 스케줄(시드 47/문턱 450), 100-day 회복 가능 밸런스 가드
- **완료일**: 2026-07-11 (Codex 코드리뷰·640×360 시각승인·수동 smoke·오너 시드 재확정 승인 대기)

## 산출물 목록

| 산출물 | 경로 | 설명 |
|--------|------|------|
| 이벤트 도메인 Ops | `game/Assets/Scripts/Runtime/Events/{ActiveEventState,EventDayEffects,EventOps}.cs` | 순수 C#, FNV-1a 스케줄·수명 전이·효과 합성·예고·DayModifier 합성·정산 원인 라인 단일원천 |
| DayModifier/plan 확장 | `Runtime/Genre/{DayModifier,GenreDemandPlan,GenreSelectionOps}.cs` | 이벤트 수요 축(단체 손님) 3필드, 3분기 고객 pick, base-prefix+SNS-prefix 불변 |
| 서비스 태그/통계 | `Runtime/Service/{ServiceOrderState,ServiceOps,ServiceManager}.cs` | `eventInflow` 태그(세그먼트 범위비교), 단체 파티 override, 이벤트 당일 통계, SNS→이벤트 합성 5단계 |
| 경제/정산 overload | `Runtime/Economy/{EconomyOps,EconomyManager}.cs`, `Runtime/Settlement/{SettlementOps,SettlementManager}.cs` | 이벤트 반영 구매가(2단계 합성)·할증 추적, 운영비 milli+flat overload, `TryCalculatePurchaseCost` 단일 경로 |
| 매니저 배선 | `Runtime/Managers/GameManager.cs` | 이벤트 catalog 소유·fx/forecast API·Night 경계 원자 교체·Settlement/Night 게이트 |
| UI | `Runtime/UI/{NightPanel,MarketPanel,PhaseHud,ServicePanel,SettlementPanel}Controller.cs` | Night 예고 라인(F1 v2)·Market 오늘 이벤트 표시·HUD badge·Service 단체 태그·Settlement 원인 라인 |
| 표현 | `Runtime/Presentation/{ServicePresentationEventArgs,ShopPresentationController}.cs` | 단체 손님 표현 이벤트·무대 라벨(Brass Amber) |
| 데이터/씬 | `Editor/InitialDataBuilder.cs`, `Editor/SceneBuilder.cs`, `Scenes/{MainMenu,Shop}.unity`, `Data/Definitions/Events/*.asset` | 이벤트 시드 재확정(위생 8,000원/단체 4인), Night/Settlement 이벤트 UI·catalog 주입 |
| 이벤트 도메인 테스트 | `Tests/EditMode/EventOpsTests.cs` | 투영·검증·FNV 스케줄(C4 표 전체 재현)·수명 전이·효과 합성·예고==적용·원인 라인 단일원천 (51 tests) |
| 밸런스 테스트 | `Tests/EditMode/EventBalanceTests.cs` | design.md G절 가드 1~7 프로덕션 Ops 재유도, 100-day 전량서빙 시뮬레이션 (9 tests) |
| JSON 왕복 테스트 | `Tests/EditMode/EventJsonRoundTripTests.cs` | activeEvents(영구+시한 혼합) 보존 + plan 동등성 (3 tests) |
| 매니저 게이트 테스트 | `Tests/EditMode/EventManagerGateTests.cs` | 손상 activeEvents의 phase 전환 차단·상태 불변 (5 tests) |
| plan/service 테스트 확장 | `Tests/EditMode/{GenreSelectionOps,ServiceOps}Tests.cs` | 이벤트축 검증·3분기 pick·base/SNS-prefix 불변·세그먼트 태깅·통계 |
| 씬/UI 회귀 테스트 확장 | `Tests/EditMode/{NightPanelScene,SettlementPanelScene,MarketPanelScene,ServicePanelScene,SceneBuilder}Tests.cs` + 신규 `NightPanelEventFlowTests.cs` | F1 v2 좌표·이벤트 예고/표시·원인 라인 worst-case 폭 자동검증·catalog 주입·멱등, 총 330/330 |
| PlayMode | `Tests/PlayMode/GenrePersistencePlayModeTests.cs` | 이벤트 catalog 생존, Day1→2→3 무이벤트/폭등 도메인 경로 검증, 6/6 pass |
| task 기록 | `kb/tasks/task-112/`, 이 요약 | manifest·design·design-review-codex·implementation-notes |

## 주요 결정 / 이탈

- **결정론 수학 고정**: FNV-1a 시드 47/문턱 450 strict `<`, `GenreSelectionOps.Fnv1a`/`RoundHalfUp`/
  `MulMilliHalfUp` 재사용(제3 사본 없음). C4 스케줄 표(day 2~101, 폭등15/단체13/위생16/임대료1, 총45회)를
  `EventBalanceTests.Guard6`이 프로덕션 코드로 정확히 재현.
- **EventDayEffects/DayModifier 분리 유지**(Codex 설계 리뷰 승인 사항): 재료값/운영비는 경제 축 DTO로
  구매·정산 경로에 직접 전달하고, 단체 주문은 `TryComposeDayModifier`로 수요 축에 합성 — Economy/
  Settlement가 SNS 고객 투영 입력에 결합되지 않는다.
- **design-review-codex.md의 request-changes 2건 Action 반영**: (1) `BuildSettlementCauseLine`의
  구매 할증 stale-day 필터를 API 내부 단일 원천으로 고정(`marketSpendDay==fx.Day` 아니면 강제 0원),
  `EventOpsTests`/`EventBalanceTests`에 stale-day 테스트 포함. (2) 다중 이벤트 원인 라인 fallback을
  `≤2 전체 포맷 / ≥3 축약 포맷`으로 구현 전 확정, `SettlementPanelSceneTests`에 worst-case 460px 자동
  검증(TMP_Text.GetPreferredValues) 2건을 U7에서 신규 추가해 수동 시각 승인 의존도를 축소했다.
- **밸런스 가드 1회차 재검산으로 float32 정밀도 이슈 규명**(에스컬레이션 불필요): design.md G절 표의
  면류(noodles) worked value 3건(15,954/20,497/30,579)이 실제 float32(`0.95f==0.949999988...`)
  정밀도를 반영하지 않은 이상화 십진 계산이었음을 진단 테스트로 확인·정정(정확값 15,991/20,538/30,621
  — 근거는 implementation-notes.md 참조). 프로덕션 코드는 design.md의 float32 계약대로 이미 정확했다 —
  변경 없음. 나머지 3장르(국밥/분식/제네럴리스트)는 design.md 표와 1회차부터 정확히 일치.
- **day≥3 기존 테스트 기대값 갱신 — 해당 없음**: 조사 결과 `GameManager.AdvancePhase()`로 day 3
  이상을 진행하는 기존 테스트가 없었다(day: 3 등 리터럴은 전부 순수 도메인 시뮬레이션으로 이벤트
  스케줄과 무관). 조용한 우회 없이 "해당 없음"으로 확정·기록.

## 검증

- 컴파일 exit 0(error CS 0, 4회 반복) · **EditMode 330/330**(2회 연속) · **PlayMode 6/6**(2회 연속) ·
  씬 멱등성(오브젝트 총수 동일) · `git status --short game`에 금지 디렉터리 없음 · 신규 파일 전부
  `.meta` 존재 · Build Settings 씬 정확히 2개(하드캡 불변) · 이벤트 4종 하드캡 불변(demo-scope.md).
- **중요 발견(향후 세션 참고)**: Unity 배치 테스트에서 `-runTests`는 `-quit`과 함께 쓰면 결과 파일이
  생성되지 않는 무증상 실패가 발생한다 — `-quit` 없이 실행하고 프로세스 종료를 폴링해야 한다.

## 미결(Codex/오너 게이트)

- Codex 코드 리뷰(U1~U8 전체 diff, request-changes 2건 반영에 대한 재검토) — 미수행.
- 640×360 원본 캡처 시각 승인(Night v2·EventEffectText·축약 카피 톤) — 미수행(self-approve 금지).
- 수동 Play smoke(Day1→3 인과 사슬, Night 판단 60초 이내) — 미수행.
- 오너 이벤트 시드 재확정 승인(위생 8,000원/단체 4인) — design.md 오픈 이슈, 미승인.
- design.md G절 표 면류 worked value 정정 여부 — Codex/오너 판단 대상(테스트 코드만 정정, 문서 미변경).

## 관련 문서

- 설계: `kb/tasks/task-112/design.md`, Codex 교차검토: `kb/tasks/task-112/design-review-codex.md`
- 구현 노트: `kb/tasks/task-112/implementation-notes.md` (세 그룹 상세, float32 재검산 근거 포함)
