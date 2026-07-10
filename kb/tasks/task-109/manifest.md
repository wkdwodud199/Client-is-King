# Manifest — task-109

> **Load**: 기본 로드 세트의 일부. 이 task 가 *실제로* 의존하는 것만 명시해 컨텍스트를 최소화한다.
> 여기에 없는 개념/파일은 기본적으로 열지 않는다.

- **task_id**: task-109
- **inputs**: kb/concepts/art-direction.md (승인된 아트 도입 디렉션 SSOT — combo 1, 오너 승인 2026-07-10), kb/concepts/demo-scope.md (CC0/OFL·씬 2개 하드캡, 인테리어 주차장), kb/tasks/task-108/implementation-notes.md (무대·catalog·트윈 구조 기준선)
- **concepts_needed**: kb/concepts/art-direction.md, kb/concepts/demo-scope.md
- **related_files**: game/Assets/Art/OpenSource/** (원본 4팩 무수정 + LICENSE), game/Assets/Art/Placeholders/{Customers,FoodIcons}/** + PLACEHOLDER-PROVENANCE.md (슬라이스/리컬러 산출물), game/Assets/Scripts/Editor/{PlaceholderArtBuilder,SceneBuilder}.cs, game/Assets/Scripts/Runtime/Presentation/{SpriteCatalog,ShopPresentationController,PresentationTween}.cs, game/Assets/Scenes/Shop.unity, game/Assets/Tests/EditMode/{PlaceholderArt,ShopPresentationScene}Tests.cs
- **notes**: itch.io 3종(Ninja Adventure·karsiori·Henry)은 고정 직링크 없음 → 오너 수동 다운로드 후 game/Assets/Art/OpenSource/ 배치 전제(Kenney만 curl 직링크). CustomerSpriteEntry.walkFrames+idle 확장은 단일 sprite 하위호환 유지. 걷기 애니는 Animator 금지 — 코루틴 프레임 스왑 + localScale.x=-1 플립. 무대 개선은 순수 장식(좌석/동선/충돌 없음 — M1.5 상한). PlaceholderArtBuilder/SceneBuilder 멱등성 유지. EditMode 90종이 회귀 기준선.
- **generated_by**: design=claude opus-4-8[1m] (Codex 교대 작성, codex exec 불안정), 2026-07-10 (fallback=none)
