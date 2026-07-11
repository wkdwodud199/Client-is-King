using System.Linq;
using ClientIsKing.Data;
using ClientIsKing.EditorTools;
using ClientIsKing.Managers;
using ClientIsKing.Service;
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
    /// task-110 (U6): 장르 선택 UI 멱등 생성·persistent listener 중복 0 을 추가 검증한다.
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

        // ── task-110 U6: 장르 선택 UI + genre catalog 주입 + 멱등성 ─────────

        [Test]
        public void Shop_Has_GenreSelectionModal_As_Last_Sibling_And_Active_Initially()
        {
            var scene = OpenSingle(SceneBuilder.ShopPath);
            var canvasGo = Root(scene, "Canvas");
            Assert.IsNotNull(canvasGo, "Canvas 누락");

            var modal = canvasGo.transform.Find("Panel_GenreSelection");
            Assert.IsNotNull(modal, "Panel_GenreSelection 누락");
            Assert.AreEqual(canvasGo.transform.childCount - 1, modal.GetSiblingIndex(),
                "장르 modal 은 canvas 의 마지막 자식이어야 한다 (렌더/raycast 최상단, design.md E3)");
            Assert.IsTrue(modal.gameObject.activeSelf, "미선택 상태에서는 modal 이 초기부터 노출되어야 한다");

            var genreBadge = canvasGo.transform.Find("GenreBadge");
            Assert.IsNotNull(genreBadge, "HUD GenreBadge 누락");
            Assert.IsNotNull(genreBadge.GetComponent<TMP_Text>(), "GenreBadge 는 TMP_Text 여야 한다");
        }

        [Test]
        public void GameManager_Bootstrap_Has_Sorted_Genre_Catalog_And_ServiceManager_Defs()
        {
            var scene = OpenSingle(SceneBuilder.ShopPath);
            var gameManagerGo = Root(scene, "GameManager");
            Assert.IsNotNull(gameManagerGo, "GameManager 오브젝트 누락");

            var gm = gameManagerGo.GetComponent<GameManager>();
            Assert.AreEqual(4, gm.GenreCatalog.Count, "장르 4종 주입");
            var ids = gm.GenreCatalog.Select(g => g.Id).ToList();
            var sortedIds = new System.Collections.Generic.List<string>(ids);
            sortedIds.Sort(System.StringComparer.Ordinal);
            CollectionAssert.AreEqual(sortedIds, ids, "genre catalog 는 ID ordinal 정렬이어야 한다");

            var service = gameManagerGo.GetComponent<ServiceManager>();
            Assert.AreEqual(6, service.RecipeDefs.Count, "recipe 6종 주입");
            Assert.AreEqual(4, service.CustomerDefs.Count, "customer 4종 주입");
        }

        [Test]
        public void Repeated_Apply_Is_Idempotent_In_Object_Count_And_Persistent_Listeners()
        {
            SceneBuilder.Apply();
            var firstScene = OpenSingle(SceneBuilder.ShopPath);
            var firstCanvas = Root(firstScene, "Canvas");
            int firstChildCount = CountAllDescendants(firstCanvas.transform);
            int firstAdvanceListeners = firstCanvas.transform.Find("AdvanceButton")
                .GetComponent<Button>().onClick.GetPersistentEventCount();

            SceneBuilder.Apply();
            var secondScene = OpenSingle(SceneBuilder.ShopPath);
            var secondCanvas = Root(secondScene, "Canvas");
            int secondChildCount = CountAllDescendants(secondCanvas.transform);
            int secondAdvanceListeners = secondCanvas.transform.Find("AdvanceButton")
                .GetComponent<Button>().onClick.GetPersistentEventCount();

            Assert.AreEqual(firstChildCount, secondChildCount, "재실행해도 canvas 하위 오브젝트 수가 같아야 한다 (멱등)");
            // SceneBuilder 는 코드로 버튼 리스너를 onClick.AddListener(런타임) 로만 등록하고
            // EditorInit 은 참조 주입만 한다 — persistent(직렬화) 리스너는 0개여야 중복 배선이 없다.
            Assert.AreEqual(0, firstAdvanceListeners, "AdvanceButton 은 persistent listener 없이 런타임 AddListener 만 사용해야 한다");
            Assert.AreEqual(0, secondAdvanceListeners, "재실행 후에도 persistent listener 는 0 이어야 한다 (중복 배선 없음)");
        }

        static int CountAllDescendants(Transform root)
        {
            int count = 0;
            foreach (Transform child in root)
            {
                count++;
                count += CountAllDescendants(child);
            }
            return count;
        }

        // ── task-111 U6: SNS catalog 주입 + Night SNS UI 산출물 + 멱등 ─────

        [Test]
        public void GameManager_Bootstrap_Has_Sorted_Sns_Catalog_On_Both_Scenes()
        {
            var shop = OpenSingle(SceneBuilder.ShopPath);
            var shopService = Root(shop, "GameManager").GetComponent<ServiceManager>();
            Assert.AreEqual(3, shopService.SnsCampaignDefs.Count, "SNS 캠페인 3종 주입 (Shop)");
            var shopIds = shopService.SnsCampaignDefs.Select(d => d.Id).ToList();
            CollectionAssert.AreEqual(new[] { "local_board", "photo_feed", "short_form" }, shopIds,
                "SNS catalog 는 ID ordinal 정렬이어야 한다 (Shop)");

            var mainMenu = OpenSingle(SceneBuilder.MainMenuPath);
            var mainMenuGameManagerGo = Root(mainMenu, "GameManager");
            Assert.IsNotNull(mainMenuGameManagerGo, "MainMenu GameManager 누락");
            var mainMenuService = mainMenuGameManagerGo.GetComponent<ServiceManager>();
            Assert.IsNotNull(mainMenuService, "MainMenu ServiceManager 누락");
            var mainMenuIds = mainMenuService.SnsCampaignDefs.Select(d => d.Id).ToList();
            CollectionAssert.AreEqual(shopIds, mainMenuIds, "MainMenu/Shop 양쪽에 동일 정렬 SNS catalog 주입");
        }

        [Test]
        public void Night_Panel_Has_Sns_Ui_Objects()
        {
            var scene = OpenSingle(SceneBuilder.ShopPath);
            var canvasGo = Root(scene, "Canvas");
            var nightPanel = canvasGo.transform.Find("Panel_Night");
            Assert.IsNotNull(nightPanel, "Panel_Night 누락");

            string[] names =
            {
                "FollowerText", "SnsTitleText",
                "Button_Sns_PhotoFeed", "Button_Sns_ShortForm", "Button_Sns_LocalBoard", "SnsInfoText",
            };
            foreach (var name in names)
            {
                Assert.IsNotNull(nightPanel.Find(name), $"Panel_Night/{name} 누락");
            }
        }

        [Test]
        public void Settlement_Panel_Has_Sns_Effect_Text()
        {
            var scene = OpenSingle(SceneBuilder.ShopPath);
            var canvasGo = Root(scene, "Canvas");
            var settlementPanel = canvasGo.transform.Find("Panel_Settlement");
            Assert.IsNotNull(settlementPanel, "Panel_Settlement 누락");
            Assert.IsNotNull(settlementPanel.Find("SnsEffectText"), "Panel_Settlement/SnsEffectText 누락");
        }

        [Test]
        public void Repeated_Apply_Is_Idempotent_For_Sns_Catalog_And_Night_Object_Count()
        {
            // 각 scene 오픈 직후 필요한 값을 전부 읽어둔다 — 다음 OpenScene 이 이전 Transform 참조를
            // 무효화하므로(MissingReferenceException, task-110 씬 재로드 함정) 참조를 넘겨 들고 있지 않는다.
            SceneBuilder.Apply();
            var firstScene = OpenSingle(SceneBuilder.ShopPath);
            var firstService = Root(firstScene, "GameManager").GetComponent<ServiceManager>();
            int firstSnsCount = firstService.SnsCampaignDefs.Count;
            var firstNightPanel = Root(firstScene, "Canvas").transform.Find("Panel_Night");
            int firstNightChildCount = CountAllDescendants(firstNightPanel);
            int firstListeners = firstNightPanel.Find("Button_Sns_PhotoFeed").GetComponent<Button>()
                .onClick.GetPersistentEventCount();

            SceneBuilder.Apply();
            var secondScene = OpenSingle(SceneBuilder.ShopPath);
            var secondService = Root(secondScene, "GameManager").GetComponent<ServiceManager>();
            int secondSnsCount = secondService.SnsCampaignDefs.Count;
            var secondNightPanel = Root(secondScene, "Canvas").transform.Find("Panel_Night");
            int secondNightChildCount = CountAllDescendants(secondNightPanel);
            int secondListeners = secondNightPanel.Find("Button_Sns_PhotoFeed").GetComponent<Button>()
                .onClick.GetPersistentEventCount();

            Assert.AreEqual(firstSnsCount, secondSnsCount, "재실행해도 SNS catalog 수가 같아야 한다 (멱등)");
            Assert.AreEqual(firstNightChildCount, secondNightChildCount, "재실행해도 Night 패널 하위 오브젝트 수가 같아야 한다 (멱등)");
            Assert.AreEqual(0, firstListeners, "SNS 버튼도 persistent listener 없이 런타임 AddListener 만 사용해야 한다");
            Assert.AreEqual(0, secondListeners, "재실행 후에도 SNS 버튼 persistent listener 는 0 이어야 한다");
        }

        // ── task-112 U7: 이벤트 catalog 4종 양씬 동일 주입 + 오브젝트 멱등 ───

        [Test]
        public void GameManager_Bootstrap_Has_Sorted_Event_Catalog_On_Both_Scenes()
        {
            var shop = OpenSingle(SceneBuilder.ShopPath);
            var shopGm = Root(shop, "GameManager").GetComponent<GameManager>();
            Assert.AreEqual(4, shopGm.EventCatalog.Count, "이벤트 4종 주입 (Shop)");
            var shopIds = shopGm.EventCatalog.Select(d => d.Id).ToList();
            CollectionAssert.AreEqual(
                new[] { "group_customers", "hygiene_inspection", "ingredient_price_surge", "rent_increase" },
                shopIds, "이벤트 catalog 는 ID ordinal 정렬이어야 한다 (Shop)");

            var mainMenu = OpenSingle(SceneBuilder.MainMenuPath);
            var mainMenuGm = Root(mainMenu, "GameManager").GetComponent<GameManager>();
            Assert.IsNotNull(mainMenuGm, "MainMenu GameManager 누락");
            var mainMenuIds = mainMenuGm.EventCatalog.Select(d => d.Id).ToList();
            CollectionAssert.AreEqual(shopIds, mainMenuIds, "MainMenu/Shop 양쪽에 동일 정렬 이벤트 catalog 주입");
        }

        [Test]
        public void Night_Panel_Has_EventNoticeText_And_Settlement_Has_EventEffectText()
        {
            var scene = OpenSingle(SceneBuilder.ShopPath);
            var canvasGo = Root(scene, "Canvas");
            var nightPanel = canvasGo.transform.Find("Panel_Night");
            Assert.IsNotNull(nightPanel.Find("EventNoticeText"), "Panel_Night/EventNoticeText 누락");

            var settlementPanel = canvasGo.transform.Find("Panel_Settlement");
            Assert.IsNotNull(settlementPanel.Find("EventEffectText"), "Panel_Settlement/EventEffectText 누락");
        }

        [Test]
        public void Repeated_Apply_Is_Idempotent_For_Event_Catalog_And_Night_Object_Count()
        {
            SceneBuilder.Apply();
            var firstScene = OpenSingle(SceneBuilder.ShopPath);
            var firstGm = Root(firstScene, "GameManager").GetComponent<GameManager>();
            int firstEventCount = firstGm.EventCatalog.Count;
            var firstNightPanel = Root(firstScene, "Canvas").transform.Find("Panel_Night");
            int firstNightChildCount = CountAllDescendants(firstNightPanel);

            SceneBuilder.Apply();
            var secondScene = OpenSingle(SceneBuilder.ShopPath);
            var secondGm = Root(secondScene, "GameManager").GetComponent<GameManager>();
            int secondEventCount = secondGm.EventCatalog.Count;
            var secondNightPanel = Root(secondScene, "Canvas").transform.Find("Panel_Night");
            int secondNightChildCount = CountAllDescendants(secondNightPanel);

            Assert.AreEqual(firstEventCount, secondEventCount, "재실행해도 이벤트 catalog 수가 같아야 한다 (멱등)");
            Assert.AreEqual(firstNightChildCount, secondNightChildCount, "재실행해도 Night 패널 하위 오브젝트 수가 같아야 한다 (멱등)");
        }
    }
}
