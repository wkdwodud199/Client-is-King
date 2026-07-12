using System;
using ClientIsKing.DayCycle;
using ClientIsKing.Social;
using NUnit.Framework;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-115 C2: EndingOps 파생 규칙 — GameState 필드 추가 없이 isBankrupt/daysCompleted 에서만
    /// 엔딩 상태를 파생한다는 계약을 고정한다(파산 우선, ClearTargetDays=7, BuildSummary 필드 전수).
    /// </summary>
    public class EndingOpsTests
    {
        [Test]
        public void ClearTargetDays_Is_Pinned_To_7()
        {
            Assert.AreEqual(7, EndingOps.ClearTargetDays);
        }

        [Test]
        public void GetStatus_Throws_On_Null_State()
        {
            Assert.Throws<ArgumentNullException>(() => EndingOps.GetStatus(null));
        }

        [Test]
        public void BuildSummary_Throws_On_Null_State()
        {
            Assert.Throws<ArgumentNullException>(() => EndingOps.BuildSummary(null));
        }

        // ── GetStatus 파생 매트릭스 — daysCompleted 0/N-1/N/N+1 × isBankrupt ──────

        [TestCase(0, false, RunEndingStatus.None)]
        [TestCase(EndingOps.ClearTargetDays - 1, false, RunEndingStatus.None)]
        [TestCase(EndingOps.ClearTargetDays, false, RunEndingStatus.Cleared)]
        [TestCase(EndingOps.ClearTargetDays + 1, false, RunEndingStatus.Cleared)]
        [TestCase(0, true, RunEndingStatus.Bankrupt)]
        [TestCase(EndingOps.ClearTargetDays - 1, true, RunEndingStatus.Bankrupt)]
        [TestCase(EndingOps.ClearTargetDays, true, RunEndingStatus.Bankrupt)]
        [TestCase(EndingOps.ClearTargetDays + 1, true, RunEndingStatus.Bankrupt)]
        public void GetStatus_Derives_From_Bankrupt_And_DaysCompleted(int daysCompleted, bool isBankrupt, RunEndingStatus expected)
        {
            var state = new GameState { daysCompleted = daysCompleted, isBankrupt = isBankrupt };
            Assert.AreEqual(expected, EndingOps.GetStatus(state));
        }

        [Test]
        public void GetStatus_Bankrupt_Takes_Priority_Even_When_DaysCompleted_Reaches_Target()
        {
            // 파산은 정산 성공 분기에서만 daysCompleted 를 갱신하므로 구조적으로 동시 성립 불가하지만,
            // 분기 순서 계약 자체(파산 우선)를 인위적 fixture 로도 고정한다.
            var state = new GameState { daysCompleted = EndingOps.ClearTargetDays + 3, isBankrupt = true };
            Assert.AreEqual(RunEndingStatus.Bankrupt, EndingOps.GetStatus(state));
        }

        // ── IsCleared 원시형 동치 ──────────────────────────────────────────────

        [TestCase(0, false, false)]
        [TestCase(EndingOps.ClearTargetDays - 1, false, false)]
        [TestCase(EndingOps.ClearTargetDays, false, true)]
        [TestCase(EndingOps.ClearTargetDays + 1, false, true)]
        [TestCase(EndingOps.ClearTargetDays, true, false)]
        public void IsCleared_Matches_GetStatus_Equivalence(int daysCompleted, bool isBankrupt, bool expected)
        {
            Assert.AreEqual(expected, EndingOps.IsCleared(daysCompleted, isBankrupt));

            var state = new GameState { daysCompleted = daysCompleted, isBankrupt = isBankrupt };
            Assert.AreEqual(expected, EndingOps.GetStatus(state) == RunEndingStatus.Cleared);
        }

        // ── IsRunEnded ──────────────────────────────────────────────────────

        [Test]
        public void IsRunEnded_False_When_None()
        {
            var state = new GameState { daysCompleted = 0, isBankrupt = false };
            Assert.IsFalse(EndingOps.IsRunEnded(state));
        }

        [Test]
        public void IsRunEnded_True_When_Cleared_Or_Bankrupt()
        {
            var cleared = new GameState { daysCompleted = EndingOps.ClearTargetDays, isBankrupt = false };
            Assert.IsTrue(EndingOps.IsRunEnded(cleared));

            var bankrupt = new GameState { daysCompleted = 0, isBankrupt = true };
            Assert.IsTrue(EndingOps.IsRunEnded(bankrupt));
        }

        // ── BuildSummary 필드 전수 ──────────────────────────────────────────

        [Test]
        public void BuildSummary_Fields_Match_State_For_Cleared_Run()
        {
            var state = new GameState
            {
                daysCompleted = EndingOps.ClearTargetDays,
                cash = 175000,
                isBankrupt = false,
                bankruptcyReason = "",
            };

            var summary = EndingOps.BuildSummary(state);

            Assert.AreEqual(RunEndingStatus.Cleared, summary.Status);
            Assert.AreEqual(EndingOps.ClearTargetDays, summary.DaysCompleted);
            Assert.AreEqual(175000, summary.FinalCash);
            Assert.AreEqual(175000 - GameState.StartingCash, summary.NetProfit);
            Assert.AreEqual(SNSCampaignOps.CalculateFollowerDisplay(state.snsCampaignHistory), summary.FollowerDisplay);
            Assert.AreEqual("", summary.BankruptcyReason);
        }

        [Test]
        public void BuildSummary_NetProfit_Is_Negative_For_Bankrupt_Run()
        {
            var state = new GameState
            {
                daysCompleted = 1,
                cash = 0,
                isBankrupt = true,
                bankruptcyDay = 2,
                bankruptcyReason = "Day 2 운영비 28,000원 미납 (부족액 26,000원)",
            };

            var summary = EndingOps.BuildSummary(state);

            Assert.AreEqual(RunEndingStatus.Bankrupt, summary.Status);
            Assert.AreEqual(0, summary.FinalCash);
            Assert.AreEqual(0 - GameState.StartingCash, summary.NetProfit);
            Assert.Less(summary.NetProfit, 0, "파산 순손익은 음수로 정직하게 표기");
            Assert.AreEqual("Day 2 운영비 28,000원 미납 (부족액 26,000원)", summary.BankruptcyReason);
        }

        [Test]
        public void BuildSummary_FollowerDisplay_Matches_SNSCampaignOps_With_History()
        {
            var state = new GameState { daysCompleted = EndingOps.ClearTargetDays, cash = 100000 };
            state.snsCampaignHistory.Add(new SNSCampaignRecord
            {
                campaignId = "short_form",
                executedOnDay = 3,
                bonusOrderCount = 2,
                effectiveMilliReach = 1200,
                followerGain = SNSCampaignOps.CalculateFollowerGain(1200),
            });

            var summary = EndingOps.BuildSummary(state);

            Assert.AreEqual(SNSCampaignOps.CalculateFollowerDisplay(state.snsCampaignHistory), summary.FollowerDisplay);
            Assert.Greater(summary.FollowerDisplay, 120, "집행 기록이 있으면 기본값(120)보다 커야 한다");
        }
    }
}
