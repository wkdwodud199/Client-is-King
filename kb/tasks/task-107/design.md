# 설계 문서 — task-107

> Status: ready
> Inputs: `kb/concepts/project-brief.md`, `kb/concepts/demo-scope.md`, `kb/tasks/task-106/design.md`, `kb/tasks/task-106/implementation-notes.md`, 현재 Unity 프로젝트 기준선(`game/`)
> Outputs: Settlement phase 정산/하루 마감/파산 런타임 상태 확장, `SettlementOps`/`SettlementManager`, Settlement/Night UI와 `SceneBuilder` 갱신, 첫 플레이어블 검증 설계
> Next step: Claude가 이 설계를 구현한 뒤 M1 첫 플레이어블을 사용자 플레이테스트하고, 통과 시 `task-108` 장르 선택 시스템으로 진행

## 목표 (Objective)

`Client is King`의 M1 코어 루프를 닫기 위해 `Market → Service → Settlement → Night → Market` 하루 흐름을 실제 플레이 가능한 재정 루프로 완성한다. task-105의 구매 지출, task-106의 영업 매출/통계를 정산 화면에 모으고, 하루 운영비를 차감한 뒤 파산이면 진행을 멈추는 첫 플레이어블 게이트를 만든다.

## 범위 (Scope)

- 포함:
  - `GameState`에 당일 구매 지출, 정산 결과, 완료 일수, 파산 상태를 저장 친화적인 public field로 추가한다.
  - `EconomyOps.TryPurchaseIngredient` 성공 경로에서 당일 구매 지출을 누적한다. 구매 실패는 기존처럼 자금/인벤토리/지출 통계를 모두 변경하지 않는다.
  - `SettlementOps`를 추가해 일일 정산을 순수 C# 규칙으로 처리한다. 매출은 task-106에서 이미 `cash`에 반영되므로 정산에서는 다시 더하지 않고, 하루 운영비만 한 번 차감한다.
  - 하루 운영비는 `SettlementOps.DailyOperatingCost = 12000` 상수로 둔다. 이는 데모용 기본 임대료+운영비이며, task-110의 임대료 인상 이벤트를 구현하지 않는다.
  - 정산 요약 수식은 `grossRevenue = serviceRevenueToday`, `ingredientSpend = marketSpendToday`, `operatingCost = DailyOperatingCost`, `netProfit = grossRevenue - ingredientSpend - operatingCost`로 고정한다.
  - 정산 적용은 day당 정확히 1회만 수행한다. `settlementDay == day`이면 재호출해도 cash를 다시 차감하지 않고 기존 결과를 반환한다.
  - 운영비 납부 전 cash가 운영비보다 작으면 `cash`를 0으로 보정하고 `isBankrupt = true`, `bankruptcyDay = day`, `bankruptcyReason`을 기록한다. cash 음수는 허용하지 않는다.
  - `GameManager.AdvancePhase()`는 파산 상태에서 더 이상 phase를 진행하지 않는다. `Settlement`에서 다음 phase로 진행하려 할 때 정산이 아직 적용되지 않았으면 먼저 정산을 적용하고, 그 결과 파산이면 `Settlement`에 머문다.
  - `SettlementManager`를 추가해 `SettlementOps`에 위임하는 thin wrapper를 제공하고, `GameManager` 부트스트랩 오브젝트에 탑재한다.
  - `Panel_Settlement` placeholder를 실제 정산 UI로 교체한다. UI는 당일 매출, 구매 지출, 운영비, 순손익, 정산 전/후 cash, 서빙/이탈 통계, 파산 여부 메시지를 표시한다.
  - `Panel_Night` placeholder를 하루 마감 UI로 교체한다. UI는 정산 후 보유 cash, 완료 일수, 다음 날 진행 가능 여부, 파산 상태를 표시하되 SNS/저장 기능은 만들지 않는다.
  - `PhaseHudController`는 phase별 패널 토글 계약을 유지하면서 파산 시 진행 버튼을 비활성화하고, 버튼 라벨을 현재 phase에 맞게 갱신한다.
  - `SceneBuilder.Apply`를 갱신해 Settlement/Night UI를 멱등 생성하고 `SettlementPanelController`/`NightPanelController` 참조를 주입한다.
  - EditMode 테스트를 추가해 구매 지출 누적, 정산 수식, 운영비 1회 차감, 파산 판정, phase 진행 차단, Settlement/Night UI 산출물, 기존 Market/Service 테스트 지속 통과를 검증한다.
  - 구현 완료 기록으로 `kb/tasks/task-107/manifest.md`, `implementation-notes.md`, `kb/artifacts/task-107-summary.md`, `kb/index/status.md`를 갱신하도록 포함한다.
- 제외:
  - 장르 선택과 장르별 원가·조리시간·객단가·고객층 친화도 배수는 `task-108` 범위다. 정산 수식에 장르 배수를 넣지 않는다.
  - SNS 마케팅 UI/실행, 익일 손님 수·분포 변경은 `task-109` 범위다. Night UI는 SNS placeholder를 실제 기능처럼 보이게 만들지 않는다.
  - 이벤트/장애물, 임대료 인상 이벤트, 재료값 폭등, 위생 점검, 단체 손님은 `task-110` 범위다. 이번 task의 운영비 상수는 이벤트 시스템이 아니다.
  - 저장/불러오기 파일 I/O는 `task-111` 범위다. 추가 상태 필드는 `JsonUtility` 호환만 보장한다.
  - PlayMode 자동화, 튜토리얼, 난이도 선택, 밸런싱 고도화, 엔딩, Windows 빌드는 후속 task 범위다.
  - Steam 연동, 직원 고용, 인테리어, 다점포, 미슐랭 트랙, 조리 미니게임 등 `demo-scope.md`의 주차장 기능은 구현하지 않는다.
  - 신규 씬, 신규 ScriptableObject 타입, 외부 UI 프레임워크, 커스텀 아트/폰트는 추가하지 않는다.

## 제약 (Constraints)

- `kb/concepts/project-brief.md`가 코어 루프, 데이터 구조, 매니저 방향, 씬 하드캡의 SSOT다. 이 task는 ScriptableObject 6종을 늘리거나 `MainMenu.unity`/`Shop.unity` 2씬 하드캡을 깨지 않는다.
- Unity 구현 대상은 `game/` 하위로 제한한다. task 기록은 `kb/`에 둔다.
- `GameState`는 후속 `task-111`에서 `JsonUtility`로 직렬화될 수 있어야 한다. `Dictionary`, ScriptableObject 직접 참조, LINQ 캐시를 상태 필드로 저장하지 않는다.
- 핵심 정산 규칙은 scene 없이 테스트 가능한 순수 C# `SettlementOps`에 둔다. `SettlementManager`, `SettlementPanelController`, `NightPanelController`는 상태 접근과 UI 표시만 맡는 얇은 계층으로 유지한다.
- cash는 음수가 되면 안 된다. 파산은 `cash < DailyOperatingCost` 상태에서 정산을 적용할 때 발생하며, 적용 후 cash는 0으로 고정한다.
- task-106의 서빙 성공은 이미 `cash += salePrice`와 `serviceRevenueToday += salePrice`를 수행한다. 정산에서 매출을 다시 더하면 이중 반영 버그이므로 금지한다.
- 당일 구매 지출은 구매 성공 시에만 누적한다. 같은 날 Market 패널을 여러 번 열거나 구매 실패가 발생해도 지출 통계가 왜곡되면 안 된다.
- 정산 적용은 idempotent해야 한다. Settlement 패널 활성화, 진행 버튼 클릭, 테스트 재호출이 겹쳐도 운영비 차감은 하루 1회만 일어난다.
- `DayPhaseMachine`의 phase 순서 규칙은 유지한다. 파산 진행 차단과 Settlement 선적용은 `GameManager.AdvancePhase()` 또는 그 주변 thin wrapper에서 처리하고, 순수 phase 순서 함수 자체를 복잡하게 만들지 않는다.
- UI는 기존 `SceneBuilder` 코드 저작 방식으로만 갱신한다. `Shop.unity`를 수동 편집해 해결하지 않는다.
- 첫 플레이어블은 키보드/마우스 기본 UI 클릭 흐름이면 충분하다. 새 입력 시스템, 애니메이션, 사운드, 세이브 파일, PlayMode 테스트 러너 도입은 하지 않는다.

## 구현 단계 (Implementation Steps)

1. `GameState`를 확장한다. `marketSpendDay`, `marketSpendToday`, `settlementDay`, `settlementGrossRevenue`, `settlementIngredientSpend`, `settlementOperatingCost`, `settlementNetProfit`, `settlementCashBefore`, `settlementCashAfter`, `daysCompleted`, `isBankrupt`, `bankruptcyDay`, `bankruptcyReason`을 추가하고 새 게임 기본값을 명확히 둔다.
2. `EconomyOps.TryPurchaseIngredient` 성공 경로에 당일 구매 지출 누적을 추가한다. `state.marketSpendDay != state.day`이면 `marketSpendDay = state.day`, `marketSpendToday = 0`으로 보정한 뒤 이번 구매 비용을 더한다.
3. `SettlementResult` readonly struct를 추가한다. day, applied/alreadyApplied, bankrupt, gross revenue, ingredient spend, operating cost, net profit, cash before/after, unpaid cost, message를 담아 UI와 테스트가 같은 계약을 쓰게 한다.
4. `SettlementOps`를 추가한다. `DailyOperatingCost = 12000`, `IsSettlementApplied(GameState)`, `ApplyDailySettlement(GameState)`를 제공하고 null state는 명확한 예외로 처리한다.
5. `ApplyDailySettlement`는 `settlementDay == day`이면 기존 `GameState`의 정산 필드로 결과를 재구성해 반환하고 cash를 변경하지 않는다.
6. 아직 적용되지 않은 day이면 `serviceRevenueToday`, 당일 `marketSpendToday`, 운영비를 읽어 정산 필드를 채운다. `cashBefore`는 현재 cash이고, `cashAfter`는 운영비 납부 후 값이다.
7. `cashBefore >= DailyOperatingCost`이면 `cash -= DailyOperatingCost`, `daysCompleted = max(daysCompleted, day)`, 파산 아님 결과를 반환한다.
8. `cashBefore < DailyOperatingCost`이면 `cash = 0`, `isBankrupt = true`, `bankruptcyDay = day`, `bankruptcyReason`과 미납 운영비를 기록하고 파산 결과를 반환한다.
9. `SettlementManager` MonoBehaviour를 추가한다. 싱글턴 패턴은 `EconomyManager`/`ServiceManager`와 맞추고, `ApplyDailySettlement`, `IsAppliedToday`, `LatestResult` 또는 동등 조회 API를 `SettlementOps`로 위임한다.
10. `GameManager.AdvancePhase()`를 보강한다. `state.isBankrupt`이면 현재 phase를 반환하고 이벤트를 새로 발행하지 않는다. 현재 phase가 `Settlement`이고 오늘 정산이 아직이면 `SettlementOps.ApplyDailySettlement(state)`를 먼저 호출하며, 그 결과 파산이면 phase를 진행하지 않는다.
11. `PhaseHudController`를 갱신한다. `Refresh`에서 `GameManager.Instance.State`를 읽어 파산이면 advance button을 비활성화하고, phase별 버튼 라벨을 `영업 시작`, `정산`, `밤으로`, `다음 날`처럼 갱신한다. 라벨 주입 방식은 기존 `EditorInit` 패턴을 따른다.
12. `SettlementPanelController`를 추가한다. OnEnable에서 `SettlementManager.ApplyDailySettlement()`를 호출하고, 당일 매출/구매 지출/운영비/순손익/cash 전후/서빙·이탈 통계/파산 메시지를 TMP 텍스트에 표시한다.
13. `NightPanelController`를 추가한다. OnEnable 또는 `GameEvents.DayPhaseChanged` 구독으로 정산 후 상태를 표시하고, 파산이면 게임 오버 문구와 최종 완료 일수를 보여준다. 비파산이면 다음 날 진입 가능 상태와 현재 cash를 보여준다.
14. `SceneBuilder.CreateGameManager()`에 `SettlementManager`를 탑재한다.
15. `SceneBuilder.BuildShop()`에서 `Panel_Settlement` placeholder를 `BuildSettlementPanel(...)`로, `Panel_Night` placeholder를 `BuildNightPanel(...)`로 교체한다. 두 패널은 기존 phase panel 영역을 사용하고 초기 활성 상태는 false로 유지한다.
16. `SceneBuilder` 공통 버튼 생성 헬퍼가 advance button 라벨 TMP 참조를 주입하기 어렵다면, `PhaseHudController.EditorInit`에 Button만 넘기고 런타임에서 `GetComponentInChildren<TMP_Text>()`로 라벨을 찾는 방식을 허용한다.
17. EditMode 테스트를 추가한다. 순수 규칙 테스트는 scene load 없이 `GameState`, `EconomyOps`, `SettlementOps`, `GameManager`를 검증하고, UI 산출물 테스트는 `SceneBuilder.Apply` 후 `Shop` 씬을 열어 Settlement/Night 컨트롤러와 필수 TMP 오브젝트를 검증한다.
18. 첫 플레이어블 smoke 체크를 문서화한다. MainMenu에서 시작해 Market 구매, Service 서빙/포기, Settlement 정산, Night 마감, 다음 날 Market 복귀가 클릭 흐름으로 가능해야 하며, 파산 시 진행 버튼이 막혀야 한다.
19. Unity 배치 모드에서 `SceneBuilder.Apply`, 컴파일 게이트, EditMode 테스트를 실행한다. 마지막으로 구현 기록 문서와 status board를 갱신한다.

## 실행 계획 (Execution Plan)

- implement_model: claude-fable-5
- implement_effort: high
- routing_reason: M1 완료 작업으로 정산 도메인, 하루 마감 UI, 파산 게이트, SceneBuilder와 테스트를 함께 다루지만 신규 데이터/복잡한 AI는 없으므로 `project-brief.md`의 task-107 라우팅대로 fable-5/high가 적합하다.

| unit | 파일 범위 | depends_on | group |
|------|-----------|------------|-------|
| U1-settlement-state-and-ops | `game/Assets/Scripts/Runtime/DayCycle/GameState.cs`, `game/Assets/Scripts/Runtime/Settlement/{SettlementResult,SettlementOps}.cs`, 관련 `.meta` | 없음 | G1 |
| U2-economy-spend-tracking | `game/Assets/Scripts/Runtime/Economy/EconomyOps.cs`, 필요 시 `PurchaseResult.cs`, 관련 테스트 | U1-settlement-state-and-ops | G2 |
| U3-manager-and-phase-gate | `game/Assets/Scripts/Runtime/Settlement/SettlementManager.cs`, `game/Assets/Scripts/Runtime/Managers/GameManager.cs`, `game/Assets/Scripts/Runtime/UI/PhaseHudController.cs` | U1-settlement-state-and-ops | G2 |
| U4-settlement-night-ui | `game/Assets/Scripts/Runtime/UI/{SettlementPanelController,NightPanelController}.cs`, 관련 `.meta` | U1-settlement-state-and-ops, U3-manager-and-phase-gate | G3 |
| U5-scene-builder | `game/Assets/Scripts/Editor/SceneBuilder.cs`, `game/Assets/Scenes/Shop.unity`, 관련 `.meta` | U3-manager-and-phase-gate, U4-settlement-night-ui | G4 |
| U6-editmode-tests | `game/Assets/Tests/EditMode/{SettlementOpsTests,SettlementPanelSceneTests,FirstPlayableLoopTests}.cs`, 필요 시 기존 `DayPhaseMachineTests`/`EconomyManagerTests`/`SceneBuilderTests` | U1-settlement-state-and-ops, U2-economy-spend-tracking, U5-scene-builder | G5 |
| U7-validation-pass | Unity 배치 `SceneBuilder.Apply` 로그, 컴파일 로그, EditMode 결과 XML, `runtime/validator` 결과 | U5-scene-builder, U6-editmode-tests | G6 |
| U8-task-records | `kb/tasks/task-107/manifest.md`, `kb/tasks/task-107/implementation-notes.md`, `kb/artifacts/task-107-summary.md`, `kb/index/status.md` | U7-validation-pass | G7 |

## 파일/모듈 영향 (Affected Files/Modules)

| 파일/모듈 | 변경 유형 | 설명 |
|-----------|-----------|------|
| `game/Assets/Scripts/Runtime/DayCycle/GameState.cs` | modify | 당일 구매 지출, 정산 결과, 완료 일수, 파산 상태/사유 필드 추가 |
| `game/Assets/Scripts/Runtime/Economy/EconomyOps.cs` | modify | 구매 성공 시 `marketSpendToday`를 현재 day 기준으로 누적하고 실패 불변 계약 유지 |
| `game/Assets/Scripts/Runtime/Settlement/SettlementResult.cs` | create | 정산 적용 여부, 수입/지출/운영비/순손익/cash 전후/파산 정보를 담는 결과 타입 |
| `game/Assets/Scripts/Runtime/Settlement/SettlementOps.cs` | create | 하루 운영비 1회 차감, 정산 요약 계산, 파산 판정을 수행하는 순수 C# 코어 |
| `game/Assets/Scripts/Runtime/Settlement/SettlementManager.cs` | create | `GameManager.Instance.State`를 기준으로 `SettlementOps`에 위임하는 singleton thin wrapper |
| `game/Assets/Scripts/Runtime/Managers/GameManager.cs` | modify | 파산 시 phase 진행 차단, Settlement phase 이탈 전 정산 선적용, 기존 새 게임 초기화 유지 |
| `game/Assets/Scripts/Runtime/UI/PhaseHudController.cs` | modify | 파산 시 advance button 비활성화, phase별 진행 버튼 라벨 갱신, 기존 패널 토글 계약 유지 |
| `game/Assets/Scripts/Runtime/UI/SettlementPanelController.cs` | create | Settlement phase 진입 시 정산 적용 및 정산 요약/파산 메시지 표시 |
| `game/Assets/Scripts/Runtime/UI/NightPanelController.cs` | create | 하루 마감 요약, cash, 완료 일수, 다음 날 진행 가능 여부 또는 파산 상태 표시 |
| `game/Assets/Scripts/Editor/SceneBuilder.cs` | modify | `SettlementManager` 탑재, `Panel_Settlement`/`Panel_Night` 실제 UI 생성 및 컨트롤러 참조 주입 |
| `game/Assets/Scenes/Shop.unity` | modify | SceneBuilder가 재생성하는 Settlement/Night UI 포함 씬 산출물 |
| `game/Assets/Tests/EditMode/SettlementOpsTests.cs` | create | 정산 수식, 운영비 차감, idempotency, 파산 판정, cash 음수 방지를 검증 |
| `game/Assets/Tests/EditMode/FirstPlayableLoopTests.cs` | create | 순수/경량 런타임 흐름으로 Market 구매 지출, Service 매출, Settlement, Night→Market 진행과 파산 차단을 검증 |
| `game/Assets/Tests/EditMode/SettlementPanelSceneTests.cs` | create | `Shop.unity`의 Settlement/Night 패널 컨트롤러, 필수 TMP/버튼 상태, `SettlementManager` 탑재를 검증 |
| `game/Assets/Tests/EditMode/EconomyManagerTests.cs` | modify | 구매 성공 시 당일 구매 지출 누적, 실패 시 지출 불변 검증 추가 |
| `game/Assets/Tests/EditMode/DayPhaseMachineTests.cs` | modify | `GameState` 기본값에 정산/파산 필드 초기값 검증 추가 |
| `game/Assets/Tests/EditMode/SceneBuilderTests.cs` | modify | 기존 placeholder 기대가 있다면 실제 Settlement/Night UI 산출물과 충돌하지 않게 갱신 |
| `game/**/*.meta` | create/modify | Unity가 생성하는 새 Runtime/UI/Test 파일과 씬 변경에 대응하는 메타 파일 |
| `kb/tasks/task-107/manifest.md` | modify | done-gate 통과를 위해 inputs/concepts_needed/related_files placeholder를 실제 값으로 교체 |
| `kb/tasks/task-107/implementation-notes.md` | create | 구현 결정, 정산 수식, UI 구성, 첫 플레이어블 smoke 결과, Unity 환경 이슈를 기록 |
| `kb/artifacts/task-107-summary.md` | create | task-107 완료 산출물 요약과 M1 플레이테스트 인계 사항 기록 |
| `kb/index/status.md` | modify | `runtime/generate-status.py`로 재생성되는 task 상태 보드 |

## 테스트 기준 (Test Criteria)

- [ ] `python runtime/validator/cli.py kb/tasks/task-107/design.md`가 0으로 통과한다.
- [ ] Unity 배치 모드에서 SceneBuilder가 성공한다: `Unity.exe -batchmode -quit -nographics -projectPath game -executeMethod ClientIsKing.EditorTools.SceneBuilder.Apply -logFile <temp-log>` 종료 코드가 0이다.
- [ ] 배치 컴파일 게이트가 통과한다: `Unity.exe -batchmode -quit -nographics -projectPath game -logFile <temp-log>` 종료 코드가 0이고 로그에 `error CS`가 없다.
- [ ] EditMode 테스트가 통과한다: `Unity.exe -batchmode -nographics -projectPath game -runTests -testPlatform EditMode -testResults <temp-results> -logFile <temp-log>` 종료 코드가 0이다.
- [ ] `GameState` 기본값은 day 1, phase `Market`, 시작 자금, 빈 인벤토리/서비스 주문, `marketSpendToday = 0`, `settlementDay = 0`, `daysCompleted = 0`, `isBankrupt = false`를 보장한다.
- [ ] `EconomyManagerTests` 또는 동등 테스트는 구매 성공 시 `marketSpendToday`가 구매 총액만큼 증가하고, 자금 부족/null 재료/0 이하 수량 실패 시 지출 통계가 변경되지 않음을 검증한다.
- [ ] `SettlementOpsTests`는 매출을 cash에 다시 더하지 않고 `DailyOperatingCost`만 cash에서 차감한다고 검증한다.
- [ ] `SettlementOpsTests`는 `netProfit = serviceRevenueToday - marketSpendToday - DailyOperatingCost`이며, 이 값은 표시용 요약이고 실제 Settlement cash delta는 운영비 차감뿐임을 검증한다.
- [ ] `SettlementOpsTests`는 같은 day에 `ApplyDailySettlement`를 여러 번 호출해도 운영비가 한 번만 차감되고 기존 결과가 유지됨을 검증한다.
- [ ] `SettlementOpsTests`는 cash가 운영비보다 작을 때 cash를 0으로 만들고 `isBankrupt`, `bankruptcyDay`, `bankruptcyReason`, unpaid cost를 기록함을 검증한다.
- [ ] `FirstPlayableLoopTests`는 비파산 흐름에서 Market 구매 지출, Service 매출, Settlement 운영비 차감, Night→Market day +1 진행이 모두 가능한 것을 검증한다.
- [ ] `FirstPlayableLoopTests`는 파산 상태에서 `GameManager.AdvancePhase()`가 phase를 진행하지 않고 추가 `DayPhaseChanged` 이벤트도 발행하지 않음을 검증한다.
- [ ] `SettlementPanelSceneTests`는 `Shop.unity`의 `Panel_Settlement`에 `SettlementPanelController`, 매출/구매지출/운영비/순손익/cash 전후/서비스 통계/메시지 텍스트가 존재함을 검증한다.
- [ ] `SettlementPanelSceneTests`는 `Panel_Night`에 `NightPanelController`, 하루 마감 요약, cash, 완료 일수, 상태 메시지 텍스트가 존재함을 검증한다.
- [ ] `SettlementPanelSceneTests`는 `GameManager` 부트스트랩 오브젝트에 `SettlementManager`가 탑재되어 있음을 검증한다.
- [ ] 기존 `DataDefinitionTests`, `InventoryManagerTests`, `EconomyManagerTests`, `MarketPanelSceneTests`, `ServiceOpsTests`, `ServicePanelSceneTests`, `SceneBuilderTests`, `DayPhaseMachineTests`가 계속 통과한다.
- [ ] `EditorBuildSettings.scenes`에는 계속 `Assets/Scenes/MainMenu.unity`, `Assets/Scenes/Shop.unity` 2개만 등록되어 있다.
- [ ] 수동 Play smoke 기준: MainMenu에서 시작해 Market 구매, Service 서빙/포기, Settlement 정산, Night 마감, 다음 날 Market 복귀가 가능하고, 운영비 부족 파산 시 진행 버튼이 비활성화된다.
- [ ] `git status --short game`에 Unity 캐시/빌드 산출물(`Library`, `Temp`, `Obj`, `Logs`, `UserSettings`, `Build*`)이 나타나지 않는다.
- [ ] 구현 완료 후 `python runtime/validator/cli.py --check-done task-107`와 `python runtime/generate-status.py --check`가 통과한다.

## 오픈 이슈 (Open Issues)

- 없음. `DailyOperatingCost = 12000`과 첫 플레이어블의 수익성은 M1 플레이테스트용 초안이며, 재미 검증 결과에 따른 밸런싱은 task-107 완료 후 M2 진입 전에 조정한다.
