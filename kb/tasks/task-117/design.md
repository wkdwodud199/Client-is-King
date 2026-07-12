# 설계 문서 — task-117: 인게임 크레딧 UI (릴리스 필수 표기)

> Status: ready
> Inputs: `kb/tasks/task-116/design.md` C절(AI 아트 공개·라이선스 정책 확정본 — 공개문 KO/EN, 표시 위치에 "인게임 Credits UI = 공개 출시 전 필수 별도 release task" 명시), `LICENSE`(루트 MIT — "Copyright (c) 2026 P0t4t0"), `game/Assets/Art/Fonts/Galmuri-LICENSE.txt`(Galmuri11 — SIL OFL 1.1, © 2019–2025 Lee Minseo), `game/Assets/Art/Placeholders/PLACEHOLDER-PROVENANCE.md`(CC0 팩 4종 출처 표), `kb/concepts/art-originals/PROVENANCE.md`(AI 콘셉트 원본 계보), `README.md`(코드 MIT), `game/Assets/Scripts/Runtime/UI/{EndingOverlayController,MainMenuController}.cs`(오버레이/버튼 미러 패턴), `game/Assets/Scripts/Editor/SceneBuilder.cs`(BuildMainMenu·BuildEndingOverlay·CreateButton·팔레트 상수), `game/Assets/Tests/EditMode/{EndingOverlaySceneTests,SceneBuilderTests,MainMenuSaveFlowTests}.cs` + `game/Assets/Tests/PlayMode/EndingPlayModeTests.cs`(테스트 전례), 현 기준선 EditMode 494 / PlayMode 9, **Codex 독립 재검토 P0/P1 반영(2026-07-13)**
> Outputs: task-117 구현 계약 — 크레딧 표시 문구 전문(A절), 문구 단일 원천 전략(B절), AI 고지 표시 시점 권고(C절), MainMenu 버튼·패널 좌표 계약(D절), 컨트롤러 모달 입력 격리 계약(E절), 라이선스·릴리스 패키지(F절), 테스트 계약 — EditMode + PlayMode 신규 1(G절), 오너/Codex 게이트 라우팅(오픈 이슈)
> Next step: Claude가 U1~U5 순으로 구현한다(task-116 U3와 병렬 금지 — 제약 참조). done 전제 게이트 3종(오너 문구·저작자명 승인 + 엔진 표기 방식 확정 + 640×360 시각 승인 — Claude self-approve 금지)을 통과한 뒤 Codex 코드리뷰로 마감한다. 완료 범위는 source-ready이며 출시는 F절 릴리스 게이트(재빌드 필수)를 따른다.

## 목표 (Objective)

공개 출시 전 필수 표기(task-116 C절 정책의 인게임 표시 위치)를 이행하는 **MainMenu 크레딧 버튼 → 크레딧 패널(모달 오버레이)** 을 추가한다. 패널은 게임 제작 주체·엔진·폰트(Galmuri11, SIL OFL 1.1)·플레이스홀더 아트(CC0 팩 4종)·AI 보조 아트 고지(C절 확정 공개문 verbatim)·라이선스 요약(코드 MIT / 고유 아트 별도)을 정적 텍스트로 표시하며, UI + 텍스트 외에는 아무것도 바꾸지 않는다.

## 범위 (Scope)

이 작업에 포함되는 것과 포함되지 않는 것:

- 포함:
  - `CreditsCopy.cs` 신설 — 크레딧 표시 문구의 단일 C# 원천(A절 전문, B절 전략).
  - `CreditsController.cs` 신설 — 열기/닫기 토글 + focus + **모달 입력 격리(배경 버튼 interactable 저장/잠금/복원, Cancel 닫기)** 컨트롤러(E절 — EndingOverlay 미러, 도메인 무접촉).
  - `SceneBuilder.BuildMainMenu` 확장 + `BuildCreditsPanel` 신설 — `CreditsButton`(MainMenu 우상단) + `Panel_Credits`(640×360 모달, 초기 비활성, 최상단 sibling, CloseButton Navigation None) 저작, `MainMenu.unity` 재생성.
  - 라이선스 동봉(F절): `game/Assets/StreamingAssets/Licenses/`에 **OFL 전문 + 루트 MIT LICENSE** 바이트 동일 사본(오너가 이슈 2 (a)안 채택 시 THIRD-PARTY-NOTICES 포함).
  - 신규 `CreditsPanelSceneTests.cs`(EditMode) + **`CreditsPlayModeTests.cs`(PlayMode 신규 1건 — focus/입력 격리)** + `SceneBuilderTests.cs` 추가 테스트(기존 기대값 무변경 — 추가만).
  - 기록: `implementation-notes.md`, `manifest.md`, `kb/artifacts/task-117-summary.md`, `kb/index/status.md` 재생성.
- 제외:
  - 아트 에셋 일체(신규 스프라이트·아이콘·이미지 없음 — 텍스트와 기존 팔레트 단색 dim만 사용).
  - 도메인·수치·저장 스키마·엔딩·결정론 변경 전부. 씬 추가(2-씬 하드캡 내 — 크레딧은 `MainMenu.unity` 안의 패널), Shop 씬 진입점, 신규 매니저, 외부 라이브러리(신규 InputSystem 도입 포함 — Cancel은 기존 legacy Input 사용), ScrollRect/페이징, Animator.
  - `MainMenuController.cs` 수정 — 크레딧 버튼·패널·입력 격리는 전부 별도 `CreditsController`가 소유한다(세이브 플로우 코드 무접촉으로 회귀면 최소화). 버튼 네비게이션 연결은 SceneBuilder 소유.
  - EN 인게임 크레딧 — 데모는 KO 단일 언어이며 EN 공개문은 README/AI-ART-NOTICE.txt(task-116 소유)가 담당한다. 저장소 URL·문의처 표기(오픈 이슈 7, v2 후보).
  - task-116 산출물 선점 금지 — `README.md`/`README.en.md` 공개문·라이선스 범위 반영, `AI-ART-NOTICE.txt`, `NYC-ART-PROVENANCE.md`는 task-116 U2 소유. 이 task는 그 문구를 **참조(상수 복제 + 동기화 테스트)** 만 하며, README 반영은 F절의 완료/릴리스 선행조건으로 의존만 기록한다.
  - **Windows 재빌드/배포** — task-117 완료 범위는 **source-ready**까지다. 출시용 빌드는 F절 릴리스 게이트(task-116+117 합류 후 재빌드)가 소유한다.

## 제약 (Constraints)

- **UI + 텍스트 전용**: 도메인/저장/밸런스/엔딩/결정론 소스·테스트가 diff에 나타나지 않아야 한다(무변경 증거). 변경은 표시 계층(SceneBuilder·Runtime/UI·씬·테스트)에 한정.
- **법적 문구 정확성 = 오너 민감**: AI 보조 아트 고지는 task-116 C절 오너 확정 공개문 [KO]를 **한 글자도 바꾸지 않고(verbatim)** 사용한다 — 줄바꿈도 TMP 자동 wrap에 맡기고 문자열에 개행을 삽입하지 않는다. 라이선스 요약도 C절 정책 문장을 그대로 인용한다(A절). 그 외 신규 표기(제작 저작자명·엔진 표기)는 **잠정**이며 done 전 오너 게이트(오픈 이슈 1·2)를 통과해야 한다. Claude가 새 법적 문구를 창작·수정·self-approve하는 것은 금지(task-114/115의 자동 검증 vs 주관 승인 분리 전례).
- **CC0 표기 금지 규칙 승계**(task-116 C절): AI 보조 아트를 CC0로 표기하지 않으며, 재사용·재배포 허가 문구를 넣지 않는다.
- **모달 입력 격리**: 크레딧 패널이 열려 있는 동안 pointer(dim raycast)·키보드·게임패드 어느 경로로도 배경 UI(게임 시작/이어하기/크레딧 버튼)가 조작되거나 focus가 탈출할 수 없어야 한다(E절 계약 + G절 PlayMode 검증).
- **패턴 미러**: 오버레이 = `Panel_Ending` 전례(640×360 dim, raycastTarget true 모달, 초기 비활성, 최상단 sibling, Canvas 탑재 컨트롤러, EditorInit 주입, OnEnable/OnDisable 리스너 쌍, persistent listener 0). 버튼 = `CreateButton` 헬퍼 + `LinkVerticalNavigation`. 신규 UI 프레임워크·매니저 금지.
- **픽셀/폰트 표준 불변**: 캔버스 기준 해상도 640×360, Galmuri11 TMP Dynamic(기본 폰트), 기존 팔레트 hex(Ink Navy `#16202A` · Steam Cream `#F4E5C2` · Brass Amber `#E5A84B`)만 사용. 크레딧 전체 문자는 Galmuri11이 커버해야 하며(G절 글리프 테스트 — 원본 폰트 에셋 비파괴), 커버 불가 특수문자는 대체한다(`©`→`(C)`, `–`→`-` — 법적 등가 표기) — 단 AI 공개문은 verbatim 원칙이 우선하며 해당 문구는 KO/기본 라틴·문장부호만 포함하므로 충돌 없음.
- **기준선**: EditMode 기존 494 무회귀 + 신규 크레딧 테스트 추가. PlayMode 기존 9 무회귀 + **신규 1건(G절) 추가 = 총 10**. 기존 테스트 파일의 기대값 변경 0(신규 오브젝트 검증은 추가만). `SceneBuilder.Apply` 멱등(연속 2회 오브젝트 수·persistent listener 동일). Shop.unity는 재생성 산출이 무변경이어야 한다(변경 발생 = 이상 신호).
- **task-116 U3와 병렬 구현 금지**: 두 task가 `SceneBuilder.cs`·`MainMenu.unity`·`SceneBuilderTests.cs`·`kb/index/status.md`에서 겹친다. **task-117을 단독 완료한 뒤 task-116 U3(씬 전환)를 진행**하고, U3는 재생성 시 최신 Credits UI를 보존해야 한다(순서 고정 — 오픈 이슈 4에 릴리스 순서와 함께 기록).
- **사용자 소유 파일 staging 금지**: `UPDATING.md`, `kb/concepts/art-references/**`, `kb/concepts/development-priority.md`는 사용자 소유다 — 이 task의 어떤 커밋에도 포함하지 않는다.
- **2-씬 하드캡**: `MainMenu` + `Shop` 2개 유지 — 크레딧은 MainMenu 내 패널.
- **`.meta` 쌍 규칙**: 신규 파일(스크립트·StreamingAssets 폴더/파일)마다 `.meta` 동반 커밋, 삭제 0건.
- **시각 승인 게이트**: 640×360 크레딧 화면(원본 + 2× 캡처)은 오너/Codex가 판정한다(task-114 D3 전례). Claude self-approve 금지.
- **출시 게이트**: 기존 task-115 Windows 빌드는 크레딧·라이선스 동봉이 없으므로 **출시 금지**. 공개 출시는 task-116+117 합류 후 재빌드(F절)로만 한다.
- 리포 public 유의 — 커밋 = 공개 게시. 문구는 확정 정책과 이 계약 범위만 사용한다.

## 구현 단계 (Implementation Steps)

### A. 크레딧 콘텐츠 계약 — 표시 문구 전문 (KO)

패널 본문은 2단 정적 텍스트다(레이아웃 근거는 D절). 섹션 헤더는 rich text로 Brass Amber(`<color=#E5A84B>`)를 쓴다(NightPanel rich text 색 태그 전례). 아래 전문이 계약이며, AI 공개문·라이선스 요약은 task-116 C절 확정 문장 verbatim, **제작 저작자명·엔진 줄은 잠정(오픈 이슈 1·2 확정 대기)**, 나머지는 오너 승인 게이트(오픈 이슈 1) 대상 초안이다.

**좌측 컬럼 (`CreditsCopy.LeftColumn`)** — 수동 `\n` 줄바꿈, 각 줄 폭 ≤300px(G절 테스트):

```
<color=#E5A84B>제작</color>
Client is King — © 2026 Client is King Project

<color=#E5A84B>엔진</color>
Unity 6 (6000.3.8f1)

<color=#E5A84B>폰트</color>
Galmuri11 — SIL Open Font License 1.1
© 2019–2025 Lee Minseo (quiple@quiple.dev)

<color=#E5A84B>플레이스홀더 아트 — CC0 1.0</color>
Ninja Adventure Asset Pack — Pixel-Boy / AAA
Pixel Art Food Pack — karsiori
Free Pixel Food — Henry Software
Roguelike/RPG pack — Kenney (kenney.nl)
```

- 제작 줄의 저작자명은 잠정이다 — 루트 `LICENSE`는 `Copyright (c) 2026 P0t4t0`인데 크레딧 초안은 "Client is King Project"로 표기가 갈린다. **임의 확정 금지, 오너 게이트(오픈 이슈 1)** 로 라우팅한다.
- 엔진 줄은 잠정이다 — `© Unity Technologies` 같은 권리 표기·상표 사용 문구를 넣지 않고 사실 서술만 남겼다. 최종 방식(THIRD-PARTY-NOTICES 귀속·비제휴 고지 vs 엔진 표기 제거)은 **오너 게이트(오픈 이슈 2)**.

**우측 컬럼 (`CreditsCopy.RightColumn`)** — AI 공개문·라이선스 문장은 자동 wrap(개행 미삽입), 전체 높이 ≤240px(G절 테스트):

```
<color=#E5A84B>AI 보조 아트</color>
{CreditsCopy.AiArtNoticeKo — 아래 인용은 이 문서의 표시용 줄바꿈이 포함된 참고 사본이다.
구현 시 원문은 반드시 `kb/tasks/task-116/design.md` C절 "공개문 [KO]" 행에서 복사한다
(개행 없는 단일 문자열 — G절 동기화 테스트 ③이 verbatim을 검증한다):
"Client is King의 NYC 코리아타운 배경, 캐릭터, 음식 및 UI 아이콘 일부는 프로젝트
오너가 승인한 비주얼 콘셉트를 바탕으로 OpenAI의 Codex 내 이미지 생성 도구를
사용해 사전 생성하고, 프로젝트 팀이 방향을 정하고 선택·검수·통합한 AI 보조
아트입니다. 정확한 백엔드 모델 식별자는 도구에서 노출되지 않아 추정하여
표기하지 않습니다. 게임 실행 중에는 생성형 AI 또는 외부 AI 서비스를 사용하지
않습니다."}

<color=#E5A84B>라이선스</color>
코드: MIT License
프로젝트 고유 아트(AI 보조 아트 포함): MIT 적용 제외, CC0 아님.
프로젝트 오너는 별도의 재사용·재배포·2차 저작물 작성 허가를
부여하지 않습니다. AI 요소의 저작권 성립과 보호 범위는
관할권에 따라 달라질 수 있습니다.
```

콘텐츠 결정 기록:
- **라이선스 요약 = C절 정책 문장 직접 인용**: "프로젝트 오너는 별도의 재사용·재배포·2차 저작물 작성 허가를 부여하지 않습니다. AI 요소의 저작권 성립과 보호 범위는 관할권에 따라 달라질 수 있습니다."는 task-116 C절 라이선스/권리 정책과 동일 의미의 확정 표현이다(초안의 "재사용·재배포 불허" 축약은 정책 뉘앙스 손실로 기각 — Codex P0-2). "각 원본 팩의 CC0 1.0" 중복 행은 좌측 컬럼 헤더가 이미 전달하므로 제거했다.
- **CC0 팩 크레딧 유지**: CC0는 표기 의무가 없지만 4팩 전부 명기한다(선의 표기 + 데모 출시본이 CC0 세트 기반). task-116 U3(NYC 전환) 이후에도 목록을 유지한다 — 조건부 표시 로직을 만들지 않는 것이 표시 전용 원칙에 부합하고, CC0 과잉 표기는 무해하다. 오너가 출시 시점에 축소를 원하면 문구 1곳(`CreditsCopy`) 수정으로 끝난다(오픈 이슈 3과 함께 판단).
- 폰트 저작권 표기는 `Galmuri-LICENSE.txt` 1행(Copyright © 2019–2025 Lee Minseo (quiple@quiple.dev))을 그대로 옮긴다. OFL 조건 2의 "라이선스 동봉"은 F절이 담당한다.

### B. 문구 단일 원천 전략 — C# 상수 + 동기화 테스트 (복제 + 기계 검증 채택)

**결정: 표시 문구는 `game/Assets/Scripts/Runtime/UI/CreditsCopy.cs`(static class, `ClientIsKing.UI`)의 public 상수 3종(`AiArtNoticeKo`, `LeftColumn`, `RightColumn`)을 단일 C# 원천으로 두고, 원 문서와의 정합은 EditMode 동기화 테스트가 고정한다.**

- 런타임 단일 소스(원 문서 직접 로드)는 기각 — `kb/`는 빌드에 포함되지 않고, `.md`는 Unity TextAsset 확장자가 아니며, provenance 문서는 개발 문서라 표시 카피로 부적합하다.
- 검증 없는 복제는 기각 — 라이선스 표기가 원 문서와 드리프트해도 감지 수단이 없다.
- 채택안의 동기화 테스트(G절): ① `AiArtNoticeKo`가 `kb/tasks/task-116/design.md`에 부분 문자열로 존재(C절 확정문 verbatim 증명 — EditMode 테스트의 리포 상대 경로 파일 읽기는 task-116 H8의 `kb/concepts/` md5 검증 계약과 동일 패턴), ② 폰트 저작권자 표기가 `Galmuri-LICENSE.txt`에 존재, ③ CC0 팩 4종 표기가 `PLACEHOLDER-PROVENANCE.md` 도입 팩 표에 존재, ④ 씬에 구운 텍스트 == `CreditsCopy` 상수(빌드 산출 동기화), ⑤ 라이선스 요약 문장이 task-116 design.md C절에 존재. task-116 U2가 `AI-ART-NOTICE.txt`를 만들면 같은 문구의 제3 사본이 생기지만, 그 파일의 정합은 task-116 테스트 소유다(이 task는 의존하지 않는다 — 조건부 테스트 금지).
- 텍스트는 **SceneBuilder가 빌드 타임에 씬에 굽는다**(정적 라벨 전례 — Title·버튼 라벨과 동일). 런타임 Render가 없으므로 컨트롤러는 토글·입력 격리만 소유한다(E절). 씬 드리프트는 ④가 잡는다.

### C. AI 보조 아트 고지 표시 시점 — 권고: 지금부터 정적 표시 (오너 게이트)

NYC AI 아트는 현 빌드에 없다(task-116 U2 대기). 선택지는 "지금부터 표시" vs "U2 합류 후 표시(게이트)"이며, **권고는 지금부터 정적 표시**다:

1. 정책은 이미 오너 확정(2026-07-12)이고, AI 콘셉트 원본은 public 리포에 이미 게시되어 있다 — 고지가 아트보다 앞서는 것은 과잉 고지 방향의 어긋남(정직 원칙에 무해)이다.
2. 이 task 자체가 "공개 출시 전 필수"이고, **출시 가능한 빌드는 F절 릴리스 게이트상 task-116+117 합류 재빌드뿐**이다 — 출시본에서 문구와 빌드 내용이 항상 일치한다(task-115 빌드 출시 금지가 이를 보강).
3. 조건부 표시는 상태 로직·테스트 분기를 낳아 표시 전용 원칙(B절)을 훼손한다.

리스크: NYC 아트가 없는 중간 소스 상태에서는 "…일부는 AI 보조 아트입니다"가 빌드 내용을 앞서간다 — 단 중간 상태는 출시가 금지되어 있어 노출 경로가 없다. 최종 판단은 오너에게 라우팅한다(오픈 이슈 3 — done 비차단).

### D. UI 구조 — MainMenu 크레딧 버튼 + 640×360 모달 패널

**패널 형식 결정: 단일 패널 · 2단 정적 텍스트(좌/우 300px 컬럼).** A절 전문은 단일 컬럼(460px) 10pt로는 약 22행 ≈ 300px+가 필요해 640×360에서 본문 가용 높이를 초과한다. 페이징(컨트롤러 상태 + 페이지 로직 + 테스트 분기)과 ScrollRect(프로젝트 미사용 패턴, 픽셀 폰트와 부정합)를 기각하고, 무상태로 전량이 들어가는 2단 배치를 채택한다 — 컬럼당 최대 ~18행 ≈ 234px로 240px 컬럼에 수납된다(가독성 판정은 시각 게이트 몫; 실측이 240 초과 시 폰트 10→9pt 축소로 맞춘다 — D절 표 개정 허용, 문구 무변경).

MainMenu 기존 레이아웃(전부 중앙 앵커, x∈[-320,320]·y∈[-180,180]): Title (0,60) 520×80 → y 20..100 / StartButton (0,-50) 200×44 → y -72..-28 / ContinueButton (0,-104) 200×44 → y -126..-82 / SaveStatusText (0,-146) 420×24 → y -158..-134. **우상단 y 100 위·x 180 밖이 비어 있으므로** 크레딧 버튼은 Shop `AdvanceButton (250,150)` 좌표 관례를 미러해 우상단에 둔다 — 기존 4요소 rect와 전부 비겹침(버튼 rect x 190..310 · y 134..166 vs Title 최대 y 100).

| 오브젝트 | anchoredPosition | sizeDelta | 폰트/색 | 비고 |
|----------|------------------|-----------|---------|------|
| `CreditsButton` (canvas 자식) | (250, 150) | 120×32 | 라벨 "크레딧" 18pt(CreateButton 기본) | Shop AdvanceButton 좌표 관례 미러, 기존 레이아웃과 비겹침 |
| `Panel_Credits` (canvas 마지막 자식) | (0, 0) | 640×360 | dim Image Ink Navy `#16202A` alpha 0.92, raycastTarget true | Panel_Ending 미러 — 모달, 초기 비활성, 최상단 sibling |
| `Panel_Credits/CreditsTitleText` | (0, 158) | 400×32 | 24pt Brass Amber | 제목 "크레딧" |
| `Panel_Credits/CreditsLeftText` | (-160, 10) | 300×240 | 10pt Steam Cream, TopLeft 정렬, wrap 유지 | `CreditsCopy.LeftColumn` — 수동 `\n`, 각 줄 ≤300px, 전체 높이 ≤240px |
| `Panel_Credits/CreditsRightText` | (160, 10) | 300×240 | 10pt Steam Cream, TopLeft 정렬, wrap 유지 | `CreditsCopy.RightColumn` — 자동 wrap, 전체 높이 ≤240px |
| `Panel_Credits/CreditsCloseButton` | (0, -152) | 200×40 | 라벨 "닫기" 18pt | **Navigation = Mode.None(빌드 타임에 굽기 — 포커스 탈출 차단, E절)**, 열림 시 focus 대상 |

- rect 검산: 제목 y 142..174(상단 여백 6) / 컬럼 y 130..-120(제목과 12px 간격) / 닫기 y -172..-132(컬럼과 12px, 화면 하단과 8px). 전부 화면 경계(±180) 내·상호 비겹침.
- 키보드 네비게이션(메뉴 화면): `LinkVerticalNavigation(creditsButton, startButton, continueButton)` — 기존 Start↔Continue 상하 연결(task-113 G1)은 그대로 보존되고 `startButton.selectOnUp = creditsButton`만 추가된다(기존 테스트 기대값 무변경). 패널 내부는 selectable이 닫기 버튼 1개 + Navigation None이라 체인 불요.

### E. 컨트롤러 / SceneBuilder 계약 — 모달 입력 격리 포함

**`CreditsController.cs`** (`game/Assets/Scripts/Runtime/UI/`, `ClientIsKing.UI`, sealed MonoBehaviour — Canvas 탑재):

- 직렬화 필드: `GameObject panelRoot` · `Button openButton`(=CreditsButton) · `Button closeButton` · `Button startButton` · `Button continueButton`. 텍스트 참조 없음 — 문구는 씬에 구워져 있어(B절) 컨트롤러는 **토글 + focus + 입력 격리**만 수행한다(도메인 접근 없음).
- **모달 입력 격리(Codex P0-1)**: dim raycast는 pointer만 차단하므로 키보드/게임패드 경로를 별도로 막는다.
  - **열 때**: 배경 버튼 3종(start/continue/open)의 **현재 `interactable` 값을 저장한 뒤 전부 false**로 잠근다(이어하기는 원래 disabled일 수 있으므로 값 저장이 필수). `panelRoot.SetActive(true)` + 닫기 버튼 focus.
  - **닫을 때**: 저장값으로 **정확히 복원**(무조건 true 복원 금지) → `panelRoot.SetActive(false)` → `CreditsButton`으로 focus 복귀.
  - **포커스 탈출 차단**: `CreditsCloseButton`의 `Navigation.Mode.None`(SceneBuilder가 빌드 타임에 굽는다 — D절)으로 방향키 이동 자체가 불가. 배경 버튼은 interactable false라 우회 focus도 불가.
- **Cancel 닫기(Codex P1 — 지원 채택)**: `Update()`에서 `panelRoot.activeSelf && Input.GetButtonDown("Cancel")`이면 닫기 경로를 재사용한다. 프로젝트 기본 InputManager의 "Cancel" 축 = Esc + 게임패드 B(joystick button 1) — 신규 InputSystem 도입 없음. 열기/닫기 본체는 `internal OpenNow()`/`CloseNow()`로 분리해 버튼 리스너·Cancel·테스트가 같은 경로를 탄다(EndingOverlay `RefreshNow` 전례).
- `OnEnable`/`OnDisable`: open/close 버튼 리스너 등록·해제 쌍(EndingOverlayController 미러, persistent listener 0 규약). focus는 `Application.isPlaying && EventSystem.current != null` 가드(FocusMainMenuButton 전례) — EditMode 테스트에서 no-op.
- 미배선 fixture no-op 가드(`panelRoot == null` 등 — RefreshSaveUi 전례). `#if UNITY_EDITOR` `EditorInit(GameObject panelRoot, Button openButton, Button closeButton, Button startButton, Button continueButton)`(task-102 패턴).
- `MainMenuController.cs`는 계속 **무수정** — 배경 버튼 잠금/복원은 CreditsController가 참조 주입으로 소유한다.

**`SceneBuilder.cs`**:

- `BuildMainMenu`: SaveStatusText 생성 뒤 `CreditsButton` 생성(D절 표) → `LinkVerticalNavigation(creditsButton, startButton, continueButton)`으로 기존 호출 대체 → 기존 요소 전부 생성 후 마지막에 `BuildCreditsPanel(canvasGo, creditsButton, startButton, continueButton)` 호출(최상단 sibling 보장).
- `BuildCreditsPanel` 신설(`BuildEndingOverlay` 미러): D절 표대로 패널·제목·좌/우 텍스트(`CreditsCopy` 상수를 구움, TopLeft 정렬 오버라이드)·닫기 버튼(Navigation None 굽기) 생성, `panel.SetActive(false)`, `canvasGo.AddComponent<CreditsController>()` + `EditorInit(panel, openButton, closeButton, startButton, continueButton)`.
- Shop 씬 접점 없음. `MainMenu.unity` 재생성(Apply가 Shop도 재생성하지만 산출 무변경이어야 한다).

### F. 라이선스·릴리스 패키지 — 동봉 파일 + 릴리스 게이트

**동봉 (이 task 산출)** — 런타임 코드는 읽지 않는 배포 동봉 전용, 각 사본은 원본과 바이트 동일(G절 테스트):

- `game/Assets/StreamingAssets/Licenses/Galmuri-LICENSE.txt` ← `game/Assets/Art/Fonts/Galmuri-LICENSE.txt` 사본. SIL OFL 1.1 조건 2는 배포 사본에 저작권 고지 **및 라이선스 전문** 동봉을 요구한다 — 전문(약 100행)은 640×360 인게임 표시가 비실용적이므로 패널에는 저작권 1행 + 라이선스명(A절)만 표시하고 전문을 동봉한다.
- `game/Assets/StreamingAssets/Licenses/LICENSE.txt` ← 루트 `LICENSE`(MIT) 사본 — 크레딧이 "코드: MIT License"를 선언하는 이상 배포물에 그 전문이 실려야 한다(Codex P0-3).
- (조건부) `THIRD-PARTY-NOTICES.txt` — 오너가 오픈 이슈 2 (a)안(Unity 상표 귀속·비제휴 고지)을 채택하면 같은 폴더에 추가하고 동일한 바이트 동일 테스트를 건다.

**선행 의존 (task-116 소유 — 의존성 기록)**: `README.md`/`README.en.md`에 코드 MIT · 프로젝트 고유 아트 MIT 제외 범위를 반영하는 커밋은 task-116 U2 소유이며, **task-117 done 및 공개 릴리스의 선행조건**이다 — 크레딧 패널의 라이선스 요약이 리포 문서와 일치해야 하기 때문이다(테스트 기준에 게이트로 기록).

**릴리스 게이트 (구현 비차단 · 출시 차단 — 오픈 이슈 4)**:

1. **task-115 Windows 빌드 출시 금지** — 크레딧 UI·라이선스 동봉이 없다.
2. 공개 출시 빌드는 **task-116(U2·U3 포함) + task-117 합류 후 재빌드**로만 만든다.
3. 최종 빌드 산출물 검증: `<빌드>/ClientIsKing_Data/StreamingAssets/Licenses/`에 MIT·OFL(+채택 시 THIRD-PARTY-NOTICES) notice 파일이 **실제 존재**하는지 릴리스 체크리스트에서 확인한다(빌드 스모크 항목).

### G. 테스트 계약

**신규 `game/Assets/Tests/EditMode/CreditsPanelSceneTests.cs`** (`SceneBuilder.Apply` OneTimeSetUp — EndingOverlaySceneTests 골격 미러):

1. 정적 구조 — `CreditsButton`·`Panel_Credits`·자식 4종 존재, D절 표의 좌표·크기·폰트 크기·색(dim Ink Navy a≈0.92 + raycastTarget true 포함) 전수, 패널 초기 비활성 + `childCount - 1` 최상단 sibling, **`CreditsCloseButton.navigation.mode == Navigation.Mode.None`**, `CreditsController`는 Canvas 탑재 1개.
2. 배선 — SerializedObject로 `panelRoot`/`openButton`/`closeButton`/`startButton`/`continueButton` 5참조 주입 검증. 네비게이션: `creditsButton.selectOnDown == startButton`, `startButton.selectOnUp == creditsButton`, 기존 Start↔Continue 연결 보존.
3. 열기/닫기 흐름(EditMode 몫) — `ForceOnEnable` 후 open 리스너 invoke → 패널 활성 + **배경 3버튼 interactable 전부 false**, close invoke → 비활성 + **저장값 정확 복원(Continue를 미리 `interactable=false`로 둔 fixture 포함 — 무조건 true 복원이면 fail)**. Cancel 경로가 `CloseNow()` 동일 경로임을 내부 메서드 호출로 검증(실 키 입력은 수동 smoke). 리스너 쌍: OnEnable 등록·OnDisable 해제(RuntimeCalls 리플렉션 전례), persistent listener 0.
4. 문구 동기화(B절 ①~⑤) — 씬에 구운 좌/우 텍스트 == `CreditsCopy` 상수, `RightColumn` ⊇ `AiArtNoticeKo`, `AiArtNoticeKo`·라이선스 요약 문장이 `kb/tasks/task-116/design.md`에 verbatim 존재(리포 상대 경로 `Application.dataPath/../../…`), 폰트 저작권자 표기(`Lee Minseo (quiple@quiple.dev)`, `SIL Open Font License`)가 `Galmuri-LICENSE.txt`에 존재, CC0 팩 4종 표기가 `PLACEHOLDER-PROVENANCE.md`에 존재.
5. 폭/높이 fit(**양 컬럼 — Codex P1**) — 좌측: 수동 개행 각 줄 `GetPreferredValues(줄).x ≤ 300`. **좌·우 모두**: TopLeft 정렬·wrap 활성 설정 확인 + `GetPreferredValues(전체 문자열, 300, 0).y ≤ 240` + `ForceMeshUpdate()` 후 `isTextOverflowing == false`. worst-case = 정적 전문 자체.
6. 글리프 커버리지(**비파괴 — Codex P1**) — **원본 Galmuri TMP 에셋에 `TryAddCharacters` 호출 금지**(아틀라스 dirty·에셋 오염). `Galmuri11.ttf`에서 임시 TMP_FontAsset을 생성(`TMP_FontAsset.CreateFontAsset`)해 크레딧 전체 문자(rich text 태그 제외, `©`·`–`·`·` 포함)의 커버리지를 검증하고 teardown에서 임시 에셋을 파기한다. 실패 시 제약 절의 대체 규칙 적용 후 재검증.
7. 라이선스 동봉 — `StreamingAssets/Licenses/Galmuri-LICENSE.txt` == `Art/Fonts/Galmuri-LICENSE.txt` 바이트 동일, `StreamingAssets/Licenses/LICENSE.txt` == 루트 `LICENSE` 바이트 동일(+ THIRD-PARTY-NOTICES 채택 시 동일 패턴 추가).
8. 멱등 — `SceneBuilder.Apply` 연속 2회: Panel_Credits 하위 오브젝트 수 동일, `CreditsController` 정확히 1개, open/close persistent listener 0.

**신규 `game/Assets/Tests/PlayMode/CreditsPlayModeTests.cs`** (PlayMode 신규 1건 — 기준선 9 → **10**, EndingPlayModeTests 전례):

- MainMenu 로드 → 크레딧 열기: 배경 3버튼(start/continue/credits) `interactable == false` + `EventSystem.current.currentSelectedGameObject == CreditsCloseButton` + CloseButton `Navigation.Mode.None`(방향키 탈출 불가 조건) → 닫기: 저장값 정확 복원(**Continue가 disabled인 fixture 포함** — 세이브 없음 상태가 기본 fixture라 Continue는 자연히 disabled) + focus가 `CreditsButton`으로 복귀.

**기존 테스트**: `SceneBuilderTests`에 MainMenu 크레딧 오브젝트 존재 검증 1~2건 **추가**(기존 기대값 무변경 — 기존 멱등 테스트는 상대 비교라 자동 커버). `MainMenuSaveFlowTests`는 무수정 통과 예상(이름 기반 조회·focus 가드 무접촉) — 수정이 필요해지면 크레딧 오브젝트 한정으로만 갱신하고 구현 노트에 기록한다.

### H. 구현 절차

1. U1 — `CreditsCopy.cs` 작성(A절 전문·B절 상수 3종) + `StreamingAssets/Licenses/` OFL·MIT 사본(+`.meta`). 기준선(EditMode 494 / PlayMode 9) 사전 실행·기록.
2. U2 — `CreditsController.cs` 작성(E절 계약 — 입력 격리·Cancel 포함).
3. U3 — `SceneBuilder` 확장(D/E절) + `SceneBuilder.Apply`로 씬 재생성(연속 2회 멱등 확인, Shop.unity 무변경 확인).
4. U4 — `CreditsPanelSceneTests`(EditMode) + `CreditsPlayModeTests`(PlayMode) + `SceneBuilderTests` 추가분 작성, 배치 컴파일·EditMode·PlayMode 게이트, `git status --short game` 오염 검사(사용자 소유 파일 staging 부재 확인 포함).
5. U5 — 640×360 원본 + 2× 캡처(메뉴 전경 / 크레딧 패널) 제출 → 오너 게이트(이슈 1 문구·저작자명, 이슈 2 엔진 표기) + 시각 승인(이슈 5) → notes/manifest/summary/status 기록, Codex 코드리뷰. done은 source-ready 선언이며 출시는 F절 릴리스 게이트를 따른다.

## 실행 계획 (Execution Plan)

- implement_model: claude-fable-5
- implement_effort: medium
- routing_reason: 기존 오버레이/버튼 패턴 미러 + 정적 텍스트 + 소규모 입력 격리 로직으로 신규 도메인·알고리즘이 없고, 법적 문구 정확성은 verbatim 상수·동기화 테스트·오너 게이트가 담보하므로 표준 effort로 충분하다.

| unit | 파일 범위 | depends_on | group |
|------|-----------|------------|-------|
| U1-copy | `game/Assets/Scripts/Runtime/UI/CreditsCopy.cs`, `game/Assets/StreamingAssets/Licenses/{Galmuri-LICENSE,LICENSE}.txt`(+`.meta` 쌍) | 없음 | G1 |
| U2-controller | `game/Assets/Scripts/Runtime/UI/CreditsController.cs` | 없음 | G1 |
| U3-scene | `game/Assets/Scripts/Editor/SceneBuilder.cs`, `game/Assets/Scenes/MainMenu.unity` 재생성 | U1-copy, U2-controller | G2 |
| U4-tests | `game/Assets/Tests/EditMode/CreditsPanelSceneTests.cs` 신설, `game/Assets/Tests/PlayMode/CreditsPlayModeTests.cs` 신설, `game/Assets/Tests/EditMode/SceneBuilderTests.cs` 추가분 | U3-scene | G3 |
| U5-records | 캡처 제출 + 게이트, `kb/tasks/task-117/{implementation-notes,manifest}.md`, `kb/artifacts/task-117-summary.md`, `kb/index/status.md` | U4-tests | G4 |

## 파일/모듈 영향 (Affected Files/Modules)

| 파일/모듈 | 변경 유형 | 설명 |
|-----------|-----------|------|
| `game/Assets/Scripts/Runtime/UI/CreditsCopy.cs` | create | 크레딧 문구 단일 C# 원천 — `AiArtNoticeKo`(C절 verbatim)·`LeftColumn`·`RightColumn` |
| `game/Assets/Scripts/Runtime/UI/CreditsController.cs` | create | 토글 + focus + 모달 입력 격리(interactable 저장/잠금/복원, Cancel 닫기) — 무상태 표시 계층 |
| `game/Assets/Scripts/Editor/SceneBuilder.cs` | modify | BuildMainMenu에 CreditsButton + 네비게이션 3-체인, `BuildCreditsPanel` 신설(CloseButton Navigation None 포함) |
| `game/Assets/Scenes/MainMenu.unity` | modify | SceneBuilder 재생성 산출 (크레딧 버튼·패널 추가) — task-116 U3와 병렬 금지, 본 task 선완료 |
| `game/Assets/Scenes/Shop.unity` | none | 재생성 멱등 — diff 발생 시 이상 신호 |
| `game/Assets/StreamingAssets/Licenses/Galmuri-LICENSE.txt` · `LICENSE.txt` (+폴더/파일 `.meta`) | create | OFL 조건 2 전문 + 루트 MIT 전문 동봉 — 원본 바이트 동일 사본 (THIRD-PARTY-NOTICES는 이슈 2 (a)안 채택 시 추가) |
| `game/Assets/Tests/EditMode/CreditsPanelSceneTests.cs` | create | G절 EditMode 1~8 자동 검증 |
| `game/Assets/Tests/PlayMode/CreditsPlayModeTests.cs` | create | G절 PlayMode — focus/입력 격리·복원 (기준선 9→10) |
| `game/Assets/Tests/EditMode/SceneBuilderTests.cs` | modify | MainMenu 크레딧 오브젝트 존재 검증 추가 (기존 기대값 무변경) |
| `game/Assets/Scripts/Runtime/UI/MainMenuController.cs` · 도메인/저장/밸런스/엔딩 소스·테스트 | none | 무수정 — diff 부재가 무변경 증거 |
| `UPDATING.md` · `kb/concepts/art-references/**` · `kb/concepts/development-priority.md` | none | 사용자 소유 — staging 금지 (제약) |
| `README.md` · `README.en.md` | none (의존) | 라이선스 범위 반영은 task-116 U2 소유 — task-117 done·릴리스 선행조건으로 기록만 |
| `kb/tasks/task-117/implementation-notes.md` · `manifest.md` · `kb/artifacts/task-117-summary.md` | create | 구현 기록·산출 요약 |
| `kb/index/status.md` | modify | `runtime/generate-status.py` 재생성 |

## 테스트 기준 (Test Criteria)

- [ ] `python -B runtime/validator/cli.py kb/tasks/task-117/design.md`가 종료 코드 0으로 통과한다.
- [ ] 구현 전 기준선 EditMode 494 / PlayMode 9를 실행해 실제 결과를 구현 노트에 기록한다.
- [ ] `CreditsPanelSceneTests` G절 1~8 전수 green: 정적 구조(D절 좌표·크기·색·초기 비활성·최상단 sibling·CloseButton Navigation None) / 배선 5참조·네비게이션 / 열기·닫기 + interactable 저장·잠금·정확 복원(Continue disabled fixture 포함) + Cancel 동일 경로 + 리스너 쌍 / 문구 동기화 5종 / 양 컬럼 fit(좌 줄폭 ≤300px · 양측 wrap 높이 ≤240px · overflow 없음) / 글리프 커버리지(원본 폰트 에셋 비파괴 — 임시 clone) / 라이선스 사본 2종 바이트 동일 / Apply 2회 멱등.
- [ ] `CreditsPlayModeTests` green: 열기 → 배경 3버튼 interactable false + focus == CloseButton(Navigation None), 닫기 → 저장값 정확 복원 + focus == CreditsButton. **PlayMode 총 10(기존 9 무수정 통과 + 신규 1)**.
- [ ] 기존 테스트 파일 무수정 통과(기대값 변경 0 — `SceneBuilderTests`는 추가만): EditMode 기존 494 전부 green + 신규 테스트 green(총계는 구현 노트에 기록).
- [ ] Unity 배치 컴파일 종료 코드 0 · `error CS` 없음. `SceneBuilder.Apply` 연속 2회 멱등(오브젝트 수·persistent listener 0), Build Settings 씬 2개(`MainMenu`+`Shop`)만 존재, Shop.unity 무변경.
- [ ] `git status --short game`에 `Library/Temp/Obj/Logs/UserSettings/Build*` 오염이 없고, 신규 `.meta`가 신규 파일에 한정되며 삭제 0건, 변경이 영향 파일 표 목록에 한정되고, 사용자 소유 파일(`UPDATING.md`·`kb/concepts/art-references/**`·`kb/concepts/development-priority.md`)이 staging에 없다.
- [ ] 도메인 무변경 증거: `GameState`/Ops/매니저/저장/엔딩/`MainMenuController` 소스·테스트가 diff에 나타나지 않는다.
- [ ] 오너 게이트(오픈 이슈 1·2 — done 전제, Claude self-approve 금지): 제작 저작자명(P0t4t0 vs 프로젝트명)·표기 문구·엔진 표기 방식을 오너가 확정했다.
- [ ] 시각 승인 게이트(오픈 이슈 5 — done 전제): 640×360 원본+2× 캡처(메뉴 전경/크레딧 패널)로 오너/Codex가 레이아웃·2단 10pt 가독성을 승인했다.
- [ ] README/README.en 라이선스 범위 반영(task-116 U2)이 task-117 done 이전에 합류했음을 확인한다(F절 선행 의존).
- [ ] 수동 smoke(오너): MainMenu에서 크레딧 열기(마우스/키보드) → Esc·게임패드 B로 닫기 → 게임 시작/이어하기 흐름이 이전과 동일하게 동작하고, 패널이 열린 동안 방향키로 배경 버튼에 도달할 수 없다.
- [ ] **릴리스 게이트(출시 차단 — 오픈 이슈 4)**: task-115 빌드 미출시 유지, task-116+117 합류 후 재빌드, 빌드 산출물 `StreamingAssets/Licenses/`에 MIT·OFL(+채택 시 THIRD-PARTY-NOTICES) 실존 확인을 릴리스 체크리스트에 기록했다.
- [ ] 구현 완료 후 `python runtime/validator/cli.py --check-done task-117`과 `python runtime/generate-status.py --check`가 통과한다.

## 오픈 이슈 (Open Issues)

1. **[오너 게이트 — done 전제] 표기 문구·저작자명 승인**: 루트 `LICENSE` 권리자는 `P0t4t0`인데 크레딧 초안 제작 줄은 "© 2026 Client is King Project"다 — **저작자명 표기(P0t4t0 vs 프로젝트명 vs 병기)는 임의 확정 금지, 오너 확정 대기**. 그 외 A절 신규 문구(AI 공개문·라이선스 요약은 C절 확정본이라 제외)도 함께 승인받는다. 반려 시 `CreditsCopy` 문구만 교체(구조 무변경).
2. **[오너 게이트 — done 전제] Unity 엔진 표기 방식**: `© Unity Technologies` 권리 표기는 사용하지 않는다(Codex P0-2). 두 안 중 오너가 확정: **(a)** 엔진 줄 유지 + `THIRD-PARTY-NOTICES.txt`에 공식 Unity 상표 귀속·비제휴(non-affiliation) 고지 기재(F절 동봉·테스트 추가), **(b)** 크레딧에서 엔진 표기 제거. 확정 전 A절 엔진 줄("Unity 6 (6000.3.8f1)" 사실 서술)은 잠정.
3. **[오너 결정 — 구현 비차단] AI 고지 표시 시점 + CC0 목록 처분**: 권고 = 지금부터 정적 표시(C절 근거 3종 — 출시 경로가 재빌드 게이트로 단일화되어 문구/빌드 불일치 노출 없음). CC0 팩 목록의 출시본 유지/축소도 이때 함께 판단.
4. **[릴리스 게이트 — 구현 비차단·출시 차단] 라이선스·릴리스 패키지(F절)**: task-115 Windows 빌드 출시 금지, task-116(U2·U3)+117 합류 후 재빌드 필수, 빌드 내 notice 파일 실존 검증, README/README.en 반영(task-116 U2) 선행. task-116 U3와의 구현 순서 고정(task-117 선완료 — 제약 참조)도 이 이슈가 추적한다.
5. **[오너/Codex 게이트 — done 전제] 640×360 시각 승인**: 2단 300px 컬럼 · 10pt Galmuri의 가독성 판정(task-114 D3 캡처 제출 전례). 반려 시 좌표·폰트 크기 조정은 이 설계의 D절 표 개정(경미 변경)으로 처리하고 문구는 건드리지 않는다.
6. **[기록 — 비차단] task 번호 충돌**: task-116의 "배치 2 = task-117" 참조(오픈 이슈 4 + 제외 절 49행)는 본 task와 충돌해 **양쪽 모두 task-118+로 정정 완료(2026-07-13)**. task-116 전수에 구 참조 잔존 0.
7. **[비차단 — v2 후보] EN 인게임 크레딧·저장소 URL·문의처 표기**: 데모 KO 단일 언어 범위 밖. EN 공개문은 README/AI-ART-NOTICE.txt(task-116)가 담당 — 다국어 크레딧이 필요해지는 시점(스토어 출시)에 별도 판단.

---

**결정 기록 (2026-07-13)**: Codex 독립 재검토(changes-requested)를 반영했다 — P0-1 모달 입력 격리(배경 버튼 interactable 저장/잠금/정확 복원 + CloseButton Navigation None + PlayMode 신규 1건, 기준선 9→10), P0-2 법적 문구 정정(라이선스 요약을 C절 정책 문장 verbatim으로 교체, 저작자명 P0t4t0 게이트 신설, Unity © 표기 제거 + 2안 게이트), P0-3 라이선스·릴리스 패키지(MIT 전문 동봉 추가, README 반영 선행 의존, task-115 빌드 출시 금지 + 합류 재빌드 릴리스 게이트, 완료 범위 = source-ready), P1(Cancel/게임패드 B 닫기 채택, 글리프 검증 비파괴 clone, 양 컬럼 fit 검증), 운영(task-116 U3 병렬 금지, 사용자 소유 파일 staging 금지). task-116 오픈 이슈 4의 task-117 참조는 task-118+로 정정했다. Status는 ready를 유지하며 오너 게이트(이슈 1·2·5)는 done 전제다.
