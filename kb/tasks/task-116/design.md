# 설계 문서 — task-116: 현대 NYC 코리아타운 아트 오버홀 (승인 콘셉트 → 런타임 적용 계약)

> Status: ready
> Inputs: `kb/concepts/art-originals/PROVENANCE.md`(6종 md5 핀 포함 — 2026-07-12 추가) + 콘셉트 시트 6종(오너 승인 2026-07-12 — Visual North Star·protagonist·customers·food·ui-icons·foodtruck-environment, 전부 1536~1672px RGB 무알파 일러스트), **오너 결정: B절 A안(타깃 해상도 재생산) 채택 + Codex 설계 교차검토 승인(규격 E·알파/프레임 F 적합 — 2026-07-12)**, `kb/concepts/project-brief.md`(SSOT — 로드맵 v3은 task-115에서 종료, 아트 하드캡 "CC0/OFL 플레이스홀더만"), `kb/concepts/demo-scope.md`(하드캡 + 범위 변경 절차), `kb/concepts/art-direction.md`(현 CC0 디렉션 SSOT — 본 task가 v2로 개정 대상), `kb/tasks/task-114/design.md`(자동 검증/시각 승인 분리 전례 + NYC 오버홀 이관 오픈 이슈), `game/Assets/Scripts/Editor/PlaceholderArtBuilder.cs`(파생 파이프라인 + `AllSpritePaths()` 28종 계약), `game/Assets/Scripts/Editor/SceneBuilder.cs`(무대/카탈로그 배선 — CustomerSprite 64×64·FoodIcon 64×64·장르 Icon 32×32·Stage_Backdrop 640×160·Stage_Counter 320×32), 현 기준선 EditMode **494**/PlayMode **9**(task-113 리뷰 반영 후 — commit ff0077b), **[입력 게이트 — U2 전제]** 타깃 해상도 NYC 스프라이트 패키지 배치 1(32파일 — E/F절 확정 규격·스펙대로 오너/Codex 생산)
> Outputs: task-116 구현 계약 — 생산 방식 확정(A안 — 3안 비교는 결정 기록으로 보존), AI 아트 정책 + NYC provenance 형식, 최종 파일 경로 + Asset Map, 카테고리별 픽셀 규격(확정), 투명 배경·프레임·레이어 규칙(입력 패키지 스펙, 확정), PlaceholderArtBuilder 유지/병행 결정, Unity 임포트 + 회귀 테스트 기준, SSOT/하드캡 개정 확정 문구
> Next step: Claude가 U0(SSOT 개정 커밋)→U1(계약 코드) 순으로 구현에 착수한다. U2는 Codex가 2026-07-12 확정 결정과 장르 ID `noodles`를 이 설계에 기록하고, 오너/Codex가 파일럿 9파일 승인 후 최종 32파일 전부를 전달·합격시킨 뒤에만 시작한다. 시각 승인 게이트(H절) 통과 후 Codex 코드리뷰로 마감한다.

## 목표 (Objective)

오너가 2026-07-12 최종 아트 방향으로 승인한 Codex Visual North Star(현대 뉴욕 코리아타운 푸드트럭) 콘셉트 6종을 **런타임 스프라이트로 적용하기 위한 엔지니어링 + 정책 계약**을 확정한다. 이 설계는 비주얼 아트를 새로 만들거나 고치지 않는다 — 승인된 콘셉트(캐릭터 외형·아이콘 모양·색·레이아웃)는 고정이며, 이 문서가 정하는 것은 **파이프라인·규격·경로·정책·테스트 기준**이다. 콘셉트 시트는 1536~1672px RGB(무알파) 일러스트이고 런타임은 32px급 RGBA 픽셀 스프라이트이므로, 그 간극을 어떻게 메울지(생산 방식)가 이 설계의 중심 질문이었고 — **오너가 A안(타깃 해상도 재생산)으로 확정했다(2026-07-12, B절)**. 모든 주관적 아트/정책 판단의 결정권은 오너 + Codex에게 있고, 이 문서는 확정된 결정을 계약으로 고정하며 잔여 판단을 라우팅한다.

### 역할과 결정권

| 영역 | 결정권자 | 수행자 | 게이트 |
|------|----------|--------|--------|
| 일러스트→스프라이트 생산 방식 (B절 3안) | **오너 + Codex** | 오너/Codex 생산 + Claude 검증 (A안) | **확정: A안 (오너 2026-07-12 — 오픈 이슈 1 해소)** |
| 픽셀 규격·Asset Map·경로·네이밍 | 이 설계(제안) → **Codex 교차검토, 오너 최종 승인** | Claude 구현 | design validator + 자동 테스트 |
| 스프라이트 픽셀 내용(외형·색·프레임) | **승인 콘셉트가 고정** — 재디자인 금지 | 오너/Codex (아트 생산) | 입력 패키지 자체가 산출물 |
| AI 아트 정책·공개 표기·SSOT 개정 문구 | **오너** (Codex 자문) | Claude가 문서 반영 | 2026-07-12 확정 문구만 사용 |
| 임포트·검증 코드·테스트·씬 배선 | 설계 계약 내 Claude | Claude | 컴파일·EditMode·PlayMode·멱등 게이트 |
| 최종 화면 판정(식별성·톤) | **오너/Codex** (Claude self-approve 금지) | Claude가 640×360 캡처 제공 | task-114 D3 전례의 시각 승인 게이트 |
| 구현 중 발생하는 모든 디자인 판단 | **Codex 검수** — Claude 추측 금지 | Claude가 질문 기록 | implementation-notes + 리뷰 루프 |

## 범위 (Scope)

### 범위 변경 선언 (demo-scope 절차 이행)

현행 하드캡 "아트 = CC0/OFL 플레이스홀더만 · 유료/커스텀 아트 금지"는 본 task와 충돌한다. task-114 오픈 이슈가 명시 이관한 "현대 NYC 오버홀 = 별도 오너 결정 + SSOT 개정" 경로가 **2026-07-12 오너 승인으로 개시**되었으므로, demo-scope의 범위 변경 절차(오너 승인 → project-brief SSOT 선갱신 → demo-scope 갱신 → 신규 task 설계)에 따라 다음 개정을 이 task의 U0으로 수행한다(**아래 문구는 오너 승인(2026-07-12) 확정본** — U0에서 그대로 커밋한다):

- `project-brief.md`: 로드맵에 task-116 행 추가 — `| task-116 | NYC 코리아타운 아트 오버홀 — 승인 콘셉트 런타임 적용 | M4 (데모 후) | claude-fable-5 | high |` + 로드맵 v4 주석("오너가 Codex Visual North Star를 최종 아트 방향으로 승인, NYC 오버홀을 task-116으로 승격 — 2026-07-12"). 하드캡 문구를 아래 demo-scope 개정안과 동기화.
- `demo-scope.md` 하드캡 개정안: `아트 | CC0/OFL 플레이스홀더 + 오너 승인 AI 생성 아트(task-116 계약 내, provenance 기록 필수) | 유료 외주·스톡 구매 금지`.
- `art-direction.md` v2: 현 CC0 combo 1 디렉션을 "프로토타입(데모 출시본)"으로 재분류하고 NYC 콘셉트를 최종 디렉션으로 기록(개정 본문은 오너/Codex 승인 후).

### 포함 — task-116 구현 범위 (배치 1: 기존 표현 슬롯의 정체성 교체)

- **확정된 A안(타깃 해상도 재생산)의 이행** (B절 — 오너 채택 2026-07-12. C~H절 전부 A안 확정 기준).
- **AI 아트 정책 + provenance 체계**: `game/Assets/Art/NYC/NYC-ART-PROVENANCE.md` 신설(C절 형식), `kb/concepts/art-originals/`는 콘셉트 원본 아카이브로 **바이트 불변 보존**.
- **최종 파일 경로 + Asset Map**: `game/Assets/Art/NYC/` 신설(D절) — 기존 `Placeholders/` 경로는 건드리지 않는다.
- **기존 런타임 슬롯 32종의 NYC 교체**: 손님 4종 × (idle 1 + walk 4) = 20, 음식 아이콘 6, 장르 UI 아이콘 4(현재 FoodIcons 재활용 중인 장르 modal Icon 슬롯을 전용 아이콘으로 승격 — 신규 UI 오브젝트 없음), 무대 배경/카운터 2.
- **임포트·검증 코드**: `NycArtContract.cs`(경로 상수·Asset Map·임포트 표준 적용) + `NycArtTests.cs`(존재·규격·알파·픽셀 아트 계약·provenance — H절).
- **SceneBuilder 스프라이트 소스 전환**: `PlaceholderArtBuilder` 경로 → `NycArtContract` 경로 (단일 커밋 단위, 롤백 용이). 씬 2종 재생성.
- **기록**: `implementation-notes.md`, `kb/artifacts/task-116-summary.md`, `runtime/generate-status.py` 재생성.

### 제외 — task-116에서 하지 않음

- **콘셉트 시트의 자동 축소·임의 크롭으로 런타임 스프라이트 생성** — 오너 금지 사항. 콘셉트 파일은 어떤 형태로도 런타임 에셋으로 변환·임포트하지 않는다.
- **콘셉트 재디자인** — 캐릭터 외형·아이콘 모양·색·레이아웃 변경 일절 금지. 이 설계의 규격은 "무엇을 그릴지"가 아니라 "어떤 그릇에 담을지"만 정한다.
- **task-114 재개봉** — task-114는 CC0/OFL 전용으로 완료·커밋됨. `Placeholders/**`·`OpenSource/**`·`PlaceholderArtBuilder.cs`·`PlaceholderArtTests.cs`는 **무수정 보존**(G절).
- **신규 표현 슬롯 (배치 2 — 별도 결정)**: 주인공 스프라이트(현 런타임에 슬롯 없음), 시스템 아이콘 8종 UI 배선(현 UI는 텍스트 기반), 손님 표정 프레임, 증기 이펙트, 트럭 독립 소품화. 콘셉트에는 존재하지만 런타임 슬롯 신설 = 표현 시스템 확장이므로 오픈 이슈 4로 라우팅(승인 시 task-117).
- 도메인·수치·저장 스키마·엔딩·결정론 변경 전부. 씬 추가, 신규 매니저/컨트롤러, Animator/Tilemap/외부 라이브러리. 사운드. Windows 재빌드 여부는 오너 결정(오픈 이슈 8).

## 제약 (Constraints)

- **콘셉트 고정**: 승인 콘셉트 6종이 비주얼의 단일 원천이다. 입력 패키지(스프라이트)가 콘셉트와 다르게 보이는지의 판정은 오너/Codex 몫이며 Claude는 판정하지 않는다.
- **콘셉트 원본 불변**: `kb/concepts/art-originals/**` 바이트 불변(테스트로 고정 — H절). 리사이즈·크롭·색보정·알파 처리 등 어떤 파생도 이 경로에서 만들지 않는다.
- **결정 라우팅**: 구현 중 디자인 판단이 필요하면 추측하지 않고 Codex 검수로 돌린다(PROVENANCE.md 오너 제약). 시각 판정은 task-114 D3 전례대로 640×360 원본+2× 캡처 제출 방식, Claude self-approve 금지.
- **기준선 보존**: EditMode 494 / PlayMode 9 무회귀(이 설계가 명시 갱신하는 기대값 — U3의 스프라이트 소스 경로 — 제외). 도메인/저장/밸런스/엔딩 코드·테스트는 diff에 나타나지 않아야 한다(아트는 에디터+에셋 전용).
- **픽셀 표준 불변**: PPU 32 · Point · 무압축 · mipmap off · Pixel Perfect 640×360 · 씬 2개(`MainMenu`+`Shop`) · 정수배 표시 원칙. 기존 rect(CustomerSprite 64×64 등)는 배치 1에서 변경하지 않는다 — 규격(E절)이 rect의 정수 약수가 되도록 제안했다.
- **`.meta` 쌍 규칙**: 신규 에셋마다 `.meta` 동반 커밋. `Placeholders/**`·`OpenSource/**`에는 `.meta` 포함 어떤 변경도 없어야 한다(변경 발생 = 이상 신호, 테스트 기준).
- **입력 게이트**: 채택 방식이 요구하는 아트 에셋(타깃 해상도 NYC 스프라이트)은 **오너/Codex가 제공하는 입력**이다. 입력이 F절 스펙을 통과하지 못하면 Claude는 수정 생성하지 않고 반려 사유를 기록해 재생산을 요청한다.
- **Claude 아트 작업 금지**: Claude는 NYC 배치 1 아트를 생성·리터칭·리사이즈·크롭·재디자인하지 않는다. Claude의 범위는 기계 검증·임포트·씬 연결·테스트이며, 640×360 시각 결과도 self-approve하지 않는다.
- **리포 public 유의**: 커밋 = 공개 게시다. AI 아트 자산·provenance·표기 문구는 C절의 오너 확정 정책과 공개문을 그대로 사용하며 CC0로 표기하지 않는다.
- 결정론 훼손 위험이 있는 부동소수 이미지 변환(리샘플링·HSV·디더링)은 파이프라인에 도입하지 않는다(task-114 제약 승계). RGB 산출물이 발생하면 F3의 exact-match 배경 키잉은 오너/Codex 생산 단계에서만 적용하고, Claude는 최종 RGBA PNG 검증만 수행한다.

## 구현 단계 (Implementation Steps)

### A. 중심 문제 — 콘셉트 시트에서 런타임 픽셀 스프라이트는 어떻게 만들어지는가

콘셉트 6종은 1536~1672px RGB 일러스트다. 픽셀 아트풍이지만 **픽셀 그리드에 정렬되어 있지 않고**(AI 생성 유사 픽셀 텍스처 — 셀 크기 불균일·경계 블렌딩), 알파 채널이 없으며, 걷기 애니메이션은 아키타입당 1포즈뿐이다(런타임은 4프레임 필요). 런타임 표적은 16~32px RGBA·Point 필터·정수배 표시다. 자동 축소는 오너가 금지했고, 기술적으로도 그리드 비정렬 일러스트의 다운스케일은 정체성을 파괴한다(어떤 보간이든 색 혼합 → 픽셀 아트 성립 불가; nearest-neighbor는 임의 픽셀 탈락). 아래 3안을 비교했고, **오너가 A안을 채택했다(2026-07-12, Codex 교차검토 승인 — 오픈 이슈 1 해소). 이하 C~H절은 A안 확정 기준이며, B/C안 비교는 결정 기록으로 보존한다.**

#### B. 생산 방식 3안 비교 (결정 기록 — A안 확정)

| | A안 — 타깃 해상도 재생산 (**확정**) | B안 — 에셋별 수동 추출·축소 (기각) | C안 — 현 실루엣 + NYC 팔레트 하이브리드 (기각) |
|---|---|---|---|
| 방법 | 콘셉트 시트는 **레퍼런스 성경**으로만 사용. 오너/Codex(이미지 생성 또는 작가)가 E절 규격·F절 스펙대로 **스프라이트 해상도에서 에셋을 직접 생산**해 입력 패키지로 전달. Claude는 임포트·Asset Map·배선·테스트만 구현 | 콘셉트에서 에셋별로 영역을 수동 선별·정리 후 다운스케일. **매 에셋 오너 승인 게이트** | 현 CC0 파생 스프라이트의 실루엣·프레임을 유지하고, `PlaceholderArtBuilder` 전례의 결정론 팔레트 스왑을 NYC 팔레트(Visual North Star 팔레트 칩)로 재조정한 신규 빌더 적용 |
| 콘셉트 충실도 | **높음** — 같은 생산자(Codex 이미지 생성)가 같은 정체성을 타깃 해상도로 재해석. 걷기 4프레임·알파 등 콘셉트에 없는 요소도 스펙대로 생산 가능 | 낮음~중 — 축소 과정에서 정체성 훼손 필연(그리드 비정렬), 오너 금지("자동 축소") 취지와 충돌 소지. 걷기 4프레임은 어차피 별도 생산 필요 | **낮음** — NYC "무드"만 이식되고 승인된 캐릭터·음식·아이콘 정체성은 미적용. 오버홀이 아님 |
| 오너/Codex 부담 | 중 — 규격 확정 + 배치 1 생산(32파일) + 반려 시 재생산 | **높음** — 32에셋 × 개별 게이트, 가장 느림 | 낮음 — 팔레트 승인만 |
| Claude 범위 | 임포트/검증/배선/테스트 (결정론 영역만 — 역할 분리 최적) | 동일 + 반려 루프 다수 예상 | 빌더 구현까지 Claude (단, 색 선정은 Codex) |
| 리스크 | 입력 패키지 품질 편차 → F절 기계 검증 + 시각 게이트로 흡수 | 일정 폭발·품질 하한 미달 | 오너 기대(승인 콘셉트 적용)와 산출물 불일치 |

**확정: A안 (오너 2026-07-12).** 근거 — (1) 오너 금지 사항(자동 축소·임의 크롭)과 충돌하는 유일한 경로가 원천 차단된다. (2) 픽셀 아트는 타깃 해상도에서 저작해야 성립한다는 기술적 사실과 일치한다. (3) 주관적 산출(아트)은 오너/Codex, 결정론적 산출(파이프라인)은 Claude라는 이 프로젝트의 역할 분리와 정확히 겹친다. (4) 콘셉트를 생산한 동일 계보(Codex 이미지 생성)가 스프라이트 해상도 재생산도 담당하므로 정체성 연속성이 가장 높다. B안(정체성 훼손 필연 + 일정 폭발)·C안(승인 콘셉트 미적용 — 오버홀 목표 미충족)은 기각되었다.

### C. AI 아트 사용 정책 + provenance (계약 — 오픈 이슈 5 확정·닫힘, 2026-07-12)

- **승인 기록**: 승인자 Project Owner, 승인일 2026-07-12. AI 생성 아트는 오너가 방향을 승인한 콘셉트 계보(Visual North Star 6종)에서 파생·재생산된 것만, task-116 계약 하에서만 허용한다. 계보 밖 신규 AI 생성(새 캐릭터·새 아이콘)은 오너 사전 승인 없이 금지.
- **라이선스/권리 정책**: 코드에는 MIT 라이선스를 적용하되 프로젝트 고유 아트는 별도 라이선스로 둔다. `kb/concepts/art-originals/**`와 `game/Assets/Art/NYC/**`는 리포 MIT 적용 대상에서 제외한다. 별도 재사용·재배포·2차 저작물 작성 허가는 부여하지 않는다. AI 요소의 저작권 성립·보호 범위는 관할권별로 다를 수 있으며 독점성·고유성·비침해성은 보증하지 않는다. **CC0 표기 금지**.
- **기록 체계 (2층)**:
  - 콘셉트 원본: `kb/concepts/art-originals/` + `PROVENANCE.md` (현행 유지 — exec-id·md5·바이트 동일 백업. 본 task에서 불변).
  - 런타임 자산: `game/Assets/Art/NYC/NYC-ART-PROVENANCE.md` 신설. 파일별 필수 필드 — 파일명·규격·생성 도구(모델)·생성일·참조 콘셉트 시트(계보)·후처리 내역(원칙 "없음"; 오너/Codex 생산 단계에서 키잉을 적용했다면 배경색 hex 기록)·오너 승인일. 이 문서의 존재·전 파일 커버리지를 테스트가 고정한다(H절).
- **표시 위치**: `README.md` + `README.en.md`, `game/Assets/Art/NYC/NYC-ART-PROVENANCE.md`, `game/Assets/StreamingAssets/AI-ART-NOTICE.txt`, Steam Content Survey = `Pre-Generated AI`. 인게임 Credits UI는 공개 출시 전 필수인 별도 release task로 처리한다.
- **공개문 [KO]**: "Client is King의 NYC 코리아타운 배경, 캐릭터, 음식 및 UI 아이콘 일부는 프로젝트 오너가 승인한 비주얼 콘셉트를 바탕으로 OpenAI의 Codex 내 이미지 생성 도구를 사용해 사전 생성하고, 프로젝트 팀이 방향을 정하고 선택·검수·통합한 AI 보조 아트입니다. 정확한 백엔드 모델 식별자는 도구에서 노출되지 않아 추정하여 표기하지 않습니다. 게임 실행 중에는 생성형 AI 또는 외부 AI 서비스를 사용하지 않습니다."
- **공개문 [EN]**: "Some NYC Koreatown backgrounds, characters, food art, and UI icons in Client is King are pre-generated, AI-assisted artwork created with OpenAI image generation through Codex from the owner-approved visual concepts, then directed, selected, reviewed, and integrated by the project team. The exact backend model identifier is not exposed by the tool and is therefore not guessed. No generative AI or external AI service runs while the game is being played."
- **CC0 자산과의 구획**: 기존 `PLACEHOLDER-PROVENANCE.md`(CC0 계보)와 NYC provenance(AI 계보)는 파일·디렉터리 수준에서 분리한다 — 라이선스 체계가 다른 자산을 한 문서에 섞지 않는다.

### D. 최종 파일 경로 + Asset Map (확정 — Codex 교차검토 승인)

배치 1 = 32파일. 네이밍은 기존 catalog id를 그대로 써서 콘셉트↔런타임↔코드의 1:1 대응을 만든다.

```
game/Assets/Art/NYC/
  NYC-ART-PROVENANCE.md
  Customers/{student, office_worker, family_parent, senior_regular}.png          (idle 4)
  Customers/{...}_walk0..3.png                                                   (걷기 16)
  FoodIcons/{pork_gukbap, beef_gukbap, janchi_guksu, bibim_guksu,
             tteokbokki, gimbap}.png                                             (6)
  UiIcons/genre_{gukbap, bunsik, noodles, generalist}.png                        (4)
  Stage/{backdrop, counter}.png                                                  (2)
```

장르 ID 주의: 실제 도메인 장르 ID는 `noodles`이며 대응 파일명은 `genre_noodles.png`다. 단수형 ID/파일명은 오류이므로 Claude는 `NycArtContract`·규격서·테스트 전부를 `noodles` 및 `genre_noodles.png`로 통일한다.

콘셉트 → 런타임 대응 (정체성 참조 — 좌표 추출이 아니라 "무엇을 그릴지"의 원천 지정):

| 런타임 자산 | 원천 콘셉트 | 정체성 (고정) | 비고 |
|-------------|-------------|----------------|------|
| Customers/student* | customers.png 1열 | 백팩+후디 청년 | 걷기 4프레임은 콘셉트 1포즈를 기준으로 신규 생산 |
| Customers/office_worker* | customers.png 2열 | 브라운 코트+토트백 직장인 | 〃 |
| Customers/family_parent* | customers.png 3열 | 그린 패딩 부모 + 옐로 패딩 아이 페어 | 캔버스 32×32 확정 — 부모+아이 페어를 우선 시도하고, 1× 식별 불가할 때만 오너/Codex 재승인 후 부모 단독으로 전환(이슈 2 콘텐츠 확정) |
| Customers/senior_regular* | customers.png 4열 | 헌팅캡+브라운 재킷 노신사 | 〃 |
| FoodIcons/pork_gukbap 등 6종 | food.png 대형 6종 | 뽀얀국밥/얼큰국밥/떡볶이/김밥/잔치국수/비빔국수 | 소형 변형은 배치 1 미사용 |
| UiIcons/genre_* | ui-icons.png 장르 4종 | 국그릇=`gukbap` · 꼬치=`bunsik` · 면기=`noodles` · 도시락=`generalist` | 버튼 프레임 없이 투명 배경 심볼만 사용(이슈 9 확정) |
| Stage/backdrop | foodtruck-environment.png 상단 씬 | `backdrop.png` 640×160 야간 벽돌 거리 + 트럭 단판 합성 | 타일 아님(이슈 6 확정) |
| Stage/counter | foodtruck-environment.png 트럭 카운터부 | `counter.png` 320×32 트럭 서빙 카운터 단판 | 타일 아님(이슈 6 확정) |

미사용 콘셉트 요소(주인공·시스템 아이콘 8종·표정·증기·개별 소품·소형 음식)는 배치 2 후보로 오픈 이슈 4에 기록만 한다.

### E. 카테고리별 픽셀 규격 (**확정** — Codex 적합 판정 + 오너 승인 2026-07-12. 근거: 현 rect의 정수 약수)

| 카테고리 | 현행 | 확정 | 표시 rect (불변) | 배율 | 근거 |
|----------|------|------|------------------|------|------|
| 손님 idle+walk ×4종 | 16×16 | **32×32** | CustomerSprite 64×64 | ×2 | NYC 콘셉트의 의상 디테일(백팩·코트·비니)이 16×16에서 소실. 32×32는 rect 정수 약수라 씬 무변경 |
| 음식 아이콘 6종 | 32×32 | **32×32 유지** | FoodIcon 64×64 | ×2 | task-114 캔버스 표준과 동일 — 소품 3종(32×32 rect ×1)도 자동 정합 |
| 장르 UI 아이콘 4종 | (FoodIcons 재활용) | **32×32** | 장르 버튼 Icon 32×32 | ×1 | 전용 슬롯 승격, rect 무변경 |
| 무대 배경 | 16×16 타일 반복 | **640×160 단판 1장** | Stage_Backdrop 640×160 | ×1 | 콘셉트의 거리+트럭 합성 장면은 타일 반복으로 재현 불가. Image type Tiled→Simple 전환(SceneBuilder 1행) |
| 무대 카운터 | 16×16 타일 반복 | **320×32 단판 1장** | Stage_Counter 320×32 | ×1 | 트럭 카운터 띠 — 〃 |

- 기각된 대안(결정 기록): 손님 16×16 유지(×4 표시 — 디테일 손실)와 family_parent 페어의 48×32 캔버스(rect 조정 = 유일한 씬 변경 유발 지점)는 기각. **family_parent는 캔버스 32×32로 락**(rect·씬 무변경) — 부모+아이 페어를 우선 시도하고, 1× 식별 불가할 때만 오너/Codex 재승인 후 부모 단독으로 전환한다. 이는 32×32 캔버스 안의 **아트 콘텐츠 결정**이지 씬/규격 결정이 아니다(Claude는 콘텐츠를 결정하지 않는다). 이로써 오픈 이슈 2 해소.
- 위 확정 규격이 곧 **입력 패키지의 납품 규격**이다 — 배치 1 생산은 이 표를 계약으로 삼는다.

### F. 투명 배경·스프라이트 프레임·레이어 분리 규칙 (입력 패키지 스펙 — **Codex 적합 판정으로 확정**, 2026-07-12)

1. **알파 (확정)**: 모든 스프라이트는 PNG **RGBA + 실제 투명 배경**으로 납품한다(콘셉트의 크림색 배경 금지). 픽셀 아트 경계 선명도를 위해 alpha ∈ {0, 255} 이진(반투명 금지)을 적용한다. 예외: 무대 backdrop은 완전 불투명 허용. (오픈 이슈 3 해소)
2. **프레임 전달 형식 (확정)**: **개별 PNG 파일(프레임당 1파일)** — 슬라이싱 단계 자체를 제거해 "임의 크롭" 리스크를 0으로 만들고, 현 catalog 구조(`walkFrames` 배열에 개별 Sprite 주입)와 직결된다. 대안(수평 스트립 시트 + 균일 셀 그리드 계약)은 기각 — 슬라이싱 검증이 추가된다. (오픈 이슈 7 해소)
3. **RGB 산출 시 처리(키잉 — 생산 측 한정)**: 최종 납품 PNG는 H절 검증을 위해 반드시 IHDR color type 6(실 RGBA)이어야 한다. 생산 도구가 RGB만 산출하면 오너/Codex가 최종 32파일 전달 전에 지정 단일 배경색 exact-match 키잉(해당 RGB만 alpha 0, 그 외 무변경)을 적용한 RGBA PNG로 재납품한다. Claude는 RGB 파일을 U2 자산으로 수입하지 않고 반려 사유를 보고한다. 배경색·적용 파일은 provenance에 기록한다. (오픈 이슈 3 해소)
4. **레이어 분리**: 캐릭터·음식·아이콘·배경·카운터는 각각 독립 파일(상호 합성 금지 — backdrop만 거리+트럭 합성 단판). 캐릭터 발밑 그림자는 캐릭터 파일에 포함 가능(현행 CC0 파생과 동일)하되 배경 요소 포함 금지. 장르 UI 아이콘은 버튼 프레임 없이 투명 배경 심볼만 납품한다.
5. **걷기 프레임 규약**: 우향 4프레임(0..3 순환), idle 1프레임 별도. 좌향은 런타임 `localScale.x = -1` horizontal flip을 허용하며 **좌우 비대칭 디테일(백팩·토트백 등)이 반전되어 보이는 것까지 수용**한다(오픈 이슈 10 확정). 프레임 간 캔버스 크기·발 기준선 고정(점프 방지).
6. **픽셀 아트 계약(기계 검증 가능 조건 — 확정)**: 규격 정확 일치 · 알파 규칙(1항) · 불투명 고유색 상한 — 손님 ≤24 / 음식 ≤32 / 장르 아이콘 ≤12 / backdrop·counter ≤64 (Codex 적합 판정 포함 — 이 확정치를 테스트가 고정한다).

### G. PlaceholderArtBuilder 유지/교체 범위 — **유지 + 병행, 전환은 SceneBuilder 1점** (확정)

- `PlaceholderArtBuilder.cs` · `Placeholders/**`(파생 28종) · `OpenSource/**` · `PLACEHOLDER-PROVENANCE.md` · `PlaceholderArtTests.cs`는 **전부 무수정 보존**한다. `AllSpritePaths()` 28종 계약과 task-114의 리컬러 검증은 그대로 green으로 남는다(기준선 보존의 최단 경로).
- 신규 `NycArtContract.cs`(에디터)가 NYC 자산의 경로 상수·32파일 목록·임포트 표준 적용(`ApplyImportSettings` 패턴 재사용)을 소유한다. **생성 빌더가 아니라 검증·임포트 계약**이다 — A안에서 픽셀을 만드는 것은 입력 패키지이지 코드가 아니며, Claude는 F3 키잉도 수행하지 않는다.
- 전환은 `SceneBuilder`의 스프라이트 로드 경로(무대 타일·손님/음식 catalog·장르 Icon·소품)를 `NycArtContract` 상수로 바꾸는 **단일 커밋(U3)** 으로 수행한다. 반려·회귀 시 이 커밋 하나를 되돌리면 CC0 화면으로 즉시 복귀한다.
- 기각된 대안(결정 기록): (i) `Placeholders/` 경로에 in-place 덮어쓰기 — 씬 무변경이지만 CC0 provenance와 AI 자산이 섞이고 task-114 검증이 깨진다(기각). (ii) Placeholder 세트 제거 — 롤백 불능 + 기준선 파괴(기각). 최종 처분(데모 출시본에 어느 세트를 넣는지)은 오픈 이슈 8.

### H. Unity 임포트 + 회귀 테스트 기준

- **임포트 표준(불변 승계)**: Sprite · Single · PPU 32 · Point · 무압축 · mipmap off · alphaIsTransparency. 슬라이싱 없음(F2 개별 파일 채택 시 — Multiple 모드 금지).
- **신규 `NycArtTests.cs` (EditMode — U2에서 자산과 함께 커밋, 커밋 상태에서 항상 green)**:
  1. 32파일 전수 존재 + `.meta` 쌍 존재.
  2. 규격 정확 일치(E절 확정치) — 파일별 width×height.
  3. 알파 규칙: 배경 투명(네 모서리 alpha 0 — backdrop 예외), 이진 알파(채택 시 전 픽셀 a ∈ {0,255}).
  4. 원본 PNG RGBA 검증: Unity `Texture2D` 로드 결과뿐 아니라 원본 PNG 바이트를 직접 읽어 IHDR color type == 6(실 RGBA)인지 검증한다.
  5. 픽셀 아트 계약: 불투명 고유색 상한(F6 확정치).
  6. 임포트 표준 전수.
  7. provenance: `NYC-ART-PROVENANCE.md` 존재 + 32파일 전수 언급 + 필수 필드(C절).
  8. 원본 보존: `kb/concepts/art-originals/` 6종 md5가 PROVENANCE.md의 md5 핀(2026-07-12 선행 추가됨)과 일치(콘셉트 불변 증거).
  9. 격리: `Placeholders/**`·`OpenSource/**` 파일 집합·바이트 불변(비교 스냅샷 또는 git 기반 — 구현 시 방식 확정, 불명확하면 Codex 검수).
- **기존 테스트**: `ShopPresentationSceneTests`·`SceneBuilderTests`는 U3에서 스프라이트 소스 경로 기대값만 갱신(rect·좌표·색·오브젝트 수 기대값 무변경 — E절이 rect 불변으로 설계된 이유). 그 외 EditMode 494 / PlayMode 9 전부 파일 무수정 통과 = 도메인 무변경의 증거.
- **시각 승인 게이트(오너/Codex — done 전제, task-114 D3 전례)**: 640×360 원본 + 2× 캡처 4종(무대 전경 / 서빙 팝 / 장르 modal / Night 페이드)을 제출 — 콘셉트 정체성 재현·음식 6종 식별·손님 4종 구분·NYC 야간 무드 성립을 판정받는다. 반려 시 Claude는 색·형태를 수정하지 않고 **입력 패키지 재생산 요청**으로 라우팅한다(반려 사유 기록).

### I. 구현 절차 (U0부터 즉시 착수 가능 — 입력 게이트는 U2 전제)

1. U0 — 오너 확정 문구로 `project-brief.md`·`demo-scope.md`·`art-direction.md` 개정 커밋(범위 변경 절차 완결). 기준선(EditMode 494/PlayMode 9) 재실행·기록.
2. U1 — `NycArtContract.cs` 추가(컴파일 안전 — 자산 부재 상태에서도 빌드 green). manifest.md 정비.
3. 파일럿 게이트 — 오너/Codex가 9파일(student idle+walk 4프레임 = 5, `pork_gukbap.png`, `genre_gukbap.png`, `backdrop.png`, `counter.png`)을 먼저 생산한다. 오너/Codex가 1×·4× contact sheet, 걷기 애니메이션, 좌우 반전 결과를 승인한 뒤 나머지 23파일을 생산한다. 파일럿은 U2 부분 납품이 아니며, 최종 32파일 전부가 합격해야 U2를 시작한다.
4. U2 — 합격한 최종 32파일을 `game/Assets/Art/NYC/`에 수입(+`.meta`), provenance 작성, `NycArtTests` 추가, EditMode green 확인. 이 자산 수입 커밋은 U3의 `SceneBuilder` 전환 커밋과 분리한다. 불합격 파일은 수정하지 않고 파일별 반려 사유만 보고한다.
5. U3 — `SceneBuilder` 경로 전환 + 씬 2종 재생성(연속 2회 멱등) + 씬 테스트 기대값 갱신. 배치 컴파일·EditMode·PlayMode 게이트.
6. U4 — `git status --short game` 오염 검사(변경이 영향 파일 표에 한정), 시각 승인 캡처 제출(H절), 반려 시 3으로 복귀. 승인 후 notes/summary/status 기록.

## 실행 계획 (Execution Plan)

- implement_model: claude-fable-5
- implement_effort: high
- routing_reason: 신규 도메인·수학 없이 에셋 계약+임포트 검증+씬 전환+테스트로 구성된 표현 계층 작업이나, 신설 검증 체계(NycArtTests)와 격리 증명(기준선 494/9 + Placeholder 불변)의 정밀도가 필요해 medium보다 한 단계 높인다.

| unit | 파일 범위 | depends_on | group |
|------|-----------|------------|-------|
| U0-ssot-amend | `kb/concepts/{project-brief,demo-scope,art-direction}.md` (오너 확정 문구 반영) | 없음 (오너 문구 확정 게이트) | G0 |
| U1-contract | `game/Assets/Scripts/Editor/NycArtContract.cs` 신설, `kb/tasks/task-116/manifest.md` | U0-ssot-amend | G1 |
| U2-intake-validate | `game/Assets/Art/NYC/**` 32파일+`.meta`+`NYC-ART-PROVENANCE.md`, `game/Assets/StreamingAssets/AI-ART-NOTICE.txt`, `README.md`, `README.en.md`, `game/Assets/Tests/EditMode/NycArtTests.cs` 신설/수정 | U1-contract + 파일럿 9파일 승인 + 최종 32파일 전달·합격 + 공개/라이선스 정책 확정(C절) | G2 |
| U3-scene-switch | `game/Assets/Scripts/Editor/SceneBuilder.cs`(스프라이트 소스 경로), `game/Assets/Scenes/{Shop,MainMenu}.unity` 재생성, `game/Assets/Tests/EditMode/{ShopPresentationScene,SceneBuilder}Tests.cs` 기대값 갱신 | U2-intake-validate | G3 |
| U4-validation-records | 배치 컴파일·EditMode·PlayMode 게이트, 시각 승인 캡처, `kb/tasks/task-116/implementation-notes.md`·`kb/artifacts/task-116-summary.md`·`kb/index/status.md` | U3-scene-switch | G4 |

## 파일/모듈 영향 (Affected Files/Modules)

| 파일/모듈 | 변경 유형 | 설명 |
|-----------|-----------|------|
| `kb/concepts/project-brief.md` | modify | 로드맵 v4 — task-116 행 + 아트 하드캡 개정(오너 확정 문구, U0) |
| `kb/concepts/demo-scope.md` | modify | 하드캡 표 개정 — 오너 승인 AI 생성 아트 조건부 허용(U0) |
| `kb/concepts/art-direction.md` | modify | v2 — CC0 combo 1을 프로토타입으로 재분류, NYC 최종 디렉션 기록(U0) |
| `game/Assets/Scripts/Editor/NycArtContract.cs` | create | NYC 자산 경로 상수·32파일 목록·임포트 표준 적용 (검증·임포트 계약 — 생성 빌더 아님) |
| `game/Assets/Art/NYC/**` (PNG 32 + `.meta`) | create | 입력 패키지 수입 산출 — 오너/Codex 생산, Claude는 검증·배치만 |
| `game/Assets/Art/NYC/NYC-ART-PROVENANCE.md` | create | AI 아트 계보·후처리·승인 기록 (C절 형식) |
| `README.md` · `README.en.md` | modify | C절 공개문과 프로젝트 고유 아트 별도 라이선스 고지 반영 |
| `game/Assets/StreamingAssets/AI-ART-NOTICE.txt` | create | 게임 배포물 내 AI 아트 고지문(C절 공개문) 포함 |
| Steam Content Survey | external | `Pre-Generated AI`로 표기(릴리스 체크리스트에서 처리) |
| 인게임 Credits UI | future task | 공개 출시 전 별도 release task에서 AI 아트 고지 반영 |
| `game/Assets/Tests/EditMode/NycArtTests.cs` | create | H절 1~9 자동 검증 |
| `game/Assets/Scripts/Editor/SceneBuilder.cs` | modify | 스프라이트 소스 경로를 NycArtContract로 전환 + backdrop/counter Image type Tiled→Simple (U3 단일 커밋 — 롤백 지점) |
| `game/Assets/Scenes/{Shop,MainMenu}.unity` | modify | SceneBuilder 재생성 산출 |
| `game/Assets/Tests/EditMode/{ShopPresentationScene,SceneBuilder}Tests.cs` | modify | 스프라이트 소스 경로 기대값만 갱신(rect·좌표·오브젝트 수 불변) |
| `game/Assets/Scripts/Editor/PlaceholderArtBuilder.cs` · `game/Assets/Art/Placeholders/**` · `game/Assets/Art/OpenSource/**` · `PlaceholderArtTests.cs` | none | 무수정 보존 — 변경 발생 시 이상 신호(테스트 기준) |
| `kb/concepts/art-originals/*.png` (6종) | none | PNG 바이트 불변 — PROVENANCE.md의 md5 핀으로 검증(H8; md5 열은 2026-07-12 본 설계 교차검토에서 선행 추가됨) |
| 도메인·저장·밸런스·엔딩 소스/테스트 전부 | none | diff 부재가 무변경 증거 |
| `kb/tasks/task-116/implementation-notes.md` · `kb/artifacts/task-116-summary.md` | create | 구현 기록·산출 요약 |
| `kb/index/status.md` | modify | `runtime/generate-status.py` 재생성 |

## 테스트 기준 (Test Criteria)

- [ ] `python -B runtime/validator/cli.py kb/tasks/task-116/design.md`가 종료 코드 0으로 통과한다.
- [ ] 구현 전 기준선 EditMode 494 / PlayMode 9를 실행해 실제 결과를 구현 노트에 기록한다.
- [ ] U0 개정 3문서가 오너 확정 문구와 일치하고, demo-scope 범위 변경 절차(SSOT 선갱신 순서)가 지켜졌다.
- [ ] 파일럿 9파일(student 5 + `pork_gukbap.png` + `genre_gukbap.png` + `backdrop.png` + `counter.png`)이 1×·4× contact sheet, 걷기 애니메이션, 좌우 반전 승인으로 통과했고, 이후 최종 32파일이 F절 스펙(규격·알파·색 상한·프레임 규약)을 기계 검증으로 전수 통과했다. 불합격분은 반려 기록이 남아 있다(Claude의 수정 생성 0건).
- [ ] `NycArtTests` H절 1~9 전수 green: 존재+`.meta` 쌍 / 규격 / 알파 규칙 / 원본 PNG IHDR color type 6 / 색 상한 / 임포트 표준 / provenance 커버리지 / 콘셉트 원본 md5 불변 / Placeholders·OpenSource 불변.
- [ ] `SceneBuilder.Apply` 연속 2회 멱등(오브젝트 수·persistent listener), Build Settings 씬 2개(`MainMenu`+`Shop`)만 존재.
- [ ] 씬 전환 후에도 rect 계약 불변: CustomerSprite 64×64 · FoodIcon 64×64/(-40,78) · 소품 32×32 · 장르 Icon 32×32/(-38,0) · CashPopupText (-40,120) · NightOverlay `#16202A` alpha 0.
- [ ] Unity 배치 컴파일 종료 코드 0·`error CS` 없음. EditMode 전체 green(기준선 494 + NycArtTests 신규, 명시 갱신한 스프라이트 경로 기대값 외 무회귀). PlayMode 9 무수정 통과.
- [ ] `git status --short game`에 `Library/Temp/Obj/Logs/UserSettings/Build*` 오염이 없고, `.meta` 추가가 신규 NYC 자산에 한정되며 삭제 0건, 변경이 영향 파일 표 목록에 한정된다.
- [ ] 도메인 무변경 증거: `GameState`/Ops/매니저/UI 컨트롤러/저장/엔딩 소스·테스트가 diff에 나타나지 않는다.
- [ ] 시각 승인 게이트(오너/Codex, Claude self-approve 금지): 640×360 원본+2× 캡처 4종으로 콘셉트 정체성 재현·음식 6종 식별·손님 4종 구분·NYC 야간 무드를 승인받았다. 반려는 입력 패키지 재생산 요청으로 처리되었다.
- [ ] 수동 Play smoke(오너): 장보기→영업→정산 1일 루프에서 NYC 손님·음식·무대가 표시되고, 걷기 애니·주문/정산 수치·저장/이어하기·엔딩 판정이 이전과 동일하게 동작한다.
- [ ] 구현 완료 후 `python runtime/validator/cli.py --check-done task-116`과 `python runtime/generate-status.py --check`가 통과한다.

## 오픈 이슈 (Open Issues)

번호는 본문 상호참조 보존을 위해 초안 그대로 유지한다. 1·2·3·5·6·7·9·10은 **확정으로 닫힘**(결정 기록), 4·8은 **배치 1 구현 비차단**이다.

1. **[확정 — 닫힘] 일러스트→스프라이트 생산 방식**: 오너가 **A안(타깃 해상도 재생산)** 을 채택했다(2026-07-12, Codex 교차검토 승인). 콘셉트는 레퍼런스, 입력 패키지는 오너/Codex 생산, Claude는 임포트·검증·배선·테스트만 — B절 확정 기준으로 구현한다.
2. **[확정 — 닫힘] 픽셀 규격 + family_parent 콘텐츠**: E절 표 확정(손님 32×32 · 음식 32×32 · 장르 아이콘 32×32 · backdrop 640×160 · counter 320×32 — 전부 rect 정수 약수, 씬 rect 무변경). **family_parent 캔버스 32×32 락** — 부모+아이 페어를 우선 시도하고, 1× 식별 불가할 때만 오너/Codex 재승인 후 부모 단독으로 전환한다.
3. **[확정 — 닫힘] 알파 정책**: 최종 납품은 RGBA 실투명 배경 + 이진 알파(a∈{0,255}, backdrop 불투명 예외)로 확정한다. RGB 산출 시 exact-match 키잉은 오너/Codex 생산 단계에서 RGBA PNG로 재납품하기 위한 처리이며, Claude는 RGB 파일을 반려한다(F1/F3 확정 — Codex 적합 판정).
4. **[비차단] 배치 2 범위 (신규 표현 슬롯)**: 주인공 스프라이트·시스템 아이콘 8종 UI 배선·손님 표정·증기 이펙트·트럭 독립 소품. 콘셉트에는 존재하나 런타임 슬롯 신설이 필요해 배치 1에서 제외했다. 승격 시 별도 task-117 설계(오너 결정 — 이 표에 기록만). 배치 1 진행을 막지 않는다.
5. **[확정 — 닫힘] AI 아트 공개 표기·라이선스 선언 문구**: 승인자 Project Owner, 승인일 2026-07-12. 코드 MIT와 프로젝트 고유 아트 별도 라이선스를 분리하고, `kb/concepts/art-originals/**`와 `game/Assets/Art/NYC/**`는 리포 MIT에서 제외한다. 재사용·재배포·2차 저작물 허가는 부여하지 않으며 CC0 표기는 금지한다. 공개문과 표시 위치는 C절 확정본을 사용한다.
6. **[확정 — 닫힘] 무대 배경 방식**: `backdrop.png`는 640×160 단판 합성, `counter.png`는 320×32 단판으로 확정했다. 타일셋 반복은 기각하며, U3에서 Image type Tiled→Simple 전환을 반영한다.
7. **[확정 — 닫힘] 프레임 전달 형식**: **개별 PNG(프레임당 1파일)** 확정 — 슬라이싱 단계 제거(F2). 그리드 시트 대안 기각.
8. **[비차단] CC0 세트 최종 처분 + 데모 재빌드**: task-115 Windows 빌드는 CC0 마감본으로 완료됨. NYC 전환 후 데모 재빌드 여부, `Placeholders/`·`OpenSource/` 세트의 장기 보존/제거 시점은 오너 결정(이 task는 보존 — G절).
9. **[확정 — 닫힘] 장르 아이콘 의미 매핑**: 국그릇=`gukbap`, 꼬치=`bunsik`, 면기=`noodles`, 도시락=`generalist`로 확정했다. 장르 아이콘은 버튼 프레임 없이 투명 배경 심볼만 사용한다.
10. **[확정 — 닫힘] 좌향 플립의 비대칭 디테일**: 좌향은 런타임 horizontal flip을 허용하고, 백팩·토트백 등 비대칭 소품이 반전되어 보이는 것도 수용한다. 별도 좌향 프레임은 배치 1 범위에 넣지 않는다.

---

**결정 확정 기록 (2026-07-12)**: 오너가 B절 A안(타깃 해상도 재생산)을 채택하고, Codex 설계 교차검토가 픽셀 규격(E절)·알파/프레임 스펙(F절)을 적합 판정했으며, 콘셉트 원본 md5 핀이 `PROVENANCE.md`에 추가되어 H8 검증이 구현 가능해졌다. 추가로 실제 장르 ID `noodles`, AI 아트 공개·라이선스 정책, 무대 단판 방식, family_parent 페어 우선 원칙, 좌향 horizontal flip 허용, 파일럿 9파일 선검증, 원본 PNG IHDR color type 6 검증이 확정되었다. 이로써 오픈 이슈 1·2·3·5·6·7·9·10이 닫혔고 Status는 ready를 유지한다. 잔여 오픈 이슈 4·8은 배치 1 구현을 차단하지 않으며, 구현 중 디자인 판단이 필요하면 추측하지 않고 Codex 검수로 돌린다.
