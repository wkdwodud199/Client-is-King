using System.Linq;
using ClientIsKing.DayCycle;
using ClientIsKing.Managers;
using ClientIsKing.Service;
using ClientIsKing.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-111 U6: Night SNS 집행 상태 흐름 — 집행→버튼 잠금→결과 문구→현금 반영.
    /// 각 테스트가 씬을 새로 열어(own [SetUp]) GameState/컨트롤러 상태를 공유하지 않는다
    /// (task-110 씬 상태 격리 교훈 — 구조 테스트는 NightPanelSceneTests 로 분리).
    /// </summary>
    public class NightPanelSnsFlowTests
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
            gm.State.currentPhase = DayPhase.Night; // 도메인 게이트만 만족시키면 충분 — 정산 흐름은 이 테스트의 관심사가 아니다.

            nightPanel.gameObject.SetActive(true);
            // 배치 EditMode 에서는 SetActive/OpenScene 이 OnEnable 을 동기 호출한다는 보장이 없다
            // (task-110 group1/U6 공통 함정) — 직접 호출로 onClick 리스너 등록·초기 Render 를 확정한다.
            TestSceneSupport.ForceOnEnable(controller);
        }

        [Test]
        public void Executing_PhotoFeed_Deducts_Cash_And_Appends_History_Record()
        {
            int cashBefore = gm.State.cash;
            var photoFeedButton = nightPanel.Find("Button_Sns_PhotoFeed").GetComponent<Button>();

            photoFeedButton.onClick.Invoke();

            Assert.AreEqual(cashBefore - 15000, gm.State.cash, "픽쳐그램 집행 후 현금이 15,000원 차감되어야 한다");
            Assert.AreEqual(1, gm.State.snsCampaignHistory.Count, "집행 레코드가 정확히 1건 추가되어야 한다");
            Assert.AreEqual("photo_feed", gm.State.snsCampaignHistory[0].campaignId);
            Assert.AreEqual(gm.State.day, gm.State.snsCampaignHistory[0].executedOnDay);
        }

        [Test]
        public void Executing_One_Campaign_Locks_All_Three_Buttons_Same_Night()
        {
            var photoFeedButton = nightPanel.Find("Button_Sns_PhotoFeed").GetComponent<Button>();
            var shortFormButton = nightPanel.Find("Button_Sns_ShortForm").GetComponent<Button>();
            var localBoardButton = nightPanel.Find("Button_Sns_LocalBoard").GetComponent<Button>();

            photoFeedButton.onClick.Invoke();

            Assert.IsFalse(photoFeedButton.interactable, "집행한 채널 버튼은 잠겨야 한다");
            Assert.IsFalse(shortFormButton.interactable, "1밤 1회 규칙 — 다른 채널 버튼도 잠겨야 한다");
            Assert.IsFalse(localBoardButton.interactable, "1밤 1회 규칙 — 다른 채널 버튼도 잠겨야 한다");
        }

        [Test]
        public void Executing_PhotoFeed_Shows_Executed_Outline_And_Result_Message()
        {
            var photoFeedButton = nightPanel.Find("Button_Sns_PhotoFeed").GetComponent<Button>();
            var outline = photoFeedButton.GetComponent<UnityEngine.UI.Outline>();
            var snsInfoText = nightPanel.Find("SnsInfoText").GetComponent<TMPro.TMP_Text>();
            var label = photoFeedButton.GetComponentInChildren<TMPro.TMP_Text>();

            photoFeedButton.onClick.Invoke();

            Assert.IsTrue(outline.enabled, "집행 완료 버튼은 outline 이 켜져야 한다(색+문구 병용, F2)");
            Assert.IsTrue(label.text.Contains("집행 완료"), $"버튼 2행 라벨이 '집행 완료' 로 교체되어야 함: '{label.text}'");
            Assert.IsTrue(snsInfoText.text.Contains("집행 완료"), $"결과 문구에 집행 완료가 표시되어야 함: '{snsInfoText.text}'");
            Assert.IsTrue(snsInfoText.text.Contains("SNS 유입"), $"결과 문구에 내일 유입 예고가 있어야 함: '{snsInfoText.text}'");
        }

        [Test]
        public void Follower_Text_Increases_After_Execution()
        {
            var followerText = nightPanel.Find("FollowerText").GetComponent<TMPro.TMP_Text>();
            string before = followerText.text;
            Assert.IsTrue(before.Contains("120"), $"초기 팔로워는 120명이어야 함: '{before}'");

            var photoFeedButton = nightPanel.Find("Button_Sns_PhotoFeed").GetComponent<Button>();
            photoFeedButton.onClick.Invoke();

            Assert.IsTrue(followerText.text.Contains("145"), $"픽쳐그램 첫 집행 후 팔로워 120+25=145명이어야 함: '{followerText.text}'");
        }

        [Test]
        public void Insufficient_Cash_Disables_All_Buttons()
        {
            gm.State.cash = 1000; // 3종 모두 비용(7,000/12,000/15,000)보다 적음
            TestSceneSupport.ForceOnEnable(controller);

            foreach (var name in new[] { "Button_Sns_PhotoFeed", "Button_Sns_ShortForm", "Button_Sns_LocalBoard" })
            {
                var button = nightPanel.Find(name).GetComponent<Button>();
                Assert.IsFalse(button.interactable, $"{name} 은 자금 부족 시 비활성이어야 한다");
            }
        }

        [Test]
        public void GameEvents_SNSCampaignExecuted_Fires_Exactly_Once_On_Success()
        {
            int fireCount = 0;
            System.Action<string> handler = _ => fireCount++;
            GameEvents.SNSCampaignExecuted += handler;
            try
            {
                var photoFeedButton = nightPanel.Find("Button_Sns_PhotoFeed").GetComponent<Button>();
                photoFeedButton.onClick.Invoke();
                Assert.AreEqual(1, fireCount, "집행 성공 시 SNSCampaignExecuted 가 정확히 1회 발행되어야 한다");
            }
            finally
            {
                GameEvents.SNSCampaignExecuted -= handler;
            }
        }

        [Test]
        public void Bankrupt_State_Disables_All_Sns_Buttons()
        {
            gm.State.isBankrupt = true;
            TestSceneSupport.ForceOnEnable(controller);

            foreach (var name in new[] { "Button_Sns_PhotoFeed", "Button_Sns_ShortForm", "Button_Sns_LocalBoard" })
            {
                var button = nightPanel.Find(name).GetComponent<Button>();
                Assert.IsFalse(button.interactable, $"{name} 은 파산 상태에서 비활성이어야 한다");
            }
        }

        // ── task-113 U5: 자동 저장 표시 라인(G4) ─────────────────────────────
        // 저장 성공/실패에 따른 문구·색 전환은 GameManager.LastAutoSave* 값을 그대로 읽는데, 그 값은
        // AutoSave()(Application.isPlaying 가드)만 갱신하고 EditMode 는 항상 isPlaying==false 이므로
        // 이 상태 흐름은 재현 불가능하다 — PlayMode(SaveLoadPlayModeTests)가 실제 흐름을 검증한다.
        // 여기서는 "저장 시도 기록이 없으면(fixture 기본값) 기존 1행만 유지"하는 정적 경우만 확인한다.

        [Test]
        public void StatusText_Shows_Base_Line_Only_When_No_Save_Attempt_Recorded()
        {
            var statusText = nightPanel.Find("StatusText").GetComponent<TMPro.TMP_Text>();
            Assert.AreEqual("내일 영업 준비 완료 — '다음 날 ▶' 버튼으로 진행하세요.", statusText.text);
        }
    }
}
