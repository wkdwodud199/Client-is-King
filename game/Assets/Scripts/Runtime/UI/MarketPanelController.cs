using System.Collections.Generic;
using ClientIsKing.Data;
using ClientIsKing.Economy;
using ClientIsKing.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClientIsKing.UI
{
    /// <summary>
    /// Market phase 장보기 UI — 재료/등급 선택, 수량 조절, 예상 비용, 구매 처리.
    /// 재료·등급 선택은 픽셀 UI 에 맞춘 ◀ ▶ 순환 셀렉터 (TMP_Dropdown 코드 저작 대체 — impl-notes 참조).
    /// phase 전환 규칙에는 관여하지 않는다 (설계 11단계 — 패널 토글은 PhaseHudController 소관).
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

        /// <summary>SceneBuilder 가 주입한 재료 정의 (테스트 검증용 read-only 노출).</summary>
        public IReadOnlyList<IngredientDef> IngredientDefs => ingredientDefs;

        private List<IngredientKind> kinds;
        private int kindIndex;
        private IngredientGrade grade = IngredientGrade.C;
        private int quantity = MinQuantity;

        private void OnEnable()
        {
            ingredientPrevButton.onClick.AddListener(OnPrevKind);
            ingredientNextButton.onClick.AddListener(OnNextKind);
            gradeToggleButton.onClick.AddListener(OnToggleGrade);
            quantityMinusButton.onClick.AddListener(OnMinus);
            quantityPlusButton.onClick.AddListener(OnPlus);
            buyButton.onClick.AddListener(OnBuy);

            // 패널 재활성화(다음 날 Market 복귀) 시 최신 상태로 다시 그린다.
            if (kinds != null)
            {
                RefreshAll();
            }
        }

        private void OnDisable()
        {
            ingredientPrevButton.onClick.RemoveListener(OnPrevKind);
            ingredientNextButton.onClick.RemoveListener(OnNextKind);
            gradeToggleButton.onClick.RemoveListener(OnToggleGrade);
            quantityMinusButton.onClick.RemoveListener(OnMinus);
            quantityPlusButton.onClick.RemoveListener(OnPlus);
            buyButton.onClick.RemoveListener(OnBuy);
        }

        private void Start()
        {
            BuildKindList();
            if (messageText != null)
            {
                messageText.text = "";
            }
            RefreshAll();
        }

        /// <summary>주입된 defs 를 id 순으로 정렬해 종류 목록 구성 — 초기 선택은 첫 종류 + C 등급 (결정론).</summary>
        private void BuildKindList()
        {
            ingredientDefs.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            kinds = new List<IngredientKind>();
            foreach (var def in ingredientDefs)
            {
                if (!kinds.Contains(def.Kind))
                {
                    kinds.Add(def.Kind);
                }
            }
            kindIndex = 0;
            grade = IngredientGrade.C;
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
                int cost = EconomyOps.CalculatePurchaseCost(def, quantity);
                costText.text = $"예상 비용 {cost:N0}원";
            }
            if (ownedText != null && def != null)
            {
                int owned = inventory != null ? inventory.GetQuantity(def.Kind, def.Grade) : 0;
                ownedText.text = $"보유 {owned}개";
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
        /// <summary>SceneBuilder 전용 참조/데이터 주입 (EditorInit 패턴).</summary>
        internal void EditorInit(
            TMP_Text cashText,
            Button ingredientPrevButton, Button ingredientNextButton, TMP_Text ingredientLabel,
            Button gradeToggleButton, TMP_Text gradeLabel,
            Button quantityMinusButton, Button quantityPlusButton, TMP_Text quantityText,
            TMP_Text costText, TMP_Text ownedText, Button buyButton, TMP_Text messageText,
            List<IngredientDef> ingredientDefs)
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
        }
#endif
    }
}
