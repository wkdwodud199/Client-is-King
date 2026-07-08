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
| task-107 | 정산 + 하루 마감 + 파산 (첫 플레이어블) | **M1 완료** | claude-fable-5 | high |
| task-108 | 장르 선택 시스템 | M2 | claude-fable-5 | high |
| task-109 | SNS 마케팅 시스템 | M2 | claude-fable-5 | xhigh |
| task-110 | 이벤트/장애물 시스템 | M2 | claude-fable-5 | high |
| task-111 | 저장/불러오기 (JSON) | M3 | claude-fable-5 | high |
| task-112 | 아트·폰트 패스 | M3 | claude-fable-5 | medium |
| task-113 | 밸런싱 + 엔딩 + Windows 빌드 (데모 완료) | **M3 완료** | claude-fable-5 | high |

**게이트**: M1(task-107) 완료 시 사용자 플레이테스트 — 재미 검증 실패 시 M2 진입 전 루프 수정.
**번호 규칙**: 게임 task는 task-101부터 (task-001~003 legacy 예외 회피).

## Status / Inputs / Outputs / Next step

- **Status**: done (브리프 확정, 2026-07-08)
- **Inputs**: 사용자 결정 (게임 컨셉·엔진·마일스톤, 2026-07-08 대화)
- **Outputs**: 이 문서 — 전체 task 설계의 전제
- **Next step**: task-101 설계 (`runtime/codex-design.ps1 task-101`)
