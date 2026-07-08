# 구현 노트 — task-106

> Status: done
> Inputs: kb/tasks/task-106/design.md
> Outputs: Service 상태/Ops/매니저 + 영업 UI + 테스트 15종 추가 — 조리·서빙 코어 루프 완성
> Next step: task-107 설계 요청 (`runtime/codex-design.ps1 task-107 --auto`) — 정산 + 하루 마감 + 파산 (M1 완료, 첫 플레이어블)

## 설계 대비 변경 사항

| 항목 | 설계 내용 | 실제 구현 | 변경 사유 |
|------|-----------|-----------|-----------|
| 재료 이름 표시 | (설계 미규정 — 필요 재료 텍스트 표시) | ServicePanelController 에 `List<IngredientDef>` 도 주입해 한국어 재료명 표시 | 표시명 SSOT 는 IngredientDef (중복 상수 금지 규약) — kind enum 영문 노출 회피 |
| 결과 타입 파일 | ServiceResult.cs | ServiceResult + RequiredIngredient 를 같은 파일에 배치 | 결과/조회용 소형 타입 묶음 — 파일 수 최소화 |
| IVT 확장 | (설계 미규정) | AssemblyInfo 에 `InternalsVisibleTo("ClientIsKing.Tests.EditMode")` 추가 | 시드에 없는 fixture(중복 kind 레시피)를 테스트가 EditorInit 로 구성 — kind 합산 검증용 |

## 구현 결정 기록

1. **결정론 = 산식, 씨앗 아님**: BuildOrders 는 id 정렬 후 `recipeIdx=(day-1+i)%R`,
   `custIdx=((day-1)*2+i*3)%C`, `party=min+(day-1+i)%span` — 같은 입력+day 면 동일, day 마다 상이.
   의사난수/Random 미사용이라 재현·테스트가 자명하다.
2. **인덱스 규약 통일**: `serviceCurrentOrderIndex` 는 "다음 미처리 주문 위치, 전부 처리 시 목록 길이".
   GetCurrentOrder/TryServe/Skip 모두 `FindNextOpenIndex` 하나로 판정.
3. **서빙 트랜잭션**: CanServeOrder preflight(전 재료 검증) 통과 후에만 소비 —
   InventoryOps.TryConsume 의 실패 불변 계약을 그대로 계승. 부족 재료는 "kind have/need" 로 메시지화.
4. **등급 혼합 금지**: 선택 등급(C/B)만 검사·소비. UI 는 Market 과 동일한 토글 패턴.
5. **버튼 비활성화**: 주문 소진 시 서빙/포기 버튼 interactable=false + "오늘 영업 종료" 표시.
6. **EnsureServiceDay**: `state.serviceDay != state.day` 일 때만 재생성 — 같은 날 재진입(패널 토글)은
   진행 상태 유지, 다음 날 Service 진입 시 자동 리셋. 하루 마감 정산은 task-107 소관.

## 발생한 이슈

1. **GetCurrentOrder 인덱스 규약 불일치 버그 — 테스트 게이트가 검출**: `FindNextOpenIndex` 는 "없음" 을
   `orders.Count` 로 반환하는데(필드 규약) GetCurrentOrder 가 `-1` 규약(`index >= 0`)으로 검사해
   빈/소진 목록에서 `ArgumentOutOfRangeException` — 1차 테스트 실행에서 3케이스 실패(54/57)로 즉시 노출,
   범위 검사로 수정 후 57/57. **도메인 버그가 UI/Play 에 닿기 전에 EditMode 게이트에서 잡힌 첫 사례.**
2. 테스트 실행 1회가 외부 요인으로 중단(killed)됨 — 잔여 프로세스/결과물 없음 확인 후 재실행으로 해소.

## 테스트 결과

| 테스트 기준 (design.md 참조) | 결과 | 비고 |
|------------------------------|------|------|
| validator rc0 (design.md) | pass | 러너 게이트 |
| 배치 SceneBuilder.Apply exit 0 | pass | Service UI 포함 재생성 (첫 실행 통과) |
| 배치 컴파일 게이트 exit 0 + `error CS` 없음 | pass | |
| EditMode 테스트 exit 0 | pass | **57/57** (1차 54/57 → 이슈 1 수정 → 전량 통과) |
| GameState 기본값 + 서비스 통계 0/빈 주문 | pass | defaults 테스트 확장 |
| 주문 생성 결정론 + 유효 id/파티(1↑, 범위 내) + day 별 상이 | pass | `ServiceOpsTests` 3케이스 |
| 필요 재료 = 요구량×파티, kind 합산 | pass | 중복 kind fixture 포함 |
| 판매가 = BasePrice×파티 (배수 없음) | pass | |
| 서빙 성공: 재료 정확 차감 + cash/매출 동액 증가 + 통계/인덱스 이동 | pass | |
| 재료 부족: 전 상태 불변 | pass | |
| 등급 혼합 금지 (C 부족 시 B 자동 사용 안 함) | pass | B 지정 시에만 B 소비 검증 |
| 포기: 매출/재료 불변 + 실패 통계 + 인덱스 이동, 소진 시 실패 | pass | |
| Service UI 컨트롤 11종 + 컨트롤러 존재, 초기 비활성 | pass | `ServicePanelSceneTests` |
| RecipeDef 6 + CustomerDef 4+ 주입, ServiceManager 탑재 | pass | |
| 기존 테스트 42종 지속 통과 + Build Settings 씬 2개 | pass | |
| git 캐시/빌드 미노출 | pass | -uall 0건 |
| `--check-done` + `generate-status --check` | pass | 기록 후 실행 (요약 참조) |

## 산출물

- `game/Assets/Scripts/Runtime/Service/{ServiceOrderState,ServiceResult,ServiceOps,ServiceManager}.cs`
- `game/Assets/Scripts/Runtime/DayCycle/GameState.cs` — 서비스 당일 상태 8필드 확장
- `game/Assets/Scripts/Runtime/UI/ServicePanelController.cs` — 영업 UI 컨트롤러
- `game/Assets/Scripts/Runtime/AssemblyInfo.cs` — IVT Tests.EditMode 추가
- `game/Assets/Scripts/Editor/SceneBuilder.cs` — BuildServicePanel + Recipe/Customer 로더 + ServiceManager 탑재
- `game/Assets/Scenes/{MainMenu,Shop}.unity` — 재생성 (영업 UI 포함)
- `game/Assets/Tests/EditMode/{ServiceOps,ServicePanelScene}Tests.cs` + defaults 확장 (테스트 +15)
- `kb/tasks/task-106/{manifest,implementation-notes}.md`, `kb/artifacts/task-106-summary.md`
