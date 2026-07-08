using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-102 smoke: 프로젝트 기준선(버전·URP 연결·픽셀 표준 전제·식별자)만 검증한다.
    /// 게임 도메인 로직 테스트는 task-103 이후 각 task 의 몫이다.
    /// </summary>
    public class ProjectBaselineTests
    {
        [Test]
        public void EditorVersion_Matches_Brief()
        {
            // 브리프/설계 고정: Unity 6 에디터 6000.3.8f1
            StringAssert.StartsWith("6000.3.8", Application.unityVersion);
        }

        [Test]
        public void DefaultRenderPipeline_Is_URP()
        {
            var rp = GraphicsSettings.defaultRenderPipeline;
            Assert.IsNotNull(rp, "defaultRenderPipeline 이 비어 있습니다 — ProjectSetup.Apply 미실행?");
            Assert.IsInstanceOf<UniversalRenderPipelineAsset>(rp);
        }

        [Test]
        public void PixelPerfectCamera_From_URP_Accepts_PixelStandard()
        {
            // 브리프 픽셀 표준: PPU 32, 기준 해상도 640x360.
            // URP 17.x 는 PixelPerfectCamera 를 내장하므로 별도 2d.pixel-perfect 패키지가 필요 없다.
            var go = new GameObject("ppc-probe");
            try
            {
                go.AddComponent<Camera>();
                var ppc = go.AddComponent<PixelPerfectCamera>();
                ppc.assetsPPU = 32;
                ppc.refResolutionX = 640;
                ppc.refResolutionY = 360;

                Assert.AreEqual(32, ppc.assetsPPU);
                Assert.AreEqual(640, ppc.refResolutionX);
                Assert.AreEqual(360, ppc.refResolutionY);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ProductName_Is_ClientIsKing()
        {
            Assert.AreEqual("Client is King", PlayerSettings.productName);
        }

        [Test]
        public void RootNamespace_Is_ClientIsKing()
        {
            Assert.AreEqual("ClientIsKing", EditorSettings.projectGenerationRootNamespace);
        }

        [Test]
        public void TextMeshPro_Available_Via_UGUI_Merge()
        {
            // Unity 6: TextMeshPro 는 com.unity.ugui 2.x 에 병합됨 — TMPro 타입 로드로 확인.
            var tmp = System.AppDomain.CurrentDomain.GetAssemblies()
                .Select(asm => asm.GetType("TMPro.TMP_Settings"))
                .FirstOrDefault(t => t != null);
            Assert.IsNotNull(tmp, "TMPro.TMP_Settings 타입을 찾을 수 없습니다 (ugui/TMP 병합 확인 필요)");
        }
    }
}
