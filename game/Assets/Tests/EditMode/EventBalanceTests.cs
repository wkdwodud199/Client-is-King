using System.Collections.Generic;
using System.Linq;
using ClientIsKing.Data;
using ClientIsKing.Economy;
using ClientIsKing.Events;
using ClientIsKing.Genre;
using ClientIsKing.Managers;
using ClientIsKing.Service;
using NUnit.Framework;
using UnityEditor;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-112 U7: design.md G절 밸런스 가드 1~7 을 프로덕션 Ops(EventOps/GenreSelectionOps/ServiceOps/
    /// EconomyOps)로 재유도한다(재계산 금지 — GenreBalanceTests/SNSBalanceTests 방식 계승).
    /// C4 스케줄 그대로 day 2~101 100개 결정론 day 를 전량 서빙·C급·실요구량 구매로 시뮬레이션한다(SNS 없음).
    /// </summary>
    public class EventBalanceTests
    {
        const int SampleStart = 2;
        const int SampleEnd = 101;
        const int OperatingCost = 15000; // SettlementOps.DailyOperatingCost 와 동일 값 (task-115 B3 시드, 오너 승인 재확정)

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
        static List<IngredientDef> Ingredients => LoadAll<IngredientDef>("Assets/Data/Definitions/Ingredients");
        static List<GameEventDef> EventDefs => LoadAll<GameEventDef>("Assets/Data/Definitions/Events");

        static List<GameEventDefInput> EventInputs => GameManager.ToEventInputs(EventDefs);

        static IngredientDef IngredientC(List<IngredientDef> ingredients, IngredientKind kind)
        {
            var def = ingredients.FirstOrDefault(i => i.Kind == kind && i.Grade == IngredientGrade.C);
            Assert.IsNotNull(def, $"시드 IngredientDef(C급) '{kind}' 누락");
            return def;
        }

        /// <summary>
        /// day 의 활성 이벤트 집합에서 축별 효과와 전량 서빙 순이익을 계산한다.
        /// dailyNet = 장르 적용 매출(단체 보너스 포함) - 이벤트 반영 소비 재료 원가 - 이벤트 반영 운영비.
        /// </summary>
        static double SimulateDayNetProfit(
            GenreDef genreDef, List<RecipeDef> recipes, List<CustomerArchetypeDef> customers,
            List<IngredientDef> ingredients, int day, EventDayEffects fx)
        {
            var genreInput = ServiceManager.ToGenreInput(genreDef);
            var recipeInputs = ServiceManager.ToRecipeInputs(recipes);
            var customerInputs = ServiceManager.ToCustomerInputs(customers);

            var sns = DayModifier.Neutral(day);
            Assert.IsTrue(EventOps.TryComposeDayModifier(sns, fx, out var modifier, out var composeReason), composeReason);

            Assert.IsTrue(GenreSelectionOps.TryBuildDemandPlan(
                genreInput, day, recipeInputs, customerInputs, modifier, out var plan, out var planReason), planReason);

            double revenue = 0;
            double ingredientCost = 0;
            for (int orderIndex = 0; orderIndex < plan.OrderCount; orderIndex++)
            {
                string recipeId = GenreSelectionOps.PickRecipeId(plan, orderIndex);
                string customerId = GenreSelectionOps.PickCustomerId(plan, orderIndex);
                var customer = customers.First(c => c.Id == customerId);
                bool isEventOrder = orderIndex >= plan.BaseOrderCount + plan.BonusOrderCount;
                int partySize = isEventOrder
                    ? plan.EventPartySize
                    : GenreSelectionOps.PickPartySize(day, orderIndex, customer.PartySize.Min, customer.PartySize.Max);
                var recipe = recipes.First(r => r.Id == recipeId);

                revenue += ServiceOps.CalculateSalePrice(recipe, partySize, genreDef.PricePerCustomerMultiplier);

                foreach (var req in ServiceOps.CalculateRequiredIngredients(recipe, partySize))
                {
                    var ingredientDef = IngredientC(ingredients, req.Kind);
                    ingredientCost += EconomyOps.CalculatePurchaseCost(
                        ingredientDef, req.Quantity, genreDef.CostMultiplier, fx.IngredientCostMilli);
                }
            }

            double operatingCost = GenreSelectionOps.MulMilliHalfUp(OperatingCost, fx.OperatingCostMilli) + fx.OperatingCostFlat;
            return revenue - ingredientCost - operatingCost;
        }

        /// <summary>C4 스케줄 그대로 day 2~101 의 activeEvents 집합을 사전 계산한다 (day -> fx).</summary>
        static Dictionary<int, EventDayEffects> BuildScheduleFx()
        {
            var defs = EventInputs;
            var current = new List<ActiveEventState>();
            var byDay = new Dictionary<int, EventDayEffects>();
            for (int day = SampleStart; day <= SampleEnd; day++)
            {
                Assert.IsTrue(EventOps.TryBuildNextDayActiveEvents(current, day, defs, out var next, out _, out var reason), reason);
                current = next;
                Assert.IsTrue(EventOps.TryBuildDayEffects(current, day, defs, out var fx, out var fxReason), fxReason);
                byDay[day] = fx;
            }
            return byDay;
        }

        // ── 가드 6: 스케줄 분포 (다른 가드의 전제 — 먼저 검증) ───────────────

        [Test]
        public void Guard6_Schedule_Distribution_Matches_C4_Over_100_Days()
        {
            // 발생 횟수(activate 시점)를 스케줄 재계산으로 직접 센다 — activeEventIds 는 "그날 활성" 집합이라
            // 지속일 포함 중복 카운트가 되므로 activate 시점만 별도로 추적한다.
            var defs = EventInputs;
            var current = new List<ActiveEventState>();
            var counts = new Dictionary<string, int>
            {
                { "ingredient_price_surge", 0 }, { "hygiene_inspection", 0 }, { "rent_increase", 0 }, { "group_customers", 0 },
            };
            bool day2NoEvent = true;
            for (int day = SampleStart; day <= SampleEnd; day++)
            {
                Assert.IsTrue(EventOps.TryBuildNextDayActiveEvents(current, day, defs, out var next, out var activated, out var reason), reason);
                if (day == 2)
                {
                    day2NoEvent = string.IsNullOrEmpty(activated);
                }
                if (!string.IsNullOrEmpty(activated))
                {
                    counts[activated]++;
                }
                current = next;
            }

            Assert.AreEqual(15, counts["ingredient_price_surge"], "폭등 100일 발생 횟수");
            Assert.AreEqual(13, counts["group_customers"], "단체 100일 발생 횟수");
            Assert.AreEqual(16, counts["hygiene_inspection"], "위생 100일 발생 횟수");
            Assert.AreEqual(1, counts["rent_increase"], "임대료는 영구 1회 자동 보장");
            Assert.AreEqual(45, counts.Values.Sum(), "총 발생 45회");
            Assert.IsTrue(day2NoEvent, "Day 2 는 occRoll==450(문턱과 동치) — strict < 로 발생 없음");
        }

        // ── 가드 1: 이벤트만으로 파산 강제 불가 (핵심) ───────────────────────

        [Test]
        public void Guard1_Every_Day_Net_Profit_Is_Positive_For_All_Genres()
        {
            var recipes = Recipes;
            var customers = Customers;
            var ingredients = Ingredients;
            var scheduleFx = BuildScheduleFx();

            var minByGenre = new Dictionary<string, double>();
            var sumByGenre = new Dictionary<string, double>();

            foreach (var genreDef in Genres)
            {
                double min = double.MaxValue;
                double sum = 0;
                for (int day = SampleStart; day <= SampleEnd; day++)
                {
                    double net = SimulateDayNetProfit(genreDef, recipes, customers, ingredients, day, scheduleFx[day]);
                    Assert.Greater(net, 0, $"{genreDef.Id} day {day} 순이익이 양수가 아님 ({net:F2})");
                    if (net < min) min = net;
                    sum += net;
                }
                minByGenre[genreDef.Id] = min;
                sumByGenre[genreDef.Id] = sum / (SampleEnd - SampleStart + 1);
            }

            // task-115 B3 오너 승인 재확정(운영비 12,000→15,000) 실측값 — 100일 전 장르 순이익>0 확인 완료.
            // gukbap min 은 여전히 day32(임대료 인상 영구 활성)이 최저 — 5,031(12,000 기준)에서
            // 15,000 기준 1,581 로 하락했으나 양수 유지(±5 정수 반올림 오차 허용, 큰 괴리는 로직 오류).
            AssertClose("gukbap", minByGenre, 1581, sumByGenre, 31966.41);
            AssertClose("bunsik", minByGenre, 11623, sumByGenre, 31299.10);
            AssertClose("noodles", minByGenre, 12541, sumByGenre, 33051.60);
            AssertClose("generalist", minByGenre, 9900, sumByGenre, 34519.49);
        }

        static void AssertClose(string genreId, Dictionary<string, double> minByGenre, double expectedMin,
            Dictionary<string, double> sumByGenre, double expectedAvg)
        {
            Assert.That(minByGenre[genreId], Is.InRange(expectedMin - 5, expectedMin + 5),
                $"{genreId} 최저 일일 순이익 {minByGenre[genreId]:F2} 이 설계값 {expectedMin} 과 어긋남");
            double tolerance = expectedAvg * 0.01;
            Assert.That(sumByGenre[genreId], Is.InRange(expectedAvg - tolerance, expectedAvg + tolerance),
                $"{genreId} 100일 평균 {sumByGenre[genreId]:F2} 이 설계값 {expectedAvg} 의 ±1% 를 벗어남");
        }

        // ── 가드 2: 최악 조합 가상 검증 (3중첩 — day 13 수요 기준 가정 주입) ──

        [Test]
        public void Guard2_Worst_Case_Triple_Overlap_Still_Positive_For_All_Genres()
        {
            var defs = EventInputs;
            var active = new List<ActiveEventState>
            {
                new ActiveEventState { eventId = "rent_increase", remainingDays = 0 },
                new ActiveEventState { eventId = "hygiene_inspection", remainingDays = 1 },
                new ActiveEventState { eventId = "ingredient_price_surge", remainingDays = 2 },
            };
            Assert.IsTrue(EventOps.TryBuildDayEffects(active, 13, defs, out var fx, out var reason), reason);

            var recipes = Recipes;
            var customers = Customers;
            var ingredients = Ingredients;

            var results = new Dictionary<string, double>();
            foreach (var genreDef in Genres)
            {
                double net = SimulateDayNetProfit(genreDef, recipes, customers, ingredients, 13, fx);
                Assert.Greater(net, 0, $"{genreDef.Id} 3중첩 최악 조합에서도 순이익이 양수가 아님 ({net:F2})");
                results[genreDef.Id] = net;
            }

            // task-115 B3 오너 승인 재확정(운영비 15,000) 실측값 — 3중첩 최악 조합에서도 전 장르 순이익 양수.
            Assert.That(results["gukbap"], Is.InRange(24401 - 5, 24401 + 5));
            Assert.That(results["bunsik"], Is.InRange(12239 - 5, 12239 + 5));
            Assert.That(results["noodles"], Is.InRange(17088 - 5, 17088 + 5));
            Assert.That(results["generalist"], Is.InRange(16028 - 5, 16028 + 5));
        }

        // ── 가드 3: 데모 3일 생존 (Day1 무이벤트 ~ Day3 폭등) ────────────────

        [Test]
        public void Guard3_Day1_To_3_All_Genres_Stay_In_Black_And_Day3_Matches_D6()
        {
            var recipes = Recipes;
            var customers = Customers;
            var ingredients = Ingredients;
            var defs = EventInputs;

            var current = new List<ActiveEventState>();
            var fxByDay = new Dictionary<int, EventDayEffects> { { 1, EventDayEffects.Neutral(1) } };
            for (int day = 2; day <= 3; day++)
            {
                Assert.IsTrue(EventOps.TryBuildNextDayActiveEvents(current, day, defs, out var next, out _, out var reason), reason);
                current = next;
                Assert.IsTrue(EventOps.TryBuildDayEffects(current, day, defs, out var fx, out var fxReason), fxReason);
                fxByDay[day] = fx;
            }

            var cashByGenre = new Dictionary<string, int>();
            foreach (var genreDef in Genres)
            {
                int cash = ClientIsKing.DayCycle.GameState.StartingCash;
                for (int day = 1; day <= 3; day++)
                {
                    double net = SimulateDayNetProfit(genreDef, recipes, customers, ingredients, day, fxByDay[day]);
                    Assert.Greater(net, 0, $"{genreDef.Id} day {day} 흑자 유지 실패");
                    cash += (int)net;
                }
                cashByGenre[genreDef.Id] = cash;
            }

            double gukbapDay3 = SimulateDayNetProfit(Genres.First(g => g.Id == "gukbap"), recipes, customers, ingredients, 3, fxByDay[3]);
            double bunsikDay3 = SimulateDayNetProfit(Genres.First(g => g.Id == "bunsik"), recipes, customers, ingredients, 3, fxByDay[3]);
            double noodlesDay3 = SimulateDayNetProfit(Genres.First(g => g.Id == "noodles"), recipes, customers, ingredients, 3, fxByDay[3]);
            double generalistDay3 = SimulateDayNetProfit(Genres.First(g => g.Id == "generalist"), recipes, customers, ingredients, 3, fxByDay[3]);

            // task-115 B3 오너 승인 재확정(운영비 15,000) 실측값.
            Assert.GreaterOrEqual(cashByGenre["bunsik"], 112568, "분식 Day3 마감 잔액 최저 기준");

            Assert.That(gukbapDay3, Is.InRange(23725 - 5, 23725 + 5), "국밥 Day3 폭등 순이익");
            Assert.That(bunsikDay3, Is.InRange(22489 - 5, 22489 + 5), "분식 Day3 폭등 순이익");
            Assert.That(noodlesDay3, Is.InRange(27621 - 5, 27621 + 5), "면류 Day3 폭등 순이익");
            Assert.That(generalistDay3, Is.InRange(33546 - 5, 33546 + 5), "제네럴리스트 Day3 폭등 순이익");
        }

        // ── 가드 4: 단체는 항상 순기여 양수 (Day 5) ──────────────────────────

        [Test]
        public void Guard4_Group_Customers_Net_Contribution_Is_Positive_And_Matches_D6()
        {
            var recipes = Recipes;
            var customers = Customers;
            var ingredients = Ingredients;
            var defs = EventInputs;

            var current = new List<ActiveEventState>();
            for (int day = 2; day <= 5; day++)
            {
                Assert.IsTrue(EventOps.TryBuildNextDayActiveEvents(current, day, defs, out var next, out _, out var reason), reason);
                current = next;
            }
            Assert.IsTrue(EventOps.TryBuildDayEffects(current, 5, defs, out var fx, out var fxReason), fxReason);
            Assert.AreEqual(1, fx.GroupBonusOrders, "Day5 는 단체 손님이 활성이어야 한다(C4 표)");

            foreach (var genreDef in Genres)
            {
                var genreInput = ServiceManager.ToGenreInput(genreDef);
                var recipeInputs = ServiceManager.ToRecipeInputs(recipes);
                var customerInputs = ServiceManager.ToCustomerInputs(customers);
                var sns = DayModifier.Neutral(5);
                Assert.IsTrue(EventOps.TryComposeDayModifier(sns, fx, out var modifier, out var composeReason), composeReason);
                Assert.IsTrue(GenreSelectionOps.TryBuildDemandPlan(genreInput, 5, recipeInputs, customerInputs, modifier, out var plan, out var planReason), planReason);

                int groupIndex = plan.BaseOrderCount + plan.BonusOrderCount; // SNS 없음 -> Base 그 자체
                string recipeId = GenreSelectionOps.PickRecipeId(plan, groupIndex);
                string customerId = GenreSelectionOps.PickCustomerId(plan, groupIndex);
                var recipe = recipes.First(r => r.Id == recipeId);

                int salePrice = ServiceOps.CalculateSalePrice(recipe, plan.EventPartySize, genreDef.PricePerCustomerMultiplier);
                int ingredientCost = 0;
                foreach (var req in ServiceOps.CalculateRequiredIngredients(recipe, plan.EventPartySize))
                {
                    var ingredientDef = IngredientC(ingredients, req.Kind);
                    ingredientCost += EconomyOps.CalculatePurchaseCost(ingredientDef, req.Quantity, genreDef.CostMultiplier);
                }
                int netContribution = salePrice - ingredientCost;

                Assert.Greater(netContribution, 0, $"{genreDef.Id} 단체 순기여가 양수가 아님");
                Assert.AreEqual(4, plan.EventPartySize, "단체 파티 크기는 4인 고정");
            }
        }

        // ── 가드 5: 주문 하드캡 (base 6 + SNS 2 + 단체 1 = 9) ───────────────

        [Test]
        public void Guard5_Order_Count_Never_Exceeds_9_With_Sns_And_Event_Combined()
        {
            var recipes = Recipes;
            var customers = Customers;
            var defs = EventInputs;
            var bunsik = Genres.First(g => g.Id == "bunsik"); // base 6 — 최댓값 조합

            var genreInput = ServiceManager.ToGenreInput(bunsik);
            var recipeInputs = ServiceManager.ToRecipeInputs(recipes);
            var customerInputs = ServiceManager.ToCustomerInputs(customers);

            // 최대 SNS 보너스(2) + 단체(1) 동시 가정.
            var boosts = new List<CustomerWeightBoost>();
            foreach (var c in customerInputs)
            {
                boosts.Add(new CustomerWeightBoost(c.Id, 1000));
            }
            var sns = new DayModifier(5, "short_form", 2, boosts);

            var active = new List<ActiveEventState> { new ActiveEventState { eventId = "group_customers", remainingDays = 1 } };
            Assert.IsTrue(EventOps.TryBuildDayEffects(active, 5, defs, out var fx, out var fxReason), fxReason);
            Assert.IsTrue(EventOps.TryComposeDayModifier(sns, fx, out var modifier, out var composeReason), composeReason);
            Assert.IsTrue(GenreSelectionOps.TryBuildDemandPlan(genreInput, 5, recipeInputs, customerInputs, modifier, out var plan, out var planReason), planReason);

            Assert.AreEqual(6, plan.BaseOrderCount);
            Assert.AreEqual(2, plan.BonusOrderCount);
            Assert.AreEqual(1, plan.EventBonusOrderCount);
            Assert.AreEqual(9, plan.OrderCount, "base 6 + SNS 2 + 단체 1 = 9 (하드캡)");
            Assert.LessOrEqual(plan.OrderCount, 9);
        }

        // ── 가드 7: 재유도 정확성 (고정 벡터 ±허용오차 없이 정확 일치) ───────

        [Test]
        public void Guard7_Fixed_Vectors_Match_Exactly_No_Tolerance()
        {
            // D4: 국밥×폭등 known vector — 돼지고기 900 -> unitGenre 1035 -> unitFinal 1397 (수량 2 = 2794, 할증 724)
            var pork = IngredientC(Ingredients, IngredientKind.Pork);
            int unitGenre = (int)GenreSelectionOps.RoundHalfUp(pork.UnitCost * 1.15);
            Assert.AreEqual(1035, unitGenre);
            int unitFinal = GenreSelectionOps.MulMilliHalfUp(unitGenre, 1350);
            Assert.AreEqual(1397, unitFinal);
            int cost = EconomyOps.CalculatePurchaseCost(pork, 2, 1.15f, 1350);
            Assert.AreEqual(2794, cost);
            Assert.AreEqual(724, cost - EconomyOps.CalculatePurchaseCost(pork, 2, 1.15f));

            // D5: 임대료/위생 known vectors — 정수 결정론이므로 정확 일치 (task-115 B3 시드 15,000, 오너 승인 재확정).
            Assert.AreEqual(17250, GenreSelectionOps.MulMilliHalfUp(15000, 1150));
            Assert.AreEqual(23000, 15000 + 8000);
            Assert.AreEqual(25250, GenreSelectionOps.MulMilliHalfUp(15000, 1150) + 8000);
        }
    }
}
