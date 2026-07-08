# 구현 노트 — task-103

> Status: done
> Inputs: kb/tasks/task-103/design.md
> Outputs: SO 6종 + 공유 타입, 멱등 시드 빌더, 시드 에셋 39개, 데이터 무결성 테스트 7종
> Next step: task-104 설계 요청 (`runtime/codex-design.ps1 task-104 --auto`) — 씬 2종 + 하루 상태 머신 + SceneBuilder

## 설계 대비 변경 사항

| 항목 | 설계 내용 | 실제 구현 | 변경 사유 |
|------|-----------|-----------|-----------|
| SO 필드 주입 방식 | (설계 미규정 — private SerializeField + read-only property 요구) | `#if UNITY_EDITOR` `internal EditorInit(...)` + Runtime 에 `AssemblyInfo.cs` (`InternalsVisibleTo("ClientIsKing.Editor")`) | private 필드를 유지하면서 Editor 빌더가 값을 주입할 표준 경로 필요. 빌드에서는 컴파일 제외 |
| 이벤트 지속 규약 | 기간(day) 필드 | `durationDays = 0` 을 "영구" 로 규약 (임대료 인상) | 인상은 되돌아가지 않는 이벤트 — 적용 해석은 task-110 에서 확정하도록 XML 주석에 명시 |
| 고객 archetype 수 | "4개 이상" | 정확히 4개 (연령대 4구간 × 1) | 데모 분포 계산의 최소 직교 셋. 추가는 후속 밸런싱에서 |

## 구현 결정 기록

1. **재료 9종 선정** — 레시피 6종(국밥 2·분식 2·면류 2)의 실제 식자재로 역산:
   쌀·떡·소면·돼지고기·소고기·어묵·김·채소·고추장. 9종 전부가 최소 1개 레시피에서 사용되어
   "요구 재료마다 C/B 존재" 검증이 전 종류를 커버한다.
2. **id 규약**: 소문자 snake_case ASCII (`rice_c`, `pork_gukbap`, `photo_feed`), 한글은 displayName/설명에만
   (설계 제약 준수). 파일명 = id.
3. **SNS 채널은 가상 브랜드**: 픽쳐그램(PhotoFeed)/숏핑(ShortForm)/동네게시판(LocalBoard) —
   실제 상표 회피 + 타겟층 차별화 (20-30 여성 / 10-20 / 40-50).
4. **빌더 멱등성**: `Upsert` = LoadAssetAtPath 후 없을 때만 CreateAsset (삭제/재생성 금지 → GUID 안정,
   설계 제약). 값은 매 실행 `EditorInit` 으로 갱신 + `SetDirty` → 시드 수치 변경 시 재실행만 하면 됨.
5. **빌더 순서 = 참조 방향 역순**: 재료 → 고객 → 장르(고객 참조) → 레시피(장르 참조) → SNS → 이벤트.
6. **밸런스 초안 수치**: 장르 트레이드오프는 브리프 방향대로 — 국밥(원가 1.15/조리 1.2/객단가 1.25, 중장년 친화),
   분식(0.85/0.8/0.75, 젊은층), 면류(0.95/1.0/0.95, 균형), 제네럴리스트(전부 1.0).
   수치는 하드캡/무결성만 계약이고 M1 플레이테스트에서 조정.

## 발생한 이슈

- 없음. task-102 에서 확립한 배치 게이트 3종이 그대로 재사용되었고, 라이선스도 유효해
  전 과정이 사용자 개입 없이 완료됐다.

## 테스트 결과

| 테스트 기준 (design.md 참조) | 결과 | 비고 |
|------------------------------|------|------|
| validator rc0 (design.md) | pass | 러너 게이트 |
| 배치 시드 빌더 exit 0 | pass | `[InitialDataBuilder] seed data applied (18/6/4/4/3/4)` |
| 배치 컴파일 게이트 exit 0 + `error CS` 없음 | pass | 명시적 2차 실행으로 확인 |
| EditMode 테스트 exit 0 | pass | **13/13 통과** (기준선 6 + 데이터 7) |
| 6종 SO 에셋 존재 + id 유일성 | pass | `DataDefinitionTests` 7케이스 |
| 카운트: 재료 18·레시피 6·장르 4·고객 4·SNS 3·이벤트 4 | pass | 폴더별 실측 + 테스트 이중 확인 |
| 레시피: concrete 장르 + 재료 ≥1 + 양수 조리시간/판매가 | pass | 장르별 2개 분포까지 검증 |
| 요구 재료 kind 마다 C/B 등급 존재 | pass | 9종 전부 커버 |
| 장르 4종 정확히 1개씩 + 제네럴리스트 비-레시피 장르 | pass | |
| SNS 3채널·이벤트 4종 정확히 1개씩 | pass | |
| `--check-done` + `generate-status --check` | pass | 기록 완료 후 실행 (요약 문서 참조) |

## 산출물

- `game/Assets/Scripts/Runtime/Data/DataTypes.cs` — enum 7종 + 공유 직렬화 struct 4종
- `game/Assets/Scripts/Runtime/Data/{Ingredient,Recipe,Genre,CustomerArchetype,SNSCampaign,GameEvent}Def.cs` — SO 6종
- `game/Assets/Scripts/Runtime/AssemblyInfo.cs` — InternalsVisibleTo(Editor)
- `game/Assets/Scripts/Editor/InitialDataBuilder.cs` — 멱등 시드 빌더 (배치 진입점)
- `game/Assets/Data/Definitions/**` — 시드 에셋 39개 (+.meta)
- `game/Assets/Tests/EditMode/DataDefinitionTests.cs` — 무결성 테스트 7종
- `game/Assets/Tests/EditMode/ClientIsKing.Tests.EditMode.asmdef` — ClientIsKing.Runtime 참조 추가
- `kb/tasks/task-103/{manifest,implementation-notes}.md`, `kb/artifacts/task-103-summary.md` — 기록
