using System;

namespace ClientIsKing.DayCycle
{
    /// <summary>
    /// C# event 기반 런타임 이벤트 허브의 초기 골격 (브리프 규약: 이벤트버스 라이브러리 금지).
    /// task-104 시점에는 DayPhaseChanged 하나만 둔다 — 후속 시스템(경제/서빙/SNS)이 이벤트를 추가한다.
    /// </summary>
    public static class GameEvents
    {
        /// <summary>하루 phase 전환마다 정확히 1회 발행된다.</summary>
        public static event Action<DayPhaseChangedEventArgs> DayPhaseChanged;

        /// <summary>발행은 상태 머신(DayPhaseMachine)만 호출한다 (같은 어셈블리 internal).</summary>
        internal static void RaiseDayPhaseChanged(DayPhaseChangedEventArgs args)
        {
            DayPhaseChanged?.Invoke(args);
        }
    }
}
