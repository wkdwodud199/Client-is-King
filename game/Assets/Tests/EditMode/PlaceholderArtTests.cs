using System.IO;
using ClientIsKing.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>task-108: 플레이스홀더 아트 — 존재·임포트 설정(픽셀 표준)·provenance 기록 검증.</summary>
    public class PlaceholderArtTests
    {
        [OneTimeSetUp]
        public void BuildArt()
        {
            PlaceholderArtBuilder.Apply(); // 멱등 — 이미 있으면 변화 없음
        }

        [Test]
        public void All_Customer_And_Food_Sprites_Exist()
        {
            int count = 0;
            foreach (var path in PlaceholderArtBuilder.AllSpritePaths())
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(path), $"{path}: Sprite 로드 실패");
                count++;
            }
            Assert.AreEqual(10, count, "고객 4 + 음식 6 = 10");
        }

        [Test]
        public void Import_Settings_Follow_Pixel_Standard()
        {
            foreach (var path in PlaceholderArtBuilder.AllSpritePaths())
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.IsNotNull(importer, $"{path}: TextureImporter 없음");
                Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, $"{path}: Sprite 타입");
                Assert.AreEqual(32f, importer.spritePixelsPerUnit, $"{path}: PPU 32");
                Assert.AreEqual(FilterMode.Point, importer.filterMode, $"{path}: Point 필터");
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, $"{path}: 무압축");
                Assert.IsFalse(importer.mipmapEnabled, $"{path}: mipmap off");
            }
        }

        [Test]
        public void Provenance_Document_Mentions_Every_Sprite()
        {
            Assert.IsTrue(File.Exists(PlaceholderArtBuilder.ProvenancePath), "PLACEHOLDER-PROVENANCE.md 누락");
            string doc = File.ReadAllText(PlaceholderArtBuilder.ProvenancePath);
            foreach (var path in PlaceholderArtBuilder.AllSpritePaths())
            {
                string fileName = Path.GetFileName(path);
                StringAssert.Contains(fileName, doc, $"provenance 에 {fileName} 기록 누락");
            }
        }
    }
}
