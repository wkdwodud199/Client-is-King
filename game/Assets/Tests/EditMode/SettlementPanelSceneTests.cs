using System.Linq;
using ClientIsKing.EditorTools;
using ClientIsKing.Settlement;
using ClientIsKing.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>task-107: SceneBuilder 가 생성한 Settlement/Night UI 산출물 검증.</summary>
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
            var canvas = roots.FirstOrDefault(go => go.name == "Canvas");
            Assert.IsNotNull(canvas, "Canvas 누락");
            settlementPanel = canvas.transform.Find("Panel_Settlement");
            nightPanel = canvas.transform.Find("Panel_Night");
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
}
