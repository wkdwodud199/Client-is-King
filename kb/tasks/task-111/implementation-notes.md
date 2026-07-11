# 구현 노트 — task-111

> Status: done
> Inputs: kb/tasks/task-111/design.md
> Outputs: SNS 마케팅 도메인 수학(U1-U3)·UI/SceneBuilder(U4-U5)·밸런스/씬 회귀·PlayMode 테스트(U6-U8)·Codex 리뷰 001 반영
> Next step: Codex 리뷰 001 반영분 재검토 + 640×360 시각 승인 + 수동 Play smoke + 오너 비용 재시드 승인 → 통과 시 task-112(이벤트/장애물)

## 현재 상태: done (구현 완료, Unity green — 3개 그룹 세션에 걸쳐 완료)

이 turn 은 세 그룹으로 나눠 순차 구현했다(같은 세션, SendMessage 재개).

- **그룹1 (U1 social domain + U2 plan composition + U3 manager wiring)**: 결정론 수학(SNSCampaignOps),
  DayModifier 합성(GenreSelectionOps modifier overload), ServiceManager SNS catalog·집행/미리보기 API.
- **그룹2 (U4 UI + U5 SceneBuilder/InitialDataBuilder)**: Night SNS 블록, Service/Settlement/Market/HUD 표시,
  SNS 비용 재시드, 씬 wiring 완료.
- **그룹3 (U6 밸런스/씬 테스트 + U7 검증 + U8 기록, 이 문서)**: SNSBalanceTests 재유도, scene/UI 테스트 확장,
  PlayMode 신규, 최종 배치 검증.

## U1-U3 요약 (그룹1)

- `GameState`: `snsCampaignHistory` List + `serviceSns{OrdersServedToday,OrdersMissedToday,RevenueToday}`
  3필드 추가(JsonUtility 호환, 기본값 하위호환). `ServiceOrderState.snsInflow` bool 추가(기본 false).
- 신규 `Runtime/Social/{SNSCampaignRecord(직렬화 상태),SNSCampaignResult+SNSCampaignPreview(순수 DTO),
  SNSCampaignOps(순수 C#)}` + `Runtime/Genre/DayModifier.cs`(익일 수요 modifier 순수 DTO, task-112 합성 공용 훅).
- **결정론 수학**: `MulMilliHalfUp(a,b)=(a×b+500)/1000`(SNSCampaignOps·GenreSelectionOps 양쪽에 독립 보유 —
  Genre 가 Social 을 참조하지 않도록 동일 공식을 의도적으로 중복시킴). SO float 은 `ProjectMilli`로 경계에서
  한 번만 밀리 정수화. 반복 감쇠는 정수 fold 체인(`Math.Pow` 미사용). 고객 roll 은 task-110 FNV-1a
  (`genreId|day|orderIndex`, offset 2166136261/prime 16777619/unchecked) 그대로 재사용.
- `SNSCampaignOps.TryExecute`: 검사 순서 def무결성→파산→Night phase→오늘밤 이미 집행→자금부족(design E1
  그대로). 성공 시 원자적 cash 차감 + 레코드 append.
- `SNSCampaignOps.TryBuildPreview(state, def, customers, ...)`: top-target 계산(`BuildTopTargetCustomerIds`)의
  유일한 non-UI 경로 — Codex 설계 리뷰(`design-review-codex.md`)의 blocking 지적을 그대로 반영해 customer
  투영 입력을 받는 순수 helper 로 구현했다. 게이트 3~6 실패는 `CanExecute=false+BlockReason`으로 구분(실패
  아님), def/customers 무결성 위반만 `failReason` 명시적 실패.
- `SNSCampaignOps.TryBuildDayModifier`: history 에서 `executedOnDay==day-1` 레코드 탐색 — 0건 Neutral 성공,
  1건 저장값 그대로 사용(재계산 아님, B3 약속 고정), 2건 이상/미지 campaignId/bonus 범위밖 명시적 실패.
- `GenreDemandPlan`: `BaseOrderCount`/`BonusOrderCount`/`BonusCustomerWeights`/`SourceCampaignId` 추가,
  기존 생성자는 neutral 값 위임으로 보존. `GenreSelectionOps.TryBuildDemandPlan` modifier overload 추가
  (boost 커버리지 검증 — 빈 목록=전 고객 중립, 비어있지 않으면 정확히 1회씩 커버). 보너스 풀
  `bonusMilli(c)=MulMilliHalfUp(genreMilli(c), boostMilli(c))`. `PickCustomerId`는
  `orderIndex < BaseOrderCount`면 CustomerWeights, 아니면 BonusCustomerWeights(base-prefix 불변).
- `ServiceOps.BuildOrders`: `i >= plan.BaseOrderCount` 인덱스에 `snsInflow=true` 태그. `StartServiceDay`가
  SNS 3필드 리셋, `TryServeCurrentOrder`/`SkipCurrentOrder`가 태그 주문에만 SNS 통계 갱신.
- `ServiceManager`: SNS catalog(`[SerializeField] List<SNSCampaignDef>`)·`ToSnsCampaignInput`/
  `ToSnsCampaignInputs`·`TryExecuteSnsCampaign`·`TryGetSnsPreview`(def/CustomerDefs 투영만, top-target
  재계산 없음)·`TryBuildDayPlan`(history→modifier 합성, 실패 시 명시적 사유로 Market→Service 차단).
  `EditorInit` 2-arg(기존)+3-arg(SNS catalog 포함) 양쪽 보존.
- `GameEvents.SNSCampaignExecuted` 추가(발행은 `NightPanelController.OnExecute` 성공 경로 1회).

## U4-U5 요약 (그룹2, 관찰 기준)

- `InitialDataBuilder.BuildSNSCampaigns`: 비용만 재시드 — photo_feed 50,000→**15,000**, short_form
  40,000→**12,000**, local_board 25,000→**7,000**(GUID 보존 upsert, 도달·감쇠·친화·문구 불변).
- `SceneBuilder`: `BuildNightPanel`에 F1 좌표 그대로 SNS 블록 추가(FollowerText(0,50)·SnsTitleText(0,32)·
  Button_Sns_{PhotoFeed(-150,0),ShortForm(0,0),LocalBoard(150,0)}·SnsInfoText(0,-40)), Settlement 에
  `SnsEffectText`(0,-62) 추가. `LoadSnsCampaignDefs()`가 ID ordinal 정렬(local_board, photo_feed,
  short_form) 3종을 MainMenu/Shop 양쪽 `ServiceManager.EditorInit` 3-arg overload 로 동일 주입.
- `NightPanelController`: 팔로워 표시(`CalculateFollowerDisplay`)·버튼 3종(`TryGetSnsPreview` 결과로
  라벨 2행·interactable=`preview.CanExecute` 단일 게이트)·집행 완료 outline·결과/경고 문구·focus 체인
  (픽쳐그램→숏핑→동네게시판→다음 날 ▶). 집행 성공 시에만 `GameEvents.RaiseSNSCampaignExecuted` 1회.
- `ServicePanelController`: 태그 주문만 `SNS 유입` 문구. `ServicePresentationEventArgs.SnsInflow`(기존
  생성자는 false 위임). `ShopPresentationController`: 태그 주문만 Jade Green `SNS` rich text.
- `SettlementPanelController.BuildSnsEffectLine`: 어제 레코드 있으면
  `SNS({표시명}): 어제 {cost}원 → 유입 {served}/{bonus}팀 · 매출 +{revenue}원`, 없으면 빈 문자열.
- `PhaseHudController`: 보너스 있는 날 `{장르}·주문 {base}+{bonus}건(SNS)`, `OnDayPhaseChanged`에서도
  badge 재계산(day 전환 후 낡은 값 잔존 결함 보정). `MarketPanelController`: 확정 상세에
  `SNS 유입 +{bonus}팀 예정`(plan 값 사용, UI 직접 계산 없음).

## U6 — SNSBalanceTests 재유도 (이 그룹)

`kb/tasks/task-111/design.md` G절의 100-day 밸런스 guard 방법론을 그룹1 수학 그대로 프로덕션 Ops
(`SNSCampaignOps`/`GenreSelectionOps`/`ServiceOps`/`EconomyOps`)를 호출해 독립 재현했다(`GenreBalanceTests.cs`
방식 계승 — 재계산 없이 실제 코드 경로 그대로 100-day 표본을 만든다). "전날 밤 채널을 집행했다"고 가정한
`DayModifier`를 `SNSCampaignOps.TryBuildDayModifier`(D4)로 실제 재구성해 `TryBuildDemandPlan` modifier
overload 에 그대로 먹인 뒤, 보너스 인덱스(`BaseOrderCount..OrderCount-1`)만의 기여이익에서 채널 비용을 뺀
값을 day 2~101 100개 표본 평균한다.

### 재유도 실측값 (100-day, C급, 실제 recipe 요구량, 전량 서빙 가정)

| 장르 | 픽쳐그램 1회/2회 | 숏핑 1회/2회 | 동네게시판 1회/2회 |
|------|------------------:|------------------:|------------------:|
| 국밥 | +9,552.15 / −2,895.75 | +12,658.15 / +850.60 | +5,529.70 / +5,529.70 |
| 분식 | +1,340.73 / −6,686.44 | +3,644.44 / −4,054.87 | +1,067.89 / +1,067.89 |
| 면류 | +5,264.17 / −4,973.16 | +7,296.81 / −2,688.23 | +3,589.87 / +3,589.87 |
| 제네럴리스트 | +5,178.50 / −5,041.00 | +8,315.50 / −1,879.00 | +3,684.00 / +3,684.00 |

- **1회차 재유도 즉시 통과** — 설계값(design.md G절 표) 대비 최대 편차 분식×숏핑 0.006%, 전 조합 0.3원
  이내로 사실상 일치. **에스컬레이션 불필요**(1회차에 통과, opus-4-8 재검산·Codex 재설계 대상 아님).
- **가드1 통과**: 12조합 1회차 평균이 전부 +500~+14,000원 범위(min 분식×동네게시판 +1,067.89, max
  국밥×숏핑 +12,658.15).
- **가드2 통과**: 픽쳐그램·숏핑 2회차가 전 장르에서 1회차보다 낮고, 픽쳐그램 2회차는 전 장르 손실 전환
  (−2,895.75~−6,686.44).
- **가드3 통과**: 동네게시판 2회차 == 1회차(완만 감쇠 정체성, 4장르 전부 소수점 이하까지 동일). 감쇠는
  팔로워 획득(reachMilli 150→135, followerGain 15→14)에서만 드러남(보너스 주문 수는 1회차·2회차 모두 1팀
  유지) — `LocalBoard_Decay_Shows_Only_In_Follower_Gain_Not_Bonus_Order_Count` 로 별도 검증.
- **가드5 통과**: 12조합 전부 하루 총 주문 ≤ 8건(분식 6+보너스 2가 최댓값).
- **가드6 통과**: 집행 게이트는 `cash==cost` 정확히 일치해도 성공(하한 미강제) — `Execution_Gate_Requires_
  Only_Cash_Sufficiency_No_Minimum_Balance_Floor` 로 검증.

`SNSBalanceTests.cs`(신규, 9 tests): `Average_Bonus_Net_Matches_Design_Table_Within_1_Percent_{First,Second}_Use`,
`First_Use_All_Combos_Are_Positive_And_Within_Approved_Band`,
`PhotoFeed_And_ShortForm_Second_Use_Is_Lower_Than_First_Use_For_Every_Genre`,
`PhotoFeed_Second_Use_Turns_Negative_For_Every_Genre`,
`LocalBoard_Second_Use_Equals_First_Use_Flat_Decay_Identity`,
`LocalBoard_Decay_Shows_Only_In_Follower_Gain_Not_Bonus_Order_Count`,
`Daily_Order_Count_Never_Exceeds_Hard_Cap_Of_8`,
`Execution_Gate_Requires_Only_Cash_Sufficiency_No_Minimum_Balance_Floor`.

## U6 — SNS 도메인 테스트 (신규)

- **`SNSCampaignOpsTests.cs`**(37 tests, 그룹1 turn 작성): 밀리 투영·감쇠 체인(C2 표 전체)·보너스/팔로워
  공식·C4 매칭(연령+성별 호환·무매칭 중립·다중매칭 최댓값)·집행 게이트 전량(무결성/파산/phase/1밤1회/
  자금부족)·미리보기(top-target·게이트 구분·customers 무결성)·`TryBuildDayModifier`(0/1/2건·미지 id·
  범위밖) 전부 커버.
- **`GenreSelectionOpsTests.cs` 확장**(그룹1): neutral overload와 4-입력 overload field-by-field 동등성,
  modifier 검증 4종 실패(null/day mismatch/bonus 범위/coverage violation), **base-prefix 불변**(같은
  genre/day 에서 modifier 유무와 무관하게 인덱스 0..BaseOrderCount-1 완전 동일), **C5 고정벡터**(분식
  Day2×숏핑 보너스풀 family900/office1716/senior420/student2400=5436, FNV `bunsik|2|6`=1202351915→
  roll1127→office_worker, `bunsik|2|7`=1185574296→roll4440→student — 설계 문서 값과 정확히 일치).
- **`ServiceOpsTests.cs` 확장**(그룹1): `BuildOrders`태그 정확성(`i>=BaseOrderCount`만), `StartServiceDay`
  SNS 3필드 리셋, `TryServeCurrentOrder`/`SkipCurrentOrder` SNS 통계 갱신(태그 주문에만), neutral 주문
  기존 수치·메시지 불변 회귀.

## U6 — scene/UI 회귀 확장 (이 그룹, 신규/수정)

- **`NightPanelSceneTests.cs`**(신규, 구조 전용): F1 오브젝트 9종 존재 + 정확한 anchoredPosition 좌표
  일치, SNS 버튼 3종 Outline 컴포넌트 존재·기본 비활성, focus 체인 explicit navigation(픽쳐그램→숏핑→
  동네게시판).
- **`NightPanelSnsFlowTests.cs`**(신규, 상태 — `TestSceneSupport` 재사용, task-110 씬 상태 격리 교훈에
  따라 구조 테스트와 분리): 집행 시 cash 차감+레코드 append, 1밤1회로 3버튼 동시 잠금, outline+결과
  문구("집행 완료"), 팔로워 표시 갱신(120→145), 자금부족/파산 시 버튼 비활성, `SNSCampaignExecuted`
  정확히 1회 발행.
- **`SceneBuilderTests.cs` 확장**: SNS catalog 3종 ID ordinal 정렬 MainMenu/Shop 양쪽 동일 주입, Night
  패널 SNS UI 오브젝트 존재, Settlement `SnsEffectText` 존재, SNS catalog·Night 오브젝트 수·persistent
  listener 0 기준 재실행 멱등성(2회 연속 `SceneBuilder.Apply()`).
- **`MarketPanelSceneTests.cs`**(`MarketPanelGenreFlowTests` 확장): 보너스 있는 날 상세에
  `SNS 유입 +{bonus}팀 예정` 문구 표시(plan 값 그대로, UI 재계산 없음).
- **`ServicePanelSceneTests.cs`**(`ServicePanelGenreFlowTests` 확장): base 인덱스 주문은 태그 없음, 보너스
  인덱스 주문만 `SNS 유입` 태그 표시(분식 base 6건 스킵 후 확인).
- **`SettlementPanelSceneTests.cs`**(`SettlementPanelGenreFlowTests` 확장): 어제 집행 레코드 있으면
  `SNS(동네게시판): 어제 {cost}원 → 유입 {served}/{bonus}팀 · 매출 +{revenue}원` 표시(전량 서빙 시
  served==bonus 확인), 레코드 없으면 빈 문자열.

### 트러블슈팅 — 이번 그룹에서 발견/수정한 3건 (교훈 기록)

1. **`SceneBuilderTests.Repeated_Apply_Is_Idempotent_For_Sns_Catalog_And_Night_Object_Count`**: 두 번째
   `OpenSingle(ShopPath)` 이후 첫 번째 scene 에서 캐시해둔 `firstNightPanel`(RectTransform) 참조가
   `MissingReferenceException`을 던졌다 — task-110 이 이미 문서화한 "OpenScene 재로드가 이전 Transform
   참조를 무효화한다" 함정과 동일 계열. **수정**: persistent listener count 를 각 scene 오픈 직후
   즉시 읽어두고 참조를 넘겨 들지 않도록 재구성(기존 `Repeated_Apply_Is_Idempotent_In_Object_Count_And_
   Persistent_Listeners` 와 동일 패턴).
2. **`ServicePanelGenreFlowTests.CustomerText_Shows_Sns_Inflow_Tag_Only_For_Tagged_Orders`**: `gm.State.day
   = 2`를 먼저 설정한 뒤 `GenreSelectionOps.TrySelect`를 호출해 "전문 분야는 Day 1 에만 선택할 수
   있습니다" 로 실패했다. **수정**: 장르 확정(Day 1 게이트)을 day 변경보다 먼저 수행하도록 순서 교정.
3. **`SettlementPanelGenreFlowTests.SnsEffectText_Shows_Cause_Line_When_Yesterday_Campaign_Executed`**:
   같은 원인(day 변경 후 TrySelect 호출) — 동일하게 순서 교정.

세 건 모두 프로덕션 코드가 아니라 이번 그룹이 작성한 테스트 fixture 의 호출 순서 문제였다(생산 코드는
변경하지 않음). 수정 후 재실행에서 전부 통과.

## PlayMode (이 그룹 확장)

`GenrePersistencePlayModeTests.cs`에 2개 테스트 추가(기존 2개는 무변경):

- `ServiceManager_Survives_Scene_Load_With_Sns_Catalog_Of_Three`: MainMenu→Shop 실제 씬 전환 후 persistent
  `ServiceManager`가 SNS catalog 3종(ID ordinal 정렬)을 보유.
- `Night_Execution_Then_Day2_Plan_Has_Bonus_And_Tagged_Orders_Without_Any_Ui`: UI 를 전혀 활성화하지 않고
  Day1 루프(Market→Service→전량포기→Settlement→Night)→`TryExecuteSnsCampaign("photo_feed")`→Night→Market
  진입(Day2)→`TryBuildDayPlan`이 `BonusOrderCount>0`인 plan 생성→Market→Service 전환 후 실제
  `serviceOrders`가 `plan.OrderCount`와 일치하고 보너스 인덱스만 `snsInflow=true`임을 도메인 경로로 검증.

## Unity 검증 결과 (U7)

| 게이트 | 명령 | 결과 |
|--------|------|------|
| 컴파일 | `Unity.exe -batchmode -quit -nographics -projectPath game` | **exit 0**, `error CS` 0 (수 회 반복 검증) |
| EditMode | `-runTests -testPlatform EditMode` | **exit 0, 236/236 pass, 0 fail**(2회 연속 재실행 — 기존 230 + Codex 리뷰 001 반영 신규 6) |
| PlayMode | `-runTests -testPlatform PlayMode` | **exit 0, 4/4 pass**(기존 2 + 그룹3 신규 2, 무회귀) |

- `InitialDataBuilder.Apply → SceneBuilder.Apply → compile → EditMode → PlayMode` 순서로 전체 수행(그룹2가
  이미 적용한 산출물 위에서 이 그룹이 재검증).
- 씬 멱등성은 fileID multiset(오브젝트 총수) 동일 + persistent listener 0 기준으로 검증(Unity 가 매 실행
  fileID 를 재부여하므로 byte-hash 비교는 대상이 아님).
- `git status --short game`에 Library/Temp/Obj/Logs/UserSettings/Build 산출물 없음. 신규 파일
  (`DayModifier.cs`, `Social/*.cs`, `SNSCampaignOpsTests.cs`, `SNSBalanceTests.cs`, `NightPanelSceneTests.cs`,
  `NightPanelSnsFlowTests.cs`) 전부 `.meta` 존재. Scene/폰트 파일 diff 는 배치 테스트 재실행 중
  `SceneBuilder.Apply()`/`OpenScene` 재실행으로 인한 fileID 재직렬화이며 실제 내용 변경이 아니다(task-110
  부터 일관되게 관찰됨).
- Build Settings 씬은 정확히 2개(`MainMenu.unity`, `Shop.unity`) — 씬 하드캡 불변.

## Codex 리뷰 001(request-changes) 반영: DayModifier 경로 audience 검증 + JsonUtility 왕복 테스트 추가

`kb/tasks/task-111/reviews/001.md`(request-changes)의 Action 2건을 정확히 그 범위만 반영했다. 핵심 수학·계약은
Codex 가 "설계와 일치" 확인했으므로 변경하지 않았다.

- **Action 1 — DayModifier 경로 audience row 검증**: `SNSCampaignOps.TryBuildDayModifier`(D4)가 history
  레코드의 campaignId 로 def 를 조회한 뒤 곧바로 `ProjectAffinityRows(def.AudienceAffinities)`를 호출해,
  집행(`TryExecute`)·미리보기(`TryBuildPreview`) 경로가 쓰는 `IsDefInvalid` 검증(중복 `(AgeBand,Gender)`·
  잘못된 multiplier)을 거치지 않는 gap 이 있었다. **수정**: 매칭된 def 로 boost 를 만들기 전에
  `IsDefInvalid(def, out failReason)`를 그대로 재사용해 검증하도록 `SNSCampaignOps.cs`에 한 줄 추가했다 —
  저장 후 재개·catalog 변경으로 audience row 가 손상되면 조용한 max-매칭 대신 명시적 실패 + GameState
  완전 불변을 보장한다.
  - **부수 발견/수정**: 이 재사용 과정에서 `IsDefInvalid`의 기존 `foreach (var row in def.AudienceAffinities)`
    루프가 `AudienceAffinities == null`을 방어하지 않아 `NullReferenceException`을 던지는 잠재 버그를
    신규 테스트가 즉시 노출했다(`ProjectAffinityRows`/`HasDuplicateAudienceRow`는 이미 null-safe였으나 이
    루프만 누락). null 가드를 추가해 일관성을 맞췄다 — 이 자체도 Action 1 범위(동일 검증 로직 재사용) 안의
    수정이며 별도 계약 변경은 아니다.
  - **신규 테스트**(`SNSCampaignOpsTests.cs`, 4건): 매칭 def 가 중복 audience row 를 가지면 명시적 실패,
    multiplier `{0, -1, NaN, +Infinity}` 각각 명시적 실패, `AudienceAffinities == null`(타겟 없음 — 정상
    케이스)은 예외 없이 전 고객 중립(1000) modifier 생성, 실패 경로에서 `snsCampaignHistory` 레코드가
    교체·변경되지 않음(참조 동일성 확인).
- **Action 2 — JsonUtility 왕복 결정론 테스트**: 신규 `SNSJsonRoundTripTests`(`SNSCampaignOpsTests.cs`
  같은 파일, 별도 클래스 — 씬/매니저가 필요해 순수 클래스와 분리) 2건.
  - `TryBuildDayPlan_Result_Is_Identical_Before_And_After_JsonUtility_RoundTrip`: `TestSceneSupport`로 실제
    `ServiceManager`를 띄우고 short_form 집행 → `JsonUtility.ToJson`/`FromJsonOverwrite`로 `GameState`를
    같은 인스턴스 위에 왕복(참조 불변, `GameManager.State`가 읽기 전용 프로퍼티이므로) → 왕복 전/후
    `service.TryBuildDayPlan`(실제 프로덕션 API, 재계산 없음) 결과가 `BaseOrderCount`/`BonusOrderCount`/
    `SourceCampaignId`/가중치 목록/주문별 recipe·customer·snsInflow 태그까지 field-by-field 동일함을 확인.
  - `SnsCampaignHistory_Survives_JsonUtility_RoundTrip_Field_By_Field`: `snsCampaignHistory` 2건짜리
    `GameState`를 `ToJson`→`FromJson<GameState>`로 새 인스턴스 왕복해 레코드 5필드(`campaignId`/
    `executedOnDay`/`costPaid`/`effectiveMilliReach`/`bonusOrderCount`/`followerGain`) 손실 없음을 확인 —
    task-113 저장/불러오기의 선행 보증.

**재검증**: 컴파일 exit 0(error CS 0) · **EditMode 236/236 pass**(2회 연속, 기존 230 + 신규 6) ·
**PlayMode 4/4 pass**(무회귀) · `git status --short game` 오염 없음(이번 반영은 기존 파일 2개만 수정 —
`SNSCampaignOps.cs`/`SNSCampaignOpsTests.cs`, 신규 파일 없음).

## 설계 대비 변경 사항

| 항목 | 설계 내용 | 실제 구현 | 사유 |
|------|-----------|-----------|------|
| U6 밸런스 guard 재유도 방법 | "100개 결정론 day" 방법론 서술 | `GenreBalanceTests.cs`(task-110) 패턴 계승 — 실제 `DayModifier`를 `TryBuildDayModifier`로 재구성해 프로덕션 Ops 를 그대로 호출 | 재계산 없이 프로덕션 경로 그대로 검증하는 편이 회귀에 더 강함. 1회차 재유도에서 즉시 통과(±0.3원 이내) — 에스컬레이션 불필요 |
| scene 테스트 fixture 구조 | 명시 없음 | task-110 계승 — 구조 테스트(`NightPanelSceneTests`)와 상태 테스트(`NightPanelSnsFlowTests`)를 별도 클래스로 분리 | OpenScene 재로드가 Awake/Start/OnEnable 동기 호출을 보장하지 않는 특성과 결합해 상태 오염 방지 |

## Codex 대기 게이트 (self-approve 하지 않음)

design.md 테스트 기준 목록 중 다음 항목은 Claude 가 자체 승인할 수 없다 — Codex/오너 검토 대기:

- **640×360 원본 캡처 시각 승인**: Night SNS 블록이 겹침·canvas 이탈 없이 F1 좌표·폰트·focus 순서와
  일치함을 검토 — 자동 테스트는 오브젝트 존재/좌표값만 확인했고, 실제 렌더 결과의 시각적 검토는
  하지 않았다. **대기**.
- **수동 Play smoke**(Night 집행 판단 60초 이내, 집행→익일 badge `+N`→`SNS 유입` 태그→Settlement 라인의
  인과 사슬, 반복 집행 시 `+2→+1팀` 미리보기 감쇠, 자금 부족 버튼 비활성): PlayMode 자동 테스트로 도메인
  경로는 검증했으나, 실제 화면 조작 기반 수동 smoke 는 이번 turn 에서 수행하지 않았다. **대기**.
- **코드 리뷰**: `reviews/001.md`(request-changes) 2건 Action 을 위 절에서 반영·재검증 완료. 반영에 대한
  Codex 재검토·승인은 **대기**.
- **오너 승인**: SNS 비용 재시드(15,000/12,000/7,000)는 design.md 오픈 이슈에 따라 오너 최종 승인이
  필요하다. **대기**.

## 다음 단계

1. Codex 가 리뷰 001 반영분을 재검토·승인, 오너가 나머지 3개 게이트(640×360 시각·수동 smoke·비용 재시드)를 검토·승인.
2. task-112(이벤트/장애물)로 진행. 전체 3일 수직 슬라이스 게이트는 task-112 이후 수행.
