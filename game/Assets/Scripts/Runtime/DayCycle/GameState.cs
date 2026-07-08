using System;
using System.Collections.Generic;
using ClientIsKing.Inventory;
using ClientIsKing.Service;

namespace ClientIsKing.DayCycle
{
    /// <summary>
    /// 런타임 상태 컨테이너 (task-105 시점: day/phase + 자금 + 재료 인벤토리).
    /// 순수 C# + public 필드 — 브리프의 JsonUtility 직렬화 규약(Dictionary 금지) 전제.
    /// 통계/저장 포맷 확장은 task-106+ 에서 이 클래스에 추가한다.
    /// </summary>
    [Serializable]
    public sealed class GameState
    {
        /// <summary>데모 시작 자금 (초안 밸런스 — playtest 조정 대상, task-105 설계 2단계).</summary>
        public const int StartingCash = 30000;

        /// <summary>현재 일차 (1부터 시작).</summary>
        public int day = 1;

        /// <summary>현재 하루 phase.</summary>
        public DayPhase currentPhase = DayPhase.Market;

        /// <summary>보유 자금 (원, 음수 불허 — 경제 규칙이 보장).</summary>
        public int cash = StartingCash;

        /// <summary>재료 인벤토리 — 종류×등급별 항목의 List (Dictionary 금지 규약).</summary>
        public List<IngredientStock> ingredientStocks = new List<IngredientStock>();

        // ── Service phase 당일 상태 (task-106) — 하루 마감/리셋은 task-107 ──

        /// <summary>serviceOrders 가 속한 일차 (0 = 아직 영업 시작 전).</summary>
        public int serviceDay = 0;

        /// <summary>당일 주문 목록 (id 참조 기반 — SO 직접 참조 금지 규약).</summary>
        public List<ServiceOrderState> serviceOrders = new List<ServiceOrderState>();

        /// <summary>다음 미처리 주문 위치 (전부 처리되면 목록 길이).</summary>
        public int serviceCurrentOrderIndex = 0;

        /// <summary>당일 누적 매출 (원).</summary>
        public int serviceRevenueToday = 0;

        public int serviceOrdersServedToday = 0;
        public int serviceOrdersMissedToday = 0;
        public int serviceCustomersServedToday = 0;
        public int serviceCustomersMissedToday = 0;

        // ── 당일 구매 지출 추적 (task-107 — EconomyOps 가 구매 성공 시 누적) ──

        /// <summary>marketSpendToday 가 속한 일차 (day 가 바뀌면 자동 리셋).</summary>
        public int marketSpendDay = 0;

        /// <summary>당일 재료 구매 지출 합계 (원).</summary>
        public int marketSpendToday = 0;

        // ── 일일 정산 기록 (task-107 — SettlementOps 소유, day 당 1회) ──

        /// <summary>마지막으로 정산이 적용된 일차 (0 = 아직 없음).</summary>
        public int settlementDay = 0;

        public int settlementGrossRevenue = 0;
        public int settlementIngredientSpend = 0;
        public int settlementOperatingCost = 0;
        public int settlementNetProfit = 0;
        public int settlementCashBefore = 0;
        public int settlementCashAfter = 0;

        /// <summary>정산까지 마친 완료 일수.</summary>
        public int daysCompleted = 0;

        // ── 파산 상태 (task-107 — 진행 차단 게이트) ──

        public bool isBankrupt = false;
        public int bankruptcyDay = 0;
        public string bankruptcyReason = "";
    }
}
