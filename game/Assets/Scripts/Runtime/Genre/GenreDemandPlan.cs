using System;
using System.Collections.Generic;

namespace ClientIsKing.Genre
{
    /// <summary>
    /// 특정 genre/day 의 결정론적 수요 예측 — Unity 타입을 포함하지 않는 순수 C# DTO (task-110 U1).
    /// 같은 genreId/day/정의 목록이면 scalar 와 모든 ordered list 가 field-by-field 동등해야 한다 (design.md D6).
    /// </summary>
    public sealed class GenreDemandPlan
    {
        public GenreDemandPlan(
            string genreId, int day, int orderCount,
            IReadOnlyList<string> allowedRecipeIds,
            IReadOnlyList<CustomerWeightRow> customerWeights,
            IReadOnlyList<string> topCustomerIds,
            int minPricePerCustomer, int maxPricePerCustomer)
        {
            GenreId = genreId ?? throw new ArgumentNullException(nameof(genreId));
            Day = day;
            OrderCount = orderCount;
            AllowedRecipeIds = allowedRecipeIds ?? throw new ArgumentNullException(nameof(allowedRecipeIds));
            CustomerWeights = customerWeights ?? throw new ArgumentNullException(nameof(customerWeights));
            TopCustomerIds = topCustomerIds ?? throw new ArgumentNullException(nameof(topCustomerIds));
            MinPricePerCustomer = minPricePerCustomer;
            MaxPricePerCustomer = maxPricePerCustomer;
        }

        public string GenreId { get; }
        public int Day { get; }
        public int OrderCount { get; }

        /// <summary>ID ordinal 오름차순으로 정렬된 허용 recipe ID 목록.</summary>
        public IReadOnlyList<string> AllowedRecipeIds { get; }

        /// <summary>ID ordinal 오름차순으로 정렬된 고객 milli-weight row 목록.</summary>
        public IReadOnlyList<CustomerWeightRow> CustomerWeights { get; }

        /// <summary>표시용 상위 고객 2종 (동률은 customer ID ordinal 오름차순으로 해결됨).</summary>
        public IReadOnlyList<string> TopCustomerIds { get; }

        /// <summary>허용 recipe 의 1인 가격 최솟값 (party size 미포함, RoundHalfUp(basePrice × priceMultiplier)).</summary>
        public int MinPricePerCustomer { get; }

        /// <summary>허용 recipe 의 1인 가격 최댓값 (party size 미포함).</summary>
        public int MaxPricePerCustomer { get; }
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
