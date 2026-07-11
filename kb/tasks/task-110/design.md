# 설계 문서 — task-110: 장르 선택 시스템과 제품 GDD v0.9

> Status: ready
> Inputs: `kb/concepts/project-brief.md`, `kb/concepts/demo-scope.md`, `kb/concepts/development-priority.md`, `kb/concepts/art-direction.md`, `kb/tasks/task-109/design.md`, 현재 Unity 프로젝트 기준선(`game/`), 오너의 2026-07-11 확정 컨셉(뉴욕 한인타운·한국계 이민자 2세·푸드트럭·지역별 요리대회·장슐랭·한식 카테고리), 2026-07-10 공식 Steam/개발사 페이지 기반 경쟁작 조사
> Outputs: 전체 게임 비전과 데모 경계를 분리한 제품 GDD v0.9 제안, Codex 소유의 UX·아트 디렉션, Claude가 즉시 구현할 task-110 장르 선택·수요 예측·경제/서비스 배수·UI·테스트 계약, 후속 PPTX 제작용 18장 슬라이드 사양
> Next step: Claude가 먼저 scaffold `manifest.md`의 placeholder를 이 문서의 Inputs·영향 파일로 채운 뒤 **task-110 구현 계약만** 구현하고, EditMode·배치 검증과 장르 인과성 집중 Play 테스트를 통과시킨다. Codex 구현 리뷰 후 `task-111` SNS로 진행하며 전체 3일 수직 슬라이스 게이트는 `task-112` 이후 수행한다.

## 목표 (Objective)

`Client is King`을 “미국에서 한식을 파는 게임”이 아니라 **포기했던 요리사의 꿈을 다시 시작한 한국계 미국인 2세가, 시장과 고객을 읽는 경영 판단으로 푸드트럭에서 장슐랭까지 성장하는 2D 픽셀 경영 게임**으로 제안한다. 스토리·시스템·UX·아트·기술 구조·제작 로드맵을 하나의 전문 GDD v0.9로 정렬한다.

동시에 현재 데모 SSOT와 코드 기준선을 존중해, Claude가 이번 task에서 구현할 범위를 **장르 선택 → 예상 수요 → 장보기 원가 → 주문 구성·객단가 → 정산 결과**의 인과가 보이는 첫 의미 있는 선택으로 제한한다. 푸드트럭 플레이, 요리대회 미니게임, 장슐랭 트랙 등 정식 게임 비전은 이 문서에서 설계하되 이번 구현에는 포함하지 않는다.

### 결정 상태

| 상태 | 내용 |
|------|------|
| **오너 확정** | 뉴욕 한인타운, 한국계 이민자 2세, 가난 때문에 포기한 요리사의 꿈, 요리 서바이벌 프로그램을 보고 재도전, 푸드트럭 시작, 장슐랭 최종 목표, 한식 카테고리 후보, 소·중·대규모 요리대회, 디자인은 Codex 소유 |
| **Codex 제안** | 푸드트럭 1~2일 뒤 단일 식당으로 교체, 따뜻한 코미디 중심 톤, 미국 내 동네→뉴욕→전국 대회, fame/reputation 분리, 8~12시간 1.0 목표, 0~3별+복수 엔딩, 주식·코인 주차 |
| **추가 승인 필요** | 푸드트럭의 실제 playable 범위, 대회 ‘지역’의 정확한 의미, 1.0 taxonomy, 주인공 세부 정보, USD 전환, 공개 방송·평가 명칭, 최종 아트 자산 정책 |

이 문서는 제품 GDD **working baseline**이다. 새 컨셉이 `project-brief.md`와 `demo-scope.md`에 반영되기 전에는 concept-level SSOT를 대체하지 않는다. task-110 구현 계약은 기존 SSOT 안에서 독립적으로 ready다.

### 문서 사용법

- **GDD v0.9 제안**: 아래 A~G 절은 정식 게임의 제품·서사·시스템·비주얼·기술 방향을 설명한다.
- **구현 계약**: 아래 H 절, 실행 계획, 영향 파일, 테스트 기준만 task-110에서 Claude가 구현한다.
- **장기 기능**: 푸드트럭·대회·장슐랭·정식 캠페인은 승인된 SSOT/하드캡 변경 후 각각 별도 task로 설계한다.
- **프레젠테이션 사양**: J 절은 개발팀·협업자 설명용 PPTX의 슬라이드별 내용과 비주얼 계약이다. 이 task에서 저장소 파일로 PPTX를 만들지는 않는다.

### 역할과 결정권

| 영역 | 결정권자 | 수행자 | 게이트 |
|------|----------|--------|--------|
| 게임 컨셉·시스템·밸런스 의도 | Codex 설계, 오너 최종 승인 | Claude 구현 | design.md validator + Play 테스트 |
| UX 흐름·정보 위계·화면 구성 | **Codex** | Claude가 SceneBuilder/UI로 구현 | Codex 화면 리뷰 |
| 아트 디렉션·팔레트·캐릭터/음식 시안 | **Codex** | Claude가 승인된 에셋만 통합 | Codex 원본 크기 시각 QA |
| 도파민 피드백·애니메이션 타이밍 | **Codex** | Claude 구현 | Codex Play 리뷰 |
| 코드 구조·테스트·배치 자동화 | 설계 계약 내 Claude | Claude | 컴파일·EditMode·멱등성 게이트 |
| 설계와 다른 화면 판단 | Codex에 재설계 요청 | Claude가 임의 확정하지 않음 | 새 design/review 기록 |

Claude는 비어 있는 디자인을 임의로 채우지 않는다. 구현 중 레이아웃·색·애니메이션·에셋 선택이 불명확하면 기능을 임시 확장하지 말고 Codex 리뷰 대상으로 남긴다.

## 범위 (Scope)

### 포함 — 전체 GDD 설계 범위

- 뉴욕 한인타운, 한국계 이민자 2세 주인공, 가난으로 포기한 요리사의 꿈, 가상 요리 서바이벌 프로그램을 계기로 한 재도전 서사를 정의한다.
- 푸드트럭 → 작은 식당 → 지역/도시/전국 요리대회 → 장슐랭 평가로 이어지는 정식 캠페인의 성장선을 정의한다.
- `주문`, `하루`, `3일/챕터`, `전체 캠페인` 네 층의 반복 루프와 보상 구조를 정의한다.
- 한식 전문 분야, 레시피 카테고리, 재료 등급, 고객 archetype, 수요 예측, SNS, 이벤트, 유명도, 평판, 요리대회, 장슐랭의 장기 계약을 정의한다.
- UX 화면 흐름, 픽셀아트 방향, 색·타이포·아이콘·애니메이션·사운드·접근성 기준을 정의한다.
- Unity 데이터/상태/Ops/매니저/UI/SceneBuilder/테스트의 확장 방향과 단계별 제작 경계를 정의한다.
- 개발팀 설명용 18장 PPTX의 메시지·순서·시각 목적을 문서 안에 명시한다.

### 포함 — task-110 구현 범위

- 첫 실행 Day 1 Market에서 `국밥 / 분식 / 면류 / 제네럴리스트` 중 전문 분야를 한 번 선택하고 현재 run 동안 잠근다.
- 전문 분야를 선택하기 전에는 재료 구매와 Service 진입을 막고, 네 선택지의 장점·단점·예상 주문 수·주 고객층을 비교하게 한다.
- UI 우회가 불가능하도록 GameManager/EconomyManager 도메인에서도 미선택 구매·Market→Service 진행을 차단하고, 열린 주문이 있으면 Service→Settlement 진행을 차단한다.
- B급의 품질 보상식이 아직 없으므로 task-110 플레이어 UI에서는 B급 구매/서빙 토글을 숨기고 C급만 사용한다. B급 데이터와 기존 neutral Ops는 삭제하지 않는다.
- `GameState`에 선택한 장르 ID를 문자열로 저장하고, SO 참조를 런타임 상태에 넣지 않는다.
- `GenreDef`의 원가·조리시간·객단가·고객 친화도 배수를 실제 경제와 주문 생성 결과에 연결한다.
- 전문 분야는 해당 장르 레시피만 주문 후보로 사용하고, 제네럴리스트는 모든 데모 레시피를 사용한다.
- 장르별 회전율에 따라 하루 주문 수를 4~6개로 바꾸고, 고객 친화도에 따라 고객 archetype 분포를 결정론적으로 바꾼다.
- Market에 정확한 주문 목록이 아닌 `예상 주문 수 + 주 고객층 2종 + 위험/보상 요약`을 표시한다.
- Service와 HUD에 선택 장르, 적용된 객단가, 유효 회전율을 표시해 결과의 원인을 읽을 수 있게 한다.
- 기존 순수 C# Ops/manager thin-wrapper/SceneBuilder 멱등 구조를 유지하고 EditMode 회귀 테스트를 추가한다.

### 제외 — task-110에서 구현하지 않음

- 푸드트럭 운전·이동·별도 씬·차량 커스터마이징, 작은 식당으로의 실제 전환.
- 장편 컷신, 대화 선택지, 관계 시스템, 라이벌 NPC, 할머니 퀘스트.
- 실제 `흑백요리사` 명칭·영상·로고·인물의 사용. 공개판은 가상 프로그램을 사용한다.
- 국·탕·찌개/해산물/고기 1.0 taxonomy로의 데이터 마이그레이션과 신규 레시피·재료·아트 제작.
- SNS 런타임 효과(`task-111`), 이벤트(`task-112`), 저장/불러오기(`task-113`), 아트 마감(`task-114`), 엔딩·빌드(`task-115`).
- 지역·도시·전국 요리대회, 직접 조리 미니게임, 장슐랭 평가 트랙.
- 직원, 인테리어 시스템, 다점포, 협동, 자유 건축, 자유 칼질/물리 조리.
- 주식·암호화폐. 위험/보상은 식재료 선계약·SNS 집행·행사 참가처럼 식당 판타지 안에서 표현한다.
- 신규 singleton manager, 외부 DI/ECS/이벤트버스/tween 라이브러리, 신규 씬.
- PPTX 파일 생성. 저장소 규약상 이 설계 턴은 `design.md` 한 파일만 Codex가 수정한다.

## 제약 (Constraints)

- `kb/concepts/project-brief.md`가 현재 데모 컨셉·로드맵·아키텍처의 SSOT이고 `kb/concepts/demo-scope.md`가 구현 하드캡이다. 이 문서의 정식 게임 비전은 SSOT를 자동 변경하지 않는다.
- task-110은 기존 로드맵의 “장르 선택 시스템” 범위 안에서만 `Status: ready`다. 푸드트럭·대회·장슐랭은 별도 승인과 SSOT 개정 전까지 비실행 설계다.
- Unity 6 `6000.3.8f1`, 2D URP, Windows PC/Steam 우선, 640×360 Pixel Perfect, PPU 32, Point filter, compression none 기준을 유지한다.
- 씬은 `MainMenu.unity`와 `Shop.unity` 두 개뿐이다. UI와 무대는 `SceneBuilder.Apply`로 코드 저작하며 수동 씬 편집을 정본으로 삼지 않는다.
- 정적 데이터는 ScriptableObject, 런타임 상태는 순수 C# `GameState`, 장기 저장은 문자열 ID + List 기반 `JsonUtility` 계약을 유지한다. Dictionary와 SO 직접 참조를 저장하지 않는다.
- 순수 규칙은 `*Ops`에, Unity lifecycle/참조는 manager/controller에 둔다. 새 기능을 singleton manager로 분리하기보다 기존 `GameManager`, `EconomyManager`, `ServiceManager`, UI controller를 얇게 확장한다.
- 모든 수학은 같은 입력에서 같은 결과를 내야 한다. `System.HashCode`, 런타임별로 달라질 수 있는 문자열 hash, 프레임 시간 기반 난수를 주문 생성에 사용하지 않는다.
- 기존 공개 API와 97개 EditMode 테스트 기준선을 최대한 보존한다. 기존 시그니처는 neutral multiplier overload로 하위호환한다.
- 장르 선택은 새 run에서 한 번만 가능하다. 저장/새 게임 UI가 없는 현재 데모에서는 `GameState.selectedGenreId`가 비어 있을 때만 선택한다.
- 현재 통화와 수치는 기존 원화 정수 계약을 유지한다. 뉴욕 배경에 맞춘 USD/cent 마이그레이션은 저장 스키마와 UI를 함께 바꾸는 후속 결정이다.
- 실제 프로그램과 평가 기관을 연상시키는 이름은 내부 영감에만 사용한다. 공개명은 가상화하고 출시 전 별도 명칭 검토를 거친다.
- 현대 뉴욕 한인타운이 최종 비주얼 기준이다. `art-direction.md`의 에도풍 Ninja 에셋은 임시 가독성 자산일 뿐 최종 정체성을 결정하지 않는다.
- Figma는 사용하지 않는다. Codex 이미지 생성은 콘셉트/목업, Pixelorama는 픽셀 정리, Penpot은 필요 시 무료 UI 흐름 도구로만 사용한다.
- UI는 색만으로 상태를 전달하지 않고 아이콘·문구·형태를 함께 사용한다. 한국어/영어 길이 변화와 640×360 가독성을 고려한다.
- 주인공의 빈곤·이민 배경을 희화화하거나 고통을 보상 자판기로 사용하지 않는다. 따뜻한 코미디와 경쟁 드라마가 중심이고 문화 고증은 음식·언어·손님 반응으로 전달한다.

## 구현 단계 (Implementation Steps)

### A. 제품 GDD

#### A1. 게임 개요

| 항목 | 결정 |
|------|------|
| 작업명 | `Client is King / 손님이 왕이다` |
| 하이컨셉 | 뉴욕 한인타운의 작은 장사에서 시작해 고객과 시장을 읽는 경영 판단으로 장슐랭을 노리는 2D 픽셀 한식당 시뮬레이션 |
| 로그라인 | 가난 때문에 요리사의 꿈을 포기한 한국계 미국인 2세가 가상 요리 서바이벌 쇼에서 용기를 얻고, 할머니의 기억과 한식으로 푸드트럭부터 다시 시작한다. |
| 장르 | 싱글플레이 식당 경영 시뮬레이션 + 짧은 서사 + 대회 한정 조리 챌린지 |
| 플랫폼 | Steam / Windows PC 우선 |
| 비즈니스 모델 제안 | 1회 구매형 premium 싱글플레이, 게임 내 광고·확률형 결제·유료 재화 없음 |
| 카메라·표현 | 2D 픽셀, 고정 가게 무대와 UI 중심, 직접 이동 캐릭터 조작 없음 |
| 목표 이용자 | 경영·코지·음식 문화 게임을 좋아하고 복잡한 공장 자동화보다 명확한 선택과 따뜻한 캐릭터를 선호하는 이용자 |
| 데모 길이 | 첫 3일 10~15분 |
| 1.0 목표 길이 | 메인 캠페인 8~12시간, 하루 3~5분, 세션 20~40분을 설계 목표로 둠 |
| 핵심 판타지 | “내가 고객과 시장을 제대로 읽어서 이 작은 한식당을 살렸다.” |

#### A2. 디자인 기둥

| 기둥 | 플레이어가 느낄 것 | 시스템 증거 |
|------|-------------------|-------------|
| 선택의 인과 | 내 판단이 결과를 만들었다 | 예상 수요, 장르·등급·SNS 선택, 정산 원인 분해 |
| 작은 가게의 온기 | 이 공간과 손님을 지키고 싶다 | 단골, 한마디 반응, 할머니 레시피, 무대 변화 |
| 생존과 영리함 | 위험을 읽고 한 수 앞섰다 | 운영비, 변동 원가, 이벤트 예고, 회복 가능한 실패 |
| 한식의 정체성 | 낯선 손님에게 한식이 통하는 기쁨 | 음식별 실루엣, 고객 취향, 문화적 대사와 반응 |
| 계단식 성취 | 다음 단계가 가깝고 선명하다 | 첫 단골 → 지역 대회 → 식당 → 큰 대회 → 장슐랭 |

#### A3. 차별화 포지션

- `Gimbap Heaven`과 겹치는 고정비·고객 유형·일일 사건은 **고객층/SNS 수요 설계와 한국계 미국인 서사**로 차별화한다.
- `Kimbap Heaven Simulator`·`POJANGMACHA`가 강한 1인칭 직접 조리와 현장 운영을 따라가지 않는다.
- `Touhou Mystia’s Izakaya`의 고객 취향 가독성, `Cafeteria Nipponica`의 압축 경영, `Good Pizza, Great Pizza`의 기억나는 손님, `DAVE THE DIVER`의 준비→영업→재투자, `PlateUp!`의 하루마다 하나의 변화만 가져온다.
- 한 줄 USP는 **“오늘 어떤 손님을 끌어올지 메뉴·재료·SNS로 설계하는 한식당 경영”**이다.

#### A4. 비목표

- 식당 안을 직접 뛰어다니는 액션 게임이 아니다.
- 레시피마다 서로 다른 물리 조작을 요구하는 요리 모음집이 아니다.
- 직원 감정·좌석 동선·인테리어·다점포를 동시에 관리하는 초고밀도 타이쿤이 아니다.
- 오픈월드 생활·농사·채집 RPG가 아니다.
- 주식·코인으로 식당 외부의 부를 얻는 게임이 아니다.
- 플레이어가 이해할 수 없는 숨은 배수의 조합이 아니다.

#### A5. 콘텐츠 예산

| 범위 | 장소 | 음식/재료 | 고객·사건 | 대회·엔딩 |
|------|------|-----------|-----------|-----------|
| 현재 데모 하드캡 | 활성 장소 1 | 레시피 6, 재료 9×C/B, 전문 분야 3+균형 | 고객 4, SNS 3, 이벤트 4 | 실제 대회 0, 3일 결과 카드 |
| Post-demo 수직 슬라이스 | 푸드트럭→식당 순차 2테마 | 카테고리 1, 레시피 2~3 | 고객 4, 사건 1 | 지역 대회 1 |
| 1.0 잠정 상한 | 순차 장소 3, 동시 운영 1 | 상위 카테고리 3+혼합, 레시피 최대 18, 재료 최대 18 | archetype 최대 8, 사건 최대 12, 주요 단골/NPC 최대 6 | 공통 엔진의 대회 3단계, 엔딩 최대 4 |

1.0 숫자는 오너 확정이 아니라 제작비 상한 제안이다. 3일 데모와 지역 대회 수직 슬라이스가 각각 통과하기 전에는 확장하지 않는다.

### B. 세계관과 서사

#### B1. 배경과 톤

- 장소는 현대 뉴욕 한인타운과 인접 동네다. 네온 간판, 증기, 좁은 주방, 지하철 소리, 배달 자전거, 다양한 세대의 손님이 장소성을 만든다.
- 현실의 특정 상점·방송·평가 기관을 복제하지 않고 가상의 방송과 음식 가이드를 사용한다.
- 톤은 **따뜻한 생활 코미디 60% + 경영 생존 25% + 경쟁 드라마 15%**를 기준으로 한다.
- 이민자 정체성은 설명문보다 언어 선택, 음식 기억, 세대별 손님 반응, 할머니와의 짧은 장면으로 보여준다.

#### B2. 주인공

- 뉴욕 한인타운에서 자란 한국계 이민자 2세다. 성별·이름은 오너 확정 전까지 데이터화 가능한 placeholder로 유지한다.
- 어릴 때 요리사를 꿈꿨지만 집안 형편 때문에 전문 교육을 받지 못하고 꿈을 접었다.
- 가상의 요리 서바이벌 프로그램을 본 뒤 “완벽한 조건이 없어도 시작할 수 있다”는 용기를 얻는다.
- 전문 셰프가 아니라 학습하는 경영자이므로, 초반 실패와 플레이어의 배움이 서사와 일치한다.

#### B3. 할머니의 역할

- 할머니는 레시피·가족 기억·정서적 안전망을 연결하는 인물이다.
- 긴 튜토리얼 NPC가 아니라 챕터 사이 2~4문장 대화, 오래된 레시피 카드, 첫 단골 장면으로 등장한다.
- 할머니의 고향 지역은 확정 전까지 특정하지 않는다. 지역 고증이 끝난 뒤 음식·어휘와 함께 결정한다.

#### B4. 캠페인 구조

| 구간 | 서사 목표 | 게임 목표 | 종료 보상 |
|------|-----------|-----------|-----------|
| 프롤로그 | 포기했던 꿈을 다시 선택 | 첫 영업 준비 | 중고 푸드트럭/영업 허가 |
| Chapter 1 — 첫 불 | 3일을 버티고 첫 단골 확보 | 기본 루프·전문 분야 학습 | 뉴욕 지역 예선 초대 티저 |
| Chapter 2 — 동네의 가게 | 한인타운에서 신뢰 확보 | SNS·이벤트·평판 | 작은 고정 식당 입성 |
| Chapter 3 — 뉴욕의 식탁 | 도시의 다양한 고객층 공략 | 지역 대회·메뉴 확장 | 도시 대회 우승·유명도 상승 |
| Chapter 4 — 더 큰 무대 | 정체성과 흥행 사이의 갈등 | 전국 대회·고난도 수요 | 장슐랭 심사 자격 |
| Finale — 우리 방식 | 지속 가능한 가게의 의미 확정 | 장슐랭 평가·최종 정산 | 별 0~3개와 복수 엔딩 |

#### B5. 스토리 전달 규칙

- 하루 시작/종료 장면은 최대 20초, 핵심 버튼 입력까지 60초를 넘기지 않는다.
- 스토리는 시스템 변화를 예고하거나 결과를 감정적으로 해석할 때만 등장한다.
- 대사 선택으로 거대한 분기를 만들지 않는다. 엔딩은 누적 경영 성향·평판·장르 숙련·대회 결과로 결정한다.
- 데모는 오프닝 2~4문장, 고객 반응, 첫 단골, 지역 예선 초대 카드까지만 사용한다.

### C. 핵심 플레이 루프

#### C1. 네 층의 루프

| 층 | 입력 | 판단 | 보상/학습 |
|----|------|------|-----------|
| 주문(10~20초) | 고객·메뉴·재고·등급 | 서빙/등급/포기 | 표정·한마디·음식 팝·매출 |
| 하루(3~5분) | 수요 예측·현금·이벤트 | 장르·구매·영업·SNS | 정산 원인·단골·다음 날 기대 |
| 챕터(3~7일) | 목표·위기·평판 | 안정/성장/대회 준비 | 새 레시피·장소·초대장 |
| 캠페인(8~12시간) | 유명도·평판·숙련 | 어떤 식당이 될지 | 대회·장슐랭·엔딩 |

#### C2. 하루 상태 머신

1. **Morning Brief**: 오늘 수요 범위, 주 고객층, 예고된 사건, 운영비 위험을 본다.
2. **Market**: 전문 분야와 예상 수요를 바탕으로 C/B급 재료를 산다.
3. **Service**: 주문을 처리하며 재고·객단가·고객 반응을 확인한다. 식당 영업에는 직접 조리 미니게임이 없다.
4. **Settlement**: 매출, 재료비, 운영비, 순이익, 유명도/평판 원인을 분해한다.
5. **Night**: SNS 캠페인을 고르고 저장한다. 선택의 효과는 다음 날 손님 태그로 돌아온다.

#### C3. 성공·실패 원칙

- 한 번의 실수는 회복 가능하고, 반복된 나쁜 판단만 파산으로 이어진다.
- 주문을 남긴 채 정산으로 넘어갈 수 없다. 조기 마감은 남은 주문을 이탈로 처리하고 원인을 표시한다.
- 파산은 즉시 삭제가 아니라 결과 카드와 재도전 제안을 준다.
- 데모 성공은 `3일 생존 + 첫 단골`, 정식 성공은 장슐랭 별뿐 아니라 지속 가능한 가게/동네 명소 엔딩도 포함한다.

### D. 시스템 설계

#### D1. 장소 성장

- 정식 게임의 단일 활성 장소는 `푸드트럭 → 작은 식당 → 자리 잡은 식당`으로 교체된다. 여러 장소를 동시에 운영하지 않는다.
- 푸드트럭은 운전 게임이 아니라 1~2일 튜토리얼용 고정 영업 무대다.
- 공개 데모의 제품 목표는 기존 `Shop` 씬 이름과 2씬 하드캡을 유지하면서 무대를 **고정형 한인타운 푸드트럭 서비스 창구**로 리스킨하는 것이다. 운전·지역 이동은 없다.
- task-110에서는 무대를 바꾸지 않는다. 위 리스킨은 concept SSOT 승인 후 task-114/115 아트·오프닝 범위로 넘기며, 승인 전 빌드에서는 푸드트럭을 오프닝 이미지/스토리 카드로만 표현한다.
- 용어를 분리한다: `데모 푸드트럭 외형`은 기존 Shop 규칙을 그대로 쓰는 시각 테마이고, `Post-demo 푸드트럭 venue`만 `VenueDef`의 고유 시작 자금·운영비·메뉴 제한을 가진 실제 게임 단계다.
- 정식 구현 시 `VenueDef`로 시작 자금, 운영비, 메뉴 슬롯, 무대 테마를 데이터화한다.

#### D2. 한식 taxonomy

- **데모 운영 전문 분야**는 기존 SSOT대로 `국밥 / 분식 / 면류 / 제네럴리스트` 3+1을 유지한다.
- **1.0 레시피 상위 카테고리 잠정안**은 오너 메모의 `국·탕·찌개 / 해산물 / 고기`다.
- 세 층을 혼동하지 않는다.
  1. `RestaurantSpecialization`: 플레이어가 선택하는 경영 방식(데모 국밥/분식/면류/균형).
  2. `RecipeTag`: 음식 자체의 콘텐츠 분류(잠정 국·탕·찌개/해산물/고기 + 국밥/면/구이/찜 같은 secondary tag).
  3. `CompetitionTheme`: 대회의 제한 규칙(특정 tag, 재료, 지역 음식, 시간 제한 등)이며 전문 분야를 직접 복제하지 않는다.
- 전문 분야는 원가·회전율·객단가·고객층을, RecipeTag는 해금·검색·대회 조건을, CompetitionTheme은 참가 가능 메뉴와 심사 가중치를 결정한다.
- task-110의 matching recipe는 데모 전용 adapter `recipe.Genre.Id == selectedGenreId`이고 제네럴리스트만 전체를 허용한다. 1.0 taxonomy 도입 시에는 `SpecializationDef.allowedRecipeTagIds`의 명시적 다대다 매핑으로 교체하며 대회는 `CompetitionTheme.required/bonusRecipeTagIds`를 별도로 사용한다.
- 현재 `GenreKind` enum은 Unity 직렬화 정수이므로 단순 이름 변경을 금지한다. taxonomy 변경은 새 타입/ID 마이그레이션 task로 진행한다.

#### D3. 재료와 품질

- 데모는 재료 9종 × C/B급을 유지한다.
- C급은 낮은 원가와 가성비 고객, B급은 높은 만족·평판과 품질 민감 고객에게 가치가 있어야 한다.
- 등급은 모든 상황에서 상위 등급이 정답이 되지 않게 한다.
- 정확한 B급 만족 배수는 장르 인과가 검증된 뒤 별도 수학 task에서 확정한다. 그 전까지 task-110 플레이어 UI는 C급만 노출해 가짜 선택을 제거하며 B급 데이터와 하위 API는 보존한다.
- 복귀 책임은 `task-115` 밸런싱 게이트에 둔다. 공개 데모 후보 전에 (A) 고객별 품질 보상식+정산 원인을 구현해 B급을 다시 노출하거나, (B) 오너 승인으로 공개 데모를 C급 전용으로 공식 축소하는 둘 중 하나를 반드시 결정한다.

#### D4. 고객

| 데모 archetype | 뉴욕 재해석 | 주요 성향 |
|----------------|-------------|-----------|
| 학생 단골 | NYU/지역 학생 | 가격 민감, 분식 친화, SNS 반응 빠름 |
| 직장인 | Midtown/인근 사무직 | 시간 민감, 중간 이상 객단가 |
| 가족 손님 | 지역 가족·관광객 | 파티 크기 큼, 안정적 메뉴 선호 |
| 동네 어르신 | 한인타운 1세대 단골 | 국물·면 친화, 인내심과 충성도 높음 |

- 고객은 `연령/성별/가격 민감도/인내심/장르 친화도/파티 크기`를 가진다.
- 손님 개인을 관계 시뮬레이션하지 않는다. 일부 대표 단골만 서사 NPC로 승격한다.
- 반응은 `좋아한 이유/싫어한 이유`를 한 문장으로 표시해 숨은 배수를 설명한다.

#### D5. task-110 전문 분야 규칙

- 선택 시점: 새 `GameState`의 Day 1 Market, 첫 구매 전.
- 잠금: `selectedGenreId`가 채워지면 현재 run에서 재선택 불가.
- 전문 분야 선택 시 해당 장르 레시피만 주문 후보가 된다. 제네럴리스트는 6개 전체 레시피를 사용한다.
- 전문 분야 Market은 해당 장르 레시피가 실제로 요구하는 재료만 구매 목록에 표시한다. 제네럴리스트는 기존 전체 재료를 표시한다.
- 양수 반올림은 공통 helper `RoundHalfUp(x) = floor(x + 0.5)`를 사용한다. 기본 banker rounding이나 런타임별 기본값에 의존하지 않는다.
- 원가: `RoundHalfUp(baseUnitCost × GenreDef.CostMultiplier) × quantity`를 사용한다.
- 객단가: `RoundHalfUp(RecipeDef.BasePrice × partySize × GenreDef.PricePerCustomerMultiplier)`를 사용한다.
- 회전율 기반 주문 수: `clamp(RoundHalfUp(5 / CookTimeMultiplier), 4, 6)`으로 계산한다.
  - 국밥 4건, 분식 6건, 면류 5건, 제네럴리스트 5건이 현재 시드값의 기대 결과다.
- 고객 weight: `CustomerArchetypeDef.BaseSpawnWeight × selectedGenre affinity`를 사용한다.
- 표시: 전문 분야 카드에는 `원가`, `1인 예상 가격 범위`, `예상 주문 수`, `주 고객층 2종`을 문장과 아이콘으로 보여준다. Market에서는 party size를 포함한 주문 총액을 “객단가”라고 부르지 않는다.
- 숫자를 숨기지 않는다. 배수 원문 대신 플레이어 친화 문구와 예상 범위를 우선 표시하고 상세보기에서 수치를 제공한다.
- task-110 승인 seed는 아래 값으로 고정한다. Claude가 구현 중 임의 재밸런싱하지 않는다.

| 전문 분야 | cost | time | price | 주문 수 | 100-day 평균 기여이익(설계 계산) |
|-----------|-----:|-----:|------:|--------:|--------------------------------:|
| 국밥 | 1.15 | 1.20 | **0.95** | 4 | 약 49,266원 |
| 분식 | 0.85 | 0.80 | **1.05** | 6 | 약 48,532원 |
| 면류 | 0.95 | 1.00 | 0.95 | 5 | 약 49,949원 |
| 제네럴리스트 | 1.00 | 1.00 | 1.00 | 5 | 약 51,581원 |

- 국밥·분식의 price multiplier가 직관과 반대로 보이는 이유는 recipe base price 자체가 이미 국밥 9,000~11,000원, 분식 4,500~5,000원으로 다르기 때문이다. 화면에는 multiplier가 아니라 최종 1인 가격 범위를 표시한다.
- balance guard는 100개 결정론적 day, C급, 모든 주문 성공, 실제 recipe 요구량 기준으로 계산한다. `일일 기여이익 = 장르 적용 매출 - 장르 적용 소비 재료 원가`이며 specialist 3종+제네럴리스트 평균의 max/min 비율은 1.10 이하로 고정한다.
- 생존 guard는 각 장르의 Day 1 전체 주문용 C급 이론 구매비가 시작 자금 30,000원 이하여야 하고, Day 1~3 완전 서빙 시 운영비 12,000원을 차감한 순이익이 매일 0원보다 커야 한다.
- 지배적 선택을 막기 위해 국밥은 1인 가격, 분식은 주문 수·낮은 원가, 면류는 중간 변동성, 제네럴리스트는 recipe 다양성에서만 우위를 갖고 모든 축을 동시에 이기는 장르는 허용하지 않는다.

#### D6. 수요 예측과 주문 생성

- `GenreDemandPlan`은 `genreId`, `day`, 주문 수, 정렬된 허용 recipe ID 목록, 정렬된 고객 weight rows, 표시용 상위 고객 2종, `min/maxPricePerCustomer`를 가진 순수 DTO다.
- 같은 genre/day/정의 목록이면 같은 plan과 주문 순서가 나온다.
- customer/recipe 입력은 ID ordinal 오름차순으로 정렬한다. 중복 ID, null 정의, 일치 recipe 없음, 누락 affinity, multiplier가 0 이하·NaN·Infinity, 최종 weight 합 0이면 plan 생성은 명시적 실패다. neutral fallback으로 조용히 진행하지 않는다.
- customer weight는 `RoundHalfUp(baseSpawnWeight × affinity × 1000)`의 양수 정수 milli-weight로 고정한다. 동률 forecast는 customer ID ordinal 오름차순으로 푼다.
- 고객 roll seed는 UTF-8 문자열 `genreId|day|orderIndex`에 32-bit FNV-1a(offset `2166136261`, prime `16777619`, unchecked)를 적용한다. `roll = seed % totalMilliWeight`로 두고 누적합이 roll을 처음 초과하는 고객을 선택한다.
- recipe는 정렬된 허용 목록에서 `(day - 1 + orderIndex) % recipeCount`, party size는 기존 규칙 `min + (day - 1 + orderIndex) % span`을 유지한다.
- “동일 plan”은 JSON byte가 아니라 scalar 값과 모든 ordered list의 field-by-field 동등성을 뜻한다.
- 예측 UI는 주문 수와 주요 고객층만 공개하고 정확한 레시피·순서는 공개하지 않는다.
- task-111에서 SNS modifier, task-112에서 event modifier를 `DayModifier`로 합성할 수 있게 순수 입력을 분리한다.

#### D7. Service

- 식당 영업은 주문 확인, 서빙/포기라는 경영 명령으로 유지한다. task-110에서는 C급이 고정이고 품질 수학 승인 후 등급 선택을 다시 노출한다.
- 전문 분야가 바뀐 판매가와 주문 수를 Service UI에 표시한다.
- 직접 조리 애니메이션은 0.7~1초의 짧은 표현이며 규칙 상태를 막지 않는다.
- 서빙 성공은 `음식 팝 → 손님 반응 → 매출 팝` 3박자로 0.6초 안에 시작한다.
- 고객 친화도는 주문 생성에, 품질·가격 반응은 task-후속 만족도 계산에 사용한다.

#### D8. Settlement

- 매출, 구매비, 운영비, 순이익, 잔액을 기존처럼 day당 한 번만 확정한다.
- task-110에서는 `전문 분야 효과: 주문 수/객단가/원가`를 원인 한 줄로 추가한다.
- 후속에서는 유명도와 평판을 분리한다.
  - `fame`: SNS·대회·화제성으로 변하며 손님 수와 초대 조건에 영향.
  - `reputation`: 만족·품질·일관성으로 변하며 가격 신뢰와 장슐랭 조건에 영향.
- 모든 milestone·대회 보상은 claimed ID로 중복 지급을 막는다.

#### D9. SNS

- 채널 3종 × 연령·성별 target, 비용, reach, 반복 감쇠를 가진다.
- 결과는 다음 날 고객 수와 분포에 적용하고, 유입 손님에 `SNS 유입` 태그를 표시한다.
- follower는 MVP에서 별도 화폐가 아니라 fame에서 파생한 표시값으로 둔다.
- 같은 캠페인을 반복하면 수확체감하고, 채널마다 강한 고객층이 다르다.

#### D10. 이벤트

- 데모 하드캡은 재료값 폭등, 위생 점검, 임대료 인상, 단체 손님 4종이다.
- 푸드트럭 정식 전환 시 임대료 인상은 `영업지 사용료/허가비 인상`으로 재해석할 수 있다.
- 사건은 가능하면 전날 예고하고, 결과 화면에 영향을 받은 수치를 표시한다.
- 이벤트는 `ActiveEventState(id, remainingDays)`로 저장하고 효과 합성은 순수 `EventOps`가 담당한다.

#### D11. 요리대회

- 대회는 식당의 하루 phase가 아니라 별도 모드다. 정식 구현 시 `Competition` 씬 추가가 가장 깨끗하며 하드캡 변경 승인이 필요하다.
- 규모는 동일 엔진을 데이터로 강화한다.
  - 소규모: 한인타운/동네 예선, 기본 레시피와 넓은 판정 구간.
  - 중규모: 뉴욕 도시 대회, 테마·시간·심사 성향 추가.
  - 대규모: 전국 무대, 복합 레시피·좁은 판정·라이벌 점수.
- 직접 조리는 `메뉴 선택 → 준비 타이밍 → 가열 조절 → 플레이팅 → 심사`의 공통 상태 머신만 사용한다.
- 자유 칼질, 물리 드래그, 레시피마다 다른 전용 미니게임은 만들지 않는다.
- 점수 초안은 재료 품질 25, 준비 정확도 25, 가열 25, 플레이팅 15, 카테고리 숙련 10이다.
- 대회 보상은 fame, reputation, 레시피/카테고리 해금, 다음 규모 초대다. 식당 수요에 돌아오지 않는 독립 미니게임이 되어서는 안 된다.

#### D12. 장슐랭

- 장슐랭은 가상의 음식 평가 가이드이며 최종 캠페인 목표다. 내부 ID는 명칭 변경에 안전한 `grand_award`를 사용한다.
- 단일 대회가 아니라 캠페인 전반을 평가한다: 맛/품질, 일관성, 한식 정체성, 손님 신뢰, 지속 가능한 경영.
- 별 1개는 우수한 가게, 2개는 찾아갈 가치가 있는 가게, 3개는 여행할 가치가 있는 상징적 가게라는 패러디 문법을 사용하되 공개 문구는 별도 검토한다.
- 장슐랭만이 유일한 좋은 엔딩은 아니다. 동네의 사랑을 선택한 엔딩과 지속 가능한 경영 엔딩도 긍정적으로 취급한다.

#### D13. 진행과 보상

| 시점 | 보상 | 다음 행동을 만드는 이유 |
|------|------|--------------------------|
| 주문 | 매출·표정·한마디 | 즉시 잘했다는 느낌 |
| 하루 | 흑자/적자 원인·완판 도장 | 다음 날 전략 수정 |
| 3일 | 첫 단골·초대장 | 챕터 목표 확인 |
| 대회 | 유명도·평판·레시피 | 식당 수요 변화 |
| 장소 전환 | 무대·메뉴 슬롯·운영비 변화 | 성장의 물리적 증거 |
| 캠페인 | 장슐랭·복수 엔딩 | 플레이 스타일의 결론 |

#### D14. 주식·코인 아이디어 처리

- 현재 제품 기둥과 직접 연결되지 않아 주차한다.
- 같은 도파민은 `해산물 선계약`, `유행 메뉴 선투자`, `SNS 광고 몰아쓰기`, `지역 축제 참가비`로 구현한다.
- 향후 포함하더라도 실제 종목/화폐를 모사하지 않고, 식당 의사결정의 한정 이벤트로만 사용한다.

### E. UX/UI 설계

#### E1. 화면 흐름

```text
MainMenu
  → 오프닝 Story Overlay
  → Shop / Market
      → 전문 분야 선택(첫 run 1회)
      → 오늘 수요 예측
      → 재료 구매
  → Service
      → 주문/손님/재고 판단
  → Settlement
      → 원인별 결과
  → Night
      → SNS(후속) / 저장(후속)
  → 다음 Day 또는 3일 결과 카드
```

#### E2. 640×360 정보 위계

1. 상단 HUD: Day, phase, 현금, 선택 전문 분야.
2. 중앙 무대: 손님, 카운터, 음식, 반응.
3. 하단/우측 작업 패널: 현재 phase의 결정 하나.
4. 위험 정보: 운영비 부족, 재고 부족, 남은 주문.
5. 설명: 결과 원인과 다음 행동.

한 화면에서 네 개 이상의 동등한 강조점을 만들지 않는다. 현재 행동 버튼은 하나의 primary color만 사용한다.

#### E3. 전문 분야 선택 UI

- Market 첫 진입 시 기존 장보기 패널 위에 modal을 띄운다. `Panel_GenreSelection`은 640×360 canvas 중앙 기준 `anchoredPosition=(0,-20)`, `sizeDelta=(560,250)`이며 뒤 UI raycast를 차단한다.
- title은 `(0,90)/(520×26)/20pt`, 네 버튼은 x=`-180,-60,60,180`, y=`50`, 각 `110×32/15pt`로 고정한다.
- detail 영역은 `(0,-15)/(520×84)`, 선택명 16pt, 본문 13pt, 수치 12pt다. confirm은 `(0,-80)/(190×30)/15pt`, helper는 `(0,-115)/(520×18)/11pt`다. 버튼 하단과 detail 상단 사이에 최소 7px를 확보한다.
- 선택된 버튼만 Gochujang Red outline 2px와 아이콘을 사용하고 나머지는 Night Blue/Steam Cream 조합을 쓴다.
- `이 전문 분야로 시작` 확인 전 구매 버튼은 잠긴다.
- 확정 후 패널은 접히고 HUD badge와 `상세 보기`만 남는다.
- HUD `GenreBadge`는 `(80,150)/(150×30)/12pt`로 두어 기존 DayPhaseText와 AdvanceButton 사이를 사용한다.
- 확정 후 전문 분야와 무관한 재료 행은 숨기고, 제네럴리스트만 전체 재료 행을 표시한다.
- 색과 함께 bowl/skewer/noodle/mixed 아이콘을 사용한다.
- focus 순서는 국밥→분식→면류→균형→확정이며 좌우 방향키와 Tab을 지원한다. modal은 필수 선택이므로 취소 버튼을 두지 않는다.

| 버튼 | headline | 비교 문구 | forecast 문구 |
|------|----------|-----------|---------------|
| 국밥 | `묵직한 한 그릇` | `원가 높음 · 1인 가격 높음 · 주문 4건` | `주 고객: 직장인 · 동네 어르신` |
| 분식 | `싸고 빠른 회전` | `원가 낮음 · 1인 가격 낮음 · 주문 6건` | `주 고객: 학생 · 직장인` |
| 면류 | `균형 잡힌 운영` | `원가 보통 · 1인 가격 보통 · 주문 5건` | `주 고객: 직장인 · 가족` |
| 균형 | `메뉴 폭으로 승부` | `장르 배수 없음 · 주문 5건` | `주 고객: 직장인 · 학생` |

#### E4. 도파민 피드백

| 순간 | 시간 | 시각·음향 | 정보 |
|------|------|-----------|------|
| 전문 분야 확정 | 0.25s | 카드 snap, 작은 도장음 | 선택이 run 동안 유지됨 |
| 구매 | 0.2s | 현금 감소·재고 증가 동시 pulse | 장르 원가 반영 |
| 주문 입장 | 0.4s | 손님 이동·주문 bubble | 고객/메뉴/가치 |
| 서빙 성공 | 0.6s 시작 | 음식 pop·표정·매출 pop | 성공 이유 한 문장 |
| 완판 | 0.5s | `오늘 완판` 도장 | 경제 보너스 없이 성취만 제공 |
| 정산 | 1.0~1.5s | 수치 count-up, 흑/적자 색 | 원인 분해 |
| 3일 완료 | 2~4s | 첫 단골·초대장 카드 | 다음 목표 |

#### E5. 접근성·현지화

- 긍정/부정은 green/red만이 아니라 `+/-`, 아이콘, 문구를 함께 사용한다.
- 핵심 버튼은 키보드 focus 순서를 갖고 마우스 없이 선택 가능하게 확장할 수 있어야 한다.
- 화면 흔들림·점멸은 기본 약하게 하고 옵션에서 줄일 수 있게 설계한다.
- 한글과 영어가 혼재하는 뉴욕 배경은 분위기로 사용하되 핵심 규칙은 선택 언어 하나로 일관되게 표시한다.
- 텍스트는 TMP Dynamic Galmuri를 사용한다. 이 modal의 영어 탭은 폭을 늘리지 않고 `Soup / Street / Noodles / Mixed`의 짧은 label을 사용하며, detail 본문만 영어 길이에 맞춰 줄바꿈한다. 일반 UI는 영어 텍스트 30% 길이 증가를 가정한다.

### F. 아트·오디오 디렉션 — Codex 소유

#### F1. 시각 정체성

- 최종 목표는 **현대 뉴욕의 차가운 밤 + 한식당의 따뜻한 증기**다.
- 화면은 실내의 amber, 거리의 navy, 음식의 gochujang red를 대비시킨다.
- 과도한 에도풍·판타지 의상은 최종 아트에서 제거한다. 현재 CC0 팩은 동작/가독성 prototype으로만 사용한다.
- 픽셀 크기는 캐릭터 32×48 또는 32×32 계열을 기준으로 하고, 정수배 표시와 제한 팔레트를 유지한다.

#### F2. 기준 팔레트

| 역할 | 색상 | 용도 |
|------|------|------|
| Ink Navy | `#16202A` | 본문·윤곽·깊은 그림자 |
| Night Blue | `#253B56` | 뉴욕 야경·비활성 패널 |
| Steam Cream | `#F4E5C2` | 종이·증기·따뜻한 배경 |
| Gochujang Red | `#D34A3A` | primary action·음식 accent |
| Brass Amber | `#E5A84B` | 수익·조명·성취 |
| Jade Green | `#4FAE82` | 긍정 상태·신선도 |
| Warning Plum | `#A93E58` | 적자·위기·이탈 |

#### F3. 아트 제작 흐름

1. Codex가 캐릭터 실루엣, 푸드트럭/식당 무대, 음식, HUD 목업의 콘셉트 시안을 만든다.
2. 오너가 한 방향을 승인한다.
3. Pixelorama에서 32px grid, 팔레트, 투명도, 프레임을 정리한다.
4. Claude가 Unity import/슬라이스/catalog/SceneBuilder에 통합한다.
5. Codex가 원본 크기와 2×/4× 확대 화면에서 가독성·톤·애니메이션을 리뷰한다.

#### F4. 오디오

- 첫 우선순위는 구매, 서빙 성공, 정산 세 효과음이다.
- 배경은 지하철 저음, 거리 ambience, 주방 끓는 소리, 늦은 밤의 잔잔한 음악을 사용한다.
- 대회는 같은 악기 테마의 tempo와 percussion만 높여 별도 게임처럼 분리되지 않게 한다.
- 데모 에셋은 CC0 provenance를 기록한다.

### G. 기술 설계

#### G1. 현재 기준선

- `GameState`에 day/phase/cash/inventory/service/settlement/bankruptcy가 존재한다.
- 6개 SO 정의 타입, 기존 manager/controller, `GameEvents`, SceneBuilder, 97개 EditMode 테스트 기반을 재사용한다.
- SNS/Event는 정의 데이터만 있고 런타임은 후속이다.
- 저장·유명도·평판·커리어·대회는 아직 없다.

#### G2. 목표 의존 방향

```text
ScriptableObject Definitions / future GameCatalog
                    ↓
           Serializable GameState
                    ↓
        Pure Ops + Result/Plan DTO
                    ↓
   Existing Managers / future Coordinators
                    ↓
        UI Controllers / Presentation
```

- UI가 SO 배수를 직접 계산하지 않는다.
- `ServiceOps`가 SNS/Event manager를 직접 찾지 않는다. 후속 `DayPlan`이 modifier 결과를 고정해 전달한다.
- 저장 후 재개해도 같은 DayPlan/주문이 나오도록 plan을 상태에 저장하거나 같은 입력으로 재생성 가능하게 한다.

#### G3. task-110 신규 계약

```text
GameState.selectedGenreId
        ↓
GenreSelectionOps → GenreDemandPlan
        ├─ EconomyOps 구매 원가
        ├─ ServiceOps 레시피/고객/주문 수
        └─ UI forecast / HUD / 정산 설명
```

- `GenreSelectionOps`는 선택 가능 여부, 장르 조회, 주문 수, customer weights, forecast를 제공한다.
- `GenreDemandPlan`은 Unity 타입을 포함하지 않는 순수 C# DTO다.
- 기존 Economy/Service public API는 neutral overload로 보존한다.
- 선택 실패는 상태를 변경하지 않고 명시적 result/message를 반환한다.
- `GameManager`가 정렬된 `List<GenreDef>` catalog의 유일한 런타임 소유자다. `SceneBuilder.CreateGameManager()`가 MainMenu와 Shop 모두 같은 목록을 `EditorInit`으로 주입하고, MainMenu에서 생존한 persistent instance도 lookup을 잃지 않는다.
- `ServiceManager`는 정렬된 recipe/customer 목록을 `EditorInit`으로 주입받고 순수 검증 `TryBuildDayPlan(out plan, out reason)`과 원자적 초기화 `TryStartServiceDay(out reason)`를 제공한다. MainMenu와 Shop 양쪽 bootstrap에 같은 목록을 주입해 persistent instance에서도 plan validation이 가능해야 한다.
- `GameManager.TryGetGenre(id, out def)`와 `CanAdvancePhase(out reason)`를 제공한다. Market→Service는 선택 ID가 catalog에 존재하고 `TryBuildDayPlan`이 성공해야 한다. `AdvancePhase()`는 Market→Service 직전에 `TryStartServiceDay`가 plan 생성과 `ServiceOps.StartServiceDay`를 성공시킨 경우에만 phase event를 발행한다.
- Service→Settlement는 `serviceDay == day`, 생성 주문 수가 현재 plan의 `orderCount`와 일치, 열린 주문 없음의 세 조건을 모두 요구한다. UI lifecycle은 주문을 생성하지 않으며 `ServicePanelController.OnEnable`은 표시 refresh만 수행한다.
- `EconomyManager.TryPurchaseIngredient`는 선택 ID가 없거나 catalog에서 찾지 못하면 `PurchaseResult` 실패를 반환하고 현금·재고·지출을 바꾸지 않는다.
- `PhaseHudController`는 bankrupt 여부와 `CanAdvancePhase`를 **하나의 식**으로 계산한다. 현재처럼 다른 controller가 끈 버튼을 다음 프레임에 다시 켜는 비교식을 사용하지 않는다.
- 선택 성공 시 기존 `GameEvents`에 `GenreSelected(string genreId)`를 발행해 HUD badge, Market panel, advance gate를 즉시 refresh한다.
- 실제 서빙 transaction은 `ServiceOps.TryServeCurrentOrder(state, recipe, grade, genre)` 한 경로에서 장르 판매가를 계산해 `cash`, `serviceRevenueToday`, `ServiceResult.RevenueGained`를 동시에 갱신한다. UI 예상가와 실제 transaction도 같은 Ops helper를 사용한다.

#### G4. 장기 GameState 목표

저장 구현 전 구조화 후보는 다음과 같다. task-110에서는 전체 리팩터링하지 않는다.

```text
GameState
├─ schemaVersion
├─ CareerState(stage, fame, reputation, milestones)
├─ RestaurantState(day, phase, cash, inventory, plan, settlement)
├─ SocialState(campaign usage, pending effects)
├─ EventRuntimeState(active events)
└─ CompetitionProgressState(unlocks, best scores, mastery)
```

### H. task-110 상세 구현 순서

1. Claude는 코드 변경 전에 scaffold `manifest.md`의 `inputs`, `concepts_needed`, `related_files`, `notes` placeholder를 이 문서의 Inputs·영향 파일·검증 명령으로 채운다.
2. 현재 97개 EditMode 기준선을 실행해 결과 XML과 실패 여부를 구현 노트에 기록한다.
3. `GameState`에 `selectedGenreId`를 추가한다. 빈 문자열은 미선택, SO 직접 참조와 enum 직렬화 추가는 금지한다.
4. 순수 DTO `GenreSelectionResult`와 `GenreDemandPlan`을 만든다. plan에는 genre ID, orderCount, ordered recipe IDs, ordered customer milli-weight rows, top customer IDs, min/max 1인 가격을 둔다.
5. `GenreSelectionOps.TrySelect`를 만든다. Day 1 Market, 첫 구매 전, 미선택 상태에서만 성공하고 재선택/잘못된 ID/다른 phase는 상태 불변 실패다.
6. `TryBuildDemandPlan`을 만든다. specialist는 matching recipes, generalist는 전체 recipes를 ID ordinal로 사용하고 잘못된 정의는 명시적 failure reason을 반환한다.
7. 공통 `RoundHalfUp(x) = floor(x + 0.5)` helper와 주문 수 공식 `clamp(RoundHalfUp(5 / cookTimeMultiplier), 4, 6)`을 구현한다.
8. 고객 milli-weight, UTF-8 FNV-1a seed `genreId|day|orderIndex`, 누적 구간 pick, 기존 recipe round-robin·party-size 산식을 D6 계약 그대로 구현한다.
9. `GameManager`에 정렬된 genre catalog `EditorInit`, `TryGetGenre`, `CanAdvancePhase(out reason)`를 추가하고 `ServiceManager`에 정렬된 recipe/customer `EditorInit`, `TryBuildDayPlan`, `TryStartServiceDay`를 추가한다. `AdvancePhase`가 Market→Service 직전에 주문 초기화를 원자적으로 완료하고 Service→Settlement에서 day/orderCount/open-order를 모두 검증하게 한다.
10. `GameEvents.GenreSelected`를 추가하고 `PhaseHudController`가 selection 직후 badge와 advance interactable을 갱신하게 한다. interactable은 `!bankrupt && CanAdvancePhase` 단일 식으로 계산한다.
11. `EconomyOps.CalculatePurchaseCost/TryPurchaseIngredient`와 `EconomyManager`에 `GenreDef` 경로를 추가한다. Market 예상가와 transaction은 동일 helper를 사용하며 기존 overload는 1.0 neutral로 유지한다.
12. `ServiceOps.BuildOrders/CalculateSalePrice/TryServeCurrentOrder`와 `ServiceManager`에 genre/plan 경로를 추가한다. `ServicePanelController.OnEnable`의 주문 생성 책임은 제거하고 표시 refresh만 남긴다. 장르 가격은 실제 `cash`, `serviceRevenueToday`, `ServiceResult.RevenueGained`에 한 번만 반영한다.
13. `MarketPanelController`에 4개 장르 선택, 상세/forecast, confirm, 잠금 상태를 추가한다. 미선택 시 구매/phase 진행을 막고, 확정 후 specialist는 plan recipe가 요구하는 C급 재료만 표시하며 B급 control은 숨긴다.
14. `ServicePanelController`는 현재 주문의 실제 1인 가격과 파티 포함 예상 주문 총액을 같은 Ops 경로로 표시한다. `PhaseHudController`는 선택 genre와 주문 수를 badge에 표시한다.
15. `SettlementPanelController`에 전문 분야가 실제 원가·주문 수·매출에 미친 방식 한 줄을 추가한다. 정산 수학과 day 멱등성은 변경하지 않는다.
16. `InitialDataBuilder`는 기존 4개 `GenreDef` 수치와 description을 task-110 공식값으로 GUID를 보존해 upsert한다.
17. `SceneBuilder.CreateGameManager`가 MainMenu와 Shop 모두 같은 genre catalog를 persistent GameManager에 주입한다. 장르 modal·badge·controller 참조는 한 SceneBuilder unit에서 최종 wiring한다.
18. `FirstPlayableLoopTests` fixture가 플레이어 경로로 장르를 먼저 선택하도록 갱신하고, 순수 neutral Ops 테스트는 기존 결과를 유지한다.
19. 장르 수학, transaction 일치, persistent catalog, phase gate, scene/UI, 데이터 무결성, 회귀 테스트를 추가한다.
20. `InitialDataBuilder.Apply → SceneBuilder.Apply → compile → EditMode → PlayMode` 순으로 배치 검증한다.
21. 수동 Play smoke에서 첫 선택 60초 이내, 선택 전 구매·Service 차단, 네 장르의 주문 수/고객/1인 가격 차이, 실제 transaction과 UI 일치, 정산 원인 문구를 확인한다.

### I. 제작 로드맵

| 단계 | 포함 | 성공 게이트 | 제외 |
|------|------|-------------|------|
| M2 task-110 | 전문 분야·수요 예측·장르 인과 | 1일 집중 테스트에서 선택 결과를 설명 가능 | SNS/이벤트/저장 |
| M2 task-111 | SNS → 익일 고객 변화 | 선택과 다음 날 인과 인지 | 대회 |
| M2 task-112 | 이벤트 4종 | 예고된 위기 대응 | 새 이벤트 추가 |
| M2 통합 게이트 | 장르+SNS+이벤트 3일 루프 | 10~15분 완주·원인 설명·재도전 의향 | 기능 수 추가 |
| M3 task-113~115 | 저장·아트 마감·B급 품질 결정·3일 엔딩·빌드 | 10~15분 데모 재도전 의향, B급 재노출 또는 공식 제거 | 푸드트럭 규칙·대회·장슐랭 |
| Post-demo Vertical Slice | 푸드트럭 1일 → 식당 2~3일 → 지역 대회 1회 | 두 루프가 서로 보상 | 중/대규모 대회 |
| 1.0 | 전체 캠페인·대회 3단계·장슐랭·복수 엔딩 | 8~12시간 완주와 전략 다양성 | 직원·다점포·자유 건축 |
| 출시 후 | 새 레시피·라이벌·심사 규칙 | 기존 코어 재사용 | 코어 검증 전 선행 개발 금지 |

### J. PPTX 18장 제작 사양

**커뮤니케이션 목표:** 발표가 끝나면 오너·구현 협업자·잠재 협력자는 `Client is King`이 왜 차별화되는지, 플레이어가 무엇을 반복하는지, 데모에서 무엇을 만들고 무엇을 미루는지 이해하고 task-110 구현을 승인해야 한다.

**서사:** 포기한 꿈 → 차별화된 플레이어 판타지 → 핵심 루프 → 시스템 증거 → 비주얼 정체성 → 구현 가능한 범위 → 다음 행동.

**덱 테마:** 16:9, Steam Cream 배경, Ink Navy 본문, Gochujang Red 단일 강조색, Night Blue 사진/픽셀아트 프레임. 제목 35pt 이상, 본문 16pt 이상, 한 슬라이드 한 주장. UI 카드 그리드를 반복하지 않고 큰 이미지·루프·타임라인·표를 교차 사용한다.

| # | 주장형 제목 | 핵심 내용 | 주 비주얼 |
|---|-------------|-----------|-----------|
| 1 | 포기했던 꿈이 뉴욕 한인타운에서 다시 끓기 시작한다 | 제목·로그라인 | 밤의 푸드트럭 hero concept |
| 2 | 이 게임의 판타지는 요리가 아니라 ‘손님을 읽는 사장’이다 | player fantasy | 주인공과 손님 군중 대비 |
| 3 | 직접 조리 경쟁작 사이에 경영 중심의 공백이 있다 | 경쟁작·차별화 | 2×2 포지셔닝 맵 |
| 4 | 네 개의 디자인 기둥이 모든 기능을 통제한다 | 인과·온기·생존·한식 | 큰 단어+상징 4개 |
| 5 | 푸드트럭에서 장슐랭까지 한 방향으로 성장한다 | 캠페인 arc | 6단계 수평 타임라인 |
| 6 | 하루는 준비한 판단이 결과로 되돌아오는 실험이다 | Market→Service→Settlement→Night | 원형 루프 |
| 7 | 10~15분의 3일이 전체 게임의 재미를 증명한다 | Day 1~3 vertical slice | 3열 일정 |
| 8 | 전문 분야 하나가 원가·회전율·객단가·손님을 바꾼다 | task-110 장르 | 4개 선택 비교표 |
| 9 | 손님은 숫자가 아니라 기억나는 이유를 남긴다 | 고객 4종·반응 | 캐릭터 lineup |
| 10 | SNS는 보너스 버튼이 아니라 내일의 군중을 설계한다 | targeting causality | 전날 선택/다음 날 변화 split |
| 11 | 위기는 예고되고, 실패는 배우고 회복할 수 있어야 한다 | 이벤트·경제 | 원인→결과→회복 흐름 |
| 12 | 직접 요리는 가장 중요한 순간인 대회에만 등장한다 | competition state machine | 5단계 조리 흐름 |
| 13 | 대회 보상이 식당으로 돌아와야 두 게임이 하나가 된다 | fame/reputation loop | 식당↔대회 순환 |
| 14 | 장슐랭은 별보다 ‘어떤 가게가 되었는가’를 평가한다 | 최종 평가·복수 엔딩 | 5개 평가 축 |
| 15 | 뉴욕의 차가운 밤과 한식의 따뜻한 증기가 시각 정체성이다 | art direction | palette+moodboard |
| 16 | 화면은 한 번에 하나의 결정만 요구한다 | HUD·phase UI hierarchy | 640×360 wireframe |
| 17 | 기존 Unity 루프를 보존해 확장 위험을 낮춘다 | architecture·current/reuse | 정의→state→ops→UI 구조 |
| 18 | 지금 만들 것은 장르 인과, 다음은 SNS의 지연 보상이다 | scope·next action | M2/M3 로드맵과 승인 요청 |

## 실행 계획 (Execution Plan)

- implement_model: claude-fable-5
- implement_effort: high
- routing_reason: 기존 순수 Ops·GameState·manager·UI·SceneBuilder를 모두 건드리지만 신규 씬/매니저 없이 장르 수학과 결정론적 주문 생성에 집중하는 M2 단일 시스템 작업이므로 프로젝트 SSOT의 fable-5/high 라우팅을 유지한다.

| unit | 파일 범위 | depends_on | group |
|------|-----------|------------|-------|
| U1-domain-contract | `GameState`, 신규 `GenreSelectionResult/GenreDemandPlan/GenreSelectionOps` | 없음 | G1 |
| U2-economy-service-math | `EconomyOps`, `ServiceOps` overload와 순수 수학 테스트 | U1-domain-contract | G2 |
| U3-manager-wiring | `GameManager`, `EconomyManager`, `ServiceManager`의 genre lookup/plan 전달 | U1-domain-contract, U2-economy-service-math | G3 |
| U4-ui-controllers | `MarketPanelController`, `PhaseHudController`, `ServicePanelController`, `SettlementPanelController` | U3-manager-wiring | G4 |
| U5-scene-data-wiring | `SceneBuilder` 단독 최종 wiring 소유, `InitialDataBuilder`, 씬 산출물 | U4-ui-controllers | G5 |
| U6-data-tests | data/scene/ops/transaction/balance/loop 회귀 테스트 | U2-economy-service-math, U3-manager-wiring, U5-scene-data-wiring | G6 |
| U7-validation | builder·compile·EditMode·수동 smoke·validator | U6-data-tests | G7 |
| U8-implementation-records | Claude 소유 구현 노트·summary·status 갱신 | U7-validation | G8 |

## 파일/모듈 영향 (Affected Files/Modules)

| 파일/모듈 | 변경 유형 | 설명 |
|-----------|-----------|------|
| `game/Assets/Scripts/Runtime/DayCycle/GameState.cs` | modify | `selectedGenreId` 문자열 상태 추가 |
| `game/Assets/Scripts/Runtime/DayCycle/GameEvents.cs` | modify | `GenreSelected` event 추가, 기존 phase/presentation event 유지 |
| `game/Assets/Scripts/Runtime/Genre/GenreSelectionResult.cs` | create | 선택 성공/실패와 메시지 DTO |
| `game/Assets/Scripts/Runtime/Genre/GenreDemandPlan.cs` | create | ordered recipe/customer rows, 주문 수, min/max 1인 가격 forecast DTO |
| `game/Assets/Scripts/Runtime/Genre/GenreSelectionOps.cs` | create | 선택/정의 validation, 배수, 주문 수, FNV-1a 수요 plan |
| `game/Assets/Scripts/Runtime/Economy/EconomyOps.cs` | modify | genre 원가 multiplier overload, neutral 기존 API 유지 |
| `game/Assets/Scripts/Runtime/Economy/EconomyManager.cs` | modify | 선택 genre를 구매 transaction에 전달 |
| `game/Assets/Scripts/Runtime/Service/ServiceOps.cs` | modify | plan 기반 recipe/customer/orderCount와 genre 적용 실제 서빙 transaction |
| `game/Assets/Scripts/Runtime/Service/ServiceManager.cs` | modify | persistent recipe/customer 주입, plan 사전검증·생성, genre 적용 서빙 전달 |
| `game/Assets/Scripts/Runtime/Managers/GameManager.cs` | modify | persistent genre catalog, `TryGetGenre`, Market/Service phase domain gate |
| `game/Assets/Scripts/Runtime/UI/MarketPanelController.cs` | modify | 장르 4선택, 상세/forecast, confirm, 구매 잠금 |
| `game/Assets/Scripts/Runtime/UI/PhaseHudController.cs` | modify | 선택 전문 분야 badge 표시 |
| `game/Assets/Scripts/Runtime/UI/ServicePanelController.cs` | modify | 1인 가격·파티 포함 예상 주문 총액·전문 분야 설명 |
| `game/Assets/Scripts/Runtime/UI/SettlementPanelController.cs` | modify | 전문 분야 결과 원인 한 줄 |
| `game/Assets/Scripts/Editor/InitialDataBuilder.cs` | modify | 기존 GenreDef 4종 task-110 공식값/문구 멱등 upsert |
| `game/Assets/Scripts/Editor/SceneBuilder.cs` | modify | 선택 UI 생성·참조 주입·2씬 재생성 |
| `game/Assets/Scenes/Shop.unity` | modify | SceneBuilder 산출 장르 선택/forecast UI |
| `game/Assets/Scenes/MainMenu.unity` | modify | SceneBuilder 재실행 산출 가능, 기능 변경 없음 |
| `game/Assets/Tests/EditMode/GenreSelectionOpsTests.cs` | create | 선택 게이트·수학·결정론·상태 불변 검증 |
| `game/Assets/Tests/EditMode/GenreBalanceTests.cs` | create | 100-day 이론 기여이익·지배적 장르 방지 guard |
| `game/Assets/Tests/EditMode/EconomyManagerTests.cs` | modify | genre 적용 구매 원가·neutral 회귀 |
| `game/Assets/Tests/EditMode/ServiceOpsTests.cs` | modify | genre recipe 필터·주문 수·customer weight·가격 |
| `game/Assets/Tests/EditMode/MarketPanelSceneTests.cs` | modify | 선택 UI·구매 잠금·forecast wiring |
| `game/Assets/Tests/EditMode/ServicePanelSceneTests.cs` | modify | genre 표시·가격 표시·기존 서빙 회귀 |
| `game/Assets/Tests/EditMode/SettlementPanelSceneTests.cs` | modify | 장르 원인 문구·정산 멱등성 회귀 |
| `game/Assets/Tests/EditMode/SceneBuilderTests.cs` | modify | 2씬 하드캡·장르 UI 멱등 생성 |
| `game/Assets/Tests/EditMode/FirstPlayableLoopTests.cs` | modify | 플레이어 경로 fixture가 장르 선택 후 구매·영업하도록 갱신, neutral Ops 회귀 유지 |
| `game/Assets/Tests/PlayMode/ClientIsKing.Tests.PlayMode.asmdef` | create | persistent scene 전환 통합 테스트 assembly |
| `game/Assets/Tests/PlayMode/GenrePersistencePlayModeTests.cs` | create | MainMenu→Shop instance 생존·catalog·plan·원자적 주문 초기화 검증 |
| 관련 `game/**/*.meta` | create/modify | 신규 Genre 폴더·스크립트·테스트·씬의 Unity 메타 쌍 |

## 테스트 기준 (Test Criteria)

- [ ] `python -B runtime/validator/cli.py kb/tasks/task-110/design.md`가 종료 코드 0으로 통과한다.
- [ ] 구현 전 현재 EditMode 기준선을 실행하고 테스트 속성 97개 기준의 실제 결과를 구현 노트에 기록한다.
- [ ] 새 `GameState`의 `selectedGenreId` 기본값은 빈 문자열이고 SO/Unity 오브젝트를 참조하지 않는다.
- [ ] Day 1 Market의 첫 구매 전에는 네 장르 중 하나를 선택할 수 있다.
- [ ] 이미 선택됨, Day 1이 아님, Market이 아님, 구매 지출이 발생함, 알 수 없는 genre ID인 경우 선택이 실패하고 `GameState`가 완전히 불변이다.
- [ ] 국밥/분식/면류 선택은 matching recipe 2종만 plan 후보로 사용하고 제네럴리스트는 기존 6종 전체를 사용한다.
- [ ] specialist Market에는 matching recipe가 요구하는 재료만 표시되고 제네럴리스트에는 기존 전체 재료가 표시된다.
- [ ] 현재 시드값에서 주문 수 공식 결과가 국밥 4, 분식 6, 면류 5, 제네럴리스트 5로 나온다.
- [ ] 같은 정의·genreId·day 입력은 scalar와 ordered rows가 field-by-field 동등한 `GenreDemandPlan`과 동일 주문 순서를 만든다.
- [ ] FNV-1a known vector `gukbap|1|0`의 unsigned 32-bit 결과가 `2190636514`이며 다른 런타임 실행에서도 동일하다.
- [ ] null/중복 ID, matching recipe 없음, 누락 affinity, 0 이하·NaN·Infinity multiplier, customer milli-weight 합 0은 모두 명시적 plan 실패가 되고 state/phase가 불변이다.
- [ ] 현재 시드의 최종 `baseSpawnWeight × affinity` 기준 forecast 상위 2종은 국밥=`office_worker, senior_regular`, 분식=`student, office_worker`, 면류=`office_worker, family_parent`, 제네럴리스트=`office_worker, student` 순서다.
- [ ] 장르 적용 구매비가 `RoundHalfUp(baseUnitCost × costMultiplier) × quantity`와 일치하고 자금 부족/잘못된 수량 실패 시 현금·재고·marketSpend가 불변이다.
- [ ] 장르 적용 판매가가 `RoundHalfUp(basePrice × partySize × priceMultiplier)`와 일치하고 기존 neutral overload 결과는 변경되지 않는다.
- [ ] Market forecast의 `min/maxPricePerCustomer`는 허용 recipe의 `RoundHalfUp(basePrice × priceMultiplier)` 최솟값/최댓값이고 party size를 포함하지 않는다.
- [ ] 장르 적용 서빙 1회에서 UI 예상 주문 총액, `cash` 증가, `serviceRevenueToday` 증가, `ServiceResult.RevenueGained`, Settlement gross revenue가 모두 동일한 장르 적용 판매가를 사용한다.
- [ ] MainMenu에서 시작해 Shop으로 이동한 경우와 Shop 씬 직접 실행 모두 persistent `GameManager`가 정렬된 genre 4종 catalog를 보유하고 같은 ID lookup 결과를 낸다.
- [ ] PlayMode에서 MainMenu의 `GameManager` instance와 `ServiceManager`가 Shop 로드 후에도 동일 instance로 생존하고 genre 4종, recipe 6종, customer 4종을 보유하며 `TryBuildDayPlan`이 성공한다.
- [ ] UI controller를 활성화하지 않고 장르 선택 후 `GameManager.AdvancePhase()`를 호출해도 Market→Service 전환 전에 `serviceDay`, plan 주문 수, 주문 목록이 원자적으로 초기화되고, 곧바로 다시 호출하면 열린 주문 때문에 Service에 머문다.
- [ ] specialist plan의 주문에는 다른 장르 recipe가 한 건도 없고 generalist plan은 장기 표본에서 6종을 모두 포함한다.
- [ ] `InitialDataBuilder`가 국밥 `(1.15,1.20,0.95)`, 분식 `(0.85,0.80,1.05)`, 면류 `(0.95,1.00,0.95)`, 제네럴리스트 `(1,1,1)`의 cost/time/price multiplier를 upsert한다.
- [ ] `GenreBalanceTests`의 100-day C급 전량 서빙 표본에서 네 장르 평균 기여이익 max/min 비율이 1.10 이하이고 각 평균이 D5 설계 계산값의 ±1% 안에 있다.
- [ ] 네 장르 모두 Day 1 전체 주문용 C급 이론 구매비가 시작 자금 30,000원 이하이고, Day 1~3 완전 서빙 시 운영비 12,000원 차감 후 순이익이 매일 양수다.
- [ ] 국밥/분식/면류/제네럴리스트 중 어느 하나도 원가·1인 가격·주문 수·recipe 다양성 네 축을 모두 동시에 지배하지 않는다.
- [ ] 장르를 선택하기 전 Market 구매 버튼과 Service 진입 버튼이 비활성이고, UI를 우회해 `EconomyManager`/`GameManager.AdvancePhase`를 직접 호출해도 구매·phase·event가 변하지 않는다.
- [ ] 열린 주문이 있으면 Service→Settlement 도메인 게이트가 진행을 막고, 모든 주문 처리 후에만 진행한다.
- [ ] task-110 UI에서는 B급 구매 행과 Service grade toggle이 노출되지 않고 C급으로만 거래되며, B급 정의 asset과 neutral Ops 회귀 테스트는 유지된다.
- [ ] 640×360 원본 크기 캡처를 Codex가 검토해 장르 modal, 상세 문구, forecast, HUD badge가 겹치거나 canvas 밖으로 넘치지 않고 지정 좌표·폰트·focus 순서와 일치한다고 승인한다.
- [ ] 확정 후 선택 패널은 compact badge로 접히며 현재 장르를 Market/Service/HUD에서 일관되게 표시한다.
- [ ] Settlement 결과에 장르가 원가·주문 수·객단가에 미친 영향 요약이 표시되며 실제 정산 적용은 day당 한 번뿐이다.
- [ ] `InitialDataBuilder.Apply`와 `SceneBuilder.Apply`를 연속 두 번 실행해도 asset GUID·오브젝트 수·직렬화 참조가 동일하고 persistent listener가 중복되지 않는다. 런타임 `OnEnable→OnDisable→OnEnable` 테스트는 클릭 1회당 callback 1회만 발생함을 별도로 검증한다.
- [ ] Build Settings에는 `MainMenu.unity`와 `Shop.unity` 두 씬만 존재한다.
- [ ] Unity 배치 compile이 종료 코드 0이고 로그에 `error CS`가 없다.
- [ ] `Unity.exe -batchmode -nographics -projectPath game -runTests -testPlatform EditMode -testResults <edit-results.xml> -logFile <edit-log>`가 종료 코드 0이다.
- [ ] `Unity.exe -batchmode -nographics -projectPath game -runTests -testPlatform PlayMode -testResults <play-results.xml> -logFile <play-log>`가 종료 코드 0이다.
- [ ] 전체 EditMode 테스트가 종료 코드 0으로 통과하고 기존 경제·인벤토리·서비스·정산·표현 테스트 회귀가 없다.
- [ ] 전체 PlayMode 테스트가 종료 코드 0으로 통과하며 `DontDestroyOnLoad`와 중복 bootstrap 제거 경로가 검증된다.
- [ ] 수동 Play smoke에서 첫 의미 있는 선택이 60초 안에 나오고, 네 선택 각각의 예상 주문 수·주 고객층·1인 가격 차이가 화면과 실제 transaction에서 확인된다.
- 후속 제품 검증(비차단): 초심자 5명 중 4명 이상이 선택 결과 두 가지를 설명하는 기준은 task-112 이후 3일 통합 플레이테스트에서 측정한다.
- [ ] `git status --short game`에 `Library`, `Temp`, `Obj`, `Logs`, `UserSettings`, `Build*`가 나타나지 않고 모든 신규 Unity 파일에 `.meta`가 존재한다.

## 오픈 이슈 (Open Issues)

- **제품 SSOT 승격**: 이 문서는 GDD v0.9 working baseline이며 기존 `project-brief.md`를 아직 대체하지 않는다. 오너가 제안 항목을 승인하면 별도 concept-update 작업으로 뉴욕 한인타운·주인공·푸드트럭·장슐랭·post-demo 대회 로드맵을 먼저 SSOT에 반영하고 `demo-scope.md`를 그다음 정렬해야 한다. task-110 장르 구현 자체는 기존 SSOT와 일치한다.
- **manifest 인계**: runner가 만든 `kb/tasks/task-110/manifest.md`는 Codex 단일 파일 소유 규약 때문에 이 turn에서 수정하지 않았다. Claude는 구현 시작 전에 H-1 계약대로 placeholder를 실제 inputs/concepts/related files/notes로 교체해야 한다.
- **정식 장소 진행**: 푸드트럭을 1.0에서 실제 1~2일 playable venue로 만들고 이후 식당으로 교체하는 방향을 제안했다. task-110에서는 story card 설정만 유지하고, 공개 데모의 `Shop` 무대 푸드트럭 리스킨 여부는 SSOT 승인 뒤 task-114/115에서 확정한다.
- **장슐랭 정의**: 가상 음식 평가 가이드와 0~3별 엔딩으로 설계했지만 공개 명칭·문구는 출시 전 별도 검토가 필요하다. 내부 ID는 `grand_award`로 중립화한다.
- **1.0 음식 taxonomy**: `국·탕·찌개/해산물/고기`를 상위 카테고리 후보로 두되, 기존 demo `GenreKind`를 이번 task에서 변경하지 않는다. 새 taxonomy는 저장 구현 전 별도 migration 설계가 필요하다.
- **대회 지역의 의미**: GDD v0.9는 뉴욕 한인타운→뉴욕 도시→미국 전국 규모로 가정했다. 오너가 한국 지역 음식(전라도·경상도 등)을 뜻한 경우 competition theme과 문화 고증 구조를 다시 설계해야 한다.
- **주인공 세부 정보**: 이름·성별·나이·한국어 능력·할머니 출신 지역은 문화 고증과 캐릭터 디자인 단계에서 오너가 확정해야 한다. 고정 주인공을 기본으로 한다.
- **통화**: 뉴욕 배경에는 USD가 자연스럽지만 현재 원화 정수 UI와 테스트를 이번 task에서 유지한다. USD 전환은 internal cents, 현지화, 전체 밸런스를 함께 바꾸는 별도 task다.
- **아트 SSOT 충돌**: 현재 `art-direction.md`의 에도풍 CC0 조합은 현대 뉴욕 최종 방향과 충돌한다. task-109 자산은 prototype으로 한정하고, 최종 아트 도입 전 Codex가 현대 NYC/Koreatown 스타일 보드와 승인된 asset map을 새로 설계해야 한다.
- **PPTX 산출물**: 프레젠테이션 제작 스킬에 맞춘 18장 콘텐츠·디자인 사양은 J 절에 완성했지만, Codex 운영 규약의 “설계 요청 중 design.md 한 파일만 수정” 제약 때문에 이 turn에서 `.pptx` 파일을 생성하지 않는다. 별도 문서 산출 권한이 있는 presentation task에서 이 사양을 그대로 제작해야 하며, 게임 구현의 blocker는 아니다.
- **경쟁작 정보 변동**: 출시 예정작과 Steam 지표는 바뀔 수 있다. 마케팅/스토어 페이지 작성 시 공식 페이지를 다시 검증한다.
