using System;
using UnityEngine;

namespace ClientIsKing.Presentation
{
    /// <summary>
    /// 표현 계층 전용 sprite catalog entry — id 문자열로 도메인과 연결한다.
    /// GameState(저장 대상)에는 절대 넣지 않는다 (task-108 제약: 표현 레이어는 저장 대상 아님).
    /// </summary>
    [Serializable]
    public sealed class CustomerSpriteEntry
    {
        public string customerId = "";
        public Sprite sprite;
    }

    [Serializable]
    public sealed class RecipeSpriteEntry
    {
        public string recipeId = "";
        public Sprite sprite;
    }
}
