using System.Collections;
using ClientIsKing.DayCycle;
using ClientIsKing.Managers;
using ClientIsKing.Presentation;
using ClientIsKing.Settlement;
using TMPro;
using UnityEngine;

namespace ClientIsKing.UI
{
    /// <summary>
    /// Settlement phase 정산 UI — 패널 활성화 시 오늘 정산을 적용(멱등)하고 요약을 표시한다.
    /// task-108: 주요 숫자는 짧은 카운트업 연출 후 정확한 최종값으로 남고(설계 26단계),
    /// 렌더 직후 SettlementPresented 표현 이벤트를 발행한다. cash delta 규칙은 task-107 그대로.
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

        private Coroutine countUpRoutine;

        private void OnEnable()
        {
            var settlement = SettlementManager.Instance;
            if (settlement == null)
            {
                return;
            }
            var result = settlement.ApplyDailySettlement();
            RenderAndPublish(result);
            if (Application.isPlaying)
            {
                if (countUpRoutine != null)
                {
                    StopCoroutine(countUpRoutine);
                }
                countUpRoutine = StartCoroutine(CountUpRoutine(result));
            }
        }

        private void OnDisable()
        {
            if (countUpRoutine != null)
            {
                StopCoroutine(countUpRoutine);
                countUpRoutine = null;
            }
        }

        /// <summary>최종값 렌더 + 표현 이벤트 발행 — EditMode 테스트가 직접 호출한다 (IVT).</summary>
        internal SettlementPresentationEventArgs RenderAndPublish(SettlementResult result)
        {
            RenderFinal(result);
            var args = SettlementPresentationEventArgs.From(result);
            GameEvents.RaiseSettlementPresented(args);
            return args;
        }

        /// <summary>정확한 최종 표시값 (카운트업의 종착점이자 EditMode 의 즉시 상태).</summary>
        internal void RenderFinal(SettlementResult result)
        {
            ApplyNumbers(result.GrossRevenue, result.IngredientSpend, result.OperatingCost,
                result.NetProfit, result.CashBefore, result.CashAfter);

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

        private void ApplyNumbers(int gross, int spend, int operating, int net, int cashBefore, int cashAfter)
        {
            if (grossText != null)
            {
                grossText.text = $"매출  +{gross:N0}원";
            }
            if (spendText != null)
            {
                spendText.text = $"재료 지출  -{spend:N0}원";
            }
            if (operatingText != null)
            {
                operatingText.text = $"운영비  -{operating:N0}원";
            }
            if (netText != null)
            {
                string sign = net >= 0 ? "+" : "";
                netText.text = $"순손익  {sign}{net:N0}원";
            }
            if (cashText != null)
            {
                cashText.text = $"잔액  {cashBefore:N0}원 → {cashAfter:N0}원";
            }
        }

        /// <summary>0.6초 카운트업 — 마지막 프레임에 RenderFinal 로 정확성을 보장한다.</summary>
        private IEnumerator CountUpRoutine(SettlementResult result)
        {
            const float duration = 0.6f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                ApplyNumbers(
                    Mathf.RoundToInt(result.GrossRevenue * k),
                    Mathf.RoundToInt(result.IngredientSpend * k),
                    Mathf.RoundToInt(result.OperatingCost * k),
                    Mathf.RoundToInt(result.NetProfit * k),
                    result.CashBefore,
                    Mathf.RoundToInt(Mathf.Lerp(result.CashBefore, result.CashAfter, k)));
                yield return null;
            }
            RenderFinal(result);
            countUpRoutine = null;
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
