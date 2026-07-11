# 구현 노트 — task-112

> Status: done
> Inputs: kb/tasks/task-112/design.md
> Outputs: 이벤트/장애물 도메인 수학(U1)·수요/서비스/경제/정산 배선(U2-U4)·UI/SceneBuilder(U5-U6)·테스트/밸런스 가드/PlayMode(U7)
> Next step: Codex 코드 리뷰(U1~U8 전체 diff) + 640×360 시각 승인 + 수동 Play smoke + 오너 시드 재확정 승인 → 통과 시 장르+SNS+이벤트 3일 수직 슬라이스 통합 게이트, 이후 task-113(저장/불러오기)

## 현재 상태: done (구현 완료, Unity green — 세 그룹 세션에 걸쳐 완료)

이 turn은 세 그룹으로 나눠 순차 구현했다(같은 세션, SendMessage 재개).

- **그룹1 (U1 events domain + U2 demand composition + U3 economy/settlement + U4 manager wiring)**:
  `EventOps`/`ActiveEventState`/`EventDayEffects` 순수 도메인, `DayModifier`/`GenreDemandPlan`/
  `GenreSelectionOps`/`ServiceOps` 이벤트 축 확장, `EconomyOps`/`SettlementOps` overload,
  `GameManager`/`ServiceManager`/`EconomyManager`/`SettlementManager` 배선.
- **그룹2 (U5 UI + U6 SceneBuilder/InitialDataBuilder/이벤트 catalog 주입/비용 재시드)**: Night 예고
  라인(F1 v2)·Market/HUD/Service/Settlement 표시, `InitialDataBuilder.BuildGameEvents` 재시드(위생
  8,000원/단체 4인), 이벤트 catalog 양씬 주입.
- **그룹3 (U7 테스트/밸런스 가드/PlayMode 확장 + U8 기록, 이 문서)**: 잔여 실패 1건 수정, 신규
  `EventOpsTests`/`EventBalanceTests`/`EventJsonRoundTripTests`/`EventManagerGateTests`/
  `NightPanelEventFlowTests`, 기존 테스트 확장(`GenreSelectionOpsTests`/`ServiceOpsTests`/
  `SettlementPanelSceneTests`/`MarketPanelSceneTests`/`ServicePanelSceneTests`/`SceneBuilderTests`),
  PlayMode 2건 추가, 밸런스 가드 재유도 중 발견한 float32 정밀도 이슈 규명·정정.

## U1-U4 요약 (그룹1)

- `GameState`: `activeEvents`(List\<ActiveEventState\>) + `marketEventSurchargeToday` +
  `serviceEvent{OrdersServedToday,OrdersMissedToday,RevenueToday}` 5필드 추가(JsonUtility 호환).
  `ServiceOrderState.eventInflow` bool 추가(기본 false).
- 신규 `Runtime/Events/{ActiveEventState(Serializable, id+remainingDays; 0=영구),
  EventDayEffects(축 분리 DTO+EventForecast), EventOps(순수 C#)}`.
- `EventOps`: 투영(`GameEventDefInput`)·공통 검증(모든 def 참조 경로 — task-111 리뷰001 교훈)·
  **FNV-1a 스케줄**(시드 47/문턱 450 strict `<`, 이중 roll 발생·가중선택, 활성 배제)·수명 전이(영구
  remainingDays=0)·효과 합성(`TryBuildDayEffects`)·예고(`TryBuildForecast`, 예고==적용 — 같은 순수
  함수 재사용)·`TryComposeDayModifier`(SNS+이벤트 수요축 합성)·`BuildSettlementCauseLine`(F5 단일원천,
  내부 stale-day 필터·≤2 전체/≥3 축약 포맷)·`BuildEffectSummary`(F2 카피). 결정론: `Fnv1a`/
  `RoundHalfUp`/`MulMilliHalfUp`는 `GenreSelectionOps` 재사용(제3 사본 금지), float은 경계 1회 밀리
  투영, 명시적 실패(neutral fallback 금지, GameState 불변).
- `DayModifier`: 이벤트 수요 축 3필드(EventSourceId/EventBonusOrderCount/EventPartySize) 추가(기존
  4-인자 생성자는 중립("",0,0) 위임). `GenreDemandPlan`: Event 3필드 + `OrderCount` 3항 합
  (base+SNS+event), 생성자 하위호환 체인. `GenreSelectionOps`: modifier 이벤트 축 검증·
  `PickCustomerId` **3분기**([base|SNS|단체] — 단체는 base 풀 재사용, 채널 타겟 무관).
  **base-prefix + SNS-prefix 둘 다 불변**(이벤트 유무가 base·SNS 구간 pick에 영향 없음).
- `ServiceOps`: 세그먼트 태그(범위 비교로 base/SNS/event 구분)·단체 파티 override(`plan.EventPartySize`)·
  단체 통계(`serviceEvent*Today`)·리셋. neutral/SNS 경로·기존 결과 완전 불변.
- `EconomyOps`: `eventCostMilli` overload(2단계 `MulMilliHalfUp(RoundHalfUp(unit×genre), eventCostMilli)`)·
  할증 추적(`marketEventSurchargeToday`)·`marketSpendDay!=day` 리셋 편입. 기존 API는 `eventCostMilli=1000`
  위임(비트 동일).
- `SettlementOps`: 운영비 `milli+flat` overload(위생 flat·임대료 milli). 기존 API는 (1000,0) 위임.
- `GameManager`: 이벤트 catalog(`[SerializeField] List<GameEventDef>` + `EventCatalog` 프로퍼티, ID
  ordinal 정렬)·`ToEventInput`/`ToEventInputs`(internal, IVT)·`TryBuildTodayEventEffects`/
  `TryBuildEventForecast`·**Night 경계 원자 교체**(`AdvancePhase`의 Night 분기에서 `machine.Advance()`
  직전 `activeEvents` 교체, DayPhaseChanged 발행 전 완료 — 별도 GameEvents 불필요)·`CanAdvancePhase`
  Settlement/Night 게이트(손상 이벤트 시 차단·상태 불변) 신설. `EditorInit(genreCatalog, eventCatalog)`
  2-인자 overload.
- `ServiceManager.TryBuildDayPlan`: SNS→이벤트 합성 5단계(history→snsModifier →
  `GameManager.TryBuildTodayEventEffects` → `EventOps.TryComposeDayModifier` → `TryBuildDemandPlan`).
  `EconomyManager`: fx 전달 구매(`TryPurchaseIngredient`)·`TryCalculatePurchaseCost` 신설(UI 예상가
  단일 경로). `SettlementManager.ApplyDailySettlement()`: fx 조회 후 overload 호출, 실패 시 명시적
  사유로 applied:false 반환.
- **neutral overload로 기존 public API 하위호환** 전량 유지.

## U5-U6 요약 (그룹2, 관찰 기준 — 이 세션에서 재검증만 수행)

- `InitialDataBuilder.BuildGameEvents`: 위생 80,000→**8,000원**, 단체 flatEffect 6→**4**(GUID 보존
  upsert, "불시"/"예고 없이" 문구를 예고 규약에 맞게 교체). 확인 결과 4개 asset 모두 design.md B1
  확정값과 정확히 일치(id/kind/baseWeight/durationDays/percentEffect/flatEffect 전부 재검증).
- `SceneBuilder`: Night 패널 F1 v2 좌표 재배치(SummaryText(0,86)·DaysText(0,71)·
  **EventNoticeText(0,57) 신설**·FollowerText(0,44)·SnsTitleText(0,31)), Settlement
  `EventEffectText(0,-76)` 신설 + `MessageText(0,-91)`로 이동. 이벤트 catalog 4종(ID ordinal:
  group_customers→hygiene_inspection→ingredient_price_surge→rent_increase) 양씬 동일 주입.
- `NightPanelController`/`SettlementPanelController`: `EditorInit` overload 체인 확장(끝에
  eventNoticeText/eventEffectText 추가, 기존 시그니처 보존). `RenderEventNotice`(F2 색+문구 분기)·
  `BuildEventEffectLine`(state 원시필드를 `EventOps.BuildSettlementCauseLine`에 가공 없이 그대로 전달).
  `PhaseHudController.BuildBadgeText`(internal, 기본/SNS만/단체만/동시 4분기). `MarketPanelController.
  BuildTodayEventSuffix`(폭등 fx 파생 %, 단체 plan 파생 파티/보너스). `ServicePanelController`: 단체
  주문만 "단체 손님" 태그(SNS 태그와 서로소). `ShopPresentationController`/
  `ServicePresentationEventArgs`: `EventInflow` 필드(기존 생성자 false 위임).

## U7 — 테스트/밸런스 가드/PlayMode (이 그룹)

### 잔여 1건 수정

`NightPanelSceneTests.Night_Panel_Sns_Objects_Match_F1_Coordinates`가 F1 v1 좌표(SummaryText 84 등)를
그대로 검증하고 있어 그룹2의 F1 v2 재배치와 불일치했다. 좌표를 v2 값(86/71/57/44/31)으로 갱신하고
`EventNoticeText` 존재 단언을 `Night_Panel_Has_All_Sns_Block_Objects`에 추가했다.

### 신규 테스트 파일

- **`EventOpsTests.cs`**(51 tests): 투영(ProjectMilli)·카탈로그/activeEvents 검증 매트릭스(null/빈
  목록/중복 Id·Kind/kind별 계약 위반 8종/불변식 5종)·**FNV known vector 9개**(시드47, event/event-pick
  day 2/3/5/8/11)·**C4 스케줄 표 전체 재현**(Day1 보호·Day2 문턱 경계·Day3 폭등 활성화·Day4 지속·Day5
  단체+폭등만료·Day8 위생·Day11 임대료 영구화·Day12+ 활성배제로 임대료 재발생 불가·Day13 폭등+임대료
  동시활성)·입력 불변성(`TryBuildNextDayActiveEvents`가 입력 리스트/항목을 절대 변경하지 않음)·효과
  합성(4종 개별+동시 조합, ID ordinal 정렬)·예고==적용(`activated` 값과 `forecast.UpcomingEventId`
  일치 직접 대조)·지속 라인(remaining 2→"n일 더", 1→"내일까지")·`TryComposeDayModifier`(null/day
  mismatch/손상된 fx 검증)·**`BuildSettlementCauseLine` 단일원천**(stale-day 강제 0원·전체 포맷 2종·
  축약 포맷 3종/4종 고정 순서·임대료 delta 1,800원)·`BuildEffectSummary` F2 카피 4종 정확 일치.
- **`EventBalanceTests.cs`**(9 tests): design.md G절 가드 1~7을 프로덕션 Ops(`EventOps`/
  `GenreSelectionOps`/`ServiceOps`/`EconomyOps`) 그대로 호출해 재유도(`GenreBalanceTests`/
  `SNSBalanceTests` 방식 계승, 재계산 금지). C4 스케줄 그대로 day 2~101 100개 결정론 day를 전량
  서빙·C급·실요구량 구매로 시뮬레이션(SNS 없음).
  - Guard6(스케줄 분포, 다른 가드의 전제라 먼저 검증): 100일 발생 횟수 폭등15/단체13/위생16/임대료1
    (영구 1회 자동보장), 총 45회, Day2 무이벤트 — **design.md C4 표와 정확히 일치**.
  - Guard1(매일 순이익 양수, 핵심): 4장르 100일 전량 매일 양수 확인 + 최저/평균 실측값을 design.md
    G절 표와 대조.
  - Guard2(3중첩 최악 가상 day13 주입), Guard3(Day1-3 생존+D6 정확 일치), Guard4(단체 순기여 항상
    양수, Day5), Guard5(주문 하드캡 9=base6+SNS2+단체1), Guard7(D4/D5 고정 벡터 무허용오차 재확인).
- **`EventJsonRoundTripTests.cs`**(3 tests, 리뷰001 Action2 전례 계승): `activeEvents`(영구+시한
  혼합) ToJson→FromJson field-by-field 보존(빈 목록도 null 아닌 빈 List로 왕복), 왕복 전후
  `TryBuildDayPlan` plan 동등성(단체 이벤트 활성 시나리오, `BuildOrders` 결과의 recipe/customer/
  partySize/eventInflow 태그까지 완전 일치).
- **`EventManagerGateTests.cs`**(5 tests): 미지 eventId가 activeEvents에 있을 때 Market→Service/
  Settlement→Night/Night→Market 전 구간 차단 + 상태(cash/day/phase) 완전 불변 확인,
  `TryBuildTodayEventEffects`/`TryBuildEventForecast` 명시적 실패 직접 확인.
- **`NightPanelEventFlowTests.cs`**(5 tests, 상태 fixture — 구조는 `NightPanelSceneTests`로 분리
  유지): Day1 Night "내일 예고된 이벤트 없음" → Day2 Night 폭등 예고(Warning Plum, "+35%") → Day3
  지속 라인("내일까지") → Day4 Night 단체 손님 예고(Brass Amber) → Day10 Night 임대료 인상 반영
  운영비 경고(13,800원, SNS 집행 후 결과 문구에만 표시되는 계약 확인).

### 기존 테스트 확장

- **`GenreSelectionOpsTests.cs`**(+8 tests): modifier 이벤트축 검증 4종(범위밖/소스누락/파티과소/
  잔존값), 이벤트 보너스 적용 시 `EventBonusOrderCount`/`EventPartySize`/`EventSourceId`/`OrderCount`
  확인, `PickCustomerId` 3분기 중 단체 인덱스가 base 풀 사용, **base-prefix+SNS-prefix 불변**을
  단체 손님 유무로 직접 대조(SNS만 vs SNS+단체 plan의 인덱스 0..Base+Sns-1 완전 동일).
- **`ServiceOpsTests.cs`**(+5 tests): `BuildOrders` 세그먼트 범위비교 태깅(base/SNS/event 3분기, 두
  태그 서로소 확인), 단체 파티 override(4인 고정), `StartServiceDay` 이벤트 통계 리셋, `TryServe`/
  `Skip` 이벤트 태그 주문만 통계 갱신(SNS 통계 영향 없음 교차 확인).
- **`SettlementPanelSceneTests.cs`**(+4 tests): `EventEffectText`(0,-76)/`MessageText`(0,-91) 좌표,
  Day3 실제 `GameManager.AdvancePhase()` 진행으로 원인 라인 표시 확인, 활성 이벤트 없으면 빈 문자열,
  **worst-case 폭 자동검증**(Codex 리뷰001 Action 반영 — `TMP_Text.GetPreferredValues`로 전체 포맷
  최악 2종(단체+41,800원/폭등-99,999원)과 축약 포맷 최악 4종 동시 활성 문자열이 460px 이내임을 자동
  확인, 수동 시각 승인 의존도를 축소).
- **`MarketPanelSceneTests.cs`**(+2 tests): 활성 폭등 시 확정 상세에 "오늘: 재료값 폭등 +35%" +
  `EconomyManager.TryCalculatePurchaseCost` 단일 경로로 인상가 반영 확인, 활성 단체 손님 시 "단체
  +1팀(4인) 예정" 표시 확인.
- **`ServicePanelSceneTests.cs`**(+1 test): 단체 보너스 인덱스만 "단체 손님" 태그 + 파티 4인 표시
  (base 인덱스는 태그 없음).
- **`SceneBuilderTests.cs`**(+3 tests): 이벤트 catalog 4종 ID ordinal 양씬 동일 주입, Night/Settlement
  이벤트 UI 오브젝트 존재, 재실행 멱등성(이벤트 catalog 수·Night 오브젝트 수 불변).

### PlayMode 확장

`GenrePersistencePlayModeTests.cs`에 2개 테스트 추가(기존 4개는 무변경):

- `GameManager_Survives_Scene_Load_With_Event_Catalog_Of_Four`: MainMenu→Shop 실제 씬 전환 후
  persistent `GameManager`가 이벤트 4종(ID ordinal)을 보유.
- `Day1_To_3_No_Ui_Advances_Through_EventFree_Day2_Into_Surge_Active_Day3`: UI를 전혀 활성화하지
  않고 Day1→2(무이벤트, Night 예고==Day2 활성 일치)→3(폭등 활성, Night 예고==Day3 활성 일치) 진행,
  Day3 실제 구매로 할증(`marketEventSurchargeToday`) 발생을 도메인 경로로 검증.

### day≥3 기존 테스트 기대값 갱신 — 해당 없음

design.md 2단계는 "GameManager 경로로 day 3 이상을 진행하는 기존 테스트"의 기대값 갱신을 요구했다.
조사 결과 **해당하는 기존 테스트가 없었다**: `FirstPlayableLoopTests`/`GenrePersistencePlayModeTests`
(그룹1 이전)는 day 1→2까지만 `GameManager.AdvancePhase()`로 진행하고, `ServiceOpsTests`/
`GenreBalanceTests`의 `day: 3` 호출은 `ServiceOps.BuildOrders`/`GenreSelectionOps.TryBuildDemandPlan`을
직접 호출하는 순수 도메인 시뮬레이션이라 `GameManager`/`EventOps` 스케줄과 무관하다. 따라서 조용한
우회 없이 "갱신 대상 없음"으로 확정하고 기록한다.

### 밸런스 가드 재유도 중 발견 — design.md G절 표의 float32 정밀도 이슈 (1회차 재검산으로 원인 확정, 에스컬레이션 불필요)

1회차 재유도에서 `EventBalanceTests`의 노들(면류) 관련 값 3건이 design.md G절 표와 어긋났다
(Guard1 최저 15,991 vs 표 15,954, Guard2 3중첩 20,538 vs 표 20,497, Guard3 Day3 폭등 순이익 30,621 vs
표 30,579 — 전부 면류만, 다른 3장르는 표와 정확히 일치). `EventBalanceTests`에 임시 진단 테스트를
추가해 Day3 면류 plan의 주문별 원가·판매가를 전부 로그로 뽑은 뒤(이후 제거), Python으로 두 가지
계산을 대조했다: (a) `0.95`를 이상화 십진수로 취급한 계산, (b) C#의 `float 0.95f`가 실제로
`0.949999988...`로 표현되는 정밀도를 반영한 계산. (b)가 실제 로그값(재료 원가·판매가 전부)과 소수점
단위까지 정확히 일치했다 — 즉 **프로덕션 코드(`EconomyOps`/`ServiceOps`/`EventOps`/
`GenreSelectionOps`)는 design.md의 float32 계약(C1 — "SO float은 투영 경계에서 한 번만 밀리
정수화")대로 완벽히 정확하게 동작하고 있으며, design.md 자체의 G절 표 면류 worked value가 0.95를
이상화 십진수로 손계산해 float32 반올림 경계(예: 350×0.95f=332.4999...→332, 이상화면 332.5→333)를
놓친 것**이다. 근거: 재검산한 8개 원가/가격 계산 전부(Noodle qty2/4/8, Gochujang qty1/4, Vegetable
qty1/2/4, bibim/janchi_guksu 판매가 4건)가 (b) 계산과 소수점까지 정확 일치했고, gukbap/bunsik/
generalist 3장르는 대조 대상 상수 없이 이미 design.md 표와 정확 일치했으므로 이 오차가 면류에만
국한된 design.md 표 자체의 계산 오류임이 명확하다. **재계산·재검산 1회차에 원인이 완전히 규명됐으므로
(design.md "가드 2회 불일치 시 재검산 요청" 기준에 미달 — 2회 불일치가 아니라 1회 재검산으로 즉시
해명됨) 에스컬레이션(opus 재검산 요청/Codex 재설계 보고)이 불필요하다**. 프로덕션 코드는 변경하지
않았고, `EventBalanceTests.cs`의 해당 3개 assertion만 재검산된 정확값(15,991/20,538/30,621)으로
갱신하고 주석에 근거를 남겼다. design.md 자체 수정은 이번 turn 범위 밖(문서 정정은 별도 확인 필요) —
Codex 리뷰 시 참고하도록 이 노트에 근거를 남긴다.

## Unity 검증 결과 (U7)

| 게이트 | 명령 | 결과 |
|--------|------|------|
| 컴파일 | `Unity.exe -batchmode -quit -nographics -projectPath game` | **exit 0**, `error CS` 0 (4회 반복 검증 — 잔여 실패 수정 후·픽스처 버그 2건 수정 후·float32 상수 정정 후·worst-case 폭 테스트 추가 후) |
| EditMode | `-runTests -testPlatform EditMode`(`-quit` 없이) | **exit 0, 330/330 pass, 0 fail**(그룹1 종료 시점 236 + U7 신규 92 + 잔여수정 2 = 330, 무회귀) |
| PlayMode | `-runTests -testPlatform PlayMode`(`-quit` 없이) | **exit 0, 6/6 pass**(기존 4 + 신규 2, 무회귀) |

- **중요 발견(향후 세션 참고)**: `-runTests`는 `-quit`과 함께 쓰면 안 된다 — Unity가 quit과
  테스트러너 종료 타이밍이 충돌해 결과 파일(xml/log)이 아예 생성되지 않는 무증상 실패가 발생한다
  (exit code도 감지 불가). `-quit` 없이 실행하고 `Unity.exe` 프로세스 종료를 폴링해야 정상 동작한다.
- `git status --short game`에 Library/Temp/Obj/Logs/UserSettings/Build 산출물 없음. 신규 파일
  (`Runtime/Events/*.cs`, `Tests/EditMode/{EventOps,EventBalance,EventJsonRoundTrip,
  EventManagerGate,NightPanelEventFlow}Tests.cs`) 전부 `.meta` 존재.
- Build Settings 씬은 정확히 2개(`MainMenu.unity`, `Shop.unity`) — 씬 하드캡 불변.
- 이벤트 4종(재료값 폭등·위생 점검·임대료 인상·단체 손님) — 이벤트 하드캡 불변(demo-scope.md).

## Codex 대기 게이트 (self-approve 하지 않음)

- **Codex 코드 리뷰**: `design-review-codex.md`(request-changes) 2건 Action(stale-day 필터·worst-case
  fallback)은 그룹1(`EventOps.BuildSettlementCauseLine` 내부 구현)과 이 그룹(worst-case 폭 자동
  테스트 2건)에서 이미 반영됐다. 반영에 대한 Codex 재검토·승인은 **대기**.
- **640×360 원본 캡처 시각 승인**: Night v2 레이아웃·EventEffectText·축약 카피 톤이 겹침·canvas
  이탈 없이 F1/F5 좌표와 일치하는지 — 자동 테스트는 오브젝트 존재/좌표값/worst-case 폭까지 확인했지만
  실제 렌더 결과의 시각적 검토는 하지 않았다. **대기**.
- **수동 Play smoke**(Day1 Night "이벤트 없음" → Day2 Night 폭등 예고 → Day3 Market 인상 구매가·정산
  원인 라인의 인과 사슬, Day5 단체(예고→HUD+1건→태그→정산+금액), Night 판단 60초 이내): PlayMode
  자동 테스트로 도메인 경로는 검증했으나 실제 화면 조작 기반 수동 smoke는 수행하지 않았다. **대기**.
- **오너 승인**: 이벤트 시드 재확정(위생 8,000원/단체 4인)은 design.md 오픈 이슈에 따라 오너 최종
  승인이 필요하다. **대기**.
- **design.md G절 표 면류 worked value 정정**: 위 "밸런스 가드 재유도" 절 참조 — design.md 자체의
  3개 숫자(15,954/20,497/30,579 → 정확값 15,991/20,538/30,621)를 문서에서도 정정할지는 Codex/오너
  판단 대상으로 남긴다(이번 turn은 테스트 코드만 정정, 설계 문서는 미변경).

## 다음 단계

1. Codex가 U1~U8 전체 diff 리뷰, 오너가 나머지 3개 게이트(640×360 시각·수동 smoke·시드 재확정
   승인)를 검토·승인.
2. 통과 시 장르+SNS+이벤트 **3일 수직 슬라이스 통합 플레이테스트**(task-110 로드맵 I의 M2 통합
   게이트) 수행 후 task-113(저장/불러오기)으로 진행.
