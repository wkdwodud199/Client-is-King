using System.IO;
using System.Linq;
using System.Reflection;
using ClientIsKing.EditorTools;
using ClientIsKing.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-117 U4: MainMenu 크레딧 버튼 + 모달 패널 — G절 1~8 (정적 구조 D절 전수 / 배선·네비게이션 /
    /// 열기·닫기 interactable 저장·잠금·정확 복원 + Cancel 동일 경로 + 리스너 쌍 / 문구 동기화 ①~⑤ /
    /// 양 컬럼 fit / 글리프 커버리지(원본 폰트 에셋 비파괴 임시 clone) / 라이선스 동봉 바이트 동일 /
    /// Apply 멱등). focus·실 키 입력 격리는 PlayMode(CreditsPlayModeTests)가 담당한다.
    /// </summary>
    public class CreditsPanelSceneTests
    {
        /// <summary>라이선스 요약 확정 문장 (task-117 design.md A절 — Codex P0-2 정정본 verbatim).</summary>
        const string LicenseSummarySentence =
            "프로젝트 오너는 별도의 재사용·재배포·2차 저작물 작성 허가를 부여하지 않습니다. " +
            "AI 요소의 저작권 성립과 보호 범위는 관할권에 따라 달라질 수 있습니다.";

        [OneTimeSetUp]
        public void BuildScenesOnce()
        {
            SceneBuilder.Apply();
        }

        static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        static Scene OpenMainMenu()
        {
            return EditorSceneManager.OpenScene(SceneBuilder.MainMenuPath, OpenSceneMode.Single);
        }

        static GameObject Canvas(Scene scene)
        {
            var canvasGo = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "Canvas");
            Assert.IsNotNull(canvasGo, "Canvas 누락");
            return canvasGo;
        }

        static void AssertRect(Transform t, float x, float y, float w, float h, string name)
        {
            var rt = (RectTransform)t;
            Assert.AreEqual(x, rt.anchoredPosition.x, 0.01f, $"{name} anchoredPosition.x");
            Assert.AreEqual(y, rt.anchoredPosition.y, 0.01f, $"{name} anchoredPosition.y");
            Assert.AreEqual(w, rt.sizeDelta.x, 0.01f, $"{name} sizeDelta.x");
            Assert.AreEqual(h, rt.sizeDelta.y, 0.01f, $"{name} sizeDelta.y");
        }

        static void AssertColor(Color actual, byte r, byte g, byte b, string name)
        {
            Assert.AreEqual((Color)new Color32(r, g, b, 0xFF), actual,
                $"{name} 색상 불일치 — 기대 #{r:X2}{g:X2}{b:X2}, 실제 {actual}");
        }

        // ── G절 1: 정적 구조 (D절 표 전수) ──────────────────────────────────

        [Test]
        public void Credits_Button_Matches_D_Table()
        {
            var canvasGo = Canvas(OpenMainMenu());
            var button = canvasGo.transform.Find("CreditsButton");
            Assert.IsNotNull(button, "CreditsButton 누락");
            Assert.IsNotNull(button.GetComponent<Button>(), "CreditsButton 은 Button 이어야 한다");
            AssertRect(button, 250f, 150f, 120f, 32f, "CreditsButton");
            var label = button.GetComponentInChildren<TMP_Text>(true);
            Assert.AreEqual("크레딧", label.text, "크레딧 버튼 라벨");
            Assert.AreEqual(18f, label.fontSize, 0.01f, "크레딧 버튼 라벨 18pt (CreateButton 기본)");
        }

        [Test]
        public void Credits_Panel_Is_Inactive_Last_Sibling_With_Modal_Raycast_Dim()
        {
            var canvasGo = Canvas(OpenMainMenu());
            var panel = canvasGo.transform.Find("Panel_Credits");
            Assert.IsNotNull(panel, "Panel_Credits 누락");
            Assert.IsFalse(panel.gameObject.activeSelf, "크레딧 패널은 초기 비활성이어야 한다 (D절)");
            Assert.AreEqual(canvasGo.transform.childCount - 1, panel.GetSiblingIndex(),
                "크레딧 패널은 canvas 의 마지막 자식(렌더/raycast 최상단)이어야 한다 (D절)");

            AssertRect(panel, 0f, 0f, 640f, 360f, "Panel_Credits");
            var dim = panel.GetComponent<Image>();
            Assert.IsNotNull(dim, "Panel_Credits dim Image 누락");
            Assert.IsTrue(dim.raycastTarget, "dim 은 하부 UI 클릭을 차단해야 한다 (raycastTarget true, 모달)");
            // Ink Navy #16202A alpha ≈0.92 (Panel_Ending 미러)
            Assert.AreEqual(0x16 / 255f, dim.color.r, 0.002f, "dim R (Ink Navy)");
            Assert.AreEqual(0x20 / 255f, dim.color.g, 0.002f, "dim G (Ink Navy)");
            Assert.AreEqual(0x2A / 255f, dim.color.b, 0.002f, "dim B (Ink Navy)");
            Assert.AreEqual(0.92f, dim.color.a, 0.005f, "dim alpha ≈0.92");

            Assert.AreEqual(1, canvasGo.GetComponents<CreditsController>().Length,
                "CreditsController 는 Canvas 탑재 정확히 1개여야 한다 (E절)");
        }

        [Test]
        public void Credits_Panel_Children_Match_D_Table()
        {
            var canvasGo = Canvas(OpenMainMenu());
            var panel = canvasGo.transform.Find("Panel_Credits");

            var title = panel.Find("CreditsTitleText");
            Assert.IsNotNull(title, "CreditsTitleText 누락");
            AssertRect(title, 0f, 158f, 400f, 32f, "CreditsTitleText");
            var titleTmp = title.GetComponent<TMP_Text>();
            Assert.AreEqual("크레딧", titleTmp.text, "제목 카피");
            Assert.AreEqual(24f, titleTmp.fontSize, 0.01f, "CreditsTitleText 24pt");
            AssertColor(titleTmp.color, 0xE5, 0xA8, 0x4B, "CreditsTitleText (Brass Amber)");

            foreach (var (name, x) in new[] { ("CreditsLeftText", -160f), ("CreditsRightText", 160f) })
            {
                var column = panel.Find(name);
                Assert.IsNotNull(column, $"{name} 누락");
                AssertRect(column, x, 10f, 300f, 240f, name);
                var tmp = column.GetComponent<TMP_Text>();
                Assert.AreEqual(10f, tmp.fontSize, 0.01f, $"{name} 10pt");
                AssertColor(tmp.color, 0xF4, 0xE5, 0xC2, $"{name} (Steam Cream)");
                Assert.AreEqual(TextAlignmentOptions.TopLeft, tmp.alignment, $"{name} TopLeft 정렬");
                Assert.AreEqual(TextWrappingModes.Normal, tmp.textWrappingMode, $"{name} wrap 유지");
            }

            var close = panel.Find("CreditsCloseButton");
            Assert.IsNotNull(close, "CreditsCloseButton 누락");
            Assert.IsNotNull(close.GetComponent<Button>(), "CreditsCloseButton 은 Button 이어야 한다");
            AssertRect(close, 0f, -152f, 200f, 40f, "CreditsCloseButton");
            Assert.AreEqual("닫기", close.GetComponentInChildren<TMP_Text>(true).text, "닫기 버튼 라벨");
            Assert.AreEqual(Navigation.Mode.None, close.GetComponent<Button>().navigation.mode,
                "닫기 버튼은 Navigation None — 방향키 focus 탈출 차단 (E절 P0-1)");
        }

        // ── G절 2: 배선 + 네비게이션 ────────────────────────────────────────

        [Test]
        public void Controller_References_Are_Injected_In_Saved_Scene()
        {
            var canvasGo = Canvas(OpenMainMenu());
            var panel = canvasGo.transform.Find("Panel_Credits");
            var controller = canvasGo.GetComponent<CreditsController>();
            Assert.IsNotNull(controller, "CreditsController 누락");

            var so = new SerializedObject(controller);
            Assert.AreSame(panel.gameObject, so.FindProperty("panelRoot").objectReferenceValue, "panelRoot 배선");
            Assert.AreSame(canvasGo.transform.Find("CreditsButton").GetComponent<Button>(),
                so.FindProperty("openButton").objectReferenceValue, "openButton 배선");
            Assert.AreSame(panel.Find("CreditsCloseButton").GetComponent<Button>(),
                so.FindProperty("closeButton").objectReferenceValue, "closeButton 배선");
            Assert.AreSame(canvasGo.transform.Find("StartButton").GetComponent<Button>(),
                so.FindProperty("startButton").objectReferenceValue, "startButton 배선");
            Assert.AreSame(canvasGo.transform.Find("ContinueButton").GetComponent<Button>(),
                so.FindProperty("continueButton").objectReferenceValue, "continueButton 배선");
        }

        [Test]
        public void Credits_Button_Navigation_Extends_Chain_And_Preserves_Start_Continue_Link()
        {
            var canvasGo = Canvas(OpenMainMenu());
            var creditsButton = canvasGo.transform.Find("CreditsButton").GetComponent<Button>();
            var startButton = canvasGo.transform.Find("StartButton").GetComponent<Button>();
            var continueButton = canvasGo.transform.Find("ContinueButton").GetComponent<Button>();

            Assert.AreEqual(Navigation.Mode.Explicit, creditsButton.navigation.mode, "CreditsButton explicit navigation");
            Assert.AreSame(startButton, creditsButton.navigation.selectOnDown, "크레딧 → 게임 시작 (아래)");
            Assert.AreSame(creditsButton, startButton.navigation.selectOnUp, "게임 시작 → 크레딧 (위)");
            // 기존 Start↔Continue 상하 연결 (task-113 G1) 보존 — 기대값 무변경.
            Assert.AreSame(continueButton, startButton.navigation.selectOnDown, "게임 시작 → 이어하기 (아래)");
            Assert.AreSame(startButton, continueButton.navigation.selectOnUp, "이어하기 → 게임 시작 (위)");
        }

        // ── G절 3: 열기/닫기 흐름 (EditMode 몫 — interactable 저장·잠금·정확 복원) ──

        static (GameObject canvasGo, CreditsController controller, GameObject panel,
            Button creditsButton, Button startButton, Button continueButton, Button closeButton) OpenForFlow()
        {
            var canvasGo = Canvas(OpenMainMenu());
            var controller = canvasGo.GetComponent<CreditsController>();
            var panel = canvasGo.transform.Find("Panel_Credits").gameObject;
            var creditsButton = canvasGo.transform.Find("CreditsButton").GetComponent<Button>();
            var startButton = canvasGo.transform.Find("StartButton").GetComponent<Button>();
            var continueButton = canvasGo.transform.Find("ContinueButton").GetComponent<Button>();
            var closeButton = panel.transform.Find("CreditsCloseButton").GetComponent<Button>();
            return (canvasGo, controller, panel, creditsButton, startButton, continueButton, closeButton);
        }

        [Test]
        public void OpenClose_Locks_Background_And_Restores_Saved_Interactable_Exactly()
        {
            var (_, controller, panel, creditsButton, startButton, continueButton, closeButton) = OpenForFlow();
            TestSceneSupport.ForceOnEnable(controller);

            // Continue disabled fixture — 무조건 true 복원이면 fail 해야 한다 (Codex P0-1).
            continueButton.interactable = false;

            creditsButton.onClick.Invoke();
            Assert.IsTrue(panel.activeSelf, "열기 후 패널 활성");
            Assert.IsFalse(startButton.interactable, "열림 동안 게임 시작 잠금");
            Assert.IsFalse(continueButton.interactable, "열림 동안 이어하기 잠금");
            Assert.IsFalse(creditsButton.interactable, "열림 동안 크레딧 버튼 잠금");

            closeButton.onClick.Invoke();
            Assert.IsFalse(panel.activeSelf, "닫기 후 패널 비활성");
            Assert.IsTrue(startButton.interactable, "게임 시작은 저장값(true) 복원");
            Assert.IsFalse(continueButton.interactable, "이어하기는 저장값(disabled) 정확 복원 — 무조건 true 복원 금지");
            Assert.IsTrue(creditsButton.interactable, "크레딧 버튼은 저장값(true) 복원");

            // 전부 활성 fixture 왕복 — true 는 true 로 복원된다.
            continueButton.interactable = true;
            creditsButton.onClick.Invoke();
            Assert.IsFalse(continueButton.interactable);
            closeButton.onClick.Invoke();
            Assert.IsTrue(continueButton.interactable, "활성 이어하기는 true 복원");
        }

        [Test]
        public void Cancel_Path_Reuses_Same_CloseNow_Route()
        {
            // Cancel(Esc/게임패드 B) 은 Update 에서 CloseNow() 를 호출한다 — 실 키 입력은 수동 smoke,
            // 여기서는 동일 경로(내부 메서드)가 버튼 닫기와 같은 복원을 수행함을 검증한다 (E절).
            var (_, controller, panel, creditsButton, startButton, continueButton, _) = OpenForFlow();
            continueButton.interactable = false;

            controller.OpenNow();
            Assert.IsTrue(panel.activeSelf, "OpenNow 후 패널 활성");
            Assert.IsFalse(startButton.interactable);

            controller.CloseNow();
            Assert.IsFalse(panel.activeSelf, "CloseNow 후 패널 비활성");
            Assert.IsTrue(startButton.interactable, "게임 시작 저장값(true) 복원");
            Assert.IsFalse(continueButton.interactable, "이어하기 저장값(disabled) 정확 복원");
            Assert.IsTrue(creditsButton.interactable, "크레딧 버튼 저장값(true) 복원");

            // 닫힌 상태 재호출/미열림 CloseNow 는 no-op — 저장값 오염 없음.
            controller.CloseNow();
            Assert.IsFalse(panel.activeSelf);
            Assert.IsFalse(continueButton.interactable, "no-op CloseNow 가 상태를 바꾸면 안 된다");
        }

        static System.Collections.IList RuntimeCalls(UnityEngine.Events.UnityEventBase evt)
        {
            var callsField = typeof(UnityEngine.Events.UnityEventBase)
                .GetField("m_Calls", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(callsField, "UnityEventBase.m_Calls 리플렉션 실패 (Unity 내부 구조 변경?)");
            var invokableCallList = callsField.GetValue(evt);
            var runtimeField = invokableCallList.GetType()
                .GetField("m_RuntimeCalls", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(runtimeField, "InvokableCallList.m_RuntimeCalls 리플렉션 실패");
            return (System.Collections.IList)runtimeField.GetValue(invokableCallList);
        }

        [Test]
        public void Open_Close_Listeners_Are_Registered_By_OnEnable_And_Removed_By_OnDisable()
        {
            var (_, controller, _, creditsButton, _, _, closeButton) = OpenForFlow();

            Assert.AreEqual(0, creditsButton.onClick.GetPersistentEventCount(),
                "CreditsButton persistent listener 는 0 — 런타임 AddListener 만 사용한다 (멱등 규약)");
            Assert.AreEqual(0, closeButton.onClick.GetPersistentEventCount(),
                "CreditsCloseButton persistent listener 는 0");
            var openCalls = RuntimeCalls(creditsButton.onClick);
            var closeCalls = RuntimeCalls(closeButton.onClick);
            Assert.AreEqual(0, openCalls.Count, "OnEnable 전에는 open 런타임 리스너가 없어야 한다");
            Assert.AreEqual(0, closeCalls.Count, "OnEnable 전에는 close 런타임 리스너가 없어야 한다");

            TestSceneSupport.ForceOnEnable(controller);
            Assert.AreEqual(1, openCalls.Count, "OnEnable 이 open 리스너를 정확히 1개 등록해야 한다");
            Assert.AreEqual(1, closeCalls.Count, "OnEnable 이 close 리스너를 정확히 1개 등록해야 한다");

            var delegateField = openCalls[0].GetType().GetField("Delegate", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(delegateField, "InvokableCall.Delegate 리플렉션 실패");
            var openAction = (System.Delegate)delegateField.GetValue(openCalls[0]);
            Assert.AreEqual("OnOpenClicked", openAction.Method.Name, "크레딧 버튼은 OnOpenClicked 로 배선되어야 한다");
            Assert.AreSame(controller, openAction.Target, "open 리스너 대상은 CreditsController 자신이어야 한다");
            var closeAction = (System.Delegate)delegateField.GetValue(closeCalls[0]);
            Assert.AreEqual("OnCloseClicked", closeAction.Method.Name, "닫기 버튼은 OnCloseClicked 로 배선되어야 한다");

            var onDisable = typeof(CreditsController)
                .GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance);
            onDisable.Invoke(controller, null);
            Assert.AreEqual(0, openCalls.Count, "OnDisable 이 open 리스너를 해제해야 한다 (쌍 규약)");
            Assert.AreEqual(0, closeCalls.Count, "OnDisable 이 close 리스너를 해제해야 한다 (쌍 규약)");
        }

        // ── G절 4: 문구 동기화 (B절 ①~⑤) ───────────────────────────────────

        [Test]
        public void Scene_Baked_Texts_Match_CreditsCopy_Constants()
        {
            var canvasGo = Canvas(OpenMainMenu());
            var panel = canvasGo.transform.Find("Panel_Credits");
            Assert.AreEqual(CreditsCopy.LeftColumn, panel.Find("CreditsLeftText").GetComponent<TMP_Text>().text,
                "씬에 구운 좌측 텍스트 == CreditsCopy.LeftColumn (빌드 산출 동기화 ④)");
            Assert.AreEqual(CreditsCopy.RightColumn, panel.Find("CreditsRightText").GetComponent<TMP_Text>().text,
                "씬에 구운 우측 텍스트 == CreditsCopy.RightColumn (빌드 산출 동기화 ④)");
        }

        [Test]
        public void AiArtNotice_And_License_Summary_Are_Verbatim_From_Design_Contracts()
        {
            // ① AI 공개문 — task-116 design.md C절 확정문 [KO] verbatim (부분 문자열, 개행 없는 단일 원문).
            string t116 = File.ReadAllText(Path.Combine(RepoRoot, "kb", "tasks", "task-116", "design.md"));
            StringAssert.Contains(CreditsCopy.AiArtNoticeKo, t116,
                "AiArtNoticeKo 는 task-116 design.md C절 공개문 [KO] 와 한 글자도 다르면 안 된다 (verbatim)");
            StringAssert.Contains(CreditsCopy.AiArtNoticeKo, CreditsCopy.RightColumn,
                "RightColumn 은 AiArtNoticeKo 를 그대로 포함해야 한다");

            // ⑤ 라이선스 요약 — 확정 표현의 verbatim 원천은 task-117 design.md A절(Codex P0-2 정정본).
            // A절 블록은 문서 표시용 줄바꿈을 포함하므로 개행을 공백으로 정규화해 비교한다.
            // task-116 C절(평서체 정책 원문)에는 정책 앵커 구문 존재로 드리프트를 감지한다.
            string t117 = File.ReadAllText(Path.Combine(RepoRoot, "kb", "tasks", "task-117", "design.md"))
                .Replace("\r\n", "\n").Replace('\n', ' ');
            StringAssert.Contains(LicenseSummarySentence, t117,
                "라이선스 요약 문장은 task-117 design.md A절 확정본과 verbatim 일치해야 한다");
            StringAssert.Contains("프로젝트 고유 아트(AI 보조 아트 포함): MIT 적용 제외, CC0 아님.", t117,
                "아트 라이선스 구분 행은 A절 확정본과 일치해야 한다");
            StringAssert.Contains(LicenseSummarySentence, CreditsCopy.RightColumn,
                "RightColumn 은 라이선스 요약 확정 문장을 포함해야 한다");
            StringAssert.Contains("재사용·재배포·2차 저작물 작성 허가", t116,
                "task-116 C절 라이선스 정책 앵커가 사라지면 요약 문구 재검토가 필요하다 (드리프트 감지)");
            // CC0 표기 금지 규칙 승계 — AI 보조 아트를 CC0 로 표기하지 않는다.
            StringAssert.Contains("CC0 아님", CreditsCopy.RightColumn);
        }

        [Test]
        public void Font_Copyright_And_CC0_Pack_Credits_Match_Source_Documents()
        {
            // ② 폰트 저작권자 표기 — Galmuri-LICENSE.txt (OFL) 에 존재.
            string ofl = File.ReadAllText(Path.Combine(Application.dataPath, "Art", "Fonts", "Galmuri-LICENSE.txt"));
            StringAssert.Contains("Lee Minseo (quiple@quiple.dev)", ofl, "OFL 저작권자 표기");
            StringAssert.Contains("SIL Open Font License", ofl, "OFL 라이선스명");
            StringAssert.Contains("Lee Minseo (quiple@quiple.dev)", CreditsCopy.LeftColumn, "크레딧 폰트 저작권자");
            StringAssert.Contains("SIL Open Font License 1.1", CreditsCopy.LeftColumn, "크레딧 폰트 라이선스명");

            // ③ CC0 팩 4종 표기 — PLACEHOLDER-PROVENANCE.md 도입 팩 표에 존재.
            string provenance = File.ReadAllText(
                Path.Combine(Application.dataPath, "Art", "Placeholders", "PLACEHOLDER-PROVENANCE.md"));
            foreach (var pack in new[]
                { "Ninja Adventure Asset Pack", "Pixel Art Food Pack", "Free Pixel Food", "Roguelike/RPG pack" })
            {
                StringAssert.Contains(pack, provenance, $"provenance 도입 팩 표에 '{pack}' 이 있어야 한다");
                StringAssert.Contains(pack, CreditsCopy.LeftColumn, $"크레딧에 '{pack}' 표기가 있어야 한다");
            }
            foreach (var author in new[] { "Pixel-Boy", "karsiori", "Henry Software", "Kenney" })
            {
                StringAssert.Contains(author, provenance, $"provenance 에 작가 '{author}' 가 있어야 한다");
                StringAssert.Contains(author, CreditsCopy.LeftColumn, $"크레딧에 작가 '{author}' 표기가 있어야 한다");
            }
            StringAssert.Contains("CC0 1.0", provenance);
            StringAssert.Contains("CC0 1.0", CreditsCopy.LeftColumn);
            StringAssert.Contains("kenney.nl", provenance);
        }

        // ── G절 5: 폭/높이 fit (양 컬럼 — Codex P1) ─────────────────────────

        [Test]
        public void Both_Columns_Fit_Within_300x240_Without_Overflow()
        {
            var canvasGo = Canvas(OpenMainMenu());
            var panel = canvasGo.transform.Find("Panel_Credits");
            panel.gameObject.SetActive(true); // 측정용 임시 활성 (씬 저장 없음)

            var left = panel.Find("CreditsLeftText").GetComponent<TMP_Text>();
            var right = panel.Find("CreditsRightText").GetComponent<TMP_Text>();

            // 좌측 — 수동 개행 각 줄 폭 ≤300px.
            foreach (var line in CreditsCopy.LeftColumn.Split('\n'))
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                var preferred = left.GetPreferredValues(line);
                Assert.LessOrEqual(preferred.x, 300f,
                    $"좌측 줄 폭 {preferred.x:F1}px 이 300px 를 초과함: '{line}'");
            }

            // 좌·우 — 300px 폭 wrap 높이 ≤240px + overflow 없음 (Codex P1 — 양 컬럼).
            foreach (var (tmp, text, name) in new[]
            {
                (left, CreditsCopy.LeftColumn, "CreditsLeftText"),
                (right, CreditsCopy.RightColumn, "CreditsRightText"),
            })
            {
                var preferred = tmp.GetPreferredValues(text, 300f, 0f);
                Assert.LessOrEqual(preferred.y, 240f,
                    $"{name} wrap 높이 {preferred.y:F1}px 이 240px 를 초과함 (D절 — 폰트 10→9pt 축소 규칙 검토)");
                tmp.ForceMeshUpdate();
                Assert.IsFalse(tmp.isTextOverflowing, $"{name} 는 300×240 rect 를 넘치면 안 된다");
            }
        }

        // ── G절 6: 글리프 커버리지 (원본 Galmuri TMP 에셋 비파괴 — 임시 clone) ──

        [Test]
        public void Galmuri_Covers_All_Credit_Characters_Via_NonDestructive_Temp_FontAsset()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/Galmuri11.ttf");
            Assert.IsNotNull(font, "Galmuri11.ttf 누락");

            // 원본 Galmuri11 SDF 에셋에는 TryAddCharacters 를 호출하지 않는다(아틀라스 dirty·에셋 오염 —
            // Codex P1). ttf 에서 임시 Dynamic TMP_FontAsset 을 만들어 검증하고 teardown 에서 파기한다.
            var temp = TMP_FontAsset.CreateFontAsset(
                font, 64, 6, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: true);
            Assert.IsNotNull(temp, "임시 TMP_FontAsset 생성 실패");
            try
            {
                string all = CreditsCopy.LeftColumn + "\n" + CreditsCopy.RightColumn + "\n크레딧\n닫기";
                string visible = System.Text.RegularExpressions.Regex.Replace(all, "<[^>]+>", "");
                visible = visible.Replace("\n", "").Replace(" ", "");

                bool covered = temp.TryAddCharacters(visible, out string missing);
                Assert.IsTrue(covered && string.IsNullOrEmpty(missing),
                    $"Galmuri11 이 커버하지 못한 크레딧 문자: '{missing}' — 제약 절 대체 규칙(©→(C), –→-) 적용 필요");
            }
            finally
            {
                if (temp != null)
                {
                    if (temp.atlasTextures != null)
                    {
                        foreach (var atlas in temp.atlasTextures.Where(a => a != null))
                        {
                            Object.DestroyImmediate(atlas);
                        }
                    }
                    if (temp.material != null)
                    {
                        Object.DestroyImmediate(temp.material);
                    }
                    Object.DestroyImmediate(temp);
                }
            }
        }

        // ── G절 7: 라이선스 동봉 — 원본과 바이트 동일 (F절) ─────────────────

        [Test]
        public void Bundled_License_Copies_Are_Byte_Identical_To_Sources()
        {
            string licensesDir = Path.Combine(Application.dataPath, "StreamingAssets", "Licenses");
            AssertBytesEqual(
                Path.Combine(licensesDir, "Galmuri-LICENSE.txt"),
                Path.Combine(Application.dataPath, "Art", "Fonts", "Galmuri-LICENSE.txt"),
                "OFL 전문 (조건 2 — 배포 사본 동봉)");
            AssertBytesEqual(
                Path.Combine(licensesDir, "LICENSE.txt"),
                Path.Combine(RepoRoot, "LICENSE"),
                "루트 MIT 전문 (Codex P0-3)");
            AssertBytesEqual(
                Path.Combine(licensesDir, "THIRD-PARTY-NOTICES.md"),
                Path.Combine(RepoRoot, "THIRD-PARTY-NOTICES.md"),
                "Unity 상표 귀속·비제휴 고지 (오픈 이슈 2 (a)안 — 오너 확정 2026-07-13)");
        }

        static void AssertBytesEqual(string copyPath, string sourcePath, string name)
        {
            Assert.IsTrue(File.Exists(copyPath), $"{name} 사본 누락: {copyPath}");
            Assert.IsTrue(File.Exists(sourcePath), $"{name} 원본 누락: {sourcePath}");
            CollectionAssert.AreEqual(File.ReadAllBytes(sourcePath), File.ReadAllBytes(copyPath),
                $"{name} 사본은 원본과 바이트 동일이어야 한다");
        }

        // ── G절 8: Apply 멱등 ───────────────────────────────────────────────

        [Test]
        public void Repeated_Apply_Is_Idempotent_For_Credits_Panel()
        {
            SceneBuilder.Apply();
            var firstCanvas = Canvas(OpenMainMenu());
            var firstPanel = firstCanvas.transform.Find("Panel_Credits");
            int firstChildCount = CountAllDescendants(firstPanel);
            int firstControllerCount = firstCanvas.GetComponents<CreditsController>().Length;
            int firstOpenListeners = firstCanvas.transform.Find("CreditsButton")
                .GetComponent<Button>().onClick.GetPersistentEventCount();
            int firstCloseListeners = firstPanel.Find("CreditsCloseButton")
                .GetComponent<Button>().onClick.GetPersistentEventCount();

            SceneBuilder.Apply();
            var secondCanvas = Canvas(OpenMainMenu());
            var secondPanel = secondCanvas.transform.Find("Panel_Credits");
            int secondChildCount = CountAllDescendants(secondPanel);
            int secondControllerCount = secondCanvas.GetComponents<CreditsController>().Length;
            int secondOpenListeners = secondCanvas.transform.Find("CreditsButton")
                .GetComponent<Button>().onClick.GetPersistentEventCount();
            int secondCloseListeners = secondPanel.Find("CreditsCloseButton")
                .GetComponent<Button>().onClick.GetPersistentEventCount();

            Assert.AreEqual(firstChildCount, secondChildCount, "재실행해도 크레딧 패널 하위 오브젝트 수가 같아야 한다 (멱등)");
            Assert.AreEqual(1, firstControllerCount, "CreditsController 는 Canvas 에 정확히 1개");
            Assert.AreEqual(1, secondControllerCount, "재실행 후에도 CreditsController 는 1개 (중복 탑재 없음)");
            Assert.AreEqual(0, firstOpenListeners, "크레딧 버튼은 persistent listener 없이 런타임 AddListener 만 사용해야 한다");
            Assert.AreEqual(0, secondOpenListeners, "재실행 후에도 크레딧 버튼 persistent listener 는 0");
            Assert.AreEqual(0, firstCloseListeners, "닫기 버튼 persistent listener 는 0");
            Assert.AreEqual(0, secondCloseListeners, "재실행 후에도 닫기 버튼 persistent listener 는 0");
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
    }
}
