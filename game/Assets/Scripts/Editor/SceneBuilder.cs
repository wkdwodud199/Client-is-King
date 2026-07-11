using System.Collections.Generic;
using System.Linq;
using ClientIsKing.Data;
using ClientIsKing.Economy;
using ClientIsKing.Inventory;
using ClientIsKing.Managers;
using ClientIsKing.Presentation;
using ClientIsKing.Service;
using ClientIsKing.Settlement;
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

        // phase 패널 공통 배치 (task-108 — 상단 무대가 보이도록 하단 압축)
        static readonly Vector2 PhasePanelPos = new Vector2(0f, -80f);
        static readonly Vector2 PhasePanelSize = new Vector2(480f, 200f);

        // 기준 팔레트 (task-110 design.md F2 hex 고정)
        static readonly Color32 InkNavy = new Color32(0x16, 0x20, 0x2A, 0xFF);
        static readonly Color32 NightBlue = new Color32(0x25, 0x3B, 0x56, 0xFF);
        static readonly Color32 SteamCream = new Color32(0xF4, 0xE5, 0xC2, 0xFF);
        static readonly Color32 GochujangRed = new Color32(0xD3, 0x4A, 0x3A, 0xFF);

        public static void Apply()
        {
            if (!AssetDatabase.IsValidFolder(ScenesDir))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            // 플레이스홀더 스프라이트 선행 생성 (무대가 로드한다 — task-108)
            PlaceholderArtBuilder.Apply();

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

            // 무대(+밤 오버레이)를 캔버스 맨 앞 자식으로 — UI 가 항상 위에 렌더/클릭된다 (설계 20단계).
            BuildShopStage(canvasGo.transform);

            var dayPhaseText = CreateText(canvasGo.transform, "DayPhaseText", "Day 1 — 장보기", 24f,
                new Vector2(-160f, 150f), new Vector2(300f, 40f));
            // Market 패널(task-105)이 하단을 차지하므로 진행 버튼은 우상단으로 (겹침 방지).
            var advanceButton = CreateButton(canvasGo.transform, "AdvanceButton", "다음 단계 ▶",
                new Vector2(250f, 150f), new Vector2(140f, 36f));
            // HUD 전문 분야 badge — DayPhaseText 와 AdvanceButton 사이 (task-110 E3 좌표 고정).
            var genreBadge = CreateText(canvasGo.transform, "GenreBadge", "", 12f,
                new Vector2(80f, 150f), new Vector2(150f, 30f));

            // 장르 선택 modal (task-110 E3) — 참조는 이 unit 에서 최종 wiring 하고,
            // 렌더/raycast 는 canvas 마지막 자식으로 최상단 배치한다 (아래 SetAsLastSibling).
            var genreModal = BuildGenreSelectionModal(canvasGo.transform);

            // phase 별 패널 — Market 은 task-105 실 장보기 UI, 나머지는 task-106+ 가 교체한다.
            var marketPanel = BuildMarketPanel(canvasGo.transform, genreModal);
            var servicePanel = BuildServicePanel(canvasGo.transform);
            var settlementPanel = BuildSettlementPanel(canvasGo.transform);
            var nightPanel = BuildNightPanel(canvasGo.transform);

            // 초기 표시는 Market(미선택 → modal 노출) — 런타임에서는 controller 가 상태에 맞춰 토글한다.
            marketPanel.SetActive(true);
            servicePanel.SetActive(false);
            settlementPanel.SetActive(false);
            nightPanel.SetActive(false);
            genreModal.Panel.transform.SetAsLastSibling();
            genreModal.Panel.SetActive(true);

            var hud = canvasGo.AddComponent<PhaseHudController>();
            hud.EditorInit(dayPhaseText, advanceButton, marketPanel, servicePanel, settlementPanel, nightPanel,
                genreBadge);

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
            // task-110 (U5): MainMenu/Shop 양쪽에 동일한 정렬 catalog 를 주입해 persistent
            // instance 가 어느 씬에서 생존해도 lookup/plan 검증을 잃지 않는다 (design.md G3).
            var go = new GameObject("GameManager", typeof(GameManager));
            go.GetComponent<GameManager>().EditorInit(LoadGenreDefs());
            go.AddComponent<EconomyManager>();
            go.AddComponent<InventoryManager>();
            go.AddComponent<ServiceManager>().EditorInit(LoadRecipeDefs(), LoadCustomerDefs());
            go.AddComponent<SettlementManager>();
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

        // ── Shop 무대 + 밤 오버레이 (task-108) ──────────────────────────────
        static void BuildShopStage(Transform canvas)
        {
            var stage = CreateUIObject("ShopStage", canvas);
            var stageRt = (RectTransform)stage.transform;
            stageRt.anchorMin = stageRt.anchorMax = new Vector2(0.5f, 0.5f);
            stageRt.anchoredPosition = Vector2.zero;
            stageRt.sizeDelta = new Vector2(640f, 360f);

            // 무대 배경/카운터 — Kenney 타일을 Tiled 로 반복 (task-109). 타일 로드 실패 시 단색 폴백.
            var floorTile = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderArtBuilder.FloorTilePath);
            var counterTile = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderArtBuilder.CounterTilePath);

            CreateStageImage(stage.transform, "Stage_Backdrop",
                new Vector2(0f, 100f), new Vector2(640f, 160f), new Color(0.23f, 0.17f, 0.13f, 1f),
                floorTile);

            // 손님 이동 영역 마커 (레이아웃 앵커 — 시각 요소 없음)
            var customerArea = CreateUIObject("Stage_CustomerArea", stage.transform);
            var areaRt = (RectTransform)customerArea.transform;
            areaRt.anchorMin = areaRt.anchorMax = new Vector2(0.5f, 0.5f);
            areaRt.anchoredPosition = new Vector2(-220f, 56f);
            areaRt.sizeDelta = new Vector2(280f, 110f);

            CreateStageImage(stage.transform, "Stage_Counter",
                new Vector2(40f, 48f), new Vector2(320f, 32f), new Color(0.38f, 0.26f, 0.18f, 1f),
                counterTile);

            // 순수 장식 소품 — 좌석/동선/충돌/상호작용 없음 (주차장 가드, task-109). 음식 그릇 스프라이트 재활용.
            BuildStageProps(stage.transform);

            var customerGo = CreateUIObject("CustomerSprite", stage.transform);
            var customerRt = (RectTransform)customerGo.transform;
            customerRt.anchorMin = customerRt.anchorMax = new Vector2(0.5f, 0.5f);
            customerRt.anchoredPosition = new Vector2(-360f, 56f);
            customerRt.sizeDelta = new Vector2(64f, 64f); // 16×16 Ninja Adventure 스프라이트 ×4 (픽셀 정수배, task-109)
            var customerImage = customerGo.AddComponent<Image>();
            customerImage.raycastTarget = false;
            customerImage.preserveAspect = true;
            var customerLabel = CreateText(customerGo.transform, "CustomerLabel", "", 12f,
                new Vector2(0f, -32f), new Vector2(64f, 16f));

            var orderLabel = CreateText(stage.transform, "OrderLabel", "", 13f,
                new Vector2(-160f, 96f), new Vector2(200f, 20f));

            var foodGo = CreateUIObject("FoodIcon", stage.transform);
            var foodRt = (RectTransform)foodGo.transform;
            foodRt.anchorMin = foodRt.anchorMax = new Vector2(0.5f, 0.5f);
            foodRt.anchoredPosition = new Vector2(-40f, 78f);
            foodRt.sizeDelta = new Vector2(40f, 32f); // 20×16 스프라이트 ×2
            var foodImage = foodGo.AddComponent<Image>();
            foodImage.raycastTarget = false;
            foodImage.preserveAspect = true;
            foodGo.SetActive(false);

            var cashPopup = CreateText(stage.transform, "CashPopupText", "", 15f,
                new Vector2(-40f, 96f), new Vector2(180f, 22f));
            var pulse = CreateText(stage.transform, "SettlementPulseText", "", 19f,
                new Vector2(150f, 104f), new Vector2(280f, 28f));

            // 밤 오버레이 — 무대 위·HUD/패널 아래 순서, 클릭 차단 금지 (설계 153행)
            var overlayGo = CreateUIObject("NightOverlay", canvas);
            var overlayRt = (RectTransform)overlayGo.transform;
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;
            var overlayImage = overlayGo.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0.04f, 0f);
            overlayImage.raycastTarget = false;

            var controller = stage.AddComponent<ShopPresentationController>();
            controller.EditorInit(
                customerRt, customerImage, customerLabel, orderLabel,
                foodImage, cashPopup, pulse, overlayImage,
                LoadCustomerSpriteEntries(), LoadRecipeSpriteEntries());
        }

        static Image CreateStageImage(Transform parent, string name, Vector2 pos, Vector2 size, Color color,
            Sprite tileSprite = null)
        {
            var go = CreateUIObject(name, parent);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var image = go.AddComponent<Image>();
            if (tileSprite != null)
            {
                // 16×16 타일을 원본 픽셀 크기로 반복 (PPU 32 → 16px = 0.5 unit; Tiled 는 sprite rect 단위로 반복).
                image.sprite = tileSprite;
                image.type = Image.Type.Tiled;
                image.color = Color.white; // 타일 원색 유지
            }
            else
            {
                image.color = color; // 타일 없으면 단색 폴백
            }
            image.raycastTarget = false; // 무대는 UI 클릭을 가로채지 않는다
            return image;
        }

        /// <summary>순수 장식 소품 — 카운터 위 음식 그릇 장식 (좌석/동선/충돌/상호작용 없음, task-109 주차장 가드).</summary>
        static void BuildStageProps(Transform stage)
        {
            var props = new (string name, string spritePath, Vector2 pos, Vector2 size)[]
            {
                ("Prop_BowlLeft",  $"{PlaceholderArtBuilder.FoodIconsDir}/pork_gukbap.png",  new Vector2(140f, 70f), new Vector2(26f, 26f)),
                ("Prop_BowlMid",   $"{PlaceholderArtBuilder.FoodIconsDir}/janchi_guksu.png", new Vector2(178f, 70f), new Vector2(26f, 26f)),
                ("Prop_BowlRight", $"{PlaceholderArtBuilder.FoodIconsDir}/bibim_guksu.png",  new Vector2(216f, 70f), new Vector2(26f, 26f)),
            };
            foreach (var (name, spritePath, pos, size) in props)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite == null)
                {
                    continue;
                }
                var go = CreateUIObject(name, stage);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = pos;
                rt.sizeDelta = size;
                var img = go.AddComponent<Image>();
                img.sprite = sprite;
                img.preserveAspect = true;
                img.raycastTarget = false; // 순수 장식 — 클릭/충돌 없음
            }
        }

        static List<CustomerSpriteEntry> LoadCustomerSpriteEntries()
        {
            var list = new List<CustomerSpriteEntry>();
            foreach (var id in new[] { "student", "office_worker", "family_parent", "senior_regular" })
            {
                var walk = PlaceholderArtBuilder.WalkFramePaths(id)
                    .Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p))
                    .ToArray();
                list.Add(new CustomerSpriteEntry
                {
                    customerId = id,
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PlaceholderArtBuilder.CustomersDir}/{id}.png"),
                    walkFrames = walk,
                });
            }
            return list;
        }

        static List<RecipeSpriteEntry> LoadRecipeSpriteEntries()
        {
            var list = new List<RecipeSpriteEntry>();
            foreach (var id in new[] { "pork_gukbap", "beef_gukbap", "tteokbokki", "gimbap", "janchi_guksu", "bibim_guksu" })
            {
                list.Add(new RecipeSpriteEntry
                {
                    recipeId = id,
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PlaceholderArtBuilder.FoodIconsDir}/{id}.png"),
                });
            }
            return list;
        }

        // ── 장르 선택 modal (task-110 design.md E3 — 좌표·크기·폰트 픽셀 고정) ──

        /// <summary>BuildShop 한 unit 안에서 modal 생성 → MarketPanelController 주입을 잇는 참조 묶음.</summary>
        struct GenreModalRefs
        {
            public GameObject Panel;
            public Button Gukbap;
            public Button Bunsik;
            public Button Noodles;
            public Button Generalist;
            public Button Confirm;
            public TMP_Text DetailName;
            public TMP_Text DetailBody;
            public TMP_Text DetailNumbers;
            public TMP_Text Helper;
        }

        static GenreModalRefs BuildGenreSelectionModal(Transform parent)
        {
            var panel = CreateUIObject("Panel_GenreSelection", parent);
            var rt = (RectTransform)panel.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -20f);
            rt.sizeDelta = new Vector2(560f, 250f);
            var background = panel.AddComponent<Image>();
            background.color = SteamCream;
            // raycastTarget 기본 true — modal 뒤 UI 클릭을 차단한다 (E3).

            var title = CreateText(panel.transform, "Title", "전문 분야를 선택하세요", 20f,
                new Vector2(0f, 90f), new Vector2(520f, 26f));
            title.color = InkNavy;

            // 4버튼 x=-180,-60,60,180 / y=50 / 110×32 / 15pt — 아이콘은 bowl/skewer/noodle/mixed
            // placeholder (task-109 FoodIcons 재활용, 최종 아트는 Codex 승인 후 교체).
            var gukbap = CreateGenreButton(panel.transform, "Button_Gukbap", "국밥", -180f,
                $"{PlaceholderArtBuilder.FoodIconsDir}/pork_gukbap.png");
            var bunsik = CreateGenreButton(panel.transform, "Button_Bunsik", "분식", -60f,
                $"{PlaceholderArtBuilder.FoodIconsDir}/tteokbokki.png");
            var noodles = CreateGenreButton(panel.transform, "Button_Noodles", "면류", 60f,
                $"{PlaceholderArtBuilder.FoodIconsDir}/janchi_guksu.png");
            var generalist = CreateGenreButton(panel.transform, "Button_Generalist", "균형", 180f,
                $"{PlaceholderArtBuilder.FoodIconsDir}/gimbap.png");

            // detail 영역 (0,-15)/(520×84) — 버튼 하단(34)과 detail 상단(27) 사이 7px 확보 (E3).
            var detail = CreateUIObject("Detail", panel.transform);
            var detailRt = (RectTransform)detail.transform;
            detailRt.anchorMin = detailRt.anchorMax = new Vector2(0.5f, 0.5f);
            detailRt.anchoredPosition = new Vector2(0f, -15f);
            detailRt.sizeDelta = new Vector2(520f, 84f);
            var detailName = CreateText(detail.transform, "DetailName", "", 16f,
                new Vector2(0f, 33f), new Vector2(520f, 18f));
            detailName.color = InkNavy;
            var detailBody = CreateText(detail.transform, "DetailBody", "", 13f,
                new Vector2(0f, 7f), new Vector2(520f, 32f));
            detailBody.color = InkNavy;
            var detailNumbers = CreateText(detail.transform, "DetailNumbers", "", 12f,
                new Vector2(0f, -26f), new Vector2(520f, 30f));
            detailNumbers.color = InkNavy;

            var confirm = CreateButton(panel.transform, "ConfirmButton", "이 전문 분야로 시작",
                new Vector2(0f, -80f), new Vector2(190f, 30f));
            confirm.GetComponent<Image>().color = GochujangRed; // 현재 행동의 단일 primary color (E2)
            var confirmLabel = confirm.GetComponentInChildren<TMP_Text>();
            confirmLabel.fontSize = 15f;
            confirmLabel.color = SteamCream;

            var helper = CreateText(panel.transform, "HelperText", "선택은 이번 런 동안 유지됩니다 · ←/→·Tab 이동", 11f,
                new Vector2(0f, -115f), new Vector2(520f, 18f));
            helper.color = InkNavy;

            // focus 순서 국밥→분식→면류→균형→확정 — 좌우 방향키는 explicit navigation (E3).
            LinkHorizontalNavigation(gukbap, bunsik, noodles, generalist, confirm);

            return new GenreModalRefs
            {
                Panel = panel,
                Gukbap = gukbap,
                Bunsik = bunsik,
                Noodles = noodles,
                Generalist = generalist,
                Confirm = confirm,
                DetailName = detailName,
                DetailBody = detailBody,
                DetailNumbers = detailNumbers,
                Helper = helper,
            };
        }

        /// <summary>장르 버튼 — Night Blue/Steam Cream 기본, 선택 시 controller 가 outline·아이콘을 켠다 (E3/F2).</summary>
        static Button CreateGenreButton(Transform parent, string name, string label, float x, string iconPath)
        {
            var button = CreateButton(parent, name, label, new Vector2(x, 50f), new Vector2(110f, 32f));
            button.GetComponent<Image>().color = NightBlue;
            var text = button.GetComponentInChildren<TMP_Text>();
            text.fontSize = 15f;
            text.color = SteamCream;

            // 선택 표시 outline 2px (Gochujang Red) — 기본 꺼짐, controller 가 선택 버튼만 켠다.
            var outline = button.gameObject.AddComponent<Outline>();
            outline.effectColor = GochujangRed;
            outline.effectDistance = new Vector2(2f, 2f);
            outline.enabled = false;

            // 선택 아이콘 — 기본 비활성 (색만으로 상태 전달 금지, E5).
            var iconGo = CreateUIObject("Icon", button.transform);
            var iconRt = (RectTransform)iconGo.transform;
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(-42f, 0f);
            iconRt.sizeDelta = new Vector2(20f, 20f);
            var iconImage = iconGo.AddComponent<Image>();
            iconImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconGo.SetActive(false);

            return button;
        }

        /// <summary>좌우 방향키 focus 체인 — explicit navigation 으로 순서를 고정한다 (E3).</summary>
        static void LinkHorizontalNavigation(params Selectable[] chain)
        {
            for (int i = 0; i < chain.Length; i++)
            {
                var navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = i > 0 ? chain[i - 1] : null,
                    selectOnRight = i < chain.Length - 1 ? chain[i + 1] : null,
                };
                chain[i].navigation = navigation;
            }
        }

        // ── Market 장보기 UI (task-105, task-110 U5: 장르 modal 참조 주입) ──
        static GameObject BuildMarketPanel(Transform parent, GenreModalRefs genreModal)
        {
            var panel = CreateUIObject("Panel_Market", parent);
            var rt = (RectTransform)panel.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = PhasePanelPos;
            rt.sizeDelta = PhasePanelSize;
            panel.AddComponent<Image>().color = new Color(0.20f, 0.45f, 0.30f, 0.85f);

            var cashText = CreateText(panel.transform, "CashText", "자금 30,000원", 15f,
                new Vector2(0f, 80f), new Vector2(440f, 22f));

            var ingredientPrev = CreateButton(panel.transform, "IngredientPrev", "◀",
                new Vector2(-190f, 54f), new Vector2(32f, 26f));
            var ingredientLabel = CreateText(panel.transform, "IngredientLabel", "쌀", 15f,
                new Vector2(0f, 54f), new Vector2(260f, 26f));
            var ingredientNext = CreateButton(panel.transform, "IngredientNext", "▶",
                new Vector2(190f, 54f), new Vector2(32f, 26f));

            var gradeButton = CreateButton(panel.transform, "GradeButton", "등급: C급",
                new Vector2(0f, 28f), new Vector2(150f, 26f));
            var gradeLabel = gradeButton.GetComponentInChildren<TMP_Text>();

            var quantityMinus = CreateButton(panel.transform, "QuantityMinus", "−",
                new Vector2(-80f, 2f), new Vector2(32f, 26f));
            var quantityText = CreateText(panel.transform, "QuantityText", "1", 15f,
                new Vector2(0f, 2f), new Vector2(90f, 26f));
            var quantityPlus = CreateButton(panel.transform, "QuantityPlus", "＋",
                new Vector2(80f, 2f), new Vector2(32f, 26f));

            var costText = CreateText(panel.transform, "CostText", "예상 비용 300원", 13f,
                new Vector2(0f, -22f), new Vector2(440f, 20f));
            var ownedText = CreateText(panel.transform, "OwnedText", "보유 0개", 12f,
                new Vector2(0f, -42f), new Vector2(440f, 18f));

            var buyButton = CreateButton(panel.transform, "BuyButton", "구매",
                new Vector2(0f, -66f), new Vector2(150f, 28f));
            var messageText = CreateText(panel.transform, "MessageText", "", 11f,
                new Vector2(0f, -90f), new Vector2(460f, 16f));

            // 확정 후 남는 `상세 보기` 토글 (E3 — modal 은 접히고 badge + 상세 보기만).
            var genreDetailButton = CreateButton(panel.transform, "GenreDetailButton", "상세 보기",
                new Vector2(190f, 80f), new Vector2(80f, 20f));
            genreDetailButton.GetComponent<Image>().color = NightBlue;
            var genreDetailLabel = genreDetailButton.GetComponentInChildren<TMP_Text>();
            genreDetailLabel.fontSize = 10f;
            genreDetailLabel.color = SteamCream;
            genreDetailButton.gameObject.SetActive(false); // 확정 전 숨김 — controller 가 토글

            var controller = panel.AddComponent<MarketPanelController>();
            controller.EditorInit(
                cashText,
                ingredientPrev, ingredientNext, ingredientLabel,
                gradeButton, gradeLabel,
                quantityMinus, quantityPlus, quantityText,
                costText, ownedText, buyButton, messageText,
                LoadIngredientDefs(), LoadRecipeDefs(),
                genreModal.Panel,
                genreModal.Gukbap, genreModal.Bunsik, genreModal.Noodles, genreModal.Generalist,
                genreModal.DetailName, genreModal.DetailBody, genreModal.DetailNumbers,
                genreModal.Confirm, genreModal.Helper, genreDetailButton);
            return panel;
        }

        // ── Service 영업 UI (task-106) ──────────────────────────────────────
        static GameObject BuildServicePanel(Transform parent)
        {
            var panel = CreateUIObject("Panel_Service", parent);
            var rt = (RectTransform)panel.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = PhasePanelPos;
            rt.sizeDelta = PhasePanelSize;
            panel.AddComponent<Image>().color = new Color(0.45f, 0.35f, 0.20f, 0.85f);

            var orderText = CreateText(panel.transform, "OrderText", "주문 1/5", 14f,
                new Vector2(0f, 80f), new Vector2(440f, 20f));
            var customerText = CreateText(panel.transform, "CustomerText", "-", 12f,
                new Vector2(-110f, 60f), new Vector2(220f, 18f));
            var recipeText = CreateText(panel.transform, "RecipeText", "-", 14f,
                new Vector2(110f, 60f), new Vector2(220f, 20f));
            var cookTimeText = CreateText(panel.transform, "CookTimeText", "", 10f,
                new Vector2(-110f, 42f), new Vector2(220f, 16f));
            var revenueText = CreateText(panel.transform, "RevenueText", "", 11f,
                new Vector2(110f, 42f), new Vector2(220f, 16f));

            var gradeButton = CreateButton(panel.transform, "GradeButton", "등급: C급",
                new Vector2(0f, 18f), new Vector2(150f, 26f));
            var gradeLabel = gradeButton.GetComponentInChildren<TMP_Text>();

            var requiredText = CreateText(panel.transform, "RequiredText", "", 10f,
                new Vector2(0f, -6f), new Vector2(460f, 18f));

            var serveButton = CreateButton(panel.transform, "ServeButton", "서빙",
                new Vector2(-95f, -32f), new Vector2(150f, 28f));
            var skipButton = CreateButton(panel.transform, "SkipButton", "포기",
                new Vector2(95f, -32f), new Vector2(150f, 28f));

            var statsText = CreateText(panel.transform, "StatsText", "오늘 매출 0원 · 서빙 0명 · 이탈 0명", 11f,
                new Vector2(0f, -58f), new Vector2(460f, 16f));
            var messageText = CreateText(panel.transform, "MessageText", "", 10f,
                new Vector2(0f, -82f), new Vector2(460f, 24f));

            var controller = panel.AddComponent<ServicePanelController>();
            controller.EditorInit(
                orderText, customerText, recipeText, cookTimeText, revenueText,
                gradeButton, gradeLabel, requiredText,
                serveButton, skipButton, statsText, messageText,
                LoadRecipeDefs(), LoadCustomerDefs(), LoadIngredientDefs());
            return panel;
        }

        // ── Settlement 정산 UI (task-107) ───────────────────────────────────
        static GameObject BuildSettlementPanel(Transform parent)
        {
            var panel = CreateUIObject("Panel_Settlement", parent);
            var rt = (RectTransform)panel.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = PhasePanelPos;
            rt.sizeDelta = PhasePanelSize;
            panel.AddComponent<Image>().color = new Color(0.30f, 0.30f, 0.50f, 0.85f);

            var grossText = CreateText(panel.transform, "GrossText", "매출  +0원", 14f,
                new Vector2(0f, 78f), new Vector2(440f, 20f));
            var spendText = CreateText(panel.transform, "SpendText", "재료 지출  -0원", 12f,
                new Vector2(0f, 58f), new Vector2(440f, 18f));
            var operatingText = CreateText(panel.transform, "OperatingText", "운영비  -12,000원", 12f,
                new Vector2(0f, 40f), new Vector2(440f, 18f));
            var netText = CreateText(panel.transform, "NetText", "순손익  +0원", 17f,
                new Vector2(0f, 16f), new Vector2(440f, 26f));
            var cashText = CreateText(panel.transform, "CashText", "잔액  0원 → 0원", 13f,
                new Vector2(0f, -10f), new Vector2(440f, 20f));
            var statsText = CreateText(panel.transform, "StatsText", "서빙 0명 · 이탈 0명", 11f,
                new Vector2(0f, -32f), new Vector2(440f, 16f));
            // task-110: 전문 분야 원인 한 줄 — stats 아래·message 위 (message 는 하단으로 압축).
            var genreEffectText = CreateText(panel.transform, "GenreEffectText", "", 10f,
                new Vector2(0f, -48f), new Vector2(460f, 14f));
            var messageText = CreateText(panel.transform, "MessageText", "", 11f,
                new Vector2(0f, -76f), new Vector2(460f, 28f));

            var controller = panel.AddComponent<SettlementPanelController>();
            controller.EditorInit(grossText, spendText, operatingText, netText, cashText, statsText, messageText,
                genreEffectText);
            return panel;
        }

        // ── Night 하루 마감 UI (task-107 — SNS/저장은 task-109/111 이 추가) ──
        static GameObject BuildNightPanel(Transform parent)
        {
            var panel = CreateUIObject("Panel_Night", parent);
            var rt = (RectTransform)panel.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = PhasePanelPos;
            rt.sizeDelta = PhasePanelSize;
            panel.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.25f, 0.85f);

            var summaryText = CreateText(panel.transform, "SummaryText", "Day 1 마감", 17f,
                new Vector2(0f, 52f), new Vector2(440f, 26f));
            var daysText = CreateText(panel.transform, "DaysText", "완료 일수 0일", 13f,
                new Vector2(0f, 24f), new Vector2(440f, 20f));
            var statusText = CreateText(panel.transform, "StatusText", "", 12f,
                new Vector2(0f, -34f), new Vector2(460f, 70f));

            var controller = panel.AddComponent<NightPanelController>();
            controller.EditorInit(summaryText, daysText, statusText);
            return panel;
        }

        /// <summary>시드 GenreDef 4종을 id 순으로 로드한다 (MainMenu/Shop 동일 정렬 catalog — task-110).</summary>
        static List<GenreDef> LoadGenreDefs()
        {
            return AssetDatabase.FindAssets("t:GenreDef", new[] { "Assets/Data/Definitions/Genres" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<GenreDef>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(def => def != null)
                .OrderBy(def => def.Id, System.StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>시드 RecipeDef 6종을 id 순으로 로드한다.</summary>
        static List<RecipeDef> LoadRecipeDefs()
        {
            return AssetDatabase.FindAssets("t:RecipeDef", new[] { "Assets/Data/Definitions/Recipes" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<RecipeDef>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(def => def != null)
                .OrderBy(def => def.Id, System.StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>시드 CustomerArchetypeDef 4종을 id 순으로 로드한다.</summary>
        static List<CustomerArchetypeDef> LoadCustomerDefs()
        {
            return AssetDatabase.FindAssets("t:CustomerArchetypeDef", new[] { "Assets/Data/Definitions/Customers" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<CustomerArchetypeDef>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(def => def != null)
                .OrderBy(def => def.Id, System.StringComparer.Ordinal)
                .ToList();
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
