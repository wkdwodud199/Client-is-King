# Placeholder Art Provenance (task-109 — OpenSource CC0 도입 패스)

task-108 의 자체 생성 픽셀맵을 **CC0 오픈소스 팩 서브셋 파생**으로 교체했다.
원본 팩은 `Assets/Art/OpenSource/<팩명>/` 아래 **무수정 보존(source-of-truth)** 하며,
`PlaceholderArtBuilder`(에디터 스크립트)가 그 시트에서 특정 프레임/타일을 잘라
개별 PNG(`Customers/`·`FoodIcons/`·`Stage/`)로 파생한다.

재생성: `Unity.exe -batchmode -quit -nographics -projectPath game -executeMethod ClientIsKing.EditorTools.SceneBuilder.Apply`
(SceneBuilder.Apply 가 PlaceholderArtBuilder.Apply 를 선행 호출).
파생 규약: PNG 를 `File.ReadAllBytes` + `Texture2D.LoadImage`(임포트 설정 무관 readable)로 읽어
`GetPixels32` → 영역 슬라이스 → `EncodeToPNG` → 파일 기록. 바이트 동일 시 재기록 금지(GUID 안정).
임포트 표준: Sprite · PPU 32 · Point · 무압축 · mipmap off · Single 슬라이스.

> 한식 톤 정합 리컬러·32×32 캔버스 표준화는 `task-114`(아트 마감 패스)로 **적용 완료** —
> 아래 "task-114 리컬러/캔버스 규약" 절 참조. 매핑 표의 단일 원천은
> `PlaceholderArtBuilder.PaletteMaps` 공개 상수이며 테스트가 같은 표를 검증한다.

## 도입 팩 (전부 CC0 — 다운로드일 2026-07-10)

| 팩 | 작가 | 출처 URL | 라이선스 | 라이선스 파일 |
|----|------|----------|----------|----------------|
| Ninja Adventure Asset Pack | Pixel-Boy / AAA (Studio) | https://pixel-boy.itch.io/ninja-adventure-asset-pack | CC0 1.0 | `OpenSource/NinjaAdventure/LICENSE.txt` |
| Pixel Art Food Pack (karsiori) | karsiori | https://karsiori.itch.io/pixel-art-food-pack | CC0 1.0 | `OpenSource/karsiori-FoodPack/Pixel Art Food Pack - Read Me.txt` |
| Free Pixel Food (Henry Software) | Henry Software (ben/david Henry) | https://henrysoftware.itch.io/pixel-food | CC0 1.0 | `OpenSource/HenrySoftware-PixelFood/readme.txt` |
| Roguelike/RPG pack (Kenney) | Kenney Vleugels (www.kenney.nl) | https://kenney.nl/assets/roguelike-rpg-pack | CC0 1.0 | `OpenSource/Kenney-RoguelikeRPG/License.txt` |

> karsiori·Henry 팩은 배포본에 별도 `LICENSE-CC0.txt` 파일이 없어 팩의 Read Me/readme 텍스트를
> 라이선스 근거로 사용한다(itch.io 배포 페이지 CC0 명시). 원본 팩은 무수정 보존이며 파일을 추가·수정하지 않는다.

## Customers/ — 손님 archetype 4종 (Ninja Adventure 우향 idle + 걷기 4프레임, 16×16px)

**아키타입→캐릭터**: student→Boy, office_worker→ManGreen, family_parent→Woman, senior_regular→OldMan.
소스 시트: `OpenSource/NinjaAdventure/Character/<char>/SeparateAnim/`.
- `Walk.png` 64×64 = 4방향(행) × 4프레임(열), 프레임 16×16.
  방향 행 배열(시각 위→아래): 행0=아래(정면), 행1=위(후면), 행2=좌, 행3=우.
- `Idle.png` 64×16 = 4프레임(4방향).
- **슬라이스 규약**: 우향 = 이미지 맨 아래 행(행3). `Texture2D.LoadImage` 는 bottom-origin 이므로
  우향 행은 텍스처 y∈[0,16). 걷기 4프레임 x=0,16,32,48. Idle 우향 = `Idle.png` 프레임 index 3(x∈[48,64), y∈[0,16)).

| 파일 | 소스 | 파생 |
|------|------|------|
| Customers/student.png | Boy/SeparateAnim/Idle.png | idle 프레임3 (우향, x[48,64) y[0,16)) |
| Customers/student_walk0.png, student_walk1.png, student_walk2.png, student_walk3.png | Boy/SeparateAnim/Walk.png | 우향 행(y[0,16)) 프레임 0..3 |
| Customers/office_worker.png | ManGreen/SeparateAnim/Idle.png | idle 프레임3 (우향) |
| Customers/office_worker_walk0.png, office_worker_walk1.png, office_worker_walk2.png, office_worker_walk3.png | ManGreen/SeparateAnim/Walk.png | 우향 행 프레임 0..3 |
| Customers/family_parent.png | Woman/SeparateAnim/Idle.png | idle 프레임3 (우향) |
| Customers/family_parent_walk0.png, family_parent_walk1.png, family_parent_walk2.png, family_parent_walk3.png | Woman/SeparateAnim/Walk.png | 우향 행 프레임 0..3 |
| Customers/senior_regular.png | OldMan/SeparateAnim/Idle.png | idle 프레임3 (우향) |
| Customers/senior_regular_walk0.png, senior_regular_walk1.png, senior_regular_walk2.png, senior_regular_walk3.png | OldMan/SeparateAnim/Walk.png | 우향 행 프레임 0..3 |

> 좌향(불만 퇴장)은 스프라이트 재작업 없이 런타임 `localScale.x = -1` 플립으로 처리(우향 시트만 사용).

## FoodIcons/ — 레시피 6종 (karsiori/Henry 매핑 + task-114 한식 리컬러·32×32 캔버스)

투명 여백 트림 후 task-114 파이프라인(정수 사전 확대 → 팔레트 스왑 → 32×32 캔버스 패딩) 적용.
karsiori 그릇 트림 ~30×24px(31px 포함), Sushi 트림 13×13px → ×2 확대 26×26px.

| 파일 | 소스 | 비고 |
|------|------|------|
| FoodIcons/pork_gukbap.png | karsiori-FoodPack/Carrot stew.png | 국밥 → 스튜 그릇 매핑 |
| FoodIcons/beef_gukbap.png | karsiori-FoodPack/Pumpkin soup.png | 국밥 → 수프 그릇 매핑 |
| FoodIcons/janchi_guksu.png | karsiori-FoodPack/Mushroom Stew.png | 국수 → 스튜 그릇 매핑 |
| FoodIcons/bibim_guksu.png | karsiori-FoodPack/Tomato stew.png | 국수 → 스튜 그릇 매핑 |
| FoodIcons/tteokbokki.png | karsiori-FoodPack/Meatballs.png | 떡볶이 → 미트볼 접시 매핑 |
| FoodIcons/gimbap.png | HenrySoftware-PixelFood/Food/Sushi.png | 김밥 → 스시 매핑 (16×16) |

## Stage/ — 무대 타일 (Kenney Roguelike/RPG, 16×16px 순수 장식)

소스 시트: `OpenSource/Kenney-RoguelikeRPG/roguelikeSheet_transparent.png` (968×526).
타일 16×16 + 타일 간 1px 여백 → 타일 (col,row) 원점 = (col×17, row×17). row 는 시트 top-origin 그리드.

| 파일 | 소스 타일 (col,row) | 용도 |
|------|---------------------|------|
| Stage/floor.png | (8,1) 크림 plaster | Stage_Backdrop 배경(Tiled 반복) |
| Stage/counter.png | (6,10) 나무 판자 | Stage_Counter 카운터(Tiled 반복) |

> 무대는 순수 장식(M1.5 상한) — 좌석/동선/충돌/상호작용 시스템 없음(demo-scope 주차장 가드).
> 소품(카운터 위 그릇 장식)은 파생한 FoodIcons 스프라이트를 재활용(추가 원본 없음).

## task-114 리컬러/캔버스 규약 (아트 마감 패스 — 2026-07-12)

task-109 에서 이월된 한식 톤 정합 리컬러를 `PlaceholderArtBuilder` 파생 단계의
**결정론적 exact-match 팔레트 스왑**으로 적용했다. 매핑 표의 단일 원천은
`PlaceholderArtBuilder.PaletteMaps` 공개 상수이며(테스트가 같은 표로 from 부재·앵커 존재를 검증),
아래 표는 그 사본이다. from 색은 전부 2026-07-12 파생 PNG 실측값, to 색은 task-114 설계 시드(D2).

**파생 파이프라인 (순서 고정)**: 원본 슬라이스/투명 트림 →
① 정수 사전 확대 — 트림 결과 w≤16 ∧ h≤16 인 음식 소스만 nearest-neighbor ×2 (gimbap 13×13→26×26 만 해당) →
② 팔레트 스왑 — 불투명 픽셀 exact-match 단일 패스 (alpha 보존 · 미등재 색 통과 · HSV/디더링 금지) →
③ 캔버스 패딩 — 음식 아이콘만 32×32 투명 캔버스 중앙 배치 (offset = floor((32−w)/2), floor((32−h)/2),
bottom-origin — 홀수 잔여는 내용이 좌/하로 붙음) → WritePng 바이트 비교 멱등.
손님(student)·무대 타일은 ② 스왑만 적용(16×16 유지).

그릇·식기 계열(국그릇 공통 `#501C07`/`#B3400F`/`#3B0A09`/`#2C0302`/`#8D2D04`/`#D15C2B`/`#6F2709`/`#A74116`,
접시 계열 포함)은 **매핑하지 않는다** — 원본 팩의 식기 세트 일관성 보존.

### 음식 6종 매핑 (from → to)

| 파일 | 목표 톤 | 매핑 | 유지(비매핑) |
|------|---------|------|--------------|
| pork_gukbap.png | 뽀얀 돼지국밥 | `#E3C77B→#EDE3D2`(국물 밝음) · `#D1B770→#E2D6C0`(국물 중간) · `#B79661→#CBBBA0`(국물 음영) · `#D1671D→#8A5B3F`(편육) · `#E5792B→#A6714C`(고기 밝음) · `#A25119→#6E4630`(고기 음영) | 허브 green `#73AD48`/`#568931` |
| beef_gukbap.png | 얼큰 소고기국밥 | `#C14202→#C24A22`(국물 주) · `#FF6F00→#E67A3C`(기름 하이라이트) · `#C0E400→#6FA04E`(대파 밝음) · `#8EA800→#4E7A33`(대파 음영) · `#F7E0A0→#F2D9A6`(기름방울) | `#E3C77B` · `#300200` |
| janchi_guksu.png | 맑은 잔치국수 | `#EAD374→#F2E9CF`(맑은 국물) · `#BFAD67→#E3D8B8`(국물 중간) · `#A59244→#C9BC94`(국물 음영) · `#B9B9B9→#FBF7EB`(소면 밝음) · `#9B9B9B→#EFE7D4`(소면 중간) · `#7E7881→#CFC5AE`(소면 음영) · `#CBB660→#E8C86A`(지단) | greens `#61812F`/`#85AD48` |
| bibim_guksu.png | 비빔국수 | `#C30500→#A83226`(양념 주) · `#AA110D→#9E2D1F`(양념 중간) · `#880300→#6E1C12`(양념 음영) · `#E3C77B→#E8C86A`(면 하이라이트) · `#28B3BC→#79A84C` · `#0B696F→#4E7A33` · `#174E74→#3D6626` · `#0B325D→#2F5220`(그릇 장식 무늬 → 무광 green) | greens `#598936`/`#73AD48` · `#D1671D`(당근채) · `#300200`/`#812700` |
| tteokbokki.png | 밝은 떡볶이 | `#5B0503→#D34A3A`(바닥 소스 주 — F2 Gochujang Red) · `#881F00→#C74534`(소스 중간) · `#400002→#A93325`(구체 접촉 그림자→소스 음영) · `#630200→#8C2A1E`(깊은 소스 점) · `#890502→#F2E6D8`(구체 밝음→떡 밝음) · `#730907→#E7D6C2`(구체 중간→떡 중간) · `#4B0807→#D9C7B2`(구체 음영→떡 음영) · `#28B3BC→#6FA04E` · `#0B696F→#4E7A33` · `#174E74→#3D6626` · `#0B325D→#2F5220`(그릇 장식 무늬 → 무광 green) | greens `#2C6A12`/`#21550B` · `#300200`/`#812700` |
| gimbap.png | 김밥 | `#170625→#1B2A1A`(김 주) · `#1C1030→#223420`(김 결) · `#362939→#2C4128`(김 음영) · `#524654→#3A4F36`(김 경계) | 밥 `#DFD9C8`/`#FFFEE8`/`#B4A8A0`/`#8B7F7E` · 속재료 소색상 전부 |

> **red 3종 분화**: 떡볶이 `#D34A3A`(밝은 소스+흰 떡) > 얼큰국밥 `#C24A22`(주황빛+국그릇) >
> 비빔국수 `#A83226`(진한 양념+노란 면) — 세 주색은 각자 자기 파일에만 존재한다(테스트 고정).

### tteokbokki 역할 실측 확정 (설계 F-2)

Meatballs 소스 픽셀 레이아웃 실측 결과, 구체(미트볼)는 `#890502`(밝음)/`#730907`(중간)/`#4B0807`(음영),
바닥 소스는 `#5B0503`(지배)/`#881F00`(밝은 영역), `#400002`는 구체 접촉 그림자였다 — 설계 시드의
구체/바닥 가정과 반대여서 **같은 from/to 집합 안에서 역할을 재배정**했다(방향 앵커
"밝은 고추장 소스 `#D34A3A` + 흰 떡 `#F2E6D8`" 불변). cyan 4색(`#28B3BC` 계열)은 고명이 아니라
**그릇 장식 무늬**로 실측 확인(bibim_guksu 와 동일 그릇 공유) — 설계 표대로 무광 green 으로 전환.

### 손님 의상·무대 타일 매핑

| 대상 | 매핑 | 근거 |
|------|------|------|
| Customers/student.png + student_walk0..3.png (5파일 공통) | `#D14B34 → #3F6FA6` (의상 주색 — 청 데님) | 실측 ΔRGB(2,1,10)로 Gochujang Red `#D34A3A`(음식 accent·primary action 예약색)와 사실상 동일 → 4종 의상 hue 를 파랑/올리브/골드/그레이로 분리 |
| office_worker / family_parent / senior_regular (각 5파일, 총 15파일) | **매핑 없음 — 바이트 불변** | 실측 주색 올리브/골드/그레이 — hue 분리 충분, 불필요한 개조 금지 |
| Stage/floor.png (2색 전수) | `#E6DABF→#F4E5C2` · `#D9CAA9→#E4D0A6` | Steam Cream 실내 바닥 (F2 방향 보정) |
| Stage/counter.png (4색 전수) | `#B48355→#BE8A4E` · `#C58F5C→#D3A462` · `#A07247→#A2713F` · `#8F673F→#7C5A34` | warm amber 나무 카운터 (F2 방향 보정) |

> `NightOverlay` 의 Ink Navy `#16202A` 전환은 SceneBuilder 소유(씬 산출물) — 이 문서 범위 밖.
> 파생 경로·파일 수(28종)·GUID 는 task-114 전후 불변이며, 신규 에셋은 0 이다.

## 폰트

- `Assets/Art/Fonts/Galmuri11.ttf` — 외부 OFL 폰트, 출처/라이선스는 `Assets/Art/Fonts/Galmuri-LICENSE.txt` (OFL-1.1, quiple/galmuri).
