# 작업 현황 (Status Board)

> 아래 자동 생성 블록(HTML 주석 마커 사이)의 **활성/완료 표는 `runtime/generate-status.py` 가 생성**한다.
> 직접 편집하지 말 것 (CI 가 drift 를 검사한다). 새 워크스페이스에는 task 가 없어 표가 비어 있는 것이 정상이다.

<!-- BEGIN:generated -->
## 활성 작업

| Task ID | 제목 | Status | 비고 |
|---------|------|--------|------|
| (없음) | — | — | — |

## 완료 작업

| Task ID | 제목 | 완료일 | 산출물 |
|---------|------|--------|--------|
| task-101 | 리포 규약 확장 + Unity gitignore + 스코프 가드 | 2026-07-08 | [summary](../artifacts/task-101-summary.md) |
| task-102 | Unity 프로젝트 생성 검증 + 기본 세팅 | 2026-07-09 | [summary](../artifacts/task-102-summary.md) |
| task-103 | 코어 데이터 모델 (SO 6종 + 초기 데이터) | 2026-07-09 | [summary](../artifacts/task-103-summary.md) |
| task-104 | 씬 2종 + 하루 상태 머신 + SceneBuilder | 2026-07-09 | [summary](../artifacts/task-104-summary.md) |
| task-105 | 경제·인벤토리 + 장보기 UI + EditMode 테스트 | 2026-07-09 | [summary](../artifacts/task-105-summary.md) |
| task-106 | 조리·서빙 코어 루프 | 2026-07-09 | [summary](../artifacts/task-106-summary.md) |
| task-107 | 정산 + 하루 마감 + 파산 (**M1 완료 — 첫 플레이어블**) | 2026-07-09 | [summary](../artifacts/task-107-summary.md) |
| task-108 | 표현 미니 패스 — 가게 씬 연출 + 손님 스프라이트 + 서빙/정산 연출 (**M1.5 완료**) | 2026-07-09 | [summary](../artifacts/task-108-summary.md) |
| task-109 | 아트 도입 패스 — CC0 오픈소스 손님 걷기 애니 + 음식/무대 스프라이트 (M1.5 품질 상한) | 2026-07-10 (커밋 완료 `fd6256c`) | [summary](../artifacts/task-109-summary.md) |
| task-110 | 장르 선택 시스템 — 국밥/분식/면류/제네럴리스트 전문 분야, 결정론적 수요 예측, 경제/서비스 배수 적용 | 2026-07-11 (커밋 완료 `9265da5`; Codex 코드리뷰·640×360 시각승인·수동 smoke 대기) | [summary](../artifacts/task-110-summary.md) |
<!-- END:generated -->

## 러너 빠른 참조

기본은 수동 모드, 자동 호출은 `--auto` (세션 내부에서는 재귀 가드가 막으며 `*_AUTO_FORCE=1` 로만 우회).

```
# Bash                                        # PowerShell
./runtime/codex-design.sh <id> "<설명>"       ./runtime/codex-design.ps1 <id> "<설명>"
./runtime/claude-implement.sh <id>            ./runtime/claude-implement.ps1 <id>
./runtime/claude-implement.sh --done <id>     ./runtime/claude-implement.ps1 <id> -Done
```

- 컨텍스트 예산: `python3 runtime/context-budget.py <id> --baseline` (경고 전용).
- 완료 검증: `python3 runtime/validator/cli.py --check-done <id>` (done-gate).
- 보드 재생성: `python3 runtime/generate-status.py` (검사: `--check`).
- (선택) 설계 교차검토: `runtime/review-design.sh <id>` · 구현 리뷰: `runtime/codex-review.sh <id>`.
