# 산출물 요약 — task-103

> Status: done
> Inputs: kb/tasks/task-103/implementation-notes.md
> Outputs: 이 요약 문서
> Next step: task-104 설계 요청 (씬 2종 + 하루 상태 머신 + SceneBuilder, M1)

## 작업 요약

- **Task ID**: task-103
- **제목**: 코어 데이터 모델 (SO 6종 + 초기 데이터)
- **완료일**: 2026-07-09

## 산출물 목록

| 산출물 | 경로 | 설명 |
|--------|------|------|
| 데이터 타입 | `game/Assets/Scripts/Runtime/Data/DataTypes.cs` | enum 7종(재료/등급/장르/연령/성별/SNS/이벤트) + 직렬화 struct 4종 |
| SO 6종 | `game/Assets/Scripts/Runtime/Data/*Def.cs` | Ingredient/Recipe/Genre/CustomerArchetype/SNSCampaign/GameEvent — 데이터 컨테이너 (로직 없음) |
| 시드 빌더 | `game/Assets/Scripts/Editor/InitialDataBuilder.cs` | 멱등 Upsert (GUID 안정), 배치 `-executeMethod` 진입점 |
| 시드 에셋 39개 | `game/Assets/Data/Definitions/**` | 재료 18(9종×C/B) · 레시피 6(장르별 2) · 장르 4 · 고객 4 · SNS 3 · 이벤트 4 |
| 무결성 테스트 | `game/Assets/Tests/EditMode/DataDefinitionTests.cs` | 하드캡·id 유일성·참조 정합·양수 수치 7케이스 |
| task 기록 | `kb/tasks/task-103/`, `kb/artifacts/task-103-summary.md` | manifest(provenance)·구현 노트·요약 |

## 주요 결정

- **재료 9종은 레시피 6종에서 역산** — 전 종류가 레시피에 사용되어 C/B 쌍 검증이 전수 커버.
- **id = 소문자 snake_case ASCII**, 한글은 표시명/설명 전용. SNS 는 가상 브랜드 3종(픽쳐그램/숏핑/동네게시판).
- **SO 필드 주입은 `internal EditorInit` + InternalsVisibleTo(Editor)** — private SerializeField 유지 + 재현 가능 주입.
- **`durationDays 0 = 영구` 규약** (임대료 인상) — 적용 해석은 task-110 에서 확정.
- 밸런스 수치는 초안 — **하드캡·무결성이 계약**, 수치는 M1 플레이테스트에서 조정.

## 검증

- 배치 시드 빌더 exit 0 (18/6/4/4/3/4) · 컴파일 게이트 exit 0 (`error CS` 0건) · **EditMode 13/13 통과**

## 관련 문서

- 설계: `kb/tasks/task-103/design.md`
- 구현 노트: `kb/tasks/task-103/implementation-notes.md`
