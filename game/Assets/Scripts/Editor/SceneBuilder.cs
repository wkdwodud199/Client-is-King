using System.Collections.Generic;
using System.Linq;
using ClientIsKing.Data;
using ClientIsKing.Economy;
using ClientIsKing.Inventory;
using ClientIsKing.Managers;
using ClientIsKing.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ClientIsKing.EditorTools
{
    /// <summary>
    /// task-104: 씬 2종(MainMenu/Shop)과 기본 UI 를 코드로 저작하는 배치 진입점 (브리프 규약 —
    /// 수동 에디터 조작 금지, 씬을 고치려면 이 빌더를 수정하고 재실행한다).
    ///
    /// 실행(리포 루트 기준):
    ///   Unity.exe -batchmode -quit -nographics -projectPath game
    ///     -executeMethod ClientIsKing.EditorTools.SceneBuilder.Apply -logFile [log]
    ///
    /// 멱등 규약: 이 시점의 두 씬은 전체가 빌더 소유이므로 매 실행 빈 씬에서 전체 재생성 후
    /// 같은 경로에 저장한다 (.meta 유지 → GUID 안정).
    /// </summary>
    public static class SceneBuilder
    {
        public const string ScenesDir = "Assets/Scenes";
        public const string MainMenuPath = ScenesDir + "/MainMenu.unity";
        public const string ShopPath = ScenesDir + "/Shop.unity";

        static readonly Vector2 ReferenceResolution = new Vector2(640f, 360f);

        public static void Apply()
        {
            if (!AssetDatabase.IsValidFolder(ScenesDir))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            BuildMainMenu();
            BuildShop();
            ApplyBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SceneBuilder] scenes built (MainMenu, Shop) + build settings locked");
        }

        // ── MainMenu ────────────────────────────────────────────────────────
        static void BuildMainMenu()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateGameManager();
            CreateCamera(pixelPerfect: false);
            CreateEventSystem();
            var canvasGo = CreateCanvas();

            CreateText(canvasGo.transform, "Title", "Client is King", 48f,
                new Vector2(0f, 60f), new Vector2(520f, 80f));
            var startButton = CreateButton(canvasGo.transform, "StartButton", "게임 시작",
                new Vector2(0f, -50f), new Vector2(200f, 44f));

            var controller = canvasGo.AddComponent<MainMenuController>();
            controller.EditorInit(startButton);

            SaveScene(scene, MainMenuPath);
        }

        // ── Shop ────────────────────────────────────────────────────────────
        static void BuildShop()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateGameManager();
            CreateCamera(pixelPerfect: true);
            CreateEventSystem();
            var canvasGo = CreateCanvas();

            var dayPhaseText = CreateText(canvasGo.transform, "DayPhaseText", "Day 1 — 장보기", 24f,
                new Vector2(-160f, 150f), new Vector2(300f, 40f));
            // Market 패널(task-105)이 하단을 차지하므로 진행 버튼은 우상단으로 (겹침 방지).
            var advanceButton = CreateButton(canvasGo.transform, "AdvanceButton", "다음 단계 ▶",
                new Vector2(250f, 150f), new Vector2(140f, 36f));

            // phase 별 패널 — Market 은 task-105 실 장보기 UI, 나머지는 task-106+ 가 교체한다.
            var marketPanel = BuildMarketPanel(canvasGo.transform);
            var servicePanel = CreatePanel(canvasGo.transform, "Panel_Service", "영업 (task-106)",
                new Color(0.45f, 0.35f, 0.20f, 0.85f));
            var settlementPanel = CreatePanel(canvasGo.transform, "Panel_Settlement", "정산 (task-107)",
                new Color(0.30f, 0.30f, 0.50f, 0.85f));
            var nightPanel = CreatePanel(canvasGo.transform, "Panel_Night", "밤 — SNS/저장 (task-109/111)",
                new Color(0.15f, 0.15f, 0.25f, 0.85f));

            // 초기 표시는 Market — 런타임에서는 PhaseHudController 가 상태에 맞춰 토글한다.
            marketPanel.SetActive(true);
            servicePanel.SetActive(false);
            settlementPanel.SetActive(false);
            nightPanel.SetActive(false);

            var hud = canvasGo.AddComponent<PhaseHudController>();
            hud.EditorInit(dayPhaseText, advanceButton, marketPanel, servicePanel, settlementPanel, nightPanel);

            SaveScene(scene, ShopPath);
        }

        // ── Build Settings (씬 하드캡 2 고정) ───────────────────────────────
        static void ApplyBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuPath, true),
                new EditorBuildSettingsScene(ShopPath, true),
            };
        }

        // ── 공통 생성 헬퍼 ──────────────────────────────────────────────────
        static void CreateGameManager()
        {
            // 두 씬 모두 부트스트랩 포함 — 어느 씬에서 시작해도 동작 (중복은 Awake 가드가 제거).
            // 경제/인벤토리 매니저(task-105)는 같은 GO 에 탑재 — DontDestroyOnLoad 를 함께 탄다.
            var go = new GameObject("GameManager", typeof(GameManager));
            go.AddComponent<EconomyManager>();
            go.AddComponent<InventoryManager>();
        }

        static void CreateCamera(bool pixelPerfect)
        {
            var go = new GameObject("Main Camera", typeof(Camera));
            go.tag = "MainCamera";
            var cam = go.GetComponent<Camera>();
            cam.orthographic = true;
            // 640x360 @ PPU 32 → 세로 절반 = 360 / (2*32) = 5.625 (PixelPerfectCamera 가 런타임 보정)
            cam.orthographicSize = 5.625f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.10f, 1f);
            cam.GetUniversalAdditionalCameraData(); // URP 추가 데이터를 씬에 직렬화

            if (pixelPerfect)
            {
                // 픽셀 표준 (브리프): PPU 32, 기준 해상도 640x360 — URP 내장 컴포넌트 사용 (task-102 결정)
                var ppc = go.AddComponent<PixelPerfectCamera>();
                ppc.assetsPPU = 32;
                ppc.refResolutionX = 640;
                ppc.refResolutionY = 360;
            }
        }

        static void CreateEventSystem()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        static GameObject CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            return go;
        }

        static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static TMP_Text CreateText(Transform parent, string name, string text, float fontSize,
            Vector2 anchoredPos, Vector2 size)
        {
            var go = CreateUIObject(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false; // 라벨이 버튼/패널 클릭을 가로채지 않게 (task-105)
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return tmp;
        }

        static Button CreateButton(Transform parent, string name, string label,
            Vector2 anchoredPos, Vector2 size)
        {
            var go = CreateUIObject(name, parent);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.25f, 0.45f, 0.85f, 1f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            CreateText(go.transform, "Label", label, 18f, Vector2.zero, size);
            return button;
        }

        static GameObject CreatePanel(Transform parent, string name, string label, Color color)
        {
            var go = CreateUIObject(name, parent);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -20f);
            rt.sizeDelta = new Vector2(420f, 160f);
            var img = go.AddComponent<Image>();
            img.color = color;
            CreateText(go.transform, "Label", label, 22f, Vector2.zero, new Vector2(400f, 60f));
            return go;
        }

        // ── Market 장보기 UI (task-105) ─────────────────────────────────────
        static GameObject BuildMarketPanel(Transform parent)
        {
            var panel = CreateUIObject("Panel_Market", parent);
            var rt = (RectTransform)panel.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -22f);
            rt.sizeDelta = new Vector2(480f, 300f);
            panel.AddComponent<Image>().color = new Color(0.20f, 0.45f, 0.30f, 0.85f);

            var cashText = CreateText(panel.transform, "CashText", "자금 30,000원", 20f,
                new Vector2(0f, 118f), new Vector2(440f, 28f));

            var ingredientPrev = CreateButton(panel.transform, "IngredientPrev", "◀",
                new Vector2(-190f, 78f), new Vector2(36f, 32f));
            var ingredientLabel = CreateText(panel.transform, "IngredientLabel", "쌀", 20f,
                new Vector2(0f, 78f), new Vector2(280f, 32f));
            var ingredientNext = CreateButton(panel.transform, "IngredientNext", "▶",
                new Vector2(190f, 78f), new Vector2(36f, 32f));

            var gradeButton = CreateButton(panel.transform, "GradeButton", "등급: C급",
                new Vector2(0f, 38f), new Vector2(160f, 32f));
            var gradeLabel = gradeButton.GetComponentInChildren<TMP_Text>();

            var quantityMinus = CreateButton(panel.transform, "QuantityMinus", "−",
                new Vector2(-80f, -2f), new Vector2(36f, 32f));
            var quantityText = CreateText(panel.transform, "QuantityText", "1", 20f,
                new Vector2(0f, -2f), new Vector2(100f, 32f));
            var quantityPlus = CreateButton(panel.transform, "QuantityPlus", "＋",
                new Vector2(80f, -2f), new Vector2(36f, 32f));

            var costText = CreateText(panel.transform, "CostText", "예상 비용 300원", 18f,
                new Vector2(0f, -38f), new Vector2(440f, 26f));
            var ownedText = CreateText(panel.transform, "OwnedText", "보유 0개", 16f,
                new Vector2(0f, -64f), new Vector2(440f, 24f));

            var buyButton = CreateButton(panel.transform, "BuyButton", "구매",
                new Vector2(0f, -100f), new Vector2(180f, 36f));
            var messageText = CreateText(panel.transform, "MessageText", "", 14f,
                new Vector2(0f, -134f), new Vector2(460f, 24f));

            var controller = panel.AddComponent<MarketPanelController>();
            controller.EditorInit(
                cashText,
                ingredientPrev, ingredientNext, ingredientLabel,
                gradeButton, gradeLabel,
                quantityMinus, quantityPlus, quantityText,
                costText, ownedText, buyButton, messageText,
                LoadIngredientDefs());
            return panel;
        }

        /// <summary>시드 IngredientDef 18종을 id 순으로 로드해 주입한다 (Resources 미사용 — 설계 8단계).</summary>
        static List<IngredientDef> LoadIngredientDefs()
        {
            return AssetDatabase.FindAssets("t:IngredientDef", new[] { "Assets/Data/Definitions/Ingredients" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<IngredientDef>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(def => def != null)
                .OrderBy(def => def.Id, System.StringComparer.Ordinal)
                .ToList();
        }

        static void SaveScene(Scene scene, string path)
        {
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new System.InvalidOperationException($"[SceneBuilder] 씬 저장 실패: {path}");
            }
        }
    }
}
