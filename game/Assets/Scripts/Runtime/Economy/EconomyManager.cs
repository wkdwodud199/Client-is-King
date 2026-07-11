using ClientIsKing.Data;
using ClientIsKing.Managers;
using UnityEngine;

namespace ClientIsKing.Economy
{
    /// <summary>
    /// 경제 매니저 (싱글턴 8종 중 하나) — EconomyOps 로 위임하는 thin wrapper.
    /// GameManager 부트스트랩 오브젝트에 함께 배치된다 (SceneBuilder 소유).
    /// </summary>
    public sealed class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        static DayCycle.GameState State => GameManager.Instance != null ? GameManager.Instance.State : null;

        /// <summary>현재 자금 (상태 미초기화 시 0).</summary>
        public int Cash => State != null ? State.cash : 0;

        public int CalculatePurchaseCost(IngredientDef def, int quantity)
        {
            return EconomyOps.CalculatePurchaseCost(def, quantity);
        }

        public bool CanAfford(int cost)
        {
            return State != null && EconomyOps.CanAfford(State, cost);
        }

        public PurchaseResult TryPurchaseIngredient(IngredientDef def, int quantity)
        {
            var state = State;
            if (state == null)
            {
                return new PurchaseResult(false, "게임 상태가 초기화되지 않았습니다.", 0, 0, 0);
            }
            // task-110: 전문 분야 미선택/미존재 genre 는 현금·재고·marketSpend 불변 실패 (design.md D5/G3).
            if (string.IsNullOrEmpty(state.selectedGenreId))
            {
                return new PurchaseResult(false, "먼저 전문 분야를 선택하세요.", 0, state.cash, 0);
            }
            var gm = GameManager.Instance;
            if (gm == null || !gm.TryGetGenre(state.selectedGenreId, out var genre))
            {
                return new PurchaseResult(false, "선택된 전문 분야를 찾을 수 없습니다.", 0, state.cash, 0);
            }
            // task-112 E3: 오늘 이벤트 효과(재료값 폭등 배수)를 조회해 구매 경로에 전달한다.
            if (!gm.TryBuildTodayEventEffects(out var fx, out var reason))
            {
                return new PurchaseResult(false, reason, 0, state.cash, 0);
            }
            return EconomyOps.TryPurchaseIngredient(state, def, quantity, genre.CostMultiplier, fx.IngredientCostMilli);
        }

        /// <summary>
        /// 장르+이벤트를 같은 helper 로 합성한 UI 예상가의 단일 경로 (task-112 E3 — 예상가 = 거래가 보장, task-110 G3 유지).
        /// </summary>
        public bool TryCalculatePurchaseCost(IngredientDef def, int quantity, out int cost, out string reason)
        {
            var state = State;
            if (state == null)
            {
                cost = 0;
                reason = "게임 상태가 초기화되지 않았습니다.";
                return false;
            }
            var gm = GameManager.Instance;
            if (gm == null || !gm.TryGetGenre(state.selectedGenreId, out var genre))
            {
                cost = 0;
                reason = "선택된 전문 분야를 찾을 수 없습니다.";
                return false;
            }
            if (!gm.TryBuildTodayEventEffects(out var fx, out reason))
            {
                cost = 0;
                return false;
            }
            cost = EconomyOps.CalculatePurchaseCost(def, quantity, genre.CostMultiplier, fx.IngredientCostMilli);
            reason = "";
            return true;
        }
    }
}
