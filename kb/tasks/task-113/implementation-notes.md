# 구현 노트 — task-113

> Status: done
> Inputs: kb/tasks/task-113/design.md
> Outputs: 저장/불러오기 도메인(U1)·매니저 배선(U2)·UI/SceneBuilder(U3-U4)·테스트/PlayMode(U5)·검증·기록(U6)
> Next step: Codex 코드 리뷰 완료(2026-07-12, reviews/001-003 — 코드 결함 소진, "Codex 대기 게이트"
> 절 참조). 남은 오너 게이트: 640×360 시각 승인 + 수동 Play smoke(종료 후 이어하기 재개, 파산 잠금,
> 손상 파일 3종 사유 표시). (task-114/115 는 이미 완료됨)

## 현재 상태: done (구현 완료, Unity green — 세 그룹 세션에 걸쳐 완료)

이 turn은 세 그룹으로 나눠 순차 구현했다(같은 세션, SendMessage 재개).

- **그룹1 (U1 save domain + U2 manager wiring)**: `GameState.schemaVersion`, 신규
  `Runtime/Save/{SaveOps,SaveSummary,SaveFileStore}`, `GameEvents.SaveStateChanged`, `GameManager`
  Save/Load/Peek API + V11 + 트리거 5종, `ServiceManager.EnsureServiceDay` 제거,
  `SettlementManager` 트리거3 배선.
- **그룹2 (U3 MainMenu/Night UI + U4 SceneBuilder)**: 이어하기 블록(`ContinueButton`/
  `SaveStatusText`)·G2 분기 4종·Night 저장 표시 라인, SceneBuilder 좌표 생성·주입.
- **그룹3 (U5 테스트/PlayMode + U6 검증·기록, 이 문서)**: 그룹1이 남긴 도메인/매니저 테스트에
  씬/UI/flow/PlayMode 커버리지를 추가하고 최종 배치 검증·기록을 수행.

## U1-U2 요약 (그룹1)

- `GameState`: `SaveSchemaVersion`(=1) 상수 + `schemaVersion` 필드(클래스 최상단).
- 신규 `Runtime/Save/{SaveOps(순수 파이프라인),SaveSummary(peek DTO),SaveFileStore(System.IO 원자 쓰기)}`.
- `SaveOps`: `TrySerialize`(검증 후 `ToJson(prettyPrint:true)`) / `TryDeserialize`(공백검사→
  `SaveVersionProbe`→버전확인/`TryMigrateToCurrent`(v1 단일)→`FromJson<GameState>`→**V2b 정규형**
  (`ToJson(loaded,prettyPrint:true)==입력json` 바이트 동일)→`TryValidateState`(V3~V10)) / 저장·peek·
  로드 세 경로가 공유하는 **단일 `TryValidateState`**(V3 non-null 총칙: List 4종+문자열 ID 필드,
  V4 파산정합, V5 장르, V6 인벤토리, V7 서비스(prefix/suffix+통계 일치), V8 구매, V9 SNS(Night cash
  일치), V10은 `EventOps.TryBuildDayEffects` 재사용) / `BuildSummary`.
- `GameManager`: `SaveFilePathOverride`(internal IVT)/`ResolveSavePath`/`LastAutoSave{FailReason,Day,
  Phase}`/`HasSaveFile`/`SaveGame`/`AutoSave`(internal, `Application.isPlaying` 가드)/`StartNewRun`/
  `TryPeekSave`(dry-run 원상복구)/`TryLoadGame`(V11 사후검증+실패 시 완전 롤백). **V11**(설치 후,
  manager 계층 — SaveOps 합성 복제 금지): 파산/미선택 장르는 통과, 아니면 `TryBuildDayPlan`+
  `ServiceOps.BuildOrders(plan,customerDefs)` 재생성 후 `serviceDay==day`일 때만 인덱스별
  recipeId/customerId/partySize/snsInflow/eventInflow 정확 일치 비교(served/missed/index는 저장값
  존중). 트리거 5종: ①`StartNewRun`, ②`AdvancePhase` 전환 발생 시, ③정산 신규 적용(`SettlementManager.
  ApplyDailySettlement()` + `GameManager.AdvancePhase()` 내부 인라인 경로 둘 다, 파산 확정 포함),
  ④`GameEvents.GenreSelected` 구독, ⑤`GameEvents.SNSCampaignExecuted` 구독 — 전부 `AutoSave()` 경유.
- `ServiceManager.EnsureServiceDay` 제거(호출자 0, 컴파일 게이트 재확인). neutral
  `ServiceOps.BuildOrders(recipes,customers,day)` 3-인자 오버로드는 보존.

## U3-U4 요약 (그룹2, 관찰 기준 — 이 세션에서 재검증만 수행)

- `MainMenuController`: `continueButton`/`saveStatusText` 필드, `EditorInit(start,continue,
  saveStatus)` 3-인자(1-인자 위임 보존), `RefreshSaveUi()`(internal, G2 분기 4종 — 없음 Cream/정상
  활성 Cream/파산 잠금 Plum/손상 잠금 Plum), `OnStartClicked`→`StartNewRun()+LoadShopScene()`,
  `OnContinueClicked`→`TryLoadGame` 성공 시 `LoadShopScene()`/실패 시 `RefreshSaveUi()`(조용한 새게임
  진행 없음).
- `NightPanelController`: `RenderStatusLine`(비파산 2행 확장 — 성공 `자동 저장됨 · Day{n} {phase}`/
  실패 `<color=#A93E58>자동 저장 실패: {사유}</color>`), `GameEvents.SaveStateChanged` 구독
  (OnEnable/OnDisable). 좌표·EditorInit 시그니처 불변.
- `SceneBuilder.BuildMainMenu`: `ContinueButton`(0,-104)/(200×44), `SaveStatusText`(0,-146)/(420×24)
  10pt, 상하 explicit navigation(StartButton↔ContinueButton).

## U5 — 테스트 (이 그룹)

### 신규 EditMode 테스트

- **`MainMenuSaveFlowTests.cs`**(7 tests): G2 분기 4종(없음/정상/파산/손상) — `RefreshSaveUi` 상태·
  카피·버튼 활성/잠금·색(Steam Cream `#F4E5C2`/Warning Plum `#A93E58`) 정확 일치, 재계산 금지(peek
  이후 state 변경이 이미 렌더된 문구에 영향 없음), 손상 세이브는 조용한 새 게임 진행 없이 state 참조
  유지, `OnContinueClicked` 실패 시 재잠금. 모두 `GameManager.SaveFilePathOverride`(temporaryCachePath)
  경로 격리. `TestSceneSupport.OpenMainMenuSceneWithLiveSingletons()`(신규 helper) +
  `ForceAwake`(신규 helper — 배치 EditMode 에서 `Awake()` 비동기 호출 함정으로 `onClick.AddListener`
  가 등록되지 않아 클릭이 무시되는 증상을 재현·수정) + `ForceStart`.
- **`SceneBuilderTests.cs` 확장**(5 tests): MainMenu `ContinueButton`/`SaveStatusText` 존재·G1
  좌표(anchoredPosition/sizeDelta) 정확 일치·상하 explicit navigation·MainMenu `GameManager`+
  `ServiceManager` 동거 확인·연속 2회 `Apply` 멱등(오브젝트 수 동일, persistent listener 0).
- **`NightPanelSceneTests.cs` 확장**(3 tests): StatusText 좌표 불변((0,-72)/(460×32)) 재확인,
  자동 저장 성공/실패 worst-case 라인 460px 이하(`GetPreferredValues`) — 실패 사유는 검증 매트릭스
  중 대표적으로 긴 문자열("서비스 통계가 주문 목록과 일치하지 않습니다") 사용(I/O 예외 메시지는
  환경 의존적이라 worst-case 대표값에서 제외 — task-112 F5 전례와 동일 원칙).
- **`NightPanelSnsFlowTests.cs` 확장**(1 test): 저장 시도 기록 없음(fixture 기본값) 시 기존 1행
  유지만 EditMode 에서 검증 — 성공/실패 라인의 실제 `LastAutoSave*` 갱신은 `AutoSave()`가
  `Application.isPlaying` 가드로 EditMode 에서 항상 no-op 이라 재현 불가능해 PlayMode 로 이관(아래).
- **`TestSceneSupport.cs` 확장**: `OpenMainMenuSceneWithLiveSingletons()`(Shop 버전과 대칭),
  `ForceAwake()`(신규 — Start/OnEnable 과 같은 배치 EditMode 타이밍 함정 계열).

### 신규 PlayMode 테스트 — `SaveLoadPlayModeTests.cs`(2 tests)

- **`[SetUpFixture] SaveLoadPlayModeFixture`**: `OneTimeSetUp`/`OneTimeTearDown`으로 전 PlayMode
  테스트 세션에 `GameManager.SaveFilePathOverride`(temporaryCachePath 하위 고유 폴더)를 걸고 세션
  종료 시 정리 — 기존 PlayMode 6개를 포함해 어떤 테스트도 실사용 `persistentDataPath/save.json`을
  건드리지 않는다. 각 테스트의 `[UnityTearDown]`이 세이브 파일 자체를 매번 삭제해 다음 테스트가
  "파일 없음" 상태에서 시작하도록 보장한다.
- **트리거 5종 실파일 확인 + RT1/RT3**: MainMenu 부팅→`StartNewRun`(트리거1, 파일 생성+미선택
  상태 확인)→Shop 진입→장르 확정(트리거4, `GameEvents.RaiseGenreSelected` + 저장된
  `selectedGenreId` 필드 직접 대조)→Market→Service 전환(트리거2, 저장된 `currentPhase` 값 대조)→
  전량 포기→Service→Settlement 전환(트리거3, 저장된 `currentPhase`+`settlementDay` 대조)→
  Settlement→Night 전환→SNS 집행(트리거5, 저장 파일에 `"photo_feed"` 레코드 존재 확인)→상태
  스냅샷→`StartNewGame()`으로 소거→`TryLoadGame()`→day/cash/selectedGenreId field 동일(RT1) +
  `TryBuildDayPlan`+`ServiceOps.BuildOrders` 재생성 결과가 왕복 전후 완전 일치(RT3, recipeId/
  customerId/partySize/snsInflow 전 항목 대조). **mtime 비교 대신 저장 파일의 실제 필드 값을
  직접 읽어 대조**(더 강한 증거 — 자체 파서는 정규형 JSON 한 줄=한 필드 규약만 이용, 프로덕션
  역직렬화는 여전히 `SaveOps`/`GameManager.TryLoadGame` 단일 경로).
- **Night 세이브 이어하기 → Shop 진입**: 도메인 경로로 Night phase 세이브 준비→MainMenu 재진입→
  `TryPeekSave`(요약 Phase==Night 확인)→`TryLoadGame`+`LoadShopScene()`→Shop `Panel_Night` 활성·
  `DayPhaseText`에 "밤" 포함·`GameManager`/`ServiceManager` persistent instance 생존·genre/event/
  SNS catalog 카운트 로드 전후 동일.

### 자체검증 중 발견·수정한 테스트 버그 (프로덕션 코드 변경 아님)

- 그룹1 작성 `SaveOpsTests`/`GameManagerSaveLoadTests` 잔여 실패 7건(이번 세션 재검증 시 발견 —
  V7 서비스 phase 전제(`serviceDay==day`) 누락, `settlementNetProfit` 미설정, Night 분기 SNS
  잔액 계약 미반영, V5 미선택 장르 예외조건(day1/Market) 오해, V11 테스트가 V5/V7 을 먼저 건드려
  의도한 경로를 가리지 못함)를 픽스처 수정으로 해결 — `TryValidateState`/`TryValidateOrderIdentity`
  자체는 첫 작성 그대로 정확했다.
- `MainMenuSaveFlowTests` 2건: 파산 픽스처에 `selectedGenreId` 누락(V5 우선 실패), `Awake()` 배치
  타이밍 문제로 `ContinueButton.onClick` 리스너 미등록(클릭이 무시됨) — `ForceAwake` 신설로 해결.

## U6 — 검증·기록 (이 그룹)

### Unity 배치 검증 결과

| 게이트 | 명령 | 결과 |
|--------|------|------|
| 컴파일 | `Unity.exe -batchmode -quit -nographics -projectPath game` | **exit 0**, `error CS` 0 (그룹1/2/3 단계별 3회 확인) |
| EditMode | `-runTests -testPlatform EditMode`(`-quit` 없이) | **exit 0, 428/428 pass, 0 fail**(그룹1 종료 시점 413 + U5 신규 15 = 428, 무회귀. 최초 실행 시 자체 픽스처 버그 2건 발견 즉시 수정 후 재실행에서 확정) |
| PlayMode | `-runTests -testPlatform PlayMode`(`-quit` 없이) | **exit 0, 8/8 pass**(기존 6 + 신규 2, 무회귀. 최초 실행에서 이미 통과) |

> **위 428/8 은 task-113 원 완료 시점 기준선이다.** 이후 task-114/115 가 스위트를 486 으로 확장했고,
> 2026-07-12 Codex 코드 리뷰가 발견한 실질 버그를 수정하며 회귀 테스트 8건을 추가해 **현재 기준선은
> EditMode 494 / PlayMode 9**(독립 재검증 통과). 상세는 아래 "Codex 대기 게이트" 절.

- Build Settings 씬 정확히 2개(`MainMenu.unity`, `Shop.unity`) — 씬 하드캡 불변.
- `git status --short game`에 세이브 산출물 없음 재확인: 실사용
  `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Client is King\` 아래에도 `save.json` 없음(존재하는
  파일은 Unity 기본 `TestResults.xml` 뿐 — 이 프로젝트의 세이브 파일과 무관). 모든 저장 I/O 테스트가
  `Application.temporaryCachePath` 하위 고유 폴더 override + TearDown 삭제로 격리됨을 확인.
- 신규 파일(`Runtime/Save/*.cs`, `Tests/EditMode/{SaveOps,SaveFileStore,GameManagerSaveLoad,
  MainMenuSaveFlow}Tests.cs`, `Tests/PlayMode/SaveLoadPlayModeTests.cs`) 전부 `.meta` 존재.
- 씬 재생성(`SceneBuilder.Apply()`) 시 매번 `MainMenu.unity`/`Shop.unity`/`Galmuri11 SDF.asset`의
  내부 fileID 가 재배치된다(빈 씬에서 매번 전체 재생성하는 기존 멱등 규약의 부작용 — 오브젝트
  수·좌표·컴포넌트 내용은 항상 동일, fileID 안정성은 애초에 보장 대상이 아니다). 이 세션의 최종
  git 상태는 U5/U6 테스트 배치 실행이 만든 최신 재생성 결과를 그대로 둔다.
- **중요 재확인(향후 세션 참고, task-112 노트 계승)**: `-runTests`는 `-quit`과 함께 쓰면 결과 파일이
  생성되지 않는 무증상 실패가 발생한다 — `-quit` 없이 실행하고 `tasklist`로 `Unity.exe` 프로세스
  종료를 폴링해야 한다. 이번 세션에서도 백그라운드 실행 래퍼가 조기에 "완료"를 보고하는 반면 실제
  Unity 프로세스는 계속 실행 중인 경우가 반복 관찰됐다 — 결과 파일 존재 + 프로세스 부재를 함께
  확인해야 안전하다.

### 검증 매트릭스/계약 최종 확인

- **V2b 정규형**: `ToJson(TryDeserialize(json))==json` 바이트 동일 — 필드 누락/명시 null/미지 키/
  순서 변조 4종 전부 `SaveOpsTests`에서 명시 실패 확인.
- **V11 주문 identity**: `GameManagerSaveLoadTests`에서 recipeId/partySize 변조 시 명시 실패 +
  이전 state 참조로 완전 롤백, recipe catalog 축소(genre 매칭 레시피 소실) 시에도 동일하게 롤백
  확인. served/index 만 다른 저장은 로드 성공(V7 이 정합 담당).
- **왕복 바이트 동일(RT2)**: `SaveOpsTests`가 대표 상태 6종 각각에서 이중 왕복 바이트 동일을
  확인. `SaveLoadPlayModeTests`가 실파일 경유로 RT1(field-by-field)/RT3(plan/주문 재생성 동일)를
  추가 확인.
- **트리거 `Application.isPlaying` 가드(332→428 EditMode 무회귀)**: `GameManagerSaveLoadTests`가
  `AdvancePhase`/`StartNewRun`이 EditMode 에서 파일을 쓰지 않음을 직접 확인, `SaveLoadPlayModeTests`
  가 PlayMode 에서는 5종 트리거 각각 실제로 파일을 갱신함을 확인.
- `EnsureServiceDay` 제거 후 컴파일 통과(호출자 0 재확인) — U1~U6 전 구간에서 재확인.

## Codex 대기 게이트 (self-approve 하지 않음)

- **Codex 코드 리뷰 (2026-07-12 재시도 성공 → 실질 결함 수정 완료)**: 당초 3회(560s/595s/600s)
  타임아웃으로 보류했으나(U1~U6 diff가 커서), 2026-07-12 재시도가 성공(reviews/001.md). Codex 가
  **실제 P1 버그**를 발견: `GameManager.TryPeekSave` 가 V11(주문 identity) 검증을 생략해, 주문 개수만
  맞고 recipeId/customerId/partySize/snsInflow/eventInflow 가 변조된 세이브가 MainMenu 이어하기에
  "정상"으로 표시(버튼 활성)됐다가 클릭 시에만 실패 → "손상=잠금+사유" 계약 위반. P2: V11 변조
  테스트가 5필드 중 recipeId/partySize 만 커버.
  - **수정 (커밋 예정)**: `TryValidateOrderIdentity(out)` → `(GameState s, out)` 파라미터화해
    `TryPeekSave`(dry-run loaded)·`TryLoadGame`(설치 state) 양쪽에서 호출(V11 비교 로직 불변).
    테스트 +5: customerId/snsInflow/eventInflow 변조 롤백 + `TryPeekSave` V11 잠금 회귀 + MainMenu
    V11-손상 분기. 회귀 테스트가 실제로 버그를 잡음을 확인(수정 임시 제거 시 2/2 FAIL).
  - 부수 발견·수정: task-115 D3 클리어 픽스처(`RefreshSaveUi_Shows_Cleared_Branch...`)가 serviceDay==day
    인데 serviceOrders 미충전인 불가능 상태를 손으로 세팅했고, peek 의 V11 생략 버그가 이를 가려주고
    있었다 → 실제 게임 흐름(장르선택→Service 진입→주문 처리→Settlement)으로 픽스처 재구성.
  - **2차 리뷰(reviews/002.md, request-changes) — 위 1차 수정이 불완전함을 발견**: `TryValidateOrderIdentity`
    내부 `ServiceManager.TryBuildDayPlan` 이 파라미터가 아니라 `ServiceManager.State`(= 전역
    `GameManager.Instance.State`)로 계획을 재생성한다(ServiceManager.cs:66/85/98/109). `TryLoadGame` 은
    loaded 를 설치한 뒤 검증해 우연히 맞지만, `TryPeekSave` 는 미설치라 콜드 스타트 MainMenu(Day 1)에서
    Day 2+ **정상** 세이브가 손상으로 잠기는 회귀. 1차 테스트는 저장 직후 같은 상태로 peek 해서 못 잡음.
  - **2차 수정 (커밋 예정)**: `TryPeekSave` 가 V11 검증 "동안"만 loaded 를 임시 설치(state/machine)하고
    성공/실패 무관하게 원복(TryLoadGame 미러 — 부작용 0). 회귀 테스트 +3: 런타임 state 를 `StartNewGame()`
    으로 발산시킨 뒤 유효 Day-N peek 성공 + MainMenu 이어하기 활성 / 변조본은 잠금 유지. **임시 설치 없이는
    이 3건이 FAIL 함을 확인**(정상 세이브 잠김 재현).
  - 독립 재검증(2차 수정 후): **EditMode 494/494 · PlayMode 9/9**(486→491→494, 누적 +8 신규).
    설계 리뷰 3건(V11 강화·V1~V10 non-null·schemaVersion 정책)은 원래대로 유지. V11 비교 로직·`TryLoadGame`
    무변경. 3차 재리뷰로 approved 확인 예정(reviews/003.md).
- **640×360 원본 캡처 시각 승인**: MainMenu 이어하기 블록(ContinueButton/SaveStatusText)·Night
  저장 표시 라인이 겹침·이탈 없이 좌표·폰트·카피와 일치하는지 — 자동 테스트는 오브젝트 존재/좌표값/
  worst-case 폭까지 확인했지만 실제 렌더 결과의 시각적 검토는 하지 않았다. **대기**.
- **수동 Play smoke**(design.md I16): 새 게임→Day2 Night 진행→에디터 Play 종료→재Play→이어하기
  요약·재개 화면·같은 예상 수요 확인, 파산 런 후 이어하기 잠금 확인, `save.json`을 손으로 깨뜨려
  (빈 파일/버전 99/필드 삭제) 사유 표시 확인 — PlayMode 자동 테스트로 도메인 경로는 검증했으나 실제
  화면 조작 기반 수동 smoke 는 수행하지 않았다. **대기**.

## 다음 단계

1. Codex가 U1~U6 전체 diff 리뷰, 오너가 나머지 2개 게이트(640×360 시각·수동 smoke)를 검토·승인.
2. 통과 시 task-114(아트 마감)로 진행.
