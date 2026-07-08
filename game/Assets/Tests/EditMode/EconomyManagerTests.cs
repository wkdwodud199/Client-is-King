using System.Linq;
using ClientIsKing.Data;
using ClientIsKing.DayCycle;
using ClientIsKing.Economy;
using ClientIsKing.Inventory;
using NUnit.Framework;
using UnityEditor;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-105: 경제 핵심 규칙(EconomyOps — EconomyManager 가 위임하는 순수 코어) 검증.
    /// 재료 정의는 task-103 시드 asset 을 그대로 사용한다 (수치 하드코딩 대신 def 에서 역산).
    /// </summary>
    public class EconomyManagerTests
    {
        static IngredientDef LoadDef(string id)
        {
            var def = AssetDatabase.FindAssets("t:IngredientDef", new[] { "Assets/Data/Definitions/Ingredients" })
                .Select(g => AssetDatabase.LoadAssetAtPath<IngredientDef>(AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault(d => d != null && d.Id == id);
            Assert.IsNotNull(def, $"시드 IngredientDef '{id}' 를 찾을 수 없다 (task-103 전제)");
            return def;
        }

        [Test]
        public void CalculatePurchaseCost_Is_UnitCost_Times_Quantity()
        {
            var def = LoadDef("rice_c");
            Assert.AreEqual(def.UnitCost * 4, EconomyOps.CalculatePurchaseCost(def, 4));
            Assert.AreEqual(0, EconomyOps.CalculatePurchaseCost(def, 0), "0 수량은 비용 0");
            Assert.AreEqual(0, EconomyOps.CalculatePurchaseCost(null, 3), "null 재료는 비용 0");
        }

        [Test]
        public void CanAfford_Boundary()
        {
            var state = new GameState { cash = 1000 };
            Assert.IsTrue(EconomyOps.CanAfford(state, 1000), "잔액과 같은 금액은 지출 가능");
            Assert.IsFalse(EconomyOps.CanAfford(state, 1001));
        }

        [Test]
        public void TryPurchase_Success_Deducts_Cash_And_Adds_Stock()
        {
            var def = LoadDef("rice_c");
            var state = new GameState();
            int expectedCost = def.UnitCost * 3;

            var result = EconomyOps.TryPurchaseIngredient(state, def, 3);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(expectedCost, result.TotalCost);
            Assert.AreEqual(GameState.StartingCash - expectedCost, state.cash, "자금 정확 차감");
            Assert.AreEqual(state.cash, result.CashAfter);
            Assert.AreEqual(3, InventoryOps.GetQuantity(state, def.Kind, def.Grade), "보유 수량 정확 증가");
            Assert.AreEqual(3, result.QuantityAfter);
        }

        [Test]
        public void TryPurchase_InsufficientCash_Fails_Without_Change()
        {
            var def = LoadDef("beef_b"); // 가장 비싼 축 — 소액 잔고로 실패 유도
            var state = new GameState { cash = def.UnitCost - 1 };

            var result = EconomyOps.TryPurchaseIngredient(state, def, 1);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(def.UnitCost - 1, state.cash, "실패 시 자금 불변");
            Assert.AreEqual(0, state.ingredientStocks.Count, "실패 시 인벤토리 불변");
        }

        [Test]
        public void TryPurchase_NullDef_Fails_Without_Change()
        {
            var state = new GameState();

            var result = EconomyOps.TryPurchaseIngredient(state, null, 2);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(GameState.StartingCash, state.cash);
            Assert.AreEqual(0, state.ingredientStocks.Count);
        }

        [Test]
        public void TryPurchase_NonPositiveQuantity_Fails_Without_Change()
        {
            var def = LoadDef("rice_c");
            var state = new GameState();

            Assert.IsFalse(EconomyOps.TryPurchaseIngredient(state, def, 0).Success);
            Assert.IsFalse(EconomyOps.TryPurchaseIngredient(state, def, -3).Success);
            Assert.AreEqual(GameState.StartingCash, state.cash);
            Assert.AreEqual(0, state.ingredientStocks.Count);
        }

        // ── task-107: 당일 구매 지출 추적 ──────────────────────────────────

        [Test]
        public void Purchase_Accumulates_MarketSpend_And_Resets_On_New_Day()
        {
            var def = LoadDef("rice_c");
            var state = new GameState();

            EconomyOps.TryPurchaseIngredient(state, def, 2);
            Assert.AreEqual(def.UnitCost * 2, state.marketSpendToday);
            Assert.AreEqual(1, state.marketSpendDay);

            EconomyOps.TryPurchaseIngredient(state, def, 1);
            Assert.AreEqual(def.UnitCost * 3, state.marketSpendToday, "같은 날은 누적");

            state.day = 2; // 다음 날 첫 구매는 리셋 후 누적
            EconomyOps.TryPurchaseIngredient(state, def, 1);
            Assert.AreEqual(def.UnitCost, state.marketSpendToday);
            Assert.AreEqual(2, state.marketSpendDay);
        }

        [Test]
        public void Failed_Purchase_Does_Not_Touch_MarketSpend()
        {
            var def = LoadDef("beef_b");
            var state = new GameState { cash = 10 };

            EconomyOps.TryPurchaseIngredient(state, def, 1);   // 자금 부족
            EconomyOps.TryPurchaseIngredient(state, null, 1);  // null 재료
            EconomyOps.TryPurchaseIngredient(state, def, 0);   // 0 수량

            Assert.AreEqual(0, state.marketSpendToday, "실패 경로는 지출 통계 불변");
        }
    }
}
