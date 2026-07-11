using System.Linq;
using ClientIsKing.DayCycle;
using ClientIsKing.EditorTools;
using ClientIsKing.Events;
using NUnit.Framework;
using UnityEngine;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-112 U7: JsonUtility 왕복 결정론 — activeEvents(영구+시한 혼합) field-by-field 보존과
    /// 왕복 전후 TryBuildDayPlan plan 동등성을 검증한다(task-111 리뷰 001 Action 2 전례 계승,
    /// task-113 저장/불러오기의 선행 보증).
    /// </summary>
    public class EventJsonRoundTripTests
    {
        [Test]
        public void ActiveEvents_Mixed_Permanent_And_Timed_Survives_RoundTrip_Field_By_Field()
        {
            var state = new GameState
            {
                day = 14,
                activeEvents =
                {
                    new ActiveEventState { eventId = "rent_increase", remainingDays = 0 },
                    new ActiveEventState { eventId = "ingredient_price_surge", remainingDays = 1 },
                },
                marketEventSurchargeToday = 3009,
                serviceEventOrdersServedToday = 1,
                serviceEventOrdersMissedToday = 0,
                serviceEventRevenueToday = 18900,
            };

            string json = JsonUtility.ToJson(state);
            var restored = JsonUtility.FromJson<GameState>(json);

            Assert.AreEqual(state.activeEvents.Count, restored.activeEvents.Count);
            for (int i = 0; i < state.activeEvents.Count; i++)
            {
                Assert.AreEqual(state.activeEvents[i].eventId, restored.activeEvents[i].eventId, $"항목 {i} eventId");
                Assert.AreEqual(state.activeEvents[i].remainingDays, restored.activeEvents[i].remainingDays, $"항목 {i} remainingDays");
            }
            Assert.AreEqual(state.marketEventSurchargeToday, restored.marketEventSurchargeToday);
            Assert.AreEqual(state.serviceEventOrdersServedToday, restored.serviceEventOrdersServedToday);
            Assert.AreEqual(state.serviceEventOrdersMissedToday, restored.serviceEventOrdersMissedToday);
            Assert.AreEqual(state.serviceEventRevenueToday, restored.serviceEventRevenueToday);
        }

        [Test]
        public void Empty_ActiveEvents_Survives_RoundTrip_As_Empty_Not_Null()
        {
            var state = new GameState { day = 1 };
            string json = JsonUtility.ToJson(state);
            var restored = JsonUtility.FromJson<GameState>(json);

            Assert.IsNotNull(restored.activeEvents, "빈 목록은 null 이 아니라 빈 List 로 왕복해야 한다");
            Assert.AreEqual(0, restored.activeEvents.Count);
        }

        [Test]
        public void TryBuildDayPlan_Result_Is_Identical_Before_And_After_RoundTrip_With_Active_Event()
        {
            // 배치 EditMode 에서는 OpenScene 이 Awake 를 동기 호출한다는 보장이 없다(group1/U6 공통 함정).
            var gameManagerGo = TestSceneSupport.OpenShopSceneWithLiveSingletons();
            var gm = gameManagerGo.GetComponent<ClientIsKing.Managers.GameManager>();
            gm.StartNewGame();

            var genreIds = gm.GenreCatalog.Select(g => g.Id).ToList();
            var selection = ClientIsKing.Genre.GenreSelectionOps.TrySelect(gm.State, "gukbap", genreIds);
            Assert.IsTrue(selection.Success, selection.Message);

            // Day 5(단체 손님 활성) 시나리오로 진행 — activeEvents 에 단체 손님을 직접 구성한다.
            gm.State.day = 5;
            gm.State.activeEvents.Add(new ActiveEventState { eventId = "group_customers", remainingDays = 1 });

            var service = ClientIsKing.Service.ServiceManager.Instance;
            bool okBefore = service.TryBuildDayPlan(
                gm.GenreCatalog.First(g => g.Id == "gukbap"), out var planBefore, out var reasonBefore);
            Assert.IsTrue(okBefore, reasonBefore);
            Assert.Greater(planBefore.EventBonusOrderCount, 0, "왕복 전 plan 에 단체 보너스가 있어야 시나리오가 성립한다");

            string json = JsonUtility.ToJson(gm.State);
            JsonUtility.FromJsonOverwrite(json, gm.State);

            bool okAfter = service.TryBuildDayPlan(
                gm.GenreCatalog.First(g => g.Id == "gukbap"), out var planAfter, out var reasonAfter);
            Assert.IsTrue(okAfter, reasonAfter);

            Assert.AreEqual(planBefore.GenreId, planAfter.GenreId);
            Assert.AreEqual(planBefore.Day, planAfter.Day);
            Assert.AreEqual(planBefore.BaseOrderCount, planAfter.BaseOrderCount);
            Assert.AreEqual(planBefore.BonusOrderCount, planAfter.BonusOrderCount);
            Assert.AreEqual(planBefore.EventBonusOrderCount, planAfter.EventBonusOrderCount);
            Assert.AreEqual(planBefore.EventPartySize, planAfter.EventPartySize);
            Assert.AreEqual(planBefore.EventSourceId, planAfter.EventSourceId);
            Assert.AreEqual(planBefore.OrderCount, planAfter.OrderCount);

            var ordersBefore = ClientIsKing.Service.ServiceOps.BuildOrders(planBefore, service.CustomerDefs);
            var ordersAfter = ClientIsKing.Service.ServiceOps.BuildOrders(planAfter, service.CustomerDefs);
            Assert.AreEqual(ordersBefore.Count, ordersAfter.Count);
            for (int i = 0; i < ordersBefore.Count; i++)
            {
                Assert.AreEqual(ordersBefore[i].recipeId, ordersAfter[i].recipeId, $"주문 {i} recipeId");
                Assert.AreEqual(ordersBefore[i].customerId, ordersAfter[i].customerId, $"주문 {i} customerId");
                Assert.AreEqual(ordersBefore[i].partySize, ordersAfter[i].partySize, $"주문 {i} partySize");
                Assert.AreEqual(ordersBefore[i].eventInflow, ordersAfter[i].eventInflow, $"주문 {i} eventInflow 태그");
            }
        }
    }
}
