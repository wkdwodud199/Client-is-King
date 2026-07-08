using ClientIsKing.Data;

namespace ClientIsKing.Service
{
    /// <summary>서빙/포기 시도 결과 — UI 와 테스트가 같은 계약을 쓴다.</summary>
    public readonly struct ServiceResult
    {
        public ServiceResult(bool success, string message, int revenueGained, int cashAfter)
        {
            Success = success;
            Message = message;
            RevenueGained = revenueGained;
            CashAfter = cashAfter;
        }

        public bool Success { get; }
        /// <summary>UI 에 그대로 표시하는 한국어 메시지.</summary>
        public string Message { get; }
        /// <summary>이번 시도로 늘어난 매출 (실패/포기 시 0).</summary>
        public int RevenueGained { get; }
        /// <summary>시도 후 자금 (실패 시 변동 없음).</summary>
        public int CashAfter { get; }
    }

    /// <summary>레시피×파티 크기에서 계산된 종류별 필요 재료량 (kind 중복 합산 후).</summary>
    public readonly struct RequiredIngredient
    {
        public RequiredIngredient(IngredientKind kind, int quantity)
        {
            Kind = kind;
            Quantity = quantity;
        }

        public IngredientKind Kind { get; }
        public int Quantity { get; }
    }
}
