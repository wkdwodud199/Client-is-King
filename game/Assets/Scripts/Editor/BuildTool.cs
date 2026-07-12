using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ClientIsKing.EditorTools
{
    /// <summary>
    /// task-115: Windows 데모 플레이어 배치(비대화형) 빌드 진입점 — `-nographics` 는 쓰지 않는다
    /// (E1 — 플레이어 빌드의 셰이더 처리 안전 우선. 빌더/테스트 배치와 다른 선택임을 명기하며,
    /// 첫 빌드에서 실측 기록한다 — 오픈 이슈).
    ///
    /// 실행(리포 루트 기준):
    ///   Unity.exe -batchmode -quit -projectPath game -buildTarget StandaloneWindows64
    ///     -executeMethod ClientIsKing.EditorTools.BuildTool.BuildWindows -logFile [log]
    ///
    /// 실패는 예외로 전파한다 — 배치 모드 exit code 비제로 보장 (SceneBuilder.SaveScene 전례).
    /// </summary>
    public static class BuildTool
    {
        /// <summary>projectPath(game/) 상대 산출 디렉터리 — `.gitignore` `game/[Bb]uild*/` 가 커버한다.</summary>
        public const string OutputDir = "Build/Windows";

        public const string PlayerExeName = "ClientIsKing.exe";

        /// <summary>
        /// EditorBuildSettings 활성 씬 투영 — 정확히 [MainMenu, Shop] 순서 2개가 아니면 예외.
        /// (SceneBuilder.MainMenuPath/ShopPath 상수 재사용 — 씬 하드캡의 빌드측 검증, EditMode 테스트 대상)
        /// </summary>
        public static string[] ScenePaths()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();
            if (scenes.Length != 2
                || scenes[0] != SceneBuilder.MainMenuPath
                || scenes[1] != SceneBuilder.ShopPath)
            {
                throw new System.InvalidOperationException(
                    "[BuildTool] Build Settings 활성 씬은 정확히 [MainMenu, Shop] 2개여야 합니다 "
                    + $"(씬 하드캡): [{string.Join(", ", scenes)}]");
            }
            return scenes;
        }

        /// <summary>
        /// BuildPipeline.BuildPlayer(StandaloneWindows64, BuildOptions.None) —
        /// summary.result != Succeeded 또는 totalErrors > 0 이면 예외. 성공 시 exe 절대경로·totalSize 로그.
        /// </summary>
        public static void BuildWindows()
        {
            // Application.dataPath = <projectPath>/Assets — projectPath 상대 OutputDir 로 투영한다.
            string projectPath = Path.GetDirectoryName(Application.dataPath);
            string exePath = Path.Combine(projectPath, OutputDir, PlayerExeName);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = ScenePaths(),
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });

            var summary = report.summary;
            if (summary.result != BuildResult.Succeeded || summary.totalErrors > 0)
            {
                throw new System.InvalidOperationException(
                    $"[BuildTool] Windows 빌드 실패: result={summary.result}, totalErrors={summary.totalErrors}");
            }
            Debug.Log($"[BuildTool] Windows 빌드 성공: {Path.GetFullPath(exePath)} ({summary.totalSize:N0} bytes)");
        }
    }
}
