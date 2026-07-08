using System.Collections.Generic;
using UnityEngine;

namespace ClientIsKing.Data
{
    /// <summary>
    /// SNS 캠페인 정적 정의 — 채널별 비용·도달률·반복 감쇠·타겟 친화 배수.
    /// 실제 마케팅 효과 계산(익일 손님 수/분포 변화)은 task-109 몫이다.
    /// </summary>
    [CreateAssetMenu(menuName = "Client is King/SNS Campaign Def", fileName = "SNSCampaignDef")]
    public sealed class SNSCampaignDef : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private SNSChannel channel;
        [SerializeField, Min(1)] private int baseCost;
        [SerializeField, Range(0f, 1f)] private float baseReach;
        [SerializeField, Range(0f, 1f)] private float repeatDecay;
        [SerializeField] private List<SNSAudienceAffinity> audienceAffinities = new List<SNSAudienceAffinity>();
        [SerializeField, TextArea] private string description;

        public string Id => id;
        public string DisplayName => displayName;
        public SNSChannel Channel => channel;
        /// <summary>1회 집행 비용(원).</summary>
        public int BaseCost => baseCost;
        /// <summary>기본 도달률 0~1.</summary>
        public float BaseReach => baseReach;
        /// <summary>반복 사용 시 효과 감쇠율 0~1 (수확체감).</summary>
        public float RepeatDecay => repeatDecay;
        /// <summary>연령/성별 타겟 친화 배수.</summary>
        public IReadOnlyList<SNSAudienceAffinity> AudienceAffinities => audienceAffinities;
        public string Description => description;

#if UNITY_EDITOR
        internal void EditorInit(
            string id, string displayName, SNSChannel channel,
            int baseCost, float baseReach, float repeatDecay,
            List<SNSAudienceAffinity> audienceAffinities, string description)
        {
            this.id = id;
            this.displayName = displayName;
            this.channel = channel;
            this.baseCost = baseCost;
            this.baseReach = baseReach;
            this.repeatDecay = repeatDecay;
            this.audienceAffinities = audienceAffinities;
            this.description = description;
        }
#endif
    }
}
