using System.Collections;
using System.Linq;
using ClientIsKing.DayCycle;
using ClientIsKing.Genre;
using ClientIsKing.Managers;
using ClientIsKing.Service;
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
    }
}
