# 산출물 요약 — task-105

> Status: done
> Inputs: kb/tasks/task-105/implementation-notes.md
> Outputs: 이 요약 문서
> Next step: task-106 설계 요청 (조리·서빙 코어 루프, M1)

## 작업 요약

- **Task ID**: task-105
- **제목**: 경제·인벤토리 + 장보기 UI + EditMode 테스트
- **완료일**: 2026-07-09

## 산출물 목록

| 산출물 | 경로 | 설명 |
|--------|------|------|
| 상태 확장 | `game/.../DayCycle/GameState.cs` | `cash`(시작 30,000원 상수) + `List<IngredientStock>` 인벤토리 (Dictionary 금지 규약) |
| 인벤토리 도메인 | `game/.../Inventory/` | IngredientStock + 순수 InventoryOps(누적/차감/불변 실패) + thin InventoryManager |
| 경제 도메인 | `game/.../Economy/` | PurchaseResult + 순수 EconomyOps(UnitCost×qty 트랜잭션) + thin EconomyManager |
| 장보기 UI | `game/.../UI/MarketPanelController.cs` | 재료 ◀▶ 셀렉터·등급 토글·수량 stepper·예상 비용·보유·구매·메시지 |
| SceneBuilder 갱신 | `game/.../Editor/SceneBuilder.cs` | Panel_Market 실 UI 교체 + 시드 18종 주입 + 매니저 GO 탑재 |
| 테스트 +16 | `game/Assets/Tests/EditMode/` | 경제 6 · 인벤 8 · UI 산출물 2 — **총 42/42 통과** |
| task 기록 | `kb/tasks/task-105/`, `kb/artifacts/task-105-summary.md` | manifest(provenance)·구현 노트·요약 |

## 주요 결정

- **순수 Ops / thin 매니저 분리** — 구매·인벤 규칙은 scene 없이 테스트, 매니저는 위임만.
- **구매 트랜잭션 불변 계약** — 실패 경로(자금 부족/null/0 이하)는 자금·인벤토리 완전 불변.
- **드롭다운 → 순환 셀렉터 대체** (코드 저작 단순화 + 픽셀 UI 적합, 설계 요건 충족 검증 포함).
- **테스트는 시드 asset 역산** — 수치 하드코딩 없이 def.UnitCost 기준.

## 검증

- SceneBuilder/컴파일 게이트 exit 0 (첫 실행) · **EditMode 42/42** · 캐시 누출 0건 · 이슈 0건

## 관련 문서

- 설계: `kb/tasks/task-105/design.md`
- 구현 노트: `kb/tasks/task-105/implementation-notes.md`
