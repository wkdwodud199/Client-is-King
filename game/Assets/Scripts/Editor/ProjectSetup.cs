using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ClientIsKing.EditorTools
{
    /// <summary>
    /// task-102: 프로젝트 기준선 세팅을 배치 모드에서 재현 가능하게 적용한다.
    ///
    /// 실행(리포 루트 기준):
    ///   Unity.exe -batchmode -quit -nographics -projectPath game
    ///     -executeMethod ClientIsKing.EditorTools.ProjectSetup.Apply -logFile [log]
    ///
    /// 모든 단계는 재실행해도 안전하도록 멱등(idempotent)으로 작성한다.
    /// 씬/프리팹 저작은 task-104 의 SceneBuilder 몫 — 여기서는 만들지 않는다.
    /// </summary>
    public static class ProjectSetup
    {
        const string RenderingDir = "Assets/Settings/Rendering";
        const string Renderer2DPath = RenderingDir + "/Renderer2D.asset";
        const string PipelinePath = RenderingDir + "/URP-Pipeline.asset";

        public static void Apply()
        {
            EnsureFolders();
            var pipeline = EnsureUrpAssets();
            AssignPipeline(pipeline);
            ApplyIdentity();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ProjectSetup] baseline applied (URP 2D + identity)");
        }

        /// <summary>1차 폴더 구조 (design.md U3). 이미 있으면 건너뛴다.</summary>
        static void EnsureFolders()
        {
            string[] dirs =
            {
                "Assets/Scripts/Runtime",
                "Assets/Scripts/Editor",
                "Assets/Tests/EditMode",
                "Assets/Data",
                "Assets/Scenes",
                "Assets/Art/Placeholders",
                RenderingDir,
            };
            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            AssetDatabase.Refresh();
        }

        /// <summary>2D Renderer + URP 파이프라인 에셋 생성 (없을 때만).</summary>
        static UniversalRenderPipelineAsset EnsureUrpAssets()
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<Renderer2DData>(Renderer2DPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(rendererData, Renderer2DPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            return pipeline;
        }

        /// <summary>Graphics 기본 + 모든 Quality 레벨에 URP 연결.</summary>
        static void AssignPipeline(UniversalRenderPipelineAsset pipeline)
        {
            GraphicsSettings.defaultRenderPipeline = pipeline;

            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);
        }

        /// <summary>Product Name / 기본 네임스페이스 (design.md U3).</summary>
        static void ApplyIdentity()
        {
            PlayerSettings.productName = "Client is King";
            EditorSettings.projectGenerationRootNamespace = "ClientIsKing";
        }
    }
}
