using System;

namespace ClientIsKing.DayCycle
{
    /// <summary>
    /// 런타임 상태의 최소 컨테이너 (task-104 시점: day + 현재 phase 만).
    /// 순수 C# + public 필드 — 브리프의 JsonUtility 직렬화 규약(Dictionary 금지) 전제.
    /// 자금/인벤토리/통계/저장 포맷 확장은 task-105+ 에서 이 클래스에 추가한다.
    /// </summary>
    [Serializable]
    public sealed class GameState
    {
        /// <summary>현재 일차 (1부터 시작).</summary>
        public int day = 1;

        /// <summary>현재 하루 phase.</summary>
        public DayPhase currentPhase = DayPhase.Market;
    }
}
