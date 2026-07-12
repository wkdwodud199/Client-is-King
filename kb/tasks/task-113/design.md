# 설계 문서 — task-113: 저장/불러오기 (JSON)

> Status: ready
> Inputs: `kb/concepts/project-brief.md`(SSOT — 순수 C# `GameState` → `JsonUtility`, Dictionary 금지·List 기반, Night = "SNS+저장", 매니저 규약), `kb/concepts/demo-scope.md`(하드캡 — 저장/불러오기 task-113 행, 씬 2개), `kb/tasks/task-110/design.md`(G2 — 저장 후 재개 시 동일 DayPlan/주문, G4 — GameState 장기 구조화 후보, C2 — 하루 상태머신, C3 — 회복 가능한 실패), `kb/tasks/task-111/design.md` + `implementation-notes.md`(JsonUtility 왕복 결정론 테스트 = task-113 선행 보증, snsCampaignHistory 재구성 계약, EnsureServiceDay legacy 제거 후보), `kb/tasks/task-112/design.md` + `implementation-notes.md`(activeEvents 저장·EventOps 검증 재사용, 기준선 EditMode 332/PlayMode 6), `kb/tasks/task-111/reviews/001.md`·`kb/tasks/task-112/reviews/001.md`(교훈 — 모든 재구성 경로 동일 검증, 필수 완전성, 손상 데이터 명시적 실패), 현재 `game/` 코드 기준선(`GameState` 전 필드/`GameManager`/`DayPhaseMachine`/`ServiceManager`/`SettlementOps·Manager`/`EconomyOps`/`SNSCampaignOps`/`EventOps`/`MainMenuController`/`PhaseHudController`/`MarketPanelController`/`NightPanelController`/`SettlementPanelController`/`SceneBuilder`), `game/ProjectSettings/ProjectSettings.asset`(productName)
> Outputs: task-113 구현 계약 — `GameState` 평면 스키마 유지 + `schemaVersion`(v1) 결정, 순수 `SaveOps` 파이프라인(프로브→버전→마이그레이션 훅→역직렬화→V2b 정규형 검사→검증 매트릭스 V1~V11, V11은 주문 identity 재검증), 원자적 파일 I/O(`Application.persistentDataPath/save.json`, tmp→Replace), 단일 슬롯·자동 저장 트리거 5종, MainMenu "이어하기"(peek 요약·파산/손상 차단), phase별 재개 매트릭스, 왕복 결정론 계약 RT1~RT5, `EnsureServiceDay` legacy 제거, EditMode/PlayMode 테스트 계약
> Next step: Claude가 scaffold `manifest.md`의 placeholder를 이 문서의 Inputs·영향 파일로 채운 뒤 U1~U6 순서로 구현하고, 배치 compile·EditMode·PlayMode 게이트를 통과시킨다. Codex 코드 리뷰와 오너 640×360 시각 승인·수동 Play smoke(종료 후 이어하기 재개) 게이트 후 `task-114`(아트 마감)로 진행한다.

## 목표 (Objective)

순수 C# `GameState`를 `JsonUtility`로 JSON 직렬화해 `Application.persistentDataPath` 하위 단일
파일에 **자동 저장**하고, MainMenu의 **"이어하기"** 버튼으로 역직렬화해 게임을 재개한다. 저장 후
재개해도 **같은 입력에서 같은 `TryBuildDayPlan`/주문이 재생성**되는 기존 왕복 결정론 계약
(task-111 리뷰 001 Action 2 → task-112 `EventJsonRoundTripTests`로 이미 선행 보증됨)을 실제 파일
경로 전체(직렬화→디스크→역직렬화→검증→설치)로 확장·강제한다.

`GameState`에 `schemaVersion`(v1)을 추가하고, 로드는 **프로브→버전 확인→마이그레이션 훅→역직렬화→
정규형 검사→검증 매트릭스**의 단일 파이프라인만 통과한다. 파일 부재·JSON 파싱 실패·버전 불일치·필드/불변식
위반은 전부 **명시적 실패**다(조용한 기본값 진행 금지) — MainMenu가 사유를 표시하고 이어하기를
잠그며, 안전 폴백은 항상 열려 있는 **새 게임**(플레이어 선택)이다. task-110 G4의 `GameState` 장기
구조화(CareerState/RestaurantState/… 중첩)는 **이번 task에서 채택하지 않기로 결정**한다(근거 B2).

### 역할과 결정권

| 영역 | 결정권자 | 수행자 | 게이트 |
|------|----------|--------|--------|
| 저장 스키마·검증 매트릭스·트리거 정책 | 이 설계(Claude 초안) → Codex 교차검토, 오너 최종 승인 | Claude 구현 | design validator + 왕복/검증 테스트 |
| MainMenu·Night UI 좌표·카피·색 | **Codex**(이 문서 G절은 제안 초안) | Claude가 SceneBuilder/UI로 구현 | Codex 640×360 화면 리뷰 |
| 코드 구조·테스트·배치 자동화 | 설계 계약 내 Claude | Claude | 컴파일·EditMode·PlayMode·멱등성 게이트 |
| 설계와 다른 화면·정책 판단 | Codex 재설계 요청 | Claude가 임의 확정하지 않음 | 새 design/review 기록 |

이 문서는 Claude가 작성한 설계 초안이며 Codex 설계 교차검토를 전제로 한다. 구현 중 문구·레이아웃·
정책이 불명확하면 임의 확장하지 않고 리뷰 대상으로 남긴다.

## 범위 (Scope)

### 포함 — task-113 구현 범위

- `GameState`에 `schemaVersion` 필드(+`SaveSchemaVersion = 1` 상수)를 추가한다. 스키마는 **현행
  평면 구조 그대로**(전 필드 int/bool/string/enum/List — float 없음), G4 중첩 구조화는 기각·기록한다.
- 순수 `SaveOps`(신규 `Runtime/Save/`): 직렬화/역직렬화 파이프라인, 버전 프로브, 마이그레이션 훅
  (v1 단일 — 불일치는 명시적 실패), 검증 매트릭스 V1~V11 + V2b 정규형(저장 전·로드 후 **같은
  함수**, V11은 주문 identity 비교), `SaveSummary`.
- 파일 I/O(`SaveFileStore` 정적 헬퍼 + `GameManager` 얇은 확장): 단일 슬롯
  `Application.persistentDataPath/save.json`, **원자적 쓰기**(`save.json.tmp` → Replace/Move),
  UTF-8(BOM 없음), 테스트 전용 경로 override(실사용 세이브 오염 방지).
- **자동 저장 트리거 5종**(F2 표 — 새 런 시작/phase 전환 완료/정산 신규 적용/장르 확정/SNS 집행)
  + `Application.isPlaying` 가드. 수동 저장 버튼은 두지 않는다.
- **로드 진입점은 MainMenu "이어하기" 단일**: `TryPeekSave`(dry-run — 상태 원상복구)로 버튼
  활성/요약("Day n · phase · 잔액")을 결정하고, 클릭 시 `TryLoadGame`(검증→설치→사후 plan 검증→
  실패 시 롤백) 후 Shop 씬 로드. 파산 세이브·손상 세이브는 이어하기 잠금 + 사유 표시.
- phase별 재개 동작 확정(G3 매트릭스): 기존 UI(`PhaseHudController.Start` 동기화, Market 장르 modal
  조건, Settlement 멱등 재구성, Night Render)가 이미 상태 기준으로 그리므로 UI 변경은 MainMenu
  이어하기 블록과 Night 자동저장 표시 라인뿐이다.
- `GameEvents.SaveStateChanged` 추가(저장 시도 후 1회 발행 — Night 표시 라인 갱신용).
- `ServiceManager.EnsureServiceDay` legacy 제거(task-111 오픈 이슈 이행 — 호출자 0 확인, plan 없는
  주문 재생성 경로는 재개 결정론 C3의 위험 요소).
- 왕복 결정론 계약 RT1~RT5(H절)와 EditMode/PlayMode 테스트, 기존 기준선(EditMode 332/PlayMode 6)
  무회귀 보존, SceneBuilder 멱등·씬 2개 하드캡 유지.

### 제외 — task-113에서 구현하지 않음

- 아트 마감(task-114), 밸런싱·엔딩·3일 결과 카드·Windows 빌드(task-115).
- 클라우드 세이브·Steam Cloud, 다중 프로필/계정, **다중 세이브 슬롯**(단일 슬롯 하드캡), 세이브
  암호화·난독화, 자동 백업 로테이션, 세이브 내보내기/가져오기 UI.
- 외부 직렬화 라이브러리(Newtonsoft/MessagePack 등) — 순수 `JsonUtility`만.
- `OnApplicationQuit`/포커스 상실 임의 시점 저장(오픈 이슈 — 이정표 체크포인트 정책 유지, task-115
  재검토), 저장 파일 자동 삭제/자동 복구(손상 파일은 사유 표시 후 보존 — 새 런 저장이 대체).
- G4 `GameState` 중첩 구조화(CareerState 등 — B2 결정 기록, 1.0 커리어/평판 데이터가 생기는 시점에
  schemaVersion v2 마이그레이션으로 수행), run 시드 필드 추가(task-112 오픈 이슈 — task-115 몫).
- 요리대회·장슐랭·푸드트럭(post-demo GDD 설계 — 이번 구현과 무관), 신규 씬, 신규 singleton
  manager(`SaveManager` 신설 보류 — 오픈 이슈), 마지막 밤/엔딩의 저장 정책(task-115 엔딩과 함께).
- 기존 SNS·이벤트·장르·정산 수학의 어떤 변경도 없음(값·공식·시드 전부 불변).

## 제약 (Constraints)

- `kb/concepts/project-brief.md`가 SSOT, `kb/concepts/demo-scope.md`가 하드캡이다: 저장은 "순수 C#
  `GameState` → `JsonUtility` (Dictionary 금지, List 기반)", 씬은 `MainMenu.unity`+`Shop.unity` 2개.
- Unity 6 `6000.3.8f1`, 2D URP, 640×360 Pixel Perfect, TMP Dynamic Galmuri 유지. UI·무대는
  `SceneBuilder.Apply` 코드 저작이며 수동 씬 편집을 정본으로 삼지 않는다.
- 정적 데이터는 ScriptableObject(변경 없음), 런타임 상태는 순수 C# `GameState`(문자열 ID + List,
  Dictionary·SO 직접 참조 저장 금지). **세이브 파일에는 SO/에셋 참조·경로가 절대 들어가지 않는다**
  — 문자열 ID만 저장하고 catalog는 로드 시점 SceneBuilder 주입분으로 해석한다.
- 순수 규칙은 `*Ops`에, Unity lifecycle/SO 참조/파일 I/O는 manager 계층에 둔다. **예외 규정(명시적
  결정)**: `SaveOps`는 `UnityEngine.JsonUtility`만 예외적으로 참조한다 — 직렬화가 이 도메인의 규칙
  그 자체이고, JsonUtility는 lifecycle 없는 결정론적 정적 함수이며, 기존 왕복 테스트가 이미 EditMode
  에서 검증한 전례를 따른다. `System.IO`·씬·SO·`Application`은 `SaveOps`에서 금지한다(파일 I/O는
  `SaveFileStore`+`GameManager`, 경로 상수의 `Application.persistentDataPath`는 `SaveFileStore`만).
- **신규 singleton manager 금지**: 브리프 매니저 8종 목록의 `SaveManager`는 task-111 `SNSManager`·
  task-112 `EventManager` 보류 전례에 따라 **`GameManager` 얇은 확장 + 순수 `SaveOps` + 정적
  `SaveFileStore`**로 대체한다(오픈 이슈에 SSOT 정합 기록).
- **결정론**: 같은 상태 → 같은 JSON 바이트(스키마에 float 필드 없음 — B3 표가 증명), 같은 JSON →
  같은 상태 → 같은 plan/주문. 시각·프레임·랜덤·환경 의존 값(타임스탬프 등)을 세이브에 넣지 않는다.
- **명시적 실패**: 파일 부재·읽기 실패·파싱 실패·버전 불일치·검증 실패는 전부 사유 문자열과 함께
  실패를 반환하고, 실패 경로는 현재 `GameState`를 **완전히 불변**으로 유지한다(로드 사후 검증
  실패는 설치 전 상태로 롤백 — F4). 조용한 기본값 진행·부분 로드 금지. task-111/112 리뷰 교훈에
  따라 **저장·peek·로드 세 경로가 같은 검증 함수 하나**를 통과한다(경로별 검증 격차 금지).
- **하위호환**: 기존 공개 API 시그니처를 유지한다. 단 하나의 예외 — `ServiceManager.EnsureServiceDay`
  는 task-111 design 오픈 이슈가 "task-113 저장 설계 전에 제거 후보로 검토"로 예약했고 프로덕션·
  테스트 호출자 0건이 확인되어 **제거**한다(사유: plan을 거치지 않는 주문 재생성 경로가 재개 상태에서
  호출되면 C3 동일-주문 계약을 조용히 깨뜨릴 수 있는 유일한 구멍).
- 자동 저장은 `Application.isPlaying`일 때만 발동한다(EditMode 테스트의 `AdvancePhase` 호출이 파일을
  쓰지 않도록 — 기존 332개 테스트 무회귀의 전제). 명시적 `SaveGame()` 호출은 가드 없이 동작하며,
  모든 저장/로드 테스트는 **경로 override**(`Application.temporaryCachePath` 하위)로만 파일을 만들어
  실사용 세이브와 리포를 오염시키지 않는다. PlayMode는 `[SetUpFixture]`로 전 테스트에 override를 건다.
- 저장 실패는 게임 진행을 차단하지 않는다(진행 차단은 손상 **로드**에만 적용) — 실패 사유를
  `LastAutoSaveFailReason`으로 노출하고 Night 라인에 색+문구 병용으로 표시한다.
- 통화는 원화 정수 계약 유지. UI는 색만으로 상태를 전달하지 않는다. `game/ProjectSettings/`·
  `game/Packages/`·`.meta`는 버전 관리 대상이며 에셋과 `.meta`는 쌍으로 처리한다. Build Settings 씬 2개.
- 네임스페이스는 `ClientIsKing.Save`(폴더 `Runtime/Save/`)를 사용한다.

## 구현 단계 (Implementation Steps)

### A. 플레이어 경험 — 저장·재개 인과 루프

1. **첫 실행 MainMenu**: `게임 시작`만 활성이고 이어하기 아래 `저장된 게임이 없습니다.`가 보인다.
   `게임 시작`을 누르면 새 런이 시작되며 그 즉시 첫 자동 저장이 기록된다(F2 트리거 1).
2. **플레이 중**: phase가 넘어갈 때마다, 장르를 확정할 때마다, 정산이 적용될 때마다, SNS를 집행할
   때마다 자동 저장된다. Night 상태 라인에 `자동 저장됨 · Day {n} {phase}`가 함께 표시된다
   (실패 시 Warning Plum `자동 저장 실패: {사유}` — 게임은 계속된다).
3. **종료 후 재실행**: MainMenu 이어하기가 `Day {n} · {phase} · 잔액 {cash}원` 요약과 함께 활성이다.
   누르면 마지막 자동 저장 이정표에서 그대로 재개된다 — Market이면 같은 예상 수요·같은 재료 목록,
   Service면 남은 주문 그대로, Night면 정산 요약·SNS 버튼 상태 그대로. **재개 후 만들어지는 수요
   계획과 주문은 저장 전과 field-by-field 동일하다**(H절 계약 — 플레이어 관점: "껐다 켜도 게임이
   말을 바꾸지 않는다").
4. **최악 손실**: 자동 저장 지점 사이의 조작(예: Market에서 산 재료, Service에서 처리한 주문 일부)
   만 잃는다 — 하루 3~5분 루프에서 1~2분 이내.
5. **파산 런**: 파산 상태도 정직하게 저장된다. MainMenu는 이어하기를 잠그고 `지난 게임은 파산으로
   끝났습니다 (Day {n}) — 새 게임을 시작하세요.`를 표시한다(회복 서사는 task-115 결과 카드 몫).
6. **손상 파일**: 이어하기가 잠기고 `저장 데이터를 불러올 수 없습니다: {사유}`가 표시된다. 조용히
   새 게임으로 넘어가지 않으며, 플레이어가 `게임 시작`을 선택하면 새 런의 첫 저장이 파일을 대체한다.

### B. 저장 스키마 계약

#### B1. schemaVersion (GameState 필드 + 상수)

```csharp
// GameState.cs — 클래스 최상단 (JSON 필드 순서 = 선언 순서 → 프로브·육안 확인 친화)
/// <summary>세이브 스키마 버전 (task-113). 스키마 파괴 변경 시에만 +1.</summary>
public const int SaveSchemaVersion = 1;

/// <summary>이 상태의 스키마 버전 — 새 상태는 항상 SaveSchemaVersion.</summary>
public int schemaVersion = SaveSchemaVersion;
```

- 상수의 단일 원천은 `GameState.SaveSchemaVersion`이다(`SaveOps`가 참조 — Save→DayCycle 방향,
  역참조 없음). 프로브 DTO의 기본값은 **0**이므로 `schemaVersion` 필드가 없는 JSON은 v0으로 읽혀
  명시적으로 실패한다(v1 이전 배포 세이브는 존재하지 않는다 — task-113이 첫 저장 기능).
- **v1 정규형 계약 (Codex 설계 리뷰 반영)**: 유효한 v1 파일은 B3 표의 **전 필드를 정확히 그
  집합·선언 순서 그대로** 포함한다(필드 누락·잉여/미지 키·명시 null 없음 — V2b 정규형 검사가
  로드에서 강제). 따라서 **직렬화 대상 필드의 추가를 포함한 모든 스키마 변경** — 필드 추가/삭제/
  이름·타입 변경, enum 멤버의 삭제·순서 변경(= 정수값 변경), 필드 의미 변경 — 은 **스키마 파괴
  변경이며 schemaVersion +1 과 마이그레이션을 요구한다.** "기본값이 하위호환인 필드 추가는 버전
  유지 가능"이라는 JsonUtility 관행(`snsInflow`/`eventInflow` 전례)은 버전 필드가 없던 시기의
  규약이고, v1 확정 이후에는 RT2 바이트 동일 계약(`TrySerialize(TryDeserialize(json)) == json`)과
  충돌하므로 허용하지 않는다 — JsonUtility 가 누락 필드를 기본값으로 채우므로 같은 v1에서 필드를
  추가하면 과거 v1 파일 재직렬화에 새 키가 생겨 바이트 동일성이 깨지기 때문이다.

#### B2. GameState 구조 결정 — 평면 유지 (G4 중첩 구조화 기각)

**결정: 현행 평면 구조를 유지하고 `schemaVersion`만 추가한다.** task-110 G4의
`CareerState/RestaurantState/SocialState/EventRuntimeState/CompetitionProgressState` 중첩안은
이번 task에서 채택하지 않는다. 근거:

1. **구성 요소 부재**: G4의 Career(fame/reputation/milestones)·CompetitionProgress는 데모에 데이터
   자체가 없다. 지금 중첩화하면 빈 껍데기 2종 + 기존 필드의 기계적 재배치뿐이다.
2. **직렬화 이득 없음**: JsonUtility는 평면/중첩 모두 동일하게 지원한다. 중첩은 JSON 모양만 바꾸고
   결정론·왕복·검증 어디에도 이득이 없다.
3. **회귀 표면적**: 평면 필드는 `EconomyOps`/`ServiceOps`/`SettlementOps`/`SNSCampaignOps`/
   `EventOps`/UI/테스트 332개가 직접 참조한다. 전면 재배치는 데모 마감(M3) 직전에 순수 리네이밍
   리스크만 산다 — task-110 G2 "기존 구조 얇은 확장" 원칙 위반.
4. **미래 경로가 이미 확보됨**: B1 마이그레이션 훅이 있으므로 1.0 설계에서 커리어/평판 실데이터가
   생길 때 v2 마이그레이션(평면→중첩 + 신규 필드)으로 안전하게 수행할 수 있다. 지금 결정을 미루는
   것이 아니라 **"평면 유지 + 버전 훅"이 결정**이다.

(Codex 교차검토 요청 지점 — 오픈 이슈. 반대 시 v1 스키마 확정 전인 지금이 마지막 전환 기회다.)

#### B3. v1 스키마 필드 표 (직렬화 대상 전수 — float 없음)

`JsonUtility.ToJson(state)`가 쓰는 필드 전부다(public 필드, 선언 순서). 모든 필드가
int/bool/string/enum(int 직렬화)/List — **부동소수가 없어 왕복이 바이트 안정적**이다.

| 필드 | 타입 | 도입 | 키/리셋 규약 | 검증 |
|------|------|------|--------------|------|
| `schemaVersion` | int | task-113 | 항상 `SaveSchemaVersion`(=1) | V2/V3 |
| `day` | int | 104 | Night→Market에서 +1 | V3 |
| `currentPhase` | enum `DayPhase`(0~3) | 104 | 상태머신 소유 | V3 |
| `cash` | int | 105 | 음수 불허(경제 규칙) | V3 |
| `ingredientStocks` | List\<IngredientStock\>(kind/grade enum, quantity) | 105 | (kind,grade) 유일 | V6 |
| `selectedGenreId` | string | 110 | ""=미선택, run 잠금 | V5 |
| `serviceDay` | int | 106 | 주문 목록의 소속 일차 | V7 |
| `serviceOrders` | List\<ServiceOrderState\>(recipeId/customerId/partySize/served/missed/snsInflow/eventInflow) | 106~112 | StartServiceDay가 교체 | V7 |
| `serviceCurrentOrderIndex` | int | 106 | 앞은 닫힘·뒤는 열림 | V7 |
| `serviceRevenueToday` ·`serviceOrders{Served,Missed}Today`·`serviceCustomers{Served,Missed}Today` | int×5 | 106 | StartServiceDay 리셋 | V7 |
| `marketSpendDay`/`marketSpendToday` | int×2 | 107 | day-key 리셋(EconomyOps) | V8 |
| `settlementDay` + `settlement{GrossRevenue,IngredientSpend,OperatingCost,NetProfit,CashBefore,CashAfter}` | int×7 | 107 | day당 1회, 멱등 재구성 원천 | V3/V4 |
| `daysCompleted` | int | 107 | 정산 성공 시 갱신 | V3 |
| `isBankrupt`/`bankruptcyDay`/`bankruptcyReason` | bool/int/string | 107 | 파산 게이트 원천 | V4 |
| `snsCampaignHistory` | List\<SNSCampaignRecord\>(campaignId/executedOnDay/costPaid/effectiveMilliReach/bonusOrderCount/followerGain) | 111 | run 누적, 1밤 1회 | V9 |
| `serviceSns{OrdersServed,OrdersMissed,Revenue}Today` | int×3 | 111 | StartServiceDay 리셋 | V7 |
| `activeEvents` | List\<ActiveEventState\>(eventId/remainingDays, 0=영구) | 112 | Night 경계 원자 교체 | V10 |
| `marketEventSurchargeToday` | int | 112 | marketSpendDay 리셋 공유 | V8 |
| `serviceEvent{OrdersServed,OrdersMissed,Revenue}Today` | int×3 | 112 | StartServiceDay 리셋 | V7 |

**enum 값 고정 계약**(스키마의 일부 — 핀 테스트로 고정): `DayPhase` Market=0/Service=1/
Settlement=2/Night=3, `IngredientKind` Rice=0/RiceCake=1/Noodle=2/Pork=3/Beef=4/FishCake=5/
Seaweed=6/Vegetable=7/Gochujang=8, `IngredientGrade` C=0/B=1. 이 값의 변경(멤버 삭제·재정렬)은
스키마 파괴 변경이며 schemaVersion +1과 마이그레이션 없이는 금지다(task-110 D2의 `GenreKind`
개명 금지와 같은 계열 — 1.0 taxonomy 이행 시 v2로 처리).

JSON 예시(발췌 — 정확한 바이트는 테스트가 왕복 동등성으로 고정하고, 문서는 모양만 규정):

```json
{
    "schemaVersion": 1,
    "day": 2,
    "currentPhase": 3,
    "cash": 47350,
    "ingredientStocks": [{ "kind": 3, "grade": 0, "quantity": 2 }],
    "selectedGenreId": "bunsik",
    "...": "…(B3 표 순서대로 계속)…",
    "snsCampaignHistory": [{ "campaignId": "short_form", "executedOnDay": 2, "costPaid": 12000,
        "effectiveMilliReach": 200, "bonusOrderCount": 2, "followerGain": 20 }],
    "activeEvents": [{ "eventId": "rent_increase", "remainingDays": 0 }]
}
```

### C. SaveOps 순수 파이프라인 (신규 `Runtime/Save/SaveOps.cs`)

```csharp
namespace ClientIsKing.Save
{
    /// <summary>세이브 규칙의 단일 원천 (순수 — System.IO/씬/SO 금지, JsonUtility만 예외 허용).</summary>
    public static class SaveOps
    {
        /// <summary>검증에 필요한 catalog 투영 입력 (manager가 조립 — SaveOps는 SO를 모른다).</summary>
        public sealed class SaveCatalogInputs
        {
            public IReadOnlyList<string> GenreIds;        // GameManager.GenreCatalog 투영
            public IReadOnlyList<string> RecipeIds;       // ServiceManager.RecipeDefs 투영
            public IReadOnlyList<string> CustomerIds;     // ServiceManager.CustomerDefs 투영
            public IReadOnlyList<string> SnsCampaignIds;  // ServiceManager.SnsCampaignDefs 투영
            public IReadOnlyList<GameEventDefInput> EventDefs; // GameManager.ToEventInputs (task-112)
        }

        [Serializable]
        internal sealed class SaveVersionProbe { public int schemaVersion = 0; } // 부재 = 0 = 실패

        // 저장 방향: 검증(V3~V11 상태부) → ToJson. 손상 상태를 디스크로 내보내지 않는다.
        public static bool TrySerialize(GameState state, SaveCatalogInputs catalogs,
            out string json, out string failReason);

        // 로드 방향: V1 구조 → V2 버전/마이그레이션 → FromJson<GameState> → V2b 정규형 → V3~V10 검증.
        // (V11 plan 재구성·주문 identity 검증은 manager 설치 후 수행 — F4)
        public static bool TryDeserialize(string json, SaveCatalogInputs catalogs,
            out GameState state, out string failReason);

        // 저장·peek·로드가 공유하는 단일 검증 (V3~V10 전부, 실패 시 첫 위반 사유 반환)
        public static bool TryValidateState(GameState state, SaveCatalogInputs catalogs,
            out string failReason);

        // 마이그레이션 훅 — v1 단일: fileVersion == SaveSchemaVersion 만 통과.
        // 미래: case 1: MigrateV1ToV2(ref json) … 체인. 사변적 기계는 지금 만들지 않는다.
        internal static bool TryMigrateToCurrent(int fileVersion, ref string json, out string failReason);

        // 검증 통과 상태의 표시 요약 (MainMenu 이어하기 라벨용 — UI 재계산 금지)
        public static SaveSummary BuildSummary(GameState state);
    }

    /// <summary>MainMenu 표시 전용 DTO (신규 Runtime/Save/SaveSummary.cs).</summary>
    public sealed class SaveSummary
    {
        public int Day;  public DayPhase Phase;  public int Cash;
        public int DaysCompleted;  public bool IsBankrupt;  public string SelectedGenreId;
    }
}
```

파이프라인 고정 순서(로드): (1) `json` null/공백 → 실패. (2) `FromJson<SaveVersionProbe>` —
`ArgumentException` 포착 시 파싱 실패. (3) `probe.schemaVersion != GameState.SaveSchemaVersion` →
`TryMigrateToCurrent` — v1에서는 모든 불일치가 실패(`0` = 필드 누락 포함). (4)
`FromJson<GameState>` — 예외 포착 시 파싱 실패, `null` 반환 시 실패. (5)
`state.schemaVersion == probe.schemaVersion` 재확인(불일치 = 손상). (6) **V2b 정규형 검사** —
`ToJson(state, prettyPrint: true)` 재직렬화 결과가 입력 `json`과 **바이트 동일**해야 한다.
JsonUtility 는 필드 누락·명시 null 을 조용히 기본값으로 채우므로 역직렬화 결과만으로는 감지할 수
없다 — 정규형 검사는 그 세부 동작에 의존하지 않고 필드 누락·명시 null·잉여/미지 키·순서 변조·중복
키를 단일 규칙으로 전부 명시 실패시킨다(Codex 설계 리뷰 반영 — 필수 완전성). (7) `TryValidateState`.
저장 방향은 (a) `state.schemaVersion == SaveSchemaVersion` 확인, (b) `TryValidateState`, (c)
`ToJson(state, prettyPrint: true)`. **prettyPrint true 확정**(사람이 읽는 디버깅 가치, 결정론 무관).

`SaveCatalogInputs` 검증(모든 공개 API 진입 시): null 컨테이너/각 목록 null/빈 목록/중복 ID →
명시적 실패(`EventDefs`는 `EventOps.TryValidateCatalog` 재사용 — 필수 4 kind 완전성 포함, task-112
리뷰 001 교훈의 이행).

### D. 검증 매트릭스 V1~V11 (손상/부재 → 명시적 실패)

각 행은 (감지 지점, 규칙, 실패 사유 문자열)이 계약이며 전 행이 테스트 대상이다. 사유 문자열은
테스트가 정확 일치로 고정한다. **어느 행도 기본값 보정으로 통과시키지 않는다.**

| # | 감지 지점 | 규칙 (전부 만족해야 통과) | 실패 사유 (형식) |
|---|-----------|---------------------------|------------------|
| V1 | 구조 | 파일 존재(호출자)·읽기 성공·json 비어 있지 않음·JSON 파싱 성공·역직렬화 결과 non-null | `저장 파일이 없습니다.` / `저장 파일을 읽을 수 없습니다: {IO사유}` / `저장 파일이 손상되었습니다 (JSON 파싱 실패).` |
| V2 | 버전 | `probe.schemaVersion == 1` (0 = 필드 누락/과거, ≥2 = 미래) · 본문 `schemaVersion` 과 프로브 일치 | `지원하지 않는 저장 버전입니다 (v{n}).` |
| V2b | 정규형(로드/peek) | `ToJson(역직렬화 결과, prettyPrint: true)` 가 입력 json 과 **바이트 동일** — B3 전 필드 존재·선언 순서 유지·잉여/미지 키 없음·명시 null 없음·중복 키 없음이 이 한 규칙으로 강제된다(필수 완전성 — task-111/112 리뷰 교훈, Codex 설계 리뷰 반영). 저장 방향은 `TrySerialize` 산출물이 같은 `ToJson` 경로의 정규형 그 자체이므로 자동 성립 | `저장 파일이 정규 직렬화 형식과 일치하지 않습니다 (필드 누락 또는 변조).` |
| V3 | 상태 공통 | **non-null 총칙** — B3의 모든 List 필드(`ingredientStocks`/`serviceOrders`/`snsCampaignHistory`/`activeEvents`)와 모든 문자열 필드(`selectedGenreId`/`bankruptcyReason`, 항목 내 `recipeId`/`customerId`/`campaignId`/`eventId` 포함)는 null 금지(빈 값 ≠ null — null 은 손상, 조용한 기본값 대체 금지) · `day ≥ 1` · `currentPhase ∈ [0,3]`(enum 범위 — JsonUtility는 범위 밖 정수도 enum에 넣는다) · `cash ≥ 0` · `daysCompleted == (settlementDay == day && !isBankrupt ? day : day-1)` · `settlementDay ∈ [0, day]` · 정산 기록 존재(`settlementDay ≥ 1`) 시 `settlementNetProfit == Gross−Spend−Cost` ∧ 각 항목 ≥ 0 · `currentPhase == Night → settlementDay == day` | `저장 데이터 검증 실패: {규칙별 한국어 사유}` (예: `일차가 잘못되었습니다`, `알 수 없는 phase 값입니다`) |
| V4 | 파산 정합 | `isBankrupt == false → bankruptcyDay == 0 ∧ bankruptcyReason == ""` · `isBankrupt == true → bankruptcyDay == day ∧ settlementDay == day ∧ cash == 0 ∧ bankruptcyReason != ""`(파산은 진행을 차단하므로 항상 당일 정산에서만 성립) | 〃 (`파산 기록이 일관되지 않습니다`) |
| V5 | 장르 | `selectedGenreId` 는 null 금지(V3 총칙 — null 을 빈 값처럼 취급하지 않는다) · `== ""` 는 `day == 1 ∧ currentPhase == Market` 에서만 허용(선택 전 진행 불가 게이트의 미러) · 비어 있지 않으면 `GenreIds` 에 존재 | 〃 (`알 수 없는 전문 분야 ID '{id}' 입니다` 등) |
| V6 | 인벤토리 | 항목 null 금지 · `kind ∈ [0,8]` · `grade ∈ [0,1]` · `quantity ≥ 0` · `(kind,grade)` 중복 금지 | 〃 |
| V7 | 서비스 | `serviceDay ∈ [0, day]` · 주문 항목 null 금지, `recipeId`/`customerId` 각 catalog 존재, `partySize ≥ 1`, `!(served ∧ missed)` · `serviceCurrentOrderIndex ∈ [0, count]` · 인덱스 앞([0,index))은 전부 닫힘, 뒤([index,count))는 전부 열림 · 통계 5종+SNS 3종+이벤트 3종의 건수 필드가 주문 목록 집계와 일치(`ordersServed == Σserved`, `customersServed == Σ served partySize`, sns/event 계열은 태그 주문만 — 매출 3종은 `≥ 0`만) · `currentPhase ∈ {Settlement, Night} → serviceDay == day ∧ 열린 주문 0` · `currentPhase == Service → serviceDay == day` | 〃 |
| V8 | 구매 | `marketSpendDay ∈ [0, day]` · `marketSpendToday ≥ 0` · `0 ≤ marketEventSurchargeToday ≤ marketSpendToday`(할증은 지출의 부분합 — 동시 리셋 규약) | 〃 |
| V9 | SNS | 레코드 null 금지 · `campaignId != "" ∧ SnsCampaignIds 에 존재` · `executedOnDay ∈ [1, day]` · `costPaid ≥ 0` · `effectiveMilliReach ≥ 0` · `bonusOrderCount ∈ [0,2]` · `followerGain ≥ 0` · 같은 `executedOnDay` 레코드 최대 1건(1밤 1회) · `currentPhase == Night ∧ !isBankrupt` 인 오늘 레코드 존재 시 `cash == settlementCashAfter − 오늘 레코드 costPaid`, 부재 시 `cash == settlementCashAfter`(Night 중 cash 변동은 SNS뿐 — 구성 경로 근거) | 〃 |
| V10 | 이벤트 | `EventOps.TryBuildDayEffects(state.activeEvents, state.day, EventDefs, …)` 성공 요구 — B3 불변식(미지 eventId/중복/remaining 음수/영구·시한 불일치)과 catalog 필수 4 kind 완전성을 기존 검증 그대로 재사용(제2 구현 금지) | EventOps 사유 그대로 전달 |
| V11 | 설치 후(manager) | `!isBankrupt ∧ selectedGenreId != ""` → `TryGetGenre` + `ServiceManager.TryBuildDayPlan` 성공 · `serviceDay == day`(오늘 주문 보유 — Service/Settlement/Night) → 같은 plan 으로 `ServiceOps.BuildOrders(plan, customerDefs)` 재생성 후 **주문 identity 비교**: 개수 일치 + 인덱스별 `recipeId`/`customerId`/`partySize`/`snsInflow`/`eventInflow` 정확 일치. **`served`/`missed`/`serviceCurrentOrderIndex` 진행 상태는 저장값 존중 — 비교 제외**(재생성 대상이 아닌 저장 진실, 정합성은 V7이 담당) — 불일치·실패 시 이전 상태로 롤백 | `저장 상태로 수요 계획을 재구성할 수 없습니다: {사유}` / `저장된 주문이 수요 계획과 일치하지 않습니다 (주문 {i}).` |

- V3~V10은 `TryValidateState` 하나에 있고 **저장 전에도 동일하게** 실행된다(`손상된 상태는
  저장하지 않습니다: {사유}` 접두어) — 손상을 디스크로 내보내는 경로 자체를 차단한다. V2b 정규형은
  로드/peek 파이프라인 단계이며(파일 텍스트가 입력일 때만 의미), in-memory 손상은 V3 non-null
  총칙이, 파일 텍스트 손상(누락/명시 null/잉여 키)은 V2b가 잡아 **어느 경로에도 조용한 기본값
  진행이 존재하지 않는다**.
- V7의 "닫힘/열림 프리픽스"와 통계 일치는 `ServiceOps`(serve/skip이 인덱스 순서로만 진행, 통계와
  주문을 원자 갱신) 구성이 보장하는 불변식의 미러다 — 규칙이 바뀌면 스키마 검증도 같이 갱신한다.
- **V11 주문 identity 비교의 단일 원천 규약 (Codex 설계 리뷰 반영 — 초안의 OrderCount 미러를
  강화)**: 손상 JSON 이 같은 개수의 다른 주문(recipeId/customerId/partySize/태그 변조)으로 바뀌면
  V7 의 catalog/통계 검증을 통과할 수 있으므로, task-110 G2 "저장 후 재개해도 같은 DayPlan/주문"
  계약을 로드 검증이 직접 막는다. 비교는 **manager 계층(V11)** 에서 기존
  `ServiceManager.TryBuildDayPlan` + `ServiceOps.BuildOrders(plan, customerDefs)` 프로덕션 경로를
  그대로 재사용한다 — **SaveOps 에 plan 합성 로직을 복제하지 않는다**(단일 원천 유지, 그래서 이
  검증은 설치 후 단계다). 비교 대상은 주문 identity 5필드뿐이고, `served`/`missed` 플래그와
  `serviceCurrentOrderIndex` 진행 상태는 재생성 대상이 아닌 저장 진실이므로 존중한다(진행 정합성은
  V7 prefix/통계 규칙이 담당).

### E. 파일 I/O 계약 (신규 `Runtime/Save/SaveFileStore.cs` — 정적 헬퍼, manager 계층)

```csharp
namespace ClientIsKing.Save
{
    /// <summary>세이브 파일 I/O 의 단일 원천 (System.IO 는 이 클래스와 테스트만 사용).</summary>
    internal static class SaveFileStore
    {
        internal const string FileName = "save.json";
        internal const string TempSuffix = ".tmp";
        internal static string DefaultPath; // Path.Combine(Application.persistentDataPath, FileName)

        internal static bool Exists(string path);
        internal static bool TryRead(string path, out string json, out string failReason);
        internal static bool TryWriteAtomic(string path, string json, out string failReason);
        internal static bool TryDelete(string path, out string failReason); // 테스트/향후 용도
    }
}
```

- **경로 규약**: 단일 슬롯 = `Application.persistentDataPath/save.json`. Windows 실경로
  `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Client is King\save.json`
  (ProjectSettings `companyName: DefaultCompany`, `productName: Client is King` — 변경은 task-115
  빌드 몫, 오픈 이슈). 슬롯 확장이 필요해지면 파일명 규약(`save-{slot}.json`)만 확장한다.
- **원자적 쓰기**: `{path}.tmp`에 UTF-8(BOM 없음, `new UTF8Encoding(false)`)로 전체 기록 →
  대상이 존재하면 `File.Replace(tmp, path, null)`(동일 볼륨 원자 교체), 없으면 `File.Move`.
  중간 크래시 시 tmp만 부분 기록되고 기존 `save.json`은 온전하다. 로드는 tmp를 절대 읽지 않고,
  잔존 tmp는 다음 쓰기가 덮어쓴다.
- 모든 I/O 예외(`IOException`/`UnauthorizedAccessException` 등)는 포착해 `false + 사유`로 변환한다
  (예외 전파 금지 — 호출자 `GameManager`가 진행 비차단 정책을 적용).
- PlayerPrefs·레지스트리·기타 저장 매체 금지. 파일은 이 경로 하나뿐이다.

### F. 트리거·매니저 배선 (GameManager 얇은 확장)

#### F1. GameManager API

```csharp
// GameManager.cs 추가 (신규 타입·singleton 없음)
internal static string SaveFilePathOverride;   // 테스트 전용 (IVT) — null/빈 문자열 = 기본 경로
string ResolveSavePath();                      // override ?? SaveFileStore.DefaultPath
public string LastAutoSaveFailReason { get; }  // "" = 마지막 저장 성공
public int LastAutoSaveDay { get; }            // Night 표시용
public DayPhase LastAutoSavePhase { get; }

public bool HasSaveFile { get; }               // SaveFileStore.Exists(ResolveSavePath())
public bool SaveGame(out string failReason);   // catalog 조립→TrySerialize→TryWriteAtomic, 항상 동작
internal void AutoSave();                      // Application.isPlaying 가드 → SaveGame, 실패는
                                               // LastAutoSaveFailReason + Debug.LogWarning (비차단),
                                               // 성공/실패 모두 GameEvents.RaiseSaveStateChanged()
public void StartNewRun();                     // StartNewGame() + AutoSave() — MainMenu 버튼 전용
public bool TryPeekSave(out SaveSummary summary, out string failReason); // dry-run (아래 F4)
public bool TryLoadGame(out string failReason);                          // 설치 (아래 F4)
```

- catalog 조립 `BuildSaveCatalogs()`: `genreCatalog`/`ToEventInputs(eventCatalog)` + 
  `ServiceManager.Instance`의 recipe/customer/sns ID 투영. `ServiceManager.Instance == null` →
  명시적 실패 `서비스 매니저가 초기화되지 않았습니다.`(MainMenu에도 같은 GO 부트스트랩이 있어
  정상 경로에서는 항상 존재 — SceneBuilder `CreateGameManager()` 기존 구조).
- `Awake()`의 암묵적 `StartNewGame()`(state==null 부트스트랩)은 **저장하지 않는다** — 앱 부팅이
  기존 세이브를 덮어쓰는 사고를 구조적으로 차단한다. 저장을 동반한 새 런은 `StartNewRun()` 하나뿐.

#### F2. 자동 저장 트리거 표 (전 트리거가 `AutoSave()` 한 경로)

| # | 트리거 | 호출 지점 | 저장되는 이정표 |
|---|--------|-----------|-----------------|
| 1 | 새 런 시작 | `StartNewRun()`(MainMenu `게임 시작` 클릭 경로만) | Day 1 Market, 장르 미선택 |
| 2 | phase 전환 완료 | `AdvancePhase()`에서 `machine.Advance()` 반환 직후(전환이 실제 발생한 경우만) | 각 phase 시작점 (Night→Market은 day+1·activeEvents 교체 완료 후) |
| 3 | 정산 신규 적용 | `SettlementManager.ApplyDailySettlement()`와 `GameManager.AdvancePhase()` 내부 적용 경로 모두, `result.Applied == true`(멱등 재구성 제외) 직후 — **파산 확정 포함** | 하루 결과 확정(흑자/파산) |
| 4 | 장르 확정 | `GameEvents.GenreSelected` 구독(Awake 구독/OnDestroy 해제 — named handler) | Day 1 Market + 선택 잠금 |
| 5 | SNS 집행 성공 | `GameEvents.SNSCampaignExecuted` 구독 | Night + 집행 레코드 |

- 트리거 2와 3이 겹치는 날(UI 경로: Settlement 패널 enable이 정산 적용 → 저장, 이후 Night 전환 →
  저장)은 중복 저장이며 무해하다(같은 파일 원자 덮어쓰기 — 멱등).
- 수동 저장 버튼 없음(오너 승인 지점 — 오픈 이슈). `OnApplicationQuit` 저장 없음(제외 절).
- 이벤트 발행: `GameEvents.SaveStateChanged`(payload 없음)를 `AutoSave()`/`SaveGame()` 시도 후
  1회 발행한다 — 구독자(Night 패널)는 `GameManager`의 `LastAutoSave*`를 읽어 표시만 갱신한다
  (도메인 규칙은 이 이벤트에 의존하지 않는다 — 표현 전용 이벤트 전례).

#### F3. ServiceManager — legacy 제거

`EnsureServiceDay(recipes, customers)`를 삭제한다. 근거: 프로덕션·테스트 호출자 0건(전수 검색),
task-111 design 오픈 이슈의 예약된 결정, plan을 우회한 `ServiceOps.BuildOrders(recipes, customers,
day)` neutral 경로가 재개된 상태에서 호출되면 장르/SNS/이벤트가 반영되지 않은 주문으로
`serviceOrders`를 덮어써 C3(재개 동일 주문) 계약을 조용히 깨는 유일한 잔존 경로이기 때문이다.
(neutral `ServiceOps.BuildOrders` 3-인자 오버로드 자체는 순수 계층 하위호환으로 보존한다 — 제거
대상은 manager의 상태 변경 wrapper뿐.)

#### F4. 로드/peek 절차 (원자성 — 실패 시 관찰 가능한 부작용 0)

```text
TryLoadGame:
 1. path = ResolveSavePath(); 파일 없음 → 실패 "저장 파일이 없습니다."
 2. SaveFileStore.TryRead → json (실패 → 사유 그대로)
 3. BuildSaveCatalogs (매니저 부재 → 실패)
 4. SaveOps.TryDeserialize(json, catalogs, out loaded, out reason)  ← V1~V10 + V2b 정규형, 현재 state 불변
 5. 설치: prev = (state, machine) 보관 → state = loaded, machine = new DayPhaseMachine(loaded)
 6. V11 사후 검증 — TryBuildDayPlan 성공 + (serviceDay == day 이면) BuildOrders 재생성 주문과
    저장 주문의 identity 5필드(recipeId/customerId/partySize/snsInflow/eventInflow) 정확 일치
    (served/missed/진행 인덱스는 저장값 존중). 실패 시 (state, machine) = prev 원복 후 실패 반환.
    설치~원복 사이에 이벤트 발행·프레임 경과가 없어(동기 블록) 부작용이 관찰되지 않는다.
 7. 성공: LastAutoSaveFailReason = "", LastAutoSaveDay/Phase = loaded.day/currentPhase 로 초기화.

TryPeekSave: 1~6 동일하되 성공이어도 항상 원복한다(dry-run). 성공 시 SaveOps.BuildSummary 반환.
```

- 로드는 **MainMenu에서만** 진입한다(이어하기 클릭 → `TryLoadGame` 성공 → `LoadShopScene()`).
  `TryLoadGame` 자체는 `DayPhaseChanged`를 발행하지 않으며, Shop 씬 UI 동기화는 기존
  `PhaseHudController.Start()`(state 기준 초기 표시)가 담당한다 — G3 매트릭스.
- `DayPhaseMachine` 생성자의 `day < 1 → 1` 보정은 V3가 선행 차단하므로 로드 경로에서 절대
  발동하지 않는다(발동 = 검증 버그).

### G. UI/UX — MainMenu 이어하기·Night 저장 표시·재개 매트릭스 (Codex 리뷰 게이트)

#### G1. MainMenu 레이아웃 (640×360 — 기존 오브젝트 좌표 불변, 신규 2종)

| 오브젝트 | anchoredPosition | sizeDelta | 폰트 | 변경 |
|----------|------------------|-----------|------|------|
| Title | (0, 60) | (520×80) | 48pt | 불변 |
| StartButton `게임 시작` | (0, −50) | (200×44) | 기존 | 불변 |
| **ContinueButton `이어하기` (신규)** | (0, −104) | (200×44) | StartButton 동일 | 신규 |
| **SaveStatusText (신규)** | (0, −146) | (420×24) | 10pt 중앙 | 신규 |

세로 점유: Start −28~−72 / Continue −82~−126 / Status −134~−158 — 겹침 없음, canvas 하단(−180)
여유 22px. focus: 이어하기 활성이면 이어하기 → 게임 시작, 비활성이면 게임 시작 단독(재개가 기본
행동 — Codex 확인 지점). explicit navigation 상하 배선.

#### G2. MainMenu 카피·상태 분기 (Codex 소유 UX copy — 임의 수정 금지)

`MainMenuController`는 `Start()`와 이어하기 실패 직후에 `RefreshSaveUi()`를 수행한다 —
`HasSaveFile`/`TryPeekSave` 결과만 표시하고 재계산하지 않는다.

| 상태 | ContinueButton | SaveStatusText | 색 |
|------|----------------|----------------|-----|
| 파일 없음 | 비활성 | `저장된 게임이 없습니다.` | Steam Cream `#F4E5C2` |
| peek 성공 · 정상 | **활성** | `Day {day} · {phase 라벨} · 잔액 {cash:N0}원` | Steam Cream |
| peek 성공 · 파산 | 비활성 | `지난 게임은 파산으로 끝났습니다 (Day {day}) — 새 게임을 시작하세요.` | Warning Plum `#A93E58` |
| peek 실패 (손상/버전/IO) | 비활성 | `저장 데이터를 불러올 수 없습니다: {사유}` | Warning Plum |

- 클릭 흐름: `게임 시작` → `gm.StartNewRun(); gm.LoadShopScene();`(기존 `StartNewGame()` 호출 교체
  — 새 런이 즉시 저장돼 기존 세이브를 대체한다. 확인 모달 없음 — 오픈 이슈). `이어하기` →
  `TryLoadGame` 성공 시 `LoadShopScene()`, 실패 시(파일이 그 사이 변한 경우 등) `RefreshSaveUi()`로
  사유 재표시 + 버튼 잠금.
- phase 라벨은 `PhaseHudController.PhaseLabel` 재사용(중복 카피 금지). 상태는 문구가 전달하고
  색은 보조다(색+문구 병용 규칙).

#### G3. 재개 매트릭스 — 로드 직후 Shop 씬이 보이는 것 (기존 UI 재사용, 신규 코드 없음)

| 저장 phase | 재개 화면 | 이미 성립하는 근거 (테스트로 고정) |
|-----------|-----------|-----------------------------------|
| Market (day 1, 장르 미선택 — 트리거 1) | 장르 선택 modal | `MarketPanelController.RefreshGenreSelectionUI`가 `selectedGenreId == ""` 기준으로 modal 표시 |
| Market (장르 확정 후) | 장보기 패널 + badge, modal 없음, 같은 예상 수요 | 동일 함수의 confirmed 분기 + `TryBuildDayPlan` 결정론 |
| Service | 남은 주문부터 재개(닫힌 주문은 그대로) | 주문·인덱스·통계가 전부 state 소유, `ServicePanelController.OnEnable`은 표시 refresh만(task-110) |
| Settlement | 정산 결과 재표시(재적용 없음) | `SettlementPanelController.OnEnable` → `ApplyDailySettlement` 멱등 재구성 분기(task-107) |
| Night | 정산 요약·예고 라인·SNS 버튼 상태(집행 완료 outline 포함) | `NightPanelController.Render()`가 state/history/forecast만 읽음 |
| 파산 | (이어하기 잠금 — 로드 불가) | G2 분기 |

phase 패널 활성화는 `PhaseHudController.Start()`가 `gm.State.currentPhase` 기준으로 동기화한다
(기존 코드 — Shop 씬이 Market 이외 phase로 열리는 최초 사례이므로 EditMode/PlayMode 테스트로 고정).

#### G4. Night 자동 저장 표시 (statusText 확장 — Codex 카피 게이트)

- 비파산 분기 statusText를 2행으로 확장한다:
  `내일 영업 준비 완료 — '다음 날 ▶' 버튼으로 진행하세요.` + 줄바꿈 후
  성공: `자동 저장됨 · Day {LastAutoSaveDay} {PhaseLabel(LastAutoSavePhase)}` /
  실패: `<color=#A93E58>자동 저장 실패: {LastAutoSaveFailReason}</color>`.
- 갱신 시점: `Render()`(기존) + `GameEvents.SaveStateChanged` 구독(OnEnable/OnDisable 쌍) — Night
  진입 저장이 패널 첫 Render 이후에 완료되는 순서 문제를 이벤트 구독으로 해소한다.
- 좌표·오브젝트 변경 없음(statusText (0,−72)/(460×32)/11pt 그대로 — 파산 분기가 이미 2행 사용).
  worst-case 폭은 씬 테스트에서 `GetPreferredValues` 460px 이하 자동 확인(task-112 F5 전례).

### H. 결정론·왕복 계약 (RT1~RT5 — 기존 task-111/112 왕복 가드의 확장·강제)

- **RT1 상태 왕복**: 대표 상태 6종 — (a) 새 런 직후(day1 Market 미선택), (b) 장르 확정+구매 후,
  (c) Service 중간(주문 일부 처리, SNS+단체 태그 포함), (d) 정산 후 Night(SNS 집행, 임대료 영구 +
  폭등 시한 이벤트 동시 활성), (e) day 5+ 누적 history 2건, (f) 파산 — 각각
  `TrySerialize → TryDeserialize` 후 B3 표 **전 필드 field-by-field 동등**(List는 길이+항목별).
- **RT2 바이트 안정성**: 같은 상태의 `TrySerialize` 2회 결과가 문자열 동일하고,
  `TrySerialize(TryDeserialize(json)) == json`(이중 왕복 바이트 동일 — 스키마에 float 없음이 근거).
  이 성질은 V2b 정규형 검사의 성립 근거이며, 로드 파이프라인이 같은 비교를 상시 수행한다(B1 v1
  정규형 계약과 일관 — 직렬화 필드 추가는 버전 +1 없이 불가).
- **RT3 plan/주문 재생성 동일**: 상태 (b)/(d)에서 왕복 전후 `ServiceManager.TryBuildDayPlan`이
  scalar·ordered rows(이벤트/SNS 필드 포함) field-by-field 동일하고, `ServiceOps.BuildOrders`
  결과의 recipe/customer/partySize/snsInflow/eventInflow까지 완전 일치한다(task-111 리뷰 001
  Action 2 → task-112 `EventJsonRoundTripTests` 계승 — 이번에는 **SaveOps 파이프라인 경유**로 확장).
- **RT4 이벤트 연속성**: 왕복 전후 `TryBuildEventForecast`/`TryBuildNextDayActiveEvents` 결과가
  동일하다(예고==적용 계약이 저장 경계를 넘어 보존 — C4 스케줄 day 벡터로 고정).
- **RT5 스키마 핀**: enum 값 고정 테스트(B3 표의 DayPhase/IngredientKind/IngredientGrade 정수값
  전수), `new GameState().schemaVersion == GameState.SaveSchemaVersion`, 프로브 기본값 0.

### I. task-113 상세 구현 순서

1. 코드 변경 전 scaffold `manifest.md`의 placeholder를 이 문서의 Inputs·영향 파일·검증 명령으로
   채운다.
2. 현재 기준선(EditMode 332/PlayMode 6)을 실행해 실제 결과를 구현 노트에 기록한다.
3. `GameState`에 `SaveSchemaVersion` 상수와 `schemaVersion` 필드(클래스 최상단)를 추가한다(B1).
4. `Runtime/Save/SaveSummary.cs`, `Runtime/Save/SaveOps.cs`(C절 파이프라인 + D절 V1~V10 검증 +
   마이그레이션 훅), `Runtime/Save/SaveFileStore.cs`(E절 원자 쓰기)를 만든다(.meta 쌍 포함).
5. `GameEvents.SaveStateChanged`를 추가한다(F2 — 표현 전용, 발행은 GameManager만).
6. `GameManager`를 확장한다(F1/F4): 경로 resolve+override, `SaveGame`/`AutoSave`/`HasSaveFile`/
   `TryPeekSave`/`TryLoadGame`(설치·V11·롤백)/`StartNewRun`, `AdvancePhase` 전환 직후 트리거 2,
   내부 정산 적용 직후 트리거 3, `GenreSelected`/`SNSCampaignExecuted` 구독(트리거 4/5,
   Awake 구독·OnDestroy 해제). Awake의 암묵 `StartNewGame`은 저장하지 않음을 주석으로 못박는다.
7. `SettlementManager.ApplyDailySettlement()`에 트리거 3(신규 적용 시 `AutoSave`)을 배선한다.
8. `ServiceManager.EnsureServiceDay`를 제거한다(F3 — 컴파일로 호출자 0 재확인).
9. `MainMenuController`를 확장한다(G2): continueButton/saveStatusText 참조, `RefreshSaveUi` 분기
   4종, `OnStartClicked → StartNewRun`, `OnContinueClicked → TryLoadGame`. `EditorInit`은 기존
   1-인자 보존 + 3-인자 overload.
10. `NightPanelController`에 저장 표시 라인(G4)과 `SaveStateChanged` 구독을 추가한다(`EditorInit`
    시그니처 불변 — statusText 재사용).
11. `SceneBuilder.BuildMainMenu`에 ContinueButton/SaveStatusText 생성·주입(G1)을 추가하고 2씬을
    재생성한다(멱등성 — 연속 2회 Apply 오브젝트 수·persistent listener 불변, Build Settings 2씬).
12. EditMode 테스트 — 순수/파일: `SaveOpsTests`(파이프라인 성공 경로, V1~V10+V2b 매트릭스 전 행
    사유 정확 일치 — 정규형 위반(필드 누락/명시 null/미지 키/순서 변조)·non-null 총칙 포함,
    마이그레이션 훅 v0/v2/v99 실패, catalog 입력 검증, RT1/RT2/RT5),
    `SaveFileStoreTests`(원자 쓰기·tmp 잔존 무해·읽기 실패·삭제 — `temporaryCachePath` 하위 임시
    경로, TearDown 정리).
13. EditMode 테스트 — 매니저/씬: `GameManagerSaveLoadTests`(SaveGame/TryLoadGame/TryPeekSave
    원상복구·V11 롤백(catalog 제거·주문 identity 변조 5종)에서 state 참조 원복·트리거
    gating(isPlaying false에서 AdvancePhase가 파일을 만들지 않음)·StartNewRun·RT3/RT4), `MainMenuSaveFlowTests`(G2 분기 4종 — 경로 override로 파일
    없음/정상/파산/손상 각각), `NightPanelSceneTests`/`NightPanelSnsFlowTests` 확장(저장 라인 성공/
    실패 문구·색·460px 폭), `SceneBuilderTests` 확장(G1 오브젝트 존재·좌표·멱등),
    `ServiceOpsTests` 등 기존 회귀 전체 무회귀 확인.
14. PlayMode 테스트: `SaveLoadPlayModeTests`(신규 — `[SetUpFixture]`로 전 PlayMode 테스트에 경로
    override 적용): (1) MainMenu 부팅 → `StartNewRun` → 도메인 경로로 장르 확정·Day 1→2 진행 —
    각 트리거에서 파일 갱신 확인 → `StartNewGame`으로 상태 소거 → `TryLoadGame` → RT1/RT3 동일성,
    (2) Night phase 세이브 파일 준비 → MainMenu에서 이어하기 도메인 경로(`TryLoadGame` +
    `LoadShopScene`) → Shop 로드 후 Night 패널 활성·HUD `Day n — 밤`·persistent 매니저 생존.
    기존 6개 무회귀.
15. `InitialDataBuilder.Apply → SceneBuilder.Apply → compile → EditMode → PlayMode` 순 배치 검증
    (`-runTests`는 `-quit` 없이 — task-112 노트의 함정), `git status --short game` 오염 없음 확인
    (세이브 테스트 파일이 리포/게임 폴더에 남지 않아야 한다).
16. 수동 Play smoke(오너/Codex 게이트): 새 게임 → Day 2 Night까지 진행 → 에디터 Play 종료 → 재Play
    → 이어하기 요약·재개 화면·같은 예상 수요 확인, 파산 런 후 이어하기 잠금 확인, `save.json`을
    손으로 깨뜨려(빈 파일/버전 99/필드 삭제) 사유 표시 확인. 구현 노트·artifacts summary·
    `python3 runtime/generate-status.py` 재생성으로 기록을 마감한다.

## 실행 계획 (Execution Plan)

- implement_model: claude-fable-5
- implement_effort: high
- routing_reason: 프로젝트 SSOT 로드맵이 task-113을 fable-5/high로 고정 — 신규 씬·매니저·수학 없이
  기존 GameState/JsonUtility 왕복 가드 위에 파이프라인·검증·파일 I/O·UI 진입점을 얹는 M3 첫 작업이다.

| unit | 파일 범위 | depends_on | group |
|------|-----------|------------|-------|
| U1-save-domain | `GameState`, 신규 `Runtime/Save/{SaveOps,SaveSummary,SaveFileStore}` | 없음 | G1 |
| U2-manager-wiring | `GameManager`, `SettlementManager`, `ServiceManager`(legacy 제거), `GameEvents` | U1-save-domain | G2 |
| U3-ui-controllers | `MainMenuController`, `NightPanelController` | U2-manager-wiring | G3 |
| U4-scene-wiring | `SceneBuilder`(MainMenu 이어하기 블록 최종 wiring 단독 소유), 씬 2종 재생성 | U3-ui-controllers | G4 |
| U5-tests | SaveOps/파일/매니저/MainMenu/Night/SceneBuilder/PlayMode 테스트 + 기존 회귀 | U1-save-domain, U2-manager-wiring, U4-scene-wiring | G5 |
| U6-validation-records | builder·compile·EditMode·PlayMode·validator·수동 smoke 준비·구현 노트/summary/status | U5-tests | G6 |

## 파일/모듈 영향 (Affected Files/Modules)

| 파일/모듈 | 변경 유형 | 설명 |
|-----------|-----------|------|
| `game/Assets/Scripts/Runtime/DayCycle/GameState.cs` | modify | `SaveSchemaVersion` 상수 + `schemaVersion` 필드 (클래스 최상단, JsonUtility 호환) |
| `game/Assets/Scripts/Runtime/Save/SaveOps.cs` | create | 직렬화/역직렬화 파이프라인·버전 프로브·마이그레이션 훅·검증 매트릭스 V1~V10 + V2b 정규형 (순수, JsonUtility 예외 허용) |
| `game/Assets/Scripts/Runtime/Save/SaveSummary.cs` | create | MainMenu 이어하기 표시 전용 DTO |
| `game/Assets/Scripts/Runtime/Save/SaveFileStore.cs` | create | 원자적 파일 I/O 정적 헬퍼 (persistentDataPath/save.json, tmp→Replace, UTF-8 무BOM) |
| `game/Assets/Scripts/Runtime/DayCycle/GameEvents.cs` | modify | `SaveStateChanged` 표현 이벤트 추가 (발행은 GameManager 만) |
| `game/Assets/Scripts/Runtime/Managers/GameManager.cs` | modify | Save/Load/Peek/StartNewRun API·경로 override·자동 저장 트리거 2/3/4/5·V11 설치 후 plan·주문 identity 검증+롤백 |
| `game/Assets/Scripts/Runtime/Settlement/SettlementManager.cs` | modify | 정산 신규 적용 직후 AutoSave (트리거 3) |
| `game/Assets/Scripts/Runtime/Service/ServiceManager.cs` | modify | `EnsureServiceDay` legacy 제거 (호출자 0 — task-111 오픈 이슈 이행) |
| `game/Assets/Scripts/Runtime/UI/MainMenuController.cs` | modify | 이어하기 버튼·저장 상태 라인·G2 분기 4종·StartNewRun 경로 (EditorInit overload 보존) |
| `game/Assets/Scripts/Runtime/UI/NightPanelController.cs` | modify | statusText 자동 저장 라인 + SaveStateChanged 구독 (좌표·EditorInit 불변) |
| `game/Assets/Scripts/Editor/SceneBuilder.cs` | modify | MainMenu ContinueButton/SaveStatusText 생성·주입·focus 네비게이션, 2씬 재생성 멱등 유지 |
| `game/Assets/Scenes/MainMenu.unity` | modify | SceneBuilder 산출 이어하기 블록 |
| `game/Assets/Scenes/Shop.unity` | modify | SceneBuilder 재실행 산출 (기능 변경 없음) |
| `game/Assets/Scripts/Runtime/AssemblyInfo.cs` | none | 변경 없음 — 기존 IVT(EditMode/PlayMode/Editor)로 충분함을 확인만 |
| `game/Assets/Tests/EditMode/SaveOpsTests.cs` | create | 파이프라인·V1~V10+V2b 매트릭스 전 행(정규형·non-null 총칙 포함)·마이그레이션 훅·RT1/RT2/RT5·catalog 입력 검증 |
| `game/Assets/Tests/EditMode/SaveFileStoreTests.cs` | create | 원자 쓰기·tmp 무해성·읽기/삭제 실패 사유 (temporaryCachePath 격리) |
| `game/Assets/Tests/EditMode/GameManagerSaveLoadTests.cs` | create | Save/Load/Peek 원상복구·V11 롤백(catalog·주문 identity 변조)·트리거 gating·StartNewRun·RT3/RT4 |
| `game/Assets/Tests/EditMode/MainMenuSaveFlowTests.cs` | create | G2 분기 4종 (없음/정상/파산/손상) — 경로 override 상태 fixture |
| `game/Assets/Tests/EditMode/NightPanelSceneTests.cs` | modify | 저장 라인 구조·좌표 불변 회귀 |
| `game/Assets/Tests/EditMode/NightPanelSnsFlowTests.cs` | modify | 저장 성공/실패 문구·색·폭 460px 상태 테스트 |
| `game/Assets/Tests/EditMode/SceneBuilderTests.cs` | modify | MainMenu 신규 오브젝트 존재·좌표·재실행 멱등 |
| `game/Assets/Tests/PlayMode/SaveLoadPlayModeTests.cs` | create | 실파일 왕복 재개 + 이어하기 → Night 재개 통합 (SetUpFixture 경로 격리) |
| 관련 `game/**/*.meta` | create/modify | 신규 Save 폴더·스크립트·테스트의 Unity 메타 쌍 |

## 테스트 기준 (Test Criteria)

- [ ] `python -B runtime/validator/cli.py kb/tasks/task-113/design.md`가 종료 코드 0으로 통과한다.
- [ ] 구현 전 기준선(EditMode 332/PlayMode 6)을 실행하고 실제 결과를 구현 노트에 기록한다.
- [ ] `new GameState().schemaVersion == GameState.SaveSchemaVersion == 1`이고, `SaveVersionProbe`
  기본값은 0이며, `schemaVersion` 필드가 없는 JSON의 로드는 `지원하지 않는 저장 버전입니다 (v0).`
  으로 명시 실패한다.
- [ ] B3 enum 핀 테스트: `DayPhase`(0~3)·`IngredientKind`(0~8)·`IngredientGrade`(0~1)의 정수값이
  표와 정확히 일치한다(스키마 파괴 변경 조기 검출).
- [ ] RT1: 대표 상태 6종(H절 a~f)의 `TrySerialize→TryDeserialize` 왕복이 B3 표 전 필드
  field-by-field 동등하다(빈 List는 null 아닌 빈 List로 왕복).
- [ ] RT2: 같은 상태의 직렬화 2회가 문자열 동일하고, `TrySerialize(TryDeserialize(json))`이 원본
  json과 바이트 동일하다. 같은 계약이 V2b 정규형 검사로 로드 파이프라인에서도 상시 강제됨을
  코드 경로로 확인한다.
- [ ] RT3: SNS 집행 + 이벤트 활성 상태의 왕복 전후 `ServiceManager.TryBuildDayPlan`이
  field-by-field 동일하고 `BuildOrders` 결과의 recipe/customer/partySize/snsInflow/eventInflow가
  완전 일치한다(기존 `SNSJsonRoundTripTests`/`EventJsonRoundTripTests`는 무변경 통과).
- [ ] RT4: 왕복 전후 `TryBuildEventForecast`와 `TryBuildNextDayActiveEvents` 결과가 동일하다
  (C4 스케줄 known day 벡터로 고정).
- [ ] V1~V10(+V2b) 검증 매트릭스의 **각 행**이 D절 사유 문자열과 정확히 일치하며 실패하고, 실패
  경로에서 현재 `GameState`가 완전히 불변이다: 빈/깨진 JSON, v0/v2/v99, phase 범위 밖(예: 7), day 0,
  cash 음수, daysCompleted 불일치, 파산 정합 위반, 미선택 장르가 day 2에 저장됨, 미지
  genre/recipe/customer/campaign/event ID, 인벤토리 enum 범위 밖·중복 (kind,grade)·음수 수량,
  주문 prefix/suffix 위반·served∧missed 동시 참·통계 불일치·인덱스 범위 밖, Night인데 정산 미적용,
  surcharge > spend, 같은 밤 SNS 2건, executedOnDay > day, bonusOrderCount 3, Night cash ≠
  settlementCashAfter − 오늘 SNS 비용, activeEvents 손상(미지 id/영구·시한 불일치/필수 kind 누락
  catalog), **정규형 위반 4종 — B3 필드 누락(키 삭제, 예: `activeEvents` 키 제거)·명시
  null(`"ingredientStocks": null`)·미지/잉여 키 추가·필드 순서 변조(전부 V2b 사유)** — 최소 이 목록
  전부.
- [ ] 저장 방향에도 같은 검증이 적용된다: 손상 상태(예: 미지 eventId 주입)의 `SaveGame`이
  `손상된 상태는 저장하지 않습니다: …`로 실패하고 기존 파일이 변경되지 않는다.
- [ ] `TryValidateState`가 저장·peek·로드 세 경로에서 동일 함수로 호출된다(경로별 검증 격차 없음
  — 코드 경로 확인 + V-행 샘플을 세 경로 각각에서 재현).
- [ ] non-null 필수 완전성(세 경로 — Codex 설계 리뷰 반영): B3의 각 List 필드 4종과 문자열 ID
  필드를 null 로 만든 손상에 대해 — (a) in-memory 상태 주입 시 `SaveGame`(저장)과
  `TryValidateState` 직접 호출(peek/로드가 쓰는 같은 함수)이 **같은 non-null 사유**로 거부하고,
  (b) 같은 손상을 파일 텍스트로 만든 JSON(명시 null·키 삭제)은 peek·로드 두 경로에서 **서로 같은
  사유**(파이프라인 순서가 결정론적으로 고정 — V2b 정규형 또는 V3 총칙)로 실패한다. JsonUtility 가
  null/누락을 기본값으로 채우는 경우에도 통과하는 경로가 존재하지 않는다.
- [ ] V11 주문 identity(Codex 설계 리뷰 반영): 정상 Service-phase 세이브 JSON에서 주문 개수는
  유지한 채 identity 필드를 각각 변조한 5종(다른 recipeId·다른 customerId·partySize 변경·snsInflow
  토글·eventInflow 토글)이 전부 `저장된 주문이 수요 계획과 일치하지 않습니다` 계열 사유로 로드에
  명시 실패하고 이전 상태로 롤백된다. `served`/`missed`/`serviceCurrentOrderIndex` 진행 상태만
  다른(V7 정합 유지) 세이브는 재생성 비교 대상이 아니므로 통과한다(저장값 존중). Settlement/Night
  phase 세이브(오늘 주문 보유)도 같은 identity 검증을 통과해야 한다.
- [ ] `SaveFileStore.TryWriteAtomic`: 정상 쓰기 후 tmp가 남지 않고, 기존 파일이 있는 상태에서
  덮어쓰기가 성공하며, tmp만 존재하는(중간 크래시 모사) 상태에서 로드가 tmp를 읽지 않고 기존
  save.json을 읽고, 다음 쓰기가 tmp를 무해하게 대체한다. UTF-8 무BOM으로 기록된다.
- [ ] 파일 I/O를 수행하는 모든 테스트가 `Application.temporaryCachePath` 하위 override 경로만
  사용하고 TearDown에서 정리하며, 테스트 후 `game/` 트리와 실사용 persistentDataPath에 산출물이
  남지 않는다.
- [ ] 자동 저장 gating: `Application.isPlaying == false`(EditMode)에서 `AdvancePhase`/정산 적용/
  이벤트 구독 경로가 파일을 생성하지 않고, 명시적 `SaveGame()`은 EditMode에서도 동작한다.
- [ ] F2 트리거 5종이 각각 파일을 갱신한다(PlayMode — 트리거별로 파일 mtime/내용 변화 확인):
  StartNewRun 즉시 저장(장르 미선택 day1 Market 상태가 V5를 통과), phase 전환, 정산 적용(파산
  포함 — 파산 상태 저장 후 파일에 `isBankrupt: true`), 장르 확정, SNS 집행. `GameManager.Awake`의
  암묵 `StartNewGame`은 파일을 쓰지 않는다.
- [ ] `TryPeekSave`는 성공/실패 모두에서 `GameManager.State` 참조와 내용이 호출 전과 동일하다
  (dry-run 원상복구 — 참조 동일성 확인).
- [ ] `TryLoadGame` V11 롤백: 검증은 통과하지만 plan 재구성이 불가능한 상태(예: 저장 후 catalog에서
  장르 def 제거를 모사한 fixture)와 주문 identity 불일치 상태 모두에서 로드가 명시 실패하고 이전
  state/machine 참조로 원복된다.
- [ ] 로드 성공 후 재개 매트릭스(G3): Market(미선택 modal/확정 badge)·Service(남은 주문 인덱스)·
  Settlement(멱등 재표시)·Night(집행 완료 outline·예고 라인)이 저장 전과 동일하게 표시된다
  (EditMode 상태 fixture — phase별 최소 1건).
- [ ] 파산 세이브는 `TryPeekSave` 성공 + `summary.IsBankrupt == true`이며 MainMenu 이어하기가
  잠기고 G2 파산 카피가 표시된다. 정상/없음/손상 분기도 G2 표와 카피·색이 정확히 일치한다.
- [ ] `EnsureServiceDay` 제거 후 컴파일이 통과한다(호출자 0 재확인). neutral
  `ServiceOps.BuildOrders(recipes, customers, day)` 순수 오버로드와 기존 테스트는 무변경 통과한다.
- [ ] Night statusText가 저장 성공 시 `자동 저장됨 · Day {n} {phase}`, 실패 시 Warning Plum
  `자동 저장 실패: {사유}`를 표시하고, `SaveStateChanged` 발행으로 갱신되며, worst-case 문자열의
  Galmuri TMP 11pt `GetPreferredValues` 폭이 460px 이하다.
- [ ] `SceneBuilder` 산출 MainMenu가 G1 좌표 표와 정확히 일치하고(기존 2종 불변 + 신규 2종),
  연속 2회 `Apply`가 오브젝트 수·persistent listener 기준 멱등이며 Build Settings 씬은 2개뿐이다.
- [ ] PlayMode: (1) 실파일 경유 저장→상태 소거→로드 후 RT1/RT3 동일성, (2) Night 세이브 이어하기로
  Shop 진입 시 Night 패널 활성·HUD `Day {n} — 밤`·persistent 매니저(장르 4/이벤트 4/SNS 3 catalog)
  생존. `[SetUpFixture]` 경로 격리로 기존 PlayMode 6개가 실사용 세이브를 건드리지 않고 무회귀
  통과한다.
- [ ] Unity 배치 compile 종료 코드 0·`error CS` 없음, EditMode 전체 종료 코드 0(기준선 332 + 신규,
  무실패), PlayMode 전체 종료 코드 0(기존 6 + 신규 — `-quit` 없이 실행).
- [ ] `git status --short game`에 `Library/Temp/Obj/Logs/UserSettings/Build*`와 세이브 산출물이
  없고 신규 파일 전부에 `.meta`가 존재한다.
- [ ] 640×360 원본 캡처를 Codex가 검토해 MainMenu 이어하기 블록·Night 저장 라인이 겹침·이탈 없이
  좌표·폰트·카피와 일치함을 승인한다 (Claude self-approve 금지).
- [ ] 수동 Play smoke(오너/Codex): I16 시나리오 — 종료 후 이어하기 재개(같은 예상 수요·주문),
  파산 후 이어하기 잠금, 손상 파일 3종(빈 파일/버전 99/필드 삭제)의 명시적 사유 표시를 확인한다.

## 오픈 이슈 (Open Issues)

- **GameState 평면 유지 결정 (B2)**: G4 중첩 구조화를 기각하고 v1 스키마를 평면으로 확정했다.
  Codex 교차검토에서 중첩이 낫다고 판단되면 **v1 확정 전인 지금이 마지막 전환 기회**다(확정 후에는
  v2 마이그레이션 비용 발생). 1.0 커리어/평판 설계 시 v2 재편을 기본 경로로 예약한다.
- **자동 저장 트리거 정책 (F2)**: 이정표 5종 자동 저장 + 수동 버튼 없음 + `OnApplicationQuit` 저장
  없음은 설계 확정값이지만 체감(손실 허용량 1~2분)은 오너 수동 smoke에서 검증해야 한다. 부족하면
  트리거 추가(구매/서빙 단위)가 아니라 quit 시점 저장 도입을 task-115에서 재검토한다.
- **파산 세이브 이어하기 잠금 (G2)**: "정직한 기록 + 새 게임 유도"로 결정했다. task-115 엔딩(결과
  카드·재도전 제안)이 들어오면 파산 세이브를 결과 카드로 라우팅하는 정책으로 대체될 수 있다 — 그때
  이 분기와 카피를 함께 재설계한다.
- **새 게임의 기존 세이브 즉시 대체 (G2)**: `게임 시작` 클릭 = `StartNewRun` 즉시 저장으로 기존
  런이 대체되며 확인 모달이 없다. 데모 단순성 우선 결정이지만 UX 판단은 Codex 몫 — 모달 추가 시
  MainMenu 레이아웃 연쇄 변경이 필요하므로 리뷰에서 확정한다.
- **`SaveManager` singleton 보류**: 브리프 매니저 8종 목록에 Save가 있으나 task-111 SNS/task-112
  Event 전례에 따라 `GameManager` 얇은 확장 + 순수 `SaveOps`로 구현한다. 데모 완료 후 SSOT의
  매니저 목록과 실제 구조(6 singleton + 정적 허브)의 정합을 concept-update로 기록해야 한다.
- **JsonUtility의 SaveOps 허용 예외**: "순수 계층은 Unity 타입 미참조" 규약에 대해 `SaveOps`만
  `UnityEngine.JsonUtility` 예외를 명문화했다(제약 절). Codex가 부적절하다고 판단하면 직렬화
  호출부만 `GameManager`로 올리는 국소 재배치로 대응 가능하다(검증 매트릭스는 불변).
- **로드 시 주문 identity 비교 (설계에서 확정됨 — Codex 설계 리뷰 반영)**: 초안의 V11 OrderCount
  미러를 주문 identity 5필드(recipeId/customerId/partySize/snsInflow/eventInflow) 비교로 강화했다.
  같은 개수의 다른 주문 변조가 V7을 통과하는 격차를 닫기 위한 것으로, 비교는 manager V11에서
  `TryBuildDayPlan` + `ServiceOps.BuildOrders` 프로덕션 경로 재사용으로 수행하고(SaveOps 합성 복제
  금지), `served`/`missed`/진행 인덱스는 저장값을 존중한다. 이 이슈는 닫혔다.
- **persistentDataPath 경로 변동**: `companyName: DefaultCompany`가 task-115 빌드 정리에서 바뀌면
  세이브 경로도 바뀐다(기존 세이브 이전 없음 — 데모 단계 허용). 빌드 task에서 companyName 확정과
  함께 기록한다.
- **enum 직렬화 의존 (B3)**: `ingredientStocks`(task-105)와 `currentPhase`는 enum 정수 직렬화를
  이미 사용한다 — 신규 필드의 enum 추가 금지 규약(task-110)과 별개로, 기존 enum은 값 고정 계약 +
  핀 테스트로 방어한다. 1.0 taxonomy(`GenreKind` 재편 등) 이행 시 v2 마이그레이션이 필수다.
- **run 시드 부재**: task-112 오픈 이슈의 재플레이 변주용 run 시드 필드는 이번 v1 스키마에 넣지
  않았다. B1 v1 정규형 계약상 직렬화 필드 추가는 스키마 파괴 변경이므로, task-115에서 도입하려면
  schemaVersion v2 증가와 마이그레이션이 필요하다(Codex 리뷰 반영 — 초안의 "비파괴 추가 가능"
  문구를 정정).
- **Night statusText 2행 밀도**: 저장 라인 추가로 Night 하단 정보가 늘었다. 640×360 시각 승인에서
  겹침·가독을 확인하고, 넘치면 저장 라인을 이벤트 예고처럼 별도 텍스트로 분리하는 대안을 Codex가
  결정한다(좌표 연쇄 변경이라 이번 설계에서는 statusText 재사용을 기본값으로 둔다).
