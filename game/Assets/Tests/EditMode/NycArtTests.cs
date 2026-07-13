using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ClientIsKing.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-116 U2 — NYC 코리아타운 런타임 아트(배치 1 · 32파일) 수입 검증 (design.md H절 1~9).
    /// 입력 패키지는 오너/Codex가 타깃 해상도로 생산한 것이며(A안), 이 테스트는 그 입력이
    /// 계약 규격을 충족하는지 기계 검증한다. 픽셀 검사는 임포트 설정과 무관하도록
    /// File.ReadAllBytes + Texture2D.LoadImage 로 원본 바이트를 직접 디코드한다(task-114 전례).
    /// OneTimeSetUp 은 임포트 표준(H6)을 멱등 적용한다(PlaceholderArtTests.Apply 패턴과 동일) —
    /// NYC 자산에만 작용하며 Placeholders/OpenSource 는 건드리지 않는다(H9 가 고정).
    /// </summary>
    public class NycArtTests
    {
        struct NycSpec
        {
            public int W, H, Cap;
            public bool CornersTransparent; // 네 모서리 alpha 0 요구 (backdrop 예외)
            public bool BinaryAlpha;        // 전 픽셀 a ∈ {0,255} 요구 (backdrop 예외)
        }

        static readonly string[] ConceptOriginals =
        {
            "visual-north-star.png", "protagonist.png", "customers.png",
            "food.png", "ui-icons.png", "foodtruck-environment.png",
        };

        static NycSpec SpecFor(string assetPath)
        {
            string dir = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            string file = Path.GetFileName(assetPath);
            if (dir == NycArtContract.CustomersDir)
                return new NycSpec { W = 32, H = 32, Cap = 24, CornersTransparent = true, BinaryAlpha = true };
            if (dir == NycArtContract.FoodIconsDir)
                return new NycSpec { W = 32, H = 32, Cap = 32, CornersTransparent = true, BinaryAlpha = true };
            if (dir == NycArtContract.UiIconsDir)
                return new NycSpec { W = 32, H = 32, Cap = 12, CornersTransparent = true, BinaryAlpha = true };
            if (dir == NycArtContract.StageDir)
            {
                if (file == "backdrop.png")
                    return new NycSpec { W = 640, H = 160, Cap = 64, CornersTransparent = false, BinaryAlpha = false };
                if (file == "counter.png")
                    return new NycSpec { W = 320, H = 32, Cap = 64, CornersTransparent = true, BinaryAlpha = true };
            }
            Assert.Fail($"{assetPath}: 알 수 없는 카테고리 (Asset Map 계약 위반)");
            return default;
        }

        Dictionary<string, byte[]> _placeholderBefore;

        [OneTimeSetUp]
        public void ImportAndConfigure()
        {
            AssetDatabase.Refresh(); // 복사된 PNG 가 임포트되어 있음을 보장(기본 .meta 생성)
            // H9 근거: NYC 임포트 표준 적용이 Placeholders 바이트를 건드리지 않음을 증명하기 위한 사전 스냅샷.
            _placeholderBefore = PlaceholderArtBuilder.AllSpritePaths()
                .ToDictionary(p => p, p => File.ReadAllBytes(Path.GetFullPath(p)));
            NycArtContract.ApplyImportSettingsToAll(); // 멱등 — 이미 표준이면 재수입 없음
        }

        // ── H1: 32파일 전수 존재 + Sprite 로드 + .meta 쌍 ──
        [Test]
        public void All_32_Sprites_Exist_With_Meta_Pair()
        {
            int count = 0;
            foreach (var path in NycArtContract.AllSpritePaths())
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(path), $"{path}: Sprite 로드 실패");
                string metaFull = Path.GetFullPath(path) + ".meta";
                Assert.IsTrue(File.Exists(metaFull), $"{path}: .meta 쌍 누락");
                count++;
            }
            // 손님 20 + 음식 6 + 장르 4 + 무대 2 = 32
            Assert.AreEqual(32, count, "배치 1 = 32파일");
        }

        // ── H2: 규격 정확 일치 (E절 확정치, 임포트 무관 원본 디코드) ──
        [Test]
        public void Dimensions_Match_Spec()
        {
            foreach (var path in NycArtContract.AllSpritePaths())
            {
                var spec = SpecFor(path);
                var tex = LoadRawTexture(path);
                Assert.AreEqual(spec.W, tex.width, $"{path}: width {spec.W} 기대");
                Assert.AreEqual(spec.H, tex.height, $"{path}: height {spec.H} 기대");
                Object.DestroyImmediate(tex);
            }
        }

        // ── H3: 알파 규칙 — 투명 모서리(backdrop 예외) + 이진 알파(backdrop 예외) ──
        [Test]
        public void Alpha_Rules_Transparent_Corners_And_Binary()
        {
            foreach (var path in NycArtContract.AllSpritePaths())
            {
                var spec = SpecFor(path);
                var tex = LoadRawTexture(path);
                var px = tex.GetPixels32();
                int w = tex.width, h = tex.height;

                // 이진 알파
                if (spec.BinaryAlpha)
                {
                    foreach (var p in px)
                    {
                        Assert.IsTrue(p.a == 0 || p.a == 255,
                            $"{path}: 반투명 픽셀 a={p.a} (이진 알파 위반)");
                    }
                }

                // 투명 모서리 (좌하 원점 좌표계 — 네 꼭짓점)
                if (spec.CornersTransparent)
                {
                    int[] cornerIdx = { 0, w - 1, (h - 1) * w, (h - 1) * w + (w - 1) };
                    foreach (int i in cornerIdx)
                    {
                        Assert.AreEqual(0, px[i].a, $"{path}: 모서리 픽셀이 투명하지 않음 (배경 투명 위반)");
                    }
                }
                else
                {
                    // backdrop: 완전 불투명 허용 — 반투명이 없어야 한다(이진 또는 불투명)
                    foreach (var p in px)
                    {
                        Assert.IsTrue(p.a == 0 || p.a == 255,
                            $"{path}: backdrop 반투명 픽셀 a={p.a}");
                    }
                }
                Object.DestroyImmediate(tex);
            }
        }

        // ── H4: 원본 PNG IHDR color type == 6 (Texture2D 변환이 아니라 raw 바이트) ──
        [Test]
        public void Raw_Png_Ihdr_Is_Truecolor_Alpha()
        {
            foreach (var path in NycArtContract.AllSpritePaths())
            {
                string full = Path.GetFullPath(path);
                byte[] head = ReadHead(full, 33);
                // PNG 시그니처
                byte[] sig = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
                for (int i = 0; i < 8; i++)
                    Assert.AreEqual(sig[i], head[i], $"{path}: PNG 시그니처 손상 (byte {i})");
                Assert.AreEqual((byte)'I', head[12], $"{path}: IHDR 청크 아님");
                Assert.AreEqual((byte)'H', head[13], $"{path}: IHDR 청크 아님");
                Assert.AreEqual((byte)'D', head[14], $"{path}: IHDR 청크 아님");
                Assert.AreEqual((byte)'R', head[15], $"{path}: IHDR 청크 아님");
                byte bitDepth = head[24];
                byte colorType = head[25];
                Assert.AreEqual(8, bitDepth, $"{path}: IHDR bit depth 8 기대 (실제 {bitDepth})");
                Assert.AreEqual(6, colorType, $"{path}: IHDR color type 6(truecolor+alpha) 기대 (실제 {colorType})");
            }
        }

        // ── H5: 픽셀 아트 계약 — 불투명 고유색 상한 (F6 확정치) ──
        [Test]
        public void Opaque_Color_Counts_Within_Caps()
        {
            foreach (var path in NycArtContract.AllSpritePaths())
            {
                var spec = SpecFor(path);
                int count = OpaqueColorSet(path).Count;
                Assert.LessOrEqual(count, spec.Cap, $"{path}: 불투명 고유색 {count} > 상한 {spec.Cap}");
            }
        }

        // ── H6: 임포트 표준 전수 ──
        [Test]
        public void Import_Settings_Follow_Pixel_Standard()
        {
            foreach (var path in NycArtContract.AllSpritePaths())
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.IsNotNull(importer, $"{path}: TextureImporter 없음");
                Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, $"{path}: Sprite 타입");
                Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode, $"{path}: Single 슬라이스");
                Assert.AreEqual(32f, importer.spritePixelsPerUnit, $"{path}: PPU 32");
                Assert.AreEqual(FilterMode.Point, importer.filterMode, $"{path}: Point 필터");
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, $"{path}: 무압축");
                Assert.IsFalse(importer.mipmapEnabled, $"{path}: mipmap off");
                Assert.IsTrue(importer.alphaIsTransparency, $"{path}: alphaIsTransparency");
            }
        }

        // ── H7: provenance 존재 + 32파일 전수 언급 + 필수 필드 ──
        [Test]
        public void Provenance_Exists_Covers_All_32_And_Fields()
        {
            string provFull = Path.GetFullPath(NycArtContract.ProvenancePath);
            Assert.IsTrue(File.Exists(provFull), "NYC-ART-PROVENANCE.md 누락");
            string doc = File.ReadAllText(provFull);
            foreach (var path in NycArtContract.AllSpritePaths())
            {
                string fileName = Path.GetFileName(path);
                StringAssert.Contains(fileName, doc, $"provenance 에 {fileName} 기록 누락");
            }
            // C절 필수 필드 라벨
            foreach (var field in new[] { "생성 도구", "생성일", "참조 콘셉트", "후처리", "승인" })
            {
                StringAssert.Contains(field, doc, $"provenance 필수 필드 '{field}' 누락");
            }
            // CC0 표기 금지 정책 명시
            StringAssert.Contains("CC0 아님", doc, "provenance 에 'CC0 아님' 라이선스 구획 누락");
        }

        // ── H8: 콘셉트 원본 6종 md5 불변 (art-originals PROVENANCE.md 핀과 일치) ──
        [Test]
        public void Concept_Originals_Md5_Match_Pins()
        {
            string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            string dir = Path.Combine(repoRoot, "kb", "concepts", "art-originals");
            string pinDoc = File.ReadAllText(Path.Combine(dir, "PROVENANCE.md"));
            foreach (var name in ConceptOriginals)
            {
                string full = Path.Combine(dir, name);
                Assert.IsTrue(File.Exists(full), $"콘셉트 원본 {name} 누락");
                string md5 = Md5Hex(full);
                StringAssert.Contains(md5, pinDoc,
                    $"{name}: 실제 md5 {md5} 가 PROVENANCE.md 핀과 불일치 (콘셉트 원본 변조 의심)");
            }
        }

        // ── H9: Placeholders/OpenSource 격리 (경로 비중첩 + 집합 불변 + 바이트 불변) ──
        [Test]
        public void Placeholders_And_OpenSource_Are_Isolated()
        {
            // 1) NYC 경로가 Placeholders/OpenSource 와 겹치지 않는다(계약 수준).
            foreach (var p in NycArtContract.AllSpritePaths())
            {
                Assert.IsFalse(p.StartsWith("Assets/Art/Placeholders") || p.StartsWith("Assets/Art/OpenSource"),
                    $"{p}: NYC 자산이 Placeholders/OpenSource 경로를 침범");
            }
            // 2) 파생 28종 집합 불변 — 전부 여전히 Sprite 로 로드된다.
            int count = 0;
            foreach (var p in PlaceholderArtBuilder.AllSpritePaths())
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(p), $"{p}: 플레이스홀더 스프라이트 소실");
                count++;
            }
            Assert.AreEqual(28, count, "플레이스홀더 파생 28종 집합 불변");
            // 3) OpenSource 라이선스 원문 사본 존재.
            foreach (var rel in new[]
            {
                "Assets/Art/OpenSource/NinjaAdventure/LICENSE.txt",
                "Assets/Art/OpenSource/Kenney-RoguelikeRPG/License.txt",
            })
            {
                Assert.IsTrue(File.Exists(Path.GetFullPath(rel)), $"{rel}: OpenSource 라이선스 파일 소실");
            }
            // 4) NYC 임포트 표준 적용이 플레이스홀더 바이트를 바꾸지 않았다(OneTimeSetUp 스냅샷 대조).
            foreach (var entry in _placeholderBefore)
            {
                var after = File.ReadAllBytes(Path.GetFullPath(entry.Key));
                Assert.IsTrue(entry.Value.SequenceEqual(after),
                    $"{entry.Key}: NYC 수입 작업이 플레이스홀더 바이트를 변경 (격리 위반)");
            }
        }

        // ── 헬퍼 (PlaceholderArtTests 미러 — 임포트 무관 원본 디코드) ──

        static Texture2D LoadRawTexture(string assetPath)
        {
            string full = Path.GetFullPath(assetPath);
            Assert.IsTrue(File.Exists(full), $"{assetPath}: PNG 누락");
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(full)), $"{assetPath}: PNG 디코딩 실패");
            return tex;
        }

        static HashSet<int> OpaqueColorSet(string assetPath)
        {
            var tex = LoadRawTexture(assetPath);
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

        static byte[] ReadHead(string fullPath, int n)
        {
            using var fs = File.OpenRead(fullPath);
            var buf = new byte[n];
            int read = fs.Read(buf, 0, n);
            Assert.AreEqual(n, read, $"{fullPath}: 헤더 {n}바이트 읽기 실패");
            return buf;
        }

        static string Md5Hex(string fullPath)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(File.ReadAllBytes(fullPath));
            return System.BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
