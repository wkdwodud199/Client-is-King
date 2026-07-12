using ClientIsKing.DayCycle;

namespace ClientIsKing.Save
{
    /// <summary>
    /// MainMenu 이어하기 표시 전용 DTO (task-113 C절). 검증을 통과한 GameState 의 표시값만 담고
    /// UI 는 이 값을 그대로 보여준다 — 재계산하지 않는다.
    /// </summary>
    public sealed class SaveSummary
    {
        public int Day;
        public DayPhase Phase;
        public int Cash;
        public int DaysCompleted;
        public bool IsBankrupt;
        public string SelectedGenreId;
    }
}
