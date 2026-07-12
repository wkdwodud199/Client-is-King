using System.Collections;
using System.IO;
using System.Linq;
using ClientIsKing.DayCycle;
using ClientIsKing.Genre;
using ClientIsKing.Managers;
using ClientIsKing.Service;
using ClientIsKing.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ClientIsKing.Tests.PlayMode
{
    /// <summary>
    /// task-113 U5: 실파일 경유 저장/불러오기 통합 — 자동 저장 트리거 5종의 실제 파일 갱신, 왕복 후
    /// RT1/RT3 동일성, Night 세이브 이어하기로 Shop 진입 시 화면·persistent 매니저 생존을 검증한다.
    /// 이 파일이 전 PlayMode 테스트에 경로 override 를 거는 [SetUpFixture] 를 소유한다 —
    /// 실사용 Application.persistentDataPath/save.json 을 어떤 PlayMode 테스트도 건드리지 않는다.
    /// </summary>
    [SetUpFixture]
    public class SaveLoadPlayModeFixture
    {
        static string testDir;

        [OneTimeSetUp]
        public void GlobalSetUp()
        {
            testDir = Path.Combine(Application.temporaryCachePath, "task113-playmode-savetests-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDir);
            GameManager.SaveFilePathOverride = Path.Combine(testDir, "save.json");
        }

        [OneTimeTearDown]
        public void GlobalTearDown()
        {
            GameManager.SaveFilePathOverride = null;
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, recursive: true);
            }
        }
    }

    public class SaveLoadPlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // 다음 PlayMode 테스트가 깨끗한 씬에서 시작하도록 MainMenu 로 되돌린다 (기존 6개 전례와 동일).
            if (SceneManager.GetActiveScene().name != "MainMenu")
            {
                yield return SceneManager.LoadSceneAsync("MainMenu");
            }
            yield return null;

            // 다음 테스트가 파일 없음 상태에서 시작하도록 override 경로의 세이브를 정리한다
            // (SetUpFixture 는 세션당 1회만 경로를 잡으므로, 파일 자체는 테스트마다 이 TearDown 이 청소한다).
            if (GameManager.SaveFilePathOverride != null && File.Exists(GameManager.SaveFilePathOverride))
            {
                File.Delete(GameManager.SaveFilePathOverride);
            }
        }

        static string SavePath => GameManager.SaveFilePathOverride;

        [UnityTest]
        public IEnumerator StartNewRun_Writes_File_Then_Each_Trigger_Updates_It_Then_Load_Restores_Equivalent_State()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm);
            Assert.IsFalse(File.Exists(SavePath), "테스트 시작 시점에는 세이브 파일이 없어야 한다");

            // 트리거 1 — StartNewRun (게임 시작 도메인 경로).
            gm.StartNewRun();
            Assert.IsTrue(File.Exists(SavePath), "트리거1(새 런 시작) 후 파일이 생성되어야 한다");
            Assert.AreEqual("", ReadSavedField("selectedGenreId"), "트리거1 직후는 미선택 상태가 저장되어야 한다");
            Assert.AreEqual("0", ReadSavedField("currentPhase"), "트리거1 직후는 Market(0) 이 저장되어야 한다");

            yield return SceneManager.LoadSceneAsync("Shop");
            yield return null;
            gm = GameManager.Instance;

            // 트리거 4 — 장르 확정 (파일 내용으로 직접 반영 확인 — mtime 이 아니라 저장된 필드값 비교).
            var genreIds = gm.GenreCatalog.Select(g => g.Id).ToList();
            var selection = GenreSelectionOps.TrySelect(gm.State, "gukbap", genreIds);
            Assert.IsTrue(selection.Success, selection.Message);
            GameEvents.RaiseGenreSelected(selection.GenreId);
            yield return null;
            Assert.AreEqual("gukbap", ReadSavedField("selectedGenreId"), "트리거4(장르 확정) 저장 내용이 반영되어야 한다");

            // 트리거 2 — phase 전환 완료 (Market -> Service).
            var service = ServiceManager.Instance;
            Assert.AreEqual(DayPhase.Service, gm.AdvancePhase());
            yield return null;
            Assert.AreEqual("1", ReadSavedField("currentPhase"), "트리거2(phase 전환) 후 저장된 phase 가 Service(1) 여야 한다");

            while (service.CurrentOrder != null)
            {
                service.SkipCurrentOrder();
            }

            // 트리거 3 — 정산 신규 적용 (Service -> Settlement 전환 시 AdvancePhase 내부 인라인 적용).
            Assert.AreEqual(DayPhase.Settlement, gm.AdvancePhase());
            yield return null;
            Assert.AreEqual("2", ReadSavedField("currentPhase"), "트리거3(정산 적용) 후 저장된 phase 가 Settlement(2) 여야 한다");
            Assert.AreEqual(gm.State.settlementDay.ToString(), ReadSavedField("settlementDay"), "정산 적용 결과(settlementDay)가 저장되어야 한다");

            Assert.AreEqual(DayPhase.Night, gm.AdvancePhase());
            yield return null;

            // 트리거 5 — SNS 집행.
            Assert.IsTrue(service.TryExecuteSnsCampaign("photo_feed", out var execResult), execResult.Message);
            GameEvents.RaiseSNSCampaignExecuted("photo_feed");
            yield return null;
            StringAssert.Contains("\"photo_feed\"", File.ReadAllText(SavePath), "트리거5(SNS 집행) 후 집행 레코드가 저장되어야 한다");

            // RT1/RT3 — 저장된 상태를 스냅샷한 뒤 상태를 소거하고, 로드가 field-by-field 동일 + plan 동일을 보장하는지 확인.
            int dayBefore = gm.State.day;
            int cashBefore = gm.State.cash;
            var genreBefore = gm.State.selectedGenreId;
            bool okPlanBefore = service.TryBuildDayPlan(gm.GenreCatalog.First(g => g.Id == genreBefore), out var planBefore, out var planReasonBefore);
            Assert.IsTrue(okPlanBefore, planReasonBefore);

            gm.StartNewGame(); // 상태 소거 (다른 런으로 교체)
            Assert.AreEqual(1, gm.State.day, "StartNewGame 직후는 항상 Day1 (소거 확인)");

            Assert.IsTrue(gm.TryLoadGame(out var loadReason), loadReason);
            Assert.AreEqual(dayBefore, gm.State.day, "RT1: day 필드가 저장 전과 동일해야 한다");
            Assert.AreEqual(cashBefore, gm.State.cash, "RT1: cash 필드가 저장 전과 동일해야 한다");
            Assert.AreEqual(genreBefore, gm.State.selectedGenreId, "RT1: selectedGenreId 가 저장 전과 동일해야 한다");

            bool okPlanAfter = service.TryBuildDayPlan(gm.GenreCatalog.First(g => g.Id == genreBefore), out var planAfter, out var planReasonAfter);
            Assert.IsTrue(okPlanAfter, planReasonAfter);
            Assert.AreEqual(planBefore.OrderCount, planAfter.OrderCount, "RT3: 왕복 전후 plan.OrderCount 가 동일해야 한다");
            Assert.AreEqual(planBefore.BaseOrderCount, planAfter.BaseOrderCount, "RT3: BaseOrderCount 동일");
            Assert.AreEqual(planBefore.BonusOrderCount, planAfter.BonusOrderCount, "RT3: BonusOrderCount 동일");

            var ordersBefore = ServiceOps.BuildOrders(planBefore, service.CustomerDefs);
            var ordersAfter = ServiceOps.BuildOrders(planAfter, service.CustomerDefs);
            Assert.AreEqual(ordersBefore.Count, ordersAfter.Count);
            for (int i = 0; i < ordersBefore.Count; i++)
            {
                Assert.AreEqual(ordersBefore[i].recipeId, ordersAfter[i].recipeId, $"RT3 주문 {i} recipeId");
                Assert.AreEqual(ordersBefore[i].customerId, ordersAfter[i].customerId, $"RT3 주문 {i} customerId");
                Assert.AreEqual(ordersBefore[i].partySize, ordersAfter[i].partySize, $"RT3 주문 {i} partySize");
                Assert.AreEqual(ordersBefore[i].snsInflow, ordersAfter[i].snsInflow, $"RT3 주문 {i} snsInflow");
            }
        }

        [UnityTest]
        public IEnumerator MainMenu_Continue_With_Night_Save_Loads_Shop_With_Night_Panel_And_Persistent_Managers()
        {
            // 1) 도메인 경로로 Night phase 세이브를 실제 파일에 준비한다.
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            var gm = GameManager.Instance;
            gm.StartNewRun();

            yield return SceneManager.LoadSceneAsync("Shop");
            yield return null;
            gm = GameManager.Instance;
            var genreIds = gm.GenreCatalog.Select(g => g.Id).ToList();
            var selection = GenreSelectionOps.TrySelect(gm.State, "bunsik", genreIds);
            Assert.IsTrue(selection.Success, selection.Message);

            var service = ServiceManager.Instance;
            Assert.AreEqual(DayPhase.Service, gm.AdvancePhase());
            while (service.CurrentOrder != null)
            {
                service.SkipCurrentOrder();
            }
            Assert.AreEqual(DayPhase.Settlement, gm.AdvancePhase());
            Assert.AreEqual(DayPhase.Night, gm.AdvancePhase());
            yield return null;
            Assert.IsTrue(File.Exists(SavePath), "Night 진입까지의 트리거로 파일이 준비되어 있어야 한다");

            var genreCatalogSnapshot = gm.GenreCatalog.Count;
            var eventCatalogSnapshot = gm.EventCatalog.Count;
            var snsCatalogSnapshot = service.SnsCampaignDefs.Count;

            // 2) MainMenu 로 돌아가 이어하기 도메인 경로(TryLoadGame + LoadShopScene)로 재진입한다.
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            gm = GameManager.Instance;

            Assert.IsTrue(gm.HasSaveFile);
            Assert.IsTrue(gm.TryPeekSave(out var summary, out var peekReason), peekReason);
            Assert.AreEqual(DayPhase.Night, summary.Phase);

            Assert.IsTrue(gm.TryLoadGame(out var loadReason), loadReason);
            gm.LoadShopScene();
            yield return null;
            yield return null; // Start() 동기화까지 프레임 여유

            var canvasGo = GameObject.Find("Canvas");
            Assert.IsNotNull(canvasGo, "Shop Canvas 누락");
            var nightPanel = canvasGo.transform.Find("Panel_Night");
            Assert.IsNotNull(nightPanel, "Panel_Night 누락");
            Assert.IsTrue(nightPanel.gameObject.activeSelf, "Night 세이브 이어하기 후 Night 패널이 활성이어야 한다 (G3)");

            var dayPhaseText = canvasGo.transform.Find("DayPhaseText").GetComponent<TMPro.TMP_Text>();
            StringAssert.Contains("밤", dayPhaseText.text, $"HUD 는 'Day n — 밤' 을 표시해야 한다: '{dayPhaseText.text}'");

            var gmAfterLoad = GameManager.Instance;
            var serviceAfterLoad = ServiceManager.Instance;
            Assert.IsNotNull(gmAfterLoad, "GameManager persistent instance 생존해야 한다");
            Assert.IsNotNull(serviceAfterLoad, "ServiceManager persistent instance 생존해야 한다");
            Assert.AreEqual(genreCatalogSnapshot, gmAfterLoad.GenreCatalog.Count, "장르 catalog 4종 생존");
            Assert.AreEqual(eventCatalogSnapshot, gmAfterLoad.EventCatalog.Count, "이벤트 catalog 4종 생존");
            Assert.AreEqual(snsCatalogSnapshot, serviceAfterLoad.SnsCampaignDefs.Count, "SNS catalog 3종 생존");
        }

        /// <summary>
        /// 저장 파일의 "field": 값 라인에서 값을 문자열로 추출한다(정규형 JSON 은 필드당 한 줄 —
        /// V2b 계약). 문자열 값은 따옴표 안쪽을, 숫자 값은 콤마/줄바꿈 전까지를 반환한다.
        /// 테스트 전용 파서다 — 프로덕션 역직렬화는 GameManager.TryLoadGame(SaveOps) 만 담당한다.
        /// </summary>
        static string ReadSavedField(string fieldName)
        {
            string json = File.ReadAllText(SavePath);
            string key = $"\"{fieldName}\"";
            int idx = json.IndexOf(key);
            Assert.Greater(idx, -1, $"{fieldName} 필드가 파일에 있어야 한다");
            int colon = json.IndexOf(':', idx);
            int valueStart = colon + 1;
            while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
            {
                valueStart++;
            }
            if (json[valueStart] == '"')
            {
                int quoteStart = valueStart + 1;
                int quoteEnd = json.IndexOf('"', quoteStart);
                return json.Substring(quoteStart, quoteEnd - quoteStart);
            }
            int valueEnd = valueStart;
            while (valueEnd < json.Length && json[valueEnd] != ',' && json[valueEnd] != '\n' && json[valueEnd] != '\r' && json[valueEnd] != '}')
            {
                valueEnd++;
            }
            return json.Substring(valueStart, valueEnd - valueStart).Trim();
        }
    }
}
