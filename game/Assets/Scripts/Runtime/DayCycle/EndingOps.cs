using System;
using ClientIsKing.Social;

namespace ClientIsKing.DayCycle
{
    /// <summary>런 엔딩 상태 — 파생 전용 (GameState 에 영속 필드 없음, task-115 C1).</summary>
    public enum RunEndingStatus
    {
        None = 0,
        Cleared = 1,
        Bankrupt = 2,
    }

    /// <summary>
    /// 데모 엔딩 규칙의 단일 원천 (순수 — Unity/SO/IO 미참조).
    ///
    /// 계약 (task-115 C1/C2): GameState 직렬화 필드를 추가하지 않는다 — 엔딩 상태는 기존 필드
    /// (isBankrupt/daysCompleted)에서 파생한다(SaveSchemaVersion 1 유지). 분기 순서는 계약이다:
    /// isBankrupt → Bankrupt, daysCompleted ≥ ClearTargetDays → Cleared, 그 외 None. 파산 시
    /// daysCompleted 는 정산 성공 분기에서만 갱신되므로(SettlementOps) 두 상태는 동시 성립할 수 없다.
    /// </summary>
    public static class EndingOps
    {
        /// <summary>데모 클리어 목표 영업일수 (시드 — 오너 플레이테스트 게이트, task-115 B3 #5).</summary>
        public const int ClearTargetDays = 7;

        /// <summary>파산 우선 → daysCompleted ≥ ClearTargetDays 클리어 → None.</summary>
        public static RunEndingStatus GetStatus(GameState state)
        {
            Require(state);
            if (state.isBankrupt)
            {
                return RunEndingStatus.Bankrupt;
            }
            if (state.daysCompleted >= ClearTargetDays)
            {
                return RunEndingStatus.Cleared;
            }
            return RunEndingStatus.None;
        }

        /// <summary>MainMenu 가 SaveSummary 로 호출하는 원시형 (GetStatus 와 동일 판정 로직).</summary>
        public static bool IsCleared(int daysCompleted, bool isBankrupt)
        {
            return !isBankrupt && daysCompleted >= ClearTargetDays;
        }

        /// <summary>런이 종료되었는가 (status != None).</summary>
        public static bool IsRunEnded(GameState state)
        {
            return GetStatus(state) != RunEndingStatus.None;
        }

        /// <summary>엔딩 오버레이 표시용 요약 DTO 조립 — 전부 기존 필드에서 파생, UI 재계산 금지.</summary>
        public static EndingSummary BuildSummary(GameState state)
        {
            Require(state);
            return new EndingSummary
            {
                Status = GetStatus(state),
                DaysCompleted = state.daysCompleted,
                FinalCash = state.cash,
                NetProfit = state.cash - GameState.StartingCash,
                FollowerDisplay = SNSCampaignOps.CalculateFollowerDisplay(state.snsCampaignHistory),
                BankruptcyReason = state.bankruptcyReason,
            };
        }

        static void Require(GameState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state), "GameState 없이 엔딩을 판정할 수 없다");
            }
        }
    }
}
