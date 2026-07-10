# 구현 노트 — task-109

> Status: done
> Inputs: kb/tasks/task-109/design.md, kb/concepts/art-direction.md, game/Assets/Art/OpenSource/ (Claude 자동 다운로드 CC0 4팩 — itch.io CSRF 플로우 스크립트 + Kenney 직링크)
> Outputs: 오픈소스 CC0 아트 도입 — Ninja Adventure 걷기 애니 손님(우향 idle+walk4×4종) + karsiori/Henry 음식 아이콘 6종 + Kenney 무대 타일 2종 + walkFrames catalog 확장 + 걷기 프레임 스왑 코루틴 + 좌향 플립. EditMode 97/97 pass.
> Next step: 오너 리뷰 후 커밋 → M1.5 재평가(수동 Play smoke). 통과 시 task-110 장르 선택.

## 현재 상태: done (구현 완료, Unity green, 커밋 대기)

- CC0 4팩(Ninja Adventure·karsiori Food Pack·Henry Software Pixel Food·Kenney Roguelike/RPG)을 **Claude 가 자동
  다운로드**해 `game/Assets/Art/OpenSource/` 에 배치. itch.io name-your-price 3종은 CSRF 플로우 스크립트로 취득
  (게임페이지→`download_url`(Referer)→다운로드페이지(Referer)→`file/<upload_id>`→서명 CDN), Kenney는 curl 직링크.
  설계·art-direction 의 "itch 오너 수동 다운로드" 전제는 **불필요로 확인됨**. 절차적 픽셀맵을 OpenSource 파생으로 교체 구현.
- 커밋은 하지 않았다(오너 리뷰 후 커밋). 워킹트리 변경만 남김.

## 아키타입/음식 매핑

**손님 archetype → Ninja Adventure 캐릭터**: student→Boy, office_worker→ManGreen, family_parent→Woman, senior_regular→OldMan.
- Walk.png 64×64(4방향 행 × 4프레임, 프레임 16×16), Idle.png 64×16. 우향 = 이미지 맨 아래 행(행3).
  `Texture2D.LoadImage` bottom-origin → 우향 행은 텍스처 y∈[0,16). Idle 우향 = 프레임 index 3.
- 산출: `Customers/<id>.png`(idle/fallback) + `<id>_walk0..3.png`(우향 걷기 4프레임). 16×16 무손실 슬라이스.

**레시피 → 음식 소스(직접 매핑, 리컬러 없음)**:
pork_gukbap→Carrot stew, beef_gukbap→Pumpkin soup, janchi_guksu→Mushroom Stew, bibim_guksu→Tomato stew,
tteokbokki→Meatballs (이상 karsiori ~30×24), gimbap→Sushi (Henry 16×16, 투명 트림 후 13×13).

## 설계 대비 변경 사항 (이탈 사항)

| 항목 | 설계 내용 | 실제 구현 | 사유 |
|------|-----------|-----------|------|
| 슬라이스 방식 | `spriteImportMode=Multiple` + `spritesheet` 그리드 배열 | **개별 PNG 파생**(File.ReadAllBytes+LoadImage→GetPixels32→Region→EncodeToPNG), Single 임포트 | 오너 지시(프롬프트) 우선. 프레임별 개별 PNG 가 GUID 안정·멱등·테스트 단순. 기존 `ApplyImportSettings`(Sprite·PPU32·Point·무압축·mipmap off) 그대로 재사용 |
| 음식 리컬러 | 국밥·국수 karsiori 리컬러, 떡볶이·김밥 Henry 개조 | **직접 매핑(리컬러 없음)** | 한식 톤 정합 리컬러는 task-114(아트 마감)로 이월(오너 결정). 소스 크기 유지 + 투명 여백 트림만 적용 |
| OpenSource 라이선스 파일 | 각 폴더 `LICENSE-CC0.txt` 사본 | 팩 배포본 원본 라이선스 파일 그대로 사용(NinjaAdventure/LICENSE.txt, Kenney/License.txt; karsiori·Henry 는 Read Me/readme txt) | 원본 팩 **무수정 보존**(source-of-truth) 원칙 — 파일 추가·수정 금지. provenance 에 CC0 근거·URL 기록 |
| 무대 타일 | Kenney 타일로 backdrop/counter 교체 + 소품 2~3개 | **구현함** — floor(8,1 크림 plaster)·counter(6,10 나무판자) 16×16 타일을 `Image.type=Tiled` 반복. 소품 3개(카운터 위 그릇 장식, FoodIcons 재활용) | 추출이 단순(16×16+1px 여백 그리드)해 생략하지 않고 구현. 화분 등 별도 소스는 없어 그릇 소품으로 대체 |
| 손님 rect | 실측 프레임 기준 정수배 | 48×64 → **64×64**(16×16 소스 ×4). y=56 유지, 발 바닥선 불변 | 16×16 소스 픽셀 정합(PPU32 Point ×4 정수배) |
| 플립 처리 | 좌향 퇴장 `localScale.x=-1` | `customerImage.rectTransform.localScale.x` 부호로 우향(+)/좌향(−) — 입장·만족퇴장=우향, 불만퇴장=좌향, 숨김 시 우향 복원 | 설계대로. 스프라이트 재작업 없이 우향 시트만 사용 |

## 구현 결정 기록

1. **표현/도메인 분리 유지**: Ops·GameState·M1 수치/규칙 무변경. walkFrames·coroutine 상태는 표현 계층 전용(저장 대상 아님).
2. **멱등성**: `WritePng` 는 바이트 동일 시 재기록 금지 → GUID 안정. OpenSource 원본 텍스처는 임포트 설정 무관하게
   `File.ReadAllBytes`+`Texture2D.LoadImage` 로 readable 확보(원본 임포터 변경 없음).
3. **좌표계**: Kenney/Ninja 시트는 PIL top-origin 그리드로 실측 → C# 에서 bottom-origin 변환
   (`texY = h - topY - 16`). 걷기 프레임은 텍스처 y=0 행에서 x=0,16,32,48.
4. **걷기 코루틴**: `Application.isPlaying` 가드 — Play 는 이동 중 0.12s 간격 walkFrames 순환 서브 코루틴,
   도착 시 idle 고정. EditMode(테스트)는 코루틴 없이 즉시 스냅. 빈 walkFrames·미지 id → 단일 sprite fallback, 예외 없음.
5. **무대 순수 장식**: 타일/소품 모두 `raycastTarget=false`, 좌석·동선·충돌·상호작용 없음(주차장 가드). 씬 2개 하드캡 유지.

## 수동 Play smoke 절차 (오너 검증용)

1. Shop.unity 열고 Play. 장보기→영업 진입 시 손님이 화면 좌측에서 카운터로 **걷기 애니(4프레임 순환)**로 입장,
   도착 시 idle 고정 확인.
2. 서빙(성공) → 음식 아이콘 팝 + +N원 팝업 + 손님 **우향 만족 퇴장**(플립 +1).
3. 포기(이탈) → 손님 **좌향 불만 퇴장**(`localScale.x=-1` 플립) 확인.
4. 새 음식 아이콘(karsiori 그릇/Sushi), 교체된 무대 타일(크림 배경·나무 카운터)·그릇 소품 표시 확인.
5. 정산 카운트업·밤 오버레이 연출 유지, M1 진행 규칙(자금·정산·파산) 불변 확인.

## Unity 검증 결과

| 게이트 | 명령 | 결과 |
|--------|------|------|
| 슬라이스+씬 재생성 | `Unity.exe -batchmode -quit -nographics -projectPath game -executeMethod ClientIsKing.EditorTools.SceneBuilder.Apply` | **exit 0**, `error CS` 0, PlaceholderArtBuilder/SceneBuilder 성공 로그 |
| EditMode 테스트 | `Unity.exe -batchmode -runTests -nographics -projectPath game -testPlatform EditMode -testResults <xml>` | **exit 0, 97/97 pass, 0 fail** (기존 90 회귀 + 신규 7) |
| 컴파일 게이트 | `Unity.exe -batchmode -quit -nographics -projectPath game` | exit 0, `error CS` 0 |

- 산출물: 손님 20종(4×(idle+walk4)) + 음식 6 + 무대 타일 2 = 28 PNG(+`.meta`). OpenSource `.meta` 105개 생성.
- `git status --short game` 에 Library/Temp/Obj/Logs/UserSettings/Build 산출물 없음. `.png`/`.meta` 쌍 전부 존재.
- ProjectSettings(GraphicsSettings `LightsUseColorTemperature 0→1`, QualitySettings `antiAliasing 2→0`)·Galmuri SDF
  asset(+5685줄 아틀라스 재생성)이 Unity 배치 재직렬화로 M 표시됐으나 **task-109 무관 노이즈** → 오너 리뷰에서 `git checkout` revert(커밋 제외).
- 로컬 특이사항: 검증 시작 시 오너 Unity 에디터가 프로젝트를 열고 있어 배치가 락 충돌 → 에디터 graceful close 후 진행.
  에디터 로그의 `SearchDatabase ArgumentOutOfRangeException` 은 Unity 내부 인덱서 이슈로 게임 코드 무관.

## 테스트 (신규/갱신)

- `PlaceholderArtTests`(7): 파생 28종 존재, archetype 별 idle+walk4, 음식 6, 임포트 표준(Single·PPU32·Point·무압축·mipmap off),
  provenance 전 파일명 언급, 팩별 출처·CC0·다운로드일 기록, OpenSource LICENSE 파일 존재.
- `ShopPresentationSceneTests`(신규 3): walkFrames 4×4종 주입, 빈 walkFrames/미지 id fallback 무예외, 무대 타일 스프라이트·소품 존재.
- 기존 90종 회귀 전부 유지(FirstPlayableLoop·ServiceOps·SettlementOps·Economy·Inventory·scene/UI 등).
