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
    }
}
