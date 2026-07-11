using System;
using System.Collections.Generic;
using ClientIsKing.DayCycle;

namespace ClientIsKing.Genre
{
    /// <summary>
    /// 장르(전문 분야) 선택 게이트 + 결정론적 수요 예측 핵심 규칙 (순수 C# — EditMode 테스트 대상).
    /// Unity 타입을 참조하지 않는다 — 호출자(ServiceManager/GameManager)가 SO 정의를
    /// <see cref="GenreDefInput"/>/<see cref="RecipeDefInput"/>/<see cref="CustomerDefInput"/> 로 투영해 넘긴다.
    ///
    /// 계약 (task-110 design.md D5/D6/H5-H8):
    /// - 선택은 Day 1 Market, 첫 구매 전, 미선택 상태에서만 성공 — 그 외 전부 상태 불변 실패.
    /// - plan 생성은 잘못된 정의(null/중복 ID/매칭 recipe 없음/누락 affinity/multiplier ≤0·NaN·Infinity/
    ///   milli-weight 합 0)에서 명시적 실패다 — neutral fallback 으로 조용히 진행하지 않는다.
    /// - 모든 수학은 같은 입력에서 같은 결과 (RoundHalfUp, FNV-1a, ordinal 정렬).
    /// </summary>
    public static class GenreSelectionOps
    {
        /// <summary>양수 반올림 공통 helper — banker rounding 대신 항상 floor(x + 0.5) 를 사용한다.</summary>
        public static long RoundHalfUp(double x)
        {
            return (long)Math.Floor(x + 0.5);
        }

        // ── 선택 게이트 ─────────────────────────────────────────────────────

        /// <summary>
        /// 장르 선택 시도. Day 1 Market, 첫 구매 전(marketSpendToday == 0 && marketSpendDay != state.day 인 경우 포함),
        /// selectedGenreId 가 비어 있고, genreId 가 availableGenreIds 에 존재할 때만 성공한다.
        /// 실패는 state 를 절대 변경하지 않는다.
        /// </summary>
        public static GenreSelectionResult TrySelect(
            GameState state, string genreId, IReadOnlyList<string> availableGenreIds)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state), "GameState 없이 장르 선택을 할 수 없다");
            }
            if (!string.IsNullOrEmpty(state.selectedGenreId))
            {
                return Fail("이미 전문 분야를 선택했습니다.");
            }
            if (state.day != 1)
            {
                return Fail("전문 분야는 Day 1 에만 선택할 수 있습니다.");
            }
            if (state.currentPhase != DayPhase.Market)
            {
                return Fail("전문 분야는 Market phase 에서만 선택할 수 있습니다.");
            }
            // 첫 구매 전 게이트: marketSpendDay 가 오늘이고 지출이 이미 발생했으면 차단.
            if (state.marketSpendDay == state.day && state.marketSpendToday > 0)
            {
                return Fail("첫 구매 전에만 전문 분야를 선택할 수 있습니다.");
            }
            if (string.IsNullOrEmpty(genreId))
            {
                return Fail("알 수 없는 전문 분야입니다.");
            }
            if (availableGenreIds == null || !Contains(availableGenreIds, genreId))
            {
                return Fail("알 수 없는 전문 분야입니다.");
            }

            state.selectedGenreId = genreId;
            return new GenreSelectionResult(true, "전문 분야를 확정했습니다.", genreId);
        }

        static GenreSelectionResult Fail(string message)
        {
            return new GenreSelectionResult(false, message, "");
        }

        static bool Contains(IReadOnlyList<string> ids, string id)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                if (string.Equals(ids[i], id, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        // ── 주문 수 ─────────────────────────────────────────────────────────

        /// <summary>회전율 기반 하루 주문 수: clamp(RoundHalfUp(5 / cookTimeMultiplier), 4, 6).</summary>
        public static int CalculateOrderCount(float cookTimeMultiplier)
        {
            long rounded = RoundHalfUp(5.0 / cookTimeMultiplier);
            if (rounded < 4) return 4;
            if (rounded > 6) return 6;
            return (int)rounded;
        }

        // ── 수요 계획 ───────────────────────────────────────────────────────

        /// <summary>
        /// 결정론적 GenreDemandPlan 생성. specialist 는 matching recipe(recipe.GenreId == genre.Id) 만,
        /// generalist(kind==Generalist) 는 전체 recipe 를 후보로 사용한다.
        /// 잘못된 정의는 (null, failReason) 을 반환한다 — 상태를 바꾸지 않는다.
        /// </summary>
        public static bool TryBuildDemandPlan(
            GenreDefInput genre, int day,
            IReadOnlyList<RecipeDefInput> recipes,
            IReadOnlyList<CustomerDefInput> customers,
            out GenreDemandPlan plan, out string failReason)
        {
            plan = null;
            failReason = "";

            if (genre == null)
            {
                failReason = "장르 정의가 없습니다.";
                return false;
            }
            if (string.IsNullOrEmpty(genre.Id))
            {
                failReason = "장르 ID가 비어 있습니다.";
                return false;
            }
            if (genre.CookTimeMultiplier <= 0 || float.IsNaN(genre.CookTimeMultiplier) || float.IsInfinity(genre.CookTimeMultiplier))
            {
                failReason = "장르의 조리시간 배수가 잘못되었습니다.";
                return false;
            }
            if (genre.PricePerCustomerMultiplier <= 0 || float.IsNaN(genre.PricePerCustomerMultiplier) || float.IsInfinity(genre.PricePerCustomerMultiplier))
            {
                failReason = "장르의 객단가 배수가 잘못되었습니다.";
                return false;
            }
            if (recipes == null)
            {
                failReason = "레시피 정의가 없습니다.";
                return false;
            }
            if (customers == null)
            {
                failReason = "고객 정의가 없습니다.";
                return false;
            }

            // ── recipe 유효성 + 정렬 ──
            var recipeIds = new HashSet<string>(StringComparer.Ordinal);
            var allowedRecipes = new List<RecipeDefInput>();
            bool isGeneralist = genre.IsGeneralist;
            foreach (var recipe in recipes)
            {
                if (recipe == null || string.IsNullOrEmpty(recipe.Id))
                {
                    failReason = "잘못된 레시피 정의가 있습니다.";
                    return false;
                }
                if (!recipeIds.Add(recipe.Id))
                {
                    failReason = $"중복된 레시피 ID '{recipe.Id}'.";
                    return false;
                }
                if (isGeneralist || string.Equals(recipe.GenreId, genre.Id, StringComparison.Ordinal))
                {
                    allowedRecipes.Add(recipe);
                }
            }
            if (allowedRecipes.Count == 0)
            {
                failReason = $"'{genre.Id}' 장르에 매칭되는 레시피가 없습니다.";
                return false;
            }
            allowedRecipes.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

            // ── customer 유효성 + affinity + milli-weight ──
            var customerIds = new HashSet<string>(StringComparer.Ordinal);
            var sortedCustomers = new List<CustomerDefInput>();
            foreach (var customer in customers)
            {
                if (customer == null || string.IsNullOrEmpty(customer.Id))
                {
                    failReason = "잘못된 고객 정의가 있습니다.";
                    return false;
                }
                if (!customerIds.Add(customer.Id))
                {
                    failReason = $"중복된 고객 ID '{customer.Id}'.";
                    return false;
                }
                sortedCustomers.Add(customer);
            }
            sortedCustomers.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

            var affinityById = new Dictionary<string, float>(StringComparer.Ordinal);
            foreach (var affinity in genre.CustomerAffinities)
            {
                affinityById[affinity.CustomerId] = affinity.Multiplier;
            }

            var weights = new List<CustomerWeightRow>();
            long totalMilliWeight = 0;
            foreach (var customer in sortedCustomers)
            {
                if (!affinityById.TryGetValue(customer.Id, out var multiplier))
                {
                    failReason = $"고객 '{customer.Id}' 의 장르 친화도가 누락되었습니다.";
                    return false;
                }
                if (multiplier <= 0 || float.IsNaN(multiplier) || float.IsInfinity(multiplier))
                {
                    failReason = $"고객 '{customer.Id}' 의 친화도 배수가 잘못되었습니다.";
                    return false;
                }
                long milliWeight = RoundHalfUp((double)customer.BaseSpawnWeight * multiplier * 1000.0);
                if (milliWeight < 0)
                {
                    failReason = $"고객 '{customer.Id}' 의 milli-weight 가 음수입니다.";
                    return false;
                }
                weights.Add(new CustomerWeightRow(customer.Id, (int)milliWeight));
                totalMilliWeight += milliWeight;
            }
            if (totalMilliWeight <= 0)
            {
                failReason = "고객 milli-weight 합이 0입니다.";
                return false;
            }

            int orderCount = CalculateOrderCount(genre.CookTimeMultiplier);

            // ── forecast 1인 가격 min/max (party size 미포함) ──
            int minPrice = int.MaxValue;
            int maxPrice = int.MinValue;
            foreach (var recipe in allowedRecipes)
            {
                int perCustomerPrice = (int)RoundHalfUp((double)recipe.BasePrice * genre.PricePerCustomerMultiplier);
                if (perCustomerPrice < minPrice) minPrice = perCustomerPrice;
                if (perCustomerPrice > maxPrice) maxPrice = perCustomerPrice;
            }

            var allowedRecipeIds = new List<string>();
            foreach (var recipe in allowedRecipes)
            {
                allowedRecipeIds.Add(recipe.Id);
            }

            var topCustomerIds = BuildTopCustomers(weights, 2);

            plan = new GenreDemandPlan(
                genre.Id, day, orderCount, allowedRecipeIds, weights, topCustomerIds, minPrice, maxPrice);
            return true;
        }

        /// <summary>baseSpawnWeight × affinity(=milli-weight) 내림차순 상위 N종. 동률은 customer ID ordinal 오름차순.</summary>
        static List<string> BuildTopCustomers(List<CustomerWeightRow> weights, int count)
        {
            var sorted = new List<CustomerWeightRow>(weights);
            sorted.Sort((a, b) =>
            {
                int byWeight = b.MilliWeight.CompareTo(a.MilliWeight);
                return byWeight != 0 ? byWeight : string.CompareOrdinal(a.CustomerId, b.CustomerId);
            });
            var result = new List<string>();
            for (int i = 0; i < sorted.Count && i < count; i++)
            {
                result.Add(sorted[i].CustomerId);
            }
            return result;
        }

        // ── 주문 생성 (recipe round-robin + party size + 고객 선택) ────────

        /// <summary>정렬된 허용 recipe 목록에서 (day-1+orderIndex) % recipeCount 로 선택.</summary>
        public static string PickRecipeId(GenreDemandPlan plan, int orderIndex)
        {
            var recipes = plan.AllowedRecipeIds;
            int index = (plan.Day - 1 + orderIndex) % recipes.Count;
            return recipes[index];
        }

        /// <summary>partySize = min + (day-1+orderIndex) % span.</summary>
        public static int PickPartySize(int day, int orderIndex, int minPartySize, int maxPartySize)
        {
            int min = Math.Max(1, minPartySize);
            int span = Math.Max(1, maxPartySize - min + 1);
            return min + (day - 1 + orderIndex) % span;
        }

        /// <summary>
        /// 고객 roll seed = FNV-1a("genreId|day|orderIndex"), roll = seed % totalMilliWeight,
        /// 누적합이 roll 을 처음 초과하는 고객을 선택 (design.md D6/H8 계약).
        /// </summary>
        public static string PickCustomerId(GenreDemandPlan plan, int orderIndex)
        {
            long total = 0;
            foreach (var row in plan.CustomerWeights)
            {
                total += row.MilliWeight;
            }
            if (total <= 0)
            {
                throw new InvalidOperationException("GenreDemandPlan 의 총 milli-weight 가 0 이하입니다.");
            }

            uint seed = Fnv1a($"{plan.GenreId}|{plan.Day}|{orderIndex}");
            long roll = (long)(seed % (ulong)total);

            long cumulative = 0;
            foreach (var row in plan.CustomerWeights)
            {
                cumulative += row.MilliWeight;
                if (cumulative > roll)
                {
                    return row.CustomerId;
                }
            }
            // 부동소수 없는 정수 누적이므로 도달 불가 — 방어적으로 마지막 고객 반환.
            return plan.CustomerWeights[plan.CustomerWeights.Count - 1].CustomerId;
        }

        const uint FnvOffsetBasis = 2166136261;
        const uint FnvPrime = 16777619;

        /// <summary>32-bit FNV-1a, UTF-8 바이트 기준, unchecked overflow 허용 (known vector: "gukbap|1|0" → 2190636514).</summary>
        public static uint Fnv1a(string text)
        {
            unchecked
            {
                uint hash = FnvOffsetBasis;
                var bytes = System.Text.Encoding.UTF8.GetBytes(text);
                for (int i = 0; i < bytes.Length; i++)
                {
                    hash ^= bytes[i];
                    hash *= FnvPrime;
                }
                return hash;
            }
        }
    }

    /// <summary>GenreSelectionOps 입력용 순수 장르 정의 투영 — Unity 타입 없음.</summary>
    public sealed class GenreDefInput
    {
        public string Id;
        public bool IsGeneralist;
        public float CookTimeMultiplier;
        public float PricePerCustomerMultiplier;
        public IReadOnlyList<GenreAffinityInput> CustomerAffinities = Array.Empty<GenreAffinityInput>();
    }

    public readonly struct GenreAffinityInput
    {
        public GenreAffinityInput(string customerId, float multiplier)
        {
            CustomerId = customerId;
            Multiplier = multiplier;
        }

        public string CustomerId { get; }
        public float Multiplier { get; }
    }

    /// <summary>GenreSelectionOps 입력용 순수 레시피 정의 투영 — Unity 타입 없음.</summary>
    public sealed class RecipeDefInput
    {
        public string Id;
        /// <summary>이 레시피가 속한 장르의 GenreDef.Id (제네럴리스트 매칭에는 사용하지 않음).</summary>
        public string GenreId;
        public int BasePrice;
    }

    /// <summary>GenreSelectionOps 입력용 순수 고객 정의 투영 — Unity 타입 없음.</summary>
    public sealed class CustomerDefInput
    {
        public string Id;
        public float BaseSpawnWeight;
    }
}
