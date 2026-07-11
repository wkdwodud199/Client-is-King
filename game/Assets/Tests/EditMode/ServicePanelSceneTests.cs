using System.Linq;
using ClientIsKing.EditorTools;
using ClientIsKing.Service;
using ClientIsKing.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-106: SceneBuilder 가 생성한 Shop 씬 Service UI 산출물 검증.
    /// task-110 (U6): 장르 적용 가격 표시 흐름은 상태를 공유하지 않도록
    /// <see cref="ServicePanelGenreFlowTests"/> 별도 fixture 로 분리한다.
    /// </summary>
    public class ServicePanelSceneTests
    {
        Transform servicePanel;
        ServicePanelController controller;
        GameObject gameManagerGo;

        [OneTimeSetUp]
        public void BuildAndOpen()
        {
            SceneBuilder.Apply();
            var scene = EditorSceneManager.OpenScene(SceneBuilder.ShopPath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var canvas = roots.FirstOrDefault(go => go.name == "Canvas");
            Assert.IsNotNull(canvas, "Canvas 누락");
            servicePanel = canvas.transform.Find("Panel_Service");
            Assert.IsNotNull(servicePanel, "Panel_Service 누락");
            controller = servicePanel.GetComponent<ServicePanelController>();
            gameManagerGo = roots.FirstOrDefault(go => go.name == "GameManager");
        }

        [Test]
        public void ServicePanel_Has_Controller_And_All_Controls()
        {
            Assert.IsNotNull(controller, "ServicePanelController 누락");

            string[] controls =
            {
                "OrderText", "CustomerText", "RecipeText", "CookTimeText", "RevenueText",
                "GradeButton", "RequiredText", "ServeButton", "SkipButton", "StatsText", "MessageText",
            };
            foreach (var name in controls)
            {
                Assert.IsNotNull(servicePanel.Find(name), $"{name} 누락");
            }
            Assert.IsFalse(servicePanel.gameObject.activeSelf, "초기 상태는 비활성 (Market 만 활성)");
        }

        [Test]
        public void Controller_Has_Recipes_And_Customers_Injected()
        {
            Assert.IsNotNull(controller, "ServicePanelController 누락");
            Assert.AreEqual(6, controller.RecipeDefs.Count, "RecipeDef 6종 주입");
            Assert.GreaterOrEqual(controller.CustomerDefs.Count, 4, "CustomerArchetypeDef 4종 이상 주입");
        }

        [Test]
        public void GameManager_Bootstrap_Has_ServiceManager()
        {
            Assert.IsNotNull(gameManagerGo, "GameManager 오브젝트 누락");
            Assert.IsNotNull(gameManagerGo.GetComponent<ServiceManager>(), "ServiceManager 탑재 누락");
        }
    }

    /// <summary>
    /// task-110 U6: 장르 적용 1인 가격·파티 포함 예상 총액 표시가 실제 transaction 과 같은
    /// ServiceOps.CalculateSalePrice 경로를 쓰는지 검증한다. 각 테스트가 씬을 새로 연다.
    /// </summary>
    public class ServicePanelGenreFlowTests
    {
        [Test]
        public void Revenue_Text_Shows_PerCustomer_And_Order_Total_Using_Same_Genre_Price()
        {
            // 배치 EditMode 에서는 OpenScene 이 Awake/Start 를 동기 호출한다는 보장이 없다
            // (group1/U6 공통 함정) — TestSceneSupport 가 singleton 을 강제 동기화한다.
            var gameManagerGo = TestSceneSupport.OpenShopSceneWithLiveSingletons();
            var canvas = gameManagerGo.scene.GetRootGameObjects().First(go => go.name == "Canvas").transform;
            var marketPanel = canvas.Find("Panel_Market");
            var servicePanel = canvas.Find("Panel_Service");
            var controller = servicePanel.GetComponent<ServicePanelController>();

            var gm = gameManagerGo.GetComponent<ClientIsKing.Managers.GameManager>();
            gm.StartNewGame();

            // 장르 확정 — 국밥(specialist), UI 우회 없이 실제 도메인 경로로 선택한다.
            var genreIds = gm.GenreCatalog.Select(gd => gd.Id).ToList();
            var selection = ClientIsKing.Genre.GenreSelectionOps.TrySelect(gm.State, "gukbap", genreIds);
            Assert.IsTrue(selection.Success, selection.Message);
            ClientIsKing.DayCycle.GameEvents.RaiseGenreSelected(selection.GenreId);

            // Market → Service: GameManager.AdvancePhase 가 실제 plan/주문을 원자적으로 생성한다.
            Assert.AreEqual(ClientIsKing.DayCycle.DayPhase.Service, gm.AdvancePhase());
            Assert.Greater(gm.State.serviceOrders.Count, 0, "AdvancePhase 직후 주문이 이미 생성되어 있어야 한다");

            marketPanel.gameObject.SetActive(false);
            servicePanel.gameObject.SetActive(true);
            // 배치 EditMode 에서는 SetActive 가 OnEnable 을 동기 호출한다는 보장까지는 없을 수 있어
            // (Awake/Start 와 같은 함정 계열) 직접 호출로 RefreshAll/PublishCurrentOrder 를 확정한다.
            TestSceneSupport.ForceOnEnable(controller);

            var service = ServiceManager.Instance;
            var order = service.CurrentOrder;
            Assert.IsNotNull(order, "Service 진입 직후 첫 주문이 있어야 한다");

            gm.TryGetGenre("gukbap", out var genreDef);
            var recipe = controller.RecipeDefs.First(r => r.Id == order.recipeId);
            int expectedPerCustomer = ServiceOps.CalculateSalePrice(recipe, 1, genreDef.PricePerCustomerMultiplier);
            int expectedTotal = ServiceOps.CalculateSalePrice(recipe, order.partySize, genreDef.PricePerCustomerMultiplier);

            var revenueText = servicePanel.Find("RevenueText").GetComponent<TMPro.TMP_Text>();
            Assert.IsTrue(revenueText.text.Contains($"{expectedPerCustomer:N0}"),
                $"1인 가격 {expectedPerCustomer:N0}원이 RevenueText에 없음: '{revenueText.text}'");
            Assert.IsTrue(revenueText.text.Contains($"{expectedTotal:N0}"),
                $"예상 총액 {expectedTotal:N0}원이 RevenueText에 없음: '{revenueText.text}'");
            Assert.IsFalse(revenueText.text.Contains("객단가"), "party 포함 총액을 '객단가'라 부르지 않는다 (D5)");

            // 등급 토글은 숨김(C급 고정) — B급 데이터/Ops 는 보존.
            var gradeButton = servicePanel.Find("GradeButton").GetComponent<UnityEngine.UI.Button>();
            Assert.IsFalse(gradeButton.gameObject.activeSelf, "task-110 UI 는 등급 토글을 숨겨야 한다");
        }

        // ── task-111 U6: SNS 유입 태그 표시 ─────────────────────────────────

        [Test]
        public void CustomerText_Shows_Sns_Inflow_Tag_Only_For_Tagged_Orders()
        {
            var gameManagerGo = TestSceneSupport.OpenShopSceneWithLiveSingletons();
            var canvas = gameManagerGo.scene.GetRootGameObjects().First(go => go.name == "Canvas").transform;
            var marketPanel = canvas.Find("Panel_Market");
            var servicePanel = canvas.Find("Panel_Service");
            var controller = servicePanel.GetComponent<ServicePanelController>();

            var gm = gameManagerGo.GetComponent<ClientIsKing.Managers.GameManager>();
            gm.StartNewGame();

            // 장르 선택은 Day 1 게이트(TrySelect 계약)이므로 day 를 바꾸기 전에 먼저 확정한다.
            var genreIds = gm.GenreCatalog.Select(gd => gd.Id).ToList();
            var selection = ClientIsKing.Genre.GenreSelectionOps.TrySelect(gm.State, "bunsik", genreIds);
            Assert.IsTrue(selection.Success, selection.Message);
            ClientIsKing.DayCycle.GameEvents.RaiseGenreSelected(selection.GenreId);

            // 어제(Day 1) short_form 을 집행한 것으로 history 를 구성해 Day 2 plan 에 보너스가 실리게 한다.
            var service = ServiceManager.Instance;
            var shortForm = service.SnsCampaignDefs.First(d => d.Id == "short_form");
            var shortFormInput = ServiceManager.ToSnsCampaignInput(shortForm);
            int reachMilli = ClientIsKing.Social.SNSCampaignOps.ProjectMilli(shortFormInput.BaseReach);
            int bonusOrders = ClientIsKing.Social.SNSCampaignOps.CalculateBonusOrderCount(reachMilli);
            Assert.Greater(bonusOrders, 0, "short_form 첫 집행은 보너스 주문이 있어야 시나리오가 성립한다");

            gm.State.day = 2;
            gm.State.snsCampaignHistory.Add(new ClientIsKing.Social.SNSCampaignRecord
            {
                campaignId = "short_form",
                executedOnDay = 1,
                bonusOrderCount = bonusOrders,
                effectiveMilliReach = reachMilli,
            });

            Assert.AreEqual(ClientIsKing.DayCycle.DayPhase.Service, gm.AdvancePhase());
            Assert.AreEqual(6 + bonusOrders, gm.State.serviceOrders.Count, "분식 base 6건 + 보너스 태그 주문");

            marketPanel.gameObject.SetActive(false);
            servicePanel.gameObject.SetActive(true);
            TestSceneSupport.ForceOnEnable(controller);

            var customerText = servicePanel.Find("CustomerText").GetComponent<TMPro.TMP_Text>();

            // base 구간(인덱스 0..5)을 전부 포기 처리해 보너스 태그 주문까지 진행한다.
            for (int i = 0; i < 6; i++)
            {
                Assert.IsFalse(customerText.text.Contains("SNS 유입"), $"base 인덱스 {i} 주문은 SNS 유입 태그가 없어야 한다: '{customerText.text}'");
                service.SkipCurrentOrder();
                TestSceneSupport.ForceOnEnable(controller);
            }

            Assert.IsTrue(customerText.text.Contains("SNS 유입"), $"보너스 인덱스 주문은 SNS 유입 태그가 있어야 한다: '{customerText.text}'");
        }

        // ── task-112 U7: 단체 손님 태그 표시 (F4) ───────────────────────────

        [Test]
        public void CustomerText_Shows_Group_Customers_Tag_Only_For_Event_Tagged_Order()
        {
            var gameManagerGo = TestSceneSupport.OpenShopSceneWithLiveSingletons();
            var canvas = gameManagerGo.scene.GetRootGameObjects().First(go => go.name == "Canvas").transform;
            var marketPanel = canvas.Find("Panel_Market");
            var servicePanel = canvas.Find("Panel_Service");
            var controller = servicePanel.GetComponent<ServicePanelController>();

            var gm = gameManagerGo.GetComponent<ClientIsKing.Managers.GameManager>();
            gm.StartNewGame();

            var genreIds = gm.GenreCatalog.Select(gd => gd.Id).ToList();
            var selection = ClientIsKing.Genre.GenreSelectionOps.TrySelect(gm.State, "bunsik", genreIds);
            Assert.IsTrue(selection.Success, selection.Message);
            ClientIsKing.DayCycle.GameEvents.RaiseGenreSelected(selection.GenreId);

            gm.State.day = 5; // C4 표상 단체 손님 활성화 day
            gm.State.activeEvents.Add(new ClientIsKing.Events.ActiveEventState { eventId = "group_customers", remainingDays = 1 });

            var service = ServiceManager.Instance;
            Assert.AreEqual(ClientIsKing.DayCycle.DayPhase.Service, gm.AdvancePhase());
            Assert.AreEqual(7, gm.State.serviceOrders.Count, "분식 base 6건 + 단체 보너스 1건");

            marketPanel.gameObject.SetActive(false);
            servicePanel.gameObject.SetActive(true);
            TestSceneSupport.ForceOnEnable(controller);

            var customerText = servicePanel.Find("CustomerText").GetComponent<TMPro.TMP_Text>();

            for (int i = 0; i < 6; i++)
            {
                Assert.IsFalse(customerText.text.Contains("단체 손님"), $"base 인덱스 {i} 주문은 단체 손님 태그가 없어야 한다: '{customerText.text}'");
                service.SkipCurrentOrder();
                TestSceneSupport.ForceOnEnable(controller);
            }

            Assert.IsTrue(customerText.text.Contains("단체 손님"), $"단체 보너스 인덱스 주문은 단체 손님 태그가 있어야 한다: '{customerText.text}'");
            Assert.IsTrue(customerText.text.Contains("×4"), $"단체 파티는 4인 고정이어야 한다: '{customerText.text}'");
        }
    }
}
