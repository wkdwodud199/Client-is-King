# 구현 노트 — task-102

> Status: done
> Inputs: kb/tasks/task-102/design.md
> Outputs: game/ Unity 6(6000.3.8f1) 2D URP 프로젝트 + 기준선 세팅 + 배치 검증 게이트
> Next step: task-103 설계 요청 (`runtime/codex-design.ps1 task-103 --auto`) — 코어 데이터 모델 (SO 6종 + 초기 데이터)

## 설계 대비 변경 사항

| 항목 | 설계 내용 | 실제 구현 | 변경 사유 |
|------|-----------|-----------|-----------|
| 2D Pixel Perfect | `com.unity.2d.pixel-perfect` 패키지 포함 | URP 17.3.0 **내장** `PixelPerfectCamera` 사용, 별도 패키지 미설치 | 해당 패키지는 built-in RP 용 — URP 사용 시 URP 내장 컴포넌트가 공식 권고. EditMode 테스트로 PPU 32 / 640x360 수용 실검증 |
| TextMeshPro | TextMeshPro 패키지 포함 | `com.unity.ugui` 2.0.0 로 충족 (별도 패키지 없음) | Unity 6 에서 TMP 는 ugui 2.x 에 병합됨. 테스트가 `TMPro.TMP_Settings` 타입 로드로 확인 |
| URP 어셈블리 참조 | (설계 미규정) | asmdef 에 `Unity.RenderPipelines.Universal.2D.Runtime` 추가 참조 | URP 17 에서 2D 타입(Renderer2DData, PixelPerfectCamera)이 별도 어셈블리로 분리됨 — 첫 컴파일 게이트가 CS0246 으로 검출, 수정 |
| 빈 폴더 추적 | 1차 폴더 구조 생성 | `Assets/{Data,Scenes,Art/Placeholders}` 는 `.gitkeep` 로 유지 | git 은 빈 디렉터리를 추적하지 못함 — 후속 task 가 채우기 전까지 구조 보존 |

## 구현 결정 기록

1. **프로젝트 생성은 CLI `-createProject`** (Hub 수동 생성 대신) — 빈 프로젝트 생성 후 manifest 큐레이션이
   재현 가능하고, 사용자 수동 단계가 없다. 생성 결과: `m_EditorVersion: 6000.3.8f1` 확인.
2. **패키지 최소 큐레이션**: URP 17.3.0(에디터 번들) · test-framework 1.6.0 · ugui 2.0.0 · 2d.sprite 1.0.0
   (픽셀아트 스프라이트 편집용, 에디터 내장) + 기본 modules 유지. 템플릿 노이즈(`com.unity.multiplayer.center`) 제거.
   버전은 에디터 `BuiltInPackages` 실측값으로 고정.
3. **기준선 세팅은 멱등 에디터 스크립트** `ClientIsKing.EditorTools.ProjectSetup.Apply` 로 코드 저작
   (브리프의 "SceneBuilder 에디터 스크립트로 코드 저작" 철학 선행 적용) — Renderer2DData + URP 파이프라인 에셋 생성,
   Graphics 기본 + 전 Quality 레벨 연결, productName `Client is King`, rootNamespace `ClientIsKing`.
4. **배치 게이트 명령 확립** (리포 루트 기준, 매 task 반복 실행):
   - 컴파일: `Unity.exe -batchmode -quit -nographics -projectPath game -logFile <log>` → exit 0 + 로그에 `error CS` 없음
   - 세팅 재적용: 위 + `-executeMethod ClientIsKing.EditorTools.ProjectSetup.Apply`
   - EditMode 테스트: `Unity.exe -batchmode -nographics -projectPath game -runTests -testPlatform EditMode -testResults <xml> -logFile <log>` → exit 0 (`-runTests` 에는 `-quit` 금지)
5. **asmdef 3종 분리**: Runtime(빈 기준선) / Editor(Editor 전용, URP 참조) / Tests.EditMode(TestRunner + URP 참조,
   `UNITY_INCLUDE_TESTS` 제약) — 이후 task 의 코드가 들어갈 어셈블리 경계를 선확정.

## 발생한 이슈

1. **Unity 라이선스 만료 (rc 198)** — 첫 `-createProject` 가 "No valid Unity Editor license found" 로 실패
   (엔타이틀먼트 0건, access token 만료). 부분 생성물(Logs/Temp/UserSettings만) 정리 → Unity Hub 실행만으로는
   복구 안 됨 → **사용자 Hub 로그인 후 해결**. 배치 실행에는 활성 라이선스가 전제임을 manifest notes 에 기록.
2. **URP 17 의 2D 어셈블리 분리** — `Renderer2DData`/`PixelPerfectCamera` 가
   `Unity.RenderPipelines.Universal.2D.Runtime` 으로 분리되어 CS0246 3건 발생. 컴파일 게이트가 설계 의도대로
   실패를 검출했고, asmdef 참조 추가로 해결 (게이트 실증 사례).
3. task-101 에서 수리한 러너로 codex-design 이 처음으로 **전 구간 완주** (설계 생성 → validator rc0 →
   manifest provenance 자동 기록).

## 테스트 결과

| 테스트 기준 (design.md 참조) | 결과 | 비고 |
|------------------------------|------|------|
| validator rc0 (design.md) | pass | 러너 게이트 + 사후 재검증 |
| `ProjectVersion.txt` = 6000.3.8f1 | pass | `m_EditorVersion: 6000.3.8f1` |
| manifest.json 패키지 구성 + packages-lock.json 생성 | pass | URP 17.3.0 · test 1.6.0 · ugui 2.0.0 · 2d.sprite 1.0.0 (TMP=ugui 병합, pixel-perfect=URP 내장) |
| 배치 컴파일 게이트 exit 0 + `error CS` 없음 | pass | 1차 실행에서 CS0246 검출→수정→2차 통과 (게이트 동작 실증) |
| EditMode 테스트 exit 0 | pass | **6/6 통과** (버전·URP 연결·픽셀 표준·이름·네임스페이스·TMP) |
| SampleScene 등 템플릿 샘플 부재 | pass | CLI 빈 생성이라 샘플 자체가 없음 |
| `git status` 에 캐시/빌드 미노출 | pass | -uall 기준 Library/Temp/Obj/Logs/UserSettings 0건 |
| check-ignore 양성 5경로 / 음성 3경로 | pass | 양성 rc0 전부 매치, 음성 rc1 |
| `--check-done` + `generate-status --check` | pass | 기록 완료 후 실행 (요약 문서 참조) |

## 산출물

- `game/` — Unity 6(6000.3.8f1) 프로젝트 (Assets/Packages/ProjectSettings 추적, 캐시 제외)
- `game/Packages/manifest.json` + `packages-lock.json` — 큐레이션된 최소 패키지 셋
- `game/Assets/Settings/Rendering/{Renderer2D,URP-Pipeline}.asset` — 2D URP 파이프라인 (Graphics/Quality 연결됨)
- `game/Assets/Scripts/Editor/ProjectSetup.cs` — 멱등 기준선 세팅 (배치 -executeMethod 진입점)
- `game/Assets/Scripts/{Runtime,Editor}/*.asmdef`, `game/Assets/Tests/EditMode/*.asmdef` — 어셈블리 경계 3종
- `game/Assets/Tests/EditMode/ProjectBaselineTests.cs` — 기준선 스모크 6케이스
- `game/Assets/{Data,Scenes,Art/Placeholders}/.gitkeep` — 후속 task 폴더 구조
- `kb/tasks/task-102/{manifest,implementation-notes}.md`, `kb/artifacts/task-102-summary.md` — 기록
