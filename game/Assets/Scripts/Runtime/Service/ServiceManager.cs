using System.Collections.Generic;
using ClientIsKing.Data;
using ClientIsKing.DayCycle;
using ClientIsKing.Managers;
using UnityEngine;

namespace ClientIsKing.Service
{
    /// <summary>
    /// 서비스 매니저 (싱글턴 8종 중 하나) — ServiceOps 로 위임하는 thin wrapper.
    /// GameManager 부트스트랩 오브젝트에 함께 배치된다 (SceneBuilder 소유).
    /// </summary>
    public sealed class ServiceManager : MonoBehaviour
    {
        public static ServiceManager Instance { get; private set; }

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

        public ServiceResult SkipCurrentOrder()
        {
            var state = State;
            if (state == null)
            {
                return new ServiceResult(false, "게임 상태가 초기화되지 않았습니다.", 0, 0);
            }
            return ServiceOps.SkipCurrentOrder(state);
        }
    }
}
