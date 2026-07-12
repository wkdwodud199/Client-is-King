using ClientIsKing.DayCycle;
using ClientIsKing.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ClientIsKing.UI
{
    /// <summary>
    /// Shop 씬 엔딩 오버레이(클리어/게임오버 공용) — task-115 D2.
    /// 오버레이 GO 가 비활성이어도 동작하도록 Canvas 에 탑재하고(PhaseHudController 전례)
    /// Update() 에서 EndingOps.GetStatus 를 폴링해 오버레이 루트를 토글한다.
    /// GameEvents.SettlementPresented 구독안은 로드 직후·EditMode fixture 에서 발행 부재 시
    /// 표시 누락이 생겨 기각 — 상태 폴링은 발행 순서 문제가 없다(task-113 G4 교훈).
    /// Render 는 EndingOps.BuildSummary DTO 표시 전용이다(UI 재계산 금지).
    /// </summary>
    public sealed class EndingOverlayController : MonoBehaviour
    {
        // D1 색 (task-110 팔레트 hex 고정) — 상태는 문구가 전달하고 색은 보조다 (E5).
        static readonly Color32 BrassAmber = new Color32(0xE5, 0xA8, 0x4B, 0xFF);
        static readonly Color32 WarningPlum = new Color32(0xA9, 0x3E, 0x58, 0xFF);
        static readonly Color32 SteamCream = new Color32(0xF4, 0xE5, 0xC2, 0xFF);

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button mainMenuButton;

        private void OnEnable()
        {
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }
        }

        private void OnDisable()
        {
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            }
        }

        private void Update()
        {
            RefreshNow();
        }

        /// <summary>
        /// 상태 폴링 1회분 (IVT — EditMode 는 폴링 없이 이 메서드로 즉시 반영을 검증한다).
        /// status != None 이고 오버레이가 숨김이면 Render + 표시 + 버튼 focus,
        /// None 인데 표시 중이면 숨긴다(새 런 fixture 대비).
        /// </summary>
        internal void RefreshNow()
        {
            if (overlayRoot == null)
            {
                return; // 미배선 fixture 는 no-op (MainMenuController.RefreshSaveUi 전례)
            }
            var gm = GameManager.Instance;
            if (gm == null || gm.State == null)
            {
                return;
            }

            var status = EndingOps.GetStatus(gm.State);
            if (status == RunEndingStatus.None)
            {
                if (overlayRoot.activeSelf)
                {
                    overlayRoot.SetActive(false);
                }
                return;
            }
            if (!overlayRoot.activeSelf)
            {
                Render(EndingOps.BuildSummary(gm.State));
                overlayRoot.SetActive(true);
                FocusMainMenuButton();
            }
        }

        /// <summary>D1 카피/색 — Codex 소유 UX copy, 임의 수정 금지 (task-115 D1). 표시 전용, 재계산 없음.</summary>
        private void Render(EndingSummary summary)
        {
            bool cleared = summary.Status == RunEndingStatus.Cleared;
            if (titleText != null)
            {
                titleText.text = cleared ? "데모 클리어!" : "게임 오버";
                titleText.color = cleared ? BrassAmber : WarningPlum;
            }
            if (statsText != null)
            {
                // 부호는 SettlementPanel netText 규약(≥0 이면 `+`) — 음수는 N0 이 `-` 를 포함한다.
                string sign = summary.NetProfit >= 0 ? "+" : "";
                statsText.text =
                    $"영업 {summary.DaysCompleted}일 · 최종 잔액 {summary.FinalCash:N0}원\n" +
                    $"총 손익 {sign}{summary.NetProfit:N0}원 · 팔로워 {summary.FollowerDisplay:N0}명";
                statsText.color = SteamCream;
            }
            if (messageText != null)
            {
                messageText.color = SteamCream;
                messageText.text = cleared
                    ? $"목표 {EndingOps.ClearTargetDays}일 영업을 달성했습니다 — 데모는 여기까지! 플레이해 주셔서 감사합니다."
                    // 파산 1행(사유)은 Warning Plum — NightPanel 의 rich text 색 태그 전례.
                    : $"<color={SNSCampaignCopy.WarningPlumHex}>{summary.BankruptcyReason}</color>\n새 게임으로 다시 도전하세요.";
            }
        }

        private void OnMainMenuClicked()
        {
            GameManager.Instance.LoadMainMenuScene();
        }

        /// <summary>표시 시 `메인 메뉴로 ▶` 가 focus 대상 (D1) — MainMenuController.FocusDefault 미러.</summary>
        private void FocusMainMenuButton()
        {
            if (!Application.isPlaying || EventSystem.current == null)
            {
                return;
            }
            if (mainMenuButton != null)
            {
                EventSystem.current.SetSelectedGameObject(mainMenuButton.gameObject);
            }
        }

#if UNITY_EDITOR
        /// <summary>SceneBuilder 전용 참조 주입 (task-102 EditorInit 패턴).</summary>
        internal void EditorInit(GameObject overlayRoot, TMP_Text title, TMP_Text stats, TMP_Text message,
            Button mainMenuButton)
        {
            this.overlayRoot = overlayRoot;
            this.titleText = title;
            this.statsText = stats;
            this.messageText = message;
            this.mainMenuButton = mainMenuButton;
        }
#endif
    }
}
