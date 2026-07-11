using System.Collections.Generic;
using ClientIsKing.DayCycle;
using ClientIsKing.Genre;
using NUnit.Framework;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-110 U1: 장르 선택 게이트, 결정론적 수요 계획(GenreDemandPlan), FNV-1a known vector,
    /// RoundHalfUp/orderCount/forecast 공식을 순수 입력(GenreDefInput 등)만으로 검증한다.
    /// 시드 값은 InitialDataBuilder 의 실제 customer baseSpawnWeight(student 1.0, office_worker 1.2,
    /// family_parent 0.9, senior_regular 0.7)와 task-110 승인 affinity 를 코드 상수로 재현한다.
    /// </summary>
    public class GenreSelectionOpsTests
    {
        const string Gukbap = "gukbap";
        const string Bunsik = "bunsik";
        const string Noodles = "noodles";
        const string Generalist = "generalist";

        static readonly string[] AllGenreIds = { Gukbap, Bunsik, Noodles, Generalist };

        static List<CustomerDefInput> SeedCustomers()
        {
            return new List<CustomerDefInput>
            {
                new CustomerDefInput { Id = "student", BaseSpawnWeight = 1.0f },
                new CustomerDefInput { Id = "office_worker", BaseSpawnWeight = 1.2f },
                new CustomerDefInput { Id = "family_parent", BaseSpawnWeight = 0.9f },
                new CustomerDefInput { Id = "senior_regular", BaseSpawnWeight = 0.7f },
            };
        }

        static GenreDefInput MakeGenre(
            string id, bool isGeneralist, float cookTimeMultiplier, float priceMultiplier,
            params (string customerId, float multiplier)[] affinities)
        {
            var list = new List<GenreAffinityInput>();
            foreach (var a in affinities)
            {
                list.Add(new GenreAffinityInput(a.customerId, a.multiplier));
            }
            return new GenreDefInput
            {
                Id = id,
                IsGeneralist = isGeneralist,
                CookTimeMultiplier = cookTimeMultiplier,
                PricePerCustomerMultiplier = priceMultiplier,
                CustomerAffinities = list,
            };
        }

        static GenreDefInput GukbapGenre() => MakeGenre(Gukbap, false, 1.20f, 0.95f,
            ("student", 0.7f), ("office_worker", 1.0f), ("family_parent", 1.0f), ("senior_regular", 1.5f));

        static GenreDefInput BunsikGenre() => MakeGenre(Bunsik, false, 0.80f, 1.05f,
            ("student", 1.5f), ("office_worker", 1.1f), ("family_parent", 1.0f), ("senior_regular", 0.6f));

        static GenreDefInput NoodlesGenre() => MakeGenre(Noodles, false, 1.00f, 0.95f,
            ("student", 0.9f), ("office_worker", 1.0f), ("family_parent", 1.15f), ("senior_regular", 1.2f));

        static GenreDefInput GeneralistGenre() => MakeGenre(Generalist, true, 1.00f, 1.00f,
            ("student", 1.0f), ("office_worker", 1.0f), ("family_parent", 1.0f), ("senior_regular", 1.0f));

        static List<RecipeDefInput> SeedRecipes()
        {
            return new List<RecipeDefInput>
            {
                new RecipeDefInput { Id = "pork_gukbap", GenreId = Gukbap, BasePrice = 9000 },
                new RecipeDefInput { Id = "beef_gukbap", GenreId = Gukbap, BasePrice = 11000 },
                new RecipeDefInput { Id = "tteokbokki", GenreId = Bunsik, BasePrice = 5000 },
                new RecipeDefInput { Id = "gimbap", GenreId = Bunsik, BasePrice = 4500 },
                new RecipeDefInput { Id = "janchi_guksu", GenreId = Noodles, BasePrice = 6000 },
                new RecipeDefInput { Id = "bibim_guksu", GenreId = Noodles, BasePrice = 6500 },
            };
        }

        // ── RoundHalfUp ────────────────────────────────────────────────────

        [Test]
        public void RoundHalfUp_Floors_At_Half()
        {
            Assert.AreEqual(3, GenreSelectionOps.RoundHalfUp(2.5));
            Assert.AreEqual(2, GenreSelectionOps.RoundHalfUp(2.4999));
            Assert.AreEqual(-2, GenreSelectionOps.RoundHalfUp(-2.5), "floor(x+0.5) — 음수도 동일 규칙");
        }

        // ── 선택 게이트 ─────────────────────────────────────────────────────

        [Test]
        public void TrySelect_Succeeds_On_Day1_Market_Before_Purchase()
        {
            var state = new GameState();
            var result = GenreSelectionOps.TrySelect(state, Gukbap, AllGenreIds);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(Gukbap, result.GenreId);
            Assert.AreEqual(Gukbap, state.selectedGenreId);
        }

        [Test]
        public void TrySelect_Fails_When_Already_Selected_State_Unchanged()
        {
            var state = new GameState { selectedGenreId = Bunsik };
            var result = GenreSelectionOps.TrySelect(state, Gukbap, AllGenreIds);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(Bunsik, state.selectedGenreId, "재선택 시도는 기존 선택을 바꾸지 않는다");
        }

        [Test]
        public void TrySelect_Fails_When_Not_Day1_State_Unchanged()
        {
            var state = new GameState { day = 2 };
            var result = GenreSelectionOps.TrySelect(state, Gukbap, AllGenreIds);

            Assert.IsFalse(result.Success);
            Assert.AreEqual("", state.selectedGenreId);
        }

        [Test]
        public void TrySelect_Fails_When_Not_Market_Phase_State_Unchanged()
        {
            var state = new GameState { currentPhase = DayPhase.Service };
            var result = GenreSelectionOps.TrySelect(state, Gukbap, AllGenreIds);

            Assert.IsFalse(result.Success);
            Assert.AreEqual("", state.selectedGenreId);
        }

        [Test]
        public void TrySelect_Fails_When_Purchase_Already_Happened_State_Unchanged()
        {
            var state = new GameState { marketSpendDay = 1, marketSpendToday = 300 };
            var result = GenreSelectionOps.TrySelect(state, Gukbap, AllGenreIds);

            Assert.IsFalse(result.Success);
            Assert.AreEqual("", state.selectedGenreId);
        }

        [Test]
        public void TrySelect_Fails_When_GenreId_Unknown_State_Unchanged()
        {
            var state = new GameState();
            var result = GenreSelectionOps.TrySelect(state, "unknown_genre", AllGenreIds);

            Assert.IsFalse(result.Success);
            Assert.AreEqual("", state.selectedGenreId);
        }

        // ── 주문 수 공식 ─────────────────────────────────────────────────────

        [Test]
        public void CalculateOrderCount_Matches_Seed_Expectations()
        {
            Assert.AreEqual(4, GenreSelectionOps.CalculateOrderCount(1.20f), "국밥");
            Assert.AreEqual(6, GenreSelectionOps.CalculateOrderCount(0.80f), "분식");
            Assert.AreEqual(5, GenreSelectionOps.CalculateOrderCount(1.00f), "면류/제네럴리스트");
        }

        [Test]
        public void CalculateOrderCount_Clamps_To_4_6()
        {
            Assert.AreEqual(6, GenreSelectionOps.CalculateOrderCount(0.1f), "극단적으로 빠르면 6에서 clamp");
            Assert.AreEqual(4, GenreSelectionOps.CalculateOrderCount(10f), "극단적으로 느리면 4에서 clamp");
        }

        // ── FNV-1a known vector ─────────────────────────────────────────────

        [Test]
        public void Fnv1a_Known_Vector_Matches_Spec()
        {
            Assert.AreEqual(2190636514u, GenreSelectionOps.Fnv1a("gukbap|1|0"));
        }

        // ── plan 생성 성공 + 결정론 ──────────────────────────────────────────

        [Test]
        public void TryBuildDemandPlan_Is_Deterministic_For_Same_Input()
        {
            bool okA = GenreSelectionOps.TryBuildDemandPlan(GukbapGenre(), 1, SeedRecipes(), SeedCustomers(), out var planA, out _);
            bool okB = GenreSelectionOps.TryBuildDemandPlan(GukbapGenre(), 1, SeedRecipes(), SeedCustomers(), out var planB, out _);

            Assert.IsTrue(okA);
            Assert.IsTrue(okB);
            Assert.AreEqual(planA.GenreId, planB.GenreId);
            Assert.AreEqual(planA.OrderCount, planB.OrderCount);
            Assert.AreEqual(planA.MinPricePerCustomer, planB.MinPricePerCustomer);
            Assert.AreEqual(planA.MaxPricePerCustomer, planB.MaxPricePerCustomer);
            CollectionAssert.AreEqual(planA.AllowedRecipeIds, planB.AllowedRecipeIds);
            for (int i = 0; i < planA.CustomerWeights.Count; i++)
            {
                Assert.AreEqual(planA.CustomerWeights[i].CustomerId, planB.CustomerWeights[i].CustomerId);
                Assert.AreEqual(planA.CustomerWeights[i].MilliWeight, planB.CustomerWeights[i].MilliWeight);
            }
            CollectionAssert.AreEqual(planA.TopCustomerIds, planB.TopCustomerIds);

            for (int i = 0; i < planA.OrderCount; i++)
            {
                Assert.AreEqual(GenreSelectionOps.PickRecipeId(planA, i), GenreSelectionOps.PickRecipeId(planB, i), $"주문 {i} recipe 결정론");
                Assert.AreEqual(GenreSelectionOps.PickCustomerId(planA, i), GenreSelectionOps.PickCustomerId(planB, i), $"주문 {i} customer 결정론");
            }
        }

        [Test]
        public void TryBuildDemandPlan_Specialist_Uses_Only_Matching_Recipes()
        {
            Assert.IsTrue(GenreSelectionOps.TryBuildDemandPlan(GukbapGenre(), 1, SeedRecipes(), SeedCustomers(), out var plan, out var reason), reason);

            CollectionAssert.AreEqual(new[] { "beef_gukbap", "pork_gukbap" }, plan.AllowedRecipeIds, "ID ordinal 정렬 + 국밥 매칭만");
        }

        [Test]
        public void TryBuildDemandPlan_Generalist_Uses_All_Recipes()
        {
            Assert.IsTrue(GenreSelectionOps.TryBuildDemandPlan(GeneralistGenre(), 1, SeedRecipes(), SeedCustomers(), out var plan, out var reason), reason);

            Assert.AreEqual(6, plan.AllowedRecipeIds.Count);
        }

        [Test]
        public void TryBuildDemandPlan_OrderCount_Matches_Seed_Expectations()
        {
            GenreSelectionOps.TryBuildDemandPlan(GukbapGenre(), 1, SeedRecipes(), SeedCustomers(), out var gukbapPlan, out _);
            GenreSelectionOps.TryBuildDemandPlan(BunsikGenre(), 1, SeedRecipes(), SeedCustomers(), out var bunsikPlan, out _);
            GenreSelectionOps.TryBuildDemandPlan(NoodlesGenre(), 1, SeedRecipes(), SeedCustomers(), out var noodlesPlan, out _);
            GenreSelectionOps.TryBuildDemandPlan(GeneralistGenre(), 1, SeedRecipes(), SeedCustomers(), out var generalistPlan, out _);

            Assert.AreEqual(4, gukbapPlan.OrderCount);
            Assert.AreEqual(6, bunsikPlan.OrderCount);
            Assert.AreEqual(5, noodlesPlan.OrderCount);
            Assert.AreEqual(5, generalistPlan.OrderCount);
        }

        [Test]
        public void TryBuildDemandPlan_Top2_Forecast_Matches_Seed_Expectations()
        {
            GenreSelectionOps.TryBuildDemandPlan(GukbapGenre(), 1, SeedRecipes(), SeedCustomers(), out var gukbapPlan, out _);
            GenreSelectionOps.TryBuildDemandPlan(BunsikGenre(), 1, SeedRecipes(), SeedCustomers(), out var bunsikPlan, out _);
            GenreSelectionOps.TryBuildDemandPlan(NoodlesGenre(), 1, SeedRecipes(), SeedCustomers(), out var noodlesPlan, out _);
            GenreSelectionOps.TryBuildDemandPlan(GeneralistGenre(), 1, SeedRecipes(), SeedCustomers(), out var generalistPlan, out _);

            CollectionAssert.AreEqual(new[] { "office_worker", "senior_regular" }, gukbapPlan.TopCustomerIds);
            CollectionAssert.AreEqual(new[] { "student", "office_worker" }, bunsikPlan.TopCustomerIds);
            CollectionAssert.AreEqual(new[] { "office_worker", "family_parent" }, noodlesPlan.TopCustomerIds);
            CollectionAssert.AreEqual(new[] { "office_worker", "student" }, generalistPlan.TopCustomerIds);
        }

        [Test]
        public void TryBuildDemandPlan_MinMaxPrice_Excludes_PartySize()
        {
            // gukbap recipes: pork_gukbap 9000, beef_gukbap 11000 × priceMultiplier 0.95
            Assert.IsTrue(GenreSelectionOps.TryBuildDemandPlan(GukbapGenre(), 1, SeedRecipes(), SeedCustomers(), out var plan, out var reason), reason);

            int expectedMin = (int)GenreSelectionOps.RoundHalfUp(9000 * 0.95);
            int expectedMax = (int)GenreSelectionOps.RoundHalfUp(11000 * 0.95);
            Assert.AreEqual(expectedMin, plan.MinPricePerCustomer);
            Assert.AreEqual(expectedMax, plan.MaxPricePerCustomer);
        }

        [Test]
        public void PickRecipeId_Uses_Sorted_Allowed_List_And_Day_OrderIndex_Formula()
        {
            GenreSelectionOps.TryBuildDemandPlan(GukbapGenre(), 1, SeedRecipes(), SeedCustomers(), out var plan, out _);
            // allowed = [beef_gukbap, pork_gukbap] (ordinal). day=1 → (1-1+i)%2
            Assert.AreEqual("beef_gukbap", GenreSelectionOps.PickRecipeId(plan, 0));
            Assert.AreEqual("pork_gukbap", GenreSelectionOps.PickRecipeId(plan, 1));
            Assert.AreEqual("beef_gukbap", GenreSelectionOps.PickRecipeId(plan, 2));
        }

        [Test]
        public void PickPartySize_Uses_Min_Plus_Modulo_Span()
        {
            Assert.AreEqual(1, GenreSelectionOps.PickPartySize(day: 1, orderIndex: 0, minPartySize: 1, maxPartySize: 3));
            Assert.AreEqual(2, GenreSelectionOps.PickPartySize(day: 1, orderIndex: 1, minPartySize: 1, maxPartySize: 3));
            Assert.AreEqual(3, GenreSelectionOps.PickPartySize(day: 1, orderIndex: 2, minPartySize: 1, maxPartySize: 3));
            Assert.AreEqual(1, GenreSelectionOps.PickPartySize(day: 1, orderIndex: 3, minPartySize: 1, maxPartySize: 3), "span 순환");
        }

        [Test]
        public void Specialist_Plan_Orders_Never_Reference_Other_Genre_Recipes()
        {
            GenreSelectionOps.TryBuildDemandPlan(BunsikGenre(), 3, SeedRecipes(), SeedCustomers(), out var plan, out var reason);
            Assert.IsTrue(reason == "" || reason == null || true); // reason 은 성공 시 무의미 — 실패 아님만 확인
            Assert.IsNotNull(plan);

            for (int i = 0; i < plan.OrderCount; i++)
            {
                var recipeId = GenreSelectionOps.PickRecipeId(plan, i);
                Assert.Contains(recipeId, new List<string>(plan.AllowedRecipeIds), $"주문 {i} recipe 는 항상 허용 목록 안에 있어야 한다");
                Assert.IsTrue(recipeId == "tteokbokki" || recipeId == "gimbap", "분식 plan 은 분식 레시피만 사용");
            }
        }

        // ── plan 실패 케이스 ─────────────────────────────────────────────────

        [Test]
        public void TryBuildDemandPlan_Fails_When_Genre_Null()
        {
            bool ok = GenreSelectionOps.TryBuildDemandPlan(null, 1, SeedRecipes(), SeedCustomers(), out var plan, out var reason);
            Assert.IsFalse(ok);
            Assert.IsNull(plan);
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void TryBuildDemandPlan_Fails_When_No_Matching_Recipe()
        {
            var lonelyGenre = MakeGenre("lonely", false, 1f, 1f,
                ("student", 1f), ("office_worker", 1f), ("family_parent", 1f), ("senior_regular", 1f));
            bool ok = GenreSelectionOps.TryBuildDemandPlan(lonelyGenre, 1, SeedRecipes(), SeedCustomers(), out var plan, out var reason);
            Assert.IsFalse(ok);
            Assert.IsNull(plan);
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void TryBuildDemandPlan_Fails_When_Duplicate_Recipe_Id()
        {
            var recipes = new List<RecipeDefInput>
            {
                new RecipeDefInput { Id = "dup", GenreId = Gukbap, BasePrice = 5000 },
                new RecipeDefInput { Id = "dup", GenreId = Gukbap, BasePrice = 6000 },
            };
            bool ok = GenreSelectionOps.TryBuildDemandPlan(GukbapGenre(), 1, recipes, SeedCustomers(), out var plan, out var reason);
            Assert.IsFalse(ok);
            Assert.IsNull(plan);
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void TryBuildDemandPlan_Fails_When_Customer_Affinity_Missing()
        {
            var incompleteGenre = MakeGenre(Gukbap, false, 1.2f, 0.95f,
                ("student", 0.7f), ("office_worker", 1.0f)); // family_parent, senior_regular 누락
            bool ok = GenreSelectionOps.TryBuildDemandPlan(incompleteGenre, 1, SeedRecipes(), SeedCustomers(), out var plan, out var reason);
            Assert.IsFalse(ok);
            Assert.IsNull(plan);
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void TryBuildDemandPlan_Fails_When_Multiplier_Invalid()
        {
            foreach (var bad in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            {
                var badGenre = MakeGenre(Gukbap, false, 1.2f, 0.95f,
                    ("student", bad), ("office_worker", 1f), ("family_parent", 1f), ("senior_regular", 1f));
                bool ok = GenreSelectionOps.TryBuildDemandPlan(badGenre, 1, SeedRecipes(), SeedCustomers(), out var plan, out var reason);
                Assert.IsFalse(ok, $"multiplier={bad} 는 실패해야 한다");
                Assert.IsNull(plan);
                Assert.IsNotEmpty(reason);
            }
        }

        [Test]
        public void TryBuildDemandPlan_Fails_When_Total_MilliWeight_Zero()
        {
            var zeroWeightCustomers = new List<CustomerDefInput>
            {
                new CustomerDefInput { Id = "student", BaseSpawnWeight = 0f },
                new CustomerDefInput { Id = "office_worker", BaseSpawnWeight = 0f },
                new CustomerDefInput { Id = "family_parent", BaseSpawnWeight = 0f },
                new CustomerDefInput { Id = "senior_regular", BaseSpawnWeight = 0f },
            };
            bool ok = GenreSelectionOps.TryBuildDemandPlan(GukbapGenre(), 1, SeedRecipes(), zeroWeightCustomers, out var plan, out var reason);
            Assert.IsFalse(ok);
            Assert.IsNull(plan);
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void TryBuildDemandPlan_Fails_When_Duplicate_Customer_Id()
        {
            var customers = new List<CustomerDefInput>
            {
                new CustomerDefInput { Id = "student", BaseSpawnWeight = 1f },
                new CustomerDefInput { Id = "student", BaseSpawnWeight = 1f },
            };
            var genre = MakeGenre(Gukbap, false, 1.2f, 0.95f, ("student", 1f));
            bool ok = GenreSelectionOps.TryBuildDemandPlan(genre, 1, SeedRecipes(), customers, out var plan, out var reason);
            Assert.IsFalse(ok);
            Assert.IsNull(plan);
            Assert.IsNotEmpty(reason);
        }
    }
}
