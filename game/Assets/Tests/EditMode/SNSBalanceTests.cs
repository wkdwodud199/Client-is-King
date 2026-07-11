using System.Collections.Generic;
using System.Linq;
using ClientIsKing.Data;
using ClientIsKing.Economy;
using ClientIsKing.Genre;
using ClientIsKing.Service;
using ClientIsKing.Social;
using NUnit.Framework;
using UnityEditor;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-111 G절 밸런스 guard 재유도 — task-110 D5 방법론 계승(GenreBalanceTests.cs).
    /// day 2~101 의 100개 결정론 day 에 대해 "전날 밤 해당 채널을 집행했다"고 가정하고,
    /// 보너스 주문만의 기여이익(장르 적용 매출 - 장르 적용 C급 실요구 재료 원가, 전량 서빙 가정)
    /// 합계에서 채널 비용을 뺀 값을 평균한다. 1회차는 priorUses=0(reachMilli(0)), 2회차는 priorUses=1(reachMilli(1)).
    /// SNSCampaignOps/GenreSelectionOps 의 실제 프로덕션 수식만 사용한다(별도 재계산 금지) — U1(SNSCampaignOps)이
    /// 실제로 만드는 DayModifier 를 그대로 modifier overload 에 먹인다.
    /// </summary>
    public class SNSBalanceTests
    {
        const int SampleDays = 100;
        const float DesignToleranceRatio = 0.01f;
        const int OperatingCost = 12000;

        static readonly string[] GenreIds = { "gukbap", "bunsik", "noodles", "generalist" };
        static readonly string[] ChannelIds = { "photo_feed", "short_form", "local_board" };

        // design.md G절 표 — 장르 × 채널 1회차/2회차 평균 net (원).
        static readonly Dictionary<(string genre, string channel), (double first, double second)> DesignNet =
            new Dictionary<(string, string), (double, double)>
        {
            { ("gukbap", "photo_feed"), (9552, -2896) },
            { ("gukbap", "short_form"), (12658, 851) },
            { ("gukbap", "local_board"), (5530, 5530) },
            { ("bunsik", "photo_feed"), (1341, -6686) },
            { ("bunsik", "short_form"), (3644, -4055) },
            { ("bunsik", "local_board"), (1068, 1068) },
            { ("noodles", "photo_feed"), (5264, -4973) },
            { ("noodles", "short_form"), (7297, -2688) },
            { ("noodles", "local_board"), (3590, 3590) },
            { ("generalist", "photo_feed"), (5179, -5041) },
            { ("generalist", "short_form"), (8316, -1879) },
            { ("generalist", "local_board"), (3684, 3684) },
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
        static List<SNSCampaignDef> Channels => LoadAll<SNSCampaignDef>("Assets/Data/Definitions/SNS");
        static List<IngredientDef> Ingredients => LoadAll<IngredientDef>("Assets/Data/Definitions/Ingredients");

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
        /// "전날 밤 channel 을 priorUses 회차째 집행했다"고 가정한 DayModifier 를 U1 프로덕션 수식(SNSCampaignOps)
        /// 그대로 재구성한다 — 밸런스 테스트가 별도 수식을 만들지 않고 실제 감쇠/매칭 경로를 검증한다.
        /// </summary>
        static DayModifier BuildAssumedModifier(SNSCampaignDef channelDef, int priorUses, int day, List<CustomerArchetypeDef> customers)
        {
            var channelInput = ServiceManager.ToSnsCampaignInput(channelDef);
            var customerInputs = ServiceManager.ToCustomerInputs(customers);

            int reachMilli0 = SNSCampaignOps.ProjectMilli(channelInput.BaseReach);
            int decayMilli = SNSCampaignOps.ProjectMilli(channelInput.RepeatDecay);
            int effectiveReach = SNSCampaignOps.CalculateEffectiveMilliReach(reachMilli0, decayMilli, priorUses);
            int bonusOrders = SNSCampaignOps.CalculateBonusOrderCount(effectiveReach);

            // history 에 가상 레코드를 심어 TryBuildDayModifier(D4) 를 그대로 재사용한다 —
            // executedOnDay=day-1, bonusOrderCount 는 방금 계산한 실제 값(약속 고정 규약, B3).
            var history = new List<SNSCampaignRecord>
            {
                new SNSCampaignRecord
                {
                    campaignId = channelDef.Id,
                    executedOnDay = day - 1,
                    bonusOrderCount = bonusOrders,
                    effectiveMilliReach = effectiveReach,
                },
            };
            bool ok = SNSCampaignOps.TryBuildDayModifier(
                history, day, new List<SNSCampaignDefInput> { channelInput }, customerInputs, out var modifier, out var reason);
            Assert.IsTrue(ok, $"{channelDef.Id} priorUses={priorUses} day={day} DayModifier 재구성 실패: {reason}");
            return modifier;
        }

        /// <summary>
        /// day 의 보너스 주문만의 net(기여이익 - 채널 비용)을 계산한다.
        /// 기여이익 = Σ(장르 적용 매출 - 장르 적용 C급 실요구 재료 원가), 인덱스 BaseOrderCount..OrderCount-1 만 포함.
        /// </summary>
        static double CalculateBonusOnlyNet(
            GenreDef genreDef, SNSCampaignDef channelDef, int priorUses, int day,
            List<RecipeDef> recipes, List<CustomerArchetypeDef> customers, List<IngredientDef> ingredients)
        {
            var genreInput = ServiceManager.ToGenreInput(genreDef);
            var recipeInputs = ServiceManager.ToRecipeInputs(recipes);
            var customerInputs = ServiceManager.ToCustomerInputs(customers);
            var modifier = BuildAssumedModifier(channelDef, priorUses, day, customers);

            bool ok = GenreSelectionOps.TryBuildDemandPlan(
                genreInput, day, recipeInputs, customerInputs, modifier, out var plan, out var reason);
            Assert.IsTrue(ok, $"{genreDef.Id}×{channelDef.Id} day {day} plan 생성 실패: {reason}");
            Assert.LessOrEqual(plan.OrderCount, 8, "하루 총 주문 하드캡 8건 (분식 6 + 보너스 2)");

            double revenue = 0;
            double cost = 0;
            for (int orderIndex = plan.BaseOrderCount; orderIndex < plan.OrderCount; orderIndex++)
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
            return revenue - cost - channelDef.BaseCost;
        }

        /// <summary>day 2~101(=100개) 의 보너스 net 평균 — priorUses 는 회차 전체에 고정(가정된 반복 집행 시나리오).</summary>
        static double AverageBonusNet(GenreDef genreDef, SNSCampaignDef channelDef, int priorUses,
            List<RecipeDef> recipes, List<CustomerArchetypeDef> customers, List<IngredientDef> ingredients)
        {
            double total = 0;
            for (int day = 2; day <= SampleDays + 1; day++)
            {
                total += CalculateBonusOnlyNet(genreDef, channelDef, priorUses, day, recipes, customers, ingredients);
            }
            return total / SampleDays;
        }

        [Test]
        public void Average_Bonus_Net_Matches_Design_Table_Within_1_Percent_First_Use()
        {
            var genres = Genres;
            var channels = Channels;
            var recipes = Recipes;
            var customers = Customers;
            var ingredients = Ingredients;

            foreach (var genreDef in genres)
            {
                foreach (var channelDef in channels)
                {
                    double average = AverageBonusNet(genreDef, channelDef, 0, recipes, customers, ingredients);
                    double designValue = DesignNet[(genreDef.Id, channelDef.Id)].first;
                    double tolerance = System.Math.Abs(designValue) * DesignToleranceRatio;

                    Assert.That(average, Is.InRange(designValue - tolerance, designValue + tolerance),
                        $"{genreDef.Id}×{channelDef.Id} 1회차 평균 net {average:F2} 이 설계값 {designValue} 의 ±1% 를 벗어남");
                }
            }
        }

        [Test]
        public void Average_Bonus_Net_Matches_Design_Table_Within_1_Percent_Second_Use()
        {
            var genres = Genres;
            var channels = Channels;
            var recipes = Recipes;
            var customers = Customers;
            var ingredients = Ingredients;

            foreach (var genreDef in genres)
            {
                foreach (var channelDef in channels)
                {
                    double average = AverageBonusNet(genreDef, channelDef, 1, recipes, customers, ingredients);
                    double designValue = DesignNet[(genreDef.Id, channelDef.Id)].second;
                    double tolerance = System.Math.Abs(designValue) * DesignToleranceRatio;

                    Assert.That(average, Is.InRange(designValue - tolerance, designValue + tolerance),
                        $"{genreDef.Id}×{channelDef.Id} 2회차 평균 net {average:F2} 이 설계값 {designValue} 의 ±1% 를 벗어남");
                }
            }
        }

        [Test]
        public void First_Use_All_Combos_Are_Positive_And_Within_Approved_Band()
        {
            // 가드1: 12조합의 1회차 평균 net 이 전부 +500 ~ +14,000 안.
            var genres = Genres;
            var channels = Channels;
            var recipes = Recipes;
            var customers = Customers;
            var ingredients = Ingredients;

            foreach (var genreDef in genres)
            {
                foreach (var channelDef in channels)
                {
                    double average = AverageBonusNet(genreDef, channelDef, 0, recipes, customers, ingredients);
                    Assert.That(average, Is.InRange(500.0, 14000.0),
                        $"{genreDef.Id}×{channelDef.Id} 1회차 평균 net {average:F2} 이 승인 범위(+500~+14,000)를 벗어남");
                }
            }
        }

        [Test]
        public void PhotoFeed_And_ShortForm_Second_Use_Is_Lower_Than_First_Use_For_Every_Genre()
        {
            // 가드2: 픽쳐그램·숏핑의 2회차 평균 net 은 모든 장르에서 1회차보다 낮다 (수확체감 가시성).
            var genres = Genres;
            var recipes = Recipes;
            var customers = Customers;
            var ingredients = Ingredients;
            var photoFeed = Channels.First(c => c.Id == "photo_feed");
            var shortForm = Channels.First(c => c.Id == "short_form");

            foreach (var genreDef in genres)
            {
                double photoFirst = AverageBonusNet(genreDef, photoFeed, 0, recipes, customers, ingredients);
                double photoSecond = AverageBonusNet(genreDef, photoFeed, 1, recipes, customers, ingredients);
                Assert.Less(photoSecond, photoFirst, $"{genreDef.Id} 픽쳐그램 2회차({photoSecond:F2})가 1회차({photoFirst:F2})보다 낮아야 한다");

                double shortFirst = AverageBonusNet(genreDef, shortForm, 0, recipes, customers, ingredients);
                double shortSecond = AverageBonusNet(genreDef, shortForm, 1, recipes, customers, ingredients);
                Assert.Less(shortSecond, shortFirst, $"{genreDef.Id} 숏핑 2회차({shortSecond:F2})가 1회차({shortFirst:F2})보다 낮아야 한다");
            }
        }

        [Test]
        public void PhotoFeed_Second_Use_Turns_Negative_For_Every_Genre()
        {
            // 가드2 세부: 픽쳐그램 2회차는 전 장르 손실 전환(로테이션 학습 유도).
            var genres = Genres;
            var recipes = Recipes;
            var customers = Customers;
            var ingredients = Ingredients;
            var photoFeed = Channels.First(c => c.Id == "photo_feed");

            foreach (var genreDef in genres)
            {
                double photoSecond = AverageBonusNet(genreDef, photoFeed, 1, recipes, customers, ingredients);
                Assert.Less(photoSecond, 0.0, $"{genreDef.Id} 픽쳐그램 2회차 평균 net {photoSecond:F2} 이 음수로 전환되지 않음");
            }
        }

        [Test]
        public void LocalBoard_Second_Use_Equals_First_Use_Flat_Decay_Identity()
        {
            // 가드3: 동네게시판은 2회차에도 보너스 1팀이 유지되어 평균 net 이 1회차와 같다 (완만 감쇠 정체성).
            var genres = Genres;
            var recipes = Recipes;
            var customers = Customers;
            var ingredients = Ingredients;
            var localBoard = Channels.First(c => c.Id == "local_board");

            foreach (var genreDef in genres)
            {
                double first = AverageBonusNet(genreDef, localBoard, 0, recipes, customers, ingredients);
                double second = AverageBonusNet(genreDef, localBoard, 1, recipes, customers, ingredients);
                double tolerance = System.Math.Abs(first) * DesignToleranceRatio;
                Assert.That(second, Is.InRange(first - tolerance, first + tolerance),
                    $"{genreDef.Id} 동네게시판 1회차({first:F2})와 2회차({second:F2})가 ±1% 안에서 같아야 한다(완만 감쇠 정체성)");
            }
        }

        [Test]
        public void LocalBoard_Decay_Shows_Only_In_Follower_Gain_Not_Bonus_Order_Count()
        {
            // 가드3 세부: 감쇠는 팔로워 획득(15→14)과 장기 체인에서만 드러나고, 1~2회차 보너스 주문 수는 동일(1팀) 유지.
            var localBoard = Channels.First(c => c.Id == "local_board");
            var channelInput = ServiceManager.ToSnsCampaignInput(localBoard);

            int reachMilli0 = SNSCampaignOps.ProjectMilli(channelInput.BaseReach);
            int decayMilli = SNSCampaignOps.ProjectMilli(channelInput.RepeatDecay);
            int reach0 = SNSCampaignOps.CalculateEffectiveMilliReach(reachMilli0, decayMilli, 0);
            int reach1 = SNSCampaignOps.CalculateEffectiveMilliReach(reachMilli0, decayMilli, 1);

            Assert.AreEqual(1, SNSCampaignOps.CalculateBonusOrderCount(reach0), "1회차 보너스 1팀");
            Assert.AreEqual(1, SNSCampaignOps.CalculateBonusOrderCount(reach1), "2회차도 보너스 1팀 유지");
            Assert.AreEqual(15, SNSCampaignOps.CalculateFollowerGain(reach0), "1회차 팔로워 +15");
            Assert.AreEqual(14, SNSCampaignOps.CalculateFollowerGain(reach1), "2회차 팔로워 +14 로 감쇠가 드러남");
        }

        [Test]
        public void Daily_Order_Count_Never_Exceeds_Hard_Cap_Of_8()
        {
            // 가드5: 어떤 조합에서도 하루 총 주문이 8건을 넘지 않는다 (분식 6 + 보너스 2 가 최댓값).
            var genres = Genres;
            var channels = Channels;
            var recipes = Recipes;
            var customers = Customers;

            foreach (var genreDef in genres)
            {
                var genreInput = ServiceManager.ToGenreInput(genreDef);
                var recipeInputs = ServiceManager.ToRecipeInputs(recipes);
                var customerInputs = ServiceManager.ToCustomerInputs(customers);

                foreach (var channelDef in channels)
                {
                    var modifier = BuildAssumedModifier(channelDef, 0, 2, customers);
                    bool ok = GenreSelectionOps.TryBuildDemandPlan(
                        genreInput, 2, recipeInputs, customerInputs, modifier, out var plan, out var reason);
                    Assert.IsTrue(ok, reason);
                    Assert.LessOrEqual(plan.OrderCount, 8, $"{genreDef.Id}×{channelDef.Id} 총 주문 {plan.OrderCount}건이 하드캡 8건을 초과함");
                }
            }
        }

        [Test]
        public void Execution_Gate_Requires_Only_Cash_Sufficiency_No_Minimum_Balance_Floor()
        {
            // 가드6: 집행 게이트는 cash >= cost 만 요구하고 잔액 하한을 강제하지 않는다(위험 감수는 플레이어 몫).
            var localBoard = Channels.First(c => c.Id == "local_board");
            var channelInput = ServiceManager.ToSnsCampaignInput(localBoard);

            var state = new ClientIsKing.DayCycle.GameState
            {
                day = 1,
                currentPhase = ClientIsKing.DayCycle.DayPhase.Night,
                cash = channelInput.BaseCost, // 정확히 비용만큼만 있어도 집행 가능해야 한다 (하한 없음)
            };
            var result = SNSCampaignOps.TryExecute(state, channelInput);
            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(0, state.cash, "잔액이 정확히 0이 되어도 집행 자체는 성공해야 한다(하한 미강제)");
        }
    }
}
