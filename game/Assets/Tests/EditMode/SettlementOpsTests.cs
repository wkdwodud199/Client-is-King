using System;
using ClientIsKing.DayCycle;
using ClientIsKing.Settlement;
using NUnit.Framework;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>task-107: 일일 정산 규칙 — 운영비 1회 차감, 매출 재반영 금지, 멱등성, 파산 판정.</summary>
    public class SettlementOpsTests
    {
        [Test]
        public void Apply_Deducts_Only_OperatingCost_Not_Revenue()
        {
            // 매출 20,000 은 서빙 시점에 이미 cash 반영됐다는 전제 — cash 는 50,000 그대로 둔다.
            var state = new GameState { cash = 50000, serviceRevenueToday = 20000 };

            var result = SettlementOps.ApplyDailySettlement(state);

            Assert.IsTrue(result.Applied);
            Assert.IsFalse(result.AlreadyApplied);
            Assert.AreEqual(50000 - SettlementOps.DailyOperatingCost, state.cash,
                "정산의 실제 cash delta 는 운영비뿐 (매출 이중 반영 금지)");
            Assert.AreEqual(20000, result.GrossRevenue, "매출은 표시용 요약으로만 기록");
            Assert.AreEqual(state.cash, result.CashAfter);
        }

        [Test]
        public void NetProfit_Is_Gross_Minus_Spend_Minus_OperatingCost()
        {
            var state = new GameState
            {
                cash = 40000,
                serviceRevenueToday = 20000,
                marketSpendDay = 1,
                marketSpendToday = 5000,
            };

            var result = SettlementOps.ApplyDailySettlement(state);

            Assert.AreEqual(20000 - 5000 - SettlementOps.DailyOperatingCost, result.NetProfit);
            Assert.AreEqual(5000, result.IngredientSpend);
        }

        [Test]
        public void Stale_MarketSpend_From_Other_Day_Is_Ignored()
        {
            // marketSpendDay 가 오늘(day 1)이 아니면 지출 요약은 0 이어야 한다.
            var state = new GameState { cash = 40000, marketSpendDay = 0, marketSpendToday = 7777 };

            var result = SettlementOps.ApplyDailySettlement(state);

            Assert.AreEqual(0, result.IngredientSpend, "이전 날 지출은 오늘 정산에 섞지 않는다");
        }

        [Test]
        public void Apply_Is_Idempotent_Per_Day()
        {
            var state = new GameState { cash = 40000, serviceRevenueToday = 15000 };

            var first = SettlementOps.ApplyDailySettlement(state);
            int cashAfterFirst = state.cash;
            var second = SettlementOps.ApplyDailySettlement(state);

            Assert.IsTrue(first.Applied);
            Assert.IsFalse(second.Applied, "같은 날 재호출은 적용 없음");
            Assert.IsTrue(second.AlreadyApplied);
            Assert.AreEqual(cashAfterFirst, state.cash, "운영비는 하루 1회만 차감");
            Assert.AreEqual(first.GrossRevenue, second.GrossRevenue, "기존 결과 유지");
            Assert.AreEqual(first.NetProfit, second.NetProfit);
        }

        [Test]
        public void Bankrupt_When_Cash_Below_OperatingCost()
        {
            var state = new GameState { cash = 5000 };

            var result = SettlementOps.ApplyDailySettlement(state);

            Assert.IsTrue(result.Bankrupt);
            Assert.IsTrue(state.isBankrupt);
            Assert.AreEqual(0, state.cash, "파산 시 cash 0 고정 (음수 불허)");
            Assert.AreEqual(SettlementOps.DailyOperatingCost - 5000, result.UnpaidCost);
            Assert.AreEqual(state.day, state.bankruptcyDay);
            Assert.IsFalse(string.IsNullOrEmpty(state.bankruptcyReason), "파산 사유 기록");
        }

        [Test]
        public void Exactly_OperatingCost_Survives_With_Zero_Cash()
        {
            var state = new GameState { cash = SettlementOps.DailyOperatingCost };

            var result = SettlementOps.ApplyDailySettlement(state);

            Assert.IsFalse(result.Bankrupt, "정확히 운영비만큼 있으면 납부 가능 — 생존");
            Assert.IsFalse(state.isBankrupt);
            Assert.AreEqual(0, state.cash);
            Assert.AreEqual(state.day, state.daysCompleted, "완료 일수 갱신");
        }

        [Test]
        public void DaysCompleted_Keeps_Maximum()
        {
            var state = new GameState { cash = 50000, day = 3, daysCompleted = 5 };

            SettlementOps.ApplyDailySettlement(state);

            Assert.AreEqual(5, state.daysCompleted, "daysCompleted 는 뒤로 가지 않는다");
        }

        [Test]
        public void Null_State_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => SettlementOps.ApplyDailySettlement(null));
            Assert.Throws<ArgumentNullException>(() => SettlementOps.IsSettlementApplied(null));
        }
    }
}
