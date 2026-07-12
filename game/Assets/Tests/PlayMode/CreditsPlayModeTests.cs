using System.Collections;
using ClientIsKing.Managers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ClientIsKing.Tests.PlayMode
{
    /// <summary>
    /// task-117 G절 PlayMode 신규 1건 (기준선 9 → 10) — 크레딧 모달 입력 격리(Codex P0-1):
    /// 열기 → 배경 3버튼(게임 시작/이어하기/크레딧) interactable false + focus == 닫기 버튼
    /// (Navigation None — 방향키 탈출 불가 조건) → 닫기 → 저장값 정확 복원 + focus 크레딧 버튼 복귀.
    /// 세이브 없음 상태가 기본 fixture 라 이어하기는 자연히 disabled — Continue-disabled 정확 복원을
    /// 함께 검증한다(무조건 true 복원이면 fail). 경로 override 는 SaveLoadPlayModeFixture 가 소유한다.
    /// </summary>
    public class CreditsPlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // 다음 PlayMode 테스트가 깨끗한 씬에서 시작하도록 MainMenu 로 되돌린다 (기존 9종 전례와 동일).
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

        [UnityTest]
        public IEnumerator Credits_Open_Locks_Background_Then_Close_Restores_Saved_State_With_Focus_Roundtrip()
        {
            // 세이브 없음 fixture 보장 — 이어하기 disabled (RefreshSaveUi 분기 1).
            if (GameManager.SaveFilePathOverride != null && System.IO.File.Exists(GameManager.SaveFilePathOverride))
            {
                System.IO.File.Delete(GameManager.SaveFilePathOverride);
            }
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            yield return null; // Start()/RefreshSaveUi 동기화 프레임 여유

            var canvasGo = GameObject.Find("Canvas");
            Assert.IsNotNull(canvasGo, "MainMenu Canvas 누락");
            var creditsButton = canvasGo.transform.Find("CreditsButton").GetComponent<Button>();
            var startButton = canvasGo.transform.Find("StartButton").GetComponent<Button>();
            var continueButton = canvasGo.transform.Find("ContinueButton").GetComponent<Button>();
            var panel = canvasGo.transform.Find("Panel_Credits");
            Assert.IsNotNull(panel, "Panel_Credits 누락");
            var closeButton = panel.Find("CreditsCloseButton").GetComponent<Button>();

            Assert.IsFalse(panel.gameObject.activeSelf, "크레딧 패널은 초기 비활성이어야 한다");
            Assert.IsTrue(startButton.interactable, "fixture 전제: 게임 시작 활성");
            Assert.IsFalse(continueButton.interactable, "fixture 전제: 세이브 없음 → 이어하기 disabled");
            Assert.IsTrue(creditsButton.interactable, "fixture 전제: 크레딧 버튼 활성");

            // 열기 — 배경 3버튼 잠금 + focus == 닫기 버튼(Navigation None).
            creditsButton.onClick.Invoke();
            yield return null;

            Assert.IsTrue(panel.gameObject.activeSelf, "열기 후 패널 활성");
            Assert.IsFalse(startButton.interactable, "열림 동안 게임 시작 잠금 (키보드/게임패드 격리)");
            Assert.IsFalse(continueButton.interactable, "열림 동안 이어하기 잠금");
            Assert.IsFalse(creditsButton.interactable, "열림 동안 크레딧 버튼 잠금");
            Assert.AreEqual(Navigation.Mode.None, closeButton.navigation.mode,
                "닫기 버튼은 Navigation None — 방향키로 배경에 도달할 수 없다 (P0-1)");
            Assert.IsNotNull(EventSystem.current, "EventSystem 누락");
            Assert.AreSame(closeButton.gameObject, EventSystem.current.currentSelectedGameObject,
                "열림 시 focus 는 닫기 버튼이어야 한다");

            // 닫기 — 저장값 정확 복원(이어하기는 disabled 유지) + focus 크레딧 버튼 복귀.
            closeButton.onClick.Invoke();
            yield return null;

            Assert.IsFalse(panel.gameObject.activeSelf, "닫기 후 패널 비활성");
            Assert.IsTrue(startButton.interactable, "게임 시작은 저장값(true) 복원");
            Assert.IsFalse(continueButton.interactable,
                "이어하기는 저장값(disabled) 정확 복원 — 무조건 true 복원 금지 (Codex P0-1)");
            Assert.IsTrue(creditsButton.interactable, "크레딧 버튼은 저장값(true) 복원");
            Assert.AreSame(creditsButton.gameObject, EventSystem.current.currentSelectedGameObject,
                "닫은 뒤 focus 는 크레딧 버튼으로 복귀해야 한다");
        }
    }
}
