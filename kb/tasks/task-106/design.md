# 설계 문서 — task-106

> Status: ready
> Inputs: `kb/concepts/project-brief.md`, `kb/concepts/demo-scope.md`, `kb/tasks/task-105/design.md`, `kb/tasks/task-105/implementation-notes.md`, 현재 Unity 프로젝트 기준선(`game/`)
> Outputs: Service phase 조리·서빙 런타임 상태 확장, `ServiceOps`/`ServiceManager`, Service UI와 `SceneBuilder` 갱신, 조리·서빙 EditMode 테스트 설계
> Next step: Claude가 이 설계를 구현한 뒤 `task-107`에서 정산, 하루 마감, 파산 판정을 진행

## 목표 (Objective)

`Client is King`의 `Service(영업)` phase를 실제로 플레이 가능한 조리·서빙 루프로 만든다. task-105에서 구매한 재료 인벤토리를 사용해 레시피 주문을 처리하고, 성공 시 재료를 소비하며 매출과 당일 서비스 통계를 `GameState`에 누적한다.

## 범위 (Scope)

- 포함:
  - `GameState`에 당일 서비스 상태를 추가한다. 주문 목록, 현재 주문 인덱스, 당일 매출, 완료/실패 주문 수, 서빙/이탈 고객 수를 `JsonUtility` 호환 public field와 `List` 기반 타입으로 표현한다.
  - `ServiceOrderState` 같은 serializable 순수 상태 타입을 추가해 주문의 `recipeId`, `customerId`, `partySize`, 처리 여부를 저장한다. 런타임 상태에는 ScriptableObject 참조를 직접 저장하지 않는다.
  - `ServiceOps`를 추가해 주문 생성, 현재 주문 조회, 필요 재료 계산, 서빙 가능 여부, 조리·서빙 성공 처리, 주문 포기 처리를 순수 C# 규칙으로 제공한다.
  - 주문 생성은 `RecipeDef` 6종과 `CustomerArchetypeDef` 4종 이상을 입력으로 받아 id 정렬 기반의 결정론적 순서로 만든다. 기본 주문 수는 5개로 두고, `day`와 주문 인덱스를 섞어 매일 같은 고정 목록이 반복되지 않게 한다.
  - 한 주문은 한 고객 archetype의 파티가 하나의 레시피를 주문한 것으로 처리한다. 필요 재료 수량과 판매가는 모두 `partySize`를 곱한다.
  - 조리·서빙 성공 조건은 선택한 등급(C/B)의 재료가 레시피 요구량을 모두 충족하는 것이다. 자동 등급 혼합은 하지 않고, UI에서 C/B 등급을 토글해 선택하게 한다.
  - 성공 시 모든 필요 재료를 하나의 트랜잭션처럼 소비하고, `cash += recipe.BasePrice * partySize`, `serviceRevenueToday += 같은 금액`, 완료/서빙 통계를 증가시킨 뒤 다음 주문으로 이동한다.
  - 재료 부족, null 레시피, 처리할 주문 없음, 잘못된 파티 수 같은 실패 경로는 자금·인벤토리·서비스 통계를 변경하지 않고 실패 결과를 반환한다.
  - 주문 포기 버튼을 제공해 현재 주문을 실패 처리하고 다음 주문으로 이동한다. 포기 시 매출과 재료는 변하지 않고 실패 주문 수와 이탈 고객 수만 증가한다.
  - `ServiceManager`를 추가해 `ServiceOps`로 위임하는 thin wrapper를 제공한다. `GameManager` 부트스트랩 오브젝트에 `EconomyManager`/`InventoryManager`와 함께 탑재한다.
  - Service phase 진입 시 해당 day의 주문 목록이 없거나 이전 day 주문이면 `ServiceManager.EnsureServiceDay(...)`가 새 주문 목록과 당일 서비스 통계를 초기화한다.
  - `Panel_Service` placeholder를 실제 Service UI로 교체한다. UI는 현재 주문, 고객/파티, 레시피, 조리 시간, 예상 매출, 선택 등급, 필요 재료와 보유량, 서빙/포기 버튼, 당일 매출/서빙/이탈 통계, 결과 메시지를 표시한다.
  - `SceneBuilder.Apply`를 갱신해 Service UI를 멱등 생성하고 `ServicePanelController`에 `RecipeDef`와 `CustomerArchetypeDef` asset 목록, TMP/Button 참조를 주입한다. 런타임 asset 조회용 `Resources`는 쓰지 않는다.
  - EditMode 테스트를 추가해 주문 생성 결정성, 필요 재료 계산, 성공 시 재료 소비와 매출 증가, 실패 시 불변성, 주문 포기 통계, Service UI 산출물, 기존 SceneBuilder/데이터 테스트 유지 여부를 검증한다.
  - 구현 완료 기록으로 `kb/tasks/task-106/manifest.md`, `implementation-notes.md`, `kb/artifacts/task-106-summary.md`, `kb/index/status.md`를 갱신하도록 포함한다.
- 제외:
  - 정산 화면, 고정비/임대료, 하루 마감, 파산 판정, 당일 통계 리셋의 최종 마감 처리는 `task-107` 범위다.
  - 장르 선택과 장르별 원가·조리시간·객단가·고객층 친화도 배수는 `task-108` 범위다. 이 task의 판매가는 `RecipeDef.BasePrice * partySize`만 사용한다.
  - SNS에 의한 손님 수·분포 변화는 `task-109` 범위다. 이 task에서는 customer `BaseSpawnWeight`를 확률 계산에 쓰지 않고 결정론적 주문 목록만 만든다.
  - 이벤트/장애물에 의한 재료가·위생·단체손님 변동은 `task-110` 범위다.
  - 저장/불러오기 UI와 파일 I/O는 `task-111` 범위다. 단, 추가 상태 타입은 `JsonUtility` 저장을 전제로 만든다.
  - 조리 미니게임, 실시간 타이머 실패, 직원 자동 처리, 좌석/테이블 배치, 인테리어, 다점포, Steam 연동, 미슐랭 트랙은 `demo-scope.md` 주차장 기능이므로 구현하지 않는다.
  - 신규 씬, 신규 ScriptableObject 타입, 외부 UI 프레임워크, 커스텀 아트/폰트는 추가하지 않는다.

## 제약 (Constraints)

- `kb/concepts/project-brief.md`가 코어 루프, 데이터 구조, 매니저 방향, 씬 하드캡의 SSOT다. 이 task는 ScriptableObject 6종을 늘리거나 `MainMenu.unity`/`Shop.unity` 2씬 하드캡을 깨지 않는다.
- Unity 구현 대상은 `game/` 하위로 제한한다. task 기록은 `kb/`에 둔다.
- `GameState`는 후속 `task-111`에서 `JsonUtility`로 직렬화될 수 있어야 한다. `Dictionary`, LINQ 결과 캐시, ScriptableObject 직접 참조를 상태 필드로 저장하지 않는다.
- 핵심 규칙은 scene 없이 테스트 가능한 순수 C# `ServiceOps`에 둔다. `ServiceManager`와 `ServicePanelController`는 상태 접근과 UI 연결을 맡는 얇은 계층으로 유지한다.
- 기존 `InventoryOps`의 실패 불변 계약을 보존한다. 여러 재료를 소비하는 서빙 로직은 먼저 전체 필요량을 검증하고, 모두 충분할 때만 차례로 소비한다.
- 실패 경로는 예외 중심 흐름이 아니라 명시적인 `ServiceResult` 실패로 처리한다. 단, null `GameState`처럼 구현자 오류에 해당하는 입력은 기존 Ops 패턴처럼 명확한 예외를 허용한다.
- 금액, 수량, 고객 수, 주문 수는 정수로 유지한다. 음수 매출, 음수 재료 보유량, 0 이하 파티 크기를 허용하지 않는다.
- `RecipeDef.CookSeconds`와 `CustomerArchetypeDef.PatienceSeconds`는 이 task에서 표시/테스트 가능한 데이터로만 사용한다. 실시간 대기 실패나 코루틴 기반 조리 타이머는 구현하지 않는다.
- UI는 기존 `SceneBuilder` 코드 저작 방식으로만 갱신한다. `Shop.unity`를 수동 편집해 해결하지 않는다.
- `PhaseHudController`의 phase 순서와 패널 토글 계약은 유지한다. Service UI는 phase 전환 규칙을 바꾸지 않고, 활성화 시 표시와 주문 초기화만 동기화한다.

## 구현 단계 (Implementation Steps)

1. `Runtime/Service` 폴더를 만들고 `ServiceOrderState`, `ServiceResult` 같은 순수 상태/결과 타입을 추가한다. `ServiceOrderState`는 `recipeId`, `customerId`, `partySize`, `served`, `missed`를 갖고 기본값이 저장 친화적으로 동작해야 한다.
2. `GameState`를 확장한다. `serviceDay`, `serviceOrders`, `serviceCurrentOrderIndex`, `serviceRevenueToday`, `serviceOrdersServedToday`, `serviceOrdersMissedToday`, `serviceCustomersServedToday`, `serviceCustomersMissedToday`를 추가하고 기본 생성 상태에서 day 1, 빈 주문, 0 통계를 보장한다.
3. `ServiceOps.BuildOrders(...)`를 구현한다. `RecipeDef`와 `CustomerArchetypeDef`를 id 기준으로 정렬한 뒤 `day`와 주문 인덱스를 이용해 기본 5개 주문을 결정론적으로 만든다. 각 주문의 `partySize`는 customer의 `PartySize.Min/Max` 범위 안에서 결정론적으로 선택한다.
4. `ServiceOps.StartServiceDay(GameState, orders, day)`를 구현해 해당 day 주문 목록, 현재 주문 인덱스, 당일 서비스 통계를 초기화한다. `orders`가 비어 있으면 서비스 가능한 주문이 없는 상태로 만들되 예외 없이 UI가 표시할 수 있게 한다.
5. `ServiceOps.GetCurrentOrder(...)`와 `HasOpenOrders(...)`를 구현한다. 이미 처리된 주문을 건너뛰어 다음 미처리 주문을 찾고, 모든 주문이 처리되면 현재 주문 없음 상태를 반환한다.
6. `ServiceOps.CalculateRequiredIngredients(recipe, partySize)`를 구현한다. 레시피 요구량에 파티 크기를 곱하고, 같은 `IngredientKind`가 중복될 가능성에 대비해 kind별 필요량을 합산한다.
7. `ServiceOps.CalculateSalePrice(recipe, partySize)`를 구현한다. 판매가는 `recipe.BasePrice * partySize`이며 장르/품질/이벤트 배수는 적용하지 않는다.
8. `ServiceOps.CanServeOrder(state, recipe, grade, partySize)` 또는 동등 API를 구현한다. 선택 등급의 재료 보유량이 모든 필요량 이상인지 확인하고 부족한 재료 정보를 결과 메시지에 반영할 수 있게 한다.
9. `ServiceOps.TryServeCurrentOrder(state, recipe, grade)`를 구현한다. 현재 주문과 recipe id가 일치하는지 확인하고, 전체 재료 preflight 후에만 재료를 소비한다. 성공 시 cash와 당일 매출/완료 통계를 증가시키고 현재 주문을 `served=true`로 표시한 뒤 다음 주문으로 이동한다.
10. `ServiceOps.SkipCurrentOrder(state)`를 구현한다. 현재 주문이 있으면 `missed=true`, 실패 주문 수 +1, 이탈 고객 수 +partySize, 현재 인덱스 이동을 적용한다. 주문이 없으면 상태 불변 실패를 반환한다.
11. `ServiceManager` MonoBehaviour를 추가한다. 싱글턴 가드는 기존 `EconomyManager`/`InventoryManager`와 같은 패턴을 따르고, `EnsureServiceDay`, `TryServeCurrentOrder`, `SkipCurrentOrder`, 현재 통계 조회를 `ServiceOps`로 위임한다.
12. `SceneBuilder.CreateGameManager()`에 `ServiceManager`를 탑재한다. 필요하면 `using ClientIsKing.Service;`를 추가하고 기존 GameManager 부트스트랩 중복 제거 계약은 바꾸지 않는다.
13. `ServicePanelController`를 추가한다. serialized field로 주문/고객/레시피/조리시간/매출/등급/필요재료/통계/메시지 텍스트, 등급 토글 버튼, 서빙 버튼, 포기 버튼, `List<RecipeDef>`, `List<CustomerArchetypeDef>`를 받는다.
14. `ServicePanelController.OnEnable` 또는 `Start`에서 `ServiceManager.EnsureServiceDay(recipeDefs, customerDefs)`를 호출하고 UI를 갱신한다. 버튼 이벤트는 OnEnable/OnDisable에서 등록·해제한다.
15. Service UI의 등급 선택은 task-105 Market UI와 같은 C/B 토글로 구현한다. 현재 주문의 레시피 요구 재료를 선택 등급 기준으로 표시하고, 보유량이 부족한 항목은 메시지나 요약 텍스트에 드러낸다.
16. 서빙 버튼 클릭 시 `ServiceManager.TryServeCurrentOrder(currentRecipe, selectedGrade)`를 호출하고, 결과 메시지를 표시한 뒤 자금/통계/현재 주문/필요 재료 UI를 즉시 다시 그린다. 포기 버튼도 같은 방식으로 처리한다.
17. `SceneBuilder.BuildShop()`에서 `Panel_Service` placeholder를 `BuildServicePanel(...)`로 교체한다. 패널 크기와 위치는 `Panel_Market`과 충돌하지 않는 기존 phase panel 영역을 사용하고, 초기 활성 상태는 계속 false로 둔다.
18. `SceneBuilder`에 `LoadRecipeDefs()`와 `LoadCustomerDefs()` 헬퍼를 추가한다. `AssetDatabase.FindAssets`로 `Assets/Data/Definitions/Recipes`, `Assets/Data/Definitions/Customers`에서 asset을 로드하고 id 순서로 주입한다.
19. EditMode 테스트를 추가한다. 순수 규칙 테스트는 scene load 없이 `ServiceOps`와 시드 asset을 직접 검증하고, UI 산출물 테스트는 `SceneBuilder.Apply` 후 `Shop` 씬을 열어 Service 패널과 컨트롤러 참조를 검증한다.
20. Unity 배치 모드에서 `SceneBuilder.Apply`, 컴파일 게이트, EditMode 테스트를 실행한다. 마지막으로 구현 기록 문서와 status board를 갱신한다.

## 실행 계획 (Execution Plan)

- implement_model: claude-fable-5
- implement_effort: xhigh
- routing_reason: 조리·서빙은 M1 핵심 루프의 상태 확장, 트랜잭션형 인벤토리 소비, Service UI, SceneBuilder, 테스트를 함께 다루므로 `project-brief.md`의 task-106 라우팅대로 fable-5/xhigh가 적합하다.

| unit | 파일 범위 | depends_on | group |
|------|-----------|------------|-------|
| U1-service-state-and-ops | `game/Assets/Scripts/Runtime/DayCycle/GameState.cs`, `game/Assets/Scripts/Runtime/Service/{ServiceOrderState,ServiceResult,ServiceOps}.cs`, 관련 `.meta` | 없음 | G1 |
| U2-service-manager | `game/Assets/Scripts/Runtime/Service/ServiceManager.cs`, 필요 시 `game/Assets/Scripts/Runtime/Managers/GameManager.cs`, 관련 `.meta` | U1-service-state-and-ops | G2 |
| U3-service-ui-runtime | `game/Assets/Scripts/Runtime/UI/ServicePanelController.cs`, 필요 시 `PhaseHudController.cs`, 관련 `.meta` | U1-service-state-and-ops, U2-service-manager | G3 |
| U4-scene-builder | `game/Assets/Scripts/Editor/SceneBuilder.cs`, `game/Assets/Scenes/Shop.unity`, 관련 `.meta` | U2-service-manager, U3-service-ui-runtime | G4 |
| U5-editmode-tests | `game/Assets/Tests/EditMode/{ServiceOpsTests,ServicePanelSceneTests}.cs`, 필요 시 기존 `DayPhaseMachineTests`/`SceneBuilderTests` | U1-service-state-and-ops, U4-scene-builder | G5 |
| U6-validation-pass | Unity 배치 `SceneBuilder.Apply` 로그, 컴파일 로그, EditMode 결과 XML, `runtime/validator` 결과 | U4-scene-builder, U5-editmode-tests | G6 |
| U7-task-records | `kb/tasks/task-106/manifest.md`, `kb/tasks/task-106/implementation-notes.md`, `kb/artifacts/task-106-summary.md`, `kb/index/status.md` | U6-validation-pass | G7 |

## 파일/모듈 영향 (Affected Files/Modules)

| 파일/모듈 | 변경 유형 | 설명 |
|-----------|-----------|------|
| `game/Assets/Scripts/Runtime/DayCycle/GameState.cs` | modify | 당일 서비스 주문 목록, 현재 주문 인덱스, 매출/완료/실패/고객 수 통계 필드 추가 |
| `game/Assets/Scripts/Runtime/Service/ServiceOrderState.cs` | create | 저장 가능한 주문 상태(`recipeId`, `customerId`, `partySize`, 처리 여부) 정의 |
| `game/Assets/Scripts/Runtime/Service/ServiceResult.cs` | create | 조리·서빙/포기 결과, 메시지, 매출, 통계, 부족 재료 요약을 담는 결과 타입 |
| `game/Assets/Scripts/Runtime/Service/ServiceOps.cs` | create | 주문 생성, 필요 재료 계산, 판매가 계산, 서빙 트랜잭션, 주문 포기 규칙을 담는 순수 C# 코어 |
| `game/Assets/Scripts/Runtime/Service/ServiceManager.cs` | create | `GameManager.Instance.State`를 기준으로 `ServiceOps`에 위임하는 singleton thin wrapper |
| `game/Assets/Scripts/Runtime/UI/ServicePanelController.cs` | create | Service phase UI 표시, 등급 토글, 서빙/포기 버튼, 현재 주문/통계 갱신 담당 |
| `game/Assets/Scripts/Runtime/UI/PhaseHudController.cs` | modify | 필요 시 Service 패널 활성화 갱신 훅만 추가하고 phase 순서/토글 계약은 유지 |
| `game/Assets/Scripts/Editor/SceneBuilder.cs` | modify | `Panel_Service` placeholder를 실제 Service UI로 교체하고 `ServiceManager`, 레시피/고객 asset 목록을 주입 |
| `game/Assets/Scenes/Shop.unity` | modify | SceneBuilder가 재생성하는 Service UI 포함 씬 산출물 |
| `game/Assets/Tests/EditMode/ServiceOpsTests.cs` | create | 주문 생성, 필요 재료, 성공/실패 트랜잭션, 주문 포기 통계를 검증 |
| `game/Assets/Tests/EditMode/ServicePanelSceneTests.cs` | create | `Shop.unity`의 `Panel_Service` 컨트롤러, 필수 UI, 레시피/고객 asset 주입, `ServiceManager` 탑재를 검증 |
| `game/Assets/Tests/EditMode/DayPhaseMachineTests.cs` | modify | 필요 시 `GameState` 기본값 테스트에 서비스 통계 초기값과 빈 주문 목록 조건 추가 |
| `game/Assets/Tests/EditMode/SceneBuilderTests.cs` | modify | 기존 Service placeholder 기대가 있다면 실제 Service UI 산출물과 충돌하지 않게 갱신 |
| `game/**/*.meta` | create/modify | Unity가 생성하는 새 Runtime/UI/Test 파일과 씬 변경에 대응하는 메타 파일 |
| `kb/tasks/task-106/manifest.md` | modify | done-gate 통과를 위해 inputs/concepts_needed/related_files placeholder를 실제 값으로 교체 |
| `kb/tasks/task-106/implementation-notes.md` | create | 구현 결정, 서비스 수식, UI 구성, 검증 결과, Unity 환경 이슈를 기록 |
| `kb/artifacts/task-106-summary.md` | create | task-106 완료 산출물 요약과 task-107 인계 사항 기록 |
| `kb/index/status.md` | modify | `runtime/generate-status.py`로 재생성되는 task 상태 보드 |

## 테스트 기준 (Test Criteria)

- [ ] `python runtime/validator/cli.py kb/tasks/task-106/design.md`가 0으로 통과한다.
- [ ] Unity 배치 모드에서 SceneBuilder가 성공한다: `Unity.exe -batchmode -quit -nographics -projectPath game -executeMethod ClientIsKing.EditorTools.SceneBuilder.Apply -logFile <temp-log>` 종료 코드가 0이다.
- [ ] 배치 컴파일 게이트가 통과한다: `Unity.exe -batchmode -quit -nographics -projectPath game -logFile <temp-log>` 종료 코드가 0이고 로그에 `error CS`가 없다.
- [ ] EditMode 테스트가 통과한다: `Unity.exe -batchmode -nographics -projectPath game -runTests -testPlatform EditMode -testResults <temp-results> -logFile <temp-log>` 종료 코드가 0이다.
- [ ] `GameState` 기본값은 day 1, phase `Market`, 시작 자금 유지, 빈 인벤토리, 빈 서비스 주문, 서비스 통계 0을 보장한다.
- [ ] `ServiceOpsTests`는 주문 생성이 같은 입력과 day에서 결정론적이며, 기본 5개 주문이 모두 유효한 `recipeId`/`customerId`와 1 이상 party size를 갖는다고 검증한다.
- [ ] `ServiceOpsTests`는 필요 재료 계산이 레시피 요구량에 party size를 곱하고 같은 `IngredientKind` 요구량을 합산한다고 검증한다.
- [ ] `ServiceOpsTests`는 판매가가 `RecipeDef.BasePrice * partySize`이고 장르/품질/이벤트 배수를 적용하지 않는다고 검증한다.
- [ ] `ServiceOpsTests`는 충분한 선택 등급 재료가 있으면 서빙 성공 시 해당 재료가 정확히 차감되고 cash와 `serviceRevenueToday`가 같은 판매가만큼 증가한다고 검증한다.
- [ ] `ServiceOpsTests`는 성공 서빙 후 완료 주문 수 +1, 서빙 고객 수 +partySize, 현재 주문 인덱스가 다음 미처리 주문으로 이동한다고 검증한다.
- [ ] `ServiceOpsTests`는 재료가 하나라도 부족하면 cash, 인벤토리, 서비스 통계, 주문 처리 상태가 모두 변경되지 않는다고 검증한다.
- [ ] `ServiceOpsTests`는 C등급 재료가 부족하더라도 B등급 재료를 자동 혼합하지 않으며, 선택 등급을 B로 호출해야 B 재료를 소비한다고 검증한다.
- [ ] `ServiceOpsTests`는 주문 포기 시 매출과 인벤토리는 변하지 않고 실패 주문 수 +1, 이탈 고객 수 +partySize, 현재 주문 인덱스 이동이 일어난다고 검증한다.
- [ ] `ServicePanelSceneTests`는 `Shop.unity`의 `Panel_Service`에 `ServicePanelController`, 현재 주문 텍스트, 고객/파티 텍스트, 레시피 텍스트, 조리 시간 텍스트, 예상 매출 텍스트, 등급 토글, 필요 재료 텍스트, 통계 텍스트, 서빙 버튼, 포기 버튼, 메시지 텍스트가 존재함을 검증한다.
- [ ] `ServicePanelSceneTests`는 `ServicePanelController`에 6개 `RecipeDef`와 최소 4개 `CustomerArchetypeDef`가 주입되어 있고, `GameManager` 부트스트랩 오브젝트에 `ServiceManager`가 탑재되어 있음을 검증한다.
- [ ] 기존 `DataDefinitionTests`, `InventoryManagerTests`, `EconomyManagerTests`, `MarketPanelSceneTests`, `SceneBuilderTests`, `DayPhaseMachineTests`가 계속 통과한다.
- [ ] `EditorBuildSettings.scenes`에는 계속 `Assets/Scenes/MainMenu.unity`, `Assets/Scenes/Shop.unity` 2개만 등록되어 있다.
- [ ] `git status --short game`에 Unity 캐시/빌드 산출물(`Library`, `Temp`, `Obj`, `Logs`, `UserSettings`, `Build*`)이 나타나지 않는다.
- [ ] 구현 완료 후 `python runtime/validator/cli.py --check-done task-106`와 `python runtime/generate-status.py --check`가 통과한다.

## 오픈 이슈 (Open Issues)

- 없음. 주문 수 5개, 판매가 `BasePrice * partySize`, 선택 등급 단일 소비는 첫 플레이어블을 위한 단순 규칙이며, 밸런싱과 배수 적용은 task-107 이후 로드맵에서 조정한다.
