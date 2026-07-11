using System.Collections.Generic;
using ClientIsKing.Data;
using ClientIsKing.DayCycle;
using ClientIsKing.Economy;
using ClientIsKing.Genre;
using ClientIsKing.Inventory;
using ClientIsKing.Managers;
using ClientIsKing.Service;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ClientIsKing.UI
{
    /// <summary>
    /// Market phase 장보기 UI — 재료/등급 선택, 수량 조절, 예상 비용, 구매 처리.
    /// 재료·등급 선택은 픽셀 UI 에 맞춘 ◀ ▶ 순환 셀렉터 (TMP_Dropdown 코드 저작 대체 — impl-notes 참조).
    /// phase 전환 규칙에는 관여하지 않는다 (설계 11단계 — 패널 토글은 PhaseHudController 소관).
    /// task-110 (U4): 첫 진입 시 장르 선택 modal(design.md E3), confirm 전 구매 잠금,
    /// 확정 후 specialist 는 plan recipe 가 요구하는 재료 행만 표시, B급 행/등급 토글 숨김(C급만),
    /// TrySelect 성공 시 GameEvents.GenreSelected 발행.
    /// </summary>
    public sealed class MarketPanelController : MonoBehaviour
    {
        const int MinQuantity = 1;
        const int MaxQuantity = 99;

        [SerializeField] private TMP_Text cashText;
        [SerializeField] private Button ingredientPrevButton;
        [SerializeField] private Button ingredientNextButton;
        [SerializeField] private TMP_Text ingredientLabel;
        [SerializeField] private Button gradeToggleButton;
        [SerializeField] private TMP_Text gradeLabel;
        [SerializeField] private Button quantityMinusButton;
        [SerializeField] private Button quantityPlusButton;
        [SerializeField] private TMP_Text quantityText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text ownedText;
        [SerializeField] private Button buyButton;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private List<IngredientDef> ingredientDefs = new List<IngredientDef>();
        [SerializeField] private List<RecipeDef> recipeDefs = new List<RecipeDef>();

        // ── 장르 선택 modal (task-110 E3 — SceneBuilder 가 생성·주입, canvas 최상단 자식) ──
        [SerializeField] private GameObject genreSelectionPanel;
        [SerializeField] private Button gukbapButton;
        [SerializeField] private Button bunsikButton;
        [SerializeField] private Button noodlesButton;
        [SerializeField] private Button generalistButton;
        [SerializeField] private TMP_Text genreDetailNameText;
        [SerializeField] private TMP_Text genreDetailBodyText;
        [SerializeField] private TMP_Text genreDetailNumbersText;
        [SerializeField] private Button genreConfirmButton;
        [SerializeField] private TMP_Text genreHelperText;
        [SerializeField] private Button genreDetailToggleButton;

        /// <summary>SceneBuilder 가 주입한 재료 정의 (테스트 검증용 read-only 노출).</summary>
        public IReadOnlyList<IngredientDef> IngredientDefs => ingredientDefs;

        /// <summary>SceneBuilder 가 주입한 레시피 정의 (specialist 재료 행 필터용 — 테스트 검증용 노출).</summary>
        public IReadOnlyList<RecipeDef> RecipeDefs => recipeDefs;

        private List<IngredientKind> kinds;
        private int kindIndex;
        private IngredientGrade grade = IngredientGrade.C;
        private int quantity = MinQuantity;

        /// <summary>확정 전 후보 장르 — focus 순서 첫 항목 국밥 (design.md E3).</summary>
        private string candidateGenreId = GenreSelectionCopy.GukbapId;

        /// <summary>확정 후 `상세 보기` 버튼으로 modal 을 다시 연 상태 (read-only 보기).</summary>
        private bool detailViewOpen;

        private void OnEnable()
        {
            ingredientPrevButton.onClick.AddListener(OnPrevKind);
            ingredientNextButton.onClick.AddListener(OnNextKind);
            gradeToggleButton.onClick.AddListener(OnToggleGrade);
            quantityMinusButton.onClick.AddListener(OnMinus);
            quantityPlusButton.onClick.AddListener(OnPlus);
            buyButton.onClick.AddListener(OnBuy);
            if (gukbapButton != null) gukbapButton.onClick.AddListener(OnPickGukbap);
            if (bunsikButton != null) bunsikButton.onClick.AddListener(OnPickBunsik);
            if (noodlesButton != null) noodlesButton.onClick.AddListener(OnPickNoodles);
            if (generalistButton != null) generalistButton.onClick.AddListener(OnPickGeneralist);
            if (genreConfirmButton != null) genreConfirmButton.onClick.AddListener(OnConfirmGenre);
            if (genreDetailToggleButton != null) genreDetailToggleButton.onClick.AddListener(OnToggleGenreDetail);

            // task-110: B급 구매 행/등급 토글 숨김 — 플레이어 UI 는 C급만 사용한다 (B급 데이터·Ops 보존).
            grade = IngredientGrade.C;
            if (gradeToggleButton != null)
            {
                gradeToggleButton.gameObject.SetActive(false);
            }

            // 패널 재활성화(다음 날 Market 복귀) 시 최신 상태로 다시 그린다.
            if (kinds != null)
            {
                BuildKindList();
                RefreshAll();
            }
            RefreshGenreSelectionUI();
        }

        private void OnDisable()
        {
            ingredientPrevButton.onClick.RemoveListener(OnPrevKind);
            ingredientNextButton.onClick.RemoveListener(OnNextKind);
            gradeToggleButton.onClick.RemoveListener(OnToggleGrade);
            quantityMinusButton.onClick.RemoveListener(OnMinus);
            quantityPlusButton.onClick.RemoveListener(OnPlus);
            buyButton.onClick.RemoveListener(OnBuy);
            if (gukbapButton != null) gukbapButton.onClick.RemoveListener(OnPickGukbap);
            if (bunsikButton != null) bunsikButton.onClick.RemoveListener(OnPickBunsik);
            if (noodlesButton != null) noodlesButton.onClick.RemoveListener(OnPickNoodles);
            if (generalistButton != null) generalistButton.onClick.RemoveListener(OnPickGeneralist);
            if (genreConfirmButton != null) genreConfirmButton.onClick.RemoveListener(OnConfirmGenre);
            if (genreDetailToggleButton != null) genreDetailToggleButton.onClick.RemoveListener(OnToggleGenreDetail);

            // modal 은 canvas 자식이므로 Market 패널과 함께 반드시 닫는다 (phase 전환 시 잔존 방지).
            if (genreSelectionPanel != null)
            {
                genreSelectionPanel.SetActive(false);
            }
            detailViewOpen = false;
        }

        private void Start()
        {
            BuildKindList();
            if (messageText != null)
            {
                messageText.text = "";
            }
            RefreshAll();
            RefreshGenreSelectionUI();
        }

        private void Update()
        {
            // E3: 좌우 방향키는 Selectable Navigation(SceneBuilder 배선), Tab 은 여기서 순환 처리.
            if (genreSelectionPanel == null || !genreSelectionPanel.activeInHierarchy)
            {
                return;
            }
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

        /// <summary>주입된 defs 를 id 순으로 정렬해 종류 목록 구성 — 초기 선택은 첫 종류 + C 등급 (결정론).</summary>
        private void BuildKindList()
        {
            ingredientDefs.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            // task-110: specialist 확정 시 plan recipe 가 요구하는 재료 종류만 (generalist/미확정 = 전체).
            var allowedKinds = BuildAllowedKinds();
            kinds = new List<IngredientKind>();
            foreach (var def in ingredientDefs)
            {
                if (kinds.Contains(def.Kind))
                {
                    continue;
                }
                if (allowedKinds != null && !allowedKinds.Contains(def.Kind))
                {
                    continue;
                }
                kinds.Add(def.Kind);
            }
            kindIndex = 0;
            grade = IngredientGrade.C;
        }

        /// <summary>
        /// specialist 확정 시 오늘 plan 의 허용 recipe 가 요구하는 재료 종류 집합.
        /// generalist·미확정·plan 실패는 null(전체 표시) — 필터는 표시 규칙이며 구매 게이트는 도메인이 소유.
        /// </summary>
        private HashSet<IngredientKind> BuildAllowedKinds()
        {
            var genre = SelectedGenreOrNull();
            if (genre == null || genre.Kind == GenreKind.Generalist)
            {
                return null;
            }
            var service = ServiceManager.Instance;
            if (service == null || !service.TryBuildDayPlan(genre, out var plan, out _))
            {
                return null;
            }
            var allowed = new HashSet<IngredientKind>();
            foreach (var recipeId in plan.AllowedRecipeIds)
            {
                foreach (var recipe in recipeDefs)
                {
                    if (recipe == null || !string.Equals(recipe.Id, recipeId, System.StringComparison.Ordinal))
                    {
                        continue;
                    }
                    foreach (var req in recipe.Ingredients)
                    {
                        allowed.Add(req.Kind);
                    }
                    break;
                }
            }
            return allowed.Count > 0 ? allowed : null;
        }

        private IngredientDef CurrentDef
        {
            get
            {
                if (kinds == null || kinds.Count == 0)
                {
                    return null;
                }
                var kind = kinds[kindIndex];
                foreach (var def in ingredientDefs)
                {
                    if (def.Kind == kind && def.Grade == grade)
                    {
                        return def;
                    }
                }
                return null;
            }
        }

        private void OnPrevKind()
        {
            if (kinds == null || kinds.Count == 0) return;
            kindIndex = (kindIndex - 1 + kinds.Count) % kinds.Count;
            RefreshAll();
        }

        private void OnNextKind()
        {
            if (kinds == null || kinds.Count == 0) return;
            kindIndex = (kindIndex + 1) % kinds.Count;
            RefreshAll();
        }

        private void OnToggleGrade()
        {
            grade = grade == IngredientGrade.C ? IngredientGrade.B : IngredientGrade.C;
            RefreshAll();
        }

        private void OnMinus()
        {
            quantity = Mathf.Max(MinQuantity, quantity - 1);
            RefreshAll();
        }

        private void OnPlus()
        {
            quantity = Mathf.Min(MaxQuantity, quantity + 1);
            RefreshAll();
        }

        private void OnBuy()
        {
            var economy = EconomyManager.Instance;
            if (economy == null)
            {
                return;
            }
            var result = economy.TryPurchaseIngredient(CurrentDef, quantity);
            if (messageText != null)
            {
                messageText.text = result.Message;
            }
            RefreshAll();
        }

        // ── 장르 선택 modal (task-110 U4) ───────────────────────────────────

        private void OnPickGukbap() { OnPickGenre(GenreSelectionCopy.GukbapId); }
        private void OnPickBunsik() { OnPickGenre(GenreSelectionCopy.BunsikId); }
        private void OnPickNoodles() { OnPickGenre(GenreSelectionCopy.NoodlesId); }
        private void OnPickGeneralist() { OnPickGenre(GenreSelectionCopy.GeneralistId); }

        private void OnPickGenre(string genreId)
        {
            if (GenreConfirmed)
            {
                return; // 확정 후 재선택 불가 (버튼도 비활성 — 이중 방어)
            }
            candidateGenreId = genreId;
            RefreshGenreSelectionUI();
        }

        private void OnConfirmGenre()
        {
            if (GenreConfirmed)
            {
                // 확정 후 modal 은 `상세 보기` 전용 — confirm 버튼은 `닫기` 로 동작한다.
                detailViewOpen = false;
                RefreshGenreSelectionUI();
                return;
            }
            var gm = GameManager.Instance;
            if (gm == null || gm.State == null)
            {
                return;
            }
            var availableIds = new List<string>();
            foreach (var def in gm.GenreCatalog)
            {
                if (def != null)
                {
                    availableIds.Add(def.Id);
                }
            }
            var result = GenreSelectionOps.TrySelect(gm.State, candidateGenreId, availableIds);
            if (!result.Success)
            {
                if (genreHelperText != null)
                {
                    genreHelperText.text = result.Message;
                }
                return;
            }
            // 선택 성공 — 정확히 1회 발행해 HUD badge/advance gate 를 즉시 refresh (design.md H10/G3).
            GameEvents.RaiseGenreSelected(result.GenreId);
            if (messageText != null)
            {
                messageText.text = result.Message;
            }
            detailViewOpen = false;
            BuildKindList();
            RefreshGenreSelectionUI();
            RefreshAll();
        }

        private void OnToggleGenreDetail()
        {
            if (!GenreConfirmed)
            {
                return;
            }
            detailViewOpen = !detailViewOpen;
            RefreshGenreSelectionUI();
        }

        /// <summary>modal 표시/버튼 상태/E3 문구를 현재 선택 상태에 맞춘다 (미배선 fixture 는 no-op).</summary>
        private void RefreshGenreSelectionUI()
        {
            if (genreSelectionPanel == null)
            {
                return;
            }

            bool confirmed = GenreConfirmed;
            var state = StateOrNull;
            string shownGenreId = confirmed ? state.selectedGenreId : candidateGenreId;
            bool showModal = isActiveAndEnabled && (!confirmed || detailViewOpen);
            bool wasShown = genreSelectionPanel.activeSelf;
            genreSelectionPanel.SetActive(showModal);

            RefreshGenreButton(gukbapButton, GenreSelectionCopy.GukbapId, shownGenreId, confirmed);
            RefreshGenreButton(bunsikButton, GenreSelectionCopy.BunsikId, shownGenreId, confirmed);
            RefreshGenreButton(noodlesButton, GenreSelectionCopy.NoodlesId, shownGenreId, confirmed);
            RefreshGenreButton(generalistButton, GenreSelectionCopy.GeneralistId, shownGenreId, confirmed);

            if (genreConfirmButton != null)
            {
                var label = genreConfirmButton.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = confirmed ? "닫기" : "이 전문 분야로 시작";
                }
            }
            if (genreHelperText != null)
            {
                genreHelperText.text = confirmed
                    ? "전문 분야는 이번 런 동안 유지됩니다."
                    : "선택은 이번 런 동안 유지됩니다 · ←/→·Tab 이동";
            }
            if (genreDetailToggleButton != null)
            {
                genreDetailToggleButton.gameObject.SetActive(confirmed);
            }
            RefreshGenreDetail(shownGenreId);

            // modal 이 새로 열릴 때 focus 순서 첫 항목(국밥, 확정 후에는 닫기)에 focus (E3).
            if (showModal && !wasShown && Application.isPlaying && EventSystem.current != null)
            {
                var chain = BuildFocusChain();
                if (chain.Count > 0)
                {
                    EventSystem.current.SetSelectedGameObject(chain[0].gameObject);
                }
            }
        }

        /// <summary>선택(후보/확정) 버튼만 Gochujang Red outline 2px + 아이콘 — 색만으로 상태 전달 금지 (E3/E5).</summary>
        private static void RefreshGenreButton(Button button, string genreId, string shownGenreId, bool confirmed)
        {
            if (button == null)
            {
                return;
            }
            button.interactable = !confirmed;
            bool chosen = string.Equals(genreId, shownGenreId, System.StringComparison.Ordinal);
            var outline = button.GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = chosen;
            }
            var icon = button.transform.Find("Icon");
            if (icon != null)
            {
                icon.gameObject.SetActive(chosen);
            }
        }

        /// <summary>detail 영역 — 선택명 16pt, 본문 13pt(시드 description), 수치 12pt(E3 비교/forecast + plan 가격 범위).</summary>
        private void RefreshGenreDetail(string genreId)
        {
            var gm = GameManager.Instance;
            GenreDef def = null;
            bool found = gm != null && gm.TryGetGenre(genreId, out def);
            if (genreDetailNameText != null)
            {
                genreDetailNameText.text = found ? $"{def.DisplayName} — {GenreSelectionCopy.Headline(genreId)}" : "";
            }
            if (genreDetailBodyText != null)
            {
                genreDetailBodyText.text = found ? def.Description : "";
            }
            if (genreDetailNumbersText != null)
            {
                if (!found)
                {
                    genreDetailNumbersText.text = "";
                    return;
                }
                string forecastLine = GenreSelectionCopy.Forecast(genreId);
                var service = ServiceManager.Instance;
                if (service != null && service.TryBuildDayPlan(def, out var plan, out _))
                {
                    // 1인 예상 가격 범위는 plan forecast 값 — UI 가 SO 배수를 직접 계산하지 않는다 (G2).
                    forecastLine += $" · 1인 {plan.MinPricePerCustomer:N0}~{plan.MaxPricePerCustomer:N0}원";
                }
                genreDetailNumbersText.text = GenreSelectionCopy.Comparison(genreId) + "\n" + forecastLine;
            }
        }

        /// <summary>modal focus 순환 대상 — 국밥→분식→면류→균형→확정 순서 중 활성·interactable 만 (E3).</summary>
        private List<Selectable> BuildFocusChain()
        {
            var chain = new List<Selectable>();
            foreach (var button in new[] { gukbapButton, bunsikButton, noodlesButton, generalistButton, genreConfirmButton })
            {
                if (button != null && button.gameObject.activeInHierarchy && button.interactable)
                {
                    chain.Add(button);
                }
            }
            return chain;
        }

        private static GameState StateOrNull =>
            GameManager.Instance != null ? GameManager.Instance.State : null;

        private bool GenreConfirmed
        {
            get
            {
                var state = StateOrNull;
                return state != null && !string.IsNullOrEmpty(state.selectedGenreId);
            }
        }

        private static GenreDef SelectedGenreOrNull()
        {
            var gm = GameManager.Instance;
            var state = gm != null ? gm.State : null;
            if (state == null)
            {
                return null;
            }
            return gm.TryGetGenre(state.selectedGenreId, out var def) ? def : null;
        }

        /// <summary>Market 예상 비용에 쓰는 장르 원가 배수 — transaction 과 같은 EconomyOps helper 에 전달 (H11).</summary>
        private static float CurrentCostMultiplier()
        {
            var genre = SelectedGenreOrNull();
            return genre != null ? genre.CostMultiplier : 1f;
        }

        private void RefreshAll()
        {
            var def = CurrentDef;
            var economy = EconomyManager.Instance;
            var inventory = InventoryManager.Instance;

            if (cashText != null)
            {
                cashText.text = economy != null ? $"자금 {economy.Cash:N0}원" : "자금 -";
            }
            if (ingredientLabel != null)
            {
                ingredientLabel.text = def != null ? KindLabel(def) : "-";
            }
            if (gradeLabel != null)
            {
                gradeLabel.text = grade == IngredientGrade.C ? "등급: C급" : "등급: B급";
            }
            if (quantityText != null)
            {
                quantityText.text = quantity.ToString();
            }
            if (costText != null)
            {
                int cost = EconomyOps.CalculatePurchaseCost(def, quantity, CurrentCostMultiplier());
                costText.text = $"예상 비용 {cost:N0}원";
            }
            if (ownedText != null && def != null)
            {
                int owned = inventory != null ? inventory.GetQuantity(def.Kind, def.Grade) : 0;
                ownedText.text = $"보유 {owned}개";
            }
            if (buyButton != null)
            {
                // 전문 분야 confirm 전 구매 잠금 (E3) — 도메인 게이트(EconomyManager)와 이중 방어.
                buyButton.interactable = GenreConfirmed;
            }
        }

        /// <summary>표시명에서 등급 접미사를 뗀 종류 라벨 (예: "쌀 (C급)" → "쌀").</summary>
        private static string KindLabel(IngredientDef def)
        {
            var name = def.DisplayName;
            int cut = name.IndexOf(" (");
            return cut > 0 ? name.Substring(0, cut) : name;
        }

#if UNITY_EDITOR
        /// <summary>SceneBuilder 전용 참조/데이터 주입 — 기존 시그니처 (장르 modal 은 미배선 유지).</summary>
        internal void EditorInit(
            TMP_Text cashText,
            Button ingredientPrevButton, Button ingredientNextButton, TMP_Text ingredientLabel,
            Button gradeToggleButton, TMP_Text gradeLabel,
            Button quantityMinusButton, Button quantityPlusButton, TMP_Text quantityText,
            TMP_Text costText, TMP_Text ownedText, Button buyButton, TMP_Text messageText,
            List<IngredientDef> ingredientDefs)
        {
            EditorInit(cashText,
                ingredientPrevButton, ingredientNextButton, ingredientLabel,
                gradeToggleButton, gradeLabel,
                quantityMinusButton, quantityPlusButton, quantityText,
                costText, ownedText, buyButton, messageText,
                ingredientDefs, new List<RecipeDef>(),
                null, null, null, null, null, null, null, null, null, null, null);
        }

        /// <summary>SceneBuilder 전용 참조/데이터 주입 — 장르 선택 modal 배선 포함 (task-110 U5 채택 대상).</summary>
        internal void EditorInit(
            TMP_Text cashText,
            Button ingredientPrevButton, Button ingredientNextButton, TMP_Text ingredientLabel,
            Button gradeToggleButton, TMP_Text gradeLabel,
            Button quantityMinusButton, Button quantityPlusButton, TMP_Text quantityText,
            TMP_Text costText, TMP_Text ownedText, Button buyButton, TMP_Text messageText,
            List<IngredientDef> ingredientDefs, List<RecipeDef> recipeDefs,
            GameObject genreSelectionPanel,
            Button gukbapButton, Button bunsikButton, Button noodlesButton, Button generalistButton,
            TMP_Text genreDetailNameText, TMP_Text genreDetailBodyText, TMP_Text genreDetailNumbersText,
            Button genreConfirmButton, TMP_Text genreHelperText, Button genreDetailToggleButton)
        {
            this.cashText = cashText;
            this.ingredientPrevButton = ingredientPrevButton;
            this.ingredientNextButton = ingredientNextButton;
            this.ingredientLabel = ingredientLabel;
            this.gradeToggleButton = gradeToggleButton;
            this.gradeLabel = gradeLabel;
            this.quantityMinusButton = quantityMinusButton;
            this.quantityPlusButton = quantityPlusButton;
            this.quantityText = quantityText;
            this.costText = costText;
            this.ownedText = ownedText;
            this.buyButton = buyButton;
            this.messageText = messageText;
            this.ingredientDefs = ingredientDefs;
            this.recipeDefs = recipeDefs;
            this.genreSelectionPanel = genreSelectionPanel;
            this.gukbapButton = gukbapButton;
            this.bunsikButton = bunsikButton;
            this.noodlesButton = noodlesButton;
            this.generalistButton = generalistButton;
            this.genreDetailNameText = genreDetailNameText;
            this.genreDetailBodyText = genreDetailBodyText;
            this.genreDetailNumbersText = genreDetailNumbersText;
            this.genreConfirmButton = genreConfirmButton;
            this.genreHelperText = genreHelperText;
            this.genreDetailToggleButton = genreDetailToggleButton;
        }
#endif
    }

    /// <summary>
    /// design.md E3 표의 장르별 headline/비교/forecast 공식 문구 (Codex 소유 UX copy — 임의 수정 금지).
    /// 장르 modal 과 Settlement 원인 한 줄이 같은 원문을 공유한다.
    /// </summary>
    internal static class GenreSelectionCopy
    {
        public const string GukbapId = "gukbap";
        public const string BunsikId = "bunsik";
        public const string NoodlesId = "noodles";
        public const string GeneralistId = "generalist";

        public static string Headline(string genreId)
        {
            switch (genreId)
            {
                case GukbapId: return "묵직한 한 그릇";
                case BunsikId: return "싸고 빠른 회전";
                case NoodlesId: return "균형 잡힌 운영";
                case GeneralistId: return "메뉴 폭으로 승부";
                default: return "";
            }
        }

        public static string Comparison(string genreId)
        {
            switch (genreId)
            {
                case GukbapId: return "원가 높음 · 1인 가격 높음 · 주문 4건";
                case BunsikId: return "원가 낮음 · 1인 가격 낮음 · 주문 6건";
                case NoodlesId: return "원가 보통 · 1인 가격 보통 · 주문 5건";
                case GeneralistId: return "장르 배수 없음 · 주문 5건";
                default: return "";
            }
        }

        public static string Forecast(string genreId)
        {
            switch (genreId)
            {
                case GukbapId: return "주 고객: 직장인 · 동네 어르신";
                case BunsikId: return "주 고객: 학생 · 직장인";
                case NoodlesId: return "주 고객: 직장인 · 가족";
                case GeneralistId: return "주 고객: 직장인 · 학생";
                default: return "";
            }
        }
    }
}
