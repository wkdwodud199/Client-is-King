using System;

namespace ClientIsKing.Social
{
    /// <summary>
    /// SNS 캠페인 집행 1건의 저장 가능한 상태 (task-111 B3).
    /// 집행 시점의 약속을 고정한다 — 익일 plan 은 이 레코드의 <see cref="bonusOrderCount"/> 를 그대로
    /// 사용해 "집행 시 예고한 유입 = 실제 유입"을 보장하고, def 값이 도중에 바뀌어도 약속이 깨지지 않는다.
    /// public 필드의 [Serializable] 클래스 — JsonUtility 호환(Dictionary/SO 참조 없음).
    /// </summary>
    [Serializable]
    public sealed class SNSCampaignRecord
    {
        /// <summary>SNSCampaignDef.Id (문자열 ID 규약).</summary>
        public string campaignId = "";

        /// <summary>집행한 날 — 효과는 executedOnDay + 1 에 적용된다.</summary>
        public int executedOnDay = 0;

        /// <summary>집행 시점 실제 차감액(원).</summary>
        public int costPaid = 0;

        /// <summary>집행 시점 감쇠 적용 도달 (밀리 정수, C2).</summary>
        public int effectiveMilliReach = 0;

        /// <summary>집행 시점 확정 보너스 주문 수 0~2 (C3).</summary>
        public int bonusOrderCount = 0;

        /// <summary>집행 시점 팔로워 획득 (C3).</summary>
        public int followerGain = 0;
    }
}
