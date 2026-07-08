using ClientIsKing.Managers;
using TMPro;
using UnityEngine;

namespace ClientIsKing.UI
{
    /// <summary>
    /// Night phase 하루 마감 UI — 정산 후 잔액/완료 일수/다음 날 진행 가능 여부(또는 파산)를 표시.
    /// SNS(task-109)/저장(task-111)은 이 패널에 후속 task 가 추가한다.
    /// </summary>
    public sealed class NightPanelController : MonoBehaviour
    {
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private TMP_Text daysText;
        [SerializeField] private TMP_Text statusText;

        private void OnEnable()
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
        }

#if UNITY_EDITOR
        /// <summary>SceneBuilder 전용 참조 주입 (EditorInit 패턴).</summary>
        internal void EditorInit(TMP_Text summaryText, TMP_Text daysText, TMP_Text statusText)
        {
            this.summaryText = summaryText;
            this.daysText = daysText;
            this.statusText = statusText;
        }
#endif
    }
}
