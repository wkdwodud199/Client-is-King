using UnityEngine;

namespace ClientIsKing.Data
{
    /// <summary>
    /// 고객층 정적 정의 — 인구통계·출현 가중치·인내/가격 민감도·파티 크기.
    /// 손님 생성/분포 계산 로직의 입력값일 뿐, 생성 로직은 넣지 않는다 (task-103 설계 5단계).
    /// </summary>
    [CreateAssetMenu(menuName = "Client is King/Customer Archetype Def", fileName = "CustomerArchetypeDef")]
    public sealed class CustomerArchetypeDef : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private AgeBand ageBand;
        [SerializeField] private GenderTarget gender;
        [SerializeField, Min(0f)] private float baseSpawnWeight;
        [SerializeField, Min(1f)] private float patienceSeconds;
        [SerializeField, Range(0f, 2f)] private float priceSensitivity;
        [SerializeField] private IntRange partySize;

        public string Id => id;
        public string DisplayName => displayName;
        public AgeBand AgeBand => ageBand;
        public GenderTarget Gender => gender;
        /// <summary>기본 출현 가중치 (SNS 효과 적용 전).</summary>
        public float BaseSpawnWeight => baseSpawnWeight;
        /// <summary>대기 인내 시간(초).</summary>
        public float PatienceSeconds => patienceSeconds;
        /// <summary>가격 민감도 (높을수록 비싼 메뉴 기피).</summary>
        public float PriceSensitivity => priceSensitivity;
        /// <summary>파티 크기 범위 (min≥1).</summary>
        public IntRange PartySize => partySize;

#if UNITY_EDITOR
        internal void EditorInit(
            string id, string displayName, AgeBand ageBand, GenderTarget gender,
            float baseSpawnWeight, float patienceSeconds, float priceSensitivity, IntRange partySize)
        {
            this.id = id;
            this.displayName = displayName;
            this.ageBand = ageBand;
            this.gender = gender;
            this.baseSpawnWeight = baseSpawnWeight;
            this.patienceSeconds = patienceSeconds;
            this.priceSensitivity = priceSensitivity;
            this.partySize = partySize;
        }
#endif
    }
}
