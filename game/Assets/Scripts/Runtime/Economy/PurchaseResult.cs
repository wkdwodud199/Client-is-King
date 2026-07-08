namespace ClientIsKing.Economy
{
    /// <summary>구매 시도 결과 — UI 와 테스트가 같은 계약을 쓴다 (task-105 설계 3단계).</summary>
    public readonly struct PurchaseResult
    {
        public PurchaseResult(bool success, string message, int totalCost, int cashAfter, int quantityAfter)
        {
            Success = success;
            Message = message;
            TotalCost = totalCost;
            CashAfter = cashAfter;
            QuantityAfter = quantityAfter;
        }

        public bool Success { get; }
        /// <summary>UI 에 그대로 표시하는 한국어 메시지.</summary>
        public string Message { get; }
        /// <summary>시도한 구매의 총액 (실패 시에도 계산 가능한 경우 채움).</summary>
        public int TotalCost { get; }
        /// <summary>시도 후 자금 (실패 시 변동 없음).</summary>
        public int CashAfter { get; }
        /// <summary>시도 후 해당 재료(종류×등급) 보유 수량 (실패 시 변동 없음).</summary>
        public int QuantityAfter { get; }
    }
}
