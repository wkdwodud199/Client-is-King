using System.Linq;
using ClientIsKing.Data;
using ClientIsKing.EditorTools;
using ClientIsKing.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-105: SceneBuilder 가 생성한 Shop 씬 장보기 UI 산출물 검증 — 컨트롤 존재 + 재료 주입.
    /// </summary>
    public class MarketPanelSceneTests
    {
        Transform marketPanel;
        MarketPanelController controller;

        [OneTimeSetUp]
        public void BuildAndOpen()
        {
            SceneBuilder.Apply();
            var scene = EditorSceneManager.OpenScene(SceneBuilder.ShopPath, OpenSceneMode.Single);
            var canvas = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "Canvas");
            Assert.IsNotNull(canvas, "Canvas 누락");
            marketPanel = canvas.transform.Find("Panel_Market");
            Assert.IsNotNull(marketPanel, "Panel_Market 누락");
            controller = marketPanel.GetComponent<MarketPanelController>();
        }

        [Test]
        public void MarketPanel_Has_Controller_And_All_Controls()
        {
            Assert.IsNotNull(controller, "MarketPanelController 누락");

            string[] controls =
            {
                "CashText", "IngredientPrev", "IngredientLabel", "IngredientNext",
                "GradeButton", "QuantityMinus", "QuantityText", "QuantityPlus",
                "CostText", "OwnedText", "BuyButton", "MessageText",
            };
            foreach (var name in controls)
            {
                Assert.IsNotNull(marketPanel.Find(name), $"{name} 누락");
            }
        }

        [Test]
        public void Controller_Has_18_Defs_With_Both_Grades_Per_Kind()
        {
            Assert.IsNotNull(controller, "MarketPanelController 누락");
            var defs = controller.IngredientDefs;
            Assert.AreEqual(18, defs.Count, "재료 18종(9종×C/B) 주입");

            foreach (var group in defs.GroupBy(d => d.Kind))
            {
                CollectionAssert.AreEquivalent(
                    new[] { IngredientGrade.C, IngredientGrade.B },
                    group.Select(d => d.Grade).ToList(),
                    $"{group.Key}: C/B 등급이 UI 선택에 모두 노출 가능해야 한다");
            }
        }
    }
}
