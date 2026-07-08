using System;
using ClientIsKing.DayCycle;

namespace ClientIsKing.Settlement
{
    /// <summary>
    /// 일일 정산 핵심 규칙 (순수 C# — EditMode 테스트 대상, 매니저는 thin wrapper).
    ///
    /// 계약 (task-107 설계):
    /// - 매출/재료 지출은 각 시점(cash 반영 완료)의 표시용 요약 — 정산의 실제 cash delta 는 운영비뿐.
    /// - 정산은 day 당 정확히 1회 (멱등 — 재호출은 기존 결과 재구성, cash 불변).
    /// - cash < 운영비면 파산: cash 0 고정, 음수 불허, 사유/미납액 기록.
    /// </summary>
    public static class SettlementOps
    {
        /// <summary>일일 운영비 (임대료+운영, 데모 초안 — 임대료 인상 이벤트는 task-110).</summary>
        public const int DailyOperatingCost = 12000;

        public static bool IsSettlementApplied(GameState state)
        {
            Require(state);
            return state.settlementDay == state.day;
        }

        /// <summary>
        /// 오늘 정산을 적용한다. 이미 적용된 날이면 저장된 정산 필드로 결과만 재구성한다 (설계 5단계).
        /// </summary>
        public static SettlementResult ApplyDailySettlement(GameState state)
        {
            Require(state);

            if (IsSettlementApplied(state))
            {
                return new SettlementResult(
                    state.day, applied: false, alreadyApplied: true, bankrupt: state.isBankrupt,
                    state.settlementGrossRevenue, state.settlementIngredientSpend,
                    state.settlementOperatingCost, state.settlementNetProfit,
                    state.settlementCashBefore, state.settlementCashAfter,
                    state.isBankrupt && state.bankruptcyDay == state.day
                        ? state.settlementOperatingCost - state.settlementCashBefore : 0,
                    state.isBankrupt ? "파산 상태입니다." : "이미 정산이 완료된 날입니다.");
            }

            int gross = state.serviceRevenueToday;
            int spend = state.marketSpendDay == state.day ? state.marketSpendToday : 0;
            int cost = DailyOperatingCost;
            int net = gross - spend - cost;
            int cashBefore = state.cash;

            state.settlementDay = state.day;
            state.settlementGrossRevenue = gross;
            state.settlementIngredientSpend = spend;
            state.settlementOperatingCost = cost;
            state.settlementNetProfit = net;
            state.settlementCashBefore = cashBefore;

            if (cashBefore >= cost)
            {
                state.cash = cashBefore - cost;
                state.settlementCashAfter = state.cash;
                state.daysCompleted = Math.Max(state.daysCompleted, state.day);
                return new SettlementResult(
                    state.day, applied: true, alreadyApplied: false, bankrupt: false,
                    gross, spend, cost, net, cashBefore, state.cash, 0,
                    $"Day {state.day} 정산 완료 — 운영비 {cost:N0}원 납부.");
            }

            // 파산: 운영비를 낼 수 없다 — cash 0 고정 (음수 불허), 사유 기록 (설계 8단계)
            int unpaid = cost - cashBefore;
            state.cash = 0;
            state.settlementCashAfter = 0;
            state.isBankrupt = true;
            state.bankruptcyDay = state.day;
            state.bankruptcyReason = $"Day {state.day} 운영비 {cost:N0}원 미납 (부족액 {unpaid:N0}원)";
            return new SettlementResult(
                state.day, applied: true, alreadyApplied: false, bankrupt: true,
                gross, spend, cost, net, cashBefore, 0, unpaid,
                $"파산 — {state.bankruptcyReason}");
        }

        static void Require(GameState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state), "GameState 없이 정산할 수 없다");
            }
        }
    }
}
