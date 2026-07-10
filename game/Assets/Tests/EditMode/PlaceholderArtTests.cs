using System.IO;
using System.Linq;
using ClientIsKing.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-109: OpenSource(CC0) 파생 아트 — 존재·임포트 설정(픽셀 표준)·archetype 별 walk 4프레임·
    /// provenance/LICENSE 기록 검증. (task-108 픽셀맵 검증을 아트 도입 패스 기준으로 갱신.)
    /// </summary>
    public class PlaceholderArtTests
    {
        static readonly string[] CustomerIds =
            { "student", "office_worker", "family_parent", "senior_regular" };
        static readonly string[] FoodIds =
            { "pork_gukbap", "beef_gukbap", "tteokbokki", "gimbap", "janchi_guksu", "bibim_guksu" };

        [OneTimeSetUp]
        public void BuildArt()
        {
            PlaceholderArtBuilder.Apply(); // 멱등 — 이미 있으면 변화 없음
        }

        [Test]
        public void All_Derived_Sprites_Exist()
        {
            int count = 0;
            foreach (var path in PlaceholderArtBuilder.AllSpritePaths())
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(path), $"{path}: Sprite 로드 실패");
                count++;
            }
            // 손님 4 × (idle 1 + walk 4) = 20, 음식 6, 무대 타일 2 = 28
            Assert.AreEqual(28, count, "손님 20 + 음식 6 + 무대 타일 2 = 28");
        }

        [Test]
        public void Each_Customer_Has_Idle_And_Four_Walk_Frames()
        {
            foreach (var id in CustomerIds)
            {
                Assert.IsNotNull(
                    AssetDatabase.LoadAssetAtPath<Sprite>($"{PlaceholderArtBuilder.CustomersDir}/{id}.png"),
                    $"{id}: idle 스프라이트 누락");

                var walk = PlaceholderArtBuilder.WalkFramePaths(id).ToArray();
                Assert.AreEqual(4, walk.Length, $"{id}: walk 프레임 경로 4개");
                foreach (var path in walk)
                {
                    Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(path), $"{path}: walk 프레임 로드 실패");
                }
            }
        }

        [Test]
        public void All_Six_Food_Icons_Exist()
        {
            foreach (var id in FoodIds)
            {
                Assert.IsNotNull(
                    AssetDatabase.LoadAssetAtPath<Sprite>($"{PlaceholderArtBuilder.FoodIconsDir}/{id}.png"),
                    $"{id}: 음식 아이콘 누락");
            }
        }

        [Test]
        public void Import_Settings_Follow_Pixel_Standard()
        {
            foreach (var path in PlaceholderArtBuilder.AllSpritePaths())
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.IsNotNull(importer, $"{path}: TextureImporter 없음");
                Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, $"{path}: Sprite 타입");
                Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode, $"{path}: Single 슬라이스");
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

        [Test]
        public void Provenance_Records_OpenSource_Packs_And_CC0()
        {
            string doc = File.ReadAllText(PlaceholderArtBuilder.ProvenancePath);
            foreach (var pack in new[] { "Ninja Adventure", "karsiori", "Henry", "Kenney" })
            {
                StringAssert.Contains(pack, doc, $"provenance 에 {pack} 팩 출처 누락");
            }
            StringAssert.Contains("CC0", doc, "provenance 에 CC0 라이선스 명시 누락");
            StringAssert.Contains("2026-07-10", doc, "provenance 에 다운로드일 누락");
        }

        [Test]
        public void OpenSource_Packs_Have_License_Files()
        {
            // 각 원본 팩 폴더에 라이선스 원문 사본이 존재한다 (CC0 재현성).
            var licenses = new[]
            {
                "Assets/Art/OpenSource/NinjaAdventure/LICENSE.txt",
                "Assets/Art/OpenSource/Kenney-RoguelikeRPG/License.txt",
            };
            foreach (var rel in licenses)
            {
                string full = Path.GetFullPath(rel);
                Assert.IsTrue(File.Exists(full), $"{rel}: 라이선스 파일 누락");
            }
        }
    }
}
