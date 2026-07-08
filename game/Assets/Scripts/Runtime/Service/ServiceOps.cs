using System;
using System.Collections.Generic;
using ClientIsKing.Data;
using ClientIsKing.DayCycle;
using ClientIsKing.Inventory;

namespace ClientIsKing.Service
{
    /// <summary>
    /// 조리·서빙 핵심 규칙 (순수 C# — EditMode 테스트 대상, 매니저는 thin wrapper).
    ///
    /// 계약 (task-106 설계):
    /// - 주문 생성은 같은 입력+day 에서 결정론적 (의사난수 대신 day/인덱스 산식 — 씨앗 주입 불필요).
    /// - 판매가 = RecipeDef.BasePrice × partySize (장르/품질/이벤트 배수는 task-108+).
    /// - 등급 혼합 금지 — 선택 등급(C/B) 재료만 소비한다.
    /// - 서빙은 전체 필요량 preflight 후에만 소비 (실패 경로는 자금·인벤·통계 완전 불변).
    /// </summary>
    public static class ServiceOps
    {
        public const int DefaultOrdersPerDay = 5;

        /// <summary>
        /// day 기준 결정론적 주문 목록 생성. 정렬(id) 후 day/인덱스 산식으로 선택해
        /// 매일 같은 목록이 반복되지 않게 한다 (설계 3단계).
        /// </summary>
        public static List<ServiceOrderState> BuildOrders(
            IReadOnlyList<RecipeDef> recipes, IReadOnlyList<CustomerArchetypeDef> customers,
            int day, int orderCount = DefaultOrdersPerDay)
        {
            if (recipes == null) throw new ArgumentNullException(nameof(recipes));
            if (customers == null) throw new ArgumentNullException(nameof(customers));

            var orders = new List<ServiceOrderState>();
            if (recipes.Count == 0 || customers.Count == 0 || orderCount <= 0)
            {
                return orders; // 주문 없는 영업일 — StartServiceDay 가 빈 목록을 그대로 수용
            }

            var sortedRecipes = new List<RecipeDef>(recipes);
            sortedRecipes.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            var sortedCustomers = new List<CustomerArchetypeDef>(customers);
            sortedCustomers.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

            int dayShift = Math.Max(0, day - 1);
            for (int i = 0; i < orderCount; i++)
            {
                var recipe = sortedRecipes[(dayShift + i) % sortedRecipes.Count];
                // 고객은 다른 보폭(×2 시작점, ×3 스텝)으로 순회 — 레시피와 짝이 고정되지 않게.
                var customer = sortedCustomers[(dayShift * 2 + i * 3) % sortedCustomers.Count];

                int min = Math.Max(1, customer.PartySize.Min);
                int span = Math.Max(1, customer.PartySize.Max - min + 1);
                int party = min + (dayShift + i) % span;

                orders.Add(new ServiceOrderState
                {
                    recipeId = recipe.Id,
                    customerId = customer.Id,
                    partySize = party,
                });
            }
            return orders;
        }

        /// <summary>해당 day 의 주문 목록과 당일 통계를 초기화한다 (빈 목록도 허용 — 설계 4단계).</summary>
        public static void StartServiceDay(GameState state, List<ServiceOrderState> orders, int day)
        {
            Require(state);
            state.serviceDay = day;
            state.serviceOrders = orders ?? new List<ServiceOrderState>();
            state.serviceCurrentOrderIndex = FindNextOpenIndex(state, 0);
            state.serviceRevenueToday = 0;
            state.serviceOrdersServedToday = 0;
            state.serviceOrdersMissedToday = 0;
            state.serviceCustomersServedToday = 0;
            state.serviceCustomersMissedToday = 0;
        }

        /// <summary>처리되지 않은 다음 주문 (없으면 null).</summary>
        public static ServiceOrderState GetCurrentOrder(GameState state)
        {
            Require(state);
            // FindNextOpenIndex 는 "없음" 을 -1 이 아니라 orders.Count 로 반환한다
            // (serviceCurrentOrderIndex 필드 규약) — 범위 검사로 null 을 판정한다.
            int index = FindNextOpenIndex(state, 0);
            return index < state.serviceOrders.Count ? state.serviceOrders[index] : null;
        }

        public static bool HasOpenOrders(GameState state)
        {
            return GetCurrentOrder(state) != null;
        }

        /// <summary>레시피 요구량 × 파티 크기 — 같은 IngredientKind 는 합산 (설계 6단계).</summary>
        public static List<RequiredIngredient> CalculateRequiredIngredients(RecipeDef recipe, int partySize)
        {
            var result = new List<RequiredIngredient>();
            if (recipe == null || partySize <= 0)
            {
                return result;
            }
            foreach (var req in recipe.Ingredients)
            {
                int need = req.Quantity * partySize;
                int existing = result.FindIndex(r => r.Kind == req.Kind);
                if (existing >= 0)
                {
                    result[existing] = new RequiredIngredient(req.Kind, result[existing].Quantity + need);
                }
                else
                {
                    result.Add(new RequiredIngredient(req.Kind, need));
                }
            }
            return result;
        }

        /// <summary>판매가 = BasePrice × partySize (배수 없음 — 설계 7단계).</summary>
        public static int CalculateSalePrice(RecipeDef recipe, int partySize)
        {
            if (recipe == null || partySize <= 0)
            {
                return 0;
            }
            return recipe.BasePrice * partySize;
        }

        /// <summary>선택 등급 재료가 전부 충분한지 확인. 부족분은 shortage 요약으로 반환.</summary>
        public static bool CanServeOrder(GameState state, RecipeDef recipe, IngredientGrade grade,
            int partySize, out string shortage)
        {
            Require(state);
            shortage = "";
            if (recipe == null || partySize <= 0)
            {
                shortage = "잘못된 주문입니다.";
                return false;
            }
            var missing = new List<string>();
            foreach (var req in CalculateRequiredIngredients(recipe, partySize))
            {
                int have = InventoryOps.GetQuantity(state, req.Kind, grade);
                if (have < req.Quantity)
                {
                    missing.Add($"{req.Kind} {have}/{req.Quantity}");
                }
            }
            if (missing.Count > 0)
            {
                shortage = string.Join(", ", missing);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 현재 주문 서빙 트랜잭션 — preflight 전체 통과 후에만 소비·매출 반영 (설계 9단계).
        /// 실패 경로(주문 없음/레시피 불일치/재료 부족)는 상태 완전 불변.
        /// </summary>
        public static ServiceResult TryServeCurrentOrder(GameState state, RecipeDef recipe, IngredientGrade grade)
        {
            Require(state);

            var order = GetCurrentOrder(state);
            if (order == null)
            {
                return new ServiceResult(false, "처리할 주문이 없습니다.", 0, state.cash);
            }
            if (recipe == null)
            {
                return new ServiceResult(false, "레시피가 선택되지 않았습니다.", 0, state.cash);
            }
            if (!string.Equals(recipe.Id, order.recipeId, StringComparison.Ordinal))
            {
                return new ServiceResult(false, $"현재 주문은 '{order.recipeId}' 입니다.", 0, state.cash);
            }
            if (order.partySize <= 0)
            {
                return new ServiceResult(false, "잘못된 파티 크기입니다.", 0, state.cash);
            }

            if (!CanServeOrder(state, recipe, grade, order.partySize, out var shortage))
            {
                return new ServiceResult(false, $"{GradeLabel(grade)} 재료 부족: {shortage}", 0, state.cash);
            }

            // preflight 통과 — 전체 소비 (TryConsume 는 여기서 실패할 수 없다)
            foreach (var req in CalculateRequiredIngredients(recipe, order.partySize))
            {
                InventoryOps.TryConsume(state, req.Kind, grade, req.Quantity);
            }

            int price = CalculateSalePrice(recipe, order.partySize);
            state.cash += price;
            state.serviceRevenueToday += price;
            state.serviceOrdersServedToday++;
            state.serviceCustomersServedToday += order.partySize;
            order.served = true;
            state.serviceCurrentOrderIndex = FindNextOpenIndex(state, 0);

            return new ServiceResult(true,
                $"{recipe.DisplayName} ×{order.partySize} 서빙 완료 (+{price:N0}원)", price, state.cash);
        }

        /// <summary>현재 주문 포기 — 매출/인벤 불변, 실패 통계만 증가 (설계 10단계).</summary>
        public static ServiceResult SkipCurrentOrder(GameState state)
        {
            Require(state);

            var order = GetCurrentOrder(state);
            if (order == null)
            {
                return new ServiceResult(false, "포기할 주문이 없습니다.", 0, state.cash);
            }

            order.missed = true;
            state.serviceOrdersMissedToday++;
            state.serviceCustomersMissedToday += Math.Max(0, order.partySize);
            state.serviceCurrentOrderIndex = FindNextOpenIndex(state, 0);

            return new ServiceResult(true, $"주문 포기 — 손님 {order.partySize}명 이탈.", 0, state.cash);
        }

        public static string GradeLabel(IngredientGrade grade)
        {
            return grade == IngredientGrade.C ? "C급" : "B급";
        }

        static int FindNextOpenIndex(GameState state, int from)
        {
            var orders = state.serviceOrders;
            for (int i = Math.Max(0, from); i < orders.Count; i++)
            {
                if (orders[i].IsOpen)
                {
                    return i;
                }
            }
            return orders.Count; // 전부 처리됨 — 인덱스는 목록 끝을 가리킨다
        }

        static void Require(GameState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state), "GameState 없이 서비스 연산을 할 수 없다");
            }
        }
    }
}
