# 산출물 요약 — task-116

> Status: in-progress
> Inputs: kb/tasks/task-116/{design.md, implementation-notes.md, sprite-production-spec.md}, kb/concepts/art-originals/PROVENANCE.md(md5 핀), 오너/Codex 전달 NYC 스프라이트 패키지(full-01 — Codex 시각 재검수에서 반려됨, full-02 대기)
> Outputs: 이 요약 — NYC 런타임 아트 오버홀 U0~U4 구현·기계 검증 요약과 인계 (U2 자산 수입·U3 씬 전환·U4 CustomerSprite inactive 수정)
> Next step: **오너 시각 게이트 REJECTED (2026-07-14). 실제 오너/Codex 승인 전 done 금지 — Status in-progress 유지. 오너 확정 순서: (1) task-118 UI·뷰포트 설계 확정(Codex) → (2) 기존 자산으로 640×360 합성 검증 → (3) HUD·무대·장르카드·팝업 좌표+안전영역 동결 → (4) 그 규격대로 full-02 재생산 → (5) Claude 통합 후 640×360 캡처 4종(원본+nearest 2×) 시각 승인. 즉 full-02 보다 task-118 이 먼저. Claude 는 PNG·CanvasScaler·UI 임의 수정 금지.**

## 작업 요약

- **Task ID**: task-116
- **제목**: 현대 NYC 코리아타운 아트 오버홀 — 승인 콘셉트 런타임 적용
- **상태**: in-progress — **U0~U3 + U4 CustomerSprite hotfix 완료**(자산 수입·씬 전환·CustomerSprite inactive). Codex 코드리뷰 **P1/P2 반영·최종 재리뷰 pending**(verdict 아직 changes-requested). **오너 시각 게이트 REJECTED (2026-07-14)** — full-02 아트 + 화면 구성 task-118 대기. **done 아님.**
- **역할**: 아트 생산·시각 승인 = 오너/Codex. Claude = 기계 검증·임포트·씬 배선·테스트만(오너 2026-07-12/13 지시).

## 산출물 목록

| 산출물 | 경로 | 설명 |
|--------|------|------|
| SSOT 개정 | `kb/concepts/{project-brief,demo-scope,art-direction}.md` | 로드맵 v4·아트 하드캡(오너 승인 AI 아트 조건부 허용)·art-direction v2 (U0, 커밋 `9035ad0`) |
| 임포트 계약 | `game/Assets/Scripts/Editor/NycArtContract.cs` | 32파일 Asset Map·경로 상수·임포트 표준 적용(생성 빌더 아님) (U1) |
| NYC 런타임 아트 | `game/Assets/Art/NYC/**` (32 PNG + .meta) | 손님 20·음식 6·장르 4·무대 2. 오너/Codex 생산, Claude 바이트 동일 수입 (U2, 커밋 `3511d58`) |
| provenance | `game/Assets/Art/NYC/NYC-ART-PROVENANCE.md` | 파일별 생성 계보·후처리·라이선스 carve-out(MIT 제외·CC0 아님)·KO/EN 공개문 (U2) |
| 회귀 테스트 | `game/Assets/Tests/EditMode/NycArtTests.cs` | H1~9: 존재+meta·규격·알파·IHDR6·색상한·임포트표준·provenance·콘셉트md5·격리 (U2) |
| 씬 전환 | `game/Assets/Scripts/Editor/SceneBuilder.cs`, `game/Assets/Scenes/Shop.unity` | 스프라이트 소스 PlaceholderArtBuilder→NycArtContract, backdrop/counter Tiled→Simple, 장르 아이콘 전용화 (U3, 커밋 `22db186` — 롤백 지점) |
| 공개·라이선스 문서 | `README.md`·`README.en.md`·`game/Assets/StreamingAssets/AI-ART-NOTICE.txt` | AI 아트 고지 + MIT 제외 carve-out (정책 롤아웃, 커밋 `99b6616`/`6564842` — Codex Notice Re-Review 2 approved) |
| task 기록 | `kb/tasks/task-116/{design.md, implementation-notes.md, sprite-production-spec.md}`, 이 요약 | 설계·U0~U4 구현 결정·납품 규격·산출 요약 |

## 주요 결정

- **A안(타깃 해상도 재생산) 이행**: 콘셉트 6종은 레퍼런스, 입력 스프라이트는 오너/Codex 생산, Claude 는
  임포트·검증·배선·테스트만. 콘셉트 자동 축소·크롭·재디자인 없음(오너 제약 원천 차단).
- **입력 게이트 = 독립 기계 검증 + SHA-256**: 매니페스트를 신뢰하지 않고 32 PNG 를 raw 바이트에서 재검증
  (규격·IHDR6·이진 알파·투명 모서리·색 상한·baseline y30) + SHA-256 32/32 대조로 오너 preflight 교차확인.
- **rect 불변 전환(E절)**: 규격을 기존 rect 의 정수 약수로 설계 → 손님 32×32(×2)·음식 32×32(×2)·장르
  32×32(×1)·backdrop 640×160·counter 320×32 전부 씬 rect 무변경. backdrop/counter 만 Tiled→Simple.
- **장르 아이콘 D절 매핑**: 국그릇=gukbap·꼬치=bunsik·면기=noodles·도시락=generalist, 프레임 없는 심볼.
- **U2/U3 커밋 분리(롤백)**: U3 단독 revert 시 CC0 플레이스홀더 화면 즉시 복귀. PlaceholderArtBuilder 유지(G절).
- **MainMenu.unity HEAD 유지**: NYC 참조 0(무대 없음) — 재생성이 비결정적 fileID/순서 churn만 만들어 무의미
  diff 배제. 런타임 SceneBuilder 는 여전히 양 씬 재생성(의미 불변). Codex 코드리뷰에서 확인.
- **H9 격리 메커니즘**: 설계가 "불명확 시 Codex 검수"로 남긴 항목 — in-engine 측(경로 비중첩·집합 불변·
  스냅샷 바이트 대조)을 테스트로, 커밋 단위 바이트 불변은 `git status` 게이트로 분할 구현. 리뷰에서 확인.

## 검증

- EditMode: **519/519** (`-quit` 없이, 기준선 510 + NycArtTests 9). NycArtTests 9/9 green.
- PlayMode: **10/10** (`-quit` 없이, 무회귀).
- 격리: Placeholders/**·OpenSource/**·PlaceholderArtBuilder·PlaceholderArtTests·도메인/저장/밸런스/엔딩 무수정.
  콘셉트 원본 6종 md5 불변(PROVENANCE.md 핀 대조). Shop.unity 만 NYC GUID 참조 + Tiled→Simple 반영.
- `.meta` 임포트 표준 구움 확인(textureType Sprite·PPU32·Point·무압축·mipmap off·alphaIsTransparency).
- `git status --short game` 청정 — 변경이 NYC 자산·NycArtTests·SceneBuilder·Shop.unity 로 한정, `.meta`
  추가는 신규 NYC 자산 한정, 삭제 0. 노이즈(Galmuri SDF·MainMenu 재직렬화·ProjectSettings EOL) revert.
- 사용자 소유(UPDATING.md·kb/concepts/art-references/**·development-priority.md) 미staging.

## 오너 시각 게이트 REJECTED (2026-07-14) — done 금지, in-progress 유지

자산만 교체하고 실제 640×360 화면 검수를 하지 않은 채 제출한 것이 반려 사유. 지시 분류:

**task-116 범위 — 이번에 반영:**
- **CustomerSprite 저작 즉시 inactive** (SceneBuilder) + 초기 `activeSelf==false` 테스트. 주문 시 활성화/
  퇴장 시 비활성 런타임 동작은 ShopPresentationController 그대로 보존.
- summary Status `done`→`in-progress`, status 보드 되돌림. **실제 오너/Codex 승인 전 완료 표기 금지.**
- Galmuri11 SDF.asset 동적 재직렬화 변경 미커밋.

**대기 (Claude 착수 금지):**
- **full-01 아트 = Codex 시각 재검수 반려** → **full-02 입력 대기**. Claude 는 PNG 를 직접 수정하지 않는다.
- **640×360 고정 캡처 4종**(장르 모달 / 모달 닫은 무대 전경 / 손님 입장+음식·매출 팝업 / Night 페이드),
  각 원본 + nearest-neighbor 2× 확대본 — Free Aspect 는 승인 자료 아님. full-02+task-118 이후 제출.

**진행 순서 (오너 확정 2026-07-14 — task-118 이 full-02 보다 먼저):**
1. **task-118 UI·뷰포트 설계 확정** (Codex 설계).
2. 기존 자산으로 640×360 합성 화면 검증.
3. HUD·무대·장르 카드·팝업 좌표 + 안전영역 **동결**.
4. 그 동결 규격에 맞춰 **full-02 재생산**.
5. Claude 통합 후 **4종 시각 승인**.

**별도 task-118 (UI/뷰포트 설계 — 이 task 아님, Claude 임의 개편 금지):**
- 핵심 방향: **640×360 고정 레터박스** · 상단 **HUD 32px** · 중앙 **무대 192px** · 하단 **컨트롤 덱 136px**
  (32+192+136=360) · **중앙 장르 모달 폐기 → 장르 4카드 상시 표시**.
- 장르 4아이콘 상시 표시 · 선택은 빨간 외곽선/체크로 분리 · 거대 빈 크림 패널 제거 · 아이콘+장르명+
  한 줄 장단점 4카드 · NYC 배경 충분 노출.
- 시각 정본: `kb/concepts/art-references/2026-07-12-batch-01/{01-runtime-composition,02-market-screen}.png`.

**Codex 코드리뷰(U2/U3 diff):** P0 없음, P1(H8 스왑 방어)/P2 반영(`code-review-codex.md`). **단 공식 verdict
는 아직 `changes-requested` — 최종 재리뷰 pending.** verdict 가 approved 로 갱신되기 전엔 "코드리뷰 완료"로
표기하지 않는다. 데모 재빌드 여부 = 오너 결정(오픈 이슈 8).

## 관련 문서

- 설계: `kb/tasks/task-116/design.md`(ready), Codex 교차검토: `design-review-codex.md`, 공개문 리뷰: `notice-review-codex.md`
- 구현 노트: `kb/tasks/task-116/implementation-notes.md`(U0~U4 상세·게이트 결과), 납품 규격: `sprite-production-spec.md`
