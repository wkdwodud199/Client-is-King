# 산출물 요약 — task-106

> Status: done
> Inputs: kb/tasks/task-106/implementation-notes.md
> Outputs: 이 요약 문서
> Next step: task-107 설계 요청 (정산 + 하루 마감 + 파산 — M1 완료 게이트, 첫 플레이어블)

## 작업 요약

- **Task ID**: task-106
- **제목**: 조리·서빙 코어 루프
- **완료일**: 2026-07-09

## 산출물 목록

| 산출물 | 경로 | 설명 |
|--------|------|------|
| 서비스 도메인 | `game/.../Service/` | ServiceOrderState(id 참조 저장) · ServiceResult · 순수 ServiceOps · thin ServiceManager |
| 상태 확장 | `game/.../DayCycle/GameState.cs` | 당일 주문 목록/인덱스/매출/서빙·이탈 통계 8필드 |
| 영업 UI | `game/.../UI/ServicePanelController.cs` | 현재 주문·고객·레시피·조리시간·예상매출·등급 토글·필요재료(한국어)·서빙/포기·통계 |
| SceneBuilder 갱신 | `game/.../Editor/SceneBuilder.cs` | Panel_Service 실 UI 교체 + Recipe 6/Customer 4/Ingredient 18 주입 + ServiceManager 탑재 |
| 테스트 +15 | `game/Assets/Tests/EditMode/` | ServiceOps 12 · 씬 3 — **총 57/57 통과** |
| task 기록 | `kb/tasks/task-106/`, `kb/artifacts/task-106-summary.md` | manifest(provenance)·구현 노트·요약 |

## 주요 결정

- **결정론적 주문 생성** — 씨앗 없는 day/인덱스 산식 (같은 입력+day = 동일, day 별 상이). SNS 가중치 변조는 task-109.
- **서빙 트랜잭션** — 전체 preflight 후에만 소비; 실패 경로(부족/불일치/소진)는 자금·인벤·통계 완전 불변.
- **등급 혼합 금지** — 선택 등급만 소비 (C 부족 시 B 자동 대체 없음, 테스트로 고정).
- **판매가 = BasePrice×파티** — 장르/품질/이벤트 배수는 task-108+ 로 명시적 유보.

## 검증 및 이슈

- SceneBuilder/컴파일 게이트 exit 0 · EditMode **57/57** (1차 54/57 — GetCurrentOrder 인덱스 규약 버그를
  게이트가 검출 → 수정, 도메인 버그의 사전 차단 첫 사례) · 캐시 누출 0건

## 관련 문서

- 설계: `kb/tasks/task-106/design.md`
- 구현 노트: `kb/tasks/task-106/implementation-notes.md`
