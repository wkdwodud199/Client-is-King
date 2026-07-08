using ClientIsKing.Managers;
using ClientIsKing.Settlement;
using TMPro;
using UnityEngine;

namespace ClientIsKing.UI
{
    /// <summary>
    /// Settlement phase 정산 UI — 패널 활성화 시 오늘 정산을 적용(멱등)하고 요약을 표시한다.
    /// phase 전환 규칙에는 관여하지 않는다 (진행/차단은 GameManager·PhaseHud 소관).
    /// </summary>
    public sealed class SettlementPanelController : MonoBehaviour
    {
        [SerializeField] private TMP_Text grossText;
        [SerializeField] private TMP_Text spendText;
        [SerializeField] private TMP_Text operatingText;
        [SerializeField] private TMP_Text netText;
        [SerializeField] private TMP_Text cashText;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private TMP_Text messageText;

        private void OnEnable()
        {
            var settlement = SettlementManager.Instance;
            if (settlement == null)
            {
                return;
            }
            Render(settlement.ApplyDailySettlement());
        }

        private void Render(SettlementResult result)
        {
            if (grossText != null)
            {
                grossText.text = $"매출  +{result.GrossRevenue:N0}원";
            }
            if (spendText != null)
            {
                spendText.text = $"재료 지출  -{result.IngredientSpend:N0}원";
            }
            if (operatingText != null)
            {
                operatingText.text = $"운영비  -{result.OperatingCost:N0}원";
            }
            if (netText != null)
            {
                string sign = result.NetProfit >= 0 ? "+" : "";
                netText.text = $"순손익  {sign}{result.NetProfit:N0}원";
            }
            if (cashText != null)
            {
                cashText.text = $"잔액  {result.CashBefore:N0}원 → {result.CashAfter:N0}원";
            }
            if (statsText != null)
            {
                var gm = GameManager.Instance;
                var state = gm != null ? gm.State : null;
                statsText.text = state != null
                    ? $"서빙 {state.serviceCustomersServedToday}명 · 이탈 {state.serviceCustomersMissedToday}명"
                    : "";
            }
            if (messageText != null)
            {
                messageText.text = result.Message;
            }
        }

#if UNITY_EDITOR
        /// <summary>SceneBuilder 전용 참조 주입 (EditorInit 패턴).</summary>
        internal void EditorInit(
            TMP_Text grossText, TMP_Text spendText, TMP_Text operatingText, TMP_Text netText,
            TMP_Text cashText, TMP_Text statsText, TMP_Text messageText)
        {
            this.grossText = grossText;
            this.spendText = spendText;
            this.operatingText = operatingText;
            this.netText = netText;
            this.cashText = cashText;
            this.statsText = statsText;
            this.messageText = messageText;
        }
#endif
    }
}
