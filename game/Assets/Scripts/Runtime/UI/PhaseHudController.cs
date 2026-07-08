using ClientIsKing.DayCycle;
using ClientIsKing.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClientIsKing.UI
{
    /// <summary>
    /// Shop 씬 HUD — 현재 day/phase 표시, 다음 phase 버튼, phase 별 placeholder 패널 토글.
    /// 실제 phase 내용(장보기/서빙/정산 UI)은 task-105~107 이 패널을 교체하며 채운다.
    /// </summary>
    public sealed class PhaseHudController : MonoBehaviour
    {
        [SerializeField] private TMP_Text dayPhaseText;
        [SerializeField] private Button advanceButton;
        [SerializeField] private GameObject marketPanel;
        [SerializeField] private GameObject servicePanel;
        [SerializeField] private GameObject settlementPanel;
        [SerializeField] private GameObject nightPanel;

        private TMP_Text advanceLabel;

        private void OnEnable()
        {
            GameEvents.DayPhaseChanged += OnDayPhaseChanged;
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
        }

        private void OnAdvanceClicked()
        {
            GameManager.Instance.AdvancePhase();
        }

        private void Update()
        {
            // 파산은 phase 이벤트 없이 발생할 수 있어(정산 중) 가벼운 폴링으로 버튼을 잠근다 (task-107).
            if (advanceButton == null)
            {
                return;
            }
            var gm = GameManager.Instance;
            bool bankrupt = gm != null && gm.State != null && gm.State.isBankrupt;
            if (advanceButton.interactable == bankrupt)
            {
                advanceButton.interactable = !bankrupt;
            }
        }

        private void OnDayPhaseChanged(DayPhaseChangedEventArgs args)
        {
            Refresh(args.Day, args.CurrentPhase);
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
        /// <summary>SceneBuilder 전용 참조 주입.</summary>
        internal void EditorInit(
            TMP_Text dayPhaseText, Button advanceButton,
            GameObject marketPanel, GameObject servicePanel,
            GameObject settlementPanel, GameObject nightPanel)
        {
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
