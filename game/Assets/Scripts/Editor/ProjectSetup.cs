using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;

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

        // TMP 리소스/한글 폰트 (M1 게이트 핫픽스 — task-112 폰트 파트 선행, 브리프: Galmuri OFL TMP Dynamic)
        const string TmpResourcesDir = "Assets/TextMesh Pro";
        const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        const string FontsDir = "Assets/Art/Fonts";
        const string GalmuriTtfPath = FontsDir + "/Galmuri11.ttf";
        const string GalmuriFontAssetPath = FontsDir + "/Galmuri11 SDF.asset";

        public static void Apply()
        {
            EnsureFolders();
            EnsureTmpEssentials();
            var pipeline = EnsureUrpAssets();
            AssignPipeline(pipeline);
            ApplyIdentity();
            EnsureKoreanDefaultFont();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ProjectSetup] baseline applied (URP 2D + identity + TMP/Galmuri)");
        }

        /// <summary>
        /// TMP Essential Resources 임포트 (기본 셰이더/설정 — ugui 2.0 은 자동 포함하지 않음,
        /// M1 플레이테스트에서 발견). Examples & Extras 는 임포트하지 않는다.
        /// </summary>
        static void EnsureTmpEssentials()
        {
            if (AssetDatabase.IsValidFolder(TmpResourcesDir))
            {
                return;
            }
            string package = Path.GetFullPath(
                "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
            if (!File.Exists(package))
            {
                throw new FileNotFoundException("TMP Essential Resources 패키지를 찾을 수 없다", package);
            }
            // ImportPackage(false) 는 배치에서 비동기(큐잉)라 -quit 전에 완료가 보장되지 않는다 —
            // 내부 동기 API ImportPackageImmediately 를 리플렉션으로 사용한다 (M1 핫픽스에서 실측).
            var immediate = typeof(AssetDatabase).GetMethod(
                "ImportPackageImmediately",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (immediate != null)
            {
                immediate.Invoke(null, new object[] { package });
            }
            else
            {
                AssetDatabase.ImportPackage(package, false); // 구버전 폴백 (비동기 — 아래 검증이 잡는다)
            }
            AssetDatabase.Refresh();
            if (!AssetDatabase.IsValidFolder(TmpResourcesDir))
            {
                throw new System.InvalidOperationException(
                    "[ProjectSetup] TMP Essential Resources 임포트가 완료되지 않았다 — " +
                    "unitypackage 를 수동(tar) 추출해 Assets/TextMesh Pro 를 채운 뒤 재실행하라");
            }
            Debug.Log("[ProjectSetup] TMP Essential Resources imported");
        }

        /// <summary>
        /// Galmuri11(OFL) 을 TMP Dynamic 폰트 에셋으로 만들고 TMP 기본 폰트로 지정한다.
        /// TMP 기본 LiberationSans 에는 한글 글리프가 없어 한글 UI 렌더링에 필수.
        /// </summary>
        static void EnsureKoreanDefaultFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(GalmuriTtfPath);
            if (font == null)
            {
                throw new FileNotFoundException("Galmuri11.ttf 가 없다 (Assets/Art/Fonts, OFL)", GalmuriTtfPath);
            }

            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(GalmuriFontAssetPath);
            if (fontAsset == null)
            {
                fontAsset = TMP_FontAsset.CreateFontAsset(
                    font, 64, 6, GlyphRenderMode.SDFAA, 1024, 1024,
                    AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: true);
                fontAsset.name = "Galmuri11 SDF";
                AssetDatabase.CreateAsset(fontAsset, GalmuriFontAssetPath);
                fontAsset.atlasTexture.name = fontAsset.name + " Atlas";
                fontAsset.material.name = fontAsset.name + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                AssetDatabase.SaveAssets();
                Debug.Log("[ProjectSetup] Galmuri11 SDF font asset created (dynamic)");
            }

            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
            if (settings == null)
            {
                throw new FileNotFoundException("TMP Settings 를 찾을 수 없다 (essentials 임포트 전제)", TmpSettingsPath);
            }
            var so = new SerializedObject(settings);
            so.FindProperty("m_defaultFontAsset").objectReferenceValue = fontAsset;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            Debug.Log("[ProjectSetup] TMP default font = Galmuri11 SDF");
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
