using System.Collections.Generic;
using System.Linq;
using ClientIsKing.Data;
using ClientIsKing.Economy;
using ClientIsKing.Genre;
using ClientIsKing.Service;
using NUnit.Framework;
using UnityEditor;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-110 U6: design.md D5 밸런스 guard 재유도 — 100-day 결정론 표본, C급 전량 서빙,
    /// 실제 recipe 요구량 기준 "일일 기여이익 = 장르 적용 매출 - 장르 적용 소비 재료 원가".
    /// GenreSelectionOps/EconomyOps/ServiceOps 의 실제 프로덕션 수식만 사용한다(별도 재계산 금지) —
    /// ServiceManager.ToGenreInput/ToRecipeInputs/ToCustomerInputs 로 시드 SO 를 투영해
    /// U1(GenreSelectionOps)이 실제로 소비하는 것과 동일한 입력을 만든다.
    /// </summary>
    public class GenreBalanceTests
    {
        const int SampleDays = 100;
        const float MaxMinRatioGuard = 1.10f;
        const float DesignToleranceRatio = 0.01f;

        // design.md D5 100-day 평균 기여이익(설계 계산) — ±1% guard 기준값.
        static readonly Dictionary<string, double> DesignAverageContribution = new Dictionary<string, double>
        {
            { "gukbap", 49266.0 },
            { "bunsik", 48532.0 },
            { "noodles", 49949.0 },
            { "generalist", 51581.0 },
        };

        static List<T> LoadAll<T>(string folder) where T : UnityEngine.Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder })
                .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null)
                .ToList();
        }

        static List<GenreDef> Genres => LoadAll<GenreDef>("Assets/Data/Definitions/Genres");
        static List<RecipeDef> Recipes => LoadAll<RecipeDef>("Assets/Data/Definitions/Recipes");
        static List<CustomerArchetypeDef> Customers => LoadAll<CustomerArchetypeDef>("Assets/Data/Definitions/Customers");

        static RecipeDef RecipeById(List<RecipeDef> recipes, string id)
        {
            var recipe = recipes.FirstOrDefault(r => r.Id == id);
            Assert.IsNotNull(recipe, $"시드 RecipeDef '{id}' 누락");
            return recipe;
        }

        static IngredientDef IngredientC(List<IngredientDef> ingredients, IngredientKind kind)
        {
            var def = ingredients.FirstOrDefault(i => i.Kind == kind && i.Grade == IngredientGrade.C);
            Assert.IsNotNull(def, $"시드 IngredientDef(C급) '{kind}' 누락");
            return def;
        }

        /// <summary>
        /// 한 장르의 100-day 표본을 프로덕션 Ops 로 재현한다.
        /// dailyNet[d] = 장르 적용 매출 - 장르 적용 소비 재료 원가 (C급, 실제 recipe 요구량, 전량 성공 서빙 가정).
        /// </summary>
        static double[] SimulateDailyNet(GenreDef genreDef, List<RecipeDef> recipes,
            List<CustomerArchetypeDef> customers, List<IngredientDef> ingredients, int days)
        {
            var genreInput = ServiceManager.ToGenreInput(genreDef);
            var recipeInputs = ServiceManager.ToRecipeInputs(recipes);
            var customerInputs = ServiceManager.ToCustomerInputs(customers);

            var dailyNet = new double[days + 1]; // 1-based
            for (int day = 1; day <= days; day++)
            {
                bool ok = GenreSelectionOps.TryBuildDemandPlan(
                    genreInput, day, recipeInputs, customerInputs, out var plan, out var reason);
                Assert.IsTrue(ok, $"{genreDef.Id} day {day} plan 생성 실패: {reason}");

                double revenue = 0;
                double cost = 0;
                for (int orderIndex = 0; orderIndex < plan.OrderCount; orderIndex++)
                {
                    string recipeId = GenreSelectionOps.PickRecipeId(plan, orderIndex);
                    string customerId = GenreSelectionOps.PickCustomerId(plan, orderIndex);
                    var customer = customers.First(c => c.Id == customerId);
                    int partySize = GenreSelectionOps.PickPartySize(day, orderIndex, customer.PartySize.Min, customer.PartySize.Max);
                    var recipe = RecipeById(recipes, recipeId);

                    revenue += ServiceOps.CalculateSalePrice(recipe, partySize, genreDef.PricePerCustomerMultiplier);

                    foreach (var req in ServiceOps.CalculateRequiredIngredients(recipe, partySize))
                    {
                        var ingredientDef = IngredientC(ingredients, req.Kind);
                        cost += EconomyOps.CalculatePurchaseCost(ingredientDef, req.Quantity, genreDef.CostMultiplier);
                    }
                }
                dailyNet[day] = revenue - cost;
            }
            return dailyNet;
        }

        /// <summary>Day 1 전체 주문의 C급 이론 구매비 — 실제 recipe 요구량 합산, 장르 원가 배수 적용.</summary>
        static int CalculateDay1TheoreticalPurchaseCost(GenreDef genreDef, List<RecipeDef> recipes,
            List<CustomerArchetypeDef> customers, List<IngredientDef> ingredients)
        {
            var genreInput = ServiceManager.ToGenreInput(genreDef);
            var recipeInputs = ServiceManager.ToRecipeInputs(recipes);
            var customerInputs = ServiceManager.ToCustomerInputs(customers);

            Assert.IsTrue(GenreSelectionOps.TryBuildDemandPlan(genreInput, 1, recipeInputs, customerInputs, out var plan, out var reason), reason);

            var totalByKind = new Dictionary<IngredientKind, int>();
            for (int orderIndex = 0; orderIndex < plan.OrderCount; orderIndex++)
            {
                string recipeId = GenreSelectionOps.PickRecipeId(plan, orderIndex);
                string customerId = GenreSelectionOps.PickCustomerId(plan, orderIndex);
                var customer = customers.First(c => c.Id == customerId);
                int partySize = GenreSelectionOps.PickPartySize(1, orderIndex, customer.PartySize.Min, customer.PartySize.Max);
                var recipe = RecipeById(recipes, recipeId);

                foreach (var req in ServiceOps.CalculateRequiredIngredients(recipe, partySize))
                {
                    totalByKind.TryGetValue(req.Kind, out var existing);
                    totalByKind[req.Kind] = existing + req.Quantity;
                }
            }

            int total = 0;
            foreach (var kv in totalByKind)
            {
                var ingredientDef = IngredientC(ingredients, kv.Key);
                total += EconomyOps.CalculatePurchaseCost(ingredientDef, kv.Value, genreDef.CostMultiplier);
            }
            return total;
        }

        [Test]
        public void Average_Contribution_Profit_Matches_Design_Within_1_Percent()
        {
            var genres = Genres;
            var recipes = Recipes;
            var customers = Customers;
            var ingredients = LoadAll<IngredientDef>("Assets/Data/Definitions/Ingredients");

            foreach (var genreDef in genres)
            {
                var dailyNet = SimulateDailyNet(genreDef, recipes, customers, ingredients, SampleDays);
                double average = dailyNet.Skip(1).Take(SampleDays).Average();
                double designValue = DesignAverageContribution[genreDef.Id];
                double tolerance = designValue * DesignToleranceRatio;

                Assert.That(average, Is.InRange(designValue - tolerance, designValue + tolerance),
                    $"{genreDef.Id} 평균 기여이익 {average:F2} 이 설계값 {designValue} 의 ±1% 를 벗어남");
            }
        }

        [Test]
        public void Max_Min_Contribution_Ratio_Is_At_Most_1_10()
        {
            var genres = Genres;
            var recipes = Recipes;
            var customers = Customers;
            var ingredients = LoadAll<IngredientDef>("Assets/Data/Definitions/Ingredients");

            var averages = new List<double>();
            foreach (var genreDef in genres)
            {
                var dailyNet = SimulateDailyNet(genreDef, recipes, customers, ingredients, SampleDays);
                averages.Add(dailyNet.Skip(1).Take(SampleDays).Average());
            }

            double max = averages.Max();
            double min = averages.Min();
            Assert.LessOrEqual(max / min, MaxMinRatioGuard,
                $"장르 평균 기여이익 max/min 비율 {max / min:F4} 이 1.10 을 초과함 (max={max:F2}, min={min:F2})");
        }

        [Test]
        public void Day1_Theoretical_Purchase_Cost_Is_Within_Starting_Cash()
        {
            var recipes = Recipes;
            var customers = Customers;
            var ingredients = LoadAll<IngredientDef>("Assets/Data/Definitions/Ingredients");

            foreach (var genreDef in Genres)
            {
                int cost = CalculateDay1TheoreticalPurchaseCost(genreDef, recipes, customers, ingredients);
                Assert.LessOrEqual(cost, ClientIsKing.DayCycle.GameState.StartingCash,
                    $"{genreDef.Id} Day1 이론 구매비 {cost:N0}원이 시작 자금 {ClientIsKing.DayCycle.GameState.StartingCash:N0}원을 초과함");
            }
        }

        [Test]
        public void Day1_To_3_Full_Service_Net_Profit_After_Operating_Cost_Is_Positive_Every_Day()
        {
            const int operatingCost = 12000; // SettlementOps.DailyOperatingCost 와 동일 값 (design.md D5 계약)
            var recipes = Recipes;
            var customers = Customers;
            var ingredients = LoadAll<IngredientDef>("Assets/Data/Definitions/Ingredients");

            foreach (var genreDef in Genres)
            {
                var dailyNet = SimulateDailyNet(genreDef, recipes, customers, ingredients, 3);
                for (int day = 1; day <= 3; day++)
                {
                    double netProfit = dailyNet[day] - operatingCost;
                    Assert.Greater(netProfit, 0,
                        $"{genreDef.Id} day {day} 운영비 차감 후 순이익 {netProfit:F2} 이 0보다 크지 않음");
                }
            }
        }

        [Test]
        public void No_Genre_Dominates_All_Four_Axes()
        {
            // 네 축: 원가(낮을수록 유리) · 1인 가격(높을수록 유리) · 주문 수(많을수록 유리) · recipe 다양성(많을수록 유리).
            // 어느 하나도 네 축 모두에서 동시 1위(지배)가 되어서는 안 된다 (design.md D5 계약).
            var genres = Genres;
            var recipes = Recipes;

            var rows = new List<(string id, float cost, float price, int orderCount, int recipeCount)>();
            foreach (var genreDef in genres)
            {
                var genreInput = ServiceManager.ToGenreInput(genreDef);
                var recipeInputs = ServiceManager.ToRecipeInputs(recipes);
                var customerInputs = ServiceManager.ToCustomerInputs(Customers);
                Assert.IsTrue(GenreSelectionOps.TryBuildDemandPlan(genreInput, 1, recipeInputs, customerInputs, out var plan, out var reason), reason);

                rows.Add((genreDef.Id, genreDef.CostMultiplier, genreDef.PricePerCustomerMultiplier,
                    plan.OrderCount, plan.AllowedRecipeIds.Count));
            }

            // 지배 조건: 최저 원가 AND 최고 가격 AND 최다 주문 AND 최다 recipe 다양성을 모두 동시에 만족하는 장르가 없어야 한다.
            float minCost = rows.Min(r => r.cost);
            float maxPrice = rows.Max(r => r.price);
            int maxOrderCount = rows.Max(r => r.orderCount);
            int maxRecipeCount = rows.Max(r => r.recipeCount);

            foreach (var row in rows)
            {
                bool dominatesAll = row.cost == minCost && row.price == maxPrice
                    && row.orderCount == maxOrderCount && row.recipeCount == maxRecipeCount;
                Assert.IsFalse(dominatesAll, $"{row.id} 이 원가·가격·주문수·recipe다양성 네 축을 모두 지배함");
            }
        }
    }
}
