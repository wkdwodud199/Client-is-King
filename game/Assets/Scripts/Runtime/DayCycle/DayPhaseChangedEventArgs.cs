namespace ClientIsKing.DayCycle
{
    /// <summary>GameEvents.DayPhaseChanged payload — 전환 후 일차와 이전/현재 phase.</summary>
    public readonly struct DayPhaseChangedEventArgs
    {
        public DayPhaseChangedEventArgs(int day, DayPhase previousPhase, DayPhase currentPhase)
        {
            Day = day;
            PreviousPhase = previousPhase;
            CurrentPhase = currentPhase;
        }

        /// <summary>전환이 반영된 뒤의 일차 (Night→Market 이면 +1 된 값).</summary>
        public int Day { get; }
        public DayPhase PreviousPhase { get; }
        public DayPhase CurrentPhase { get; }
    }
}
