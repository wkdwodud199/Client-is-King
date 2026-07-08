using System;
using ClientIsKing.Data;
using ClientIsKing.DayCycle;
using ClientIsKing.Inventory;

namespace ClientIsKing.Economy
{
    /// <summary>
    /// 경제 핵심 규칙 (순수 C# — EditMode 테스트 대상, 설계 제약: 매니저는 thin wrapper).
    /// 구매 수식: IngredientDef.UnitCost × quantity (장르 배수는 task-108 에서 적용).
    /// 실패 경로는 자금/인벤토리를 절대 변경하지 않는다.
    /// </summary>
    public static class EconomyOps
    {
        /// <summary>구매 총액. 잘못된 입력(null/0 이하)은 0 — UI 예상 비용 표시용 방어.</summary>
        public static int CalculatePurchaseCost(IngredientDef def, int quantity)
        {
            if (def == null || quantity <= 0)
            {
                return 0;
            }
            return def.UnitCost * quantity;
        }

        public static bool CanAfford(GameState state, int cost)
        {
            Require(state);
            return cost >= 0 && state.cash >= cost;
        }

        /// <summary>
        /// 구매 트랜잭션 — 성공 시 자금 차감 + 인벤토리 증가를 한 번에 적용.
        /// 자금 부족 / null 재료 / 0 이하 수량은 상태 불변 + 실패 결과 반환 (예외 아님 — 설계 제약).
        /// </summary>
        public static PurchaseResult TryPurchaseIngredient(GameState state, IngredientDef def, int quantity)
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

            int cost = CalculatePurchaseCost(def, quantity);
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
