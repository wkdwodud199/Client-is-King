using System.Collections.Generic;
using System.Linq;
using ClientIsKing.Data;
using ClientIsKing.DayCycle;
using ClientIsKing.Inventory;
using ClientIsKing.Service;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-106: 조리·서빙 핵심 규칙(ServiceOps) 검증 — 결정론적 주문 생성, 재료 계산/소비,
    /// 매출 트랜잭션, 실패 불변성, 등급 혼합 금지, 주문 포기 통계.
    /// </summary>
    public class ServiceOpsTests
    {
        static List<T> LoadAll<T>(string folder) where T : Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder })
                .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null)
                .ToList();
        }

        static List<RecipeDef> Recipes => LoadAll<RecipeDef>("Assets/Data/Definitions/Recipes");
        static List<CustomerArchetypeDef> Customers => LoadAll<CustomerArchetypeDef>("Assets/Data/Definitions/Customers");

        static RecipeDef Recipe(string id)
        {
            var def = Recipes.FirstOrDefault(r => r.Id == id);
            Assert.IsNotNull(def, $"시드 RecipeDef '{id}' 누락 (task-103 전제)");
            return def;
        }

        static GameState StateWithOrder(RecipeDef recipe, int partySize, string customerId = "student")
        {
            var state = new GameState();
            var orders = new List<ServiceOrderState>
            {
                new ServiceOrderState { recipeId = recipe.Id, customerId = customerId, partySize = partySize },
            };
            ServiceOps.StartServiceDay(state, orders, 1);
            return state;
        }

        // ── 주문 생성 ───────────────────────────────────────────────────────

        [Test]
        public void BuildOrders_Is_Deterministic_For_Same_Input_And_Day()
        {
            var a = ServiceOps.BuildOrders(Recipes, Customers, day: 3);
            var b = ServiceOps.BuildOrders(Recipes, Customers, day: 3);

            Assert.AreEqual(ServiceOps.DefaultOrdersPerDay, a.Count);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].recipeId, b[i].recipeId, $"주문 {i}: recipeId 결정론");
                Assert.AreEqual(a[i].customerId, b[i].customerId, $"주문 {i}: customerId 결정론");
                Assert.AreEqual(a[i].partySize, b[i].partySize, $"주문 {i}: partySize 결정론");
            }
        }

        [Test]
        public void BuildOrders_Produces_Valid_Ids_And_PartySizes()
        {
            var recipes = Recipes;
            var customers = Customers;
            var orders = ServiceOps.BuildOrders(recipes, customers, day: 1);

            var recipeIds = recipes.Select(r => r.Id).ToHashSet();
            var customerById = customers.ToDictionary(c => c.Id);
            foreach (var order in orders)
            {
                Assert.IsTrue(recipeIds.Contains(order.recipeId), $"유효 recipeId: {order.recipeId}");
                Assert.IsTrue(customerById.ContainsKey(order.customerId), $"유효 customerId: {order.customerId}");
                var range = customerById[order.customerId].PartySize;
                Assert.GreaterOrEqual(order.partySize, 1);
                Assert.GreaterOrEqual(order.partySize, range.Min);
                Assert.LessOrEqual(order.partySize, range.Max);
            }
        }

        [Test]
        public void BuildOrders_Varies_By_Day()
        {
            var day1 = ServiceOps.BuildOrders(Recipes, Customers, day: 1);
            var day2 = ServiceOps.BuildOrders(Recipes, Customers, day: 2);

            bool anyDifferent = false;
            for (int i = 0; i < day1.Count; i++)
            {
                if (day1[i].recipeId != day2[i].recipeId || day1[i].customerId != day2[i].customerId
                    || day1[i].partySize != day2[i].partySize)
                {
                    anyDifferent = true;
                    break;
                }
            }
            Assert.IsTrue(anyDifferent, "day 가 다르면 주문 목록도 달라야 한다");
        }

        // ── 계산 규칙 ───────────────────────────────────────────────────────

        [Test]
        public void CalculateRequiredIngredients_Multiplies_By_PartySize()
        {
            var recipe = Recipe("pork_gukbap"); // pork 2, rice 1, vegetable 1
            var required = ServiceOps.CalculateRequiredIngredients(recipe, 3);

            Assert.AreEqual(6, required.First(r => r.Kind == IngredientKind.Pork).Quantity);
            Assert.AreEqual(3, required.First(r => r.Kind == IngredientKind.Rice).Quantity);
            Assert.AreEqual(3, required.First(r => r.Kind == IngredientKind.Vegetable).Quantity);
        }

        [Test]
        public void CalculateRequiredIngredients_Aggregates_Duplicate_Kinds()
        {
            // 시드에는 중복 kind 레시피가 없으므로 fixture 를 EditorInit 로 구성 (IVT — task-106)
            var genre = LoadAll<GenreDef>("Assets/Data/Definitions/Genres").First(g => g.Kind == GenreKind.Bunsik);
            var recipe = ScriptableObject.CreateInstance<RecipeDef>();
            recipe.EditorInit("test_dup_kind", "중복테스트", genre,
                new List<RecipeIngredientRequirement>
                {
                    new RecipeIngredientRequirement(IngredientKind.Rice, 1),
                    new RecipeIngredientRequirement(IngredientKind.Rice, 2),
                }, 5f, 1000);
            try
            {
                var required = ServiceOps.CalculateRequiredIngredients(recipe, 2);
                Assert.AreEqual(1, required.Count, "같은 kind 는 하나로 합산");
                Assert.AreEqual((1 + 2) * 2, required[0].Quantity);
            }
            finally
            {
                Object.DestroyImmediate(recipe);
            }
        }

        [Test]
        public void CalculateSalePrice_Is_BasePrice_Times_Party_Without_Multipliers()
        {
            var recipe = Recipe("gimbap");
            Assert.AreEqual(recipe.BasePrice * 4, ServiceOps.CalculateSalePrice(recipe, 4));
            Assert.AreEqual(0, ServiceOps.CalculateSalePrice(recipe, 0));
            Assert.AreEqual(0, ServiceOps.CalculateSalePrice(null, 3));
        }

        // ── 서빙 트랜잭션 ───────────────────────────────────────────────────

        [Test]
        public void TryServe_Success_Consumes_Ingredients_And_Adds_Revenue()
        {
            var recipe = Recipe("pork_gukbap");
            var state = StateWithOrder(recipe, partySize: 2);
            InventoryOps.Add(state, IngredientKind.Pork, IngredientGrade.C, 4);
            InventoryOps.Add(state, IngredientKind.Rice, IngredientGrade.C, 2);
            InventoryOps.Add(state, IngredientKind.Vegetable, IngredientGrade.C, 2);
            int expectedPrice = recipe.BasePrice * 2;

            var result = ServiceOps.TryServeCurrentOrder(state, recipe, IngredientGrade.C);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(expectedPrice, result.RevenueGained);
            Assert.AreEqual(GameState.StartingCash + expectedPrice, state.cash, "cash 증가");
            Assert.AreEqual(expectedPrice, state.serviceRevenueToday, "당일 매출 누적");
            Assert.AreEqual(0, InventoryOps.GetQuantity(state, IngredientKind.Pork, IngredientGrade.C), "재료 정확 차감");
            Assert.AreEqual(0, InventoryOps.GetQuantity(state, IngredientKind.Rice, IngredientGrade.C));
            Assert.AreEqual(0, InventoryOps.GetQuantity(state, IngredientKind.Vegetable, IngredientGrade.C));
            Assert.AreEqual(1, state.serviceOrdersServedToday);
            Assert.AreEqual(2, state.serviceCustomersServedToday, "서빙 고객 +partySize");
            Assert.IsTrue(state.serviceOrders[0].served);
            Assert.IsNull(ServiceOps.GetCurrentOrder(state), "다음 미처리 주문 없음");
            Assert.AreEqual(state.serviceOrders.Count, state.serviceCurrentOrderIndex, "인덱스 이동");
        }

        [Test]
        public void TryServe_Insufficient_Ingredient_Changes_Nothing()
        {
            var recipe = Recipe("pork_gukbap");
            var state = StateWithOrder(recipe, partySize: 2); // 필요: pork 4, rice 2, veg 2
            InventoryOps.Add(state, IngredientKind.Pork, IngredientGrade.C, 3); // 부족
            InventoryOps.Add(state, IngredientKind.Rice, IngredientGrade.C, 2);
            InventoryOps.Add(state, IngredientKind.Vegetable, IngredientGrade.C, 2);

            var result = ServiceOps.TryServeCurrentOrder(state, recipe, IngredientGrade.C);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(GameState.StartingCash, state.cash, "실패 시 cash 불변");
            Assert.AreEqual(0, state.serviceRevenueToday);
            Assert.AreEqual(3, InventoryOps.GetQuantity(state, IngredientKind.Pork, IngredientGrade.C), "재료 불변");
            Assert.AreEqual(2, InventoryOps.GetQuantity(state, IngredientKind.Rice, IngredientGrade.C));
            Assert.AreEqual(0, state.serviceOrdersServedToday);
            Assert.IsTrue(state.serviceOrders[0].IsOpen, "주문 상태 불변");
        }

        [Test]
        public void TryServe_Does_Not_Mix_Grades()
        {
            var recipe = Recipe("janchi_guksu"); // noodle 2, vegetable 1
            var state = StateWithOrder(recipe, partySize: 1);
            // C 는 부족, B 는 충분
            InventoryOps.Add(state, IngredientKind.Noodle, IngredientGrade.C, 1);
            InventoryOps.Add(state, IngredientKind.Noodle, IngredientGrade.B, 2);
            InventoryOps.Add(state, IngredientKind.Vegetable, IngredientGrade.B, 1);

            var withC = ServiceOps.TryServeCurrentOrder(state, recipe, IngredientGrade.C);
            Assert.IsFalse(withC.Success, "C 부족 시 B 자동 혼합 금지");
            Assert.AreEqual(2, InventoryOps.GetQuantity(state, IngredientKind.Noodle, IngredientGrade.B), "B 불변");

            var withB = ServiceOps.TryServeCurrentOrder(state, recipe, IngredientGrade.B);
            Assert.IsTrue(withB.Success, withB.Message);
            Assert.AreEqual(0, InventoryOps.GetQuantity(state, IngredientKind.Noodle, IngredientGrade.B), "B 소비");
            Assert.AreEqual(1, InventoryOps.GetQuantity(state, IngredientKind.Noodle, IngredientGrade.C), "C 는 그대로");
        }

        [Test]
        public void TryServe_Wrong_Recipe_Fails_Without_Change()
        {
            var pork = Recipe("pork_gukbap");
            var gimbap = Recipe("gimbap");
            var state = StateWithOrder(pork, partySize: 1);
            InventoryOps.Add(state, IngredientKind.Rice, IngredientGrade.C, 5);
            InventoryOps.Add(state, IngredientKind.Seaweed, IngredientGrade.C, 5);
            InventoryOps.Add(state, IngredientKind.Vegetable, IngredientGrade.C, 5);

            var result = ServiceOps.TryServeCurrentOrder(state, gimbap, IngredientGrade.C);

            Assert.IsFalse(result.Success, "현재 주문과 다른 레시피는 거부");
            Assert.AreEqual(GameState.StartingCash, state.cash);
            Assert.AreEqual(5, InventoryOps.GetQuantity(state, IngredientKind.Rice, IngredientGrade.C));
            Assert.IsTrue(state.serviceOrders[0].IsOpen);
        }

        [Test]
        public void Skip_Advances_With_Missed_Stats_Only()
        {
            var recipe = Recipe("tteokbokki");
            var state = new GameState();
            var orders = new List<ServiceOrderState>
            {
                new ServiceOrderState { recipeId = recipe.Id, customerId = "student", partySize = 3 },
                new ServiceOrderState { recipeId = recipe.Id, customerId = "office_worker", partySize = 2 },
            };
            ServiceOps.StartServiceDay(state, orders, 1);
            InventoryOps.Add(state, IngredientKind.RiceCake, IngredientGrade.C, 10);

            var skip1 = ServiceOps.SkipCurrentOrder(state);

            Assert.IsTrue(skip1.Success);
            Assert.AreEqual(GameState.StartingCash, state.cash, "포기 시 자금 불변");
            Assert.AreEqual(0, state.serviceRevenueToday, "포기 시 매출 불변");
            Assert.AreEqual(10, InventoryOps.GetQuantity(state, IngredientKind.RiceCake, IngredientGrade.C), "재료 불변");
            Assert.AreEqual(1, state.serviceOrdersMissedToday);
            Assert.AreEqual(3, state.serviceCustomersMissedToday, "이탈 고객 +partySize");
            Assert.IsTrue(state.serviceOrders[0].missed);
            Assert.AreSame(state.serviceOrders[1], ServiceOps.GetCurrentOrder(state), "다음 주문으로 이동");

            ServiceOps.SkipCurrentOrder(state);
            var skip3 = ServiceOps.SkipCurrentOrder(state);
            Assert.IsFalse(skip3.Success, "남은 주문이 없으면 포기도 실패");
            Assert.AreEqual(2, state.serviceOrdersMissedToday, "실패한 포기는 통계 불변");
        }

        [Test]
        public void StartServiceDay_Resets_Daily_Stats()
        {
            var state = new GameState
            {
                serviceRevenueToday = 999,
                serviceOrdersServedToday = 9,
                serviceOrdersMissedToday = 9,
                serviceCustomersServedToday = 9,
                serviceCustomersMissedToday = 9,
            };

            ServiceOps.StartServiceDay(state, new List<ServiceOrderState>(), 4);

            Assert.AreEqual(4, state.serviceDay);
            Assert.AreEqual(0, state.serviceRevenueToday);
            Assert.AreEqual(0, state.serviceOrdersServedToday);
            Assert.AreEqual(0, state.serviceOrdersMissedToday);
            Assert.AreEqual(0, state.serviceCustomersServedToday);
            Assert.AreEqual(0, state.serviceCustomersMissedToday);
            Assert.IsNull(ServiceOps.GetCurrentOrder(state), "빈 주문 목록도 예외 없이 수용");
        }
    }
}
