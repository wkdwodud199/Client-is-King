# 설계 문서 — task-105

> Status: ready
> Inputs: `kb/concepts/project-brief.md`, `kb/concepts/demo-scope.md`, `kb/tasks/task-103/design.md`, `kb/tasks/task-103/implementation-notes.md`, `kb/tasks/task-104/design.md`, `kb/tasks/task-104/implementation-notes.md`, 현재 Unity 프로젝트 기준선(`game/`)
> Outputs: 자금/인벤토리 런타임 상태 확장, `EconomyManager`/`InventoryManager`, 장보기 UI와 `SceneBuilder` 갱신, 경제·인벤토리 EditMode 테스트 설계
> Next step: Claude가 이 설계를 구현한 뒤 `task-106`에서 조리·서빙 코어 루프를 진행

## 목표 (Objective)

`Client is King`의 코어 루프 첫 단계인 `Market(장보기)` phase를 실제로 플레이 가능하게 만든다. task-103의 재료 정적 데이터와 task-104의 `GameState`/`Shop` 씬 골격을 확장해 자금, 인벤토리, 구매 계산, 장보기 UI, 관련 EditMode 테스트를 추가한다.

## 범위 (Scope)

- 포함:
  - `GameState`에 시작 자금과 재료 인벤토리 목록을 추가한다. 저장 호환성을 고려해 `Dictionary`가 아니라 serializable `List` 기반 항목을 사용한다.
  - `IngredientKind` + `IngredientGrade`별 보유 수량을 표현하는 순수 C# 상태 타입을 추가한다.
  - `InventoryManager`를 추가해 수량 조회, 증감, 충분한 재료 보유 여부 검증을 담당하게 한다.
  - `EconomyManager`를 추가해 현재 자금 조회, 지출 가능 여부, 구매 비용 계산, 재료 구매 적용을 담당하게 한다.
  - 구매 수식은 `IngredientDef.UnitCost * quantity`를 기본으로 하고, `quantity`는 양수 정수만 허용한다.
  - 구매 성공 시 자금이 차감되고 인벤토리 수량이 증가해야 한다. 자금 부족, 수량 0 이하, null 재료 정의는 상태를 바꾸지 않고 실패 결과를 반환한다.
  - `Shop` 씬의 `Panel_Market` placeholder를 실제 장보기 UI로 교체한다. UI는 보유 자금, 선택 재료, 선택 등급, 구매 수량, 예상 비용, 보유 수량, 구매 버튼, 실패/성공 메시지를 표시한다.
  - 장보기 UI는 `Assets/Data/Definitions/Ingredients/`의 `IngredientDef` asset을 기준으로 옵션을 구성한다. 초기 선택은 C등급 재료 중 하나로 결정론적으로 잡는다.
  - `SceneBuilder.Apply`를 갱신해 장보기 UI를 멱등 생성하고 `MarketPanelController`에 필요한 참조를 주입한다.
  - EditMode 테스트를 추가해 경제 계산, 인벤토리 증감, 구매 실패 불변성, `GameState` 기본값, SceneBuilder 장보기 UI 산출물을 검증한다.
  - 구현 완료 기록으로 `kb/tasks/task-105/manifest.md`, `implementation-notes.md`, `kb/artifacts/task-105-summary.md`, `kb/index/status.md`를 갱신하도록 포함한다.
- 제외:
  - 조리 큐, 레시피별 재료 소비, 손님 주문, 서빙, 수익 지급은 `task-106` 범위다.
  - 정산 수식, 하루 마감, 고정비, 파산 판정은 `task-107` 범위다.
  - 장르 선택과 장르 배수 적용은 `task-108` 범위다. 이 task의 장보기 가격에는 장르 배수를 적용하지 않는다.
  - SNS 마케팅, 이벤트/장애물, 저장/불러오기는 각각 `task-109`, `task-110`, `task-111` 범위다.
  - 인테리어, 직원 고용, 다점포, Steam 연동, 미슐랭 트랙, 조리 미니게임 등 `demo-scope.md`의 주차장 기능은 구현하지 않는다.
  - 신규 씬, 신규 ScriptableObject 타입, 외부 UI 프레임워크, 커스텀 아트/폰트는 추가하지 않는다.

## 제약 (Constraints)

- `kb/concepts/project-brief.md`가 코어 루프, 데이터 구조, 매니저 방향, 씬 하드캡의 SSOT다. 이 task는 브리프의 ScriptableObject 6종을 늘리거나 장보기 범위를 재정의하지 않는다.
- Unity 프로젝트 경로는 `game/`이며, 모든 구현 대상은 `game/Assets/` 아래에 둔다.
- `GameState`는 후속 저장 task(`task-111`)에서 `JsonUtility`로 직렬화될 수 있어야 한다. 따라서 인벤토리는 `Dictionary` 대신 `[Serializable]` list item으로 표현한다.
- 정적 데이터는 task-103의 `IngredientDef`를 그대로 사용한다. 구매 가격, 표시명, 종류, 등급을 중복 상수로 다시 정의하지 않는다.
- 매니저는 브리프의 싱글턴 MonoBehaviour 방향을 따른다. `EconomyManager`와 `InventoryManager`는 thin wrapper로 유지하고, 계산 가능한 핵심 규칙은 테스트 가능한 순수 C# 메서드로 분리한다.
- `GameManager`는 전역 상태 소유자 역할만 확장한다. 경제/인벤토리 상세 로직을 `GameManager`에 직접 넣지 않는다.
- UI는 기존 `SceneBuilder` 코드 저작 방식으로만 갱신한다. `Shop.unity`를 수동 편집해 해결하지 않는다.
- 씬은 계속 `MainMenu.unity`, `Shop.unity` 2개만 유지한다. `Market`은 `Shop` 씬 내부 phase 패널이다.
- 구매 실패는 예외 중심 흐름이 아니라 명시적인 실패 결과로 처리한다. 단, 구현자 오류에 해당하는 null state/manager 초기화 누락은 명확히 방어한다.
- 금액과 수량은 정수로 유지하고 음수 잔액·음수 보유량을 허용하지 않는다.

## 구현 단계 (Implementation Steps)

1. Runtime에 serializable 인벤토리 항목 타입을 추가한다. 예: `IngredientStock`은 `IngredientKind kind`, `IngredientGrade grade`, `int quantity`를 갖고, quantity는 0 이상으로 보정한다.
2. `GameState`를 확장한다. 기본 시작 자금은 `startingCash = 30000` 수준의 작은 데모 값으로 두고, `cash`와 `List<IngredientStock> ingredientStocks`를 추가한다. 기본 생성자는 day 1, `Market`, 시작 자금, 빈 인벤토리를 보장한다.
3. 순수 C# 구매 결과 타입을 추가한다. 예: `PurchaseResult`는 `bool Success`, `string Message`, `int TotalCost`, `int CashAfter`, `int QuantityAfter`를 담아 UI와 테스트가 같은 계약을 쓰게 한다.
4. `InventoryManager` MonoBehaviour를 추가한다. `GameManager.Instance.State`를 기준으로 재료 수량 조회, `AddIngredient`, `TryConsumeIngredient`, `HasIngredients`를 제공하되, 이번 task UI에서는 구매 증가만 사용한다.
5. `EconomyManager` MonoBehaviour를 추가한다. `CalculatePurchaseCost(IngredientDef, int)`, `CanAfford(int)`, `TryPurchaseIngredient(IngredientDef, int)`를 제공하고 성공 시 cash 차감과 inventory 증가를 한 트랜잭션으로 처리한다.
6. `GameManager`가 새 게임 시작 시 확장된 `GameState`를 초기화하도록 유지하고, 필요한 경우 `EconomyManager`/`InventoryManager`가 같은 씬의 `GameManager` 상태를 안전하게 찾을 수 있는 API를 제공한다.
7. `MarketPanelController`를 추가한다. 직렬화 필드로 보유 자금 텍스트, 재료 선택 dropdown, 등급 선택 dropdown, 수량 입력 또는 stepper용 UI, 예상 비용 텍스트, 보유 수량 텍스트, 구매 버튼, 결과 메시지 텍스트를 받는다.
8. `MarketPanelController`는 `Start()`에서 `IngredientDef` asset 참조 목록을 정렬된 id 순서로 읽어 UI 옵션을 구성한다. 런타임 asset 조회는 빌드 호환을 고려해 `Resources`를 쓰지 말고, `SceneBuilder`가 serialized list로 주입하는 방식을 우선한다.
9. UI 이벤트를 연결한다. 재료/등급/수량 변경 시 예상 비용과 보유 수량을 즉시 갱신하고, 구매 버튼 클릭 시 `EconomyManager.TryPurchaseIngredient` 결과를 표시한 뒤 자금/수량 UI를 다시 그린다.
10. `SceneBuilder.BuildShop()`을 갱신한다. `Panel_Market`의 placeholder 라벨을 제거하고 장보기 UI 오브젝트들을 생성한 뒤 `MarketPanelController.EditorInit(...)`으로 참조와 `IngredientDef` 목록을 주입한다.
11. 기존 `PhaseHudController`의 phase 패널 토글 계약은 유지한다. `MarketPanelController`는 패널 활성/비활성에 따라 표시만 갱신하고 phase 전환 규칙에는 관여하지 않는다.
12. EditMode 테스트를 추가한다. 순수 경제/인벤토리 테스트는 scene load 없이 `GameState`, `InventoryManager`/`EconomyManager` 또는 추출된 calculator API를 직접 검증하고, UI 산출물 테스트는 `SceneBuilder.Apply` 후 `Shop` 씬을 열어 장보기 컨트롤 존재와 참조를 검증한다.
13. Unity 배치 모드에서 `SceneBuilder.Apply`, 컴파일 게이트, EditMode 테스트를 실행한다. 마지막으로 구현 기록 문서와 status board를 갱신한다.

## 실행 계획 (Execution Plan)

- implement_model: claude-fable-5
- implement_effort: high
- routing_reason: M1의 경제·인벤토리 런타임과 Market UI, EditMode 검증을 함께 다루지만 조리·서빙·정산은 제외하므로 `project-brief.md`의 task-105 라우팅대로 fable-5/high가 적합하다.

| unit | 파일 범위 | depends_on | group |
|------|-----------|------------|-------|
| U1-state-and-domain | `game/Assets/Scripts/Runtime/DayCycle/GameState.cs`, `game/Assets/Scripts/Runtime/Inventory/*.cs`, `game/Assets/Scripts/Runtime/Economy/*.cs`, 관련 `.meta` | 없음 | G1 |
| U2-managers | `game/Assets/Scripts/Runtime/Managers/{InventoryManager,EconomyManager}.cs`, 필요 시 `GameManager.cs`, 관련 `.meta` | U1-state-and-domain | G2 |
| U3-market-ui-runtime | `game/Assets/Scripts/Runtime/UI/MarketPanelController.cs`, 필요 시 `PhaseHudController.cs`, 관련 `.meta` | U1-state-and-domain, U2-managers | G3 |
| U4-scene-builder | `game/Assets/Scripts/Editor/SceneBuilder.cs`, `game/Assets/Scenes/Shop.unity`, 관련 `.meta` | U3-market-ui-runtime | G4 |
| U5-editmode-tests | `game/Assets/Tests/EditMode/{EconomyManagerTests,InventoryManagerTests,MarketPanelSceneTests}.cs`, 필요 시 기존 테스트/asmdef | U1-state-and-domain, U2-managers, U4-scene-builder | G5 |
| U6-validation-pass | Unity 배치 `SceneBuilder.Apply` 로그, 컴파일 로그, EditMode 결과 XML, `runtime/validator` 결과 | U4-scene-builder, U5-editmode-tests | G6 |
| U7-task-records | `kb/tasks/task-105/manifest.md`, `kb/tasks/task-105/implementation-notes.md`, `kb/artifacts/task-105-summary.md`, `kb/index/status.md` | U6-validation-pass | G7 |

## 파일/모듈 영향 (Affected Files/Modules)

| 파일/모듈 | 변경 유형 | 설명 |
|-----------|-----------|------|
| `game/Assets/Scripts/Runtime/DayCycle/GameState.cs` | modify | 자금(`cash`)과 `List<IngredientStock>` 기반 재료 인벤토리 상태를 추가 |
| `game/Assets/Scripts/Runtime/Inventory/IngredientStock.cs` | create | `IngredientKind`/`IngredientGrade`별 보유 수량을 나타내는 serializable list item |
| `game/Assets/Scripts/Runtime/Inventory/InventoryManager.cs` | create | 재료 수량 조회, 추가, 소비 가능 여부, 소비 적용을 담당하는 singleton MonoBehaviour |
| `game/Assets/Scripts/Runtime/Economy/PurchaseResult.cs` | create | 구매 성공/실패, 총 비용, 구매 후 자금/수량, UI 메시지를 담는 결과 타입 |
| `game/Assets/Scripts/Runtime/Economy/EconomyManager.cs` | create | 구매 비용 계산, 자금 검증, 구매 트랜잭션 적용을 담당하는 singleton MonoBehaviour |
| `game/Assets/Scripts/Runtime/Managers/GameManager.cs` | modify | 확장된 `GameState` 초기화와 경제/인벤토리 매니저가 참조할 상태 접근 계약 유지 |
| `game/Assets/Scripts/Runtime/UI/MarketPanelController.cs` | create | 장보기 UI 표시, 재료/등급/수량 선택, 예상 비용 갱신, 구매 버튼 처리를 담당 |
| `game/Assets/Scripts/Runtime/UI/PhaseHudController.cs` | modify | 필요 시 Market 패널 활성화 시 장보기 UI refresh를 호출하되 phase 전환 계약은 유지 |
| `game/Assets/Scripts/Runtime/ClientIsKing.Runtime.asmdef` | modify | 새 Runtime 폴더가 기존 Runtime 어셈블리에 포함되는지 유지한다. 외부 참조 추가는 원칙적으로 불필요 |
| `game/Assets/Scripts/Editor/SceneBuilder.cs` | modify | `Panel_Market` placeholder를 실제 장보기 UI로 교체하고 `MarketPanelController` 참조/재료 목록을 주입 |
| `game/Assets/Scenes/Shop.unity` | modify | SceneBuilder가 재생성하는 장보기 UI 포함 씬 산출물 |
| `game/Assets/Tests/EditMode/InventoryManagerTests.cs` | create | 인벤토리 조회, 추가, 소비, 부족 시 불변성을 검증 |
| `game/Assets/Tests/EditMode/EconomyManagerTests.cs` | create | 구매 비용, 자금 차감, 구매 성공/실패, 음수/0 수량 방어를 검증 |
| `game/Assets/Tests/EditMode/MarketPanelSceneTests.cs` | create | `SceneBuilder.Apply` 후 `Shop` 씬의 장보기 UI 필수 오브젝트와 컨트롤러 참조를 검증 |
| `game/Assets/Tests/EditMode/DayPhaseMachineTests.cs` | modify | `GameState` 기본값 테스트에 시작 자금과 빈 인벤토리 조건을 추가 |
| `game/Assets/Tests/EditMode/SceneBuilderTests.cs` | modify | 기존 Market placeholder 검증을 장보기 UI 검증과 충돌하지 않게 갱신 |
| `game/Assets/Tests/EditMode/ClientIsKing.Tests.EditMode.asmdef` | modify | 필요 시 새 Runtime 타입 참조 상태를 유지한다. 별도 외부 패키지 참조는 추가하지 않는다 |
| `game/**/*.meta` | create/modify | Unity가 생성하는 코드/씬/폴더 메타 파일. 관련 asset과 쌍으로 추적 |
| `kb/tasks/task-105/manifest.md` | modify | done-gate 통과를 위해 inputs/concepts_needed/related_files placeholder를 실제 값으로 교체 |
| `kb/tasks/task-105/implementation-notes.md` | create | 구현 결정, 구매 수식, UI 구성, 검증 결과, Unity 환경 이슈를 기록 |
| `kb/artifacts/task-105-summary.md` | create | task-105 완료 산출물 요약과 task-106 인계 사항 기록 |
| `kb/index/status.md` | modify | `runtime/generate-status.py`로 재생성되는 task 상태 보드 |

## 테스트 기준 (Test Criteria)

- [ ] `python runtime/validator/cli.py kb/tasks/task-105/design.md`가 0으로 통과한다.
- [ ] Unity 배치 모드에서 SceneBuilder가 성공한다: `Unity.exe -batchmode -quit -nographics -projectPath game -executeMethod ClientIsKing.EditorTools.SceneBuilder.Apply -logFile <temp-log>` 종료 코드가 0이다.
- [ ] 배치 컴파일 게이트가 통과한다: `Unity.exe -batchmode -quit -nographics -projectPath game -logFile <temp-log>` 종료 코드가 0이고 로그에 `error CS`가 없다.
- [ ] EditMode 테스트가 통과한다: `Unity.exe -batchmode -nographics -projectPath game -runTests -testPlatform EditMode -testResults <temp-results> -logFile <temp-log>` 종료 코드가 0이다.
- [ ] `GameState` 기본값은 day 1, phase `Market`, 양수 시작 자금, 빈 인벤토리 list를 보장한다.
- [ ] `InventoryManagerTests`는 같은 `IngredientKind`/`IngredientGrade` 추가 시 기존 항목 수량을 누적하고, 다른 등급은 별도 항목으로 유지함을 검증한다.
- [ ] `InventoryManagerTests`는 충분한 재료 소비 시 수량을 차감하고, 부족하거나 0 이하 수량 요청 시 상태를 변경하지 않음을 검증한다.
- [ ] `EconomyManagerTests`는 `IngredientDef.UnitCost * quantity`가 구매 총액이며 성공 구매 시 cash가 정확히 차감되고 보유 수량이 정확히 증가함을 검증한다.
- [ ] `EconomyManagerTests`는 자금 부족, null `IngredientDef`, 0 이하 수량에서 구매가 실패하고 cash와 inventory가 변경되지 않음을 검증한다.
- [ ] `MarketPanelSceneTests`는 `Shop.unity`의 `Panel_Market`에 `MarketPanelController`, 보유 자금 텍스트, 재료/등급 선택 컨트롤, 수량 입력, 예상 비용 텍스트, 보유 수량 텍스트, 구매 버튼, 결과 메시지 텍스트가 존재함을 검증한다.
- [ ] `MarketPanelSceneTests`는 `MarketPanelController`에 18개 `IngredientDef` asset이 주입되어 있고 C/B 등급이 모두 UI 선택에 노출될 수 있음을 검증한다.
- [ ] 기존 `DayPhaseMachineTests`, `DataDefinitionTests`, `SceneBuilderTests`가 계속 통과한다.
- [ ] `EditorBuildSettings.scenes`에는 계속 `Assets/Scenes/MainMenu.unity`, `Assets/Scenes/Shop.unity` 2개만 등록되어 있다.
- [ ] `git status --short game`에 Unity 캐시/빌드 산출물(`Library`, `Temp`, `Obj`, `Logs`, `UserSettings`, `Build*`)이 나타나지 않는다.
- [ ] 구현 완료 후 `python runtime/validator/cli.py --check-done task-105`와 `python runtime/generate-status.py --check`가 통과한다.

## 오픈 이슈 (Open Issues)

- 없음. 시작 자금과 구매 단가는 데모 밸런스 초안이며, 이 task에서는 구매 트랜잭션의 정확성과 UI 연결을 먼저 고정한다.
