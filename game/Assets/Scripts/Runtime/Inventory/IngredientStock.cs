using System;
using ClientIsKing.Data;

namespace ClientIsKing.Inventory
{
    /// <summary>
    /// 재료 종류×등급별 보유 수량 — GameState 인벤토리의 list item.
    /// JsonUtility 직렬화 규약(Dictionary 금지)에 맞춘 순수 C# public 필드 (task-105 설계 1단계).
    /// </summary>
    [Serializable]
    public sealed class IngredientStock
    {
        public IngredientKind kind;
        public IngredientGrade grade;
        public int quantity;
    }
}
