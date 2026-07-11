using System;
using System.Collections.Generic;

namespace ClientIsKing.Events
{
    /// <summary>
    /// 하루치 이벤트 효과의 축 분리 DTO (task-112 D1) — Unity 타입 없는 순수 C#.
    /// 같은 activeEvents+defs 에서 <see cref="EventOps.TryBuildDayEffects"/> 로만 파생된다.
    /// </summary>
    public sealed class EventDayEffects
    {
        public EventDayEffects(
            int day, IReadOnlyList<string> activeEventIds,
            int ingredientCostMilli, int operatingCostMilli, int operatingCostFlat,
            int groupBonusOrders, int groupPartySize, string groupSourceEventId)
        {
            Day = day;
            ActiveEventIds = activeEventIds ?? Array.Empty<string>();
            IngredientCostMilli = ingredientCostMilli;
            OperatingCostMilli = operatingCostMilli;
            OperatingCostFlat = operatingCostFlat;
            GroupBonusOrders = groupBonusOrders;
            GroupPartySize = groupPartySize;
            GroupSourceEventId = groupSourceEventId ?? "";
        }

        /// <summary>적용 대상 일차.</summary>
        public int Day { get; }

        /// <summary>오늘 활성 이벤트 ID (ID ordinal 정렬).</summary>
        public IReadOnlyList<string> ActiveEventIds { get; }

        /// <summary>재료 구매 단가 배수 (milli, 1000 = 중립).</summary>
        public int IngredientCostMilli { get; }

        /// <summary>운영비 배수 (milli, 1000 = 중립).</summary>
        public int OperatingCostMilli { get; }

        /// <summary>운영비 가산액 (원, 0 = 중립).</summary>
        public int OperatingCostFlat { get; }

        /// <summary>단체 보너스 주문 수 (0 또는 1).</summary>
        public int GroupBonusOrders { get; }

        /// <summary>단체 파티 크기 (0 = 없음, 있으면 2 이상).</summary>
        public int GroupPartySize { get; }

        /// <summary>단체 손님을 발생시킨 이벤트 ID. 빈 문자열 = 단체 없음.</summary>
        public string GroupSourceEventId { get; }

        /// <summary>전 축 중립 (활성 이벤트 없음).</summary>
        public static EventDayEffects Neutral(int day)
        {
            return new EventDayEffects(day, Array.Empty<string>(), 1000, 1000, 0, 0, 0, "");
        }
    }

    /// <summary>
    /// 익일 예고 DTO (task-112 C3/F2) — 표시용 완성 문자열을 담는다. UI 는 재계산하지 않는다.
    /// </summary>
    public sealed class EventForecast
    {
        public EventForecast(
            int day, string upcomingEventId, string upcomingNoticeLine,
            string continuingNoticeLine, int nextDayOperatingCost)
        {
            Day = day;
            UpcomingEventId = upcomingEventId ?? "";
            UpcomingNoticeLine = upcomingNoticeLine ?? "";
            ContinuingNoticeLine = continuingNoticeLine ?? "";
            NextDayOperatingCost = nextDayOperatingCost;
        }

        /// <summary>예고 대상 일차(내일).</summary>
        public int Day { get; }

        /// <summary>내일 신규 활성화되는 이벤트 ID. 빈 문자열 = 신규 없음.</summary>
        public string UpcomingEventId { get; }

        /// <summary>신규 이벤트 예고 라인 (F2 카피). 신규 없으면 빈 문자열.</summary>
        public string UpcomingNoticeLine { get; }

        /// <summary>지속 이벤트 예고 라인 (F2 카피). 지속 없으면 빈 문자열.</summary>
        public string ContinuingNoticeLine { get; }

        /// <summary>내일 운영비 = MulMilliHalfUp(12000, 내일 milli) + 내일 flat.</summary>
        public int NextDayOperatingCost { get; }
    }
}
