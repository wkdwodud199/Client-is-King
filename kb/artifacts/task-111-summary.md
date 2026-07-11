# 산출물 요약 — task-111

> Status: done
> Inputs: kb/tasks/task-111/implementation-notes.md
> Outputs: 이 요약 문서 — SNS 마케팅 시스템(도메인 수학·DayModifier 합성·매니저 배선·UI·SceneBuilder·밸런스 guard·PlayMode) 완료 요약과 인계
> Next step: **Codex 코드 리뷰 + 640×360 시각 승인 + 수동 Play smoke + 오너 비용 재시드 승인 → 통과 시 task-112(이벤트/장애물)**

## 작업 요약

- **Task ID**: task-111
- **제목**: SNS 마케팅 시스템 — 가상 채널 3종(픽쳐그램/숏핑/동네게시판), 익일 손님 수·분포 지연 보상,
  결정론적 감쇠/보너스 주문/타겟 매칭
- **완료일**: 2026-07-11 (Codex 코드리뷰·640×360 시각승인·수동 smoke·오너 비용 재시드 승인 대기)

## 산출물 목록

| 산출물 | 경로 | 설명 |
|--------|------|------|
| SNS 도메인 Ops | `game/Assets/Scripts/Runtime/Social/{SNSCampaignRecord,SNSCampaignResult,SNSCampaignOps}.cs` | 순수 C#, 밀리 투영·감쇠 체인·보너스/팔로워·타겟 매칭·집행 게이트·미리보기·history→DayModifier 재구성 |
| DayModifier | `game/Assets/Scripts/Runtime/Genre/DayModifier.cs` | 익일 수요 modifier 순수 DTO(task-112 이벤트 합성 공용 훅) |
| plan 합성 확장 | `Runtime/Genre/{GenreDemandPlan,GenreSelectionOps}.cs` | Base/Bonus 주문 수·보너스 가중치 풀·modifier overload, neutral 하위호환 |
| 서비스 태그/통계 | `Runtime/Service/{ServiceOrderState,ServiceOps,ServiceManager}.cs` | `snsInflow` 태그, SNS 당일 통계, SNS catalog·집행/미리보기 API, plan modifier 합성 |
| 이벤트 | `Runtime/DayCycle/GameEvents.cs` | `SNSCampaignExecuted` 추가 |
| UI | `Runtime/UI/{NightPanelController,ServicePanelController,SettlementPanelController,PhaseHudController,MarketPanelController}.cs` | Night SNS 블록(팔로워·버튼3종·결과/경고), Service/Settlement 태그·원인 라인, HUD badge, Market 상세 |
| 표현 | `Runtime/Presentation/{ServicePresentationEventArgs,ShopPresentationController}.cs` | SNS 유입 표현 이벤트·무대 라벨(Jade Green) |
| 데이터/씬 | `Editor/InitialDataBuilder.cs`, `Editor/SceneBuilder.cs`, `Scenes/{MainMenu,Shop}.unity` | SNS 3종 비용 재시드(15,000/12,000/7,000), Night SNS UI·catalog 주입 |
| SNS 도메인 테스트 | `Tests/EditMode/SNSCampaignOpsTests.cs` | 밀리 투영·감쇠·보너스·팔로워·매칭·게이트·미리보기·modifier 재구성 (37 tests) |
| plan/service 테스트 확장 | `Tests/EditMode/{GenreSelectionOps,ServiceOps}Tests.cs` | modifier 검증·base-prefix 불변·C5 고정벡터·태그/통계 회귀 |
| 밸런스 테스트 | `Tests/EditMode/SNSBalanceTests.cs` | 100-day 재유도, ±1%·수확체감 가시성·완만 감쇠 정체성·하드캡8·생존 guard (9 tests) |
| 씬/UI 회귀 테스트 | `Tests/EditMode/{SceneBuilder,MarketPanelScene,ServicePanelScene,SettlementPanelScene}Tests.cs` + 신규 `{NightPanelScene,NightPanelSnsFlow}Tests.cs` | 구조적+상태(SnsFlow) fixture 분리, 총 230/230 |
| PlayMode | `Tests/PlayMode/GenrePersistencePlayModeTests.cs` | SNS catalog 생존, Night 집행→Day2 plan/태그 UI 없이 도메인 경로 검증, 4/4 pass |
| task 기록 | `kb/tasks/task-111/`, 이 요약 | manifest·design·design-review-codex·implementation-notes |

## 주요 결정 / 이탈

- **결정론 수학 고정**: `MulMilliHalfUp(a,b)=(a×b+500)/1000`(RoundHalfUp(a×b/1000) 정수 동치),
  FNV-1a 시드 형식 task-110 그대로 재사용(known vector `gukbap|1|0`→2190636514 회귀 없음, 신규
  `bunsik|2|6`→1202351915/`bunsik|2|7`→1185574296 설계값과 정확 일치).
- **밸런스 guard 1회차 통과**: 100-day 재유도 12조합 전부 설계표 대비 최대 편차 0.006%(사실상 오차 없음)
  — 재밸런싱·에스컬레이션(opus-4-8 재검산/Codex 재설계 요청) 불필요.
- **Codex 설계 리뷰 반영**: preview `TopTargetCustomerIds` 계산을 customer 투영 입력을 받는 순수
  helper(`SNSCampaignOps.TryBuildPreview`) 단일 경로로 확정 — UI/매니저 재계산 경로 없음(blocking 지적
  해소).
- **씬 테스트 fixture 분리 계승** — task-110 이 발견한 OpenScene 재로드의 Awake/Start/OnEnable 미보장·
  Transform 참조 무효화 함정에 대응해 구조 테스트와 상태 테스트를 별도 클래스로 유지. 이번 그룹에서
  동일 함정(캐시 참조 사용)에 다시 걸린 테스트 1건을 발견·수정.
- **neutral overload 전량 보존** — 기존 `GenreSelectionOps.TryBuildDemandPlan`(4-입력)·`ServiceManager`
  public API·`GameEvents` 등 하위호환 유지, task-110 기준선(EditMode 152/PlayMode 2) 회귀 없이 보존
  (현재 EditMode 230/PlayMode 4로 확장).

## 검증

- 컴파일 exit 0(error CS 0, 수 회 반복) · **EditMode 230/230**(2회 연속) · **PlayMode 4/4**(2회 연속) ·
  씬 멱등성(오브젝트 총수 동일 + persistent listener 0) · `git status --short game`에 금지 디렉터리 없음 ·
  신규 파일 전부 `.meta` 존재 · Build Settings 씬 정확히 2개(하드캡 불변).
- SNS 3종 하드캡·씬 2개 하드캡·이벤트/장르 수 불변 — demo-scope.md 준수.

## 미결(Codex/오너 게이트)

- 640×360 원본 캡처 시각 승인 — 미수행(self-approve 금지).
- 수동 Play smoke(60초 내 판단, 집행→익일 badge→태그→Settlement 인과 사슬, 반복 감쇠 확인) — 미수행.
- Codex 코드 리뷰(U1~U8 전체 diff) — 미수행.
- 오너 SNS 비용 재시드 승인(15,000/12,000/7,000) — design.md 오픈 이슈, 미승인.

## 관련 문서

- 설계: `kb/tasks/task-111/design.md`, Codex 교차검토: `kb/tasks/task-111/design-review-codex.md`
- 구현 노트: `kb/tasks/task-111/implementation-notes.md` (그룹1~3 상세, 밸런스 재유도 실측값, 트러블슈팅 포함)
