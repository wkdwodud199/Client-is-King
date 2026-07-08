using ClientIsKing.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace ClientIsKing.UI
{
    /// <summary>MainMenu 씬 — 시작 버튼을 GameManager 에 연결하는 얇은 컨트롤러.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button startButton;

        private void Awake()
        {
            startButton.onClick.AddListener(OnStartClicked);
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartClicked);
            }
        }

        private void OnStartClicked()
        {
            var gm = GameManager.Instance;
            gm.StartNewGame();
            gm.LoadShopScene();
        }

#if UNITY_EDITOR
        /// <summary>SceneBuilder 전용 참조 주입 (task-102 EditorInit 패턴).</summary>
        internal void EditorInit(Button startButton)
        {
            this.startButton = startButton;
        }
#endif
    }
}
