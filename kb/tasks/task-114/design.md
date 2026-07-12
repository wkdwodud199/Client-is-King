# 설계 문서 — task-114: 아트 마감 패스 (CC0 프로토타입 마감 폴리시)

> Status: ready
> Inputs: `kb/concepts/project-brief.md`(SSOT — 로드맵 v3 task-114 "아트 마감 패스, 폰트는 M1.5 핫픽스 선행", implement 라우팅 fable-5/medium), `kb/concepts/demo-scope.md`(하드캡 — 아트 CC0/OFL 플레이스홀더만·씬 2개·인테리어 주차장), `kb/concepts/art-direction.md`(에도풍 CC0 combo 1 디렉션 SSOT — 결정 2 "음식 ×2 정수배", 결정 3 리컬러 경로, 라이선스 운영 규칙), `kb/tasks/task-109/design.md`+`implementation-notes.md`(파생 파이프라인·"음식 직접 매핑, 한식 톤 정합 리컬러는 task-114 이월"·아키타입/음식 소스 매핑), `kb/tasks/task-110/design.md`(F1/F2 아트 디렉션·기준 팔레트·"아트 SSOT 충돌" 오픈 이슈·E2/E5 UI 원칙), `kb/concepts/development-priority.md`(§9 비주얼 우선순위·§13 판독 지표 — 이 설계가 명시적으로 입력 지정), 현재 `game/` 코드 기준선(`PlaceholderArtBuilder`/`SceneBuilder`/`ShopPresentationController`/파생 스프라이트 28종/`PLACEHOLDER-PROVENANCE.md`, task-113 완료 기준 EditMode 428·PlayMode 8), 파생 PNG 팔레트 실측(2026-07-12 — 본문 B절 표의 from 색은 전부 실측값)
> Outputs: task-114 구현 계약 — 결정론적 팔레트 스왑 파이프라인(음식 6종 한식 리컬러 + student 의상 1건 + 무대 타일 warm 보정), 음식 아이콘 32×32 캔버스 표준화, SceneBuilder 정수배 rect 정합(FoodIcon/소품/장르 아이콘)과 NightOverlay navy 정합, 자동 검증 기준과 오너/Codex 시각 승인 게이트의 분리, provenance/테스트 계약, 현대 NYC 아트 정체성 오버홀의 명시적 이관(오픈 이슈)
> Next step: Codex 설계 교차검토 후 Claude가 U1~U4 순서로 구현하고 배치 빌더→씬→컴파일→EditMode→PlayMode 게이트를 통과시킨다. 오너/Codex 640×360 원본 캡처 시각 승인 게이트를 거쳐 `task-115`(밸런싱+엔딩+Windows 빌드)로 진행한다.

## 목표 (Objective)

task-109가 도입한 CC0 파생 아트(Ninja Adventure 손님·karsiori/Henry 음식·Kenney 무대 타일)를 **현 하드캡(CC0/OFL 플레이스홀더만) 안에서 마감**한다. task-109에서 명시적으로 이월된 한식 톤 정합 리컬러(국밥 2종·국수 2종 팔레트 스왑, 떡볶이·김밥 톤 정합)를 `PlaceholderArtBuilder` 파생 단계의 **결정론적 팔레트 스왑**으로 구현하고, task-110 F2 기준 팔레트 방향(따뜻한 amber 실내 ↔ navy 야경 ↔ gochujang red 음식)으로 무대 타일·밤 오버레이·손님 의상 충돌 1건을 보정하며, 음식 아이콘의 표시 배율을 `art-direction.md` 결정 2("음식 ×2 정수배")대로 픽셀 정수배로 정합한다.

이 task는 **프로토타입 마감**이다 — 최종 아트 정체성(현대 뉴욕/코리아타운, task-110 F절)은 하드캡·SSOT 개정과 오너/Codex 승인이 선행되어야 하는 별도 결정이므로 이번 구현에 포함하지 않고 오픈 이슈로 명시 이관한다. 기존 M1~M3 루프 규칙·수치·저장 스키마·씬 2개 하드캡·EditMode 428/PlayMode 8 기준선은 불변으로 보존한다.

### 역할과 결정권

| 영역 | 결정권자 | 수행자 | 게이트 |
|------|----------|--------|--------|
| 리컬러 방향 앵커·매핑 시드·rect 정합 | 이 설계(Claude 초안) → Codex 교차검토, 오너 최종 승인 | Claude 구현 | design validator + 자동 테스트 |
| 최종 화면 톤·식별성 판정 | **오너/Codex** (Claude self-approve 금지) | Claude가 캡처 제공 | 640×360 원본+2× 시각 승인 |
| 최종 아트 정체성(현대 NYC 오버홀) | **오너 + Codex** — 이 task 범위 밖 | 별도 task | SSOT/하드캡 개정 절차 (오픈 이슈) |
| 코드 구조·테스트·배치 자동화 | 설계 계약 내 Claude | Claude | 컴파일·EditMode·PlayMode·멱등성 게이트 |
| 설계와 다른 화면 판단 | Codex 재설계 요청 | Claude가 임의 확정하지 않음 | 새 design/review 기록 |

이 문서는 Claude가 작성한 설계 초안이며 Codex 설계 교차검토를 전제로 한다. 구현 중 색·배치가 불명확하면 임의 확장하지 않고 리뷰 대상으로 남긴다.

## 범위 (Scope)

### 포함 — task-114 구현 범위

- **한식 음식 리컬러 (task-109 이월분)**: `PlaceholderArtBuilder`의 파생 단계에 아이콘별 exact-match 팔레트 스왑을 추가한다. 국밥 2종(뽀얀 돼지국밥/얼큰 소고기국밥)·국수 2종(맑은 잔치국수/비빔국수)은 karsiori 그릇 소스의 국물·건더기 색을 한국 국물 톤으로 스왑하고, 떡볶이·김밥은 gochujang red/김 흑녹 방향으로 톤 정합한다. 그릇·식기 색은 매핑에서 제외(불변)해 세트 일관성을 보존한다.
- **손님 의상 충돌 보정 1건**: student(Ninja Boy)의 의상 주색 `#D14B34`가 F2 Gochujang Red `#D34A3A`(음식 accent·primary action 예약색)와 사실상 동일해 음식/버튼과 혼동된다 — idle+걷기 4프레임 전체를 청 데님 `#3F6FA6`으로 스왑한다. 나머지 3종(office_worker 올리브·family_parent 골드·senior_regular 그레이)은 실측상 hue 분리가 충분해 **바이트 불변**으로 둔다.
- **무대·야경 F2 팔레트 정합 (하드캡 내 보정)**: floor 타일 2색을 Steam Cream 계열로, counter 타일 4색을 warm amber 나무 톤으로 스왑하고, `NightOverlay`의 RGB를 `#16202A`(Ink Navy)로 바꾼다(초기 alpha 0과 런타임 페이드 0.55 계약 불변). **신규 커스텀 아트 제작이 아니라 기존 CC0 파생물의 팔레트 스왑/보정만이다.**
- **음식 아이콘 32×32 캔버스 표준화 + 정수배 표시**: 파생 음식 아이콘을 공통 32×32 투명 캔버스에 중앙 패딩하고(트림 결과 16px 이하인 소스는 2× 정수 사전 확대 — 현재 gimbap 13×13만 해당), `SceneBuilder`의 `FoodIcon` rect를 40×32(현 1.33× 비정수 왜곡)에서 64×64(×2)로, 소품 3종을 26×26(0.87× 축소 왜곡)에서 32×32(×1)로, 장르 modal 아이콘을 20×20에서 32×32(×1)로 정합한다. `CashPopupText`를 (-40,96)→(-40,120)으로 올려 커진 음식 팝과의 겹침을 피한다.
- **결정론·멱등**: 팔레트 스왑은 순수 함수(불투명 픽셀 exact-match, 미등재 색 통과)이고, 기존 `WritePng` 바이트 비교 멱등·GUID 안정 규약을 유지한다. `Apply` 2회 연속 실행 시 파생 28종 전부 바이트 불변임을 명시 테스트로 고정한다.
- **파생 파일 경로 불변 — 신규 에셋 0**: 변경되는 것은 기존 파생 PNG 13종(student 5 + 음식 6 + 타일 2)의 **바이트뿐**이다. 파일 추가/삭제/이동이 없어 `.meta`/GUID/씬 참조가 전부 안정된다.
- **provenance 갱신**: `PLACEHOLDER-PROVENANCE.md`에 리컬러 매핑 표(from→to 전수)·캔버스 규약·사전 확대 규칙·의상 스왑·타일 보정을 기록하고, "리컬러는 미적용(task-114 이월)" 문구를 완료 기록으로 갱신한다. 매핑 표의 단일 원천은 `PlaceholderArtBuilder`의 공개 상수이며 테스트와 provenance가 이를 공유한다.
- **테스트 갱신/추가**: `PlaceholderArtTests`(매핑 적용·캔버스·색 수 상한·비대상 바이트 불변·멱등), `ShopPresentationSceneTests`(rect·`NightOverlay` 색), `SceneBuilderTests`(장르 아이콘·멱등) 갱신. 기존 EditMode 428/PlayMode 8 기준선을 무회귀 보존한다(이 설계가 명시적으로 갱신하는 기대값 제외).
- **기록**: `kb/tasks/task-114/implementation-notes.md`, `kb/artifacts/task-114-summary.md`, `python3 runtime/generate-status.py` 재생성.

### 제외 — task-114에서 구현하지 않음

- **현대 뉴욕/코리아타운 아트 정체성 오버홀** — 커스텀 아트(하드캡 위반) 또는 신규 CC0 모던셋 조달 + Codex 스타일보드 + 오너 승인 + `demo-scope.md`/`art-direction.md`/`project-brief.md` 개정이 전부 선행되어야 하는 별도 결정이다. 이번 구현에 넣지 않고 오픈 이슈로 이관한다(범위 변경 절차 — demo-scope.md).
- 신규 CC0 팩 다운로드/조달, 신규 에셋 파일 생성, 실루엣 재작업·도트 신작(떡볶이/김밥의 근접 소스 실루엣 교체 포함), 고해상 리소스.
- 폰트 변경 — Galmuri TMP Dynamic 계약은 M1.5 핫픽스로 선행 완료(로드맵 v3 명시). 이 task는 폰트 자산·설정을 건드리지 않는다.
- 사운드 도입 — development-priority §7의 CC0 효과음 3종(구매/서빙/정산) 포함 전부 제외. 도입 시점은 오너 결정(오픈 이슈).
- 밸런싱·엔딩·3일 결과 카드·Windows 빌드(task-115). 신규 씬·신규 매니저·Animator/AnimationClip·Tilemap·외부 트윈/이미지 라이브러리.
- 인테리어 시스템화(좌석·동선·충돌·상호작용 — 주차장 가드). 무대는 순수 장식 유지, 소품 개수(3종)도 늘리지 않는다.
- M1~M3 규칙·수치·저장 스키마 변경 — `GameState`/Ops/매니저 도메인 코드 무변경. `ShopPresentationController`도 무변경(오버레이 RGB는 SceneBuilder 소유, 페이드 알파 0.55 계약 유지).

## 제약 (Constraints)

- `kb/concepts/project-brief.md`가 SSOT, `kb/concepts/demo-scope.md`가 하드캡이다: **아트는 CC0/OFL 플레이스홀더만**, 씬은 `MainMenu.unity`+`Shop.unity` 2개. CC0 원본의 팔레트 스왑 개조는 허용 범위이며 개조 방법을 provenance에 기록한다(`art-direction.md` 라이선스 운영 규칙).
- `kb/concepts/art-direction.md`가 현 CC0 디렉션의 SSOT다(오너 combo 1 승인). 이 설계는 그 결정 2(정수배 표시 — "음식 ×2")와 결정 3(한식 리컬러 경로)의 이월분을 이행하는 것이지 디렉션을 바꾸는 것이 아니다. task-110 F절(현대 NYC)은 **승인 전 비실행 설계**이므로 F2 팔레트를 "방향 보정" 이상으로 적용하지 않는다.
- 원본 팩(`game/Assets/Art/OpenSource/**`)은 **무수정 보존(source-of-truth)** — 파일 추가·수정·삭제 금지. 개조는 파생물(`Assets/Art/Placeholders/**`)에만 적용한다.
- 임포트 표준 불변: Sprite · Single · PPU 32 · Point · 무압축 · mipmap off (`ApplyImportSettings` 재사용, 테스트 고정). 픽셀 표시는 정수배 원칙 — 이번에 위반 지점(FoodIcon 1.33×, 소품 0.87×, 장르 아이콘 0.67×)을 정합한다. 정수 사전 확대는 nearest-neighbor 정수배만 허용하고 축소·비정수 리샘플링은 금지한다.
- 팔레트 스왑은 **Color32 exact-match 단일 패스**(불투명 픽셀만, alpha 보존, 미등재 색 통과)로 한다. HSV 변환·색상환 회전·디더링 등 부동소수 연산 기반 변환은 결정론 훼손 위험이 있어 금지한다. 매핑 표는 `PlaceholderArtBuilder` 공개 상수가 단일 원천이다.
- 멱등: `WritePng` 바이트 동일 시 재기록 금지(GUID 안정) 유지. `SceneBuilder.Apply` 연속 2회 실행 시 중복 오브젝트·참조 파괴 없음. 파생 경로·파일 수(28종) 불변 — `AllSpritePaths()` 계약 유지.
- 표현 계층만 변경한다: `game/Assets/Scripts/Editor/{PlaceholderArtBuilder,SceneBuilder}.cs`, 파생 PNG, 씬 산출물, 테스트. `GameState`·Ops·매니저·UI 컨트롤러·저장 스키마(schemaVersion 1)는 무변경 — 세이브 호환에 영향이 없어야 한다.
- 색만으로 상태를 전달하지 않는 E5 원칙 유지 — 이번 변경은 표시 색·크기 정합이며 상태 전달 규약을 바꾸지 않는다. 640×360 Pixel Perfect·CanvasScaler 기준 해상도 불변.
- Unity 에셋과 `.meta`는 쌍이다. 이번 task는 신규 에셋이 없으므로 `.meta` 추가/삭제가 발생하면 그 자체가 이상 신호다(테스트 기준에 포함). `Library/Temp/Obj/Logs/UserSettings/Build*`는 git에 노출 금지.
- 기준선: task-113 완료 시점 EditMode 428/PlayMode 8. 이 설계가 명시적으로 갱신하는 기대값(rect·오버레이 색) 외의 회귀는 허용하지 않는다.
- 아트 마감의 최종 판정은 정량화가 불가능하므로 **자동 검증(테스트)과 시각 승인(오너/Codex)을 분리**한다(D절). 자동 검증 통과는 병합 전제 조건이고, 시각 승인은 done 게이트 조건이다.

## 구현 단계 (Implementation Steps)

### A. 플레이어 경험 — 마감이 바꾸는 화면

1. **음식이 한식으로 읽힌다**: 서빙 팝에서 돼지국밥은 뽀얀 사골 국물, 소고기국밥은 얼큰한 붉은 국물, 잔치국수는 맑은 국물에 흰 소면, 비빔국수는 진한 고추장 양념+노란 면, 떡볶이는 밝은 고추장 소스+흰 떡, 김밥은 검green 김+흰 밥으로 보인다. 당근 스튜/호박 수프/버섯 스튜/토마토 스튜/미트볼/스시의 서양·일식 톤이 사라진다.
2. **음식 팝이 커지고 픽셀이 안 깨진다**: 서빙 성공 순간 음식 아이콘이 64×64(×2 정수배)로 팝되어 도파민 순간이 강화되고(development-priority §7 "서빙 성공 3박자"), 1.33× 비정수 왜곡이 사라진다. 매출 팝업은 위로 이동해 겹치지 않는다.
3. **손님과 음식이 색으로 분리된다**: student 의상이 파란 데님이 되어 손님 4종의 의상 주색이 파랑/올리브그린/골드/그레이로 분리되고, 붉은색은 음식과 primary 버튼에만 남는다(F2 예약 이행).
4. **실내는 따뜻하고 밤은 푸르다**: 낮 무대는 Steam Cream 바닥 + warm amber 카운터, Night 진입 페이드는 Ink Navy 야경 톤 — F2 삼각 대비(amber 실내 ↔ navy 야경 ↔ gochujang red 음식)가 하드캡 안에서 성립한다.
5. **바뀌지 않는 것**: 걷기 애니·이동 동선·주문/정산 규칙·수치·저장·씬 구조는 그대로다. 이 패스는 오직 "같은 게임이 더 잘 읽히게" 만든다.

### B. 리컬러 파이프라인 계약 (PlaceholderArtBuilder)

#### B1. 파생 파이프라인 확장 (순서 고정)

```text
원본 시트 슬라이스/투명 트림 (기존)
 → [신규 1] 정수 사전 확대: 음식 아이콘 중 트림 결과 w ≤ 16 ∧ h ≤ 16 인 소스만
             nearest-neighbor ×2 (현재 gimbap 13×13 → 26×26 만 해당 — 규칙 기반, 이름 하드코딩 아님)
 → [신규 2] 팔레트 스왑: 파생 대상별 매핑 표(B2~B4)를 불투명 픽셀에 exact-match 적용
             (alpha 보존 · 미등재 색 통과 · 단일 패스 — 순수 결정론 함수)
 → [신규 3] 캔버스 패딩: 음식 아이콘만 32×32 투명 캔버스 중앙 배치
             (offset = floor((32−w)/2), floor((32−h)/2) — 잔여 픽셀은 좌/하 우선, 결정론 고정.
              w>32 ∨ h>32 이면 Region 가드와 동일하게 명시적 예외)
 → WritePng 바이트 비교 멱등 (기존) → ApplyImportSettings (기존)
```

- 손님(student)은 스왑만 적용(캔버스/확대 없음 — 16×16 유지), 무대 타일은 스왑만 적용(16×16 유지).
- 매핑 표는 `PlaceholderArtBuilder`의 **공개 정적 상수**로 노출한다(예: `public static IReadOnlyDictionary<string, (Color32 from, Color32 to)[]> PaletteMaps` — key는 파생 경로). 테스트·provenance가 같은 표를 참조한다(제2 원천 금지).
- **매핑 표의 from 색은 아래 표 전부 2026-07-12 파생 PNG 실측값**이다. to 색은 설계 시드이며 D3 시각 게이트에서 1회 보정을 허용한다(보정 시 상수·provenance·테스트 동기 갱신).

#### B2. 음식 6종 매핑 표 (from = 실측, to = 설계 시드)

그릇·식기 계열(국그릇 공통 `#501C07`/`#B3400F`/`#3B0A09`/`#2C0302`/`#8D2D04`/`#D15C2B`/`#6F2709`/`#A74116`, 접시 계열 포함)은 **매핑하지 않는다** — 원본 팩의 식기 세트 일관성을 그대로 보존한다.

| 아이콘 | 목표 톤 | 매핑 (from → to) | 유지(비매핑) |
|--------|---------|------------------|--------------|
| `pork_gukbap` (Carrot stew) | 뽀얀 돼지국밥 | `#E3C77B→#EDE3D2`(국물 밝음) · `#D1B770→#E2D6C0`(국물 중간) · `#B79661→#CBBBA0`(국물 음영) · `#D1671D→#8A5B3F`(편육) · `#E5792B→#A6714C`(고기 밝음) · `#A25119→#6E4630`(고기 음영) | 허브 green `#73AD48`/`#568931`(파 고명) |
| `beef_gukbap` (Pumpkin soup) | 얼큰 소고기국밥 | `#C14202→#C24A22`(국물 주) · `#FF6F00→#E67A3C`(기름 하이라이트) · `#C0E400→#6FA04E`(대파 밝음) · `#8EA800→#4E7A33`(대파 음영) · `#F7E0A0→#F2D9A6`(기름방울) | `#E3C77B`·`#300200` |
| `janchi_guksu` (Mushroom Stew) | 맑은 잔치국수 | `#EAD374→#F2E9CF`(맑은 국물) · `#BFAD67→#E3D8B8`(국물 중간) · `#A59244→#C9BC94`(국물 음영) · `#B9B9B9→#FBF7EB`(소면 밝음) · `#9B9B9B→#EFE7D4`(소면 중간) · `#7E7881→#CFC5AE`(소면 음영) · `#CBB660→#E8C86A`(지단) | greens `#61812F`/`#85AD48`(파) |
| `bibim_guksu` (Tomato stew) | 비빔국수 | `#C30500→#A83226`(양념 주) · `#AA110D→#9E2D1F`(양념 중간) · `#880300→#6E1C12`(양념 음영) · `#E3C77B→#E8C86A`(면 하이라이트) · `#28B3BC→#79A84C`(오이채 밝음) · `#0B696F→#4E7A33`(오이 음영) · `#174E74→#3D6626` · `#0B325D→#2F5220`(고명 정리) | greens `#598936`/`#73AD48` · `#D1671D`(당근채) · `#300200`/`#812700` |
| `tteokbokki` (Meatballs) | 밝은 떡볶이 | `#5B0503→#F2E6D8`(떡 밝음) · `#400002→#D9C7B2`(떡 음영) · `#4B0807→#E7D6C2`(떡 중간) · `#881F00→#D34A3A`(소스 주 — F2 Gochujang Red) · `#890502→#C74534`(소스 중간) · `#730907→#A93325`(소스 음영) · `#630200→#8C2A1E` · `#28B3BC→#6FA04E`(파 밝음) · `#0B696F→#4E7A33` · `#174E74→#3D6626` · `#0B325D→#2F5220` | greens `#2C6A12`/`#21550B` · `#300200`/`#812700` |
| `gimbap` (Sushi) | 김밥 | `#170625→#1B2A1A`(김 주) · `#1C1030→#223420`(김 결) · `#362939→#2C4128`(김 음영) · `#524654→#3A4F36`(김 경계) | 밥 `#DFD9C8`/`#FFFEE8`/`#B4A8A0`/`#8B7F7E` · 속재료 소색상 전부 |

- **red 3종 분화 계약**: 떡볶이(밝은 `#D34A3A`+흰 떡) > 소고기국밥(주황빛 `#C24A22`+국그릇 실루엣) > 비빔국수(진한 `#A83226`+노란 면)의 명도·구성 스펙트럼으로 분리한다. 세 아이콘의 소스/국물 주색은 서로 달라야 한다(테스트 고정).
- **tteokbokki 역할 배정 주의**: Meatballs 소스의 구체(=떡으로 전환)·바닥 소스 역할 배정은 픽셀 레이아웃 실측으로 구현 1단계에서 확정한다. 위 표의 from 색은 실측이지만 어느 색이 구체이고 어느 색이 바닥인지는 시드 가정이다 — 확정 결과가 시드와 다르면 표·상수·provenance를 함께 갱신하고 implementation-notes에 기록한다(방향 앵커 "밝은 소스+흰 떡"은 불변).

#### B3. 손님 의상 매핑 (student 5파일 — idle + walk0..3)

| 대상 | 매핑 | 근거 |
|------|------|------|
| `Customers/student.png`, `student_walk0..3.png` | `#D14B34 → #3F6FA6` (의상 주색 — 단일 쌍, 5파일 공통 적용) | 실측 ΔRGB(2,1,10)로 Gochujang Red `#D34A3A`와 사실상 동일 → 음식 accent·primary action(E2 단일 primary) 예약색과 충돌. 파랑 전환으로 4종 의상 hue가 파랑/올리브/골드/그레이로 분리 |
| `office_worker`, `family_parent`, `senior_regular` (각 5파일) | **매핑 없음 — 바이트 불변** | 실측 주색 `#A8A129`(올리브)/`#F1C471`(골드)/`#4E484A`(그레이) — hue 분리 충분, 불필요한 개조 금지 |

#### B4. 무대 타일·밤 오버레이 매핑 (F2 방향 보정)

| 대상 | 매핑 | 목표 |
|------|------|------|
| `Stage/floor.png` (2색 전수) | `#E6DABF → #F4E5C2` · `#D9CAA9 → #E4D0A6` | Steam Cream 실내 바닥 |
| `Stage/counter.png` (4색 전수) | `#B48355 → #BE8A4E` · `#C58F5C → #D3A462` · `#A07247 → #A2713F` · `#8F673F → #7C5A34` | warm amber 나무 카운터 |
| `NightOverlay` Image 색 (SceneBuilder) | `(0, 0, 0.04, 0)` → `#16202A` alpha 0 (Ink Navy) | navy 야경 페이드 — 초기 alpha 0·런타임 페이드 0.55(`ShopPresentationController.NightOverlayAlpha`, 무변경) 계약 유지 |

#### B5. 팔레트 상한 계약 (자동 검증용)

파생 스프라이트의 불투명 고유색 수 상한을 고정한다(팔레트 스왑이 색을 늘리지 않고 정리함을 보증 — 현 실측 최대: 음식 22, 손님 10, 타일 4):

| 분류 | 상한 |
|------|------|
| 음식 아이콘 6종 (각) | ≤ 24색 |
| 손님 20종 (각) | ≤ 12색 |
| 무대 타일 2종 (각) | ≤ 8색 |

### C. SceneBuilder 정수배 rect 정합 계약

캔버스 640×360 기준 좌표·크기 픽셀 고정(task-110 E3 전례). 변경은 아래 표가 전부이며 그 외 무대/HUD/패널 좌표는 불변이다.

| 오브젝트 | 현재 | 변경 | 근거 |
|----------|------|------|------|
| `FoodIcon` | (-40,78) / 40×32 | (-40,78) / **64×64** | 32×32 캔버스 ×2 — art-direction 결정 2 "음식 ×2" 이행 (현 30×24 소스가 1.33× 비정수 표시 중) |
| `CashPopupText` | (-40,96) / 180×22 | **(-40,120)** / 180×22 | 64×64 팝(내용물 상단 y≈102)과 겹침 회피. 상단 GenreBadge(y∈[135,165])와 4px 이격 유지 |
| `Prop_BowlLeft/Mid/Right` | (140/178/216, 70) / 26×26 | 동일 위치 / **32×32** | ×1 정수배 (현 0.87× 축소 왜곡 제거). 소품 간 6px 간격·SettlementPulseText(y≥90)와 비겹침 유지 |
| 장르 버튼 `Icon` | (-42,0) / 20×20 | **(-38,0) / 32×32** | ×1 정수배 (현 0.67× 축소). 110×32 버튼 내 x∈[-54,-22]로 수용, 15pt 라벨과 비겹침 |
| `CustomerSprite` | (-360,56) / 64×64 | **불변** | 16×16 ×4 — 이미 정수배 |
| `Stage_Backdrop`/`Stage_Counter` | 좌표·크기 불변 | 타일 색만 변경(B4) | Tiled 반복 규약 불변 |
| `NightOverlay` | RGB(0,0,10) alpha 0 | RGB `#16202A` alpha 0 | B4 — navy 야경 |

- `FoodIcon`·소품·장르 아이콘의 `preserveAspect = true`는 유지한다 — 32×32 캔버스와 rect가 같은 비율이므로 정수배가 성립한다.
- `ShopPresentationController`·`PresentationTween`의 코드(팝 트윈·페이드·프레임 스왑)는 무변경 — rect·색은 전부 SceneBuilder 소유다.

### D. 수용 기준 — 자동 검증과 시각 승인의 분리

#### D1. 자동 검증 (EditMode 테스트 — 병합 전제 조건)

1. 파생 28종 존재·임포트 표준(Single·PPU 32·Point·무압축·mipmap off) — 기존 검증 유지.
2. 음식 6종 텍스처가 정확히 32×32.
3. 매핑 표 적용: `PaletteMaps`의 모든 from 색이 해당 파생 PNG에 **부재**하고, 방향 앵커 to 색이 **존재**(D2 표).
4. 비대상 손님 3종(15파일)은 매핑 표가 없고 원본 슬라이스와 바이트 동일.
5. 국그릇 공통 앵커 `#501C07`·`#B3400F`가 `pork_gukbap`/`beef_gukbap`/`janchi_guksu`에 리컬러 후에도 존재(식기 불변 계약).
6. red 3종 소스/국물 주색 상호 상이: `tteokbokki`에 `#D34A3A` 존재 ∧ `beef_gukbap`에 `#C24A22` 존재 ∧ `bibim_guksu`에 `#A83226` 존재 ∧ 세 앵커가 서로 다른 파일에만 각각의 주색으로 사용.
7. 팔레트 상한(B5) 전수.
8. 멱등: `PlaceholderArtBuilder.Apply` 2회 연속 실행 후 28종 전부 바이트 불변.
9. 씬: `FoodIcon` 64×64·소품 32×32·장르 아이콘 32×32/(-38,0)·`CashPopupText` (-40,120)·`NightOverlay` RGB `#16202A`+alpha 0+raycast off.
10. provenance: 전 파생 파일명·매핑 표 언급·CC0/다운로드일(2026-07-10) 기존 검증 유지 + "리컬러" 절 존재.

#### D2. 방향 앵커 (테스트가 존재를 고정하는 to 색 — 아이콘당 대표 2개)

| 파일 | 존재해야 하는 앵커 | 부재해야 하는 from 주색 |
|------|--------------------|--------------------------|
| pork_gukbap | `#EDE3D2` · `#8A5B3F` | `#E3C77B` · `#D1671D` |
| beef_gukbap | `#C24A22` · `#E67A3C` | `#FF6F00` · `#C0E400` |
| janchi_guksu | `#F2E9CF` · `#FBF7EB` | `#EAD374` · `#9B9B9B` |
| bibim_guksu | `#A83226` · `#E8C86A` | `#C30500` · `#28B3BC` |
| tteokbokki | `#D34A3A` · `#F2E6D8` | `#5B0503` · `#28B3BC` |
| gimbap | `#1B2A1A` (밥 `#FFFEE8` 유지 확인) | `#170625` |
| student 5파일 | `#3F6FA6` | `#D14B34` |
| floor | `#F4E5C2` | `#E6DABF` |
| counter | `#BE8A4E` | `#B48355` |

#### D3. 시각 승인 게이트 (오너/Codex — done 전제 조건, Claude self-approve 금지)

640×360 원본 캡처와 2× 확대 캡처(Market 장르 modal·Service 서빙 팝 순간·Night 오버레이·무대 전경)를 제출하고 다음을 판정받는다:

1. 음식 6종이 개별·무대 팝 양쪽에서 즉시 식별된다(development-priority §13: 음식 4/6종 이상 구분 지표의 화면 전제).
2. red 3종(떡볶이/얼큰국밥/비빔국수)이 서로 혼동되지 않는다.
3. 손님 4종이 실루엣과 색으로 즉시 구분되고(§13: 손님 3/4종 지표), student가 음식/버튼의 red와 혼동되지 않는다.
4. 무대 warm 톤 ↔ Night navy 페이드의 대비가 성립하고 과하지 않다.
5. 64×64 음식 팝·(-40,120) 매출 팝업·32×32 소품/장르 아이콘이 겹침·이탈 없이 배치된다.
6. 판정 결과 팔레트 시드 보정이 필요하면 **1회에 한해** 상수·provenance·테스트를 동기 갱신해 재제출한다. 그 이상의 반복이 필요하면 마감 폴리시의 한계로 판단하고 오픈 이슈 1(NYC 오버홀)로 넘긴다.

### E. 테스트 계약 (파일별)

- `PlaceholderArtTests.cs` **갱신+신규**: 기존 7종 유지(28종 존재/idle+walk4/음식 6/임포트 표준/provenance 파일명·팩·CC0/LICENSE) + 신규 — (1) 음식 캔버스 32×32, (2) 매핑 from 부재·앵커 존재(D2 전수 — `File.ReadAllBytes`+`LoadImage` 픽셀 검사), (3) 비대상 손님 3종 매핑 없음·바이트 불변, (4) 국그릇 공통 앵커 유지, (5) red 3종 주색 상이, (6) 팔레트 상한 B5, (7) `Apply` 2회 멱등(28종 바이트 스냅샷 비교), (8) provenance 리컬러 절 존재.
- `ShopPresentationSceneTests.cs` **갱신**: `FoodIcon` sizeDelta 64×64, 소품 3종 32×32, `CashPopupText` (-40,120), `NightOverlay` RGB `#16202A`·초기 alpha 0·raycast 차단 없음(기존 테스트의 기대값 갱신), walkFrames 주입·fallback 무예외 등 기존 검증 무변경 통과.
- `SceneBuilderTests.cs` **갱신**: 장르 버튼 Icon rect 32×32/(-38,0), `SceneBuilder.Apply` 연속 2회 멱등(오브젝트 수·persistent listener) 기존 검증 통과, Build Settings 2씬.
- 기존 회귀: `FirstPlayableLoop`/`ServiceOps`/`SettlementOps`/`Economy`/`Inventory`/`GenreSelection`/`SNS`/`Event`/`Save` 계열 및 PlayMode 8종 — **어떤 파일도 수정하지 않고** 통과해야 한다(도메인 무변경의 증거).

### F. 상세 구현 순서

1. scaffold `manifest.md`가 존재하면 placeholder를 이 문서의 Inputs·영향 파일·검증 명령으로 채운다(없으면 구현 규약에 따라 생성). 코드 변경 전 현재 기준선(EditMode 428/PlayMode 8)을 실행해 실제 결과를 구현 노트에 기록한다.
2. 원본 소스에서 tteokbokki(Meatballs) 픽셀 레이아웃을 실측해 구체/바닥 소스 역할 배정을 확정하고, B2 표와 다르면 표를 확정값으로 갱신한다(방향 앵커 불변 — implementation-notes 기록).
3. `PlaceholderArtBuilder`에 공개 매핑 상수(`PaletteMaps` — B2/B3/B4 전수)와 순수 헬퍼(`SwapPalette`/`PrescaleIfSmall`/`PadToCanvas`)를 추가한다. B1 순서(슬라이스→확대→스왑→패딩→WritePng)로 파이프라인을 배선하고 기존 멱등 규약을 유지한다.
4. 배치 모드에서 `PlaceholderArtBuilder.Apply`를 실행해 파생 13종의 바이트를 갱신한다(경로·파일 수 28 불변, `.meta` 추가/삭제 0 확인).
5. `PLACEHOLDER-PROVENANCE.md`에 "task-114 리컬러/캔버스" 절을 추가한다: 매핑 표 전수(from→to·근거), 캔버스 32×32·패딩 오프셋 규칙, 사전 확대 규칙, 이월 완료 문구 갱신(기존 파일명 표·팩 출처·2026-07-10 다운로드일 기록은 보존).
6. `SceneBuilder`를 C절 표대로 갱신한다: `FoodIcon` 64×64, `CashPopupText` (-40,120), 소품 32×32, 장르 Icon 32×32/(-38,0), `NightOverlay` `#16202A`.
7. 배치 모드에서 `SceneBuilder.Apply`로 2씬을 재생성한다(연속 2회 실행으로 멱등 확인).
8. E절 테스트를 갱신/추가하고 EditMode 전체를 실행한다 — 기준선 428 + 신규가 전부 통과할 때까지 수정한다.
9. PlayMode 8종을 실행해 무회귀를 확인한다(아트 변경이 PlayMode에 영향 없음의 증거).
10. `git status --short game`으로 캐시/빌드 산출물 오염이 없고 변경이 예상 파일(파생 PNG 13 + 코드 2 + 씬 2 + provenance + 테스트 3)에 한정됨을 확인한다.
11. D3 시각 승인용 캡처 4종(원본+2×)을 준비해 오너/Codex 게이트에 제출한다. 보정 요청 시 1회 반영(상수·provenance·테스트 동기 갱신 후 8~10 재실행).
12. `kb/tasks/task-114/implementation-notes.md`(구현 결정·실측 확정값·smoke 결과), `kb/artifacts/task-114-summary.md`를 작성하고 `python3 runtime/generate-status.py`로 상태 보드를 재생성한다.

## 실행 계획 (Execution Plan)

- implement_model: claude-fable-5
- implement_effort: medium
- routing_reason: SSOT 로드맵 v3가 task-114를 fable-5/medium으로 고정 — 신규 시스템·수학·씬 없이 기존 파생 파이프라인 확장(팔레트 스왑·캔버스 표준화)과 rect 정합·테스트 갱신에 한정된 표현 마감 작업으로, 시스템 task의 3그룹 분할 없이 단일 구현 흐름으로 충분하다.

| unit | 파일 범위 | depends_on | group |
|------|-----------|------------|-------|
| U1-recolor-derivation | `game/Assets/Scripts/Editor/PlaceholderArtBuilder.cs`(매핑 상수·스왑/확대/패딩 헬퍼·파이프라인 배선), 파생 PNG 13종 바이트 갱신(`Placeholders/{Customers/student*,FoodIcons/*,Stage/*}` — 경로 불변), `game/Assets/Art/Placeholders/PLACEHOLDER-PROVENANCE.md` | 없음 | G1 |
| U2-scene-integer-alignment | `game/Assets/Scripts/Editor/SceneBuilder.cs`(C절 rect/색 표), `game/Assets/Scenes/{Shop,MainMenu}.unity` 재생성 | U1-recolor-derivation | G2 |
| U3-art-tests | `game/Assets/Tests/EditMode/{PlaceholderArt,ShopPresentationScene,SceneBuilder}Tests.cs` 갱신+신규, EditMode 428/PlayMode 8 기준선 회귀 확인 | U1-recolor-derivation, U2-scene-integer-alignment | G3 |
| U4-validation-records | 배치 빌더/씬/컴파일/EditMode/PlayMode 게이트, design/done validator, D3 시각 승인 캡처 준비, `kb/tasks/task-114/implementation-notes.md`·`kb/artifacts/task-114-summary.md`·`kb/index/status.md` | U3-art-tests | G4 |

## 파일/모듈 영향 (Affected Files/Modules)

| 파일/모듈 | 변경 유형 | 설명 |
|-----------|-----------|------|
| `game/Assets/Scripts/Editor/PlaceholderArtBuilder.cs` | modify | 공개 팔레트 매핑 상수 + exact-match 스왑·정수 사전 확대·32×32 캔버스 패딩 단계 추가, 기존 멱등/임포트 규약 유지 |
| `game/Assets/Art/Placeholders/FoodIcons/*.png` (6종) | modify | 한식 리컬러 + 32×32 캔버스 표준화 (바이트만 변경, 경로/GUID 불변) |
| `game/Assets/Art/Placeholders/Customers/student*.png` (5종) | modify | 의상 `#D14B34→#3F6FA6` 스왑 (idle+walk0..3) |
| `game/Assets/Art/Placeholders/Stage/{floor,counter}.png` (2종) | modify | Steam Cream / warm amber 팔레트 보정 |
| `game/Assets/Art/Placeholders/PLACEHOLDER-PROVENANCE.md` | modify | 리컬러 매핑 표·캔버스/확대 규약·이월 완료 기록 추가 (기존 출처·다운로드일 보존) |
| `game/Assets/Scripts/Editor/SceneBuilder.cs` | modify | `FoodIcon` 64×64·`CashPopupText` (-40,120)·소품 32×32·장르 Icon 32×32/(-38,0)·`NightOverlay` `#16202A` |
| `game/Assets/Scenes/Shop.unity` | modify | SceneBuilder 재생성 산출 (rect·오버레이 색 반영) |
| `game/Assets/Scenes/MainMenu.unity` | modify | SceneBuilder 재실행 산출 가능 — 기능 변경 없음 |
| `game/Assets/Scripts/Runtime/Presentation/ShopPresentationController.cs` | none | 변경 없음 확인 — `NightOverlayAlpha` 0.55·팝 트윈·프레임 스왑 계약 불변 |
| `game/Assets/Art/OpenSource/**` | none | 무수정 보존 확인 (source-of-truth — 파일 추가/수정/삭제 금지) |
| `game/Assets/Tests/EditMode/PlaceholderArtTests.cs` | modify | 캔버스·매핑 적용·바이트 불변·식기 앵커·red 분화·팔레트 상한·2회 멱등·provenance 리컬러 절 검증 추가 |
| `game/Assets/Tests/EditMode/ShopPresentationSceneTests.cs` | modify | rect/오버레이 색 기대값 갱신, 기존 검증 무회귀 |
| `game/Assets/Tests/EditMode/SceneBuilderTests.cs` | modify | 장르 Icon rect/좌표·멱등 재실행 검증 갱신 |
| `game/**/*.meta` | none | 신규 에셋 0 — `.meta` 추가/삭제가 없어야 함 (발생 시 이상 신호, 테스트 기준) |
| `kb/tasks/task-114/implementation-notes.md` | create | 구현 결정·tteokbokki 역할 실측 확정·시각 게이트 결과·smoke 기록 |
| `kb/artifacts/task-114-summary.md` | create | 산출물 요약 (Status/Inputs/Outputs/Next step) |
| `kb/index/status.md` | modify | `runtime/generate-status.py` 재생성 |

## 테스트 기준 (Test Criteria)

- [ ] `python -B runtime/validator/cli.py kb/tasks/task-114/design.md`가 종료 코드 0으로 통과한다.
- [ ] 구현 전 기준선(EditMode 428/PlayMode 8)을 실행하고 실제 결과를 구현 노트에 기록한다.
- [ ] `PlaceholderArtBuilder.Apply` 후 파생 스프라이트가 정확히 28종이고(`AllSpritePaths()` 계약 불변 — 신규/삭제 0), 전부 Sprite·Single·PPU 32·Point·무압축·mipmap off로 임포트된다.
- [ ] 음식 아이콘 6종의 텍스처가 정확히 32×32이고, 트림 결과 16px 이하 소스(gimbap)는 2× 정수 사전 확대가 적용됐다.
- [ ] D2 표 전수: 각 파일에서 부재 대상 from 색이 불투명 픽셀에 존재하지 않고, 방향 앵커 to 색이 존재한다(테스트는 `PaletteMaps` 공개 상수와 D2 앵커를 함께 검사).
- [ ] 비대상 손님 3종(office_worker/family_parent/senior_regular — 15파일)은 매핑 표가 없고 파생 결과가 리컬러 도입 전과 바이트 동일하다.
- [ ] 국그릇 공통 앵커 `#501C07`·`#B3400F`가 pork_gukbap/beef_gukbap/janchi_guksu에 리컬러 후에도 존재한다(식기 불변 계약).
- [ ] red 3종 분화: tteokbokki `#D34A3A`, beef_gukbap `#C24A22`, bibim_guksu `#A83226`이 각 파일에 존재하고 세 주색이 상호 다른 값이다.
- [ ] 팔레트 상한: 음식 각 ≤24색, 손님 각 ≤12색, 타일 각 ≤8색 (불투명 고유색 기준).
- [ ] 멱등: `PlaceholderArtBuilder.Apply` 2회 연속 실행 후 28종 전부 바이트 불변이고, `SceneBuilder.Apply` 2회 연속 실행이 오브젝트 수·persistent listener 기준 멱등이다.
- [ ] 씬 계약: `FoodIcon` sizeDelta 64×64/(-40,78), `CashPopupText` (-40,120), 소품 3종 32×32, 장르 버튼 Icon 32×32/(-38,0), `CustomerSprite` 64×64 불변, `NightOverlay` RGB `#16202A`·초기 alpha 0·raycastTarget false.
- [ ] `PLACEHOLDER-PROVENANCE.md`에 리컬러 매핑 표·캔버스/확대 규약이 기록되고, 기존 파생 파일명 전수·팩 출처·CC0·다운로드일(2026-07-10) 검증이 그대로 통과한다.
- [ ] `game/Assets/Art/OpenSource/**`에 어떤 변경도 없다(원본 무수정 보존 — `git status`로 확인).
- [ ] Unity 배치 compile 종료 코드 0·`error CS` 없음. EditMode 전체 종료 코드 0(기준선 428 + 신규, 이 설계가 명시 갱신한 기대값 외 무회귀). PlayMode 전체 종료 코드 0(기존 8종, `-quit` 없이 실행).
- [ ] 도메인 무변경 증거: `GameState`/Ops/매니저/UI 컨트롤러/저장 관련 소스와 테스트 파일이 diff에 나타나지 않는다.
- [ ] `git status --short game`에 `Library/Temp/Obj/Logs/UserSettings/Build*`가 없고, `.meta` 추가/삭제가 0건이며, 변경이 영향 파일 표의 목록에 한정된다.
- [ ] Build Settings에 `MainMenu.unity`·`Shop.unity` 2씬만 존재한다.
- [ ] D3 시각 승인 게이트: 640×360 원본+2× 캡처 4종(장르 modal/서빙 팝/Night/무대 전경)을 오너/Codex가 검토해 음식 6종 식별·red 3종 구분·손님 4종 색 분리·warm↔navy 대비·겹침 없음을 승인한다(Claude self-approve 금지, 보정은 1회 한정).
- [ ] 수동 Play smoke(오너): 장보기→영업→정산 1일 루프에서 리컬러 음식 팝·student 파란 의상·warm 무대·Night navy 페이드를 확인하고, 걷기 애니·주문/정산 수치·저장/이어하기가 이전과 동일하게 동작한다.
- [ ] 구현 완료 후 `python runtime/validator/cli.py --check-done task-114`와 `python runtime/generate-status.py --check`가 통과한다.

## 오픈 이슈 (Open Issues)

- **현대 NYC/코리아타운 아트 정체성 오버홀 — 별도 오너/Codex 결정으로 명시 이관 (이 task의 핵심 경계)**: 현재 아트 방향 문서는 서로 충돌한다 — `art-direction.md`(done, 오너 승인)는 에도풍 CC0 combo 1을 디렉션 SSOT로 확정했고, `task-110/design.md` F절은 최종 아트를 "현대 뉴욕의 차가운 밤 + 한식당의 따뜻한 증기"로 재정의하며 현 CC0를 "동작/가독성 prototype"으로 한정했으며(task-110 오픈 이슈 "아트 SSOT 충돌"), `demo-scope.md` 하드캡은 CC0/OFL 플레이스홀더만 허용한다. **task-114는 이 충돌을 해소하지 않는다** — 하드캡 안의 프로토타입 마감(리컬러·정합·가독성)으로 범위를 한정하고 F2 팔레트는 방향 수렴용 보정만 적용한다. 오버홀 경로는 두 가지다: (a) 커스텀/AI 생성 아트 → `demo-scope.md` 아트 하드캡 개정 + development-priority §11의 AI 생성물 정책 결정 + 오너 승인, (b) 신규 CC0 모던셋 조달 → Codex 스타일보드·asset map 설계(task-110 F3 흐름) + 오너 승인 + `art-direction.md` v2 개정. 어느 쪽이든 demo-scope의 범위 변경 절차(SSOT 선갱신 → 신규 task 설계)를 따라야 하며 이번 구현에 끼워넣지 않는다. **미결정 시 데모(task-115 빌드)는 본 마감본(에도풍 CC0 + F2 보정)으로 출시된다** — 오너가 task-115 진입 전 결정해야 한다.
- **팔레트 시드의 시각 판정 의존**: B2~B4의 to 색은 실측 from 색 위의 설계 시드다. D3 게이트에서 오너/Codex가 1회 보정을 지시할 수 있으며(상수·provenance·테스트 동기 갱신), 1회를 넘는 반복 요구는 "팔레트 스왑으로 도달 불가능한 품질"의 신호로 보고 위 오버홀 이슈로 넘긴다.
- **tteokbokki 역할 배정 실측 확정**: Meatballs 소스에서 구체(→떡)와 바닥(→소스)의 색 역할 배정은 구현 1단계 픽셀 레이아웃 실측으로 확정한다(F-2). 시드와 달라지면 표를 갱신하되 방향 앵커("밝은 고추장 소스 + 흰 떡")와 D2의 존재/부재 계약은 유지한다.
- **원본 실루엣의 한계**: bibim_guksu(Tomato stew)와 tteokbokki(Meatballs)는 같은 계열 식기 실루엣을 공유하고, gimbap은 스시 실루엣이다. 리컬러는 색 분화까지만 해결하며 실루엣 분화(그릇 형태·면발·김밥 단면)는 팔레트 스왑 범위 밖이다 — 시각 게이트에서 식별성 부족 판정이 나오면 실루엣 문제는 오버홀 이슈로 귀속시킨다(이 task에서 도트 신작 금지).
- **사운드 이월**: development-priority §7의 CC0 효과음 3종(구매/서빙 성공/정산)은 "표현 마감"에 인접하지만 이번 범위에서 제외했다(에셋 조달·오디오 배선·라이선스 기록이 별도 작업량). task-115에 포함할지, 별도 미니 task로 뺄지는 오너 결정이 필요하다.
- **CashPopupText 이동·팝 확대의 UX 판단**: (-40,120) 이동과 64×64 팝은 겹침 계산 기반의 설계 결정이지만 최종 판단은 D3 시각 게이트의 Codex 몫이다. 반려 시 rect/좌표만 롤백 가능하도록 U2를 U1(리컬러)과 독립 커밋 가능한 단위로 유지한다.
- **art-direction.md 문서 정합**: 이 task 완료로 art-direction 결정 3의 "리컬러 이월" 문구와 결정 2의 "음식 ×2" 미이행 상태가 해소된다. concept 문서 본문 갱신은 구현 노트 기록으로 갈음하고, 문서 개정 자체는 오버홀 결정과 함께 오너/Codex가 수행한다(구현 turn에서 concept SSOT를 수정하지 않는다).
