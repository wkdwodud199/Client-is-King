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

        public SettlementResult ApplyDailySettlement()
        {
            var state = State;
            if (state == null)
            {
                return new SettlementResult(0, false, false, false, 0, 0, 0, 0, 0, 0, 0,
                    "게임 상태가 초기화되지 않았습니다.");
            }
            return SettlementOps.ApplyDailySettlement(state);
        }
    }
}
