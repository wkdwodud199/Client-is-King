using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ClientIsKing.Data;
using ClientIsKing.DayCycle;
using ClientIsKing.Economy;
using ClientIsKing.Genre;
using ClientIsKing.Managers;
using ClientIsKing.Service;
using ClientIsKing.Settlement;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ClientIsKing.Tests.PlayMode
{
    /// <summary>
    /// task-115 F: 클리어 세이브 파일 → TryLoadGame → Shop 씬 로드 → 엔딩 오버레이 활성 + advance
    /// 차단 확인 → LoadMainMenuScene → MainMenu 클리어 분기 표시. 경로 override 는 SaveLoadPlayModeTests
    /// 의 [SetUpFixture] 가 세션 전체를 이미 격리하므로 이 파일은 별도로 걸지 않는다(중복 override
    /// 방지 — SetUpFixture 는 어셈블리당 동작 범위가 겹치면 안 된다).
    /// 클리어 세이브는 Day 7 실루프(전량 서빙·C급 실요구량 구매)를 실제 도메인 경로로 완주해 준비한다
    /// — 정산 필드·serviceOrders 가 프로덕션 코드로 생성되므로 V3~V11(주문 identity 재검증 포함)이
    /// 자동으로 일관된다(BalanceEndingGuardTests.PlayOneFullDay EditMode 전례의 PlayMode 판).
    /// </summary>
    public class EndingPlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // 다음 PlayMode 테스트가 깨끗한 씬에서 시작하도록 MainMenu 로 되돌린다(기존 8종 전례와 동일).
            if (SceneManager.GetActiveScene().name != "MainMenu")
            {
                yield return SceneManager.LoadSceneAsync("MainMenu");
            }
            yield return null;

            if (GameManager.SaveFilePathOverride != null && System.IO.File.Exists(GameManager.SaveFilePathOverride))
            {
                System.IO.File.Delete(GameManager.SaveFilePathOverride);
            }
        }

        /// <summary>Market phase 오늘 plan 이 실제로 요구하는 재료만 정확히(이벤트 폭등 배수 포함) 구매한다.</summary>
        static IEnumerator PurchaseExactNeeds(GameManager gm, GenreDef genre, List<RecipeDef> recipes, List<CustomerArchetypeDef> customers,
            Dictionary<IngredientKind, IngredientDef> ingredientsC)
        {
            var service = ServiceManager.Instance;
            Assert.IsTrue(service.TryBuildDayPlan(genre, out var plan, out var planReason), planReason);
            var previewOrders = ServiceOps.BuildOrders(plan, customers);

            var totalNeeded = new Dictionary<IngredientKind, int>();
            foreach (var order in previewOrders)
            {
                var recipe = recipes.First(r => r.Id == order.recipeId);
                foreach (var req in ServiceOps.CalculateRequiredIngredients(recipe, order.partySize))
                {
                    totalNeeded.TryGetValue(req.Kind, out var existing);
                    totalNeeded[req.Kind] = existing + req.Quantity;
                }
            }

            var economy = EconomyManager.Instance;
            foreach (var need in totalNeeded)
            {
                var purchase = economy.TryPurchaseIngredient(ingredientsC[need.Key], need.Value);
                Assert.IsTrue(purchase.Success, $"day {gm.State.day} {need.Key} 구매 실패: {purchase.Message}");
                yield return null;
            }
        }

        /// <summary>전량 서빙·C급 실요구량 구매로 하루를 완주(Market→Service→Settlement→Night→다음날 Market)한다.
        /// 정산 결과 클리어/파산으로 런이 종료되면 Settlement 에 머문 채 반환한다(EndingOps.IsRunEnded).</summary>
        static IEnumerator PlayOneFullDay(GameManager gm, GenreDef genre, List<RecipeDef> recipes,
            List<CustomerArchetypeDef> customers, Dictionary<IngredientKind, IngredientDef> ingredientsC)
        {
            var service = ServiceManager.Instance;
            var state = gm.State;

            yield return PurchaseExactNeeds(gm, genre, recipes, customers, ingredientsC);

            Assert.IsTrue(gm.CanAdvancePhase(out var advanceReason), $"day {state.day} Market→Service 차단: {advanceReason}");
            Assert.AreEqual(DayPhase.Service, gm.AdvancePhase());
            yield return null;

            while (service.CurrentOrder != null)
            {
                var order = service.CurrentOrder;
                var recipe = recipes.First(r => r.Id == order.recipeId);
                var serve = service.TryServeCurrentOrder(recipe, IngredientGrade.C);
                Assert.IsTrue(serve.Success, $"day {state.day} 서빙 실패: {serve.Message}");
            }
            yield return null;

            Assert.AreEqual(DayPhase.Settlement, gm.AdvancePhase());
            gm.AdvancePhase(); // Settlement 이탈 게이트가 정산을 선적용 — 클리어/파산이면 Settlement 에 머문다
            yield return null;

            if (!EndingOps.IsRunEnded(state))
            {
                gm.AdvancePhase(); // Night → Market (day+1) — 런이 끝나지 않았을 때만 진행
                yield return null;
            }
        }

        /// <summary>
        /// 도메인 경로로 클리어 세이브를 실제 파일에 준비한다: Shop 씬에서 장르 선택 후 Day 1~7 을
        /// 전량 서빙·실요구량 구매로 완주한다 — 정산 필드·serviceOrders 가 프로덕션 코드 산출물이라
        /// V3~V11 이 자동으로 일관된다. 완주 후 자동 저장(트리거 3)된 파일을 그대로 사용한다.
        /// </summary>
        static IEnumerator PrepareClearedSaveFile()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            var gm = GameManager.Instance;
            gm.StartNewRun();

            yield return SceneManager.LoadSceneAsync("Shop");
            yield return null;
            gm = GameManager.Instance;

            var genreIds = gm.GenreCatalog.Select(g => g.Id).ToList();
            var selection = GenreSelectionOps.TrySelect(gm.State, "gukbap", genreIds);
            Assert.IsTrue(selection.Success, selection.Message);

            var marketController = GameObject.Find("Canvas").transform.Find("Panel_Market")
                .GetComponent<ClientIsKing.UI.MarketPanelController>();
            var recipes = marketController.RecipeDefs.ToList();
            var ingredientsC = marketController.IngredientDefs
                .Where(i => i.Grade == IngredientGrade.C).ToDictionary(i => i.Kind);
            var customers = ServiceManager.Instance.CustomerDefs.ToList();

            gm.TryGetGenre("gukbap", out var genre);

            for (int day = 1; day <= EndingOps.ClearTargetDays; day++)
            {
                Assert.AreEqual(day, gm.State.day, $"day {day} 진입 확인");
                Assert.IsFalse(gm.State.isBankrupt, $"day {day} 진입 전 파산 상태가 아니어야 한다");
                yield return PlayOneFullDay(gm, genre, recipes, customers, ingredientsC);
            }

            Assert.IsTrue(SettlementOps.IsSettlementApplied(gm.State), $"Day {EndingOps.ClearTargetDays} 정산 적용");
            Assert.IsFalse(gm.State.isBankrupt, "클리어 fixture 는 파산 없이 완주해야 한다");
            Assert.AreEqual(RunEndingStatus.Cleared, EndingOps.GetStatus(gm.State), "클리어 도달 실패");
            Assert.IsTrue(gm.HasSaveFile, "Day 7 정산 적용(트리거 3)으로 자동 저장되어 있어야 한다");
        }

        [UnityTest]
        public IEnumerator ClearedSave_Loads_Shows_Overlay_Blocks_Advance_Then_MainMenu_Shows_Clear_Branch()
        {
            // 1) Shop 씬에서 Day1~7 실루프를 완주해 클리어 세이브 파일을 도메인 경로로 준비한다.
            yield return PrepareClearedSaveFile();

            // 2) MainMenu 를 재로드해 이어하기 도메인 경로(TryLoadGame + LoadShopScene)로 진입한다.
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            var gm = GameManager.Instance;

            Assert.IsTrue(gm.HasSaveFile);
            Assert.IsTrue(gm.TryLoadGame(out var loadReason), loadReason);
            Assert.AreEqual(RunEndingStatus.Cleared, EndingOps.GetStatus(gm.State));

            gm.LoadShopScene();
            yield return null;
            yield return null; // Start()/OnEnable 동기화 프레임 여유 (EndingOverlayController Update 폴링 포함)

            var canvasGo = GameObject.Find("Canvas");
            Assert.IsNotNull(canvasGo, "Shop Canvas 누락");
            var endingPanel = canvasGo.transform.Find("Panel_Ending");
            Assert.IsNotNull(endingPanel, "Panel_Ending 누락");
            Assert.IsTrue(endingPanel.gameObject.activeSelf, "클리어 상태 로드 후 엔딩 오버레이가 활성이어야 한다");

            var titleText = endingPanel.Find("EndingTitleText").GetComponent<TMPro.TMP_Text>();
            Assert.AreEqual("데모 클리어!", titleText.text);

            // 3) 진행 차단 확인 — CanAdvancePhase false + 사유 정확 일치, AdvancePhase 는 phase 유지.
            var gmAfterLoad = GameManager.Instance;
            bool canAdvance = gmAfterLoad.CanAdvancePhase(out var reason);
            Assert.IsFalse(canAdvance, "클리어 상태에서는 진행할 수 없어야 한다");
            Assert.AreEqual("데모 클리어 상태에서는 진행할 수 없습니다.", reason);

            var phaseBefore = gmAfterLoad.State.currentPhase;
            var phaseAfter = gmAfterLoad.AdvancePhase();
            Assert.AreEqual(phaseBefore, phaseAfter, "클리어 상태에서 AdvancePhase 는 현재 phase 를 유지해야 한다");

            // 4) 메인 메뉴로 나가 MainMenu 클리어 분기(이어하기 잠금 + Brass Amber 문구)를 확인한다.
            gmAfterLoad.LoadMainMenuScene();
            yield return null;
            yield return null;

            var mainMenuCanvas = GameObject.Find("Canvas");
            Assert.IsNotNull(mainMenuCanvas, "MainMenu Canvas 누락");
            var continueButton = mainMenuCanvas.transform.Find("ContinueButton").GetComponent<UnityEngine.UI.Button>();
            Assert.IsFalse(continueButton.interactable, "클리어 세이브는 이어하기가 잠겨야 한다");

            var saveStatusText = mainMenuCanvas.transform.Find("SaveStatusText").GetComponent<TMPro.TMP_Text>();
            StringAssert.Contains("데모 클리어!", saveStatusText.text);
            StringAssert.Contains($"영업 {EndingOps.ClearTargetDays}일 달성", saveStatusText.text);

            var brassAmber = new Color32(0xE5, 0xA8, 0x4B, 0xFF);
            Assert.AreEqual((Color)brassAmber, saveStatusText.color, "클리어 분기는 Brass Amber 여야 한다");
        }
    }
}
