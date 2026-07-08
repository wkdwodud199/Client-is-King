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
                new Vector2(0f, 150f), new Vector2(400f, 40f));
            var advanceButton = CreateButton(canvasGo.transform, "AdvanceButton", "다음 단계 ▶",
                new Vector2(240f, -150f), new Vector2(160f, 40f));

            // phase 별 placeholder 패널 — task-105~107 이 실제 UI 로 교체한다.
            var marketPanel = CreatePanel(canvasGo.transform, "Panel_Market", "장보기 (task-105)",
                new Color(0.20f, 0.45f, 0.30f, 0.85f));
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
            new GameObject("GameManager", typeof(GameManager));
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

        static void SaveScene(Scene scene, string path)
        {
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new System.InvalidOperationException($"[SceneBuilder] 씬 저장 실패: {path}");
            }
        }
    }
}
