# 구현 노트 — task-116 (NYC 코리아타운 아트 오버홀)

> Status: in-progress — U0·U1·U2·U3 완료·기계 검증. **남은 것 = U4 오너/Codex 640×360 시각 승인 게이트
> + Codex 코드리뷰(구현 diff).** (아트 생산·시각 승인 = 오너/Codex, Claude 는 기계 검증·임포트·씬 연결·
> 테스트만 — 오너 2026-07-12/07-13 지시. Claude self-approve 금지.)
> Inputs: kb/tasks/task-116/design.md (ready — 오너 A안 확정 + Codex 교차검토), design-review-codex.md,
> kb/concepts/art-originals/PROVENANCE.md (md5 핀), sprite-production-spec.md (배치1 납품 규격),
> 오너/Codex 전달 스테이징 패키지 `art-staging/.../full-01/handoff/NYC`(32 PNG + control preflight/SHA256)
> Outputs: SSOT 개정(로드맵 v4·하드캡·art-direction v2) + NycArtContract.cs(32파일 Asset Map·임포트 계약)
> + NYC 자산 32 수입(+meta+provenance) + NycArtTests(H1~9) + SceneBuilder NYC 전환(롤백 지점)
> Next step: 오너/Codex 가 640×360 시각 승인(H절 4종 캡처 — 콘셉트 정체성·음식 6 식별·손님 4 구분·NYC
> 야간 무드) → 반려 시 입력 패키지 재생산 요청 → 승인 시 Codex 코드리뷰로 마감. U2(3511d58)/U3(22db186) 분리 커밋.

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

## U2 — NYC 자산 32 수입 + NycArtTests (커밋 `3511d58`, 2026-07-13)

**입력 게이트 통과**: 오너가 파일럿 9 → 최종 32파일 전체를 스테이징(`art-staging/.../full-01`)에 전달,
자체 preflight 전수 PASS(control/validation-report·asset-manifest·SHA256SUMS·provenance-input).

- **Claude 독립 재검증(읽기 전용, 매니페스트 불신)**: 32 PNG 를 raw 바이트에서 직접 검증 — 규격(E절)·
  IHDR color type 6·bit depth 8·이진 알파(backdrop 예외)·투명 모서리(backdrop 예외)·불투명 색 상한
  (손님24/음식32/장르12/무대64)·아키타입별 발 baseline(4종 전부 y30 일치) + **SHA-256 32/32 대조**.
  전 항목 PASS로 오너 preflight 교차확인. (참고: `Stage/backdrop.png` 는 색 64/64로 상한에 정확히 붙음 —
  규격 내이나 23장 확장 시 여유 권고를 오너에 전달.) 스크립트는 스크래치패드 전용, 리포 미포함.
- **수입**: `handoff/NYC/**` 32파일만 `game/Assets/Art/NYC/{Customers,FoodIcons,UiIcons,Stage}/` 로 바이트
  동일 복사(복사본 SHA-256 재대조 PASS). `review/**`·`source/raw/**` 는 수입하지 않음(오너 지시).
- **임포트 표준**: Unity 배치가 32 PNG 를 임포트(+.meta 생성) → `NycArtContract.ApplyImportSettingsToAll`
  (NycArtTests OneTimeSetup, 멱등)이 Sprite·Single·PPU32·Point·무압축·mipmap off·alphaIsTransparency 적용.
  `.meta` 검증: `textureType:8`(Sprite)·`spriteMode:1`·`spritePixelsToUnits:32`·`filterMode:0`(Point)·
  `alphaIsTransparency:1`·`textureCompression:0` 구워짐.
- **provenance**: `game/Assets/Art/NYC/NYC-ART-PROVENANCE.md` 신설 — 파일별 규격·생성 도구·생성일·참조
  콘셉트·후처리(타깃 캔버스 재저작 + 신규 이진 알파 마스크; raw 크로마·F3 키잉 산출물 아님; Claude 픽셀
  무수정)·라이선스 carve-out(MIT 제외·CC0 아님)·KO/EN 공개문. 32파일 전수 언급.
- **NycArtTests.cs (EditMode, H1~9)**: 존재+meta 쌍 / 규격 / 알파 규칙 / 원본 PNG IHDR6 / 색 상한 /
  임포트 표준 / provenance 커버리지+필수필드 / 콘셉트 원본 6종 md5 불변(PROVENANCE.md 핀 대조) /
  Placeholders·OpenSource 격리(경로 비중첩 + 28종 집합 불변 + OneTimeSetup 스냅샷 바이트 불변).
  **H9 격리 메커니즘 주(비결정 라우팅)**: 설계 H9 는 "비교 스냅샷 또는 git 기반, 불명확 시 Codex 검수".
  Unity 테스트가 git 을 못 부르므로, in-engine 측(경로 비중첩·집합 불변·NYC 임포트 작업 전후 바이트
  스냅샷 대조)을 구현하고, 커밋 단위 바이트 불변은 U2/U3 의 `git status --short game` 게이트로 보강했다.
  이 분할을 Codex 코드리뷰에서 확인받는다.
- **검증**: EditMode **519/519 green** (기준선 510 + NycArtTests 9). NycArtTests 9/9. 노이즈(Galmuri SDF·
  씬 재직렬화)는 커밋 전 revert. `git status` 청정 — NYC 자산·NycArtTests 만, Placeholders/OpenSource/도메인/
  씬 무변경. 사용자 소유(UPDATING.md·art-references·development-priority.md) 미staging.

## U3 — SceneBuilder 스프라이트 소스 NYC 전환 (커밋 `22db186`, 롤백 지점)

- `SceneBuilder.cs`: 무대 backdrop/counter·손님 idle+walk4·음식 아이콘·장식 소품·장르 아이콘의 스프라이트
  로드 경로를 `PlaceholderArtBuilder` → `NycArtContract` 로 전환. backdrop/counter `Image.Type` Tiled→Simple
  (NYC 단판). 장르 아이콘 D절 매핑 전용화(국밥=gukbap·분식=bunsik·면류=noodles·균형=generalist).
  `PlaceholderArtBuilder.Apply()` 는 G절대로 유지(롤백 + PlaceholderArtTests 기준선).
- **rect·좌표·색·오브젝트 수 불변** — E절이 규격을 rect 정수 약수로 설계했기 때문(CustomerSprite 64×64·
  FoodIcon 64×64·장르 Icon 32×32·backdrop 640×160·counter 320×32 전부 기존 rect 유지).
- **검증**: EditMode **519/519** · PlayMode **10/10** green(무회귀). Shop.unity 재생성이 NYC GUID 참조
  (backdrop/student/genre_gukbap 존재)와 Tiled→Simple(`m_Type` 2→0)을 반영함을 diff 로 확인.
- **MainMenu.unity 처리**: NYC 참조 0(무대 없음)이라 재생성이 비결정적 fileID/순서 churn(내용 1646줄
  차이지만 동일 오브젝트·동일 텍스트)만 만든다 — HEAD 유지(무의미 diff 배제). 런타임 SceneBuilder 는
  여전히 양 씬을 재생성하며, MainMenu 산출은 바이트 불안정하나 의미 불변. 이 판단을 Codex 코드리뷰에서 확인받는다.
- **씬 테스트 기대값**: `ShopPresentationSceneTests`·`SceneBuilderTests` 는 경로·Image.Type 을 단정하지
  않고 sprite!=null·rect·오브젝트 수·멱등만 검사 → 스프라이트 소스 전환 후에도 기대값 갱신 불필요(무회귀 통과).
  (설계는 "경로 기대값 갱신"을 예상했으나 실제 테스트가 경로 무관하게 작성돼 있어 갱신 대상 0.)
- **노이즈 처리**: Galmuri SDF·MainMenu 재직렬화·ProjectSettings(내용 diff 0, EOL 노이즈) 전부 revert.
  U3 커밋 = `Shop.unity` + `SceneBuilder.cs` 2파일만.

## U4 — 검증 기록 + 남은 게이트

- 이 노트 + `kb/artifacts/task-116-summary.md` + `runtime/generate-status.py` 재생성.
- **남은 오너/Codex 게이트(비차단 아님 — done 전제)**:
  - 640×360 시각 승인 4종(무대 전경·서빙 팝·장르 modal·Night 페이드) — 콘셉트 정체성 재현·음식 6 식별·
    손님 4 구분·NYC 야간 무드. **Claude self-approve 금지.** 오너가 Shop 씬(에디터) 또는 재빌드 exe 로 확인.
    반려 시 색·형태 수정 없이 입력 패키지 재생산 요청으로 라우팅.
  - Codex 코드리뷰(U2/U3 전체 diff + H9 격리 메커니즘·MainMenu revert 판단 확인).
  - 데모 재빌드(NYC 반영) 여부 = 오너 결정(오픈 이슈 8).
