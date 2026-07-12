# 산출물 요약 — task-113

> Status: done
> Inputs: kb/tasks/task-113/implementation-notes.md
> Outputs: 이 요약 문서 — 저장/불러오기 시스템(JsonUtility 순수 파이프라인·검증 매트릭스·매니저 배선·
> UI·SceneBuilder·테스트·PlayMode) 완료 요약과 인계
> Next step: **Codex 코드 리뷰 + 640×360 시각 승인 + 수동 Play smoke → 통과 시 task-114(아트 마감)**

## 작업 요약

- **Task ID**: task-113
- **제목**: 저장/불러오기 — 순수 C# `GameState` → `JsonUtility` 단일 슬롯 자동 저장, 프로브→버전→
  마이그레이션 훅→역직렬화→V2b 정규형→검증 매트릭스(V1~V11) 파이프라인, MainMenu 이어하기
- **완료일**: 2026-07-12 (Codex 코드리뷰·640×360 시각승인·수동 smoke 대기)

## 산출물 목록

| 산출물 | 경로 | 설명 |
|--------|------|------|
| 저장 도메인 Ops | `game/Assets/Scripts/Runtime/Save/{SaveOps,SaveSummary,SaveFileStore}.cs` | 순수 파이프라인(직렬화/역직렬화/V2b 정규형/V3~V10 검증 매트릭스/마이그레이션 훅), peek 요약 DTO, 원자적 파일 I/O |
| 스키마 버전 | `Runtime/DayCycle/GameState.cs` | `SaveSchemaVersion`(=1) 상수 + `schemaVersion` 필드 |
| 표현 이벤트 | `Runtime/DayCycle/GameEvents.cs` | `SaveStateChanged`(저장 시도 후 1회 발행) |
| 매니저 배선 | `Runtime/Managers/GameManager.cs` | Save/Load/Peek API·경로 override·V11 주문 identity 사후검증+롤백·자동 저장 트리거 5종 |
| 정산/서비스 매니저 | `Runtime/Settlement/SettlementManager.cs`, `Runtime/Service/ServiceManager.cs` | 트리거3(정산 신규 적용) 배선, `EnsureServiceDay` legacy 제거 |
| UI | `Runtime/UI/{MainMenuController,NightPanelController}.cs` | 이어하기 블록 G2 분기 4종, Night 자동 저장 표시 라인 |
| 씬 | `Editor/SceneBuilder.cs`, `Scenes/{MainMenu,Shop}.unity` | `ContinueButton`/`SaveStatusText` 생성·주입·navigation |
| 저장 도메인 테스트 | `Tests/EditMode/{SaveOps,SaveFileStore}Tests.cs` | 직렬화/역직렬화/V2b 정규형/V3~V10 매트릭스/마이그레이션/RT1/RT2/RT5 |
| 매니저 테스트 | `Tests/EditMode/GameManagerSaveLoadTests.cs` | Save/Load/Peek 원상복구, V11 롤백 3종, 트리거 gating |
| UI/씬 테스트 | `Tests/EditMode/{MainMenuSaveFlow(신규),NightPanelScene,NightPanelSnsFlow,SceneBuilder}Tests.cs` | G2 분기 4종, 저장 라인 worst-case 폭·좌표 불변, G1 오브젝트·좌표·멱등 |
| 테스트 인프라 확장 | `Tests/EditMode/TestSceneSupport.cs` | `OpenMainMenuSceneWithLiveSingletons`/`ForceAwake` 신규 helper |
| PlayMode | `Tests/PlayMode/SaveLoadPlayModeTests.cs` | `[SetUpFixture]` 경로 격리, 트리거 5종 실파일 확인+RT1/RT3, Night 세이브 이어하기→Shop 진입 |
| task 기록 | `kb/tasks/task-113/`, 이 요약 | manifest·design·design-review-codex·implementation-notes |

## 주요 결정 / 이탈

- **단일 검증 함수 공유**: 저장(`SaveGame`)·peek(`TryPeekSave`)·로드(`TryLoadGame`) 세 경로가 전부
  `SaveOps.TryValidateState`(V3~V10) 하나를 통과한다 — 경로별 검증 격차·조용한 기본값 진행이 구조적으로
  불가능하다(task-111/112 리뷰 교훈의 이행).
- **V11은 manager 계층에서 프로덕션 경로 재사용**(Codex 설계 리뷰 승인 사항): `SaveOps`에 plan 합성
  로직을 복제하지 않고, `GameManager.TryValidateOrderIdentity`가 기존 `ServiceManager.TryBuildDayPlan`
  + `ServiceOps.BuildOrders(plan,customerDefs)`를 그대로 호출해 저장 주문과 identity 5필드
  (recipeId/customerId/partySize/snsInflow/eventInflow)를 비교한다. 같은 개수의 다른 주문으로 변조된
  손상 JSON이 V7(catalog/통계 검증)만으로는 걸러지지 않는 구멍을 이 계층에서 닫는다.
- **V2b 정규형 계약**: `ToJson(TryDeserialize(json),prettyPrint:true)==입력json` 바이트 동일 하나로
  필드 누락·명시 null·미지/잉여 키·순서 변조·중복 키를 전부 명시 실패시킨다 — JsonUtility 가 누락/null
  을 조용히 기본값으로 채우는 동작에 의존하지 않는 별도 규칙.
- **자동 저장만, 수동 저장 버튼 없음**: 5종 트리거(새 런 시작/phase 전환/정산 신규 적용/장르 확정/
  SNS 집행) + `Application.isPlaying` 가드로 EditMode 332→428 테스트 무회귀를 구조적으로 보장한다.
- **신규 singleton manager 없음**: `SaveManager`는 `GameManager` 얇은 확장 + 순수 `SaveOps` + 정적
  `SaveFileStore`로 대체(task-111 SNSManager·task-112 EventManager 보류 전례 계승).
- **`EnsureServiceDay` 제거**: task-111 오픈 이슈 이행 — plan 을 우회하는 주문 재생성 경로가 재개된
  상태에서 호출되면 C3(재개 동일 주문) 계약을 조용히 깨는 유일한 잔존 구멍이었다. 호출자 0건을 컴파일
  게이트로 재확인했다.
- **자체검증 중 발견한 테스트 픽스처 버그**: 프로덕션 `SaveOps`/`GameManager` 로직 자체의 결함은
  없었고, 그룹1/그룹3에서 작성한 테스트 픽스처의 전제 조건 누락(V5/V7 선행 조건 미충족, `Awake()`
  배치 EditMode 비동기 호출 함정)만 발견·수정했다(implementation-notes.md 상세 참조).

## 검증

- 컴파일 exit 0(error CS 0) · **EditMode 428/428**(기존 332 + task-113 신규 96, 무회귀) ·
  **PlayMode 8/8**(기존 6 + 신규 2, 무회귀) · `git status --short game`에 세이브 산출물 없음(실사용
  persistentDataPath 도 재확인) · 신규 파일 전부 `.meta` 존재 · Build Settings 씬 정확히 2개(하드캡
  불변) · `EnsureServiceDay` 제거 후 컴파일 통과(호출자 0).
- **중요 발견(향후 세션 참고, task-112 노트 계승)**: Unity 배치 테스트에서 `-runTests`는 `-quit`과
  함께 쓰면 결과 파일이 생성되지 않는 무증상 실패가 발생한다 — `-quit` 없이 실행하고 `Unity.exe`
  프로세스 종료를 폴링해야 한다(백그라운드 실행 래퍼의 "완료" 보고가 실제 프로세스 종료보다 먼저
  오는 경우가 반복 관찰됨 — 결과 파일 존재와 프로세스 부재를 함께 확인해야 한다).

## 미결(Codex/오너 게이트)

- Codex 코드 리뷰(U1~U6 전체 diff) — 미수행.
- 640×360 원본 캡처 시각 승인(MainMenu 이어하기 블록·Night 저장 라인) — 미수행(self-approve 금지).
- 수동 Play smoke(design.md I16 — 종료 후 이어하기 재개, 파산 잠금, 손상 파일 3종 사유 표시) — 미수행.

## 관련 문서

- 설계: `kb/tasks/task-113/design.md`, Codex 교차검토: `kb/tasks/task-113/design-review-codex.md`
- 구현 노트: `kb/tasks/task-113/implementation-notes.md`(세 그룹 상세)
