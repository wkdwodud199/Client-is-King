using System;
using System.Collections.Generic;
using ClientIsKing.Data;
using ClientIsKing.DayCycle;
using ClientIsKing.Genre;

namespace ClientIsKing.Social
{
    /// <summary>
    /// SNS 마케팅 핵심 규칙 (순수 C# — EditMode 테스트 대상). Unity 타입을 참조하지 않는다 —
    /// 호출자(ServiceManager)가 SO 정의를 <see cref="SNSCampaignDefInput"/>/<see cref="SNSAffinityInput"/> 로
    /// 투영해 넘긴다 (단, <c>ClientIsKing.Data</c>의 순수 enum AgeBand/GenderTarget 은 허용).
    ///
    /// 계약 (task-111 design.md C1~C5/D4/E1):
    /// - 밀리 투영: SO float 필드는 투영 DTO 생성 시 한 번만 밀리 정수화하고 이후 전 구간 정수 연산한다.
    /// - 반복 감쇠는 정수 fold 체인, 보너스 주문·팔로워는 정수 나눗셈 공식.
    /// - 잘못된 정의·상태는 명시적 실패다(neutral fallback 금지) — 실패 경로는 GameState 를 완전히 불변으로 유지한다.
    /// - top-target 계산은 이 클래스(TryBuildPreview)의 단일 non-UI 경로에서만 수행한다(Codex 리뷰 반영).
    /// </summary>
    public static class SNSCampaignOps
    {
        const int NeutralMilli = 1000;

        /// <summary>MulMilliHalfUp(a, b) = (a × b + 500) / 1000 — RoundHalfUp(a×b/1000) 의 정수 동치 (비음수 전제).</summary>
        public static int MulMilliHalfUp(int a, int b)
        {
            return (int)((a * (long)b + 500) / 1000);
        }

        // ── C1: 밀리 투영 ───────────────────────────────────────────────────

        /// <summary>SO float 필드를 투영 경계에서 한 번만 밀리 정수화한다: RoundHalfUp(x × 1000).</summary>
        public static int ProjectMilli(float value)
        {
            return (int)GenreSelectionOps.RoundHalfUp((double)value * 1000.0);
        }

        // ── C2: 반복 감쇠 체인 ──────────────────────────────────────────────

        /// <summary>
        /// n 회차(0-based) 유효 도달 밀리 정수. n=0 은 RoundHalfUp(baseReach×1000),
        /// n≥1 은 이전 값에 decayMilli 를 정수 fold 로 반복 적용한다 (Math.Pow 금지).
        /// </summary>
        public static int CalculateEffectiveMilliReach(int reachMilli0, int decayMilli, int priorUses)
        {
            int reach = reachMilli0;
            for (int i = 0; i < priorUses; i++)
            {
                reach = MulMilliHalfUp(reach, decayMilli);
            }
            return reach;
        }

        // ── C3: 보너스 주문 수 · 팔로워 획득 ────────────────────────────────

        /// <summary>clamp((6 × reachMilli + 500) / 1000, 0, 2) — 정수 나눗셈.</summary>
        public static int CalculateBonusOrderCount(int reachMilli)
        {
            long bonus = (6L * reachMilli + 500) / 1000;
            if (bonus < 0) return 0;
            if (bonus > 2) return 2;
            return (int)bonus;
        }

        /// <summary>RoundHalfUp(reachMilli / 10) = (reachMilli + 5) / 10.</summary>
        public static int CalculateFollowerGain(int reachMilli)
        {
            return (reachMilli + 5) / 10;
        }

        /// <summary>팔로워 표시값 = 120 + Σ history.followerGain (표시 전용, 규칙 입력 아님).</summary>
        public static int CalculateFollowerDisplay(IReadOnlyList<SNSCampaignRecord> history)
        {
            int total = 120;
            if (history != null)
            {
                foreach (var record in history)
                {
                    if (record != null)
                    {
                        total += record.followerGain;
                    }
                }
            }
            return total;
        }

        // ── C4: 연령·성별 타겟 매칭 ─────────────────────────────────────────

        /// <summary>
        /// 고객 archetype 에 대한 채널 친화 배수(milli). 매칭 행이 없으면 1000(중립),
        /// 매칭 행이 있으면 매칭 행 affinityMilli 의 최댓값. def 안 (AgeBand,Gender) 중복 행은 호출자가 사전 검증한다.
        /// </summary>
        public static int CalculateAffinityMilli(AgeBand customerAgeBand, GenderTarget customerGender, IReadOnlyList<SNSAffinityInput> rows)
        {
            int best = NeutralMilli;
            bool matched = false;
            if (rows != null)
            {
                foreach (var row in rows)
                {
                    if (row.AgeBand != customerAgeBand)
                    {
                        continue;
                    }
                    bool genderCompatible = row.Gender == GenderTarget.All
                        || customerGender == GenderTarget.All
                        || row.Gender == customerGender;
                    if (!genderCompatible)
                    {
                        continue;
                    }
                    if (!matched || row.MultiplierMilli > best)
                    {
                        best = row.MultiplierMilli;
                    }
                    matched = true;
                }
            }
            return matched ? best : NeutralMilli;
        }

        /// <summary>def 내 (AgeBand,Gender) 중복 행이 있으면 true.</summary>
        static bool HasDuplicateAudienceRow(IReadOnlyList<SNSAffinityInput> rows)
        {
            if (rows == null)
            {
                return false;
            }
            var seen = new HashSet<(AgeBand, GenderTarget)>();
            foreach (var row in rows)
            {
                if (!seen.Add((row.AgeBand, row.Gender)))
                {
                    return true;
                }
            }
            return false;
        }

        // ── def/customers 무결성 검증 (공용 — 게이트 2 / preview) ───────────

        static bool IsDefInvalid(SNSCampaignDefInput def, out string failReason)
        {
            failReason = "";
            if (def == null || string.IsNullOrEmpty(def.Id))
            {
                failReason = "잘못된 SNS 캠페인 정의입니다.";
                return true;
            }
            if (def.BaseCost <= 0)
            {
                failReason = "잘못된 SNS 캠페인 정의입니다.";
                return true;
            }
            if (def.BaseReach <= 0f || def.BaseReach > 1f || float.IsNaN(def.BaseReach) || float.IsInfinity(def.BaseReach))
            {
                failReason = "잘못된 SNS 캠페인 정의입니다.";
                return true;
            }
            if (def.RepeatDecay <= 0f || def.RepeatDecay > 1f || float.IsNaN(def.RepeatDecay) || float.IsInfinity(def.RepeatDecay))
            {
                failReason = "잘못된 SNS 캠페인 정의입니다.";
                return true;
            }
            foreach (var row in def.AudienceAffinities)
            {
                if (row.Multiplier <= 0f || float.IsNaN(row.Multiplier) || float.IsInfinity(row.Multiplier))
                {
                    failReason = "잘못된 SNS 캠페인 정의입니다.";
                    return true;
                }
            }
            if (HasDuplicateAudienceRow(ProjectAffinityRows(def.AudienceAffinities)))
            {
                failReason = "잘못된 SNS 캠페인 정의입니다.";
                return true;
            }
            return false;
        }

        static List<SNSAffinityInput> ProjectAffinityRows(IReadOnlyList<SNSRawAffinityInput> rows)
        {
            var result = new List<SNSAffinityInput>();
            if (rows != null)
            {
                foreach (var row in rows)
                {
                    result.Add(new SNSAffinityInput(row.AgeBand, row.Gender, ProjectMilli(row.Multiplier)));
                }
            }
            return result;
        }

        static bool IsCustomersInvalid(IReadOnlyList<CustomerDefInput> customers, out string failReason)
        {
            failReason = "";
            if (customers == null || customers.Count == 0)
            {
                failReason = "고객 정의가 없습니다.";
                return true;
            }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var customer in customers)
            {
                if (customer == null || string.IsNullOrEmpty(customer.Id))
                {
                    failReason = "잘못된 고객 정의가 있습니다.";
                    return true;
                }
                if (!seen.Add(customer.Id))
                {
                    failReason = $"중복된 고객 ID '{customer.Id}'.";
                    return true;
                }
            }
            return false;
        }

        // ── E1: 집행 게이트 ─────────────────────────────────────────────────

        /// <summary>
        /// SNS 캠페인 집행 시도. 검사 순서(모두 상태 완전 불변): def 무결성 → 파산 → Night phase →
        /// 오늘 밤 이미 집행 → 자금 부족. 성공 시 원자적으로 cash 차감 + 레코드 append.
        /// </summary>
        public static SNSCampaignResult TryExecute(GameState state, SNSCampaignDefInput def)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state), "GameState 없이 SNS 캠페인을 집행할 수 없다");
            }
            if (IsDefInvalid(def, out var defFail))
            {
                return Fail(defFail, def);
            }
            if (state.isBankrupt)
            {
                return Fail("파산 상태에서는 캠페인을 집행할 수 없습니다.", def);
            }
            if (state.currentPhase != DayPhase.Night)
            {
                return Fail("SNS 캠페인은 밤에만 집행할 수 있습니다.", def);
            }
            if (HasExecutedTonight(state.snsCampaignHistory, state.day))
            {
                return Fail("오늘 밤 캠페인은 이미 집행했습니다.", def);
            }
            if (state.cash < def.BaseCost)
            {
                return Fail($"자금이 부족합니다 (필요 {def.BaseCost:N0}원, 보유 {state.cash:N0}원).", def);
            }

            int priorUses = CountPriorUses(state.snsCampaignHistory, def.Id);
            int reachMilli0 = ProjectMilli(def.BaseReach);
            int decayMilli = ProjectMilli(def.RepeatDecay);
            int effectiveReach = CalculateEffectiveMilliReach(reachMilli0, decayMilli, priorUses);
            int bonusOrders = CalculateBonusOrderCount(effectiveReach);
            int followerGain = CalculateFollowerGain(effectiveReach);

            state.cash -= def.BaseCost;
            state.snsCampaignHistory.Add(new SNSCampaignRecord
            {
                campaignId = def.Id,
                executedOnDay = state.day,
                costPaid = def.BaseCost,
                effectiveMilliReach = effectiveReach,
                bonusOrderCount = bonusOrders,
                followerGain = followerGain,
            });

            string message = $"{def.DisplayName} 집행 — 내일 SNS 유입 +{bonusOrders}팀 예상 (팔로워 +{followerGain})";
            return new SNSCampaignResult(true, message, def.Id, def.BaseCost, effectiveReach, bonusOrders, followerGain, state.cash);
        }

        static SNSCampaignResult Fail(string message, SNSCampaignDefInput def)
        {
            return new SNSCampaignResult(false, message, def != null ? def.Id : "", 0, 0, 0, 0, 0);
        }

        static bool HasExecutedTonight(List<SNSCampaignRecord> history, int day)
        {
            if (history == null)
            {
                return false;
            }
            foreach (var record in history)
            {
                if (record.executedOnDay == day)
                {
                    return true;
                }
            }
            return false;
        }

        static int CountPriorUses(List<SNSCampaignRecord> history, string campaignId)
        {
            int count = 0;
            if (history != null)
            {
                foreach (var record in history)
                {
                    if (string.Equals(record.campaignId, campaignId, StringComparison.Ordinal))
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        // ── E1: 미리보기 (집행 없이 상태 불변, top-target 포함 단일 non-UI 경로) ──

        /// <summary>
        /// 집행 없이 상태 불변 미리보기를 계산한다. def/customers 무결성 위반은 명시적 실패(failReason),
        /// 게이트 3~6(파산/Night 아님/오늘 이미 집행/자금 부족)은 실패가 아니라 CanExecute=false + BlockReason 으로 구분한다.
        /// TopTargetCustomerIds 는 이 helper 가 customers 투영 입력에 C4 매칭을 적용해 계산한다 — UI/매니저 재계산 금지.
        /// </summary>
        public static bool TryBuildPreview(
            GameState state, SNSCampaignDefInput def, IReadOnlyList<CustomerDefInput> customers,
            out SNSCampaignPreview preview, out string failReason)
        {
            preview = null;
            failReason = "";

            if (state == null)
            {
                throw new ArgumentNullException(nameof(state), "GameState 없이 SNS 미리보기를 계산할 수 없다");
            }
            if (IsDefInvalid(def, out failReason))
            {
                return false;
            }
            if (IsCustomersInvalid(customers, out failReason))
            {
                return false;
            }

            bool canExecute = true;
            string blockReason = "";
            if (state.isBankrupt)
            {
                canExecute = false;
                blockReason = "파산 상태에서는 캠페인을 집행할 수 없습니다.";
            }
            else if (state.currentPhase != DayPhase.Night)
            {
                canExecute = false;
                blockReason = "SNS 캠페인은 밤에만 집행할 수 있습니다.";
            }
            else if (HasExecutedTonight(state.snsCampaignHistory, state.day))
            {
                canExecute = false;
                blockReason = "오늘 밤 캠페인은 이미 집행했습니다.";
            }
            else if (state.cash < def.BaseCost)
            {
                canExecute = false;
                blockReason = $"자금이 부족합니다 (필요 {def.BaseCost:N0}원, 보유 {state.cash:N0}원).";
            }

            int priorUses = CountPriorUses(state.snsCampaignHistory, def.Id);
            int reachMilli0 = ProjectMilli(def.BaseReach);
            int decayMilli = ProjectMilli(def.RepeatDecay);
            int effectiveReach = CalculateEffectiveMilliReach(reachMilli0, decayMilli, priorUses);
            int bonusOrders = CalculateBonusOrderCount(effectiveReach);
            int followerGain = CalculateFollowerGain(effectiveReach);

            var affinityRows = ProjectAffinityRows(def.AudienceAffinities);
            var topTargets = BuildTopTargetCustomerIds(affinityRows, customers, 2);

            preview = new SNSCampaignPreview(
                def.Id, def.BaseCost, effectiveReach, bonusOrders, followerGain, topTargets, canExecute, blockReason);
            return true;
        }

        /// <summary>
        /// 채널 주 타겟 상위 N종: snsAffinityMilli(c) > 1000 인 고객을 배수 내림차순, 동률은 customer ID ordinal 오름차순.
        /// customers 투영 입력을 받는 이 순수 helper 가 유일한 계산 경로다(UI/매니저 재계산 금지, Codex 리뷰 반영).
        /// </summary>
        static List<string> BuildTopTargetCustomerIds(IReadOnlyList<SNSAffinityInput> affinityRows, IReadOnlyList<CustomerDefInput> customers, int count)
        {
            var candidates = new List<(string id, int affinity)>();
            foreach (var customer in customers)
            {
                int affinity = CalculateAffinityMilli(customer.AgeBand, customer.Gender, affinityRows);
                if (affinity > NeutralMilli)
                {
                    candidates.Add((customer.Id, affinity));
                }
            }
            candidates.Sort((a, b) =>
            {
                int byAffinity = b.affinity.CompareTo(a.affinity);
                return byAffinity != 0 ? byAffinity : string.CompareOrdinal(a.id, b.id);
            });
            var result = new List<string>();
            for (int i = 0; i < candidates.Count && i < count; i++)
            {
                result.Add(candidates[i].id);
            }
            return result;
        }

        // ── D4: history → DayModifier 재구성 ────────────────────────────────

        /// <summary>
        /// 전날(day-1) 집행 레코드로부터 결정론적으로 DayModifier 를 재구성한다.
        /// 레코드가 0건이면 Neutral 성공, 1건이면 저장값(재계산 아님) 기반 modifier, 2건 이상·미지 campaignId·
        /// bonusOrderCount 범위 밖은 명시적 실패다. 순수 함수 — 같은 입력에서 항상 같은 결과.
        /// </summary>
        public static bool TryBuildDayModifier(
            IReadOnlyList<SNSCampaignRecord> history, int day,
            IReadOnlyList<SNSCampaignDefInput> campaignDefs, IReadOnlyList<CustomerDefInput> customers,
            out DayModifier modifier, out string failReason)
        {
            modifier = null;
            failReason = "";

            if (history == null)
            {
                modifier = DayModifier.Neutral(day);
                return true;
            }

            SNSCampaignRecord match = null;
            int matchCount = 0;
            foreach (var record in history)
            {
                if (record != null && record.executedOnDay == day - 1)
                {
                    match = record;
                    matchCount++;
                }
            }

            if (matchCount == 0)
            {
                modifier = DayModifier.Neutral(day);
                return true;
            }
            if (matchCount > 1)
            {
                failReason = "SNS 집행 기록이 손상되었습니다 (하루 2건 이상).";
                return false;
            }

            if (match.bonusOrderCount < 0 || match.bonusOrderCount > 2)
            {
                failReason = "SNS 집행 기록의 보너스 주문 수가 잘못되었습니다.";
                return false;
            }

            SNSCampaignDefInput def = null;
            if (campaignDefs != null)
            {
                foreach (var candidate in campaignDefs)
                {
                    if (candidate != null && string.Equals(candidate.Id, match.campaignId, StringComparison.Ordinal))
                    {
                        def = candidate;
                        break;
                    }
                }
            }
            if (def == null)
            {
                failReason = $"알 수 없는 SNS 캠페인 '{match.campaignId}' 기록입니다.";
                return false;
            }
            if (IsCustomersInvalid(customers, out failReason))
            {
                return false;
            }

            var affinityRows = ProjectAffinityRows(def.AudienceAffinities);
            var boosts = new List<CustomerWeightBoost>();
            foreach (var customer in customers)
            {
                int affinity = CalculateAffinityMilli(customer.AgeBand, customer.Gender, affinityRows);
                boosts.Add(new CustomerWeightBoost(customer.Id, affinity));
            }

            modifier = new DayModifier(day, match.campaignId, match.bonusOrderCount, boosts);
            return true;
        }
    }

    // ── 순수 투영 DTO (Unity 타입 없음) ─────────────────────────────────────

    /// <summary>SNSCampaignOps 입력용 순수 SNS 캠페인 정의 투영.</summary>
    public sealed class SNSCampaignDefInput
    {
        public string Id;
        public string DisplayName;
        public int BaseCost;
        public float BaseReach;
        public float RepeatDecay;
        public IReadOnlyList<SNSRawAffinityInput> AudienceAffinities = Array.Empty<SNSRawAffinityInput>();
    }

    /// <summary>SNSCampaignDef.AudienceAffinities 1행의 float 투영 (경계 — ProjectMilli 이전 원본 값).</summary>
    public readonly struct SNSRawAffinityInput
    {
        public SNSRawAffinityInput(AgeBand ageBand, GenderTarget gender, float multiplier)
        {
            AgeBand = ageBand;
            Gender = gender;
            Multiplier = multiplier;
        }

        public AgeBand AgeBand { get; }
        public GenderTarget Gender { get; }
        public float Multiplier { get; }
    }

    /// <summary>밀리 정수로 투영된 SNS 타겟 친화 행 (C1 — float 은 경계에서 한 번만).</summary>
    public readonly struct SNSAffinityInput
    {
        public SNSAffinityInput(AgeBand ageBand, GenderTarget gender, int multiplierMilli)
        {
            AgeBand = ageBand;
            Gender = gender;
            MultiplierMilli = multiplierMilli;
        }

        public AgeBand AgeBand { get; }
        public GenderTarget Gender { get; }
        public int MultiplierMilli { get; }
    }
}
