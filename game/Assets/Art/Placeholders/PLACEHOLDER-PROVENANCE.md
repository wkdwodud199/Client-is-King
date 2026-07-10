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

> 음식 **리컬러는 미적용**(직접 매핑) — 한식 톤 정합 리컬러는 `task-114`(아트 마감 패스)로 이월.

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

## FoodIcons/ — 레시피 6종 (karsiori/Henry 직접 매핑, 리컬러 없음)

소스 크기 유지 + 투명 여백 트림. karsiori 그릇 ~30×24px, Sushi 16×16px.

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

## 폰트

- `Assets/Art/Fonts/Galmuri11.ttf` — 외부 OFL 폰트, 출처/라이선스는 `Assets/Art/Fonts/Galmuri-LICENSE.txt` (OFL-1.1, quiple/galmuri).
