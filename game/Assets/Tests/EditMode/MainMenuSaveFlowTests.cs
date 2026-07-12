using System.IO;
using System.Linq;
using ClientIsKing.Managers;
using ClientIsKing.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-113 U5: MainMenuController 의 G2 분기 4종(없음/정상/파산/손상) + task-115 D3 클리어 분기 —
    /// HasSaveFile/TryPeekSave 결과만 표시하고 재계산하지 않는지, 손상·파산·클리어 세이브가 조용히
    /// 새 게임으로 넘어가지 않는지 검증한다.
    /// 모든 파일 I/O 는 GameManager.SaveFilePathOverride(temporaryCachePath 하위)로 격리한다.
    /// </summary>
    public class MainMenuSaveFlowTests
    {
        string testDir;

        [SetUp]
        public void SetUp()
        {
            testDir = Path.Combine(Application.temporaryCachePath, "task113-mainmenu-savetests-" + System.Guid.NewGuid().ToString("N"));
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

        static (GameObject gmGo, MainMenuController controller, Button continueButton, TMPro.TMP_Text saveStatusText) OpenMainMenu()
        {
            var gmGo = TestSceneSupport.OpenMainMenuSceneWithLiveSingletons();
            var canvasGo = gmGo.scene.GetRootGameObjects().First(go => go.name == "Canvas");
            var controller = canvasGo.GetComponent<MainMenuController>();
            var continueButton = canvasGo.transform.Find("ContinueButton").GetComponent<Button>();
            var saveStatusText = canvasGo.transform.Find("SaveStatusText").GetComponent<TMPro.TMP_Text>();
            // 배치 EditMode 에서는 Awake() 가 동기 호출된다는 보장이 없다 — onClick.AddListener 배선을
            // 강제해 클릭 흐름 테스트가 실제로 핸들러를 타도록 한다(task-113 U5 공통 함정).
            TestSceneSupport.ForceAwake(controller);
            return (gmGo, controller, continueButton, saveStatusText);
        }

        // ── 분기 1: 저장 파일 없음 ───────────────────────────────────────────

        [Test]
        public void RefreshSaveUi_Shows_No_Save_Branch_When_File_Absent()
        {
            var (gmGo, controller, continueButton, saveStatusText) = OpenMainMenu();
            var gm = gmGo.GetComponent<GameManager>();
            gm.StartNewGame();
            Assert.IsFalse(gm.HasSaveFile);

            TestSceneSupport.ForceStart(controller);

            Assert.IsFalse(continueButton.interactable, "저장 파일이 없으면 이어하기는 비활성이어야 한다");
            Assert.AreEqual("저장된 게임이 없습니다.", saveStatusText.text);
            AssertColor(saveStatusText, 0xF4, 0xE5, 0xC2); // Steam Cream
        }

        // ── 분기 2: 정상 세이브 ──────────────────────────────────────────────

        [Test]
        public void RefreshSaveUi_Shows_Normal_Branch_When_Save_Valid()
        {
            var (gmGo, controller, continueButton, saveStatusText) = OpenMainMenu();
            var gm = gmGo.GetComponent<GameManager>();
            gm.StartNewGame();
            gm.State.cash = 47350;
            Assert.IsTrue(gm.SaveGame(out var saveReason), saveReason);

            TestSceneSupport.ForceStart(controller);

            Assert.IsTrue(continueButton.interactable, "정상 세이브는 이어하기가 활성이어야 한다");
            StringAssert.Contains("Day 1", saveStatusText.text);
            StringAssert.Contains("장보기", saveStatusText.text);
            StringAssert.Contains("47,350", saveStatusText.text);
            AssertColor(saveStatusText, 0xF4, 0xE5, 0xC2); // Steam Cream
        }

        [Test]
        public void RefreshSaveUi_Does_Not_Recompute_Summary_Beyond_Peek_Result()
        {
            // TryPeekSave 가 반환한 값 그대로 표시되는지 확인 — state.cash 를 peek 이후 바꿔도
            // 이미 렌더된 문구는 재계산되지 않는다(재계산 금지 규약, 정적 표시 확인).
            var (gmGo, controller, continueButton, saveStatusText) = OpenMainMenu();
            var gm = gmGo.GetComponent<GameManager>();
            gm.StartNewGame();
            gm.State.cash = 1000;
            Assert.IsTrue(gm.SaveGame(out _));

            TestSceneSupport.ForceStart(controller);
            string renderedOnce = saveStatusText.text;

            gm.State.cash = 999999; // 저장 파일과 무관한 런타임 상태 변경 — RefreshSaveUi 를 다시 부르지 않으면 불변
            Assert.AreEqual(renderedOnce, saveStatusText.text, "재호출 없이는 표시 문구가 그대로여야 한다");
        }

        // ── 분기 3: 파산 세이브 ──────────────────────────────────────────────

        [Test]
        public void RefreshSaveUi_Shows_Bankrupt_Branch_And_Locks_Continue()
        {
            var (gmGo, controller, continueButton, saveStatusText) = OpenMainMenu();
            var gm = gmGo.GetComponent<GameManager>();
            gm.StartNewGame();
            gm.State.selectedGenreId = "bunsik"; // V5 통과 — Night 는 빈 장르 예외(day1/Market) 대상이 아니다
            gm.State.currentPhase = ClientIsKing.DayCycle.DayPhase.Night;
            gm.State.serviceDay = 1;
            gm.State.settlementDay = 1;
            gm.State.cash = 0;
            gm.State.isBankrupt = true;
            gm.State.bankruptcyDay = 1;
            gm.State.bankruptcyReason = "Day 1 운영비 12,000원 미납 (부족액 8,000원)";
            Assert.IsTrue(gm.SaveGame(out var saveReason), saveReason);

            TestSceneSupport.ForceStart(controller);

            Assert.IsFalse(continueButton.interactable, "파산 세이브는 이어하기를 잠가야 한다");
            StringAssert.Contains("파산으로 끝났습니다", saveStatusText.text);
            StringAssert.Contains("Day 1", saveStatusText.text);
            AssertColor(saveStatusText, 0xA9, 0x3E, 0x58); // Warning Plum
        }

        // ── 분기 5: 클리어 세이브 (task-115 D3 — 4→5분기) ───────────────────

        [Test]
        public void RefreshSaveUi_Shows_Cleared_Branch_And_Locks_Continue()
        {
            var (gmGo, controller, continueButton, saveStatusText) = OpenMainMenu();
            var gm = gmGo.GetComponent<GameManager>();
            gm.StartNewGame();
            // 클리어 세이브는 정상 v1 세이브다 (Day N·Settlement·settlementDay==N·daysCompleted==N —
            // BalanceEndingGuardTests 왕복 fixture 미러, task-115 C4). 장르 선택은 Day 1 에만 허용되므로
            // 먼저 Day 1 에서 선택한 뒤 day 를 7로 맞추고(TryBuildDayPlan 이 state.day 를 시드로 쓴다)
            // Service phase 로 진입해 오늘(7일차) 주문을 생성·전부 처리(served/missed)해야 serviceDay==day
            // 로 V11(주문 identity, task-113)이 재생성과 실제로 일치하고, Settlement 저장 시 V7(미처리
            // 주문 잔존 금지)도 통과한다.
            var genreIds = gm.GenreCatalog.Select(g => g.Id).ToList();
            var selection = ClientIsKing.Genre.GenreSelectionOps.TrySelect(gm.State, "bunsik", genreIds);
            Assert.IsTrue(selection.Success, selection.Message);
            gm.State.day = ClientIsKing.DayCycle.EndingOps.ClearTargetDays;
            Assert.AreEqual(ClientIsKing.DayCycle.DayPhase.Service, gm.AdvancePhase());
            Assert.AreEqual(ClientIsKing.DayCycle.EndingOps.ClearTargetDays, gm.State.serviceDay,
                "픽스처 전제: day 를 미리 맞춘 뒤 진입해야 serviceDay==day 로 V11 비교 대상이 된다");
            var service = ClientIsKing.Service.ServiceManager.Instance;
            while (gm.State.serviceOrders.Any(o => o.IsOpen))
            {
                service.SkipCurrentOrder();
            }
            gm.State.currentPhase = ClientIsKing.DayCycle.DayPhase.Settlement;
            gm.State.settlementDay = ClientIsKing.DayCycle.EndingOps.ClearTargetDays;
            gm.State.daysCompleted = ClientIsKing.DayCycle.EndingOps.ClearTargetDays;
            gm.State.cash = 150000;
            Assert.IsTrue(gm.SaveGame(out var saveReason), saveReason);

            TestSceneSupport.ForceStart(controller);

            Assert.IsFalse(continueButton.interactable, "클리어 세이브는 이어하기를 잠가야 한다 (D3)");
            Assert.AreEqual("데모 클리어! (영업 7일 달성) — 새 게임을 시작하세요.", saveStatusText.text,
                "D3 클리어 문구 정확 일치 (Codex 소유 UX copy)");
            AssertColor(saveStatusText, 0xE5, 0xA8, 0x4B); // Brass Amber
        }

        [Test]
        public void RefreshSaveUi_Bankrupt_Takes_Priority_Over_Cleared()
        {
            // 분기 우선순위(파산 → 클리어 → 정상, D3) — daysCompleted ≥ N 이면서 파산인 유일한
            // v1 유효 형태는 "Day 8 파산"(settledToday && bankrupt → daysCompleted == day−1 == 7,
            // SaveOps V3 규칙)이다. 파산 시 daysCompleted 미갱신이라 같은 날 동시 성립은 표현 불가(C1).
            var (gmGo, controller, continueButton, saveStatusText) = OpenMainMenu();
            var gm = gmGo.GetComponent<GameManager>();
            gm.StartNewGame();
            int dayAfterClear = ClientIsKing.DayCycle.EndingOps.ClearTargetDays + 1;
            gm.State.selectedGenreId = "bunsik";
            gm.State.day = dayAfterClear;
            gm.State.currentPhase = ClientIsKing.DayCycle.DayPhase.Night;
            gm.State.serviceDay = dayAfterClear;
            gm.State.settlementDay = dayAfterClear;
            gm.State.daysCompleted = ClientIsKing.DayCycle.EndingOps.ClearTargetDays;
            gm.State.cash = 0;
            gm.State.isBankrupt = true;
            gm.State.bankruptcyDay = dayAfterClear;
            gm.State.bankruptcyReason = "Day 8 운영비 15,000원 미납 (부족액 15,000원)";
            Assert.IsTrue(gm.SaveGame(out var saveReason), saveReason);

            TestSceneSupport.ForceStart(controller);

            Assert.IsFalse(continueButton.interactable);
            StringAssert.Contains("파산으로 끝났습니다", saveStatusText.text, "파산이 클리어보다 우선한다");
            AssertColor(saveStatusText, 0xA9, 0x3E, 0x58); // Warning Plum
        }

        // ── 분기 4: 손상 세이브 ──────────────────────────────────────────────

        [Test]
        public void RefreshSaveUi_Shows_Corrupt_Branch_And_Locks_Continue_Without_Silent_NewGame()
        {
            var (gmGo, controller, continueButton, saveStatusText) = OpenMainMenu();
            var gm = gmGo.GetComponent<GameManager>();
            gm.StartNewGame();
            File.WriteAllText(GameManager.SaveFilePathOverride, "{not valid json");

            var stateBefore = gm.State;
            TestSceneSupport.ForceStart(controller);

            Assert.IsFalse(continueButton.interactable, "손상 세이브는 이어하기를 잠가야 한다");
            StringAssert.Contains("저장 데이터를 불러올 수 없습니다", saveStatusText.text);
            AssertColor(saveStatusText, 0xA9, 0x3E, 0x58); // Warning Plum
            Assert.AreSame(stateBefore, gm.State, "손상 표시는 조용한 새 게임 진행 없이 현재 state 를 그대로 유지해야 한다");
            Assert.IsTrue(File.Exists(GameManager.SaveFilePathOverride), "손상 파일은 보존된다 (새 런 저장이 대체)");
        }

        [Test]
        public void RefreshSaveUi_Shows_Corrupt_Branch_When_Order_Identity_Tampered()
        {
            // 회귀 방지 — Codex 코드리뷰 P1: TryPeekSave 가 V11(주문 identity)을 생략하면 주문 개수만
            // 맞는 변조 세이브가 "정상"으로 표시되어 이어하기가 활성화되는 버그가 있었다.
            var (gmGo, controller, continueButton, saveStatusText) = OpenMainMenu();
            var gm = gmGo.GetComponent<GameManager>();
            gm.StartNewGame();
            var genreIds = gm.GenreCatalog.Select(g => g.Id).ToList();
            var selection = ClientIsKing.Genre.GenreSelectionOps.TrySelect(gm.State, "generalist", genreIds);
            Assert.IsTrue(selection.Success, selection.Message);
            Assert.AreEqual(ClientIsKing.DayCycle.DayPhase.Service, gm.AdvancePhase());
            Assert.Greater(gm.State.serviceOrders.Count, 0, "픽스처 전제: 최소 1건의 주문이 있어야 한다");
            Assert.IsTrue(gm.SaveGame(out var saveReason), saveReason);

            var service = ClientIsKing.Service.ServiceManager.Instance;
            string otherRecipeId = null;
            foreach (var recipe in service.RecipeDefs)
            {
                if (recipe != null && recipe.Id != gm.State.serviceOrders[0].recipeId)
                {
                    otherRecipeId = recipe.Id;
                    break;
                }
            }
            Assert.IsNotNull(otherRecipeId, "픽스처 전제: 최소 2종의 레시피가 있어야 한다");

            string json = File.ReadAllText(GameManager.SaveFilePathOverride);
            string tampered = json.Replace(
                $"\"recipeId\": \"{gm.State.serviceOrders[0].recipeId}\"",
                $"\"recipeId\": \"{otherRecipeId}\"");
            Assert.AreNotEqual(json, tampered, "치환이 실제로 발생해야 테스트가 유효하다");
            File.WriteAllText(GameManager.SaveFilePathOverride, tampered);

            var stateBefore = gm.State;
            TestSceneSupport.ForceStart(controller);

            Assert.IsFalse(continueButton.interactable, "V11 변조 세이브는 이어하기를 잠가야 한다");
            StringAssert.Contains("저장 데이터를 불러올 수 없습니다", saveStatusText.text);
            AssertColor(saveStatusText, 0xA9, 0x3E, 0x58); // Warning Plum
            Assert.AreSame(stateBefore, gm.State, "손상 표시는 조용한 새 게임 진행 없이 현재 state 를 그대로 유지해야 한다");
        }

        // ── 클릭 흐름 ────────────────────────────────────────────────────────

        [Test]
        public void OnContinueClicked_Failure_Refreshes_Ui_Instead_Of_Silent_Fallback()
        {
            var (gmGo, controller, continueButton, saveStatusText) = OpenMainMenu();
            var gm = gmGo.GetComponent<GameManager>();
            gm.StartNewGame();
            gm.State.cash = 5000;
            Assert.IsTrue(gm.SaveGame(out _));
            TestSceneSupport.ForceStart(controller);
            Assert.IsTrue(continueButton.interactable);

            // 클릭 사이 파일이 손상된 상황을 모사한다.
            File.WriteAllText(GameManager.SaveFilePathOverride, "{not valid json");
            continueButton.onClick.Invoke();

            Assert.IsFalse(continueButton.interactable, "실패 시 이어하기는 다시 잠겨야 한다");
            StringAssert.Contains("저장 데이터를 불러올 수 없습니다", saveStatusText.text);
        }

        static void AssertColor(TMPro.TMP_Text text, byte r, byte g, byte b)
        {
            var expected = new Color32(r, g, b, 0xFF);
            Assert.AreEqual((Color)expected, text.color, $"색상 불일치 — 기대 #{r:X2}{g:X2}{b:X2}, 실제 {text.color}");
        }
    }
}
