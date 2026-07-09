# 구현 노트 — task-108

> Status: done
> Inputs: kb/tasks/task-108/design.md
> Outputs: 플레이스홀더 아트 10종 + 표현 이벤트 3종 + 무대 연출 컨트롤러 + 패널 재배치 — **M1.5 표현 미니 패스 완성**
> Next step: **M1.5 게이트 — 사용자 Play 모드 플레이테스트** (재미 검증 통과 시 task-109 장르 선택으로 진행)

## 설계 대비 변경 사항

| 항목 | 설계 내용 | 실제 구현 | 변경 사유 |
|------|-----------|-----------|-----------|
| 스프라이트 조달 | 프로젝트 생성형 CC0 vs 외부 CC0 택일 | **코드 생성 픽셀** (PlaceholderArtBuilder — 결정론적 픽셀 패턴) | 다운로드 의존 0, 재현성 완전, provenance 자명. 고객 4종은 상의 색+머리색, 음식 6종은 그릇+내용물 색으로 구분 |
| SceneBuilderTests 갱신 | "필요 시" 수정 | 미수정 — 무대/레이아웃 검증은 신설 ShopPresentationSceneTests 로 일원화 | 기존 단언(카메라/HUD/패널 존재)은 그대로 유효 — 중복 회피 |
| 발행 경로 테스트 | OnServe/OnSkip 경로 검증 | payload 빌더(internal)를 직접 검증 + GameEvents 왕복 검증으로 분해, 통합 경로는 Play smoke | EditMode 는 MonoBehaviour lifecycle(OnEnable/Instance)이 없어 통합 경로 실행 불가 — 계약(payload 정확성 + 전달)은 동등하게 고정됨 |
| NightPanelController | "필요 시" 수정 | 미수정 | 오버레이는 ShopPresentationController 가 DayPhaseChanged 로 처리 — Night 패널 역할 불변 |

## 구현 결정 기록

1. **표현/도메인 분리 유지**: Ops(Service/Settlement/Economy/Inventory) 는 단 한 줄도 변경 없음.
   표현 이벤트(ServiceOrderPresented/ServiceOutcomeResolved/SettlementPresented)는 **UI 컨트롤러만 발행**,
   ShopPresentationController 만 구독. 게임 규칙은 연출 완료를 기다리지 않는다 (설계 제약 그대로).
2. **처리 전 캡처 계약**: OnServe/OnSkip 은 `service.CurrentOrder` 를 먼저 캡처한 뒤 도메인 호출 —
   outcome payload 가 처리 전 주문 정보(고객/레시피/파티)를 보존한다 (테스트 고정).
3. **Play/Edit 이중 모드**: 모든 트윈은 `Application.isPlaying` 가드 — Play 는 코루틴 연출,
   Edit(테스트)는 즉시 최종 상태 스냅. 정산 카운트업도 동일 (최종값 정확성은 RenderFinal 이 보장, 설계 26단계).
4. **레이아웃**: 무대 상단 밴드(y20~180 — 배경 640×160, 카운터, 손님 동선 x-360→-160),
   패널 4종은 하단 (0,-80) 480×200 압축 재배치. 무대/오버레이는 캔버스 선두 자식 —
   렌더 순서 = 무대 < 오버레이 < HUD/패널 (밤에도 UI 는 밝게, 클릭 항상 가능).
5. **클릭 안전**: 무대 Image 전부 + NightOverlay `raycastTarget=false` (테스트 고정) — 연출이 입력을 못 막는다.
6. **미지 id 복구**: catalog 에 없는 customerId/recipeId → 이미지 비표시 fallback, 예외 없음 (테스트 고정).
7. **주문 라벨 규약**: ServiceOrderPresented.Message 에 레시피 표시명을 실어 무대 orderLabel 이 그대로 표시
   (표현 계층은 RecipeDef 를 직접 알 필요 없음).

## 발생한 이슈

- 없음. 3게이트 모두 첫 실행 통과 (90/90). 에디터 내부 Search 인덱싱 에러는 `Library/Search` 캐시
  삭제로 재색인 유도 (게임 코드 무관 — task-108 범위 외 정리).

## 테스트 결과

| 테스트 기준 (design.md 참조) | 결과 | 비고 |
|------------------------------|------|------|
| validator rc0 (design.md) | pass | 러너 게이트 |
| 배치 SceneBuilder.Apply exit 0 | pass | PlaceholderArtBuilder 선행 호출 포함 (첫 실행 통과) |
| 배치 컴파일 게이트 exit 0 + `error CS` 없음 | pass | |
| EditMode 테스트 exit 0 | pass | **90/90 통과** (+아트 3, 무대 6, 이벤트 5, 정산 표현 4) |
| 스프라이트 10종 존재 + 픽셀 표준 임포트(PPU32/Point/무압축/mipmap off) | pass | `PlaceholderArtTests` |
| provenance 문서에 파일별 출처 기록 | pass | 전 파일 명시 검증 |
| 무대 오브젝트 9종 + 컨트롤러 + catalog 4/6 주입 | pass | `ShopPresentationSceneTests` |
| NightOverlay 초기 alpha 0 + raycast 차단 없음 | pass | 무대 Image 전체 raycast off 포함 |
| 패널이 무대 밴드를 가리지 않음 (top ≤ backdrop bottom) | pass | 640×360 기하 검증 |
| 주문 표시 payload (id/파티/번호/표시명) + 빈 슬롯 신호 | pass | `ServicePresentationEventTests` |
| 서빙 outcome: 처리 전 정보 보존 + revenue>0 / 포기: missed+revenue 0 | pass | |
| GameEvents 왕복 payload 동일성 | pass | |
| 정산 최종 텍스트 = SettlementResult 정확 일치 + payload 일치 | pass | `SettlementPresentationTests` (파산 포함) |
| 멱등 재표시 (cash 불변 + 동일 최종값) | pass | |
| 미지 id fallback 무예외 | pass | |
| 기존 테스트 72종 지속 통과 + 씬 2개 하드캡 | pass | |
| git 캐시/빌드 미노출 | pass | -uall 0건 |
| `--check-done` + `generate-status --check` | pass | 기록 후 실행 (요약 참조) |

## 수동 Play smoke 기준 (M1.5 게이트 — 사용자 확인 항목)

1. MainMenu → 게임 시작 → Market 구매 → "영업 시작 ▶"
2. **손님 입장**: 왼쪽에서 걸어와 카운터 앞 정지, 머리 위 주문 메뉴 + ×인원 표시
3. **서빙**: 음식 아이콘 팝 + `+N원` 상승 팝업, 손님 초록 틴트 후 오른쪽 만족 퇴장
4. **포기**: 빨간 틴트 후 왼쪽 불만 퇴장 (팝업 없음)
5. **정산**: 숫자 카운트업 + 무대 순손익 pulse (양수 초록/음수 주황/파산 빨강)
6. **밤**: 무대 어두워짐(UI 는 밝게 유지) → "다음 날 ▶" → 오버레이 해제 + Day+1
7. 파산 시 진행 잠금 유지, 연출이 phase 를 추가 진행시키지 않음

## 산출물

- `game/Assets/Art/Placeholders/{Customers×4,FoodIcons×6}.png` + PLACEHOLDER-PROVENANCE.md
- `game/Assets/Scripts/Editor/PlaceholderArtBuilder.cs` — 결정론적 픽셀 생성 + 임포트 고정
- `game/Assets/Scripts/Runtime/Presentation/` — EventArgs 2종·SpriteCatalog·PresentationTween·ShopPresentationController
- `game/Assets/Scripts/Runtime/DayCycle/GameEvents.cs` — 표현 이벤트 3종 확장
- `game/Assets/Scripts/Runtime/UI/{ServicePanel,SettlementPanel}Controller.cs` — 발행 훅 + 카운트업
- `game/Assets/Scripts/Editor/SceneBuilder.cs` — BuildShopStage + 패널 4종 하단 재배치 + catalog 주입
- `game/Assets/Scenes/{MainMenu,Shop}.unity` — 재생성 (무대 포함)
- `game/Assets/Tests/EditMode/` — 신규 테스트 4파일 (+18 케이스)
- `kb/tasks/task-108/{manifest,implementation-notes}.md`, `kb/artifacts/task-108-summary.md`
