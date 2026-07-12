using ClientIsKing.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ClientIsKing.UI
{
    /// <summary>
    /// MainMenu 씬 — 시작/이어하기 버튼을 GameManager 에 연결하는 얇은 컨트롤러.
    /// task-113 (U3): 이어하기 블록(design.md G1/G2) — HasSaveFile/TryPeekSave 결과만 표시하고
    /// 재계산하지 않는다. 파일 없음/정상/파산/손상 4분기 전부 문구+색 병용이며, 손상·파산 세이브를
    /// 조용히 새 게임으로 넘기지 않는다 — 폴백은 항상 열려 있는 `게임 시작`(플레이어 선택)이다.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        // G2 상태 라인 색 (task-110 팔레트 hex 고정) — 상태는 문구가 전달하고 색은 보조다.
        static readonly Color32 SteamCream = new Color32(0xF4, 0xE5, 0xC2, 0xFF);
        static readonly Color32 WarningPlum = new Color32(0xA9, 0x3E, 0x58, 0xFF);

        [SerializeField] private Button startButton;

        // ── 이어하기 블록 (task-113 G1 — SceneBuilder 가 생성·주입) ─────────
        [SerializeField] private Button continueButton;
        [SerializeField] private TMP_Text saveStatusText;

        private void Awake()
        {
            startButton.onClick.AddListener(OnStartClicked);
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }
        }

        private void Start()
        {
            RefreshSaveUi();
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartClicked);
            }
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueClicked);
            }
        }

        private void OnStartClicked()
        {
            var gm = GameManager.Instance;
            // task-113 F2 트리거 1 — `게임 시작` = StartNewRun (새 런 + 즉시 저장, 기존 세이브 대체 G2).
            gm.StartNewRun();
            gm.LoadShopScene();
        }

        private void OnContinueClicked()
        {
            var gm = GameManager.Instance;
            if (gm.TryLoadGame(out _))
            {
                gm.LoadShopScene();
            }
            else
            {
                // 클릭 사이 파일이 변한 경우 등 — 사유 재표시 + 버튼 잠금 (G2, 조용한 진행 금지).
                RefreshSaveUi();
            }
        }

        /// <summary>G2 분기 4종 — HasSaveFile/TryPeekSave 결과 표시 전용 (UI 재계산 금지).</summary>
        internal void RefreshSaveUi()
        {
            var gm = GameManager.Instance;
            if (gm == null || continueButton == null)
            {
                return; // 미배선 fixture 는 no-op (기존 EditorInit 1-인자 경로 보존)
            }

            bool canContinue = false;
            string status;
            Color32 color;
            if (!gm.HasSaveFile)
            {
                status = "저장된 게임이 없습니다.";
                color = SteamCream;
            }
            else if (gm.TryPeekSave(out var summary, out var failReason))
            {
                if (summary.IsBankrupt)
                {
                    // 파산 세이브 — 정직한 기록 + 새 게임 유도 (이어하기 잠금, G2).
                    status = $"지난 게임은 파산으로 끝났습니다 (Day {summary.Day}) — 새 게임을 시작하세요.";
                    color = WarningPlum;
                }
                else
                {
                    canContinue = true;
                    status = $"Day {summary.Day} · {PhaseHudController.PhaseLabel(summary.Phase)} · 잔액 {summary.Cash:N0}원";
                    color = SteamCream;
                }
            }
            else
            {
                // 손상/버전/IO — 사유 표시 + 잠금. 파일은 보존한다 (새 런 저장이 대체, A6).
                status = $"저장 데이터를 불러올 수 없습니다: {failReason}";
                color = WarningPlum;
            }

            continueButton.interactable = canContinue;
            if (saveStatusText != null)
            {
                saveStatusText.text = status;
                saveStatusText.color = color;
            }
            FocusDefault(canContinue);
        }

        /// <summary>G1 focus — 이어하기 활성이면 이어하기(재개가 기본 행동), 비활성이면 게임 시작.</summary>
        private void FocusDefault(bool canContinue)
        {
            if (!Application.isPlaying || EventSystem.current == null)
            {
                return;
            }
            var target = canContinue ? continueButton : startButton;
            if (target != null)
            {
                EventSystem.current.SetSelectedGameObject(target.gameObject);
            }
        }

#if UNITY_EDITOR
        /// <summary>SceneBuilder 전용 참조 주입 (task-102 EditorInit 패턴) — 기존 시그니처 보존.</summary>
        internal void EditorInit(Button startButton)
        {
            EditorInit(startButton, null, null);
        }

        /// <summary>SceneBuilder 전용 참조 주입 — 이어하기 블록 포함 (task-113 U4 채택 대상).</summary>
        internal void EditorInit(Button startButton, Button continueButton, TMP_Text saveStatusText)
        {
            this.startButton = startButton;
            this.continueButton = continueButton;
            this.saveStatusText = saveStatusText;
        }
#endif
    }
}
