using System.Collections.Generic;
using ClientIsKing.Data;
using ClientIsKing.Events;
using ClientIsKing.Genre;
using NUnit.Framework;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-112 U7: EventOps 순수 도메인 규칙 검증 — 투영·검증·FNV 스케줄(시드47/문턱450)·수명 전이·
    /// 효과 합성·예고==적용·정산 원인 라인 단일원천·명시적 실패 매트릭스·상태 완전 불변.
    /// B1 승인 seed(design.md): 폭등 baseWeight1.0/dur2/pct0.35, 위생 0.8/1/flat8000,
    /// 임대 0.6/dur0/pct0.15, 단체 0.9/1/flat4 — C4 스케줄 표가 이 값들의 known vector 다.
    /// </summary>
    public class EventOpsTests
    {
        const string Surge = "ingredient_price_surge";
        const string Hygiene = "hygiene_inspection";
        const string Rent = "rent_increase";
        const string Group = "group_customers";

        static List<GameEventDefInput> SeedDefs()
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

        // ── 투영 ────────────────────────────────────────────────────────────

        [Test]
        public void ProjectMilli_Rounds_Half_Up_At_Boundary()
        {
            Assert.AreEqual(350, EventOps.ProjectMilli(0.35f));
            Assert.AreEqual(150, EventOps.ProjectMilli(0.15f));
        }

        // ── 검증: 카탈로그 ───────────────────────────────────────────────────

        [Test]
        public void TryValidateCatalog_Succeeds_For_Seed_Defs()
        {
            Assert.IsTrue(EventOps.TryValidateCatalog(SeedDefs(), out var reason), reason);
        }

        [Test]
        public void TryValidateCatalog_Fails_When_Null_Or_Empty()
        {
            Assert.IsFalse(EventOps.TryValidateCatalog(null, out var r1));
            Assert.IsNotEmpty(r1);
            Assert.IsFalse(EventOps.TryValidateCatalog(new List<GameEventDefInput>(), out var r2));
            Assert.IsNotEmpty(r2);
        }

        [Test]
        public void TryValidateCatalog_Fails_On_Duplicate_Id()
        {
            var defs = SeedDefs();
            defs.Add(new GameEventDefInput
            {
                Id = Surge, DisplayName = "dup", Kind = GameEventKind.HygieneInspection,
                BaseWeight = 1f, DurationDays = 1, FlatEffect = 100,
            });
            Assert.IsFalse(EventOps.TryValidateCatalog(defs, out var reason));
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void TryValidateCatalog_Fails_On_Duplicate_Kind()
        {
            var defs = SeedDefs();
            defs.Add(new GameEventDefInput
            {
                Id = "second_surge", DisplayName = "dup kind", Kind = GameEventKind.IngredientPriceSurge,
                BaseWeight = 1f, DurationDays = 1, PercentEffect = 0.1f,
            });
            Assert.IsFalse(EventOps.TryValidateCatalog(defs, out var reason));
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void TryValidateCatalog_Fails_When_Required_Kind_Missing()
        {
            // Codex 리뷰001 Action: group_customers(GroupCustomers) 가 빠진 3종 catalog — 축소된
            // 후보군으로 조용히 진행하지 않고 명시적으로 실패해야 한다(하드캡 4종 완전성 강제).
            var defs = SeedDefs();
            defs.RemoveAll(d => d.Kind == GameEventKind.GroupCustomers);
            Assert.AreEqual(3, defs.Count, "픽스처 전제: 3종만 남아야 한다");

            Assert.IsFalse(EventOps.TryValidateCatalog(defs, out var reason));
            Assert.IsNotEmpty(reason);
            Assert.IsTrue(reason.Contains("GroupCustomers"), $"누락 kind 가 사유에 명시되어야 함: '{reason}'");
        }

        [Test]
        public void TryValidateCatalog_Fails_On_Invalid_BaseWeight()
        {
            foreach (var bad in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            {
                var defs = new List<GameEventDefInput>
                {
                    new GameEventDefInput { Id = Surge, Kind = GameEventKind.IngredientPriceSurge, BaseWeight = bad, DurationDays = 2, PercentEffect = 0.35f },
                };
                Assert.IsFalse(EventOps.TryValidateCatalog(defs, out var reason), $"baseWeight={bad}");
                Assert.IsNotEmpty(reason);
            }
        }

        [Test]
        public void TryValidateCatalog_Fails_Per_Kind_Contract_Violations()
        {
            // Surge: percentEffect must be > 0, duration >= 1
            AssertKindInvalid(new GameEventDefInput { Id = Surge, Kind = GameEventKind.IngredientPriceSurge, BaseWeight = 1f, DurationDays = 0, PercentEffect = 0.35f });
            AssertKindInvalid(new GameEventDefInput { Id = Surge, Kind = GameEventKind.IngredientPriceSurge, BaseWeight = 1f, DurationDays = 2, PercentEffect = 0f });
            // Rent: percentEffect must be > 0, duration must == 0
            AssertKindInvalid(new GameEventDefInput { Id = Rent, Kind = GameEventKind.RentIncrease, BaseWeight = 1f, DurationDays = 1, PercentEffect = 0.15f });
            AssertKindInvalid(new GameEventDefInput { Id = Rent, Kind = GameEventKind.RentIncrease, BaseWeight = 1f, DurationDays = 0, PercentEffect = 0f });
            // Hygiene: flatEffect > 0, duration >= 1
            AssertKindInvalid(new GameEventDefInput { Id = Hygiene, Kind = GameEventKind.HygieneInspection, BaseWeight = 1f, DurationDays = 1, FlatEffect = 0 });
            AssertKindInvalid(new GameEventDefInput { Id = Hygiene, Kind = GameEventKind.HygieneInspection, BaseWeight = 1f, DurationDays = 0, FlatEffect = 8000 });
            // Group: flatEffect >= 2, duration >= 1
            AssertKindInvalid(new GameEventDefInput { Id = Group, Kind = GameEventKind.GroupCustomers, BaseWeight = 1f, DurationDays = 1, FlatEffect = 1 });
            AssertKindInvalid(new GameEventDefInput { Id = Group, Kind = GameEventKind.GroupCustomers, BaseWeight = 1f, DurationDays = 0, FlatEffect = 4 });
        }

        static void AssertKindInvalid(GameEventDefInput def)
        {
            var defs = new List<GameEventDefInput> { def };
            Assert.IsFalse(EventOps.TryValidateCatalog(defs, out var reason), $"{def.Id} kind={def.Kind} 는 실패해야 한다");
            Assert.IsNotEmpty(reason);
        }

        // ── 검증: activeEvents 불변식 ────────────────────────────────────────

        [Test]
        public void TryValidateActiveEvents_Succeeds_For_Mixed_Permanent_And_Timed()
        {
            var active = new List<ActiveEventState>
            {
                new ActiveEventState { eventId = Rent, remainingDays = 0 },
                new ActiveEventState { eventId = Surge, remainingDays = 2 },
            };
            Assert.IsTrue(EventOps.TryValidateActiveEvents(active, SeedDefs(), out var reason), reason);
        }

        [Test]
        public void TryValidateActiveEvents_Fails_On_Unknown_Id()
        {
            var active = new List<ActiveEventState> { new ActiveEventState { eventId = "ghost_event", remainingDays = 1 } };
            Assert.IsFalse(EventOps.TryValidateActiveEvents(active, SeedDefs(), out var reason));
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void TryValidateActiveEvents_Fails_On_Duplicate_Active_Id()
        {
            var active = new List<ActiveEventState>
            {
                new ActiveEventState { eventId = Surge, remainingDays = 2 },
                new ActiveEventState { eventId = Surge, remainingDays = 1 },
            };
            Assert.IsFalse(EventOps.TryValidateActiveEvents(active, SeedDefs(), out var reason));
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void TryValidateActiveEvents_Fails_When_Permanent_Has_Nonzero_Remaining()
        {
            var active = new List<ActiveEventState> { new ActiveEventState { eventId = Rent, remainingDays = 1 } };
            Assert.IsFalse(EventOps.TryValidateActiveEvents(active, SeedDefs(), out var reason));
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void TryValidateActiveEvents_Fails_When_Timed_Has_Zero_Remaining()
        {
            var active = new List<ActiveEventState> { new ActiveEventState { eventId = Surge, remainingDays = 0 } };
            Assert.IsFalse(EventOps.TryValidateActiveEvents(active, SeedDefs(), out var reason));
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void TryValidateActiveEvents_Fails_On_Negative_Remaining()
        {
            var active = new List<ActiveEventState> { new ActiveEventState { eventId = Surge, remainingDays = -1 } };
            Assert.IsFalse(EventOps.TryValidateActiveEvents(active, SeedDefs(), out var reason));
            Assert.IsNotEmpty(reason);
        }

        // ── C1: FNV known vectors (시드47) ──────────────────────────────────

        [Test]
        public void Fnv1a_Known_Vectors_For_Event_Schedule_Seed_47()
        {
            Assert.AreEqual(4061914450u, GenreSelectionOps.Fnv1a("event|47|2"));
            Assert.AreEqual(4078692069u, GenreSelectionOps.Fnv1a("event|47|3"));
            Assert.AreEqual(1368159469u, GenreSelectionOps.Fnv1a("event-pick|47|3"));
            Assert.AreEqual(3978026355u, GenreSelectionOps.Fnv1a("event|47|5"));
            Assert.AreEqual(1267493755u, GenreSelectionOps.Fnv1a("event-pick|47|5"));
            Assert.AreEqual(4162580164u, GenreSelectionOps.Fnv1a("event|47|8"));
            Assert.AreEqual(1183605660u, GenreSelectionOps.Fnv1a("event-pick|47|8"));
            Assert.AreEqual(484915338u, GenreSelectionOps.Fnv1a("event|47|11"));
            Assert.AreEqual(806839874u, GenreSelectionOps.Fnv1a("event-pick|47|11"));
        }

        // ── C1/C2: 스케줄 + 수명 전이 — C4 표 재현 ───────────────────────────

        [Test]
        public void Day1_Is_Always_Event_Free_Protected()
        {
            var current = new List<ActiveEventState>();
            Assert.IsTrue(EventOps.TryBuildNextDayActiveEvents(current, 1, SeedDefs(), out var next, out var activated, out var reason), reason);
            Assert.AreEqual(0, next.Count, "Day 1 은 무조건 이벤트 없음");
            Assert.AreEqual("", activated);
        }

        [Test]
        public void Day2_OccRoll_Equals_Threshold_Strict_Less_Than_Means_No_Event()
        {
            var current = new List<ActiveEventState>();
            Assert.IsTrue(EventOps.TryBuildNextDayActiveEvents(current, 2, SeedDefs(), out var next, out var activated, out var reason), reason);
            Assert.AreEqual(0, next.Count, "occRoll==450 은 문턱과 동치 — strict < 로 발생 없음");
            Assert.AreEqual("", activated);
        }

        [Test]
        public void Day3_Activates_Ingredient_Price_Surge_With_Duration_2()
        {
            var current = new List<ActiveEventState>();
            Assert.IsTrue(EventOps.TryBuildNextDayActiveEvents(current, 3, SeedDefs(), out var next, out var activated, out var reason), reason);
            Assert.AreEqual(Surge, activated);
            Assert.AreEqual(1, next.Count);
            Assert.AreEqual(Surge, next[0].eventId);
            Assert.AreEqual(2, next[0].remainingDays);
        }

        [Test]
        public void Day4_Surge_Continues_With_Remaining_1_No_New_Activation()
        {
            var current = new List<ActiveEventState> { new ActiveEventState { eventId = Surge, remainingDays = 2 } };
            Assert.IsTrue(EventOps.TryBuildNextDayActiveEvents(current, 4, SeedDefs(), out var next, out var activated, out var reason), reason);
            Assert.AreEqual("", activated);
            Assert.AreEqual(1, next.Count);
            Assert.AreEqual(Surge, next[0].eventId);
            Assert.AreEqual(1, next[0].remainingDays);
        }

        [Test]
        public void Day5_Surge_Expires_And_Group_Customers_Activates()
        {
            var current = new List<ActiveEventState> { new ActiveEventState { eventId = Surge, remainingDays = 1 } };
            Assert.IsTrue(EventOps.TryBuildNextDayActiveEvents(current, 5, SeedDefs(), out var next, out var activated, out var reason), reason);
            Assert.AreEqual(Group, activated);
            Assert.AreEqual(1, next.Count, "surge 는 remaining 1 이었으므로 만료 제거");
            Assert.AreEqual(Group, next[0].eventId);
            Assert.AreEqual(1, next[0].remainingDays);
        }

        [Test]
        public void Day8_Hygiene_Inspection_Activates()
        {
            var current = new List<ActiveEventState>();
            Assert.IsTrue(EventOps.TryBuildNextDayActiveEvents(current, 8, SeedDefs(), out var next, out var activated, out var reason), reason);
            Assert.AreEqual(Hygiene, activated);
            Assert.AreEqual(1, next.Count);
            Assert.AreEqual(1, next[0].remainingDays);
        }

        [Test]
        public void Day11_Rent_Increase_Activates_As_Permanent_Remaining_Zero()
        {
            var current = new List<ActiveEventState>();
            Assert.IsTrue(EventOps.TryBuildNextDayActiveEvents(current, 11, SeedDefs(), out var next, out var activated, out var reason), reason);
            Assert.AreEqual(Rent, activated);
            Assert.AreEqual(1, next.Count);
            Assert.AreEqual(0, next[0].remainingDays, "영구 이벤트는 remainingDays=0 로 저장");
        }

        [Test]
        public void Rent_Increase_Persists_Forever_Once_Active()
        {
            var current = new List<ActiveEventState> { new ActiveEventState { eventId = Rent, remainingDays = 0 } };
            // day 12: occRoll 719 -> 발생 없음(C4 표) -> rent 만 유지
            Assert.IsTrue(EventOps.TryBuildNextDayActiveEvents(current, 12, SeedDefs(), out var next, out _, out var reason), reason);
            Assert.AreEqual(1, next.Count);
            Assert.AreEqual(Rent, next[0].eventId);
            Assert.AreEqual(0, next[0].remainingDays, "영구 이벤트는 절대 감소·만료하지 않는다");
        }

        [Test]
        public void Rent_Increase_Active_Exclusion_Guarantees_Once_Per_Run()
        {
            // rent 가 이미 활성인 상태에서 다시 발생 후보에 포함되지 않는지 확인 (활성 배제 규칙).
            var current = new List<ActiveEventState> { new ActiveEventState { eventId = Rent, remainingDays = 0 } };
            for (int day = 12; day <= 20; day++)
            {
                Assert.IsTrue(EventOps.TryBuildNextDayActiveEvents(current, day, SeedDefs(), out var next, out var activated, out var reason), reason);
                Assert.AreNotEqual(Rent, activated, $"day {day}: rent 는 활성 배제로 재발생 불가");
                current = next;
            }
        }

        [Test]
        public void Day13_Surge_Reactivates_While_Rent_Stays_Active_Simultaneously()
        {
            var current = new List<ActiveEventState> { new ActiveEventState { eventId = Rent, remainingDays = 0 } };
            Assert.IsTrue(EventOps.TryBuildNextDayActiveEvents(current, 13, SeedDefs(), out var next, out var activated, out var reason), reason);
            Assert.AreEqual(Surge, activated);
            Assert.AreEqual(2, next.Count, "rent(영구) + surge(신규) 동시 활성");
        }

        [Test]
        public void TryBuildNextDayActiveEvents_Does_Not_Mutate_Input_List()
        {
            var current = new List<ActiveEventState> { new ActiveEventState { eventId = Surge, remainingDays = 2 } };
            var snapshot = new List<ActiveEventState>(current);
            EventOps.TryBuildNextDayActiveEvents(current, 4, SeedDefs(), out _, out _, out _);
            Assert.AreEqual(snapshot.Count, current.Count);
            Assert.AreEqual(snapshot[0].eventId, current[0].eventId);
            Assert.AreEqual(snapshot[0].remainingDays, current[0].remainingDays, "입력 리스트/항목은 절대 변경되지 않는다");
        }

        [Test]
        public void TryBuildNextDayActiveEvents_Fails_Explicit_On_Corrupt_Active_State_Unchanged()
        {
            var corrupt = new List<ActiveEventState> { new ActiveEventState { eventId = "unknown_id", remainingDays = 1 } };
            bool ok = EventOps.TryBuildNextDayActiveEvents(corrupt, 5, SeedDefs(), out var next, out var activated, out var reason);
            Assert.IsFalse(ok);
            Assert.IsNull(next);
            Assert.AreEqual("", activated);
            Assert.IsNotEmpty(reason);
        }

        // ── D1: 효과 합성 ────────────────────────────────────────────────────

        [Test]
        public void TryBuildDayEffects_Neutral_When_No_Active_Events()
        {
            Assert.IsTrue(EventOps.TryBuildDayEffects(new List<ActiveEventState>(), 1, SeedDefs(), out var fx, out var reason), reason);
            Assert.AreEqual(1000, fx.IngredientCostMilli);
            Assert.AreEqual(1000, fx.OperatingCostMilli);
            Assert.AreEqual(0, fx.OperatingCostFlat);
            Assert.AreEqual(0, fx.GroupBonusOrders);
            Assert.AreEqual(0, fx.GroupPartySize);
            Assert.AreEqual("", fx.GroupSourceEventId);
            Assert.AreEqual(0, fx.ActiveEventIds.Count);
        }

        [Test]
        public void TryBuildDayEffects_Surge_Applies_IngredientCostMilli_1350()
        {
            var active = new List<ActiveEventState> { new ActiveEventState { eventId = Surge, remainingDays = 2 } };
            Assert.IsTrue(EventOps.TryBuildDayEffects(active, 3, SeedDefs(), out var fx, out var reason), reason);
            Assert.AreEqual(1350, fx.IngredientCostMilli);
            Assert.AreEqual(1000, fx.OperatingCostMilli, "폭등은 운영비 축에 영향 없음");
        }

        [Test]
        public void TryBuildDayEffects_Rent_Applies_OperatingCostMilli_1150()
        {
            var active = new List<ActiveEventState> { new ActiveEventState { eventId = Rent, remainingDays = 0 } };
            Assert.IsTrue(EventOps.TryBuildDayEffects(active, 11, SeedDefs(), out var fx, out var reason), reason);
            Assert.AreEqual(1150, fx.OperatingCostMilli);
        }

        [Test]
        public void TryBuildDayEffects_Hygiene_Applies_OperatingCostFlat_8000()
        {
            var active = new List<ActiveEventState> { new ActiveEventState { eventId = Hygiene, remainingDays = 1 } };
            Assert.IsTrue(EventOps.TryBuildDayEffects(active, 8, SeedDefs(), out var fx, out var reason), reason);
            Assert.AreEqual(8000, fx.OperatingCostFlat);
        }

        [Test]
        public void TryBuildDayEffects_Group_Sets_Bonus_And_PartySize_4()
        {
            var active = new List<ActiveEventState> { new ActiveEventState { eventId = Group, remainingDays = 1 } };
            Assert.IsTrue(EventOps.TryBuildDayEffects(active, 5, SeedDefs(), out var fx, out var reason), reason);
            Assert.AreEqual(1, fx.GroupBonusOrders);
            Assert.AreEqual(4, fx.GroupPartySize);
            Assert.AreEqual(Group, fx.GroupSourceEventId);
        }

        [Test]
        public void TryBuildDayEffects_All_Four_Compose_Independently()
        {
            var active = new List<ActiveEventState>
            {
                new ActiveEventState { eventId = Rent, remainingDays = 0 },
                new ActiveEventState { eventId = Surge, remainingDays = 2 },
                new ActiveEventState { eventId = Hygiene, remainingDays = 1 },
                new ActiveEventState { eventId = Group, remainingDays = 1 },
            };
            Assert.IsTrue(EventOps.TryBuildDayEffects(active, 13, SeedDefs(), out var fx, out var reason), reason);
            Assert.AreEqual(1350, fx.IngredientCostMilli);
            Assert.AreEqual(1150, fx.OperatingCostMilli);
            Assert.AreEqual(8000, fx.OperatingCostFlat);
            Assert.AreEqual(1, fx.GroupBonusOrders);
            Assert.AreEqual(4, fx.GroupPartySize);
            CollectionAssert.AreEqual(new[] { Group, Hygiene, Surge, Rent }, fx.ActiveEventIds, "ID ordinal 정렬");
        }

        [Test]
        public void TryBuildDayEffects_Fails_Explicit_On_Corrupt_Active_State_Unchanged()
        {
            var corrupt = new List<ActiveEventState> { new ActiveEventState { eventId = "ghost", remainingDays = 1 } };
            bool ok = EventOps.TryBuildDayEffects(corrupt, 5, SeedDefs(), out var fx, out var reason);
            Assert.IsFalse(ok);
            Assert.IsNull(fx);
            Assert.IsNotEmpty(reason);
        }

        // ── 예고 == 적용 ─────────────────────────────────────────────────────

        [Test]
        public void TryBuildForecast_Upcoming_Matches_Actual_Next_Day_Activation()
        {
            var current = new List<ActiveEventState>();
            Assert.IsTrue(EventOps.TryBuildForecast(current, 3, SeedDefs(), out var forecast, out var reason), reason);
            Assert.AreEqual(Surge, forecast.UpcomingEventId);
            Assert.IsTrue(forecast.UpcomingNoticeLine.Contains("[예고]"));
            Assert.IsTrue(forecast.UpcomingNoticeLine.Contains("재료값 폭등"));
            Assert.IsTrue(forecast.UpcomingNoticeLine.Contains("+35%"));

            // 예고와 실제 적용이 같은 함수를 공유하므로 activatedEventId 가 정확히 일치해야 한다.
            Assert.IsTrue(EventOps.TryBuildNextDayActiveEvents(current, 3, SeedDefs(), out _, out var activated, out _));
            Assert.AreEqual(activated, forecast.UpcomingEventId, "예고==적용");
        }

        [Test]
        public void TryBuildForecast_Continuing_Line_Shows_Remaining_Days()
        {
            var current = new List<ActiveEventState> { new ActiveEventState { eventId = Surge, remainingDays = 2 } };
            Assert.IsTrue(EventOps.TryBuildForecast(current, 4, SeedDefs(), out var forecast, out var reason), reason);
            Assert.AreEqual("", forecast.UpcomingEventId, "day4 는 신규 없음(C4 표)");
            Assert.IsTrue(forecast.ContinuingNoticeLine.Contains("[지속]"));
            Assert.IsTrue(forecast.ContinuingNoticeLine.Contains("내일까지"), "remaining 1 -> 내일까지");
        }

        [Test]
        public void TryBuildForecast_No_Event_Shows_Empty_Lines()
        {
            var current = new List<ActiveEventState>();
            Assert.IsTrue(EventOps.TryBuildForecast(current, 2, SeedDefs(), out var forecast, out var reason), reason);
            Assert.AreEqual("", forecast.UpcomingEventId);
            Assert.AreEqual("", forecast.UpcomingNoticeLine);
            Assert.AreEqual("", forecast.ContinuingNoticeLine);
        }

        [Test]
        public void TryBuildForecast_NextDayOperatingCost_Reflects_Rent_Increase()
        {
            var current = new List<ActiveEventState>();
            Assert.IsTrue(EventOps.TryBuildForecast(current, 11, SeedDefs(), out var forecast, out var reason), reason);
            Assert.AreEqual(Rent, forecast.UpcomingEventId);
            Assert.AreEqual(13800, forecast.NextDayOperatingCost, "MulMilliHalfUp(12000,1150)=13800");
        }

        [Test]
        public void TryBuildForecast_Fails_Explicit_On_Corrupt_State()
        {
            var corrupt = new List<ActiveEventState> { new ActiveEventState { eventId = "ghost", remainingDays = 1 } };
            bool ok = EventOps.TryBuildForecast(corrupt, 5, SeedDefs(), out var forecast, out var reason);
            Assert.IsFalse(ok);
            Assert.IsNull(forecast);
            Assert.IsNotEmpty(reason);
        }

        // ── D2: DayModifier 합성 ─────────────────────────────────────────────

        [Test]
        public void TryComposeDayModifier_Merges_Sns_And_Event_Axes()
        {
            var sns = new DayModifier(5, "photo_feed", 1, new List<CustomerWeightBoost>
            {
                new CustomerWeightBoost("student", 1000),
            });
            var fx = new EventDayEffects(5, new List<string> { Group }, 1000, 1000, 0, 1, 4, Group);

            Assert.IsTrue(EventOps.TryComposeDayModifier(sns, fx, out var composed, out var reason), reason);
            Assert.AreEqual("photo_feed", composed.SourceCampaignId, "SNS 필드는 그대로 보존");
            Assert.AreEqual(1, composed.BonusOrderCount);
            Assert.AreEqual(Group, composed.EventSourceId);
            Assert.AreEqual(1, composed.EventBonusOrderCount);
            Assert.AreEqual(4, composed.EventPartySize);
        }

        [Test]
        public void TryComposeDayModifier_Fails_When_Null_Inputs()
        {
            var sns = DayModifier.Neutral(1);
            var fx = EventDayEffects.Neutral(1);
            Assert.IsFalse(EventOps.TryComposeDayModifier(null, fx, out var c1, out var r1));
            Assert.IsNull(c1);
            Assert.IsNotEmpty(r1);
            Assert.IsFalse(EventOps.TryComposeDayModifier(sns, null, out var c2, out var r2));
            Assert.IsNull(c2);
            Assert.IsNotEmpty(r2);
        }

        [Test]
        public void TryComposeDayModifier_Fails_On_Day_Mismatch()
        {
            var sns = DayModifier.Neutral(1);
            var fx = EventDayEffects.Neutral(2);
            Assert.IsFalse(EventOps.TryComposeDayModifier(sns, fx, out var composed, out var reason));
            Assert.IsNull(composed);
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void TryComposeDayModifier_Fails_When_Group_Effect_Corrupt()
        {
            var sns = DayModifier.Neutral(5);
            // GroupBonusOrders=1 인데 PartySize<2 — 손상된 fx.
            var corruptFx = new EventDayEffects(5, new List<string> { Group }, 1000, 1000, 0, 1, 1, Group);
            Assert.IsFalse(EventOps.TryComposeDayModifier(sns, corruptFx, out var composed, out var reason));
            Assert.IsNull(composed);
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void TryComposeDayModifier_Neutral_Fx_Leaves_Event_Axis_Neutral()
        {
            var sns = DayModifier.Neutral(1);
            var fx = EventDayEffects.Neutral(1);
            Assert.IsTrue(EventOps.TryComposeDayModifier(sns, fx, out var composed, out var reason), reason);
            Assert.AreEqual(0, composed.EventBonusOrderCount);
            Assert.AreEqual(0, composed.EventPartySize);
            Assert.AreEqual("", composed.EventSourceId);
        }

        // ── F5: BuildSettlementCauseLine 단일원천 ────────────────────────────

        [Test]
        public void BuildSettlementCauseLine_Empty_When_No_Active_Events()
        {
            var fx = EventDayEffects.Neutral(5);
            string line = EventOps.BuildSettlementCauseLine(fx, SeedDefs(), 5, 0, 0, 0);
            Assert.AreEqual("", line);
        }

        [Test]
        public void BuildSettlementCauseLine_Stale_Day_Surcharge_Is_Forced_To_Zero()
        {
            var active = new List<ActiveEventState> { new ActiveEventState { eventId = Surge, remainingDays = 2 } };
            EventOps.TryBuildDayEffects(active, 3, SeedDefs(), out var fx, out _);

            // marketSpendDay(2) != fx.Day(3) — stale 값 3486 은 차단되고 0으로 표시되어야 한다.
            string line = EventOps.BuildSettlementCauseLine(fx, SeedDefs(), 2, 3486, 0, 0);
            Assert.IsTrue(line.Contains("재료값 폭등 -0원"), $"stale-day 차단 실패: '{line}'");
        }

        [Test]
        public void BuildSettlementCauseLine_Same_Day_Surcharge_Passes_Through()
        {
            var active = new List<ActiveEventState> { new ActiveEventState { eventId = Surge, remainingDays = 2 } };
            EventOps.TryBuildDayEffects(active, 3, SeedDefs(), out var fx, out _);
            string line = EventOps.BuildSettlementCauseLine(fx, SeedDefs(), 3, 3486, 0, 0);
            Assert.IsTrue(line.Contains("재료값 폭등 -3,486원"), $"'{line}'");
        }

        [Test]
        public void BuildSettlementCauseLine_Full_Format_For_Two_Active_Events()
        {
            var active = new List<ActiveEventState>
            {
                new ActiveEventState { eventId = Group, remainingDays = 1 },
                new ActiveEventState { eventId = Hygiene, remainingDays = 1 },
            };
            EventOps.TryBuildDayEffects(active, 8, SeedDefs(), out var fx, out _);
            string line = EventOps.BuildSettlementCauseLine(fx, SeedDefs(), 8, 0, 1, 18900);
            Assert.AreEqual("이벤트: 단체 손님 1/1팀 +18,900원 · 위생 점검 -8,000원", line);
        }

        [Test]
        public void BuildSettlementCauseLine_Abbreviated_Format_For_Three_Or_More_Active_Events()
        {
            var active = new List<ActiveEventState>
            {
                new ActiveEventState { eventId = Rent, remainingDays = 0 },
                new ActiveEventState { eventId = Hygiene, remainingDays = 1 },
                new ActiveEventState { eventId = Surge, remainingDays = 2 },
            };
            EventOps.TryBuildDayEffects(active, 13, SeedDefs(), out var fx, out _);
            string line = EventOps.BuildSettlementCauseLine(fx, SeedDefs(), 13, 3009, 0, 0);
            Assert.AreEqual("이벤트: 위생 -8,000 · 폭등 -3,009 · 임대 -1,800", line, "3종 이상은 축약 포맷, kind 상수 사용");
        }

        [Test]
        public void BuildSettlementCauseLine_All_Four_Uses_Abbreviated_Fixed_Order()
        {
            var active = new List<ActiveEventState>
            {
                new ActiveEventState { eventId = Rent, remainingDays = 0 },
                new ActiveEventState { eventId = Hygiene, remainingDays = 1 },
                new ActiveEventState { eventId = Surge, remainingDays = 2 },
                new ActiveEventState { eventId = Group, remainingDays = 1 },
            };
            EventOps.TryBuildDayEffects(active, 99, SeedDefs(), out var fx, out _);
            string line = EventOps.BuildSettlementCauseLine(fx, SeedDefs(), 99, 3009, 1, 18900);
            Assert.AreEqual("이벤트: 단체 1/1 +18,900 · 위생 -8,000 · 폭등 -3,009 · 임대 -1,800", line);
        }

        [Test]
        public void BuildSettlementCauseLine_Rent_Delta_Is_1800()
        {
            var active = new List<ActiveEventState> { new ActiveEventState { eventId = Rent, remainingDays = 0 } };
            EventOps.TryBuildDayEffects(active, 11, SeedDefs(), out var fx, out _);
            string line = EventOps.BuildSettlementCauseLine(fx, SeedDefs(), 11, 0, 0, 0);
            Assert.AreEqual("이벤트: 임대료 인상 -1,800원", line, "MulMilliHalfUp(12000,1150)-12000=1800");
        }

        // ── F2: 효과 요약 ────────────────────────────────────────────────────

        [Test]
        public void BuildEffectSummary_Matches_F2_Copy_For_All_Four_Kinds()
        {
            var defs = SeedDefs();
            var byId = new Dictionary<string, GameEventDefInput>();
            foreach (var d in defs) byId[d.Id] = d;

            Assert.AreEqual("재료 구매가 +35% (2일)", EventOps.BuildEffectSummary(byId[Surge]));
            Assert.AreEqual("대응 비용 8,000원 (1일)", EventOps.BuildEffectSummary(byId[Hygiene]));
            Assert.AreEqual("운영비 +15% (영구)", EventOps.BuildEffectSummary(byId[Rent]));
            Assert.AreEqual("단체 손님 1팀(4인) 방문 (1일)", EventOps.BuildEffectSummary(byId[Group]));
        }

        [Test]
        public void BuildEffectSummary_Null_Def_Returns_Empty()
        {
            Assert.AreEqual("", EventOps.BuildEffectSummary(null));
        }
    }
}
