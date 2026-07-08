using UnityEngine;

namespace ClientIsKing.Data
{
    /// <summary>
    /// 재료 정적 정의 — 종류×등급(C/B)별 구매 비용과 품질.
    /// 데이터 컨테이너로만 유지한다 (구매/조리 로직 금지 — task-103 제약).
    /// </summary>
    [CreateAssetMenu(menuName = "Client is King/Ingredient Def", fileName = "IngredientDef")]
    public sealed class IngredientDef : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private IngredientKind kind;
        [SerializeField] private IngredientGrade grade;
        [SerializeField, Min(0)] private int unitCost;
        [SerializeField, Range(0f, 1f)] private float quality;

        /// <summary>안정적인 ASCII 식별자 (파일명/표시명 대신 검증·조회 기준).</summary>
        public string Id => id;
        public string DisplayName => displayName;
        public IngredientKind Kind => kind;
        public IngredientGrade Grade => grade;
        /// <summary>단위 구매 비용 (원).</summary>
        public int UnitCost => unitCost;
        /// <summary>품질 0~1 (등급이 높을수록 큼).</summary>
        public float Quality => quality;

#if UNITY_EDITOR
        internal void EditorInit(
            string id, string displayName, IngredientKind kind,
            IngredientGrade grade, int unitCost, float quality)
        {
            this.id = id;
            this.displayName = displayName;
            this.kind = kind;
            this.grade = grade;
            this.unitCost = unitCost;
            this.quality = quality;
        }
#endif
    }
}
