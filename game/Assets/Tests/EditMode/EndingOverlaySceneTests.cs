using System.Linq;
using System.Reflection;
using ClientIsKing.DayCycle;
using ClientIsKing.EditorTools;
using ClientIsKing.Managers;
using ClientIsKing.Social;
using ClientIsKing.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-115 U4: Shop 엔딩 오버레이 — D1 표 전수(존재·좌표·크기·색·raycast·초기 비활성·최상단
    /// sibling), RefreshNow 3분기(None 숨김/클리어/파산 — 문구·색), worst-case 폭 ≤460px,
    /// 버튼 리스너 배선(OnEnable/OnDisable 쌍)을 검증한다. 클릭 → MainMenu 실제 전환은
    /// PlayMode(EndingPlayModeTests, G3)가 담당한다.
    /// </summary>
    public class EndingOverlaySceneTests
    {
        [OneTimeSetUp]
        public void BuildScenesOnce()
        {
            SceneBuilder.Apply();
        }

        static Scene OpenShop()
        {
            return EditorSceneManager.OpenScene(SceneBuilder.ShopPath, OpenSceneMode.Single);
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

        // ── D1 표 전수: 정적 구조 ────────────────────────────────────────────

        [Test]
        public void Ending_Overlay_Is_Inactive_Last_Sibling_With_Modal_Raycast_Dim()
        {
            var canvasGo = Canvas(OpenShop());
            var overlay = canvasGo.transform.Find("Panel_Ending");
            Assert.IsNotNull(overlay, "Panel_Ending 누락");
            Assert.IsFalse(overlay.gameObject.activeSelf, "엔딩 오버레이는 초기 비활성이어야 한다 (D1)");
            Assert.AreEqual(canvasGo.transform.childCount - 1, overlay.GetSiblingIndex(),
                "엔딩 오버레이는 canvas 의 마지막 자식(렌더/raycast 최상단)이어야 한다 (D1)");
            Assert.Greater(overlay.GetSiblingIndex(),
                canvasGo.transform.Find("Panel_GenreSelection").GetSiblingIndex(),
                "엔딩 오버레이는 장르 modal 보다도 위여야 한다 (D1)");

            AssertRect(overlay, 0f, 0f, 640f, 360f, "Panel_Ending");
            var dim = overlay.GetComponent<Image>();
            Assert.IsNotNull(dim, "Panel_Ending dim Image 누락");
            Assert.IsTrue(dim.raycastTarget, "dim 은 하부 UI 클릭을 차단해야 한다 (raycastTarget true, 모달)");
            // Ink Navy #16202A alpha ≈0.92
            Assert.AreEqual(0x16 / 255f, dim.color.r, 0.002f, "dim R (Ink Navy)");
            Assert.AreEqual(0x20 / 255f, dim.color.g, 0.002f, "dim G (Ink Navy)");
            Assert.AreEqual(0x2A / 255f, dim.color.b, 0.002f, "dim B (Ink Navy)");
            Assert.AreEqual(0.92f, dim.color.a, 0.005f, "dim alpha ≈0.92");

            Assert.IsNotNull(canvasGo.GetComponent<EndingOverlayController>(),
                "EndingOverlayController 는 Canvas 탑재여야 한다 (오버레이 비활성에도 폴링 유지 — D2)");
        }

        [Test]
        public void Ending_Overlay_Children_Match_D1_Table()
        {
            var canvasGo = Canvas(OpenShop());
            var overlay = canvasGo.transform.Find("Panel_Ending");

            var title = overlay.Find("EndingTitleText");
            Assert.IsNotNull(title, "EndingTitleText 누락");
            AssertRect(title, 0f, 70f, 400f, 40f, "EndingTitleText");
            Assert.AreEqual(28f, title.GetComponent<TMP_Text>().fontSize, 0.01f, "EndingTitleText 28pt");

            var stats = overlay.Find("EndingStatsText");
            Assert.IsNotNull(stats, "EndingStatsText 누락");
            AssertRect(stats, 0f, 26f, 460f, 32f, "EndingStatsText");
            Assert.AreEqual(12f, stats.GetComponent<TMP_Text>().fontSize, 0.01f, "EndingStatsText 12pt");
            AssertColor(stats.GetComponent<TMP_Text>().color, 0xF4, 0xE5, 0xC2, "EndingStatsText (Steam Cream)");

            var message = overlay.Find("EndingMessageText");
            Assert.IsNotNull(message, "EndingMessageText 누락");
            AssertRect(message, 0f, -20f, 460f, 36f, "EndingMessageText");
            Assert.AreEqual(11f, message.GetComponent<TMP_Text>().fontSize, 0.01f, "EndingMessageText 11pt");
            AssertColor(message.GetComponent<TMP_Text>().color, 0xF4, 0xE5, 0xC2, "EndingMessageText (Steam Cream)");

            var button = overlay.Find("EndingMainMenuButton");
            Assert.IsNotNull(button, "EndingMainMenuButton 누락");
            Assert.IsNotNull(button.GetComponent<Button>(), "EndingMainMenuButton 은 Button 이어야 한다");
            AssertRect(button, 0f, -72f, 200f, 40f, "EndingMainMenuButton");
            Assert.AreEqual("메인 메뉴로 ▶", button.GetComponentInChildren<TMP_Text>(true).text,
                "버튼 카피 (D1 — Codex 소유 UX copy)");
        }

        [Test]
        public void Controller_References_Are_Injected_In_Saved_Scene()
        {
            var canvasGo = Canvas(OpenShop());
            var overlay = canvasGo.transform.Find("Panel_Ending");
            var controller = canvasGo.GetComponent<EndingOverlayController>();

            var so = new UnityEditor.SerializedObject(controller);
            Assert.AreSame(overlay.gameObject, so.FindProperty("overlayRoot").objectReferenceValue, "overlayRoot 배선");
            Assert.AreSame(overlay.Find("EndingTitleText").GetComponent<TMP_Text>(),
                so.FindProperty("titleText").objectReferenceValue, "titleText 배선");
            Assert.AreSame(overlay.Find("EndingStatsText").GetComponent<TMP_Text>(),
                so.FindProperty("statsText").objectReferenceValue, "statsText 배선");
            Assert.AreSame(overlay.Find("EndingMessageText").GetComponent<TMP_Text>(),
                so.FindProperty("messageText").objectReferenceValue, "messageText 배선");
            Assert.AreSame(overlay.Find("EndingMainMenuButton").GetComponent<Button>(),
                so.FindProperty("mainMenuButton").objectReferenceValue, "mainMenuButton 배선");
        }

        // ── RefreshNow 3분기 (D2 — 상태 폴링, 표시 전용) ─────────────────────

        static (GameManager gm, EndingOverlayController controller, GameObject overlay,
            TMP_Text title, TMP_Text stats, TMP_Text message) OpenShopForFlow()
        {
            var gmGo = TestSceneSupport.OpenShopSceneWithLiveSingletons();
            var gm = gmGo.GetComponent<GameManager>();
            gm.StartNewGame();
            var canvasGo = gmGo.scene.GetRootGameObjects().First(go => go.name == "Canvas");
            var controller = canvasGo.GetComponent<EndingOverlayController>();
            var overlay = canvasGo.transform.Find("Panel_Ending").gameObject;
            var title = overlay.transform.Find("EndingTitleText").GetComponent<TMP_Text>();
            var stats = overlay.transform.Find("EndingStatsText").GetComponent<TMP_Text>();
            var message = overlay.transform.Find("EndingMessageText").GetComponent<TMP_Text>();
            return (gm, controller, overlay, title, stats, message);
        }

        [Test]
        public void RefreshNow_Keeps_Overlay_Hidden_While_Run_Is_Not_Ended()
        {
            var (gm, controller, overlay, _, _, _) = OpenShopForFlow();
            Assert.AreEqual(RunEndingStatus.None, EndingOps.GetStatus(gm.State));

            controller.RefreshNow();

            Assert.IsFalse(overlay.activeSelf, "런 진행 중(None)에는 오버레이가 계속 숨김이어야 한다");
        }

        [Test]
        public void RefreshNow_Shows_Clear_Overlay_With_D1_Copy_And_Colors()
        {
            var (gm, controller, overlay, title, stats, message) = OpenShopForFlow();
            gm.State.day = EndingOps.ClearTargetDays;
            gm.State.currentPhase = DayPhase.Settlement;
            gm.State.serviceDay = EndingOps.ClearTargetDays;
            gm.State.settlementDay = EndingOps.ClearTargetDays;
            gm.State.daysCompleted = EndingOps.ClearTargetDays;
            gm.State.cash = 150000;

            controller.RefreshNow();

            Assert.IsTrue(overlay.activeSelf, "클리어 상태에서 오버레이가 표시되어야 한다");
            Assert.AreEqual("데모 클리어!", title.text);
            AssertColor(title.color, 0xE5, 0xA8, 0x4B, "클리어 타이틀 (Brass Amber)");

            int follower = SNSCampaignOps.CalculateFollowerDisplay(gm.State.snsCampaignHistory);
            Assert.AreEqual(
                $"영업 7일 · 최종 잔액 150,000원\n총 손익 +120,000원 · 팔로워 {follower:N0}명",
                stats.text, "요약 표기는 BuildSummary DTO 그대로 (부호는 netText 규약 — ≥0 이면 +)");
            AssertColor(stats.color, 0xF4, 0xE5, 0xC2, "요약 (Steam Cream)");

            Assert.AreEqual("목표 7일 영업을 달성했습니다 — 데모는 여기까지! 플레이해 주셔서 감사합니다.",
                message.text, "클리어 메시지 카피 (D1 — Codex 소유 UX copy)");
            AssertColor(message.color, 0xF4, 0xE5, 0xC2, "클리어 메시지 (Steam Cream)");
        }

        [Test]
        public void RefreshNow_Shows_GameOver_Overlay_With_D1_Copy_And_Colors()
        {
            var (gm, controller, overlay, title, stats, message) = OpenShopForFlow();
            gm.State.day = 2;
            gm.State.daysCompleted = 1;
            gm.State.cash = 0;
            gm.State.isBankrupt = true;
            gm.State.bankruptcyDay = 2;
            gm.State.bankruptcyReason = "Day 2 운영비 15,000원 미납 (부족액 15,000원)";

            controller.RefreshNow();

            Assert.IsTrue(overlay.activeSelf, "파산 상태에서 오버레이가 표시되어야 한다");
            Assert.AreEqual("게임 오버", title.text);
            AssertColor(title.color, 0xA9, 0x3E, 0x58, "게임 오버 타이틀 (Warning Plum)");

            int follower = SNSCampaignOps.CalculateFollowerDisplay(gm.State.snsCampaignHistory);
            Assert.AreEqual(
                $"영업 1일 · 최종 잔액 0원\n총 손익 -30,000원 · 팔로워 {follower:N0}명",
                stats.text, "파산 손익은 음수 정직 표기 (NetProfit = cash − StartingCash)");

            Assert.AreEqual(
                "<color=#A93E58>Day 2 운영비 15,000원 미납 (부족액 15,000원)</color>\n새 게임으로 다시 도전하세요.",
                message.text, "파산 1행(사유)은 Warning Plum rich text, 2행은 재도전 안내 (D1)");
            AssertColor(message.color, 0xF4, 0xE5, 0xC2, "파산 메시지 기본색 (Steam Cream)");
        }

        [Test]
        public void RefreshNow_Hides_Overlay_When_State_Returns_To_None()
        {
            var (gm, controller, overlay, _, _, _) = OpenShopForFlow();
            gm.State.daysCompleted = EndingOps.ClearTargetDays;
            controller.RefreshNow();
            Assert.IsTrue(overlay.activeSelf, "선행 조건 — 클리어 표시");

            gm.StartNewGame(); // 새 런 fixture — 상태가 None 으로 돌아간다
            controller.RefreshNow();

            Assert.IsFalse(overlay.activeSelf, "None 인데 표시 중이면 숨겨야 한다 (새 런 대비, D2)");
        }

        // ── worst-case 폭 ≤460px (task-112 F5 전례 — Galmuri TMP GetPreferredValues) ──

        [Test]
        public void Ending_Texts_WorstCase_Fit_Within_460px()
        {
            var canvasGo = Canvas(OpenShop());
            var overlay = canvasGo.transform.Find("Panel_Ending");

            var stats = overlay.Find("EndingStatsText").GetComponent<TMP_Text>();
            string statsWorst = "영업 7일 · 최종 잔액 999,999원\n총 손익 +969,999원 · 팔로워 999,999명";
            stats.text = statsWorst;
            stats.ForceMeshUpdate();
            var statsPreferred = stats.GetPreferredValues(statsWorst);
            Assert.LessOrEqual(statsPreferred.x, 460f,
                $"요약 worst-case 폭 {statsPreferred.x:F1}px 이 460px 를 초과함: '{statsWorst}'");

            var message = overlay.Find("EndingMessageText").GetComponent<TMP_Text>();
            string clearWorst = "목표 7일 영업을 달성했습니다 — 데모는 여기까지! 플레이해 주셔서 감사합니다.";
            message.text = clearWorst;
            message.ForceMeshUpdate();
            var clearPreferred = message.GetPreferredValues(clearWorst);
            Assert.LessOrEqual(clearPreferred.x, 460f,
                $"클리어 메시지 폭 {clearPreferred.x:F1}px 이 460px 를 초과함: '{clearWorst}'");

            // 최장 파산 사유 — 임대료 인상(+15%) 반영 운영비 자릿수 포함 (SettlementOps 사유 포맷).
            string bankruptWorst = "<color=#A93E58>Day 7 운영비 17,250원 미납 (부족액 17,250원)</color>\n새 게임으로 다시 도전하세요.";
            message.text = bankruptWorst;
            message.ForceMeshUpdate();
            var bankruptPreferred = message.GetPreferredValues(bankruptWorst);
            Assert.LessOrEqual(bankruptPreferred.x, 460f,
                $"파산 메시지 worst-case 폭 {bankruptPreferred.x:F1}px 이 460px 를 초과함: '{bankruptWorst}'");
        }

        // ── 버튼 리스너 배선 (OnEnable/OnDisable 쌍 — 런타임 AddListener 규약) ──

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
        public void MainMenu_Button_Listener_Is_Registered_By_OnEnable_And_Removed_By_OnDisable()
        {
            var (gm, controller, overlay, _, _, _) = OpenShopForFlow();
            var button = overlay.transform.Find("EndingMainMenuButton").GetComponent<Button>();

            Assert.AreEqual(0, button.onClick.GetPersistentEventCount(),
                "persistent listener 는 0 — 런타임 AddListener 만 사용한다 (멱등 규약)");
            var calls = RuntimeCalls(button.onClick);
            Assert.AreEqual(0, calls.Count, "OnEnable 전에는 런타임 리스너가 없어야 한다");

            TestSceneSupport.ForceOnEnable(controller);
            Assert.AreEqual(1, calls.Count, "OnEnable 이 메인 메뉴 리스너를 정확히 1개 등록해야 한다");

            // 리스너 대상 검증 — controller.OnMainMenuClicked → GameManager.LoadMainMenuScene (D2).
            var delegateField = calls[0].GetType().GetField("Delegate", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(delegateField, "InvokableCall.Delegate 리플렉션 실패");
            var action = (System.Delegate)delegateField.GetValue(calls[0]);
            Assert.AreEqual("OnMainMenuClicked", action.Method.Name, "버튼은 OnMainMenuClicked 로 배선되어야 한다");
            Assert.AreSame(controller, action.Target, "리스너 대상은 EndingOverlayController 자신이어야 한다");

            var onDisable = typeof(EndingOverlayController)
                .GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance);
            onDisable.Invoke(controller, null);
            Assert.AreEqual(0, calls.Count, "OnDisable 이 리스너를 해제해야 한다 (쌍 규약)");
        }
    }
}
