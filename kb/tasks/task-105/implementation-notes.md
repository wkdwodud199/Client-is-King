# 구현 노트 — task-105

> Status: done
> Inputs: kb/tasks/task-105/design.md
> Outputs: 자금/인벤토리 상태 확장 + 순수 경제/인벤 Ops + 매니저 2종 + 장보기 UI + 테스트 16종 추가
> Next step: task-106 설계 요청 (`runtime/codex-design.ps1 task-106 --auto`) — 조리·서빙 코어 루프

## 설계 대비 변경 사항

| 항목 | 설계 내용 | 실제 구현 | 변경 사유 |
|------|-----------|-----------|-----------|
| 재료/등급 선택 컨트롤 | "재료 선택 dropdown, 등급 선택 dropdown" | ◀ ▶ 순환 셀렉터(재료) + 토글 버튼(등급 — C/B 2값) | TMP_Dropdown 은 템플릿 계층(Viewport/Content/Item) 코드 저작 비용이 크고 640x360 픽셀 UI 에 부적합. 설계 테스트 기준의 "재료/등급 선택 컨트롤" 요건은 충족 — C/B 모두 노출 가능함을 테스트로 검증 |
| 수량 입력 | "수량 입력 또는 stepper" | − / ＋ stepper (1~99) | 설계가 허용한 두 옵션 중 batch 저작이 단순한 쪽 |
| 매니저 파일 위치 | 설계 영향 표: Inventory/Economy 폴더 | `Runtime/Inventory/InventoryManager.cs`, `Runtime/Economy/EconomyManager.cs` (표 그대로) | 도메인 폴더 응집 — 네임스페이스도 폴더 일치 |
| AdvanceButton 위치 | (설계 미규정) | 우하단 → 우상단 (250,150) 이동 + 전체 라벨 raycastTarget=false | Market 패널(480×300)이 하단을 차지해 겹침 — 클릭 차단 방지 |

## 구현 결정 기록

1. **핵심 규칙 = 순수 static Ops** (`EconomyOps`/`InventoryOps`), 매니저는 thin wrapper (설계 제약 그대로).
   EditMode 테스트는 Ops 를 직접 검증 — MonoBehaviour 매니저는 EditMode 에서 DontDestroyOnLoad/Awake
   제약이 있어 순수 코어 분리가 테스트 가능성의 핵심.
2. **구매 트랜잭션 계약**: 성공 시에만 `cash -= cost; InventoryOps.Add(...)` — 실패(자금 부족/null 재료/
   0 이하 수량)는 상태 완전 불변 + `PurchaseResult(Success,Message,TotalCost,CashAfter,QuantityAfter)` 반환.
   null GameState 는 구현자 오류로 `ArgumentNullException` (설계 제약의 이원화).
3. **매니저 배치**: EconomyManager/InventoryManager 를 GameManager 부트스트랩 GO 에 탑재 — DDOL 을 함께
   타고, GO 중복 제거는 GameManager 가 담당하므로 각 매니저 Awake 는 Instance 유지 가드만.
4. **테스트 fixture 는 시드 asset 재사용** — IngredientDef 를 테스트에서 CreateInstance+EditorInit 하는 대신
   task-103 시드(`rice_c`, `beef_b`)를 로드해 def.UnitCost 로 기대값 역산. InternalsVisibleTo 확장 불필요 +
   수치 하드코딩 0.
5. **시작 자금 = `GameState.StartingCash` 상수(30000)** — 테스트/코드가 같은 원천 참조. B급 소고기(1900원)
   기준 15개 남짓 — "재정 부족으로 저급부터" 압박이 자연 발생하는 초안 밸런스.
6. **UI 갱신 경로**: 패널 재활성화(다음 날 Market 복귀) 시 `OnEnable → RefreshAll` 로 자금/보유 최신화.
   구매 후엔 result.Message 표시 + 즉시 재갱신. phase 전환 규칙에는 불관여 (PhaseHud 소관 유지).

## 발생한 이슈

- 없음. SceneBuilder/컴파일/테스트 3게이트 모두 첫 실행 통과. (누적 패턴: 어셈블리 경계와 EditorInit
  규약이 안정화된 뒤로는 task 당 이슈 0 유지.)

## 테스트 결과

| 테스트 기준 (design.md 참조) | 결과 | 비고 |
|------------------------------|------|------|
| validator rc0 (design.md) | pass | 러너 게이트 |
| 배치 SceneBuilder.Apply exit 0 | pass | 장보기 UI 포함 재생성 |
| 배치 컴파일 게이트 exit 0 + `error CS` 없음 | pass | |
| EditMode 테스트 exit 0 | pass | **42/42 통과** (+경제 6, 인벤 8, 장보기 UI 2) |
| GameState 기본값: day1/Market/양수 시작자금/빈 인벤 | pass | `GameState.StartingCash=30000` |
| 인벤: 동일 종류×등급 누적, 등급별 별도 항목 | pass | `InventoryManagerTests` |
| 인벤: 충분 소비 차감, 부족/0 이하 불변 | pass | |
| 경제: UnitCost×qty, 성공 시 정확 차감/증가 | pass | 시드 def 역산 검증 |
| 경제: 자금 부족/null/0 이하 실패 + 불변 | pass | |
| 장보기 UI: 컨트롤러+컨트롤 12종 존재 | pass | `MarketPanelSceneTests` |
| 18개 IngredientDef 주입 + 종류별 C/B 노출 | pass | |
| 기존 테스트(DayPhase/Data/SceneBuilder) 지속 통과 | pass | 42 중 26 |
| Build Settings 씬 2개 유지 | pass | |
| git 캐시/빌드 미노출 | pass | -uall 0건 |
| `--check-done` + `generate-status --check` | pass | 기록 후 실행 (요약 참조) |

## 산출물

- `game/Assets/Scripts/Runtime/Inventory/{IngredientStock,InventoryOps,InventoryManager}.cs`
- `game/Assets/Scripts/Runtime/Economy/{PurchaseResult,EconomyOps,EconomyManager}.cs`
- `game/Assets/Scripts/Runtime/DayCycle/GameState.cs` — cash + ingredientStocks 확장
- `game/Assets/Scripts/Runtime/UI/MarketPanelController.cs` — 장보기 UI 컨트롤러
- `game/Assets/Scripts/Editor/SceneBuilder.cs` — BuildMarketPanel + 매니저 탑재 + 라벨 raycast 차단
- `game/Assets/Scenes/{MainMenu,Shop}.unity` — 재생성 (장보기 UI 포함)
- `game/Assets/Tests/EditMode/{EconomyManager,InventoryManager,MarketPanelScene}Tests.cs` + defaults 확장
- `kb/tasks/task-105/{manifest,implementation-notes}.md`, `kb/artifacts/task-105-summary.md`
