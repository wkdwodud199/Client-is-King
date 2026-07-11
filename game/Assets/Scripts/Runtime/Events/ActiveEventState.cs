using System;

namespace ClientIsKing.Events
{
    /// <summary>
    /// 활성 이벤트 1건의 저장 가능한 상태 (task-110 D10, task-112 B3).
    /// GameEventDef 를 직접 참조하지 않고 문자열 ID 만 저장한다 (JsonUtility 규약).
    /// </summary>
    [Serializable]
    public sealed class ActiveEventState
    {
        /// <summary>GameEventDef.Id (문자열 ID 규약).</summary>
        public string eventId = "";

        /// <summary>
        /// 남은 활성 일수. 0 = 영구(durationDays=0 규약 미러링, task-103) — 절대 감소·만료하지 않는다.
        /// 시한 이벤트는 활성 시 durationDays 로 시작해 항상 1 이상이다.
        /// </summary>
        public int remainingDays = 0;
    }
}
