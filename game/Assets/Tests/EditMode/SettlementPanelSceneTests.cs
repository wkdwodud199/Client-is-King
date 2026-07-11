using System.Linq;
using ClientIsKing.EditorTools;
using ClientIsKing.Settlement;
using ClientIsKing.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-107: SceneBuilder 가 생성한 Settlement/Night UI 산출물 검증.
    /// task-110 (U6): 전문 분야 원인 한 줄과 정산 멱등성 흐름은 상태를 공유하지 않도록
    /// <see cref="SettlementPanelGenreFlowTests"/> 별도 fixture 로 분리한다.
    /// </summary>
    public class SettlementPanelSceneTests
    {
        Transform settlementPanel;
        Transform nightPanel;
        GameObject gameManagerGo;

        [OneTimeSetUp]
        public void BuildAndOpen()
        {
            SceneBuilder.Apply();
            var scene = EditorSceneManager.OpenScene(SceneBuilder.ShopPath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var canvasGo = roots.FirstOrDefault(go => go.name == "Canvas");
            Assert.IsNotNull(canvasGo, "Canvas 누락");
            settlementPanel = canvasGo.transform.Find("Panel_Settlement");
            nightPanel = canvasGo.transform.Find("Panel_Night");
            gameManagerGo = roots.FirstOrDefault(go => go.name == "GameManager");
        }

        [Test]
        public void Settlement_Panel_Has_Controller_And_Texts()
        {
            Assert.IsNotNull(settlementPanel, "Panel_Settlement 누락");
            Assert.IsNotNull(settlementPanel.GetComponent<SettlementPanelController>(), "SettlementPanelController 누락");

            string[] texts = { "GrossText", "SpendText", "OperatingText", "NetText", "CashText", "StatsText", "MessageText" };
            foreach (var name in texts)
            {
                Assert.IsNotNull(settlementPanel.Find(name), $"{name} 누락");
            }
            Assert.IsFalse(settlementPanel.gameObject.activeSelf, "초기 비활성 유지");
        }

        [Test]
        public void Night_Panel_Has_Controller_And_Texts()
        {
            Assert.IsNotNull(nightPanel, "Panel_Night 누락");
            Assert.IsNotNull(nightPanel.GetComponent<NightPanelController>(), "NightPanelController 누락");

            string[] texts = { "SummaryText", "DaysText", "StatusText" };
            foreach (var name in texts)
            {
                Assert.IsNotNull(nightPanel.Find(name), $"{name} 누락");
            }
            Assert.IsFalse(nightPanel.gameObject.activeSelf, "초기 비활성 유지");
        }

        [Test]
        public void GameManager_Bootstrap_Has_SettlementManager()
        {
            Assert.IsNotNull(gameManagerGo, "GameManager 오브젝트 누락");
            Assert.IsNotNull(gameManagerGo.GetComponent<SettlementManager>(), "SettlementManager 탑재 누락");
        }
    }

    /// <summary>
    /// task-110 U6: 전문 분야 원인 한 줄(GenreEffectText)과 정산 멱등성(재표시해도 cash 불변)을 검증한다.
    /// </summary>
    public class SettlementPanelGenreFlowTests
    {
        [Test]
        public void GenreEffectText_Shows_Comparison_Line_And_Settlement_Stays_Idempotent()
        {
            // 배치 EditMode 에서는 OpenScene 이 Awake 를 동기 호출한다는 보장이 없다
            // (group1/U6 공통 함정) — TestSceneSupport 가 singleton 을 강제 동기화한다.
            var gameManagerGo = TestSceneSupport.OpenShopSceneWithLiveSingletons();
            var canvas = gameManagerGo.scene.GetRootGameObjects().First(go => go.name == "Canvas").transform;
            var settlementPanel = canvas.Find("Panel_Settlement");

            var gm = gameManagerGo.GetComponent<ClientIsKing.Managers.GameManager>();
            gm.StartNewGame();

            var genreIds = gm.GenreCatalog.Select(gd => gd.Id).ToList();
            var selection = ClientIsKing.Genre.GenreSelectionOps.TrySelect(gm.State, "bunsik", genreIds);
            Assert.IsTrue(selection.Success, selection.Message);
            ClientIsKing.DayCycle.GameEvents.RaiseGenreSelected(selection.GenreId);

            Assert.AreEqual(ClientIsKing.DayCycle.DayPhase.Service, gm.AdvancePhase()); // Market → Service

            // Service → Settlement 게이트는 열린 주문이 없어야 통과한다 (design.md 테스트 기준) —
            // 모든 주문을 포기 처리해 조건을 채운다 (매출 원인은 이 테스트의 관심사가 아니다).
            var service = ClientIsKing.Service.ServiceManager.Instance;
            while (service.CurrentOrder != null)
            {
                service.SkipCurrentOrder();
            }

            Assert.AreEqual(ClientIsKing.DayCycle.DayPhase.Settlement, gm.AdvancePhase()); // Service → Settlement (정산 아직 미적용)

            var settlementController = settlementPanel.GetComponent<SettlementPanelController>();
            settlementPanel.gameObject.SetActive(true);
            // 배치 EditMode 에서는 SetActive 가 OnEnable 을 동기 호출한다는 보장까지는 없을 수 있어
            // (Awake/Start 와 같은 함정 계열) 직접 호출로 ApplyDailySettlement+RenderFinal 을 확정한다.
            TestSceneSupport.ForceOnEnable(settlementController);

            var genreEffectText = settlementPanel.Find("GenreEffectText");
            Assert.IsNotNull(genreEffectText, "Panel_Settlement/GenreEffectText 누락");
            var text = genreEffectText.GetComponent<TMPro.TMP_Text>();
            Assert.IsTrue(text.text.Contains("전문 분야 효과"), $"전문 분야 원인 한 줄 누락: '{text.text}'");
            Assert.IsTrue(text.text.Contains("분식"), $"선택 장르 표시명이 포함되어야 함: '{text.text}'");

            int cashAfterFirstRender = gm.State.cash;

            // 같은 패널을 다시 활성화해도(재표시) 정산은 day 당 1회 — cash 불변 (멱등성).
            TestSceneSupport.ForceOnEnable(settlementController);

            Assert.AreEqual(cashAfterFirstRender, gm.State.cash, "정산 재표시는 cash 를 바꾸지 않아야 한다 (day 당 1회 계약)");
            Assert.IsTrue(SettlementOps.IsSettlementApplied(gm.State));
        }

        // ── task-111 U6: SNS 원인 라인 (F4) ─────────────────────────────────

        [Test]
        public void SnsEffectText_Shows_Cause_Line_When_Yesterday_Campaign_Executed()
        {
            var gameManagerGo = TestSceneSupport.OpenShopSceneWithLiveSingletons();
            var canvas = gameManagerGo.scene.GetRootGameObjects().First(go => go.name == "Canvas").transform;
            var settlementPanel = canvas.Find("Panel_Settlement");

            var gm = gameManagerGo.GetComponent<ClientIsKing.Managers.GameManager>();
            gm.StartNewGame();

            // 장르 선택은 Day 1 게이트(TrySelect 계약)이므로 day 를 바꾸기 전에 먼저 확정한다.
            var genreIds = gm.GenreCatalog.Select(gd => gd.Id).ToList();
            var selection = ClientIsKing.Genre.GenreSelectionOps.TrySelect(gm.State, "gukbap", genreIds);
            Assert.IsTrue(selection.Success, selection.Message);
            ClientIsKing.DayCycle.GameEvents.RaiseGenreSelected(selection.GenreId);

            var service = ClientIsKing.Service.ServiceManager.Instance;
            var localBoard = service.SnsCampaignDefs.First(d => d.Id == "local_board");
            var localBoardInput = ClientIsKing.Service.ServiceManager.ToSnsCampaignInput(localBoard);
            int reachMilli = ClientIsKing.Social.SNSCampaignOps.ProjectMilli(localBoardInput.BaseReach);
            int bonusOrders = ClientIsKing.Social.SNSCampaignOps.CalculateBonusOrderCount(reachMilli);
            Assert.Greater(bonusOrders, 0, "local_board 첫 집행은 보너스 주문이 있어야 시나리오가 성립한다");

            gm.State.day = 2;
            gm.State.snsCampaignHistory.Add(new ClientIsKing.Social.SNSCampaignRecord
            {
                campaignId = "local_board",
                executedOnDay = 1,
                costPaid = localBoard.BaseCost,
                bonusOrderCount = bonusOrders,
                effectiveMilliReach = reachMilli,
            });

            Assert.AreEqual(ClientIsKing.DayCycle.DayPhase.Service, gm.AdvancePhase());

            // 태그 주문(뒤쪽 bonusOrders 건)만 전량 서빙, base 는 포기 처리 — 인과 라인의 served 값을 확정한다.
            var recipes = LoadAll<ClientIsKing.Data.RecipeDef>("Assets/Data/Definitions/Recipes");
            var ingredients = LoadAll<ClientIsKing.Data.IngredientDef>("Assets/Data/Definitions/Ingredients");
            foreach (var kind in System.Enum.GetValues(typeof(ClientIsKing.Data.IngredientKind)))
            {
                var ingredientDef = ingredients.FirstOrDefault(i => i.Kind.Equals(kind) && i.Grade == ClientIsKing.Data.IngredientGrade.C);
                if (ingredientDef != null)
                {
                    ClientIsKing.Inventory.InventoryOps.Add(gm.State, (ClientIsKing.Data.IngredientKind)kind, ClientIsKing.Data.IngredientGrade.C, 50);
                }
            }

            while (service.CurrentOrder != null)
            {
                var order = service.CurrentOrder;
                if (order.snsInflow)
                {
                    var recipe = recipes.First(r => r.Id == order.recipeId);
                    var result = service.TryServeCurrentOrder(recipe, ClientIsKing.Data.IngredientGrade.C);
                    Assert.IsTrue(result.Success, result.Message);
                }
                else
                {
                    service.SkipCurrentOrder();
                }
            }

            Assert.AreEqual(ClientIsKing.DayCycle.DayPhase.Settlement, gm.AdvancePhase());

            var settlementController = settlementPanel.GetComponent<SettlementPanelController>();
            settlementPanel.gameObject.SetActive(true);
            TestSceneSupport.ForceOnEnable(settlementController);

            var snsEffectText = settlementPanel.Find("SnsEffectText");
            Assert.IsNotNull(snsEffectText, "Panel_Settlement/SnsEffectText 누락");
            var text = snsEffectText.GetComponent<TMPro.TMP_Text>();
            Assert.IsTrue(text.text.Contains("SNS(동네게시판)"), $"SNS 원인 라인에 표시명이 포함되어야 함: '{text.text}'");
            Assert.IsTrue(text.text.Contains($"유입 {bonusOrders}/{bonusOrders}팀"), $"전량 서빙 시 유입 {bonusOrders}/{bonusOrders}팀 이어야 함: '{text.text}'");
        }

        [Test]
        public void SnsEffectText_Is_Empty_When_No_Yesterday_Campaign()
        {
            var gameManagerGo = TestSceneSupport.OpenShopSceneWithLiveSingletons();
            var canvas = gameManagerGo.scene.GetRootGameObjects().First(go => go.name == "Canvas").transform;
            var settlementPanel = canvas.Find("Panel_Settlement");

            var gm = gameManagerGo.GetComponent<ClientIsKing.Managers.GameManager>();
            gm.StartNewGame();

            var genreIds = gm.GenreCatalog.Select(gd => gd.Id).ToList();
            var selection = ClientIsKing.Genre.GenreSelectionOps.TrySelect(gm.State, "noodles", genreIds);
            Assert.IsTrue(selection.Success, selection.Message);
            ClientIsKing.DayCycle.GameEvents.RaiseGenreSelected(selection.GenreId);

            Assert.AreEqual(ClientIsKing.DayCycle.DayPhase.Service, gm.AdvancePhase());
            var service = ClientIsKing.Service.ServiceManager.Instance;
            while (service.CurrentOrder != null)
            {
                service.SkipCurrentOrder();
            }
            Assert.AreEqual(ClientIsKing.DayCycle.DayPhase.Settlement, gm.AdvancePhase());

            var settlementController = settlementPanel.GetComponent<SettlementPanelController>();
            settlementPanel.gameObject.SetActive(true);
            TestSceneSupport.ForceOnEnable(settlementController);

            var text = settlementPanel.Find("SnsEffectText").GetComponent<TMPro.TMP_Text>();
            Assert.AreEqual("", text.text, "SNS 집행 기록이 없으면 SnsEffectText 는 빈 문자열이어야 한다");
        }

        static System.Collections.Generic.List<T> LoadAll<T>(string folder) where T : UnityEngine.Object
        {
            return UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder })
                .Select(g => UnityEditor.AssetDatabase.LoadAssetAtPath<T>(UnityEditor.AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null)
                .ToList();
        }
    }
}
