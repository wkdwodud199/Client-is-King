using System.Linq;
using ClientIsKing.EditorTools;
using ClientIsKing.Presentation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-108: SceneBuilder 가 생성한 Shop 무대 산출물 — 오브젝트/참조/카탈로그/오버레이/레이아웃 검증.
    /// </summary>
    public class ShopPresentationSceneTests
    {
        Transform canvas;
        Transform stage;
        ShopPresentationController controller;

        [OneTimeSetUp]
        public void BuildAndOpen()
        {
            SceneBuilder.Apply();
            var scene = EditorSceneManager.OpenScene(SceneBuilder.ShopPath, OpenSceneMode.Single);
            var canvasGo = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "Canvas");
            Assert.IsNotNull(canvasGo, "Canvas 누락");
            canvas = canvasGo.transform;
            stage = canvas.Find("ShopStage");
            Assert.IsNotNull(stage, "ShopStage 누락");
            controller = stage.GetComponent<ShopPresentationController>();
        }

        [Test]
        public void Stage_Has_All_Required_Objects()
        {
            string[] children =
            {
                "Stage_Backdrop", "Stage_CustomerArea", "Stage_Counter",
                "CustomerSprite", "OrderLabel", "FoodIcon", "CashPopupText", "SettlementPulseText",
            };
            foreach (var name in children)
            {
                Assert.IsNotNull(stage.Find(name), $"{name} 누락");
            }
            Assert.IsNotNull(canvas.Find("NightOverlay"), "NightOverlay 누락");
            Assert.IsNotNull(controller, "ShopPresentationController 누락");
        }

        [Test]
        public void Catalogs_Injected_With_Valid_Sprites()
        {
            Assert.IsNotNull(controller);
            Assert.AreEqual(4, controller.CustomerSprites.Count, "고객 catalog 4종");
            foreach (var entry in controller.CustomerSprites)
            {
                Assert.IsFalse(string.IsNullOrEmpty(entry.customerId));
                Assert.IsNotNull(entry.sprite, $"{entry.customerId}: sprite 누락");
            }
            Assert.AreEqual(6, controller.RecipeSprites.Count, "레시피 catalog 6종");
            foreach (var entry in controller.RecipeSprites)
            {
                Assert.IsFalse(string.IsNullOrEmpty(entry.recipeId));
                Assert.IsNotNull(entry.sprite, $"{entry.recipeId}: sprite 누락");
            }
        }

        [Test]
        public void WalkFrames_Injected_For_All_Customers()
        {
            Assert.IsNotNull(controller);
            foreach (var entry in controller.CustomerSprites)
            {
                Assert.IsNotNull(entry.walkFrames, $"{entry.customerId}: walkFrames 누락");
                Assert.AreEqual(4, entry.walkFrames.Length, $"{entry.customerId}: 우향 walk 4프레임 (task-109)");
                foreach (var frame in entry.walkFrames)
                {
                    Assert.IsNotNull(frame, $"{entry.customerId}: walk 프레임 스프라이트 누락");
                }
            }
        }

        [Test]
        public void Empty_WalkFrames_Falls_Back_To_Single_Sprite_Without_Exception()
        {
            Assert.IsNotNull(controller);
            // walkFrames 가 비어 있어도 단일 sprite 로 폴백해 예외가 나면 안 된다 (EditMode 즉시 스냅 경로).
            var args = new ServicePresentationEventArgs(
                true, 1, 1, 5, "student", "pork_gukbap", 2, false, false, 0, "학생");
            Assert.DoesNotThrow(() => controller.HandleOrderPresented(args),
                "walkFrames 유무와 무관하게 입장 처리는 예외 없이 진행");
        }

        [Test]
        public void Stage_Has_Tiles_And_Decorative_Props()
        {
            // Kenney 타일 배경/카운터가 스프라이트로 교체됐고, 순수 장식 소품이 배치됐다 (task-109).
            var backdrop = stage.Find("Stage_Backdrop").GetComponent<Image>();
            var counter = stage.Find("Stage_Counter").GetComponent<Image>();
            Assert.IsNotNull(backdrop.sprite, "Stage_Backdrop 타일 스프라이트 누락");
            Assert.IsNotNull(counter.sprite, "Stage_Counter 타일 스프라이트 누락");

            int props = 0;
            foreach (var name in new[] { "Prop_BowlLeft", "Prop_BowlMid", "Prop_BowlRight" })
            {
                var prop = stage.Find(name);
                if (prop != null)
                {
                    Assert.IsFalse(prop.GetComponent<Image>().raycastTarget, $"{name}: 장식은 클릭을 막지 않는다");
                    props++;
                }
            }
            Assert.GreaterOrEqual(props, 2, "순수 장식 소품 2~3개");
        }

        [Test]
        public void NightOverlay_Starts_Transparent_And_Never_Blocks_Clicks()
        {
            var overlay = canvas.Find("NightOverlay").GetComponent<Image>();
            Assert.IsNotNull(overlay);
            Assert.AreEqual(0f, overlay.color.a, 0.001f, "초기 alpha 0");
            Assert.IsFalse(overlay.raycastTarget, "오버레이는 클릭을 막지 않는다 (설계 153행)");
        }

        [Test]
        public void Stage_Images_Do_Not_Block_Clicks()
        {
            Assert.IsFalse(stage.Find("Stage_Backdrop").GetComponent<Image>().raycastTarget);
            Assert.IsFalse(stage.Find("Stage_Counter").GetComponent<Image>().raycastTarget);
            Assert.IsFalse(stage.Find("CustomerSprite").GetComponent<Image>().raycastTarget);
        }

        [Test]
        public void Panels_Do_Not_Cover_Stage_Band()
        {
            // 640×360 기준 — 패널 상단(y)이 무대 backdrop 하단보다 위로 올라오지 않는다 (설계 155행)
            var backdrop = (RectTransform)stage.Find("Stage_Backdrop");
            float backdropBottom = backdrop.anchoredPosition.y - backdrop.sizeDelta.y * 0.5f;

            foreach (var panelName in new[] { "Panel_Market", "Panel_Service", "Panel_Settlement", "Panel_Night" })
            {
                var panel = (RectTransform)canvas.Find(panelName);
                Assert.IsNotNull(panel, $"{panelName} 누락");
                float panelTop = panel.anchoredPosition.y + panel.sizeDelta.y * 0.5f;
                Assert.LessOrEqual(panelTop, backdropBottom + 0.5f,
                    $"{panelName} 상단({panelTop})이 무대 하단({backdropBottom})을 침범");
            }
            Assert.IsTrue(canvas.Find("Panel_Market").gameObject.activeSelf, "초기 활성 패널은 Market");
        }

        [Test]
        public void Unknown_Ids_Fall_Back_Without_Exception()
        {
            Assert.IsNotNull(controller);
            var args = new ServicePresentationEventArgs(
                true, 1, 1, 5, "unknown_customer", "unknown_recipe", 2, false, false, 0, "?");

            Assert.DoesNotThrow(() => controller.HandleOrderPresented(args), "미지 id 는 예외 없이 복구 (설계 161행)");

            var customerImage = stage.Find("CustomerSprite").GetComponent<Image>();
            Assert.IsFalse(customerImage.enabled, "미지 customerId → 이미지 표시 안 함 (fallback)");

            Assert.DoesNotThrow(() => controller.HandleOutcomeResolved(
                new ServicePresentationEventArgs(true, 1, 0, 5, "unknown_customer", "unknown_recipe", 2, true, false, 100, "")));
        }
    }
}
