# 구현 노트 — task-110

> Status: done
> Inputs: kb/tasks/task-110/design.md
> Outputs: 장르 선택 도메인/경제/서비스 수학(U1-U3), UI/SceneBuilder(U4-U5), 밸런스·씬 회귀·PlayMode 테스트(U6-U8)
> Next step: 커밋 완료(`9265da5`)·Codex 코드 리뷰 완료(`reviews/001.md` request-changes, 기록 지적 반영). 남은 오너 게이트: 640×360 시각 승인 + 수동 Play smoke → 통과 시 task-111(SNS)

## 현재 상태: done (구현 완료, Unity green, 커밋 완료 `9265da5` — 3개 그룹 세션에 걸쳐 완료)

이 turn 은 세 그룹으로 나눠 순차 구현했다(같은 세션, SendMessage 재개).

- **그룹1 (U1 domain + U2 economy/service math + U3 manager wiring)**: 결정론 수학, GameManager/ServiceManager/EconomyManager 배선.
- **그룹2 (U4 UI + U5 SceneBuilder/InitialDataBuilder)**: 장르 선택 modal, HUD badge, Settlement 원인 문구, 시드 배수 확정, 씬 wiring 완료.
- **그룹3 (U6 밸런스/씬 테스트 + U7 검증 + U8 기록, 이 문서)**: `GenreBalanceTests` 재유도, scene/UI 커버리지 확장, PlayMode 신규, 최종 배치 검증.

팀리드가 독립 검증(EditMode 152/152 · PlayMode 2/2 재실행, done-gate rc=0) 후 **커밋 완료 — `9265da5`**. 기록 정합 후속 커밋 `b5e1c13`(상태보드 완료 반영·완료일).

## U1-U3 요약 (그룹1)

- `GameState.selectedGenreId`(문자열, 기본 빈) 추가. SO 직접참조·enum 직렬화 없음.
- 신규 `Runtime/Genre/{GenreSelectionResult,GenreDemandPlan,GenreSelectionOps}.cs` — Unity 타입 미참조 순수 C#.
  `GenreDefInput/RecipeDefInput/CustomerDefInput` 순수 DTO 로 SO 를 투영해 받는다. 투영 자체는
  `ServiceManager.ToGenreInput/ToRecipeInputs/ToCustomerInputs`(internal, IVT 로 테스트 노출)가 소유.
- **결정론 수학**: `RoundHalfUp(x)=floor(x+0.5)`. orderCount=`clamp(RoundHalfUp(5/cookTimeMultiplier),4,6)`.
  milli-weight=`RoundHalfUp(baseSpawnWeight×affinity×1000)`. 고객 seed=UTF-8 `genreId|day|orderIndex`
  32-bit FNV-1a(offset 2166136261/prime 16777619/unchecked) → `roll=seed%totalMilliWeight`, 누적합 첫 초과 고객.
  recipe=`(day-1+orderIndex)%recipeCount`(정렬 허용목록). partySize=`min+(day-1+orderIndex)%span`.
- `EconomyOps.CalculatePurchaseCost/TryPurchaseIngredient`, `ServiceOps.CalculateSalePrice/TryServeCurrentOrder/BuildOrders`
  에 genre 배수 overload 추가 — 기존 neutral(1.0) overload 는 하위호환으로 보존.
- `GameManager`: 정렬 `List<GenreDef>` catalog(EditorInit 주입), `TryGetGenre`, `CanAdvancePhase(out reason)`.
  `AdvancePhase()` 는 Market→Service 직전 `ServiceManager.TryStartServiceDay` 가 원자적으로 성공한 경우에만 진행.
  Service→Settlement 는 `serviceDay==day` + 생성 주문수 일치 + 열린 주문 없음 3조건.
- `ServiceManager`: 정렬 recipe/customer EditorInit 주입, `TryBuildDayPlan`/`TryStartServiceDay` 추가.
- `EconomyManager.TryPurchaseIngredient`: 미선택/미존재 genre 는 현금·재고·marketSpend 불변 실패.
- `GameEvents.GenreSelected(string genreId)` 추가(발행은 그룹2 U4 의 `MarketPanelController.OnConfirmGenre`).
- `PhaseHudController`: `!bankrupt && CanAdvancePhase` 단일식으로 interactable 계산(기존 버그성 재활성 로직 제거),
  genreBadge 필드 + 7-인자 EditorInit overload 추가(기존 6-인자 유지, 그룹2 가 채택).
- `ServicePanelController.OnEnable`: 주문 생성 책임 제거, 표시 refresh 만.

## U4-U5 요약 (그룹2, 관찰 기준)

- `InitialDataBuilder`: 최종 GenreDef 확정 — 국밥(1.15,1.20,**0.95**)·분식(0.85,0.80,**1.05**)·면류(0.95,1.00,0.95)·
  제네럴리스트(1,1,1). (그룹1 시점엔 시드가 구 버전 1.25/0.75 였고 이번 그룹2 에서 D5 확정값으로 교체됨.)
- `SceneBuilder`: `Panel_GenreSelection`(canvas 마지막 자식, raycast 차단) + 4버튼(Icon/Outline 포함) + Detail
  + ConfirmButton + HelperText, HUD `GenreBadge`, `Panel_Market/GenreDetailButton` 생성. `CreateGameManager()`
  가 정렬된 genre/recipe/customer catalog 를 MainMenu/Shop 양쪽에 동일하게 주입.
- `MarketPanelController`: 장르 4선택 UI, `GenreSelectionOps.TrySelect` 성공 시 `GameEvents.RaiseGenreSelected` 1회
  발행, 확정 전 구매 잠금, 확정 후 specialist 는 plan 이 요구하는 재료 행만 표시, B급 행/등급 토글 숨김.
  E3 문구 원천은 `GenreSelectionCopy`(internal, Settlement 와 공유).
- `ServicePanelController`/`SettlementPanelController`: 1인가+예상총액 표시(genre overload 경로), 전문 분야
  원인 한 줄(`GenreEffectText`). 등급 토글 숨김(C급 고정, B급 데이터·Ops 보존).

## U6 — GenreBalanceTests 재유도 (이 그룹)

`kb/tasks/task-110/design.md` D5 의 100-day 밸런스 guard 방법론을 그룹1 수학 그대로 C# 표준 라이브러리로
독립 재현(`BalanceCheck.cs`, 스크래치패드, PowerShell `Add-Type` 인라인 컴파일)해 **±1% guard 사전 통과를 확인한 뒤**
`GenreBalanceTests.cs` 를 작성했다. 재계산 없이 프로덕션 Ops(`GenreSelectionOps.TryBuildDemandPlan/PickRecipeId/
PickCustomerId/PickPartySize`, `EconomyOps.CalculatePurchaseCost`, `ServiceOps.CalculateSalePrice/
CalculateRequiredIngredients`) 를 그대로 호출해 100-day 표본을 만든다.

### 재유도 실측값 (100-day, C급, 실제 recipe 요구량, 전량 서빙 가정)

| 장르 | orderCount | 평균 기여이익(재유도) | 설계값(D5) | 편차 |
|------|-----------:|----------------------:|-----------:|-----:|
| 국밥 | 4 | 49,266.45 | ≈49,266 | +0.001% |
| 분식 | 6 | 48,532.20 | ≈48,532 | +0.0004% |
| 면류 | 5 | 49,973.66 | ≈49,949 | +0.05% |
| 제네럴리스트 | 5 | 51,580.50 | ≈51,581 | -0.001% |

- **max/min 비율 = 1.0628** (guard 1.10 이하 통과, 재유도 1회차에 즉시 통과 — 에스컬레이션 불필요).
- **Day1 이론 구매비**(C급, 실제 요구량): 국밥 23,920 / 분식 8,930 / 면류 12,238 / 제네럴리스트 10,000 — 전부 시작 자금
  30,000원 이하.
- **Day1~3 완전 서빙, 운영비 12,000원 차감 후 최소 순이익**: 국밥 25,850 / 분식 28,945 / 면류 33,612 / 제네럴리스트
  28,000 — 전부 매일 양수.
- **4축 동시 지배 없음**: 원가 최저=분식(0.85), 1인가 최고=분식(1.05×base) 이지만 주문수 최다=분식(6)·recipe
  다양성 최다=제네럴리스트(6) 이므로 어느 장르도 4축을 동시에 이기지 않음 — `No_Genre_Dominates_All_Four_Axes` 로 검증.

`GenreBalanceTests.cs`(신규, 5 tests): `Average_Contribution_Profit_Matches_Design_Within_1_Percent`,
`Max_Min_Contribution_Ratio_Is_At_Most_1_10`, `Day1_Theoretical_Purchase_Cost_Is_Within_Starting_Cash`,
`Day1_To_3_Full_Service_Net_Profit_After_Operating_Cost_Is_Positive_Every_Day`, `No_Genre_Dominates_All_Four_Axes`.

## U6 — scene/UI 회귀 확장 (신규/수정 테스트)

- **`MarketPanelSceneTests.cs`**: 기존 구조적 테스트(`[OneTimeSetUp]` 공유 씬) 유지 + `Panel_GenreSelection`
  전체 오브젝트 존재, `GenreDetailButton` 기본 숨김 신규 추가. genre 선택/확정 흐름은 **`MarketPanelGenreFlowTests`
  별도 fixture**로 분리(아래 "씬 상태 격리" 참조) — 구매 잠금→확정→해제, forecast 문구, 확정 후 modal 접힘,
  specialist 확정 시 비-매칭 재료(돼지고기/소고기/소면) 순환 배제.
- **`ServicePanelSceneTests.cs`** + 신규 `ServicePanelGenreFlowTests`: 1인가/예상총액이 `ServiceOps.CalculateSalePrice`
  경로와 일치, "객단가" 표현 금지, 등급 토글 숨김.
- **`SettlementPanelSceneTests.cs`** + 신규 `SettlementPanelGenreFlowTests`: `GenreEffectText` 원인 문구
  포함(장르 표시명+비교 문구), 정산 재표시 시 cash 불변(멱등성).
- **`SceneBuilderTests.cs`**: `Panel_GenreSelection` 이 canvas 마지막 자식+초기 활성, `GenreBadge` 존재,
  genre catalog 4종 ID ordinal 정렬 확인, `Repeated_Apply_Is_Idempotent_In_Object_Count_And_Persistent_Listeners`
  (연속 2회 `SceneBuilder.Apply()` 후 canvas 하위 오브젝트 총수 동일 + `AdvanceButton.onClick` persistent listener 0).
- **`EconomyManagerTests.cs`**: genre 배수 `CalculatePurchaseCost`/`TryPurchaseIngredient` overload(RoundHalfUp,
  neutral 일치, 잘못된 배수 실패) + `EconomyManager` 씬 레벨 genre 게이트(미선택 실패/선택 후 원가 일치).
- **`ServiceOpsTests.cs`**: genre 배수 `CalculateSalePrice` overload, `TryServeCurrentOrder(...,GenreDefInput)`
  실제 cash/revenue 반영·null genre neutral fallback, plan 기반 `BuildOrders` 의 recipe 필터·결정론.

### 씬 상태 격리 (재사용 가능한 교훈)

배치 EditMode 에서 `EditorSceneManager.OpenScene`/`GameObject.AddComponent` 는 `Awake()`/`Start()`/`OnEnable()`
동기 호출을 보장하지 않는다(그룹1 에서 발견한 `AddComponent` 함정과 동일 계열이 `OpenScene` 에도 존재함을
이번에 추가로 확인). 대응책:

1. **`TestSceneSupport.cs`(신규, internal 공용 헬퍼)**: `OpenShopSceneWithLiveSingletons()` 가 씬을 열고
   `GameManager/ServiceManager/EconomyManager/SettlementManager` 4종 singleton `Instance` 를 리플렉션으로
   씬의 실제 컴포넌트에 강제 동기화한다. `ForceStart`/`ForceOnEnable` 이 private lifecycle 메서드를
   리플렉션으로 직접 호출해 `BuildKindList`/`RefreshAll`/`onClick` 리스너 등록/`ApplyDailySettlement` 등을
   확정시킨다. **production 코드는 전혀 바꾸지 않는다** — 테스트 전용 우회.
2. **fixture 분리**: 상태를 공유하는 구조적 테스트(`[OneTimeSetUp]` 공유 씬)와, 씬을 재로드하며 GameState 를
   변경하는 stateful 테스트를 **같은 클래스에 두지 않는다** — 후자가 전자의 캐시된 Transform 참조를
   무효화(`MissingReferenceException`)한다. `XxxSceneTests`(구조) / `XxxGenreFlowTests`(상태) 로 분리.

## PlayMode (U6 신규 + U7 검증)

- `game/Assets/Tests/PlayMode/ClientIsKing.Tests.PlayMode.asmdef`(신규) — EditMode asmdef 와 별도, `UNITY_INCLUDE_TESTS`
  defineConstraint. `AssemblyInfo.cs` 에 `InternalsVisibleTo("ClientIsKing.Tests.PlayMode")` 추가.
- `GenrePersistencePlayModeTests.cs`(신규, 2 tests): `SceneManager.LoadSceneAsync`(진짜 Play 씬 전환)로
  MainMenu→Shop 이동 후 `GameManager`/`ServiceManager` **persistent instance 생존**(`Assert.AreSame`),
  genre 4/recipe 6/customer 4 보유, `TryBuildDayPlan` 성공. 두 번째 테스트는 **UI controller 를 전혀 활성화하지
  않고** `GameManager.AdvancePhase()` 만으로 주문이 원자적으로 초기화됨과, 열린 주문 상태에서 재호출 시
  Settlement 로 넘어가지 않음을 검증. **PlayMode 는 진짜 씬 로드라 Awake 가 정상적으로 동기 호출됨을 확인**
  (EditMode 배치의 OpenScene 함정과 다름 — TestSceneSupport 류 우회가 필요 없었음).

## Unity 검증 결과 (U7)

| 게이트 | 명령 | 결과 |
|--------|------|------|
| 컴파일 | `Unity.exe -batchmode -quit -nographics -projectPath game` | **exit 0**, `error CS` 0 (여러 차례 반복 검증) |
| EditMode | `-runTests -testPlatform EditMode` | **exit 0, 152/152 pass, 0 fail** (2회 연속 재실행으로 안정성 확인 — 기존 123 + 신규 29) |
| PlayMode | `-runTests -testPlatform PlayMode` | **exit 0, 2/2 pass** (2회 연속 재실행) — 인프라 문제 없이 그린 확보 |

- `InitialDataBuilder.Apply → SceneBuilder.Apply → compile → EditMode → PlayMode` 순서로 전체 수행.
- 씬 멱등성은 fileID multiset(오브젝트 총수) 동일 + persistent listener 0 기준으로 검증(Unity 가 매 실행
  fileID 를 재부여하므로 byte-hash 비교는 처음부터 계약 대상이 아님 — `Repeated_Apply_Is_Idempotent_...` 테스트).
- `git status --short game` 에 Library/Temp/Obj/Logs/UserSettings/Build 산출물 없음. 신규 파일 전부 `.meta` 존재.
- Scene(`MainMenu.unity`/`Shop.unity`)·폰트(`Galmuri11 SDF.asset`) 파일에 diff 가 남는 것은 테스트 실행 중
  `SceneBuilder.Apply()`/`OpenScene` 재실행으로 인한 fileID 재직렬화이며 실제 내용 변경이 아니다(그룹1부터
  일관되게 관찰됨, U5 SceneBuilder 소관, 커밋 대상 아님).

## 설계 대비 변경 사항

| 항목 | 설계 내용 | 실제 구현 | 사유 |
|------|-----------|-----------|------|
| U6 balance guard 재유도 방법 | "100개 결정론적 day" 방법론 서술만 | 프로덕션 Ops 를 그대로 호출하는 C# 테스트 + 별도 스크래치패드 독립 재현(PowerShell 인라인 컴파일)으로 사전 검산 후 작성 | 1회차 재유도에서 ±1%·1.10 guard 모두 통과 — design.md 의 재밸런싱/에스컬레이션 규칙이 발동할 필요가 없었음 |
| PlayMode 테스트 인프라 | "먼저 빈 테스트로 compile 확인" | 최소 asmdef 확인 없이 바로 완성된 2-test 파일로 작성(컴파일 1회 통과) | 시간 절약 목적으로 병합했으나 결과적으로 문제 없이 컴파일·통과함. 향후 유사 작업에서는 여전히 빈 asmdef 우선 확인 권장 |
| scene 테스트 fixture 구조 | 명시 없음 | 구조적 테스트와 stateful 테스트를 별도 클래스로 분리 | OpenScene 의 Awake 미보장 특성과 결합해 같은 클래스 내 테스트 간 상태 오염을 방지하기 위한 필수 조치로 판명 |

## Codex 대기 게이트 (self-approve 하지 않음)

design.md 테스트 기준 목록 중 다음 항목은 Claude 가 자체 승인할 수 없다 — Codex/오너 검토 필요:

- **640×360 원본 캡처 시각 승인**: "장르 modal, 상세 문구, forecast, HUD badge 가 겹치거나 canvas 밖으로 넘치지
  않고 지정 좌표·폰트·focus 순서와 일치" — 자동 테스트는 오브젝트 존재/좌표값만 확인했고, 실제 렌더 결과의
  시각적 검토는 하지 않았다. **Codex 승인 대기**.
- **수동 Play smoke**(60초 내 첫 선택, 화면/실제 transaction 일치): PlayMode 자동 테스트로 도메인 경로는
  검증했으나, 실제 화면 조작 기반 수동 smoke 는 이번 turn 에서 수행하지 않았다. **오너/Codex 수동 검증 대기**.
- **코드 리뷰**: U1~U8 전체 diff 에 대한 Codex 구현 리뷰가 아직 없음.

## 다음 단계

1. 오너/Codex 가 위 3개 게이트를 검토·승인.
2. 팀리드 독립 검증 후 **커밋 완료 — `9265da5`**(기록 정합 `b5e1c13`). Codex 리뷰 `reviews/001.md`(request-changes)의 기록 정합 지적은 반영 완료; 남은 두 Action(640×360 시각 승인·수동 Play smoke)은 오너 게이트.
3. `task-111`(SNS) 진행. 전체 3일 수직 슬라이스 게이트는 `task-112` 이후.
