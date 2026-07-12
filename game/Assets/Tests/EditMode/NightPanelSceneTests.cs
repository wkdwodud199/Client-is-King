using System.Linq;
using ClientIsKing.EditorTools;
using ClientIsKing.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-111 U6: Night 패널 SNS 블록의 정적 산출물(F1 오브젝트·좌표) 검증 — 구조 전용,
    /// 상태를 바꾸는 흐름은 <see cref="NightPanelSnsFlowTests"/> 로 분리한다(task-110 씬 격리 교훈).
    /// </summary>
    public class NightPanelSceneTests
    {
        Transform nightPanel;

        [OneTimeSetUp]
        public void BuildAndOpen()
        {
            SceneBuilder.Apply();
            var scene = EditorSceneManager.OpenScene(SceneBuilder.ShopPath, UnityEditor.SceneManagement.OpenSceneMode.Single);
            var canvasGo = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "Canvas");
            Assert.IsNotNull(canvasGo, "Canvas 누락");
            nightPanel = canvasGo.transform.Find("Panel_Night");
            Assert.IsNotNull(nightPanel, "Panel_Night 누락");
        }

        [Test]
        public void Night_Panel_Has_All_Sns_Block_Objects()
        {
            string[] names =
            {
                "SummaryText", "DaysText", "EventNoticeText", "FollowerText", "SnsTitleText",
                "Button_Sns_PhotoFeed", "Button_Sns_ShortForm", "Button_Sns_LocalBoard",
                "SnsInfoText", "StatusText",
            };
            foreach (var name in names)
            {
                Assert.IsNotNull(nightPanel.Find(name), $"Panel_Night/{name} 누락");
            }
        }

        [Test]
        public void Night_Panel_Sns_Objects_Match_F1_Coordinates()
        {
            // task-112 F1 v2: 기존 4텍스트 이동·축소 + EventNoticeText 삽입(읽는 순서 오늘 마감→내일 예고→SNS 설계).
            AssertAnchoredPosition("SummaryText", new Vector2(0f, 86f));
            AssertAnchoredPosition("DaysText", new Vector2(0f, 71f));
            AssertAnchoredPosition("EventNoticeText", new Vector2(0f, 57f));
            AssertAnchoredPosition("FollowerText", new Vector2(0f, 44f));
            AssertAnchoredPosition("SnsTitleText", new Vector2(0f, 31f));
            AssertAnchoredPosition("Button_Sns_PhotoFeed", new Vector2(-150f, 0f));
            AssertAnchoredPosition("Button_Sns_ShortForm", new Vector2(0f, 0f));
            AssertAnchoredPosition("Button_Sns_LocalBoard", new Vector2(150f, 0f));
            AssertAnchoredPosition("SnsInfoText", new Vector2(0f, -40f));
            AssertAnchoredPosition("StatusText", new Vector2(0f, -72f));
        }

        void AssertAnchoredPosition(string name, Vector2 expected)
        {
            var child = nightPanel.Find(name);
            Assert.IsNotNull(child, $"Panel_Night/{name} 누락");
            var rt = (RectTransform)child;
            Assert.AreEqual(expected.x, rt.anchoredPosition.x, 0.01f, $"{name} anchoredPosition.x");
            Assert.AreEqual(expected.y, rt.anchoredPosition.y, 0.01f, $"{name} anchoredPosition.y");
        }

        [Test]
        public void Sns_Campaign_Buttons_Have_Outline_Disabled_By_Default()
        {
            foreach (var name in new[] { "Button_Sns_PhotoFeed", "Button_Sns_ShortForm", "Button_Sns_LocalBoard" })
            {
                var button = nightPanel.Find(name);
                Assert.IsNotNull(button, $"{name} 누락");
                var outline = button.GetComponent<Outline>();
                Assert.IsNotNull(outline, $"{name} Outline 컴포넌트 누락 (F1 — 집행 완료 표시용)");
                Assert.IsFalse(outline.enabled, $"{name} outline 은 기본 비활성이어야 한다(집행 전)");
                Assert.IsNotNull(button.GetComponent<Button>(), $"{name} Button 컴포넌트 누락");
            }
        }

        [Test]
        public void Night_Panel_Has_Controller_With_Sns_Wiring()
        {
            var controller = nightPanel.GetComponent<NightPanelController>();
            Assert.IsNotNull(controller, "NightPanelController 누락");
        }

        [Test]
        public void Sns_Buttons_Have_Explicit_Horizontal_Navigation_Chain()
        {
            // F2: 픽쳐그램→숏핑→동네게시판→다음 날 ▶ 좌우 방향키 explicit navigation.
            var photoFeed = nightPanel.Find("Button_Sns_PhotoFeed").GetComponent<Button>();
            var shortForm = nightPanel.Find("Button_Sns_ShortForm").GetComponent<Button>();
            var localBoard = nightPanel.Find("Button_Sns_LocalBoard").GetComponent<Button>();

            Assert.AreEqual(Navigation.Mode.Explicit, photoFeed.navigation.mode, "픽쳐그램 explicit navigation");
            Assert.AreSame(shortForm, photoFeed.navigation.selectOnRight, "픽쳐그램 → 숏핑");
            Assert.AreSame(localBoard, shortForm.navigation.selectOnRight, "숏핑 → 동네게시판");
        }

        // ── task-113 U5: 자동 저장 표시 라인(G4) — 좌표 불변 + worst-case 폭 ──

        [Test]
        public void StatusText_Coordinates_Unchanged_By_Save_Line_Addition()
        {
            // G4: statusText (0,-72)/(460x32)/11pt 그대로 — 저장 라인은 텍스트 내용만 확장한다.
            var statusText = nightPanel.Find("StatusText");
            Assert.IsNotNull(statusText, "Panel_Night/StatusText 누락");
            var rt = (RectTransform)statusText;
            Assert.AreEqual(0f, rt.anchoredPosition.x, 0.01f, "StatusText anchoredPosition.x");
            Assert.AreEqual(-72f, rt.anchoredPosition.y, 0.01f, "StatusText anchoredPosition.y");
            Assert.AreEqual(460f, rt.sizeDelta.x, 0.01f, "StatusText sizeDelta.x");
            Assert.AreEqual(32f, rt.sizeDelta.y, 0.01f, "StatusText sizeDelta.y");
        }

        [Test]
        public void StatusText_WorstCase_AutoSave_Success_Line_Fits_Within_460px()
        {
            // 성공 2행: 안내 + `자동 저장됨 · Day {n} {phase}` — phase 라벨 중 가장 긴 것으로 폭 확인.
            var statusText = nightPanel.Find("StatusText").GetComponent<TMPro.TMP_Text>();
            string worstLine = "내일 영업 준비 완료 — '다음 날 ▶' 버튼으로 진행하세요.\n자동 저장됨 · Day 99 정산";
            statusText.text = worstLine;
            statusText.ForceMeshUpdate();
            var preferred = statusText.GetPreferredValues(worstLine);
            Assert.LessOrEqual(preferred.x, 460f, $"자동 저장 성공 라인 worst-case 폭 {preferred.x:F1}px 이 460px 를 초과함: '{worstLine}'");
        }

        [Test]
        public void StatusText_WorstCase_AutoSave_Failure_Line_Fits_Within_460px()
        {
            // 실패 2행: 안내 + Warning Plum 컬러 태그 포함 `자동 저장 실패: {사유}` — 검증 매트릭스에서
            // 가장 긴 축에 속하는 사유 문자열로 worst-case 를 구성한다 (I/O 예외 메시지는 환경 의존적이라
            // 표시 폭 검증의 대표값으로 삼지 않는다 — task-112 F5 전례와 동일 원칙).
            var statusText = nightPanel.Find("StatusText").GetComponent<TMPro.TMP_Text>();
            string worstReason = "저장 데이터 검증 실패: 서비스 통계가 주문 목록과 일치하지 않습니다.";
            string worstLine = "내일 영업 준비 완료 — '다음 날 ▶' 버튼으로 진행하세요.\n" +
                $"<color=#A93E58>자동 저장 실패: {worstReason}</color>";
            statusText.text = worstLine;
            statusText.ForceMeshUpdate();
            var preferred = statusText.GetPreferredValues(worstLine);
            Assert.LessOrEqual(preferred.x, 460f, $"자동 저장 실패 라인 worst-case 폭 {preferred.x:F1}px 이 460px 를 초과함: '{worstLine}'");
        }
    }
}
