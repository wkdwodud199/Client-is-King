# 산출물 요약 — task-115

> Status: done
> Inputs: kb/tasks/task-115/implementation-notes.md
> Outputs: 이 요약 문서 — 밸런싱(운영비 시드 재확정)·엔딩(파생 도메인+표현)·Windows 빌드 완료 요약과 인계
> Next step: **오너 플레이테스트 + 640×360 시각 승인(엔딩 오버레이 포함) + 빌드 exe 스모크 + Codex 코드리뷰 → 통과 시 M3 완료 = 데모 완료 선언**

## 작업 요약

- **Task ID**: task-115
- **제목**: 밸런싱 + 엔딩 + Windows 빌드 (M3 최종 — 데모 완료)
- **완료일**: 2026-07-12 (오너 플레이테스트·640×360 시각 승인·빌드 exe 스모크·Codex 코드리뷰 대기)

## 산출물 목록

| 산출물 | 경로 | 설명 |
|--------|------|------|
| 밸런스 상수 | `game/Assets/Scripts/Runtime/Settlement/SettlementOps.cs`, `Runtime/Events/EventOps.cs` | `DailyOperatingCost`/`BaseDailyOperatingCost` 12,000→**15,000**(오너 승인, 설계 원안 28,000/밴드[22k,30k] 폐기) |
| 엔딩 도메인 | `game/Assets/Scripts/Runtime/DayCycle/{EndingOps.cs,EndingSummary.cs}` | 파생 전용(`isBankrupt→Bankrupt`, `daysCompleted≥7→Cleared`), GameState 필드 0 추가, `SaveSchemaVersion` v1 유지 |
| 클리어 게이트 | `game/Assets/Scripts/Runtime/Managers/GameManager.cs` | `CanAdvancePhase`/`AdvancePhase` 클리어 차단(파산 게이트 미러), `LoadMainMenuScene()` 신설 |
| 엔딩 표현 | `game/Assets/Scripts/Runtime/UI/EndingOverlayController.cs`, `Editor/SceneBuilder.cs`(`BuildEndingOverlay`) | `Panel_Ending` 640×360 모달(상태 폴링), 클리어="데모 클리어!"/파산="게임 오버" |
| MainMenu 클리어 분기 | `game/Assets/Scripts/Runtime/UI/MainMenuController.cs` | `RefreshSaveUi` 4→5분기(파산→클리어→정상 우선순위, `EndingOps.IsCleared` 단일 판정) |
| Windows 빌드 도구 | `game/Assets/Scripts/Editor/BuildTool.cs` | `BuildWindows()` — StandaloneWindows64, 실패 시 예외(exit 비제로), `game/Build/Windows/ClientIsKing.exe` |
| 테스트(EditMode) | `game/Assets/Tests/EditMode/{EndingOps,GameManagerEndingGate,BalanceEndingGuard,EndingOverlayScene,BuildTool}Tests.cs`(신규) + `{GenreBalance,SNSBalance,EventBalance,EventOps,NightPanelEventFlow,SceneBuilder,MainMenuSaveFlow}Tests.cs`(갱신) | 486/486 green |
| 테스트(PlayMode) | `game/Assets/Tests/PlayMode/EndingPlayModeTests.cs`(신규) | 9/9 green(기존 8 + 신규 1) |
| 빌드 산출물 | `game/Build/Windows/ClientIsKing.exe`(+ 동반 데이터) | gitignored, exit 0, ≈97.7MB, 빌드 소요 ≈2분31초 |
| task 기록 | `kb/tasks/task-115/{design.md,design-review-codex.md,implementation-notes.md}`, 이 요약 | 설계 수정(오너 승인)·실측 근거·G1/G2/G3 요약·게이트 결과표 |

## 주요 결정

- **B3 밴드 폐기(오너 승인)**: design.md 원안(운영비 28,000, 밴드[22,000,30,000])은 "100일 평균
  기여이익"과 "100일 중 최악의 단일 날 마진"을 혼동한 전제 오류였다. 실측(early-exit 없는 전수
  진단)으로 `gukbap` day32(임대료 인상 영구 활성)의 실제 최악 마진이 원래(12,000) 기준 +5,031원
  뿐임을 확인했고, 이 값이 조건을 만족하는 최댓값을 16,373원(여유 0)으로 묶는다. 오너가 "온건"
  노선으로 **15,000**(여유 1,581, 12,000 대비 +25%)을 최종 채택했다.
- **엔딩은 파생만으로 성립(C1)**: `GameState` 직렬화 필드를 추가하지 않고 `isBankrupt`/
  `daysCompleted`에서 엔딩 상태를 파생한다 — `SaveSchemaVersion` v1 유지, 기존 세이브 전부 호환.
- **스코프 밖 fixture 3개는 최소 침습으로 처리**: `SettlementPanelSceneTests`/`SettlementPresentationTests`
  는 15,000 확정값에서 재설계 없이 그대로 통과(byte-unchanged). `GenrePersistencePlayModeTests`의
  Day3 구매 검증은 밸런스 시드 변경의 실제 collateral(Day1~2 무구매로 cash 소진)이라 소액 현금
  보충 1줄만 추가했다(fixture 의도·구조는 보존).
- **BuildTool 은 `-nographics` 미사용**(플레이어 빌드의 셰이더 처리 안전 우선, 배치 테스트와
  다른 선택임을 코드 주석에 명기) — 첫 실행에서 URP 관련 설정 자동 채움(1회성, 수동 변경 아님)이
  관찰되어 기록했다.

## 검증

- EditMode: **486/486** (`-quit` 없이 실행, 무회귀 — G1 완료 시점 470 + G2 신규 16).
- PlayMode: **9/9** (`-quit` 없이 실행, 기존 8 + 신규 `EndingPlayModeTests` 1).
- BuildWindows: **exit 0**, `game/Build/Windows/ClientIsKing.exe` 존재 확인(≈97.7MB), 소요 ≈2분31초.
- `git check-ignore game/Build/Windows/ClientIsKing.exe` 성공 — 빌드 산출물 git 오염 없음.
- whitelist 8지점(SNS 숏폼 12,000·SaveOpsTests V3 fixture) **byte-unchanged** 재확인.
- `.gitignore`/`ProjectSettings/companyName`/`productName` 무변경 확인(빌드 1회성 자동 설정 diff는
  구현 노트에 기록·커밋 판단은 오너 몫).

## 미결(오너/Codex 게이트)

- 오너 플레이테스트 — 밸런스 시드 15,000 체감(N=7 데모 완주) 확인.
- 640×360 시각 승인 3종(엔딩 오버레이 클리어/게임오버 포함) — Claude self-approve 금지.
- 빌드 exe 실행 스모크(오너 더블클릭) — 새 게임→7일→클리어/파산→이어하기 전체 플레이.
- Codex 코드리뷰 — G1/G2/G3 전체 diff.

## 관련 문서

- 설계: `kb/tasks/task-115/design.md`, Codex 교차검토: `kb/tasks/task-115/design-review-codex.md`
- 구현 노트: `kb/tasks/task-115/implementation-notes.md`(설계 수정·실측 근거·G1/G2/G3 상세)
