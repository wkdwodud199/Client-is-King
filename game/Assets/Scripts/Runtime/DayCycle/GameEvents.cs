using System;
using ClientIsKing.Presentation;

namespace ClientIsKing.DayCycle
{
    /// <summary>
    /// C# event 기반 런타임 이벤트 허브 (브리프 규약: 이벤트버스 라이브러리 금지).
    /// task-104: DayPhaseChanged. task-108: 표현 전용 이벤트 3종 —
    /// 도메인 Ops 는 발행하지 않고 UI/controller 계층이 발행한다 (설계 제약).
    /// </summary>
    public static class GameEvents
    {
        /// <summary>하루 phase 전환마다 정확히 1회 발행된다.</summary>
        public static event Action<DayPhaseChangedEventArgs> DayPhaseChanged;

        /// <summary>발행은 상태 머신(DayPhaseMachine)만 호출한다 (같은 어셈블리 internal).</summary>
        internal static void RaiseDayPhaseChanged(DayPhaseChangedEventArgs args)
        {
            DayPhaseChanged?.Invoke(args);
        }

        // ── 표현 전용 이벤트 (task-108) — 게임 규칙은 이 이벤트에 의존하지 않는다 ──

        /// <summary>현재 표시할 주문이 바뀔 때 (없으면 HasOrder=false 로 슬롯 비움 신호).</summary>
        public static event Action<ServicePresentationEventArgs> ServiceOrderPresented;

        /// <summary>서빙 성공/포기 결과가 확정될 때 (처리 전 주문 정보 포함).</summary>
        public static event Action<ServicePresentationEventArgs> ServiceOutcomeResolved;

        /// <summary>정산 결과가 표시될 때 (멱등 재표시 포함 — payload 는 저장된 결과).</summary>
        public static event Action<SettlementPresentationEventArgs> SettlementPresented;

        internal static void RaiseServiceOrderPresented(ServicePresentationEventArgs args)
        {
            ServiceOrderPresented?.Invoke(args);
        }

        internal static void RaiseServiceOutcomeResolved(ServicePresentationEventArgs args)
        {
            ServiceOutcomeResolved?.Invoke(args);
        }

        internal static void RaiseSettlementPresented(SettlementPresentationEventArgs args)
        {
            SettlementPresented?.Invoke(args);
        }
    }
}
