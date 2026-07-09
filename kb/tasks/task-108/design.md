# 설계 문서 — task-108

> Status: ready
> Inputs: `kb/concepts/project-brief.md`, `kb/concepts/demo-scope.md`, `kb/tasks/task-107/design.md`, `kb/tasks/task-107/implementation-notes.md`, `kb/artifacts/task-107-summary.md`, 현재 Unity 프로젝트 기준선(`game/`)
> Outputs: M1.5 표현 미니 패스 구현 설계 — Shop 씬 무대/손님/서빙/정산/밤 연출, placeholder art provenance, SceneBuilder 갱신, 검증 기준
> Next step: Claude가 이 설계를 구현한 뒤 M1.5 사용자 Play 모드 플레이테스트를 진행하고, 통과 시 `task-109` 장르 선택 시스템으로 진행

## 목표 (Objective)

`Client is King`의 M1 기계적 루프를 사용자 재미 평가가 가능한 "보이는 게임"으로 만든다. 기존 Market/Service/Settlement/Night 규칙과 수치는 유지하면서 Shop 씬에 가게 무대, 손님 스프라이트, 서빙 피드백, 정산 카운트업, 밤 톤 전환을 추가한다.

## 범위 (Scope)

- 포함:
  - `Shop.unity`에 코드 생성 기반 가게 무대 레이어를 추가한다. 구성은 가게 내부 배경, 손님 입장/대기 영역, 카운터, 음식 아이콘 표시 위치, 매출 팝업 위치, 밤 오버레이를 포함한다.
  - `SceneBuilder`가 Shop 씬 전체를 멱등 재생성하는 기존 계약을 유지하면서 무대 레이어와 UI 패널 배치를 갱신한다. `MainMenu.unity`는 기존 기능만 유지한다.
  - 640×360 기준에서 손님 영역과 카운터가 첫 화면에 보이도록 Market/Service/Settlement/Night 패널을 재배치한다. UI 텍스트와 버튼은 겹치지 않아야 하며 기존 조작 흐름은 유지한다.
  - 고객 archetype 4종(`student`, `office_worker`, `family_parent`, `senior_regular`)에 대응하는 placeholder 손님 스프라이트를 준비한다.
  - 레시피 6종에 대응하는 placeholder 음식 아이콘을 준비한다. 서빙 성공 시 현재 주문 레시피 아이콘을 잠깐 표시한다.
  - placeholder art는 `Assets/Art/Placeholders/` 하위에 둔다. 구현자는 프로젝트 생성형 CC0 placeholder 또는 외부 CC0 asset 중 하나를 사용하되, 출처/생성 방식을 같은 폴더의 provenance 문서에 기록한다.
  - placeholder texture import 설정은 PPU 32, Point filter, compression none, Sprite type으로 고정한다.
  - `GameEvents`에 표현 전용 C# event를 추가한다. 이벤트는 외부 이벤트버스 라이브러리 없이 같은 Runtime assembly 안에서 발행한다.
  - `ServicePanelController`는 서빙/포기 처리 직전의 현재 주문 정보를 캡처하고, 처리 결과를 표현 이벤트로 발행한다. `ServiceOps`의 순수 도메인 규칙에는 Unity 오브젝트나 연출 의존성을 넣지 않는다.
  - Service phase 진입 또는 주문 갱신 시 손님이 입장 위치에서 카운터로 이동하고, 현재 고객 archetype/파티 크기/주문 메뉴가 무대에 표시된다.
  - 서빙 성공 시 손님은 만족 상태로 퇴장하고, 음식 아이콘과 `+N원` 팝업이 코루틴/lerp로 표시된다. 포기/이탈 시 손님은 불만 상태로 퇴장하고, 매출 팝업은 표시하지 않는다.
  - `SettlementPanelController`는 정산 적용 결과를 기존처럼 한 번만 계산하되, 표시 텍스트는 카운트업 연출 후 최종값이 정확히 남도록 한다.
  - Settlement phase에서는 순손익/잔액 변화에 짧은 강조 연출을 넣는다. 실제 cash delta와 정산 멱등성은 task-107 규칙을 그대로 따른다.
  - Night phase에서는 Shop 씬 전체에 어두운 오버레이를 적용하고, Market/Service/Settlement phase로 돌아오면 오버레이를 해제한다.
  - 연출 시간은 짧게 유지한다. 모든 클릭 조작은 연출 중에도 입력이 영구 차단되지 않아야 하며, 게임 진행 상태는 연출 완료 여부에 의존하지 않는다.
  - EditMode 테스트를 추가해 placeholder asset import 설정, SceneBuilder 산출물, 표현 이벤트 발행, 시각 컨트롤러 참조 주입, 기존 M1 루프 테스트 지속 통과를 검증한다.
  - 구현 완료 기록으로 `kb/tasks/task-108/manifest.md`, `implementation-notes.md`, `kb/artifacts/task-108-summary.md`, `kb/index/status.md`를 갱신하도록 포함한다.
- 제외:
  - 장르 선택, 장르별 원가/조리시간/객단가/고객층 친화도 배수는 `task-109` 범위다.
  - SNS 마케팅, 익일 손님 수/분포 변경, Night phase의 실제 SNS 조작은 `task-110` 범위다.
  - 이벤트/장애물, 임대료 인상, 재료값 폭등, 위생 점검, 단체 손님은 `task-111` 범위다.
  - 저장/불러오기 파일 I/O는 `task-112` 범위다. 표현 상태를 `GameState`에 저장하지 않는다.
  - 아트 마감, 밸런싱 고도화, 엔딩, Windows 빌드는 `task-113~114` 범위다.
  - 신규 씬, 신규 ScriptableObject 타입, 신규 매니저 singleton, 외부 tween/animation/UI 프레임워크는 추가하지 않는다.
  - Steam 연동, 직원 고용, 인테리어, 다점포, 미슐랭 트랙, 조리 미니게임 등 `demo-scope.md`의 주차장 기능은 구현하지 않는다.
  - 유료 asset, 라이선스가 불명확한 asset, 데모 하드캡을 넘는 커스텀 아트 제작은 포함하지 않는다.
  - 사운드는 이번 task의 필수 산출물이 아니다. 이미 provenance가 명확한 CC0 사운드가 로컬에 있는 경우에만 선택적으로 연결할 수 있다.

## 제약 (Constraints)

- `kb/concepts/project-brief.md`가 게임 컨셉, Unity 버전, 픽셀 표준, 씬 하드캡, 아키텍처 규약의 SSOT다. 이 설계는 `MainMenu.unity`와 `Shop.unity` 2씬 하드캡을 깨지 않는다.
- Unity 구현 대상은 `game/` 하위로 제한한다. task 기록은 `kb/`에 둔다.
- 기존 M1 플레이어블 규칙을 변경하지 않는다. 시작 자금, 구매 가격, 주문 생성, 판매가, 운영비, 정산 멱등성, 파산 판정은 task-105~107 테스트가 고정한 값 그대로 유지한다.
- 표현 레이어는 저장 대상이 아니다. `GameState`에 `RectTransform`, `Sprite`, `Texture2D`, coroutine 상태, 애니메이션 진행률 같은 Unity 참조나 transient 필드를 추가하지 않는다.
- `ServiceOps`, `SettlementOps`, `EconomyOps`, `InventoryOps`는 순수 C# 도메인 규칙 계층으로 유지한다. 연출 이벤트 발행은 UI/controller 계층에서 수행한다.
- `GameEvents` 확장은 C# `event` 기반이어야 한다. DI, ECS, UniRx, DOTween, LeanTween, 외부 이벤트버스 라이브러리는 사용하지 않는다.
- 트윈은 `MonoBehaviour.StartCoroutine`, `Mathf.Lerp`, `Color.Lerp`, `RectTransform.anchoredPosition` 보간만 사용한다. 게임 규칙은 코루틴 완료를 기다리지 않는다.
- placeholder art는 PPU 32, Point filter, no compression을 유지한다. SceneBuilder 또는 전용 editor builder가 asset을 생성/갱신하더라도 import 설정이 테스트로 고정되어야 한다.
- UI는 640×360 기준 해상도에서 텍스트가 버튼/패널 밖으로 넘치거나 다른 UI와 겹치면 안 된다. 패널을 줄일 경우 텍스트 크기와 줄바꿈을 함께 조정한다.
- `CanvasScaler` 기준 해상도 640×360, PixelPerfectCamera PPU 32, 한글 TMP 폰트 사용 계약을 유지한다.
- `SceneBuilder.Apply`는 멱등해야 한다. 같은 입력에서 여러 번 실행해도 중복 오브젝트, 누락 참조, 깨진 `.meta`가 생기면 안 된다.
- 새 asset이나 코드 파일을 만들 때 Unity `.meta`가 함께 생성되어야 한다. `Library`, `Temp`, `Obj`, `Logs`, `UserSettings`, `Build*` 산출물은 git에 노출하지 않는다.
- Play 모드 수동 테스트 전에도 배치 `SceneBuilder.Apply`, 컴파일 게이트, EditMode 테스트가 통과해야 한다.

## 구현 단계 (Implementation Steps)

1. `Assets/Art/Placeholders/` 하위 구조를 확정한다. 권장 경로는 `Customers/`, `FoodIcons/`, provenance 문서다.
2. `PlaceholderArtBuilder` editor utility를 추가하거나 `SceneBuilder` 내부 helper를 확장해 손님 4종과 음식 아이콘 6종 placeholder sprite asset을 준비한다.
3. placeholder sprite가 프로젝트 생성형이면 provenance 문서에 생성 방식과 CC0 placeholder 의도를 기록한다. 외부 CC0 asset이면 asset명, 출처 URL, 라이선스명, 사용 파일 목록을 기록한다.
4. asset importer를 설정한다. `TextureImporter.textureType = Sprite`, `spritePixelsPerUnit = 32`, `filterMode = Point`, compression none, mipmap off를 적용하고 테스트 가능하게 만든다.
5. `Runtime/Presentation` 폴더를 추가한다. 표현 이벤트 args, sprite catalog entry, 간단한 tween helper, `ShopPresentationController`를 배치한다.
6. `ServicePresentationEventArgs`를 추가한다. 최소 필드는 `day`, `orderNumber`, `totalOrders`, `customerId`, `recipeId`, `partySize`, `served`, `missed`, `revenueGained`, `message`다.
7. `SettlementPresentationEventArgs`를 추가한다. `SettlementResult`의 day, gross/spend/operating/net/cash before/after/bankrupt/message를 표현 계층이 읽을 수 있게 한다.
8. `GameEvents`에 `ServiceOrderPresented`, `ServiceOutcomeResolved`, `SettlementPresented` 또는 동등한 표현 이벤트를 추가한다. 발행 메서드는 Runtime assembly 내부에서만 호출되게 유지한다.
9. `ShopPresentationController`를 구현한다. serialized reference로 stage root, customer slot, customer image, customer label, order label, food icon image, cash popup text, settlement pulse text, night overlay image, customer/recipe sprite catalog를 받는다.
10. `ShopPresentationController.OnEnable/OnDisable`에서 `GameEvents.DayPhaseChanged`와 새 표현 이벤트를 구독/해제한다. 비활성 오브젝트나 누락 singleton이 있어도 예외 없이 조용히 복구한다.
11. Service phase 진입 이벤트를 받으면 night overlay를 끄고, 현재 주문 이벤트를 받으면 customer image를 해당 archetype sprite로 바꾼 뒤 입장 위치에서 카운터 위치로 lerp 이동한다.
12. 서빙 성공 이벤트를 받으면 음식 아이콘을 짧게 표시하고 `+{revenueGained:N0}원` 팝업을 위로 이동/페이드시킨다. 이후 customer image는 만족 색조 또는 상태 라벨을 잠깐 표시한 뒤 퇴장 위치로 이동한다.
13. 포기/이탈 이벤트를 받으면 customer image는 불만 색조 또는 상태 라벨을 표시한 뒤 퇴장한다. revenue가 0인 경우 cash popup은 표시하지 않는다.
14. `ServicePanelController.OnEnable`은 `EnsureServiceDay`와 `RefreshAll` 후 현재 주문 정보를 `GameEvents`로 발행한다. 주문이 없으면 customer slot을 비우는 이벤트를 발행한다.
15. `ServicePanelController.OnServe`와 `OnSkip`은 처리 전 `CurrentOrder`와 매칭 recipe/customer 정보를 캡처하고, `ServiceManager` 호출 후 `ServiceOutcomeResolved`를 발행한다. 그 다음 `RefreshAll` 후 다음 현재 주문을 다시 발행한다.
16. `SettlementPanelController`는 `ApplyDailySettlement()` 호출 결과를 기존 텍스트 최종값과 동일하게 렌더링하되, `gross/spend/operating/net/cash` 주요 숫자를 0 또는 이전 표시값에서 최종값으로 짧게 카운트업한다.
17. `SettlementPanelController`는 정산 결과 렌더링 직후 `SettlementPresented` 이벤트를 발행한다. 이미 정산된 날 재호출이어도 cash를 바꾸지 않고 최종 표시값과 이벤트 payload는 저장된 결과를 따른다.
18. `ShopPresentationController`는 `SettlementPresented`를 받으면 정산 pulse text를 표시한다. 순손익이 양수면 긍정 색, 음수면 경고 색, 파산이면 별도 경고 색을 사용한다.
19. `GameEvents.DayPhaseChanged`를 통해 Night phase 진입 시 `NightOverlay` alpha를 어둡게 보간한다. Market/Service/Settlement phase 진입 시 overlay alpha를 0으로 되돌린다.
20. `SceneBuilder.BuildShop()`에 `BuildShopStage(...)`를 추가한다. Canvas 자식 순서는 stage가 먼저, phase 패널과 HUD가 뒤에 오도록 해 UI가 클릭 가능하게 유지한다.
21. `SceneBuilder`의 phase panel RectTransform을 재배치한다. 손님/카운터 무대가 보이는 공간을 확보하고, 기존 Market/Service/Settlement/Night 기능 버튼은 같은 이름으로 유지하거나 테스트를 함께 갱신한다.
22. `BuildShopStage`는 `ShopStage`, `Stage_Backdrop`, `Stage_CustomerArea`, `Stage_Counter`, `CustomerSprite`, `CustomerLabel`, `OrderLabel`, `FoodIcon`, `CashPopupText`, `SettlementPulseText`, `NightOverlay` 같은 안정적인 이름을 생성한다.
23. `SceneBuilder`가 customer/recipe sprite asset을 로드해 `ShopPresentationController.EditorInit` 또는 동등한 editor-only 주입 메서드로 전달한다. Resources 폴더 의존은 만들지 않는다.
24. `ShopPresentationSceneTests`를 추가해 stage 오브젝트, controller, 참조 주입, customer/recipe catalog 개수, NightOverlay 초기 alpha 0, Market 초기 활성 상태를 검증한다.
25. `PlaceholderArtTests`를 추가해 손님 4종, 음식 아이콘 6종 asset 존재와 importer 설정(PPU 32, Point, Sprite, compression none)을 검증한다.
26. `ServicePresentationEventTests` 또는 동등 테스트를 추가해 `ServicePanelController`의 서빙/포기 경로가 처리 전 주문 정보를 보존한 이벤트를 발행하고, 실패/주문 없음 경로에서 예외가 없음을 검증한다.
27. `SettlementPresentationTests` 또는 기존 settlement scene 테스트를 확장해 정산 패널이 최종 텍스트를 정확히 남기고 `SettlementPresented` payload가 `SettlementOps` 결과와 일치함을 검증한다.
28. 기존 `FirstPlayableLoopTests`, `ServiceOpsTests`, `SettlementOpsTests`, `MarketPanelSceneTests`, `ServicePanelSceneTests`, `SettlementPanelSceneTests`, `SceneBuilderTests`가 새 표현 레이어와 함께 계속 통과하도록 갱신한다.
29. Unity 배치 모드에서 `SceneBuilder.Apply`, 컴파일 게이트, EditMode 테스트를 실행한다.
30. 수동 Play smoke 기준을 구현 노트에 기록한다. MainMenu에서 시작해 구매, 영업, 손님 입장/서빙/이탈, 정산 카운트업, 밤 오버레이, 다음 날 복귀, 파산 진행 잠금을 확인한다.
31. 구현 기록 문서와 status board를 갱신한다.

## 실행 계획 (Execution Plan)

- implement_model: claude-fable-5
- implement_effort: xhigh
- routing_reason: M1.5 완료 게이트 작업으로 SceneBuilder 레이아웃, placeholder asset 생성/검증, UI event hook, coroutine 연출, 기존 루프 회귀 테스트를 함께 다루므로 `project-brief.md`의 task-108 라우팅대로 fable-5/xhigh가 적합하다.

| unit | 파일 범위 | depends_on | group |
|------|-----------|------------|-------|
| U1-placeholder-art | `game/Assets/Scripts/Editor/PlaceholderArtBuilder.cs` 또는 `SceneBuilder` helper, `game/Assets/Art/Placeholders/**`, 관련 `.meta`와 provenance 문서 | 없음 | G1 |
| U2-presentation-events | `game/Assets/Scripts/Runtime/DayCycle/GameEvents.cs`, `game/Assets/Scripts/Runtime/Presentation/*EventArgs.cs`, tween/format helper | 없음 | G1 |
| U3-shop-presentation-runtime | `game/Assets/Scripts/Runtime/Presentation/ShopPresentationController.cs`, sprite catalog entry 타입, 관련 `.meta` | U1-placeholder-art, U2-presentation-events | G2 |
| U4-ui-event-hooks | `game/Assets/Scripts/Runtime/UI/ServicePanelController.cs`, `SettlementPanelController.cs`, 필요 시 `NightPanelController.cs` | U2-presentation-events, U3-shop-presentation-runtime | G3 |
| U5-scene-builder-layout | `game/Assets/Scripts/Editor/SceneBuilder.cs`, `game/Assets/Scenes/Shop.unity`, 필요 시 `MainMenu.unity` 재저장 | U1-placeholder-art, U3-shop-presentation-runtime, U4-ui-event-hooks | G4 |
| U6-editmode-tests | `game/Assets/Tests/EditMode/{PlaceholderArt,ShopPresentationScene,ServicePresentationEvent,SettlementPresentation}Tests.cs`, 기존 scene/UI 테스트 갱신 | U1-placeholder-art, U4-ui-event-hooks, U5-scene-builder-layout | G5 |
| U7-validation-pass | Unity 배치 `SceneBuilder.Apply` 로그, 컴파일 로그, EditMode 결과 XML, design/done validator 결과 | U5-scene-builder-layout, U6-editmode-tests | G6 |
| U8-task-records | `kb/tasks/task-108/manifest.md`, `kb/tasks/task-108/implementation-notes.md`, `kb/artifacts/task-108-summary.md`, `kb/index/status.md` | U7-validation-pass | G7 |

## 파일/모듈 영향 (Affected Files/Modules)

| 파일/모듈 | 변경 유형 | 설명 |
|-----------|-----------|------|
| `game/Assets/Art/Placeholders/Customers/*.png` | create | 고객 archetype 4종에 대응하는 placeholder 손님 sprite |
| `game/Assets/Art/Placeholders/FoodIcons/*.png` | create | 레시피 6종 서빙 표시용 placeholder 음식 icon |
| `game/Assets/Art/Placeholders/PLACEHOLDER-PROVENANCE.md` | create | 프로젝트 생성형 또는 외부 CC0 placeholder asset의 출처/라이선스/생성 방식 기록 |
| `game/Assets/Scripts/Editor/PlaceholderArtBuilder.cs` | create | placeholder texture 생성 또는 import 설정 보정을 수행하는 editor utility |
| `game/Assets/Scripts/Editor/SceneBuilder.cs` | modify | Shop stage 생성, 패널 재배치, presentation controller 참조 주입, sprite asset 로드 |
| `game/Assets/Scenes/Shop.unity` | modify | SceneBuilder가 재생성하는 가게 무대, 손님/음식/팝업/밤 overlay 포함 씬 산출물 |
| `game/Assets/Scenes/MainMenu.unity` | modify | SceneBuilder 재실행으로 저장될 수 있으나 기능 변경은 없음 |
| `game/Assets/Scripts/Runtime/DayCycle/GameEvents.cs` | modify | 표현 전용 Service/Settlement event와 internal raise 메서드 추가 |
| `game/Assets/Scripts/Runtime/Presentation/ServicePresentationEventArgs.cs` | create | 서빙/포기 결과와 현재 주문 표시 정보를 전달하는 event payload |
| `game/Assets/Scripts/Runtime/Presentation/SettlementPresentationEventArgs.cs` | create | 정산 표시 연출에 필요한 settlement payload |
| `game/Assets/Scripts/Runtime/Presentation/PresentationTween.cs` | create | coroutine 연출에서 사용하는 보간/숫자 포맷 helper |
| `game/Assets/Scripts/Runtime/Presentation/ShopPresentationController.cs` | create | 손님 입장/퇴장, 음식 아이콘, cash popup, settlement pulse, night overlay를 제어 |
| `game/Assets/Scripts/Runtime/UI/ServicePanelController.cs` | modify | 현재 주문/서빙/포기 이벤트 발행, 처리 전 주문 정보 캡처, 기존 UI 갱신 유지 |
| `game/Assets/Scripts/Runtime/UI/SettlementPanelController.cs` | modify | 정산 숫자 카운트업, settlement 표현 이벤트 발행, 최종 텍스트 정확성 유지 |
| `game/Assets/Scripts/Runtime/UI/NightPanelController.cs` | modify | 필요 시 Night 표시와 overlay 이벤트 timing 보정; SNS/저장 기능은 추가하지 않음 |
| `game/Assets/Tests/EditMode/PlaceholderArtTests.cs` | create | placeholder asset 존재, importer 설정, provenance 문서 존재 검증 |
| `game/Assets/Tests/EditMode/ShopPresentationSceneTests.cs` | create | Shop stage/controller/catalog/overlay/초기 활성 상태 검증 |
| `game/Assets/Tests/EditMode/ServicePresentationEventTests.cs` | create | ServicePanelController의 주문 표시/서빙/포기 이벤트 payload 검증 |
| `game/Assets/Tests/EditMode/SettlementPresentationTests.cs` | create | SettlementPanelController 최종 표시값과 표현 이벤트 payload 검증 |
| `game/Assets/Tests/EditMode/SceneBuilderTests.cs` | modify | Shop stage 존재와 씬 2개 하드캡 지속 검증 추가 |
| `game/Assets/Tests/EditMode/ServicePanelSceneTests.cs` | modify | 재배치 후에도 Service panel 필수 컨트롤과 데이터 주입 검증 유지 |
| `game/Assets/Tests/EditMode/SettlementPanelSceneTests.cs` | modify | 재배치/카운트업 후에도 Settlement/Night 컨트롤러와 텍스트 검증 유지 |
| `game/**/*.meta` | create/modify | 새 asset, Runtime/Presentation 파일, 테스트 파일, 씬 변경에 대응 |
| `kb/tasks/task-108/manifest.md` | modify | done-gate 통과를 위해 inputs/concepts_needed/related_files placeholder를 실제 값으로 교체 |
| `kb/tasks/task-108/implementation-notes.md` | create | 구현 결정, asset provenance, 연출 범위, smoke 결과, Unity 검증 결과 기록 |
| `kb/artifacts/task-108-summary.md` | create | M1.5 표현 미니 패스 완료 요약과 사용자 플레이테스트 인계 사항 기록 |
| `kb/index/status.md` | modify | `runtime/generate-status.py`로 재생성되는 task 상태 보드 |

## 테스트 기준 (Test Criteria)

- [ ] `python runtime/validator/cli.py kb/tasks/task-108/design.md`가 0으로 통과한다.
- [ ] Unity 배치 모드에서 SceneBuilder가 성공한다: `Unity.exe -batchmode -quit -nographics -projectPath game -executeMethod ClientIsKing.EditorTools.SceneBuilder.Apply -logFile <temp-log>` 종료 코드가 0이다.
- [ ] 배치 컴파일 게이트가 통과한다: `Unity.exe -batchmode -quit -nographics -projectPath game -logFile <temp-log>` 종료 코드가 0이고 로그에 `error CS`가 없다.
- [ ] EditMode 테스트가 통과한다: `Unity.exe -batchmode -nographics -projectPath game -runTests -testPlatform EditMode -testResults <temp-results> -logFile <temp-log>` 종료 코드가 0이다.
- [ ] `PlaceholderArtTests`는 customer sprite 4종과 food icon 6종이 존재한다고 검증한다.
- [ ] `PlaceholderArtTests`는 모든 placeholder sprite의 import 설정이 Sprite, PPU 32, Point filter, no compression, mipmap off라고 검증한다.
- [ ] `PlaceholderArtTests`는 `Assets/Art/Placeholders/PLACEHOLDER-PROVENANCE.md`가 존재하고 각 sprite 파일의 출처 또는 생성 방식이 기록되어 있다고 검증한다.
- [ ] `ShopPresentationSceneTests`는 `Shop.unity`에 `ShopStage`, `Stage_Backdrop`, `Stage_CustomerArea`, `Stage_Counter`, `CustomerSprite`, `FoodIcon`, `CashPopupText`, `SettlementPulseText`, `NightOverlay`가 존재한다고 검증한다.
- [ ] `ShopPresentationSceneTests`는 `ShopPresentationController`가 customer catalog 4종과 recipe/food icon catalog 6종을 주입받았다고 검증한다.
- [ ] `ShopPresentationSceneTests`는 `NightOverlay` 초기 alpha가 0이고 raycast target이 꺼져 있어 UI 클릭을 막지 않는다고 검증한다.
- [ ] `SceneBuilderTests`는 Build Settings에 계속 `Assets/Scenes/MainMenu.unity`, `Assets/Scenes/Shop.unity` 2개만 등록되어 있다고 검증한다.
- [ ] `SceneBuilderTests` 또는 presentation scene 테스트는 640×360 기준에서 stage와 phase panel RectTransform이 의도한 영역에 있고, phase panel이 stage 전체를 가리지 않는다고 검증한다.
- [ ] `ServicePresentationEventTests`는 Service phase 진입 시 현재 주문의 `customerId`, `recipeId`, `partySize`, 주문 번호가 포함된 주문 표시 이벤트가 발행된다고 검증한다.
- [ ] `ServicePresentationEventTests`는 서빙 성공 시 처리 전 주문 정보와 `revenueGained > 0`이 포함된 outcome 이벤트가 발행된다고 검증한다.
- [ ] `ServicePresentationEventTests`는 포기/이탈 시 처리 전 주문 정보와 `missed = true`, `revenueGained = 0`이 포함된 outcome 이벤트가 발행된다고 검증한다.
- [ ] `SettlementPresentationTests`는 Settlement panel 활성화 시 `SettlementOps.ApplyDailySettlement` 결과와 동일한 payload의 `SettlementPresented` 이벤트가 발행된다고 검증한다.
- [ ] `SettlementPresentationTests`는 카운트업 연출 후 매출, 재료 지출, 운영비, 순손익, 잔액 텍스트의 최종값이 `SettlementResult`와 일치한다고 검증한다.
- [ ] `ShopPresentationController`는 event payload에 알려지지 않은 `customerId`나 `recipeId`가 들어와도 예외 없이 fallback sprite 또는 빈 표시로 복구한다고 검증한다.
- [ ] 기존 `FirstPlayableLoopTests`는 구매→서빙→정산→밤→다음 날 흐름과 파산 진행 차단이 표현 레이어 추가 후에도 그대로 통과한다.
- [ ] 기존 `ServiceOpsTests`, `SettlementOpsTests`, `EconomyManagerTests`, `InventoryManagerTests`, `MarketPanelSceneTests`, `ServicePanelSceneTests`, `SettlementPanelSceneTests`, `DataDefinitionTests`, `DayPhaseMachineTests`가 계속 통과한다.
- [ ] 수동 Play smoke 기준: MainMenu에서 시작해 Market 구매, Service 진입 시 손님 입장, 서빙 성공 시 음식 아이콘과 `+N원` 팝업, 포기 시 불만 퇴장, Settlement 카운트업, Night 어두운 overlay, 다음 날 Market 복귀가 확인된다.
- [ ] 수동 Play smoke 기준: 파산 시 Settlement/Night 표시와 진행 버튼 잠금이 task-107과 동일하게 유지되고, 연출 때문에 phase가 추가 진행되지 않는다.
- [ ] `git status --short game`에 Unity 캐시/빌드 산출물(`Library`, `Temp`, `Obj`, `Logs`, `UserSettings`, `Build*`)이 나타나지 않는다.
- [ ] 구현 완료 후 `python runtime/validator/cli.py --check-done task-108`와 `python runtime/generate-status.py --check`가 통과한다.

## 오픈 이슈 (Open Issues)

- 없음. placeholder art는 프로젝트 생성형 CC0 placeholder 또는 외부 CC0 asset provenance 중 하나로 구현 단계에서 확정하며, 둘 다 데모 스코프 가드의 CC0/OFL 하드캡 안에 있다.
