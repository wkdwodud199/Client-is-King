using System;

namespace ClientIsKing.Service
{
    /// <summary>
    /// 당일 서비스 주문 하나의 저장 가능한 상태 (task-106 설계 1단계).
    /// ScriptableObject 직접 참조 대신 안정적 id 문자열을 저장한다 (JsonUtility 규약).
    /// </summary>
    [Serializable]
    public sealed class ServiceOrderState
    {
        /// <summary>주문된 레시피의 RecipeDef.Id.</summary>
        public string recipeId = "";

        /// <summary>주문한 고객 archetype 의 CustomerArchetypeDef.Id.</summary>
        public string customerId = "";

        /// <summary>파티 인원 (1 이상).</summary>
        public int partySize = 1;

        /// <summary>서빙 성공 처리됨.</summary>
        public bool served;

        /// <summary>포기/이탈 처리됨.</summary>
        public bool missed;

        /// <summary>이 주문이 SNS 보너스 유입인가 (task-111 — 기본 false, 기존 저장/테스트 하위호환).</summary>
        public bool snsInflow;

        /// <summary>아직 처리되지 않은 주문인가.</summary>
        public bool IsOpen => !served && !missed;
    }
}
