using ClientIsKing.DayCycle;
using ClientIsKing.Managers;
using ClientIsKing.Service;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClientIsKing.UI
{
    /// <summary>
    /// Shop 씬 HUD — 현재 day/phase 표시, 다음 phase 버튼, phase 별 placeholder 패널 토글.
    /// 실제 phase 내용(장보기/서빙/정산 UI)은 task-105~107 이 패널을 교체하며 채운다.
    /// task-110: 선택 전문 분야 badge 표시와 CanAdvancePhase 게이트를 추가한다.
    /// </summary>
    public sealed class PhaseHudController : MonoBehaviour
    {
        [SerializeField] private TMP_Text dayPhaseText;
        [SerializeField] private Button advanceButton;
        [SerializeField] private GameObject marketPanel;
        [SerializeField] private GameObject servicePanel;
        [SerializeField] private GameObject settlementPanel;
        [SerializeField] private GameObject nightPanel;
        [SerializeField] private TMP_Text genreBadge;

        private TMP_Text advanceLabel;

        private void OnEnable()
        {
            GameEvents.DayPhaseChanged += OnDayPhaseChanged;
            GameEvents.GenreSelected += OnGenreSelected;
            GameEvents.SNSCampaignExecuted += OnSnsCampaignExecuted;
            advanceButton.onClick.AddListener(OnAdvanceClicked);
            if (advanceLabel == null && advanceButton != null)
            {
                // 라벨 TMP 는 런타임에 탐색 (설계 16단계 허용 — EditorInit 시그니처 유지)
                advanceLabel = advanceButton.GetComponentInChildren<TMP_Text>();
            }
        }

        private void OnDisable()
        {
            GameEvents.DayPhaseChanged -= OnDayPhaseChanged;
            GameEvents.GenreSelected -= OnGenreSelected;
            GameEvents.SNSCampaignExecuted -= OnSnsCampaignExecuted;
            if (advanceButton != null)
            {
                advanceButton.onClick.RemoveListener(OnAdvanceClicked);
            }
        }

        private void Start()
        {
            // GameManager.Awake 이후 시점(Start)에서 초기 표시를 동기화한다.
            var gm = GameManager.Instance;
            if (gm != null && gm.State != null)
            {
                Refresh(gm.State.day, gm.State.currentPhase);
            }
            RefreshGenreBadge();
        }

        private void OnAdvanceClicked()
        {
            GameManager.Instance.AdvancePhase();
        }

        private void OnGenreSelected(string genreId)
        {
            RefreshGenreBadge();
        }

        /// <summary>SNS 집행 확정 수신 — badge 갱신 대비 (task-111 E3 구독 계약).</summary>
        private void OnSnsCampaignExecuted(string campaignId)
        {
            RefreshGenreBadge();
        }

        private void Update()
        {
            // task-110: 다른 controller 가 끈 버튼을 무조건 다시 켜던 비교식을 제거하고,
            // !bankrupt && CanAdvancePhase 단일 식으로만 interactable 을 계산한다 (design.md H10/G3).
            if (advanceButton == null)
            {
                return;
            }
            var gm = GameManager.Instance;
            bool interactable = gm != null && gm.State != null
                && !gm.State.isBankrupt && gm.CanAdvancePhase(out _);
            if (advanceButton.interactable != interactable)
            {
                advanceButton.interactable = interactable;
            }
        }

        private void OnDayPhaseChanged(DayPhaseChangedEventArgs args)
        {
            Refresh(args.Day, args.CurrentPhase);
            // task-111 F5: day/phase 전환 시 badge 주문 수 재계산 — Night→Market 전환 후
            // 낡은 주문 수가 남던 결함 보정 (Start/GenreSelected 만으로는 갱신 누락).
            RefreshGenreBadge();
        }

        private void Refresh(int day, DayPhase phase)
        {
            if (dayPhaseText != null)
            {
                dayPhaseText.text = $"Day {day} — {PhaseLabel(phase)}";
            }
            if (advanceLabel != null)
            {
                advanceLabel.text = AdvanceLabel(phase);
            }
            if (marketPanel != null) marketPanel.SetActive(phase == DayPhase.Market);
            if (servicePanel != null) servicePanel.SetActive(phase == DayPhase.Service);
            if (settlementPanel != null) settlementPanel.SetActive(phase == DayPhase.Settlement);
            if (nightPanel != null) nightPanel.SetActive(phase == DayPhase.Night);
        }

        /// <summary>선택된 전문 분야 + 주문 수를 HUD badge 에 표시한다 (미선택은 빈 문구 — design.md E3/H14).</summary>
        private void RefreshGenreBadge()
        {
            if (genreBadge == null)
            {
                return;
            }
            var gm = GameManager.Instance;
            string genreId = gm != null && gm.State != null ? gm.State.selectedGenreId : "";
            if (string.IsNullOrEmpty(genreId))
            {
                genreBadge.text = "";
                return;
            }
            if (!gm.TryGetGenre(genreId, out var def))
            {
                genreBadge.text = genreId;
                return;
            }
            // 주문 수는 plan 경로에서 읽는다 — UI 가 SO 배수를 직접 계산하지 않는다 (G2).
            // task-111 F5: SNS 보너스가 있는 날은 `{base}+{bonus}건(SNS)` 로 인과를 표시한다.
            var service = ServiceManager.Instance;
            if (service != null && service.TryBuildDayPlan(def, out var plan, out _))
            {
                genreBadge.text = plan.BonusOrderCount > 0
                    ? $"{def.DisplayName} · 주문 {plan.BaseOrderCount}+{plan.BonusOrderCount}건(SNS)"
                    : $"{def.DisplayName} · 주문 {plan.OrderCount}건";
            }
            else
            {
                genreBadge.text = def.DisplayName;
            }
        }

        /// <summary>phase 별 진행 버튼 라벨 (task-107 설계 11단계).</summary>
        public static string AdvanceLabel(DayPhase phase)
        {
            switch (phase)
            {
                case DayPhase.Market: return "영업 시작 ▶";
                case DayPhase.Service: return "정산 ▶";
                case DayPhase.Settlement: return "밤으로 ▶";
                case DayPhase.Night: return "다음 날 ▶";
                default: return "다음 단계 ▶";
            }
        }

        /// <summary>한국어 표시명 (데이터가 아니라 HUD 전용 라벨).</summary>
        public static string PhaseLabel(DayPhase phase)
        {
            switch (phase)
            {
                case DayPhase.Market: return "장보기";
                case DayPhase.Service: return "영업";
                case DayPhase.Settlement: return "정산";
                case DayPhase.Night: return "밤";
                default: return phase.ToString();
            }
        }

#if UNITY_EDITOR
        /// <summary>SceneBuilder 전용 참조 주입 (기존 시그니처 — genreBadge 는 미배선 상태로 유지).</summary>
        internal void EditorInit(
            TMP_Text dayPhaseText, Button advanceButton,
            GameObject marketPanel, GameObject servicePanel,
            GameObject settlementPanel, GameObject nightPanel)
        {
            EditorInit(dayPhaseText, advanceButton, marketPanel, servicePanel, settlementPanel, nightPanel, null);
        }

        /// <summary>SceneBuilder 전용 참조 주입 — genreBadge 배선 포함 (task-110, U5 채택 전까지 overload 유지).</summary>
        internal void EditorInit(
            TMP_Text dayPhaseText, Button advanceButton,
            GameObject marketPanel, GameObject servicePanel,
            GameObject settlementPanel, GameObject nightPanel, TMP_Text genreBadge)
        {
            this.genreBadge = genreBadge;
            this.dayPhaseText = dayPhaseText;
            this.advanceButton = advanceButton;
            this.marketPanel = marketPanel;
            this.servicePanel = servicePanel;
            this.settlementPanel = settlementPanel;
            this.nightPanel = nightPanel;
        }
#endif
    }
}
