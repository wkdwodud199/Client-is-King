# 설계 문서 — task-115: 밸런싱 + 엔딩 + Windows 빌드 (데모 완료)

> Status: ready
> Inputs: `kb/concepts/project-brief.md`(SSOT — 로드맵 v3 task-115 "밸런싱 + 엔딩 + Windows 빌드 (데모 완료)", implement 라우팅 fable-5/high, 미슐랭은 최종 컨셉·데모 밖), `kb/concepts/demo-scope.md`(하드캡 — 씬 2개·이벤트 4종·장르 4종·CC0만, 주차장 "미슐랭 트랙"), `kb/tasks/task-113/design.md`(저장 v1 정규형 계약 — 직렬화 필드 추가 = 스키마 파괴 = v2+마이그레이션, 파산 세이브 잠금 G2, companyName 오픈 이슈, quit 저장 재검토 예약), `kb/tasks/task-114/design.md`+`kb/artifacts/task-114-summary.md`(기준선 EditMode 439/PlayMode 8, 자동 검증/시각 승인 분리 전례, NYC 오버홀 미결 시 본 마감본 출시), 현재 `game/` 코드 실측(2026-07-12 — `GameState.StartingCash=30000`·`SaveSchemaVersion=1`(GameState.cs:20,23), `SettlementOps.DailyOperatingCost=12000`(SettlementOps.cs:18)·`daysCompleted` 갱신(SettlementOps.cs:81), `GameManager` 파산 게이트(GameManager.cs:214-218,315-334), EventOps 운영비 리터럴 12000 2곳(EventOps.cs:444,508), 주문 수 공식(GenreSelectionOps.cs:99-104), 밸런스 가드 3종 기대값(GenreBalanceTests.cs:26-32,195 · SNSBalanceTests.cs:25,31-46 · EventBalanceTests.cs:23,137-141,365-375), `InitialDataBuilder.cs`(시드 SO 39종 단일 원천·멱등 upsert), `SceneBuilder.cs`(BuildShop/BuildNightPanel/ApplyBuildSettings), `MainMenuController.RefreshSaveUi` 분기 4종, `.gitignore:38`(`game/[Bb]uild*/` 기존), `ProjectSettings.asset:15-16`(DefaultCompany/Client is King))
> Outputs: task-115 구현 계약 — (1) 밸런싱: 운영비 상수 시드 12,000→28,000(조정 밴드+기계 가드 4종+오너 플레이테스트 게이트), EventOps 이중 기입 해소, SO `.asset` 무변경 결정, 락스텝 테스트 갱신 전수 목록; (2) 엔딩: 파생 엔딩 결정(스키마 v1 유지 — GameState 필드 0 추가), `EndingOps`(ClearTargetDays=7 시드) + GameManager 클리어 게이트(파산 미러) + Shop 엔딩 오버레이/MainMenu 클리어 분기; (3) Windows 빌드: `BuildTool.BuildWindows`(StandaloneWindows64, 배치(비대화형), 실패 시 비제로 exit), 산출 경로/검증 규약; 테스트 계약과 자동 검증/오너·Codex 게이트 분리
> Next step: Codex 설계 교차검토 후 G1(도메인·밸런스)→G2(표현·빌드)→G3(통합 검증) 순으로 구현하고 배치 컴파일→EditMode→PlayMode→BuildWindows 게이트를 통과시킨다. 오너 플레이테스트(밸런스 시드·N=7 체감)와 640×360 시각 승인, 빌드 실행 스모크를 거쳐 **M3 완료 = 데모 완료**를 선언한다.

## 목표 (Objective)

데모를 완성한다: (1) **밸런싱** — 유능한 플레이어(장르 선택·합리적 구매·성실 서빙)는 목표일에 도달하고, 방만한 플레이어(과다 구매·주문 방치·이벤트 무시)는 파산할 수 있는 난이도 곡선을 순수 밸런스 상수 튜닝만으로 만든다(공식·시드·라운딩 바이트 불변). (2) **엔딩** — 목표일 N일 정산 생존 = **데모 클리어**(승리), 기존 파산 = **게임 오버**(패배)의 양방향 엔딩을 도메인 게이트(파산 게이트 미러)와 씬 내 오버레이 연출로 완성한다. 최종 컨셉의 미슐랭 스타는 주차장(미슐랭 트랙) 그대로 두고, 데모 클리어는 "목표일 생존/달성"으로 스코프한다. (3) **Windows 빌드** — `MainMenu`+`Shop` 2씬을 StandaloneWindows64 플레이어로 배치(비대화형) 빌드하는 `BuildTool`을 만들어 배포 가능한 데모 산출물을 확보한다.

이 task 완료 = **M3 완료 = 데모 완료**다. 기존 M1~M3 규칙·수식·저장 스키마(v1)·씬 2개 하드캡·EditMode 439/PlayMode 8 기준선은 이 설계가 명시적으로 갱신하는 밸런스 파생 기대값을 제외하고 전부 불변으로 보존한다.

### 역할과 결정권

| 영역 | 결정권자 | 수행자 | 게이트 |
|------|----------|--------|--------|
| 밸런스 시드·엔딩 게이트·빌드 계약 | 이 설계(Claude 초안) → Codex 교차검토, 오너 최종 승인 | Claude 구현 | design validator + 자동 테스트 |
| 밸런스 시드 최종 확정(28,000·N=7) | **오너 플레이테스트** (Claude self-approve 금지) | Claude가 기계 가드 통과본 제공 | 수동 플레이 게이트 |
| 엔딩 화면 톤·카피 판정 | **오너/Codex** (Claude self-approve 금지) | Claude가 캡처 제공 | 640×360 원본+2× 시각 승인 |
| 빌드 실행 확인 | **오너** (exe 더블클릭 스모크) | Claude가 빌드 산출 | 수동 실행 게이트 |
| 코드 구조·테스트·배치 자동화 | 설계 계약 내 Claude | Claude | 컴파일·EditMode·PlayMode·멱등성 게이트 |
| 설계와 다른 수치·화면 판단 | Codex 재설계 요청 | Claude가 임의 확정하지 않음 | 새 design/review 기록 |

## 범위 (Scope)

### 포함 — task-115 구현 범위

- **밸런싱(상수만)**: `SettlementOps.DailyOperatingCost` 12,000 → **28,000**(시드 — B3 조정 밴드·확정 조건 포함), `EventOps`의 운영비 리터럴 12000 2곳(EventOps.cs:444,508)을 명명 상수 `EventOps.BaseDailyOperatingCost`로 승격하고 `SettlementOps.DailyOperatingCost`와의 동기 핀 테스트를 추가한다(발견된 이중 기입 위험 해소). `GameState.StartingCash`는 30,000 **유지**. 밸런스 파생 기대값을 하드코딩한 기존 테스트의 락스텝 갱신(B4 전수 목록).
- **SO `.asset` 데이터 무변경 결정**: 재료 18·레시피 6·장르 4·손님 4·SNS 3·이벤트 4의 시드 값은 task-110/111/112에서 승인·가드(±1%)된 상태로 **변경하지 않는다**(근거 B3-3). 변경이 필요해지면 `InitialDataBuilder` 경유 원칙(GUID 안정 upsert)만 계약으로 남긴다.
- **엔딩 도메인**: 순수 `EndingOps`(신규 `Runtime/DayCycle/EndingOps.cs`) — `ClearTargetDays = 7`(시드, 오너 게이트), 엔딩 상태는 기존 필드에서 **파생**(`isBankrupt` 우선, `daysCompleted ≥ N` 클리어)한다. **GameState 직렬화 필드 추가 없음 → `SaveSchemaVersion` 1 유지, 마이그레이션 불필요**(결정 C1). `EndingSummary` 표시 DTO(영업일수·최종 잔액·총 손익·팔로워 표시값).
- **엔딩 게이트**: `GameManager.CanAdvancePhase`/`AdvancePhase`에 클리어 차단을 파산 게이트와 대칭으로 추가한다(사유 문자열 고정, 이벤트 미발행, 상태 불변). `GameManager.LoadMainMenuScene()` 신설(LoadShopScene 미러).
- **엔딩 표현**: `SceneBuilder.BuildShop`에 전화면 엔딩 오버레이(클리어/게임오버 공용, 초기 비활성·최상단 sibling)를 코드 저작하고, 신규 `EndingOverlayController`(Canvas 탑재, 상태 폴링 — PhaseHud 전례)가 표시한다. `MainMenuController.RefreshSaveUi`에 **클리어 분기 1종 추가**(기존 4분기 + 1 — 이어하기 잠금 + Brass Amber 문구). 씬 2종 재생성. 파산의 엔딩 연출(게임 오버 오버레이)도 같은 오버레이가 담당한다 — 도메인 파산 규칙은 무변경.
- **Windows 빌드**: 신규 `game/Assets/Scripts/Editor/BuildTool.cs` — `BuildTool.BuildWindows`가 `EditorBuildSettings` 활성 씬 2개(MainMenu 선두)를 검증하고 `BuildPipeline.BuildPlayer`로 `game/Build/Windows/ClientIsKing.exe`(StandaloneWindows64)를 빌드, `BuildResult.Succeeded` 아니면 예외(배치 exit 비제로), 성공 시 exe 경로·크기 로그. `game/Build/`는 기존 `.gitignore:38`(`game/[Bb]uild*/`)이 이미 커버 — **gitignore 변경 없음, 검증만** 한다.
- **테스트**: 신규 — `EndingOpsTests`(파생 규칙·요약 DTO), `GameManagerEndingGateTests`(차단·사유·이벤트 0·상태 불변), `BalanceEndingGuardTests`(전 장르 N일 실루프 클리어 도달 + 무위 플레이 파산 도달 + 상수 동기 핀), `EndingOverlaySceneTests`(오버레이 구조·좌표·sibling·폭), `BuildToolTests`(씬 목록·산출 경로 순수부), `MainMenuSaveFlowTests` 클리어 분기 추가, PlayMode `EndingPlayModeTests` 1종. 갱신 — B4 락스텝 목록. 기준선 EditMode 439/PlayMode 8 + 신규 전부 통과.
- **기록**: `kb/tasks/task-115/implementation-notes.md`, `kb/artifacts/task-115-summary.md`, `python3 runtime/generate-status.py` 재생성.

### 제외 — task-115에서 구현하지 않음

- **미슐랭 트랙·요리대회·장슐랭** — 주차장(demo-scope.md) 유지. 데모 클리어는 목표일 생존 엔딩으로 한정하고 승격은 범위 변경 절차를 따른다.
- **GameState 직렬화 필드 추가 일체** — 누적 서빙 손님 수·run 시드(task-112/113 이월)·클리어 플래그 영속화 전부 제외. task-113 v1 정규형 계약상 필드 추가 = 스키마 파괴 = v2+마이그레이션이며, 이번 엔딩은 파생만으로 성립한다(C1). v2는 post-demo 오너 결정.
- **밸런스 공식·알고리즘 변경** — 수요/가격/감쇠/이벤트 수식, FNV-1a 시드, RoundHalfUp/MulMilliHalfUp, 밀리 투영, ordinal 정렬 전부 바이트 불변. 일차별 운영비 증가 같은 신규 수식도 금지(상수 튜닝만).
- **신규 장르·이벤트·재료·레시피·SNS 채널** — 하드캡. SO `.asset` 값 변경도 이번 결정으로 제외(B3-3).
- 신규 씬·신규 singleton manager(`EndingManager` 금지 — task-113이 Save를 GameManager 얇은 확장+순수 Ops로 대체한 전례를 따른다)·외부 트윈/빌드 라이브러리.
- 빌드 아이콘·코드 서명·압축 설정·IL2CPP 전환·해상도/윈도우 모드 커스텀·Steam 연동·인스톨러 — CC0 데모 범위 밖(오픈 이슈).
- `OnApplicationQuit` 저장(task-113 이월 재검토) — 이정표 저장 5종이 엔딩 시점(정산 트리거 3)을 이미 커버하므로 도입하지 않는다(오픈 이슈에 종결 기록).
- 사운드(task-114 이월) — 미포함, 오너 결정 대기(오픈 이슈).
- 아트 변경 — `PlaceholderArtBuilder`·파생 PNG·`Art/OpenSource/**` 무변경.

## 제약 (Constraints)

- `kb/concepts/project-brief.md`가 SSOT, `kb/concepts/demo-scope.md`가 하드캡: 씬 `MainMenu.unity`+`Shop.unity` 2개, 장르 4·이벤트 4, 아트 CC0/OFL만, 매니저는 신규 singleton 없이 GameManager/UI 얇은 확장(task-111/112/113 전례).
- **결정론 계약 바이트 보존**: 밸런싱은 **입력 숫자만** 바꾼다. `GenreSelectionOps.RoundHalfUp(x)=floor(x+0.5)`(GenreSelectionOps.cs:22), `MulMilliHalfUp(a,b)=(a*b+500)/1000`(:31), FNV-1a(offset 2166136261/prime 16777619, known vector `"gukbap|1|0"→2190636514`, :452-455), SO float 밀리 투영·ordinal 정렬은 무변경이며, 이벤트 100일 발생 스케줄(폭등 15·단체 13·위생 16·임대료 1 = 45회 — EventBalanceTests.cs:137-141)은 가중치 무변경의 증거로 기존 기대값 그대로 통과해야 한다.
- **저장 스키마 v1 불변**: task-113 정규형 계약(V2b — 직렬화 필드 집합·순서 고정) 때문에 `GameState` 필드 추가/삭제/개명은 전부 스키마 파괴다. 이 설계는 필드를 추가하지 않으므로 `SaveSchemaVersion == 1`이 유지되고 기존 세이브가 그대로 호환된다. 구현 중 필드 추가가 필요해 보이면 구현하지 말고 리뷰로 되돌린다.
- **기준선**: task-114 완료 실측 EditMode **439**/PlayMode **8**(task-114-summary "검증" 절). 이 설계가 명시 갱신하는 밸런스 파생 기대값(B4 전수 목록)이 기존 테스트 수치를 바꾸는 **유일하게 허용된** 변경이며, 그 외 무회귀. 신규 엔딩/빌드/가드 테스트는 기준선에 가산된다.
- 순수 규칙은 `*Ops`(Unity 미참조), lifecycle/SO/파일/씬은 manager·UI 계층. `EndingOps`는 순수 C#(참조: DayCycle·Social의 순수 헬퍼만). `BuildTool`은 Editor 전용(`ClientIsKing.EditorTools`).
- UI는 색만으로 상태를 전달하지 않는다(문구+색 병용 — E5). 좌표는 640×360 캔버스 픽셀 고정, 코드 저작(`SceneBuilder.Apply`)만이 씬의 정본. `SceneBuilder.Apply` 연속 2회 멱등(오브젝트 수·persistent listener) 유지, Build Settings 씬 2개 유지.
- 통화는 원 정수. `game/ProjectSettings/`·`game/Packages/`·`*.meta`는 버전 관리 대상(추가 에셋의 `.meta` 쌍 생성). `Library/Temp/Obj/Logs/UserSettings/Build*`는 git 노출 금지 — `game/Build/` 산출물이 `git status`에 나타나면 실패다.
- Unity 배치 규약: `-runTests`에는 `-quit` 금지(task-112/114 노트의 함정), 빌더/빌드는 `-batchmode -quit -executeMethod ...`. 실패는 예외로 전파해 exit 비제로를 보장한다(SceneBuilder.SaveScene 전례).
- 밸런스 시드·목표일 N·엔딩 카피의 **최종 판정은 오너/Codex**다. 자동 검증(기계 가드) 통과는 병합 전제 조건이고, 오너 플레이테스트·시각 승인은 done 게이트 조건이다(task-114 D절 분리 전례).

## 구현 단계 (Implementation Steps)

### A. 플레이어 경험 — 데모가 완결되는 인과

1. **긴장이 생긴다**: 하루 순마진이 "이벤트·SNS가 흔들 수 있는 크기"로 줄어(B2), 위생 점검(-8,000)·임대료 인상(영구 +15%)·재료값 폭등(+35%)이 체감되는 위협이 되고, SNS 집행(7,000~15,000원)이 하루 이익의 3~7할을 거는 진짜 베팅이 된다. 성실한 플레이는 여전히 흑자, 방만한 플레이(주문 방치·과다 구매·감쇠 무시 연속 집행)는 파산으로 굴러떨어질 수 있다. 아무것도 하지 않으면 Day 2에 파산한다.
2. **끝이 생긴다 (승리)**: Day 7 정산을 흑자로 마치는 순간 화면 전체에 **"데모 클리어!"** 오버레이가 뜬다 — 영업일수·최종 잔액·총 손익·팔로워가 요약되고, "메인 메뉴로 ▶" 버튼만 남는다. 진행 버튼은 잠긴다(런 종료). MainMenu로 나가면 이어하기 자리에 "데모 클리어!" 기록이 남고 새 게임을 권한다.
3. **끝이 생긴다 (패배)**: 파산 순간 같은 오버레이가 **"게임 오버"** 로 뜬다 — 버틴 일수·파산 사유가 표시되고 메인 메뉴로 나가 재도전한다(파산 세이브 잠금은 task-113 그대로).
4. **배포물이 생긴다**: `BuildTool.BuildWindows` 한 방으로 `ClientIsKing.exe`가 나오고, 오너가 에디터 없이 더블클릭으로 데모 전체(새 게임 → 7일 → 클리어/파산 → 이어하기)를 플레이할 수 있다.
5. **바뀌지 않는 것**: 하루 루프 규칙·수요/가격/이벤트/SNS 수식·저장 포맷·아트·조작감은 그대로다. 숫자와 끝맺음만 데모답게 조여진다.

### B. 밸런싱 계약 (상수만 — 공식 불변)

#### B1. 현행 실측 (2026-07-12 — 전부 코드/테스트 인용)

| 항목 | 현행 값 | 원천 |
|------|---------|------|
| 시작 자금 | 30,000원 | `GameState.StartingCash` (GameState.cs:23) |
| 일일 운영비 | 12,000원 | `SettlementOps.DailyOperatingCost` (SettlementOps.cs:18) |
| 운영비 **이중 기입** | 리터럴 `12000` 2곳 | EventOps.cs:444(예고 운영비)·:508(정산 원인 라인) + 주석 EventDayEffects.cs:86 |
| 하루 주문 수 | 국밥 4 · 분식 6 · 면류 5 · 제네럴 5 | `clamp(RoundHalfUp(5/time), 4, 6)` (GenreSelectionOps.cs:99-104) |
| 100일 평균 기여이익(전량 서빙·C급, 매출−재료비) | 국밥 49,266 · 분식 48,532 · 면류 49,949 · 제네럴 51,581 | GenreBalanceTests.cs:26-32 (±1% 가드) |
| → 현행 일 순마진(기여이익−운영비) | **약 36,500~39,600원** | 위 두 행의 차 |
| SNS 1회차 net(보너스 매출−비용, 운영비 제외) | 채널·장르별 +1,068~+12,658원 / 2회차 대부분 음수 | SNSBalanceTests.cs:31-46 |
| 이벤트 효과 시드 | 폭등 +35%/2일 · 위생 flat 8,000/1일 · 임대료 +15%/영구 · 단체 4인/1일 | InitialDataBuilder.cs:257-264 |
| 이벤트 발생 밀도 | 100일 45회(≈0.45회/일) → 7일 기대 ≈ 3.2회 | EventBalanceTests.cs:137-141 |
| 시드 SO 데이터 단일 원천 | 재료 18·레시피 6·장르 4·손님 4·SNS 3·이벤트 4 | InitialDataBuilder.cs (멱등 upsert·GUID 안정) |

#### B2. 진단과 목표

현행 일 순마진 ~37,000원 대비 위험 요소의 크기(위생 8,000 = 마진의 22% · 임대료 인상분 +1,800/일 = 5% · SNS 최대 15,000 = 41%)가 왜소해 **Day 1을 넘긴 순간 긴장이 소멸**한다. 파산은 무위 플레이(현행도 Day 3 파산)나 Day 1 과소비에만 존재한다.

**목표(밸런싱 계약)**: ① 숙련 플레이(전량 서빙·C급·이벤트 대응)는 목표일 N=7에 안정적으로 도달한다. ② 방만 플레이(주문 방치·감쇠 무시 SNS 연타·이벤트 방치)는 파산 가능하다 — 무위 플레이는 Day 2 파산. ③ 위생/임대료/SNS가 각각 일 순마진의 약 20~80% 크기가 되어 의사결정이 유의미해진다. ④ 장르 간 공정성(기여이익 max/min ≤ 1.10)과 Day-1 구매 가능성(이론 구매비 ≤ 시작 자금)은 불변.

#### B3. 결정 — 변경 표 (from → to)

| # | 항목 | from | to (시드) | 근거 |
|---|------|------|-----------|------|
| 1 | `SettlementOps.DailyOperatingCost` | 12,000 | **28,000** | 일 순마진 ~37k → **~20.5k~23.6k**. 위생 8,000 = 마진의 ~38%, 임대료 인상분 +4,200/일 = ~20%, SNS 12~15k = 하루 이익 급 베팅. 무위 플레이 파산이 Day 3 → **Day 2**(30,000 − 28,000 = 2,000 < 28,000)로 당겨진다 |
| 2 | EventOps 운영비 리터럴 2곳 | `12000` (EventOps.cs:444,508) | 신규 상수 `EventOps.BaseDailyOperatingCost`(= #1과 동일 값) + **동기 핀 테스트** | 이중 기입 drift 차단. EventOps는 계층 규약상 Settlement를 참조하지 않으므로(EventOps.cs:11 — Genre만 허용) 직접 참조 대신 명명 상수+핀 테스트로 동기화한다. 주석(EventDayEffects.cs:86 등)도 동기 갱신 |
| 3 | `GameState.StartingCash` | 30,000 | **30,000 유지** | 전 장르 Day-1 이론 구매비 ≤ 30,000 가드(GenreBalanceTests) 보존 + 무위 파산 Day 2 성립(30,000 < 28,000×2). 올리면 긴장 지연, 내리면 국밥 Day-1 구매 불가 위험 |
| 4 | SO `.asset` 39종 값 | task-110/111/112 승인 시드 | **전부 무변경** | 장르 공정성 ±1%·SNS 감쇠 곡선(1회차 양수/2회차 음수)·이벤트 스케줄이 이미 가드된 승인 시드다. SO를 건드리면 GenreBalanceTests 설계값 4종·SNSBalanceTests 표 12쌍·EventBalanceTests 발생 횟수의 전면 재유도가 필요해 데모 마감 직전 리스크만 산다. 난이도 문제의 원인은 **돈 나가는 구멍(운영비)** 하나이므로 그 상수만 조인다 |
| 5 | 클리어 목표일 | (없음) | **`EndingOps.ClearTargetDays = 7`** | 하루 루프 3~5분(task-113 A4) × 7일 ≈ 데모 20~35분. 7일 안에 이벤트 기대 ~3.2회·SNS 감쇠 학습(1→2회차)·스노볼(클리어 시 잔액 기대 ≈ 30,000 + 7×21k ≈ 17만원대)이 모두 등장한다 |

**시드 조정 밴드(기계 게이트)**: #1의 확정값은 오너 플레이테스트 게이트지만, 구현 단계에서 아래 4조건을 **모두** 만족해야 한다. 시드 28,000이 실측에서 하나라도 깨지면 밴드 **[22,000, 30,000]** 안에서 조건을 만족하는 최대값으로 조정하고 implementation-notes에 실측 근거를 기록한다(임의 재밸런싱 아님 — 이 절차 자체가 계약). 밴드 내 어떤 값도 조건을 못 지키면 구현을 멈추고 설계 리뷰로 되돌린다.

1. 전 장르: Day 1~7 실루프(전량 서빙·C급·SNS 무집행·이벤트 FNV 스케줄 포함, `GameManager.AdvancePhase` 프로덕션 경로)가 파산 없이 Day 7 정산 성공 → 클리어 도달.
2. 전 장르: Day 1~3 각 일의 (기여이익 − 운영비) > 0 (무이벤트 기준 — 기존 GenreBalanceTests 가드의 상수 갱신).
3. 무위 플레이(장르 선택 후 구매 0·전 주문 `SkipCurrentOrder`): Day 3 이내 파산.
4. 전 장르 Day-1 이론 구매비 ≤ `StartingCash` (기존 가드 유지).

#### B4. 락스텝 테스트/주석 갱신 — 전수 목록 (이것 외의 기존 기대값 변경 금지)

| 파일 | 갱신 내용 |
|------|-----------|
| `GenreBalanceTests.cs` | :195 `const int operatingCost = 12000` → 시드값. `DesignAverageContribution`(:26-32)은 운영비 미포함이므로 **불변** |
| `SNSBalanceTests.cs` | :25 `OperatingCost = 12000` → 시드값(사용처 실측 후 파생 기대값 함께). `DesignNet` 표(:31-46)는 운영비 미포함이므로 **불변** |
| `EventBalanceTests.cs` | :23 `OperatingCost` 상수 + 운영비 파생 asserts(:373-375 — `13800=12000×1.15`→`32200`, `20000=12000+8000`→`36000`, `21800`→`40200` 등) 재계산. 발생 횟수(:137-141)·단가(:365-370)는 **불변** |
| `EventOpsTests.cs` | `NextDayOperatingCost`/`BuildSettlementCauseLine` 운영비 파생 기대값만 재계산(FNV 스케줄·비율 검증은 불변) |
| `NightPanelEventFlowTests.cs` | :93,:97 임대료 인상(+15%) 반영 경고 기대값 `13,800`(= MulMilliHalfUp(12000,1150)) → **`32,200`**(= MulMilliHalfUp(28000,1150)) 갱신 + :93 `cash=5000` 주석을 32,200 기준으로 갱신(5,000 < 32,200이라 경고 표시 fixture는 그대로 유효) |
| `SceneBuilder.cs` | :710 정산 패널 정적 플레이스홀더 `"운영비  -12,000원"` → **`"운영비  -0원"`** 중립값(형제 플레이스홀더 `매출 +0원`(:706)·`재료 지출 -0원`(:708)·`순손익 +0원`(:712)과 일관 — 런타임은 `SettlementPanelController.ApplyNumbers`(:185)가 덮어쓰므로 기능 무변경, 씬 재생성으로 반영) |
| `SettlementOpsTests.cs` / `FirstPlayableLoopTests.cs` / PlayMode `SaveLoadPlayModeTests.cs` | `DailyOperatingCost` 상징 참조라 원칙 자동 추종 — **fixture 잔액의 지불능력 전제**(예: FirstPlayableLoopTests.cs:193 `cash=100` 파산 fixture, PlayMode Day1→2 경로)만 실측 확인, 필요 시 fixture 값·주석("운영비(12,000)" 류)을 갱신 |
| 소스 주석 | SettlementOps.cs:17 · EventDayEffects.cs:86 · FirstPlayableLoopTests.cs:117,193 등 12,000 언급 주석 동기화 |

**갱신 검증 기준 — 광역 grep-0이 아니다**: `12,000`은 운영비뿐 아니라 **SNS 숏폼 채널 비용(SO 시드)** 과 값이 겹치므로, 광역 "grep 0건" 기준은 변경 금지 값을 잡아 잘못된 수정을 유도한다. 기준은 다음과 같다 — "**운영비 문맥의 리터럴이 전부 `SettlementOps.DailyOperatingCost`/`EventOps.BaseDailyOperatingCost` 심볼 또는 위 표의 갱신 기대값으로 교체되었고, 아래 변경 금지 whitelist는 바이트 불변**".

**변경 금지 whitelist (`12,000` 값 겹침 지점 — diff 0 유지, 건드리면 실패)**:

- SNS 숏폼 채널 비용 12,000원(SO 시드 — B3 #4 무변경 결정): `InitialDataBuilder.cs:229`, `SceneBuilder.cs:763`(Night 패널 초기 라벨 `"숏핑 12,000원"`), `SNSCampaignOpsTests.cs:40,:590`(`BaseCost = 12000`), `NightPanelSnsFlowTests.cs:101`(`비용(7,000/12,000/15,000)` 잔액 부족 fixture — **SNS 비용이지 운영비 fixture가 아니다**)
- SaveOps 정규형 자기일관 fixture: `SaveOpsTests.cs:141,:177,:178,:184`(`0−0−12000=−12000` 등 — V3 규칙(Net==Gross−Spend−Cost)의 **내부 일관성**만 검증하므로 상수 값과 무관, "SaveOpsTests diff 0" 계약과 일관)

#### B5. 불변 계약 (밸런싱이 건드리지 않는 것)

- 수식·해시·라운딩·투영·정렬 전부 바이트 불변(제약 절). 변경 파일에 `Ops` 수식 diff가 없어야 한다(상수 선언·주석 제외).
- 이벤트 스케줄 100일 발생 횟수(15/13/16/1)·SNS 감쇠 곡선·장르 기여이익 설계값 4종·max/min ≤ 1.10 가드 전부 기존 기대값 그대로 통과 — **SO 무변경·수식 무변경의 기계적 증거**다.

### C. 엔딩 도메인 계약

#### C1. 저장 스키마 결정 — 파생 엔딩 채택 (v1 유지, 필드 0 추가)

두 안을 비교해 **파생**을 채택한다:

| | (a) 영속 필드(`isCleared`/`clearedDay`) | (b) 파생(`!isBankrupt && daysCompleted ≥ N`) — **채택** |
|---|---|---|
| 스키마 | task-113 v1 정규형 계약상 **파괴 변경** → v2 + 마이그레이션 + 정규형/왕복/검증 매트릭스 확장 + 기존 세이브 이전 | **v1 그대로** — 기존 세이브·검증 매트릭스 V1~V11·정규형 검사 무변경 |
| 정합성 | `isCleared`와 `daysCompleted`의 이중 진실 — V-매트릭스 정합 행 추가 필요 | 단일 진실(`daysCompleted`는 SettlementOps.cs:81이 정산 성공 시에만 갱신) — 모순 상태가 표현 불가능 |
| 파산 우선순위 | 필드 간 배타 규칙 별도 검증 필요 | 파산 시 `daysCompleted` 미갱신(SettlementOps 성공 분기 전용)이라 Day-N 파산은 클리어가 될 수 없음 — 구조적 배타 |
| 대가 | 누적 통계(총 서빙 손님 수 등) 영속화 가능 | 누적 통계 불가 — 엔딩 요약은 파생 가능한 값만(C2). 필요 시 post-demo v2(오픈 이슈) |

`N`(=`ClearTargetDays`) 변경은 코드 상수 변경이지 세이브 파괴가 아니다 — 과거 세이브도 새 N 기준으로 재해석된다(허용: 데모 단계, 오픈 이슈에 기록).

#### C2. EndingOps (신규 `Runtime/DayCycle/EndingOps.cs` + `EndingSummary.cs` — 순수 C#)

```csharp
namespace ClientIsKing.DayCycle
{
    /// <summary>런 엔딩 상태 — 파생 전용 (GameState 에 영속 필드 없음, task-115 C1).</summary>
    public enum RunEndingStatus { None = 0, Cleared = 1, Bankrupt = 2 }

    /// <summary>데모 엔딩 규칙의 단일 원천 (순수 — Unity/SO/IO 미참조).</summary>
    public static class EndingOps
    {
        /// <summary>데모 클리어 목표 영업일수 (시드 — 오너 플레이테스트 게이트, B3 #5).</summary>
        public const int ClearTargetDays = 7;

        /// <summary>파산 우선 → daysCompleted ≥ N 클리어 → None. (파산 시 daysCompleted 미갱신이라 동시 성립 불가)</summary>
        public static RunEndingStatus GetStatus(GameState state);
        public static bool IsCleared(int daysCompleted, bool isBankrupt); // MainMenu 가 SaveSummary 로 호출하는 원시형
        public static bool IsRunEnded(GameState state);                  // status != None
        public static EndingSummary BuildSummary(GameState state);       // 아래 DTO — UI 재계산 금지
    }

    /// <summary>엔딩 오버레이 표시 전용 DTO (전부 기존 필드에서 파생).</summary>
    public sealed class EndingSummary
    {
        public RunEndingStatus Status;
        public int DaysCompleted;        // state.daysCompleted
        public int FinalCash;            // state.cash
        public int NetProfit;            // state.cash - GameState.StartingCash (파산 시 음수 정직 표기)
        public int FollowerDisplay;      // SNSCampaignOps.CalculateFollowerDisplay(state.snsCampaignHistory) — NightPanel 전례(:163)
        public string BankruptcyReason;  // state.bankruptcyReason ("" = 비파산)
    }
}
```

- null state는 명확한 예외(`SettlementOps.Require` 전례). `GetStatus`는 분기 순서까지 계약이다: `isBankrupt → Bankrupt`, `daysCompleted >= ClearTargetDays → Cleared`, 그 외 `None`.
- 클리어 성립 시점 = Day N 정산 **성공** 적용 순간(`daysCompleted`가 N에 도달, Settlement phase). Day N의 Market/Service 중에는 `daysCompleted == N−1`이라 게이트가 절대 선발동하지 않는다(결정론 — 테스트 고정).

#### C3. GameManager 클리어 게이트 (파산 게이트 미러 — 2지점)

1. `CanAdvancePhase`(GameManager.cs:206): 기존 파산 차단(:214-218) **직후**에 추가 —
   `if (EndingOps.GetStatus(state) == RunEndingStatus.Cleared) { reason = "데모 클리어 상태에서는 진행할 수 없습니다."; return false; }` (사유 문자열은 테스트가 정확 일치로 고정 — 파산 사유 `"파산 상태에서는 진행할 수 없습니다."` 미러).
2. `AdvancePhase`(GameManager.cs:313): (a) 최상단 `if (state.isBankrupt)`(:315-318)를 `if (EndingOps.IsRunEnded(state))`로 확장(파산+클리어 공용 조기 반환 — 현재 phase 유지, 이벤트 미발행, 상태 불변). (b) Settlement 인라인 정산 적용 분기(:319-334)에서 `result.Applied` 후 `AutoSave()`(트리거 3 — 클리어 상태가 여기서 저장된다), `result.Bankrupt` 반환(:330-333) **다음**에 `if (EndingOps.GetStatus(state) == RunEndingStatus.Cleared) return state.currentPhase;` 추가 — Day N 정산이 이 호출 안에서 적용된 경우 Night로 넘어가지 않고 Settlement에 머문다(파산 미러).
3. `LoadMainMenuScene()` 신설: `SceneManager.LoadScene("MainMenu")` — `LoadShopScene`(:376-379) 미러. 엔딩 오버레이 버튼 전용.
4. **무변경 확인 지점**: `PhaseHudController.Update`(:88-89)는 `CanAdvancePhase` 경유로 자동 잠기므로 무변경. `SettlementOps`·`DayPhaseMachine`·저장 파이프라인(`SaveOps`/V1~V11) 무변경. 자동 저장 트리거 5종 무변경(클리어는 트리거 3이 이미 저장).

#### C4. 세이브·재개 상호작용

- 클리어 세이브는 **정상 v1 세이브**다(Day N·Settlement·settlementDay==N·daysCompleted==N — V3 규칙 `daysCompleted == day` 만족). `TryPeekSave`/`TryLoadGame` 도메인 경로는 무변경 통과한다(파산 세이브도 도메인은 로드 가능, UI만 잠그는 task-113 미러).
- `MainMenuController.RefreshSaveUi` 분기 4종(파일 없음/정상/파산/손상 — MainMenuController.cs:87-112)에 **클리어 분기**를 파산 분기 앞에 추가한다: `summary.IsBankrupt`가 아니고 `EndingOps.IsCleared(summary.DaysCompleted, summary.IsBankrupt)`이면 이어하기 잠금 + 클리어 문구(D3 표). `SaveSummary`는 이미 `DaysCompleted`/`IsBankrupt`를 노출하므로 DTO 무변경.
- 기존 테스트 간섭 실측: `AdvancePhase`를 N일 이상 돌리는 기존 테스트는 없다(FirstPlayableLoopTests는 1일 루프, 밸런스 가드 3종은 순수 Ops 시뮬). 구현 시 `daysCompleted >= 7` fixture 전수 grep으로 재확인하고, 발견 시 해당 테스트를 <N로 조정하는 대신 이 설계의 게이트 계약과 함께 리뷰에 올린다(임의 수정 금지).

### D. 엔딩 표현 계약 (SceneBuilder — Codex 카피/시각 게이트)

#### D1. Shop 엔딩 오버레이 (640×360 픽셀 고정 — 신규 오브젝트 6종)

`BuildShop`에서 장르 modal `SetAsLastSibling()`(SceneBuilder.cs:139-140) **이후** 생성해 오버레이가 항상 최상단 sibling이 되게 한다. 초기 `SetActive(false)`.

| 오브젝트 | anchoredPosition / sizeDelta | 내용 | 비고 |
|----------|------------------------------|------|------|
| `Panel_Ending` | (0,0) / 640×360 | Image `#16202A`(Ink Navy) alpha ≈0.92 | **raycastTarget true** — 하부 UI 클릭 차단(모달) |
| `EndingTitleText` | (0,70) / 400×40, 28pt | `데모 클리어!` / `게임 오버` | 클리어 Brass Amber `#E5A84B` / 파산 Warning Plum `#A93E58` |
| `EndingStatsText` | (0,26) / 460×32, 12pt | `영업 {DaysCompleted}일 · 최종 잔액 {FinalCash:N0}원`\n`총 손익 {부호}{NetProfit:N0}원 · 팔로워 {FollowerDisplay:N0}명` | Steam Cream `#F4E5C2`. 부호는 SettlementPanel netText 규약(≥0이면 `+`) |
| `EndingMessageText` | (0,−20) / 460×36, 11pt | 클리어: `목표 {ClearTargetDays}일 영업을 달성했습니다 — 데모는 여기까지! 플레이해 주셔서 감사합니다.` / 파산: `{bankruptcyReason}`\n`새 게임으로 다시 도전하세요.` | Steam Cream / 파산 1행은 Warning Plum |
| `EndingMainMenuButton` | (0,−72) / 200×40 | `메인 메뉴로 ▶` (기본 18pt) | 표시 시 focus 대상 |

세로 점유: Title 50~90 / Stats 10~42 / Message −38~−2 / Button −92~−52 — 겹침 없음, 캔버스(±180) 내. worst-case 문자열(최장 파산 사유 포함)의 Galmuri TMP `GetPreferredValues` 폭 ≤ 460px을 씬 테스트로 고정한다(task-112 F5 전례). 카피는 Codex 소유 UX copy — 임의 수정 금지, 상태는 문구가 전달하고 색은 보조(E5).

#### D2. EndingOverlayController (신규 `Runtime/UI/EndingOverlayController.cs` — Canvas 탑재)

- 오버레이 GO가 비활성이어도 동작하도록 컨트롤러는 **Canvas에 탑재**하고(PhaseHudController 전례) 오버레이 루트를 참조로 토글한다. `Update()`에서 `EndingOps.GetStatus(gm.State)`를 폴링(PhaseHud `Update` 폴링 전례 — 이벤트 신설 없음, 로드/에디터 fixture에도 상태 기준으로 안전)해 `None`이 아니고 오버레이 비활성이면 `Render(summary)` + `SetActive(true)` + 버튼 focus(`Application.isPlaying` 가드), `None`인데 활성이면 숨긴다(새 런 fixture 대비).
- `Render`는 `EndingOps.BuildSummary` DTO 표시 전용(UI 재계산 금지). 버튼 클릭 → `GameManager.Instance.LoadMainMenuScene()`(리스너는 OnEnable/OnDisable 쌍 — 기존 컨트롤러 규약).
- `internal void RefreshNow()`(IVT)로 EditMode가 폴링 없이 즉시 상태 반영을 검증한다. `EditorInit(GameObject overlayRoot, TMP_Text title, TMP_Text stats, TMP_Text message, Button mainMenuButton)` — SceneBuilder 주입 전용.
- 대안 검토: `GameEvents.SettlementPresented` 구독안은 로드 직후·EditMode fixture에서 발행 부재 시 표시 누락이 생겨 기각(상태 폴링이 발행 순서 문제가 없다 — task-113 G4가 겪은 순서 문제의 교훈).
- 파산 표현과의 정합: 파산은 Settlement phase에 머물며(기존) SettlementPanel 위로 오버레이가 덮인다. NightPanel 파산 분기·SettlementPanel 메시지는 무변경(오버레이 뒤에 존재).

#### D3. MainMenu 클리어 분기 (RefreshSaveUi 4→5분기)

| 상태 | ContinueButton | SaveStatusText | 색 |
|------|----------------|----------------|-----|
| (기존 4분기) | task-113 G2 그대로 | 그대로 | 그대로 |
| **peek 성공 · 클리어 (신규)** | 비활성 | `데모 클리어! (영업 {DaysCompleted}일 달성) — 새 게임을 시작하세요.` | Brass Amber `#E5A84B` (상수 추가) |

판정은 `EndingOps.IsCleared(summary.DaysCompleted, summary.IsBankrupt)` 하나만 쓴다(제2 원천 금지). 분기 우선순위: 파산 → 클리어 → 정상(파산이 앞 — C2 우선순위 미러). MainMenu 레이아웃·좌표는 무변경.

### E. Windows 빌드 계약 (BuildTool)

#### E1. BuildTool (신규 `game/Assets/Scripts/Editor/BuildTool.cs`)

```csharp
namespace ClientIsKing.EditorTools
{
    /// <summary>task-115: Windows 데모 플레이어 배치(비대화형) 빌드 진입점 — `-nographics` 는 쓰지 않는다(E1).
    /// 실행(리포 루트 기준):
    ///   Unity.exe -batchmode -quit -projectPath game -buildTarget StandaloneWindows64
    ///     -executeMethod ClientIsKing.EditorTools.BuildTool.BuildWindows -logFile [log]
    /// 실패는 예외로 전파한다 — 배치 모드 exit code 비제로 보장 (SceneBuilder.SaveScene 전례).</summary>
    public static class BuildTool
    {
        public const string OutputDir = "Build/Windows";          // projectPath(game/) 상대 — .gitignore:38 이 커버
        public const string PlayerExeName = "ClientIsKing.exe";

        /// <summary>EditorBuildSettings 활성 씬 투영 — 정확히 [MainMenu, Shop] 순서 2개가 아니면 예외.
        /// (SceneBuilder.MainMenuPath/ShopPath 상수 재사용 — 씬 하드캡의 빌드측 검증, EditMode 테스트 대상)</summary>
        public static string[] ScenePaths();

        /// <summary>BuildPipeline.BuildPlayer(StandaloneWindows64, BuildOptions.None).
        /// summary.result != Succeeded 또는 totalErrors > 0 이면 예외. 성공 시 exe 절대경로·totalSize 로그.</summary>
        public static void BuildWindows();
    }
}
```

- 산출: `game/Build/Windows/ClientIsKing.exe` + Unity 동반 폴더. 재실행은 덮어쓴다(정리 불필요 — gitignored).
- `ScenePaths()`는 순수(EditMode 테스트: 2개·순서·경로 상수 일치). `BuildWindows()` 자체는 EditMode 테스트로 실행하지 않는다(수 분 소요) — 배치 스모크 게이트(G절)로 검증한다.
- `-nographics`는 쓰지 **않는다**(플레이어 빌드의 셰이더 처리 안전 우선 — 빌더/테스트와 다른 선택임을 주석에 명기, 첫 빌드에서 실측 기록 — 오픈 이슈).
- 로그는 gitignored 경로(`game/Logs/build-windows.log` — `.gitignore:39` `game/[Ll]ogs/`)를 권장 예시로 문서화한다.

#### E2. 리포·설정 계약

- **`.gitignore` 변경 없음**: `game/[Bb]uild*/`(.gitignore:38)와 루트 방어선 `/[Bb]uild*/`(:75)이 이미 커버한다. 검증: `git check-ignore game/Build/Windows/ClientIsKing.exe` 성공 + 빌드 후 `git status --short` 무변화. `ProjectSettings/`·`Packages/`·`*.meta`는 계속 버전 관리(변경 금지 재확인).
- **PlayerSettings 무변경**: `companyName: DefaultCompany`·`productName: Client is King`(ProjectSettings.asset:15-16) 유지 — companyName 변경은 `persistentDataPath` 세이브 경로를 바꾼다(task-113 오픈 이슈). 유지 시 에디터와 플레이어가 같은 `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Client is King\save.json`을 공유한다(데모 단계 허용 — 오너 표기 결정은 오픈 이슈). 해상도/풀스크린 기본값도 무변경(PixelPerfectCamera 640×360 스케일링 전제).
- `BuildTarget` 스위치 최초 1회는 재임포트로 느릴 수 있다 — 구현 노트에 소요 시간·산출물 크기를 실측 기록한다.

### F. 테스트 계약 (파일별)

- `EndingOpsTests.cs` **신규**: (1) GetStatus 파생 — daysCompleted 0/N−1/N/N+1 × isBankrupt 조합(파산 우선), (2) IsCleared 원시형 동치, (3) BuildSummary 필드 전수(NetProfit = cash−StartingCash 음수 포함, FollowerDisplay가 `SNSCampaignOps.CalculateFollowerDisplay`와 일치), (4) `ClearTargetDays == 7` 핀, (5) null state 예외.
- `GameManagerEndingGateTests.cs` **신규**: 클리어 상태(fixture: day=N·settlementDay=N·daysCompleted=N)에서 (1) `CanAdvancePhase` false + 사유 정확 일치, (2) `AdvancePhase`가 현재 phase 반환·`DayPhaseChanged` 0회·상태 field-by-field 불변(파산 차단 테스트 미러 — FirstPlayableLoopTests:184-202 전례), (3) Day N Market/Service(daysCompleted=N−1)에서는 게이트 미발동, (4) Day N 정산 인라인 적용 경로가 Settlement에 머묾, (5) 파산 게이트 기존 동작 무회귀.
- `BalanceEndingGuardTests.cs` **신규** (B3 밴드 확정 조건의 기계화): (1) 전 장르 × N일 실루프 클리어 도달(조건 1 — FirstPlayableLoop 구매/서빙 패턴을 일반화, 이벤트 스케줄 포함, 결정론이므로 값 고정 가능), (2) 무위 플레이 Day ≤3 파산(조건 3), (3) 상수 동기 핀 — `EventOps.BaseDailyOperatingCost == SettlementOps.DailyOperatingCost`, `StartingCash == 30000`, 시드 운영비 값 핀(밴드 조정 시 함께 갱신), (4) 클리어 직후 상태가 저장·재로드(RT 왕복) 후에도 `GetStatus == Cleared`.
- `EndingOverlaySceneTests.cs` **신규**: D1 표 전수(존재·좌표·크기·초기 비활성·최상단 sibling index·dim raycastTarget true·색), `RefreshNow` 3분기(None 숨김/클리어/파산 — 문구·색), worst-case `GetPreferredValues` ≤460px, 버튼 리스너 배선.
- `BuildToolTests.cs` **신규**: `ScenePaths()` — 2개·[MainMenu, Shop] 순서·SceneBuilder 상수 일치, `OutputDir`/`PlayerExeName` 핀, Build Settings 변조 시 예외(가능 범위에서 — EditorBuildSettings 임시 변조 후 원복).
- `MainMenuSaveFlowTests.cs` **갱신**: 클리어 세이브 fixture → 이어하기 비활성 + D3 문구·Brass Amber. 기존 4분기 무회귀.
- `SceneBuilderTests.cs` **갱신**: 신규 오브젝트 존재·`Apply` 2회 멱등(오브젝트 수·listener) 유지·Build Settings 2씬 유지.
- B4 목록 **갱신**: GenreBalance/SNSBalance/EventBalance/EventOps/NightPanelEventFlow(+ SceneBuilder:710 플레이스홀더 중립화, 필요 시 fixture) — 락스텝 전용. whitelist 지점(`NightPanelSnsFlowTests`·`SNSCampaignOpsTests`·`SaveOpsTests`)은 diff 0.
- PlayMode `EndingPlayModeTests.cs` **신규(+1)**: 클리어 세이브 파일(경로 override — SaveLoad SetUpFixture 격리 전례) → `TryLoadGame` → Shop 로드 → 오버레이 활성·advance 비활성 → `LoadMainMenuScene` → MainMenu 클리어 분기 표시. 기존 8종 무회귀.
- 기존 회귀: Save/Service/Economy/Genre/SNS/Event/Presentation/PlaceholderArt 계열 전부 — B4 목록 외 **어떤 파일도 수정 없이** 통과(스키마 v1·수식·아트 무변경의 증거).

### G. 수용 기준 — 자동 검증과 오너/Codex 게이트의 분리

**자동(병합 전제)**: F절 전 테스트 + 컴파일 exit 0 + `SceneBuilder.Apply` 멱등 + `BuildTool.BuildWindows` 배치 exit 0·exe 존재·git 청정. **오너/Codex(done 전제, Claude self-approve 금지)**: ① 오너 플레이테스트 — 시드 28,000·N=7의 체감(숙련 클리어 가능/방만 파산 가능/이벤트·SNS 긴장) 승인 또는 밴드 내 보정 지시(1회 반영 후 재제출 — task-114 D3 전례), ② 640×360 원본+2× 캡처 3종(클리어 오버레이/게임오버 오버레이/MainMenu 클리어 분기) 시각·카피 승인, ③ 빌드 exe 더블클릭 스모크 — 새 게임→1일 루프→종료→이어하기 재개, ④ Codex 코드 리뷰(`kb/tasks/task-115/reviews/`).

### H. 상세 구현 순서

1. scaffold `manifest.md`의 placeholder를 이 문서의 Inputs·영향 파일·검증 명령으로 채운다. 구현 전 기준선(EditMode 439/PlayMode 8)을 실행해 실제 결과를 구현 노트에 기록한다.
2. **[G1]** `SettlementOps.DailyOperatingCost` 시드 적용 + `EventOps.BaseDailyOperatingCost` 상수 도입(리터럴 2곳 교체·주석 동기) + B4 락스텝 갱신. 운영비 문맥 리터럴의 심볼/기대값 교체와 whitelist 바이트 불변을 B4 기준으로 확인.
3. **[G1]** `EndingOps.cs`/`EndingSummary.cs` 작성(.meta 쌍), `GameManager` 게이트 2지점 + `LoadMainMenuScene` + 사유 문자열. `EndingOpsTests`/`GameManagerEndingGateTests` 작성.
4. **[G1]** `BalanceEndingGuardTests` 작성 → B3 확정 조건 4종 실측. 시드 28,000이 깨지면 밴드 내 최대 통과값으로 조정하고(테스트·상수·주석 동기) 구현 노트에 실측 표를 기록한다.
5. **[G2]** `EndingOverlayController` 작성, `MainMenuController` 클리어 분기(+Brass Amber 상수), `SceneBuilder.BuildShop`에 `BuildEndingOverlay` 추가(D1 표·sibling 순서·EditorInit 배선).
6. **[G2]** `BuildTool.cs` 작성(E1 계약). 배치 모드에서 `SceneBuilder.Apply` 2회 실행(멱등 확인) → 씬 2종 재생성.
7. **[G3]** `EndingOverlaySceneTests`/`BuildToolTests`/`MainMenuSaveFlowTests` 갱신분/`SceneBuilderTests` 갱신분 작성. EditMode 전체 실행 — 기준선 439 + 신규 전부 통과까지 수정.
8. **[G3]** PlayMode `EndingPlayModeTests` 작성 → PlayMode 전체(기존 8 + 1, `-quit` 없이) 통과.
9. **[G3]** `BuildTool.BuildWindows` 배치 실행(E1 명령) — exit 0, exe 존재, 로그에 경로·크기, `git status --short` 무변화, `git check-ignore` 통과. 소요 시간·크기 실측 기록.
10. **[G3]** `git status --short game`으로 변경이 영향 파일 표에 한정됨을 확인(세이브/빌드 산출물·Library 오염 0, `.meta` 쌍 완전).
11. G절 오너/Codex 게이트 제출물 준비: 캡처 3종(원본+2×), 플레이테스트 절차 안내(숙련 1런·방만 1런), 빌드 exe 경로.
12. `kb/tasks/task-115/implementation-notes.md`(시드 실측 표·게이트 결과·빌드 실측), `kb/artifacts/task-115-summary.md` 작성, `python3 runtime/generate-status.py` 재생성. 오너 승인 시 **M3 완료 = 데모 완료** 기록.

## 실행 계획 (Execution Plan)

- implement_model: claude-fable-5
- implement_effort: high
- routing_reason: SSOT 로드맵 v3가 task-115를 fable-5/high로 고정(러너 화이트리스트 `runtime/config/model-profiles.json`은 fable-5/opus-4-8만 허용) — 신규 수식 없이 상수 튜닝·파생 게이트·오버레이·빌드 스크립트를 기존 계약 위에 얹는 M3 마감 작업. 팀 병렬 실행 시 권장 분담은 M2 전례(3그룹)를 따른다: G1 도메인·밸런스(명세 충실 — claude-sonnet-5/high 또는 fable-5/high), G2 표현·빌드(claude-fable-5/high), G3 통합 검증(claude-sonnet-5/high 또는 fable-5/high — G1과 동일 모델, 작성/검증 레인 분리).

| unit | 파일 범위 | depends_on | group |
|------|-----------|------------|-------|
| U1-balance-constants | `SettlementOps.cs`(상수)·`EventOps.cs`(상수 도입)·`EventDayEffects.cs`(주석)·B4 락스텝 테스트 5+파일 갱신 | 없음 | G1 |
| U2-ending-domain | 신규 `Runtime/DayCycle/{EndingOps,EndingSummary}.cs`·`GameManager.cs`(게이트 2지점+LoadMainMenuScene)·`EndingOpsTests`·`GameManagerEndingGateTests` | 없음 | G1 |
| U3-balance-guards | `BalanceEndingGuardTests.cs`(실루프 클리어/무위 파산/상수 핀) + 시드 밴드 실측·확정 | U1-balance-constants, U2-ending-domain | G1 |
| U4-ending-ui | 신규 `Runtime/UI/EndingOverlayController.cs`·`MainMenuController.cs`(클리어 분기)·`SceneBuilder.cs`(오버레이)·씬 2종 재생성 | U2-ending-domain | G2 |
| U5-build-tool | 신규 `Editor/BuildTool.cs`·`BuildToolTests.cs`(순수부) | 없음 | G2 |
| U6-integration-validation | `EndingOverlaySceneTests`·`MainMenuSaveFlowTests`/`SceneBuilderTests` 갱신·PlayMode `EndingPlayModeTests`·배치 게이트 전체(컴파일→EditMode→PlayMode→BuildWindows)·G절 게이트 제출물·implementation-notes/summary/status | U3-balance-guards, U4-ending-ui, U5-build-tool | G3 |

## 파일/모듈 영향 (Affected Files/Modules)

| 파일/모듈 | 변경 유형 | 설명 |
|-----------|-----------|------|
| `game/Assets/Scripts/Runtime/Settlement/SettlementOps.cs` | modify | `DailyOperatingCost` 12,000 → 시드 28,000(밴드 [22,000,30,000] — B3)·주석 동기. 수식 무변경 |
| `game/Assets/Scripts/Runtime/Events/EventOps.cs` | modify | `BaseDailyOperatingCost` 상수 신설, :444/:508 리터럴 교체 — 공식·FNV 스케줄 바이트 불변 |
| `game/Assets/Scripts/Runtime/Events/EventDayEffects.cs` | modify | :86 doc 주석의 12000 표기 동기화(코드 무변경) |
| `game/Assets/Scripts/Runtime/DayCycle/EndingOps.cs` | create | 파생 엔딩 규칙 단일 원천 — `ClearTargetDays=7`·GetStatus/IsCleared/IsRunEnded/BuildSummary (순수) |
| `game/Assets/Scripts/Runtime/DayCycle/EndingSummary.cs` | create | 엔딩 오버레이 표시 전용 DTO (전 필드 파생) |
| `game/Assets/Scripts/Runtime/DayCycle/GameState.cs` | none | **필드 무추가 확인** — `SaveSchemaVersion` 1 유지, diff 0 (C1 결정의 증거) |
| `game/Assets/Scripts/Runtime/Managers/GameManager.cs` | modify | 클리어 게이트(CanAdvancePhase+AdvancePhase 2지점, 파산 미러)·`LoadMainMenuScene()` |
| `game/Assets/Scripts/Runtime/UI/EndingOverlayController.cs` | create | Canvas 탑재 상태 폴링 컨트롤러 — Render/RefreshNow/focus/메인 메뉴 버튼 |
| `game/Assets/Scripts/Runtime/UI/MainMenuController.cs` | modify | RefreshSaveUi 클리어 분기(4→5) + Brass Amber 상수 |
| `game/Assets/Scripts/Editor/SceneBuilder.cs` | modify | `BuildEndingOverlay`(D1 표) + BuildShop 배선(최상단 sibling)·멱등 유지 + :710 정산 플레이스홀더 `"운영비  -0원"` 중립화(B4). :763 `"숏핑 12,000원"` 은 SNS 비용 — 불변 |
| `game/Assets/Scripts/Editor/BuildTool.cs` | create | `BuildWindows`/`ScenePaths` — StandaloneWindows64 배치(비대화형) 빌드, 실패 시 예외 |
| `game/Assets/Scenes/{Shop,MainMenu}.unity` | modify | SceneBuilder 재생성 산출(엔딩 오버레이 — MainMenu는 기능 변경 없음) |
| `game/Assets/Data/Definitions/**` · `InitialDataBuilder.cs` | none | SO 시드 39종 **무변경 확인**(B3-4 결정의 증거 — diff 0) |
| `.gitignore` | none | `game/[Bb]uild*/`(:38) 기존 커버 확인만 — 변경 금지 |
| `game/ProjectSettings/ProjectSettings.asset` | none | companyName/productName 무변경(E2 결정 — 오픈 이슈) |
| `game/Assets/Tests/EditMode/{GenreBalance,SNSBalance,EventBalance,EventOps,NightPanelEventFlow}Tests.cs` | modify | B4 락스텝 — 운영비 파생 기대값만 갱신(발생 횟수·설계 기여이익·DesignNet 표 불변). whitelist(NightPanelSnsFlow/SNSCampaignOps/SaveOps Tests)는 diff 0 |
| `game/Assets/Tests/EditMode/{SettlementOps,FirstPlayableLoop}Tests.cs` · `PlayMode/SaveLoadPlayModeTests.cs` | modify(조건부) | 상징 참조 자동 추종 — fixture 지불능력·주석만 실측 확인 후 필요 시 갱신(B4) |
| `game/Assets/Tests/EditMode/{EndingOps,GameManagerEndingGate,BalanceEndingGuard,EndingOverlayScene,BuildTool}Tests.cs` | create | F절 신규 테스트 5파일 |
| `game/Assets/Tests/EditMode/{MainMenuSaveFlow,SceneBuilder}Tests.cs` | modify | 클리어 분기·오버레이 오브젝트·멱등 검증 추가 |
| `game/Assets/Tests/PlayMode/EndingPlayModeTests.cs` | create | 클리어 세이브 → 재개 → 오버레이 → MainMenu 통합 (+1) |
| 관련 `game/**/*.meta` | create | 신규 스크립트/테스트의 Unity 메타 쌍 (기존 에셋 `.meta` 삭제 0) |
| `kb/tasks/task-115/implementation-notes.md` | create | 시드 실측·밴드 확정 근거·빌드 실측·게이트 결과 |
| `kb/artifacts/task-115-summary.md` | create | 산출물 요약 (Status/Inputs/Outputs/Next step) |
| `kb/index/status.md` | modify | `runtime/generate-status.py` 재생성 |

## 테스트 기준 (Test Criteria)

- [ ] `python -B runtime/validator/cli.py kb/tasks/task-115/design.md`가 종료 코드 0으로 통과한다.
- [ ] 구현 전 기준선(EditMode 439/PlayMode 8)을 실행하고 실제 결과를 구현 노트에 기록한다.
- [ ] 상수 핀: `SettlementOps.DailyOperatingCost == EventOps.BaseDailyOperatingCost == 확정 시드`(기본 28,000 — 밴드 조정 시 테스트·구현 노트 동기), `GameState.StartingCash == 30000`, `EndingOps.ClearTargetDays == 7`, `GameState.SaveSchemaVersion == 1`(스키마 무변경).
- [ ] 운영비 문맥 리터럴 검증(B4 기준): EventOps.cs:444/:508 리터럴이 `BaseDailyOperatingCost` 상수로 교체되고, B4 표의 파생 기대값(NightPanelEventFlowTests `13,800`→`32,200`·SceneBuilder:710 중립화 포함)이 전부 갱신되었으며, B4 변경 금지 whitelist(SNS 숏폼 비용 4지점·SaveOpsTests 자기일관 fixture 4지점)는 바이트 불변이다 — `SaveOpsTests`·`SNSCampaignOpsTests` diff 0.
- [ ] B3 확정 조건 기계 가드(`BalanceEndingGuardTests`): 전 장르 N일 실루프(전량 서빙·C급·이벤트 포함 프로덕션 `AdvancePhase` 경로)가 파산 없이 Day 7 클리어에 도달하고, 무위 플레이(구매 0·전 주문 skip)가 Day 3 이내 파산하며, 전 장르 Day-1 이론 구매비 ≤ StartingCash(기존 가드)와 Day 1~3 순익 > 0(갱신된 GenreBalanceTests)이 성립한다.
- [ ] 밸런스 불변 증거: GenreBalanceTests 설계 기여이익 4종(±1%)·max/min ≤ 1.10, SNSBalanceTests DesignNet 12쌍, EventBalanceTests 발생 횟수(15/13/16/1=45)·단가 asserts가 **무변경으로** 통과한다(SO·수식 무변경). FNV known vector `"gukbap|1|0"→2190636514` 검증 무변경 통과.
- [ ] B4 락스텝: 열거된 파일의 운영비 파생 기대값만 갱신되고, `SaveOpsTests` 등 세이브 fixture·V-매트릭스 테스트는 diff 0으로 통과한다.
- [ ] `EndingOpsTests`: 파생 매트릭스(daysCompleted × isBankrupt — 파산 우선), Day N Market/Service 미발동(daysCompleted=N−1), BuildSummary 필드 전수(NetProfit 음수 포함, FollowerDisplay 일치), null 예외.
- [ ] `GameManagerEndingGateTests`: 클리어 상태에서 `CanAdvancePhase` false + 사유 `"데모 클리어 상태에서는 진행할 수 없습니다."` 정확 일치, `AdvancePhase` 현재 phase 유지·`DayPhaseChanged` 0회·상태 불변, Day N 정산 인라인 적용 후 Settlement 유지, 파산 게이트 무회귀.
- [ ] 저장 왕복: 클리어 상태가 저장→재로드 후에도 `GetStatus == Cleared`이고, 클리어 세이브가 V1~V11을 통과하며(정상 v1), `MainMenuSaveFlowTests`에서 이어하기 잠금 + D3 문구·Brass Amber로 표시된다. 기존 4분기 무회귀.
- [ ] `EndingOverlaySceneTests`: D1 표 전수(좌표·크기·색·raycast·초기 비활성·최상단 sibling), RefreshNow 3분기 문구·색, worst-case 폭 ≤460px, 버튼 → `LoadMainMenuScene` 배선.
- [ ] `SceneBuilder.Apply` 연속 2회가 오브젝트 수·persistent listener 기준 멱등이고 Build Settings 씬이 [MainMenu, Shop] 2개뿐이다.
- [ ] `BuildToolTests`: `ScenePaths()` 2개·순서·상수 일치, 산출 경로 핀. 배치 `BuildWindows` 실행이 exit 0, `game/Build/Windows/ClientIsKing.exe` 존재, 로그에 경로·크기 기록, 실패 주입(가능 범위) 시 비제로 exit.
- [ ] 빌드 산출물 git 청정: `git check-ignore game/Build/Windows/ClientIsKing.exe` 통과, 빌드 후 `git status --short` 무변화, `.gitignore`·`ProjectSettings`·`Packages` diff 0.
- [ ] Unity 배치 compile 종료 코드 0·`error CS` 0. EditMode 전체 종료 코드 0(기준선 439 + 신규, B4 명시 갱신 외 무회귀). PlayMode 전체 종료 코드 0(기존 8 + `EndingPlayModeTests` 1, `-quit` 없이).
- [ ] 도메인 무변경 증거: `GameState`·`SaveOps`·`ServiceOps`·`GenreSelectionOps`·`SNSCampaignOps`·`EventOps`(상수 외)·아트 관련 소스가 diff에 나타나지 않고, `Data/Definitions/**`·`Art/**` diff 0, `.meta` 추가는 신규 파일 쌍뿐이다.
- [ ] `git status --short game`에 `Library/Temp/Obj/Logs/UserSettings/Build*`·세이브 산출물이 없고 변경이 영향 파일 표에 한정된다.
- [ ] **오너 플레이테스트 게이트(Claude self-approve 금지)**: 숙련 1런(클리어 도달)·방만 1런(파산 체감)으로 시드 28,000·N=7을 승인하거나 밴드 내 보정을 1회 지시한다(보정 시 상수·테스트·노트 동기 갱신 후 재제출).
- [ ] **오너/Codex 시각 승인 게이트(Claude self-approve 금지)**: 640×360 원본+2× 캡처 3종(클리어/게임오버/MainMenu 클리어 분기)의 레이아웃·카피·색(문구+색 병용)을 승인한다.
- [ ] **오너 빌드 스모크(Claude self-approve 금지)**: 빌드 exe를 에디터 없이 실행 — 새 게임→1일 루프→종료→이어하기 재개→(치트 없이 또는 세이브 fixture로) 엔딩 확인.
- [ ] 구현 완료 후 `python runtime/validator/cli.py --check-done task-115`와 `python runtime/generate-status.py --check`가 통과하고, Codex 코드 리뷰(`reviews/001.md`)가 approved다.

## 오픈 이슈 (Open Issues)

- **밸런스 시드·목표일의 오너 게이트 (이 설계의 핵심 유보)**: `DailyOperatingCost 28,000`·`ClearTargetDays 7`은 B1 실측 위의 설계 시드다. 기계 가드(B3 조건 4종)는 도달 가능성만 보증하고 **재미(긴장 체감)는 오너 플레이테스트만이 판정**한다. 보정은 1회 한정(task-114 D3 전례), 그 이상 반복되면 상수 튜닝의 한계(SO 값·수식 영역)로 보고 별도 task로 승격한다(범위 변경 절차).
- **N 변경과 과거 세이브**: `ClearTargetDays`는 코드 상수라 세이브 비파괴지만, N을 낮추면 진행 중이던 세이브가 로드 즉시 클리어로 재해석될 수 있다(데모 단계 허용 — 출시 후 변경 시 v2 검토).
- **클리어 세이브 이어하기 잠금**: 파산 잠금(task-113 G2) 미러로 "기록 표시 + 새 게임 유도"를 기본값으로 한다. "이어하기로 엔딩 재관람" UX가 낫다는 판단이면 Codex 리뷰에서 뒤집을 수 있다(도메인은 로드 가능하므로 UI 분기만 바뀐다).
- **누적 통계 부재**: 총 서빙 손님 수·최고 일매출 같은 run 누적 통계는 현 스키마에서 파생 불가라 엔딩 요약에서 제외했다(C1의 대가). 도입하려면 task-113 정규형 계약상 schemaVersion v2 + 마이그레이션 + 왕복 테스트가 필요하다 — post-demo 오너 결정. run 시드 필드(task-112/113 이월)도 같은 이유로 데모 제외를 확정 기록한다.
- **미슐랭 트랙**: 데모 클리어는 "목표일 생존"으로 스코프했다(주차장 유지). 최종 컨셉의 미슐랭 엔딩은 M3 완료 후 확장 설계에서 커리어/평판 데이터(v2)와 함께 다룬다.
- **companyName `DefaultCompany`**: 유지 결정(E2 — 세이브 경로 보존). 공개 데모 배포 표기로 부적절하다고 오너가 판단하면 변경하되, 기존 플레이테스트 세이브가 고아가 됨(이전 없음)을 감수한다 — task-113 오픈 이슈의 종결 지점.
- **빌드 세부 품질**: 아이콘·서명·압축·IL2CPP·창 모드/해상도 기본값·인스톨러는 범위 외(CC0 데모, Mono 기본). `-nographics` 미사용 결정과 빌드 소요 시간·산출물 크기는 첫 빌드에서 실측해 구현 노트에 기록하고, 문제 시(셰이더/URP 이슈) 커맨드 라인 옵션 조정만 허용한다.
- **`OnApplicationQuit` 저장 재검토 종결**: task-113이 task-115로 미룬 항목 — 엔딩 시점은 정산 트리거 3이 저장을 보증하므로 quit 저장은 도입하지 않는다. 오너 smoke에서 이정표 간 손실(1~2분)이 불편하다는 판정이 나오면 별도 미니 task로 뺀다.
- **사운드 이월(task-114 인계)**: CC0 효과음 3종(구매/서빙/정산)은 이번에도 미포함. 데모 완료 선언 전 마지막 결정 기회다 — 포함하려면 별도 미니 task(에셋 조달·라이선스 기록) 승인이 필요하다.
- **NYC 아트 오버홀 미결(task-114 인계)**: 미결정 상태로 빌드하면 데모는 에도풍 CC0+F2 보정본으로 출시된다 — **BuildWindows 최종 실행 전** 오너 확인이 필요하다.
