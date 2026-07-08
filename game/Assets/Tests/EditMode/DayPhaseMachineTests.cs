using System;
using ClientIsKing.DayCycle;
using NUnit.Framework;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>task-104: 하루 상태 머신 — 전환 순서·day 증가·방어 로직·이벤트 발행 검증 (순수 C#).</summary>
    public class DayPhaseMachineTests
    {
        [Test]
        public void GameState_Defaults_Are_Day1_Market_With_StartingCash_And_EmptyInventory()
        {
            var state = new GameState();
            Assert.AreEqual(1, state.day);
            Assert.AreEqual(DayPhase.Market, state.currentPhase);
            Assert.AreEqual(GameState.StartingCash, state.cash, "시작 자금 규약 (task-105)");
            Assert.Greater(state.cash, 0, "시작 자금은 양수");
            Assert.IsNotNull(state.ingredientStocks, "인벤토리 list 는 항상 non-null");
            Assert.AreEqual(0, state.ingredientStocks.Count, "새 게임 인벤토리는 비어 있다");
            // task-106: 서비스 상태 초기값
            Assert.AreEqual(0, state.serviceDay, "영업 시작 전 serviceDay 0");
            Assert.IsNotNull(state.serviceOrders, "주문 list 는 항상 non-null");
            Assert.AreEqual(0, state.serviceOrders.Count, "새 게임 주문 목록은 비어 있다");
            Assert.AreEqual(0, state.serviceRevenueToday);
            Assert.AreEqual(0, state.serviceOrdersServedToday);
            Assert.AreEqual(0, state.serviceOrdersMissedToday);
            Assert.AreEqual(0, state.serviceCustomersServedToday);
            Assert.AreEqual(0, state.serviceCustomersMissedToday);
            // task-107: 정산/파산 초기값
            Assert.AreEqual(0, state.marketSpendToday);
            Assert.AreEqual(0, state.settlementDay, "정산 전 settlementDay 0");
            Assert.AreEqual(0, state.daysCompleted);
            Assert.IsFalse(state.isBankrupt);
            Assert.AreEqual("", state.bankruptcyReason);
        }

        [Test]
        public void Advance_Cycles_Market_Service_Settlement_Night_Market()
        {
            var machine = new DayPhaseMachine(new GameState());
            Assert.AreEqual(DayPhase.Service, machine.Advance());
            Assert.AreEqual(DayPhase.Settlement, machine.Advance());
            Assert.AreEqual(DayPhase.Night, machine.Advance());
            Assert.AreEqual(DayPhase.Market, machine.Advance());
        }

        [Test]
        public void Day_Increments_Only_On_Night_To_Market()
        {
            var state = new GameState();
            var machine = new DayPhaseMachine(state);

            machine.Advance(); // Market → Service
            Assert.AreEqual(1, state.day);
            machine.Advance(); // Service → Settlement
            Assert.AreEqual(1, state.day);
            machine.Advance(); // Settlement → Night
            Assert.AreEqual(1, state.day);
            machine.Advance(); // Night → Market (+1)
            Assert.AreEqual(2, state.day);
            machine.Advance(); // Market → Service (변화 없음)
            Assert.AreEqual(2, state.day);
        }

        [Test]
        public void Ctor_Null_State_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _ = new DayPhaseMachine(null));
        }

        [Test]
        public void Ctor_Day_Below_1_Is_Corrected_To_1()
        {
            var state = new GameState { day = -5 };
            var machine = new DayPhaseMachine(state);
            Assert.AreEqual(1, machine.State.day);
        }

        [Test]
        public void Event_Fired_Exactly_Once_With_Correct_Payload()
        {
            int calls = 0;
            DayPhaseChangedEventArgs captured = default;
            Action<DayPhaseChangedEventArgs> handler = args => { calls++; captured = args; };

            GameEvents.DayPhaseChanged += handler;
            try
            {
                var machine = new DayPhaseMachine(new GameState());
                machine.Advance(); // Market → Service

                Assert.AreEqual(1, calls, "전환 1회당 이벤트는 정확히 1회");
                Assert.AreEqual(1, captured.Day);
                Assert.AreEqual(DayPhase.Market, captured.PreviousPhase);
                Assert.AreEqual(DayPhase.Service, captured.CurrentPhase);
            }
            finally
            {
                GameEvents.DayPhaseChanged -= handler;
            }
        }

        [Test]
        public void Event_On_NightToMarket_Carries_Incremented_Day()
        {
            DayPhaseChangedEventArgs captured = default;
            Action<DayPhaseChangedEventArgs> handler = args => captured = args;

            GameEvents.DayPhaseChanged += handler;
            try
            {
                var state = new GameState { currentPhase = DayPhase.Night };
                var machine = new DayPhaseMachine(state);
                machine.Advance(); // Night → Market (day 1 → 2)

                Assert.AreEqual(2, captured.Day, "payload 의 day 는 +1 반영된 값");
                Assert.AreEqual(DayPhase.Night, captured.PreviousPhase);
                Assert.AreEqual(DayPhase.Market, captured.CurrentPhase);
            }
            finally
            {
                GameEvents.DayPhaseChanged -= handler;
            }
        }
    }
}
