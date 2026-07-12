using ClientIsKing.EditorTools;
using NUnit.Framework;
using UnityEditor;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-115 U5: BuildTool 순수부 — ScenePaths(2개·[MainMenu, Shop] 순서·SceneBuilder 상수 일치)와
    /// 산출 경로 상수 핀. BuildWindows 실행 자체는 수 분이 걸려 EditMode 로 돌리지 않는다 —
    /// 배치 스모크 게이트(design.md E1/G절, G3)가 담당한다.
    /// </summary>
    public class BuildToolTests
    {
        [OneTimeSetUp]
        public void BuildScenesOnce()
        {
            SceneBuilder.Apply(); // Build Settings 를 [MainMenu, Shop] 정본으로 고정
        }

        [Test]
        public void ScenePaths_Returns_Exactly_MainMenu_Then_Shop()
        {
            var paths = BuildTool.ScenePaths();
            Assert.AreEqual(2, paths.Length, "빌드 씬은 정확히 2개 (씬 하드캡)");
            Assert.AreEqual(SceneBuilder.MainMenuPath, paths[0], "MainMenu 선두 (부트 씬)");
            Assert.AreEqual(SceneBuilder.ShopPath, paths[1]);
        }

        [Test]
        public void ScenePaths_Throws_When_Build_Settings_Deviate()
        {
            var original = EditorBuildSettings.scenes;
            try
            {
                // 1개뿐
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(SceneBuilder.ShopPath, true),
                };
                Assert.Throws<System.InvalidOperationException>(() => BuildTool.ScenePaths(), "씬 1개는 예외");

                // 순서 뒤집힘 — MainMenu 선두 계약 위반
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(SceneBuilder.ShopPath, true),
                    new EditorBuildSettingsScene(SceneBuilder.MainMenuPath, true),
                };
                Assert.Throws<System.InvalidOperationException>(() => BuildTool.ScenePaths(), "순서 뒤집힘은 예외");

                // 비활성 씬은 투영에서 제외 — 활성 1개만 남으면 예외
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(SceneBuilder.MainMenuPath, true),
                    new EditorBuildSettingsScene(SceneBuilder.ShopPath, false),
                };
                Assert.Throws<System.InvalidOperationException>(() => BuildTool.ScenePaths(), "비활성 씬은 제외되어야 한다");
            }
            finally
            {
                EditorBuildSettings.scenes = original; // 원복 — 다른 테스트의 Build Settings 전제 보존
            }
        }

        [Test]
        public void Output_Path_Constants_Are_Pinned()
        {
            Assert.AreEqual("Build/Windows", BuildTool.OutputDir,
                "E1 산출 디렉터리 핀 — projectPath(game/) 상대, .gitignore `game/[Bb]uild*/` 가 커버");
            Assert.AreEqual("ClientIsKing.exe", BuildTool.PlayerExeName, "E1 플레이어 exe 이름 핀");
        }
    }
}
