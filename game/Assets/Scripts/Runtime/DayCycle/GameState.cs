using System;
using System.Collections.Generic;
using ClientIsKing.Inventory;

namespace ClientIsKing.DayCycle
{
    /// <summary>
    /// 런타임 상태 컨테이너 (task-105 시점: day/phase + 자금 + 재료 인벤토리).
    /// 순수 C# + public 필드 — 브리프의 JsonUtility 직렬화 규약(Dictionary 금지) 전제.
    /// 통계/저장 포맷 확장은 task-106+ 에서 이 클래스에 추가한다.
    /// </summary>
    [Serializable]
    public sealed class GameState
    {
        /// <summary>데모 시작 자금 (초안 밸런스 — playtest 조정 대상, task-105 설계 2단계).</summary>
        public const int StartingCash = 30000;

        /// <summary>현재 일차 (1부터 시작).</summary>
        public int day = 1;

        /// <summary>현재 하루 phase.</summary>
        public DayPhase currentPhase = DayPhase.Market;

        /// <summary>보유 자금 (원, 음수 불허 — 경제 규칙이 보장).</summary>
        public int cash = StartingCash;

        /// <summary>재료 인벤토리 — 종류×등급별 항목의 List (Dictionary 금지 규약).</summary>
        public List<IngredientStock> ingredientStocks = new List<IngredientStock>();
    }
}
