# Art Direction — 오픈소스 에셋 도입 (M1.5 피드백)

> Status: done (오너 combo 1 승인 완료 2026-07-10 — Claude 리서치+디렉션 결정 초안, codex exec 불안정으로 작성/리뷰 역할 교대·Codex 교차검토 보류)
> Inputs: Fable 리서치 (CC0 에셋 조사, 2026-07-09), kb/concepts/project-brief.md (픽셀 표준·하드캡), M1.5 플레이테스트 피드백 (자체 생성 도트 품질 불충분)
> Outputs: 에셋 조합 선정·스케일 정책·개조 경로·통합 체크리스트 — 아트 도입 구현의 전제
> Next step: 신규 task 도입 구현 (combo 1) — itch.io 3종(Ninja Adventure·karsiori·Henry)은 오너 수동 다운로드 대기, Kenney 보강분은 curl 직링크

## 배경

- M1.5 표현 미니 패스의 자체 생성 픽셀(24×32 코드 생성)로는 품질 상한이 낮다는 오너 피드백.
- 데모 하드캡: **아트는 CC0/OFL 만** (demo-scope.md). 무료 + 재배포/개조 제약 없음이 절대 조건.
- 프로젝트 표준: PPU 32, Point 필터, 무압축, 기준 해상도 640×360, Pixel Perfect Camera.
- 현 구조: UI Canvas 무대(ShopStage — Image 기반), ShopPresentationController 가 catalog(id→Sprite)로
  손님/음식 표시, 손님 이동은 anchoredPosition lerp (스프라이트 교체만으로 애니 프레임 확장 가능).

## Fable 리서치 — 본후보 (전부 CC0, 페이지 원문 검증 완료)

| 팩명 | 작가 | 확인 URL | 포함물 | 스케일 | 니즈 커버 |
|---|---|---|---|---|---|
| Ninja Adventure Asset Pack | Pixel-boy & AAA | https://pixel-boy.itch.io/ninja-adventure-asset-pack | 캐릭터 50+ (4방향 걷기 애니+페이스셋), 실내외 타일셋, 아이템 60+, UI, 음악/SFX | 16×16 | 캐릭터 ◎ / 음식 △ / 인테리어 ○ / UI ○ |
| Roguelike/RPG Pack | Kenney | https://kenney.nl/assets/roguelike-rpg-pack | 1,700+ 타일 (바닥/벽/가구/문) | 16×16 | 인테리어 ◎ |
| Roguelike Characters | Kenney | https://kenney.nl/assets/roguelike-characters | 모듈러 448 (베이스+옷+모자 조합) — 걷기 애니 없음 | 16×16 | 캐릭터 ○ (정적) |
| UI Pack — Pixel Adventure | Kenney | https://kenney.nl/assets/ui-pack-pixel-adventure | 픽셀 버튼/패널/슬라이더 | 픽셀 UI | UI ◎ |
| FREE Pixel Art — Food Pack | karsiori | https://karsiori.itch.io/free-pixel-art-food-pack | 음식 24종 + 그릇/컵 — 스튜 5종 (국물요리 리컬러 적합) | 16px급 (실측 필요) | 음식 ◎ |
| Free Pixel Food | Henry Software | https://henrysoftware.itch.io/pixel-food | 음식 64종 단일 시트 | 16×16 | 음식 ◎ (아이콘성) |
| Zelda-like tilesets and sprites | ArMM1998 | https://opengameart.org/content/zelda-like-tilesets-and-sprites | 걷기 애니 캐릭터, 실내 타일, 소품 | 16×16 | 캐릭터 △ / 인테리어 ○ |
| OPP2017 | Hapiel 외 | https://opengameart.org/content/opp2017-sprites-characters-objects-effects | NPC 걷기 사이클 다수, 아이콘 ~100 (CC0 선택 가능, DB32 팔레트) | 16px급 | 캐릭터 ○ |

- 다운로드: Kenney = 직링크 zip(로그인 불필요) / itch.io 3종 = $0 다운로드(로그인 불필요, 고정 직링크 없음) / OGA = 첨부 직링크.
- 참고 후보(CC0 아님 — 하드캡 밖, 도입 불가): LimeZu Modern Interiors(크레딧 필수·재배포 금지), CC-BY 음식/카페 팩 등.

### Fable 추천 조합

1. **아시아 무드 통일**: Ninja Adventure(캐릭터·인테리어·UI 기둥) + karsiori(국물요리 리컬러) + Henry(메뉴 아이콘)
   — 걷기 애니 완비가 결정적. 리스크: 에도풍→현대 한국 개조, 음식 팩 톤 보정, 16px 존재감(정수 스케일 필요), 식당 카운터류는 Kenney 타일 보강 가능성.
2. **Kenney 세트**: Roguelike/RPG + Roguelike Characters + UI Pack + 음식 2종
   — 톤 자체 일관성 최고·물량 최대. 리스크: 걷기 애니 전무(정적) — 타이쿤 생동감에 직접 타격.

## 디렉션 결정 (Claude 초안 → Codex 리뷰)

> 프로젝트 제약(하드캡 CC0/OFL, PPU 32 표준, UI Canvas 무대 구조, 코루틴 트윈 한정,
> 씬 2개, SceneBuilder 코드 저작)을 전제로 판단했다.

### 1. 에셋 조합 선정

- **결정**: Fable 1순위 "아시아 무드 통일" 채택 —
  **Ninja Adventure**(캐릭터 걷기 애니 + 무대 소품 기둥) + **karsiori Food Pack**(국물요리 리컬러 소스)
  + **Henry Software Pixel Food**(메뉴 아이콘 보강) + 필요분만 **Kenney Roguelike/RPG**(카운터·바닥 타일 보강).
- 근거: 경영 시뮬의 생동감 핵심인 **걷기 애니메이션을 가진 유일한 CC0 캐릭터 팩**. 아시아 무드가
  한식당 배경과 자연스럽고, 4팩 전부 CC0 원문 확인 완료.
- 기각 대안: Kenney 세트(정적 캐릭터 — 걷기 없음이 치명), OPP2017(DB32 팔레트 톤 이질).
- 리스크/완화: 에도풍→현대 한국 표현 괴리 → 데모는 archetype 4종만 필요하므로 의상 팔레트 스왑
  최소 개조로 흡수. 음식 팩과 톤 차이 → 도입 시 팔레트 보정 1회.

### 2. 스케일/임포트 정책

- **결정**: 소스 16px 원본 유지 + **PPU 32 임포트 표준 유지** + 무대(UI Image) rect 를 **정수배**로 표시
  (실측 후 캐릭터 ×3=48px급 or ×4, 음식 ×2). 임포트 설정은 기존 파이프라인
  (Sprite·PPU 32·Point·무압축·mipmap off — PlaceholderArtBuilder.ApplyImportSettings 패턴) 재사용.
- 근거: 현 무대는 UI Canvas(Image) 기반이라 표시 크기는 rect 가 결정 — PPU 변경 없이 정수배 rect 로
  픽셀 정합 확보. 브리프의 PPU 32 표준 불변.
- 기각 대안: PPU 16 전환 — 브리프 표준 위반 + 월드 스프라이트 도입 시(후속) 혼선.
- 리스크: 팩별 프레임 크기 상이(16×16/16×24 혼재 가능) → 도입 1단계에서 실측하고 슬라이스 규약을
  provenance 에 문서화.

### 3. 한식 표현 개조 경로

- **결정**: 국밥 2종·국수 2종 = **karsiori 스튜/그릇 리컬러**. 떡볶이·김밥 = Henry 시트에서 근접 소스
  탐색 후 개조, 근접 소스가 없으면 **현 v2 픽셀맵 유지**(이미 형태 인식 가능).
- 저장/재현 규약: 원본 팩은 `game/Assets/Art/OpenSource/<팩명>/` (LICENSE 사본 포함, 무수정 보존),
  개조본은 기존 catalog 경로(`Assets/Art/Placeholders/FoodIcons/`)에 교체 커밋. 개조 방법(소스 파일·
  팔레트 매핑)은 PLACEHOLDER-PROVENANCE.md 에 기록.
- 기각 대안: 전 음식 자작 유지 — 품질 상한이 낮다는 오너 피드백이 출발점이므로 배제.
- 리스크: 리컬러 결과 톤 불일치 → 팔레트 보정 1회 반복 허용 (그 이상은 task-114 아트 마감 패스로 이월).

### 4. 손님 애니메이션 적용 방식

- **결정**: **옆걷기(우향) 1방향 × 3~4프레임 스왑**을 기존 lerp 이동과 결합.
  - catalog 확장: `CustomerSpriteEntry` 에 `Sprite[] walkFrames`(+idle 1프레임) 추가.
  - ShopPresentationController 의 MoveAnchored 이동 코루틴 동안 **0.12s 간격 프레임 순환** 서브 코루틴,
    도착 시 idle 프레임 고정. 좌향 퇴장은 `localScale.x = -1` 플립(스프라이트 재작업 없음).
  - 4방향 시트 중 우향 행만 사용 (탑다운 이동 없음 — 무대는 수평 동선).
- 기각 대안: Animator/AnimationClip — 코드 저작 규약(SceneBuilder)과 충돌하고 씬 직렬화 복잡도만 증가.
- 리스크: 팩의 시트 행 구성(방향 순서) 상이 → 슬라이스 규약에서 행 인덱스 명시로 흡수.

### 5. 인테리어/무대 개선 범위 (M1.5 상한)

- **결정**: ① Stage_Backdrop → 벽/바닥 타일 2~3종 반복 패턴 이미지, ② Stage_Counter → 카운터
  스프라이트, ③ 장식 소품 2~3개(테이블·화분 등) 배치. **여기까지만** — 좌석/동선/충돌 없음(순수 장식).
- 근거: M1.5 목적은 "재미 평가 가능한 최소 표현" — 인테리어 시스템화는 주차장(인테리어 기능) 침범 위험.
- 기각 대안: 타일맵(Tilemap) 도입 — 씬 구조 변경 과투자, UI Image 반복으로 충분.
- 리스크: 없음 (범위 상한 명시로 통제).

### 6. 통합 체크리스트 (구현 작업 단위, 순서)

1. **다운로드**: Kenney 팩 = curl 직링크. itch.io 3종 = 고정 직링크 없음 → 오너 수동 다운로드 개입
   가능성 (다운로드 파일을 지정 폴더에 두면 이후 단계는 자동).
2. **라이선스 문서화**: `game/Assets/Art/OpenSource/<팩>/LICENSE-CC0.txt` 사본 + provenance 에
   출처 URL·버전·다운로드일 기록.
3. **시트 실측·슬라이스**: 프레임 크기/행 구성 실측 → Sprite Editor 그리드 슬라이스(코드:
   TextureImporter spritesheet 설정) — archetype 4종 × (idle 1 + walk 3~4) + 음식 소스 선별.
4. **한식 리컬러**: 3번 산출물 기반 팔레트 스왑 (빌더 확장 or 1회 수작업 + provenance 기록).
5. **catalog/코드 확장**: CustomerSpriteEntry.walkFrames + SceneBuilder 주입 갱신.
6. **걷기 연출**: ShopPresentationController 프레임 스왑 코루틴 + 좌향 플립.
7. **무대 교체**: Backdrop 타일 패턴·카운터·소품 (SceneBuilder).
8. **테스트 갱신**: 아트 존재/임포트/frames 검증 + 기존 90종 회귀.
9. **배치 3종 → Play 확인 → 커밋** (M1.5 게이트 재평가).

## 라이선스 운영 규칙

- 도입 에셋마다 `game/Assets/Art/<팩명>/LICENSE-*.txt` (원문 사본) + PLACEHOLDER-PROVENANCE.md 항목 갱신.
- CC0 여도 출처·버전·다운로드일을 기록한다 (재현성).
- 개조본(리컬러 등)은 원본 경로와 개조 방법을 provenance 에 명시.
