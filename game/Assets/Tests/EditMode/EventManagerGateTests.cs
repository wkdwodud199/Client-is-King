using System.Linq;
using ClientIsKing.DayCycle;
using ClientIsKing.EditorTools;
using ClientIsKing.Events;
using ClientIsKing.Managers;
using NUnit.Framework;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-112 U7: 손상된 activeEvents(미지 eventId)가 phase 전환을 차단하는지 검증한다.
    /// GameManager.CanAdvancePhase/AdvancePhase 가 Market→Service(Settlement 자동정산 경유)/
    /// Settlement→Night/Night→Market 전 구간에서 명시적 사유로 차단하고 상태를 완전히 불변으로 유지해야 한다.
    /// </summary>
    public class EventManagerGateTests
    {
        static GameManager OpenGameManager()
        {
            var go = TestSceneSupport.OpenShopSceneWithLiveSingletons();
            var gm = go.GetComponent<GameManager>();
            gm.StartNewGame();
            return gm;
        }

        static void SelectGenre(GameManager gm, string genreId)
        {
            var genreIds = gm.GenreCatalog.Select(g => g.Id).ToList();
            var selection = ClientIsKing.Genre.GenreSelectionOps.TrySelect(gm.State, genreId, genreIds);
            Assert.IsTrue(selection.Success, selection.Message);
        }

        [Test]
        public void Market_To_Service_Blocked_When_ActiveEvents_Reference_Unknown_Id()
        {
            var gm = OpenGameManager();
            SelectGenre(gm, "generalist");
            gm.State.activeEvents.Add(new ActiveEventState { eventId = "ghost_event", remainingDays = 1 });

            bool canAdvance = gm.CanAdvancePhase(out var reason);
            Assert.IsFalse(canAdvance, "손상된 activeEvents 는 Market→Service 를 차단해야 한다");
            Assert.IsNotEmpty(reason);

            var phaseBefore = gm.State.currentPhase;
            var phaseAfter = gm.AdvancePhase();
            Assert.AreEqual(phaseBefore, phaseAfter, "차단된 진행은 phase 를 바꾸지 않는다");
            Assert.AreEqual(DayPhase.Market, phaseAfter);
        }

        [Test]
        public void Settlement_To_Night_Blocked_When_ActiveEvents_Reference_Unknown_Id()
        {
            var gm = OpenGameManager();
            SelectGenre(gm, "generalist");

            // 정상 경로로 Service 까지 진행한 뒤(정상 activeEvents), 전부 포기 처리하고 Settlement 진입.
            Assert.AreEqual(DayPhase.Service, gm.AdvancePhase());
            var service = ClientIsKing.Service.ServiceManager.Instance;
            while (service.CurrentOrder != null)
            {
                service.SkipCurrentOrder();
            }
            Assert.AreEqual(DayPhase.Settlement, gm.AdvancePhase());

            // Settlement 진입 후(정산 아직 미적용 상태에서) activeEvents 를 손상시킨다.
            gm.State.activeEvents.Add(new ActiveEventState { eventId = "ghost_event", remainingDays = 1 });

            int cashBefore = gm.State.cash;
            bool settlementApplied = ClientIsKing.Settlement.SettlementOps.IsSettlementApplied(gm.State);
            Assert.IsFalse(settlementApplied, "정산이 아직 적용되지 않은 상태여야 시나리오가 성립한다");

            var phaseAfter = gm.AdvancePhase(); // Settlement 자동 정산 시도 -> fx 조회 실패 -> 차단
            Assert.AreEqual(DayPhase.Settlement, phaseAfter, "손상된 이벤트 상태는 Settlement 이탈을 차단해야 한다");
            Assert.AreEqual(cashBefore, gm.State.cash, "차단된 진행은 cash 를 바꾸지 않는다");
            Assert.IsFalse(ClientIsKing.Settlement.SettlementOps.IsSettlementApplied(gm.State), "차단 시 정산도 적용되지 않는다");
        }

        [Test]
        public void Night_To_Market_Blocked_When_ActiveEvents_Reference_Unknown_Id()
        {
            var gm = OpenGameManager();
            SelectGenre(gm, "generalist");

            Assert.AreEqual(DayPhase.Service, gm.AdvancePhase());
            var service = ClientIsKing.Service.ServiceManager.Instance;
            while (service.CurrentOrder != null)
            {
                service.SkipCurrentOrder();
            }
            Assert.AreEqual(DayPhase.Settlement, gm.AdvancePhase());
            Assert.AreEqual(DayPhase.Night, gm.AdvancePhase()); // 정상 정산 적용 후 Night 진입

            // Night 진입 후 activeEvents 를 손상시켜 다음날 전이가 차단되는지 확인한다.
            gm.State.activeEvents.Add(new ActiveEventState { eventId = "ghost_event", remainingDays = 1 });

            bool canAdvance = gm.CanAdvancePhase(out var reason);
            Assert.IsFalse(canAdvance, "손상된 activeEvents 는 Night→Market 전이를 차단해야 한다");
            Assert.IsNotEmpty(reason);

            int dayBefore = gm.State.day;
            var phaseAfter = gm.AdvancePhase();
            Assert.AreEqual(DayPhase.Night, phaseAfter, "차단된 진행은 Night 에 머문다");
            Assert.AreEqual(dayBefore, gm.State.day, "차단된 진행은 day 를 진행하지 않는다");
        }

        [Test]
        public void TryBuildTodayEventEffects_Fails_Explicit_When_ActiveEvents_Corrupt()
        {
            var gm = OpenGameManager();
            gm.State.activeEvents.Add(new ActiveEventState { eventId = "ghost_event", remainingDays = 1 });

            bool ok = gm.TryBuildTodayEventEffects(out var fx, out var reason);
            Assert.IsFalse(ok);
            Assert.IsNull(fx);
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void TryBuildEventForecast_Fails_Explicit_When_ActiveEvents_Corrupt()
        {
            var gm = OpenGameManager();
            gm.State.activeEvents.Add(new ActiveEventState { eventId = "ghost_event", remainingDays = 1 });

            bool ok = gm.TryBuildEventForecast(out var forecast, out var reason);
            Assert.IsFalse(ok);
            Assert.IsNull(forecast);
            Assert.IsNotEmpty(reason);
        }

        // ── Codex 리뷰001 Action: 필수 kind 누락 catalog(3종)의 phase gate 차단 회귀 ──

        [Test]
        public void Market_To_Service_Blocked_When_Event_Catalog_Missing_Required_Kind()
        {
            var gm = OpenGameManager();
            SelectGenre(gm, "generalist");

            // 정상 4종 catalog 에서 group_customers(GroupCustomers) 를 제거한 3종으로 재주입한다
            // (asset 유실 등으로 필수 kind 가 빠진 실제 손상 시나리오를 시뮬레이션).
            var reducedCatalog = gm.EventCatalog
                .Where(d => d != null && d.Kind != ClientIsKing.Data.GameEventKind.GroupCustomers)
                .ToList();
            Assert.AreEqual(3, reducedCatalog.Count, "픽스처 전제: 3종만 남아야 한다");
            gm.EditorInit(gm.GenreCatalog.ToList(), reducedCatalog);

            bool canAdvance = gm.CanAdvancePhase(out var reason);
            Assert.IsFalse(canAdvance, "필수 kind 가 누락된 catalog 는 Market→Service 를 차단해야 한다");
            Assert.IsNotEmpty(reason);

            var phaseBefore = gm.State.currentPhase;
            var cashBefore = gm.State.cash;
            var phaseAfter = gm.AdvancePhase();
            Assert.AreEqual(phaseBefore, phaseAfter, "차단된 진행은 phase 를 바꾸지 않는다");
            Assert.AreEqual(DayPhase.Market, phaseAfter);
            Assert.AreEqual(cashBefore, gm.State.cash, "차단된 진행은 상태를 바꾸지 않는다");
        }
    }
}
