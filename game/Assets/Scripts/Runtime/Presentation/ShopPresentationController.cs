using System.Collections;
using System.Collections.Generic;
using ClientIsKing.DayCycle;
using ClientIsKing.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClientIsKing.Presentation
{
    /// <summary>
    /// Shop 무대 연출 컨트롤러 (task-108) — 손님 입장/퇴장, 음식 아이콘, +N원 팝업,
    /// 정산 pulse, 밤 오버레이. 게임 규칙에는 관여하지 않으며(표현 전용),
    /// 이벤트 payload 의 미지 id 는 예외 없이 빈 표시로 복구한다 (설계 161행).
    /// 트윈은 Play 모드에서만 돌고, Edit 모드(테스트)에서는 즉시 상태로 스냅한다.
    /// </summary>
    public sealed class ShopPresentationController : MonoBehaviour
    {
        // 무대 좌표 (ShopStage 로컬 기준 — SceneBuilder 배치와 동기: 바닥선 y≈32, 카운터 좌측 정지)
        static readonly Vector2 CustomerEnterPos = new Vector2(-360f, 56f);
        static readonly Vector2 CustomerCounterPos = new Vector2(-160f, 56f);
        static readonly Vector2 CustomerExitHappyPos = new Vector2(380f, 56f);
        static readonly Vector2 CustomerExitUnhappyPos = new Vector2(-380f, 56f);
        const float NightOverlayAlpha = 0.55f;

        const float WalkFrameInterval = 0.12f; // 걷기 프레임 스왑 간격 (task-109)

        static readonly Color HappyTint = new Color(0.75f, 1f, 0.75f, 1f);
        static readonly Color UnhappyTint = new Color(1f, 0.55f, 0.55f, 1f);
        static readonly Color PositivePulse = new Color(0.45f, 0.95f, 0.45f, 1f);
        static readonly Color NegativePulse = new Color(1f, 0.6f, 0.3f, 1f);
        static readonly Color BankruptPulse = new Color(0.95f, 0.25f, 0.25f, 1f);

        [SerializeField] private RectTransform customerRect;
        [SerializeField] private Image customerImage;
        [SerializeField] private TMP_Text customerLabel;
        [SerializeField] private TMP_Text orderLabel;
        [SerializeField] private Image foodIconImage;
        [SerializeField] private TMP_Text cashPopupText;
        [SerializeField] private TMP_Text settlementPulseText;
        [SerializeField] private Image nightOverlayImage;
        [SerializeField] private List<CustomerSpriteEntry> customerSprites = new List<CustomerSpriteEntry>();
        [SerializeField] private List<RecipeSpriteEntry> recipeSprites = new List<RecipeSpriteEntry>();

        /// <summary>테스트/검증용 read-only 노출.</summary>
        public IReadOnlyList<CustomerSpriteEntry> CustomerSprites => customerSprites;
        public IReadOnlyList<RecipeSpriteEntry> RecipeSprites => recipeSprites;

        private Coroutine customerRoutine;
        private Coroutine walkRoutine;
        private Coroutine foodRoutine;
        private Coroutine popupRoutine;
        private Coroutine pulseRoutine;
        private Coroutine overlayRoutine;
        private Vector2 popupBasePos;
        private Sprite[] currentWalkFrames; // 현재 손님의 걷기 프레임 (없으면 null → idle 고정)

        private void OnEnable()
        {
            GameEvents.DayPhaseChanged += HandleDayPhaseChanged;
            GameEvents.ServiceOrderPresented += HandleOrderPresented;
            GameEvents.ServiceOutcomeResolved += HandleOutcomeResolved;
            GameEvents.SettlementPresented += HandleSettlementPresented;

            if (cashPopupText != null)
            {
                popupBasePos = ((RectTransform)cashPopupText.transform).anchoredPosition;
                SetAlpha(cashPopupText, 0f);
            }
            if (settlementPulseText != null)
            {
                SetAlpha(settlementPulseText, 0f);
            }
            if (foodIconImage != null)
            {
                foodIconImage.gameObject.SetActive(false);
            }
            HideCustomer();

            // 현재 phase 에 맞는 오버레이 초기화 (Night 재진입/씬 로드 대응)
            var gm = GameManager.Instance;
            bool night = gm != null && gm.State != null && gm.State.currentPhase == DayPhase.Night;
            SnapOverlay(night ? NightOverlayAlpha : 0f);
        }

        private void OnDisable()
        {
            GameEvents.DayPhaseChanged -= HandleDayPhaseChanged;
            GameEvents.ServiceOrderPresented -= HandleOrderPresented;
            GameEvents.ServiceOutcomeResolved -= HandleOutcomeResolved;
            GameEvents.SettlementPresented -= HandleSettlementPresented;
            StopAllCoroutines();
        }

        // ── 이벤트 핸들러 (internal — EditMode 테스트가 직접 호출 가능, IVT) ──

        internal void HandleDayPhaseChanged(DayPhaseChangedEventArgs args)
        {
            float target = args.CurrentPhase == DayPhase.Night ? NightOverlayAlpha : 0f;
            if (Application.isPlaying && isActiveAndEnabled && nightOverlayImage != null)
            {
                Run(ref overlayRoutine, PresentationTween.FadeAlpha(
                    nightOverlayImage, nightOverlayImage.color.a, target, 0.5f));
            }
            else
            {
                SnapOverlay(target);
            }
        }

        internal void HandleOrderPresented(ServicePresentationEventArgs args)
        {
            if (!args.HasOrder)
            {
                HideCustomer();
                return;
            }

            var entry = FindCustomerEntry(args.CustomerId);
            var sprite = entry?.sprite;
            currentWalkFrames = ValidWalkFrames(entry);
            if (customerImage != null)
            {
                customerImage.sprite = sprite;
                customerImage.color = Color.white;
                customerImage.enabled = sprite != null; // 미지 id → 이미지 숨김 (fallback)
                customerImage.gameObject.SetActive(true);
                FaceRight(); // 입장 = 우향
            }
            if (customerLabel != null)
            {
                // task-111 F3: SNS 유입 주문만 Jade Green 태그 (TMP rich text) — 게임 규칙에는 관여하지 않는다.
                customerLabel.text = args.SnsInflow
                    ? $"×{args.PartySize} <color=#4FAE82>SNS</color>"
                    : $"×{args.PartySize}";
            }
            if (orderLabel != null)
            {
                // Message 에 레시피 표시명이 실려 온다 (ServicePanelController 발행 규약)
                orderLabel.text = string.IsNullOrEmpty(args.Message) ? args.RecipeId : args.Message;
            }

            if (Application.isPlaying && isActiveAndEnabled && customerRect != null)
            {
                Run(ref customerRoutine, EnterRoutine(sprite));
            }
            else if (customerRect != null)
            {
                customerRect.anchoredPosition = CustomerCounterPos;
            }
        }

        internal void HandleOutcomeResolved(ServicePresentationEventArgs args)
        {
            if (args.Served)
            {
                ShowFoodIcon(args.RecipeId);
                ShowCashPopup(args.RevenueGained);
                DepartCustomer(happy: true);
            }
            else if (args.Missed)
            {
                DepartCustomer(happy: false);
            }
        }

        internal void HandleSettlementPresented(SettlementPresentationEventArgs args)
        {
            if (settlementPulseText == null)
            {
                return;
            }
            string sign = args.NetProfit >= 0 ? "+" : "";
            settlementPulseText.text = $"순손익 {sign}{args.NetProfit:N0}원";
            settlementPulseText.color = WithAlpha(
                args.Bankrupt ? BankruptPulse : args.NetProfit >= 0 ? PositivePulse : NegativePulse, 1f);

            if (Application.isPlaying && isActiveAndEnabled)
            {
                Run(ref pulseRoutine, PulseRoutine());
            }
        }

        // ── 연출 시퀀스 ─────────────────────────────────────────────────────

        private void ShowFoodIcon(string recipeId)
        {
            if (foodIconImage == null)
            {
                return;
            }
            var sprite = FindRecipeSprite(recipeId);
            foodIconImage.sprite = sprite;
            foodIconImage.enabled = sprite != null;
            foodIconImage.gameObject.SetActive(true);
            if (Application.isPlaying && isActiveAndEnabled)
            {
                Run(ref foodRoutine, FoodIconRoutine());
            }
        }

        private IEnumerator FoodIconRoutine()
        {
            yield return PresentationTween.ScaleTo(foodIconImage.transform, Vector3.one * 0.5f, Vector3.one, 0.18f);
            yield return new WaitForSeconds(0.7f);
            foodIconImage.gameObject.SetActive(false);
        }

        private void ShowCashPopup(int revenue)
        {
            if (cashPopupText == null || revenue <= 0)
            {
                return;
            }
            cashPopupText.text = $"+{revenue:N0}원";
            if (Application.isPlaying && isActiveAndEnabled)
            {
                Run(ref popupRoutine, CashPopupRoutine());
            }
            else
            {
                SetAlpha(cashPopupText, 1f);
            }
        }

        private IEnumerator CashPopupRoutine()
        {
            var rect = (RectTransform)cashPopupText.transform;
            rect.anchoredPosition = popupBasePos;
            SetAlpha(cashPopupText, 1f);
            float duration = 0.8f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                rect.anchoredPosition = popupBasePos + new Vector2(0f, 34f * k);
                SetAlpha(cashPopupText, 1f - k);
                yield return null;
            }
            SetAlpha(cashPopupText, 0f);
            rect.anchoredPosition = popupBasePos;
        }

        private void DepartCustomer(bool happy)
        {
            if (customerImage == null || customerRect == null)
            {
                return;
            }
            if (Application.isPlaying && isActiveAndEnabled)
            {
                Run(ref customerRoutine, DepartRoutine(happy));
            }
            else
            {
                HideCustomer();
            }
        }

        private IEnumerator DepartRoutine(bool happy)
        {
            customerImage.color = happy ? HappyTint : UnhappyTint;
            yield return new WaitForSeconds(0.22f);
            // 만족 = 우향(+1), 불만 = 좌향(-1) 플립. 스프라이트 재작업 없이 우향 시트만 사용.
            if (happy) { FaceRight(); } else { FaceLeft(); }
            StartWalkCycle();
            yield return PresentationTween.MoveAnchored(
                customerRect, customerRect.anchoredPosition,
                happy ? CustomerExitHappyPos : CustomerExitUnhappyPos, 0.4f);
            StopWalkCycle(snapSprite: null); // 퇴장 후 숨겨질 것이므로 스프라이트 복원 불필요
            HideCustomer();
        }

        // ── 걷기 프레임 스왑 (task-109) ─────────────────────────────────────

        /// <summary>입장: 이동하며 걷기 프레임 순환 → 도착 시 idle 고정.</summary>
        private IEnumerator EnterRoutine(Sprite idleSprite)
        {
            StartWalkCycle();
            yield return PresentationTween.MoveAnchored(
                customerRect, CustomerEnterPos, CustomerCounterPos, 0.45f);
            StopWalkCycle(snapSprite: idleSprite); // 정지 시 idle 고정
        }

        /// <summary>이동 중에만 도는 프레임 스왑 서브 코루틴 시작 (walkFrames 없으면 no-op).</summary>
        private void StartWalkCycle()
        {
            if (!Application.isPlaying || currentWalkFrames == null || currentWalkFrames.Length == 0 || customerImage == null)
            {
                return;
            }
            Run(ref walkRoutine, WalkCycleRoutine());
        }

        private IEnumerator WalkCycleRoutine()
        {
            int i = 0;
            while (true)
            {
                var frame = currentWalkFrames[i % currentWalkFrames.Length];
                if (frame != null)
                {
                    customerImage.sprite = frame;
                    customerImage.enabled = true;
                }
                i++;
                yield return new WaitForSeconds(WalkFrameInterval);
            }
        }

        /// <summary>걷기 순환 정지 + (있으면) idle 스프라이트로 고정.</summary>
        private void StopWalkCycle(Sprite snapSprite)
        {
            if (walkRoutine != null)
            {
                StopCoroutine(walkRoutine);
                walkRoutine = null;
            }
            if (snapSprite != null && customerImage != null)
            {
                customerImage.sprite = snapSprite;
                customerImage.enabled = true;
            }
        }

        private void FaceRight()
        {
            if (customerImage != null)
            {
                var s = customerImage.rectTransform.localScale;
                s.x = Mathf.Abs(s.x);
                customerImage.rectTransform.localScale = s;
            }
        }

        private void FaceLeft()
        {
            if (customerImage != null)
            {
                var s = customerImage.rectTransform.localScale;
                s.x = -Mathf.Abs(s.x);
                customerImage.rectTransform.localScale = s;
            }
        }

        private IEnumerator PulseRoutine()
        {
            yield return PresentationTween.ScaleTo(settlementPulseText.transform, Vector3.one * 0.7f, Vector3.one, 0.2f);
            yield return new WaitForSeconds(1.1f);
            yield return PresentationTween.FadeAlpha(settlementPulseText, 1f, 0f, 0.4f);
        }

        // ── 헬퍼 ────────────────────────────────────────────────────────────

        private void HideCustomer()
        {
            StopWalkCycle(snapSprite: null);
            currentWalkFrames = null;
            if (customerImage != null)
            {
                customerImage.gameObject.SetActive(false);
                FaceRight(); // 다음 입장을 위해 우향으로 복원
            }
            if (customerLabel != null)
            {
                customerLabel.text = "";
            }
            if (orderLabel != null)
            {
                orderLabel.text = "";
            }
            if (customerRect != null)
            {
                customerRect.anchoredPosition = CustomerEnterPos;
            }
        }

        private CustomerSpriteEntry FindCustomerEntry(string customerId)
        {
            foreach (var entry in customerSprites)
            {
                if (entry != null && entry.customerId == customerId)
                {
                    return entry;
                }
            }
            return null;
        }

        /// <summary>entry 의 walkFrames 중 null 이 아닌 실제 스프라이트만; 없으면 null (단일 sprite 폴백).</summary>
        static Sprite[] ValidWalkFrames(CustomerSpriteEntry entry)
        {
            if (entry?.walkFrames == null || entry.walkFrames.Length == 0)
            {
                return null;
            }
            var valid = new List<Sprite>(entry.walkFrames.Length);
            foreach (var f in entry.walkFrames)
            {
                if (f != null)
                {
                    valid.Add(f);
                }
            }
            return valid.Count > 0 ? valid.ToArray() : null;
        }

        private Sprite FindRecipeSprite(string recipeId)
        {
            foreach (var entry in recipeSprites)
            {
                if (entry != null && entry.recipeId == recipeId)
                {
                    return entry.sprite;
                }
            }
            return null;
        }

        private void SnapOverlay(float alpha)
        {
            if (nightOverlayImage != null)
            {
                SetAlpha(nightOverlayImage, alpha);
            }
        }

        private void Run(ref Coroutine slot, IEnumerator routine)
        {
            if (slot != null)
            {
                StopCoroutine(slot);
            }
            slot = StartCoroutine(routine);
        }

        static void SetAlpha(Graphic g, float a)
        {
            var c = g.color;
            c.a = a;
            g.color = c;
        }

        static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }

#if UNITY_EDITOR
        /// <summary>SceneBuilder 전용 참조/카탈로그 주입 (EditorInit 패턴).</summary>
        internal void EditorInit(
            RectTransform customerRect, Image customerImage, TMP_Text customerLabel, TMP_Text orderLabel,
            Image foodIconImage, TMP_Text cashPopupText, TMP_Text settlementPulseText, Image nightOverlayImage,
            List<CustomerSpriteEntry> customerSprites, List<RecipeSpriteEntry> recipeSprites)
        {
            this.customerRect = customerRect;
            this.customerImage = customerImage;
            this.customerLabel = customerLabel;
            this.orderLabel = orderLabel;
            this.foodIconImage = foodIconImage;
            this.cashPopupText = cashPopupText;
            this.settlementPulseText = settlementPulseText;
            this.nightOverlayImage = nightOverlayImage;
            this.customerSprites = customerSprites;
            this.recipeSprites = recipeSprites;
        }
#endif
    }
}
