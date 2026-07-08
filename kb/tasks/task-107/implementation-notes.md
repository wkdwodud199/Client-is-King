# 구현 노트 — task-107

> Status: done
> Inputs: kb/tasks/task-107/design.md
> Outputs: 정산/파산 도메인 + 지출 추적 + 진행 게이트 + 정산·밤 UI + 테스트 15종 추가 — **M1 첫 플레이어블 완성**
> Next step: **M1 게이트 — 사용자 Play 모드 플레이테스트** (재미 검증 통과 시 task-108 장르 선택 설계로 진행)

## 설계 대비 변경 사항

| 항목 | 설계 내용 | 실제 구현 | 변경 사유 |
|------|-----------|-----------|-----------|
| DontDestroyOnLoad | (설계 미규정) | `Application.isPlaying` 가드 추가 | FirstPlayableLoopTests 가 EditMode 에서 GameManager 를 생성·구동할 수 있게 — Play 동작 불변 |
| 파산 시 버튼 잠금 전파 | "Refresh 에서 파산이면 비활성화" (11단계) | PhaseHud `Update()` 1행 폴링 (상태 불일치 시에만 쓰기) | 파산은 phase 이벤트 없이(정산 중) 발생 — 이벤트 훅 없이 즉시 잠금 반영. 새 GameEvents 추가는 설계가 채택하지 않아 회피 |
| advance 라벨 참조 | EditorInit 로 주입 or 런타임 탐색 허용 (16단계) | 런타임 `GetComponentInChildren<TMP_Text>()` 캐시 | 설계가 명시 허용한 옵션 — EditorInit 시그니처/기존 씬 주입 불변 |

## 구현 결정 기록

1. **정산 cash delta = 운영비뿐** (설계 제약 그대로): 매출은 서빙 시점, 재료비는 구매 시점에 이미 cash 반영
   — 정산은 `DailyOperatingCost(12,000)` 1회 차감 + 표시용 요약(`net = gross − spend − cost`) 기록만.
2. **멱등 정산**: `settlementDay == day` 면 저장된 정산 필드로 결과 재구성(cash 불변, `AlreadyApplied`).
   패널 OnEnable / 진행 버튼 / 테스트 재호출이 겹쳐도 하루 1회 보장.
3. **파산 규약**: `cashBefore < 운영비` → cash 0 고정(음수 불허), `isBankrupt/bankruptcyDay/Reason(미납액 포함)`
   기록. `GameManager.AdvancePhase()` 는 파산 시 현재 phase 반환(이벤트 미발행), Settlement 이탈 전
   미정산이면 선적용 후 파산이면 잔류.
4. **지출 추적은 day-key 리셋**: `marketSpendDay != day` 면 리셋 후 누적 — 정산의 `IngredientSpend` 는
   오늘 지출만 반영 (이전 날 잔재 무시, 테스트로 고정).
5. **완료 일수**: `daysCompleted = max(기존, day)` — 뒤로 가지 않음 (파산 화면의 "버틴 일수" 원천).
6. **첫 플레이어블 smoke (배치 검증 완료, 수동 Play 는 사용자 몫)**: MainMenu → 게임 시작 → 장보기 구매 →
   영업(서빙/포기) → 정산(자동 적용+요약) → 밤(마감 요약) → 다음 날. 운영비 미납 시 파산 화면 + 진행 잠금.

## 발생한 이슈

- 없음. 3게이트(SceneBuilder/컴파일/테스트) 모두 첫 실행 통과 — 72/72.

## 테스트 결과

| 테스트 기준 (design.md 참조) | 결과 | 비고 |
|------------------------------|------|------|
| validator rc0 (design.md) | pass | 러너 게이트 |
| 배치 SceneBuilder.Apply exit 0 | pass | Settlement/Night UI 포함 재생성 (첫 실행) |
| 배치 컴파일 게이트 exit 0 + `error CS` 없음 | pass | |
| EditMode 테스트 exit 0 | pass | **72/72 통과** (+정산 8, 루프 2, 씬 3, 경제 2) |
| GameState 기본값 + 정산/파산 초기값 | pass | defaults 테스트 확장 |
| 구매 성공 시 지출 누적·day 리셋, 실패 시 불변 | pass | `EconomyManagerTests` +2 |
| 정산: 운영비만 차감 (매출 재반영 금지) | pass | `SettlementOpsTests` |
| netProfit = 매출−지출−운영비 (표시용) + 이전 날 지출 무시 | pass | |
| 같은 day 재호출 멱등 (차감 1회, 결과 유지) | pass | |
| 파산: cash 0 고정 + 사유/미납/일차 기록 | pass | 경계(정확히 운영비 = 생존) 별도 검증 |
| 루프: 구매→서빙→정산(선적용)→밤→다음 날 day+1 | pass | `FirstPlayableLoopTests` |
| 파산 시 AdvancePhase 차단 + 이벤트 미발행 | pass | |
| Settlement/Night 패널 컨트롤러+텍스트 존재, 초기 비활성 | pass | `SettlementPanelSceneTests` |
| SettlementManager 탑재 | pass | |
| 기존 테스트 57종 지속 통과 + 씬 2개 유지 | pass | |
| git 캐시/빌드 미노출 | pass | -uall 0건 |
| `--check-done` + `generate-status --check` | pass | 기록 후 실행 (요약 참조) |

## 산출물

- `game/Assets/Scripts/Runtime/Settlement/{SettlementResult,SettlementOps,SettlementManager}.cs`
- `game/Assets/Scripts/Runtime/DayCycle/GameState.cs` — 지출/정산/완료일수/파산 13필드 확장
- `game/Assets/Scripts/Runtime/Economy/EconomyOps.cs` — 당일 지출 누적 (실패 불변 유지)
- `game/Assets/Scripts/Runtime/Managers/GameManager.cs` — 파산 차단 + 정산 선적용 게이트, DDOL Play 가드
- `game/Assets/Scripts/Runtime/UI/{SettlementPanel,NightPanel}Controller.cs` + PhaseHud 라벨/잠금 갱신
- `game/Assets/Scripts/Editor/SceneBuilder.cs` — BuildSettlementPanel/BuildNightPanel + SettlementManager 탑재
- `game/Assets/Scenes/{MainMenu,Shop}.unity` — 재생성
- `game/Assets/Tests/EditMode/{SettlementOps,FirstPlayableLoop,SettlementPanelScene}Tests.cs` + 기존 2종 확장
- `kb/tasks/task-107/{manifest,implementation-notes}.md`, `kb/artifacts/task-107-summary.md`
