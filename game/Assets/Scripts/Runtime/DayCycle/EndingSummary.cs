namespace ClientIsKing.DayCycle
{
    /// <summary>엔딩 오버레이 표시 전용 DTO (전부 기존 필드에서 파생, task-115 C2). UI 는 재계산하지 않는다.</summary>
    public sealed class EndingSummary
    {
        public RunEndingStatus Status;

        /// <summary>state.daysCompleted.</summary>
        public int DaysCompleted;

        /// <summary>state.cash.</summary>
        public int FinalCash;

        /// <summary>state.cash - GameState.StartingCash (파산 시 음수 정직 표기).</summary>
        public int NetProfit;

        /// <summary>SNSCampaignOps.CalculateFollowerDisplay(state.snsCampaignHistory).</summary>
        public int FollowerDisplay;

        /// <summary>state.bankruptcyReason ("" = 비파산).</summary>
        public string BankruptcyReason;
    }
}
