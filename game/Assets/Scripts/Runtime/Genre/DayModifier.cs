using System;
using System.Collections.Generic;

namespace ClientIsKing.Genre
{
    /// <summary>
    /// 익일 수요 계획에 합성되는 순수 DTO (task-110 D6/G2 예약 훅의 실체화, task-111 D1).
    /// SNS 캠페인 효과를 Unity 타입 없이 <see cref="GenreSelectionOps.TryBuildDemandPlan"/> 에 전달한다.
    /// task-112 이벤트 modifier 는 이 DTO 에 합성 소스를 추가하는 별도 설계로 확장한다(오픈 이슈).
    /// </summary>
    public sealed class DayModifier
    {
        public DayModifier(int day, string sourceCampaignId, int bonusOrderCount, IReadOnlyList<CustomerWeightBoost> weightBoosts)
        {
            Day = day;
            SourceCampaignId = sourceCampaignId ?? "";
            BonusOrderCount = bonusOrderCount;
            WeightBoosts = weightBoosts ?? Array.Empty<CustomerWeightBoost>();
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

        /// <summary>중립 modifier — bonus 0, boost 빈 목록(=전 고객 중립).</summary>
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
