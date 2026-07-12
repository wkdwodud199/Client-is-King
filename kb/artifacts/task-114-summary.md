# 산출물 요약 — task-114

> Status: done
> Inputs: kb/tasks/task-114/implementation-notes.md
> Outputs: 이 요약 문서 — 아트 마감 패스(한식 리컬러 파이프라인·32×32 캔버스 표준화·정수배 rect
> 정합·F2 팔레트 방향 보정·테스트·provenance) 완료 요약과 인계
> Next step: **오너/Codex 640×360 시각 승인(D3) + 수동 Play smoke → 통과 시 task-115(밸런싱+엔딩+Windows 빌드)**

## 작업 요약

- **Task ID**: task-114
- **제목**: 아트 마감 패스 — task-109 이월 한식 톤 정합 리컬러(결정론적 exact-match 팔레트 스왑),
  음식 아이콘 32×32 캔버스 표준화, SceneBuilder 정수배 rect 정합, NightOverlay Ink Navy 보정
- **완료일**: 2026-07-12 (오너/Codex 640×360 시각 승인·수동 Play smoke 대기)

## 산출물 목록

| 산출물 | 경로 | 설명 |
|--------|------|------|
| 리컬러 파이프라인 | `game/Assets/Scripts/Editor/PlaceholderArtBuilder.cs` | 공개 `PaletteMaps` 상수(13항목, 단일 원천) + `SwapPalette`/`PrescaleIfSmall`/`PadToCanvas` 순수 헬퍼, B1 순서 배선(트림→확대→스왑→패딩), 기존 멱등/임포트 규약 유지 |
| 음식 아이콘 6종 | `game/Assets/Art/Placeholders/FoodIcons/*.png` | 한식 리컬러 + 32×32 캔버스 (국밥 2·국수 2·떡볶이·김밥 — red 3종 `#D34A3A`/`#C24A22`/`#A83226` 분화, 식기 앵커 보존, gimbap ×2 정수 확대) |
| student 5파일 | `game/Assets/Art/Placeholders/Customers/student*.png` | 의상 `#D14B34→#3F6FA6` 청 데님 (idle+walk0..3) — 손님 4종 hue 파랑/올리브/골드/그레이 분리 |
| 무대 타일 2종 | `game/Assets/Art/Placeholders/Stage/{floor,counter}.png` | Steam Cream 바닥 / warm amber 카운터 (F2 방향 보정) |
| 씬 정합 | `game/Assets/Scripts/Editor/SceneBuilder.cs`, `Scenes/{Shop,MainMenu}.unity` | FoodIcon 64×64(×2)·소품 32×32(×1)·장르 Icon (-38,0)/32×32(×1)·CashPopup (-40,120)·NightOverlay `#16202A` |
| provenance | `game/Assets/Art/Placeholders/PLACEHOLDER-PROVENANCE.md` | "task-114 리컬러/캔버스 규약" 절 — 매핑 표 전수·파이프라인 순서·tteokbokki 실측 확정·이월 완료 갱신 (기존 출처·CC0·2026-07-10 보존) |
| 테스트 | `game/Assets/Tests/EditMode/{PlaceholderArt,ShopPresentationScene,SceneBuilder}Tests.cs` | +11종: 캔버스·from 부재/앵커 존재·비대상 바이트 불변·식기 앵커·red 분화·팔레트 상한·Apply 2회 멱등·provenance 절·rect 계약·장르 Icon |
| task 기록 | `kb/tasks/task-114/`, 이 요약 | manifest·design·design-review-codex·implementation-notes |

## 주요 결정 / 이탈

- **tteokbokki 역할 재배정 (설계 F-2 위임 절차)**: Meatballs 픽셀 실측 결과 구체=`#890502`/`#730907`/
  `#4B0807`(→흰 떡), 바닥=`#5B0503`/`#881F00`(→고추장 소스), `#400002`=접촉 그림자(→소스 음영) —
  설계 시드와 반대여서 같은 from/to 집합 안에서 대응만 재배정했다. 방향 앵커("밝은 고추장
  `#D34A3A`+흰 떡 `#F2E6D8`")·D2 존재/부재 계약 불변, 상수·provenance·테스트 동기 갱신.
- **cyan 4색은 그릇 장식 무늬로 실측 확인**(고명 아님, tteokbokki·bibim_guksu 공유) — D2 부재 계약에
  따라 설계 표대로 무광 green 전환. 시각 게이트 1회 보정 후보로 기록.
- 그 외 이탈 없음 — B2/B3/B4 매핑 표·C절 rect 표·팔레트 상한·비대상 바이트 불변 전부 설계 그대로.

## 검증

- 기준선(변경 전) 실측: EditMode 428/428 · PlayMode 8/8 (설계 기재값과 일치, exit 0).
- 변경 후: 컴파일 exit 0(`error CS` 0) · **EditMode 439/439**(기준선 428 + 신규 11, 무회귀) ·
  **PlayMode 8/8**(무회귀) · `SceneBuilder.Apply` 2회 연속 실행 후 **파생 PNG 28종 md5 전부 불변**
  (씬 멱등은 오브젝트 수·persistent listener 기준 — 테스트 검증).
- `git status --short game` 변경 = 파생 PNG 13 + 코드 2 + 씬 2 + provenance + 테스트 3 (설계 영향
  파일 표와 일치). `.meta` 추가/삭제 0 · **`OpenSource/**` 무변경** · 비대상 손님 15파일 diff 0 ·
  도메인/Ops/매니저/UI 컨트롤러/저장 소스·테스트 diff 0 · Library/Temp 오염 없음 · Build Settings 2씬.
- 보조 픽셀 검증(PIL): 32×32 캔버스·from 전수 부재·D2 앵커 존재·팔레트 상한·red 3종 상호 배타 —
  0 FAIL.

## 미결(오너/Codex 게이트)

- **D3 640×360 시각 승인 대기** — 원본+2× 캡처 4종(장르 modal/서빙 팝/Night/무대 전경) 판정.
  Claude self-approve 금지, 팔레트 시드 보정은 1회 한정.
- 수동 Play smoke(오너) — 1일 루프에서 리컬러 팝·데님 student·warm 무대·navy 페이드·수치/저장 무회귀.
- 현대 NYC 아트 정체성 오버홀은 설계 오픈 이슈대로 별도 오너/Codex 결정(미결정 시 task-115 데모는
  본 마감본으로 출시).

## 관련 문서

- 설계: `kb/tasks/task-114/design.md`, Codex 교차검토: `kb/tasks/task-114/design-review-codex.md`
- 구현 노트: `kb/tasks/task-114/implementation-notes.md`(실측 확정·검증 상세)
