using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ClientIsKing.UI
{
    /// <summary>
    /// MainMenu 크레딧 패널(모달 오버레이) 컨트롤러 — task-117 E절.
    /// 문구는 SceneBuilder 가 빌드 타임에 씬에 굽고(B절), 이 컨트롤러는 열기/닫기 토글 + focus +
    /// 모달 입력 격리만 소유한다(도메인 무접촉 — MainMenuController 는 무수정, 세이브 플로우 무접촉).
    /// dim raycast 는 pointer 만 차단하므로, 열려 있는 동안 배경 버튼 3종(게임 시작/이어하기/크레딧)의
    /// interactable 을 저장 후 잠그고 닫을 때 저장값으로 정확히 복원한다 — 이어하기는 원래 disabled
    /// 일 수 있어 무조건 true 복원 금지(Codex P0-1). 닫기 버튼은 Navigation None(SceneBuilder 소유)
    /// 이라 방향키 focus 탈출도 불가하다.
    /// </summary>
    public sealed class CreditsController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button startButton;
        [SerializeField] private Button continueButton;

        // 열 때 저장한 배경 버튼 interactable 원본값 — 닫을 때 정확 복원 (E절).
        private bool savedStartInteractable;
        private bool savedContinueInteractable;
        private bool savedOpenInteractable;

        private void OnEnable()
        {
            if (openButton != null)
            {
                openButton.onClick.AddListener(OnOpenClicked);
            }
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseClicked);
            }
        }

        private void OnDisable()
        {
            if (openButton != null)
            {
                openButton.onClick.RemoveListener(OnOpenClicked);
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(OnCloseClicked);
            }
        }

        private void Update()
        {
            // Cancel 닫기 (Codex P1) — 기본 InputManager "Cancel" 축 = Esc + 게임패드 B.
            // 버튼 리스너와 같은 CloseNow 경로를 탄다 (신규 InputSystem 도입 없음 — 제외 절).
            if (panelRoot != null && panelRoot.activeSelf && Input.GetButtonDown("Cancel"))
            {
                CloseNow();
            }
        }

        private void OnOpenClicked()
        {
            OpenNow();
        }

        private void OnCloseClicked()
        {
            CloseNow();
        }

        /// <summary>
        /// 열기 1회분 (버튼 리스너·테스트 공용 경로 — EndingOverlay RefreshNow 전례).
        /// 배경 버튼 3종의 현재 interactable 을 저장한 뒤 전부 잠그고 패널 표시 + 닫기 버튼 focus.
        /// </summary>
        internal void OpenNow()
        {
            if (panelRoot == null || panelRoot.activeSelf)
            {
                return; // 미배선 fixture no-op + 중복 열기 시 저장값 덮어쓰기 방지
            }
            if (startButton != null)
            {
                savedStartInteractable = startButton.interactable;
                startButton.interactable = false;
            }
            if (continueButton != null)
            {
                savedContinueInteractable = continueButton.interactable;
                continueButton.interactable = false;
            }
            if (openButton != null)
            {
                savedOpenInteractable = openButton.interactable;
                openButton.interactable = false;
            }
            panelRoot.SetActive(true);
            Focus(closeButton);
        }

        /// <summary>닫기 1회분 — 저장값 정확 복원 → 패널 숨김 → 크레딧 버튼 focus 복귀 (E절).</summary>
        internal void CloseNow()
        {
            if (panelRoot == null || !panelRoot.activeSelf)
            {
                return;
            }
            if (startButton != null)
            {
                startButton.interactable = savedStartInteractable;
            }
            if (continueButton != null)
            {
                continueButton.interactable = savedContinueInteractable;
            }
            if (openButton != null)
            {
                openButton.interactable = savedOpenInteractable;
            }
            panelRoot.SetActive(false);
            Focus(openButton);
        }

        /// <summary>focus 이동 — EditMode/EventSystem 부재 시 no-op (FocusMainMenuButton 전례).</summary>
        private static void Focus(Button target)
        {
            if (!Application.isPlaying || EventSystem.current == null)
            {
                return;
            }
            if (target != null)
            {
                EventSystem.current.SetSelectedGameObject(target.gameObject);
            }
        }

#if UNITY_EDITOR
        /// <summary>SceneBuilder 전용 참조 주입 (task-102 EditorInit 패턴).</summary>
        internal void EditorInit(GameObject panelRoot, Button openButton, Button closeButton,
            Button startButton, Button continueButton)
        {
            this.panelRoot = panelRoot;
            this.openButton = openButton;
            this.closeButton = closeButton;
            this.startButton = startButton;
            this.continueButton = continueButton;
        }
#endif
    }
}
