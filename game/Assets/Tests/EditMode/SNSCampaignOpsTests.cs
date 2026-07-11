using System;
using System.Collections.Generic;
using System.Linq;
using ClientIsKing.Data;
using ClientIsKing.DayCycle;
using ClientIsKing.Genre;
using ClientIsKing.Social;
using NUnit.Framework;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-111 U1: SNS 마케팅 순수 규칙(SNSCampaignOps) 검증 — 밀리 투영, 반복 감쇠, 보너스 주문·팔로워,
    /// 연령/성별 타겟 매칭, 집행 게이트, 미리보기(top-target), history→DayModifier 재구성.
    /// 시드 값은 B1 확정 채널값(비용 재시드 전 도달/감쇠/친화)과 실제 CustomerArchetypeDef 값을 코드 상수로 재현한다.
    /// </summary>
    public class SNSCampaignOpsTests
    {
        // ── B1 확정 채널 시드 (비용은 U5 재시드 대상 — 여기서는 도달/감쇠/친화만 사용) ──

        static SNSCampaignDefInput PhotoFeed() => new SNSCampaignDefInput
        {
            Id = "photo_feed",
            DisplayName = "픽쳐그램",
            BaseCost = 15000,
            BaseReach = 0.25f,
            RepeatDecay = 0.85f,
            AudienceAffinities = new List<SNSRawAffinityInput>
            {
                new SNSRawAffinityInput(AgeBand.Twenties, GenderTarget.Female, 1.5f),
                new SNSRawAffinityInput(AgeBand.ThirtiesForties, GenderTarget.Female, 1.2f),
                new SNSRawAffinityInput(AgeBand.Teens, GenderTarget.All, 1.1f),
            },
        };

        static SNSCampaignDefInput ShortForm() => new SNSCampaignDefInput
        {
            Id = "short_form",
            DisplayName = "숏핑",
            BaseCost = 12000,
            BaseReach = 0.30f,
            RepeatDecay = 0.80f,
            AudienceAffinities = new List<SNSRawAffinityInput>
            {
                new SNSRawAffinityInput(AgeBand.Teens, GenderTarget.All, 1.6f),
                new SNSRawAffinityInput(AgeBand.Twenties, GenderTarget.All, 1.3f),
            },
        };

        static SNSCampaignDefInput LocalBoard() => new SNSCampaignDefInput
        {
            Id = "local_board",
            DisplayName = "동네게시판",
            BaseCost = 7000,
            BaseReach = 0.15f,
            RepeatDecay = 0.90f,
            AudienceAffinities = new List<SNSRawAffinityInput>
            {
                new SNSRawAffinityInput(AgeBand.FiftiesPlus, GenderTarget.All, 1.5f),
                new SNSRawAffinityInput(AgeBand.ThirtiesForties, GenderTarget.All, 1.25f),
            },
        };

        // 실제 archetype: student(Teens,All,1.0) / office_worker(Twenties,All,1.2) /
        // family_parent(ThirtiesForties,All,0.9) / senior_regular(FiftiesPlus,All,0.7)
        static List<CustomerDefInput> SeedCustomers() => new List<CustomerDefInput>
        {
            new CustomerDefInput { Id = "student", BaseSpawnWeight = 1.0f, AgeBand = AgeBand.Teens, Gender = GenderTarget.All },
            new CustomerDefInput { Id = "office_worker", BaseSpawnWeight = 1.2f, AgeBand = AgeBand.Twenties, Gender = GenderTarget.All },
            new CustomerDefInput { Id = "family_parent", BaseSpawnWeight = 0.9f, AgeBand = AgeBand.ThirtiesForties, Gender = GenderTarget.All },
            new CustomerDefInput { Id = "senior_regular", BaseSpawnWeight = 0.7f, AgeBand = AgeBand.FiftiesPlus, Gender = GenderTarget.All },
        };

        static GameState NightState(int day = 1, int cash = 30000)
        {
            return new GameState { day = day, currentPhase = DayPhase.Night, cash = cash };
        }

        // ── MulMilliHalfUp ──────────────────────────────────────────────────

        [Test]
        public void MulMilliHalfUp_Matches_RoundHalfUp_Equivalent()
        {
            Assert.AreEqual(213, SNSCampaignOps.MulMilliHalfUp(250, 850));
            Assert.AreEqual(0, SNSCampaignOps.MulMilliHalfUp(0, 850));
        }

        // ── C1: 밀리 투영 ───────────────────────────────────────────────────

        [Test]
        public void ProjectMilli_Matches_C1_Table()
        {
            Assert.AreEqual(250, SNSCampaignOps.ProjectMilli(0.25f), "photo reachMilli0");
            Assert.AreEqual(850, SNSCampaignOps.ProjectMilli(0.85f), "photo decayMilli");
            Assert.AreEqual(300, SNSCampaignOps.ProjectMilli(0.30f), "short reachMilli0");
            Assert.AreEqual(800, SNSCampaignOps.ProjectMilli(0.80f), "short decayMilli");
            Assert.AreEqual(150, SNSCampaignOps.ProjectMilli(0.15f), "local reachMilli0");
            Assert.AreEqual(900, SNSCampaignOps.ProjectMilli(0.90f), "local decayMilli");
            Assert.AreEqual(1500, SNSCampaignOps.ProjectMilli(1.5f));
            Assert.AreEqual(1200, SNSCampaignOps.ProjectMilli(1.2f));
            Assert.AreEqual(1100, SNSCampaignOps.ProjectMilli(1.1f));
            Assert.AreEqual(1600, SNSCampaignOps.ProjectMilli(1.6f));
            Assert.AreEqual(1300, SNSCampaignOps.ProjectMilli(1.3f));
            Assert.AreEqual(1250, SNSCampaignOps.ProjectMilli(1.25f));
        }

        // ── C2: 반복 감쇠 체인 ──────────────────────────────────────────────

        [Test]
        public void CalculateEffectiveMilliReach_Matches_C2_Chain_Photo()
        {
            int[] expected = { 250, 213, 181, 154, 131, 111, 94, 80 };
            for (int n = 0; n < expected.Length; n++)
            {
                Assert.AreEqual(expected[n], SNSCampaignOps.CalculateEffectiveMilliReach(250, 850, n), $"photo n={n}");
            }
        }

        [Test]
        public void CalculateEffectiveMilliReach_Matches_C2_Chain_ShortForm()
        {
            int[] expected = { 300, 240, 192, 154, 123, 98, 78, 62 };
            for (int n = 0; n < expected.Length; n++)
            {
                Assert.AreEqual(expected[n], SNSCampaignOps.CalculateEffectiveMilliReach(300, 800, n), $"short n={n}");
            }
        }

        [Test]
        public void CalculateEffectiveMilliReach_Matches_C2_Chain_LocalBoard()
        {
            int[] expected = { 150, 135, 122, 110, 99, 89, 80, 72 };
            for (int n = 0; n < expected.Length; n++)
            {
                Assert.AreEqual(expected[n], SNSCampaignOps.CalculateEffectiveMilliReach(150, 900, n), $"local n={n}");
            }
        }

        // ── C3: 보너스 주문 수 · 팔로워 획득 ────────────────────────────────

        [Test]
        public void CalculateBonusOrderCount_Matches_C3_Seed_Expectations()
        {
            Assert.AreEqual(2, SNSCampaignOps.CalculateBonusOrderCount(250), "photo 1회차");
            Assert.AreEqual(1, SNSCampaignOps.CalculateBonusOrderCount(213), "photo 2회차");
            Assert.AreEqual(0, SNSCampaignOps.CalculateBonusOrderCount(80), "photo 8회차(n=7)");
            Assert.AreEqual(2, SNSCampaignOps.CalculateBonusOrderCount(300), "short 1회차");
            Assert.AreEqual(1, SNSCampaignOps.CalculateBonusOrderCount(240), "short 2회차");
            Assert.AreEqual(1, SNSCampaignOps.CalculateBonusOrderCount(150), "local 1회차");
            Assert.AreEqual(1, SNSCampaignOps.CalculateBonusOrderCount(135), "local 2회차 유지");
        }

        [Test]
        public void CalculateBonusOrderCount_Clamps_To_0_2()
        {
            Assert.AreEqual(0, SNSCampaignOps.CalculateBonusOrderCount(0));
            Assert.AreEqual(2, SNSCampaignOps.CalculateBonusOrderCount(100000));
        }

        [Test]
        public void CalculateFollowerGain_Matches_First_Execution_Seed()
        {
            Assert.AreEqual(25, SNSCampaignOps.CalculateFollowerGain(250), "photo 첫 집행");
            Assert.AreEqual(30, SNSCampaignOps.CalculateFollowerGain(300), "short 첫 집행");
            Assert.AreEqual(15, SNSCampaignOps.CalculateFollowerGain(150), "local 첫 집행");
            Assert.AreEqual(21, SNSCampaignOps.CalculateFollowerGain(213), "photo 2회차");
        }

        [Test]
        public void CalculateFollowerDisplay_Is_120_Plus_Sum_And_Matches_Three_Execution_Example()
        {
            var history = new List<SNSCampaignRecord>
            {
                new SNSCampaignRecord { campaignId = "photo_feed", followerGain = 25 },
                new SNSCampaignRecord { campaignId = "short_form", followerGain = 30 },
                new SNSCampaignRecord { campaignId = "photo_feed", followerGain = 21 },
            };
            Assert.AreEqual(196, SNSCampaignOps.CalculateFollowerDisplay(history), "120+25+30+21");
        }

        [Test]
        public void CalculateFollowerDisplay_Empty_History_Is_120()
        {
            Assert.AreEqual(120, SNSCampaignOps.CalculateFollowerDisplay(new List<SNSCampaignRecord>()));
            Assert.AreEqual(120, SNSCampaignOps.CalculateFollowerDisplay(null));
        }

        // ── C4: 연령·성별 타겟 매칭 ─────────────────────────────────────────

        [Test]
        public void CalculateAffinityMilli_Matches_On_AgeBand_And_Gender_Compatibility()
        {
            var rows = new List<SNSAffinityInput>
            {
                new SNSAffinityInput(AgeBand.Twenties, GenderTarget.Female, 1500),
            };
            Assert.AreEqual(1500, SNSCampaignOps.CalculateAffinityMilli(AgeBand.Twenties, GenderTarget.All, rows),
                "row Female + customer All 은 호환(All 이 무관 축 역할)");
            Assert.AreEqual(1500, SNSCampaignOps.CalculateAffinityMilli(AgeBand.Twenties, GenderTarget.Female, rows));
        }

        [Test]
        public void CalculateAffinityMilli_No_Match_Is_Neutral_1000()
        {
            var rows = new List<SNSAffinityInput> { new SNSAffinityInput(AgeBand.Teens, GenderTarget.All, 1600) };
            Assert.AreEqual(1000, SNSCampaignOps.CalculateAffinityMilli(AgeBand.FiftiesPlus, GenderTarget.All, rows));
        }

        [Test]
        public void CalculateAffinityMilli_Gender_Mismatch_Excluded_Falls_Back_To_Neutral_Or_Other_Row()
        {
            var rows = new List<SNSAffinityInput> { new SNSAffinityInput(AgeBand.Twenties, GenderTarget.Male, 1800) };
            Assert.AreEqual(1000, SNSCampaignOps.CalculateAffinityMilli(AgeBand.Twenties, GenderTarget.Female, rows),
                "정확한 성별 불일치(Male vs Female, 둘 다 All 아님)는 매칭 제외");
        }

        [Test]
        public void CalculateAffinityMilli_Multiple_Matches_Uses_Max()
        {
            var rows = new List<SNSAffinityInput>
            {
                new SNSAffinityInput(AgeBand.Teens, GenderTarget.All, 1100),
                new SNSAffinityInput(AgeBand.Teens, GenderTarget.Male, 1900),
            };
            Assert.AreEqual(1900, SNSCampaignOps.CalculateAffinityMilli(AgeBand.Teens, GenderTarget.Male, rows));
        }

        [Test]
        public void CalculateAffinityMilli_PhotoFeed_Female_Row_Matches_All_Gender_Archetype_Same_AgeBand()
        {
            // 현재 archetype 4종은 전부 Gender==All 이므로 픽쳐그램 여성 타겟 행도 해당 연령대에 매칭된다(오픈 이슈).
            var rows = new List<SNSAffinityInput> { new SNSAffinityInput(AgeBand.Twenties, GenderTarget.Female, 1500) };
            Assert.AreEqual(1500, SNSCampaignOps.CalculateAffinityMilli(AgeBand.Twenties, GenderTarget.All, rows));
        }

        // ── 집행 게이트 (TryExecute) ─────────────────────────────────────────

        [Test]
        public void TryExecute_Null_State_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => SNSCampaignOps.TryExecute(null, PhotoFeed()));
        }

        [Test]
        public void TryExecute_Success_Is_Atomic_And_Fills_Record()
        {
            var state = NightState(day: 1, cash: 30000);
            var result = SNSCampaignOps.TryExecute(state, PhotoFeed());

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(30000 - 15000, state.cash);
            Assert.AreEqual(1, state.snsCampaignHistory.Count);
            var record = state.snsCampaignHistory[0];
            Assert.AreEqual("photo_feed", record.campaignId);
            Assert.AreEqual(1, record.executedOnDay);
            Assert.AreEqual(15000, record.costPaid);
            Assert.AreEqual(250, record.effectiveMilliReach);
            Assert.AreEqual(2, record.bonusOrderCount);
            Assert.AreEqual(25, record.followerGain);

            Assert.AreEqual("photo_feed", result.CampaignId);
            Assert.AreEqual(15000, result.CostPaid);
            Assert.AreEqual(250, result.EffectiveMilliReach);
            Assert.AreEqual(2, result.BonusOrderCount);
            Assert.AreEqual(25, result.FollowerGain);
            Assert.AreEqual(state.cash, result.CashAfter);
        }

        [Test]
        public void TryExecute_Invalid_Def_Fails_State_Unchanged()
        {
            var state = NightState();
            var badDef = new SNSCampaignDefInput { Id = "", DisplayName = "x", BaseCost = 1000, BaseReach = 0.1f, RepeatDecay = 0.5f };
            var result = SNSCampaignOps.TryExecute(state, badDef);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(30000, state.cash);
            Assert.AreEqual(0, state.snsCampaignHistory.Count);
        }

        [Test]
        public void TryExecute_Bankrupt_Fails_State_Unchanged()
        {
            var state = NightState();
            state.isBankrupt = true;
            var result = SNSCampaignOps.TryExecute(state, PhotoFeed());

            Assert.IsFalse(result.Success);
            Assert.AreEqual(30000, state.cash);
        }

        [Test]
        public void TryExecute_Not_Night_Phase_Fails_State_Unchanged()
        {
            var state = NightState();
            state.currentPhase = DayPhase.Market;
            var result = SNSCampaignOps.TryExecute(state, PhotoFeed());

            Assert.IsFalse(result.Success);
            Assert.AreEqual(30000, state.cash);
        }

        [Test]
        public void TryExecute_Already_Executed_Tonight_Fails_Even_Different_Channel()
        {
            var state = NightState();
            var first = SNSCampaignOps.TryExecute(state, PhotoFeed());
            Assert.IsTrue(first.Success, first.Message);

            var second = SNSCampaignOps.TryExecute(state, ShortForm());
            Assert.IsFalse(second.Success, "같은 밤 두 번째 집행은 채널이 달라도 실패");
            Assert.AreEqual(1, state.snsCampaignHistory.Count, "레코드 추가 없음");
        }

        [Test]
        public void TryExecute_Insufficient_Cash_Fails_State_Unchanged()
        {
            var state = NightState(cash: 1000);
            var result = SNSCampaignOps.TryExecute(state, PhotoFeed());

            Assert.IsFalse(result.Success);
            Assert.AreEqual(1000, state.cash);
            Assert.AreEqual(0, state.snsCampaignHistory.Count);
        }

        [Test]
        public void TryExecute_Duplicate_Audience_Row_Fails()
        {
            var dupDef = new SNSCampaignDefInput
            {
                Id = "dup",
                DisplayName = "dup",
                BaseCost = 1000,
                BaseReach = 0.2f,
                RepeatDecay = 0.5f,
                AudienceAffinities = new List<SNSRawAffinityInput>
                {
                    new SNSRawAffinityInput(AgeBand.Teens, GenderTarget.All, 1.2f),
                    new SNSRawAffinityInput(AgeBand.Teens, GenderTarget.All, 1.5f),
                },
            };
            var state = NightState();
            var result = SNSCampaignOps.TryExecute(state, dupDef);
            Assert.IsFalse(result.Success);
            Assert.AreEqual(30000, state.cash);
        }

        [Test]
        public void TryExecute_Second_Execution_Uses_Decayed_Reach_Matching_Stored_Recalculation()
        {
            var state = NightState(day: 1);
            SNSCampaignOps.TryExecute(state, PhotoFeed());
            state.day = 2;
            state.currentPhase = DayPhase.Night;
            var second = SNSCampaignOps.TryExecute(state, PhotoFeed());

            Assert.IsTrue(second.Success, second.Message);
            Assert.AreEqual(213, second.EffectiveMilliReach, "priorUses=1 반영 감쇠");
            Assert.AreEqual(1, second.BonusOrderCount);
            Assert.AreEqual(21, second.FollowerGain);

            // 저장값 == 같은 def·priorUses 재계산값 (약속 고정 검증)
            var recalculated = SNSCampaignOps.CalculateEffectiveMilliReach(250, 850, 1);
            Assert.AreEqual(recalculated, state.snsCampaignHistory[1].effectiveMilliReach);
        }

        // ── 미리보기 (TryBuildPreview) ───────────────────────────────────────

        [Test]
        public void TryBuildPreview_Null_State_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                SNSCampaignOps.TryBuildPreview(null, PhotoFeed(), SeedCustomers(), out _, out _));
        }

        [Test]
        public void TryBuildPreview_Succeeds_Without_Changing_State()
        {
            var state = NightState(cash: 30000);
            bool ok = SNSCampaignOps.TryBuildPreview(state, PhotoFeed(), SeedCustomers(), out var preview, out var reason);

            Assert.IsTrue(ok, reason);
            Assert.AreEqual(30000, state.cash, "미리보기는 상태를 바꾸지 않는다");
            Assert.AreEqual(0, state.snsCampaignHistory.Count);
            Assert.IsTrue(preview.CanExecute);
            Assert.AreEqual("", preview.BlockReason);
            Assert.AreEqual(2, preview.BonusOrderCount);
            Assert.AreEqual(25, preview.FollowerGain);
        }

        [Test]
        public void TryBuildPreview_TopTargets_Match_Design_Expectations_Photo_Short_Local()
        {
            var state = NightState();
            SNSCampaignOps.TryBuildPreview(state, PhotoFeed(), SeedCustomers(), out var photoPreview, out var r1);
            SNSCampaignOps.TryBuildPreview(state, ShortForm(), SeedCustomers(), out var shortPreview, out var r2);
            SNSCampaignOps.TryBuildPreview(state, LocalBoard(), SeedCustomers(), out var localPreview, out var r3);

            Assert.IsNotNull(photoPreview, r1);
            Assert.IsNotNull(shortPreview, r2);
            Assert.IsNotNull(localPreview, r3);

            CollectionAssert.AreEqual(new[] { "office_worker", "family_parent" }, photoPreview.TopTargetCustomerIds);
            CollectionAssert.AreEqual(new[] { "student", "office_worker" }, shortPreview.TopTargetCustomerIds);
            CollectionAssert.AreEqual(new[] { "senior_regular", "family_parent" }, localPreview.TopTargetCustomerIds);
        }

        [Test]
        public void TryBuildPreview_Blocked_State_Returns_CanExecute_False_Not_Failure()
        {
            var state = NightState(cash: 1000);
            bool ok = SNSCampaignOps.TryBuildPreview(state, PhotoFeed(), SeedCustomers(), out var preview, out var reason);

            Assert.IsTrue(ok, "게이트 실패는 명시적 실패가 아니라 CanExecute=false 로 구분");
            Assert.IsFalse(preview.CanExecute);
            Assert.IsNotEmpty(preview.BlockReason);
        }

        [Test]
        public void TryBuildPreview_Invalid_Customers_Is_Explicit_Failure()
        {
            var state = NightState();
            bool ok = SNSCampaignOps.TryBuildPreview(state, PhotoFeed(), new List<CustomerDefInput>(), out var preview, out var reason);

            Assert.IsFalse(ok);
            Assert.IsNull(preview);
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void TryBuildPreview_Duplicate_Customer_Id_Is_Explicit_Failure()
        {
            var state = NightState();
            var customers = new List<CustomerDefInput>
            {
                new CustomerDefInput { Id = "student", AgeBand = AgeBand.Teens, Gender = GenderTarget.All, BaseSpawnWeight = 1f },
                new CustomerDefInput { Id = "student", AgeBand = AgeBand.Teens, Gender = GenderTarget.All, BaseSpawnWeight = 1f },
            };
            bool ok = SNSCampaignOps.TryBuildPreview(state, PhotoFeed(), customers, out var preview, out var reason);

            Assert.IsFalse(ok);
            Assert.IsNull(preview);
            Assert.IsNotEmpty(reason);
        }

        // ── history → DayModifier 재구성 (TryBuildDayModifier) ──────────────

        [Test]
        public void TryBuildDayModifier_No_Yesterday_Record_Returns_Neutral_Success()
        {
            bool ok = SNSCampaignOps.TryBuildDayModifier(
                new List<SNSCampaignRecord>(), 3, new List<SNSCampaignDefInput> { PhotoFeed() }, SeedCustomers(),
                out var modifier, out var reason);

            Assert.IsTrue(ok, reason);
            Assert.AreEqual(0, modifier.BonusOrderCount);
            Assert.AreEqual(0, modifier.WeightBoosts.Count);
            Assert.AreEqual("", modifier.SourceCampaignId);
            Assert.AreEqual(3, modifier.Day);
        }

        [Test]
        public void TryBuildDayModifier_One_Record_Uses_Stored_Values_Not_Recalculated()
        {
            var history = new List<SNSCampaignRecord>
            {
                new SNSCampaignRecord { campaignId = "photo_feed", executedOnDay = 1, bonusOrderCount = 2, followerGain = 25, effectiveMilliReach = 999 },
            };
            bool ok = SNSCampaignOps.TryBuildDayModifier(
                history, 2, new List<SNSCampaignDefInput> { PhotoFeed() }, SeedCustomers(),
                out var modifier, out var reason);

            Assert.IsTrue(ok, reason);
            Assert.AreEqual(2, modifier.BonusOrderCount, "레코드 저장값 사용 — def 재계산 아님");
            Assert.AreEqual("photo_feed", modifier.SourceCampaignId);
            Assert.AreEqual(4, modifier.WeightBoosts.Count, "전 고객 커버");
        }

        [Test]
        public void TryBuildDayModifier_Two_Records_Same_Day_Is_Explicit_Failure()
        {
            var history = new List<SNSCampaignRecord>
            {
                new SNSCampaignRecord { campaignId = "photo_feed", executedOnDay = 1, bonusOrderCount = 2 },
                new SNSCampaignRecord { campaignId = "short_form", executedOnDay = 1, bonusOrderCount = 1 },
            };
            bool ok = SNSCampaignOps.TryBuildDayModifier(
                history, 2, new List<SNSCampaignDefInput> { PhotoFeed(), ShortForm() }, SeedCustomers(),
                out var modifier, out var reason);

            Assert.IsFalse(ok);
            Assert.IsNull(modifier);
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void TryBuildDayModifier_Unknown_CampaignId_Is_Explicit_Failure()
        {
            var history = new List<SNSCampaignRecord>
            {
                new SNSCampaignRecord { campaignId = "ghost_channel", executedOnDay = 1, bonusOrderCount = 1 },
            };
            bool ok = SNSCampaignOps.TryBuildDayModifier(
                history, 2, new List<SNSCampaignDefInput> { PhotoFeed() }, SeedCustomers(),
                out var modifier, out var reason);

            Assert.IsFalse(ok);
            Assert.IsNull(modifier);
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void TryBuildDayModifier_BonusOrderCount_Out_Of_Range_Is_Explicit_Failure()
        {
            var history = new List<SNSCampaignRecord>
            {
                new SNSCampaignRecord { campaignId = "photo_feed", executedOnDay = 1, bonusOrderCount = 3 },
            };
            bool ok = SNSCampaignOps.TryBuildDayModifier(
                history, 2, new List<SNSCampaignDefInput> { PhotoFeed() }, SeedCustomers(),
                out var modifier, out var reason);

            Assert.IsFalse(ok);
            Assert.IsNull(modifier);
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void TryBuildDayModifier_Is_Deterministic_For_Same_Input()
        {
            var history = new List<SNSCampaignRecord>
            {
                new SNSCampaignRecord { campaignId = "short_form", executedOnDay = 2, bonusOrderCount = 2, followerGain = 30 },
            };
            var defs = new List<SNSCampaignDefInput> { ShortForm() };

            SNSCampaignOps.TryBuildDayModifier(history, 3, defs, SeedCustomers(), out var modifierA, out _);
            SNSCampaignOps.TryBuildDayModifier(history, 3, defs, SeedCustomers(), out var modifierB, out _);

            Assert.AreEqual(modifierA.BonusOrderCount, modifierB.BonusOrderCount);
            Assert.AreEqual(modifierA.SourceCampaignId, modifierB.SourceCampaignId);
            for (int i = 0; i < modifierA.WeightBoosts.Count; i++)
            {
                Assert.AreEqual(modifierA.WeightBoosts[i].CustomerId, modifierB.WeightBoosts[i].CustomerId);
                Assert.AreEqual(modifierA.WeightBoosts[i].BoostMilli, modifierB.WeightBoosts[i].BoostMilli);
            }
        }

        // ── Codex 리뷰 001: 매칭 def 의 audience row 무결성 검증 (history→DayModifier 경로) ──

        [Test]
        public void TryBuildDayModifier_Matched_Def_With_Duplicate_Audience_Row_Is_Explicit_Failure()
        {
            var corruptDef = new SNSCampaignDefInput
            {
                Id = "photo_feed",
                DisplayName = "픽쳐그램",
                BaseCost = 15000,
                BaseReach = 0.25f,
                RepeatDecay = 0.85f,
                AudienceAffinities = new List<SNSRawAffinityInput>
                {
                    new SNSRawAffinityInput(AgeBand.Twenties, GenderTarget.Female, 1.5f),
                    new SNSRawAffinityInput(AgeBand.Twenties, GenderTarget.Female, 1.9f), // 중복 (AgeBand,Gender)
                },
            };
            var history = new List<SNSCampaignRecord>
            {
                new SNSCampaignRecord { campaignId = "photo_feed", executedOnDay = 1, bonusOrderCount = 2, followerGain = 25 },
            };

            bool ok = SNSCampaignOps.TryBuildDayModifier(
                history, 2, new List<SNSCampaignDefInput> { corruptDef }, SeedCustomers(),
                out var modifier, out var reason);

            Assert.IsFalse(ok, "catalog 의 매칭 def 가 중복 audience row 를 가지면 명시적으로 실패해야 한다 (조용한 max-매칭 금지)");
            Assert.IsNull(modifier);
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void TryBuildDayModifier_Matched_Def_With_Invalid_Multiplier_Is_Explicit_Failure()
        {
            foreach (var bad in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            {
                var corruptDef = new SNSCampaignDefInput
                {
                    Id = "short_form",
                    DisplayName = "숏핑",
                    BaseCost = 12000,
                    BaseReach = 0.30f,
                    RepeatDecay = 0.80f,
                    AudienceAffinities = new List<SNSRawAffinityInput>
                    {
                        new SNSRawAffinityInput(AgeBand.Teens, GenderTarget.All, bad),
                    },
                };
                var history = new List<SNSCampaignRecord>
                {
                    new SNSCampaignRecord { campaignId = "short_form", executedOnDay = 1, bonusOrderCount = 1, followerGain = 30 },
                };

                bool ok = SNSCampaignOps.TryBuildDayModifier(
                    history, 2, new List<SNSCampaignDefInput> { corruptDef }, SeedCustomers(),
                    out var modifier, out var reason);

                Assert.IsFalse(ok, $"multiplier={bad} 인 매칭 def 는 명시적으로 실패해야 한다");
                Assert.IsNull(modifier);
                Assert.IsNotEmpty(reason);
            }
        }

        [Test]
        public void TryBuildDayModifier_Matched_Def_With_Null_Audience_List_Does_Not_Throw()
        {
            var nullListDef = new SNSCampaignDefInput
            {
                Id = "local_board",
                DisplayName = "동네게시판",
                BaseCost = 7000,
                BaseReach = 0.15f,
                RepeatDecay = 0.90f,
                AudienceAffinities = null,
            };
            var history = new List<SNSCampaignRecord>
            {
                new SNSCampaignRecord { campaignId = "local_board", executedOnDay = 1, bonusOrderCount = 1, followerGain = 15 },
            };

            // AudienceAffinities == null 은 "타겟 없음"(전 고객 중립)으로 유효하다 — IsDefInvalid 의 다른
            // 항목(cost/reach/decay)이 전부 정상이면 실패가 아니라 boost 전부 1000(중립)인 modifier 를 만든다.
            bool ok = SNSCampaignOps.TryBuildDayModifier(
                history, 2, new List<SNSCampaignDefInput> { nullListDef }, SeedCustomers(),
                out var modifier, out var reason);

            Assert.IsTrue(ok, reason);
            Assert.AreEqual(SeedCustomers().Count, modifier.WeightBoosts.Count);
            foreach (var boost in modifier.WeightBoosts)
            {
                Assert.AreEqual(1000, boost.BoostMilli, $"{boost.CustomerId} 는 타겟 없음이므로 중립 배수여야 한다");
            }
        }

        [Test]
        public void TryBuildDayModifier_Matched_Def_Corruption_Leaves_History_Unchanged()
        {
            var corruptDef = new SNSCampaignDefInput
            {
                Id = "photo_feed",
                DisplayName = "픽쳐그램",
                BaseCost = 15000,
                BaseReach = 0.25f,
                RepeatDecay = 0.85f,
                AudienceAffinities = new List<SNSRawAffinityInput>
                {
                    new SNSRawAffinityInput(AgeBand.Twenties, GenderTarget.Female, 1.5f),
                    new SNSRawAffinityInput(AgeBand.Twenties, GenderTarget.Female, 1.9f),
                },
            };
            var record = new SNSCampaignRecord { campaignId = "photo_feed", executedOnDay = 1, bonusOrderCount = 2, followerGain = 25 };
            var history = new List<SNSCampaignRecord> { record };

            SNSCampaignOps.TryBuildDayModifier(
                history, 2, new List<SNSCampaignDefInput> { corruptDef }, SeedCustomers(), out _, out _);

            Assert.AreEqual(1, history.Count, "실패 경로는 history 를 건드리지 않아야 한다");
            Assert.AreSame(record, history[0], "실패 경로는 기존 레코드를 교체하지 않아야 한다");
            Assert.AreEqual(2, record.bonusOrderCount, "실패 경로는 레코드 필드를 바꾸지 않아야 한다");
        }
    }

    /// <summary>
    /// Codex 리뷰 001: design.md 테스트 기준 — GameState 를 JsonUtility 로 ToJson→FromJson 왕복한 뒤
    /// snsCampaignHistory 기반 TryBuildDayPlan 결과가 왕복 전과 field-by-field 동일해야 한다(저장 후
    /// 재개 동일성, task-113 선행 보증). 실제 ServiceManager.TryBuildDayPlan 을 그대로 호출한다(재계산 없음).
    /// </summary>
    public class SNSJsonRoundTripTests
    {
        [Test]
        public void TryBuildDayPlan_Result_Is_Identical_Before_And_After_JsonUtility_RoundTrip()
        {
            var gameManagerGo = TestSceneSupport.OpenShopSceneWithLiveSingletons();
            var gm = gameManagerGo.GetComponent<ClientIsKing.Managers.GameManager>();
            gm.StartNewGame();

            var genreIds = gm.GenreCatalog.Select(g => g.Id).ToList();
            var selection = ClientIsKing.Genre.GenreSelectionOps.TrySelect(gm.State, "bunsik", genreIds);
            Assert.IsTrue(selection.Success, selection.Message);

            var service = ClientIsKing.Service.ServiceManager.Instance;
            gm.State.currentPhase = ClientIsKing.DayCycle.DayPhase.Night; // 집행 게이트(E1)를 만족시킨다
            Assert.IsTrue(service.TryExecuteSnsCampaign("short_form", out var execResult), execResult.Message);
            gm.State.day = 2; // 익일 plan 이 어제 집행 효과를 싣도록 진행

            bool okBefore = service.TryBuildDayPlan(
                gm.GenreCatalog.First(g => g.Id == "bunsik"), out var planBefore, out var reasonBefore);
            Assert.IsTrue(okBefore, reasonBefore);
            Assert.Greater(planBefore.BonusOrderCount, 0, "왕복 전 plan 에 SNS 보너스가 있어야 시나리오가 성립한다");

            // ToJson→FromJsonOverwrite 왕복 — GameState 는 public 필드만의 순수 클래스이므로 참조를 바꾸지
            // 않고 같은 인스턴스 위에 왕복 결과를 덮어써 GameManager.State(읽기 전용 프로퍼티)를 그대로 쓴다.
            string json = UnityEngine.JsonUtility.ToJson(gm.State);
            UnityEngine.JsonUtility.FromJsonOverwrite(json, gm.State);

            bool okAfter = service.TryBuildDayPlan(
                gm.GenreCatalog.First(g => g.Id == "bunsik"), out var planAfter, out var reasonAfter);
            Assert.IsTrue(okAfter, reasonAfter);

            Assert.AreEqual(planBefore.GenreId, planAfter.GenreId);
            Assert.AreEqual(planBefore.Day, planAfter.Day);
            Assert.AreEqual(planBefore.BaseOrderCount, planAfter.BaseOrderCount);
            Assert.AreEqual(planBefore.BonusOrderCount, planAfter.BonusOrderCount);
            Assert.AreEqual(planBefore.OrderCount, planAfter.OrderCount);
            Assert.AreEqual(planBefore.SourceCampaignId, planAfter.SourceCampaignId);
            Assert.AreEqual(planBefore.MinPricePerCustomer, planAfter.MinPricePerCustomer);
            Assert.AreEqual(planBefore.MaxPricePerCustomer, planAfter.MaxPricePerCustomer);
            CollectionAssert.AreEqual(planBefore.AllowedRecipeIds, planAfter.AllowedRecipeIds);
            CollectionAssert.AreEqual(planBefore.TopCustomerIds, planAfter.TopCustomerIds);

            Assert.AreEqual(planBefore.CustomerWeights.Count, planAfter.CustomerWeights.Count);
            for (int i = 0; i < planBefore.CustomerWeights.Count; i++)
            {
                Assert.AreEqual(planBefore.CustomerWeights[i].CustomerId, planAfter.CustomerWeights[i].CustomerId);
                Assert.AreEqual(planBefore.CustomerWeights[i].MilliWeight, planAfter.CustomerWeights[i].MilliWeight);
            }
            Assert.AreEqual(planBefore.BonusCustomerWeights.Count, planAfter.BonusCustomerWeights.Count);
            for (int i = 0; i < planBefore.BonusCustomerWeights.Count; i++)
            {
                Assert.AreEqual(planBefore.BonusCustomerWeights[i].CustomerId, planAfter.BonusCustomerWeights[i].CustomerId);
                Assert.AreEqual(planBefore.BonusCustomerWeights[i].MilliWeight, planAfter.BonusCustomerWeights[i].MilliWeight);
            }

            // 주문 순서(recipe/customer/파티/태그)까지 완전 동일해야 저장 후 재개 동일성이 실질적으로 보장된다.
            for (int i = 0; i < planBefore.OrderCount; i++)
            {
                Assert.AreEqual(
                    ClientIsKing.Genre.GenreSelectionOps.PickRecipeId(planBefore, i),
                    ClientIsKing.Genre.GenreSelectionOps.PickRecipeId(planAfter, i), $"주문 {i} recipe");
                Assert.AreEqual(
                    ClientIsKing.Genre.GenreSelectionOps.PickCustomerId(planBefore, i),
                    ClientIsKing.Genre.GenreSelectionOps.PickCustomerId(planAfter, i), $"주문 {i} customer");
                bool taggedBefore = i >= planBefore.BaseOrderCount;
                bool taggedAfter = i >= planAfter.BaseOrderCount;
                Assert.AreEqual(taggedBefore, taggedAfter, $"주문 {i} snsInflow 태그");
            }
        }

        [Test]
        public void SnsCampaignHistory_Survives_JsonUtility_RoundTrip_Field_By_Field()
        {
            // TryBuildDayPlan 왕복 동일성의 원천 데이터인 snsCampaignHistory 자체도 직렬화 손실이 없어야 한다.
            var state = new ClientIsKing.DayCycle.GameState();
            state.snsCampaignHistory.Add(new SNSCampaignRecord
            {
                campaignId = "local_board",
                executedOnDay = 1,
                costPaid = 7000,
                effectiveMilliReach = 150,
                bonusOrderCount = 1,
                followerGain = 15,
            });
            state.snsCampaignHistory.Add(new SNSCampaignRecord
            {
                campaignId = "photo_feed",
                executedOnDay = 2,
                costPaid = 15000,
                effectiveMilliReach = 213,
                bonusOrderCount = 1,
                followerGain = 21,
            });

            string json = UnityEngine.JsonUtility.ToJson(state);
            var restored = UnityEngine.JsonUtility.FromJson<ClientIsKing.DayCycle.GameState>(json);

            Assert.AreEqual(state.snsCampaignHistory.Count, restored.snsCampaignHistory.Count);
            for (int i = 0; i < state.snsCampaignHistory.Count; i++)
            {
                var a = state.snsCampaignHistory[i];
                var b = restored.snsCampaignHistory[i];
                Assert.AreEqual(a.campaignId, b.campaignId, $"레코드 {i} campaignId");
                Assert.AreEqual(a.executedOnDay, b.executedOnDay, $"레코드 {i} executedOnDay");
                Assert.AreEqual(a.costPaid, b.costPaid, $"레코드 {i} costPaid");
                Assert.AreEqual(a.effectiveMilliReach, b.effectiveMilliReach, $"레코드 {i} effectiveMilliReach");
                Assert.AreEqual(a.bonusOrderCount, b.bonusOrderCount, $"레코드 {i} bonusOrderCount");
                Assert.AreEqual(a.followerGain, b.followerGain, $"레코드 {i} followerGain");
            }
        }
    }
}
