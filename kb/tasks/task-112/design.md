# 설계 문서 — task-112: 이벤트/장애물 시스템

> Status: ready
> Inputs: `kb/concepts/project-brief.md`(SSOT — 이벤트 4종, 매니저 규약), `kb/concepts/demo-scope.md`(하드캡 — 이벤트 4종, 씬 2개), `kb/concepts/development-priority.md`(위기는 예고되고 실패는 회복 가능, 수직 슬라이스 Day 3 = 예고된 재료값 폭등), `kb/tasks/task-110/design.md`(D10 — `ActiveEventState(id, remainingDays)` + 순수 `EventOps`, D6/G2/G3 — DayModifier 분리 훅·의존 방향, C2 — 하루 상태머신), `kb/tasks/task-111/design.md` + `implementation-notes.md`(DayModifier·밀리 정수·FNV-1a·base-prefix 불변·명시적 실패·neutral overload 패턴, 기준선 EditMode 236/PlayMode 4), `kb/tasks/task-111/reviews/001.md`(교훈 — 모든 재구성 경로의 def 무결성 검증 + JsonUtility 왕복 결정론 테스트), `kb/tasks/task-103/implementation-notes.md`(`durationDays=0`=영구 규약), `game/Assets/Data/Definitions/Events/*.asset` 4종과 `GameEventDef` 필드, 현재 `game/` 코드 기준선(`GameState`/`GameEvents`/`DayPhaseMachine`/`EconomyOps`/`EconomyManager`/`ServiceOps`/`ServiceManager`/`SettlementOps`/`SettlementManager`/`GameManager`/`GenreSelectionOps`/`DayModifier`/`SNSCampaignOps`/`SceneBuilder`/`InitialDataBuilder`/Night·Market·Service·Settlement·HUD UI)
> Outputs: task-112 구현 계약 — 이벤트 4종의 결정론 스케줄(FNV-1a, 시드 47/문턱 450)·전날 예고(Night)·당일/기간 적용, `ActiveEventState` 저장 + 순수 `EventOps`(수명·스케줄·효과 합성·예고), `EventDayEffects` 축 분리(재료값/운영비/파티·주문), SNS와 동일 `DayModifier` 합성으로 단체 손님 보너스 주문, 구매 할증·정산 원인 라인·화면 인과 사슬, 이벤트 시드 재확정(위생 8,000원/단체 4인), 100-day 회복 가능 밸런스 가드, EditMode/PlayMode 테스트 계약
> Next step: Claude가 scaffold `manifest.md`의 placeholder를 이 문서의 Inputs·영향 파일로 채운 뒤 U1~U8 순서로 구현하고, 배치 compile·EditMode·PlayMode 게이트를 통과시킨다. Codex 코드 리뷰와 오너 640×360 시각 승인·수동 Play smoke·시드 재확정 승인 게이트 후 `task-113`(저장/불러오기)으로 진행하며, 장르+SNS+이벤트 전체 3일 수직 슬라이스 통합 게이트를 task-112 완료 직후 수행한다.

## 목표 (Objective)

이벤트/장애물 4종(재료값 폭등·위생 점검·임대료 인상·단체 손님)을 **"예고된 위기, 회복 가능한 실패"**로
구현한다. 모든 이벤트는 **전날 Night에 예고**되고 다음 날 Market부터 적용되며, 적용 중인 날의 구매
가격·주문 구성·정산 내역에 원인이 수치로 표시되어 "어제 예고를 보고 오늘 대응했다"는 인과가 화면에서
증명된다. 어떤 단일 이벤트도, 어떤 이벤트 조합도 전량 서빙 기준 하루 순이익을 음수로 만들지 않는다
(단일 이벤트 파산 강제 금지 — G절 가드가 증명).

기술적으로는 task-110 D10 계약을 실체화한다: 이벤트 상태는 `GameState.activeEvents`
(`ActiveEventState(id, remainingDays)` 리스트, JsonUtility 호환)에 저장되고, 발생 스케줄·수명 전이·효과
합성은 순수 C# `EventOps`가 담당한다. 발생 스케줄은 task-110/111과 동일한 32-bit FNV-1a로 완전
결정론이며(같은 day → 같은 이벤트, 플레이어 선택과 무관), 단체 손님 효과는 SNS와 **같은 `DayModifier`**
로 익일 수요 계획에 합성되고, 재료값/운영비 효과는 `EventDayEffects` DTO의 정수 밀리 배수로
구매·정산 경로에 전달된다. 기존 neutral 경로(장르+SNS만 적용된 plan·구매·정산)는 회귀 없이 보존한다.

### 역할과 결정권

| 영역 | 결정권자 | 수행자 | 게이트 |
|------|----------|--------|--------|
| 이벤트 수학·스케줄 시드·재시드 값 | 이 설계(Claude 초안) → Codex 교차검토, 오너 최종 승인 | Claude 구현 | design validator + 밸런스 가드 테스트 |
| Night/Market/Settlement UI 좌표·카피·색 | **Codex**(이 문서 F절은 제안 초안) | Claude가 SceneBuilder/UI로 구현 | Codex 640×360 화면 리뷰 |
| 코드 구조·테스트·배치 자동화 | 설계 계약 내 Claude | Claude | 컴파일·EditMode·PlayMode·멱등성 게이트 |
| 설계와 다른 화면·수치 판단 | Codex 재설계 요청 | Claude가 임의 확정하지 않음 | 새 design/review 기록 |

이 문서는 Claude가 작성한 설계 초안이며 Codex 설계 교차검토를 전제로 한다. 구현 중 문구·레이아웃·수치가
불명확하면 임의 확장하지 않고 리뷰 대상으로 남긴다.

## 범위 (Scope)

### 포함 — task-112 구현 범위

- 이벤트 4종의 **결정론 발생 스케줄**: day별 FNV-1a 이중 roll(발생 여부 + 가중 선택). Day 1은 이벤트
  없음(학습 보호), 같은 이벤트는 활성 중 재발생 금지, 임대료 인상(영구)은 이 규칙의 자연 결과로 run당
  정확히 1회만 발생한다.
- **전날 예고**: Night 패널에 다음 날 신규 이벤트·지속 이벤트를 예고한다(예고 없는 적용 금지 —
  예고 함수와 적용 함수가 같은 순수 계산을 공유해 항상 일치).
- **효과 적용 4종**:
  1. 재료값 폭등 — 활성일 동안 재료 구매 단가 ×1.35(밀리 정수 합성), 2일 지속.
  2. 위생 점검 — 당일 정산 운영비에 대응 비용 8,000원 가산, 1일.
  3. 임대료 인상 — 일일 운영비 ×1.15(12,000 → 13,800원), `durationDays=0`=영구(task-103 규약).
  4. 단체 손님 — 당일 수요 계획에 파티 4인 고정 보너스 주문 +1건(SNS와 같은 `DayModifier` 합성), 1일.
- `GameState.activeEvents`(`List<ActiveEventState>`) 저장 + 하루 경계(Night→Market)에서의 결정론
  수명 전이(감소·만료·영구 유지)와 신규 활성화. 원자적 적용(실패 시 상태 완전 불변).
- 순수 `EventOps`: 카탈로그/상태 검증, 스케줄, 수명 전이, `EventDayEffects` 합성, `DayModifier` 합성,
  예고 DTO, 정산 원인 라인 문자열 — 전부 명시적 실패, neutral fallback 금지.
- `DayModifier`/`GenreDemandPlan`/`ServiceOps` 확장: 단체 보너스 주문 세그먼트(base → SNS → 단체 순서
  append), `eventInflow` 태그, base-prefix·SNS-prefix 불변 유지.
- `EconomyOps`/`SettlementOps` 이벤트 overload(구매 단가 밀리 배수, 운영비 밀리 배수+가산) + 당일 구매
  할증(`marketEventSurchargeToday`)·단체 주문 통계(`serviceEvent*Today`) 추적.
- 화면 인과 사슬: Night 예고 라인 → Market 오늘 이벤트 표시·인상된 구매가 → HUD badge 단체 표기 →
  Service `단체 손님` 태그 → Settlement `이벤트:` 원인 라인(할증/비용/인상/단체 수치).
- 이벤트 시드 재확정(GUID 보존 upsert, **값 2건 + 문구 2건만**): 위생 대응 비용 80,000 → **8,000원**,
  단체 flatEffect 6 → **4**(파티 크기 규약 전환), "불시"/"예고 없이" 문구를 예고 규약에 맞게 교체.
- EditMode 결정론·게이트·밸런스·씬 회귀 테스트, JsonUtility 왕복 테스트, PlayMode 통합 테스트.

### 제외 — task-112에서 구현하지 않음

- 저장/불러오기(`task-113` — 단, JsonUtility 왕복 결정론은 이번 task가 선행 보증), 아트 마감(`task-114`),
  밸런싱·엔딩·Windows 빌드(`task-115`).
- **5번째 이벤트 금지**(demo-scope 하드캡 4종). 이벤트 종류·효과 축 추가, 이벤트 데이터의 카탈로그화
  확장(발생 조건식·연출 필드 등)도 금지.
- 요리대회·장슐랭·푸드트럭 규칙(task-110 GDD post-demo 설계 — 이번 구현과 무관).
- 신규 씬, 신규 singleton manager(브리프 매니저 8종의 `EventManager`는 SNS 전례에 따라 기존 매니저
  얇은 확장으로 대체 — 오픈 이슈), 외부 DI/ECS/이벤트버스/tween 라이브러리.
- B급 재노출·품질 만족 수학(task-110 D3 유지 — C급 고정), 고객 인내 타이머.
- 위생 점검의 "판정/등급 검사" 변형(청결도 스탯·B급 연동 등) — 데모는 고정 대응 비용만.
  run 시드 도입(스케줄 변주), 이벤트 회피/보험 같은 대응 행동 — task-115 밸런싱에서 재검토.
- Morning Brief 별도 화면 신설(GDD C2의 개념 단계) — 현 데모에서 아침 역할은 Market phase가 겸하며,
  예고는 Night·적용 표시는 Market/HUD가 담당한다.
- SNS 수학·비용·감쇠 변경(task-111 확정 seed 불변), `GameEvents`(C# 이벤트 허브) 신규 이벤트 추가 없음
  — 활성화는 DayPhaseChanged 발행 전에 완료되므로 별도 알림이 불필요하다(E1).

## 제약 (Constraints)

- `kb/concepts/project-brief.md`가 SSOT, `kb/concepts/demo-scope.md`가 하드캡이다: 이벤트 정확히 4종,
  씬은 `MainMenu.unity`+`Shop.unity` 2개, 아트는 CC0/OFL placeholder만.
- Unity 6 `6000.3.8f1`, 2D URP, 640×360 Pixel Perfect, PPU 32, TMP Dynamic Galmuri 유지. UI·무대는
  `SceneBuilder.Apply` 코드 저작이며 수동 씬 편집을 정본으로 삼지 않는다.
- 정적 데이터는 ScriptableObject(`GameEventDef` — 필드 변경 없음), 런타임 상태는 순수 C# `GameState`
  (문자열 ID + List, Dictionary·SO 직접 참조 저장 금지, `JsonUtility` 호환). `ActiveEventState`는
  public 필드의 `[Serializable]` 클래스다.
- 순수 규칙은 `*Ops`에, Unity lifecycle/SO 참조는 manager/controller에 둔다. `EventOps`는 Unity 타입을
  참조하지 않으며 SO 투영 DTO(`GameEventDefInput`)만 입력받는다(순수 enum `GameEventKind`는 기존
  `AgeBand`/`GenderTarget` 전례대로 허용). `EventOps`는 순수 계층인 `ClientIsKing.Genre`
  (`GenreSelectionOps.RoundHalfUp`/`MulMilliHalfUp`/`Fnv1a`, `DayModifier`)만 참조하고
  `ClientIsKing.Social`은 참조하지 않는다.
- **결정론**: 같은 입력이면 같은 결과. `System.HashCode`·런타임 가변 문자열 hash·프레임 난수·`Math.Pow`
  금지. 스케줄 roll은 task-110의 UTF-8 32-bit FNV-1a(offset `2166136261`, prime `16777619`, unchecked)를
  그대로 재사용하고, SO float(`baseWeight`/`percentEffect`)은 투영 경계에서 **한 번만**
  `RoundHalfUp(x × 1000)`으로 밀리 정수화한 뒤 전 구간 정수 연산한다. 반올림은 `RoundHalfUp`/
  `MulMilliHalfUp(a,b) = (a×b+500)/1000`만 사용한다(`MulMilliHalfUp(x, 1000) == x` 항등 — neutral 경로
  비트 동일 보장).
- 잘못된 정의·상태는 **명시적 실패**다(neutral fallback으로 조용히 진행 금지). 실패 경로는 `GameState`를
  완전히 불변으로 유지한다. task-111 리뷰 001 교훈에 따라 **모든** def 참조 경로(스케줄·수명 전이·효과
  합성·예고)가 같은 무결성 검증을 통과해야 하며, 저장 후 재개·catalog 손상 시 조용한 진행 대신 명시적
  사유로 phase 진행이 차단된다.
- 기존 공개 API는 neutral overload로 하위호환하고 task-111 기준선(EditMode 236/PlayMode 4)을 회귀 없이
  보존한다. 단, GameManager 경로로 day 3 이상을 진행하는 기존 테스트는 이벤트 적용이 **의도된 동작
  변화**이므로 C4 스케줄 표 기준으로 기대값을 갱신한다(조용한 우회 금지 — H2에서 식별·기록).
- `ServiceOps`/`GenreSelectionOps`/`EconomyOps`/`SettlementOps`는 이벤트 매니저·SO를 직접 찾지 않는다
  (task-110 G2). 이벤트 효과는 `DayModifier`(수요 축)와 정수 파라미터(경제 축)로 고정되어 전달되며,
  저장 후 재개 시 `activeEvents`에서 동일 효과가 재생성된다.
- 통화는 기존 원화 정수 계약을 유지한다. UI는 색만으로 상태를 전달하지 않는다(`[예고]`/`[지속]` 문구·
  아이콘성 prefix 병용). 하루 시작/종료 60초 예산(task-110 B5)을 Night 예고 라인 추가 후에도 지킨다.
- `game/ProjectSettings/`·`game/Packages/`·`.meta`는 버전 관리 대상이며 에셋과 `.meta`는 쌍으로 처리한다.
  `InitialDataBuilder`는 GUID 보존 upsert만 사용한다(삭제/재생성 금지). Build Settings 씬은 2개 유지.
- 네임스페이스는 `ClientIsKing.Events`(폴더 `Runtime/Events/`)를 사용한다. `UnityEngine.Events`와 같은
  파일에서 함께 쓰일 때는 using alias로 충돌을 피한다(순수 계층은 Unity 참조가 없어 무관).

## 구현 단계 (Implementation Steps)

### A. 플레이어 경험 — 예고→적용 인과 루프

1. **Night(Day N)**: 정산 요약 아래 예고 라인이 보인다 — `[예고] 내일 재료값 폭등 — 재료 구매가 +35%
   (2일)`(위기 = Warning Plum) 또는 `[예고] 내일 단체 손님 — 단체 손님 1팀(4인) 방문`(기회 = Brass
   Amber), 없으면 `내일 예고된 이벤트 없음`. SNS 잔액 경고도 내일의 실제 운영비(임대료 인상 반영)로
   계산된다.
2. **Market(Day N+1)**: 확정 상세에 `오늘 이벤트: 재료값 폭등 +35%`가 표시되고 재료 구매가가 실제로
   올라 있다(예상가 = 실제 거래가, 같은 helper). 단체 손님 날은 HUD badge와 상세에 `+1건(단체)`가 보여
   재료를 더 사야 할 이유를 장보기 전에 알 수 있다.
3. **Service(Day N+1)**: 단체 주문(맨 뒤 1건)에 `단체 손님` 태그와 파티 4인이 표시된다. 재고를 준비하지
   못했으면 포기 = 큰 기회 손실이 정직하게 기록된다.
4. **Settlement(Day N+1)**: `이벤트:` 원인 라인이 수치로 분해된다 — 1~2종 활성이면 전체 포맷
   `이벤트: 재료값 폭등 -3,486원`, 3종 이상 동시 활성이면 축약 포맷 `이벤트: 단체 1/1 +18,900 ·
   위생 -8,000 · 폭등 -3,486 · 임대 -1,800`(활성 이벤트만, ID ordinal 순 — F5 결정론 분기).
5. **지속·회복**: 폭등 2일차 Night에는 `[지속]` 라인으로 남은 기간이 보이고, 임대료 인상은 이후 매일
   정산 라인에 남는다(영구의 무게). 어떤 조합의 이벤트 날에도 전량 서빙하면 흑자가 유지되어(G절 가드)
   실패는 플레이어의 준비 부족에서만 온다.

### B. 데이터·상태 계약

#### B1. GameEventDef 시드 재확정 (값 2건 + 문구 2건만 변경)

기존 `game/Assets/Data/Definitions/Events/*.asset`은 task-103 시점의 경제 미검증 placeholder다.
위생 80,000원은 시작 자금 30,000원·일 순이익 25,000~34,000원(task-110/111 실측) 대비 즉시 파산 수준으로
"단일 이벤트 파산 강제 금지"를 위반하고, 단체 `+6명`은 주문 시스템 이전의 해석이다. G절 가드를 통과하는
아래 값으로 재시드한다(`InitialDataBuilder.BuildGameEvents` GUID 보존 upsert). `GameEventDef` 스크립트
필드는 변경하지 않는다.

| id | 표시명 | kind | baseWeight | durationDays | percentEffect | flatEffect (기존→확정) | description |
|----|--------|------|-----------:|-------------:|--------------:|-----------------------:|-------------|
| `ingredient_price_surge` | 재료값 폭등 | IngredientPriceSurge | 1.0 | 2 | 0.35 | 0 (유지) | 유지 — "시장 파동으로 재료 구매가가 +35% 오른다 (2일)." |
| `hygiene_inspection` | 위생 점검 | HygieneInspection | 0.8 | 1 | 0 | 80,000 → **8,000** | **교체** — "위생 점검이 예고됐다. 대응 비용 8,000원이 정산에 더해진다 (1일)." |
| `rent_increase` | 임대료 인상 | RentIncrease | 0.6 | 0 (영구) | 0.15 | 0 (유지) | 유지 — "건물주가 임대료를 +15% 올린다 (영구, durationDays 0 = 영구 규약)." |
| `group_customers` | 단체 손님 | GroupCustomers | 0.9 | 1 | 0 | 6 → **4** | **교체** — "회식 단체 1팀(4인)이 예약 방문한다. 재료를 넉넉히 준비하자 (1일)." |

- `flatEffect` 해석 규약(kind별): HygieneInspection = 정산 가산 비용(원), GroupCustomers = **단체 파티
  크기(인)**. IngredientPriceSurge/RentIncrease는 `percentEffect`만 사용한다(+0.35 → 밀리 350, +0.15 →
  밀리 150).
- 위생 8,000원 = 일 순이익의 약 25~30% — 따끔하되 회복 가능. 단체 파티 4인 = 최대 archetype(가족 max 4)
  동급이며 장르별 순기여 +16,348~+28,460원(G절)으로 SNS 최고 효용(+12,658원)보다 큰 "준비할 가치가 있는
  기회". 파티 6인은 국밥 기준 +43,000원 수준으로 하루 경제를 뒤집어 기각했다.
- 기존 "불시"(위생)·"예고 없이"(단체) 문구는 전날 예고 규약과 모순되어 교체한다.
- 구현 중 임의 재밸런싱을 금지한다. 이 표가 G절 가드와 함께 승인 seed다.

#### B2. GameState 확장 (JsonUtility 호환)

```csharp
// GameState.cs 추가 필드 (전부 public, List/문자열/int — Dictionary·SO 참조 없음)

/// <summary>활성 이벤트 목록 (task-110 D10) — 스케줄·효과 재구성의 유일한 원천.</summary>
public List<ActiveEventState> activeEvents = new List<ActiveEventState>();

/// <summary>당일 재료 구매의 이벤트 할증 합계 (원) — marketSpendDay 리셋 규약을 공유한다.</summary>
public int marketEventSurchargeToday = 0;

// Service 당일 단체 손님 인과 통계 (StartServiceDay 가 리셋, 서빙/포기가 갱신)
public int serviceEventOrdersServedToday = 0;
public int serviceEventOrdersMissedToday = 0;
public int serviceEventRevenueToday = 0;
```

`marketEventSurchargeToday`는 `EconomyOps`의 기존 `marketSpendDay != day` 리셋 블록에서
`marketSpendToday`와 함께 0으로 리셋되고, 구매 성공 시에만 누적된다. 정산 표시는 `marketSpendDay ==
day`일 때만 읽는다(기존 spend 규약 미러링).

#### B3. ActiveEventState (신규 `Runtime/Events/ActiveEventState.cs`)

```csharp
[Serializable]
public sealed class ActiveEventState
{
    public string eventId = "";   // GameEventDef.Id (문자열 ID 규약)
    public int remainingDays = 0; // 남은 활성 일수. 0 = 영구 (durationDays=0 규약 미러링), 활성 시한 이벤트는 항상 ≥ 1
}
```

- **수명 표현 규약**: 시한 이벤트는 활성화 시 `remainingDays = durationDays`로 시작하고, 오늘을 포함해
  `remainingDays`일 동안 활성이다. 영구 이벤트는 `remainingDays = 0`으로 저장되어 절대 감소·만료하지
  않는다(task-103 `durationDays=0`=영구와 동형 — 저장 스키마에 별도 플래그 불필요).
- **목록 불변식**(검증 대상): eventId 비어 있지 않음·catalog에 존재·중복 없음, `remainingDays ≥ 0`,
  def가 영구(durationDays=0)면 remainingDays==0, 시한이면 remainingDays ≥ 1. 위반 = 손상 데이터 →
  명시적 실패.

#### B4. ServiceOrderState 태그

```csharp
// ServiceOrderState.cs 추가 필드
public bool eventInflow; // 이 주문이 단체 손님 보너스인가 (기본 false — 기존 저장/테스트 하위호환)
```

한 주문은 `snsInflow`/`eventInflow` 중 최대 하나만 참이다(세그먼트가 서로소 — D3).

### C. 결정론 스케줄 계약 (전 구간 정수)

#### C1. 상수·시드 규약

```csharp
// EventOps 상수 (코드 고정 — 데이터화하지 않음, 하드캡 4종 전제)
const int ScheduleSeed = 47;            // C4 스캔으로 선정 (선정 근거 아래)
const int OccurrenceThresholdMilli = 450; // 발생 확률 45% (roll < 450 일 때만 발생, strict less-than)
```

- **발생 roll**: `occRoll(day) = Fnv1a("event|47|" + day) % 1000`. `occRoll < 450`이면 그날 신규 이벤트
  1건이 발생한다. **strict `<`** — Day 2의 `occRoll == 450`(경계값)은 발생 없음이 계약이다(C4 벡터).
- **선택 roll**: `pickRoll(day) = Fnv1a("event-pick|47|" + day) % totalWeightMilli`. 후보는 **활성 중이
  아닌** 이벤트를 ID ordinal 오름차순으로 정렬하고, `weightMilli = RoundHalfUp(baseWeight × 1000)`
  (1000/800/600/900)의 누적합이 pickRoll을 처음 초과하는 이벤트를 선택한다(task-110 고객 pick과 동일
  패턴). 후보가 비면 발생 없음(성공).
- **Day 1 보호**: `day ≤ 1`은 무조건 이벤트 없음(첫날은 순수 학습).
- FNV-1a는 `GenreSelectionOps.Fnv1a`를 그대로 재사용한다(UTF-8, offset `2166136261`, prime `16777619`,
  unchecked — 시드 문자열 형식만 다르고 해시 함수는 단일 원천).
- **시드 47 선정 근거**(0~1999 전수 스캔): (1) Day 2 무이벤트 — 첫 이벤트 전에 기본 루프 하루 학습,
  (2) Day 3 재료값 폭등 — development-priority 수직 슬라이스의 "Day 3 예고된 재료값 폭등" 계약 충족,
  (3) Day 2 occRoll이 정확히 450으로 문턱 경계 테스트 벡터 제공, (4) 100일 분포 폭등 15/단체 13/위생
  16/임대료 1(총 45회)로 4종 전부 등장, (5) 초반 다양성 — 단체 d5, 위생 d8, 임대료 d11.
- 스케줄은 **플레이어 행동·장르·SNS와 완전히 독립**이다(입력이 day와 스케줄 자신의 활성 집합뿐).
  따라서 전체 스케줄은 day의 순수 함수로 사전 계산 가능하고 C4 표가 그 known vector다.

#### C2. 하루 경계 전이 (수명 규약)

Night(Day N) → Market(Day N+1) 경계에서 **정확히 한 번**, 아래 순서로 전이한다(원자적 — 실패 시
`activeEvents` 불변, phase 전환 차단):

```text
1. advance: 각 활성 이벤트에 대해
     remainingDays == 0 → 그대로 유지 (영구)
     remainingDays == 1 → 제거 (만료)
     remainingDays ≥ 2 → remainingDays - 1 로 복사
2. schedule: C1 규칙으로 day N+1 의 신규 이벤트를 결정 (활성 배제는 advance 후 집합 기준)
3. activate: 신규 이벤트가 있으면 remainingDays = (durationDays == 0 ? 0 : durationDays) 로 append
```

- 예: 폭등(2일)이 Day 3에 활성화되면 Day 3(remaining 2)·Day 4(remaining 1)에 활성이고 Day 5 경계에서
  제거된다. 임대료 인상은 remaining 0으로 영원히 남는다 → **활성 배제 규칙에 의해 run당 1회가 자동
  보장**된다(별도 once-per-run 로직 불필요).
- 같은 이벤트의 자기 중첩은 불가능(활성 배제)하고, kind가 서로 다른 이벤트의 동시 활성은 허용된다
  (효과 축이 서로 달라 합성이 항상 유일 — D1).

#### C3. EventOps API (순수, 신규 `Runtime/Events/EventOps.cs`)

```csharp
public static class EventOps
{
    // 투영 (float 은 경계에서 한 번만 밀리 정수화 — task-111 C1 규약)
    // GameEventDefInput { Id, DisplayName, Kind, BaseWeight, DurationDays, PercentEffect, FlatEffect }

    // 검증 — 모든 공개 API 가 진입 시 공유 (task-111 리뷰 001 교훈: 모든 경로 동일 검증)
    //   def: null/빈 Id, BaseWeight ≤0·NaN·Inf, DurationDays < 0, PercentEffect NaN·Inf,
    //        kind별: Surge(percentMilli>0, duration ≥1) / Rent(percentMilli>0, duration==0)
    //               / Hygiene(FlatEffect>0, duration ≥1) / Group(FlatEffect ≥2, duration ≥1)
    //   catalog: null/빈 목록, 중복 Id, 중복 Kind (효과 축 유일성의 근거)
    //   activeEvents: B3 불변식 전체
    public static bool TryValidateCatalog(IReadOnlyList<GameEventDefInput> defs, out string failReason);

    // C2 전이 — 순수: 새 목록을 만들어 반환하고 입력을 절대 변경하지 않는다.
    public static bool TryBuildNextDayActiveEvents(
        IReadOnlyList<ActiveEventState> current, int nextDay, IReadOnlyList<GameEventDefInput> defs,
        out List<ActiveEventState> next, out string activatedEventId, out string failReason);

    // D1 효과 합성 — 오늘 활성 집합 → 축별 효과 (순수).
    public static bool TryBuildDayEffects(
        IReadOnlyList<ActiveEventState> activeForDay, int day, IReadOnlyList<GameEventDefInput> defs,
        out EventDayEffects fx, out string failReason);

    // 예고 — TryBuildNextDayActiveEvents + TryBuildDayEffects(내일) 재사용 (예고==적용 보장).
    public static bool TryBuildForecast(
        IReadOnlyList<ActiveEventState> current, int nextDay, IReadOnlyList<GameEventDefInput> defs,
        out EventForecast forecast, out string failReason);

    // D2 합성 — SNS modifier 에 이벤트 수요 축을 결합한 완성 DayModifier (순수).
    public static bool TryComposeDayModifier(
        DayModifier snsModifier, EventDayEffects fx, out DayModifier composed, out string failReason);

    // F5 정산 원인 라인 — 조립·stale 필터·길이 fallback 의 단일 원천 (UI 재계산·호출자 사전 필터 금지).
    // 호출자(SettlementPanelController)는 state 원시 필드를 그대로 전달한다. 내부 고정 규칙:
    //   surcharge = (marketSpendDay == fx.Day) ? marketEventSurchargeToday : 0   ← 전일 잔존값 차단
    //   fx.ActiveEventIds.Count ≤ 2 → 전체 포맷, ≥ 3 → 축약 포맷 (F5 결정론 분기)
    public static string BuildSettlementCauseLine(
        EventDayEffects fx, IReadOnlyList<GameEventDefInput> defs,
        int marketSpendDay, int marketEventSurchargeToday, int groupServed, int groupRevenue);

    // F2 효과 요약 — def 데이터에서 조립: Surge "재료 구매가 +35% (2일)" / Hygiene "대응 비용 8,000원 (1일)"
    //              / Rent "운영비 +15% (영구)" / Group "단체 손님 1팀(4인) 방문 (1일)"
    public static string BuildEffectSummary(GameEventDefInput def);
}
```

정수 수학은 `GenreSelectionOps.RoundHalfUp`/`MulMilliHalfUp`/`Fnv1a`를 재사용한다(제3의 사본 금지 —
Genre가 순수 수학의 공유 원천, `ServiceOps`/`EconomyOps` 전례).

#### C4. 승인 스케줄 표 + FNV known vectors (테스트 고정 벡터)

시드 47/문턱 450의 Day 2~14 스케줄(전량 사전 계산 — 플레이어 입력 무관):

| day | occRoll | 신규 활성화 | 그날의 활성 집합 (id: remaining) |
|----:|--------:|-------------|----------------------------------|
| 2 | **450** | 없음 (경계: 450 < 450 거짓) | — |
| 3 | 69 | ingredient_price_surge | surge: 2 |
| 4 | 736 | 없음 | surge: 1 (지속) |
| 5 | 355 | group_customers | group: 1 |
| 6 | 974 | 없음 | — |
| 7 | 593 | 없음 | — |
| 8 | 164 | hygiene_inspection | hygiene: 1 |
| 9 | 783 | 없음 | — |
| 10 | 957 | 없음 | — |
| 11 | 338 | rent_increase | rent: 0 (영구) |
| 12 | 719 | 없음 | rent: 0 |
| 13 | 100 | ingredient_price_surge | rent: 0, surge: 2 |
| 14 | 481 | 없음 | rent: 0, surge: 1 |

FNV known vectors (unsigned 32-bit):

| 문자열 | FNV-1a | 파생 값 |
|--------|-------:|---------|
| `"event|47|2"` | 4061914450 | %1000 = **450** → 문턱과 동치, strict `<` 로 발생 없음 |
| `"event|47|3"` | 4078692069 | %1000 = 69 → 발생 |
| `"event-pick|47|3"` | 1368159469 | %3300 = 2569 → 누적 1700<2569≤2700 → `ingredient_price_surge` |
| `"event|47|5"` | 3978026355 | %1000 = 355 → 발생 |
| `"event-pick|47|5"` | 1267493755 | %3300 = 55 → 누적 0<55≤900 → `group_customers` |
| `"event|47|8"` | 4162580164 | %1000 = 164 → 발생 |
| `"event-pick|47|8"` | 1183605660 | %3300 = 1260 → 누적 900<1260≤1700 → `hygiene_inspection` |
| `"event|47|11"` | 484915338 | %1000 = 338 → 발생 |
| `"event-pick|47|11"` | 806839874 | %3300 = 3074 → 누적 2700<3074≤3300 → `rent_increase` |

(4종 전부 후보일 때 누적 구간: group 0~900, hygiene 900~1700, surge 1700~2700, rent 2700~3300 —
ID ordinal 정렬 `group_customers < hygiene_inspection < ingredient_price_surge < rent_increase`.)

100일(day 2~101) 분포: 폭등 15회 / 단체 13회 / 위생 16회 / 임대료 1회(영구), 총 45회 발생.

### D. 효과 합성 계약

#### D1. EventDayEffects — 축 분리 DTO (신규 `Runtime/Events/EventDayEffects.cs`)

```csharp
public sealed class EventDayEffects
{
    public int Day { get; }                              // 적용 대상 일차
    public IReadOnlyList<string> ActiveEventIds { get; } // 오늘 활성 (ID ordinal 정렬)
    public int IngredientCostMilli { get; }              // 재료 구매 단가 배수 (1000 중립)
    public int OperatingCostMilli { get; }               // 운영비 배수 (1000 중립)
    public int OperatingCostFlat { get; }                // 운영비 가산 (0 중립, 원)
    public int GroupBonusOrders { get; }                 // 단체 보너스 주문 (0 또는 1)
    public int GroupPartySize { get; }                   // 단체 파티 크기 (0 = 없음, 있으면 ≥ 2)
    public string GroupSourceEventId { get; }            // "" = 단체 없음
    public static EventDayEffects Neutral(int day);      // 전 축 중립
}
```

합성 규칙(kind 유일성 — 카탈로그 검증이 보장 — 으로 각 축은 소스가 최대 1개):

```text
중립에서 시작해 활성 이벤트를 ID ordinal 순서로 적용:
  IngredientPriceSurge → IngredientCostMilli += percentMilli   (1000 + 350 = 1350)
  RentIncrease         → OperatingCostMilli  += percentMilli   (1000 + 150 = 1150)
  HygieneInspection    → OperatingCostFlat   += FlatEffect     (0 + 8000 = 8000)
  GroupCustomers       → GroupBonusOrders = 1, GroupPartySize = FlatEffect(4), GroupSourceEventId = id
```

#### D2. DayModifier 확장 — SNS와 동일 훅으로 수요 합성 (task-111 D1의 예약 확장)

```csharp
// Runtime/Genre/DayModifier.cs — 기존 4-인자 생성자는 이벤트 축 중립으로 위임 보존 (하위호환)
public sealed class DayModifier
{
    // 기존: Day, SourceCampaignId, BonusOrderCount(SNS 0~2), WeightBoosts
    // task-112 추가 (수요 축만):
    public string EventSourceId { get; }      // "" = 단체 이벤트 없음
    public int EventBonusOrderCount { get; }  // 0 또는 1
    public int EventPartySize { get; }        // 0 = 없음, 있으면 ≥ 2 (파티 크기 고정 override)
}
```

- **결정 기록 — 축 분리**: `DayModifier`는 **수요(주문) 합성 전용**으로 유지하고, 재료값/운영비 축은
  `EventDayEffects`가 구매·정산 경로에 직접 전달한다. 두 DTO 모두 같은 `activeEvents`+defs에서
  `EventOps` 단일 원천으로 결정론 파생되므로 합성 일관성이 보장되며, `EconomyOps`/`SettlementOps`가
  SNS 투영 입력(고객·채널)에 불필요하게 결합되는 것을 막는다. (Codex 교차검토 요청 지점 — 오픈 이슈.)
- `EventOps.TryComposeDayModifier(sns, fx, ...)` 검증: `sns == null`/`fx == null`, `sns.Day != fx.Day`,
  fx 축 범위 위반(`GroupBonusOrders ∉ {0,1}`, 있음에도 `GroupPartySize < 2` 또는 소스 ID 빈 문자열) →
  명시적 실패. 성공 시 SNS 필드는 그대로, 이벤트 수요 축만 채운 새 DayModifier를 반환한다.
- `GenreSelectionOps.TryBuildDemandPlan`(modifier overload) 검증 추가(모두 명시적 실패, 상태 불변):
  `EventBonusOrderCount ∉ [0,1]`, `EventBonusOrderCount==1 ∧ (EventPartySize<2 ∨ EventSourceId=="")`,
  `EventBonusOrderCount==0 ∧ (EventPartySize≠0 ∨ EventSourceId≠"")`. 기존 SNS 검증(boost 커버리지 등)은
  그대로 선행한다.

#### D3. 주문 세그먼트·태그 (`GenreDemandPlan`/`GenreSelectionOps`/`ServiceOps`)

- `GenreDemandPlan` 확장 필드: `EventBonusOrderCount`, `EventPartySize`, `EventSourceId`.
  `OrderCount = BaseOrderCount + BonusOrderCount + EventBonusOrderCount`(neutral에서 기존 값과 동일).
  기존 생성자는 neutral 위임 보존, "동일 plan" field-by-field 동등성 계약은 신규 필드까지 확장한다.
- **주문 인덱스 세그먼트**(고정 배치):

```text
[0, Base)                : base 주문 — CustomerWeights, 기존 파티 산식
[Base, Base+Sns)         : SNS 보너스 — BonusCustomerWeights, 기존 파티 산식, snsInflow = true
[Base+Sns, Base+Sns+Evt) : 단체 보너스 — CustomerWeights(base 풀), partySize = EventPartySize 고정, eventInflow = true
```

- **불변식 2종**: (1) base-prefix 불변 — 인덱스 `0..Base-1`은 SNS·이벤트 유무와 무관하게 동일(task-111
  유지). (2) **SNS-prefix 불변** — 같은 genre/day/SNS 이력에서 이벤트 유무와 무관하게 인덱스
  `Base..Base+Sns-1`의 고객/레시피/파티가 동일하다(단체가 SNS 뒤에 append되므로 자동 성립 — 테스트 고정).
- `PickCustomerId` 3분기: `i < Base` → `CustomerWeights`, `i < Base+Sns` → `BonusCustomerWeights`,
  그 외 → `CustomerWeights`(단체는 채널 타겟과 무관, 장르 친화만 반영). 시드는 전 구간 동일하게
  `Fnv1a("genreId|day|i")`.
- `PickRecipeId`/`PickPartySize` 산식 무변경 — 단체 주문의 recipe는 round-robin을 이어 쓰고, 파티만
  `ServiceOps.BuildOrders`가 `plan.EventPartySize`로 override한다(archetype 파티 범위 초과 허용 —
  "회식 단체" 예외, 직장인 4인 등).
- `ServiceOps.BuildOrders(plan, customers)`: 위 세그먼트 규칙으로 `snsInflow`/`eventInflow`를 부여한다.
  기존 `i >= plan.BaseOrderCount` 단일 비교를 **범위 비교로 교체**한다(단체 주문에 snsInflow가 잘못
  붙는 회귀 방지 — 테스트 고정).
- `StartServiceDay`: `serviceEvent*Today` 3필드 리셋 추가. `TryServeCurrentOrder` 성공 시 `eventInflow`
  주문이면 `serviceEventOrdersServedToday++`, `serviceEventRevenueToday += price`(동일 판매가 — 별도
  계산 금지). `SkipCurrentOrder`는 `serviceEventOrdersMissedToday++`. neutral·SNS 경로의 기존 수치·
  메시지는 완전 불변이다.

#### D4. 구매 경로 (`EconomyOps` — 재료값 폭등)

```csharp
// 신규 overload — 기존 3-인자 API 는 eventCostMilli = 1000 위임으로 결과 비트 동일 (MulMilliHalfUp(x,1000)==x)
public static int CalculatePurchaseCost(IngredientDef def, int quantity, float costMultiplier, int eventCostMilli);
public static PurchaseResult TryPurchaseIngredient(GameState state, IngredientDef def, int quantity,
    float costMultiplier, int eventCostMilli);
```

- 합성 순서 고정(2단계 — neutral 비트 동일성의 근거):
  `unitGenre = RoundHalfUp(UnitCost × costMultiplier)`(기존 그대로) →
  `unitFinal = MulMilliHalfUp(unitGenre, eventCostMilli)` → `cost = unitFinal × quantity`.
- `eventCostMilli ≤ 0`: Calculate는 0 반환(기존 UI 방어 규약), TryPurchase는 상태 불변 실패
  `"잘못된 이벤트 원가 배수입니다."`.
- 성공 시에만 `state.marketEventSurchargeToday += (unitFinal - unitGenre) × quantity`. 리셋은 기존
  `marketSpendDay != day` 블록에 편입(B2).
- **known vector**: 국밥(1.15)×폭등(1350): 돼지고기 C 900 → 1035 → `MulMilliHalfUp(1035,1350)` =
  (1,397,250+500)/1000 = **1,397** (수량 2 구매 시 cost 2,794, 할증 724). 전 장르 단가 표:

| 재료 C (기본) | 국밥 1.15 → ×1350 | 분식 0.85 → ×1350 | 면류 0.95 → ×1350 | 제네럴 1.00 → ×1350 |
|--------------|-------------------|-------------------|-------------------|---------------------|
| 돼지고기 900 | 1035 → 1397 | 765 → 1033 | 855 → 1154 | 900 → 1215 |
| 소고기 1200 | 1380 → 1863 | 1020 → 1377 | 1140 → 1539 | 1200 → 1620 |
| 쌀 300 | 345 → 466 | 255 → 344 | 285 → 385 | 300 → 405 |
| 떡 400 | 460 → 621 | 340 → 459 | 380 → 513 | 400 → 540 |
| 소면 350 | 402 → 543 | 298 → 402 | 333 → 450 | 350 → 473 |
| 채소 200 | 230 → 311 | 170 → 230 | 190 → 257 | 200 → 270 |

#### D5. 정산 경로 (`SettlementOps` — 임대료 인상·위생 점검)

```csharp
// 신규 overload — 기존 1-인자 API 는 (1000, 0) 위임으로 결과 불변
public static SettlementResult ApplyDailySettlement(GameState state, int operatingCostMilli, int operatingCostFlat);
// cost = MulMilliHalfUp(DailyOperatingCost, operatingCostMilli) + operatingCostFlat
```

- 검증: `operatingCostMilli ≤ 0` 또는 `operatingCostFlat < 0` → `ArgumentOutOfRangeException`
  (호출자는 항상 EventOps 검증을 거친 fx에서 파생 — 계약 위반은 프로그래머 오류, `ArgumentNullException`
  전례와 동급 방어).
- 이미 적용된 날의 재구성 분기(멱등)는 저장 필드만 사용하므로 파라미터와 무관하게 기존과 동일하다.
- 파산 판정·cash 0 고정·사유 문자열 규칙은 무변경 — 운영비 숫자만 이벤트 반영값으로 커진다.
- **known vectors**: 임대료만 `MulMilliHalfUp(12000, 1150)` = **13,800** / 위생만 12,000+8,000 =
  **20,000** / 임대료+위생 = **21,800**.

#### D6. Worked examples (테스트 고정 벡터 — C4 스케줄 실측)

**Day 3 재료값 폭등** (전량 서빙·C급·실요구량 구매 기준, 장르별 당일 할증과 순이익):

| 장르 | 당일 구매 할증 | 폭등 있는 순이익 | 폭등 없는 가정 |
|------|--------------:|----------------:|---------------:|
| 국밥 | +7,450 | +26,725 | +34,175 |
| 분식 | +3,486 | +25,489 | +28,975 |
| 면류 | +3,009 | +30,579 | +33,588 |
| 제네럴리스트 | +5,954 | +36,546 | +42,500 |

> **정정 (float32, 2026-07-12 — Codex 코드리뷰 001 + 구현 재검산)**: 위 면류 `폭등 있는 순이익 +30,579`
> (→ 파생 `폭등 없는 가정 +33,588`)와 G절 Guard1 최저 `+15,954`·Guard2 3중첩 `+20,497`은 float32 반올림
> 경계(`350×0.95f = 332.4999… → 332`, 이상화 십진이면 `332.5 → 333`)를 놓친 손계산 오류다. 정확한 float32
> 재검산값은 각각 **+30,621 · +15,991 · +20,538**(파생 `폭등 없는 가정`은 +33,630)이며, 프로덕션 코드·
> `EventBalanceTests`가 이 값을 쓴다(프로덕션 결함 아님, C1 float32 계약대로 동작). 근거 8개 원가/가격
> 대조는 `kb/tasks/task-112/implementation-notes.md`.

**Day 5 단체 손님** (SNS 없음 — 단체 주문은 인덱스 = Base, 시드 `"{genre}|5|{Base}"`):

| 장르 | 인덱스 | FNV | roll/합 | 고객 | recipe | 파티 | 판매가 | 재료 C원가 | 순기여 |
|------|-------:|----:|--------:|------|--------|-----:|-------:|-----------:|-------:|
| 국밥 | 4 | 858745026 | 2526/3850 | senior_regular | beef_gukbap | 4 | 41,800 | 13,340 | +28,460 |
| 분식 | 6 | 2720929010 | 950/4140 | office_worker | gimbap | 4 | 18,900 | 2,552 | +16,348 |
| 면류 | 5 | 781558231 | 1681/3975 | office_worker | janchi_guksu | 4 | 22,800 | 3,424 | +19,376 |
| 제네럴리스트 | 5 | 3316028721 | 521/3800 | family_parent | janchi_guksu | 4 | 24,000 | 3,600 | +20,400 |

**SNS×단체 동시 (분식 Day 5, 전날 숏핑 첫 집행 가정 — 총 9주문 = 6+2+1)**:

- 인덱스 6 (SNS 풀 5436): `"bunsik|5|6"` = 2720929010, roll 4442 → `student`, recipe `gimbap`.
- 인덱스 7 (SNS 풀 5436): `"bunsik|5|7"` = 2737706629, roll 1129 → `office_worker`, recipe `tteokbokki`.
- 인덱스 8 (단체, base 풀 4140): `"bunsik|5|8"` = 2754484248, roll 1488 → `office_worker`,
  recipe `gimbap`, 파티 4 고정, 판매가 `RoundHalfUp(4500×4×1.05)` = **18,900원**, 태그 `eventInflow`만 참.
- SNS-prefix 불변 증명: 단체 유무와 무관하게 인덱스 6·7의 고객/레시피/파티가 동일하다(같은 풀·같은 시드).

### E. 매니저 배선·게이트

#### E1. GameManager — 이벤트 catalog 소유·하루 경계 전이·phase 게이트

- `[SerializeField] List<GameEventDef> eventCatalog` + read-only `EventCatalog`. `EditorInit(genres)`
  기존 보존 + `EditorInit(genres, events)` overload 추가. SceneBuilder가 MainMenu/Shop 양쪽에 ID ordinal
  정렬 4종(`group_customers, hygiene_inspection, ingredient_price_surge, rent_increase`)을 동일 주입한다.
- `TryGetEventDef(string id, out GameEventDef def)`(ordinal 비교),
  `internal static GameEventDefInput ToEventInput(GameEventDef)` / `ToEventInputs`(IVT로 테스트 노출).
- `bool TryBuildTodayEventEffects(out EventDayEffects fx, out string reason)`: state.activeEvents +
  투영 catalog → `EventOps.TryBuildDayEffects(active, state.day, defs, ...)`.
- `bool TryBuildEventForecast(out EventForecast forecast, out string reason)`: `state.day + 1` 대상
  `EventOps.TryBuildForecast`. `EventForecast`는 표시 완성 문자열을 담는다(F2) — UI 재계산 금지.
- `CanAdvancePhase` 확장(기존 Market/Service 게이트 유지):
  - **Settlement**: `TryBuildTodayEventEffects` 실패 시 그 사유로 차단(정산 운영비 계산 불가 = 손상
    데이터 전용 경로).
  - **Night**: `EventOps.TryBuildNextDayActiveEvents(state.activeEvents, state.day+1, defs, ...)` 실패
    시 그 사유로 차단.
- `AdvancePhase` 확장:
  - Settlement 자동 정산을 `ApplyDailySettlement(state, fx.OperatingCostMilli, fx.OperatingCostFlat)`로
    교체(fx 실패 시 현재 phase 유지).
  - **Night 분기 신설**: `machine.Advance()` 호출 **직전**에 `TryBuildNextDayActiveEvents` 성공 시에만
    `state.activeEvents = next`로 원자 교체 후 진행한다(실패 시 phase 유지). 활성화가
    `DayPhaseChanged` 발행보다 먼저 완료되므로 모든 구독자(HUD/Market)는 새 활성 집합을 관찰한다 —
    별도 `GameEvents` C# 이벤트가 필요 없는 이유.
  - `StartNewGame()`은 새 `GameState`(activeEvents 빈 목록)로 충분하다 — Day 1은 이벤트 없음(C1).

#### E2. ServiceManager — plan 합성 (SNS→이벤트 순서 고정)

`TryBuildDayPlan(genre, out plan, out reason)` 내부를 다음 순서로 확장한다(각 단계 실패 = 명시적 사유
전달, Market→Service 차단):

```text
1. SNSCampaignOps.TryBuildDayModifier(history, day, sns 투영, customer 투영) → snsModifier
2. GameManager.Instance 부재 → 실패 "게임 매니저가 초기화되지 않았습니다."
3. GameManager.TryBuildTodayEventEffects → fx
4. EventOps.TryComposeDayModifier(snsModifier, fx) → modifier (완성 — SNS+단체)
5. GenreSelectionOps.TryBuildDemandPlan(genreInput, day, recipes, customers, modifier) → plan
```

`TryStartServiceDay`/`EnsureServiceDay`(legacy)·서빙/포기 API는 무변경 — plan과 태그가 모든 이벤트
정보를 이미 고정 전달한다.

#### E3. EconomyManager·SettlementManager — 이벤트 파라미터 전달

- `EconomyManager.TryPurchaseIngredient`: 기존 장르 resolve 후
  `GameManager.TryBuildTodayEventEffects` 실패 시 상태 불변 `PurchaseResult` 실패(사유 전달), 성공 시
  `EconomyOps.TryPurchaseIngredient(state, def, qty, genre.CostMultiplier, fx.IngredientCostMilli)`.
- `EconomyManager.TryCalculatePurchaseCost(IngredientDef def, int quantity, out int cost, out string
  reason)` 신설: 장르+이벤트를 같은 helper로 합성한 **UI 예상가의 단일 경로**.
  `MarketPanelController`의 직접 `EconomyOps.CalculatePurchaseCost(def, qty, CurrentCostMultiplier())`
  호출을 이 API로 교체한다(예상가 = 거래가 보장, task-110 G3 유지).
- `SettlementManager.ApplyDailySettlement()`: `IsSettlementApplied`면 기존 재구성 경로(파라미터 무관),
  아니면 `TryBuildTodayEventEffects` 실패 시 `applied:false` + 사유 message의 SettlementResult 반환,
  성공 시 `SettlementOps.ApplyDailySettlement(state, fx.OperatingCostMilli, fx.OperatingCostFlat)`.

#### E4. 의존 방향 (task-110 G2 준수)

```text
GameEventDef (SO)                         GameState.activeEvents
      └── GameManager 투영 ──┐                    │
                             ▼                    ▼
                 EventOps (순수: 검증·스케줄·수명·효과·예고)
                   │ TryBuildDayEffects            │ TryBuildNextDayActiveEvents
                   ▼                               ▼
             EventDayEffects            Night→Market 경계 원자 교체 (GameManager)
              │         │
              │         └─ TryComposeDayModifier(+SNS) → DayModifier → GenreSelectionOps → GenreDemandPlan
              │                                                              │
              ├─ EconomyOps (IngredientCostMilli)                            ▼
              ├─ SettlementOps (OperatingCostMilli/Flat)        ServiceOps.BuildOrders(eventInflow)
              ▼                                                              │
        Managers → UI (Night 예고 / Market 오늘 표시 / Service 태그 / Settlement 원인)
```

`ServiceOps`/`GenreSelectionOps`/`EconomyOps`/`SettlementOps`는 이벤트 매니저·SO를 모르고, UI는 SO
배수를 직접 계산하지 않는다(fx/forecast/plan DTO만 표시).

### F. UI/UX — 예고·적용 인과 표시 (Codex 리뷰 게이트)

#### F1. Panel_Night 레이아웃 v2 (640×360, 패널 480×200 @ (0,−80))

예고는 정산 요약 바로 아래(읽는 순서: 오늘 마감 → 내일 예고 → SNS 설계)에 배치한다. 기존 상단 4개
텍스트를 재배치·축소해 `EventNoticeText`를 삽입한다(버튼·SnsInfoText·StatusText 좌표 불변).

| 오브젝트 | anchoredPosition | sizeDelta | 폰트 | 변경 |
|----------|------------------|-----------|------|------|
| SummaryText | (0, 86) | (440×16) | 12pt | 이동·축소 (기존 (0,84)/(440×18)/13pt) |
| DaysText | (0, 71) | (440×12) | 10pt | 이동·축소 (기존 (0,66)/(440×14)) |
| **EventNoticeText (신규)** | (0, 57) | (460×14) | 11pt | F2 예고 카피 |
| FollowerText | (0, 44) | (440×12) | 10pt | 이동·축소 (기존 (0,50)/(440×14)/11pt) |
| SnsTitleText | (0, 31) | (440×14) | 12pt | 이동·축소 (기존 (0,32)/(440×16)) |
| Button_Sns_* 3종 | (−150/0/150, 0) | (140×46) | 11pt | 불변 |
| SnsInfoText | (0, −40) | (460×28) | 11pt | 불변 |
| StatusText | (0, −72) | (460×32) | 11pt | 불변 |

세로 점유(로컬 y): 78~94 / 65~77 / 50~64 / 38~50 / 24~38 / −23~23 / −26~−54 / −56~−88 — 겹침 없음,
상단 여백 6px·하단 12px. focus 순서(픽쳐그램→숏핑→동네게시판→다음 날 ▶)는 불변 — 예고는 텍스트 전용.

#### F2. 예고·경고 카피 (Codex 소유 UX copy — 임의 수정 금지)

`EventForecast` DTO가 완성 문자열을 담는다: `UpcomingEventId`, `UpcomingNoticeLine`,
`ContinuingNoticeLine`, `NextDayOperatingCost`(내일 운영비 = `MulMilliHalfUp(12000, 내일 milli) + 내일
flat`).

| 상황 | EventNoticeText | 색 |
|------|-----------------|-----|
| 신규 위기 (폭등/위생/임대료) | `[예고] 내일 {표시명} — {효과 요약}` | Warning Plum `#A93E58` |
| 신규 기회 (단체) | `[예고] 내일 {표시명} — {효과 요약}` | Brass Amber `#E5A84B` |
| 신규 없음 + 지속 있음 | `[지속] {표시명} — 내일까지` (remaining 2 이상이면 `{n}일 더`) | Warning Plum |
| 신규 + 지속 동시 | `[예고] 내일 {표시명} — {효과 요약} · 지속: {표시명}` | 신규 기준 색 |
| 없음 | `내일 예고된 이벤트 없음` | 기본(Steam Cream) |
| forecast 실패 (손상 데이터) | `이벤트 상태 오류: {사유}` | Warning Plum |

- 효과 요약은 `EventOps.BuildEffectSummary`(C3)의 단일 원천 문자열 — `재료 구매가 +35% (2일)` /
  `대응 비용 8,000원 (1일)` / `운영비 +15% (영구)` / `단체 손님 1팀(4인) 방문 (1일)`. 퍼센트 표시는
  `percentMilli / 10` 정수 조립(부동소수 문자열화 금지).
- SNS 집행 후 잔액 경고(task-111 F2)의 고정 `12,000원`을 `forecast.NextDayOperatingCost` 기반
  `내일 운영비 {n:N0}원이 부족할 수 있습니다`로 교체한다(임대료 인상 후 13,800원 정확 표시).
- 색+문구 병용 규칙: 상태는 `[예고]`/`[지속]` prefix 문구가 전달하고 색은 보조다.

#### F3. Market·HUD — 당일 적용 표시

- `MarketPanelController` 확정 상태 상세(`genreDetailNumbersText`): task-111의 SNS 문구 뒤에 활성
  이벤트를 덧붙인다 — ` · 오늘: 재료값 폭등 +35%`(fx 파생, `(IngredientCostMilli−1000)/10`%),
  단체 날은 ` · 단체 +1팀(4인) 예정`(plan의 `EventBonusOrderCount`/`EventPartySize` 사용 — UI 재계산
  금지). 재료 행 예상가는 E3의 `TryCalculatePurchaseCost`로 이미 인상가를 표시한다.
- `PhaseHudController.RefreshGenreBadge` 확장(plan 파생):
  - 기본 `{장르} · 주문 {n}건`, SNS만 `{base}+{b}건(SNS)`(기존 불변), 단체만 `{base}+1건(단체)`,
    동시 `{base}+{b}+1건(SNS·단체)`.
  - 갱신 시점 불변(Start/GenreSelected/DayPhaseChanged — task-111 F5 보정 유지).

#### F4. Service·무대 태그

- `ServicePanelController.customerText`: 단체 주문이면 `{고객 표시명} ×{party} · 단체 손님`.
- `ServicePresentationEventArgs`에 `EventInflow` 필드 추가(기존 생성자는 false 위임 overload 보존 —
  task-111 `SnsInflow` 전례).
- 무대 `ShopPresentationController.customerLabel`: 단체 주문만 ` <color=#E5A84B>단체</color>`(Brass
  Amber rich text — SNS의 Jade Green과 구분). 게임 규칙에는 관여하지 않는다.

#### F5. Settlement — 이벤트 원인 라인

- `SettlementPanelController`에 `eventEffectText`(신규 TMP) 추가. 좌표: `EventEffectText (0, −76) /
  (460×14) / 10pt`, 기존 `MessageText`를 `(0,−84)/(460×24)/11pt` → **`(0,−91)/(460×14)/10pt`**로 이동·
  축소(겹침 방지 — GenreEffectText (0,−48)·SnsEffectText (0,−62)는 불변. 세로 점유: sns −69~−55,
  event −83~−69, message −98~−84).
- 내용은 `EventOps.BuildSettlementCauseLine`(단일 원천 — 조립·stale 필터·길이 fallback을 전부 내부
  처리)이 조립한다. controller는 state 원시 필드(`marketSpendDay`/`marketEventSurchargeToday`/
  `serviceEventOrdersServedToday`/`serviceEventRevenueToday`)를 **필터링·가공 없이 그대로** 전달한다
  (UI 사전 계산·사전 필터 금지 — C3 시그니처).
- **stale 필터(내부 고정 규칙)**: 폭등 할증은 `marketSpendDay == fx.Day`일 때만
  `marketEventSurchargeToday`를 사용하고, 아니면 **0으로 강제**한다 — 전일 잔존값이 원인 라인에 새는
  것을 단일 원천이 차단한다(0원도 정직 표시). 호출자 측 필터 구현은 금지한다(이중 원천 방지).
- **포맷 선택(결정론 분기)**: `fx.ActiveEventIds.Count ≤ 2`면 전체 포맷, `≥ 3`이면 축약 포맷.
  활성 이벤트만, ID ordinal 순:

```text
[전체 포맷 — 활성 ≤ 2종, 해당 항목만 ' · ' 결합]
이벤트: 단체 {served}/{bonus}팀 +{rev:N0}원 · 위생 점검 -{flat:N0}원
      · 재료값 폭등 -{surcharge:N0}원 · 임대료 인상 -{delta:N0}원

[축약 포맷 — 활성 ≥ 3종, 같은 순서·같은 값]
이벤트: 단체 {served}/{bonus} +{rev:N0} · 위생 -{flat:N0} · 폭등 -{surcharge:N0} · 임대 -{delta:N0}
```

  (`단체` = GroupCustomers 활성일 때 `served = serviceEventOrdersServedToday`, `bonus =
  fx.GroupBonusOrders`, `rev = serviceEventRevenueToday`; `위생` flat 그대로; `폭등` surcharge = stale
  필터 통과값; `임대료` delta = `MulMilliHalfUp(12000, OperatingCostMilli) − 12000`. 전체 포맷의
  이벤트명은 def `DisplayName`(해석 실패 시 eventId — task-111 F4 전례), 축약명은 kind별 EventOps 상수
  `단체/위생/폭등/임대`로 고정한다 — displayName 변경과 무관하게 결정론.)
- **worst-case 길이 상한(자동 검증 계약)**: 전체 포맷 최악 2종 조합(단체 `+41,800원` = 데모 최대 단체
  판매가 beef_gukbap×4×0.95, 폭등 `-99,999원` 6자리 여유)은 실측 39자 — **상한 42자**. 축약 포맷 4종
  동시(같은 최악값)는 실측 56자 — **상한 60자**. 씬 테스트는 실제 Galmuri TMP 10pt로 두 포맷 worst-case
  문자열의 `TMP_Text.GetPreferredValues(...)` 폭이 **460px 이하**임을 자동 검증한다 — 수동 시각 승인에
  의존하지 않으며, 시각 승인 게이트에는 축약 카피의 톤 확인만 남는다.
- 활성 이벤트가 없으면 빈 문자열. fx 조회 실패 시 빈 문자열(표시 전용 — 도메인 게이트가 이미 차단).
- `EditorInit`은 기존 시그니처 보존 overload로 확장한다.

### G. 밸런스 가드 (승인 seed 근거 — "회복 가능" 증명)

방법론(task-110 D5/task-111 G 계승): C4 스케줄 그대로 day 2~101의 100개 결정론 day를 전량 서빙·C급·
실요구량 구매로 시뮬레이션한다(SNS 없음). 가드는 프로덕션 Ops를 그대로 호출해 재유도한다
(`GenreBalanceTests` 방식 — 재계산 금지).

**가드 1 — 이벤트만으로 파산 강제 불가(핵심)**: 100일 × 4장르 전 구간에서 일일 순이익(매출 − 소비
재료 구매비 − 이벤트 반영 운영비)이 **매일 양수**다. 정수 결정론이므로 실측 최저/평균을 고정한다:

| 장르 | 최저 일일 순이익 (발생 day) | 100일 평균 |
|------|---------------------------:|-----------:|
| 국밥 | +5,031 (day 32) | +35,375.9 |
| 분식 | +15,073 (day 59) | +34,708.6 |
| 면류 | +15,954 (day 57) | +36,429.8 |
| 제네럴리스트 | +13,350 (day 54) | +37,929.0 |

**가드 2 — 최악 조합 가상 검증**: 스케줄상 미발생인 3중첩(임대료+위생+폭등, day 13 수요 기준)을 가정
주입해도 전 장르 순이익 양수(국밥 +27,851 / 분식 +15,689 / 면류 +20,497 / 제네럴리스트 +19,478).

**가드 3 — 데모 3일 생존**: Day 1(무이벤트)~Day 3(폭등) 전량 서빙 시 4장르 모두 매일 흑자, Day 3 마감
잔액 ≥ 121,568원(분식 최저). Day 3 폭등 순이익은 D6 표와 정확히 일치해야 한다.

**가드 4 — 단체는 항상 순기여 양수**: Day 5 단체 주문의 (판매가 − C급 재료 구매비)가 전 장르 양수이며
D6 표와 정확히 일치한다(+16,348~+28,460).

**가드 5 — 주문 하드캡**: 어떤 조합에서도 하루 총 주문 ≤ **9**건(base 6 + SNS 2 + 단체 1 — task-111의
캡 8을 단체 1건만큼 상향 개정).

**가드 6 — 스케줄 분포**: 100일 발생 횟수 폭등 15/단체 13/위생 16/임대료 1(영구 1회 자동 보장), 총 45.
Day 2는 발생 없음(occRoll 450 == 문턱, strict `<`).

**가드 7 — 재유도 정확성**: 정수 결정론이므로 고정 벡터(C4/D4/D5/D6)는 ±허용오차 없이 **정확히
일치**해야 한다(task-111의 ±1%는 평균 산출용 — 이번 가드 1의 평균만 ±1% 허용).

긴장 구조: 위기 이벤트의 하루 비용(위생 8,000 / 임대료 +1,800 / 폭등 할증 +3,009~+7,450)은 일 순이익의
10~30%로 "따끔하지만 회복 가능"하고, 단체(+16,348~+28,460)는 준비(재료 4인분 추가 구매)를 요구하는
기회다. 국밥은 재료가 비싸 폭등에 가장 취약하고(할증 +7,450) 단체 보상이 가장 크다(+28,460) — 장르
선택이 이벤트 대응 전략까지 바꾼다.

### H. task-112 상세 구현 순서

1. 코드 변경 전 scaffold `manifest.md`의 placeholder를 이 문서의 Inputs·영향 파일·검증 명령으로 채운다.
2. 현재 기준선(EditMode 236/PlayMode 4)을 실행해 실제 결과를 구현 노트에 기록하고, GameManager 경로로
   day 3 이상을 진행하는 기존 테스트(`FirstPlayableLoopTests` 등)를 식별해 C4 스케줄 반영이 필요한
   기대값 목록을 만든다(변경은 7단계에서 일괄 — 조용한 우회 금지, 구현 노트에 전/후 기록).
3. `GameState`에 B2 필드 5종, `ServiceOrderState`에 `eventInflow`를 추가한다(기본값 하위호환).
   `Runtime/Events/ActiveEventState.cs`(B3)를 만든다.
4. `Runtime/Events/EventOps.cs`(C1~C3: `GameEventDefInput` 투영, 검증, 스케줄, 수명 전이, 효과 합성,
   예고, 카피 조립)와 `Runtime/Events/EventDayEffects.cs`(D1 fx + `EventForecast`)를 만든다.
5. `DayModifier` 이벤트 수요 축 확장(D2 — 기존 생성자 neutral 위임), `GenreDemandPlan` 확장(D3),
   `GenreSelectionOps.TryBuildDemandPlan` 검증·보너스 풀·`PickCustomerId` 3분기(D2/D3),
   `EventOps.TryComposeDayModifier`(D2)를 구현한다.
6. `ServiceOps`(D3 — 세그먼트 태그 범위 비교·파티 override·단체 통계·리셋), `EconomyOps`(D4 —
   overload·할증 추적·리셋 편입), `SettlementOps`(D5 — overload)를 구현한다. neutral 경로 비트 동일을
   확인한다.
7. `GameManager`(E1 — catalog·투영·fx/forecast API·Night 경계 원자 교체·CanAdvancePhase 확장),
   `ServiceManager.TryBuildDayPlan` 합성(E2), `EconomyManager`(E3 — 구매 전달·`TryCalculatePurchaseCost`),
   `SettlementManager`(E3)를 배선한다. 2단계에서 식별한 기존 테스트 기대값을 C4 표 기준으로 갱신한다.
8. `NightPanelController`(F1/F2 — 예고 라인·운영비 경고 교체), `MarketPanelController`(F3 — 상세 문구·
   예상가 API 교체), `PhaseHudController`(F3 badge), `ServicePanelController`·
   `ServicePresentationEventArgs`·`ShopPresentationController`(F4), `SettlementPanelController`(F5)를
   구현한다. `EditorInit`은 전부 기존 시그니처 보존 overload로 확장한다.
9. `InitialDataBuilder.BuildGameEvents`를 B1 확정값(위생 8,000/단체 4 + 문구 2건)으로 GUID 보존
   upsert한다(다른 필드 불변).
10. `SceneBuilder`: Night 레이아웃 v2 재배치·`EventNoticeText` 생성·Settlement `EventEffectText` 생성·
    `MessageText` 이동, `GameManager.EditorInit` 2-인자 overload로 이벤트 catalog를 MainMenu/Shop 양쪽
    동일 주입, 2씬 재생성. 멱등성(연속 2회 Apply 오브젝트 수·persistent listener 불변)을 유지한다.
11. EditMode 테스트 — 순수 도메인: `EventOpsTests`(신규 — 투영·검증 매트릭스·C4 스케줄/FNV 벡터·경계
    450·수명 전이·영구 유지·run당 임대료 1회·효과 합성·예고==적용 일치·카피 조립·명시적 실패 전량),
    `GenreSelectionOpsTests` 확장(D2 검증·3분기 pick·base/SNS-prefix 불변·D6 벡터·plan 동등성),
    `ServiceOpsTests` 확장(태그 범위·파티 override·단체 통계·neutral 회귀), `EconomyManagerTests`/신규
    구매 벡터(D4 표·항등·할증 추적·리셋), `SettlementOpsTests` 확장(D5 벡터·멱등·파산 경로).
12. EditMode 테스트 — 가드·통합: `EventBalanceTests`(신규 — G절 가드 1~7 프로덕션 Ops 재유도),
    JsonUtility 왕복(신규 클래스 — `activeEvents`(영구+시한 혼합) 왕복 field-by-field + 왕복 전후
    `TryBuildDayPlan` plan 동등성 — task-111 리뷰 001 Action 2 전례를 처음부터 포함), 매니저 게이트
    (미지 eventId 활성 시 Market→Service/Settlement→Night/Night→Market 차단·상태 불변).
13. 씬 테스트: `NightPanelSceneTests`(F1 v2 좌표 갱신 + EventNoticeText 존재), 신규
    `NightPanelEventFlowTests`(상태 — Day 1 Night `이벤트 없음` → Day 2 Night 폭등 예고 문구·색, 구조/
    상태 fixture 분리 규약 유지), `SettlementPanelSceneTests`(EventEffectText·MessageText 좌표 갱신·원인
    라인 문구), `MarketPanelSceneTests`(오늘 이벤트 문구·인상 예상가), `ServicePanelSceneTests`(단체
    태그), `SceneBuilderTests`(이벤트 catalog 4종 ID ordinal 양씬 동일 주입·오브젝트 수 멱등).
14. PlayMode 테스트: MainMenu→Shop 전환 후 persistent `GameManager`가 이벤트 catalog 4종을 보유하고,
    UI 없이 도메인 경로로 Day 1→2(무이벤트)→3(폭등 활성·구매 할증 발생) 진행과 Night 예고==다음 날
    활성화 일치를 검증한다(기존 4개 무회귀).
15. `InitialDataBuilder.Apply → SceneBuilder.Apply → compile → EditMode → PlayMode` 순으로 배치 검증하고
    `git status --short game`에 산출물 오염이 없는지 확인한다.
16. 수동 Play smoke(오너/Codex 게이트): Day 1 Night `이벤트 없음` → Day 2 Night 폭등 예고 → Day 3
    Market 인상 구매가·정산 원인 라인의 인과 사슬, Day 5 단체(예고 → HUD +1건 → 태그 → 정산 +금액),
    Night 판단 60초 이내를 확인한다.
17. 구현 노트·artifacts summary·`python3 runtime/generate-status.py` 재생성으로 기록을 마감하고, 장르+
    SNS+이벤트 **3일 수직 슬라이스 통합 플레이테스트**(task-110 로드맵 I의 M2 통합 게이트)를 오너에게
    요청한다.

## 실행 계획 (Execution Plan)

- implement_model: claude-fable-5
- implement_effort: high
- routing_reason: 프로젝트 SSOT 로드맵이 task-112를 fable-5/high로 고정 — 신규 씬·매니저 없이 기존
  DayModifier 훅과 Ops overload 패턴 위에 결정론 스케줄·효과 합성을 얹는 M2 마지막 단일 시스템 작업이다.

| unit | 파일 범위 | depends_on | group |
|------|-----------|------------|-------|
| U1-events-domain | `GameState`, `ServiceOrderState`, 신규 `ActiveEventState`/`EventOps`/`EventDayEffects` | 없음 | G1 |
| U2-demand-composition | `DayModifier`, `GenreDemandPlan`, `GenreSelectionOps`, `ServiceOps` | U1-events-domain | G2 |
| U3-economy-settlement | `EconomyOps`, `SettlementOps` overload·할증 추적 | U1-events-domain | G2 |
| U4-manager-wiring | `GameManager`, `ServiceManager`, `EconomyManager`, `SettlementManager` | U2-demand-composition, U3-economy-settlement | G3 |
| U5-ui-controllers | `NightPanelController`, `MarketPanelController`, `PhaseHudController`, `ServicePanelController`, `SettlementPanelController`, `ServicePresentationEventArgs`, `ShopPresentationController` | U4-manager-wiring | G4 |
| U6-scene-data-wiring | `SceneBuilder`(레이아웃 v2·catalog 주입 최종 wiring 단독 소유), `InitialDataBuilder`(B1 재시드), 씬·에셋 산출물 | U5-ui-controllers | G5 |
| U7-tests | ops/스케줄/가드/왕복/매니저/scene/flow/PlayMode 테스트 + 기존 day≥3 기대값 갱신 | U2-demand-composition, U3-economy-settlement, U4-manager-wiring, U6-scene-data-wiring | G6 |
| U8-validation-records | builder·compile·EditMode·PlayMode·validator·수동 smoke 준비·구현 노트/summary/status | U7-tests | G7 |

## 파일/모듈 영향 (Affected Files/Modules)

| 파일/모듈 | 변경 유형 | 설명 |
|-----------|-----------|------|
| `game/Assets/Scripts/Runtime/DayCycle/GameState.cs` | modify | `activeEvents` List + `marketEventSurchargeToday` + `serviceEvent*Today` 3필드 (JsonUtility 호환) |
| `game/Assets/Scripts/Runtime/Service/ServiceOrderState.cs` | modify | `eventInflow` bool 태그 필드 |
| `game/Assets/Scripts/Runtime/Events/ActiveEventState.cs` | create | 활성 이벤트 Serializable 상태 (B3 — id + remainingDays, 0=영구) |
| `game/Assets/Scripts/Runtime/Events/EventOps.cs` | create | 투영 DTO·검증·FNV 스케줄·수명 전이·효과 합성·예고·카피 조립 (순수 C#) |
| `game/Assets/Scripts/Runtime/Events/EventDayEffects.cs` | create | 축 분리 효과 DTO + EventForecast (순수 DTO) |
| `game/Assets/Scripts/Runtime/Genre/DayModifier.cs` | modify | 이벤트 수요 축 3필드 (EventSourceId/EventBonusOrderCount/EventPartySize), 기존 생성자 neutral 위임 |
| `game/Assets/Scripts/Runtime/Genre/GenreDemandPlan.cs` | modify | Event 3필드 + `OrderCount` 3항 합, 생성자 하위호환 |
| `game/Assets/Scripts/Runtime/Genre/GenreSelectionOps.cs` | modify | modifier 이벤트 축 검증·`PickCustomerId` 3분기 |
| `game/Assets/Scripts/Runtime/Service/ServiceOps.cs` | modify | 세그먼트 태그(범위 비교)·단체 파티 override·단체 통계·리셋 (neutral/SNS 경로 불변) |
| `game/Assets/Scripts/Runtime/Economy/EconomyOps.cs` | modify | eventCostMilli overload·할증 추적·리셋 편입 (기존 API 비트 동일 위임) |
| `game/Assets/Scripts/Runtime/Economy/EconomyManager.cs` | modify | fx 전달 구매·`TryCalculatePurchaseCost` UI 예상가 단일 경로 |
| `game/Assets/Scripts/Runtime/Settlement/SettlementOps.cs` | modify | 운영비 milli+flat overload (기존 API (1000,0) 위임) |
| `game/Assets/Scripts/Runtime/Settlement/SettlementManager.cs` | modify | fx 조회 후 overload 호출·손상 시 명시적 실패 결과 |
| `game/Assets/Scripts/Runtime/Managers/GameManager.cs` | modify | 이벤트 catalog·투영·fx/forecast API·Night 경계 원자 교체·CanAdvancePhase Settlement/Night 게이트 |
| `game/Assets/Scripts/Runtime/Service/ServiceManager.cs` | modify | `TryBuildDayPlan` SNS→이벤트 합성 배선 (E2 5단계) |
| `game/Assets/Scripts/Runtime/UI/NightPanelController.cs` | modify | EventNoticeText 예고 표시·운영비 경고를 forecast 기반으로 교체 (EditorInit overload) |
| `game/Assets/Scripts/Runtime/UI/MarketPanelController.cs` | modify | 오늘 이벤트 문구·예상가 API 교체 |
| `game/Assets/Scripts/Runtime/UI/PhaseHudController.cs` | modify | badge 단체 표기 (`+1건(단체)`/`(SNS·단체)`) |
| `game/Assets/Scripts/Runtime/UI/ServicePanelController.cs` | modify | `단체 손님` 태그 표시·표현 이벤트에 태그 전달 |
| `game/Assets/Scripts/Runtime/UI/SettlementPanelController.cs` | modify | `eventEffectText` 원인 라인 (EditorInit overload) |
| `game/Assets/Scripts/Runtime/Presentation/ServicePresentationEventArgs.cs` | modify | `EventInflow` 필드 (기존 생성자 overload 보존) |
| `game/Assets/Scripts/Runtime/Presentation/ShopPresentationController.cs` | modify | 무대 손님 라벨 `단체` 표기 (Brass Amber rich text) |
| `game/Assets/Scripts/Editor/InitialDataBuilder.cs` | modify | 이벤트 재시드 — 위생 flat 8,000·단체 flat 4·문구 2건 (GUID 보존, 타 필드 불변) |
| `game/Assets/Scripts/Editor/SceneBuilder.cs` | modify | Night 레이아웃 v2·EventNoticeText·Settlement EventEffectText/MessageText·이벤트 catalog 주입·2씬 재생성 |
| `game/Assets/Scenes/Shop.unity` | modify | SceneBuilder 산출 예고/원인 UI |
| `game/Assets/Scenes/MainMenu.unity` | modify | SceneBuilder 재실행 산출 (catalog 주입 외 기능 변경 없음) |
| `game/Assets/Data/Definitions/Events/hygiene_inspection.asset` | modify | flatEffect 8,000 + 문구 upsert |
| `game/Assets/Data/Definitions/Events/group_customers.asset` | modify | flatEffect 4 + 문구 upsert |
| `game/Assets/Tests/EditMode/EventOpsTests.cs` | create | 투영·검증 매트릭스·스케줄/FNV 벡터·수명·효과·예고 일치·명시적 실패·JsonUtility 왕복 |
| `game/Assets/Tests/EditMode/EventBalanceTests.cs` | create | G절 가드 1~7 프로덕션 Ops 재유도 (100-day·최악 조합·데모 3일·하드캡 9) |
| `game/Assets/Tests/EditMode/GenreSelectionOpsTests.cs` | modify | 이벤트 축 검증·3분기 pick·prefix 불변 2종·D6 벡터·plan 동등성 |
| `game/Assets/Tests/EditMode/ServiceOpsTests.cs` | modify | 태그 범위·파티 override·단체 통계·neutral/SNS 회귀 |
| `game/Assets/Tests/EditMode/EconomyManagerTests.cs` | modify | D4 구매 벡터·항등·할증 추적·리셋·실패 불변 |
| `game/Assets/Tests/EditMode/SettlementOpsTests.cs` | modify | D5 운영비 벡터·멱등·파산 경로·ArgumentOutOfRange 방어 |
| `game/Assets/Tests/EditMode/FirstPlayableLoopTests.cs` | modify | day ≥ 3 구간 기대값을 C4 스케줄 반영으로 갱신 (H2 식별분) |
| `game/Assets/Tests/EditMode/NightPanelSceneTests.cs` | modify | F1 v2 좌표 갱신 + EventNoticeText 구조 |
| `game/Assets/Tests/EditMode/NightPanelEventFlowTests.cs` | create | 예고 문구·색 상태 테스트 (구조/상태 fixture 분리 규약) |
| `game/Assets/Tests/EditMode/SettlementPanelSceneTests.cs` | modify | EventEffectText/MessageText 좌표·원인 라인 문구 |
| `game/Assets/Tests/EditMode/MarketPanelSceneTests.cs` | modify | 오늘 이벤트 문구·인상 예상가 표시 |
| `game/Assets/Tests/EditMode/ServicePanelSceneTests.cs` | modify | 단체 태그 표시 회귀 |
| `game/Assets/Tests/EditMode/SceneBuilderTests.cs` | modify | 이벤트 catalog 4종 양씬 동일 주입·레이아웃 v2 오브젝트·멱등 |
| `game/Assets/Tests/PlayMode/EventSchedulePlayModeTests.cs` | create | 씬 전환 catalog 생존·Day 1→3 도메인 경로 예고==활성화·구매 할증 통합 검증 |
| 관련 `game/**/*.meta` | create/modify | 신규 Events 폴더·스크립트·테스트의 Unity 메타 쌍 |

## 테스트 기준 (Test Criteria)

- [ ] `python -B runtime/validator/cli.py kb/tasks/task-112/design.md`가 종료 코드 0으로 통과한다.
- [ ] 구현 전 기준선(EditMode 236/PlayMode 4)을 실행하고 실제 결과와 day ≥ 3 영향 테스트 식별 목록을
  구현 노트에 기록한다.
- [ ] `GameState.activeEvents` 기본값은 빈 List이고 `ActiveEventState`는 문자열 ID·정수만 담는
  `[Serializable]` public 필드 클래스다 (SO/Dictionary/enum 직렬화 없음).
- [ ] FNV known vectors가 C4 표와 정확히 일치한다: `"event|47|2"`=4061914450(%1000=450 — 문턱과 동치,
  strict `<`로 **발생 없음**), `"event|47|3"`=4078692069(→발생), `"event-pick|47|3"`=1368159469
  (%3300=2569→폭등), day 5/8/11 벡터 전부 포함.
- [ ] Day 2~14 스케줄이 C4 표와 정확히 일치한다(신규 활성화·활성 집합·remaining 전이 전부). Day 1은
  항상 이벤트 없음이다.
- [ ] 수명 전이가 C2와 일치한다: 폭등(2일)이 remaining 2→1→제거로 전이하고, 임대료 인상(remaining 0)은
  100일 시뮬레이션 내내 유지되며 **run당 정확히 1회만** 발생한다(활성 배제의 자동 결과).
- [ ] 이벤트 def/카탈로그/활성 상태 검증 매트릭스가 전부 명시적 실패다: 빈/중복 Id, 중복 Kind,
  BaseWeight·PercentEffect 0 이하·NaN·Infinity, kind별 duration/flat 규약 위반(B1), 미지 eventId 활성,
  remainingDays 음수, 영구/시한 remaining 불일치 — 실패 경로는 `GameState` 완전 불변이다.
- [ ] `TryBuildNextDayActiveEvents`는 순수하다(입력 목록 불변·새 목록 반환)이고, `GameManager`의 Night
  경계 교체는 원자적이다(실패 시 `activeEvents`·phase 불변).
- [ ] **예고==적용**: day 2~14 각각에 대해 전날 `TryBuildForecast` 결과(UpcomingEventId·지속 목록)가
  다음 날 실제 활성화·활성 집합과 일치한다.
- [ ] `EventDayEffects` 합성이 D1과 일치한다: 폭등 IngredientCostMilli 1350, 임대료 OperatingCostMilli
  1150, 위생 OperatingCostFlat 8000, 단체 GroupBonusOrders 1/GroupPartySize 4, 다중 활성 시 축별 독립.
- [ ] `TryComposeDayModifier` 검증(null/fx·day 불일치·축 범위·정합성 위반)이 명시적 실패고, 성공 시
  SNS 필드가 보존된다. `TryBuildDemandPlan`의 이벤트 축 검증 3종(D2)도 명시적 실패 + 상태 불변이다.
- [ ] 기존 4-입력 `TryBuildDemandPlan`·Neutral modifier·SNS-only modifier의 plan이 이벤트 필드 neutral로
  field-by-field 동등하고, task-110/111의 기존 결정론 테스트(주문 수 4/6/5/5, base-prefix 불변, C5 벡터,
  FNV `"gukbap|1|0"`=2190636514)가 전부 회귀 없이 통과한다.
- [ ] **세그먼트 태그 정확성**: `BuildOrders`가 `snsInflow`를 정확히 `[Base, Base+Sns)`에만,
  `eventInflow`를 정확히 `[Base+Sns, 총합)`에만 부여하며 한 주문에 두 태그가 동시에 참이 될 수 없다.
- [ ] **SNS-prefix 불변**: 같은 genre/day/SNS 이력에서 단체 유무와 무관하게 SNS 구간 주문의 고객/레시피/
  파티가 동일하다(D6 분식 Day 5 벡터로 고정).
- [ ] D6 단체 worked example이 정확히 일치한다: 4장르 Day 5 단체 주문의 FNV·roll·고객·recipe·파티 4·
  판매가·재료 원가·순기여, 그리고 분식 Day 5 SNS×단체 9주문 벡터(인덱스 6/7/8의 고객·recipe·태그).
- [ ] 단체 주문 파티 4가 archetype 파티 범위와 무관하게 override되고, 서빙/포기 시
  `serviceEvent{OrdersServed,OrdersMissed,Revenue}Today`가 단체 주문에만 갱신되며 `StartServiceDay`가
  3필드를 리셋한다. neutral·SNS 주문의 기존 수치·메시지는 불변이다.
- [ ] D4 구매 벡터가 정확히 일치한다: 전 장르 단가 표(D4), 국밥 돼지고기 C 2개 = 2,794원(할증 724),
  `MulMilliHalfUp(x, 1000) == x` 항등으로 기존 3-인자 API 결과가 비트 동일, `eventCostMilli ≤ 0` 실패
  시 현금·재고·marketSpend·할증 불변.
- [ ] `marketEventSurchargeToday`가 구매 성공 시에만 누적되고 `marketSpendDay != day` 리셋 블록에서
  `marketSpendToday`와 함께 0이 된다.
- [ ] D5 정산 벡터가 정확히 일치한다: 임대료 13,800 / 위생 20,000 / 동시 21,800. 기존 1-인자 API는
  (1000,0) 위임으로 불변, 멱등 재구성 분기 불변, 이벤트 운영비로 cash 부족 시 기존 파산 규칙 그대로
  동작, `operatingCostMilli ≤ 0`/`flat < 0`은 `ArgumentOutOfRangeException`.
- [ ] `GameManager.CanAdvancePhase`: 활성 이벤트가 catalog에 없는 손상 상태에서 Settlement→Night와
  Night→Market이 명시적 사유로 차단되고(Market→Service는 `TryBuildDayPlan` 실패로 기존 차단), 정상
  상태에서는 기존 게이트 결과가 불변이다.
- [ ] `GameState`를 `JsonUtility` 왕복한 뒤 `activeEvents`(영구+시한 혼합 2건)·할증·단체 통계 필드가
  손실 없고, 왕복 전/후 `TryBuildDayPlan` 결과 plan(이벤트 필드 포함)이 field-by-field 동등하다
  (task-113 선행 보증 — task-111 리뷰 001 교훈 선반영).
- [ ] `EventBalanceTests` 가드: (1) 100일×4장르 전량 서빙 일일 순이익 매일 양수 + 최저/평균이 G절 표와
  일치(최저 정확, 평균 ±1%), (2) 3중첩 가상 조합 전 장르 양수, (3) 데모 3일 매일 흑자·Day 3 순이익 D6
  일치, (4) 단체 순기여 D6 일치, (5) 하루 총 주문 ≤ 9, (6) 100일 분포 15/13/16/1, (7) Day 2 경계 무발생.
- [ ] `InitialDataBuilder`가 위생 flatEffect 8,000·단체 flatEffect 4·문구 2건만 upsert하고 weight·
  duration·percent·GUID·타 이벤트가 불변이다. 연속 2회 `InitialDataBuilder.Apply`+`SceneBuilder.Apply`가
  asset GUID·오브젝트 수·persistent listener 기준 멱등이다.
- [ ] `SceneBuilder` 산출 Night 패널이 F1 v2 좌표 표와 정확히 일치하고(이동 4종 + EventNoticeText 신규 +
  불변 4종), Settlement 패널의 EventEffectText (0,−76)/MessageText (0,−91) 좌표가 일치하며, 겹침·canvas
  이탈이 없고 Build Settings 씬은 2개뿐이다.
- [ ] `GameManager`가 MainMenu/Shop 양쪽에서 이벤트 catalog 4종(ID ordinal)을 동일 보유하고, PlayMode
  씬 전환 후에도 유지된다.
- [ ] Night 예고 라인이 F2 카피와 일치한다: Day 1 Night `내일 예고된 이벤트 없음` → Day 2 Night
  `[예고] 내일 재료값 폭등 — 재료 구매가 +35% (2일)`(Warning Plum), Day 3 Night `[지속]` 표기, 단체
  예고는 Brass Amber, 손상 데이터는 오류 문구 — 색+문구 병용.
- [ ] SNS 집행 후 잔액 경고가 `forecast.NextDayOperatingCost` 기반으로 표시된다(임대료 인상 활성 후
  13,800원 정확 표시 — 고정 12,000원 하드코딩 제거).
- [ ] Market 확정 상세에 폭등 `오늘: 재료값 폭등 +35%`·단체 `단체 +1팀(4인) 예정`이 fx/plan 값으로
  표시되고(UI 재계산 금지), 재료 행 예상가·구매 결과·정산 재료 지출이 모두 같은 인상 단가를 사용한다.
- [ ] HUD badge가 F3 규칙(기본/SNS/단체/동시)대로 표시되고 day 전환 후 재계산된다.
- [ ] Service 패널·무대 라벨이 단체 주문에만 `단체 손님`/`단체` 표기를 붙이고 표현 이벤트가
  `EventInflow`를 전달한다(기존 생성자 경로는 false).
- [ ] Settlement `이벤트:` 원인 라인이 F5 형식·ID ordinal 순서로 활성 이벤트만 표시하고(할증 0원 포함
  정직 표시), 활성 2종 이하 전체 포맷·3종 이상 축약 포맷으로 결정론 분기하며, 없으면 빈 문자열이고,
  정산 수학·day 멱등성은 기존 그대로다. controller는 state 원시 필드를 필터링 없이 전달한다(C3
  시그니처 — `marketSpendDay`/`marketEventSurchargeToday` 원본 전달).
- [ ] **stale 할증 차단**: `marketSpendDay != fx.Day`인 상태에서 `marketEventSurchargeToday`에 전일
  잔존값(예: 5,000)이 남아 있어도 `BuildSettlementCauseLine`이 폭등 항목을 `-0원`으로 표시한다 —
  단일 원천 내부 필터의 직접 검증(B2 리셋 테스트와 별개의 stale-day 케이스).
- [ ] **worst-case 길이 자동 검증**: 전체 포맷 최악 2종(단체+폭등, 실측 39자)·축약 포맷 4종 동시(실측
  56자) 고정 벡터 문자열이 정확히 일치하고 각각 상한 42자/60자 이내이며, 씬 테스트에서 두 worst-case
  문자열의 Galmuri TMP 10pt `GetPreferredValues` 폭이 460px 이하다.
- [ ] Unity 배치 compile 종료 코드 0·`error CS` 없음, EditMode 전체 종료 코드 0(기준선 236 + 신규,
  갱신 기대값 포함 무실패), PlayMode 전체 종료 코드 0(기존 4 + 신규)이다.
- [ ] `git status --short game`에 `Library/Temp/Obj/Logs/UserSettings/Build*`가 없고 신규 파일 전부에
  `.meta`가 존재한다.
- [ ] 640×360 원본 캡처를 Codex가 검토해 Night 레이아웃 v2·Settlement 원인 라인이 겹침·이탈 없이 좌표·
  폰트·카피와 일치함을 승인한다 (Claude self-approve 금지).
- [ ] 수동 Play smoke(오너/Codex): H16 인과 사슬(무이벤트 → 폭등 예고 → 인상 구매가 → 정산 원인,
  단체 예고 → +1건 → 태그 → 정산 +금액)과 Night 판단 60초 이내를 확인한다.

## 오픈 이슈 (Open Issues)

- **이벤트 시드 재확정 오너 승인**: 위생 80,000→8,000원, 단체 flat 6→4(파티 크기 규약 전환)와 문구
  2건 교체는 G절 가드에 근거한 설계 확정값이지만 기존 asset 값 변경이므로 Codex 교차검토와 오너 승인을
  거쳐야 한다(task-111 SNS 비용 재시드 전례). 거부되면 스케줄·합성 공식이 아니라 값 재협상으로 처리한다
  (공식은 값과 독립).
- **고정 스케줄의 재플레이 변주 부재**: 스케줄이 day의 순수 함수(시드 47 고정)라 모든 run이 같은
  이벤트 달력을 가진다. 3일 데모에서는 "예고→학습" 가독성이 장점이지만, 장기 재플레이 변주가 필요하면
  task-113 저장 스키마에 run 시드 필드를 추가하고 task-115 밸런싱에서 시드별 가드를 재검증해야 한다.
  이번 task에서 선제 도입하지 않는 이유: 저장 스키마와 가드 방법론을 함께 바꾸는 결정이라 분리한다.
- **DayModifier(수요 축) vs EventDayEffects(경제 축) 분리**: 지시서의 "이벤트 효과는 DayModifier로
  합성"을 수요 축에 한정 적용하고 재료값/운영비는 별도 DTO로 전달했다(D2 결정 기록 — Economy/Settlement가
  SNS 투영 입력에 결합되는 것을 방지). 두 DTO가 같은 `activeEvents`에서 `EventOps` 단일 원천으로
  파생되므로 결정론·일관성은 동일하다. Codex 교차검토에서 단일 DTO 통합이 낫다고 판단되면 D1/D2만
  국소 재설계하면 된다(합성 수학은 불변).
- **위기 예고와 대응 수단의 비대칭**: 예고가 Night(그날 Market 이후)에 오므로 폭등을 "미리 사재기"로
  회피할 수 없다(재고는 이월되지만 예고 시점엔 이미 장보기가 끝났다). 데모는 "적게 사서 버티기"만으로
  회복 가능하도록 밸런스했지만(G절), 예고를 하루 더 앞당기는 대응 전략화는 task-115에서 재검토한다.
- **정산 원인 라인 길이 (설계에서 확정됨 — Codex 설계 리뷰 반영)**: 3종 이상 동시 활성 시 축약 포맷으로
  결정론 분기하고 worst-case 문자 상한(42자/60자)과 Galmuri TMP `GetPreferredValues` 폭 460px 이하를
  자동 검증한다(F5). 레이아웃 리스크는 닫혔고, 시각 승인 게이트에는 축약 카피의 톤 확인만 남는다.
- **HUD badge 동시 표기 길이**: `{base}+{b}+1건(SNS·단체)`는 150×30 badge에서 빠듯하다. Codex 리뷰에서
  넘치면 badge 폭 확장 대신 카피 축약을 우선한다(레이아웃 연쇄 변경 방지).
- **EventManager singleton 보류**: 브리프 매니저 8종 목록에 Event가 있으나 task-110 G2 "기존 매니저
  얇은 확장" 원칙과 task-111 SNSManager 보류 전례에 따라 `GameManager`(catalog·경계 전이)+순수
  `EventOps`로 구현한다. task-113 저장에서 책임이 비대해지면 분리를 재평가하고 SSOT와 정합시킨다.
- **마지막 밤 예고의 낭비**: 3일 데모의 Day 3 Night 예고는 Day 4에 적용되어 현재 무한 진행 데모에선
  유효하지만, task-115 엔딩(3일 결과 카드) 도입 시 마지막 밤 예고 표시 정책(숨김 또는 "데모 종료" 문구)
  을 함께 결정해야 한다(task-111 마지막 밤 SNS 이슈와 동일 계열).
- **day ≥ 3 기존 테스트 기대값 갱신**: 이벤트 상시 적용으로 GameManager 경로 day 3+ 테스트의 경제
  수치가 바뀐다(의도된 변화). H2/H7에서 전수 식별·갱신하고 구현 노트에 전/후를 기록하지만, 갱신 범위가
  예상(FirstPlayableLoopTests 중심)을 크게 넘으면 스케줄 문턱 재조정 대신 Codex와 범위를 재협의한다.
- **`ClientIsKing.Events` 네임스페이스**: `UnityEngine.Events`와 파일 내 동시 using 시 모호성이 생길 수
  있어 alias 규약을 둔다(순수 계층은 무관, manager/UI에서만 발생 가능). 구현 중 충돌이 잦으면
  `ClientIsKing.Obstacles`로의 개명을 리뷰에서 결정한다(이 문서의 계약은 폴더/타입명 기준으로 동일).
- **단체 손님과 고객 파티 범위의 예외**: 파티 4 override는 직장인(max 2)·어르신(max 2)에게 archetype
  범위 밖 값이다. "회식 단체" 픽션으로 정당화하고 태그로 구분하지만, 고객별 단체 서사(가족 6인 등)가
  필요해지면 1.0 고객 확장에서 archetype별 단체 규칙으로 재설계한다.
