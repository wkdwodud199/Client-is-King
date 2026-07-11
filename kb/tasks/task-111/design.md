# 설계 문서 — task-111: SNS 마케팅 시스템

> Status: ready
> Inputs: `kb/concepts/project-brief.md`(SSOT — SNS 채널 3종×연령·성별 타겟, 익일 손님 수·분포 변화, 수확체감), `kb/concepts/demo-scope.md`(하드캡 — SNS 3종, 씬 2개), `kb/concepts/development-priority.md`(SNS = 지연 보상, 유입 태그로 인과 증명), `kb/tasks/task-110/design.md`(D6·D9·G2·G3 계약 — DayModifier 분리 훅, 결정론 수학), `kb/tasks/task-110/implementation-notes.md`(구현 기준선 — EditMode 152/PlayMode 2, `9265da5`), `game/Assets/Data/Definitions/SNS/*.asset`(photo_feed·short_form·local_board), 현재 `game/` 코드 기준선(`GenreSelectionOps`/`GenreDemandPlan`/`ServiceOps`/`ServiceManager`/`GameManager`/`NightPanelController`/`SceneBuilder`/`InitialDataBuilder`)
> Outputs: task-111 구현 계약 — Night phase SNS 캠페인 집행(1밤 1회), 밀리 정수 결정론 수학(도달·감쇠·보너스 주문·팔로워), `DayModifier` 순수 합성으로 익일 `GenreDemandPlan` 확장, `SNS 유입` 주문 태그와 정산 원인 라인, SNS 비용 시드 재확정, Night UI 좌표·카피, EditMode/PlayMode 테스트 계약
> Next step: Claude가 scaffold `manifest.md`의 placeholder를 이 문서의 Inputs·영향 파일로 채운 뒤 U1~U8 순서로 구현하고, 배치 compile·EditMode·PlayMode 게이트를 통과시킨다. Codex 코드 리뷰와 오너 640×360 시각 승인·수동 Play smoke 게이트 후 `task-112`(이벤트/장애물)로 진행하며, 전체 3일 수직 슬라이스 게이트는 task-112 이후 수행한다.

## 목표 (Objective)

SNS 마케팅을 **"내일의 군중을 설계하는 지연 보상"**으로 구현한다. 플레이어는 Night phase에서 가상 채널
3종(픽쳐그램/숏핑/동네게시판) 중 하나를 골라 비용을 내고, 그 효과는 **다음 날** 주문 수 증가와 고객 분포
변화로 돌아오며, 유입된 손님에게 `SNS 유입` 태그가 붙어 "어제 내 선택이 오늘 이 손님을 데려왔다"는 인과가
화면에서 증명된다. 같은 채널을 반복하면 수확체감으로 유입이 줄고, 채널마다 강한 고객층이 달라 장르 선택과
결합된 두 번째 의미 있는 경영 판단이 된다.

기술적으로는 task-110이 예약해 둔 `DayModifier` 분리 훅(D6/G2)을 실체화한다. SNS 효과는 순수 C# Ops가
`GameState.snsCampaignHistory`에서 결정론적으로 재구성한 `DayModifier` DTO로 계산되고,
`GenreSelectionOps.TryBuildDemandPlan`이 이를 익일 plan에 합성한다. `ServiceOps`는 SNS 매니저를 전혀
모르며, 저장 후 재개해도 같은 입력에서 같은 plan이 재생성된다. 기존 neutral 경로(장르만 적용된 plan)는
회귀 없이 보존한다.

### 역할과 결정권

| 영역 | 결정권자 | 수행자 | 게이트 |
|------|----------|--------|--------|
| SNS 수학·감쇠·시드 값 | 이 설계(Claude 초안) → Codex 교차검토, 오너 최종 승인 | Claude 구현 | design validator + 밸런스 가드 테스트 |
| Night UI 좌표·카피·색 | **Codex**(이 문서 F절은 제안 초안) | Claude가 SceneBuilder/UI로 구현 | Codex 640×360 화면 리뷰 |
| 코드 구조·테스트·배치 자동화 | 설계 계약 내 Claude | Claude | 컴파일·EditMode·PlayMode·멱등성 게이트 |
| 설계와 다른 화면·수치 판단 | Codex 재설계 요청 | Claude가 임의 확정하지 않음 | 새 design/review 기록 |

이 문서는 Claude가 작성한 설계 초안이며 Codex 설계 교차검토를 전제로 한다. 구현 중 문구·레이아웃·수치가
불명확하면 임의 확장하지 않고 리뷰 대상으로 남긴다.

## 범위 (Scope)

### 포함 — task-111 구현 범위

- Night phase에서 SNS 캠페인 3종 중 **하나만**(1밤 1회) 선택·집행한다. 집행은 즉시 현금을 차감하고
  `GameState.snsCampaignHistory`에 결정론 레코드를 남긴다. 건너뛰기(집행 없이 다음 날)는 항상 허용한다.
- 집행 효과는 **다음 날(집행일+1)** 에만 적용된다: 장르 기본 주문 수(4~6)에 SNS 보너스 주문(0~2)이 붙고,
  보너스 주문의 고객은 채널 타겟 친화 배수가 곱해진 가중치 테이블에서 결정론적으로 뽑힌다.
- 보너스 주문(=유입 손님)에 `snsInflow` 태그를 붙이고 Service 패널·무대 연출·정산 원인 라인에
  `SNS 유입`으로 표시해 인과를 증명한다.
- 같은 캠페인 반복 사용 시 도달이 감쇠(수확체감)해 보너스 주문·팔로워 획득이 줄어든다. 감쇠는 run 내
  누적이며 회복 규칙은 이번 task에 없다.
- follower는 별도 화폐가 아니라 **표시값**이다: `120 + Σ(집행 레코드의 followerGain)`. Night 패널에만
  표시하고 어떤 게임 규칙에도 입력되지 않는다.
- `DayModifier` 순수 DTO를 신설하고 `GenreSelectionOps.TryBuildDemandPlan`에 modifier overload를 추가한다.
  기존 4-입력 overload는 `DayModifier.Neutral(day)` 위임으로 하위호환한다. base 주문 구간(인덱스
  `0..BaseOrderCount-1`)의 고객 pick은 SNS 유무와 무관하게 동일하다(base-prefix 불변).
- `ServiceManager`를 얇게 확장한다: SNS 캠페인 catalog 주입, 집행/미리보기 API, plan 빌드 시 history →
  modifier 합성. `GameEvents.SNSCampaignExecuted` 이벤트를 추가한다.
- 기존 SNS 데이터 asset 3종의 **비용만** 경제 검증값으로 재시드한다(도달·감쇠·친화 배수·문구는 유지).
- Night 패널에 SNS UI(팔로워·버튼 3종·결과/경고 라인)를 SceneBuilder 코드 저작으로 추가하고, HUD
  badge·Market 상세·Settlement에 SNS 효과 표시를 추가한다.
- EditMode 결정론·게이트·밸런스·씬 회귀 테스트와 PlayMode 통합 테스트를 추가한다.

### 제외 — task-111에서 구현하지 않음

- 이벤트/장애물 4종(`task-112`), 저장/불러오기(`task-113`), 아트 마감(`task-114`), 밸런싱·엔딩·빌드(`task-115`).
- 요리대회, 장슐랭 트랙, 푸드트럭 규칙(task-110 GDD의 post-demo 설계 — 이번 구현과 무관).
- 실제 SNS 플랫폼 명칭·로고·UI 모사. 기존 가상 채널명(픽쳐그램/숏핑/동네게시판)만 사용한다.
- 신규 씬, 신규 singleton manager, 외부 DI/ECS/이벤트버스/tween 라이브러리. 브리프의 매니저 8종 목록에
  `SNSManager`가 있으나 이번 task는 task-110 G2의 "기존 매니저 얇은 확장" 원칙에 따라 `ServiceManager`
  확장으로 구현한다(오픈 이슈 참조).
- fame/reputation 시스템 본체(task-110 D8 후속). 이번 task의 follower는 SNS 누적 도달에서 파생한 표시값
  stand-in이며, fame 필드를 `GameState`에 추가하지 않는다.
- 하룻밤 다중 캠페인 집행("SNS 광고 몰아쓰기"는 task-110 D14의 후속 아이디어), 감쇠 회복 규칙, 캠페인
  예약/취소, follower 마일스톤 보상.
- SNS 도달률·감쇠율·타겟 친화 배수 asset 값 변경(비용만 재시드). 성별 분리 고객 archetype 신설.
- B급 재노출, 고객 인내 타이머, 품질 만족 수학(task-110 D3 계약 유지 — C급 고정).

## 제약 (Constraints)

- `kb/concepts/project-brief.md`가 SSOT, `kb/concepts/demo-scope.md`가 하드캡이다: SNS 채널 3종 고정,
  씬은 `MainMenu.unity`+`Shop.unity` 2개, 이벤트·장르 수 불변, 아트는 CC0/OFL placeholder만.
- Unity 6 `6000.3.8f1`, 2D URP, 640×360 Pixel Perfect, PPU 32, TMP Dynamic Galmuri를 유지한다. UI·무대는
  `SceneBuilder.Apply` 코드 저작이며 수동 씬 편집을 정본으로 삼지 않는다.
- 정적 데이터는 ScriptableObject, 런타임 상태는 순수 C# `GameState`(문자열 ID + List, Dictionary·SO 직접
  참조 저장 금지, `JsonUtility` 호환)를 유지한다. `SNSCampaignRecord`는 public 필드의 `[Serializable]`
  클래스다.
- 순수 규칙은 `*Ops`에, Unity lifecycle/SO 참조는 manager/controller에 둔다. `SNSCampaignOps`는 Unity
  타입을 참조하지 않으며 SO 투영 DTO만 입력받는다(단, `ClientIsKing.Data`의 순수 enum `AgeBand`/
  `GenderTarget`은 기존 `InventoryOps`의 `IngredientKind` 사용과 같은 수준에서 허용).
- **결정론**: 같은 입력이면 같은 결과. `System.HashCode`·런타임 가변 문자열 hash·프레임 난수·`Math.Pow`
  금지. 고객 roll은 task-110의 UTF-8 32-bit FNV-1a(`genreId|day|orderIndex`, offset `2166136261`, prime
  `16777619`, unchecked)를 그대로 재사용하고 시드 형식을 바꾸지 않는다. SNS 수학은 SO float 필드를
  **한 번만** 밀리 정수로 투영한 뒤 전 구간 정수 연산한다(C1).
- 잘못된 정의·상태는 **명시적 실패**다(neutral fallback으로 조용히 진행 금지). 실패 경로는 `GameState`를
  완전히 불변으로 유지한다. 기존 공개 API는 neutral overload로 하위호환하고 task-110 기준선(EditMode
  152/PlayMode 2)을 회귀 없이 보존한다.
- `ServiceOps`/`GenreSelectionOps`는 SNS 매니저·SO를 직접 찾지 않는다(task-110 G2). SNS 효과는
  `DayModifier`로 고정되어 전달되며, 저장 후 재개 시 `snsCampaignHistory`에서 동일 modifier가 재생성된다.
- 반올림은 task-110 공통 helper `RoundHalfUp(x) = floor(x + 0.5)`만 사용한다. 신규 정수 helper
  `MulMilliHalfUp(a, b) = (a × b + 500) / 1000`(long 중간값, 비음수 전제)은 RoundHalfUp(a×b/1000)의 정수
  동치로 정의한다.
- 통화는 기존 원화 정수 계약을 유지한다. SNS 비용 재시드도 원 단위 정수다.
- UI는 색만으로 상태를 전달하지 않는다(문구·outline 병용). Night 핵심 버튼은 키보드 focus 순서를 갖는다.
  하루 시작/종료 장면 60초 예산(task-110 B5)을 Night SNS 추가 후에도 지킨다.
- `game/ProjectSettings/`·`game/Packages/`·`.meta`는 버전 관리 대상이며 에셋과 `.meta`는 쌍으로 처리한다.
  `InitialDataBuilder`는 GUID 보존 upsert만 사용한다(삭제/재생성 금지).

## 구현 단계 (Implementation Steps)

### A. 플레이어 경험 — 하루 인과 루프

1. **Night(Day N)**: 정산을 마친 잔액으로 캠페인 1종을 고르거나 건너뛴다. 버튼에 비용·주 타겟·예상
   유입(+N팀)이 미리 보이고, 집행 즉시 현금이 줄고 "내일 SNS 유입 +N팀 예상" 확정 문구와 팔로워 증가가
   표시된다.
2. **Market(Day N+1)**: HUD badge와 상세 보기에 `주문 {base}+{bonus}건(SNS)`이 보여 어제 선택이 오늘
   수요를 바꿨음을 장보기 전에 알 수 있다(재료를 더 사야 할 이유).
3. **Service(Day N+1)**: 뒤쪽 {bonus}건 주문의 손님에 `SNS 유입` 태그가 붙는다. 채널 타겟에 맞는
   고객층이 더 자주 온다(예: 숏핑 → 학생).
4. **Settlement(Day N+1)**: 원인 라인 `SNS(숏핑): 어제 12,000원 → 유입 2/2팀 · 매출 +24,675원`으로 지연
   보상이 정산된다. 유입 주문을 놓치면 `유입 1/2팀`처럼 낭비가 정직하게 보인다.
5. **반복**: 같은 채널을 연달아 쓰면 예상 유입이 +2팀 → +1팀으로 줄어드는 것이 집행 전 버튼에서 미리
   보인다(수확체감 학습). 채널마다 강한 고객층·비용·감쇠 속도가 달라 로테이션과 장르 궁합을 고민하게 된다.

### B. 데이터·상태 계약

#### B1. SNSCampaignDef 시드 재확정 (비용만 변경)

기존 `game/Assets/Data/Definitions/SNS/*.asset`의 비용 50,000/40,000/25,000원은 task-103 시점의 경제
미검증 placeholder다. 시작 자금 30,000원·일 순이익 25,000~34,000원(task-110 실측) 기준으로 집행 자체가
불가능하거나 항상 손해라서, G절 밸런스 가드를 통과하는 아래 값으로 **비용만** 재시드한다(`InitialDataBuilder`
GUID 보존 upsert). 도달률·감쇠율·친화 배수·표시명·설명은 기존 asset 값을 그대로 유지한다.

| id | 표시명 | channel | baseCost (기존→확정) | baseReach | repeatDecay | audienceAffinities (유지) |
|----|--------|---------|---------------------:|----------:|------------:|---------------------------|
| `photo_feed` | 픽쳐그램 | PhotoFeed | 50,000 → **15,000** | 0.25 | 0.85 | (20대,여,1.5) (30-40대,여,1.2) (10대,전체,1.1) |
| `short_form` | 숏핑 | ShortForm | 40,000 → **12,000** | 0.30 | 0.80 | (10대,전체,1.6) (20대,전체,1.3) |
| `local_board` | 동네게시판 | LocalBoard | 25,000 → **7,000** | 0.15 | 0.90 | (50대+,전체,1.5) (30-40대,전체,1.25) |

구현 중 임의 재밸런싱을 금지한다. 이 표가 G절 가드와 함께 승인 seed다.

#### B2. GameState 확장 (JsonUtility 호환)

```csharp
// GameState.cs 추가 필드 (전부 public, List/문자열/int — Dictionary·SO 참조 없음)
public List<SNSCampaignRecord> snsCampaignHistory = new List<SNSCampaignRecord>();

// Service 당일 SNS 인과 통계 (StartServiceDay 가 리셋, 서빙/포기가 갱신)
public int serviceSnsOrdersServedToday = 0;
public int serviceSnsOrdersMissedToday = 0;
public int serviceSnsRevenueToday = 0;
```

#### B3. SNSCampaignRecord (신규 `Runtime/Social/SNSCampaignRecord.cs`)

```csharp
[Serializable]
public sealed class SNSCampaignRecord
{
    public string campaignId = "";      // SNSCampaignDef.Id (문자열 ID 규약)
    public int executedOnDay = 0;       // 집행한 날 (효과는 executedOnDay + 1 에 적용)
    public int costPaid = 0;            // 집행 시점 실제 차감액
    public int effectiveMilliReach = 0; // 집행 시점 감쇠 적용 도달 (C2)
    public int bonusOrderCount = 0;     // 집행 시점 확정 보너스 주문 수 0~2 (C3)
    public int followerGain = 0;        // 집행 시점 팔로워 획득 (C3)
}
```

레코드는 집행 시점의 **약속을 고정**한다: 익일 plan은 레코드의 `bonusOrderCount`를 그대로 사용해
"집행 시 예고한 유입 = 실제 유입"을 보장하고, def 값이 도중에 바뀌어도 약속이 깨지지 않는다.
저장값과 def 재계산의 일치는 테스트가 검증한다(1밤 1회 규칙 때문에 `executedOnDay`는 순증가한다).

#### B4. ServiceOrderState 태그

```csharp
// ServiceOrderState.cs 추가 필드
public bool snsInflow; // 이 주문이 SNS 보너스 유입인가 (기본 false — 기존 저장/테스트 하위호환)
```

### C. 결정론 수학 계약 (전 구간 정수)

#### C1. 밀리 투영 규약 — float은 경계에서 한 번만

SO의 float 필드(`baseReach`/`repeatDecay`/`multiplier`)는 투영 DTO 생성 시 **단 한 번**
`RoundHalfUp((double)floatField × 1000.0)`으로 밀리 정수화하고, 이후 모든 수학은 정수로만 한다
(부동소수 반복 곱·`Math.Pow` 금지 — 플랫폼·런타임 간 비트 동일성 보장).

| 채널 | reachMilli₀ | decayMilli | affinityMilli (매칭 archetype 기준) |
|------|------------:|-----------:|--------------------------------------|
| photo_feed | 250 | 850 | office_worker 1500 · family_parent 1200 · student 1100 |
| short_form | 300 | 800 | student 1600 · office_worker 1300 |
| local_board | 150 | 900 | senior_regular 1500 · family_parent 1250 |

정수 helper: `MulMilliHalfUp(a, b) = (int)((a * (long)b + 500) / 1000)` — 비음수 입력 전제(검증이 보장),
`RoundHalfUp(a × b / 1000)`과 동치.

#### C2. 반복 감쇠 체인 (수확체감)

`priorUses` = 현재 run의 `snsCampaignHistory`에서 같은 `campaignId` 레코드 수(이번 집행 이전).

```text
reachMilli(0)  = RoundHalfUp(baseReach × 1000)
reachMilli(n)  = MulMilliHalfUp(reachMilli(n-1), decayMilli)   // n ≥ 1, 정수 fold
```

승인 감쇠 체인(테스트 고정 벡터):

| 채널 | n=0 | n=1 | n=2 | n=3 | n=4 | n=5 | n=6 | n=7 |
|------|----:|----:|----:|----:|----:|----:|----:|----:|
| photo_feed | 250 | 213 | 181 | 154 | 131 | 111 | 94 | 80 |
| short_form | 300 | 240 | 192 | 154 | 123 | 98 | 78 | 62 |
| local_board | 150 | 135 | 122 | 110 | 99 | 89 | 80 | 72 |

#### C3. 보너스 주문 수·팔로워 획득

```text
bonusOrderCount(n) = clamp( (6 × reachMilli(n) + 500) / 1000 , 0, 2 )   // 정수 나눗셈
followerGain(n)    = (reachMilli(n) + 5) / 10                            // RoundHalfUp(reach/10)
follower 표시값     = 120 + Σ history.followerGain                        // 표시 전용, 규칙 입력 아님
```

승인 결과 벡터: 보너스 — photo `2,1,1,1,1,1,1,0` / short `2,1,1,1,1,1,0` / local `1,1,1,1,1,1,0`
(n=0부터). 팔로워 — 첫 집행 photo 25 / short 30 / local 15, 이후 감쇠 체인을 따른다(예: photo 2회차 21).
하루 총 주문 하드캡은 `base(4~6) + bonus(0~2) ≤ 8`이다.

#### C4. 연령·성별 타겟 매칭 (결정론 규칙)

고객 archetype `c`에 대한 채널 친화 배수 `snsAffinityMilli(c)`:

1. **매칭 조건**: `row.AgeBand == c.AgeBand` **그리고** `(row.Gender == All ∨ c.Gender == All ∨
   row.Gender == c.Gender)`.
2. 매칭 행이 없으면 `1000`(중립). 매칭 행이 있으면 **매칭 행 affinityMilli의 최댓값**.
3. def 안에 같은 `(AgeBand, Gender)` 쌍이 중복되면 **명시적 실패**(집행·plan 모두).

현재 데모 archetype 4종은 전부 `Gender == All`이므로 픽쳐그램의 여성 타겟 행도 규칙 1에 의해 해당
연령대 전체에 매칭된다(성별 축은 데이터에 존재하나 현 archetype 구성에서는 무차별 — 오픈 이슈 기록).
채널별 **주 타겟 상위 2종** = `snsAffinityMilli(c) > 1000`인 고객을 배수 내림차순, 동률은 customer ID
ordinal 오름차순: photo=`office_worker, family_parent` / short=`student, office_worker` /
local=`senior_regular, family_parent`. 이 상위 2종 계산은 customer 투영 입력을 받는 순수 계층
(`SNSCampaignOps.TryBuildPreview`, E1)의 단일 경로다 — UI·매니저 재계산 금지.

#### C5. 고정 검증 벡터 (known vectors)

- FNV-1a 재확인: `"gukbap|1|0"` → `2190636514` (task-110 벡터 유지 — 시드 형식 불변 증명).
- 신규: `"bunsik|2|6"` → `1202351915`, `"bunsik|2|7"` → `1185574296`.
- 분식×숏핑 보너스 풀(C4·D2 합성, ID ordinal 정렬): family 900 / office 1716 / senior 420 / student
  2400, 합 5436. roll(`bunsik|2|6`) = `1202351915 % 5436` = **1127** → 누적 900<1127≤2616 →
  `office_worker`. roll(`bunsik|2|7`) = **4440** → 누적 3036<4440≤5436 → `student`.

### D. DayModifier 합성 계약

#### D1. DayModifier DTO (신규 `Runtime/Genre/DayModifier.cs`, 순수 C#)

```csharp
public sealed class DayModifier
{
    public int Day { get; }                    // 적용 대상 일차 (plan day 와 일치해야 함)
    public string SourceCampaignId { get; }    // "" = 중립 (SNS 소스 없음)
    public int BonusOrderCount { get; }        // 0~2
    public IReadOnlyList<CustomerWeightBoost> WeightBoosts { get; } // (customerId, boostMilli≥1)
    public static DayModifier Neutral(int day); // bonus 0, boosts 빈 목록
}
public readonly struct CustomerWeightBoost { string CustomerId; int BoostMilli; } // 1000 = 중립
```

- `WeightBoosts`가 **빈 목록**이면 전 고객 중립(1000)으로 간주한다(Neutral 전용 규약).
- 비어 있지 않으면 고객 archetype 전원을 **정확히 1회씩** 커버해야 한다 — 누락·중복·미지 ID는 plan
  생성의 명시적 실패다(부분 적용으로 조용히 진행 금지).
- task-112 이벤트 modifier는 이 DTO에 합성 소스를 추가·병합하는 별도 설계로 확장한다(오픈 이슈).

#### D2. TryBuildDemandPlan 확장 (`GenreSelectionOps`)

```csharp
// 신규 overload — 기존 4-입력 overload 는 DayModifier.Neutral(day) 위임으로 결과 불변
public static bool TryBuildDemandPlan(
    GenreDefInput genre, int day,
    IReadOnlyList<RecipeDefInput> recipes, IReadOnlyList<CustomerDefInput> customers,
    DayModifier modifier, out GenreDemandPlan plan, out string failReason)
```

- 검증(모두 명시적 실패, 상태 불변): `modifier == null`, `modifier.Day != day`,
  `BonusOrderCount ∉ [0,2]`, boost 커버리지 위반(D1), `BoostMilli ≤ 0`.
- 기존 장르 검증·정렬·milli-weight 계산(task-110 D6)은 그대로 선행한다.
- **보너스 풀**: `bonusMilli(c) = MulMilliHalfUp(genreMilli(c), boostMilli(c))` — 장르 친화와 채널
  타겟이 곱으로 결합. 보너스 풀 합이 0이면 명시적 실패.
- `GenreDemandPlan` 확장 필드: `BaseOrderCount`(장르 공식값), `BonusOrderCount`,
  `OrderCount = BaseOrderCount + BonusOrderCount`(기존 프로퍼티 의미 유지 — neutral 에서 기존 값과
  동일), `BonusCustomerWeights`(ID ordinal 정렬), `SourceCampaignId`. 기존 생성자는 neutral 값으로
  위임 보존하고, "동일 plan" field-by-field 동등성 계약은 신규 필드까지 확장한다.
- `PickCustomerId(plan, i)`: `i < BaseOrderCount`면 기존 `CustomerWeights`, 아니면
  `BonusCustomerWeights`로 누적 구간 pick. 시드는 기존 그대로 `FNV-1a("genreId|day|i")` —
  **base-prefix 불변**: 같은 genre/day에서 SNS 유무와 무관하게 인덱스 `0..BaseOrderCount-1`의
  고객/레시피/파티가 완전히 동일하다.
- `PickRecipeId`/`PickPartySize`는 무변경 — 보너스 인덱스도 같은 round-robin/산식을 이어 쓴다.
- forecast `Min/MaxPricePerCustomer`·`TopCustomerIds`는 기존(base) 정의를 유지한다(SNS는 유입 표시로
  구분하지 상시 분포 예보를 바꾸지 않는다).

#### D3. 주문 생성·태그·통계 (`ServiceOps`)

- `BuildOrders(plan, customers)`: `orders[i].snsInflow = (i >= plan.BaseOrderCount)`. 보너스 주문은
  base 뒤에 append된다(유입 손님은 그날 뒤쪽에 온다 — 표현 단순화, 오픈 이슈에 기록).
- `StartServiceDay`: `serviceSns*Today` 3필드를 0으로 리셋한다.
- `TryServeCurrentOrder`(모든 overload 공통 내부 경로): 성공한 주문이 `snsInflow`면
  `serviceSnsOrdersServedToday++`, `serviceSnsRevenueToday += price`(동일 판매가 — 별도 계산 금지).
- `SkipCurrentOrder`: 포기 주문이 `snsInflow`면 `serviceSnsOrdersMissedToday++`.
- neutral 경로(태그 없는 주문)의 기존 수치·메시지는 완전 불변이다.

#### D4. history → modifier 재구성 (`SNSCampaignOps.BuildDayModifier`)

```csharp
public static bool TryBuildDayModifier(
    IReadOnlyList<SNSCampaignRecord> history, int day,
    IReadOnlyList<SNSCampaignDefInput> campaignDefs, IReadOnlyList<CustomerDefInput> customers,
    out DayModifier modifier, out string failReason)
```

- `executedOnDay == day - 1`인 레코드가 0건이면 `Neutral(day)` 반환(성공).
- 1건이면: 레코드의 `campaignId`를 defs에서 조회(없으면 실패), `bonusOrderCount ∉ [0,2]`면 실패,
  C4 매칭으로 전 고객 boost를 만들어 modifier 구성. **레코드의 저장값을 사용**하고 재계산하지 않는다
  (B3 약속 고정 — 저장·재개 후에도 동일).
- 2건 이상이면 명시적 실패(1밤 1회 불변식 위반 — 손상 데이터).
- 이 함수는 순수하며 같은 입력에서 항상 같은 modifier를 만든다. 저장/불러오기(task-113)는
  `snsCampaignHistory` 직렬화만으로 plan 재현이 보장된다(JsonUtility 왕복 테스트로 검증).

### E. 집행 게이트·매니저 배선

#### E1. SNSCampaignOps.TryExecute (순수, 신규 `Runtime/Social/SNSCampaignOps.cs`)

```csharp
public static SNSCampaignResult TryExecute(GameState state, SNSCampaignDefInput def)
```

검사 순서(모든 실패는 상태 완전 불변 + 한국어 사유):

1. `state == null` → `ArgumentNullException`(기존 Ops 관례).
2. def 무결성: `Id` 비어 있음 / `BaseCost ≤ 0` / `baseReach ∉ (0,1]`·NaN·Infinity /
   `repeatDecay ∉ (0,1]`·NaN·Infinity / affinity `multiplier ≤ 0`·NaN·Infinity / `(AgeBand,Gender)`
   중복 행 → 실패 "잘못된 SNS 캠페인 정의입니다."
3. `state.isBankrupt` → 실패 "파산 상태에서는 캠페인을 집행할 수 없습니다."
4. `state.currentPhase != DayPhase.Night` → 실패 "SNS 캠페인은 밤에만 집행할 수 있습니다."
5. `snsCampaignHistory`에 `executedOnDay == state.day` 레코드 존재 → 실패 "오늘 밤 캠페인은 이미
   집행했습니다."(채널 무관 1밤 1회)
6. `state.cash < BaseCost` → 실패 "자금이 부족합니다 (필요 {cost:N0}원, 보유 {cash:N0}원)."

성공 시 원자적으로: `cash -= BaseCost`; C2/C3로 `effectiveMilliReach`/`bonusOrderCount`/`followerGain`
계산(priorUses = 같은 id 기존 레코드 수); 레코드 append; 결과 DTO
`SNSCampaignResult{Success, Message, CampaignId, CostPaid, EffectiveMilliReach, BonusOrderCount,
FollowerGain, CashAfter}` 반환. 성공 메시지: `"{표시명} 집행 — 내일 SNS 유입 +{n}팀 예상 (팔로워 +{f})"`.

미리보기(집행 없이 상태 불변 — **top-target을 포함한 단일 non-UI 계산 경로**):

```csharp
public static bool TryBuildPreview(
    GameState state, SNSCampaignDefInput def, IReadOnlyList<CustomerDefInput> customers,
    out SNSCampaignPreview preview, out string failReason)
```

- 반환 DTO: `SNSCampaignPreview{CampaignId, Cost, EffectiveMilliReach, BonusOrderCount, FollowerGain,
  TopTargetCustomerIds, CanExecute, BlockReason}`.
- def 무결성 위반(위 게이트 2와 동일 검증)과 customers 무결성 위반(null/빈 목록/빈·중복 ID)은
  **명시적 실패**(`failReason`)다. 게이트 3~6에 걸리는 상태(파산/Night 아님/오늘 이미 집행/자금 부족)는
  실패가 아니라 `CanExecute=false + BlockReason`(게이트와 동일 규칙·문구)으로 구분해 반환한다.
- `TopTargetCustomerIds`는 **이 순수 helper가** C4 매칭을 customers 투영 입력에 적용해 결정론적으로
  계산한다: `snsAffinityMilli(c) > 1000`인 고객을 배수 내림차순, 동률은 customer ID ordinal 오름차순,
  상위 2종. UI·매니저는 이 목록을 재계산하지 않는다(task-110 G2 "UI가 SO 배수를 직접 계산하지 않는다"
  준수).
- 팔로워 표시 `CalculateFollowerDisplay(history) = 120 + Σ followerGain`도 순수 helper로 둔다.

#### E2. ServiceManager 확장 (thin wrapper — 유일한 SNS catalog 소유자)

- `[SerializeField] List<SNSCampaignDef> snsCampaignDefs` + read-only 노출 `SnsCampaignDefs`.
  `EditorInit(recipes, customers, snsCampaigns)` overload 추가(기존 2-인자 overload 보존),
  SceneBuilder가 MainMenu/Shop 양쪽에 ID ordinal 정렬 목록(`local_board, photo_feed, short_form`)을
  동일 주입한다.
- `internal static SNSCampaignDefInput ToSnsCampaignInput(SNSCampaignDef)` 투영(IVT로 테스트 노출).
  `ToCustomerInputs`는 `CustomerDefInput`에 신설되는 `AgeBand`/`Gender` 필드를 함께 채운다.
- `TryExecuteSnsCampaign(string campaignId, out SNSCampaignResult result)`: catalog에서 id 조회(미지
  id는 상태 불변 실패) → `SNSCampaignOps.TryExecute`.
- `TryGetSnsPreview(string campaignId, out SNSCampaignPreview preview, out string reason)`: catalog에서
  id 조회(미지 id 실패) → 자신의 def를 `ToSnsCampaignInput`으로, `CustomerDefs`를 `ToCustomerInputs`로
  투영해 `SNSCampaignOps.TryBuildPreview`에 전달하고 DTO를 그대로 반환한다(task-110 투영 패턴).
  top-target 계산은 순수 계층에만 존재하며 매니저·UI는 재계산하지 않는다.
- `TryBuildDayPlan(genre, out plan, out reason)` 내부: `SNSCampaignOps.TryBuildDayModifier(history,
  state.day, 투영 defs, 투영 customers, ...)` → 실패면 plan 실패 사유 전달, 성공이면 modifier overload로
  `TryBuildDemandPlan` 호출. `GameManager.CanAdvancePhase`/`AdvancePhase`/`TryStartServiceDay`는 무변경
  — 어제 레코드가 미지 캠페인을 참조하면 Market→Service가 명시적 사유로 차단된다(조용한 fallback 금지,
  손상 데이터 전용 경로).

#### E3. GameEvents

```csharp
public static event Action<string> SNSCampaignExecuted; // campaignId, 집행 확정 시 정확히 1회
internal static void RaiseSNSCampaignExecuted(string campaignId);
```

발행은 집행을 확정한 UI 경로(`NightPanelController`)가 1회 수행한다(task-108 규약 — 도메인 Ops는
발행하지 않는다). 구독: `PhaseHudController`(badge 갱신 대비), Night 자체 refresh.

#### E4. 의존 방향 (task-110 G2 준수)

```text
SNSCampaignDef / CustomerArchetypeDef (SO)          GameState.snsCampaignHistory
            └──── ServiceManager 투영 ────┐                  │
                                          ▼                  ▼
                              SNSCampaignOps (순수: 집행 게이트·감쇠·미리보기)
                                          │ TryBuildDayModifier
                                          ▼
                                    DayModifier ──→ GenreSelectionOps.TryBuildDemandPlan
                                                              │
                                                              ▼
                                    GenreDemandPlan(+Base/Bonus) ──→ ServiceOps.BuildOrders(snsInflow)
                                                              │
                                                              ▼
                                     Managers → UI (Night 집행 / Service 태그 / Settlement 원인)
```

`ServiceOps`·`GenreSelectionOps`는 SNS 매니저를 모르고, UI는 SO 배수를 직접 계산하지 않는다.

### F. UI/UX — Night 패널과 인과 표시 (Codex 리뷰 게이트)

#### F1. Panel_Night 레이아웃 (640×360, 패널 480×200 @ (0,−80) 유지)

기존 3개 텍스트를 재배치·축소하고 SNS 블록을 추가한다(로컬 y 범위 ±100, 상단 여백 7px·하단 12px 확보).

| 오브젝트 | anchoredPosition | sizeDelta | 폰트 | 내용 |
|----------|------------------|-----------|------|------|
| SummaryText (기존 이동) | (0, 84) | (440×18) | 13pt | `Day {n} 마감 — 잔액 {cash:N0}원` |
| DaysText (기존 이동) | (0, 66) | (440×14) | 10pt | `완료 일수 {n}일` |
| FollowerText (신규) | (0, 50) | (440×14) | 11pt | `팔로워 {follower:N0}명` |
| SnsTitleText (신규) | (0, 32) | (440×16) | 12pt | `SNS 캠페인 — 내일의 손님을 설계하세요` |
| Button_Sns_PhotoFeed (신규) | (−150, 0) | (140×46) | 라벨 2행 11pt | F2 카피 |
| Button_Sns_ShortForm (신규) | (0, 0) | (140×46) | 라벨 2행 11pt | F2 카피 |
| Button_Sns_LocalBoard (신규) | (150, 0) | (140×46) | 라벨 2행 11pt | F2 카피 |
| SnsInfoText (신규) | (0, −40) | (460×28) | 11pt | 안내/결과/경고 최대 2행 |
| StatusText (기존 이동) | (0, −72) | (460×32) | 11pt | 기존 다음 날/파산 안내 |

버튼 기본색 Night Blue `#253B56` + Steam Cream 라벨(장르 버튼 규약), 집행 완료 버튼만 Gochujang Red
`#D34A3A` outline 2px + 라벨 2행을 `집행 완료`로 교체. 버튼 세로 구간 −23~+23, SnsInfoText 상단 −26
(간격 3px), 가로 마진 20px·버튼 간 10px — 겹침 금지.

#### F2. 캠페인 버튼 카피 (Codex 소유 UX copy — 임의 수정 금지)

| 채널 | 라벨 1행 | 라벨 2행 (미리보기 연동) |
|------|----------|--------------------------|
| photo_feed | `픽쳐그램 15,000원` | `직장인·가족 · 내일 +{n}팀` |
| short_form | `숏핑 12,000원` | `학생·직장인 · 내일 +{n}팀` |
| local_board | `동네게시판 7,000원` | `어르신·가족 · 내일 +{n}팀` |

- `{n}`은 `TryGetSnsPreview`의 감쇠 반영 실시간 값(첫 집행 photo/short 2, local 1). `{n}==0`이면 2행을
  `피로 누적 · 내일 +0팀`으로 표시해 낭비를 사전 경고한다.
- 주 타겟 문구는 preview DTO의 `TopTargetCustomerIds`(순수 계층이 C4 규칙으로 계산 — E1) 순서를
  그대로 표시한다. UI는 ID→표시명 매핑(`ServiceManager.CustomerDefs` 조회)만 수행하며 친화 배수·
  가중치를 재계산하는 경로를 두지 않는다.
- interactable = `!오늘 집행됨 && !isBankrupt && cash ≥ cost`. 갱신 시점: `OnEnable`, 집행 직후,
  `SNSCampaignExecuted` 수신.
- SnsInfoText 기본: `밤마다 한 채널만 집행할 수 있습니다 · 잔액 {cash:N0}원`. 집행 후:
  `{표시명} 집행 완료 — 내일 SNS 유입 +{n}팀 · 팔로워 +{f} · 잔액 {cash:N0}원`. 집행 후 잔액이
  운영비(12,000원) 미만이면 Warning Plum `#A93E58`으로
  `내일 운영비 12,000원이 부족할 수 있습니다` 1행을 덧붙인다(색+문구 병용).
- 키보드 focus 순서: 픽쳐그램 → 숏핑 → 동네게시판 → `다음 날 ▶`(좌우 방향키·Tab). 집행은 되돌릴 수
  없으므로 버튼 1회 클릭 = 확정이며, 확정 전 2행 미리보기가 사전 고지 역할을 한다.

#### F3. Service·무대 태그 표시

- `ServicePanelController.customerText`: 태그 주문이면 `{고객 표시명} ×{party} · SNS 유입`.
- `ServicePresentationEventArgs`에 `SnsInflow` 필드 추가(기존 생성자는 false 위임 overload로 보존,
  `BuildOrderPresentedArgs`/`BuildOutcomeArgs`가 주문의 `snsInflow`를 전달).
- 무대 `ShopPresentationController.customerLabel`: `×{party}` 뒤에 태그 주문만
  ` <color=#4FAE82>SNS</color>`(Jade Green, TMP rich text)를 덧붙인다. 게임 규칙에는 관여하지 않는다.

#### F4. Settlement 원인 라인

`SettlementPanelController`에 `snsEffectText`(신규 TMP, `GenreEffectText` 아래 배치, SceneBuilder가
좌표 확정)를 추가한다. 어제(`executedOnDay == state.day - 1`) 레코드가 있으면:

```text
SNS({표시명}): 어제 {costPaid:N0}원 → 유입 {served}/{bonus}팀 · 매출 +{serviceSnsRevenueToday:N0}원
```

`served = serviceSnsOrdersServedToday`, `bonus = record.bonusOrderCount`. 유입 0팀(감쇠)도 그대로
표시한다(정직한 낭비 학습). 레코드가 없으면 빈 문자열. 표시명 해석 실패 시 campaignId를 그대로 쓴다
(표시 전용 fallback — 도메인 실패와 구분). `EditorInit`은 기존 시그니처 보존 overload로 확장한다.

#### F5. HUD·Market 표시

- `PhaseHudController.RefreshGenreBadge`: plan에 보너스가 있으면 `{장르} · 주문 {base}+{bonus}건(SNS)`,
  없으면 기존 `{장르} · 주문 {n}건`. 그리고 **`OnDayPhaseChanged`에서도 badge를 재계산**한다(현재
  Start/GenreSelected에서만 갱신되어 다음 날 주문 수가 낡은 값으로 남는 결함 보정 — task-111 필요 조건).
- `MarketPanelController` 확정 상태 상세(`genreDetailNumbersText`): plan `BonusOrderCount > 0`이면
  forecast 행 뒤에 ` · SNS 유입 +{bonus}팀 예정`을 덧붙인다(plan 값 사용 — UI 직접 계산 금지).

### G. 밸런스 가드 (승인 seed 근거)

방법론(task-110 D5 방법론 계승): day 2~101의 100개 결정론 day에 대해 "전날 밤 해당 채널을 집행했다"고
가정하고, **보너스 주문의 기여이익**(장르 적용 판매가 − 장르 적용 C급 실요구 재료 원가, 전량 서빙 가정)
합계에서 채널 비용을 뺀 값을 평균한다. 1회차는 `reachMilli(0)`, 2회차는 `reachMilli(1)` 기준.

| 장르 | 픽쳐그램 1회/2회 | 숏핑 1회/2회 | 동네게시판 1회/2회 |
|------|------------------:|------------------:|------------------:|
| 국밥 | +9,552 / −2,896 | +12,658 / +851 | +5,530 / +5,530 |
| 분식 | +1,341 / −6,686 | +3,644 / −4,055 | +1,068 / +1,068 |
| 면류 | +5,264 / −4,973 | +7,297 / −2,688 | +3,590 / +3,590 |
| 제네럴리스트 | +5,179 / −5,041 | +8,316 / −1,879 | +3,684 / +3,684 |

가드 조건(구현 테스트가 재유도로 검증, 임의 재밸런싱 금지):

1. **첫 집행 전 조합 양수·잭팟 없음**: 12조합(장르 4 × 채널 3)의 1회차 평균 net이 전부
   `+500원 ~ +14,000원` 안이다(실측 min 분식×동네게시판 +1,068 / max 국밥×숏핑 +12,658).
2. **수확체감 가시성**: 픽쳐그램·숏핑의 2회차 평균 net은 모든 장르에서 1회차보다 낮다(픽쳐그램 2회차는
   전 장르 손실 전환 — 로테이션 학습 유도).
3. **완만 감쇠 정체성**: 동네게시판은 2회차에도 보너스 1팀이 유지되어 평균 net이 1회차와 같고, 감쇠는
   팔로워 획득(15→14)과 장기 체인(7회차 +0팀)에서만 드러난다.
4. **재유도 허용 오차**: 구현 밸런스 테스트의 100-day 재유도 값은 위 표의 ±1% 안이어야 한다.
5. **주문 하드캡**: 어떤 조합에서도 하루 총 주문이 8건(분식 6 + 2)을 넘지 않는다.
6. **생존 가드**: 집행 게이트는 `cash ≥ cost`만 요구하고 잔액 하한을 강제하지 않는다 — 대신 F2의
   운영비 부족 경고가 사전 고지한다(위험 감수는 플레이어 몫, 파산 규칙은 task-107 그대로).

집행 판단의 긴장 구조: 채널 비용(7k/12k/15k)은 일 순이익(25k~34k)의 20~60%로, "내일을 사는" 투자가
항상 옳지도 그르지도 않게 한다. 분식은 이미 주문 6건 최다라 SNS 한계효용이 가장 낮고(+1.1k~3.6k),
국밥은 객단가가 높아 유입 가치가 가장 크다(+5.5k~12.7k) — 장르 선택이 SNS 전략까지 바꾼다.

### H. task-111 상세 구현 순서

1. 코드 변경 전 scaffold `manifest.md`의 placeholder를 이 문서의 Inputs·영향 파일·검증 명령으로 채운다.
2. 현재 기준선(EditMode 152/PlayMode 2)을 실행해 실제 결과를 구현 노트에 기록한다.
3. `GameState`에 `snsCampaignHistory`·`serviceSns*Today` 3필드를 추가하고, `ServiceOrderState`에
   `snsInflow`를 추가한다(B2/B4 — 기본값 하위호환).
4. `Runtime/Social/SNSCampaignRecord.cs`(B3)와 `Runtime/Social/SNSCampaignOps.cs`(C1~C4, E1: 투영
   DTO `SNSCampaignDefInput`/`SNSAffinityInput`, `MulMilliHalfUp`, 감쇠 체인, 보너스/팔로워, 매칭,
   `TryExecute`/`TryBuildPreview`(customer 투영 입력으로 top-target 계산)/`CalculateFollowerDisplay`/
   `TryBuildDayModifier`)를 만든다. 결과 DTO는
   `Runtime/Social/SNSCampaignResult.cs`(Result+Preview)로 둔다.
5. `Runtime/Genre/DayModifier.cs`(D1)를 만들고 `CustomerDefInput`에 `AgeBand`/`Gender` 필드를 추가한다.
6. `GenreDemandPlan`에 `BaseOrderCount`/`BonusOrderCount`/`BonusCustomerWeights`/`SourceCampaignId`를
   추가하고 기존 생성자를 neutral 위임으로 보존한다(D2).
7. `GenreSelectionOps.TryBuildDemandPlan` modifier overload와 `PickCustomerId` 분기(D2)를 구현한다.
   기존 4-입력 overload는 `DayModifier.Neutral(day)` 위임으로 결과를 유지한다.
8. `ServiceOps.BuildOrders`의 태그 부여와 `StartServiceDay`/`TryServeCurrentOrder`/`SkipCurrentOrder`의
   SNS 통계 갱신(D3)을 구현한다. neutral 경로 수치·메시지 불변을 확인한다.
9. `ServiceManager`에 SNS catalog 주입(EditorInit overload)·`ToSnsCampaignInput`·
   `TryExecuteSnsCampaign`·`TryGetSnsPreview`(def·customers를 투영해 `TryBuildPreview`에 전달)를
   추가하고 `TryBuildDayPlan`에 modifier 합성을 배선한다(E2).
10. `GameEvents.SNSCampaignExecuted`를 추가한다(E3).
11. `NightPanelController`에 SNS 블록(F1/F2 — 팔로워·버튼 3종 상태 계산·집행·결과/경고 라인·focus 순서)을
    구현하고 `EditorInit`을 기존 시그니처 보존 overload로 확장한다.
12. `ServicePanelController` 태그 표시(F3), `ServicePresentationEventArgs.SnsInflow` 확장,
    `ShopPresentationController` 무대 라벨(F3)을 구현한다.
13. `SettlementPanelController`에 `snsEffectText`(F4)를 추가한다.
14. `PhaseHudController` badge 확장 + `OnDayPhaseChanged` 재계산(F5), `MarketPanelController` 상세
    문구(F5)를 구현한다.
15. `InitialDataBuilder.BuildSNSCampaigns`의 비용을 B1 확정값(15,000/12,000/7,000)으로 GUID 보존
    upsert한다(다른 필드 불변).
16. `SceneBuilder`: Night 패널 재배치·SNS 오브젝트 생성·controller 참조 주입, `ServiceManager`에
    정렬된 SNS catalog를 MainMenu/Shop 양쪽 동일 주입, 2씬 재생성. 멱등성(연속 2회 Apply 오브젝트
    수·persistent listener 불변)을 유지한다.
17. EditMode 테스트: `SNSCampaignOpsTests`(투영·감쇠·보너스·팔로워·매칭·게이트·상태 불변·미리보기·
    modifier 재구성), `GenreSelectionOpsTests` 확장(modifier 검증·base-prefix 불변·C5 벡터·plan 동등성),
    `ServiceOpsTests` 확장(태그·통계·neutral 회귀), `ServiceManagerTests`류(catalog·집행·plan 합성·미지
    id 실패), `SNSBalanceTests`(G 가드 재유도), JsonUtility 왕복 plan 동일성.
18. 씬 테스트: `SceneBuilderTests` 확장(Night SNS 오브젝트·멱등), 신규 `NightPanelSceneTests`(구조)와
    `NightPanelSnsFlowTests`(상태 — 집행→버튼 잠금→문구, task-110의 씬 상태 격리 교훈에 따라 구조/상태
    fixture 분리), `SettlementPanelSceneTests`·`ServicePanelSceneTests`·`MarketPanelSceneTests` 확장.
19. PlayMode 테스트: MainMenu→Shop 로드 후 persistent `ServiceManager`가 SNS catalog 3종을 보유하고,
    Day 1 루프 → Night 집행 → Day 2 진입 시 plan `OrderCount == base+bonus`와 태그 주문 존재를
    UI 없이 도메인 경로로 검증한다.
20. `InitialDataBuilder.Apply → SceneBuilder.Apply → compile → EditMode → PlayMode` 순으로 배치
    검증하고 `git status --short game`에 산출물 오염이 없는지 확인한다.
21. 수동 Play smoke(오너/Codex 게이트): Night에서 60초 안에 캠페인 판단 가능, 집행 → 다음 날 Market
    badge `+N` → Service `SNS 유입` 태그 → Settlement SNS 라인의 인과 사슬, 반복 집행 시 `+2→+1팀`
    미리보기 감쇠, 잔액 부족 시 버튼 비활성을 확인한다.
22. 구현 노트·artifacts summary·`python3 runtime/generate-status.py` 재생성으로 기록을 마감한다.

## 실행 계획 (Execution Plan)

- implement_model: claude-fable-5
- implement_effort: xhigh
- routing_reason: 프로젝트 SSOT 로드맵이 task-111을 fable-5/xhigh로 고정 — 순수 수학·plan 합성·매니저·
  UI·SceneBuilder·시드·테스트를 관통하는 M2 지연 보상 시스템으로 결정론 계약 밀도가 task-110과 동급이다.

| unit | 파일 범위 | depends_on | group |
|------|-----------|------------|-------|
| U1-social-domain | `GameState`, `ServiceOrderState`, 신규 `SNSCampaignRecord/SNSCampaignResult/SNSCampaignOps`, 신규 `DayModifier` | 없음 | G1 |
| U2-plan-composition | `GenreDemandPlan`, `GenreSelectionOps`(modifier overload·pick 분기·`CustomerDefInput` 확장), `ServiceOps`(태그·통계) | U1-social-domain | G2 |
| U3-manager-wiring | `ServiceManager`(SNS catalog·집행·미리보기·plan 합성), `GameEvents` | U1-social-domain, U2-plan-composition | G3 |
| U4-ui-controllers | `NightPanelController`, `ServicePanelController`, `SettlementPanelController`, `PhaseHudController`, `MarketPanelController`, `ShopPresentationController`, `ServicePresentationEventArgs` | U3-manager-wiring | G4 |
| U5-scene-data-wiring | `SceneBuilder`(Night SNS UI·catalog 주입 최종 wiring 단독 소유), `InitialDataBuilder`(비용 재시드), 씬 산출물 | U4-ui-controllers | G5 |
| U6-tests | ops/plan/manager/balance/scene/flow/PlayMode 테스트 | U2-plan-composition, U3-manager-wiring, U5-scene-data-wiring | G6 |
| U7-validation | builder·compile·EditMode·PlayMode·validator·수동 smoke 준비 | U6-tests | G7 |
| U8-implementation-records | 구현 노트·summary·status 갱신 | U7-validation | G8 |

## 파일/모듈 영향 (Affected Files/Modules)

| 파일/모듈 | 변경 유형 | 설명 |
|-----------|-----------|------|
| `game/Assets/Scripts/Runtime/DayCycle/GameState.cs` | modify | `snsCampaignHistory` List + `serviceSns*Today` 3필드 (JsonUtility 호환) |
| `game/Assets/Scripts/Runtime/Service/ServiceOrderState.cs` | modify | `snsInflow` bool 태그 필드 |
| `game/Assets/Scripts/Runtime/Social/SNSCampaignRecord.cs` | create | 집행 레코드 Serializable 상태 (B3) |
| `game/Assets/Scripts/Runtime/Social/SNSCampaignResult.cs` | create | 집행 결과·미리보기 순수 DTO |
| `game/Assets/Scripts/Runtime/Social/SNSCampaignOps.cs` | create | 밀리 투영·감쇠·보너스·팔로워·매칭·집행 게이트·미리보기(top-target — customer 투영 입력)·DayModifier 재구성 (순수 C#) |
| `game/Assets/Scripts/Runtime/Genre/DayModifier.cs` | create | 익일 수요 modifier 순수 DTO (task-112 이벤트 합성의 공용 훅) |
| `game/Assets/Scripts/Runtime/Genre/GenreDemandPlan.cs` | modify | Base/Bonus 주문 수·보너스 가중치·SourceCampaignId, 생성자 하위호환 |
| `game/Assets/Scripts/Runtime/Genre/GenreSelectionOps.cs` | modify | modifier overload·보너스 풀 합성·pick 분기·`CustomerDefInput` 연령/성별 필드 |
| `game/Assets/Scripts/Runtime/Service/ServiceOps.cs` | modify | 보너스 주문 태그 부여·SNS 당일 통계 갱신 (neutral 경로 불변) |
| `game/Assets/Scripts/Runtime/Service/ServiceManager.cs` | modify | SNS catalog 주입·투영·집행/미리보기 API·plan modifier 합성 |
| `game/Assets/Scripts/Runtime/DayCycle/GameEvents.cs` | modify | `SNSCampaignExecuted` 이벤트 추가 |
| `game/Assets/Scripts/Runtime/UI/NightPanelController.cs` | modify | SNS 블록: 팔로워·버튼 3종·집행·결과/경고·focus (EditorInit overload) |
| `game/Assets/Scripts/Runtime/UI/ServicePanelController.cs` | modify | `SNS 유입` 태그 표시·표현 이벤트에 태그 전달 |
| `game/Assets/Scripts/Runtime/UI/SettlementPanelController.cs` | modify | SNS 원인 라인 `snsEffectText` (EditorInit overload) |
| `game/Assets/Scripts/Runtime/UI/PhaseHudController.cs` | modify | badge `{base}+{bonus}건(SNS)` + day 전환 시 badge 재계산 |
| `game/Assets/Scripts/Runtime/UI/MarketPanelController.cs` | modify | 확정 상세에 `SNS 유입 +N팀 예정` 문구 |
| `game/Assets/Scripts/Runtime/Presentation/ServicePresentationEventArgs.cs` | modify | `SnsInflow` 필드 (기존 생성자 overload 보존) |
| `game/Assets/Scripts/Runtime/Presentation/ShopPresentationController.cs` | modify | 무대 손님 라벨 SNS 표기 (Jade Green rich text) |
| `game/Assets/Scripts/Editor/InitialDataBuilder.cs` | modify | SNS 3종 비용 재시드 15,000/12,000/7,000 (GUID 보존, 타 필드 불변) |
| `game/Assets/Scripts/Editor/SceneBuilder.cs` | modify | Night 패널 재배치·SNS UI 생성·참조/catalog 주입·2씬 재생성 |
| `game/Assets/Scenes/Shop.unity` | modify | SceneBuilder 산출 Night SNS UI |
| `game/Assets/Scenes/MainMenu.unity` | modify | SceneBuilder 재실행 산출 (catalog 주입 외 기능 변경 없음) |
| `game/Assets/Data/Definitions/SNS/photo_feed.asset` | modify | baseCost 15,000 upsert |
| `game/Assets/Data/Definitions/SNS/short_form.asset` | modify | baseCost 12,000 upsert |
| `game/Assets/Data/Definitions/SNS/local_board.asset` | modify | baseCost 7,000 upsert |
| `game/Assets/Tests/EditMode/SNSCampaignOpsTests.cs` | create | 투영·감쇠·보너스·팔로워·매칭·게이트·상태 불변·미리보기·modifier 재구성 |
| `game/Assets/Tests/EditMode/SNSBalanceTests.cs` | create | G절 100-day 가드 재유도 (±1%·수확체감·하드캡 8) |
| `game/Assets/Tests/EditMode/GenreSelectionOpsTests.cs` | modify | modifier 검증·base-prefix 불변·C5 벡터·plan field-by-field 동등성 |
| `game/Assets/Tests/EditMode/ServiceOpsTests.cs` | modify | 태그 부여·SNS 통계·neutral 회귀 |
| `game/Assets/Tests/EditMode/SceneBuilderTests.cs` | modify | Night SNS 오브젝트 존재·멱등·SNS catalog 정렬 주입 |
| `game/Assets/Tests/EditMode/NightPanelSceneTests.cs` | create | Night 구조 테스트 (오브젝트·기본 상태) |
| `game/Assets/Tests/EditMode/NightPanelSnsFlowTests.cs` | create | 상태 테스트 — 집행→버튼 잠금→문구→현금 (구조/상태 fixture 분리) |
| `game/Assets/Tests/EditMode/ServicePanelSceneTests.cs` | modify | 태그 표시 회귀 |
| `game/Assets/Tests/EditMode/SettlementPanelSceneTests.cs` | modify | SNS 원인 라인·정산 멱등성 회귀 |
| `game/Assets/Tests/EditMode/MarketPanelSceneTests.cs` | modify | 상세 `SNS 유입 +N팀` 문구 회귀 |
| `game/Assets/Tests/PlayMode/SnsInflowPlayModeTests.cs` | create | 씬 전환 생존 catalog·Night 집행→익일 plan/태그 통합 검증 |
| 관련 `game/**/*.meta` | create/modify | 신규 Social 폴더·스크립트·테스트의 Unity 메타 쌍 |

## 테스트 기준 (Test Criteria)

- [ ] `python -B runtime/validator/cli.py kb/tasks/task-111/design.md`가 종료 코드 0으로 통과한다.
- [ ] 구현 전 기준선(EditMode 152/PlayMode 2)을 실행하고 실제 결과를 구현 노트에 기록한다.
- [ ] `GameState.snsCampaignHistory` 기본값은 빈 List이고 `SNSCampaignRecord`는 문자열 ID·정수만 담는
  `[Serializable]` public 필드 클래스다 (SO/Dictionary/enum 직렬화 없음).
- [ ] 밀리 투영이 C1 표와 일치한다: reachMilli₀ 250/300/150, decayMilli 850/800/900, 친화 1500·1200·
  1100 / 1600·1300 / 1500·1250.
- [ ] 감쇠 체인이 C2 표와 일치한다: photo `250,213,181,154,131,111,94,80` / short `300,240,192,154,123,
  98,78,62` / local `150,135,122,110,99,89,80,72` — `Math.Pow` 없이 정수 fold로 계산된다.
- [ ] 보너스 주문 수가 C3와 일치한다: photo/short 첫 집행 2, 2회차 1, local 1 유지, photo 8회차·short
  7회차·local 7회차에 0. 팔로워 획득 첫 집행 25/30/15.
- [ ] `TryExecute` 성공이 원자적이다: `cash -= cost`, 레코드 1건 append(`campaignId/executedOnDay/
  costPaid/effectiveMilliReach/bonusOrderCount/followerGain` 전부 채움), 결과 DTO 값 일치.
- [ ] 집행 게이트 실패(E1의 2~6: 잘못된 def·파산·Night 아님·오늘 이미 집행·자금 부족)가 모두 상태
  완전 불변 + 한국어 사유를 반환하고, `state == null`은 `ArgumentNullException`이다.
- [ ] 같은 밤 두 번째 집행은 채널이 달라도 실패한다(1밤 1회 하드 규칙).
- [ ] 레코드의 저장값(`effectiveMilliReach/bonusOrderCount/followerGain`)이 같은 def·priorUses의 재계산
  값과 일치한다(약속 고정 검증).
- [ ] C4 매칭: (AgeBand 일치 && 성별 호환) 규칙, 무매칭 1000, 다중 매칭 최댓값, `(AgeBand,Gender)` 중복
  행 명시적 실패가 각각 검증된다. 현 archetype(전원 All)에서 여성 타겟 행이 해당 연령대에 매칭된다.
- [ ] 팔로워 표시값이 `120 + Σ followerGain`이고 photo→short→photo 3연속 집행 예시가 `120+25+30+21=196`
  이다.
- [ ] `TryBuildDayModifier`: 전날 레코드 0건 → Neutral 성공, 1건 → 레코드 저장값 기반 modifier, 2건
  이상·미지 campaignId·`bonusOrderCount ∉ [0,2]` → 명시적 실패.
- [ ] `TryBuildDemandPlan` modifier overload 검증: `modifier == null`·`Day != day`·bonus 범위 밖·boost
  커버리지 위반(누락/중복/미지 ID)·`BoostMilli ≤ 0`·보너스 풀 합 0이 모두 명시적 실패고 state/phase가
  불변이다.
- [ ] 기존 4-입력 `TryBuildDemandPlan`과 Neutral modifier overload의 plan이 field-by-field 동등하고,
  task-110의 기존 결정론 테스트(주문 수 4/6/5/5, forecast top-2, FNV `"gukbap|1|0"`=`2190636514`)가
  전부 회귀 없이 통과한다.
- [ ] **base-prefix 불변**: 같은 genre/day에서 SNS modifier 유무와 무관하게 인덱스 `0..BaseOrderCount-1`
  의 고객·레시피·파티 크기가 완전히 동일하다.
- [ ] C5 벡터: 분식 Day 2 × 숏핑 첫 집행의 보너스 풀이 family 900/office 1716/senior 420/student 2400
  (합 5436)이고, FNV `"bunsik|2|6"`=`1202351915`(roll 1127→office_worker),
  `"bunsik|2|7"`=`1185574296`(roll 4440→student)이다.
- [ ] 분식 Day 2 × 숏핑 worked example: 총 8주문, 인덱스 6·7만 `snsInflow == true`, 태그 주문이
  `떡볶이/직장인×2 (매출 10,500원)`·`김밥/학생×3 (매출 14,175원)`, 전량 서빙 시
  `serviceSnsRevenueToday == 24,675`.
- [ ] `BuildOrders(plan, customers)`가 태그를 정확히 `i >= BaseOrderCount`에만 부여하고, 서빙/포기 시
  `serviceSnsOrdersServedToday/MissedToday/RevenueToday`가 태그 주문에만 갱신되며 `StartServiceDay`가
  3필드를 리셋한다. neutral 주문의 기존 수치·메시지는 불변이다.
- [ ] `GameState`를 `JsonUtility` 왕복(ToJson→FromJson)한 뒤 `TryBuildDayPlan` 결과 plan이 왕복 전과
  field-by-field 동등하다(저장 후 재개 동일성 — task-113 선행 보증).
- [ ] `ServiceManager.TryExecuteSnsCampaign`이 catalog 미지 id에서 상태 불변 실패하고,
  `TryGetSnsPreview`가 def·`CustomerDefs`를 `ToSnsCampaignInput`/`ToCustomerInputs`로 투영해
  `TryBuildPreview`에 전달하며, 감쇠 반영 `BonusOrderCount`·`CanExecute/BlockReason`을 게이트와 동일
  규칙으로 반환한다.
- [ ] preview의 `TopTargetCustomerIds`가 customer 투영 입력을 받은 순수 helper(`TryBuildPreview`)에서만
  계산되고(photo=`office_worker, family_parent` / short=`student, office_worker` /
  local=`senior_regular, family_parent`, 동률은 customer ID ordinal 오름차순), customers 무결성 위반
  (null/빈 목록/빈·중복 ID)은 미리보기의 명시적 실패다. `NightPanelController`는 DTO 표시와 ID→표시명
  매핑만 수행하며 `SNSCampaignDef`의 배수·도달 필드를 참조해 재계산하는 경로가 없다.
- [ ] 어제 레코드가 catalog에 없는 campaignId를 참조하면 `TryBuildDayPlan`이 명시적 사유로 실패하고
  Market→Service 진행이 차단된다(조용한 neutral 진행 없음).
- [ ] PlayMode: MainMenu→Shop 전환 후 persistent `ServiceManager`가 SNS catalog 3종(ID ordinal 정렬)을
  보유하고, UI 없이 도메인 경로로 Day 1 루프→Night 집행→Day 2 진입 시 plan `OrderCount == base+bonus`,
  태그 주문 `bonus`건 존재, `serviceOrders` 원자적 초기화가 검증된다.
- [ ] `SNSBalanceTests` 100-day 재유도가 G절 표의 ±1% 안이고: 12조합 1회차 전부 `+500~+14,000원`,
  photo/short 2회차 < 1회차(전 장르), local 2회차 == 1회차, 하루 총 주문 ≤ 8이 성립한다.
- [ ] `InitialDataBuilder`가 SNS 비용만 15,000/12,000/7,000으로 upsert하고 도달·감쇠·친화·문구·GUID가
  불변이다. 연속 2회 `InitialDataBuilder.Apply`+`SceneBuilder.Apply`가 asset GUID·오브젝트 수·persistent
  listener 기준으로 멱등이다.
- [ ] `SceneBuilder` 산출 Night 패널에 F1 오브젝트 전부가 지정 좌표·크기·폰트로 존재하고 Build Settings
  씬은 2개뿐이다.
- [ ] Night 버튼 interactable 규칙(오늘 집행됨/파산/자금 부족 시 비활성)과 집행 후 전 버튼 잠금·결과
  문구·운영비 부족 경고가 씬 상태 테스트로 검증된다(구조/상태 fixture 분리).
- [ ] 집행 성공 시 `GameEvents.SNSCampaignExecuted`가 정확히 1회 발행되고 실패 시 발행되지 않는다.
- [ ] HUD badge가 보너스 있는 날 `{장르} · 주문 {base}+{bonus}건(SNS)`으로 표시되고, Night→Market day
  전환 후 badge 주문 수가 새 plan 값으로 재계산된다(낡은 값 잔존 없음).
- [ ] Settlement에 SNS 원인 라인이 어제 집행일에만 표시되고(`유입 {served}/{bonus}팀`, 0팀 포함), 정산
  수학·day 멱등성은 기존 그대로다.
- [ ] Service 패널·무대 라벨이 태그 주문에만 `SNS 유입`/`SNS` 표기를 붙이고 표현 이벤트가 `SnsInflow`를
  전달한다(기존 생성자 경로는 false).
- [ ] Unity 배치 compile 종료 코드 0·`error CS` 없음, EditMode 전체(`-runTests -testPlatform EditMode`)
  종료 코드 0, PlayMode 전체(`-runTests -testPlatform PlayMode`) 종료 코드 0으로 기존 회귀가 없다.
- [ ] `git status --short game`에 `Library/Temp/Obj/Logs/UserSettings/Build*`가 없고 신규 파일 전부에
  `.meta`가 존재한다.
- [ ] 640×360 원본 캡처를 Codex가 검토해 Night SNS 블록이 겹침·canvas 이탈 없이 F1 좌표·폰트·focus
  순서와 일치함을 승인한다 (Claude self-approve 금지).
- [ ] 수동 Play smoke(오너/Codex): Night 집행 판단 60초 이내, 집행→익일 badge `+N`→`SNS 유입` 태그→
  Settlement 라인의 인과 사슬 확인, 같은 채널 반복 시 미리보기 `+2→+1팀` 감쇠 확인, 자금 부족 버튼
  비활성 확인.

## 오픈 이슈 (Open Issues)

- **SNS 비용 재시드 오너 승인**: 50,000/40,000/25,000 → 15,000/12,000/7,000은 G절 가드에 근거한 설계
  확정값이지만 기존 asset 값 변경이므로 Codex 교차검토와 오너 승인을 거쳐야 한다. 거부되면 보너스/감쇠
  공식 재조정이 아니라 비용 재협상으로 처리한다(공식은 비용과 독립).
- **성별 축의 실질 무차별**: 데모 archetype 4종이 전부 `Gender == All`이라 픽쳐그램의 여성 타겟 행이
  연령대 전체에 매칭된다(C4 규칙으로 결정론은 보장). 성별 분리 archetype은 1.0 taxonomy/고객 확장에서
  도입하고 그때 C4 매칭의 우선순위 규칙(정확 성별 > All)을 재검토한다.
- **follower의 fame 파생 전환**: 이번 task의 follower는 SNS 누적 도달 파생 stand-in(기준 120 + Σ
  followerGain)이다. task-110 D8의 fame/reputation 분리가 post-demo에 구현되면 follower를 fame 파생
  공식으로 교체하고 기준값·환산율을 다시 정한다.
- **DayModifier 다중 소스 병합**: task-112 이벤트 modifier와의 합성 규칙(보너스 주문 합산·boost 곱
  결합·하드캡 재검토)은 task-112 설계에서 확정한다. 이번 task는 단일 소스(SNS)만 싣는 필드 구성으로
  병합 확장을 막지 않게만 설계했다.
- **동네게시판 장기 반복 지배 가능성**: 완만 감쇠 정체성 때문에 2~6회차 net이 유지된다. 3일 데모에서는
  드러나지 않지만(밤 2~3회) 장기 플레이 밸런스는 task-115 게이트에서 감쇠 강화/회복 규칙과 함께
  재검토한다.
- **보너스 주문의 후미 배치**: 유입 손님이 항상 그날 뒤쪽 주문으로 오는 단순화를 채택했다(base-prefix
  불변의 대가). 인터리브 배치가 필요하면 표현 계층 연출로 흡수하거나 task-115에서 시드 규약과 함께
  재설계한다.
- **마지막 밤 집행의 낭비**: 데모 3일 루프의 3일차 밤 집행 효과는 4일차에 적용되어 현재 무한 진행
  데모에선 유효하지만, task-115 엔딩(3일 결과 카드) 도입 시 마지막 밤 처리(집행 차단 또는 낭비 경고)를
  결정해야 한다.
- **SNSManager 신설 보류**: 브리프 아키텍처 규약의 매니저 8종 목록에 SNS가 있으나, 이번 task는 신규
  singleton 금지 지시와 task-110 G2 원칙에 따라 `ServiceManager` 확장으로 구현했다. 후속(task-113 저장,
  이벤트 확장)에서 책임이 비대해지면 분리를 재평가하고 SSOT와 정합시킨다.
- **EnsureServiceDay legacy 경로**: task-110 이후 UI가 호출하지 않는 구식 주문 생성 경로가
  `ServiceManager`에 남아 있다. 이번 task에서는 건드리지 않으며 task-113 저장 설계 전에 제거 후보로
  검토한다.
- **Night 60초 예산**: SNS 블록 추가 후에도 Night 핵심 판단이 60초 안에 끝나는지는 수동 smoke에서
  측정한다. 초과 시 카피 축약·기본 선택 강조를 Codex가 재설계한다.
