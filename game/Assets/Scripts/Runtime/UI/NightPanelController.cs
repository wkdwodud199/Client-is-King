using System.Collections.Generic;
using ClientIsKing.Data;
using ClientIsKing.DayCycle;
using ClientIsKing.Events;
using ClientIsKing.Managers;
using ClientIsKing.Service;
using ClientIsKing.Settlement;
using ClientIsKing.Social;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ClientIsKing.UI
{
    /// <summary>
    /// Night phase 하루 마감 UI — 정산 후 잔액/완료 일수/다음 날 진행 가능 여부(또는 파산)를 표시.
    /// task-111 (U4): SNS 캠페인 블록(design.md F1/F2) — 팔로워 표시, 채널 버튼 3종(버튼 1회 클릭 = 집행 확정),
    /// 미리보기 라벨(+N팀·감쇠), 결과/경고 라인, focus 순서(픽쳐그램→숏핑→동네게시판→다음 날 ▶).
    /// 도달·감쇠·타겟은 재계산하지 않는다 — TryGetSnsPreview DTO 표시와 ID→표시명 매핑만 수행한다 (G2/E1).
    /// </summary>
    public sealed class NightPanelController : MonoBehaviour
    {
        // F2 예고 라인 색 (task-110 팔레트 hex 고정) — 상태는 [예고]/[지속] 문구가 전달하고 색은 보조다.
        static readonly Color32 WarningPlum = new Color32(0xA9, 0x3E, 0x58, 0xFF);
        static readonly Color32 BrassAmber = new Color32(0xE5, 0xA8, 0x4B, 0xFF);
        static readonly Color32 SteamCream = new Color32(0xF4, 0xE5, 0xC2, 0xFF);

        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private TMP_Text daysText;
        [SerializeField] private TMP_Text statusText;

        // ── 이벤트 예고 라인 (task-112 F1/F2 — SceneBuilder 가 생성·주입) ───
        [SerializeField] private TMP_Text eventNoticeText;

        // ── SNS 블록 (task-111 F1 — SceneBuilder 가 생성·주입) ──────────────
        [SerializeField] private TMP_Text followerText;
        [SerializeField] private TMP_Text snsTitleText;
        [SerializeField] private Button photoFeedButton;
        [SerializeField] private Button shortFormButton;
        [SerializeField] private Button localBoardButton;
        [SerializeField] private TMP_Text snsInfoText;
        [SerializeField] private Button advanceButton; // focus 체인 마지막 (다음 날 ▶, HUD 소유)

        private void OnEnable()
        {
            if (photoFeedButton != null) photoFeedButton.onClick.AddListener(OnExecutePhotoFeed);
            if (shortFormButton != null) shortFormButton.onClick.AddListener(OnExecuteShortForm);
            if (localBoardButton != null) localBoardButton.onClick.AddListener(OnExecuteLocalBoard);
            GameEvents.SNSCampaignExecuted += OnSnsCampaignExecuted;
            Render();
            FocusFirst();
        }

        private void OnDisable()
        {
            if (photoFeedButton != null) photoFeedButton.onClick.RemoveListener(OnExecutePhotoFeed);
            if (shortFormButton != null) shortFormButton.onClick.RemoveListener(OnExecuteShortForm);
            if (localBoardButton != null) localBoardButton.onClick.RemoveListener(OnExecuteLocalBoard);
            GameEvents.SNSCampaignExecuted -= OnSnsCampaignExecuted;
        }

        private void Update()
        {
            // F2: 좌우 방향키는 Selectable Navigation(SceneBuilder 배선), Tab 은 여기서 순환 처리
            // (Market 장르 modal 과 동일 패턴).
            if (!Input.GetKeyDown(KeyCode.Tab))
            {
                return;
            }
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return;
            }
            var chain = BuildFocusChain();
            if (chain.Count == 0)
            {
                return;
            }
            int current = -1;
            for (int i = 0; i < chain.Count; i++)
            {
                if (eventSystem.currentSelectedGameObject == chain[i].gameObject)
                {
                    current = i;
                    break;
                }
            }
            eventSystem.SetSelectedGameObject(chain[(current + 1) % chain.Count].gameObject);
        }

        private void OnExecutePhotoFeed() { OnExecute(SNSCampaignCopy.PhotoFeedId); }
        private void OnExecuteShortForm() { OnExecute(SNSCampaignCopy.ShortFormId); }
        private void OnExecuteLocalBoard() { OnExecute(SNSCampaignCopy.LocalBoardId); }

        /// <summary>버튼 1회 클릭 = 집행 확정 (F2 — 확정 전 2행 미리보기가 사전 고지 역할).</summary>
        private void OnExecute(string campaignId)
        {
            var service = ServiceManager.Instance;
            if (service == null)
            {
                return;
            }
            if (service.TryExecuteSnsCampaign(campaignId, out var result))
            {
                // 집행 확정 시 정확히 1회 발행 (E3 — 도메인 Ops 는 발행하지 않는다). 수신 핸들러가 Render 한다.
                GameEvents.RaiseSNSCampaignExecuted(campaignId);
            }
            else
            {
                // 상태 불변 실패 — 버튼 비활성이 1차 방어이며, 사유는 결과 라인에 그대로 표시한다.
                Render();
                if (snsInfoText != null && result != null)
                {
                    snsInfoText.text = result.Message;
                }
            }
        }

        private void OnSnsCampaignExecuted(string campaignId)
        {
            Render();
        }

        private void Render()
        {
            var gm = GameManager.Instance;
            var state = gm != null ? gm.State : null;
            if (state == null)
            {
                return;
            }

            if (summaryText != null)
            {
                summaryText.text = $"Day {state.day} 마감 — 잔액 {state.cash:N0}원";
            }
            if (daysText != null)
            {
                daysText.text = $"완료 일수 {state.daysCompleted}일";
            }
            if (statusText != null)
            {
                statusText.text = state.isBankrupt
                    ? $"파산 — 게임 오버 (버틴 일수 {state.daysCompleted}일)\n{state.bankruptcyReason}"
                    : "내일 영업 준비 완료 — '다음 날 ▶' 버튼으로 진행하세요.";
            }

            // ── SNS 블록 (미배선 fixture 는 no-op) ──────────────────────────
            if (followerText != null)
            {
                // follower 는 표시값(120 + Σ followerGain) — 순수 helper 산출, 규칙 입력 아님 (E1).
                followerText.text = $"팔로워 {SNSCampaignOps.CalculateFollowerDisplay(state.snsCampaignHistory):N0}명";
            }
            if (snsTitleText != null)
            {
                snsTitleText.text = "SNS 캠페인 — 내일의 손님을 설계하세요";
            }

            // task-112 F2: 내일 예고 — forecast DTO 표시 전용 (예고==적용, UI 재계산 금지).
            // 실패 시 forecast 는 null (EventOps 보장) — 오류 라인 표시 + 경고는 기본 운영비로 폴백.
            gm.TryBuildEventForecast(out var forecast, out var forecastFailReason);
            RenderEventNotice(forecast, forecastFailReason);

            var tonight = FindTonightRecord(state);
            RenderCampaignButton(photoFeedButton, SNSCampaignCopy.PhotoFeedId, tonight);
            RenderCampaignButton(shortFormButton, SNSCampaignCopy.ShortFormId, tonight);
            RenderCampaignButton(localBoardButton, SNSCampaignCopy.LocalBoardId, tonight);
            RenderInfoLine(state, tonight, forecast);
        }

        /// <summary>
        /// F2 예고 라인 — 신규(위기 Plum/기회 Amber) → 지속(Plum) → 없음(Cream) → 오류(Plum) 순 분기.
        /// 문자열은 forecast DTO 완성본을 그대로 표시한다 (EventOps 단일 원천).
        /// </summary>
        private void RenderEventNotice(EventForecast forecast, string failReason)
        {
            if (eventNoticeText == null)
            {
                return;
            }
            if (forecast == null)
            {
                eventNoticeText.text = $"이벤트 상태 오류: {failReason}";
                eventNoticeText.color = WarningPlum;
                return;
            }
            if (!string.IsNullOrEmpty(forecast.UpcomingNoticeLine))
            {
                eventNoticeText.text = forecast.UpcomingNoticeLine;
                eventNoticeText.color = IsOpportunityEvent(forecast.UpcomingEventId) ? BrassAmber : WarningPlum;
                return;
            }
            if (!string.IsNullOrEmpty(forecast.ContinuingNoticeLine))
            {
                eventNoticeText.text = forecast.ContinuingNoticeLine;
                eventNoticeText.color = WarningPlum;
                return;
            }
            eventNoticeText.text = "내일 예고된 이벤트 없음";
            eventNoticeText.color = SteamCream;
        }

        /// <summary>단체 손님만 기회(Brass Amber) — 나머지 3종은 위기(Warning Plum) (F2 표).</summary>
        private static bool IsOpportunityEvent(string eventId)
        {
            var gm = GameManager.Instance;
            return gm != null && gm.TryGetEventDef(eventId, out var def)
                && def.Kind == GameEventKind.GroupCustomers;
        }

        /// <summary>
        /// 채널 버튼 라벨 2행 + interactable + 집행 완료 outline (F2).
        /// {n}은 TryGetSnsPreview 의 감쇠 반영 실시간 값이며, interactable 은 preview.CanExecute
        /// (파산/오늘 이미 집행/자금 부족 — 게이트와 동일 규칙)를 그대로 쓴다. UI 재계산 없음.
        /// </summary>
        private void RenderCampaignButton(Button button, string campaignId, SNSCampaignRecord tonight)
        {
            if (button == null)
            {
                return;
            }
            bool executed = tonight != null
                && string.Equals(tonight.campaignId, campaignId, System.StringComparison.Ordinal);
            var service = ServiceManager.Instance;
            if (service == null || !service.TryGetSnsPreview(campaignId, out var preview, out _))
            {
                // catalog 미주입/정의 손상 — 집행 불가로 잠근다 (명시적 차단은 plan 경로 소관).
                button.interactable = false;
                SetExecutedOutline(button, executed);
                return;
            }

            string line2;
            if (executed)
            {
                line2 = "집행 완료"; // 라벨 2행 교체 + outline — 색만으로 상태 전달 금지 (F1)
            }
            else if (preview.BonusOrderCount == 0)
            {
                line2 = "피로 누적 · 내일 +0팀"; // 수확체감 사전 경고 (F2)
            }
            else
            {
                line2 = $"{TargetLabel(preview.TopTargetCustomerIds)} · 내일 +{preview.BonusOrderCount}팀";
            }

            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = $"{CampaignDisplayName(campaignId)} {preview.Cost:N0}원\n{line2}";
            }
            button.interactable = preview.CanExecute;
            SetExecutedOutline(button, executed);
        }

        /// <summary>
        /// 기본 안내 / 집행 완료 결과 + 운영비 부족 경고 (색+문구 병용, F2).
        /// task-112: 경고 기준을 내일의 실제 운영비(forecast.NextDayOperatingCost — 임대료 인상·위생
        /// 반영)로 교체한다. forecast 실패(손상 데이터) 시에만 기본 운영비 상수로 폴백한다.
        /// </summary>
        private void RenderInfoLine(GameState state, SNSCampaignRecord tonight, EventForecast forecast)
        {
            if (snsInfoText == null)
            {
                return;
            }
            if (tonight == null)
            {
                snsInfoText.text = $"밤마다 한 채널만 집행할 수 있습니다 · 잔액 {state.cash:N0}원";
                return;
            }
            string text = $"{CampaignDisplayName(tonight.campaignId)} 집행 완료 — " +
                $"내일 SNS 유입 +{tonight.bonusOrderCount}팀 · 팔로워 +{tonight.followerGain} · 잔액 {state.cash:N0}원";
            int nextDayOperatingCost = forecast != null
                ? forecast.NextDayOperatingCost
                : SettlementOps.DailyOperatingCost;
            if (state.cash < nextDayOperatingCost)
            {
                text += $"\n<color={SNSCampaignCopy.WarningPlumHex}>내일 운영비 {nextDayOperatingCost:N0}원이 부족할 수 있습니다</color>";
            }
            snsInfoText.text = text;
        }

        /// <summary>오늘 밤(state.day) 집행 레코드 — 1밤 1회 규칙이므로 최대 1건.</summary>
        private static SNSCampaignRecord FindTonightRecord(GameState state)
        {
            foreach (var record in state.snsCampaignHistory)
            {
                if (record != null && record.executedOnDay == state.day)
                {
                    return record;
                }
            }
            return null;
        }

        /// <summary>catalog 의 표시명 — 해석 실패 시 campaignId 그대로 (표시 전용 fallback).</summary>
        private static string CampaignDisplayName(string campaignId)
        {
            var service = ServiceManager.Instance;
            if (service != null)
            {
                foreach (var def in service.SnsCampaignDefs)
                {
                    if (def != null && string.Equals(def.Id, campaignId, System.StringComparison.Ordinal))
                    {
                        return def.DisplayName;
                    }
                }
            }
            return campaignId ?? "";
        }

        /// <summary>
        /// preview.TopTargetCustomerIds 순서 그대로 F2 축약 라벨을 이어 붙인다 —
        /// UI 는 ID→표시명 매핑만 수행하며 친화 배수·가중치를 재계산하지 않는다 (E1/F2).
        /// </summary>
        private static string TargetLabel(IReadOnlyList<string> topTargetCustomerIds)
        {
            var parts = new List<string>();
            foreach (var id in topTargetCustomerIds)
            {
                parts.Add(SNSCampaignCopy.TargetShortLabel(id));
            }
            return string.Join("·", parts);
        }

        /// <summary>집행 완료 버튼만 Gochujang Red outline 2px (F1 — SceneBuilder 가 컴포넌트를 붙인다).</summary>
        private static void SetExecutedOutline(Button button, bool executed)
        {
            var outline = button.GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = executed;
            }
        }

        /// <summary>패널 활성화 시 focus 순서 첫 항목에 focus (F2 — Market 장르 modal 과 동일 규약).</summary>
        private void FocusFirst()
        {
            if (!Application.isPlaying || EventSystem.current == null)
            {
                return;
            }
            var chain = BuildFocusChain();
            if (chain.Count > 0)
            {
                EventSystem.current.SetSelectedGameObject(chain[0].gameObject);
            }
        }

        /// <summary>focus 순환 대상 — 픽쳐그램→숏핑→동네게시판→다음 날 ▶ 중 활성·interactable 만 (F2).</summary>
        private List<Selectable> BuildFocusChain()
        {
            var chain = new List<Selectable>();
            foreach (var button in new[] { photoFeedButton, shortFormButton, localBoardButton, advanceButton })
            {
                if (button != null && button.gameObject.activeInHierarchy && button.interactable)
                {
                    chain.Add(button);
                }
            }
            return chain;
        }

#if UNITY_EDITOR
        /// <summary>SceneBuilder 전용 참조 주입 — 기존 시그니처 (SNS 블록 미배선 유지).</summary>
        internal void EditorInit(TMP_Text summaryText, TMP_Text daysText, TMP_Text statusText)
        {
            EditorInit(summaryText, daysText, statusText, null, null, null, null, null, null, null, null);
        }

        /// <summary>SceneBuilder 전용 참조 주입 — SNS 블록 포함, 기존 시그니처 보존 (이벤트 예고 미배선).</summary>
        internal void EditorInit(
            TMP_Text summaryText, TMP_Text daysText, TMP_Text statusText,
            TMP_Text followerText, TMP_Text snsTitleText,
            Button photoFeedButton, Button shortFormButton, Button localBoardButton,
            TMP_Text snsInfoText, Button advanceButton)
        {
            EditorInit(summaryText, daysText, statusText, followerText, snsTitleText,
                photoFeedButton, shortFormButton, localBoardButton, snsInfoText, advanceButton, null);
        }

        /// <summary>SceneBuilder 전용 참조 주입 — 이벤트 예고 라인 포함 (task-112 U6 채택 대상).</summary>
        internal void EditorInit(
            TMP_Text summaryText, TMP_Text daysText, TMP_Text statusText,
            TMP_Text followerText, TMP_Text snsTitleText,
            Button photoFeedButton, Button shortFormButton, Button localBoardButton,
            TMP_Text snsInfoText, Button advanceButton, TMP_Text eventNoticeText)
        {
            this.summaryText = summaryText;
            this.daysText = daysText;
            this.statusText = statusText;
            this.followerText = followerText;
            this.snsTitleText = snsTitleText;
            this.photoFeedButton = photoFeedButton;
            this.shortFormButton = shortFormButton;
            this.localBoardButton = localBoardButton;
            this.snsInfoText = snsInfoText;
            this.advanceButton = advanceButton;
            this.eventNoticeText = eventNoticeText;
        }
#endif
    }

    /// <summary>
    /// design.md F2 표의 SNS 채널 고정 카피 (Codex 소유 UX copy — 임의 수정 금지).
    /// 타겟 축약 라벨은 F2 버튼 2행의 고정 표기이며, 순서는 preview DTO 가 결정한다 (재계산 금지).
    /// </summary>
    internal static class SNSCampaignCopy
    {
        public const string PhotoFeedId = "photo_feed";
        public const string ShortFormId = "short_form";
        public const string LocalBoardId = "local_board";

        /// <summary>운영비 부족 경고 색 — Warning Plum (task-110 팔레트, 색+문구 병용).</summary>
        public const string WarningPlumHex = "#A93E58";

        /// <summary>customer ID → F2 축약 표시명. 미지 id 는 catalog 표시명, 그것도 없으면 id 그대로.</summary>
        public static string TargetShortLabel(string customerId)
        {
            switch (customerId)
            {
                case "student": return "학생";
                case "office_worker": return "직장인";
                case "family_parent": return "가족";
                case "senior_regular": return "어르신";
            }
            var service = ServiceManager.Instance;
            if (service != null)
            {
                foreach (var def in service.CustomerDefs)
                {
                    if (def != null && string.Equals(def.Id, customerId, System.StringComparison.Ordinal))
                    {
                        return def.DisplayName;
                    }
                }
            }
            return customerId ?? "";
        }
    }
}
