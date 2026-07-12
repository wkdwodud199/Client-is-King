using System.Collections.Generic;
using ClientIsKing.Data;
using ClientIsKing.DayCycle;
using ClientIsKing.Events;
using ClientIsKing.Inventory;
using ClientIsKing.Save;
using ClientIsKing.Service;
using ClientIsKing.Social;
using NUnit.Framework;
using UnityEngine;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-113 U1 자체검증: SaveOps 순수 파이프라인 — 직렬화/역직렬화, V2b 정규형, V3~V10 검증 매트릭스,
    /// 마이그레이션 훅, 왕복 결정론(RT1/RT2/RT5). 이벤트/스케줄 무작위를 피하기 위해 activeEvents 는
    /// 빈 목록(day 1 neutral) 시나리오를 기본으로 쓴다.
    /// </summary>
    public class SaveOpsTests
    {
        const string Surge = "ingredient_price_surge";
        const string Hygiene = "hygiene_inspection";
        const string Rent = "rent_increase";
        const string Group = "group_customers";

        static List<GameEventDefInput> SeedEventDefs()
        {
            return new List<GameEventDefInput>
            {
                new GameEventDefInput
                {
                    Id = Surge, DisplayName = "재료값 폭등", Kind = GameEventKind.IngredientPriceSurge,
                    BaseWeight = 1.0f, DurationDays = 2, PercentEffect = 0.35f, FlatEffect = 0,
                },
                new GameEventDefInput
                {
                    Id = Hygiene, DisplayName = "위생 점검", Kind = GameEventKind.HygieneInspection,
                    BaseWeight = 0.8f, DurationDays = 1, PercentEffect = 0f, FlatEffect = 8000,
                },
                new GameEventDefInput
                {
                    Id = Rent, DisplayName = "임대료 인상", Kind = GameEventKind.RentIncrease,
                    BaseWeight = 0.6f, DurationDays = 0, PercentEffect = 0.15f, FlatEffect = 0,
                },
                new GameEventDefInput
                {
                    Id = Group, DisplayName = "단체 손님", Kind = GameEventKind.GroupCustomers,
                    BaseWeight = 0.9f, DurationDays = 1, PercentEffect = 0f, FlatEffect = 4,
                },
            };
        }

        static SaveOps.SaveCatalogInputs SeedCatalogs()
        {
            return new SaveOps.SaveCatalogInputs
            {
                GenreIds = new List<string> { "bunsik", "generalist", "gukbap", "noodles" },
                RecipeIds = new List<string> { "recipe_a", "recipe_b" },
                CustomerIds = new List<string> { "customer_a", "customer_b" },
                SnsCampaignIds = new List<string> { "short_form" },
                EventDefs = SeedEventDefs(),
            };
        }

        /// <summary>Day1/Market/미선택/neutral — 가장 단순한 유효 상태.</summary>
        static GameState FreshState()
        {
            return new GameState();
        }

        // ── 직렬화/역직렬화 성공 경로 ────────────────────────────────────────

        [Test]
        public void TrySerialize_Succeeds_For_Fresh_State()
        {
            var state = FreshState();
            bool ok = SaveOps.TrySerialize(state, SeedCatalogs(), out var json, out var reason);
            Assert.IsTrue(ok, reason);
            Assert.IsNotEmpty(json);
        }

        [Test]
        public void TryDeserialize_Succeeds_For_Serialized_Fresh_State()
        {
            var state = FreshState();
            Assert.IsTrue(SaveOps.TrySerialize(state, SeedCatalogs(), out var json, out var reason), reason);

            bool ok = SaveOps.TryDeserialize(json, SeedCatalogs(), out var restored, out var reason2);
            Assert.IsTrue(ok, reason2);
            Assert.AreEqual(state.day, restored.day);
            Assert.AreEqual(state.schemaVersion, restored.schemaVersion);
        }

        // ── RT1: 왕복 field-by-field 동등 ────────────────────────────────────

        [Test]
        public void RoundTrip_Preserves_All_Fields_Field_By_Field_Day1_Market_Unselected()
        {
            AssertRoundTripFieldByField(FreshState());
        }

        [Test]
        public void RoundTrip_Preserves_All_Fields_Field_By_Field_Genre_Confirmed_With_Purchase()
        {
            var state = FreshState();
            state.selectedGenreId = "bunsik";
            state.marketSpendDay = 1;
            state.marketSpendToday = 5000;
            state.ingredientStocks.Add(new IngredientStock { kind = IngredientKind.Rice, grade = IngredientGrade.C, quantity = 10 });
            AssertRoundTripFieldByField(state);
        }

        [Test]
        public void RoundTrip_Preserves_All_Fields_Field_By_Field_Service_Mid_Day_With_Sns_And_Group_Tags()
        {
            var state = FreshState();
            state.selectedGenreId = "bunsik";
            state.currentPhase = DayPhase.Service;
            state.serviceDay = 1;
            state.serviceOrders.Add(new ServiceOrderState { recipeId = "recipe_a", customerId = "customer_a", partySize = 2, served = true, snsInflow = true });
            state.serviceOrders.Add(new ServiceOrderState { recipeId = "recipe_b", customerId = "customer_b", partySize = 4, eventInflow = true });
            state.serviceCurrentOrderIndex = 1;
            state.serviceOrdersServedToday = 1;
            state.serviceCustomersServedToday = 2;
            state.serviceSnsOrdersServedToday = 1;
            state.serviceRevenueToday = 9000;
            state.serviceSnsRevenueToday = 9000;
            AssertRoundTripFieldByField(state);
        }

        [Test]
        public void RoundTrip_Preserves_All_Fields_Field_By_Field_Night_After_Settlement_With_Sns_And_Permanent_Plus_Timed_Events()
        {
            var state = FreshState();
            state.selectedGenreId = "gukbap";
            state.currentPhase = DayPhase.Night;
            state.serviceDay = 1; // Night phase 는 serviceDay==day + 열린 주문 0 을 요구한다 (V7)
            state.settlementDay = 1;
            state.settlementGrossRevenue = 20000;
            state.settlementIngredientSpend = 5000;
            state.settlementOperatingCost = 12000;
            state.settlementNetProfit = 3000;
            state.settlementCashBefore = 42000;
            state.settlementCashAfter = 30000;
            state.cash = 30000;
            state.daysCompleted = 1;
            state.activeEvents.Add(new ActiveEventState { eventId = Rent, remainingDays = 0 });
            state.activeEvents.Add(new ActiveEventState { eventId = Surge, remainingDays = 1 });
            AssertRoundTripFieldByField(state);
        }

        [Test]
        public void RoundTrip_Preserves_All_Fields_Field_By_Field_Day5_With_Accumulated_Sns_History()
        {
            var state = FreshState();
            state.day = 5;
            state.selectedGenreId = "noodles";
            state.currentPhase = DayPhase.Night;
            state.serviceDay = 5; // Night phase 는 serviceDay==day + 열린 주문 0 을 요구한다 (V7)
            state.settlementDay = 5;
            state.settlementCashAfter = 15000;
            state.cash = 15000 - 3000; // Night cash 는 정산 후 잔액에서 오늘 SNS 비용을 뺀 값이어야 한다 (V9)
            state.daysCompleted = 5;
            state.snsCampaignHistory.Add(new SNSCampaignRecord { campaignId = "short_form", executedOnDay = 2, costPaid = 3000, effectiveMilliReach = 200, bonusOrderCount = 1, followerGain = 10 });
            state.snsCampaignHistory.Add(new SNSCampaignRecord { campaignId = "short_form", executedOnDay = 5, costPaid = 3000, effectiveMilliReach = 150, bonusOrderCount = 1, followerGain = 5 });
            AssertRoundTripFieldByField(state);
        }

        [Test]
        public void RoundTrip_Preserves_All_Fields_Field_By_Field_Bankrupt()
        {
            var state = FreshState();
            state.selectedGenreId = "bunsik";
            state.currentPhase = DayPhase.Night;
            state.serviceDay = 1;
            state.settlementDay = 1;
            state.settlementOperatingCost = 12000;
            state.settlementNetProfit = -12000; // gross(0) - spend(0) - cost(12000)
            state.settlementCashBefore = 4000;
            state.settlementCashAfter = 0;
            state.cash = 0;
            state.isBankrupt = true;
            state.bankruptcyDay = 1;
            state.bankruptcyReason = "Day 1 운영비 12,000원 미납 (부족액 8,000원)";
            AssertRoundTripFieldByField(state);
        }

        static void AssertRoundTripFieldByField(GameState state)
        {
            var catalogs = SeedCatalogs();
            Assert.IsTrue(SaveOps.TrySerialize(state, catalogs, out var json, out var reason), reason);
            Assert.IsTrue(SaveOps.TryDeserialize(json, catalogs, out var restored, out var reason2), reason2);

            Assert.AreEqual(state.schemaVersion, restored.schemaVersion);
            Assert.AreEqual(state.day, restored.day);
            Assert.AreEqual(state.currentPhase, restored.currentPhase);
            Assert.AreEqual(state.cash, restored.cash);
            Assert.AreEqual(state.selectedGenreId, restored.selectedGenreId);
            Assert.AreEqual(state.serviceDay, restored.serviceDay);
            Assert.AreEqual(state.serviceOrders.Count, restored.serviceOrders.Count);
            for (int i = 0; i < state.serviceOrders.Count; i++)
            {
                Assert.AreEqual(state.serviceOrders[i].recipeId, restored.serviceOrders[i].recipeId, $"주문 {i} recipeId");
                Assert.AreEqual(state.serviceOrders[i].customerId, restored.serviceOrders[i].customerId, $"주문 {i} customerId");
                Assert.AreEqual(state.serviceOrders[i].partySize, restored.serviceOrders[i].partySize, $"주문 {i} partySize");
                Assert.AreEqual(state.serviceOrders[i].served, restored.serviceOrders[i].served, $"주문 {i} served");
                Assert.AreEqual(state.serviceOrders[i].missed, restored.serviceOrders[i].missed, $"주문 {i} missed");
                Assert.AreEqual(state.serviceOrders[i].snsInflow, restored.serviceOrders[i].snsInflow, $"주문 {i} snsInflow");
                Assert.AreEqual(state.serviceOrders[i].eventInflow, restored.serviceOrders[i].eventInflow, $"주문 {i} eventInflow");
            }
            Assert.AreEqual(state.serviceCurrentOrderIndex, restored.serviceCurrentOrderIndex);
            Assert.AreEqual(state.serviceRevenueToday, restored.serviceRevenueToday);
            Assert.AreEqual(state.serviceOrdersServedToday, restored.serviceOrdersServedToday);
            Assert.AreEqual(state.serviceOrdersMissedToday, restored.serviceOrdersMissedToday);
            Assert.AreEqual(state.serviceCustomersServedToday, restored.serviceCustomersServedToday);
            Assert.AreEqual(state.serviceCustomersMissedToday, restored.serviceCustomersMissedToday);
            Assert.AreEqual(state.marketSpendDay, restored.marketSpendDay);
            Assert.AreEqual(state.marketSpendToday, restored.marketSpendToday);
            Assert.AreEqual(state.settlementDay, restored.settlementDay);
            Assert.AreEqual(state.settlementGrossRevenue, restored.settlementGrossRevenue);
            Assert.AreEqual(state.settlementIngredientSpend, restored.settlementIngredientSpend);
            Assert.AreEqual(state.settlementOperatingCost, restored.settlementOperatingCost);
            Assert.AreEqual(state.settlementNetProfit, restored.settlementNetProfit);
            Assert.AreEqual(state.settlementCashBefore, restored.settlementCashBefore);
            Assert.AreEqual(state.settlementCashAfter, restored.settlementCashAfter);
            Assert.AreEqual(state.daysCompleted, restored.daysCompleted);
            Assert.AreEqual(state.isBankrupt, restored.isBankrupt);
            Assert.AreEqual(state.bankruptcyDay, restored.bankruptcyDay);
            Assert.AreEqual(state.bankruptcyReason, restored.bankruptcyReason);
            Assert.AreEqual(state.ingredientStocks.Count, restored.ingredientStocks.Count);
            for (int i = 0; i < state.ingredientStocks.Count; i++)
            {
                Assert.AreEqual(state.ingredientStocks[i].kind, restored.ingredientStocks[i].kind, $"재료 {i} kind");
                Assert.AreEqual(state.ingredientStocks[i].grade, restored.ingredientStocks[i].grade, $"재료 {i} grade");
                Assert.AreEqual(state.ingredientStocks[i].quantity, restored.ingredientStocks[i].quantity, $"재료 {i} quantity");
            }
            Assert.AreEqual(state.snsCampaignHistory.Count, restored.snsCampaignHistory.Count);
            for (int i = 0; i < state.snsCampaignHistory.Count; i++)
            {
                Assert.AreEqual(state.snsCampaignHistory[i].campaignId, restored.snsCampaignHistory[i].campaignId, $"SNS {i} campaignId");
                Assert.AreEqual(state.snsCampaignHistory[i].executedOnDay, restored.snsCampaignHistory[i].executedOnDay, $"SNS {i} executedOnDay");
                Assert.AreEqual(state.snsCampaignHistory[i].costPaid, restored.snsCampaignHistory[i].costPaid, $"SNS {i} costPaid");
                Assert.AreEqual(state.snsCampaignHistory[i].effectiveMilliReach, restored.snsCampaignHistory[i].effectiveMilliReach, $"SNS {i} effectiveMilliReach");
                Assert.AreEqual(state.snsCampaignHistory[i].bonusOrderCount, restored.snsCampaignHistory[i].bonusOrderCount, $"SNS {i} bonusOrderCount");
                Assert.AreEqual(state.snsCampaignHistory[i].followerGain, restored.snsCampaignHistory[i].followerGain, $"SNS {i} followerGain");
            }
            Assert.AreEqual(state.activeEvents.Count, restored.activeEvents.Count);
            for (int i = 0; i < state.activeEvents.Count; i++)
            {
                Assert.AreEqual(state.activeEvents[i].eventId, restored.activeEvents[i].eventId, $"이벤트 {i} eventId");
                Assert.AreEqual(state.activeEvents[i].remainingDays, restored.activeEvents[i].remainingDays, $"이벤트 {i} remainingDays");
            }

            // RT2: 이중 왕복 바이트 동일
            Assert.IsTrue(SaveOps.TrySerialize(restored, catalogs, out var json2, out var reason3), reason3);
            Assert.AreEqual(json, json2, "재직렬화 결과가 바이트 동일해야 한다 (RT2)");
        }

        // ── RT2: 같은 상태 2회 직렬화 동일 ────────────────────────────────────

        [Test]
        public void TrySerialize_Called_Twice_Produces_Identical_Bytes()
        {
            var state = FreshState();
            var catalogs = SeedCatalogs();
            Assert.IsTrue(SaveOps.TrySerialize(state, catalogs, out var json1, out _));
            Assert.IsTrue(SaveOps.TrySerialize(state, catalogs, out var json2, out _));
            Assert.AreEqual(json1, json2);
        }

        // ── RT5: enum 값 고정 + 프로브 기본값 ────────────────────────────────

        [Test]
        public void DayPhase_Enum_Values_Are_Pinned()
        {
            Assert.AreEqual(0, (int)DayPhase.Market);
            Assert.AreEqual(1, (int)DayPhase.Service);
            Assert.AreEqual(2, (int)DayPhase.Settlement);
            Assert.AreEqual(3, (int)DayPhase.Night);
        }

        [Test]
        public void IngredientKind_Enum_Values_Are_Pinned()
        {
            Assert.AreEqual(0, (int)IngredientKind.Rice);
            Assert.AreEqual(1, (int)IngredientKind.RiceCake);
            Assert.AreEqual(2, (int)IngredientKind.Noodle);
            Assert.AreEqual(3, (int)IngredientKind.Pork);
            Assert.AreEqual(4, (int)IngredientKind.Beef);
            Assert.AreEqual(5, (int)IngredientKind.FishCake);
            Assert.AreEqual(6, (int)IngredientKind.Seaweed);
            Assert.AreEqual(7, (int)IngredientKind.Vegetable);
            Assert.AreEqual(8, (int)IngredientKind.Gochujang);
        }

        [Test]
        public void IngredientGrade_Enum_Values_Are_Pinned()
        {
            Assert.AreEqual(0, (int)IngredientGrade.C);
            Assert.AreEqual(1, (int)IngredientGrade.B);
        }

        [Test]
        public void New_GameState_SchemaVersion_Equals_SaveSchemaVersion_Equals_1()
        {
            Assert.AreEqual(1, GameState.SaveSchemaVersion);
            Assert.AreEqual(GameState.SaveSchemaVersion, new GameState().schemaVersion);
        }

        // ── 마이그레이션 훅 ──────────────────────────────────────────────────

        [Test]
        public void TryDeserialize_Fails_Explicitly_When_SchemaVersion_Field_Missing()
        {
            // schemaVersion 필드를 제거한 JSON — 프로브 기본값 0으로 읽힌다.
            string json = "{\n    \"day\": 1,\n    \"currentPhase\": 0,\n    \"cash\": 30000\n}";
            bool ok = SaveOps.TryDeserialize(json, SeedCatalogs(), out var state, out var reason);
            Assert.IsFalse(ok);
            Assert.IsNull(state);
            Assert.AreEqual("지원하지 않는 저장 버전입니다 (v0).", reason);
        }

        [Test]
        public void TryDeserialize_Fails_Explicitly_For_Future_SchemaVersion_99()
        {
            var probeJson = "{\n    \"schemaVersion\": 99\n}";
            bool ok = SaveOps.TryDeserialize(probeJson, SeedCatalogs(), out var state, out var reason);
            Assert.IsFalse(ok);
            Assert.IsNull(state);
            Assert.AreEqual("지원하지 않는 저장 버전입니다 (v99).", reason);
        }

        [Test]
        public void TryMigrateToCurrent_Fails_For_V0_And_V2_And_V99()
        {
            string json = "{}";
            Assert.IsFalse(SaveOps.TryMigrateToCurrent(0, ref json, out var r0));
            Assert.IsNotEmpty(r0);
            Assert.IsFalse(SaveOps.TryMigrateToCurrent(2, ref json, out var r2));
            Assert.IsNotEmpty(r2);
            Assert.IsFalse(SaveOps.TryMigrateToCurrent(99, ref json, out var r99));
            Assert.IsNotEmpty(r99);
        }

        [Test]
        public void TryMigrateToCurrent_Succeeds_For_V1()
        {
            string json = "{}";
            Assert.IsTrue(SaveOps.TryMigrateToCurrent(1, ref json, out var reason));
            Assert.IsEmpty(reason);
        }

        // ── V1 구조 손상 ─────────────────────────────────────────────────────

        [Test]
        public void TryDeserialize_Fails_On_Empty_Json()
        {
            Assert.IsFalse(SaveOps.TryDeserialize("", SeedCatalogs(), out var state, out var reason));
            Assert.IsNull(state);
            Assert.AreEqual("저장 파일이 손상되었습니다 (JSON 파싱 실패).", reason);
        }

        [Test]
        public void TryDeserialize_Fails_On_Whitespace_Only_Json()
        {
            Assert.IsFalse(SaveOps.TryDeserialize("   \n\t  ", SeedCatalogs(), out var state, out var reason));
            Assert.IsNull(state);
            Assert.AreEqual("저장 파일이 손상되었습니다 (JSON 파싱 실패).", reason);
        }

        [Test]
        public void TryDeserialize_Fails_On_Malformed_Json()
        {
            Assert.IsFalse(SaveOps.TryDeserialize("{not valid json!!", SeedCatalogs(), out var state, out var reason));
            Assert.IsNull(state);
            Assert.AreEqual("저장 파일이 손상되었습니다 (JSON 파싱 실패).", reason);
        }

        // ── V2b 정규형 위반 4종 ──────────────────────────────────────────────

        [Test]
        public void TryDeserialize_Fails_When_Field_Is_Removed_From_Canonical_Json()
        {
            var state = FreshState();
            var catalogs = SeedCatalogs();
            Assert.IsTrue(SaveOps.TrySerialize(state, catalogs, out var json, out _));

            // activeEvents 키를 제거한다 (정규형 필드 누락).
            string tampered = RemoveJsonLine(json, "\"activeEvents\"");
            bool ok = SaveOps.TryDeserialize(tampered, catalogs, out var restored, out var reason);
            Assert.IsFalse(ok);
            Assert.IsNull(restored);
            Assert.AreEqual("저장 파일이 정규 직렬화 형식과 일치하지 않습니다 (필드 누락 또는 변조).", reason);
        }

        [Test]
        public void TryDeserialize_Fails_When_Field_Is_Explicit_Null()
        {
            var state = FreshState();
            var catalogs = SeedCatalogs();
            Assert.IsTrue(SaveOps.TrySerialize(state, catalogs, out var json, out _));

            string tampered = json.Replace("\"ingredientStocks\": []", "\"ingredientStocks\": null");
            Assert.AreNotEqual(json, tampered, "치환이 실제로 발생해야 테스트가 유효하다");

            bool ok = SaveOps.TryDeserialize(tampered, catalogs, out var restored, out var reason);
            Assert.IsFalse(ok);
            Assert.IsNull(restored);
            Assert.AreEqual("저장 파일이 정규 직렬화 형식과 일치하지 않습니다 (필드 누락 또는 변조).", reason);
        }

        [Test]
        public void TryDeserialize_Fails_When_Unknown_Key_Is_Added()
        {
            var state = FreshState();
            var catalogs = SeedCatalogs();
            Assert.IsTrue(SaveOps.TrySerialize(state, catalogs, out var json, out _));

            string tampered = json.Replace(
                "\"schemaVersion\": 1,",
                "\"schemaVersion\": 1,\n    \"unknownField\": 123,");
            bool ok = SaveOps.TryDeserialize(tampered, catalogs, out var restored, out var reason);
            Assert.IsFalse(ok);
            Assert.IsNull(restored);
            Assert.AreEqual("저장 파일이 정규 직렬화 형식과 일치하지 않습니다 (필드 누락 또는 변조).", reason);
        }

        [Test]
        public void TryDeserialize_Fails_When_Field_Order_Is_Changed()
        {
            var state = FreshState();
            var catalogs = SeedCatalogs();
            Assert.IsTrue(SaveOps.TrySerialize(state, catalogs, out var json, out _));

            // day 와 schemaVersion 라인을 맞바꿔 순서를 변조한다.
            var lines = json.Split('\n');
            int schemaIdx = System.Array.FindIndex(lines, l => l.Contains("\"schemaVersion\""));
            int dayIdx = System.Array.FindIndex(lines, l => l.Contains("\"day\""));
            Assert.Greater(dayIdx, schemaIdx);
            (lines[schemaIdx], lines[dayIdx]) = (lines[dayIdx], lines[schemaIdx]);
            string tampered = string.Join("\n", lines);
            Assert.AreNotEqual(json, tampered);

            bool ok = SaveOps.TryDeserialize(tampered, catalogs, out var restored, out var reason);
            Assert.IsFalse(ok);
            Assert.IsNull(restored);
            Assert.AreEqual("저장 파일이 정규 직렬화 형식과 일치하지 않습니다 (필드 누락 또는 변조).", reason);
        }

        static string RemoveJsonLine(string json, string containing)
        {
            var lines = new List<string>(json.Split('\n'));
            lines.RemoveAll(l => l.Contains(containing));
            return string.Join("\n", lines);
        }

        // ── V3~V10 검증 매트릭스 — TryValidateState 직접 호출 ────────────────

        [Test]
        public void TryValidateState_Fails_When_Day_Is_Zero()
        {
            var state = FreshState();
            state.day = 0;
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: 일차가 잘못되었습니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_Phase_Out_Of_Range()
        {
            var state = FreshState();
            state.currentPhase = (DayPhase)7;
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: 알 수 없는 phase 값입니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_Cash_Negative()
        {
            var state = FreshState();
            state.cash = -1;
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: 보유 자금이 음수입니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_DaysCompleted_Inconsistent()
        {
            var state = FreshState();
            state.daysCompleted = 5;
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: 완료 일수가 정산 기록과 일치하지 않습니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_Bankruptcy_Inconsistent()
        {
            var state = FreshState();
            state.isBankrupt = true; // day/settlement/cash/reason 미설정 — 불일치
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: 파산 기록이 일관되지 않습니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_Unselected_Genre_Saved_On_Day2()
        {
            var state = FreshState();
            state.day = 2;
            state.daysCompleted = 1;
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: 전문 분야가 선택되지 않은 상태가 잘못되었습니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_Genre_Id_Unknown()
        {
            var state = FreshState();
            state.selectedGenreId = "unknown_genre";
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: 알 수 없는 전문 분야 ID 'unknown_genre' 입니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_Inventory_Kind_Out_Of_Range()
        {
            var state = FreshState();
            state.ingredientStocks.Add(new IngredientStock { kind = (IngredientKind)99, grade = IngredientGrade.C, quantity = 1 });
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: 알 수 없는 재료 종류입니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_Inventory_Duplicate_Kind_Grade()
        {
            var state = FreshState();
            state.ingredientStocks.Add(new IngredientStock { kind = IngredientKind.Rice, grade = IngredientGrade.C, quantity = 1 });
            state.ingredientStocks.Add(new IngredientStock { kind = IngredientKind.Rice, grade = IngredientGrade.C, quantity = 2 });
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: 인벤토리에 중복된 (종류,등급) 항목이 있습니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_Inventory_Quantity_Negative()
        {
            var state = FreshState();
            state.ingredientStocks.Add(new IngredientStock { kind = IngredientKind.Rice, grade = IngredientGrade.C, quantity = -1 });
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: 재료 수량이 음수입니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_Order_Recipe_Id_Unknown()
        {
            var state = FreshState();
            state.selectedGenreId = "bunsik";
            state.serviceDay = 1;
            state.serviceOrders.Add(new ServiceOrderState { recipeId = "unknown_recipe", customerId = "customer_a", partySize = 1 });
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: 알 수 없는 레시피 ID 'unknown_recipe' 입니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_Order_Served_And_Missed_Both_True()
        {
            var state = FreshState();
            state.selectedGenreId = "bunsik";
            state.serviceDay = 1;
            state.serviceOrders.Add(new ServiceOrderState { recipeId = "recipe_a", customerId = "customer_a", partySize = 1, served = true, missed = true });
            state.serviceCurrentOrderIndex = 1;
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: 주문이 서빙과 포기 상태를 동시에 갖고 있습니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_Order_Index_Out_Of_Range()
        {
            var state = FreshState();
            state.selectedGenreId = "bunsik";
            state.serviceDay = 1;
            state.serviceOrders.Add(new ServiceOrderState { recipeId = "recipe_a", customerId = "customer_a", partySize = 1 });
            state.serviceCurrentOrderIndex = 5;
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: 현재 주문 인덱스가 범위를 벗어났습니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_Open_Order_Precedes_Index()
        {
            // 주문 시퀀스 자체는 닫힘→열림 접두/접미 불변식을 지키지만(served, missed, open, open),
            // 인덱스가 그 경계(2)를 지나쳐(3) 아직 열린 주문(2번)을 "이미 처리됨" 구간에 포함시킨다.
            var state = FreshState();
            state.selectedGenreId = "bunsik";
            state.serviceDay = 1;
            state.serviceOrders.Add(new ServiceOrderState { recipeId = "recipe_a", customerId = "customer_a", partySize = 1, served = true });
            state.serviceOrders.Add(new ServiceOrderState { recipeId = "recipe_b", customerId = "customer_b", partySize = 1, missed = true });
            state.serviceOrders.Add(new ServiceOrderState { recipeId = "recipe_a", customerId = "customer_a", partySize = 1 }); // 열림
            state.serviceOrders.Add(new ServiceOrderState { recipeId = "recipe_b", customerId = "customer_b", partySize = 1 }); // 열림
            state.serviceCurrentOrderIndex = 3; // 경계(2)를 지나쳐 아직 열린 2번 주문을 앞쪽 구간에 포함
            state.serviceOrdersServedToday = 1;
            state.serviceOrdersMissedToday = 1;
            state.serviceCustomersServedToday = 1;
            state.serviceCustomersMissedToday = 1;
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: 인덱스 앞의 주문이 아직 열려 있습니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_Service_Stats_Mismatch_Orders()
        {
            var state = FreshState();
            state.selectedGenreId = "bunsik";
            state.serviceDay = 1;
            state.serviceOrders.Add(new ServiceOrderState { recipeId = "recipe_a", customerId = "customer_a", partySize = 1, served = true });
            state.serviceCurrentOrderIndex = 1;
            state.serviceOrdersServedToday = 2; // 실제 서빙 1건인데 통계는 2
            state.serviceCustomersServedToday = 1;
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: 서비스 통계가 주문 목록과 일치하지 않습니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_Night_Without_Todays_Settlement()
        {
            var state = FreshState();
            state.currentPhase = DayPhase.Night;
            // settlementDay 는 기본값 0 — Night 인데 오늘(day1) 정산 미적용
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: Night phase 인데 오늘 정산이 적용되지 않았습니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_MarketEventSurcharge_Exceeds_Spend()
        {
            var state = FreshState();
            state.marketSpendDay = 1;
            state.marketSpendToday = 1000;
            state.marketEventSurchargeToday = 2000;
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: 이벤트 할증액이 구매 지출액과 일치하지 않습니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_Sns_Same_Day_Duplicate()
        {
            var state = FreshState();
            state.snsCampaignHistory.Add(new SNSCampaignRecord { campaignId = "short_form", executedOnDay = 1, costPaid = 1000 });
            state.snsCampaignHistory.Add(new SNSCampaignRecord { campaignId = "short_form", executedOnDay = 1, costPaid = 2000 });
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: 같은 날 SNS 집행 기록이 중복되었습니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_Sns_ExecutedOnDay_Exceeds_Day()
        {
            var state = FreshState();
            state.snsCampaignHistory.Add(new SNSCampaignRecord { campaignId = "short_form", executedOnDay = 2, costPaid = 1000 });
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: SNS 집행 일차가 잘못되었습니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_Sns_BonusOrderCount_Out_Of_Range()
        {
            var state = FreshState();
            state.snsCampaignHistory.Add(new SNSCampaignRecord { campaignId = "short_form", executedOnDay = 1, costPaid = 1000, bonusOrderCount = 3 });
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: SNS 보너스 주문 수가 잘못되었습니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_Night_Cash_Mismatches_Settlement_And_Sns()
        {
            var state = FreshState();
            state.selectedGenreId = "bunsik"; // V5 통과(빈 값은 day1/Market 에서만 허용) — Night 이므로 확정 필요
            state.currentPhase = DayPhase.Night;
            state.serviceDay = 1;
            state.settlementDay = 1;
            state.daysCompleted = 1;
            state.settlementCashAfter = 20000;
            state.cash = 20000;
            state.snsCampaignHistory.Add(new SNSCampaignRecord { campaignId = "short_form", executedOnDay = 1, costPaid = 3000 });
            // cash 가 오늘 SNS 비용을 반영하지 않음 (기대: 20000 - 3000 = 17000)
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: Night 잔액이 정산/SNS 집행과 일치하지 않습니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_ActiveEvents_Has_Unknown_EventId()
        {
            var state = FreshState();
            state.activeEvents.Add(new ActiveEventState { eventId = "unknown_event", remainingDays = 1 });
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            StringAssert.Contains("unknown_event", reason);
        }

        // ── non-null 완전성 (in-memory 손상) ─────────────────────────────────

        [Test]
        public void TryValidateState_Fails_When_IngredientStocks_Is_Null()
        {
            var state = FreshState();
            state.ingredientStocks = null;
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: 재료 인벤토리 목록이 없습니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_ServiceOrders_Is_Null()
        {
            var state = FreshState();
            state.serviceOrders = null;
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: 주문 목록이 없습니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_SnsCampaignHistory_Is_Null()
        {
            var state = FreshState();
            state.snsCampaignHistory = null;
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: SNS 집행 기록 목록이 없습니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_ActiveEvents_Is_Null()
        {
            var state = FreshState();
            state.activeEvents = null;
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: 활성 이벤트 목록이 없습니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_SelectedGenreId_Is_Null()
        {
            var state = FreshState();
            state.selectedGenreId = null;
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: 전문 분야 ID 가 없습니다.", reason);
        }

        [Test]
        public void TryValidateState_Fails_When_BankruptcyReason_Is_Null()
        {
            var state = FreshState();
            state.bankruptcyReason = null;
            Assert.IsFalse(SaveOps.TryValidateState(state, SeedCatalogs(), out var reason));
            Assert.AreEqual("저장 데이터 검증 실패: 파산 사유 필드가 없습니다.", reason);
        }

        // ── 저장 방향도 같은 검증 (손상 상태 저장 차단) ──────────────────────

        [Test]
        public void TrySerialize_Fails_With_Same_Reason_Prefix_When_State_Is_Corrupt()
        {
            var state = FreshState();
            state.cash = -1;
            bool ok = SaveOps.TrySerialize(state, SeedCatalogs(), out var json, out var reason);
            Assert.IsFalse(ok);
            Assert.IsNull(json);
            Assert.AreEqual("손상된 상태는 저장하지 않습니다: 저장 데이터 검증 실패: 보유 자금이 음수입니다.", reason);
        }

        // ── 세 경로(저장/peek≈TryValidateState/로드) 같은 검증 함수 ──────────

        [Test]
        public void TryValidateState_Direct_And_TrySerialize_Share_Same_NonNull_Reason()
        {
            var state = FreshState();
            state.ingredientStocks = null;
            var catalogs = SeedCatalogs();

            Assert.IsFalse(SaveOps.TryValidateState(state, catalogs, out var directReason));
            Assert.IsFalse(SaveOps.TrySerialize(state, catalogs, out _, out var serializeReason));
            StringAssert.Contains(directReason, serializeReason);
        }

        [Test]
        public void TryDeserialize_And_TryValidateState_Fail_With_Same_Reason_For_Same_File_Corruption()
        {
            var state = FreshState();
            var catalogs = SeedCatalogs();
            Assert.IsTrue(SaveOps.TrySerialize(state, catalogs, out var json, out _));

            string tampered = json.Replace("\"ingredientStocks\": []", "\"ingredientStocks\": null");
            Assert.IsFalse(SaveOps.TryDeserialize(tampered, catalogs, out _, out var loadReason));
            Assert.AreEqual("저장 파일이 정규 직렬화 형식과 일치하지 않습니다 (필드 누락 또는 변조).", loadReason);
        }

        // ── catalog 입력 검증 ────────────────────────────────────────────────

        [Test]
        public void TrySerialize_Fails_When_Catalogs_Null()
        {
            Assert.IsFalse(SaveOps.TrySerialize(FreshState(), null, out var json, out var reason));
            Assert.IsNull(json);
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void TrySerialize_Fails_When_GenreIds_Empty()
        {
            var catalogs = SeedCatalogs();
            catalogs.GenreIds = new List<string>();
            Assert.IsFalse(SaveOps.TrySerialize(FreshState(), catalogs, out var json, out var reason));
            Assert.IsNull(json);
            StringAssert.Contains("장르", reason);
        }

        [Test]
        public void TrySerialize_Fails_When_RecipeIds_Has_Duplicate()
        {
            var catalogs = SeedCatalogs();
            catalogs.RecipeIds = new List<string> { "recipe_a", "recipe_a" };
            Assert.IsFalse(SaveOps.TrySerialize(FreshState(), catalogs, out var json, out var reason));
            Assert.IsNull(json);
            StringAssert.Contains("레시피", reason);
        }

        // ── BuildSummary ─────────────────────────────────────────────────────

        [Test]
        public void BuildSummary_Reflects_State_Fields()
        {
            var state = FreshState();
            state.selectedGenreId = "bunsik";
            state.cash = 12345;
            state.daysCompleted = 3;

            var summary = SaveOps.BuildSummary(state);
            Assert.AreEqual(state.day, summary.Day);
            Assert.AreEqual(state.currentPhase, summary.Phase);
            Assert.AreEqual(12345, summary.Cash);
            Assert.AreEqual(3, summary.DaysCompleted);
            Assert.AreEqual(state.isBankrupt, summary.IsBankrupt);
            Assert.AreEqual("bunsik", summary.SelectedGenreId);
        }
    }
}
