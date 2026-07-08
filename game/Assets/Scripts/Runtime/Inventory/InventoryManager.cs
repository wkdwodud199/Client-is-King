using ClientIsKing.Data;
using ClientIsKing.Managers;
using UnityEngine;

namespace ClientIsKing.Inventory
{
    /// <summary>
    /// 인벤토리 매니저 (싱글턴 8종 중 하나) — InventoryOps 로 위임하는 thin wrapper.
    /// GameManager 부트스트랩 오브젝트에 함께 배치된다 (SceneBuilder 소유).
    /// </summary>
    public sealed class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        private void Awake()
        {
            // GO 중복 제거는 GameManager 가 담당 — 여기서는 최초 인스턴스만 유지한다.
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

        public int GetQuantity(IngredientKind kind, IngredientGrade grade)
        {
            return State != null ? InventoryOps.GetQuantity(State, kind, grade) : 0;
        }

        public void AddIngredient(IngredientKind kind, IngredientGrade grade, int quantity)
        {
            if (State != null)
            {
                InventoryOps.Add(State, kind, grade, quantity);
            }
        }

        public bool HasIngredients(IngredientKind kind, IngredientGrade grade, int quantity)
        {
            return State != null && InventoryOps.Has(State, kind, grade, quantity);
        }

        public bool TryConsumeIngredient(IngredientKind kind, IngredientGrade grade, int quantity)
        {
            return State != null && InventoryOps.TryConsume(State, kind, grade, quantity);
        }
    }
}
