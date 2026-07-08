using System;
using System.Collections.Generic;
using System.Linq;
using ClientIsKing.Data;
using ClientIsKing.DayCycle;
using ClientIsKing.Economy;
using ClientIsKing.Inventory;
using ClientIsKing.Managers;
using ClientIsKing.Service;
using ClientIsKing.Settlement;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-107: 첫 플레이어블 루프 검증 — 구매→서빙→정산→마감→다음 날 흐름과 파산 진행 차단.
    /// GameManager 는 EditMode 에서 생성 가능 (DontDestroyOnLoad 는 Play 모드 가드 — 구현 노트 참조).
    /// </summary>
    public class FirstPlayableLoopTests
    {
        static T LoadDef<T>(string folder, string id) where T : ScriptableObject
        {
            var def = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder })
                .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault(d =>
                {
                    var ingredient = d as IngredientDef;
                    if (ingredient != null) return ingredient.Id == id;
                    var recipe = d as RecipeDef;
                    return recipe != null && recipe.Id == id;
                });
            Assert.IsNotNull(def, $"시드 {typeof(T).Name} '{id}' 누락");
            return def;
        }

        static GameManager CreateGameManager(out GameObject go)
        {
            go = new GameObject("gm-under-test");
            var gm = go.AddComponent<GameManager>();
            gm.StartNewGame(); // EditMode 에서 Awake 호출 여부와 무관하게 결정론적 초기화
            return gm;
        }

        [Test]
        public void NonBankrupt_Full_Day_Loop_Works()
        {
            var gm = CreateGameManager(out var go);
            try
            {
                var state = gm.State;
                var rice = LoadDef<IngredientDef>("Assets/Data/Definitions/Ingredients", "rice_c");
                var seaweed = LoadDef<IngredientDef>("Assets/Data/Definitions/Ingredients", "seaweed_c");
                var vegetable = LoadDef<IngredientDef>("Assets/Data/Definitions/Ingredients", "vegetable_c");
                var gimbap = LoadDef<RecipeDef>("Assets/Data/Definitions/Recipes", "gimbap");

                // Market: 재료 구매 (지출 추적 확인)
                Assert.IsTrue(EconomyOps.TryPurchaseIngredient(state, rice, 1).Success);
                Assert.IsTrue(EconomyOps.TryPurchaseIngredient(state, seaweed, 1).Success);
                Assert.IsTrue(EconomyOps.TryPurchaseIngredient(state, vegetable, 1).Success);
                int spend = rice.UnitCost + seaweed.UnitCost + vegetable.UnitCost;
                Assert.AreEqual(spend, state.marketSpendToday);

                // Market → Service
                Assert.AreEqual(DayPhase.Service, gm.AdvancePhase());

                // Service: 김밥 1건 서빙 (매출 발생)
                ServiceOps.StartServiceDay(state, new List<ServiceOrderState>
                {
                    new ServiceOrderState { recipeId = gimbap.Id, customerId = "student", partySize = 1 },
                }, state.day);
                var serve = ServiceOps.TryServeCurrentOrder(state, gimbap, IngredientGrade.C);
                Assert.IsTrue(serve.Success, serve.Message);

                int cashBeforeSettlement = state.cash;

                // Service → Settlement
                Assert.AreEqual(DayPhase.Settlement, gm.AdvancePhase());
                // Settlement → Night (게이트가 정산을 선적용한다)
                Assert.AreEqual(DayPhase.Night, gm.AdvancePhase());

                Assert.IsTrue(SettlementOps.IsSettlementApplied(state), "Settlement 이탈 시 정산 적용");
                Assert.AreEqual(cashBeforeSettlement - SettlementOps.DailyOperatingCost, state.cash);
                Assert.AreEqual(1, state.daysCompleted);
                Assert.IsFalse(state.isBankrupt);

                // Night → Market (day +1)
                Assert.AreEqual(DayPhase.Market, gm.AdvancePhase());
                Assert.AreEqual(2, state.day, "하루 루프 완주 — 다음 날 진입");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Bankruptcy_Blocks_Advance_And_Emits_No_Events()
        {
            var gm = CreateGameManager(out var go);
            int events = 0;
            Action<DayPhaseChangedEventArgs> handler = _ => events++;
            GameEvents.DayPhaseChanged += handler;
            try
            {
                var state = gm.State;
                state.cash = 100; // 운영비(12,000) 미달
                state.currentPhase = DayPhase.Settlement;

                var afterFirst = gm.AdvancePhase(); // 정산 선적용 → 파산 → 진행 차단

                Assert.AreEqual(DayPhase.Settlement, afterFirst, "파산 정산 후 Settlement 에 머문다");
                Assert.IsTrue(state.isBankrupt);
                Assert.AreEqual(0, state.cash);

                var afterSecond = gm.AdvancePhase(); // 파산 상태 — 즉시 차단

                Assert.AreEqual(DayPhase.Settlement, afterSecond);
                Assert.AreEqual(0, events, "차단된 진행은 DayPhaseChanged 를 발행하지 않는다");
            }
            finally
            {
                GameEvents.DayPhaseChanged -= handler;
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
