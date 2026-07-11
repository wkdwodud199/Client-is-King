using System.Collections;
using System.Linq;
using ClientIsKing.DayCycle;
using ClientIsKing.Genre;
using ClientIsKing.Managers;
using ClientIsKing.Service;
using ClientIsKing.Social;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ClientIsKing.Tests.PlayMode
{
    /// <summary>
    /// task-110 U6: MainMenu→Shop 전환 후 GameManager/ServiceManager persistent instance 생존,
    /// genre/recipe/customer catalog 보유, UI 미활성 상태에서 GameManager.AdvancePhase() 만으로
    /// 원자적 주문 초기화가 되는지 실제 씬 로드로 검증한다 (design.md G3, 테스트 기준 순수 도메인 경로).
    /// InitialDataBuilder.Apply/SceneBuilder.Apply 는 EditMode 배치 단계에서 이미 실행된 산출물(Build
    /// Settings 등록 완료)을 전제한다 — 이 어셈블리는 PlayMode 전용이라 Editor 빌더를 직접 참조하지 않는다.
    /// </summary>
    public class GenrePersistencePlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // 다음 PlayMode 테스트/세션이 깨끗한 씬에서 시작하도록 MainMenu 로 되돌린다.
            if (SceneManager.GetActiveScene().name != "MainMenu")
            {
                yield return SceneManager.LoadSceneAsync("MainMenu");
            }
        }

        [UnityTest]
        public IEnumerator GameManager_And_ServiceManager_Survive_MainMenu_To_Shop_With_Full_Catalog()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;

            var gmInMainMenu = GameManager.Instance;
            var serviceInMainMenu = ServiceManager.Instance;
            Assert.IsNotNull(gmInMainMenu, "MainMenu 에서 GameManager.Instance 누락");
            Assert.IsNotNull(serviceInMainMenu, "MainMenu 에서 ServiceManager.Instance 누락");

            yield return SceneManager.LoadSceneAsync("Shop");
            yield return null;

            var gmInShop = GameManager.Instance;
            var serviceInShop = ServiceManager.Instance;

            Assert.AreSame(gmInMainMenu, gmInShop, "GameManager 는 DontDestroyOnLoad persistent instance 로 생존해야 한다");
            Assert.AreSame(serviceInMainMenu, serviceInShop, "ServiceManager 도 같은 persistent instance 로 생존해야 한다");

            Assert.AreEqual(4, gmInShop.GenreCatalog.Count, "genre 4종 보유");
            Assert.AreEqual(6, serviceInShop.RecipeDefs.Count, "recipe 6종 보유");
            Assert.AreEqual(4, serviceInShop.CustomerDefs.Count, "customer 4종 보유");

            gmInShop.TryGetGenre("generalist", out var genre);
            Assert.IsNotNull(genre, "generalist GenreDef lookup 성공해야 한다");
            Assert.IsTrue(serviceInShop.TryBuildDayPlan(genre, out var plan, out var reason), reason);
            Assert.IsNotNull(plan);
        }

        [UnityTest]
        public IEnumerator AdvancePhase_Atomically_Initializes_Orders_Without_Any_UI_Active()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            yield return SceneManager.LoadSceneAsync("Shop");
            yield return null;

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm);
            gm.StartNewGame(); // 이 테스트 전용 상태로 리셋 (UI 는 건드리지 않음 — 순수 도메인 경로 검증)

            var genreIds = gm.GenreCatalog.Select(g => g.Id).ToList();
            var selection = GenreSelectionOps.TrySelect(gm.State, "generalist", genreIds);
            Assert.IsTrue(selection.Success, selection.Message);

            // UI controller 를 전혀 활성화하지 않고 GameManager.AdvancePhase() 만 호출한다.
            var nextPhase = gm.AdvancePhase();

            Assert.AreEqual(DayPhase.Service, nextPhase, "Market→Service 전환이 원자적으로 성공해야 한다");
            Assert.AreEqual(gm.State.day, gm.State.serviceDay, "전환과 동시에 오늘 주문이 초기화되어야 한다");
            Assert.Greater(gm.State.serviceOrders.Count, 0, "주문이 실제로 생성되어야 한다");

            // 열린 주문이 있는 채로 다시 호출하면 Service 에 머물러야 한다 (원자성 재확인).
            var secondAttempt = gm.AdvancePhase();
            Assert.AreEqual(DayPhase.Service, secondAttempt, "열린 주문이 있으면 Settlement 로 진행하지 않는다");
        }

        [UnityTest]
        public IEnumerator ServiceManager_Survives_Scene_Load_With_Sns_Catalog_Of_Three()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            yield return SceneManager.LoadSceneAsync("Shop");
            yield return null;

            var service = ServiceManager.Instance;
            Assert.IsNotNull(service, "ServiceManager.Instance 누락");
            Assert.AreEqual(3, service.SnsCampaignDefs.Count, "SNS catalog 3종 보유");
            var ids = service.SnsCampaignDefs.Select(d => d.Id).ToList();
            var sortedIds = new System.Collections.Generic.List<string>(ids);
            sortedIds.Sort(System.StringComparer.Ordinal);
            CollectionAssert.AreEqual(sortedIds, ids, "SNS catalog 는 ID ordinal 정렬이어야 한다");
        }

        [UnityTest]
        public IEnumerator Night_Execution_Then_Day2_Plan_Has_Bonus_And_Tagged_Orders_Without_Any_Ui()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            yield return SceneManager.LoadSceneAsync("Shop");
            yield return null;

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm);
            gm.StartNewGame(); // 이 테스트 전용 상태로 리셋 (UI 는 건드리지 않음 — 순수 도메인 경로 검증)

            var genreIds = gm.GenreCatalog.Select(g => g.Id).ToList();
            var selection = GenreSelectionOps.TrySelect(gm.State, "gukbap", genreIds);
            Assert.IsTrue(selection.Success, selection.Message);

            // Day 1 루프: Market → Service → (전부 포기) → Settlement → Night, UI 없이 도메인 경로만 사용.
            Assert.AreEqual(DayPhase.Service, gm.AdvancePhase());
            var service = ServiceManager.Instance;
            while (service.CurrentOrder != null)
            {
                service.SkipCurrentOrder();
            }
            Assert.AreEqual(DayPhase.Settlement, gm.AdvancePhase());
            Assert.AreEqual(DayPhase.Night, gm.AdvancePhase()); // Settlement 진입 시 정산 자동 적용, 그 다음 호출로 Night 진입

            // Night: SNS 캠페인 1종 집행 (photo_feed) — 순수 도메인 경로.
            Assert.IsTrue(service.TryExecuteSnsCampaign("photo_feed", out var execResult), execResult.Message);
            Assert.AreEqual(1, gm.State.snsCampaignHistory.Count);

            // Day 2 진입: Night → Market.
            Assert.AreEqual(DayPhase.Market, gm.AdvancePhase());
            Assert.AreEqual(2, gm.State.day, "Night → Market 전환으로 Day 2 진입");

            // Day 2 plan 이 modifier 합성으로 base+bonus 를 반영하는지 확인.
            gm.TryGetGenre("gukbap", out var genreDef);
            Assert.IsTrue(service.TryBuildDayPlan(genreDef, out var plan, out var reason), reason);
            Assert.Greater(plan.BonusOrderCount, 0, "전날 밤 집행 효과로 Day 2 plan 에 보너스 주문이 있어야 한다");
            Assert.AreEqual(plan.BaseOrderCount + plan.BonusOrderCount, plan.OrderCount);

            // Market → Service 전환까지 실제로 진행해 태그 주문 존재를 도메인 경로로 검증한다.
            Assert.AreEqual(DayPhase.Service, gm.AdvancePhase());
            Assert.AreEqual(plan.OrderCount, gm.State.serviceOrders.Count, "생성된 주문 수가 plan.OrderCount 와 일치해야 한다");
            Assert.IsTrue(gm.State.serviceOrders.Skip(plan.BaseOrderCount).All(o => o.snsInflow),
                "보너스 인덱스 주문은 전부 snsInflow 태그가 있어야 한다");
            Assert.IsTrue(gm.State.serviceOrders.Take(plan.BaseOrderCount).All(o => !o.snsInflow),
                "base 인덱스 주문은 snsInflow 태그가 없어야 한다");
        }
    }
}
