using System.Linq;
using ClientIsKing.Data;
using ClientIsKing.DayCycle;
using ClientIsKing.EditorTools;
using ClientIsKing.Economy;
using ClientIsKing.Genre;
using ClientIsKing.Inventory;
using ClientIsKing.Managers;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-105: 경제 핵심 규칙(EconomyOps — EconomyManager 가 위임하는 순수 코어) 검증.
    /// 재료 정의는 task-103 시드 asset 을 그대로 사용한다 (수치 하드코딩 대신 def 에서 역산).
    /// task-110 (U6): 장르 적용 구매원가 overload + EconomyManager 의 genre 게이트를 추가한다.
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

        static GenreDef LoadGenre(string id)
        {
            var def = AssetDatabase.FindAssets("t:GenreDef", new[] { "Assets/Data/Definitions/Genres" })
                .Select(g => AssetDatabase.LoadAssetAtPath<GenreDef>(AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault(d => d != null && d.Id == id);
            Assert.IsNotNull(def, $"시드 GenreDef '{id}' 를 찾을 수 없다");
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

        // ── task-110: 장르 원가 배수 overload ────────────────────────────────

        [Test]
        public void CalculatePurchaseCost_With_Genre_Multiplier_Uses_RoundHalfUp()
        {
            var def = LoadDef("pork_c"); // unitCost 900
            int expected = (int)GenreSelectionOps.RoundHalfUp(900 * 1.15) * 4; // 국밥 costMultiplier 1.15, qty 4
            Assert.AreEqual(expected, EconomyOps.CalculatePurchaseCost(def, 4, 1.15f));
        }

        [Test]
        public void CalculatePurchaseCost_Neutral_Overload_Matches_Multiplier_1()
        {
            var def = LoadDef("rice_c");
            Assert.AreEqual(EconomyOps.CalculatePurchaseCost(def, 5, 1f), EconomyOps.CalculatePurchaseCost(def, 5),
                "neutral overload 는 배수 1.0 경로와 같아야 한다");
        }

        [Test]
        public void CalculatePurchaseCost_With_Invalid_Multiplier_Is_Zero()
        {
            var def = LoadDef("rice_c");
            Assert.AreEqual(0, EconomyOps.CalculatePurchaseCost(def, 3, 0f));
            Assert.AreEqual(0, EconomyOps.CalculatePurchaseCost(def, 3, -1f));
            Assert.AreEqual(0, EconomyOps.CalculatePurchaseCost(def, 3, float.NaN));
            Assert.AreEqual(0, EconomyOps.CalculatePurchaseCost(def, 3, float.PositiveInfinity));
        }

        [Test]
        public void TryPurchaseIngredient_With_Genre_Multiplier_Deducts_Genre_Cost()
        {
            var def = LoadDef("rice_c");
            var state = new GameState();
            int expectedCost = (int)GenreSelectionOps.RoundHalfUp(def.UnitCost * 1.15) * 2;

            var result = EconomyOps.TryPurchaseIngredient(state, def, 2, 1.15f);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(expectedCost, result.TotalCost);
            Assert.AreEqual(GameState.StartingCash - expectedCost, state.cash);
        }

        [Test]
        public void TryPurchaseIngredient_With_Invalid_Multiplier_Fails_Without_Change()
        {
            var def = LoadDef("rice_c");
            var state = new GameState();

            var result = EconomyOps.TryPurchaseIngredient(state, def, 2, 0f);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(GameState.StartingCash, state.cash, "잘못된 배수는 자금 불변");
            Assert.AreEqual(0, state.ingredientStocks.Count);
        }

        // ── task-110: EconomyManager 의 genre 게이트 (씬 레벨) ──────────────

        [Test]
        public void EconomyManager_TryPurchase_Fails_Without_Change_When_Genre_Not_Selected()
        {
            // 배치 EditMode 에서는 OpenScene 이 Awake 를 동기 호출한다는 보장이 없다 — group1/U6 공통 함정.
            // TestSceneSupport 가 4종 singleton Instance 를 이 씬의 실제 컴포넌트로 강제 동기화한다.
            var gameManagerGo = TestSceneSupport.OpenShopSceneWithLiveSingletons();
            var gm = gameManagerGo.GetComponent<GameManager>();
            gm.StartNewGame(); // selectedGenreId 빈 상태로 리셋

            var economy = EconomyManager.Instance;
            Assert.IsNotNull(economy, "EconomyManager.Instance 누락");
            var def = LoadDef("rice_c");
            int cashBefore = gm.State.cash;

            var result = economy.TryPurchaseIngredient(def, 2);

            Assert.IsFalse(result.Success, "장르 미선택 상태의 구매는 실패해야 한다");
            Assert.AreEqual(cashBefore, gm.State.cash, "실패 시 현금 불변");
            Assert.AreEqual(0, InventoryOps.GetQuantity(gm.State, def.Kind, def.Grade), "실패 시 재고 불변");
            Assert.AreEqual(0, gm.State.marketSpendToday, "실패 시 marketSpend 불변");
        }

        [Test]
        public void EconomyManager_TryPurchase_Applies_Genre_Cost_After_Selection()
        {
            var gameManagerGo = TestSceneSupport.OpenShopSceneWithLiveSingletons();
            var gm = gameManagerGo.GetComponent<GameManager>();
            gm.StartNewGame();
            var genreIds = gm.GenreCatalog.Select(g => g.Id).ToList();
            var selection = GenreSelectionOps.TrySelect(gm.State, "noodles", genreIds);
            Assert.IsTrue(selection.Success, selection.Message);

            var economy = EconomyManager.Instance;
            var def = LoadDef("noodle_c");
            var genreDef = LoadGenre("noodles");
            int expectedCost = EconomyOps.CalculatePurchaseCost(def, 3, genreDef.CostMultiplier);

            var result = economy.TryPurchaseIngredient(def, 3);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(expectedCost, result.TotalCost, "장르 원가 배수 적용 구매비가 EconomyOps 와 일치해야 한다");
        }
    }
}
