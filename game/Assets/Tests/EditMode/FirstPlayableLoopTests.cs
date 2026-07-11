using System;
using System.Collections.Generic;
using System.Linq;
using ClientIsKing.Data;
using ClientIsKing.DayCycle;
using ClientIsKing.Economy;
using ClientIsKing.Inventory;
using ClientIsKing.Managers;
using ClientIsKing.Service;
using ClientIsKing.Settlement;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-107: 첫 플레이어블 루프 검증 — 구매→서빙→정산→마감→다음 날 흐름과 파산 진행 차단.
    /// GameManager 는 EditMode 에서 생성 가능 (DontDestroyOnLoad 는 Play 모드 가드 — 구현 노트 참조).
    /// task-110: Market→Service 전환에 전문 분야 선택 + 원자적 plan 시작이 필요해 fixture 가 먼저 genre 를 선택한다.
    /// </summary>
    public class FirstPlayableLoopTests
    {
        static T LoadDef<T>(string folder, string id) where T : ScriptableObject
        {
            var def = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder })
                .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault(d =>
                {
                    var ingredient = d as IngredientDef;
                    if (ingredient != null) return ingredient.Id == id;
                    var recipe = d as RecipeDef;
                    return recipe != null && recipe.Id == id;
                });
            Assert.IsNotNull(def, $"시드 {typeof(T).Name} '{id}' 누락");
            return def;
        }

        static List<T> LoadAll<T>(string folder) where T : UnityEngine.Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder })
                .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null)
                .ToList();
        }

        static GameManager CreateGameManager(out GameObject go)
        {
            // task-110: 다른 EditMode fixture 가 연 씬(SceneBuilderTests 등)의 GameManager/ServiceManager
            // 가 살아있으면 static Instance 가 그 쪽을 계속 가리켜 이 fixture 의 신규 인스턴스가 무시된다.
            // AdvancePhase() 가 이제 ServiceManager.Instance 를 조회하므로(design.md G3), 생성 전에
            // 남아있는 singleton 을 명시적으로 정리해 이 테스트가 자기 자신의 인스턴스를 갖게 한다.
            if (GameManager.Instance != null)
            {
                UnityEngine.Object.DestroyImmediate(GameManager.Instance.gameObject);
            }
            if (ServiceManager.Instance != null && ServiceManager.Instance.gameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(ServiceManager.Instance.gameObject);
            }

            go = new GameObject("gm-under-test");
            var gm = go.AddComponent<GameManager>();
            gm.StartNewGame(); // EditMode 에서 Awake 호출 여부와 무관하게 결정론적 초기화
            var genres = LoadAll<GenreDef>("Assets/Data/Definitions/Genres");
            genres.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            gm.EditorInit(genres);
            // task-110: EditMode 에서 loose GameObject 에 AddComponent 로 붙인 컴포넌트는 Awake 가
            // 동기 호출되지 않을 수 있다(씬 로드와 달리 — SceneBuilderTests 는 OpenScene 으로 우회).
            // AdvancePhase() 가 GameManager.Instance/ServiceManager.Instance 를 조회하므로(design.md G3),
            // production API 는 바꾸지 않고 테스트에서만 리플렉션으로 static Instance 를 강제한다.
            ForceSingletonInstance(typeof(GameManager), gm);

            var service = go.AddComponent<ServiceManager>();
            var recipes = LoadAll<RecipeDef>("Assets/Data/Definitions/Recipes");
            var customers = LoadAll<CustomerArchetypeDef>("Assets/Data/Definitions/Customers");
            recipes.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            customers.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            service.EditorInit(recipes, customers);
            ForceSingletonInstance(typeof(ServiceManager), service);

            return gm;
        }

        /// <summary>
        /// private static Instance setter 를 리플렉션으로 강제한다 — EditMode 의 Awake 미호출 대비
        /// (production API 는 바꾸지 않는다 — 테스트 전용 우회).
        /// </summary>
        static void ForceSingletonInstance(System.Type managerType, object instance)
        {
            var prop = managerType.GetProperty("Instance",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (ReferenceEquals(prop.GetValue(null), instance))
            {
                return;
            }
            prop.SetValue(null, instance, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.SetProperty, null, null, null);
        }

        [Test]
        public void NonBankrupt_Full_Day_Loop_Works()
        {
            var gm = CreateGameManager(out var go);
            try
            {
                var state = gm.State;

                // task-110: Day 1 Market 첫 구매 전 전문 분야 선택 — 균형(generalist) 은 전체 레시피를 허용한다.
                var genreIds = gm.GenreCatalog.Select(gd => gd.Id).ToList();
                var selection = ClientIsKing.Genre.GenreSelectionOps.TrySelect(state, "generalist", genreIds);
                Assert.IsTrue(selection.Success, selection.Message);

                // task-110: 오늘 plan 을 미리 순수 조회(state 불변)해 실제로 필요한 재료만 정확히 구매한다
                // (전 재료를 임의 대량 구매하면 시작 자금 30,000원을 넘어 실패한다).
                gm.TryGetGenre(state.selectedGenreId, out var purchaseGenre);
                var service = ServiceManager.Instance;
                Assert.IsTrue(service.TryBuildDayPlan(purchaseGenre, out var previewPlan, out var planReason), planReason);
                var customers = LoadAll<CustomerArchetypeDef>("Assets/Data/Definitions/Customers");
                var recipes = LoadAll<RecipeDef>("Assets/Data/Definitions/Recipes");
                var previewOrders = ServiceOps.BuildOrders(previewPlan, customers);

                var totalNeeded = new Dictionary<IngredientKind, int>();
                foreach (var order in previewOrders)
                {
                    var recipe = recipes.First(r => r.Id == order.recipeId);
                    foreach (var req in ServiceOps.CalculateRequiredIngredients(recipe, order.partySize))
                    {
                        totalNeeded.TryGetValue(req.Kind, out var existing);
                        totalNeeded[req.Kind] = existing + req.Quantity;
                    }
                }

                var ingredientsByKind = LoadAll<IngredientDef>("Assets/Data/Definitions/Ingredients")
                    .Where(i => i.Grade == IngredientGrade.C).ToDictionary(i => i.Kind);
                foreach (var need in totalNeeded)
                {
                    var purchase = EconomyOps.TryPurchaseIngredient(state, ingredientsByKind[need.Key], need.Value, purchaseGenre.CostMultiplier);
                    Assert.IsTrue(purchase.Success, purchase.Message);
                }
                Assert.Greater(state.marketSpendToday, 0, "지출 추적 확인");

                // Market → Service (원자적 plan 생성 + StartServiceDay)
                Assert.IsNotNull(ServiceManager.Instance, "ServiceManager.Instance 는 이 fixture 의 인스턴스여야 한다");
                Assert.IsTrue(gm.CanAdvancePhase(out var advanceReason), advanceReason);
                Assert.AreEqual(DayPhase.Service, gm.AdvancePhase());
                Assert.AreEqual(state.day, state.serviceDay, "Service 진입 시 오늘 주문이 생성되어야 한다");
                Assert.Greater(state.serviceOrders.Count, 0);

                // Service: 오늘 생성된 주문을 전부 서빙 (매출 발생)
                while (service.CurrentOrder != null)
                {
                    var order = service.CurrentOrder;
                    var recipe = recipes.First(r => r.Id == order.recipeId);
                    var serve = service.TryServeCurrentOrder(recipe, IngredientGrade.C);
                    Assert.IsTrue(serve.Success, serve.Message);
                }

                int cashBeforeSettlement = state.cash;

                // Service → Settlement
                Assert.AreEqual(DayPhase.Settlement, gm.AdvancePhase());
                // Settlement → Night (게이트가 정산을 선적용한다)
                Assert.AreEqual(DayPhase.Night, gm.AdvancePhase());

                Assert.IsTrue(SettlementOps.IsSettlementApplied(state), "Settlement 이탈 시 정산 적용");
                Assert.AreEqual(cashBeforeSettlement - SettlementOps.DailyOperatingCost, state.cash);
                Assert.AreEqual(1, state.daysCompleted);
                Assert.IsFalse(state.isBankrupt);

                // Night → Market (day +1)
                Assert.AreEqual(DayPhase.Market, gm.AdvancePhase());
                Assert.AreEqual(2, state.day, "하루 루프 완주 — 다음 날 진입");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Bankruptcy_Blocks_Advance_And_Emits_No_Events()
        {
            var gm = CreateGameManager(out var go);
            int events = 0;
            Action<DayPhaseChangedEventArgs> handler = _ => events++;
            GameEvents.DayPhaseChanged += handler;
            try
            {
                var state = gm.State;
                state.cash = 100; // 운영비(12,000) 미달
                state.currentPhase = DayPhase.Settlement;

                var afterFirst = gm.AdvancePhase(); // 정산 선적용 → 파산 → 진행 차단

                Assert.AreEqual(DayPhase.Settlement, afterFirst, "파산 정산 후 Settlement 에 머문다");
                Assert.IsTrue(state.isBankrupt);
                Assert.AreEqual(0, state.cash);

                var afterSecond = gm.AdvancePhase(); // 파산 상태 — 즉시 차단

                Assert.AreEqual(DayPhase.Settlement, afterSecond);
                Assert.AreEqual(0, events, "차단된 진행은 DayPhaseChanged 를 발행하지 않는다");
            }
            finally
            {
                GameEvents.DayPhaseChanged -= handler;
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
