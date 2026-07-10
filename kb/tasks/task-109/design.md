# 설계 문서 — task-109

> Status: ready
> Inputs: `kb/concepts/art-direction.md` (승인된 디렉션 SSOT — 오너 combo 1 승인 2026-07-10), `kb/concepts/demo-scope.md` (CC0/OFL·씬 2개 하드캡, 인테리어 주차장), `kb/tasks/task-108/design.md`, `kb/tasks/task-108/implementation-notes.md` (무대/catalog/트윈 구조), 현재 Unity 프로젝트 기준선(`game/`)
> Outputs: 오픈소스 CC0 아트 도입 구현 설계 — Ninja Adventure 걷기 애니 손님, karsiori/Henry 음식 아이콘, Kenney 무대 타일 보강, `CustomerSpriteEntry.walkFrames` 확장, 걷기 프레임 스왑 코루틴, 무대 스프라이트 교체, 라이선스/provenance 문서화, 검증 기준
> Next step: 오너가 itch.io 3종(Ninja Adventure·karsiori·Henry)을 `game/Assets/Art/OpenSource/`에 배치하면 Claude가 이 설계로 구현하고 M1.5 재평가 후 통과 시 `task-110` 장르 선택으로 진행

## 목표 (Objective)

`Client is King`의 M1.5 표현 미니 패스에서 사용된 자체 생성 픽셀 아트(24×32 손님, 20×16 음식)를 CC0 오픈소스 에셋으로 교체해 품질 상한을 끌어올린다. Ninja Adventure Asset Pack의 걷기 애니메이션을 손님 입장/퇴장 동선에 적용하고, karsiori·Henry 음식 팩으로 메뉴 아이콘을 보강하며, Kenney Roguelike/RPG 타일로 무대 배경·카운터를 장식한다. 기존 M1 루프 규칙·수치·정산·파산 판정과 씬 2개 하드캡은 불변으로 유지한다.

## 범위 (Scope)

- 포함:
  - `art-direction.md`가 승인한 combo 1을 도입한다. 손님은 **Ninja Adventure Asset Pack**(CC0의 유일한 걷기 애니 캐릭터 팩), 국물요리 아이콘은 **karsiori Food Pack** 리컬러, 메뉴 아이콘 보강은 **Henry Software Pixel Food**, 무대 카운터·바닥 타일은 필요분만 **Kenney Roguelike/RPG**로 조달한다.
  - 원본 팩은 `game/Assets/Art/OpenSource/<팩명>/`에 무수정 보존하고 각 폴더에 `LICENSE-CC0.txt` 사본을 둔다. 출처 URL·버전·다운로드일·개조 방법은 provenance 문서에 기록한다.
  - 각 팩 시트의 프레임 크기·행 구성을 실측하고, `PlaceholderArtBuilder`에 spritesheet 그리드 슬라이스 로직(`TextureImporter.spriteImportMode = Multiple`, `TextureImporter.spritesheet` 배열 설정)을 코드로 추가한다. 슬라이스 규약(프레임 크기·행 인덱스)은 provenance에 문서화한다.
  - 손님 archetype 4종(`student`, `office_worker`, `family_parent`, `senior_regular`)에 대해 idle 1프레임 + 우향 걷기 3~4프레임을 준비한다. 한국식 표현이 필요하면 의상 팔레트 스왑(리컬러) 최소 개조로 흡수한다.
  - 레시피 6종(`pork_gukbap`, `beef_gukbap`, `tteokbokki`, `gimbap`, `janchi_guksu`, `bibim_guksu`) 중 국밥·국수 4종은 karsiori 스튜/그릇 리컬러로, 떡볶이·김밥은 Henry 시트에서 근접 소스를 개조한다. 근접 소스가 없으면 현 v2 픽셀맵을 유지한다.
  - `SpriteCatalog.CustomerSpriteEntry`에 `Sprite[] walkFrames`와 idle 프레임 필드를 추가한다. 기존 단일 `sprite` 필드는 idle/fallback로 하위호환 유지한다.
  - `ShopPresentationController`(또는 `PresentationTween`)에 걷기 프레임 순환 서브 코루틴을 추가한다. 이동 lerp 동안 0.12s 간격 프레임 스왑, 도착 시 idle 고정, 좌향 퇴장은 `localScale.x = -1` 플립으로 처리한다.
  - `SceneBuilder.BuildShopStage`의 `Stage_Backdrop`·`Stage_Counter`를 Kenney 타일 패턴 이미지로 교체하고, 장식 소품(테이블·화분 등) 2~3개를 순수 장식으로 배치한다. 손님 스프라이트 rect(현 48×64)와 음식 아이콘 rect(현 40×32)는 실측 프레임 기준 정수배로 재조정한다.
  - `SceneBuilder`의 catalog 로드(`LoadCustomerSpriteEntries`/`LoadRecipeSpriteEntries`)와 `EditorInit` 주입을 `walkFrames` 확장에 맞춰 갱신한다.
  - 임포트 설정은 기존 파이프라인(Sprite·PPU 32·Point·무압축·mipmap off — `ApplyImportSettings` 패턴)을 재사용한다.
  - EditMode 테스트를 갱신해 새 에셋 존재·임포트 설정·`walkFrames` 슬라이스 개수·무대 스프라이트 존재·provenance/LICENSE 기록을 검증하고, 기존 90종 회귀가 지속 통과하게 한다.
  - 구현 완료 기록으로 `kb/tasks/task-109/manifest.md`, `implementation-notes.md`, `kb/artifacts/task-109-summary.md`, `kb/index/status.md`를 갱신하도록 포함한다.
- 제외:
  - 인테리어 시스템화(좌석·동선·충돌·상호작용)는 `demo-scope.md`의 주차장이다. 무대 개선은 순수 장식(M1.5 상한)까지만 하며 시스템을 추가하지 않는다.
  - 장르 선택·장르별 배수는 `task-110` 범위다. SNS 마케팅은 `task-111`, 이벤트/장애물은 `task-112`, 저장/불러오기는 `task-113` 범위다.
  - 아트 마감 패스(고해상 리소스·전면 재작업·통합 팔레트 확정)는 `task-114` 범위다. 밸런싱·엔딩·Windows 빌드는 `task-115` 범위다.
  - Animator·AnimationClip·외부 tween/animation 프레임워크(DOTween·LeanTween 등)는 추가하지 않는다. 걷기 애니는 코루틴 프레임 스왑으로만 구현한다.
  - 신규 씬·신규 ScriptableObject 타입·신규 매니저 singleton·Tilemap 컴포넌트는 추가하지 않는다. 무대는 기존 UI Image 반복 패턴으로 처리한다.
  - 유료 asset, 라이선스가 CC0/OFL이 아닌 asset(LimeZu Modern Interiors·CC-BY 팩 등), 데모 하드캡을 넘는 커스텀 아트 제작은 포함하지 않는다.
  - M1 루프 규칙 변경(시작 자금·구매 가격·주문 생성·판매가·운영비·정산 멱등성·파산 판정)은 포함하지 않는다.

## 제약 (Constraints)

- `kb/concepts/art-direction.md`가 아트 도입 디렉션의 SSOT다. 이 설계는 그 6개 결정(에셋 조합·스케일 정책·한식 개조·걷기 애니 방식·무대 범위·통합 체크리스트)과 라이선스 운영 규칙을 구현 작업으로 구체화한 것이다.
- 아트는 **CC0/OFL만** 도입한다(`demo-scope.md` 하드캡). combo 1의 4팩은 전부 CC0 원문 확인 완료다. CC0라도 출처·버전·다운로드일을 provenance에 기록한다(재현성).
- `MainMenu.unity`와 `Shop.unity` 2씬 하드캡을 깨지 않는다. 씬을 추가하지 않는다.
- Unity 구현 대상은 `game/` 하위로 제한한다. task 기록은 `kb/`에 둔다.
- 걷기 애니는 Animator/AnimationClip 없이 `MonoBehaviour.StartCoroutine` 프레임 스왑 + `localScale.x = -1` 플립으로만 구현한다. 이동 트윈은 기존 `PresentationTween.MoveAnchored`(anchoredPosition lerp)를 유지한다.
- 임포트 설정은 PPU 32, Point filter, 무압축, mipmap off, Sprite type을 유지한다. 슬라이스 시트는 `spriteImportMode = Multiple`이며 임포트 설정이 테스트로 고정되어야 한다.
- `PlaceholderArtBuilder`의 멱등성을 유지한다. 바이트 동일 시 재기록을 금지해 GUID를 안정시키고, 슬라이스 그리드 설정도 동일 입력에서 반복 실행 시 중복·drift가 없어야 한다.
- `SceneBuilder.Apply`는 멱등해야 한다. 같은 입력에서 여러 번 실행해도 중복 오브젝트·누락 참조·깨진 `.meta`가 생기면 안 된다.
- Unity 에셋과 `.meta`는 쌍이다. 원본 팩·개조본·슬라이스 산출물에 대응하는 `.meta`가 함께 생성/보존되어야 한다. `Library`, `Temp`, `Obj`, `Logs`, `UserSettings`, `Build*` 산출물은 git에 노출하지 않는다.
- 기존 M1 플레이어블 규칙·수치를 변경하지 않는다. 표현 레이어는 저장 대상이 아니며 `GameState`에 `Sprite[]`·coroutine 상태 등 Unity 참조나 transient 필드를 추가하지 않는다.
- `CanvasScaler` 기준 해상도 640×360, PixelPerfectCamera PPU 32, 한글 TMP 폰트 계약을 유지한다. 손님/음식 rect는 정수배 스케일로 픽셀 정합을 확보한다.
- Play 모드 수동 테스트 전에도 배치 `SceneBuilder.Apply`, 컴파일 게이트, EditMode 테스트가 통과해야 한다.
- 외부 입력 의존: itch.io 3종(Ninja Adventure·karsiori·Henry)은 고정 직링크가 없어 오너 수동 다운로드가 선행되어야 코드 구현이 진행 가능하다(Kenney는 curl 직링크). 이는 **구현 진행도** 의존성이며 설계 준비도와 무관하다(오픈 이슈 참조).

## 구현 단계 (Implementation Steps)

1. `game/Assets/Art/OpenSource/` 하위 구조를 확정한다. 팩별 폴더(`NinjaAdventure/`, `karsiori-FoodPack/`, `HenrySoftware-PixelFood/`, `Kenney-RoguelikeRPG/`)와 각 폴더 `LICENSE-CC0.txt`, 개조 산출물이 들어갈 기존 catalog 경로(`Assets/Art/Placeholders/{Customers,FoodIcons}/`)를 확정한다.
2. Kenney Roguelike/RPG 팩을 curl 직링크로 다운로드해 `Kenney-RoguelikeRPG/`에 무수정 배치한다. itch.io 3종은 오너가 수동 다운로드해 배치한 산출물을 전제로 진행한다(오픈 이슈).
3. 각 팩에 `LICENSE-CC0.txt` 원문 사본을 두고, `PLACEHOLDER-PROVENANCE.md`에 팩명·작가·출처 URL·버전·다운로드일·사용 파일 목록을 기록한다. 파일명은 기존 provenance 항목과 동일 포맷을 따른다.
4. 각 시트의 프레임 크기·행 구성을 실측한다. Ninja Adventure 캐릭터 시트는 4방향 걷기 중 **우향 행 인덱스**를 식별하고, karsiori/Henry 음식 시트는 대상 아이콘의 프레임 좌표를 식별한다. 실측 결과(프레임 크기·행 인덱스·좌표)를 provenance에 슬라이스 규약으로 문서화한다.
5. `PlaceholderArtBuilder`에 spritesheet 그리드 슬라이스 로직을 추가한다. `TextureImporter.spriteImportMode = SpriteImportMode.Multiple`, `spritesheet` 배열(`SpriteMetaData` 그리드)을 실측 프레임 크기로 설정하고, 기존 `ApplyImportSettings`(PPU 32·Point·무압축·mipmap off)를 재사용한다. 바이트/설정 동일 시 재기록을 금지해 멱등성·GUID 안정을 유지한다.
6. 손님 archetype 4종에 대해 idle 1 + 우향 걷기 3~4프레임을 슬라이스 산출한다. 한국식 표현이 필요한 archetype은 의상 팔레트 스왑(리컬러)을 빌더 확장 또는 1회 수작업으로 적용하고 개조 방법을 provenance에 기록한다.
7. 국밥·국수 4종은 karsiori 스튜/그릇 리컬러로 아이콘을 산출하고, 떡볶이·김밥은 Henry 시트에서 근접 소스를 개조한다. 근접 소스가 없으면 현 v2 픽셀맵을 유지하고 그 판단을 provenance에 남긴다. 개조본은 기존 catalog 경로에 교체 커밋한다.
8. `SpriteCatalog.CustomerSpriteEntry`에 `Sprite[] walkFrames`와 idle 프레임 필드를 추가한다. 기존 단일 `sprite`는 idle/fallback로 유지해 하위호환을 보장하고, `walkFrames`가 비어 있으면 단일 스프라이트로 폴백하게 한다.
9. `ShopPresentationController.FindCustomerSprite`/catalog 조회 경로를 `walkFrames`/idle 반환까지 지원하도록 확장하고, 미지 id fallback(예외 없음) 계약을 유지한다.
10. 걷기 프레임 순환 서브 코루틴을 추가한다(`ShopPresentationController` 또는 `PresentationTween`). `MoveAnchored` 이동 코루틴 동안 0.12s 간격으로 `walkFrames`를 순환하고, 도착 시 idle 프레임으로 고정한다. `Application.isPlaying` 가드로 EditMode에서는 즉시 최종 상태로 스냅한다.
11. 좌향 퇴장(ExitUnhappy, x=-380 방향)은 손님 이미지 `localScale.x = -1` 플립을 적용하고, 우향 이동/입장 시 `localScale.x = 1`로 복원한다. 스프라이트 재작업 없이 우향 시트만 사용한다.
12. `SceneBuilder`의 `LoadCustomerSpriteEntries`/`LoadRecipeSpriteEntries`가 슬라이스된 서브 스프라이트(walkFrames·idle·음식 아이콘)를 id별로 로드하도록 갱신하고, `EditorInit`(catalog 주입)에 `walkFrames`가 채워지도록 확장한다.
13. `SceneBuilder.BuildShopStage`의 `Stage_Backdrop`(640×160)과 `Stage_Counter`(320×32)를 Kenney 타일 패턴 스프라이트 이미지로 교체한다. 손님 `CustomerSprite` rect와 `FoodIcon` rect를 실측 프레임 기준 정수배로 재조정한다.
14. 장식 소품(테이블·화분 등) 2~3개를 순수 장식 Image로 무대에 배치한다. 좌석·동선·충돌·상호작용을 추가하지 않는다(주차장 가드). `SceneBuilder` 멱등성과 캔버스 자식 순서(무대 < 오버레이 < HUD/패널)를 유지한다.
15. `PlaceholderArtTests`를 갱신해 새 손님 idle/walkFrames·음식 아이콘 존재, 임포트 설정(Sprite·PPU 32·Point·무압축·mipmap off), 슬라이스 프레임 개수, provenance/LICENSE 기록을 검증한다.
16. `ShopPresentationSceneTests`를 갱신해 무대 배경·카운터·소품 스프라이트 오브젝트 존재, catalog에 `walkFrames`가 주입됨, 미지 id fallback 무예외, NightOverlay 초기 alpha 0을 검증한다.
17. 기존 EditMode 90종(`FirstPlayableLoopTests`·`ServiceOpsTests`·`SettlementOpsTests`·scene/UI 테스트 등)이 아트 교체 후에도 그대로 통과하도록 갱신한다.
18. Kenney 다운로드 후 원본 팩·개조본·슬라이스 산출물의 `.meta`가 모두 생성/보존됐는지, Unity 캐시/빌드 산출물이 git에 노출되지 않는지 확인한다.
19. Unity 배치 모드에서 `PlaceholderArtBuilder` 슬라이스 → `SceneBuilder.Apply` → 컴파일 게이트 → EditMode 테스트를 실행한다.
20. 수동 Play smoke 기준을 구현 노트에 기록한다. 손님이 걷기 애니로 입장/퇴장하고, 좌향 퇴장 시 플립되며, 새 음식 아이콘과 무대 타일이 표시되고, 기존 서빙/정산/밤 연출이 유지됨을 확인한다.
21. `kb/tasks/task-109/implementation-notes.md`, `kb/artifacts/task-109-summary.md`, `kb/index/status.md`를 갱신하고 done-gate를 통과시킨다.

## 실행 계획 (Execution Plan)

- implement_model: claude-fable-5
- implement_effort: high
- routing_reason: 시트 실측·코드 슬라이스, catalog `walkFrames` 확장, 걷기 코루틴+플립, 무대 스프라이트 교체, 기존 90종 회귀를 함께 다루는 M1.5 아트 도입 게이트 작업으로 fable-5/high가 적합하다.

| unit | 파일 범위 | depends_on | group |
|------|-----------|------------|-------|
| U1-asset-intake | `game/Assets/Art/OpenSource/**` (원본 4팩 무수정 + `LICENSE-CC0.txt`), `game/Assets/Art/Placeholders/PLACEHOLDER-PROVENANCE.md`, 관련 `.meta` | 없음 | G1 |
| U2-sheet-slice | `game/Assets/Scripts/Editor/PlaceholderArtBuilder.cs` (spritesheet 그리드 슬라이스 + 임포트 재사용), `game/Assets/Art/Placeholders/{Customers,FoodIcons}/**` 슬라이스/리컬러 산출물, 관련 `.meta` | U1-asset-intake | G2 |
| U3-catalog-walkframes | `game/Assets/Scripts/Runtime/Presentation/SpriteCatalog.cs` (`CustomerSpriteEntry.walkFrames`+idle), `ShopPresentationController.cs` 조회 확장 | U2-sheet-slice | G3 |
| U4-walk-anim | `game/Assets/Scripts/Runtime/Presentation/{PresentationTween,ShopPresentationController}.cs` (프레임 스왑 코루틴 + 좌향 플립) | U3-catalog-walkframes | G4 |
| U5-scene-stage | `game/Assets/Scripts/Editor/SceneBuilder.cs` (무대 타일/카운터/소품 교체 + catalog 주입 확장), `game/Assets/Scenes/Shop.unity` | U2-sheet-slice, U3-catalog-walkframes, U4-walk-anim | G5 |
| U6-editmode-tests | `game/Assets/Tests/EditMode/{PlaceholderArt,ShopPresentationScene}Tests.cs` 갱신, 기존 scene/UI 테스트 회귀 확인 | U2-sheet-slice, U4-walk-anim, U5-scene-stage | G6 |
| U7-validation-pass | Unity 배치 슬라이스/`SceneBuilder.Apply` 로그, 컴파일 로그, EditMode 결과 XML, design/done validator 결과 | U5-scene-stage, U6-editmode-tests | G7 |
| U8-task-records | `kb/tasks/task-109/manifest.md`, `kb/tasks/task-109/implementation-notes.md`, `kb/artifacts/task-109-summary.md`, `kb/index/status.md` | U7-validation-pass | G8 |

## 파일/모듈 영향 (Affected Files/Modules)

| 파일/모듈 | 변경 유형 | 설명 |
|-----------|-----------|------|
| `game/Assets/Art/OpenSource/NinjaAdventure/**` | create | 손님 걷기 애니 원본 팩 무수정 보존 + `LICENSE-CC0.txt` 사본 |
| `game/Assets/Art/OpenSource/karsiori-FoodPack/**` | create | 국물요리 리컬러 소스 팩 무수정 보존 + `LICENSE-CC0.txt` 사본 |
| `game/Assets/Art/OpenSource/HenrySoftware-PixelFood/**` | create | 메뉴 아이콘 보강 팩 무수정 보존 + `LICENSE-CC0.txt` 사본 |
| `game/Assets/Art/OpenSource/Kenney-RoguelikeRPG/**` | create | 카운터·바닥 타일 보강 팩(curl 직링크) 무수정 보존 + `LICENSE-CC0.txt` 사본 |
| `game/Assets/Art/Placeholders/Customers/*.png` | modify | archetype 4종 idle+walkFrames 슬라이스/리컬러 산출물로 교체 |
| `game/Assets/Art/Placeholders/FoodIcons/*.png` | modify | 국밥·국수 karsiori 리컬러, 떡볶이·김밥 Henry 개조(또는 v2 유지)로 교체 |
| `game/Assets/Art/Placeholders/PLACEHOLDER-PROVENANCE.md` | modify | 팩별 출처 URL·버전·다운로드일·슬라이스 규약·개조 방법 기록 |
| `game/Assets/Scripts/Editor/PlaceholderArtBuilder.cs` | modify | spritesheet 그리드 슬라이스 로직 추가, `ApplyImportSettings` 재사용, 멱등성 유지 |
| `game/Assets/Scripts/Runtime/Presentation/SpriteCatalog.cs` | modify | `CustomerSpriteEntry`에 `Sprite[] walkFrames`+idle 필드 추가, 단일 `sprite` 하위호환 |
| `game/Assets/Scripts/Runtime/Presentation/ShopPresentationController.cs` | modify | 걷기 프레임 스왑 서브 코루틴 + 좌향 플립, catalog 조회 확장, 미지 id fallback 유지 |
| `game/Assets/Scripts/Runtime/Presentation/PresentationTween.cs` | modify | 걷기 프레임 순환 코루틴 helper 추가(또는 컨트롤러에 배치) |
| `game/Assets/Scripts/Editor/SceneBuilder.cs` | modify | `Stage_Backdrop`/`Stage_Counter` 타일 교체, 장식 소품 배치, catalog 주입(`walkFrames`) 확장, rect 정수배 재조정 |
| `game/Assets/Scenes/Shop.unity` | modify | SceneBuilder 재생성으로 무대 스프라이트·소품 반영된 씬 산출물 |
| `game/Assets/Scenes/MainMenu.unity` | modify | SceneBuilder 재실행으로 저장될 수 있으나 기능 변경은 없음 |
| `game/Assets/Tests/EditMode/PlaceholderArtTests.cs` | modify | 새 에셋 존재·임포트 설정·슬라이스 프레임 개수·provenance/LICENSE 검증 |
| `game/Assets/Tests/EditMode/ShopPresentationSceneTests.cs` | modify | 무대 스프라이트·소품 존재, `walkFrames` 주입, 미지 id fallback, NightOverlay alpha 0 검증 |
| `game/**/*.meta` | create/modify | 원본 팩·슬라이스 산출물·개조본·씬 변경에 대응하는 `.meta` 쌍 |
| `kb/tasks/task-109/manifest.md` | create | done-gate용 inputs/concepts_needed/related_files |
| `kb/tasks/task-109/implementation-notes.md` | create | 구현 진행/결정·다운로드 상태·개조 방법·smoke 결과 기록 |
| `kb/artifacts/task-109-summary.md` | create | 아트 도입 완료 요약과 M1.5 재평가 인계 사항 |
| `kb/index/status.md` | modify | `runtime/generate-status.py`로 재생성되는 task 상태 보드 |

## 테스트 기준 (Test Criteria)

- [ ] `python runtime/validator/cli.py kb/tasks/task-109/design.md`가 종료 코드 0으로 통과한다.
- [ ] Unity 배치 모드에서 `PlaceholderArtBuilder` 슬라이스 후 `SceneBuilder.Apply`가 성공한다: `Unity.exe -batchmode -quit -nographics -projectPath game -executeMethod ClientIsKing.EditorTools.SceneBuilder.Apply -logFile <temp-log>` 종료 코드가 0이다.
- [ ] 배치 컴파일 게이트가 통과한다: `Unity.exe -batchmode -quit -nographics -projectPath game -logFile <temp-log>` 종료 코드가 0이고 로그에 `error CS`가 없다.
- [ ] EditMode 테스트가 통과한다: `Unity.exe -batchmode -nographics -projectPath game -runTests -testPlatform EditMode -testResults <temp-results> -logFile <temp-log>` 종료 코드가 0이다.
- [ ] `PlaceholderArtTests`는 손님 archetype 4종의 idle 스프라이트와 우향 walk 프레임(각 3~4)이 존재한다고 검증한다.
- [ ] `PlaceholderArtTests`는 레시피 6종 음식 아이콘이 존재하고, 모든 스프라이트 임포트 설정이 Sprite, PPU 32, Point filter, 무압축, mipmap off라고 검증한다.
- [ ] `PlaceholderArtTests`는 슬라이스된 시트의 서브 스프라이트 개수가 실측 슬라이스 규약과 일치한다고 검증한다.
- [ ] `PlaceholderArtTests`는 `PLACEHOLDER-PROVENANCE.md`에 도입 팩별 출처·버전·다운로드일·개조 방법이 기록되어 있고, 각 원본 팩 폴더에 `LICENSE-CC0.txt`가 존재한다고 검증한다.
- [ ] `ShopPresentationSceneTests`는 `CustomerSpriteEntry.walkFrames`가 catalog 4종에 주입됐고, `walkFrames`가 비어도 단일 `sprite`로 폴백한다고 검증한다.
- [ ] `ShopPresentationSceneTests`는 `Shop.unity`에 교체된 무대 배경·카운터 스프라이트 오브젝트와 장식 소품이 존재한다고 검증한다.
- [ ] `ShopPresentationSceneTests`는 알려지지 않은 `customerId`/`recipeId`가 들어와도 예외 없이 fallback으로 복구하고, NightOverlay 초기 alpha가 0이라고 검증한다.
- [ ] `SceneBuilderTests` 또는 presentation scene 테스트는 Build Settings에 `MainMenu.unity`·`Shop.unity` 2개만 등록된 씬 하드캡이 유지된다고 검증한다.
- [ ] 기존 `FirstPlayableLoopTests`, `ServiceOpsTests`, `SettlementOpsTests`, `EconomyManagerTests`, `InventoryManagerTests`, `MarketPanelSceneTests`, `ServicePanelSceneTests`, `SettlementPanelSceneTests`, `DataDefinitionTests`, `DayPhaseMachineTests`가 아트 교체 후에도 그대로 통과한다(EditMode 90종 회귀 기준선).
- [ ] 수동 Play smoke 기준: 손님이 걷기 애니로 입장해 카운터 앞 정지(idle 고정), 서빙 후 우향 만족 퇴장 / 포기 시 `localScale.x = -1` 좌향 불만 퇴장이 확인된다.
- [ ] 수동 Play smoke 기준: 새 음식 아이콘 팝, 교체된 무대 타일·카운터·소품 표시, 기존 정산 카운트업·밤 오버레이 연출이 유지되고 M1 진행 규칙이 불변임이 확인된다.
- [ ] `git status --short game`에 Unity 캐시/빌드 산출물(`Library`, `Temp`, `Obj`, `Logs`, `UserSettings`, `Build*`)이 나타나지 않고, 원본 팩·슬라이스 산출물의 `.meta` 쌍이 모두 존재한다.
- [ ] 구현 완료 후 `python runtime/validator/cli.py --check-done task-109`와 `python runtime/generate-status.py --check`가 통과한다.

## 오픈 이슈 (Open Issues)

- **외부 입력 의존(구현 진행도 차단, 설계 준비도 무관)**: itch.io 3종(Ninja Adventure·karsiori·Henry)은 고정 직링크가 없어 오너가 각 $0 다운로드해 `game/Assets/Art/OpenSource/<팩명>/`에 배치해야 코드 구현(슬라이스 이후 단계)이 진행 가능하다. Kenney Roguelike/RPG만 curl 직링크로 자동 취득한다. 이 의존성은 `implementation-notes.md`에서 blocked(외부 입력 대기)로 관리하며, design.md `Status`는 `ready`로 둔다.
- **시트 실측 미확정**: 각 팩의 프레임 크기·행 구성(특히 Ninja Adventure 4방향 걷기의 우향 행 인덱스, karsiori/Henry 대상 아이콘 좌표)은 실제 다운로드 산출물을 열어 실측해야 확정된다. 구현 1단계(실측 → provenance 슬라이스 규약)에서 확정하며, 팩별 프레임 크기 혼재(16×16/16×24 등) 가능성을 슬라이스 그리드에서 흡수한다.
- **한식 개조 톤 편차**: 에도풍→현대 한국 표현 괴리와 음식 팩 간 팔레트 톤 차이는 리컬러 1회로 흡수하되, 근접 소스가 없는 떡볶이·김밥은 현 v2 픽셀맵 유지로 폴백한다. 톤 정합 고도화는 `task-114`(아트 마감 패스) 범위이며 본 task에서 확장하지 않는다.
