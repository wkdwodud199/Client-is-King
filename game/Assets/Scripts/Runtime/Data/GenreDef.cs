using System.Collections.Generic;
using UnityEngine;

namespace ClientIsKing.Data
{
    /// <summary>
    /// 한식 장르 정적 정의 — 원가·조리시간·객단가·고객층 친화도 배수 (트레이드오프).
    /// 제네럴리스트는 균형형 선택지이며 레시피의 직접 장르로는 쓰지 않는다.
    /// </summary>
    [CreateAssetMenu(menuName = "Client is King/Genre Def", fileName = "GenreDef")]
    public sealed class GenreDef : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private GenreKind kind;
        [SerializeField, Min(0.1f)] private float costMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float cookTimeMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float pricePerCustomerMultiplier = 1f;
        [SerializeField] private List<CustomerGenreAffinity> customerAffinities = new List<CustomerGenreAffinity>();
        [SerializeField, TextArea] private string description;

        public string Id => id;
        public string DisplayName => displayName;
        public GenreKind Kind => kind;
        /// <summary>재료 원가 배수.</summary>
        public float CostMultiplier => costMultiplier;
        /// <summary>조리 시간 배수.</summary>
        public float CookTimeMultiplier => cookTimeMultiplier;
        /// <summary>객단가 배수.</summary>
        public float PricePerCustomerMultiplier => pricePerCustomerMultiplier;
        /// <summary>고객 archetype 별 친화도 배수.</summary>
        public IReadOnlyList<CustomerGenreAffinity> CustomerAffinities => customerAffinities;
        public string Description => description;

#if UNITY_EDITOR
        internal void EditorInit(
            string id, string displayName, GenreKind kind,
            float costMultiplier, float cookTimeMultiplier, float pricePerCustomerMultiplier,
            List<CustomerGenreAffinity> customerAffinities, string description)
        {
            this.id = id;
            this.displayName = displayName;
            this.kind = kind;
            this.costMultiplier = costMultiplier;
            this.cookTimeMultiplier = cookTimeMultiplier;
            this.pricePerCustomerMultiplier = pricePerCustomerMultiplier;
            this.customerAffinities = customerAffinities;
            this.description = description;
        }
#endif
    }
}
