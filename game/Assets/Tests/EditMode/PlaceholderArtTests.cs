using System.Collections.Generic;
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
    /// task-114: 한식 리컬러 마감 — 32×32 캔버스·PaletteMaps 적용(from 부재/앵커 존재)·비대상 바이트 불변·
    /// 식기 앵커 보존·red 3종 분화·팔레트 상한·Apply 2회 바이트 멱등·provenance 리컬러 절 검증 추가.
    /// </summary>
    public class PlaceholderArtTests
    {
        static readonly string[] CustomerIds =
            { "student", "office_worker", "family_parent", "senior_regular" };
        static readonly string[] FoodIds =
            { "pork_gukbap", "beef_gukbap", "tteokbokki", "gimbap", "janchi_guksu", "bibim_guksu" };
        static readonly string[] NonTargetCustomerIds =
            { "office_worker", "family_parent", "senior_regular" };

        // D2 방향 앵커 — 각 파일에 존재해야 하는 to 색 (from 부재는 PaletteMaps 전수로 검사).
        static readonly (string path, int[] anchors)[] DirectionAnchors =
        {
            ($"{PlaceholderArtBuilder.FoodIconsDir}/pork_gukbap.png", new[] { 0xEDE3D2, 0x8A5B3F }),
            ($"{PlaceholderArtBuilder.FoodIconsDir}/beef_gukbap.png", new[] { 0xC24A22, 0xE67A3C }),
            ($"{PlaceholderArtBuilder.FoodIconsDir}/janchi_guksu.png", new[] { 0xF2E9CF, 0xFBF7EB }),
            ($"{PlaceholderArtBuilder.FoodIconsDir}/bibim_guksu.png", new[] { 0xA83226, 0xE8C86A }),
            ($"{PlaceholderArtBuilder.FoodIconsDir}/tteokbokki.png", new[] { 0xD34A3A, 0xF2E6D8 }),
            ($"{PlaceholderArtBuilder.FoodIconsDir}/gimbap.png", new[] { 0x1B2A1A, 0xFFFEE8 }), // 밥색 유지 확인 포함
            ($"{PlaceholderArtBuilder.CustomersDir}/student.png", new[] { 0x3F6FA6 }),
            ($"{PlaceholderArtBuilder.CustomersDir}/student_walk0.png", new[] { 0x3F6FA6 }),
            ($"{PlaceholderArtBuilder.CustomersDir}/student_walk1.png", new[] { 0x3F6FA6 }),
            ($"{PlaceholderArtBuilder.CustomersDir}/student_walk2.png", new[] { 0x3F6FA6 }),
            ($"{PlaceholderArtBuilder.CustomersDir}/student_walk3.png", new[] { 0x3F6FA6 }),
            (PlaceholderArtBuilder.FloorTilePath, new[] { 0xF4E5C2 }),
            (PlaceholderArtBuilder.CounterTilePath, new[] { 0xBE8A4E }),
        };

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

        // ════════════════════════════════════════════════════════════════════
        // task-114 — 한식 리컬러 마감 (design.md D1/D2)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void Food_Icons_Are_Exactly_32x32_Canvas()
        {
            foreach (var id in FoodIds)
            {
                var tex = LoadDerivedTexture($"{PlaceholderArtBuilder.FoodIconsDir}/{id}.png");
                Assert.AreEqual(32, tex.width, $"{id}: 캔버스 너비 32 (task-114 캔버스 표준화)");
                Assert.AreEqual(32, tex.height, $"{id}: 캔버스 높이 32");
                Object.DestroyImmediate(tex);
            }
        }

        [Test]
        public void PaletteMap_From_Colors_Are_Absent_In_Derived_Pngs()
        {
            // 매핑 표의 단일 원천은 PlaceholderArtBuilder.PaletteMaps — 등재된 from 색은 스왑 후
            // 불투명 픽셀 어디에도 남아 있으면 안 된다 (D1-3).
            Assert.GreaterOrEqual(PlaceholderArtBuilder.PaletteMaps.Count, 13, "매핑 대상 13종 (음식 6 + student 5 + 타일 2)");
            foreach (var entry in PlaceholderArtBuilder.PaletteMaps)
            {
                var colors = OpaqueColorSet(entry.Key);
                foreach (var (from, _) in entry.Value)
                {
                    int rgb = (from.r << 16) | (from.g << 8) | from.b;
                    Assert.IsFalse(colors.Contains(rgb),
                        $"{entry.Key}: 매핑 from 색 #{rgb:X6} 이 리컬러 후에도 남아 있다");
                }
            }
        }

        [Test]
        public void Direction_Anchor_Colors_Are_Present()
        {
            // D2 방향 앵커 — 각 파일에 설계 시드 to 색이 실제로 존재한다.
            foreach (var (path, anchors) in DirectionAnchors)
            {
                var colors = OpaqueColorSet(path);
                foreach (int anchor in anchors)
                {
                    Assert.IsTrue(colors.Contains(anchor),
                        $"{path}: 방향 앵커 #{anchor:X6} 부재 (D2)");
                }
            }
        }

        [Test]
        public void NonTarget_Customers_Have_No_Mapping_And_Are_Byte_Stable()
        {
            // office_worker/family_parent/senior_regular 15파일 — 매핑이 없고(B3) Apply 가 바이트를 바꾸지 않는다.
            var paths = new List<string>();
            foreach (var id in NonTargetCustomerIds)
            {
                paths.Add($"{PlaceholderArtBuilder.CustomersDir}/{id}.png");
                paths.AddRange(PlaceholderArtBuilder.WalkFramePaths(id));
            }
            Assert.AreEqual(15, paths.Count, "비대상 손님 3종 × (idle+walk4) = 15파일");

            var before = new Dictionary<string, byte[]>();
            foreach (var path in paths)
            {
                Assert.IsFalse(PlaceholderArtBuilder.PaletteMaps.ContainsKey(path),
                    $"{path}: 비대상 손님에 매핑 표가 있으면 안 된다 (B3 바이트 불변 계약)");
                before[path] = File.ReadAllBytes(Path.GetFullPath(path));
            }

            PlaceholderArtBuilder.Apply();

            foreach (var path in paths)
            {
                var after = File.ReadAllBytes(Path.GetFullPath(path));
                Assert.IsTrue(before[path].SequenceEqual(after), $"{path}: 비대상 손님 바이트가 변했다");
            }
        }

        [Test]
        public void Gukbap_Bowl_Anchor_Colors_Are_Preserved()
        {
            // 국그릇 공통 식기 앵커 — 리컬러 후에도 보존된다 (식기 불변 계약, D1-5).
            foreach (var id in new[] { "pork_gukbap", "beef_gukbap", "janchi_guksu" })
            {
                var colors = OpaqueColorSet($"{PlaceholderArtBuilder.FoodIconsDir}/{id}.png");
                Assert.IsTrue(colors.Contains(0x501C07), $"{id}: 국그릇 앵커 #501C07 소실");
                Assert.IsTrue(colors.Contains(0xB3400F), $"{id}: 국그릇 앵커 #B3400F 소실");
            }
        }

        [Test]
        public void Red_Trio_Main_Colors_Are_Distinct_Per_File()
        {
            // red 3종 분화 계약 (B2/D1-6): 떡볶이 #D34A3A · 얼큰국밥 #C24A22 · 비빔국수 #A83226 —
            // 각 앵커는 자기 파일에만 존재한다.
            var tteok = OpaqueColorSet($"{PlaceholderArtBuilder.FoodIconsDir}/tteokbokki.png");
            var beef = OpaqueColorSet($"{PlaceholderArtBuilder.FoodIconsDir}/beef_gukbap.png");
            var bibim = OpaqueColorSet($"{PlaceholderArtBuilder.FoodIconsDir}/bibim_guksu.png");

            Assert.IsTrue(tteok.Contains(0xD34A3A), "tteokbokki: 소스 주색 #D34A3A 부재");
            Assert.IsTrue(beef.Contains(0xC24A22), "beef_gukbap: 국물 주색 #C24A22 부재");
            Assert.IsTrue(bibim.Contains(0xA83226), "bibim_guksu: 양념 주색 #A83226 부재");

            Assert.IsFalse(beef.Contains(0xD34A3A), "beef_gukbap 에 떡볶이 주색이 있으면 red 분화 실패");
            Assert.IsFalse(bibim.Contains(0xD34A3A), "bibim_guksu 에 떡볶이 주색이 있으면 red 분화 실패");
            Assert.IsFalse(tteok.Contains(0xC24A22), "tteokbokki 에 얼큰국밥 주색이 있으면 red 분화 실패");
            Assert.IsFalse(bibim.Contains(0xC24A22), "bibim_guksu 에 얼큰국밥 주색이 있으면 red 분화 실패");
            Assert.IsFalse(tteok.Contains(0xA83226), "tteokbokki 에 비빔국수 주색이 있으면 red 분화 실패");
            Assert.IsFalse(beef.Contains(0xA83226), "beef_gukbap 에 비빔국수 주색이 있으면 red 분화 실패");
        }

        [Test]
        public void Palette_Counts_Are_Within_Caps()
        {
            // 팔레트 상한 (B5): 스왑이 색을 늘리지 않고 정리함을 보증 — 불투명 고유색 기준.
            foreach (var id in FoodIds)
            {
                int count = OpaqueColorSet($"{PlaceholderArtBuilder.FoodIconsDir}/{id}.png").Count;
                Assert.LessOrEqual(count, 24, $"{id}: 음식 팔레트 상한 24색 초과 ({count})");
            }
            foreach (var id in CustomerIds)
            {
                int count = OpaqueColorSet($"{PlaceholderArtBuilder.CustomersDir}/{id}.png").Count;
                Assert.LessOrEqual(count, 12, $"{id}: 손님 팔레트 상한 12색 초과 ({count})");
                foreach (var path in PlaceholderArtBuilder.WalkFramePaths(id))
                {
                    int walkCount = OpaqueColorSet(path).Count;
                    Assert.LessOrEqual(walkCount, 12, $"{path}: 손님 팔레트 상한 12색 초과 ({walkCount})");
                }
            }
            foreach (var path in new[] { PlaceholderArtBuilder.FloorTilePath, PlaceholderArtBuilder.CounterTilePath })
            {
                int count = OpaqueColorSet(path).Count;
                Assert.LessOrEqual(count, 8, $"{path}: 타일 팔레트 상한 8색 초과 ({count})");
            }
        }

        [Test]
        public void Apply_Twice_Is_Byte_Idempotent_For_All_Sprites()
        {
            // 파생 28종 전수 — Apply 재실행이 어떤 파일의 바이트도 바꾸지 않는다 (D1-8, GUID 안정).
            var before = PlaceholderArtBuilder.AllSpritePaths()
                .ToDictionary(p => p, p => File.ReadAllBytes(Path.GetFullPath(p)));
            Assert.AreEqual(28, before.Count, "파생 스프라이트 28종");

            PlaceholderArtBuilder.Apply();

            foreach (var entry in before)
            {
                var after = File.ReadAllBytes(Path.GetFullPath(entry.Key));
                Assert.IsTrue(entry.Value.SequenceEqual(after), $"{entry.Key}: Apply 2회에 바이트가 변했다 (멱등 위반)");
            }
        }

        [Test]
        public void Provenance_Has_Recolor_Section()
        {
            string doc = File.ReadAllText(PlaceholderArtBuilder.ProvenancePath);
            StringAssert.Contains("task-114", doc, "provenance 에 task-114 리컬러 절 누락");
            StringAssert.Contains("리컬러", doc, "provenance 에 리컬러 기록 누락");
            StringAssert.Contains("32×32", doc, "provenance 에 캔버스 규약 누락");
            StringAssert.Contains("PaletteMaps", doc, "provenance 에 매핑 단일 원천(PaletteMaps) 언급 누락");
        }

        // ── task-114 픽셀 검사 헬퍼 — File.ReadAllBytes + LoadImage (임포트 설정 무관, E절) ──

        static Texture2D LoadDerivedTexture(string assetPath)
        {
            string full = Path.GetFullPath(assetPath);
            Assert.IsTrue(File.Exists(full), $"{assetPath}: 파생 PNG 누락");
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(full)), $"{assetPath}: PNG 디코딩 실패");
            return tex;
        }

        /// <summary>불투명 픽셀의 고유 RGB 집합 ((r&lt;&lt;16)|(g&lt;&lt;8)|b).</summary>
        static HashSet<int> OpaqueColorSet(string assetPath)
        {
            var tex = LoadDerivedTexture(assetPath);
            var set = new HashSet<int>();
            foreach (var p in tex.GetPixels32())
            {
                if (p.a != 0)
                {
                    set.Add((p.r << 16) | (p.g << 8) | p.b);
                }
            }
            Object.DestroyImmediate(tex);
            return set;
        }
    }
}
