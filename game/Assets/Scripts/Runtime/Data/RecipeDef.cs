using System.Collections.Generic;
using UnityEngine;

namespace ClientIsKing.Data
{
    /// <summary>
    /// 레시피 정적 정의 — 소속 장르, 재료 요구량(종류+수량), 조리 시간, 기본 판매가.
    /// 재료의 C/B 등급 선택은 후속 경제/인벤토리 task 가 처리한다 (task-103 설계 3단계).
    /// </summary>
    [CreateAssetMenu(menuName = "Client is King/Recipe Def", fileName = "RecipeDef")]
    public sealed class RecipeDef : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private GenreDef genre;
        [SerializeField] private List<RecipeIngredientRequirement> ingredients = new List<RecipeIngredientRequirement>();
        [SerializeField, Min(0.1f)] private float cookSeconds;
        [SerializeField, Min(1)] private int basePrice;

        public string Id => id;
        public string DisplayName => displayName;
        /// <summary>소속 장르 — 항상 concrete 장르(국밥/분식/면류)만 참조한다.</summary>
        public GenreDef Genre => genre;
        public IReadOnlyList<RecipeIngredientRequirement> Ingredients => ingredients;
        /// <summary>기본 조리 시간(초) — 장르 배수 적용 전.</summary>
        public float CookSeconds => cookSeconds;
        /// <summary>기본 판매가(원) — 장르 객단가 배수 적용 전.</summary>
        public int BasePrice => basePrice;

#if UNITY_EDITOR
        internal void EditorInit(
            string id, string displayName, GenreDef genre,
            List<RecipeIngredientRequirement> ingredients, float cookSeconds, int basePrice)
        {
            this.id = id;
            this.displayName = displayName;
            this.genre = genre;
            this.ingredients = ingredients;
            this.cookSeconds = cookSeconds;
            this.basePrice = basePrice;
        }
#endif
    }
}
