using System.IO;
using System.Linq;
using ClientIsKing.DayCycle;
using ClientIsKing.Managers;
using ClientIsKing.Service;
using NUnit.Framework;
using UnityEngine;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-113 U2 자체검증: GameManager 저장/불러오기 API — SaveGame/TryLoadGame/TryPeekSave 원상복구,
    /// V11 주문 identity 롤백, 자동 저장 트리거 gating(isPlaying=false 에서 파일 미생성), StartNewRun.
    /// 모든 파일 I/O 는 Application.temporaryCachePath 하위 override 경로만 사용한다(GameManager.
    /// SaveFilePathOverride, IVT). 실사용 persistentDataPath/리포에는 어떤 산출물도 남기지 않는다.
    /// </summary>
    public class GameManagerSaveLoadTests
    {
        string testDir;

        [SetUp]
        public void SetUp()
        {
            testDir = Path.Combine(Application.temporaryCachePath, "task113-gm-savetests-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDir);
            GameManager.SaveFilePathOverride = Path.Combine(testDir, "save.json");
        }

        [TearDown]
        public void TearDown()
        {
            GameManager.SaveFilePathOverride = null;
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, recursive: true);
            }
        }

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

        // ── SaveGame / TryLoadGame 기본 왕복 ─────────────────────────────────

        [Test]
        public void SaveGame_Writes_File_At_Override_Path()
        {
            var gm = OpenGameManager();
            Assert.IsFalse(gm.HasSaveFile);

            Assert.IsTrue(gm.SaveGame(out var reason), reason);
            Assert.IsTrue(gm.HasSaveFile);
            Assert.IsTrue(File.Exists(GameManager.SaveFilePathOverride));
        }

        [Test]
        public void SaveGame_Then_TryLoadGame_Restores_Equivalent_State()
        {
            var gm = OpenGameManager();
            SelectGenre(gm, "bunsik");
            gm.State.cash = 12345;
            Assert.IsTrue(gm.SaveGame(out var saveReason), saveReason);

            gm.StartNewGame(); // 상태 소거 (다른 런으로 교체)
            Assert.AreNotEqual(12345, gm.State.cash);

            Assert.IsTrue(gm.TryLoadGame(out var loadReason), loadReason);
            Assert.AreEqual(12345, gm.State.cash);
            Assert.AreEqual("bunsik", gm.State.selectedGenreId);
        }

        [Test]
        public void TryLoadGame_Fails_When_No_Save_File()
        {
            var gm = OpenGameManager();
            Assert.IsFalse(gm.TryLoadGame(out var reason));
            Assert.AreEqual("저장 파일이 없습니다.", reason);
        }

        // ── TryPeekSave dry-run 원상복구 ─────────────────────────────────────

        [Test]
        public void TryPeekSave_Succeeds_And_Leaves_State_Reference_And_Content_Unchanged()
        {
            var gm = OpenGameManager();
            SelectGenre(gm, "bunsik");
            gm.State.cash = 5555;
            Assert.IsTrue(gm.SaveGame(out _));

            var stateBefore = gm.State;
            Assert.IsTrue(gm.TryPeekSave(out var summary, out var reason), reason);

            Assert.AreSame(stateBefore, gm.State, "peek 은 state 참조를 원상복구해야 한다");
            Assert.AreEqual(5555, gm.State.cash);
            Assert.AreEqual("bunsik", summary.SelectedGenreId);
            Assert.AreEqual(5555, summary.Cash);
        }

        [Test]
        public void TryPeekSave_Fails_And_Leaves_State_Unchanged_When_File_Corrupt()
        {
            var gm = OpenGameManager();
            var stateBefore = gm.State;
            File.WriteAllText(GameManager.SaveFilePathOverride, "{not valid json");

            Assert.IsFalse(gm.TryPeekSave(out var summary, out var reason));
            Assert.IsNull(summary);
            Assert.IsNotEmpty(reason);
            Assert.AreSame(stateBefore, gm.State, "peek 실패도 state 참조를 원상복구해야 한다");
        }

        // ── V11 주문 identity 롤백 ───────────────────────────────────────────

        [Test]
        public void TryLoadGame_Rolls_Back_When_Order_RecipeId_Tampered()
        {
            var gm = AdvanceToServiceWithOrders();
            Assert.IsTrue(gm.SaveGame(out var saveReason), saveReason);

            string json = File.ReadAllText(GameManager.SaveFilePathOverride);
            var otherRecipeId = FindDifferentRecipeId(gm.State.serviceOrders[0].recipeId);
            string tampered = json.Replace(
                $"\"recipeId\": \"{gm.State.serviceOrders[0].recipeId}\"",
                $"\"recipeId\": \"{otherRecipeId}\"");
            Assert.AreNotEqual(json, tampered, "치환이 실제로 발생해야 테스트가 유효하다");
            File.WriteAllText(GameManager.SaveFilePathOverride, tampered);

            var stateBefore = gm.State;
            bool ok = gm.TryLoadGame(out var reason);
            Assert.IsFalse(ok, "recipeId 변조는 V11 에서 명시적으로 실패해야 한다");
            StringAssert.Contains("저장된 주문이 수요 계획과 일치하지 않습니다", reason);
            Assert.AreSame(stateBefore, gm.State, "V11 실패는 이전 상태로 롤백해야 한다");
        }

        [Test]
        public void TryLoadGame_Rolls_Back_When_Order_PartySize_Tampered()
        {
            var gm = AdvanceToServiceWithOrders();
            Assert.IsTrue(gm.SaveGame(out _));

            string json = File.ReadAllText(GameManager.SaveFilePathOverride);
            int originalPartySize = gm.State.serviceOrders[0].partySize;
            string tampered = json.Replace(
                $"\"partySize\": {originalPartySize}", $"\"partySize\": {originalPartySize + 50}");
            Assert.AreNotEqual(json, tampered);
            File.WriteAllText(GameManager.SaveFilePathOverride, tampered);

            var stateBefore = gm.State;
            Assert.IsFalse(gm.TryLoadGame(out var reason));
            StringAssert.Contains("저장된 주문이 수요 계획과 일치하지 않습니다", reason);
            Assert.AreSame(stateBefore, gm.State);
        }

        [Test]
        public void TryLoadGame_Rolls_Back_When_Order_CustomerId_Tampered()
        {
            var gm = AdvanceToServiceWithOrders();
            Assert.IsTrue(gm.SaveGame(out var saveReason), saveReason);

            string json = File.ReadAllText(GameManager.SaveFilePathOverride);
            var otherCustomerId = FindDifferentCustomerId(gm.State.serviceOrders[0].customerId);
            string tampered = json.Replace(
                $"\"customerId\": \"{gm.State.serviceOrders[0].customerId}\"",
                $"\"customerId\": \"{otherCustomerId}\"");
            Assert.AreNotEqual(json, tampered, "치환이 실제로 발생해야 테스트가 유효하다");
            File.WriteAllText(GameManager.SaveFilePathOverride, tampered);

            var stateBefore = gm.State;
            bool ok = gm.TryLoadGame(out var reason);
            Assert.IsFalse(ok, "customerId 변조는 V11 에서 명시적으로 실패해야 한다");
            StringAssert.Contains("저장된 주문이 수요 계획과 일치하지 않습니다", reason);
            Assert.AreSame(stateBefore, gm.State, "V11 실패는 이전 상태로 롤백해야 한다");
        }

        [Test]
        public void TryLoadGame_Rolls_Back_When_Order_SnsInflow_Tampered()
        {
            var gm = AdvanceToServiceWithOrders();
            Assert.IsTrue(gm.SaveGame(out var saveReason), saveReason);

            string json = File.ReadAllText(GameManager.SaveFilePathOverride);
            bool originalSnsInflow = gm.State.serviceOrders[0].snsInflow;
            string tampered = json.Replace(
                $"\"snsInflow\": {(originalSnsInflow ? "true" : "false")}",
                $"\"snsInflow\": {(originalSnsInflow ? "false" : "true")}");
            Assert.AreNotEqual(json, tampered, "치환이 실제로 발생해야 테스트가 유효하다");
            File.WriteAllText(GameManager.SaveFilePathOverride, tampered);

            var stateBefore = gm.State;
            bool ok = gm.TryLoadGame(out var reason);
            Assert.IsFalse(ok, "snsInflow 변조는 V11 에서 명시적으로 실패해야 한다");
            StringAssert.Contains("저장된 주문이 수요 계획과 일치하지 않습니다", reason);
            Assert.AreSame(stateBefore, gm.State, "V11 실패는 이전 상태로 롤백해야 한다");
        }

        [Test]
        public void TryLoadGame_Rolls_Back_When_Order_EventInflow_Tampered()
        {
            var gm = AdvanceToServiceWithOrders();
            Assert.IsTrue(gm.SaveGame(out var saveReason), saveReason);

            string json = File.ReadAllText(GameManager.SaveFilePathOverride);
            bool originalEventInflow = gm.State.serviceOrders[0].eventInflow;
            string tampered = json.Replace(
                $"\"eventInflow\": {(originalEventInflow ? "true" : "false")}",
                $"\"eventInflow\": {(originalEventInflow ? "false" : "true")}");
            Assert.AreNotEqual(json, tampered, "치환이 실제로 발생해야 테스트가 유효하다");
            File.WriteAllText(GameManager.SaveFilePathOverride, tampered);

            var stateBefore = gm.State;
            bool ok = gm.TryLoadGame(out var reason);
            Assert.IsFalse(ok, "eventInflow 변조는 V11 에서 명시적으로 실패해야 한다");
            StringAssert.Contains("저장된 주문이 수요 계획과 일치하지 않습니다", reason);
            Assert.AreSame(stateBefore, gm.State, "V11 실패는 이전 상태로 롤백해야 한다");
        }

        [Test]
        public void TryPeekSave_Fails_And_Leaves_State_Unchanged_When_Order_Identity_Tampered()
        {
            // 회귀 방지 — Codex 코드리뷰 P1: TryPeekSave 가 V11 을 생략하면 주문 개수만 맞는
            // 변조 세이브가 MainMenu 에서 "정상"으로 표시되어 이어하기가 활성화되는 버그가 있었다.
            var gm = AdvanceToServiceWithOrders();
            Assert.IsTrue(gm.SaveGame(out var saveReason), saveReason);

            string json = File.ReadAllText(GameManager.SaveFilePathOverride);
            var otherRecipeId = FindDifferentRecipeId(gm.State.serviceOrders[0].recipeId);
            string tampered = json.Replace(
                $"\"recipeId\": \"{gm.State.serviceOrders[0].recipeId}\"",
                $"\"recipeId\": \"{otherRecipeId}\"");
            Assert.AreNotEqual(json, tampered, "치환이 실제로 발생해야 테스트가 유효하다");
            File.WriteAllText(GameManager.SaveFilePathOverride, tampered);

            var stateBefore = gm.State;
            bool ok = gm.TryPeekSave(out var summary, out var reason);
            Assert.IsFalse(ok, "V11 변조 파일은 peek 도 실패해야 한다(MainMenu 이어하기 잠금 전제)");
            Assert.IsNull(summary);
            StringAssert.Contains("저장된 주문이 수요 계획과 일치하지 않습니다", reason);
            Assert.AreSame(stateBefore, gm.State, "peek 실패도 state 참조를 원상복구해야 한다");
        }

        [Test]
        public void TryLoadGame_Rolls_Back_When_No_Recipe_Matches_Selected_Genre()
        {
            // 장르 자체는 GenreCatalog(V5 검증 대상)에 그대로 남아 있어야 V11(plan 재구성)만 격리해서
            // 검증할 수 있다 — 대신 ServiceManager 의 recipe catalog 에서 "bunsik" 레시피 2종만 제거해
            // (catalog 는 비지 않음 — SaveCatalogInputs 검증 통과) TryBuildDayPlan 이 "매칭되는 레시피가
            // 없습니다" 로 실패하게 만든다(asset 유실 모사).
            var gm = OpenGameManager();
            SelectGenre(gm, "bunsik");
            Assert.IsTrue(gm.SaveGame(out _));

            var service = ServiceManager.Instance;
            var reducedRecipes = service.RecipeDefs.Where(r => r != null && r.Genre != null && r.Genre.Id != "bunsik").ToList();
            Assert.Greater(reducedRecipes.Count, 0, "픽스처 전제: bunsik 이외 레시피가 남아 있어야 카탈로그가 비지 않는다");
            service.EditorInit(reducedRecipes, service.CustomerDefs.ToList(), service.SnsCampaignDefs.ToList());

            var stateBefore = gm.State;
            Assert.IsFalse(gm.TryLoadGame(out var reason));
            StringAssert.Contains("저장 상태로 수요 계획을 재구성할 수 없습니다", reason);
            Assert.AreSame(stateBefore, gm.State);
        }

        [Test]
        public void TryLoadGame_Succeeds_When_Only_Served_And_Index_Differ()
        {
            var gm = AdvanceToServiceWithOrders();
            Assert.IsTrue(gm.SaveGame(out var saveReason), saveReason);

            // served/missed/index 만 다른 손상은 재생성 비교 대상이 아니므로 로드가 성공해야 한다
            // (V7 이 이미 정합을 담당 — 여기서는 실제로 하나 서빙 처리해 저장 후 재로드).
            var service = ServiceManager.Instance;
            service.SkipCurrentOrder();
            Assert.IsTrue(gm.SaveGame(out var saveReason2), saveReason2);

            gm.StartNewGame();
            Assert.IsTrue(gm.TryLoadGame(out var loadReason), loadReason);
            Assert.IsTrue(gm.State.serviceOrders[0].missed);
        }

        // ── 자동 저장 트리거 gating ──────────────────────────────────────────

        [Test]
        public void AdvancePhase_Does_Not_Write_File_When_Not_Playing()
        {
            Assert.IsFalse(Application.isPlaying, "EditMode 테스트 전제");
            var gm = OpenGameManager();
            SelectGenre(gm, "generalist");

            gm.AdvancePhase(); // Market -> Service (phase 전환 발생)

            Assert.IsFalse(File.Exists(GameManager.SaveFilePathOverride),
                "EditMode(Application.isPlaying==false) 에서는 AdvancePhase 가 파일을 쓰면 안 된다");
        }

        [Test]
        public void SaveGame_Explicit_Call_Writes_File_Even_When_Not_Playing()
        {
            Assert.IsFalse(Application.isPlaying);
            var gm = OpenGameManager();
            Assert.IsTrue(gm.SaveGame(out var reason), reason);
            Assert.IsTrue(File.Exists(GameManager.SaveFilePathOverride),
                "명시적 SaveGame() 은 isPlaying 가드 없이 항상 동작해야 한다");
        }

        [Test]
        public void StartNewRun_Does_Not_Write_File_When_Not_Playing_But_Resets_State()
        {
            Assert.IsFalse(Application.isPlaying);
            var gm = OpenGameManager();
            gm.State.cash = 999;

            gm.StartNewRun();

            Assert.AreEqual(GameState.StartingCash, gm.State.cash, "StartNewRun 은 항상 새 상태로 초기화해야 한다");
            Assert.IsFalse(File.Exists(GameManager.SaveFilePathOverride),
                "AutoSave 내부 호출도 isPlaying 가드를 따라야 한다(EditMode 무회귀 전제)");
        }

        // ── helpers ──────────────────────────────────────────────────────────

        static GameManager AdvanceToServiceWithOrders()
        {
            var gm = OpenGameManager();
            SelectGenre(gm, "generalist");
            Assert.AreEqual(DayPhase.Service, gm.AdvancePhase());
            Assert.Greater(gm.State.serviceOrders.Count, 0, "픽스처 전제: 최소 1건의 주문이 있어야 한다");
            return gm;
        }

        static string FindDifferentRecipeId(string currentId)
        {
            var service = ServiceManager.Instance;
            foreach (var recipe in service.RecipeDefs)
            {
                if (recipe != null && recipe.Id != currentId)
                {
                    return recipe.Id;
                }
            }
            Assert.Fail("픽스처 전제: 최소 2종의 레시피가 있어야 한다");
            return null;
        }

        static string FindDifferentCustomerId(string currentId)
        {
            var service = ServiceManager.Instance;
            foreach (var customer in service.CustomerDefs)
            {
                if (customer != null && customer.Id != currentId)
                {
                    return customer.Id;
                }
            }
            Assert.Fail("픽스처 전제: 최소 2종의 고객 archetype 이 있어야 한다");
            return null;
        }
    }
}
