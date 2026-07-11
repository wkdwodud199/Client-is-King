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
            return EconomyOps.TryPurchaseIngredient(state, def, quantity, genre.CostMultiplier);
        }
    }
}
