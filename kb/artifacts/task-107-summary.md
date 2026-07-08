# 산출물 요약 — task-107

> Status: done
> Inputs: kb/tasks/task-107/implementation-notes.md
> Outputs: 이 요약 문서
> Next step: **M1 게이트 — 사용자 플레이테스트** (통과 시 task-108 장르 선택, 실패 시 M2 진입 전 루프 수정)

## 작업 요약

- **Task ID**: task-107
- **제목**: 정산 + 하루 마감 + 파산 (**M1 완료 — 첫 플레이어블**)
- **완료일**: 2026-07-09

## 산출물 목록

| 산출물 | 경로 | 설명 |
|--------|------|------|
| 정산 도메인 | `game/.../Settlement/` | SettlementResult · 순수 SettlementOps(운영비 12,000 · 멱등 · 파산 판정) · thin SettlementManager |
| 상태 확장 | `game/.../DayCycle/GameState.cs` | 당일 지출·정산 기록·완료 일수·파산 상태 13필드 |
| 지출 추적 | `game/.../Economy/EconomyOps.cs` | 구매 성공 시 day-key 리셋 누적 (실패 불변 유지) |
| 진행 게이트 | `game/.../Managers/GameManager.cs` | 파산 시 phase 차단(이벤트 미발행) + Settlement 이탈 전 정산 선적용 |
| 정산/밤 UI | `game/.../UI/{SettlementPanel,NightPanel}Controller.cs` | 매출/지출/운영비/순손익/잔액/통계/파산 안내 + 마감 요약 |
| HUD 갱신 | `game/.../UI/PhaseHudController.cs` | phase 별 진행 버튼 라벨(영업 시작/정산/밤으로/다음 날) + 파산 잠금 |
| 테스트 +15 | `game/Assets/Tests/EditMode/` | 정산 8 · 첫 플레이어블 루프 2 · 씬 3 · 경제 2 — **총 72/72 통과** |
| task 기록 | `kb/tasks/task-107/`, `kb/artifacts/task-107-summary.md` | manifest(provenance)·구현 노트·요약 |

## 주요 결정

- **정산의 실제 cash delta 는 운영비뿐** — 매출/재료비는 각 발생 시점에 이미 반영 (이중 반영 금지 테스트 고정).
- **day 당 1회 멱등 정산** — 패널 활성화/버튼/재호출이 겹쳐도 차감 1회.
- **파산 = cash 0 고정 + 진행 차단** — 음수 잔액 불허, 사유·미납액·버틴 일수 기록.
- GameManager DDOL 을 Play 가드로 감싸 **EditMode 에서 전체 루프 통합 테스트** 가능해짐.

## M1 인계 — 플레이테스트 가이드

Unity Hub → `game/` 열기 → `Assets/Scenes/MainMenu.unity` Play:
1. "게임 시작" → Day 1 장보기 (자금 30,000원, 재료 구매)
2. "영업 시작 ▶" → 주문 5건 서빙/포기 (등급 선택, 재료 부족 메시지 확인)
3. "정산 ▶" → 매출·지출·운영비(12,000)·순손익 확인
4. "밤으로 ▶" → 마감 요약 → "다음 날 ▶" → Day 2
5. 파산 확인법: 며칠 적자 운영(구매만 하고 서빙 안 함) → 정산에서 파산 화면 + 진행 잠금

## 관련 문서

- 설계: `kb/tasks/task-107/design.md`
- 구현 노트: `kb/tasks/task-107/implementation-notes.md`
