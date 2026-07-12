# Manifest — task-114

> **Load**: 기본 로드 세트의 일부. 이 task 가 *실제로* 의존하는 것만 명시해 컨텍스트를 최소화한다.
> 여기에 없는 개념/파일은 기본적으로 열지 않는다.

- **task_id**: task-114
- **inputs**: `kb/tasks/task-114/design.md`(구현 계약 SSOT — D2 팔레트 매핑 표·rect·검증), `kb/tasks/task-114/design-review-codex.md`(Codex approved), `kb/concepts/art-direction.md`(에도 CC0 디렉션·리컬러 이월), `kb/tasks/task-109/design.md`+`implementation-notes.md`(CC0 도입·PlaceholderArtBuilder 멱등 파생·직접 매핑 음식), `kb/tasks/task-110/design.md`(F 아트 디렉션·F2 팔레트), `kb/concepts/{demo-scope,project-brief,development-priority}.md`, 현재 `game/Assets/Art/{Placeholders,OpenSource}/` 자산
- **concepts_needed**: `kb/concepts/art-direction.md`(디렉션 SSOT), `kb/concepts/demo-scope.md`(아트 CC0/OFL·커스텀 금지·씬 2 하드캡), `kb/concepts/development-priority.md`(가독성 우선순위)
- **related_files**: `game/Assets/Scripts/Editor/{PlaceholderArtBuilder,SceneBuilder}.cs`; `game/Assets/Art/Placeholders/{FoodIcons/*.png,Customers/student*.png,Stage/{floor,counter}.png,PLACEHOLDER-PROVENANCE.md}`; `game/Assets/Scenes/{Shop,MainMenu}.unity`; `game/Assets/Tests/EditMode/{PlaceholderArt,ShopPresentationScene,SceneBuilder}Tests.cs`; `game/Assets/Art/OpenSource/**`(**읽기 전용·무수정 보존**)
- **notes**: **task-114 구현 계약(design.md 구현단계·실행계획·영향파일·테스트기준)만** 구현 — **아트 마감 폴리시**. 한식 리컬러: `PlaceholderArtBuilder`에 공개 `PaletteMaps` 상수 + **exact-match Color32 팔레트 스왑**(design D2 표: from=실측·to=시드)·정수 사전확대(gimbap 2×)·**32×32 캔버스 패딩**. 음식 6종·student 5파일(의상 `#D14B34→#3F6FA6`)·Stage 2종(floor Steam Cream/counter amber) 리컬러. `SceneBuilder`: FoodIcon 64×64·CashPopup(-40,120)·소품 32×32·장르 Icon 32×32·NightOverlay `#16202A`. **멱등 필수**(Apply 2회 바이트 불변·신규 에셋 0·GUID/.meta 불변·`AllSpritePaths()` 28종 유지). **`OpenSource/**` 무수정**·**도메인/Ops/저장 소스·테스트 무변경**. red 3종 분화(떡볶이 #D34A3A·얼큰 #C24A22·비빔 #A83226)·식기 앵커 불변·팔레트 상한(음식24·손님12·타일8). **기준선 EditMode 428/PlayMode 8 회귀 없이**(명시 갱신 기대값 외). provenance에 리컬러 매핑 기록. **D3 640×360 시각 승인은 오너/Codex 게이트 — Claude self-approve 금지**. 현대 NYC 오버홀 제외(오픈이슈 이관). `-runTests`에 `-quit` 금지.
