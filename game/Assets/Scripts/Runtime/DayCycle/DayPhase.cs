namespace ClientIsKing.DayCycle
{
    /// <summary>
    /// 하루 사이클 phase 4종 (브리프 고정: Market → Service → Settlement → Night).
    /// 씬 전환이 아니라 Shop 씬 내부 상태다.
    /// </summary>
    public enum DayPhase
    {
        /// <summary>장보기 — 재료 구매 (task-105).</summary>
        Market,
        /// <summary>영업 — 조리·서빙 (task-106).</summary>
        Service,
        /// <summary>정산 — 하루 마감 계산 (task-107).</summary>
        Settlement,
        /// <summary>밤 — SNS 마케팅 + 저장 (task-109/111).</summary>
        Night,
    }
}
