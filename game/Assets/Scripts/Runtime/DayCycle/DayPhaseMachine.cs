using System;

namespace ClientIsKing.DayCycle
{
    /// <summary>
    /// 하루 사이클 상태 머신 (순수 C# — Unity 의존 없음, EditMode 테스트 대상).
    /// 전환 순서: Market → Service → Settlement → Night → Market (Night→Market 에서만 day +1).
    /// 전환 시 GameEvents.DayPhaseChanged 를 정확히 1회 발행한다.
    /// </summary>
    public sealed class DayPhaseMachine
    {
        private readonly GameState state;

        /// <param name="state">null 이면 명확한 예외, day 1 미만이면 1 로 보정 (설계 3단계).</param>
        public DayPhaseMachine(GameState state)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state), "GameState 없이 상태 머신을 만들 수 없다");
            if (this.state.day < 1)
            {
                this.state.day = 1;
            }
        }

        public GameState State => state;

        /// <summary>다음 phase 로 전환하고 그 값을 반환한다.</summary>
        public DayPhase Advance()
        {
            var previous = state.currentPhase;
            var next = Next(previous);
            state.currentPhase = next;
            if (previous == DayPhase.Night && next == DayPhase.Market)
            {
                state.day++;
            }
            GameEvents.RaiseDayPhaseChanged(new DayPhaseChangedEventArgs(state.day, previous, next));
            return next;
        }

        /// <summary>phase 순서 규칙의 단일 원천.</summary>
        public static DayPhase Next(DayPhase phase)
        {
            switch (phase)
            {
                case DayPhase.Market: return DayPhase.Service;
                case DayPhase.Service: return DayPhase.Settlement;
                case DayPhase.Settlement: return DayPhase.Night;
                case DayPhase.Night: return DayPhase.Market;
                default: throw new ArgumentOutOfRangeException(nameof(phase), phase, "unknown day phase");
            }
        }
    }
}
