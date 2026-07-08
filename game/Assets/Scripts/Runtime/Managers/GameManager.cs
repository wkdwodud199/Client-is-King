using ClientIsKing.DayCycle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClientIsKing.Managers
{
    /// <summary>
    /// 게임 전역 매니저 (싱글턴 MonoBehaviour 8종 중 첫 번째 — 브리프 규약).
    /// task-104 시점에는 상태 보관·phase 진행·씬 로드의 얇은 래퍼만 제공한다.
    /// 경제/인벤토리/서비스 로직은 후속 매니저(task-105+)가 담당한다.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private GameState state;
        private DayPhaseMachine machine;

        /// <summary>현재 런타임 상태 (Awake 이후 항상 non-null).</summary>
        public GameState State => state;

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
            DontDestroyOnLoad(gameObject);
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

        /// <summary>다음 phase 로 진행한다 (이벤트 발행은 상태 머신이 담당).</summary>
        public DayPhase AdvancePhase()
        {
            return machine.Advance();
        }

        /// <summary>Shop 씬을 로드한다 (Build Settings 등록 전제 — SceneBuilder 가 보장).</summary>
        public void LoadShopScene()
        {
            SceneManager.LoadScene("Shop");
        }
    }
}
