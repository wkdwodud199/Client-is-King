using System.Linq;
using ClientIsKing.EditorTools;
using ClientIsKing.Managers;
using ClientIsKing.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-104: SceneBuilder 산출물 검증 — 씬 존재·Build Settings 순서·필수 컴포넌트·씬 하드캡.
    /// </summary>
    public class SceneBuilderTests
    {
        [OneTimeSetUp]
        public void BuildScenesOnce()
        {
            SceneBuilder.Apply();
        }

        static Scene OpenSingle(string path)
        {
            return EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }

        static GameObject Root(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(go => go.name == name);
        }

        [Test]
        public void Scene_Assets_Exist()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneBuilder.MainMenuPath));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneBuilder.ShopPath));
        }

        [Test]
        public void BuildSettings_Contains_Exactly_MainMenu_Then_Shop()
        {
            var scenes = EditorBuildSettings.scenes;
            Assert.AreEqual(2, scenes.Length, "Build Settings 씬은 정확히 2개");
            Assert.AreEqual(SceneBuilder.MainMenuPath, scenes[0].path);
            Assert.AreEqual(SceneBuilder.ShopPath, scenes[1].path);
            Assert.IsTrue(scenes[0].enabled && scenes[1].enabled);
        }

        [Test]
        public void Assets_Contain_Exactly_Two_Scenes_HardCap()
        {
            var guids = AssetDatabase.FindAssets("t:SceneAsset", new[] { "Assets" });
            Assert.AreEqual(2, guids.Length, "씬 하드캡: Assets 아래 씬은 MainMenu/Shop 2개만 (demo-scope.md)");
        }

        [Test]
        public void MainMenu_Has_Required_Objects()
        {
            var scene = OpenSingle(SceneBuilder.MainMenuPath);

            var cameraGo = Root(scene, "Main Camera");
            Assert.IsNotNull(cameraGo, "Main Camera 누락");
            Assert.AreEqual("MainCamera", cameraGo.tag);
            Assert.IsNotNull(cameraGo.GetComponent<Camera>());

            Assert.IsNotNull(Root(scene, "GameManager")?.GetComponent<GameManager>(), "GameManager 부트스트랩 누락");
            Assert.IsNotNull(Root(scene, "EventSystem")?.GetComponent<EventSystem>(), "EventSystem 누락");

            var canvasGo = Root(scene, "Canvas");
            Assert.IsNotNull(canvasGo?.GetComponent<Canvas>(), "Canvas 누락");
            Assert.IsNotNull(canvasGo.GetComponent<MainMenuController>(), "MainMenuController 누락");
            Assert.IsNotNull(canvasGo.transform.Find("StartButton")?.GetComponent<Button>(), "StartButton 누락");
            Assert.IsNotNull(canvasGo.transform.Find("Title")?.GetComponent<TMP_Text>(), "Title 누락");
        }

        [Test]
        public void Shop_Has_PixelPerfect_Camera_With_Brief_Standard()
        {
            var scene = OpenSingle(SceneBuilder.ShopPath);

            var cameraGo = Root(scene, "Main Camera");
            Assert.IsNotNull(cameraGo, "Main Camera 누락");
            var ppc = cameraGo.GetComponent<PixelPerfectCamera>();
            Assert.IsNotNull(ppc, "URP PixelPerfectCamera 누락");
            Assert.AreEqual(32, ppc.assetsPPU, "픽셀 표준: PPU 32");
            Assert.AreEqual(640, ppc.refResolutionX, "기준 해상도 640");
            Assert.AreEqual(360, ppc.refResolutionY, "기준 해상도 360");
        }

        [Test]
        public void Shop_Has_Hud_And_Four_Phase_Panels()
        {
            var scene = OpenSingle(SceneBuilder.ShopPath);

            Assert.IsNotNull(Root(scene, "GameManager")?.GetComponent<GameManager>(), "GameManager 누락");
            Assert.IsNotNull(Root(scene, "EventSystem")?.GetComponent<EventSystem>(), "EventSystem 누락");

            var canvasGo = Root(scene, "Canvas");
            Assert.IsNotNull(canvasGo?.GetComponent<Canvas>(), "Canvas 누락");
            Assert.IsNotNull(canvasGo.GetComponent<PhaseHudController>(), "PhaseHudController 누락");
            Assert.IsNotNull(canvasGo.transform.Find("DayPhaseText")?.GetComponent<TMP_Text>(), "DayPhaseText 누락");
            Assert.IsNotNull(canvasGo.transform.Find("AdvanceButton")?.GetComponent<Button>(), "AdvanceButton 누락");

            // transform.Find 는 비활성 자식도 찾는다 — 초기 상태(Market 만 활성)와 무관하게 4개 존재 검증.
            string[] panels = { "Panel_Market", "Panel_Service", "Panel_Settlement", "Panel_Night" };
            foreach (var name in panels)
            {
                Assert.IsNotNull(canvasGo.transform.Find(name), $"{name} 누락");
            }
            Assert.IsTrue(canvasGo.transform.Find("Panel_Market").gameObject.activeSelf, "초기 활성 패널은 Market");
            Assert.IsFalse(canvasGo.transform.Find("Panel_Service").gameObject.activeSelf);
        }
    }
}
