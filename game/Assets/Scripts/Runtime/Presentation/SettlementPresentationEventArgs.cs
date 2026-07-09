using ClientIsKing.Settlement;

namespace ClientIsKing.Presentation
{
    /// <summary>정산 표현 이벤트 payload — SettlementResult 를 표현 계층 계약으로 전달 (task-108 설계 7단계).</summary>
    public readonly struct SettlementPresentationEventArgs
    {
        public SettlementPresentationEventArgs(
            int day, int grossRevenue, int ingredientSpend, int operatingCost, int netProfit,
            int cashBefore, int cashAfter, bool bankrupt, string message)
        {
            Day = day;
            GrossRevenue = grossRevenue;
            IngredientSpend = ingredientSpend;
            OperatingCost = operatingCost;
            NetProfit = netProfit;
            CashBefore = cashBefore;
            CashAfter = cashAfter;
            Bankrupt = bankrupt;
            Message = message ?? "";
        }

        public int Day { get; }
        public int GrossRevenue { get; }
        public int IngredientSpend { get; }
        public int OperatingCost { get; }
        public int NetProfit { get; }
        public int CashBefore { get; }
        public int CashAfter { get; }
        public bool Bankrupt { get; }
        public string Message { get; }

        public static SettlementPresentationEventArgs From(SettlementResult result)
        {
            return new SettlementPresentationEventArgs(
                result.Day, result.GrossRevenue, result.IngredientSpend, result.OperatingCost,
                result.NetProfit, result.CashBefore, result.CashAfter, result.Bankrupt, result.Message);
        }
    }
}
