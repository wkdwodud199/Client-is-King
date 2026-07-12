# 산출물 요약 — task-117

> Status: done
> Inputs: kb/tasks/task-117/implementation-notes.md
> Outputs: 이 요약 문서 — 인게임 크레딧 UI(MainMenu 크레딧 버튼 + 640×360 모달 패널) + 라이선스 동봉 패키지 완료 요약과 인계
> Next step: **640×360 시각 승인(메뉴 전경/크레딧 패널 캡처) + THIRD-PARTY-NOTICES.md 신규 문구 오너 확인 + Codex 코드리뷰 → 이후 task-116 U3(NYC 씬 전환) 진행(순서 고정). 출시는 F절 릴리스 게이트(task-116+117 합류 재빌드) 준수**

## 작업 요약

- **Task ID**: task-117
- **제목**: 인게임 크레딧 UI (릴리스 필수 표기)
- **완료일**: 2026-07-13 (source-ready — 시각 승인·Codex 코드리뷰 대기)

## 산출물 목록

| 산출물 | 경로 | 설명 |
|--------|------|------|
| 크레딧 문구 단일 원천 | `game/Assets/Scripts/Runtime/UI/CreditsCopy.cs` | `AiArtNoticeKo`(task-116 C절 verbatim)·`LeftColumn`(© 2026 P0t4t0·엔진·폰트 OFL·CC0 4팩)·`RightColumn`(AI 고지 + 라이선스 요약) |
| 크레딧 컨트롤러 | `game/Assets/Scripts/Runtime/UI/CreditsController.cs` | 열기/닫기 토글 + focus + 모달 입력 격리(배경 3버튼 interactable 저장/잠금/정확 복원, Cancel=Esc/패드 B 닫기) — 도메인 무접촉 |
| 씬 저작 | `game/Assets/Scripts/Editor/SceneBuilder.cs`, `game/Assets/Scenes/MainMenu.unity` | `CreditsButton` (250,150) + 3-체인 네비게이션 + `BuildCreditsPanel`(640×360 모달, CloseButton Navigation None, 초기 비활성, 최상단 sibling) |
| 라이선스 동봉 | `game/Assets/StreamingAssets/Licenses/{Galmuri-LICENSE.txt,LICENSE.txt,THIRD-PARTY-NOTICES.md}` (+`.meta`) | OFL 조건 2 전문 + 루트 MIT 전문 + Unity 상표 비제휴 고지 — 전부 원본 바이트 동일(테스트 고정) |
| Unity 상표 고지 | `THIRD-PARTY-NOTICES.md` (리포 루트) | 오픈 이슈 2 (a)안 — 신규 EN 법적 문구는 오너 확인 게이트 대상 |
| 테스트(EditMode) | `game/Assets/Tests/EditMode/CreditsPanelSceneTests.cs`(신규 15) + `SceneBuilderTests.cs`(추가 1) | G절 1~8 — 구조/배선/열닫 복원/동기화 ①~⑤/fit ≤300×240/글리프(비파괴 clone)/동봉 바이트 동일/멱등. **510/510 green** |
| 테스트(PlayMode) | `game/Assets/Tests/PlayMode/CreditsPlayModeTests.cs`(신규 1) | P0-1 — 열기 잠금 + focus 닫기 버튼, 닫기 저장값 정확 복원(Continue disabled fixture) + focus 복귀. **10/10 green (9→10)** |
| task 기록 | `kb/tasks/task-117/implementation-notes.md`, 이 요약 | 오너 확정 반영·설계 해석 4건·검증 결과표·미결 게이트 |

## 주요 결정

- **오너 확정(2026-07-13) 반영**: 제작 © = `P0t4t0`(루트 LICENSE 일치), Unity 표기 = 사실 서술
  + THIRD-PARTY-NOTICES 고지(이슈 2 (a)안 — 파일은 오너 지시로 루트 `.md` + 사본 동봉).
- **법적 문구 = 계약 verbatim**: AI 공개문은 task-116 C절 원문 그대로(동기화 테스트가 고정),
  라이선스 요약은 design.md A절 확정본 그대로("CC0 아님" 포함, CC0 중복 행 없음 — A절 결정 기록 준수).
- **모달 입력 격리(Codex P0-1)**: dim raycast(pointer) + interactable 저장/잠금/정확 복원(키보드·
  패드) + CloseButton Navigation None(포커스 탈출) 3중 차단. `MainMenuController.cs` 무수정(diff 0).
- **동기화 테스트 ⑤ 해석**: 라이선스 요약의 verbatim 원천은 task-117 design.md A절(존댓말 확정본),
  task-116 C절에는 정책 앵커 존재 검증 — 구현 노트 "설계 해석·이탈 기록" 1 참조.

## 검증

- 구현 전 기준선 실측: EditMode **494/494** · PlayMode **9/9**.
- 구현 후: EditMode **510/510** (기존 494 무회귀 + 신규 16) · PlayMode **10/10** (기존 9 + 신규 1) —
  둘 다 `-quit` 없이 실행, `error CS` 0.
- Shop.unity 실변경 0(fileID 재직렬화 노이즈 revert — 정규화 비교 diff 0), Build Settings 2씬 유지,
  `.meta` 쌍 완비·삭제 0, 도메인/저장/밸런스/엔딩 diff 없음, 사용자 소유 파일 무접촉.

## 미결(오너/Codex 게이트)

- 640×360 시각 승인(원본 + 2× 캡처 — 메뉴 전경/크레딧 패널) — Claude self-approve 금지.
- THIRD-PARTY-NOTICES.md 신규 EN 문구 오너 확인.
- Codex 코드리뷰(전체 diff).
- 릴리스 게이트: README 라이선스 반영(task-116 U2) 선행, task-116+117 합류 후 재빌드, 빌드 내
  `StreamingAssets/Licenses/` 실존 확인. task-115 빌드 출시 금지 유지.

## 관련 문서

- 설계: `kb/tasks/task-117/design.md`, Codex 교차검토: `kb/tasks/task-117/design-review-codex.md`(Re-Review 2 approved)
- 구현 노트: `kb/tasks/task-117/implementation-notes.md`
