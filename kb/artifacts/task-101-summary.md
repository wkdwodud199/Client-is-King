# 산출물 요약 — task-101

> Status: done
> Inputs: kb/tasks/task-101/implementation-notes.md
> Outputs: 이 요약 문서
> Next step: task-102 설계 요청 (Unity 프로젝트 생성 검증 + 기본 세팅, M0)

## 작업 요약

- **Task ID**: task-101
- **제목**: 리포 규약 확장 + Unity gitignore + 스코프 가드
- **완료일**: 2026-07-08

## 산출물 목록

| 산출물 | 경로 | 설명 |
|--------|------|------|
| 공통 규약 오버레이 | `AGENT.md` | 게임 프로젝트 경로(`game/`)·task 번호·범위 판단 기준·버전 관리 규약 표 |
| 설계자 규약 보강 | `AGENTS.md` | 게임 task 설계 시 브리프+스코프 가드 기준 범위 판단 |
| 구현자 규약 보강 | `CLAUDE.md` | `.meta` 보존, 스코프 가드 이탈 시 기록·중단, 버전 관리 대상 명시 |
| fast-path 보강 | `QUICKREF.md` | 게임 프로젝트 핵심 경로/SSOT/스코프 가드 3줄 요약 |
| Unity ignore 규칙 | `.gitignore` | `game/` 앵커드 캐시·빌드·IDE·crash 규칙 + 루트 방어선 (`.meta`/`ProjectSettings/`/`Packages/` 는 추적 유지) |
| 스코프 가드 문서 | `kb/concepts/demo-scope.md` | 포함 범위·하드캡·주차장·범위 변경 절차 (신설) |
| README 오버레이 | `README.md`, `README.en.md` | 게임 프로젝트 위치 설명 + 핵심 경로 (한/영) |
| task 기록 | `kb/tasks/task-101/{manifest,implementation-notes}.md` | manifest 실값 + provenance, 구현 노트 |

## 주요 결정

- 기존 CWC 규약은 삭제/축소 없이 **추가(additive) 오버레이**로만 확장 (설계 제약 준수).
- 범위 변경(주차장 승격/하드캡 완화)은 "오너 승인 → project-brief.md 갱신 → demo-scope.md 갱신 → 새 task" 절차로 고정.
- 구현 중 발견된 러너 `.ps1` 크래시 2건(Get-Command 다중 매치, PS 5.1 EAP=Stop + native stderr)은
  task 범위 외 **선행 인프라 수리**로 처리하고 구현 노트 "발생한 이슈"에 상세 기록.
- `.sh` smoke 의 로컬 16 FAIL 은 변경 전 HEAD 와 동일한 사전 환경 제약임을 워크트리 대조로 입증 (CI 위임).

## 관련 문서

- 설계: `kb/tasks/task-101/design.md`
- 구현 노트: `kb/tasks/task-101/implementation-notes.md`
