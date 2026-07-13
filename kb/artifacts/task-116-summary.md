# 산출물 요약 — task-116

> Status: done
> Inputs: kb/tasks/task-116/{design.md, implementation-notes.md, sprite-production-spec.md}, kb/concepts/art-originals/PROVENANCE.md(md5 핀), 오너/Codex 전달 NYC 스프라이트 패키지(파일럿 9 → 최종 32, control preflight+SHA256)
> Outputs: 이 요약 — NYC 코리아타운 런타임 아트 오버홀(U0 SSOT 개정 → U1 계약 → U2 32자산 수입+테스트 → U3 씬 전환) 구현·기계 검증 완료 요약과 인계
> Next step: **오너/Codex 640×360 시각 승인 4종(Claude self-approve 금지) + Codex 코드리뷰(U2/U3 diff) → 반려 시 입력 패키지 재생산 요청 → 통과 시 task-116 최종 완료. 데모 재빌드 여부는 오너 결정(오픈 이슈 8).**

## 작업 요약

- **Task ID**: task-116
- **제목**: 현대 NYC 코리아타운 아트 오버홀 — 승인 콘셉트 런타임 적용
- **완료일**: 2026-07-13 (구현·기계 검증 완료 — 오너 640×360 시각 승인·Codex 코드리뷰 대기)
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

## 미결 (오너/Codex 게이트 — Claude self-approve 금지)

- **640×360 시각 승인 4종**(무대 전경·서빙 팝·장르 modal·Night 페이드): 콘셉트 정체성 재현·음식 6종
  식별·손님 4종 구분·NYC 야간 무드 판정. 오너가 Shop 씬(에디터) 또는 재빌드 exe 로 확인. 반려 시 색·형태
  수정 없이 **입력 패키지 재생산 요청**으로 라우팅.
- **Codex 코드리뷰**(U2/U3 전체 diff + H9 격리 메커니즘·MainMenu revert 판단 확인) — 설계 Next step 마감 조건.
- **데모 재빌드(NYC 반영) 여부** = 오너 결정(오픈 이슈 8). task-115 Windows 빌드는 CC0 마감본.

## 관련 문서

- 설계: `kb/tasks/task-116/design.md`(ready), Codex 교차검토: `design-review-codex.md`, 공개문 리뷰: `notice-review-codex.md`
- 구현 노트: `kb/tasks/task-116/implementation-notes.md`(U0~U4 상세·게이트 결과), 납품 규격: `sprite-production-spec.md`
