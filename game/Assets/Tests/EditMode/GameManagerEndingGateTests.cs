using System;
using System.Collections.Generic;
using System.Linq;
using ClientIsKing.Data;
using ClientIsKing.DayCycle;
using ClientIsKing.Managers;
using ClientIsKing.Service;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-115 C3: GameManager 클리어 게이트 — 파산 게이트(FirstPlayableLoopTests
    /// Bankruptcy_Blocks_Advance_And_Emits_No_Events 전례) 미러. CanAdvancePhase/AdvancePhase 가
    /// 클리어 상태를 파산과 동일하게 진행 차단·이벤트 미발행·상태 불변으로 처리하는지 고정한다.
    /// </summary>
    public class GameManagerEndingGateTests
    {
        static List<T> LoadAll<T>(string folder) where T : UnityEngine.Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder })
                .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null)
                .ToList();
        }

        static GameManager CreateGameManager(out GameObject go)
        {
            // FirstPlayableLoopTests.CreateGameManager 전례 — 다른 fixture 의 singleton 잔존을 정리한다.
            if (GameManager.Instance != null)
            {
                UnityEngine.Object.DestroyImmediate(GameManager.Instance.gameObject);
            }
            if (ServiceManager.Instance != null && ServiceManager.Instance.gameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(ServiceManager.Instance.gameObject);
            }

            go = new GameObject("gm-under-test");
            var gm = go.AddComponent<GameManager>();
            gm.StartNewGame();
            var genres = LoadAll<GenreDef>("Assets/Data/Definitions/Genres");
            genres.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            var events = LoadAll<GameEventDef>("Assets/Data/Definitions/Events");
            events.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            gm.EditorInit(genres, events);
            ForceSingletonInstance(typeof(GameManager), gm);

            var service = go.AddComponent<ServiceManager>();
            var recipes = LoadAll<RecipeDef>("Assets/Data/Definitions/Recipes");
            var customers = LoadAll<CustomerArchetypeDef>("Assets/Data/Definitions/Customers");
            recipes.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            customers.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            service.EditorInit(recipes, customers);
            ForceSingletonInstance(typeof(ServiceManager), service);

            return gm;
        }

        static void ForceSingletonInstance(System.Type managerType, object instance)
        {
            var prop = managerType.GetProperty("Instance",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (ReferenceEquals(prop.GetValue(null), instance))
            {
                return;
            }
            prop.SetValue(null, instance, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.SetProperty, null, null, null);
        }

        /// <summary>클리어 fixture: day=N·settlementDay=N·daysCompleted=N (Day N 정산이 이미 성공 적용된 상태).</summary>
        static void MakeCleared(ClientIsKing.DayCycle.GameState state)
        {
            state.day = EndingOps.ClearTargetDays;
            state.settlementDay = EndingOps.ClearTargetDays;
            state.daysCompleted = EndingOps.ClearTargetDays;
            state.currentPhase = DayPhase.Settlement;
            state.cash = 50000;
            state.isBankrupt = false;
        }

        [Test]
        public void CanAdvancePhase_False_With_Exact_Reason_When_Cleared()
        {
            var gm = CreateGameManager(out var go);
            try
            {
                MakeCleared(gm.State);

                bool can = gm.CanAdvancePhase(out var reason);

                Assert.IsFalse(can);
                Assert.AreEqual("데모 클리어 상태에서는 진행할 수 없습니다.", reason);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AdvancePhase_Keeps_Phase_Emits_No_Events_State_Unchanged_When_Cleared()
        {
            var gm = CreateGameManager(out var go);
            int events = 0;
            Action<DayPhaseChangedEventArgs> handler = _ => events++;
            GameEvents.DayPhaseChanged += handler;
            try
            {
                var state = gm.State;
                MakeCleared(state);

                int day = state.day;
                int settlementDay = state.settlementDay;
                int daysCompleted = state.daysCompleted;
                int cash = state.cash;
                var phase = state.currentPhase;

                var after = gm.AdvancePhase();

                Assert.AreEqual(phase, after, "클리어 상태에서 진행하면 현재 phase 를 유지한다");
                Assert.AreEqual(0, events, "차단된 진행은 DayPhaseChanged 를 발행하지 않는다");
                Assert.AreEqual(day, state.day, "day 불변");
                Assert.AreEqual(settlementDay, state.settlementDay, "settlementDay 불변");
                Assert.AreEqual(daysCompleted, state.daysCompleted, "daysCompleted 불변");
                Assert.AreEqual(cash, state.cash, "cash 불변");
                Assert.AreEqual(phase, state.currentPhase, "currentPhase 필드 불변");
                Assert.IsFalse(state.isBankrupt, "클리어는 파산이 아니다");
            }
            finally
            {
                GameEvents.DayPhaseChanged -= handler;
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Gate_Inactive_On_Day_N_Before_Settlement_Applied()
        {
            // Day N 의 Market/Service 중에는 daysCompleted == N-1 이라 게이트가 절대 선발동하지 않는다(C2 결정론).
            var gm = CreateGameManager(out var go);
            try
            {
                var state = gm.State;
                state.day = EndingOps.ClearTargetDays;
                state.daysCompleted = EndingOps.ClearTargetDays - 1;
                state.settlementDay = EndingOps.ClearTargetDays - 1;
                state.currentPhase = DayPhase.Market;
                state.isBankrupt = false;

                Assert.AreEqual(RunEndingStatus.None, EndingOps.GetStatus(state));

                bool can = gm.CanAdvancePhase(out var reason);
                // Market 게이트는 장르 미선택으로 실패하지만, 사유가 클리어 사유가 아니어야 한다(게이트 미발동 확인).
                Assert.AreNotEqual("데모 클리어 상태에서는 진행할 수 없습니다.", reason);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AdvancePhase_Settlement_Inline_Apply_To_Cleared_Stays_In_Settlement()
        {
            // Day N 정산이 AdvancePhase 호출 안에서 인라인 적용되어 클리어가 되는 경우도 Settlement 에 머문다.
            var gm = CreateGameManager(out var go);
            try
            {
                var state = gm.State;
                state.day = EndingOps.ClearTargetDays;
                state.daysCompleted = EndingOps.ClearTargetDays - 1;
                state.settlementDay = EndingOps.ClearTargetDays - 1;
                state.currentPhase = DayPhase.Settlement;
                state.cash = 100000; // 운영비를 넉넉히 감당할 잔액 — 정산 성공 전제
                state.isBankrupt = false;

                var after = gm.AdvancePhase();

                Assert.AreEqual(DayPhase.Settlement, after, "Day N 정산 인라인 적용으로 클리어된 직후 Settlement 에 머문다");
                Assert.AreEqual(RunEndingStatus.Cleared, EndingOps.GetStatus(state));
                Assert.AreEqual(EndingOps.ClearTargetDays, state.daysCompleted);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Bankruptcy_Gate_No_Regression_With_Ending_Gate_Present()
        {
            // FirstPlayableLoopTests.Bankruptcy_Blocks_Advance_And_Emits_No_Events 미러 — 파산 게이트 무회귀.
            var gm = CreateGameManager(out var go);
            int events = 0;
            Action<DayPhaseChangedEventArgs> handler = _ => events++;
            GameEvents.DayPhaseChanged += handler;
            try
            {
                var state = gm.State;
                state.cash = 100; // 운영비 미달
                state.currentPhase = DayPhase.Settlement;

                var afterFirst = gm.AdvancePhase(); // 정산 선적용 → 파산 → 진행 차단

                Assert.AreEqual(DayPhase.Settlement, afterFirst, "파산 정산 후 Settlement 에 머문다");
                Assert.IsTrue(state.isBankrupt);
                Assert.AreEqual(0, state.cash);
                Assert.AreEqual(RunEndingStatus.Bankrupt, EndingOps.GetStatus(state));

                bool can = gm.CanAdvancePhase(out var reason);
                Assert.IsFalse(can);
                Assert.AreEqual("파산 상태에서는 진행할 수 없습니다.", reason, "파산 사유가 여전히 우선한다");

                var afterSecond = gm.AdvancePhase();

                Assert.AreEqual(DayPhase.Settlement, afterSecond);
                Assert.AreEqual(0, events, "차단된 진행은 DayPhaseChanged 를 발행하지 않는다");
            }
            finally
            {
                GameEvents.DayPhaseChanged -= handler;
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
