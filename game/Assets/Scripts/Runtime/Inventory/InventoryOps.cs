using System;
using ClientIsKing.Data;
using ClientIsKing.DayCycle;

namespace ClientIsKing.Inventory
{
    /// <summary>
    /// 인벤토리 핵심 규칙 (순수 C# — EditMode 테스트 대상, 설계 제약: 매니저는 thin wrapper).
    /// 모든 실패 경로는 상태를 변경하지 않는다. null state 는 구현자 오류 → 명확한 예외.
    /// </summary>
    public static class InventoryOps
    {
        public static int GetQuantity(GameState state, IngredientKind kind, IngredientGrade grade)
        {
            Require(state);
            var stock = Find(state, kind, grade);
            return stock != null ? stock.quantity : 0;
        }

        /// <summary>같은 종류×등급 항목이 있으면 누적, 없으면 새 항목. qty ≤ 0 은 무시(no-op).</summary>
        public static void Add(GameState state, IngredientKind kind, IngredientGrade grade, int quantity)
        {
            Require(state);
            if (quantity <= 0)
            {
                return;
            }
            var stock = Find(state, kind, grade);
            if (stock == null)
            {
                state.ingredientStocks.Add(new IngredientStock { kind = kind, grade = grade, quantity = quantity });
            }
            else
            {
                stock.quantity += quantity;
            }
        }

        public static bool Has(GameState state, IngredientKind kind, IngredientGrade grade, int quantity)
        {
            Require(state);
            return quantity > 0 && GetQuantity(state, kind, grade) >= quantity;
        }

        /// <summary>충분하면 차감 후 true. 부족·qty ≤ 0 이면 상태 불변 + false.</summary>
        public static bool TryConsume(GameState state, IngredientKind kind, IngredientGrade grade, int quantity)
        {
            Require(state);
            if (quantity <= 0)
            {
                return false;
            }
            var stock = Find(state, kind, grade);
            if (stock == null || stock.quantity < quantity)
            {
                return false;
            }
            stock.quantity -= quantity;
            return true;
        }

        static IngredientStock Find(GameState state, IngredientKind kind, IngredientGrade grade)
        {
            var stocks = state.ingredientStocks;
            for (int i = 0; i < stocks.Count; i++)
            {
                if (stocks[i].kind == kind && stocks[i].grade == grade)
                {
                    return stocks[i];
                }
            }
            return null;
        }

        static void Require(GameState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state), "GameState 없이 인벤토리를 조작할 수 없다");
            }
        }
    }
}
