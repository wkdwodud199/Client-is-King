using System;
using System.Collections.Generic;

namespace ClientIsKing.Genre
{
    /// <summary>
    /// 익일 수요 계획에 합성되는 순수 DTO (task-110 D6/G2 예약 훅의 실체화, task-111 D1, task-112 D2).
    /// SNS 캠페인 효과와 이벤트(단체 손님) 수요 축을 Unity 타입 없이
    /// <see cref="GenreSelectionOps.TryBuildDemandPlan"/> 에 전달한다.
    /// </summary>
    public sealed class DayModifier
    {
        /// <summary>기존(task-111) 4-인자 생성자 — 이벤트 수요 축은 중립("", 0, 0)으로 위임한다(하위호환).</summary>
        public DayModifier(int day, string sourceCampaignId, int bonusOrderCount, IReadOnlyList<CustomerWeightBoost> weightBoosts)
            : this(day, sourceCampaignId, bonusOrderCount, weightBoosts, "", 0, 0)
        {
        }

        /// <summary>task-112 확장 생성자 — 이벤트 수요 축(EventSourceId/EventBonusOrderCount/EventPartySize) 포함.</summary>
        public DayModifier(
            int day, string sourceCampaignId, int bonusOrderCount, IReadOnlyList<CustomerWeightBoost> weightBoosts,
            string eventSourceId, int eventBonusOrderCount, int eventPartySize)
        {
            Day = day;
            SourceCampaignId = sourceCampaignId ?? "";
            BonusOrderCount = bonusOrderCount;
            WeightBoosts = weightBoosts ?? Array.Empty<CustomerWeightBoost>();
            EventSourceId = eventSourceId ?? "";
            EventBonusOrderCount = eventBonusOrderCount;
            EventPartySize = eventPartySize;
        }

        /// <summary>적용 대상 일차 — plan 이 생성되는 day 와 일치해야 한다.</summary>
        public int Day { get; }

        /// <summary>효과의 소스 캠페인 ID. 빈 문자열 = SNS 소스 없음(중립).</summary>
        public string SourceCampaignId { get; }

        /// <summary>익일 plan 에 추가되는 보너스 주문 수 (0~2).</summary>
        public int BonusOrderCount { get; }

        /// <summary>
        /// 고객별 채널 타겟 친화 배수(milli, 1000=중립). 빈 목록이면 전 고객 중립으로 간주한다(Neutral 전용 규약).
        /// 비어 있지 않으면 고객 archetype 전원을 정확히 1회씩 커버해야 한다.
        /// </summary>
        public IReadOnlyList<CustomerWeightBoost> WeightBoosts { get; }

        /// <summary>단체 손님 효과의 소스 이벤트 ID (task-112). 빈 문자열 = 단체 이벤트 없음.</summary>
        public string EventSourceId { get; }

        /// <summary>익일 plan 에 추가되는 이벤트 보너스 주문 수 (0 또는 1, task-112).</summary>
        public int EventBonusOrderCount { get; }

        /// <summary>단체 손님 파티 크기 override (0 = 없음, 있으면 2 이상, task-112).</summary>
        public int EventPartySize { get; }

        /// <summary>중립 modifier — bonus 0, boost 빈 목록(=전 고객 중립), 이벤트 축 전부 중립.</summary>
        public static DayModifier Neutral(int day)
        {
            return new DayModifier(day, "", 0, Array.Empty<CustomerWeightBoost>());
        }
    }

    /// <summary>고객 1종의 채널 타겟 친화 배수 (1000 = 중립, BoostMilli > 0 전제).</summary>
    public readonly struct CustomerWeightBoost
    {
        public CustomerWeightBoost(string customerId, int boostMilli)
        {
            CustomerId = customerId;
            BoostMilli = boostMilli;
        }

        public string CustomerId { get; }
        public int BoostMilli { get; }
    }
}
