using System.Runtime.CompilerServices;

// Editor 빌더(InitialDataBuilder)가 EditorInit 로 시드 데이터를 주입할 수 있게 한다 (task-103).
// 테스트는 public 프로퍼티만 읽으므로 internal 접근이 필요 없다.
[assembly: InternalsVisibleTo("ClientIsKing.Editor")]
