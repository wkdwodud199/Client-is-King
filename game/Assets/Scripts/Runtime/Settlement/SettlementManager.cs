using ClientIsKing.DayCycle;
using ClientIsKing.Managers;
using UnityEngine;

namespace ClientIsKing.Settlement
{
    /// <summary>
    /// 정산 매니저 (싱글턴 8종 중 하나) — SettlementOps 로 위임하는 thin wrapper.
    /// GameManager 부트스트랩 오브젝트에 함께 배치된다 (SceneBuilder 소유).
    /// </summary>
    public sealed class SettlementManager : MonoBehaviour
    {
        public static SettlementManager Instance { get; private set; }

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

        public bool IsAppliedToday => State != null && SettlementOps.IsSettlementApplied(State);

        /// <summary>
        /// 오늘 정산을 적용한다. 이미 적용된 날이면 기존 재구성 경로(파라미터 무관)를 그대로 쓰고,
        /// 아니면 오늘 이벤트 효과(fx)를 조회해 운영비 배수/가산 overload 로 적용한다(task-112 E3).
        /// fx 조회 실패 시 손상 데이터로 명시적 실패(applied:false)를 반환한다.
        /// 신규 적용(result.Applied)이 성공하면 자동 저장을 트리거한다(task-113 F2 트리거 3 — 파산 포함).
        /// </summary>
        public SettlementResult ApplyDailySettlement()
        {
            var state = State;
            if (state == null)
            {
                return new SettlementResult(0, false, false, false, 0, 0, 0, 0, 0, 0, 0,
                    "게임 상태가 초기화되지 않았습니다.");
            }
            if (SettlementOps.IsSettlementApplied(state))
            {
                return SettlementOps.ApplyDailySettlement(state);
            }
            var gm = GameManager.Instance;
            if (gm == null)
            {
                return new SettlementResult(state.day, false, false, state.isBankrupt, 0, 0, 0, 0, 0, 0, 0,
                    "게임 매니저가 초기화되지 않았습니다.");
            }
            if (!gm.TryBuildTodayEventEffects(out var fx, out var reason))
            {
                return new SettlementResult(state.day, false, false, state.isBankrupt, 0, 0, 0, 0, 0, 0, 0,
                    reason);
            }
            var result = SettlementOps.ApplyDailySettlement(state, fx.OperatingCostMilli, fx.OperatingCostFlat);
            if (result.Applied)
            {
                gm.AutoSave();
            }
            return result;
        }
    }
}
