using UnityEngine;

namespace ClientIsKing.Data
{
    /// <summary>
    /// 이벤트/장애물 정적 정의 — 발생 가중치·기간·효과 파라미터.
    /// 이벤트를 적용하는 매니저/스케줄러는 task-110 몫이다 (여기서는 데이터만).
    /// </summary>
    [CreateAssetMenu(menuName = "Client is King/Game Event Def", fileName = "GameEventDef")]
    public sealed class GameEventDef : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private GameEventKind kind;
        [SerializeField, Min(0f)] private float baseWeight;
        [SerializeField, Min(0)] private int durationDays;
        [SerializeField] private float percentEffect;
        [SerializeField] private int flatEffect;
        [SerializeField, TextArea] private string description;

        public string Id => id;
        public string DisplayName => displayName;
        public GameEventKind Kind => kind;
        /// <summary>발생 가중치 (추첨 로직은 task-110).</summary>
        public float BaseWeight => baseWeight;
        /// <summary>지속 일수. 0 = 영구 (예: 임대료 인상). 적용 규약은 task-110 에서 확정.</summary>
        public int DurationDays => durationDays;
        /// <summary>퍼센트 효과 (예: 0.35 = +35%). 의미는 이벤트 종류별로 task-110 에서 해석.</summary>
        public float PercentEffect => percentEffect;
        /// <summary>고정값 효과 (원 또는 인원 수 등, 종류별 해석).</summary>
        public int FlatEffect => flatEffect;
        public string Description => description;

#if UNITY_EDITOR
        internal void EditorInit(
            string id, string displayName, GameEventKind kind,
            float baseWeight, int durationDays, float percentEffect, int flatEffect, string description)
        {
            this.id = id;
            this.displayName = displayName;
            this.kind = kind;
            this.baseWeight = baseWeight;
            this.durationDays = durationDays;
            this.percentEffect = percentEffect;
            this.flatEffect = flatEffect;
            this.description = description;
        }
#endif
    }
}
