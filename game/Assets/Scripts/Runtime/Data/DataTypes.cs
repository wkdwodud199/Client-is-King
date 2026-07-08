using System;
using UnityEngine;

namespace ClientIsKing.Data
{
    /// <summary>재료 종류 — 데모 레시피 6종을 지탱하는 9종 (task-103 설계 9단계).</summary>
    public enum IngredientKind
    {
        Rice,
        RiceCake,
        Noodle,
        Pork,
        Beef,
        FishCake,
        Seaweed,
        Vegetable,
        Gochujang,
    }

    /// <summary>재료 등급 — 데모는 C/B 만 사용 (브리프: 재정 부족으로 저급부터 시작).</summary>
    public enum IngredientGrade
    {
        C,
        B,
    }

    /// <summary>한식 장르 — 3종 + 제네럴리스트 (demo-scope.md 하드캡).</summary>
    public enum GenreKind
    {
        Gukbap,
        Bunsik,
        Noodles,
        Generalist,
    }

    /// <summary>고객 연령대 구간 (SNS 타겟팅/고객층 친화도의 축).</summary>
    public enum AgeBand
    {
        Teens,
        Twenties,
        ThirtiesForties,
        FiftiesPlus,
    }

    /// <summary>성별 타겟 (All = 무관).</summary>
    public enum GenderTarget
    {
        All,
        Female,
        Male,
    }

    /// <summary>SNS 채널 3종 (하드캡) — 실제 브랜드가 아닌 가상 채널.</summary>
    public enum SNSChannel
    {
        PhotoFeed,
        ShortForm,
        LocalBoard,
    }

    /// <summary>이벤트 4종 (demo-scope.md 하드캡).</summary>
    public enum GameEventKind
    {
        IngredientPriceSurge,
        HygieneInspection,
        RentIncrease,
        GroupCustomers,
    }

    /// <summary>
    /// 레시피가 요구하는 재료 종류와 수량.
    /// 등급(C/B) 선택과 비용 계산은 경제/인벤토리 task(105+) 몫이다.
    /// </summary>
    [Serializable]
    public struct RecipeIngredientRequirement
    {
        [SerializeField] private IngredientKind kind;
        [SerializeField, Min(1)] private int quantity;

        public IngredientKind Kind => kind;
        public int Quantity => quantity;

#if UNITY_EDITOR
        internal RecipeIngredientRequirement(IngredientKind kind, int quantity)
        {
            this.kind = kind;
            this.quantity = quantity;
        }
#endif
    }

    /// <summary>특정 고객 archetype 이 이 장르에 갖는 친화도 배수.</summary>
    [Serializable]
    public struct CustomerGenreAffinity
    {
        [SerializeField] private CustomerArchetypeDef archetype;
        [SerializeField, Min(0f)] private float multiplier;

        public CustomerArchetypeDef Archetype => archetype;
        public float Multiplier => multiplier;

#if UNITY_EDITOR
        internal CustomerGenreAffinity(CustomerArchetypeDef archetype, float multiplier)
        {
            this.archetype = archetype;
            this.multiplier = multiplier;
        }
#endif
    }

    /// <summary>SNS 캠페인의 연령/성별 타겟 친화 배수.</summary>
    [Serializable]
    public struct SNSAudienceAffinity
    {
        [SerializeField] private AgeBand ageBand;
        [SerializeField] private GenderTarget gender;
        [SerializeField, Min(0f)] private float multiplier;

        public AgeBand AgeBand => ageBand;
        public GenderTarget Gender => gender;
        public float Multiplier => multiplier;

#if UNITY_EDITOR
        internal SNSAudienceAffinity(AgeBand ageBand, GenderTarget gender, float multiplier)
        {
            this.ageBand = ageBand;
            this.gender = gender;
            this.multiplier = multiplier;
        }
#endif
    }

    /// <summary>정수 구간 [Min, Max] (예: 파티 크기).</summary>
    [Serializable]
    public struct IntRange
    {
        [SerializeField] private int min;
        [SerializeField] private int max;

        public int Min => min;
        public int Max => max;

#if UNITY_EDITOR
        internal IntRange(int min, int max)
        {
            this.min = min;
            this.max = max;
        }
#endif
    }
}
