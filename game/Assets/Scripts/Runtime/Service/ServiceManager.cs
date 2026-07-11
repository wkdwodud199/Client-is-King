using System.Collections.Generic;
using ClientIsKing.Data;
using ClientIsKing.DayCycle;
using ClientIsKing.Events;
using ClientIsKing.Genre;
using ClientIsKing.Managers;
using ClientIsKing.Social;
using UnityEngine;

namespace ClientIsKing.Service
{
    /// <summary>
    /// 서비스 매니저 (싱글턴 8종 중 하나) — ServiceOps 로 위임하는 thin wrapper.
    /// GameManager 부트스트랩 오브젝트에 함께 배치된다 (SceneBuilder 소유).
    /// task-110: 정렬된 recipe/customer 를 EditorInit 로 주입받아 plan 사전검증·원자적 주문 초기화를 제공한다.
    /// </summary>
    public sealed class ServiceManager : MonoBehaviour
    {
        public static ServiceManager Instance { get; private set; }

        [SerializeField] private List<RecipeDef> recipeDefs = new List<RecipeDef>();
        [SerializeField] private List<CustomerArchetypeDef> customerDefs = new List<CustomerArchetypeDef>();
        [SerializeField] private List<SNSCampaignDef> snsCampaignDefs = new List<SNSCampaignDef>();

        /// <summary>SceneBuilder 주입 데이터의 read-only 노출 (테스트 검증용).</summary>
        public IReadOnlyList<RecipeDef> RecipeDefs => recipeDefs;
        public IReadOnlyList<CustomerArchetypeDef> CustomerDefs => customerDefs;

        /// <summary>SceneBuilder 가 MainMenu/Shop 양쪽에 동일 주입하는 SNS 캠페인 catalog (ID ordinal 정렬, task-111 E2).</summary>
        public IReadOnlyList<SNSCampaignDef> SnsCampaignDefs => snsCampaignDefs;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        static GameState State => GameManager.Instance != null ? GameManager.Instance.State : null;

        /// <summary>
        /// 오늘(day) 주문 목록이 없거나 이전 day 것이면 새로 생성·초기화한다 (설계 25행).
        /// 같은 날 재진입(패널 토글)은 기존 진행 상태를 유지한다.
        /// </summary>
        public void EnsureServiceDay(IReadOnlyList<RecipeDef> recipes, IReadOnlyList<CustomerArchetypeDef> customers)
        {
            var state = State;
            if (state == null)
            {
                return;
            }
            if (state.serviceDay != state.day)
            {
                var orders = ServiceOps.BuildOrders(recipes, customers, state.day);
                ServiceOps.StartServiceDay(state, orders, state.day);
            }
        }

        /// <summary>
        /// 선택된 장르로 오늘의 GenreDemandPlan 을 순수 검증만으로 생성한다 (state 를 바꾸지 않음).
        /// 어제 SNS 집행 기록이 있으면 <see cref="SNSCampaignOps.TryBuildDayModifier"/> 로 재구성한 DayModifier 에
        /// 오늘 이벤트 효과(단체 손님)를 <see cref="EventOps.TryComposeDayModifier"/> 로 합성한다(task-112 E2,
        /// SNS→이벤트 순서 고정) — 기록이 없거나 미지 campaignId/이벤트 상태 손상 시 명시적 사유로 실패한다.
        /// 실패 시 out reason 에 한국어 사유를 담는다.
        /// </summary>
        public bool TryBuildDayPlan(GenreDef genre, out GenreDemandPlan plan, out string reason)
        {
            var state = State;
            if (state == null)
            {
                plan = null;
                reason = "게임 상태가 초기화되지 않았습니다.";
                return false;
            }
            if (genre == null)
            {
                plan = null;
                reason = "선택된 장르 정의를 찾을 수 없습니다.";
                return false;
            }

            var genreInput = ToGenreInput(genre);
            var recipeInputs = ToRecipeInputs(recipeDefs);
            var customerInputs = ToCustomerInputs(customerDefs);
            var snsInputs = ToSnsCampaignInputs(snsCampaignDefs);

            if (!SNSCampaignOps.TryBuildDayModifier(state.snsCampaignHistory, state.day, snsInputs, customerInputs, out var snsModifier, out reason))
            {
                plan = null;
                return false;
            }

            var gm = GameManager.Instance;
            if (gm == null)
            {
                plan = null;
                reason = "게임 매니저가 초기화되지 않았습니다.";
                return false;
            }
            if (!gm.TryBuildTodayEventEffects(out var fx, out reason))
            {
                plan = null;
                return false;
            }
            if (!EventOps.TryComposeDayModifier(snsModifier, fx, out var modifier, out reason))
            {
                plan = null;
                return false;
            }

            return GenreSelectionOps.TryBuildDemandPlan(genreInput, state.day, recipeInputs, customerInputs, modifier, out plan, out reason);
        }

        /// <summary>
        /// Market→Service 전환 직전 원자적 초기화: plan 생성 성공 시에만 실제 주문을 만들어 StartServiceDay 를 호출한다.
        /// 실패하면 state.serviceOrders 등은 전혀 건드리지 않는다.
        /// </summary>
        public bool TryStartServiceDay(GenreDef genre, out string reason)
        {
            var state = State;
            if (state == null)
            {
                reason = "게임 상태가 초기화되지 않았습니다.";
                return false;
            }
            if (!TryBuildDayPlan(genre, out var plan, out reason))
            {
                return false;
            }

            var orders = ServiceOps.BuildOrders(plan, customerDefs);
            ServiceOps.StartServiceDay(state, orders, state.day);
            reason = "";
            return true;
        }

        /// <summary>처리되지 않은 현재 주문 (없으면 null).</summary>
        public ServiceOrderState CurrentOrder => State != null ? ServiceOps.GetCurrentOrder(State) : null;

        public ServiceResult TryServeCurrentOrder(RecipeDef recipe, IngredientGrade grade)
        {
            var state = State;
            if (state == null)
            {
                return new ServiceResult(false, "게임 상태가 초기화되지 않았습니다.", 0, 0);
            }
            return ServiceOps.TryServeCurrentOrder(state, recipe, grade);
        }

        public ServiceResult TryServeCurrentOrder(RecipeDef recipe, IngredientGrade grade, GenreDef genre)
        {
            var state = State;
            if (state == null)
            {
                return new ServiceResult(false, "게임 상태가 초기화되지 않았습니다.", 0, 0);
            }
            var genreInput = genre != null ? ToGenreInput(genre) : null;
            return ServiceOps.TryServeCurrentOrder(state, recipe, grade, genreInput);
        }

        public ServiceResult SkipCurrentOrder()
        {
            var state = State;
            if (state == null)
            {
                return new ServiceResult(false, "게임 상태가 초기화되지 않았습니다.", 0, 0);
            }
            return ServiceOps.SkipCurrentOrder(state);
        }

        // ── Unity SO → 순수 GenreSelectionOps 입력 투영 ─────────────────────

        internal static GenreDefInput ToGenreInput(GenreDef genre)
        {
            var affinities = new List<GenreAffinityInput>();
            foreach (var affinity in genre.CustomerAffinities)
            {
                if (affinity.Archetype != null)
                {
                    affinities.Add(new GenreAffinityInput(affinity.Archetype.Id, affinity.Multiplier));
                }
            }
            return new GenreDefInput
            {
                Id = genre.Id,
                IsGeneralist = genre.Kind == GenreKind.Generalist,
                CookTimeMultiplier = genre.CookTimeMultiplier,
                PricePerCustomerMultiplier = genre.PricePerCustomerMultiplier,
                CustomerAffinities = affinities,
            };
        }

        internal static List<RecipeDefInput> ToRecipeInputs(IReadOnlyList<RecipeDef> recipes)
        {
            var result = new List<RecipeDefInput>();
            foreach (var recipe in recipes)
            {
                if (recipe == null)
                {
                    continue;
                }
                result.Add(new RecipeDefInput
                {
                    Id = recipe.Id,
                    GenreId = recipe.Genre != null ? recipe.Genre.Id : "",
                    BasePrice = recipe.BasePrice,
                });
            }
            return result;
        }

        internal static List<CustomerDefInput> ToCustomerInputs(IReadOnlyList<CustomerArchetypeDef> customers)
        {
            var result = new List<CustomerDefInput>();
            foreach (var customer in customers)
            {
                if (customer == null)
                {
                    continue;
                }
                result.Add(new CustomerDefInput
                {
                    Id = customer.Id,
                    BaseSpawnWeight = customer.BaseSpawnWeight,
                    AgeBand = customer.AgeBand,
                    Gender = customer.Gender,
                });
            }
            return result;
        }

        // ── Unity SO → 순수 SNSCampaignOps 입력 투영 (task-111 E2) ──────────

        internal static SNSCampaignDefInput ToSnsCampaignInput(SNSCampaignDef def)
        {
            var rows = new List<SNSRawAffinityInput>();
            foreach (var row in def.AudienceAffinities)
            {
                rows.Add(new SNSRawAffinityInput(row.AgeBand, row.Gender, row.Multiplier));
            }
            return new SNSCampaignDefInput
            {
                Id = def.Id,
                DisplayName = def.DisplayName,
                BaseCost = def.BaseCost,
                BaseReach = def.BaseReach,
                RepeatDecay = def.RepeatDecay,
                AudienceAffinities = rows,
            };
        }

        internal static List<SNSCampaignDefInput> ToSnsCampaignInputs(IReadOnlyList<SNSCampaignDef> defs)
        {
            var result = new List<SNSCampaignDefInput>();
            foreach (var def in defs)
            {
                if (def != null)
                {
                    result.Add(ToSnsCampaignInput(def));
                }
            }
            return result;
        }

        // ── SNS 캠페인 집행/미리보기 (task-111 E2) ──────────────────────────

        /// <summary>catalog 에서 id 를 조회해 <see cref="SNSCampaignOps.TryExecute"/> 로 위임한다. 미지 id 는 상태 불변 실패.</summary>
        public bool TryExecuteSnsCampaign(string campaignId, out SNSCampaignResult result)
        {
            var state = State;
            if (state == null)
            {
                result = null;
                return false;
            }
            var def = FindSnsDef(campaignId);
            if (def == null)
            {
                result = new SNSCampaignResult(false, $"알 수 없는 SNS 캠페인 '{campaignId}' 입니다.", campaignId ?? "", 0, 0, 0, 0, 0);
                return false;
            }
            result = SNSCampaignOps.TryExecute(state, ToSnsCampaignInput(def));
            return result.Success;
        }

        /// <summary>
        /// catalog 에서 id 를 조회해 자신의 def·CustomerDefs 를 투영한 뒤 <see cref="SNSCampaignOps.TryBuildPreview"/> 에
        /// 전달하고 DTO 를 그대로 반환한다. top-target 계산은 순수 계층에만 존재하며 이 매니저는 재계산하지 않는다.
        /// </summary>
        public bool TryGetSnsPreview(string campaignId, out SNSCampaignPreview preview, out string reason)
        {
            var state = State;
            if (state == null)
            {
                preview = null;
                reason = "게임 상태가 초기화되지 않았습니다.";
                return false;
            }
            var def = FindSnsDef(campaignId);
            if (def == null)
            {
                preview = null;
                reason = $"알 수 없는 SNS 캠페인 '{campaignId}' 입니다.";
                return false;
            }
            var customerInputs = ToCustomerInputs(customerDefs);
            return SNSCampaignOps.TryBuildPreview(state, ToSnsCampaignInput(def), customerInputs, out preview, out reason);
        }

        SNSCampaignDef FindSnsDef(string campaignId)
        {
            if (string.IsNullOrEmpty(campaignId))
            {
                return null;
            }
            foreach (var def in snsCampaignDefs)
            {
                if (def != null && string.Equals(def.Id, campaignId, System.StringComparison.Ordinal))
                {
                    return def;
                }
            }
            return null;
        }

#if UNITY_EDITOR
        /// <summary>SceneBuilder 전용 참조 주입 — 정렬된 recipe/customer 목록을 MainMenu/Shop 양쪽에 동일하게 주입한다.</summary>
        internal void EditorInit(List<RecipeDef> recipeDefs, List<CustomerArchetypeDef> customerDefs)
        {
            this.recipeDefs = recipeDefs;
            this.customerDefs = customerDefs;
        }

        /// <summary>SceneBuilder 전용 참조 주입 (task-111) — SNS catalog 를 포함한 3-인자 overload.</summary>
        internal void EditorInit(List<RecipeDef> recipeDefs, List<CustomerArchetypeDef> customerDefs, List<SNSCampaignDef> snsCampaignDefs)
        {
            this.recipeDefs = recipeDefs;
            this.customerDefs = customerDefs;
            this.snsCampaignDefs = snsCampaignDefs;
        }
#endif
    }
}
