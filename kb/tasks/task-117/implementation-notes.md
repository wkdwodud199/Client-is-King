# 구현 노트 — task-117 (인게임 크레딧 UI — 릴리스 필수 표기)

> Status: done
>
> U1~U5 전부 완료(source-ready). 자동 검증 게이트 전부 통과 — EditMode **510/510**(기준선 494 +
> 신규 16), PlayMode **10/10**(기준선 9 + 신규 1), `error CS` 0, Shop.unity 실변경 0(재직렬화
> 노이즈 revert). 미결 게이트: **640×360 시각 승인(오픈 이슈 5)** · **Codex 코드리뷰** ·
> **THIRD-PARTY-NOTICES.md 신규 법적 문구 오너 확인**(아래 참조) — Claude self-approve 금지.

## 오너 확정 사항 반영 (2026-07-13)

설계 오픈 이슈 1·2가 구현 시작 전에 오너 확정되어 그대로 반영했다:

1. **제작 저작자명 = `P0t4t0`** (루트 `LICENSE` 권리자와 일치) — 크레딧 제작 줄:
   `Client is King — © 2026 P0t4t0`. 설계 초안의 "Client is King Project" 표기는 폐기.
2. **Unity 엔진 표기 = 오픈 이슈 2 (a)안** — 크레딧 엔진 줄은 `Unity 6 (6000.3.8f1)` 사실 서술만
   (`© Unity Technologies` 권리 표기 없음). Unity 상표 귀속·비제휴 고지는 리포 루트
   **`THIRD-PARTY-NOTICES.md`** 신설로 이행하고 `StreamingAssets/Licenses/`에 바이트 동일 사본을
   동봉했다. **주의**: 이 고지문(EN 2문장 — trademark 귀속 + non-affiliation)은 이 task 에서 새로
   작성된 법적 문구이므로 설계 게이트 규약대로 **오너 최종 확인 대상**으로 플래그한다.

## 구현 요약 (U1~U4)

- **U1 — `CreditsCopy.cs`** (`game/Assets/Scripts/Runtime/UI/`, `ClientIsKing.UI`, static): 상수 3종.
  - `AiArtNoticeKo` = task-116 design.md C절 공개문 [KO] **verbatim**(개행 없는 단일 문자열,
    한 글자도 변경 없음 — 동기화 테스트가 원문 부분 문자열 일치로 증명).
  - `LeftColumn` = 제작(© P0t4t0)/엔진/폰트(Galmuri11 OFL 1.1 + © 2019–2025 Lee Minseo)/
    플레이스홀더 아트 CC0 4팩. 수동 `\n`, 각 줄 ≤300px.
  - `RightColumn` = AI 보조 아트(AiArtNoticeKo) + 라이선스 요약(코드 MIT / 고유 아트 MIT 제외,
    CC0 아님 / A절 확정 정책 문장) — 문장 개행 미삽입(TMP 자동 wrap).
  - 라이선스 동봉(F절): `game/Assets/StreamingAssets/Licenses/`에 `Galmuri-LICENSE.txt`(OFL 조건 2),
    `LICENSE.txt`(루트 MIT), `THIRD-PARTY-NOTICES.md` — 전부 원본과 바이트 동일(테스트 고정), `.meta` 쌍 완비.
- **U2 — `CreditsController.cs`** (Canvas 탑재, EndingOverlayController 미러): 열기/닫기 토글 +
  focus + **모달 입력 격리(Codex P0-1)**. 열 때 배경 버튼 3종(start/continue/credits)의 현재
  `interactable` 을 저장 후 전부 false 잠금 → 닫을 때 **저장값 정확 복원**(무조건 true 금지 —
  Continue disabled fixture 로 테스트 고정). `OpenNow()`/`CloseNow()` internal 공용 경로,
  Cancel(Esc/게임패드 B) 닫기는 `Update()` 의 `Input.GetButtonDown("Cancel")` → `CloseNow()`
  (legacy InputManager — 신규 InputSystem 도입 없음). OnEnable/OnDisable 리스너 쌍, 미배선 no-op
  가드, focus 가드(`Application.isPlaying && EventSystem.current`), `EditorInit` 5-참조 주입.
  **`MainMenuController.cs` 무수정** (diff 0줄 확인).
- **U3 — `SceneBuilder.cs`**: `BuildMainMenu` 에 `CreditsButton` (250,150) 120×32 "크레딧" 추가,
  기존 `LinkVerticalNavigation(startButton, continueButton)` 을
  `LinkVerticalNavigation(creditsButton, startButton, continueButton)` 3-체인으로 대체(기존
  Start↔Continue 연결 보존 — 기존 테스트 기대값 무변경), 마지막에 `BuildCreditsPanel` 호출(최상단
  sibling). `BuildCreditsPanel` 은 D절 표 그대로 — Panel_Credits 640×360 Ink Navy a0.92 dim
  raycastTarget true / CreditsTitleText (0,158) 24pt Brass Amber / 좌·우 컬럼 (∓160,10) 300×240
  10pt Steam Cream TopLeft(문구는 CreditsCopy 상수를 구움) / CreditsCloseButton (0,-152) 200×40
  **Navigation None**(포커스 탈출 차단을 빌드 타임에 굽기) / 초기 비활성 / CreditsController
  EditorInit. `MainMenu.unity` 재생성.
- **U4 — 테스트**: `CreditsPanelSceneTests.cs` 신규 15건(G절 1~8 전수 — 아래 검증 절),
  `CreditsPlayModeTests.cs` 신규 1건(P0-1 focus/잠금/정확 복원 왕복), `SceneBuilderTests.cs` 에
  크레딧 오브젝트 존재 검증 1건 추가(기존 기대값 변경 0).

## 설계 해석·이탈 기록 (Codex 코드리뷰 참조용)

1. **동기화 테스트 ⑤(라이선스 요약)의 verbatim 원천 = task-117 design.md A절**. 설계 B절/G절은
   "라이선스 요약 문장이 task-116 design.md 에 verbatim 존재"로 적었지만, task-116 C절 원문은
   평서체("…허가는 부여하지 않는다")이고 A절 확정본은 존댓말("…허가를 부여하지 않습니다")이라
   문자 그대로는 성립 불가다. Codex 승인 리뷰(1차 항목 2)도 이 관계를 "축약으로 정합"으로
   판정했으므로, 테스트는 ① 확정 문장 verbatim ⊂ **task-117 design.md**(A절 블록의 문서 표시용
   개행을 공백 정규화 후 비교) + ② 정책 앵커("재사용·재배포·2차 저작물 작성 허가") ⊂ task-116
   design.md(드리프트 감지) 2단으로 구현했다. AI 공개문(①)은 설계대로 task-116 원문 verbatim.
2. **THIRD-PARTY-NOTICES 파일 형식**: 설계 F절은 `THIRD-PARTY-NOTICES.txt`(Licenses 폴더 직접
   추가)였으나, 오너 확정 지시가 "리포 루트에 `THIRD-PARTY-NOTICES.md` 생성 후 사본 동봉"이라
   루트 `.md` + `Licenses/THIRD-PARTY-NOTICES.md` 바이트 동일 사본으로 구현했다(루트 LICENSE →
   `Licenses/LICENSE.txt` 와 동일 패턴, 바이트 동일 테스트 동일 적용).
3. **오케스트레이터 지시문과 A절 문구의 차이 — design.md 준수**: 지시문의 라이선스 요약
   rendering 은 ", CC0 아님." 이 빠지고 "플레이스홀더 아트: 각 원본 팩의 CC0 1.0" 행이 있었으나,
   Codex 승인 계약(design.md A절)은 "CC0 아님" 을 포함하고 CC0 중복 행은 명시적으로 제거했다
   (A절 결정 기록 — 좌측 컬럼 헤더가 이미 전달). 법적 문구 임의 수정 금지 제약에 따라 **design.md
   A절을 그대로** 구현했다. CloseButton 좌표도 지시문 (0,-150)이 아닌 설계 표의 **(0,-152)** 채택.
4. Shop.unity 는 Apply 재생성 시 fileID 재직렬화 노이즈(+5060/−5060)만 발생 — fileID 정규화 후
   정렬 비교로 **실변경 0** 을 확인하고 working tree 에서 revert 했다(커밋에 포함되지 않음).
   `Galmuri11 SDF.asset`/`ProjectSettings.asset` 도 내용 diff 0(EOL 노이즈)으로 revert.

## 검증 결과 (2026-07-13, Unity 6000.3.8f1 배치 — `-runTests` 에 `-quit` 미사용)

| 게이트 | 결과 |
|--------|------|
| 구현 전 기준선 | EditMode **494/494** · PlayMode **9/9** (실측 기록) |
| 구현 후 EditMode | **510/510** green — 기존 494 무회귀 + `CreditsPanelSceneTests` 15 + `SceneBuilderTests` 추가 1 |
| 구현 후 PlayMode | **10/10** green — 기존 9 무수정 통과 + `CreditsPlayModeTests` 1 (기준선 9→10) |
| 컴파일 | `error CS` 0 (Apply·EditMode·PlayMode 로그 전부) |
| 문구 동기화 ①~⑤ | green — AiArtNoticeKo ⊂ task-116 verbatim / 씬 텍스트 == 상수 / 폰트 ⊂ OFL / CC0 4팩 ⊂ PROVENANCE / 라이선스 요약 verbatim(위 해석 1) |
| 양 컬럼 fit | green — 좌 줄폭 ≤300px, 좌·우 wrap 높이 ≤240px + `isTextOverflowing` false (10pt 유지, 9pt 축소 불요) |
| 글리프 커버리지 | green — `©`·`–`·`—`·`·` 포함 전 문자 Galmuri11 커버(ttf 임시 Dynamic clone, 원본 SDF 에셋 비파괴·무변경), 대체 규칙 발동 없음 |
| 라이선스 동봉 | green — OFL·MIT·THIRD-PARTY-NOTICES 3종 바이트 동일 |
| Apply 멱등 | green — 연속 2회 오브젝트 수 동일·CreditsController 1개·persistent listener 0 |
| Shop.unity | 실변경 0 (fileID 정규화 비교 diff 0 — 노이즈 revert) |
| MainMenuController.cs | diff 0줄 (무수정) |
| 도메인/저장/밸런스/엔딩 | diff 없음 — 변경은 표시 계층(SceneBuilder·Runtime/UI·MainMenu.unity·테스트)에 한정 |
| Build Settings | 2씬(MainMenu+Shop) 유지 (기존 테스트 green) |
| 사용자 소유 파일 | `UPDATING.md`·`kb/concepts/art-references/**`·`kb/concepts/development-priority.md` 무접촉 (기존 사용자 변경분 그대로, staging 안 함) |
| validator | `python -B runtime/validator/cli.py kb/tasks/task-117/design.md` exit 0 |

## 미결 (done 전제·릴리스 게이트 — Claude self-approve 금지)

- **640×360 시각 승인(오픈 이슈 5)**: 원본 + 2× 캡처(메뉴 전경/크레딧 패널) 제출 → 오너/Codex
  판정. 반려 시 D절 표 개정(좌표·폰트 크기)으로 처리, 문구 무변경.
- **THIRD-PARTY-NOTICES.md 문구 오너 확인**: 신규 EN 법적 문구(표준 Unity 상표 고지) 승인.
- **Codex 코드리뷰**: 전체 diff.
- **릴리스 게이트(오픈 이슈 4 — 출시 차단)**: task-115 빌드 출시 금지 유지, README/README.en
  라이선스 범위 반영(task-116 U2)이 done 선행조건, task-116(U2·U3)+117 합류 후 재빌드 + 빌드
  산출물 `StreamingAssets/Licenses/` 실존 확인. task-116 U3 는 본 task 완료 후 진행(순서 고정).
- 수동 smoke(오너): 크레딧 열기(마우스/키보드) → Esc·게임패드 B 닫기 → 시작/이어하기 흐름 불변,
  패널 열린 동안 방향키로 배경 도달 불가 확인. Cancel 실 키 입력은 자동 테스트 범위 밖
  (동일 경로 `CloseNow()` 는 EditMode 로 고정).
