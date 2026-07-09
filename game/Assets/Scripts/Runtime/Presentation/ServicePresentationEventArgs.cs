namespace ClientIsKing.Presentation
{
    /// <summary>
    /// Service 표현 이벤트 payload — 현재 주문 표시(ServiceOrderPresented)와
    /// 서빙/포기 결과(ServiceOutcomeResolved)가 공용으로 쓴다 (task-108 설계 6단계).
    /// 처리 "전" 주문 정보를 캡처해 담는다 — 도메인 상태 변화와 독립.
    /// </summary>
    public readonly struct ServicePresentationEventArgs
    {
        public ServicePresentationEventArgs(
            bool hasOrder, int day, int orderNumber, int totalOrders,
            string customerId, string recipeId, int partySize,
            bool served, bool missed, int revenueGained, string message)
        {
            HasOrder = hasOrder;
            Day = day;
            OrderNumber = orderNumber;
            TotalOrders = totalOrders;
            CustomerId = customerId ?? "";
            RecipeId = recipeId ?? "";
            PartySize = partySize;
            Served = served;
            Missed = missed;
            RevenueGained = revenueGained;
            Message = message ?? "";
        }

        /// <summary>false 면 "표시할 주문 없음" 신호 — 손님 슬롯을 비운다.</summary>
        public bool HasOrder { get; }
        public int Day { get; }
        /// <summary>오늘 몇 번째 주문인가 (1-base, 표시용).</summary>
        public int OrderNumber { get; }
        public int TotalOrders { get; }
        public string CustomerId { get; }
        public string RecipeId { get; }
        public int PartySize { get; }
        /// <summary>outcome 전용 — 서빙 성공.</summary>
        public bool Served { get; }
        /// <summary>outcome 전용 — 포기/이탈.</summary>
        public bool Missed { get; }
        /// <summary>outcome 전용 — 이번 결과로 얻은 매출 (포기/실패 시 0).</summary>
        public int RevenueGained { get; }
        public string Message { get; }

        /// <summary>표시할 주문 없음 신호.</summary>
        public static ServicePresentationEventArgs Empty(int day)
        {
            return new ServicePresentationEventArgs(false, day, 0, 0, "", "", 0, false, false, 0, "");
        }
    }
}
