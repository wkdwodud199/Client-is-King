using System.Collections.Generic;
using System.Linq;
using ClientIsKing.Data;
using NUnit.Framework;
using UnityEditor;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-103: 시드 데이터 무결성 검증 — 하드캡(demo-scope.md)·id 유일성·참조 정합·양수 수치.
    /// 밸런스 수치 자체는 검증하지 않는다 (playtest 조정 대상).
    /// </summary>
    public class DataDefinitionTests
    {
        const string Root = "Assets/Data/Definitions";

        static List<T> LoadAll<T>() where T : UnityEngine.Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { Root })
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(a => a != null)
                .ToList();
        }

        static void AssertUniqueNonEmptyIds(IEnumerable<string> ids, string label)
        {
            var list = ids.ToList();
            foreach (var id in list)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(id), $"{label}: 빈 id 존재");
            }
            Assert.AreEqual(list.Count, list.Distinct().Count(), $"{label}: id 중복");
        }

        [Test]
        public void Ingredients_18_And_Every_Kind_Has_C_And_B()
        {
            var all = LoadAll<IngredientDef>();
            Assert.AreEqual(18, all.Count, "재료는 9종 × C/B = 18개여야 한다");
            AssertUniqueNonEmptyIds(all.Select(i => i.Id), "IngredientDef");

            foreach (var group in all.GroupBy(i => i.Kind))
            {
                var grades = group.Select(i => i.Grade).OrderBy(g => g).ToList();
                CollectionAssert.AreEquivalent(
                    new[] { IngredientGrade.C, IngredientGrade.B }, grades,
                    $"{group.Key}: C/B 등급이 정확히 하나씩 있어야 한다");
            }
            foreach (var i in all)
            {
                Assert.Greater(i.UnitCost, 0, $"{i.Id}: 단가는 양수");
                Assert.That(i.Quality, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f), $"{i.Id}: 품질은 (0,1]");
            }
        }

        [Test]
        public void Recipes_6_With_Concrete_Genre_And_Positive_Values()
        {
            var all = LoadAll<RecipeDef>();
            Assert.AreEqual(6, all.Count, "레시피는 6개(장르별 2개)여야 한다");
            AssertUniqueNonEmptyIds(all.Select(r => r.Id), "RecipeDef");

            foreach (var r in all)
            {
                Assert.IsNotNull(r.Genre, $"{r.Id}: 장르 참조 누락");
                Assert.AreNotEqual(GenreKind.Generalist, r.Genre.Kind,
                    $"{r.Id}: 제네럴리스트는 레시피의 직접 장르로 쓰지 않는다");
                Assert.GreaterOrEqual(r.Ingredients.Count, 1, $"{r.Id}: 재료 요구량 최소 1개");
                foreach (var req in r.Ingredients)
                {
                    Assert.Greater(req.Quantity, 0, $"{r.Id}/{req.Kind}: 수량은 양수");
                }
                Assert.Greater(r.CookSeconds, 0f, $"{r.Id}: 조리 시간은 양수");
                Assert.Greater(r.BasePrice, 0, $"{r.Id}: 판매가는 양수");
            }

            // 장르별 2개씩 (국밥/분식/면류)
            foreach (var group in all.GroupBy(r => r.Genre.Kind))
            {
                Assert.AreEqual(2, group.Count(), $"{group.Key}: 레시피 2개여야 한다");
            }
        }

        [Test]
        public void Every_Required_IngredientKind_Has_Both_Grades()
        {
            var recipes = LoadAll<RecipeDef>();
            var ingredients = LoadAll<IngredientDef>();

            var requiredKinds = recipes.SelectMany(r => r.Ingredients).Select(q => q.Kind).Distinct();
            foreach (var kind in requiredKinds)
            {
                Assert.IsTrue(ingredients.Any(i => i.Kind == kind && i.Grade == IngredientGrade.C),
                    $"{kind}: C 등급 IngredientDef 누락");
                Assert.IsTrue(ingredients.Any(i => i.Kind == kind && i.Grade == IngredientGrade.B),
                    $"{kind}: B 등급 IngredientDef 누락");
            }
        }

        [Test]
        public void Genres_ExactlyOne_Per_Kind_With_Valid_Affinities()
        {
            var all = LoadAll<GenreDef>();
            Assert.AreEqual(4, all.Count, "장르는 3종 + 제네럴리스트 = 4개 (하드캡)");
            AssertUniqueNonEmptyIds(all.Select(g => g.Id), "GenreDef");
            CollectionAssert.AreEquivalent(
                new[] { GenreKind.Gukbap, GenreKind.Bunsik, GenreKind.Noodles, GenreKind.Generalist },
                all.Select(g => g.Kind).ToList(),
                "장르 종류가 정확히 하나씩 있어야 한다");

            foreach (var g in all)
            {
                Assert.Greater(g.CostMultiplier, 0f, $"{g.Id}: 원가 배수 양수");
                Assert.Greater(g.CookTimeMultiplier, 0f, $"{g.Id}: 조리시간 배수 양수");
                Assert.Greater(g.PricePerCustomerMultiplier, 0f, $"{g.Id}: 객단가 배수 양수");
                Assert.GreaterOrEqual(g.CustomerAffinities.Count, 1, $"{g.Id}: 고객 친화도 최소 1개");
                foreach (var a in g.CustomerAffinities)
                {
                    Assert.IsNotNull(a.Archetype, $"{g.Id}: 친화도 archetype 참조 누락");
                    Assert.Greater(a.Multiplier, 0f, $"{g.Id}/{a.Archetype.Id}: 친화도 배수 양수");
                }
            }
        }

        [Test]
        public void Customers_AtLeast4_With_Valid_Ranges()
        {
            var all = LoadAll<CustomerArchetypeDef>();
            Assert.GreaterOrEqual(all.Count, 4, "고객 archetype 은 최소 4개");
            AssertUniqueNonEmptyIds(all.Select(c => c.Id), "CustomerArchetypeDef");

            foreach (var c in all)
            {
                Assert.Greater(c.BaseSpawnWeight, 0f, $"{c.Id}: 출현 가중치 양수");
                Assert.Greater(c.PatienceSeconds, 0f, $"{c.Id}: 인내 시간 양수");
                Assert.GreaterOrEqual(c.PriceSensitivity, 0f, $"{c.Id}: 가격 민감도 0 이상");
                Assert.GreaterOrEqual(c.PartySize.Min, 1, $"{c.Id}: 파티 최소 1 이상");
                Assert.GreaterOrEqual(c.PartySize.Max, c.PartySize.Min, $"{c.Id}: 파티 max ≥ min");
            }
        }

        [Test]
        public void SNS_ExactlyOne_Per_Channel()
        {
            var all = LoadAll<SNSCampaignDef>();
            Assert.AreEqual(3, all.Count, "SNS 채널은 3종 (하드캡)");
            AssertUniqueNonEmptyIds(all.Select(s => s.Id), "SNSCampaignDef");
            CollectionAssert.AreEquivalent(
                new[] { SNSChannel.PhotoFeed, SNSChannel.ShortForm, SNSChannel.LocalBoard },
                all.Select(s => s.Channel).ToList(),
                "SNS 채널이 정확히 하나씩 있어야 한다");

            foreach (var s in all)
            {
                Assert.Greater(s.BaseCost, 0, $"{s.Id}: 비용 양수");
                Assert.That(s.BaseReach, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f), $"{s.Id}: 도달률 (0,1]");
                Assert.That(s.RepeatDecay, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f), $"{s.Id}: 감쇠율 (0,1]");
                Assert.GreaterOrEqual(s.AudienceAffinities.Count, 1, $"{s.Id}: 타겟 친화 최소 1개");
                foreach (var a in s.AudienceAffinities)
                {
                    Assert.Greater(a.Multiplier, 0f, $"{s.Id}: 타겟 배수 양수");
                }
            }
        }

        [Test]
        public void Events_ExactlyOne_Per_Kind()
        {
            var all = LoadAll<GameEventDef>();
            Assert.AreEqual(4, all.Count, "이벤트는 4종 (하드캡)");
            AssertUniqueNonEmptyIds(all.Select(e => e.Id), "GameEventDef");
            CollectionAssert.AreEquivalent(
                new[]
                {
                    GameEventKind.IngredientPriceSurge,
                    GameEventKind.HygieneInspection,
                    GameEventKind.RentIncrease,
                    GameEventKind.GroupCustomers,
                },
                all.Select(e => e.Kind).ToList(),
                "이벤트 종류가 정확히 하나씩 있어야 한다");

            foreach (var e in all)
            {
                Assert.Greater(e.BaseWeight, 0f, $"{e.Id}: 발생 가중치 양수");
                Assert.GreaterOrEqual(e.DurationDays, 0, $"{e.Id}: 기간은 0(영구) 이상");
            }
        }
    }
}
