# 산출물 요약 — task-102

> Status: done
> Inputs: kb/tasks/task-102/implementation-notes.md
> Outputs: 이 요약 문서
> Next step: task-103 설계 요청 (코어 데이터 모델 — ScriptableObject 6종 + 초기 데이터, M1)

## 작업 요약

- **Task ID**: task-102
- **제목**: Unity 프로젝트 생성 검증 + 기본 세팅
- **완료일**: 2026-07-09

## 산출물 목록

| 산출물 | 경로 | 설명 |
|--------|------|------|
| Unity 프로젝트 | `game/` | Unity 6 (6000.3.8f1) — CLI `-createProject` 로 생성, 버전 검증 완료 |
| 패키지 구성 | `game/Packages/manifest.json`, `packages-lock.json` | URP 17.3.0 · test-framework 1.6.0 · ugui 2.0.0(TMP 병합) · 2d.sprite 1.0.0, 노이즈 제거 |
| 2D URP 파이프라인 | `game/Assets/Settings/Rendering/` | Renderer2DData + URP Pipeline Asset, Graphics 기본 + 전 Quality 연결 |
| 기준선 세팅 스크립트 | `game/Assets/Scripts/Editor/ProjectSetup.cs` | 멱등, 배치 `-executeMethod` 진입점 (URP·식별자·폴더) |
| 어셈블리 경계 | `game/Assets/**/*.asmdef` ×3 | Runtime / Editor / Tests.EditMode (URP 2D 어셈블리 참조 포함) |
| 스모크 테스트 | `game/Assets/Tests/EditMode/ProjectBaselineTests.cs` | 6/6 통과 — 버전·URP·픽셀 표준(PPU 32, 640x360)·이름·네임스페이스·TMP |
| 배치 게이트 | (명령 규약 — impl-notes "구현 결정 4") | 컴파일/세팅/테스트 3종 배치 명령, 매 task 반복 실행 |
| task 기록 | `kb/tasks/task-102/`, `kb/artifacts/task-102-summary.md` | manifest(provenance 자동 기록)·구현 노트·요약 |

## 주요 결정

- **CLI 빈 생성 + 코드 저작 세팅**: Hub 수동 생성 대신 `-createProject` + 멱등 `ProjectSetup.Apply` — 재현 가능,
  샘플 노이즈 0, 브리프의 "에디터 스크립트 코드 저작" 철학을 기준선부터 적용.
- **2D Pixel Perfect 는 URP 내장 사용** (별도 패키지는 built-in RP 용), **TMP 는 ugui 2.0 병합**으로 충족 —
  둘 다 EditMode 테스트로 실검증.
- **URP 17 의 2D 어셈블리 분리** (`Unity.RenderPipelines.Universal.2D.Runtime`) 를 컴파일 게이트가 검출 —
  게이트가 설계 의도대로 작동함을 실증.
- Unity 배치 실행에는 **활성 라이선스 필요** — 만료 시 rc 198. Hub 로그인으로 복구 (환경 전제로 기록).

## 관련 문서

- 설계: `kb/tasks/task-102/design.md`
- 구현 노트: `kb/tasks/task-102/implementation-notes.md`
