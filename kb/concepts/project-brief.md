# Project Brief — Client is King (손님이 왕이다)

> 이 문서는 게임 프로젝트의 **단일 진실 원천(SSOT)** 이다.
> 모든 task 설계(design.md)는 이 브리프의 컨셉·스코프·라우팅을 전제로 작성한다.

## 게임 컨셉

한류 열풍을 배경으로 한 **한식 레스토랑 경영 시뮬레이션**.
플레이어는 미국 동네 구멍가게에서 한식당을 시작해, 각종 난관을 헤쳐나가며 가게를 성장시켜 최종적으로 **미슐랭 스타**를 획득한다.

- **그래픽**: 데이브 더 다이버급 2D 픽셀아트
- **최종 플랫폼**: Steam (Windows PC 우선)
- **엔진**: Unity 6 — 에디터 **6000.3.8f1** (2D URP, C#)
- **개발자 배경**: 게임개발 첫 경험 → task는 학습 곡선 순서로 배치

## 핵심 게임 시스템 (데모 범위)

1. **코어 루프**: 재료 구매(등급 C/B — 재정 부족으로 저급부터) → 조리 → 손님 서빙 → 수익 → 재정 관리 → 다음 날
2. **하루 사이클**: `Market(장보기) → Service(영업) → Settlement(정산) → Night(SNS+저장)` 상태 머신 (씬 전환 아님)
3. **한식 장르 선택**: 국밥 / 분식 / 면류 / 제네럴리스트 — 각각 원가·조리시간·객단가·고객층 친화도 배수 (장단점 트레이드오프)
4. **SNS 마케팅**: 채널 3종 × 연령·성별 타겟팅 → 익일 손님 수·인구 분포 변화, 수확체감
5. **이벤트/장애물**: 재료값 폭등, 위생 점검, 임대료 인상, 단체 손님 (4종 하드캡)

## 데모 스코프 가드

**제외 (주차장 — `demo-scope.md`에 기록만)**: Steam 연동, 직원 고용, 인테리어, 다점포, 미슐랭 트랙, 조리 미니게임.
**하드캡**: 가게 1단계(구멍가게), 장르 3종+제네럴리스트, 이벤트 4종, 아트는 CC0/OFL 플레이스홀더만.

## 아키텍처 규약

- **위치**: Unity 프로젝트 = `game/` (리포 루트 기준). task 문서 = `kb/`.
- **데이터**: 정적 정의 = ScriptableObject 6종 (`IngredientDef/RecipeDef/GenreDef/CustomerArchetypeDef/SNSCampaignDef/GameEventDef`), 런타임 상태 = 순수 C# `GameState` → `JsonUtility` (Dictionary 금지, List 기반)
- **씬**: `MainMenu.unity` + `Shop.unity` 2개만. 씬/프리팹/UI는 `SceneBuilder` 에디터 스크립트로 코드 저작 → 배치 모드 실행
- **매니저**: 싱글턴 MonoBehaviour 8종 (Game/Economy/Inventory/Service/SNS/Event/Save/UI) + C# `event` 기반 `GameEvents` 정적 클래스. **DI/ECS/이벤트버스 라이브러리 금지**
- **픽셀 표준**: PPU 32, Point 필터, 무압축, Pixel Perfect Camera 640×360, 한글 폰트 Galmuri(OFL) TMP Dynamic
- **검증**: 배치 모드 컴파일 게이트(매 task) + EditMode 테스트(경제·SNS 수학) + 사용자 Play 모드 플레이테스트

## Task 로드맵 + 실행 계획 라우팅

design은 Codex `gpt-5.5/xhigh` 고정. implement 라우팅(각 design.md 실행 계획에 기록):

| task | 제목 | 마일스톤 | implement_model | effort |
|---|---|---|---|---|
| task-101 | 리포 규약 확장 + Unity gitignore + 스코프 가드 | M0 | claude-fable-5 | medium |
| task-102 | Unity 프로젝트 생성 검증 + 기본 세팅 | M0 | claude-fable-5 | high |
| task-103 | 코어 데이터 모델 (SO 6종 + 초기 데이터) | M1 | claude-fable-5 | high |
| task-104 | 씬 2종 + 하루 상태 머신 + SceneBuilder | M1 | claude-fable-5 | xhigh |
| task-105 | 경제·인벤토리 + 장보기 UI + EditMode 테스트 | M1 | claude-fable-5 | high |
| task-106 | 조리·서빙 코어 루프 | M1 | claude-fable-5 | xhigh |
| task-107 | 정산 + 하루 마감 + 파산 (기계적 루프 완성) | M1 | claude-fable-5 | high |
| task-108 | 표현 미니 패스 — 가게 씬 연출 + 손님 스프라이트 + 서빙/정산 연출 | **M1.5 완료** | claude-fable-5 | xhigh |
| task-109 | 아트 도입 패스 — 오픈소스 CC0 에셋(손님 걷기 애니·음식·무대) 도입 (M1.5 플레이테스트 피드백, `art-direction.md`) | M1.5 | claude-fable-5 | high |
| task-110 | 장르 선택 시스템 | M2 | claude-fable-5 | high |
| task-111 | SNS 마케팅 시스템 | M2 | claude-fable-5 | xhigh |
| task-112 | 이벤트/장애물 시스템 | M2 | claude-fable-5 | high |
| task-113 | 저장/불러오기 (JSON) | M3 | claude-fable-5 | high |
| task-114 | 아트 마감 패스 (폰트는 M1.5 핫픽스로 선행됨 — 미세조정만) | M3 | claude-fable-5 | medium |
| task-115 | 밸런싱 + 엔딩 + Windows 빌드 (데모 완료) | **M3 완료** | claude-fable-5 | high |

**로드맵 v3 (2026-07-10)**: M1.5 플레이테스트 피드백(자체 생성 도트 품질 상한)으로 **아트 도입을 task-109에 선삽입**, 기존 task-109~114를 +1 시프트(오너 승인). v2 재번호 관례대로 이 SSOT와 `demo-scope.md`만 갱신하고, 완료 task(101~108)의 역사적 task 참조는 그대로 둔다.

**게이트**: **M1.5(task-108) 완료 시 사용자 플레이테스트** — 재미 검증 실패 시 M2 진입 전 루프 수정.
(당초 M1(task-107) 게이트였으나, 표현 계층 없이는 재미 평가가 불가능함이 확인되어 오너 승인으로 이동 — 2026-07-09)
**번호 규칙**: 게임 task는 task-101부터 (task-001~003 legacy 예외 회피).

## 화면 연출 최소 기준 (M1.5 표현 미니 패스)

재미 평가가 가능한 "보이는 게임"의 최소선. 하드캡(CC0/OFL 플레이스홀더, 씬 2개, 미니게임 금지)은 불변.

- **Shop 씬 구성**: 가게 내부 배경(단순 타일/단색+카운터) + 손님 영역(좌) + 카운터(중) + UI 패널(우/하)
- **손님 표현**: archetype 별 스프라이트(CC0), 입장 → 카운터 이동 → 주문 → (서빙 시 만족 퇴장 / 포기 시 불만 퇴장)
- **서빙 연출**: 음식 아이콘 표시 + "+N원" 팝업/카운트업
- **정산·밤 연출**: 정산 수치 카운트업, Night 는 화면 톤 어둡게
- **기술 제약**: 트윈은 코루틴/lerp 코드 저작만(외부 트윈 라이브러리 금지 — 매니저 규약과 동일), 픽셀 표준(PPU 32) 준수, 사운드는 선택(CC0 한정)

## Status / Inputs / Outputs / Next step

- **Status**: done (브리프 확정 2026-07-08 · 로드맵 v2 — 표현 미니 패스 삽입/게이트 이동, 오너 승인 2026-07-09)
- **Inputs**: 사용자 결정 (게임 컨셉·엔진·마일스톤 2026-07-08 · M1 플레이테스트 피드백 "표현 없이는 재미 평가 불가" 2026-07-09)
- **Outputs**: 이 문서 — 전체 task 설계의 전제
- **Next step**: task-108 설계 (`runtime/codex-design.ps1 task-108`) — 표현 미니 패스
