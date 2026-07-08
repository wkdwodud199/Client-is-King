# 설계 문서 — task-103

> Status: ready
> Inputs: `kb/concepts/project-brief.md`, `kb/concepts/demo-scope.md`, `kb/tasks/task-102/design.md`, `kb/tasks/task-102/implementation-notes.md`, 현재 Unity 프로젝트 기준선(`game/`)
> Outputs: 코어 정적 데이터 모델 ScriptableObject 6종, 초기 데이터 에셋, 재현 가능한 데이터 생성/검증 설계
> Next step: Claude가 이 설계를 구현한 뒤 `task-104`에서 `MainMenu.unity`/`Shop.unity`, 하루 상태 머신, SceneBuilder 설계를 진행

## 목표 (Objective)

`Client is King`의 이후 게임플레이 task가 공통으로 참조할 정적 정의 계층을 만든다. 브리프가 지정한 ScriptableObject 6종(`IngredientDef`, `RecipeDef`, `GenreDef`, `CustomerArchetypeDef`, `SNSCampaignDef`, `GameEventDef`)과 데모 범위에 맞는 초기 데이터 에셋을 `game/` 프로젝트 안에 추가하고, EditMode 테스트로 데이터 무결성을 검증한다.

## 범위 (Scope)

- 포함:
  - `game/Assets/Scripts/Runtime/Data/` 하위에 정적 정의용 ScriptableObject 타입 6종을 추가한다.
  - 6종 타입을 보조하는 순수 C# enum/serializable struct를 추가한다. 단, 추가 concrete ScriptableObject asset type은 만들지 않는다.
  - 모든 정의에는 사람이 읽는 표시명과 별도로 안정적인 ASCII `id`를 둔다. 런타임/테스트는 표시명이나 파일명 대신 `id`와 serialized reference를 기준으로 검증한다.
  - `RecipeDef`는 `IngredientKind`와 수량으로 재료 요구량을 표현하고, 실제 구매/품질 차이는 `IngredientDef`의 C/B 등급 데이터가 담당하도록 분리한다.
  - `GenreDef`는 국밥/분식/면류/제네럴리스트 4종을 포함하고, 원가·조리시간·객단가·고객층 친화도 배수를 담는다.
  - `SNSCampaignDef`는 데모 채널 3종의 정적 비용·도달률·감쇠 파라미터를 담는다.
  - `GameEventDef`는 데모 이벤트 4종(재료값 폭등, 위생 점검, 임대료 인상, 단체 손님)의 정적 파라미터를 담는다.
  - 초기 데이터 에셋은 `game/Assets/Data/Definitions/` 아래에 타입별 하위 폴더로 생성한다.
  - 초기 데이터는 Editor 전용 빌더(`InitialDataBuilder.Apply`)로 재생성 가능하게 만들고, 배치 모드에서 실행할 수 있게 한다.
  - EditMode 테스트를 추가해 타입별 asset count, id uniqueness, recipe reference, genre/customer/SNS/event 하드캡을 검증한다.
  - 구현 완료 기록으로 `kb/tasks/task-103/manifest.md`, `implementation-notes.md`, `kb/artifacts/task-103-summary.md`, `kb/index/status.md`를 갱신하도록 포함한다.
- 제외:
  - `GameState`, 저장/불러오기, 런타임 인벤토리, 경제 계산, 손님 생성, SNS 효과 적용, 이벤트 발생 로직은 구현하지 않는다.
  - `MainMenu.unity`, `Shop.unity`, SceneBuilder, 프리팹/UI/카메라/씬 배치는 `task-104` 이후 범위다.
  - 장보기 UI, 구매/판매 계산, 조리·서빙 루프, 정산/파산은 `task-105`부터 다룬다.
  - 장르 선택 UX와 장르별 전략 시스템은 `task-108` 범위다. 이 task는 장르 정적 데이터만 제공한다.
  - SNS 마케팅 실행 로직은 `task-109`, 이벤트/장애물 실행 로직은 `task-110`으로 미룬다.
  - Steam 연동, 직원 고용, 인테리어, 다점포, 미슐랭 트랙, 조리 미니게임 등 `demo-scope.md`의 주차장 기능은 포함하지 않는다.

## 제약 (Constraints)

- `kb/concepts/project-brief.md`가 데이터 종류와 데모 하드캡의 SSOT다. 이 설계는 브리프의 ScriptableObject 6종을 재정의하거나 늘리지 않는다.
- Unity 프로젝트 경로는 `game/`이다. 데이터 에셋과 `.meta` 파일은 모두 `game/Assets/` 아래에 둔다.
- Unity 에디터는 task-102 기준선과 동일하게 6000.3.8f1을 사용한다. 구현 환경에 유효한 Unity 라이선스가 없으면 배치 검증이 차단될 수 있으므로 구현 노트에 기록한다.
- 정적 데이터 정의에는 `Dictionary`를 쓰지 않는다. 브리프의 저장 규약과 충돌하지 않도록 lists/arrays와 serialized reference를 사용한다.
- ScriptableObject는 데이터 컨테이너로 유지한다. 구매/조리/마케팅/이벤트 효과를 계산하는 mutable runtime behavior를 SO 안에 넣지 않는다.
- asset path와 `id`는 소문자 snake_case 또는 kebab 없는 ASCII로 유지한다. 한글은 표시명/설명 필드에만 사용한다.
- 테스트 어셈블리에서 Runtime 타입을 볼 수 있도록 `ClientIsKing.Tests.EditMode.asmdef`에 `ClientIsKing.Runtime` 참조를 추가한다.
- 초기 데이터 빌더는 멱등이어야 한다. 같은 path의 asset이 있으면 값을 갱신하고, GUID가 불필요하게 바뀌지 않도록 삭제 후 재생성 방식을 피한다.

## 구현 단계 (Implementation Steps)

1. `game/Assets/Scripts/Runtime/Data/`를 만들고 공통 enum/serializable struct를 정의한다. 필요한 enum은 `IngredientKind`, `IngredientGrade`, `GenreKind`, `AgeBand`, `GenderTarget`, `SNSChannel`, `GameEventKind` 정도로 제한한다.
2. ScriptableObject 6종을 구현한다. 각 타입은 `[CreateAssetMenu]`, private `[SerializeField]` 필드, read-only public property를 갖고, 최소 검증이 가능하도록 `id`, `displayName`, 설명/수치 필드를 노출한다.
3. `RecipeDef`에는 `GenreDef` reference, `List<RecipeIngredientRequirement>`, 조리 시간, 판매가를 둔다. `RecipeIngredientRequirement`는 `IngredientKind`와 양수 수량만 담아 C/B 등급 선택을 후속 경제/인벤토리 task가 처리할 수 있게 한다.
4. `GenreDef`에는 `GenreKind`, 원가 배수, 조리시간 배수, 객단가 배수, `List<CustomerGenreAffinity>`를 둔다. 제네럴리스트는 별도 recipe genre가 아니라 균형형 선택지로 표시한다.
5. `CustomerArchetypeDef`에는 연령대, 성별 타겟, 기본 출현 가중치, 인내 시간, 가격 민감도, 기본 파티 크기 범위를 둔다. 이 값은 후속 손님 생성/SNS 분포 계산의 입력일 뿐, 생성 로직은 넣지 않는다.
6. `SNSCampaignDef`에는 채널, 기본 비용, 기본 도달률, 반복 사용 감쇠율, 타겟 연령/성별 친화 배수를 둔다. 실제 마케팅 효과 계산은 task-109에 남긴다.
7. `GameEventDef`에는 이벤트 종류, 기본 발생 가중치, 기간(day), 퍼센트/고정값 효과 파라미터, 설명을 둔다. 이벤트를 적용하는 manager나 scheduler는 만들지 않는다.
8. `game/Assets/Scripts/Editor/InitialDataBuilder.cs`를 추가해 초기 데이터 asset을 생성/갱신한다. 빌더는 `ClientIsKing.EditorTools.InitialDataBuilder.Apply` 정적 메서드로 배치 실행 가능해야 한다.
9. 초기 데이터 세트는 다음 최소 구성을 포함한다: 재료 18개(9개 `IngredientKind` × C/B), 레시피 6개(국밥/분식/면류 각 2개), 장르 4개(국밥/분식/면류/제네럴리스트), 고객 archetype 4개 이상, SNS 채널 3개, 이벤트 4개.
10. `game/Assets/Tests/EditMode/DataDefinitionTests.cs`를 추가하고 `ClientIsKing.Tests.EditMode.asmdef`에 Runtime 참조를 더한다. 테스트는 AssetDatabase로 `Assets/Data/Definitions/`를 스캔해 asset count, id uniqueness, 필수 참조, 양수 수치, 하드캡을 검증한다.
11. Unity 배치 모드로 `InitialDataBuilder.Apply`를 실행한 뒤 컴파일 게이트와 EditMode 테스트를 실행한다. 마지막으로 구현 기록 문서와 status board를 갱신한다.

## 실행 계획 (Execution Plan)

- implement_model: claude-fable-5
- implement_effort: high
- routing_reason: M1의 첫 게임 데이터 구현으로 Runtime 타입·Editor 빌더·EditMode 검증을 함께 다루지만, 씬/게임플레이 로직은 제외하므로 `project-brief.md`의 task-103 라우팅대로 fable-5/high가 적합하다.

| unit | 파일 범위 | depends_on | group |
|------|-----------|------------|-------|
| U1-runtime-data-types | `game/Assets/Scripts/Runtime/Data/*.cs`, 관련 `.meta` | 없음 | G1 |
| U2-initial-data-builder | `game/Assets/Scripts/Editor/InitialDataBuilder.cs`, 관련 `.meta` | U1-runtime-data-types | G2 |
| U3-seed-data-assets | `game/Assets/Data/Definitions/**/*.asset`, 관련 `.meta` | U1-runtime-data-types, U2-initial-data-builder | G3 |
| U4-data-tests | `game/Assets/Tests/EditMode/DataDefinitionTests.cs`, `game/Assets/Tests/EditMode/ClientIsKing.Tests.EditMode.asmdef` | U1-runtime-data-types, U3-seed-data-assets | G4 |
| U5-validation-pass | Unity 배치 컴파일 로그, EditMode 결과 XML, `runtime/validator` 결과 | U2-initial-data-builder, U3-seed-data-assets, U4-data-tests | G5 |
| U6-task-records | `kb/tasks/task-103/manifest.md`, `kb/tasks/task-103/implementation-notes.md`, `kb/artifacts/task-103-summary.md`, `kb/index/status.md` | U5-validation-pass | G6 |

## 파일/모듈 영향 (Affected Files/Modules)

| 파일/모듈 | 변경 유형 | 설명 |
|-----------|-----------|------|
| `game/Assets/Scripts/Runtime/Data/IngredientDef.cs` | create | 재료 종류/등급별 구매 비용과 품질 값을 담는 ScriptableObject |
| `game/Assets/Scripts/Runtime/Data/RecipeDef.cs` | create | 레시피 장르, 재료 요구량, 조리 시간, 판매가를 담는 ScriptableObject |
| `game/Assets/Scripts/Runtime/Data/GenreDef.cs` | create | 국밥/분식/면류/제네럴리스트의 비용·시간·객단가·고객 친화도 배수를 담는 ScriptableObject |
| `game/Assets/Scripts/Runtime/Data/CustomerArchetypeDef.cs` | create | 고객층의 인구통계, 출현 가중치, 인내/가격 민감도, 파티 크기 범위를 담는 ScriptableObject |
| `game/Assets/Scripts/Runtime/Data/SNSCampaignDef.cs` | create | SNS 채널 3종의 비용, 도달률, 타겟 친화, 감쇠 파라미터를 담는 ScriptableObject |
| `game/Assets/Scripts/Runtime/Data/GameEventDef.cs` | create | 이벤트 4종의 발생 가중치, 기간, 효과 파라미터를 담는 ScriptableObject |
| `game/Assets/Scripts/Runtime/Data/DataTypes.cs` | create | 6종 SO가 공유하는 enum과 serializable struct 정의. 추가 concrete SO 타입은 만들지 않음 |
| `game/Assets/Scripts/Runtime/ClientIsKing.Runtime.asmdef` | modify | 필요 시 Runtime 데이터 폴더 포함 상태를 유지한다. 외부 패키지 참조 추가는 원칙적으로 불필요 |
| `game/Assets/Scripts/Editor/InitialDataBuilder.cs` | create | 초기 데이터 asset을 멱등 생성/갱신하는 Editor 전용 배치 진입점 |
| `game/Assets/Data/Definitions/Ingredients/` | create | 9개 재료 종류 × C/B 등급 초기 `IngredientDef` asset |
| `game/Assets/Data/Definitions/Recipes/` | create | 국밥/분식/면류 각 2개, 총 6개 초기 `RecipeDef` asset |
| `game/Assets/Data/Definitions/Genres/` | create | 국밥/분식/면류/제네럴리스트 4개 `GenreDef` asset |
| `game/Assets/Data/Definitions/Customers/` | create | 최소 4개 `CustomerArchetypeDef` asset |
| `game/Assets/Data/Definitions/SNS/` | create | 3개 `SNSCampaignDef` asset |
| `game/Assets/Data/Definitions/Events/` | create | 4개 `GameEventDef` asset |
| `game/Assets/Tests/EditMode/ClientIsKing.Tests.EditMode.asmdef` | modify | `ClientIsKing.Runtime` 참조를 추가해 테스트에서 데이터 타입을 직접 검증 |
| `game/Assets/Tests/EditMode/DataDefinitionTests.cs` | create | 초기 데이터 asset의 id, 참조, 하드캡, 양수 수치를 검증하는 EditMode 테스트 |
| `game/**/*.meta` | create/modify | Unity가 생성하는 script/data asset 메타 파일. 에셋과 쌍으로 추적 |
| `kb/tasks/task-103/manifest.md` | modify | done-gate 통과를 위해 inputs/concepts_needed/related_files placeholder를 실제 값으로 교체 |
| `kb/tasks/task-103/implementation-notes.md` | create | 구현 결정, 초기 데이터 목록, 검증 결과, Unity 환경 이슈를 기록 |
| `kb/artifacts/task-103-summary.md` | create | task-103 완료 산출물 요약과 task-104 인계 사항 기록 |
| `kb/index/status.md` | modify | `runtime/generate-status.py`로 재생성되는 task 상태 보드 |

## 테스트 기준 (Test Criteria)

- [ ] `python runtime/validator/cli.py kb/tasks/task-103/design.md`가 0으로 통과한다.
- [ ] Unity 배치 모드에서 초기 데이터 빌더가 성공한다: `Unity.exe -batchmode -quit -nographics -projectPath game -executeMethod ClientIsKing.EditorTools.InitialDataBuilder.Apply -logFile <temp-log>` 종료 코드가 0이다.
- [ ] 배치 컴파일 게이트가 통과한다: `Unity.exe -batchmode -quit -nographics -projectPath game -logFile <temp-log>` 종료 코드가 0이고 로그에 `error CS`가 없다.
- [ ] EditMode 테스트가 통과한다: `Unity.exe -batchmode -nographics -projectPath game -runTests -testPlatform EditMode -testResults <temp-results> -logFile <temp-log>` 종료 코드가 0이다.
- [ ] `DataDefinitionTests`가 `Assets/Data/Definitions/` 아래에서 6종 ScriptableObject asset을 모두 찾고, 타입별 `id`가 비어 있지 않으며 중복되지 않음을 검증한다.
- [ ] 초기 데이터 count가 설계와 일치한다: 재료 18개, 레시피 6개, 장르 4개, 고객 archetype 4개 이상, SNS 채널 3개, 이벤트 4개.
- [ ] 모든 `RecipeDef`는 concrete genre(국밥/분식/면류 중 하나)를 참조하고, 하나 이상의 재료 요구량과 양수 조리 시간/판매가를 가진다.
- [ ] 모든 recipe 요구 재료의 `IngredientKind`에는 C/B 두 등급의 `IngredientDef`가 모두 존재한다.
- [ ] `GenreDef`는 국밥/분식/면류/제네럴리스트를 정확히 한 개씩 포함하고, 제네럴리스트는 recipe의 직접 장르로 쓰이지 않는다.
- [ ] `SNSCampaignDef`는 채널 3종을 정확히 한 개씩 포함하고, `GameEventDef`는 이벤트 4종을 정확히 한 개씩 포함한다.
- [ ] 구현 완료 후 `python runtime/validator/cli.py --check-done task-103`와 `python runtime/generate-status.py --check`가 통과한다.

## 오픈 이슈 (Open Issues)

- 없음. 구체적인 밸런스 수치는 후속 playtest에서 조정할 수 있지만, 이 task에서는 하드캡과 참조 무결성을 먼저 고정한다.
