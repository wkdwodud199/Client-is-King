# 구현 노트 — task-114

> Status: done
> Inputs: kb/tasks/task-114/design.md (Codex approved, 무수정), kb/tasks/task-114/design-review-codex.md,
> kb/tasks/task-109/implementation-notes.md (파생 파이프라인·멱등 규약), kb/concepts/art-direction.md,
> 파생 PNG 팔레트 실측 (2026-07-12, PIL)
> Outputs: 한식 리컬러 파이프라인(PaletteMaps exact-match 스왑 + gimbap ×2 정수 확대 + 음식 32×32 캔버스)
> + student 청 데님 + Stage warm 보정 + SceneBuilder 정수배 rect 정합(FoodIcon 64×64·소품 32×32·장르
> Icon 32×32·CashPopup (-40,120)·NightOverlay #16202A) + 테스트 11종 신규/갱신 + provenance 리컬러 절.
> EditMode 439/439 · PlayMode 8/8.
> Next step: 오너/Codex 640×360 시각 승인(D3)·수동 Play smoke → 통과 시 task-115(밸런싱+엔딩+빌드)

## 현재 상태: done (구현 완료·Unity green — D3 시각 승인·수동 smoke 게이트 대기)

## 기준선 실측 (설계 F-1, 코드 변경 전)

| 게이트 | 결과 |
|--------|------|
| EditMode (변경 전) | **428/428 pass, exit 0** — 설계 기재 기준선과 일치 |
| PlayMode (변경 전) | **8/8 pass, exit 0** — 설계 기재 기준선과 일치 |

## 픽셀 실측 확정 (설계 F-2 — 구현 1단계)

### tteokbokki (Meatballs) 역할 배정 — **시드와 반대로 확정, 역할 재배정**

파생 PNG 픽셀 레이아웃 실측(ASCII 맵) 결과:

- **구체(미트볼)** = `#890502`(밝음)·`#730907`(중간)·`#4B0807`(음영) — F/G/J 구형 클러스터
  (`JFFFJ`/`HJJFJJH` 패턴)로 확인. `#400002`는 구체가 소스에 닿는 **접촉 그림자**(최암부 링).
- **바닥 소스** = `#5B0503`(지배 75px, 내부 전면)·`#881F00`(좌측 밝은 영역 36px).
- 설계 시드는 `#5B0503`→떡 밝음/`#881F00`→소스 주로 가정했으나 실측이 반대 →
  **같은 from/to 집합 안에서 역할만 재배정** (from 11색·to 11색 집합 자체는 설계 B2 표와 동일):
  `#5B0503→#D34A3A`(소스 주) · `#881F00→#C74534`(소스 중간) · `#400002→#A93325`(접촉 그림자→소스 음영) ·
  `#890502→#F2E6D8`(구체→떡 밝음) · `#730907→#E7D6C2`(떡 중간) · `#4B0807→#D9C7B2`(떡 음영) ·
  `#630200→#8C2A1E`(시드 동일). 방향 앵커("밝은 고추장 `#D34A3A` + 흰 떡 `#F2E6D8`")와 D2 존재/부재
  계약은 그대로 성립. 표·상수·provenance·테스트 동기 갱신 완료.
- 결과 화면: 밝은 고추장 소스 바닥 위에 흰 떡 구체 — 방향 앵커 그대로.

### cyan 4색은 고명이 아니라 그릇 장식 무늬 (기록)

`#28B3BC`/`#0B696F`/`#174E74`/`#0B325D`는 실측상 tteokbokki·bibim_guksu가 **공유하는 그릇 하단
장식 무늬**였다(음식 위 고명 아님). 설계 D2가 두 파일 모두 `#28B3BC` 부재를 계약하므로 설계 표대로
무광 green 전환을 적용했다(그릇 장식이 청록→녹색 밴드로 변경). 시각 게이트에서 위화감 판정 시
1회 보정 대상 후보로 기록해 둔다.

### 기타 실측 확인

- D2 표의 from 색 전수(음식 6종 41쌍 + student 1쌍 + 타일 6쌍)가 파생 PNG에 실재함을 확인 — 설계
  실측값과 완전 일치, 표 수정 불필요(tteokbokki 역할 재배정 제외).
- 트림 크기: karsiori 5종 30×24(bibim_guksu만 30×31), gimbap(Sushi) 13×13 — **정수 사전 확대는
  규칙(w≤16∧h≤16)대로 gimbap만 ×2 (26×26)**. 전부 32×32 캔버스에 floor 오프셋 중앙 패딩.
- `office_worker.png`에도 `#D14B34`가 소량 존재함을 발견 — 비대상(매핑 없음·바이트 불변)이므로 계약
  무관, D2 부재 계약은 student 5파일에만 적용됨을 확인.

## 구현 요약 (U1~U4)

### U1 — PlaceholderArtBuilder 리컬러 파이프라인

- 공개 상수 `PaletteMaps`(`IReadOnlyDictionary<string, (Color32 from, Color32 to)[]>`, key=파생 경로,
  13항목) — 테스트·provenance가 공유하는 **단일 원천**.
- 순수 헬퍼 3종: `SwapPalette`(불투명 픽셀 exact-match 단일 패스, alpha 보존, 미등재 통과, 매핑 없는
  경로는 그대로 통과), `PrescaleIfSmall`(w≤16∧h≤16만 nearest-neighbor ×2), `PadToCanvas`(32×32 투명
  캔버스, offset=floor((32−w)/2), 초과 시 명시 예외).
- 파이프라인 배선(B1 순서 고정): 슬라이스/트림 → ①확대 → ②스왑 → ③패딩 → WritePng(바이트 비교
  멱등, 기존 규약 그대로). 손님·타일은 ②만 적용.
- 비대상 손님 3종(15파일)은 매핑 키 자체가 없어 순수 슬라이스 결과 그대로 → git diff에 나타나지 않음
  (task-109 산출물과 바이트 동일)으로 확인.

### U2 — SceneBuilder 정수배 rect 정합 (C절 표 전수)

- `FoodIcon` (-40,78)/40×32 → **64×64** (32×32 캔버스 ×2), `CashPopupText` (-40,96) → **(-40,120)**,
  소품 3종 26×26 → **32×32**, 장르 버튼 Icon (-42,0)/20×20 → **(-38,0)/32×32**,
  `NightOverlay` RGB(0,0,10) → **`#16202A`**(InkNavy 상수 재사용, alpha 0·raycastTarget false 불변).
- `CustomerSprite` 64×64·무대 좌표·패널 좌표·`ShopPresentationController`/`PresentationTween` 전부 무변경.

### U3 — 테스트 (기준선 428 → 439, +11)

- `PlaceholderArtTests` +9: 32×32 캔버스 / PaletteMaps from 전수 부재 / D2 앵커 존재(13파일) /
  비대상 3종 매핑 없음+Apply 바이트 불변 / 국그릇 앵커 `#501C07`·`#B3400F` 보존 / red 3종 주색
  상호 배타(자기 파일에만 존재) / 팔레트 상한(음식≤24·손님≤12·타일≤8, 28파일 전수) / Apply 2회
  28종 바이트 멱등 / provenance 리컬러 절(task-114·리컬러·32×32·PaletteMaps).
- `ShopPresentationSceneTests` +1: 정수배 rect 계약(FoodIcon·CashPopup·CustomerSprite 불변·소품 3종
  위치/크기) + 기존 NightOverlay 테스트에 Ink Navy RGB 검증 추가(기대값 갱신).
- `SceneBuilderTests` +1: 장르 버튼 4종 Icon (-38,0)/32×32.
- 픽셀 검사는 전부 `File.ReadAllBytes`+`LoadImage`(임포트 설정 무관) — E절 계약대로.

### U4 — provenance/기록

- `PLACEHOLDER-PROVENANCE.md`: "task-114 리컬러/캔버스 규약" 절 신설(매핑 표 전수 사본·파이프라인
  순서·tteokbokki 실측 확정·cyan=그릇 무늬 기록·비대상 바이트 불변), 이월 문구를 완료로 갱신.
  기존 팩 출처·CC0·다운로드일(2026-07-10)·파일명 표 전부 보존(기존 테스트 무회귀).

## 설계 대비 이탈 사항

| 항목 | 설계 | 실제 | 사유 |
|------|------|------|------|
| tteokbokki 역할 배정 | 시드: `#5B0503`→떡·`#881F00`→소스 주 등 | 실측 확정으로 **역할 재배정**(from/to 집합 동일, 대응만 변경) | 설계 F-2·오픈 이슈가 명시 위임한 실측 확정 절차. 방향 앵커·D2 계약 불변 |
| 그 외 | — | **이탈 없음** — B2/B3/B4 표·C절 rect 표·E절 테스트 계약 그대로 | |

## Unity 검증 결과 (전부 exit 0, `-runTests`에 `-quit` 미사용)

| 게이트 | 명령 | 결과 |
|--------|------|------|
| 기준선 EditMode (변경 전) | `-batchmode -runTests -testPlatform EditMode` | **428/428 pass, exit 0** |
| 기준선 PlayMode (변경 전) | `-batchmode -runTests -testPlatform PlayMode` | **8/8 pass, exit 0** |
| Apply 1회차 | `-batchmode -quit -executeMethod SceneBuilder.Apply` | exit 0 · `error CS` 0 · 성공 로그 |
| Apply 2회차 (멱등) | 동일 | exit 0 · **파생 PNG 28종 전부 바이트(md5) 불변** · 씬은 오브젝트 수/persistent listener 기준 멱등(테스트 검증 — 씬 바이트는 fileID 재직렬화로 매 실행 변동, task-109부터의 기존 특성) |
| EditMode (변경 후) | 동일 | **439/439 pass, exit 0** (기준선 428 + 신규 11, 무회귀) |
| PlayMode (변경 후) | 동일 | **8/8 pass, exit 0** (무회귀) |
| 리컬러 픽셀 검증 (보조) | PIL 스크립트 (from 부재/앵커 존재/캔버스/상한/red 분화 전수) | **0 FAIL** |

- `git status --short game`: 변경 = 파생 PNG 13종 + 코드 2 + 테스트 3 + 씬 2 + provenance — **설계
  영향 파일 표와 정확히 일치**. `.meta` 추가/삭제 0 · `Assets/Art/OpenSource/**` 무변경 ·
  Library/Temp/Obj/Logs 오염 없음 · 도메인(GameState/Ops/매니저/UI 컨트롤러/저장) 소스·테스트 diff 0.
- Build Settings 2씬(MainMenu→Shop) 하드캡 불변 — 기존 테스트 통과로 확인.

## Codex 코드 리뷰 결과 (2026-07-12 · reviews/001.md)

- **판정 `request-changes` — 단, "현재 코드 변경 요구는 없음"** (Codex 명시). request-changes 사유는
  순수하게 설계 D3 done 게이트(640×360 시각 승인 + 수동 Play smoke)가 미제출 상태이기 때문 —
  코드 결함 아님. Codex 는 구현이 설계 B1/C/E 절과 정합함을 확인(PaletteMaps 단일 원천·exact-match/
  alpha 보존 스왑·gimbap 정수 확대·32×32 캔버스·tteokbokki 역할 재배정 F-2 범위·SceneBuilder rect·
  테스트 보강)하고, `--check-done`·`generate-status --check`·PNG 앵커 점검 통과를 기록했다.
- Codex 는 Unity 배치 테스트를 재실행하지 않았음(커밋/노트 증거 대조만) — EditMode 439/PlayMode 8 은
  Claude 가 독립 재실행으로 확인(위 표). 시각 게이트에서 팔레트 보정이 나오면 상수·provenance·테스트
  동기 갱신 조건은 그대로.
- 결론: **코드는 검증 완료**, 남은 것은 오너 주관 게이트뿐. task-115 순차 진행에 차단 아님.

## 미결 게이트 (오너/Codex — Claude self-approve 금지)

- **D3 640×360 시각 승인 대기**: 원본+2× 캡처 4종(장르 modal/서빙 팝/Night/무대 전경)의 음식 6종
  식별·red 3종 구분·손님 4종 색 분리·warm↔navy 대비·겹침 없음 판정. 팔레트 시드 보정은 1회 한정
  (상수·provenance·테스트 동기 갱신). 보조 자료로 4× 콘택트 시트를 스크래치에 생성해 확인함 —
  방향 앵커는 화면에서 읽히나 **최종 판정은 오너/Codex 게이트**.
- **수동 Play smoke 대기(오너)**: 장보기→영업→정산 1일 루프에서 리컬러 음식 팝·student 파란 의상·
  warm 무대·Night navy 페이드·걷기 애니/수치/저장 무회귀 확인.
- 현대 NYC 오버홀·사운드 3종·CashPopup UX 최종 판단은 설계 오픈 이슈대로 오너/Codex 결정 사항.
