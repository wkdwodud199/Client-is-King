using System.Collections.Generic;

namespace ClientIsKing.Social
{
    /// <summary>SNSCampaignOps.TryExecute 의 결과 DTO — 순수 C#, Unity 타입 없음 (task-111 E1).</summary>
    public sealed class SNSCampaignResult
    {
        public SNSCampaignResult(bool success, string message, string campaignId,
            int costPaid, int effectiveMilliReach, int bonusOrderCount, int followerGain, int cashAfter)
        {
            Success = success;
            Message = message ?? "";
            CampaignId = campaignId ?? "";
            CostPaid = costPaid;
            EffectiveMilliReach = effectiveMilliReach;
            BonusOrderCount = bonusOrderCount;
            FollowerGain = followerGain;
            CashAfter = cashAfter;
        }

        public bool Success { get; }
        public string Message { get; }
        public string CampaignId { get; }
        public int CostPaid { get; }
        public int EffectiveMilliReach { get; }
        public int BonusOrderCount { get; }
        public int FollowerGain { get; }
        public int CashAfter { get; }
    }

    /// <summary>SNSCampaignOps.TryBuildPreview 의 결과 DTO — 집행 없이 상태 불변 미리보기 (task-111 E1).</summary>
    public sealed class SNSCampaignPreview
    {
        public SNSCampaignPreview(string campaignId, int cost, int effectiveMilliReach,
            int bonusOrderCount, int followerGain, IReadOnlyList<string> topTargetCustomerIds,
            bool canExecute, string blockReason)
        {
            CampaignId = campaignId ?? "";
            Cost = cost;
            EffectiveMilliReach = effectiveMilliReach;
            BonusOrderCount = bonusOrderCount;
            FollowerGain = followerGain;
            TopTargetCustomerIds = topTargetCustomerIds ?? System.Array.Empty<string>();
            CanExecute = canExecute;
            BlockReason = blockReason ?? "";
        }

        public string CampaignId { get; }
        public int Cost { get; }
        public int EffectiveMilliReach { get; }
        public int BonusOrderCount { get; }
        public int FollowerGain { get; }

        /// <summary>채널 주 타겟 상위 2종 (배수 내림차순, 동률은 customer ID ordinal 오름차순) — 순수 helper 산출.</summary>
        public IReadOnlyList<string> TopTargetCustomerIds { get; }

        /// <summary>게이트 3~6 통과 여부(파산/Night/오늘 집행/자금) — false 여도 def/customers 무결성은 이미 통과한 상태.</summary>
        public bool CanExecute { get; }

        /// <summary>CanExecute == false 일 때의 한국어 사유 (게이트와 동일 문구).</summary>
        public string BlockReason { get; }
    }
}
