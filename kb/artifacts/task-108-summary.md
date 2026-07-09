# 산출물 요약 — task-108

> Status: done
> Inputs: kb/tasks/task-108/implementation-notes.md
> Outputs: 이 요약 문서
> Next step: **M1.5 게이트 — 사용자 플레이테스트** (통과 시 task-109 장르 선택, 실패 시 M2 진입 전 수정)

## 작업 요약

- **Task ID**: task-108
- **제목**: 표현 미니 패스 — 가게 씬 연출 + 손님 스프라이트 + 서빙/정산 연출 (**M1.5 완료**)
- **완료일**: 2026-07-09

## 산출물 목록

| 산출물 | 경로 | 설명 |
|--------|------|------|
| 플레이스홀더 아트 | `game/Assets/Art/Placeholders/` | 코드 생성 픽셀 — 고객 4종(색 구분 인물)·음식 6종(그릇) + provenance 문서, PPU 32/Point/무압축 |
| 아트 빌더 | `game/.../Editor/PlaceholderArtBuilder.cs` | 결정론적 픽셀 생성 + 임포트 설정 고정 (멱등) |
| 표현 이벤트 | `game/.../DayCycle/GameEvents.cs` + `Presentation/*EventArgs.cs` | ServiceOrderPresented·ServiceOutcomeResolved·SettlementPresented (UI 계층만 발행 — Ops 불변) |
| 무대 연출 | `game/.../Presentation/ShopPresentationController.cs` | 손님 입장→카운터→만족/불만 퇴장, 음식 아이콘 팝, +N원 팝업, 정산 pulse, 밤 오버레이 (코루틴 lerp 한정) |
| UI 훅 | `game/.../UI/{ServicePanel,SettlementPanel}Controller.cs` | 처리 전 주문 캡처 발행 + 정산 카운트업(최종값 정확성 보장) |
| 씬 재구성 | `game/.../Editor/SceneBuilder.cs` + `Shop.unity` | 무대 상단 밴드 + 패널 4종 하단 압축(480×200) — 무대·오버레이는 클릭 불차단 |
| 테스트 +18 | `game/Assets/Tests/EditMode/` | 아트 3·무대 6·이벤트 5·정산 표현 4 — **총 90/90 통과** |
| task 기록 | `kb/tasks/task-108/`, `kb/artifacts/task-108-summary.md` | manifest(provenance)·구현 노트·요약 |

## 주요 결정

- **코드 생성 픽셀 플레이스홀더** — 외부 다운로드 0, 결정론적 재현, provenance 자명 (CC0 하드캡 내).
- **표현/도메인 완전 분리** — Ops 무변경, 이벤트는 UI 발행·무대 구독, 게임 규칙은 연출 비의존.
- **Play/Edit 이중 모드** — 트윈은 Play 전용, EditMode 는 즉시 최종값 (테스트 가능성 확보).
- **클릭 안전 보장** — 무대·오버레이 전체 raycastTarget=false (테스트 고정).

## 검증

- SceneBuilder(+아트 생성)/컴파일 게이트 exit 0 (첫 실행) · **EditMode 90/90** · 캐시 누출 0건 · 이슈 0건

## 관련 문서

- 설계: `kb/tasks/task-108/design.md`
- 구현 노트: `kb/tasks/task-108/implementation-notes.md` (수동 Play smoke 체크리스트 포함)
