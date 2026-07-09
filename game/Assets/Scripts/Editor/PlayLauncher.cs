using UnityEditor;
using UnityEditor.SceneManagement;

namespace ClientIsKing.EditorTools
{
    /// <summary>
    /// 플레이테스트 런처 — GUI 에디터 기동 시 MainMenu 씬을 열고 바로 Play 모드로 진입한다.
    ///
    /// 실행(리포 루트 기준, batchmode 아님 — GUI):
    ///   Unity.exe -projectPath game -executeMethod ClientIsKing.EditorTools.PlayLauncher.PlayMainMenu
    /// </summary>
    public static class PlayLauncher
    {
        public static void PlayMainMenu()
        {
            EditorSceneManager.OpenScene(SceneBuilder.MainMenuPath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }
    }
}
