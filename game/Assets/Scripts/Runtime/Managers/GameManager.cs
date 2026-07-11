using System.Collections.Generic;
using ClientIsKing.Data;
using ClientIsKing.DayCycle;
using ClientIsKing.Service;
using ClientIsKing.Settlement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClientIsKing.Managers
{
    /// <summary>
    /// 게임 전역 매니저 (싱글턴 MonoBehaviour 8종 중 첫 번째 — 브리프 규약).
    /// task-104 시점에는 상태 보관·phase 진행·씬 로드의 얇은 래퍼만 제공한다.
    /// 경제/인벤토리/서비스 로직은 후속 매니저(task-105+)가 담당한다.
    /// task-110: 정렬된 GenreDef catalog 의 유일한 런타임 소유자이며, Market→Service 전환 직전
    /// ServiceManager.TryStartServiceDay 를 원자적으로 성공시킨 경우에만 phase event 를 발행한다.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private List<GenreDef> genreCatalog = new List<GenreDef>();

        private GameState state;
        private DayPhaseMachine machine;

        /// <summary>현재 런타임 상태 (Awake 이후 항상 non-null).</summary>
        public GameState State => state;

        /// <summary>정렬된 genre 4종 catalog (SceneBuilder 가 MainMenu/Shop 양쪽에 동일하게 주입).</summary>
        public IReadOnlyList<GenreDef> GenreCatalog => genreCatalog;

        private void Awake()
        {
            // 두 씬 모두 GameManager 부트스트랩을 포함하므로(어느 씬에서 시작해도 동작),
            // 이미 살아있는 인스턴스가 있으면 중복 쪽을 제거한다.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (Application.isPlaying)
            {
                // EditMode 테스트(FirstPlayableLoopTests)에서도 생성 가능하도록 Play 모드에서만 적용.
                DontDestroyOnLoad(gameObject);
            }
            if (state == null)
            {
                StartNewGame();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>새 게임 상태로 초기화한다 (day 1, Market).</summary>
        public void StartNewGame()
        {
            state = new GameState();
            machine = new DayPhaseMachine(state);
        }

        /// <summary>catalog 에서 id 로 GenreDef 를 조회한다 (ordinal 비교).</summary>
        public bool TryGetGenre(string id, out GenreDef def)
        {
            if (!string.IsNullOrEmpty(id))
            {
                for (int i = 0; i < genreCatalog.Count; i++)
                {
                    if (genreCatalog[i] != null && genreCatalog[i].Id == id)
                    {
                        def = genreCatalog[i];
                        return true;
                    }
                }
            }
            def = null;
            return false;
        }

        /// <summary>
        /// 현재 phase 에서 다음 phase 로 진행 가능한지 도메인 규칙으로 판정한다 (design.md G3/H9).
        /// Market→Service: 선택 genre 가 catalog 에 존재하고 TryBuildDayPlan 이 성공해야 한다.
        /// Service→Settlement: serviceDay==day, 생성 주문 수==plan.orderCount, 열린 주문 없음.
        /// 그 외 phase 전환은 항상 허용한다.
        /// </summary>
        public bool CanAdvancePhase(out string reason)
        {
            reason = "";
            if (state == null)
            {
                reason = "게임 상태가 초기화되지 않았습니다.";
                return false;
            }
            if (state.isBankrupt)
            {
                reason = "파산 상태에서는 진행할 수 없습니다.";
                return false;
            }

            switch (state.currentPhase)
            {
                case DayPhase.Market:
                    return CanAdvanceFromMarket(out reason);
                case DayPhase.Service:
                    return CanAdvanceFromService(out reason);
                default:
                    return true;
            }
        }

        bool CanAdvanceFromMarket(out string reason)
        {
            if (!TryGetGenre(state.selectedGenreId, out var genre))
            {
                reason = "먼저 전문 분야를 선택하세요.";
                return false;
            }
            var service = ServiceManager.Instance;
            if (service == null)
            {
                reason = "서비스 매니저가 초기화되지 않았습니다.";
                return false;
            }
            if (!service.TryBuildDayPlan(genre, out _, out reason))
            {
                return false;
            }
            reason = "";
            return true;
        }

        bool CanAdvanceFromService(out string reason)
        {
            if (state.serviceDay != state.day)
            {
                reason = "오늘 영업이 아직 시작되지 않았습니다.";
                return false;
            }
            var service = ServiceManager.Instance;
            if (service != null && TryGetGenre(state.selectedGenreId, out var genre)
                && service.TryBuildDayPlan(genre, out var plan, out _)
                && state.serviceOrders.Count != plan.OrderCount)
            {
                reason = "생성된 주문 수가 예상과 다릅니다.";
                return false;
            }
            if (ServiceOps.HasOpenOrders(state))
            {
                reason = "아직 처리하지 않은 주문이 있습니다.";
                return false;
            }
            reason = "";
            return true;
        }

        /// <summary>
        /// 다음 phase 로 진행한다 (이벤트 발행은 상태 머신이 담당).
        /// task-107 게이트: 파산이면 진행/이벤트 없이 현재 phase 를 유지하고,
        /// Settlement 이탈 전에 오늘 정산이 미적용이면 먼저 적용한다 (그 결과 파산이면 머문다).
        /// task-110 게이트: Market→Service 직전에 CanAdvancePhase 를 만족하고 TryStartServiceDay 가
        /// 원자적으로 성공한 경우에만 진행한다. 그 외 phase 는 기존 규칙을 유지한다.
        /// </summary>
        public DayPhase AdvancePhase()
        {
            if (state.isBankrupt)
            {
                return state.currentPhase;
            }
            if (state.currentPhase == DayPhase.Settlement && !SettlementOps.IsSettlementApplied(state))
            {
                var result = SettlementOps.ApplyDailySettlement(state);
                if (result.Bankrupt)
                {
                    return state.currentPhase;
                }
            }

            if (state.currentPhase == DayPhase.Market)
            {
                if (!CanAdvanceFromMarket(out _))
                {
                    return state.currentPhase;
                }
                var service = ServiceManager.Instance;
                if (service == null || !TryGetGenre(state.selectedGenreId, out var genre)
                    || !service.TryStartServiceDay(genre, out _))
                {
                    return state.currentPhase;
                }
            }
            else if (state.currentPhase == DayPhase.Service)
            {
                if (!CanAdvanceFromService(out _))
                {
                    return state.currentPhase;
                }
            }

            return machine.Advance();
        }

        /// <summary>Shop 씬을 로드한다 (Build Settings 등록 전제 — SceneBuilder 가 보장).</summary>
        public void LoadShopScene()
        {
            SceneManager.LoadScene("Shop");
        }

#if UNITY_EDITOR
        /// <summary>SceneBuilder 전용 참조 주입 — 정렬된 genre 4종을 MainMenu/Shop 양쪽에 동일하게 주입한다.</summary>
        internal void EditorInit(List<GenreDef> genreCatalog)
        {
            this.genreCatalog = genreCatalog;
        }
#endif
    }
}
