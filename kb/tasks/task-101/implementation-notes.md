# 구현 노트 — task-101

> Status: done
> Inputs: kb/tasks/task-101/design.md
> Outputs: 리포 규약 4종 게임 오버레이, Unity .gitignore, kb/concepts/demo-scope.md, README 2종 보강
> Next step: task-102 설계 요청 (`runtime/codex-design.ps1 task-102 --auto`) — Unity 프로젝트 생성 검증 + 기본 세팅

## 설계 대비 변경 사항

| 항목 | 설계 내용 | 실제 구현 | 변경 사유 |
|------|-----------|-----------|-----------|
| .gitignore 범위 | `game/` 하위 캐시/빌드 ignore | 설계 목록 + 루트 앵커드 방어선(`/[Ll]ibrary/` 등 7종) 추가 | 설계 제약 "Unity 가 루트에 생성될 경우도 고려" 반영 — 루트 앵커드라 `kb/` 하위와 충돌 없음 |
| manifest provenance | 러너가 `generated_by` 자동 기록 | 러너 포맷 그대로 수동 추가 | 아래 이슈 2 (래퍼 crash 후 고아 codex 가 설계 완성 → 러너의 기록 단계 미도달) |
| README 구조 트리 | 핵심 경로 보강 | 오버레이 섹션 신설로 갈음, 구조 트리는 유지 | 설계의 "짧은 위치 설명과 핵심 경로만" 지침에 맞춰 최소 변경 |

## 구현 결정 기록

1. **운영 규약 오버레이는 전부 추가(additive) 방식** — 기존 CWC 규약 문장은 삭제/축소하지 않고
   AGENT.md(공통 표), AGENTS.md(설계 기준), CLAUDE.md(.meta 보존·스코프 가드), QUICKREF.md(fast-path 3줄)에
   게임 프로젝트 절만 신설했다 (설계 제약 준수).
2. **demo-scope.md 는 브리프 종속 문서로 명시** — project-brief.md(SSOT)와 어긋나면 브리프 우선,
   범위 변경 절차는 "오너 승인 → 브리프 갱신 → 스코프 문서 갱신 → 새 task" 순서로 고정.
3. **.gitignore 패턴은 `game/` 앵커드가 주력** — `game/[Bb]uild*/` 글롭으로 Build/Builds/BuildOutput 커버.
   `.meta`/`ProjectSettings/`/`Packages/` 는 어떤 패턴에도 걸리지 않음을 음성 테스트로 확인.

## 발생한 이슈

이슈 1~3은 **task-101 설계 레인을 실행하는 과정에서 발견된 워크스페이스 러너(.ps1) 버그**로,
design.md 범위(러너 동작 변경 제외) 밖의 **선행 인프라 수리**다. task 산출물이 아니라 별도 변경으로 기록한다.

1. **`Get-Command` 다중 매치 크래시** — 이 머신은 PATH 에 `git.exe` 가 2개(`cmd\`, `mingw64\bin\`) 있어
   `Get-Command git` 이 배열을 반환 → `& $gitCmd.Source` 가 CommandNotFoundException 으로 즉사.
   수정: `.Source` 를 쓰는 모든 프로브에 `| Select-Object -First 1`
   (invoke-codex.ps1, invoke-claude.ps1, review-design.ps1, codex-review.ps1).
2. **PS 5.1 `EAP=Stop` + native stderr 리다이렉트 즉사** — 러너 진입점이 `$ErrorActionPreference=Stop` 인
   상태에서 `2>&1`/`2>$null` 로 리다이렉트한 native(codex/claude/git/python) 가 stderr 한 줄만 내도
   NativeCommandError 로 승격되어 러너가 죽는다 (`2>$null` 도 동일함을 실측). codex exec 의 정보성 stderr
   ("Reading additional input from stdin...")로 최초 발현. CI 는 stderr 없는 스텁만 써서 미검출.
   수정: 라이브러리 함수 5곳(Invoke-Validator/Invoke-RenderPrompt/Invoke-CodexIfEnabled/Invoke-CodexReview/
   Invoke-ClaudeIfEnabled)은 함수 로컬 `EAP=Continue`(동적 스코프 — 호출자 불변), 인라인 4곳
   (review-design.ps1 ×2, codex-review.ps1 ×2)은 save/restore 래핑. 판정은 전부 종료코드 기반이라 의미 불변.
   부작용: 이슈 2 의 래퍼 crash 시점에 codex 자식 프로세스가 고아로 살아남아 설계를 끝까지 완성했고
   (validator rc0 통과 확인), 러너의 provenance 기록 단계만 미도달 → manifest 에 수동 보충.
3. **`.sh` smoke 는 이 Windows/Git Bash 환경에서 사전 실패 상태** — 변경 전 HEAD 워크트리와 작업 트리에서
   `tests/run-smoke.sh` 를 각각 실행, FAIL 16건이 **라인 단위로 동일**함을 확인 (exit 2 = 환경 오류 계열).
   본 task 의 변경(.ps1/문서/.gitignore)과 무관한 기존 환경 제약이며 CI(Linux/macOS)가 .sh 레인을 담당한다.
4. **로컬 Pester 는 3.4.0 뿐** — tests/pester/* 는 Pester 5 전용이라 로컬 실행 생략 (테스트 파일 주석대로
   CI smoke-powershell job 위임). 대신 .ps1 수정 경로는 실 codex-design/claude-implement 실행으로 검증.

## 테스트 결과

| 테스트 기준 (design.md 참조) | 결과 | 비고 |
|------------------------------|------|------|
| `python runtime/validator/cli.py kb/tasks/task-101/design.md` → 0 | pass | 게이트 3회 통과 (codex 직후 / EAP 검증 / claude-implement) |
| `.gitignore` 양성: Library/Temp/Obj/Logs/UserSettings/Builds 6경로 ignore | pass | `git check-ignore -v` 전부 규칙 매치 (rc=0) |
| `.gitignore` 음성: Foo.cs / Foo.cs.meta / ProjectSettings / Packages 비-ignore | pass | `git check-ignore` rc=1 (매치 없음) |
| demo-scope.md 에 포함범위·하드캡·제외/주차장·범위 변경 절차 존재 | pass | 4개 절 + design.md 참조 기준 절 |
| `python -m pytest tests/validator tests/context_budget tests/status_board tests/runtime` | pass | 141 passed (pytest 9.1.1 신규 설치) |
| `./tests/run-smoke.sh` / `Invoke-Pester tests/pester` | 환경 제약 | 이슈 3·4 — 회귀 0 확인(베이스라인 대조), CI 위임 |
| `--check-done task-101` + `generate-status.py --check` | pass | 본 노트/summary 작성 후 실행 (아래 산출물 참조) |

## 산출물

- `AGENT.md` — "게임 프로젝트 오버레이 — Client is King" 절 신설 (경로/번호/범위/버전 관리 표)
- `AGENTS.md` — "게임 task 설계 기준 (task-101+)" 절 신설
- `CLAUDE.md` — "게임 프로젝트 규칙 (Client is King)" 절 신설 (.meta 보존, 스코프 가드)
- `QUICKREF.md` — "게임 프로젝트 fast-path" 절 신설
- `.gitignore` — Unity 6 `game/` 앵커드 규칙 + 루트 방어선 + IDE/crash 파일
- `kb/concepts/demo-scope.md` — 신설 (스코프 가드 정본)
- `README.md` / `README.en.md` — 게임 프로젝트 오버레이 절 신설
- `kb/tasks/task-101/manifest.md` — 실값 채움 + generated_by provenance
- (인프라, task 범위 외) `runtime/lib/{common,invoke-codex,invoke-claude}.ps1`, `runtime/{review-design,codex-review}.ps1` — 이슈 1·2 수정
