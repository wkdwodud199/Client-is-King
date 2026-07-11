using System;
using ClientIsKing.Data;
using ClientIsKing.DayCycle;
using ClientIsKing.Genre;
using ClientIsKing.Inventory;

namespace ClientIsKing.Economy
{
    /// <summary>
    /// 경제 핵심 규칙 (순수 C# — EditMode 테스트 대상, 설계 제약: 매니저는 thin wrapper).
    /// 구매 수식: RoundHalfUp(IngredientDef.UnitCost × GenreDef.CostMultiplier) × quantity (task-110).
    /// neutral(배수 1.0) overload 는 기존 공개 API·97 EditMode 기준선 하위호환용으로 유지한다.
    /// 실패 경로는 자금/인벤토리를 절대 변경하지 않는다.
    /// </summary>
    public static class EconomyOps
    {
        /// <summary>구매 총액 (neutral, 배수 1.0). 잘못된 입력(null/0 이하)은 0 — UI 예상 비용 표시용 방어.</summary>
        public static int CalculatePurchaseCost(IngredientDef def, int quantity)
        {
            return CalculatePurchaseCost(def, quantity, 1f);
        }

        /// <summary>
        /// 장르 원가 배수 적용 구매 총액: RoundHalfUp(baseUnitCost × costMultiplier) × quantity (design.md D5/G3).
        /// 잘못된 입력(null/0 이하 수량/0 이하·NaN·Infinity 배수)은 0 — UI 예상 비용 표시용 방어.
        /// </summary>
        public static int CalculatePurchaseCost(IngredientDef def, int quantity, float costMultiplier)
        {
            if (def == null || quantity <= 0 || costMultiplier <= 0
                || float.IsNaN(costMultiplier) || float.IsInfinity(costMultiplier))
            {
                return 0;
            }
            int unitCost = (int)GenreSelectionOps.RoundHalfUp((double)def.UnitCost * costMultiplier);
            return unitCost * quantity;
        }

        public static bool CanAfford(GameState state, int cost)
        {
            Require(state);
            return cost >= 0 && state.cash >= cost;
        }

        /// <summary>
        /// 구매 트랜잭션 (neutral, 배수 1.0) — 성공 시 자금 차감 + 인벤토리 증가를 한 번에 적용.
        /// 자금 부족 / null 재료 / 0 이하 수량은 상태 불변 + 실패 결과 반환 (예외 아님 — 설계 제약).
        /// </summary>
        public static PurchaseResult TryPurchaseIngredient(GameState state, IngredientDef def, int quantity)
        {
            return TryPurchaseIngredient(state, def, quantity, 1f);
        }

        /// <summary>
        /// 장르 원가 배수 적용 구매 트랜잭션 (design.md D5/G3) — Market 예상가와 같은 helper를 사용한다.
        /// 자금 부족 / null 재료 / 0 이하 수량 / 잘못된 배수는 상태 불변 + 실패 결과 반환.
        /// </summary>
        public static PurchaseResult TryPurchaseIngredient(GameState state, IngredientDef def, int quantity, float costMultiplier)
        {
            Require(state);

            if (def == null)
            {
                return new PurchaseResult(false, "재료가 선택되지 않았습니다.", 0, state.cash, 0);
            }
            int owned = InventoryOps.GetQuantity(state, def.Kind, def.Grade);
            if (quantity <= 0)
            {
                return new PurchaseResult(false, "수량은 1 이상이어야 합니다.", 0, state.cash, owned);
            }
            if (costMultiplier <= 0 || float.IsNaN(costMultiplier) || float.IsInfinity(costMultiplier))
            {
                return new PurchaseResult(false, "잘못된 원가 배수입니다.", 0, state.cash, owned);
            }

            int cost = CalculatePurchaseCost(def, quantity, costMultiplier);
            if (state.cash < cost)
            {
                return new PurchaseResult(false, $"자금이 부족합니다 (필요 {cost:N0}원, 보유 {state.cash:N0}원).",
                    cost, state.cash, owned);
            }

            state.cash -= cost;
            InventoryOps.Add(state, def.Kind, def.Grade, quantity);
            // 당일 구매 지출 누적 (task-107 정산 표시용) — day 가 바뀌었으면 먼저 리셋한다.
            if (state.marketSpendDay != state.day)
            {
                state.marketSpendDay = state.day;
                state.marketSpendToday = 0;
            }
            state.marketSpendToday += cost;
            int after = InventoryOps.GetQuantity(state, def.Kind, def.Grade);
            return new PurchaseResult(true, $"{def.DisplayName} {quantity}개 구매 완료 (-{cost:N0}원).",
                cost, state.cash, after);
        }

        static void Require(GameState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state), "GameState 없이 경제 연산을 할 수 없다");
            }
        }
    }
}
