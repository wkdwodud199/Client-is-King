using System;
using ClientIsKing.DayCycle;
using ClientIsKing.Presentation;
using ClientIsKing.Settlement;
using ClientIsKing.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-108: 정산 표현 — 최종 표시값이 SettlementResult 와 정확히 일치하고
    /// SettlementPresented payload 가 SettlementOps 결과와 같음을 검증.
    /// (카운트업 코루틴은 Play 전용 — EditMode 는 즉시 최종값, 설계 26단계 계약)
    /// </summary>
    public class SettlementPresentationTests
    {
        GameObject root;
        SettlementPanelController panel;

        TMP_Text MakeText(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(root.transform, false);
            return go.AddComponent<TextMeshProUGUI>();
        }

        [SetUp]
        public void CreatePanel()
        {
            root = new GameObject("settlement-panel-under-test");
            panel = root.AddComponent<SettlementPanelController>();
            panel.EditorInit(
                MakeText("Gross"), MakeText("Spend"), MakeText("Operating"), MakeText("Net"),
                MakeText("Cash"), MakeText("Stats"), MakeText("Message"));
        }

        [TearDown]
        public void DestroyPanel()
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        TMP_Text Find(string name)
        {
            return root.transform.Find(name).GetComponent<TMP_Text>();
        }

        [Test]
        public void Final_Texts_Match_SettlementResult_Exactly()
        {
            var state = new GameState
            {
                cash = 40000,
                serviceRevenueToday = 20000,
                marketSpendDay = 1,
                marketSpendToday = 5000,
            };
            var result = SettlementOps.ApplyDailySettlement(state);

            panel.RenderAndPublish(result);

            Assert.AreEqual($"매출  +{result.GrossRevenue:N0}원", Find("Gross").text);
            Assert.AreEqual($"재료 지출  -{result.IngredientSpend:N0}원", Find("Spend").text);
            Assert.AreEqual($"운영비  -{result.OperatingCost:N0}원", Find("Operating").text);
            Assert.AreEqual($"순손익  +{result.NetProfit:N0}원", Find("Net").text);
            Assert.AreEqual($"잔액  {result.CashBefore:N0}원 → {result.CashAfter:N0}원", Find("Cash").text);
            Assert.AreEqual(result.Message, Find("Message").text);
        }

        [Test]
        public void Published_Payload_Equals_SettlementOps_Result()
        {
            SettlementPresentationEventArgs received = default;
            int calls = 0;
            Action<SettlementPresentationEventArgs> handler = a => { calls++; received = a; };

            GameEvents.SettlementPresented += handler;
            try
            {
                var state = new GameState { cash = 15000, serviceRevenueToday = 8000 };
                var result = SettlementOps.ApplyDailySettlement(state);

                var args = panel.RenderAndPublish(result);

                Assert.AreEqual(1, calls, "렌더 직후 정확히 1회 발행");
                Assert.AreEqual(result.Day, received.Day);
                Assert.AreEqual(result.GrossRevenue, received.GrossRevenue);
                Assert.AreEqual(result.IngredientSpend, received.IngredientSpend);
                Assert.AreEqual(result.OperatingCost, received.OperatingCost);
                Assert.AreEqual(result.NetProfit, received.NetProfit);
                Assert.AreEqual(result.CashBefore, received.CashBefore);
                Assert.AreEqual(result.CashAfter, received.CashAfter);
                Assert.AreEqual(result.Bankrupt, received.Bankrupt);
                Assert.AreEqual(args.NetProfit, received.NetProfit, "반환값과 구독 payload 동일");
            }
            finally
            {
                GameEvents.SettlementPresented -= handler;
            }
        }

        [Test]
        public void Reapplied_Result_Renders_Same_Final_Values_Without_Cash_Change()
        {
            var state = new GameState { cash = 40000, serviceRevenueToday = 10000 };
            var first = SettlementOps.ApplyDailySettlement(state);
            int cashAfterFirst = state.cash;

            var second = SettlementOps.ApplyDailySettlement(state); // 멱등 재호출
            panel.RenderAndPublish(second);

            Assert.AreEqual(cashAfterFirst, state.cash, "재표시는 cash 를 바꾸지 않는다");
            Assert.AreEqual($"잔액  {first.CashBefore:N0}원 → {first.CashAfter:N0}원", Find("Cash").text,
                "저장된 결과로 동일한 최종 표시");
        }

        [Test]
        public void Bankrupt_Result_Renders_And_Publishes_Bankrupt_Flag()
        {
            SettlementPresentationEventArgs received = default;
            Action<SettlementPresentationEventArgs> handler = a => received = a;

            GameEvents.SettlementPresented += handler;
            try
            {
                var state = new GameState { cash = 3000 };
                var result = SettlementOps.ApplyDailySettlement(state);

                panel.RenderAndPublish(result);

                Assert.IsTrue(received.Bankrupt);
                StringAssert.Contains("파산", Find("Message").text);
            }
            finally
            {
                GameEvents.SettlementPresented -= handler;
            }
        }
    }
}
