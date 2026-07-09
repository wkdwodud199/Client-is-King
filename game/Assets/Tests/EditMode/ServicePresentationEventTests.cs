using System;
using System.Collections.Generic;
using System.Linq;
using ClientIsKing.Data;
using ClientIsKing.DayCycle;
using ClientIsKing.Presentation;
using ClientIsKing.Service;
using ClientIsKing.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-108: Service 표현 이벤트 — 발행 payload(처리 전 주문 보존)와 이벤트 왕복 검증.
    /// 패널의 payload 빌더는 internal(IVT) 로 직접 검증한다 (OnServe/OnSkip 통합 경로는 Play smoke).
    /// </summary>
    public class ServicePresentationEventTests
    {
        static List<T> LoadAll<T>(string folder) where T : UnityEngine.Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder })
                .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null)
                .ToList();
        }

        static ServicePanelController CreatePanel(out GameObject go)
        {
            go = new GameObject("service-panel-under-test");
            var panel = go.AddComponent<ServicePanelController>();
            // payload 빌더는 defs 목록만 사용 — UI 참조는 null 로 충분 (EditMode 는 lifecycle 미실행)
            panel.EditorInit(null, null, null, null, null, null, null, null, null, null, null, null,
                LoadAll<RecipeDef>("Assets/Data/Definitions/Recipes"),
                LoadAll<CustomerArchetypeDef>("Assets/Data/Definitions/Customers"),
                LoadAll<IngredientDef>("Assets/Data/Definitions/Ingredients"));
            return panel;
        }

        [Test]
        public void OrderPresented_Payload_Preserves_Order_And_Recipe_DisplayName()
        {
            var panel = CreatePanel(out var go);
            try
            {
                var recipe = LoadAll<RecipeDef>("Assets/Data/Definitions/Recipes").First(r => r.Id == "pork_gukbap");
                var state = new GameState();
                ServiceOps.StartServiceDay(state, new List<ServiceOrderState>
                {
                    new ServiceOrderState { recipeId = recipe.Id, customerId = "student", partySize = 2 },
                }, 1);

                var args = panel.BuildOrderPresentedArgs(state, ServiceOps.GetCurrentOrder(state));

                Assert.IsTrue(args.HasOrder);
                Assert.AreEqual(1, args.Day);
                Assert.AreEqual(1, args.OrderNumber, "첫 주문 = 1번");
                Assert.AreEqual(1, args.TotalOrders);
                Assert.AreEqual("student", args.CustomerId);
                Assert.AreEqual("pork_gukbap", args.RecipeId);
                Assert.AreEqual(2, args.PartySize);
                Assert.AreEqual(recipe.DisplayName, args.Message, "Message 에 레시피 표시명 (무대 라벨 규약)");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OrderPresented_Is_Empty_When_No_Order()
        {
            var panel = CreatePanel(out var go);
            try
            {
                var state = new GameState { day = 3 };
                var args = panel.BuildOrderPresentedArgs(state, null);
                Assert.IsFalse(args.HasOrder, "주문 없음 → 슬롯 비움 신호");
                Assert.AreEqual(3, args.Day);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Outcome_Served_Preserves_Captured_Order_With_Positive_Revenue()
        {
            var panel = CreatePanel(out var go);
            try
            {
                var state = new GameState { day = 2 };
                var captured = new ServiceOrderState { recipeId = "gimbap", customerId = "office_worker", partySize = 3 };
                var result = new ServiceResult(true, "서빙 완료", revenueGained: 13500, cashAfter: 43500);

                var args = panel.BuildOutcomeArgs(state, captured, result, missed: false);

                Assert.IsTrue(args.Served);
                Assert.IsFalse(args.Missed);
                Assert.Greater(args.RevenueGained, 0, "서빙 성공은 양수 매출");
                Assert.AreEqual(13500, args.RevenueGained);
                Assert.AreEqual("gimbap", args.RecipeId, "처리 전 주문 정보 보존");
                Assert.AreEqual("office_worker", args.CustomerId);
                Assert.AreEqual(3, args.PartySize);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Outcome_Missed_Has_Zero_Revenue()
        {
            var panel = CreatePanel(out var go);
            try
            {
                var state = new GameState();
                var captured = new ServiceOrderState { recipeId = "tteokbokki", customerId = "student", partySize = 2 };
                var result = new ServiceResult(true, "주문 포기", revenueGained: 0, cashAfter: 30000);

                var args = panel.BuildOutcomeArgs(state, captured, result, missed: true);

                Assert.IsFalse(args.Served);
                Assert.IsTrue(args.Missed);
                Assert.AreEqual(0, args.RevenueGained);
                Assert.AreEqual("tteokbokki", args.RecipeId);
                Assert.AreEqual(2, args.PartySize);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void GameEvents_Roundtrip_Delivers_Identical_Payload()
        {
            ServicePresentationEventArgs receivedOrder = default;
            ServicePresentationEventArgs receivedOutcome = default;
            int orderCalls = 0, outcomeCalls = 0;
            Action<ServicePresentationEventArgs> onOrder = a => { orderCalls++; receivedOrder = a; };
            Action<ServicePresentationEventArgs> onOutcome = a => { outcomeCalls++; receivedOutcome = a; };

            GameEvents.ServiceOrderPresented += onOrder;
            GameEvents.ServiceOutcomeResolved += onOutcome;
            try
            {
                var order = new ServicePresentationEventArgs(true, 1, 2, 5, "senior_regular", "janchi_guksu", 1, false, false, 0, "잔치국수");
                var outcome = new ServicePresentationEventArgs(true, 1, 0, 5, "senior_regular", "janchi_guksu", 1, true, false, 6000, "완료");

                GameEvents.RaiseServiceOrderPresented(order);   // internal — IVT
                GameEvents.RaiseServiceOutcomeResolved(outcome);

                Assert.AreEqual(1, orderCalls);
                Assert.AreEqual(1, outcomeCalls);
                Assert.AreEqual("janchi_guksu", receivedOrder.RecipeId);
                Assert.AreEqual(2, receivedOrder.OrderNumber);
                Assert.AreEqual(6000, receivedOutcome.RevenueGained);
                Assert.IsTrue(receivedOutcome.Served);
            }
            finally
            {
                GameEvents.ServiceOrderPresented -= onOrder;
                GameEvents.ServiceOutcomeResolved -= onOutcome;
            }
        }
    }
}
