namespace ClientIsKing.Settlement
{
    /// <summary>일일 정산 결과 — UI 와 테스트가 같은 계약을 쓴다 (task-107 설계 3단계).</summary>
    public readonly struct SettlementResult
    {
        public SettlementResult(
            int day, bool applied, bool alreadyApplied, bool bankrupt,
            int grossRevenue, int ingredientSpend, int operatingCost, int netProfit,
            int cashBefore, int cashAfter, int unpaidCost, string message)
        {
            Day = day;
            Applied = applied;
            AlreadyApplied = alreadyApplied;
            Bankrupt = bankrupt;
            GrossRevenue = grossRevenue;
            IngredientSpend = ingredientSpend;
            OperatingCost = operatingCost;
            NetProfit = netProfit;
            CashBefore = cashBefore;
            CashAfter = cashAfter;
            UnpaidCost = unpaidCost;
            Message = message;
        }

        public int Day { get; }
        /// <summary>이번 호출에서 cash 차감이 실제로 적용됐는가.</summary>
        public bool Applied { get; }
        /// <summary>같은 day 에 이미 정산된 상태였는가 (재호출 — cash 불변).</summary>
        public bool AlreadyApplied { get; }
        public bool Bankrupt { get; }
        /// <summary>당일 매출 (표시용 — cash 에는 서빙 시점에 이미 반영됨).</summary>
        public int GrossRevenue { get; }
        /// <summary>당일 재료 구매 지출 (표시용 — cash 에는 구매 시점에 이미 반영됨).</summary>
        public int IngredientSpend { get; }
        public int OperatingCost { get; }
        /// <summary>표시용 순손익 = 매출 − 재료 지출 − 운영비. 실제 정산 delta 는 운영비뿐.</summary>
        public int NetProfit { get; }
        public int CashBefore { get; }
        public int CashAfter { get; }
        /// <summary>파산 시 미납 운영비 (비파산이면 0).</summary>
        public int UnpaidCost { get; }
        public string Message { get; }
    }
}
