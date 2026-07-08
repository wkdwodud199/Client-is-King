using System.Linq;
using ClientIsKing.EditorTools;
using ClientIsKing.Service;
using ClientIsKing.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>task-106: SceneBuilder 가 생성한 Shop 씬 Service UI 산출물 검증.</summary>
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
}
