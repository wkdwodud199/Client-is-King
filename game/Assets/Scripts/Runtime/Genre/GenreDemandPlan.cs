using System;
using System.Collections.Generic;

namespace ClientIsKing.Genre
{
    /// <summary>
    /// 특정 genre/day 의 결정론적 수요 예측 — Unity 타입을 포함하지 않는 순수 C# DTO (task-110 U1, task-111 D2 확장).
    /// 같은 genreId/day/정의 목록이면 scalar 와 모든 ordered list 가 field-by-field 동등해야 한다 (design.md D6).
    /// </summary>
    public sealed class GenreDemandPlan
    {
        /// <summary>기존(task-110) 4-입력 호출 하위호환 생성자 — neutral 값(bonus 0, 빈 보너스 가중치)으로 위임한다.</summary>
        public GenreDemandPlan(
            string genreId, int day, int orderCount,
            IReadOnlyList<string> allowedRecipeIds,
            IReadOnlyList<CustomerWeightRow> customerWeights,
            IReadOnlyList<string> topCustomerIds,
            int minPricePerCustomer, int maxPricePerCustomer)
            : this(genreId, day, orderCount, 0, allowedRecipeIds, customerWeights,
                  Array.Empty<CustomerWeightRow>(), topCustomerIds, minPricePerCustomer, maxPricePerCustomer, "")
        {
        }

        /// <summary>task-111 확장 생성자 — Base/Bonus 주문 수·보너스 가중치·SourceCampaignId 를 포함한다.</summary>
        public GenreDemandPlan(
            string genreId, int day, int baseOrderCount, int bonusOrderCount,
            IReadOnlyList<string> allowedRecipeIds,
            IReadOnlyList<CustomerWeightRow> customerWeights,
            IReadOnlyList<CustomerWeightRow> bonusCustomerWeights,
            IReadOnlyList<string> topCustomerIds,
            int minPricePerCustomer, int maxPricePerCustomer,
            string sourceCampaignId)
        {
            GenreId = genreId ?? throw new ArgumentNullException(nameof(genreId));
            Day = day;
            BaseOrderCount = baseOrderCount;
            BonusOrderCount = bonusOrderCount;
            AllowedRecipeIds = allowedRecipeIds ?? throw new ArgumentNullException(nameof(allowedRecipeIds));
            CustomerWeights = customerWeights ?? throw new ArgumentNullException(nameof(customerWeights));
            BonusCustomerWeights = bonusCustomerWeights ?? Array.Empty<CustomerWeightRow>();
            TopCustomerIds = topCustomerIds ?? throw new ArgumentNullException(nameof(topCustomerIds));
            MinPricePerCustomer = minPricePerCustomer;
            MaxPricePerCustomer = maxPricePerCustomer;
            SourceCampaignId = sourceCampaignId ?? "";
        }

        public string GenreId { get; }
        public int Day { get; }

        /// <summary>장르 공식값 주문 수 (SNS 보너스 미포함).</summary>
        public int BaseOrderCount { get; }

        /// <summary>SNS 보너스 주문 수 (0~2, neutral 이면 0).</summary>
        public int BonusOrderCount { get; }

        /// <summary>총 주문 수 = BaseOrderCount + BonusOrderCount (기존 프로퍼티 의미 유지 — neutral 에서 기존 값과 동일).</summary>
        public int OrderCount => BaseOrderCount + BonusOrderCount;

        /// <summary>ID ordinal 오름차순으로 정렬된 허용 recipe ID 목록.</summary>
        public IReadOnlyList<string> AllowedRecipeIds { get; }

        /// <summary>ID ordinal 오름차순으로 정렬된 고객 milli-weight row 목록 (base 구간).</summary>
        public IReadOnlyList<CustomerWeightRow> CustomerWeights { get; }

        /// <summary>ID ordinal 오름차순으로 정렬된 보너스 구간 고객 milli-weight row 목록 (장르×채널 타겟 결합, neutral 이면 빈 목록).</summary>
        public IReadOnlyList<CustomerWeightRow> BonusCustomerWeights { get; }

        /// <summary>표시용 상위 고객 2종 (동률은 customer ID ordinal 오름차순으로 해결됨, base 기준 — SNS 무관).</summary>
        public IReadOnlyList<string> TopCustomerIds { get; }

        /// <summary>허용 recipe 의 1인 가격 최솟값 (party size 미포함, RoundHalfUp(basePrice × priceMultiplier)).</summary>
        public int MinPricePerCustomer { get; }

        /// <summary>허용 recipe 의 1인 가격 최댓값 (party size 미포함).</summary>
        public int MaxPricePerCustomer { get; }

        /// <summary>이 plan 의 보너스 주문을 발생시킨 SNS 캠페인 ID. 빈 문자열 = neutral(SNS 소스 없음).</summary>
        public string SourceCampaignId { get; }
    }

    /// <summary>고객 archetype 1건의 milli-weight row (양수 정수, RoundHalfUp(baseSpawnWeight × affinity × 1000)).</summary>
    public readonly struct CustomerWeightRow
    {
        public CustomerWeightRow(string customerId, int milliWeight)
        {
            CustomerId = customerId;
            MilliWeight = milliWeight;
        }

        public string CustomerId { get; }
        public int MilliWeight { get; }
    }
}
