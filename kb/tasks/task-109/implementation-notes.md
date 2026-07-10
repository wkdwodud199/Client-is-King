# 구현 노트 — task-109

> Status: in-progress
> Inputs: kb/tasks/task-109/design.md, kb/concepts/art-direction.md
> Outputs: (구현 완료 시) 오픈소스 CC0 아트 도입 — Ninja Adventure 걷기 애니 손님 + karsiori/Henry 음식 아이콘 + Kenney 무대 타일 + walkFrames catalog 확장 + 걷기 코루틴/플립
> Next step: 오너가 itch.io 3종을 game/Assets/Art/OpenSource/ 에 배치하면 슬라이스부터 코드 구현 착수 → M1.5 재평가

## 현재 상태: blocked (외부 입력 대기)

- 설계·폴더 구조·다운로드 안내는 준비됐으나 **코드 구현은 미착수**다.
- 차단 원인: itch.io 3종(Ninja Adventure·karsiori Food Pack·Henry Software Pixel Food)은 고정
  직링크가 없어 **오너 수동 다운로드**($0, 로그인 불필요)가 선행되어야 한다. 다운로드 산출물이
  `game/Assets/Art/OpenSource/<팩명>/` 에 배치되어야 시트 실측·슬라이스 이후 단계가 진행 가능하다.
- Kenney Roguelike/RPG 만 curl 직링크로 자동 취득 가능(로그인 불필요).
- 이 차단은 **구현 진행도** 이슈이며 설계 준비도와 무관하다. 따라서 design.md 의 `Status` 는
  `ready` 로 두고(검증기 통과 확인), 구현 진행 상태만 여기서 `in-progress`(blocked)로 관리한다.

## 준비 완료 항목

- 설계 문서 `kb/tasks/task-109/design.md` — validator 통과(rc0), 7섹션·메타·실행계획·영향표·테스트기준·오픈이슈 완비.
- 폴더 구조 계획: 원본은 `game/Assets/Art/OpenSource/{NinjaAdventure, karsiori-FoodPack, HenrySoftware-PixelFood, Kenney-RoguelikeRPG}/`(각 `LICENSE-CC0.txt` 포함, 무수정 보존),
  개조본(슬라이스·리컬러)은 기존 catalog 경로 `game/Assets/Art/Placeholders/{Customers,FoodIcons}/` 에 교체.
- 코드 확장 지점 확정: `SpriteCatalog.CustomerSpriteEntry.walkFrames`(+idle, 단일 sprite 하위호환),
  `PlaceholderArtBuilder` spritesheet 그리드 슬라이스, `ShopPresentationController`/`PresentationTween`
  걷기 프레임 스왑 코루틴 + 좌향 `localScale.x=-1` 플립, `SceneBuilder` 무대 타일/카운터/소품 교체 + catalog 주입 확장.

## 오너 액션 대기 (다운로드 안내)

| 팩 | 작가 | 취득 | 배치 경로 |
|----|------|------|-----------|
| Ninja Adventure Asset Pack | Pixel-boy & AAA | itch.io $0 수동 다운로드 (고정 직링크 없음) | `game/Assets/Art/OpenSource/NinjaAdventure/` |
| FREE Pixel Art — Food Pack | karsiori | itch.io $0 수동 다운로드 (고정 직링크 없음) | `game/Assets/Art/OpenSource/karsiori-FoodPack/` |
| Free Pixel Food | Henry Software | itch.io $0 수동 다운로드 (고정 직링크 없음) | `game/Assets/Art/OpenSource/HenrySoftware-PixelFood/` |
| Roguelike/RPG Pack | Kenney | curl 직링크 (자동, 로그인 불필요) | `game/Assets/Art/OpenSource/Kenney-RoguelikeRPG/` |

- 배치 후 각 폴더에 `LICENSE-CC0.txt` 원문 사본을 두고, provenance 에 출처 URL·버전·다운로드일을 기록한다.
- 확인 URL(art-direction.md 검증 완료): Ninja Adventure = https://pixel-boy.itch.io/ninja-adventure-asset-pack,
  karsiori = https://karsiori.itch.io/free-pixel-art-food-pack, Henry = https://henrysoftware.itch.io/pixel-food,
  Kenney = https://kenney.nl/assets/roguelike-rpg-pack.

## 설계 대비 변경 사항

- (구현 착수 후 기록)

## 테스트 결과

- (구현 착수 후 기록 — design.md 테스트 기준 참조)
