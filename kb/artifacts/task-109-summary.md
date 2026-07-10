# 산출물 요약 — task-109

> Status: done
> Inputs: kb/tasks/task-109/implementation-notes.md
> Outputs: 이 요약 문서 — CC0 오픈소스 아트 도입(손님 걷기 애니 + 음식/무대 스프라이트) 완료 요약과 M1.5 재평가 인계
> Next step: **오너 리뷰 후 커밋 → M1.5 재평가(수동 Play smoke)**. 통과 시 task-110 장르 선택.

## 작업 요약

- **Task ID**: task-109
- **제목**: 아트 도입 패스 — CC0 오픈소스 손님 걷기 애니 + 음식/무대 스프라이트 (M1.5 품질 상한)
- **완료일**: 2026-07-10 (커밋 대기 — 오너 리뷰 후)

## 산출물 목록

| 산출물 | 경로 | 설명 |
|--------|------|------|
| OpenSource 원본 4팩 | `game/Assets/Art/OpenSource/{NinjaAdventure,karsiori-FoodPack,HenrySoftware-PixelFood,Kenney-RoguelikeRPG}/` | CC0 무수정 보존(source-of-truth) + `.meta` 105 |
| 손님 스프라이트 20종 | `game/Assets/Art/Placeholders/Customers/` | archetype 4종 × (우향 idle 1 + 우향 걷기 4프레임), 16×16, Ninja Adventure 파생 |
| 음식 아이콘 6종 | `game/Assets/Art/Placeholders/FoodIcons/` | karsiori 그릇 5 + Henry Sushi 1 직접 매핑(리컬러는 task-114 이월) |
| 무대 타일 2종 | `game/Assets/Art/Placeholders/Stage/` | Kenney floor(크림)·counter(나무판자) 16×16, Image Tiled 반복 |
| 아트 빌더 | `game/.../Editor/PlaceholderArtBuilder.cs` | OpenSource PNG→GetPixels32→Region 슬라이스→EncodeToPNG, 멱등, 임포트 표준 고정 |
| catalog 확장 | `game/.../Presentation/SpriteCatalog.cs` | `CustomerSpriteEntry.walkFrames`(단일 sprite 하위호환) |
| 걷기 연출 | `game/.../Presentation/ShopPresentationController.cs` | 이동 중 0.12s 프레임 스왑 코루틴 + 좌향/우향 localScale 플립 |
| 씬 무대 교체 | `game/.../Editor/SceneBuilder.cs` + `Shop.unity` | 타일 backdrop/counter + 그릇 소품 3 + walkFrames 주입 + 손님 rect 64×64 |
| 테스트 +10 | `game/Assets/Tests/EditMode/{PlaceholderArt,ShopPresentationScene}Tests.cs` | **총 97/97 통과** (기존 90 회귀 + 신규 7) |
| task 기록 | `kb/tasks/task-109/`, 이 요약 | provenance·구현 노트·요약 |

## 주요 결정 / 이탈

- **개별 PNG 파생**(spriteImportMode=Multiple 대신) — 프레임별 PNG 로 GUID 안정·멱등·테스트 단순(오너 지시).
- **음식 리컬러 생략** — 직접 매핑, 한식 톤 정합은 task-114(아트 마감)로 이월.
- **OpenSource 무수정 보존** — 팩 원본 라이선스 파일 그대로(LICENSE-CC0.txt 사본 미추가), provenance 에 CC0 근거 기록.
- **무대 타일 구현** — 추출 단순해 생략하지 않음. 순수 장식(raycastTarget=false, 좌석/동선/충돌 없음, 주차장 가드).
- **좌향 플립** — 불만 퇴장 `localScale.x=-1`, 입장/만족 퇴장 +1, 우향 시트만 사용(스프라이트 재작업 없음).

## 검증

- SceneBuilder.Apply exit 0 · 컴파일 게이트 exit 0 (error CS 0) · **EditMode 97/97** · 멱등 재실행 바이트 불변 · 캐시 누출 0건.
- M1 루프 규칙/수치·GameState·씬 2개 하드캡 불변.

## 관련 문서

- 설계: `kb/tasks/task-109/design.md`
- 구현 노트: `kb/tasks/task-109/implementation-notes.md` (수동 Play smoke 절차 + Unity 검증 표 포함)
- provenance: `game/Assets/Art/Placeholders/PLACEHOLDER-PROVENANCE.md`
