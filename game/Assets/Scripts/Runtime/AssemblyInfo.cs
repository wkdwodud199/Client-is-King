using System.Runtime.CompilerServices;

// Editor 빌더(InitialDataBuilder/SceneBuilder)가 EditorInit 로 데이터/참조를 주입할 수 있게 한다 (task-103).
[assembly: InternalsVisibleTo("ClientIsKing.Editor")]
// 테스트 fixture 가 시드에 없는 조합(예: 중복 kind 레시피)을 EditorInit 로 만들 수 있게 한다 (task-106).
[assembly: InternalsVisibleTo("ClientIsKing.Tests.EditMode")]
