# 구현 노트 — task-116 (NYC 코리아타운 아트 오버홀)

> Status: in-progress — U0(SSOT 개정)·U1(NycArtContract 계약 코드) 완료·검증. **U2~U4 는 오너/Codex 의
> 승인된 32파일 스프라이트 패키지 전달 게이트 대기.** (아트 생산·시각 승인 = 오너/Codex, Claude 는
> 기계 검증·임포트·씬 연결·테스트만 — 오너 2026-07-12 지시.)
> Inputs: kb/tasks/task-116/design.md (ready — 오너 A안 확정 + Codex 교차검토), design-review-codex.md,
> kb/concepts/art-originals/PROVENANCE.md (md5 핀), sprite-production-spec.md (배치1 납품 규격)
> Outputs: SSOT 개정(로드맵 v4·하드캡·art-direction v2) + NycArtContract.cs(32파일 Asset Map·임포트 계약)
> Next step: Codex 가 오너 결정(noodles 정정·확정 시각 사양·생산순서·PNG IHDR·AI 아트 정책)을 design.md 에
> 기록 → Claude 가 NycArtContract/규격서를 noodles 로 통일 → 오너/Codex 파일럿9 승인 → 최종 32파일 전달 →
> U2(수입+NycArtTests) → U3(SceneBuilder 전환) → U4(게이트+시각 승인). U2/U3 커밋 분리.

## U0 — SSOT 범위 변경 (오너 승인 2026-07-12)

- `kb/concepts/project-brief.md`: 로드맵 task-116 행(M4/claude-fable-5/high) + "로드맵 v4" 주석 + 아트
  하드캡 개정("CC0/OFL 플레이스홀더 + 오너 승인 AI 생성 아트(task-116 계약 내, provenance 기록 필수)").
- `kb/concepts/demo-scope.md`: 아트 하드캡 행 개정 + 주차장에 NYC 오버홀 승격 기록. 범위 변경 절차 원문 무수정.
- `kb/concepts/art-direction.md`: v2 재분류 — CC0 combo1 을 "프로토타입(데모 출시본)"으로, NYC 를 최종
  디렉션으로 기록(task-116 참조). v1 본문 보존.

## U1 — NycArtContract.cs (계약 코드, 자산 없이 컴파일)

- `game/Assets/Scripts/Editor/NycArtContract.cs` 신설(namespace `ClientIsKing.EditorTools`): 경로 상수
  (`Assets/Art/NYC/...`), `AllSpritePaths()` 32파일 Asset Map(손님 4×(idle+walk4)=20·음식 6·장르 아이콘 4·
  무대 2), 배선 헬퍼(CustomerIdlePaths/WalkFramePaths/FoodIconPath/GenreIconPath/Backdrop/Counter),
  `ApplyImportSettings`(Sprite·PPU32·Point·무압축·mipmap off·alphaIsTransparency, 멱등, 자산 부재 시 예외).
  스프라이트 생성/슬라이싱 없음(A안). `kb/tasks/task-116/manifest.md` 신설. `sprite-production-spec.md`
  (배치1 납품 규격) 신설.

## 커밋 구조 (오너 기록 요청)

- **U0 와 U1 은 단일 커밋 `9035ad0` 로 합쳐졌다.** (설계 실행계획은 G0/G1 로 분리했으나, 둘 다 아트 없이
  가능한 준비 단계라 하나로 커밋. 이후 U2 자산 수입 커밋과 U3 SceneBuilder 전환 커밋은 오너 지시대로 분리한다.)

## 검증 (독립 재실행 — Claude, 2026-07-12)

| 게이트 | 결과 |
|--------|------|
| Unity 배치 컴파일 | exit 0 · `error CS` 0 (NycArtContract 컴파일 확인) |
| EditMode (`-quit` 없이) | **494/494 pass** — 기준선 정확 유지(U1 신규 테스트 0건) |
| PlayMode (`-quit` 없이) | **9/9 pass** — 오너 요청으로 재실행, 기준선 유지 |
| 격리 | `Placeholders/**`·`OpenSource/**`·`PlaceholderArtBuilder.cs`·`PlaceholderArtTests.cs`·도메인/저장/밸런스/엔딩 무수정. `game/Assets/Art/NYC/**` 자산 미생성(U2 몫). 씬/ProjectSettings/Galmuri 재직렬화 노이즈는 커밋 전 revert |

- 범위 밖 무터치 확인: `UPDATING.md`·`kb/concepts/development-priority.md` (오너 지시 — task-116 범위 밖 사용자 변경).

## 오너 결정 접수 (2026-07-12) — Codex design.md 기록 후 코드 반영

1. 아트 생산·시각 승인 = 오너/Codex. Claude 는 생성·리터칭·리사이즈·크롭·재디자인 금지, 검증·임포트·배선·테스트만.
2. **장르 ID 정정: `noodle`→`noodles`, `genre_noodle.png`→`genre_noodles.png`** (실제 도메인 id 는 noodles).
   Codex 가 design.md 에 먼저 기록한 뒤 NycArtContract/규격서/테스트를 통일한다(임의 매핑 금지).
3. PNG 검증: Texture2D 뿐 아니라 **원본 PNG IHDR color type 6(실 RGBA)** 를 raw 바이트로 확인(U2 NycArtTests).
4. 확정 시각 사양: 장르 매핑(국그릇 gukbap·꼬치 bunsik·면기 noodles·도시락 generalist)·장르 아이콘 프레임 없는
   투명 심볼·무대 단판(backdrop 640×160·counter 320×32)·family_parent 페어 우선(1× 식별 불가 시만 재승인 후
   부모 단독)·좌향 런타임 flip 허용.
5. 생산 순서: 파일럿 9파일(student idle+walk4·pork_gukbap·genre_gukbap·backdrop·counter) 선승인(1×/4×/걷기/
   반전) → 나머지 23. 파일럿은 U2 부분 납품 아님 — **32파일 전부 합격 후에만 U2.**
6. AI 아트 공개·라이선스 정책 확정(오픈 이슈 5): 코드 MIT / NYC·art-originals 아트는 MIT 제외·별도 라이선스·
   CC0 금지·재사용 미허가. 표시 위치: README(+en)·NYC-ART-PROVENANCE.md·StreamingAssets/AI-ART-NOTICE.txt·
   Steam Content Survey(Pre-Generated AI)·인게임 Credits(별도 release task, 공개 출시 전 필수). KO/EN 공개문은
   design.md 에 확정 기록. 승인자 Project Owner / 2026-07-12.
