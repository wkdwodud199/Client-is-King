using System.Linq;
using ClientIsKing.Data;
using ClientIsKing.EditorTools;
using ClientIsKing.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-105: SceneBuilder 가 생성한 Shop 씬 장보기 UI 산출물 검증 — 컨트롤 존재 + 재료 주입.
    /// task-110 (U6): 장르 선택 modal 정적 산출물 커버리지 추가 — genre 선택/확정 흐름은 상태를 공유하지
    /// 않도록 <see cref="MarketPanelGenreFlowTests"/> 별도 fixture 로 분리한다 (fixture 간 씬 재로드가
    /// [OneTimeSetUp] 캐시 Transform 을 무효화하는 문제 회피).
    /// </summary>
    public class MarketPanelSceneTests
    {
        Transform canvas;
        Transform marketPanel;
        MarketPanelController controller;

        [OneTimeSetUp]
        public void BuildAndOpen()
        {
            SceneBuilder.Apply();
            var scene = EditorSceneManager.OpenScene(SceneBuilder.ShopPath, OpenSceneMode.Single);
            var canvasGo = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "Canvas");
            Assert.IsNotNull(canvasGo, "Canvas 누락");
            canvas = canvasGo.transform;
            marketPanel = canvas.Find("Panel_Market");
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

        // ── task-110 U6: 장르 선택 modal 정적 산출물 (상태 비공유) ──────────────

        [Test]
        public void Panel_GenreSelection_Has_All_Required_Objects()
        {
            var modal = canvas.Find("Panel_GenreSelection");
            Assert.IsNotNull(modal, "Panel_GenreSelection 누락");

            string[] children =
            {
                "Title", "Button_Gukbap", "Button_Bunsik", "Button_Noodles", "Button_Generalist",
                "Detail", "ConfirmButton", "HelperText",
            };
            foreach (var name in children)
            {
                Assert.IsNotNull(modal.Find(name), $"Panel_GenreSelection/{name} 누락");
            }
            var detail = modal.Find("Detail");
            string[] detailChildren = { "DetailName", "DetailBody", "DetailNumbers" };
            foreach (var name in detailChildren)
            {
                Assert.IsNotNull(detail.Find(name), $"Detail/{name} 누락");
            }
            foreach (var buttonName in new[] { "Button_Gukbap", "Button_Bunsik", "Button_Noodles", "Button_Generalist" })
            {
                Assert.IsNotNull(modal.Find(buttonName).Find("Icon"), $"{buttonName}/Icon 누락");
                Assert.IsNotNull(modal.Find(buttonName).GetComponent<Outline>(), $"{buttonName} Outline 누락");
            }
        }

        [Test]
        public void Market_Panel_Has_GenreDetailButton_Hidden_By_Default()
        {
            var detailButton = marketPanel.Find("GenreDetailButton");
            Assert.IsNotNull(detailButton, "Panel_Market/GenreDetailButton 누락");
            Assert.IsFalse(detailButton.gameObject.activeSelf, "확정 전에는 상세 보기 버튼이 숨겨져야 한다");
        }
    }

    /// <summary>
    /// task-110 U6: 장르 선택/확정 UI 흐름 — 구매 잠금·forecast·확정 후 접힘·specialist 재료 필터.
    /// 각 테스트가 씬을 새로 열어(own [SetUp]) GameState/컨트롤러 상태를 공유하지 않는다.
    /// </summary>
    public class MarketPanelGenreFlowTests
    {
        Transform canvas;
        Transform marketPanel;

        [SetUp]
        public void OpenFreshShop()
        {
            var gameManagerGo = TestSceneSupport.OpenShopSceneWithLiveSingletons();
            var canvasGo = gameManagerGo.scene.GetRootGameObjects().FirstOrDefault(go => go.name == "Canvas");
            Assert.IsNotNull(canvasGo, "Canvas 누락");
            canvas = canvasGo.transform;
            marketPanel = canvas.Find("Panel_Market");

            var gm = gameManagerGo.GetComponent<ClientIsKing.Managers.GameManager>();
            gm.StartNewGame();

            // 배치 EditMode 에서는 OpenScene 이 MonoBehaviour.Start()/OnEnable() 을 동기 호출한다는
            // 보장이 없다(group1/U6 공통 함정). Start() 로 kinds 목록을 채운 뒤 OnEnable 을 직접 호출해
            // onClick 리스너 등록까지 확실히 한다 (SetActive 토글도 동기 호출을 보장하지 않는다).
            var marketController = marketPanel.GetComponent<MarketPanelController>();
            TestSceneSupport.ForceStart(marketController);
            TestSceneSupport.ForceOnEnable(marketController);
        }

        [Test]
        public void Buy_Button_Is_Locked_Before_Genre_Confirmed_And_Unlocked_After()
        {
            var gm = ClientIsKing.Managers.GameManager.Instance;
            var buyButton = marketPanel.Find("BuyButton").GetComponent<Button>();
            Assert.IsFalse(buyButton.interactable, "미선택 상태에서 구매 버튼은 비활성이어야 한다 (E3)");

            var modal = canvas.Find("Panel_GenreSelection");
            modal.Find("Button_Generalist").GetComponent<Button>().onClick.Invoke();
            modal.Find("ConfirmButton").GetComponent<Button>().onClick.Invoke();

            Assert.AreEqual("generalist", gm.State.selectedGenreId, "확정 후 selectedGenreId 반영");
            Assert.IsTrue(buyButton.interactable, "확정 후 구매 버튼은 활성화되어야 한다");
        }

        [Test]
        public void Confirming_Genre_Shows_Forecast_And_Collapses_Modal()
        {
            var gm = ClientIsKing.Managers.GameManager.Instance;
            var modal = canvas.Find("Panel_GenreSelection");
            var detailNumbers = modal.Find("Detail").Find("DetailNumbers").GetComponent<TMPro.TMP_Text>();

            modal.Find("Button_Gukbap").GetComponent<Button>().onClick.Invoke();
            Assert.IsNotEmpty(detailNumbers.text, "국밥 후보 선택 시 비교/forecast 문구가 채워져야 한다");
            Assert.IsTrue(detailNumbers.text.Contains("주문 4건"), "국밥 비교 문구: 주문 4건");

            modal.Find("ConfirmButton").GetComponent<Button>().onClick.Invoke();

            Assert.AreEqual("gukbap", gm.State.selectedGenreId);
            Assert.IsFalse(modal.gameObject.activeSelf, "확정 후 modal 은 접혀야 한다 (상세 보기 토글 전까지)");

            var detailButton = marketPanel.Find("GenreDetailButton");
            Assert.IsTrue(detailButton.gameObject.activeSelf, "확정 후 상세 보기 버튼이 노출되어야 한다");
        }

        [Test]
        public void Specialist_Confirmed_Market_Shows_Only_Matching_Recipe_Ingredients()
        {
            var gm = ClientIsKing.Managers.GameManager.Instance;
            var modal = canvas.Find("Panel_GenreSelection");
            modal.Find("Button_Bunsik").GetComponent<Button>().onClick.Invoke();
            modal.Find("ConfirmButton").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual("bunsik", gm.State.selectedGenreId);

            // 분식 레시피(tteokbokki/gimbap) 요구 재료만 순환 가능해야 한다 — Beef/Pork/Noodle 은 국밥/면류 전용.
            var ingredientLabel = marketPanel.Find("IngredientLabel").GetComponent<TMPro.TMP_Text>();
            var nextButton = marketPanel.Find("IngredientNext").GetComponent<Button>();
            var seenLabels = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < 12; i++)
            {
                seenLabels.Add(ingredientLabel.text);
                nextButton.onClick.Invoke();
            }
            Assert.IsFalse(seenLabels.Contains("돼지고기"), "분식 확정 시 돼지고기(국밥 전용)는 순환 목록에 없어야 한다");
            Assert.IsFalse(seenLabels.Contains("소고기"), "분식 확정 시 소고기(국밥 전용)는 순환 목록에 없어야 한다");
            Assert.IsFalse(seenLabels.Contains("소면"), "분식 확정 시 소면(면류 전용)은 순환 목록에 없어야 한다");
        }
    }
}
