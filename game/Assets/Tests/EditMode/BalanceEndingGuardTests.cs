using System.Collections.Generic;
using System.Linq;
using ClientIsKing.Data;
using ClientIsKing.DayCycle;
using ClientIsKing.Economy;
using ClientIsKing.Events;
using ClientIsKing.Genre;
using ClientIsKing.Inventory;
using ClientIsKing.Managers;
using ClientIsKing.Save;
using ClientIsKing.Service;
using ClientIsKing.Settlement;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-115 B3 밴드 확정 조건의 기계화 — FirstPlayableLoopTests.NonBankrupt_Full_Day_Loop_Works 전례를
    /// N일로 일반화한 실루프(전량 서빙·C급·SNS 무집행·이벤트 FNV 스케줄 포함, 프로덕션 GameManager.AdvancePhase
    /// 경로)로 클리어 도달을 검증하고, 무위 플레이의 조기 파산·상수 동기 핀·저장 왕복을 함께 고정한다.
    /// </summary>
    public class BalanceEndingGuardTests
    {
        static List<T> LoadAll<T>(string folder) where T : UnityEngine.Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder })
                .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null)
                .ToList();
        }

        static GameManager CreateGameManager(out GameObject go)
        {
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
            gm.StartNewGame();
            var genres = LoadAll<GenreDef>("Assets/Data/Definitions/Genres");
            genres.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            var events = LoadAll<GameEventDef>("Assets/Data/Definitions/Events");
            events.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            gm.EditorInit(genres, events);
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

        /// <summary>
        /// Market phase 에서 오늘 plan 이 실제로 요구하는 재료만 정확히(이벤트 폭등 배수 포함) 구매한다
        /// (FirstPlayableLoopTests.NonBankrupt_Full_Day_Loop_Works 전례 — 전 재료 대량 구매는 자금 초과 위험).
        /// </summary>
        static void PurchaseExactNeeds(GameManager gm, GenreDef genre, List<RecipeDef> recipes,
            List<CustomerArchetypeDef> customers, Dictionary<IngredientKind, IngredientDef> ingredientsC)
        {
            var service = ServiceManager.Instance;
            Assert.IsTrue(service.TryBuildDayPlan(genre, out var plan, out var planReason), planReason);
            var previewOrders = ServiceOps.BuildOrders(plan, customers);

            Assert.IsTrue(gm.TryBuildTodayEventEffects(out var fx, out var fxReason), fxReason);

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

            foreach (var need in totalNeeded)
            {
                var purchase = EconomyOps.TryPurchaseIngredient(
                    gm.State, ingredientsC[need.Key], need.Value, genre.CostMultiplier, fx.IngredientCostMilli);
                Assert.IsTrue(purchase.Success, $"day {gm.State.day} {need.Key} 구매 실패: {purchase.Message}");
            }
        }

        /// <summary>전량 서빙·C급 실요구량 구매로 하루를 완주(Market→Service→Settlement→Night→다음날 Market)한다.
        /// 정산 결과 클리어/파산으로 런이 종료되면 Settlement 에 머문 채 반환한다(EndingOps.IsRunEnded).</summary>
        static void PlayOneFullDay(GameManager gm, GenreDef genre, List<RecipeDef> recipes,
            List<CustomerArchetypeDef> customers, Dictionary<IngredientKind, IngredientDef> ingredientsC)
        {
            var service = ServiceManager.Instance;
            var state = gm.State;

            PurchaseExactNeeds(gm, genre, recipes, customers, ingredientsC);

            Assert.IsTrue(gm.CanAdvancePhase(out var advanceReason), $"day {state.day} Market→Service 차단: {advanceReason}");
            Assert.AreEqual(DayPhase.Service, gm.AdvancePhase());

            while (service.CurrentOrder != null)
            {
                var order = service.CurrentOrder;
                var recipe = recipes.First(r => r.Id == order.recipeId);
                var serve = service.TryServeCurrentOrder(recipe, IngredientGrade.C);
                Assert.IsTrue(serve.Success, $"day {state.day} 서빙 실패: {serve.Message}");
            }

            Assert.AreEqual(DayPhase.Settlement, gm.AdvancePhase());
            gm.AdvancePhase(); // Settlement 이탈 게이트가 정산을 선적용 — 클리어/파산이면 Settlement 에 머문다

            if (!EndingOps.IsRunEnded(state))
            {
                gm.AdvancePhase(); // Night → Market (day+1) — 런이 끝나지 않았을 때만 진행
            }
        }

        // ── 조건 1: 전 장르 N일 실루프 클리어 도달 ─────────────────────────────

        [Test]
        public void AllGenres_N_Day_Real_Loop_Reaches_Cleared_Via_Production_AdvancePhase()
        {
            var recipes = LoadAll<RecipeDef>("Assets/Data/Definitions/Recipes");
            var customers = LoadAll<CustomerArchetypeDef>("Assets/Data/Definitions/Customers");
            var ingredientsC = LoadAll<IngredientDef>("Assets/Data/Definitions/Ingredients")
                .Where(i => i.Grade == IngredientGrade.C).ToDictionary(i => i.Kind);
            var genres = LoadAll<GenreDef>("Assets/Data/Definitions/Genres");

            foreach (var genre in genres)
            {
                var gm = CreateGameManager(out var go);
                try
                {
                    var state = gm.State;
                    var genreIds = gm.GenreCatalog.Select(gd => gd.Id).ToList();
                    var selection = GenreSelectionOps.TrySelect(state, genre.Id, genreIds);
                    Assert.IsTrue(selection.Success, selection.Message);

                    for (int day = 1; day <= EndingOps.ClearTargetDays; day++)
                    {
                        Assert.AreEqual(day, state.day, $"{genre.Id} day {day} 진입 확인");
                        Assert.IsFalse(state.isBankrupt, $"{genre.Id} day {day} 진입 전 파산 상태가 아니어야 한다");
                        PlayOneFullDay(gm, genre, recipes, customers, ingredientsC);
                    }

                    Assert.IsTrue(SettlementOps.IsSettlementApplied(state), $"{genre.Id} Day {EndingOps.ClearTargetDays} 정산 적용");
                    Assert.IsFalse(state.isBankrupt, $"{genre.Id} 는 파산 없이 클리어에 도달해야 한다");
                    Assert.AreEqual(EndingOps.ClearTargetDays, state.daysCompleted);
                    Assert.AreEqual(RunEndingStatus.Cleared, EndingOps.GetStatus(state), $"{genre.Id} 클리어 도달 실패");
                    Assert.AreEqual(DayPhase.Settlement, state.currentPhase, "클리어 게이트가 Settlement 에 머물게 한다");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }
        }

        // ── 조건 3: 무위 플레이 Day ≤3 파산 ────────────────────────────────────

        [Test]
        public void Noop_Play_Bankrupts_By_Day_3_Or_Earlier()
        {
            var gm = CreateGameManager(out var go);
            try
            {
                var state = gm.State;
                var genreIds = gm.GenreCatalog.Select(gd => gd.Id).ToList();
                var selection = GenreSelectionOps.TrySelect(state, "generalist", genreIds);
                Assert.IsTrue(selection.Success, selection.Message);

                int day = 0;
                for (day = 1; day <= 3 && !state.isBankrupt; day++)
                {
                    Assert.IsTrue(gm.CanAdvancePhase(out var reason), $"day {day} Market→Service (구매 0): {reason}");
                    Assert.AreEqual(DayPhase.Service, gm.AdvancePhase());

                    var service = ServiceManager.Instance;
                    while (service.CurrentOrder != null)
                    {
                        var skip = service.SkipCurrentOrder();
                        Assert.IsTrue(skip.Success, skip.Message);
                    }

                    gm.AdvancePhase(); // → Settlement
                    gm.AdvancePhase(); // Settlement 이탈 게이트가 정산 선적용 — 파산이면 Settlement 에 머문다

                    if (state.isBankrupt)
                    {
                        break;
                    }
                    gm.AdvancePhase(); // → Market (day+1)
                }

                Assert.IsTrue(state.isBankrupt, $"무위 플레이는 Day 3 이내 파산해야 한다 (마지막 확인 day={day})");
                Assert.LessOrEqual(state.bankruptcyDay, 3, "파산일이 Day 3 이내여야 한다");
                Assert.AreEqual(RunEndingStatus.Bankrupt, EndingOps.GetStatus(state));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        // ── 조건 3 상수 동기 핀 ─────────────────────────────────────────────

        [Test]
        public void Constant_Sync_Pins()
        {
            Assert.AreEqual(SettlementOps.DailyOperatingCost, EventOps.BaseDailyOperatingCost,
                "EventOps.BaseDailyOperatingCost 와 SettlementOps.DailyOperatingCost 는 동기화되어야 한다");
            Assert.AreEqual(30000, GameState.StartingCash);
            Assert.AreEqual(7, EndingOps.ClearTargetDays);
            Assert.AreEqual(1, GameState.SaveSchemaVersion, "엔딩은 파생만으로 성립 — 스키마 파괴 없음");
            // 시드 운영비 값 핀 — 재조정 시 이 값도 함께 갱신한다(task-115 B3, 오너 승인 재확정).
            Assert.AreEqual(15000, SettlementOps.DailyOperatingCost, "task-115 B3 오너 승인 확정 시드");
        }

        // ── 조건 4: 클리어 직후 저장·재로드 후에도 GetStatus == Cleared ─────────

        [Test]
        public void Cleared_State_Survives_Save_Reload_Round_Trip()
        {
            var genreIds = LoadAll<GenreDef>("Assets/Data/Definitions/Genres").Select(g => g.Id).OrderBy(id => id, System.StringComparer.Ordinal).ToList();

            var state = new GameState
            {
                day = EndingOps.ClearTargetDays,
                currentPhase = DayPhase.Settlement,
                serviceDay = EndingOps.ClearTargetDays,
                settlementDay = EndingOps.ClearTargetDays,
                daysCompleted = EndingOps.ClearTargetDays,
                cash = 150000,
                isBankrupt = false,
                selectedGenreId = genreIds[0],
            };
            Assert.AreEqual(RunEndingStatus.Cleared, EndingOps.GetStatus(state));

            var catalogs = new SaveOps.SaveCatalogInputs
            {
                GenreIds = genreIds,
                RecipeIds = LoadAll<RecipeDef>("Assets/Data/Definitions/Recipes").Select(r => r.Id).ToList(),
                CustomerIds = LoadAll<CustomerArchetypeDef>("Assets/Data/Definitions/Customers").Select(c => c.Id).ToList(),
                SnsCampaignIds = LoadAll<SNSCampaignDef>("Assets/Data/Definitions/SNS").Select(s => s.Id).ToList(),
                EventDefs = GameManager.ToEventInputs(LoadAll<GameEventDef>("Assets/Data/Definitions/Events")),
            };

            Assert.IsTrue(SaveOps.TrySerialize(state, catalogs, out var json, out var serializeReason), serializeReason);
            Assert.IsTrue(SaveOps.TryDeserialize(json, catalogs, out var restored, out var deserializeReason), deserializeReason);

            Assert.AreEqual(RunEndingStatus.Cleared, EndingOps.GetStatus(restored), "왕복 후에도 클리어 상태가 유지되어야 한다");
        }
    }
}
