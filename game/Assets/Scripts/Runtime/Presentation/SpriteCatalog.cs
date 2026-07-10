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
        public Sprite sprite; // idle / fallback (우향 정지 프레임)
        public Sprite[] walkFrames; // 우향 걷기 순환 프레임 (task-109). 비어 있으면 sprite 로 폴백.
    }

    [Serializable]
    public sealed class RecipeSpriteEntry
    {
        public string recipeId = "";
        public Sprite sprite;
    }
}
