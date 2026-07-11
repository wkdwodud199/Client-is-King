using System.Collections.Generic;
using System.Linq;
using ClientIsKing.Data;
using ClientIsKing.DayCycle;
using ClientIsKing.Genre;
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
    /// task-110 (U6): genre recipe 필터·plan 기반 주문 수·customer weight·장르 적용 가격을 추가한다.
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

        // ── task-110: 장르 적용 판매가 ────────────────────────────────────────

        [Test]
        public void CalculateSalePrice_With_Genre_Multiplier_Uses_RoundHalfUp()
        {
            var recipe = Recipe("pork_gukbap"); // basePrice 9000
            int expected = (int)GenreSelectionOps.RoundHalfUp(9000.0 * 3 * 0.95);
            Assert.AreEqual(expected, ServiceOps.CalculateSalePrice(recipe, 3, 0.95f));
        }

        [Test]
        public void CalculateSalePrice_Neutral_Overload_Matches_Multiplier_1()
        {
            var recipe = Recipe("gimbap");
            Assert.AreEqual(ServiceOps.CalculateSalePrice(recipe, 2, 1f), ServiceOps.CalculateSalePrice(recipe, 2),
                "neutral overload 는 배수 1.0 경로와 같아야 한다");
        }

        [Test]
        public void CalculateSalePrice_With_Invalid_Multiplier_Is_Zero()
        {
            var recipe = Recipe("gimbap");
            Assert.AreEqual(0, ServiceOps.CalculateSalePrice(recipe, 2, 0f));
            Assert.AreEqual(0, ServiceOps.CalculateSalePrice(recipe, 2, -1f));
            Assert.AreEqual(0, ServiceOps.CalculateSalePrice(recipe, 2, float.NaN));
            Assert.AreEqual(0, ServiceOps.CalculateSalePrice(recipe, 2, float.PositiveInfinity));
        }

        [Test]
        public void TryServe_With_Genre_Input_Applies_Genre_Price_To_Cash_And_Revenue()
        {
            var recipe = Recipe("pork_gukbap");
            var state = StateWithOrder(recipe, partySize: 2);
            InventoryOps.Add(state, IngredientKind.Pork, IngredientGrade.C, 4);
            InventoryOps.Add(state, IngredientKind.Rice, IngredientGrade.C, 2);
            InventoryOps.Add(state, IngredientKind.Vegetable, IngredientGrade.C, 2);

            var genreDef = LoadAll<GenreDef>("Assets/Data/Definitions/Genres").First(g => g.Id == "gukbap");
            var genreInput = new GenreDefInput
            {
                Id = genreDef.Id,
                IsGeneralist = false,
                CookTimeMultiplier = genreDef.CookTimeMultiplier,
                PricePerCustomerMultiplier = genreDef.PricePerCustomerMultiplier,
            };
            int expectedPrice = ServiceOps.CalculateSalePrice(recipe, 2, genreDef.PricePerCustomerMultiplier);

            var result = ServiceOps.TryServeCurrentOrder(state, recipe, IngredientGrade.C, genreInput);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(expectedPrice, result.RevenueGained);
            Assert.AreEqual(GameState.StartingCash + expectedPrice, state.cash);
            Assert.AreEqual(expectedPrice, state.serviceRevenueToday);
        }

        [Test]
        public void TryServe_With_Null_Genre_Input_Falls_Back_To_Neutral()
        {
            var recipe = Recipe("gimbap");
            var state = StateWithOrder(recipe, partySize: 1);
            InventoryOps.Add(state, IngredientKind.Rice, IngredientGrade.C, 1);
            InventoryOps.Add(state, IngredientKind.Seaweed, IngredientGrade.C, 1);
            InventoryOps.Add(state, IngredientKind.Vegetable, IngredientGrade.C, 1);

            var result = ServiceOps.TryServeCurrentOrder(state, recipe, IngredientGrade.C, (GenreDefInput)null);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(recipe.BasePrice, result.RevenueGained, "null genre 는 neutral(배수 1.0) 로 처리");
        }

        // ── task-110: genre 기반 plan → 주문 (recipe 필터·주문 수·customer weight) ──

        [Test]
        public void BuildOrders_From_Plan_Never_Uses_Other_Genre_Recipes()
        {
            var genreDef = LoadAll<GenreDef>("Assets/Data/Definitions/Genres").First(g => g.Id == "bunsik");
            var recipes = Recipes;
            var customers = Customers;

            var genreInput = new GenreDefInput
            {
                Id = genreDef.Id,
                IsGeneralist = false,
                CookTimeMultiplier = genreDef.CookTimeMultiplier,
                PricePerCustomerMultiplier = genreDef.PricePerCustomerMultiplier,
                CustomerAffinities = genreDef.CustomerAffinities
                    .Select(a => new GenreAffinityInput(a.Archetype.Id, a.Multiplier)).ToList(),
            };
            var recipeInputs = recipes.Select(r => new RecipeDefInput { Id = r.Id, GenreId = r.Genre.Id, BasePrice = r.BasePrice }).ToList();
            var customerInputs = customers.Select(c => new CustomerDefInput { Id = c.Id, BaseSpawnWeight = c.BaseSpawnWeight }).ToList();

            Assert.IsTrue(GenreSelectionOps.TryBuildDemandPlan(genreInput, 5, recipeInputs, customerInputs, out var plan, out var reason), reason);
            Assert.AreEqual(6, plan.OrderCount, "분식 orderCount 는 시드 조건에서 6건");

            var orders = ServiceOps.BuildOrders(plan, customers);
            Assert.AreEqual(6, orders.Count);
            foreach (var order in orders)
            {
                Assert.IsTrue(order.recipeId == "tteokbokki" || order.recipeId == "gimbap",
                    $"분식 plan 주문에 다른 장르 recipe 가 섞임: {order.recipeId}");
            }
        }

        [Test]
        public void BuildOrders_From_Plan_Is_Deterministic()
        {
            var genreDef = LoadAll<GenreDef>("Assets/Data/Definitions/Genres").First(g => g.Id == "noodles");
            var recipes = Recipes;
            var customers = Customers;

            var genreInput = new GenreDefInput
            {
                Id = genreDef.Id,
                IsGeneralist = false,
                CookTimeMultiplier = genreDef.CookTimeMultiplier,
                PricePerCustomerMultiplier = genreDef.PricePerCustomerMultiplier,
                CustomerAffinities = genreDef.CustomerAffinities
                    .Select(a => new GenreAffinityInput(a.Archetype.Id, a.Multiplier)).ToList(),
            };
            var recipeInputs = recipes.Select(r => new RecipeDefInput { Id = r.Id, GenreId = r.Genre.Id, BasePrice = r.BasePrice }).ToList();
            var customerInputs = customers.Select(c => new CustomerDefInput { Id = c.Id, BaseSpawnWeight = c.BaseSpawnWeight }).ToList();

            GenreSelectionOps.TryBuildDemandPlan(genreInput, 7, recipeInputs, customerInputs, out var planA, out _);
            GenreSelectionOps.TryBuildDemandPlan(genreInput, 7, recipeInputs, customerInputs, out var planB, out _);
            var ordersA = ServiceOps.BuildOrders(planA, customers);
            var ordersB = ServiceOps.BuildOrders(planB, customers);

            Assert.AreEqual(ordersA.Count, ordersB.Count);
            for (int i = 0; i < ordersA.Count; i++)
            {
                Assert.AreEqual(ordersA[i].recipeId, ordersB[i].recipeId, $"주문 {i} recipeId 결정론");
                Assert.AreEqual(ordersA[i].customerId, ordersB[i].customerId, $"주문 {i} customerId 결정론");
                Assert.AreEqual(ordersA[i].partySize, ordersB[i].partySize, $"주문 {i} partySize 결정론");
            }
        }

        // ── task-111 D3: SNS 보너스 주문 태그·통계 ──────────────────────────

        [Test]
        public void BuildOrders_From_Plan_Tags_Only_Bonus_Indices_As_SnsInflow()
        {
            var genreDef = LoadAll<GenreDef>("Assets/Data/Definitions/Genres").First(g => g.Id == "bunsik");
            var recipes = Recipes;
            var customers = Customers;

            var genreInput = new GenreDefInput
            {
                Id = genreDef.Id,
                IsGeneralist = false,
                CookTimeMultiplier = genreDef.CookTimeMultiplier,
                PricePerCustomerMultiplier = genreDef.PricePerCustomerMultiplier,
                CustomerAffinities = genreDef.CustomerAffinities
                    .Select(a => new GenreAffinityInput(a.Archetype.Id, a.Multiplier)).ToList(),
            };
            var recipeInputs = recipes.Select(r => new RecipeDefInput { Id = r.Id, GenreId = r.Genre.Id, BasePrice = r.BasePrice }).ToList();
            var customerInputs = customers.Select(c => new CustomerDefInput
            {
                Id = c.Id, BaseSpawnWeight = c.BaseSpawnWeight, AgeBand = c.AgeBand, Gender = c.Gender,
            }).ToList();

            var boosts = new List<CustomerWeightBoost>
            {
                new CustomerWeightBoost("student", 1600),
                new CustomerWeightBoost("office_worker", 1300),
                new CustomerWeightBoost("family_parent", 1000),
                new CustomerWeightBoost("senior_regular", 1000),
            };
            var modifier = new DayModifier(2, "short_form", 2, boosts);

            Assert.IsTrue(GenreSelectionOps.TryBuildDemandPlan(genreInput, 2, recipeInputs, customerInputs, modifier, out var plan, out var reason), reason);
            Assert.AreEqual(8, plan.OrderCount);

            var orders = ServiceOps.BuildOrders(plan, customers);
            Assert.AreEqual(8, orders.Count);
            for (int i = 0; i < orders.Count; i++)
            {
                bool expectedTag = i >= plan.BaseOrderCount;
                Assert.AreEqual(expectedTag, orders[i].snsInflow, $"주문 {i} snsInflow 태그");
            }
        }

        [Test]
        public void StartServiceDay_Resets_Sns_Daily_Stats()
        {
            var state = new GameState
            {
                serviceSnsOrdersServedToday = 9,
                serviceSnsOrdersMissedToday = 9,
                serviceSnsRevenueToday = 9999,
            };

            ServiceOps.StartServiceDay(state, new List<ServiceOrderState>(), 4);

            Assert.AreEqual(0, state.serviceSnsOrdersServedToday);
            Assert.AreEqual(0, state.serviceSnsOrdersMissedToday);
            Assert.AreEqual(0, state.serviceSnsRevenueToday);
        }

        [Test]
        public void TryServe_Sns_Tagged_Order_Updates_Sns_Stats_Only_For_Tagged()
        {
            var recipe = Recipe("pork_gukbap");
            var state = new GameState();
            var orders = new List<ServiceOrderState>
            {
                new ServiceOrderState { recipeId = recipe.Id, customerId = "student", partySize = 2, snsInflow = true },
                new ServiceOrderState { recipeId = recipe.Id, customerId = "student", partySize = 1, snsInflow = false },
            };
            ServiceOps.StartServiceDay(state, orders, 1);
            InventoryOps.Add(state, IngredientKind.Pork, IngredientGrade.C, 10);
            InventoryOps.Add(state, IngredientKind.Rice, IngredientGrade.C, 10);
            InventoryOps.Add(state, IngredientKind.Vegetable, IngredientGrade.C, 10);

            var first = ServiceOps.TryServeCurrentOrder(state, recipe, IngredientGrade.C);
            Assert.IsTrue(first.Success, first.Message);
            Assert.AreEqual(1, state.serviceSnsOrdersServedToday);
            Assert.AreEqual(first.RevenueGained, state.serviceSnsRevenueToday);

            var second = ServiceOps.TryServeCurrentOrder(state, recipe, IngredientGrade.C);
            Assert.IsTrue(second.Success, second.Message);
            Assert.AreEqual(1, state.serviceSnsOrdersServedToday, "neutral 주문은 SNS 통계에 영향 없음");
            Assert.AreEqual(first.RevenueGained, state.serviceSnsRevenueToday, "neutral 주문 매출은 SNS 매출에 더해지지 않음");
            Assert.AreEqual(0, state.serviceSnsOrdersMissedToday);
        }

        [Test]
        public void Skip_Sns_Tagged_Order_Updates_Missed_Sns_Stat_Only()
        {
            var recipe = Recipe("tteokbokki");
            var state = new GameState();
            var orders = new List<ServiceOrderState>
            {
                new ServiceOrderState { recipeId = recipe.Id, customerId = "student", partySize = 2, snsInflow = true },
                new ServiceOrderState { recipeId = recipe.Id, customerId = "office_worker", partySize = 1, snsInflow = false },
            };
            ServiceOps.StartServiceDay(state, orders, 1);

            ServiceOps.SkipCurrentOrder(state);
            Assert.AreEqual(1, state.serviceSnsOrdersMissedToday);

            ServiceOps.SkipCurrentOrder(state);
            Assert.AreEqual(1, state.serviceSnsOrdersMissedToday, "neutral 포기는 SNS 통계에 영향 없음");
            Assert.AreEqual(2, state.serviceOrdersMissedToday, "기존 전체 통계는 그대로 갱신");
        }

        [Test]
        public void Neutral_Orders_Existing_Numbers_And_Messages_Are_Unchanged()
        {
            var recipe = Recipe("pork_gukbap");
            var state = StateWithOrder(recipe, partySize: 2);
            InventoryOps.Add(state, IngredientKind.Pork, IngredientGrade.C, 4);
            InventoryOps.Add(state, IngredientKind.Rice, IngredientGrade.C, 2);
            InventoryOps.Add(state, IngredientKind.Vegetable, IngredientGrade.C, 2);
            int expectedPrice = recipe.BasePrice * 2;

            var result = ServiceOps.TryServeCurrentOrder(state, recipe, IngredientGrade.C);

            Assert.IsTrue(result.Success);
            Assert.AreEqual($"{recipe.DisplayName} ×{2} 서빙 완료 (+{expectedPrice:N0}원)", result.Message, "neutral 메시지 불변");
            Assert.AreEqual(0, state.serviceSnsOrdersServedToday, "태그 없는 주문은 SNS 통계 영향 없음");
            Assert.AreEqual(0, state.serviceSnsRevenueToday);
        }
    }
}
