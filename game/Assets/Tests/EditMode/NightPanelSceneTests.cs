using System.Linq;
using ClientIsKing.EditorTools;
using ClientIsKing.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-111 U6: Night 패널 SNS 블록의 정적 산출물(F1 오브젝트·좌표) 검증 — 구조 전용,
    /// 상태를 바꾸는 흐름은 <see cref="NightPanelSnsFlowTests"/> 로 분리한다(task-110 씬 격리 교훈).
    /// </summary>
    public class NightPanelSceneTests
    {
        Transform nightPanel;

        [OneTimeSetUp]
        public void BuildAndOpen()
        {
            SceneBuilder.Apply();
            var scene = EditorSceneManager.OpenScene(SceneBuilder.ShopPath, UnityEditor.SceneManagement.OpenSceneMode.Single);
            var canvasGo = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "Canvas");
            Assert.IsNotNull(canvasGo, "Canvas 누락");
            nightPanel = canvasGo.transform.Find("Panel_Night");
            Assert.IsNotNull(nightPanel, "Panel_Night 누락");
        }

        [Test]
        public void Night_Panel_Has_All_Sns_Block_Objects()
        {
            string[] names =
            {
                "SummaryText", "DaysText", "FollowerText", "SnsTitleText",
                "Button_Sns_PhotoFeed", "Button_Sns_ShortForm", "Button_Sns_LocalBoard",
                "SnsInfoText", "StatusText",
            };
            foreach (var name in names)
            {
                Assert.IsNotNull(nightPanel.Find(name), $"Panel_Night/{name} 누락");
            }
        }

        [Test]
        public void Night_Panel_Sns_Objects_Match_F1_Coordinates()
        {
            AssertAnchoredPosition("SummaryText", new Vector2(0f, 84f));
            AssertAnchoredPosition("DaysText", new Vector2(0f, 66f));
            AssertAnchoredPosition("FollowerText", new Vector2(0f, 50f));
            AssertAnchoredPosition("SnsTitleText", new Vector2(0f, 32f));
            AssertAnchoredPosition("Button_Sns_PhotoFeed", new Vector2(-150f, 0f));
            AssertAnchoredPosition("Button_Sns_ShortForm", new Vector2(0f, 0f));
            AssertAnchoredPosition("Button_Sns_LocalBoard", new Vector2(150f, 0f));
            AssertAnchoredPosition("SnsInfoText", new Vector2(0f, -40f));
            AssertAnchoredPosition("StatusText", new Vector2(0f, -72f));
        }

        void AssertAnchoredPosition(string name, Vector2 expected)
        {
            var child = nightPanel.Find(name);
            Assert.IsNotNull(child, $"Panel_Night/{name} 누락");
            var rt = (RectTransform)child;
            Assert.AreEqual(expected.x, rt.anchoredPosition.x, 0.01f, $"{name} anchoredPosition.x");
            Assert.AreEqual(expected.y, rt.anchoredPosition.y, 0.01f, $"{name} anchoredPosition.y");
        }

        [Test]
        public void Sns_Campaign_Buttons_Have_Outline_Disabled_By_Default()
        {
            foreach (var name in new[] { "Button_Sns_PhotoFeed", "Button_Sns_ShortForm", "Button_Sns_LocalBoard" })
            {
                var button = nightPanel.Find(name);
                Assert.IsNotNull(button, $"{name} 누락");
                var outline = button.GetComponent<Outline>();
                Assert.IsNotNull(outline, $"{name} Outline 컴포넌트 누락 (F1 — 집행 완료 표시용)");
                Assert.IsFalse(outline.enabled, $"{name} outline 은 기본 비활성이어야 한다(집행 전)");
                Assert.IsNotNull(button.GetComponent<Button>(), $"{name} Button 컴포넌트 누락");
            }
        }

        [Test]
        public void Night_Panel_Has_Controller_With_Sns_Wiring()
        {
            var controller = nightPanel.GetComponent<NightPanelController>();
            Assert.IsNotNull(controller, "NightPanelController 누락");
        }

        [Test]
        public void Sns_Buttons_Have_Explicit_Horizontal_Navigation_Chain()
        {
            // F2: 픽쳐그램→숏핑→동네게시판→다음 날 ▶ 좌우 방향키 explicit navigation.
            var photoFeed = nightPanel.Find("Button_Sns_PhotoFeed").GetComponent<Button>();
            var shortForm = nightPanel.Find("Button_Sns_ShortForm").GetComponent<Button>();
            var localBoard = nightPanel.Find("Button_Sns_LocalBoard").GetComponent<Button>();

            Assert.AreEqual(Navigation.Mode.Explicit, photoFeed.navigation.mode, "픽쳐그램 explicit navigation");
            Assert.AreSame(shortForm, photoFeed.navigation.selectOnRight, "픽쳐그램 → 숏핑");
            Assert.AreSame(localBoard, shortForm.navigation.selectOnRight, "숏핑 → 동네게시판");
        }
    }
}
