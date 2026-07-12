using System;
using System.Collections.Generic;
using ClientIsKing.Data;
using ClientIsKing.Genre;

namespace ClientIsKing.Events
{
    /// <summary>
    /// 이벤트/장애물 핵심 규칙 (순수 C# — EditMode 테스트 대상). Unity 타입을 참조하지 않는다 —
    /// 호출자(GameManager)가 SO 정의를 <see cref="GameEventDefInput"/> 로 투영해 넘긴다(순수 enum
    /// GameEventKind 는 허용). ClientIsKing.Genre(GenreSelectionOps.RoundHalfUp/MulMilliHalfUp/Fnv1a,
    /// DayModifier)만 참조하고 ClientIsKing.Social 은 참조하지 않는다(task-112 설계 제약).
    ///
    /// 계약 (task-112 design.md B~F):
    /// - 발생 스케줄은 32-bit FNV-1a(offset 2166136261/prime 16777619/unchecked, task-110 과 동일 해시)
    ///   시드 47/문턱 450 로 완전 결정론이다. 같은 day → 같은 결과, 플레이어 선택과 무관.
    /// - 잘못된 정의·상태는 명시적 실패다(neutral fallback 금지) — 실패 경로는 GameState 를
    ///   완전히 불변으로 유지한다(task-111 리뷰 001 교훈 — 모든 def 참조 경로가 같은 검증을 공유).
    /// - SO float(baseWeight/percentEffect)은 투영 경계에서 한 번만 RoundHalfUp(x×1000)으로 밀리
    ///   정수화한 뒤 전 구간 정수 연산한다.
    /// </summary>
    public static class EventOps
    {
        /// <summary>일일 운영비 시드 (task-115) — SettlementOps.DailyOperatingCost 와 동일 값.
        /// EventOps 는 계층 규약상 Settlement 를 직접 참조하지 않으므로(진입부 주석 — Genre 만 허용)
        /// 명명 상수로 독립 보유하고 동기 핀 테스트(BalanceEndingGuardTests)로 drift 를 차단한다.</summary>
        public const int BaseDailyOperatingCost = 15000;

        const int ScheduleSeed = 47;
        const int OccurrenceThresholdMilli = 450;

        // 축약 포맷(F5) kind별 고정 명칭 — displayName 변경과 무관한 결정론 상수.
        const string AbbrevGroup = "단체";
        const string AbbrevHygiene = "위생";
        const string AbbrevSurge = "폭등";
        const string AbbrevRent = "임대";

        // ── 투영 ────────────────────────────────────────────────────────────

        /// <summary>SO float 필드를 투영 경계에서 한 번만 밀리 정수화한다: RoundHalfUp(x × 1000).</summary>
        public static int ProjectMilli(float value)
        {
            return (int)GenreSelectionOps.RoundHalfUp((double)value * 1000.0);
        }

        // ── 검증 (공용 — 모든 공개 API 가 진입 시 공유) ─────────────────────

        /// <summary>
        /// 이벤트 카탈로그 무결성 검증. def 개별 필드·kind 별 규약·카탈로그 중복(Id/Kind)을 확인한다.
        /// 모든 공개 API(스케줄·수명 전이·효과 합성·예고)가 이 검증을 진입 시 공유한다.
        /// </summary>
        public static bool TryValidateCatalog(IReadOnlyList<GameEventDefInput> defs, out string failReason)
        {
            failReason = "";
            if (defs == null || defs.Count == 0)
            {
                failReason = "이벤트 정의가 없습니다.";
                return false;
            }

            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            var seenKinds = new HashSet<GameEventKind>();
            foreach (var def in defs)
            {
                if (def == null || string.IsNullOrEmpty(def.Id))
                {
                    failReason = "잘못된 이벤트 정의가 있습니다.";
                    return false;
                }
                if (!seenIds.Add(def.Id))
                {
                    failReason = $"중복된 이벤트 ID '{def.Id}'.";
                    return false;
                }
                if (!seenKinds.Add(def.Kind))
                {
                    failReason = $"중복된 이벤트 종류 '{def.Kind}'.";
                    return false;
                }
                if (def.BaseWeight <= 0f || float.IsNaN(def.BaseWeight) || float.IsInfinity(def.BaseWeight))
                {
                    failReason = $"이벤트 '{def.Id}' 의 발생 가중치가 잘못되었습니다.";
                    return false;
                }
                if (def.DurationDays < 0)
                {
                    failReason = $"이벤트 '{def.Id}' 의 지속 일수가 잘못되었습니다.";
                    return false;
                }
                if (float.IsNaN(def.PercentEffect) || float.IsInfinity(def.PercentEffect))
                {
                    failReason = $"이벤트 '{def.Id}' 의 퍼센트 효과가 잘못되었습니다.";
                    return false;
                }

                switch (def.Kind)
                {
                    case GameEventKind.IngredientPriceSurge:
                        if (!(def.PercentEffect > 0f) || def.DurationDays < 1)
                        {
                            failReason = $"이벤트 '{def.Id}' (재료값 폭등) 의 효과/기간이 잘못되었습니다.";
                            return false;
                        }
                        break;
                    case GameEventKind.RentIncrease:
                        if (!(def.PercentEffect > 0f) || def.DurationDays != 0)
                        {
                            failReason = $"이벤트 '{def.Id}' (임대료 인상) 의 효과/기간이 잘못되었습니다.";
                            return false;
                        }
                        break;
                    case GameEventKind.HygieneInspection:
                        if (def.FlatEffect <= 0 || def.DurationDays < 1)
                        {
                            failReason = $"이벤트 '{def.Id}' (위생 점검) 의 효과/기간이 잘못되었습니다.";
                            return false;
                        }
                        break;
                    case GameEventKind.GroupCustomers:
                        if (def.FlatEffect < 2 || def.DurationDays < 1)
                        {
                            failReason = $"이벤트 '{def.Id}' (단체 손님) 의 효과/기간이 잘못되었습니다.";
                            return false;
                        }
                        break;
                    default:
                        failReason = $"이벤트 '{def.Id}' 의 종류를 알 수 없습니다.";
                        return false;
                }
            }

            // 하드캡 4종이 전부 존재해야 한다 — 필수 kind 누락(예: catalog asset 유실)은 축소된
            // 후보군으로 조용히 진행하지 않고 명시적으로 실패한다(Codex 리뷰001 Action).
            foreach (GameEventKind requiredKind in RequiredKinds)
            {
                if (!seenKinds.Contains(requiredKind))
                {
                    failReason = $"필수 이벤트 종류 '{requiredKind}' 가 누락되었습니다.";
                    return false;
                }
            }
            return true;
        }

        static readonly GameEventKind[] RequiredKinds =
        {
            GameEventKind.IngredientPriceSurge,
            GameEventKind.HygieneInspection,
            GameEventKind.RentIncrease,
            GameEventKind.GroupCustomers,
        };

        /// <summary>
        /// activeEvents 목록 불변식 검증: eventId 비어있지 않음·catalog 존재·중복 없음,
        /// remainingDays ≥ 0, def 가 영구(durationDays=0)면 remainingDays==0, 시한이면 remainingDays ≥ 1.
        /// </summary>
        public static bool TryValidateActiveEvents(
            IReadOnlyList<ActiveEventState> activeEvents, IReadOnlyList<GameEventDefInput> defs, out string failReason)
        {
            failReason = "";
            if (!TryValidateCatalog(defs, out failReason))
            {
                return false;
            }
            if (activeEvents == null)
            {
                failReason = "활성 이벤트 목록이 없습니다.";
                return false;
            }

            var defById = BuildDefLookup(defs);
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var active in activeEvents)
            {
                if (active == null || string.IsNullOrEmpty(active.eventId))
                {
                    failReason = "잘못된 활성 이벤트 항목이 있습니다.";
                    return false;
                }
                if (!defById.TryGetValue(active.eventId, out var def))
                {
                    failReason = $"알 수 없는 활성 이벤트 '{active.eventId}' 입니다.";
                    return false;
                }
                if (!seenIds.Add(active.eventId))
                {
                    failReason = $"중복된 활성 이벤트 '{active.eventId}'.";
                    return false;
                }
                if (active.remainingDays < 0)
                {
                    failReason = $"활성 이벤트 '{active.eventId}' 의 남은 일수가 잘못되었습니다.";
                    return false;
                }
                if (def.DurationDays == 0)
                {
                    if (active.remainingDays != 0)
                    {
                        failReason = $"영구 이벤트 '{active.eventId}' 의 남은 일수가 잘못되었습니다.";
                        return false;
                    }
                }
                else if (active.remainingDays < 1)
                {
                    failReason = $"활성 이벤트 '{active.eventId}' 의 남은 일수가 잘못되었습니다.";
                    return false;
                }
            }
            return true;
        }

        static Dictionary<string, GameEventDefInput> BuildDefLookup(IReadOnlyList<GameEventDefInput> defs)
        {
            var result = new Dictionary<string, GameEventDefInput>(StringComparer.Ordinal);
            foreach (var def in defs)
            {
                result[def.Id] = def;
            }
            return result;
        }

        // ── C1/C2: 스케줄 + 수명 전이 ────────────────────────────────────────

        /// <summary>
        /// Night(Day N)→Market(Day N+1) 경계 전이. advance(수명 감소/만료/영구 유지) → schedule(신규 결정) →
        /// activate(append) 순서로 원자적으로 계산한다. 입력을 변경하지 않고 새 목록을 반환한다.
        /// </summary>
        public static bool TryBuildNextDayActiveEvents(
            IReadOnlyList<ActiveEventState> current, int nextDay, IReadOnlyList<GameEventDefInput> defs,
            out List<ActiveEventState> next, out string activatedEventId, out string failReason)
        {
            next = null;
            activatedEventId = "";
            failReason = "";

            if (!TryValidateActiveEvents(current, defs, out failReason))
            {
                return false;
            }

            var defById = BuildDefLookup(defs);

            // 1. advance
            var advanced = new List<ActiveEventState>();
            foreach (var active in current)
            {
                if (active.remainingDays == 0)
                {
                    advanced.Add(new ActiveEventState { eventId = active.eventId, remainingDays = 0 });
                }
                else if (active.remainingDays == 1)
                {
                    // 만료 — 제거
                }
                else
                {
                    advanced.Add(new ActiveEventState { eventId = active.eventId, remainingDays = active.remainingDays - 1 });
                }
            }

            // 2. schedule
            string picked = "";
            if (nextDay > 1)
            {
                var activeIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var active in advanced)
                {
                    activeIds.Add(active.eventId);
                }

                uint occSeed = GenreSelectionOps.Fnv1a($"event|{ScheduleSeed}|{nextDay}");
                int occRoll = (int)(occSeed % 1000);
                if (occRoll < OccurrenceThresholdMilli)
                {
                    var candidates = new List<GameEventDefInput>();
                    foreach (var def in defs)
                    {
                        if (!activeIds.Contains(def.Id))
                        {
                            candidates.Add(def);
                        }
                    }
                    candidates.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

                    if (candidates.Count > 0)
                    {
                        long totalWeightMilli = 0;
                        var weightMillis = new List<long>();
                        foreach (var def in candidates)
                        {
                            long weightMilli = GenreSelectionOps.RoundHalfUp((double)def.BaseWeight * 1000.0);
                            weightMillis.Add(weightMilli);
                            totalWeightMilli += weightMilli;
                        }

                        if (totalWeightMilli > 0)
                        {
                            uint pickSeed = GenreSelectionOps.Fnv1a($"event-pick|{ScheduleSeed}|{nextDay}");
                            long pickRoll = (long)(pickSeed % (ulong)totalWeightMilli);

                            long cumulative = 0;
                            for (int i = 0; i < candidates.Count; i++)
                            {
                                cumulative += weightMillis[i];
                                if (cumulative > pickRoll)
                                {
                                    picked = candidates[i].Id;
                                    break;
                                }
                            }
                            if (string.IsNullOrEmpty(picked))
                            {
                                picked = candidates[candidates.Count - 1].Id;
                            }
                        }
                    }
                }
            }

            // 3. activate
            if (!string.IsNullOrEmpty(picked))
            {
                var pickedDef = defById[picked];
                advanced.Add(new ActiveEventState
                {
                    eventId = picked,
                    remainingDays = pickedDef.DurationDays == 0 ? 0 : pickedDef.DurationDays,
                });
                activatedEventId = picked;
            }

            next = advanced;
            return true;
        }

        // ── D1: 효과 합성 ────────────────────────────────────────────────────

        /// <summary>오늘 활성 집합 → 축별 효과 합성 (순수). ID ordinal 순서로 중립에서 시작해 적용한다.</summary>
        public static bool TryBuildDayEffects(
            IReadOnlyList<ActiveEventState> activeForDay, int day, IReadOnlyList<GameEventDefInput> defs,
            out EventDayEffects fx, out string failReason)
        {
            fx = null;
            failReason = "";

            if (!TryValidateActiveEvents(activeForDay, defs, out failReason))
            {
                return false;
            }

            var defById = BuildDefLookup(defs);
            var activeIds = new List<string>();
            foreach (var active in activeForDay)
            {
                activeIds.Add(active.eventId);
            }
            activeIds.Sort(StringComparer.Ordinal);

            int ingredientCostMilli = 1000;
            int operatingCostMilli = 1000;
            int operatingCostFlat = 0;
            int groupBonusOrders = 0;
            int groupPartySize = 0;
            string groupSourceEventId = "";

            foreach (var id in activeIds)
            {
                var def = defById[id];
                switch (def.Kind)
                {
                    case GameEventKind.IngredientPriceSurge:
                        ingredientCostMilli += (int)GenreSelectionOps.RoundHalfUp((double)def.PercentEffect * 1000.0);
                        break;
                    case GameEventKind.RentIncrease:
                        operatingCostMilli += (int)GenreSelectionOps.RoundHalfUp((double)def.PercentEffect * 1000.0);
                        break;
                    case GameEventKind.HygieneInspection:
                        operatingCostFlat += def.FlatEffect;
                        break;
                    case GameEventKind.GroupCustomers:
                        groupBonusOrders = 1;
                        groupPartySize = def.FlatEffect;
                        groupSourceEventId = def.Id;
                        break;
                }
            }

            fx = new EventDayEffects(
                day, activeIds, ingredientCostMilli, operatingCostMilli, operatingCostFlat,
                groupBonusOrders, groupPartySize, groupSourceEventId);
            return true;
        }

        // ── 예고 ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 내일(nextDay) 예고를 계산한다. TryBuildNextDayActiveEvents + TryBuildDayEffects(내일) 를 재사용해
        /// 예고==적용을 보장한다(같은 순수 함수).
        /// </summary>
        public static bool TryBuildForecast(
            IReadOnlyList<ActiveEventState> current, int nextDay, IReadOnlyList<GameEventDefInput> defs,
            out EventForecast forecast, out string failReason)
        {
            forecast = null;
            failReason = "";

            if (!TryBuildNextDayActiveEvents(current, nextDay, defs, out var next, out var activatedEventId, out failReason))
            {
                return false;
            }
            if (!TryBuildDayEffects(next, nextDay, defs, out var fx, out failReason))
            {
                return false;
            }

            var defById = BuildDefLookup(defs);

            string upcomingLine = "";
            if (!string.IsNullOrEmpty(activatedEventId))
            {
                var def = defById[activatedEventId];
                string summary = BuildEffectSummary(def);
                upcomingLine = $"[예고] 내일 {def.DisplayName} — {summary}";
            }

            string continuingLine = "";
            foreach (var active in next)
            {
                if (string.Equals(active.eventId, activatedEventId, StringComparison.Ordinal))
                {
                    continue; // 방금 활성화된 이벤트는 신규 라인이 이미 다룬다
                }
                if (active.remainingDays == 0)
                {
                    continue; // 영구 이벤트는 지속 라인 대상이 아니다(임대료는 매일 정산 라인에 남는 방식으로 표현)
                }
                var def = defById[active.eventId];
                continuingLine = active.remainingDays >= 2
                    ? $"[지속] {def.DisplayName} — {active.remainingDays}일 더"
                    : $"[지속] {def.DisplayName} — 내일까지";
                break; // 시한 이벤트는 kind 유일성으로 최대 1건
            }

            if (!string.IsNullOrEmpty(upcomingLine) && !string.IsNullOrEmpty(continuingLine))
            {
                upcomingLine = $"{upcomingLine} · 지속: {continuingLine.Substring(continuingLine.IndexOf(']') + 2)}";
            }

            int nextDayOperatingCost = GenreSelectionOps.MulMilliHalfUp(BaseDailyOperatingCost, fx.OperatingCostMilli) + fx.OperatingCostFlat;

            forecast = new EventForecast(nextDay, activatedEventId, upcomingLine, continuingLine, nextDayOperatingCost);
            return true;
        }

        // ── D2: DayModifier 합성 ─────────────────────────────────────────────

        /// <summary>SNS modifier 에 이벤트 수요 축(단체 손님)을 결합한 완성 DayModifier.</summary>
        public static bool TryComposeDayModifier(
            DayModifier snsModifier, EventDayEffects fx, out DayModifier composed, out string failReason)
        {
            composed = null;
            failReason = "";

            if (snsModifier == null)
            {
                failReason = "SNS DayModifier 가 없습니다.";
                return false;
            }
            if (fx == null)
            {
                failReason = "이벤트 효과가 없습니다.";
                return false;
            }
            if (snsModifier.Day != fx.Day)
            {
                failReason = "DayModifier 와 이벤트 효과의 day 가 일치하지 않습니다.";
                return false;
            }
            if (fx.GroupBonusOrders != 0 && fx.GroupBonusOrders != 1)
            {
                failReason = "단체 손님 보너스 주문 수가 잘못되었습니다.";
                return false;
            }
            if (fx.GroupBonusOrders == 1 && (fx.GroupPartySize < 2 || string.IsNullOrEmpty(fx.GroupSourceEventId)))
            {
                failReason = "단체 손님 효과가 손상되었습니다.";
                return false;
            }

            composed = new DayModifier(
                snsModifier.Day, snsModifier.SourceCampaignId, snsModifier.BonusOrderCount, snsModifier.WeightBoosts,
                fx.GroupSourceEventId, fx.GroupBonusOrders, fx.GroupPartySize);
            return true;
        }

        // ── F5: 정산 원인 라인 ───────────────────────────────────────────────

        /// <summary>
        /// 정산 원인 라인 조립의 단일 원천 — stale 필터·포맷 분기(≤2 전체/≥3 축약)를 내부에서 처리한다.
        /// 호출자는 state 원시 필드를 그대로 전달한다(사전 필터 금지).
        /// </summary>
        public static string BuildSettlementCauseLine(
            EventDayEffects fx, IReadOnlyList<GameEventDefInput> defs,
            int marketSpendDay, int marketEventSurchargeToday, int groupServed, int groupRevenue)
        {
            if (fx == null || fx.ActiveEventIds == null || fx.ActiveEventIds.Count == 0)
            {
                return "";
            }

            var defById = BuildDefLookup(defs ?? Array.Empty<GameEventDefInput>());
            int surcharge = marketSpendDay == fx.Day ? marketEventSurchargeToday : 0;
            int rentDelta = GenreSelectionOps.MulMilliHalfUp(BaseDailyOperatingCost, fx.OperatingCostMilli) - BaseDailyOperatingCost;

            bool abbreviated = fx.ActiveEventIds.Count >= 3;
            var parts = new List<string>();

            foreach (var id in fx.ActiveEventIds)
            {
                if (!defById.TryGetValue(id, out var def))
                {
                    continue;
                }
                switch (def.Kind)
                {
                    case GameEventKind.GroupCustomers:
                        parts.Add(abbreviated
                            ? $"{AbbrevGroup} {groupServed}/{fx.GroupBonusOrders} +{groupRevenue:N0}"
                            : $"{def.DisplayName} {groupServed}/{fx.GroupBonusOrders}팀 +{groupRevenue:N0}원");
                        break;
                    case GameEventKind.HygieneInspection:
                        parts.Add(abbreviated
                            ? $"{AbbrevHygiene} -{fx.OperatingCostFlat:N0}"
                            : $"{def.DisplayName} -{fx.OperatingCostFlat:N0}원");
                        break;
                    case GameEventKind.IngredientPriceSurge:
                        parts.Add(abbreviated
                            ? $"{AbbrevSurge} -{surcharge:N0}"
                            : $"{def.DisplayName} -{surcharge:N0}원");
                        break;
                    case GameEventKind.RentIncrease:
                        parts.Add(abbreviated
                            ? $"{AbbrevRent} -{rentDelta:N0}"
                            : $"{def.DisplayName} -{rentDelta:N0}원");
                        break;
                }
            }

            if (parts.Count == 0)
            {
                return "";
            }
            return "이벤트: " + string.Join(" · ", parts);
        }

        // ── F2: 효과 요약 ────────────────────────────────────────────────────

        /// <summary>def 데이터에서 조립하는 효과 요약 문자열 — Night 예고/카피의 단일 원천.</summary>
        public static string BuildEffectSummary(GameEventDefInput def)
        {
            if (def == null)
            {
                return "";
            }
            switch (def.Kind)
            {
                case GameEventKind.IngredientPriceSurge:
                    return $"재료 구매가 +{PercentDisplay(def.PercentEffect)}% ({def.DurationDays}일)";
                case GameEventKind.HygieneInspection:
                    return $"대응 비용 {def.FlatEffect:N0}원 ({def.DurationDays}일)";
                case GameEventKind.RentIncrease:
                    return $"운영비 +{PercentDisplay(def.PercentEffect)}% (영구)";
                case GameEventKind.GroupCustomers:
                    return $"단체 손님 1팀({def.FlatEffect}인) 방문 ({def.DurationDays}일)";
                default:
                    return "";
            }
        }

        /// <summary>퍼센트 표시 정수 조립 — percentMilli / 10 (부동소수 문자열화 금지).</summary>
        static int PercentDisplay(float percentEffect)
        {
            int percentMilli = (int)GenreSelectionOps.RoundHalfUp((double)percentEffect * 1000.0);
            return percentMilli / 10;
        }
    }

    /// <summary>EventOps 입력용 순수 이벤트 정의 투영 — Unity 타입 없음.</summary>
    public sealed class GameEventDefInput
    {
        public string Id;
        public string DisplayName;
        public GameEventKind Kind;
        public float BaseWeight;
        public int DurationDays;
        public float PercentEffect;
        public int FlatEffect;
    }
}
