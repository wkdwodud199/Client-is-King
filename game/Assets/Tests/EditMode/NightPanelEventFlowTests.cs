using System.Linq;
using ClientIsKing.DayCycle;
using ClientIsKing.Managers;
using ClientIsKing.UI;
using NUnit.Framework;
using UnityEngine;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-112 U7: Night 예고 라인 상태 흐름 — Day 1 "이벤트 없음" → Day 2 Night 폭등 예고 문구·색.
    /// 구조/상태 fixture 분리 규약 유지(구조는 NightPanelSceneTests, 상태는 이 클래스).
    /// C4 스케줄: day 2 는 무이벤트(occRoll==450), day 3 신규 폭등 — 이 클래스는 Night 예고(day+1) 이므로
    /// Day1 Night(예고 day2=없음)와 Day2 Night(예고 day3=폭등)를 검증한다.
    /// </summary>
    public class NightPanelEventFlowTests
    {
        Transform nightPanel;
        NightPanelController controller;
        GameManager gm;

        [SetUp]
        public void OpenFreshShopAtNight()
        {
            var gameManagerGo = TestSceneSupport.OpenShopSceneWithLiveSingletons();
            var canvasGo = gameManagerGo.scene.GetRootGameObjects().First(go => go.name == "Canvas");
            nightPanel = canvasGo.transform.Find("Panel_Night");
            controller = nightPanel.GetComponent<NightPanelController>();

            gm = gameManagerGo.GetComponent<GameManager>();
            gm.StartNewGame();
            gm.State.currentPhase = DayPhase.Night;

            nightPanel.gameObject.SetActive(true);
            TestSceneSupport.ForceOnEnable(controller);
        }

        [Test]
        public void Day1_Night_Shows_No_Event_Forecast()
        {
            // gm.State.day == 1 (기본) — 예고 대상은 day 2, C4 표상 무이벤트.
            var eventNoticeText = nightPanel.Find("EventNoticeText").GetComponent<TMPro.TMP_Text>();
            Assert.AreEqual("내일 예고된 이벤트 없음", eventNoticeText.text);
        }

        [Test]
        public void Day2_Night_Shows_Surge_Forecast_With_Warning_Color()
        {
            gm.State.day = 2; // 예고 대상은 day 3, C4 표상 재료값 폭등 신규 활성화.
            TestSceneSupport.ForceOnEnable(controller);

            var eventNoticeText = nightPanel.Find("EventNoticeText").GetComponent<TMPro.TMP_Text>();
            Assert.IsTrue(eventNoticeText.text.Contains("[예고]"), $"'{eventNoticeText.text}'");
            Assert.IsTrue(eventNoticeText.text.Contains("재료값 폭등"), $"'{eventNoticeText.text}'");
            Assert.IsTrue(eventNoticeText.text.Contains("+35%"), $"'{eventNoticeText.text}'");

            var warningPlum = new Color32(0xA9, 0x3E, 0x58, 0xFF);
            Assert.AreEqual((Color)warningPlum, eventNoticeText.color, "위기 이벤트는 Warning Plum");
        }

        [Test]
        public void Day4_Night_Shows_Continuing_Surge_Notice()
        {
            gm.State.day = 3;
            gm.State.activeEvents.Add(new ClientIsKing.Events.ActiveEventState { eventId = "ingredient_price_surge", remainingDays = 2 });
            TestSceneSupport.ForceOnEnable(controller);

            var eventNoticeText = nightPanel.Find("EventNoticeText").GetComponent<TMPro.TMP_Text>();
            Assert.IsTrue(eventNoticeText.text.Contains("[지속]"), $"'{eventNoticeText.text}'");
            Assert.IsTrue(eventNoticeText.text.Contains("내일까지"), $"remaining 1 -> 내일까지: '{eventNoticeText.text}'");
        }

        [Test]
        public void Day4_Night_Shows_Group_Customers_Forecast_With_Opportunity_Color()
        {
            gm.State.day = 4; // 예고 대상 day 5, C4 표상 단체 손님 신규 활성화.
            TestSceneSupport.ForceOnEnable(controller);

            var eventNoticeText = nightPanel.Find("EventNoticeText").GetComponent<TMPro.TMP_Text>();
            Assert.IsTrue(eventNoticeText.text.Contains("단체 손님"), $"'{eventNoticeText.text}'");

            var brassAmber = new Color32(0xE5, 0xA8, 0x4B, 0xFF);
            Assert.AreEqual((Color)brassAmber, eventNoticeText.color, "단체 손님(기회)은 Brass Amber");
        }

        [Test]
        public void Day10_Night_Warns_NextDay_OperatingCost_With_Rent_Increase()
        {
            // 경고 라인은 오늘 밤 SNS 집행 후 결과 문구에만 표시된다(RenderInfoLine 계약) — 집행을 먼저 확정한다.
            gm.State.day = 10; // 예고 대상 day 11, C4 표상 임대료 인상 신규 활성화(영구).
            var service = ClientIsKing.Service.ServiceManager.Instance;
            Assert.IsTrue(service.TryExecuteSnsCampaign("local_board", out var execResult), execResult.Message);
            gm.State.cash = 5000; // 다음날 운영비(13,800원) 대비 부족해 경고 문구가 표시되도록 설정.
            TestSceneSupport.ForceOnEnable(controller);

            var snsInfoText = nightPanel.Find("SnsInfoText").GetComponent<TMPro.TMP_Text>();
            Assert.IsTrue(snsInfoText.text.Contains("13,800"), $"임대료 인상 반영 운영비 경고가 있어야 함: '{snsInfoText.text}'");
        }
    }
}
