using ClientIsKing.Data;
using ClientIsKing.DayCycle;
using ClientIsKing.Inventory;
using NUnit.Framework;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-105: 인벤토리 핵심 규칙(InventoryOps — InventoryManager 가 위임하는 순수 코어) 검증.
    /// </summary>
    public class InventoryManagerTests
    {
        [Test]
        public void GetQuantity_Defaults_To_Zero()
        {
            var state = new GameState();
            Assert.AreEqual(0, InventoryOps.GetQuantity(state, IngredientKind.Rice, IngredientGrade.C));
        }

        [Test]
        public void Add_Accumulates_Same_Kind_And_Grade()
        {
            var state = new GameState();
            InventoryOps.Add(state, IngredientKind.Rice, IngredientGrade.C, 2);
            InventoryOps.Add(state, IngredientKind.Rice, IngredientGrade.C, 3);

            Assert.AreEqual(1, state.ingredientStocks.Count, "같은 종류×등급은 항목 1개로 누적");
            Assert.AreEqual(5, InventoryOps.GetQuantity(state, IngredientKind.Rice, IngredientGrade.C));
        }

        [Test]
        public void Different_Grade_Kept_As_Separate_Entries()
        {
            var state = new GameState();
            InventoryOps.Add(state, IngredientKind.Rice, IngredientGrade.C, 2);
            InventoryOps.Add(state, IngredientKind.Rice, IngredientGrade.B, 3);

            Assert.AreEqual(2, state.ingredientStocks.Count, "등급이 다르면 별도 항목");
            Assert.AreEqual(2, InventoryOps.GetQuantity(state, IngredientKind.Rice, IngredientGrade.C));
            Assert.AreEqual(3, InventoryOps.GetQuantity(state, IngredientKind.Rice, IngredientGrade.B));
        }

        [Test]
        public void Add_NonPositive_Is_Ignored()
        {
            var state = new GameState();
            InventoryOps.Add(state, IngredientKind.Pork, IngredientGrade.C, 0);
            InventoryOps.Add(state, IngredientKind.Pork, IngredientGrade.C, -4);
            Assert.AreEqual(0, state.ingredientStocks.Count);
        }

        [Test]
        public void TryConsume_Sufficient_Decrements()
        {
            var state = new GameState();
            InventoryOps.Add(state, IngredientKind.Noodle, IngredientGrade.C, 5);

            Assert.IsTrue(InventoryOps.TryConsume(state, IngredientKind.Noodle, IngredientGrade.C, 3));
            Assert.AreEqual(2, InventoryOps.GetQuantity(state, IngredientKind.Noodle, IngredientGrade.C));
        }

        [Test]
        public void TryConsume_Insufficient_Fails_Without_Change()
        {
            var state = new GameState();
            InventoryOps.Add(state, IngredientKind.Noodle, IngredientGrade.C, 2);

            Assert.IsFalse(InventoryOps.TryConsume(state, IngredientKind.Noodle, IngredientGrade.C, 5));
            Assert.AreEqual(2, InventoryOps.GetQuantity(state, IngredientKind.Noodle, IngredientGrade.C), "실패 시 불변");
        }

        [Test]
        public void TryConsume_NonPositive_Fails()
        {
            var state = new GameState();
            InventoryOps.Add(state, IngredientKind.Beef, IngredientGrade.B, 2);

            Assert.IsFalse(InventoryOps.TryConsume(state, IngredientKind.Beef, IngredientGrade.B, 0));
            Assert.IsFalse(InventoryOps.TryConsume(state, IngredientKind.Beef, IngredientGrade.B, -1));
            Assert.AreEqual(2, InventoryOps.GetQuantity(state, IngredientKind.Beef, IngredientGrade.B));
        }

        [Test]
        public void Has_Checks_Threshold()
        {
            var state = new GameState();
            InventoryOps.Add(state, IngredientKind.Seaweed, IngredientGrade.C, 3);

            Assert.IsTrue(InventoryOps.Has(state, IngredientKind.Seaweed, IngredientGrade.C, 3));
            Assert.IsFalse(InventoryOps.Has(state, IngredientKind.Seaweed, IngredientGrade.C, 4));
            Assert.IsFalse(InventoryOps.Has(state, IngredientKind.Seaweed, IngredientGrade.C, 0), "0 이하 요청은 false");
        }
    }
}
