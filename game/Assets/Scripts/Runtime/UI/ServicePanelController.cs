using System.Collections.Generic;
using ClientIsKing.Data;
using ClientIsKing.DayCycle;
using ClientIsKing.Economy;
using ClientIsKing.Inventory;
using ClientIsKing.Managers;
using ClientIsKing.Presentation;
using ClientIsKing.Service;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClientIsKing.UI
{
    /// <summary>
    /// Service phase 영업 UI — 현재 주문 표시, 등급 선택(C/B 토글), 서빙/포기 처리, 당일 통계.
    /// phase 전환 규칙에는 관여하지 않는다 (패널 토글은 PhaseHudController 소관).
    /// </summary>
    public sealed class ServicePanelController : MonoBehaviour
    {
        [SerializeField] private TMP_Text orderText;
        [SerializeField] private TMP_Text customerText;
        [SerializeField] private TMP_Text recipeText;
        [SerializeField] private TMP_Text cookTimeText;
        [SerializeField] private TMP_Text revenueText;
        [SerializeField] private Button gradeToggleButton;
        [SerializeField] private TMP_Text gradeLabel;
        [SerializeField] private TMP_Text requiredText;
        [SerializeField] private Button serveButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private List<RecipeDef> recipeDefs = new List<RecipeDef>();
        [SerializeField] private List<CustomerArchetypeDef> customerDefs = new List<CustomerArchetypeDef>();
        [SerializeField] private List<IngredientDef> ingredientDefs = new List<IngredientDef>();

        /// <summary>SceneBuilder 주입 데이터의 read-only 노출 (테스트 검증용).</summary>
        public IReadOnlyList<RecipeDef> RecipeDefs => recipeDefs;
        public IReadOnlyList<CustomerArchetypeDef> CustomerDefs => customerDefs;

        private IngredientGrade grade = IngredientGrade.C;

        private void OnEnable()
        {
            gradeToggleButton.onClick.AddListener(OnToggleGrade);
            serveButton.onClick.AddListener(OnServe);
            skipButton.onClick.AddListener(OnSkip);

            // Service phase 진입(패널 활성화)마다 오늘 주문을 보장하고 최신 상태로 그린다.
            var service = ServiceManager.Instance;
            if (service != null)
            {
                service.EnsureServiceDay(recipeDefs, customerDefs);
            }
            if (messageText != null)
            {
                messageText.text = "";
            }
            RefreshAll();
            PublishCurrentOrder(); // 무대 연출에 현재 주문 표시 신호 (task-108)
        }

        private void OnDisable()
        {
            gradeToggleButton.onClick.RemoveListener(OnToggleGrade);
            serveButton.onClick.RemoveListener(OnServe);
            skipButton.onClick.RemoveListener(OnSkip);
        }

        private void OnToggleGrade()
        {
            grade = grade == IngredientGrade.C ? IngredientGrade.B : IngredientGrade.C;
            RefreshAll();
        }

        private void OnServe()
        {
            var service = ServiceManager.Instance;
            if (service == null)
            {
                return;
            }
            var captured = service.CurrentOrder; // 처리 "전" 주문 캡처 (표현 이벤트 계약, task-108)
            var result = service.TryServeCurrentOrder(FindRecipe(captured), grade);
            if (messageText != null)
            {
                messageText.text = result.Message;
            }
            if (result.Success)
            {
                var gm = GameManager.Instance;
                GameEvents.RaiseServiceOutcomeResolved(
                    BuildOutcomeArgs(gm != null ? gm.State : null, captured, result, missed: false));
            }
            RefreshAll();
            PublishCurrentOrder();
        }

        private void OnSkip()
        {
            var service = ServiceManager.Instance;
            if (service == null)
            {
                return;
            }
            var captured = service.CurrentOrder;
            var result = service.SkipCurrentOrder();
            if (messageText != null)
            {
                messageText.text = result.Message;
            }
            if (result.Success)
            {
                var gm = GameManager.Instance;
                GameEvents.RaiseServiceOutcomeResolved(
                    BuildOutcomeArgs(gm != null ? gm.State : null, captured, result, missed: true));
            }
            RefreshAll();
            PublishCurrentOrder();
        }

        // ── 표현 이벤트 발행 (task-108 — 도메인 Ops 는 발행하지 않는다) ─────

        private void PublishCurrentOrder()
        {
            var gm = GameManager.Instance;
            var state = gm != null ? gm.State : null;
            var service = ServiceManager.Instance;
            var order = service != null ? service.CurrentOrder : null;
            GameEvents.RaiseServiceOrderPresented(BuildOrderPresentedArgs(state, order));
        }

        /// <summary>주문 표시 payload — Message 에 레시피 표시명을 싣는다 (무대 orderLabel 규약).</summary>
        internal ServicePresentationEventArgs BuildOrderPresentedArgs(DayCycle.GameState state, ServiceOrderState order)
        {
            if (state == null || order == null)
            {
                return ServicePresentationEventArgs.Empty(state != null ? state.day : 0);
            }
            int number = state.serviceOrdersServedToday + state.serviceOrdersMissedToday + 1;
            var recipe = FindRecipe(order);
            return new ServicePresentationEventArgs(
                true, state.day, number, state.serviceOrders.Count,
                order.customerId, order.recipeId, order.partySize,
                served: false, missed: false, revenueGained: 0,
                recipe != null ? recipe.DisplayName : order.recipeId);
        }

        /// <summary>서빙/포기 결과 payload — 처리 전 캡처한 주문 정보를 보존한다.</summary>
        internal ServicePresentationEventArgs BuildOutcomeArgs(
            DayCycle.GameState state, ServiceOrderState captured, ServiceResult result, bool missed)
        {
            return new ServicePresentationEventArgs(
                captured != null, state != null ? state.day : 0, 0,
                state != null ? state.serviceOrders.Count : 0,
                captured != null ? captured.customerId : "",
                captured != null ? captured.recipeId : "",
                captured != null ? captured.partySize : 0,
                served: !missed, missed: missed,
                result.RevenueGained, result.Message);
        }

        private RecipeDef FindRecipe(ServiceOrderState order)
        {
            if (order == null)
            {
                return null;
            }
            foreach (var def in recipeDefs)
            {
                if (def != null && def.Id == order.recipeId)
                {
                    return def;
                }
            }
            return null;
        }

        private CustomerArchetypeDef FindCustomer(ServiceOrderState order)
        {
            if (order == null)
            {
                return null;
            }
            foreach (var def in customerDefs)
            {
                if (def != null && def.Id == order.customerId)
                {
                    return def;
                }
            }
            return null;
        }

        private string IngredientLabel(IngredientKind kind)
        {
            foreach (var def in ingredientDefs)
            {
                if (def != null && def.Kind == kind)
                {
                    var name = def.DisplayName;
                    int cut = name.IndexOf(" (");
                    return cut > 0 ? name.Substring(0, cut) : name;
                }
            }
            return kind.ToString();
        }

        private void RefreshAll()
        {
            var gm = GameManager.Instance;
            var state = gm != null ? gm.State : null;
            var service = ServiceManager.Instance;
            var order = service != null ? service.CurrentOrder : null;
            var recipe = FindRecipe(order);
            var customer = FindCustomer(order);

            bool hasOrder = order != null;
            if (serveButton != null) serveButton.interactable = hasOrder;
            if (skipButton != null) skipButton.interactable = hasOrder;

            if (orderText != null)
            {
                if (state != null && state.serviceOrders.Count > 0 && hasOrder)
                {
                    int position = state.serviceOrdersServedToday + state.serviceOrdersMissedToday + 1;
                    orderText.text = $"주문 {position}/{state.serviceOrders.Count}";
                }
                else
                {
                    orderText.text = "오늘 영업 종료 — 주문 없음";
                }
            }
            if (customerText != null)
            {
                customerText.text = customer != null && order != null
                    ? $"{customer.DisplayName} ×{order.partySize}" : "-";
            }
            if (recipeText != null)
            {
                recipeText.text = recipe != null ? recipe.DisplayName : "-";
            }
            if (cookTimeText != null)
            {
                cookTimeText.text = recipe != null ? $"조리 {recipe.CookSeconds:0.#}초" : "";
            }
            if (revenueText != null && order != null)
            {
                revenueText.text = $"예상 매출 {ServiceOps.CalculateSalePrice(recipe, order.partySize):N0}원";
            }
            else if (revenueText != null)
            {
                revenueText.text = "";
            }
            if (gradeLabel != null)
            {
                gradeLabel.text = $"등급: {ServiceOps.GradeLabel(grade)}";
            }
            if (requiredText != null)
            {
                requiredText.text = BuildRequiredLine(state, recipe, order);
            }
            if (statsText != null && state != null)
            {
                statsText.text = $"오늘 매출 {state.serviceRevenueToday:N0}원 · " +
                    $"서빙 {state.serviceCustomersServedToday}명 · 이탈 {state.serviceCustomersMissedToday}명";
            }
        }

        private string BuildRequiredLine(DayCycle.GameState state, RecipeDef recipe, ServiceOrderState order)
        {
            if (state == null || recipe == null || order == null)
            {
                return "";
            }
            var parts = new List<string>();
            foreach (var req in ServiceOps.CalculateRequiredIngredients(recipe, order.partySize))
            {
                int have = InventoryOps.GetQuantity(state, req.Kind, grade);
                parts.Add($"{IngredientLabel(req.Kind)} {have}/{req.Quantity}");
            }
            return $"필요({ServiceOps.GradeLabel(grade)}): " + string.Join("  ", parts);
        }

#if UNITY_EDITOR
        /// <summary>SceneBuilder 전용 참조/데이터 주입 (EditorInit 패턴).</summary>
        internal void EditorInit(
            TMP_Text orderText, TMP_Text customerText, TMP_Text recipeText,
            TMP_Text cookTimeText, TMP_Text revenueText,
            Button gradeToggleButton, TMP_Text gradeLabel, TMP_Text requiredText,
            Button serveButton, Button skipButton, TMP_Text statsText, TMP_Text messageText,
            List<RecipeDef> recipeDefs, List<CustomerArchetypeDef> customerDefs, List<IngredientDef> ingredientDefs)
        {
            this.orderText = orderText;
            this.customerText = customerText;
            this.recipeText = recipeText;
            this.cookTimeText = cookTimeText;
            this.revenueText = revenueText;
            this.gradeToggleButton = gradeToggleButton;
            this.gradeLabel = gradeLabel;
            this.requiredText = requiredText;
            this.serveButton = serveButton;
            this.skipButton = skipButton;
            this.statsText = statsText;
            this.messageText = messageText;
            this.recipeDefs = recipeDefs;
            this.customerDefs = customerDefs;
            this.ingredientDefs = ingredientDefs;
        }
#endif
    }
}
