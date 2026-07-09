using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ClientIsKing.EditorTools
{
    /// <summary>
    /// task-108: 플레이스홀더 스프라이트를 결정론적 픽셀 패턴으로 생성하는 에디터 유틸.
    /// 외부 에셋 다운로드 없이 재현 가능(프로젝트 생성형 CC0 상당) — 출처 기록은
    /// Assets/Art/Placeholders/PLACEHOLDER-PROVENANCE.md. task-113 아트 마감에서 교체 예정.
    ///
    /// 멱등 규약: 픽셀이 결정론적이므로 기존 파일과 바이트가 같으면 다시 쓰지 않는다 (GUID/임포트 안정).
    /// </summary>
    public static class PlaceholderArtBuilder
    {
        public const string Root = "Assets/Art/Placeholders";
        public const string CustomersDir = Root + "/Customers";
        public const string FoodIconsDir = Root + "/FoodIcons";
        public const string ProvenancePath = Root + "/PLACEHOLDER-PROVENANCE.md";

        static readonly Color32 Transparent = new Color32(0, 0, 0, 0);
        static readonly Color32 Skin = new Color32(245, 204, 176, 255);
        static readonly Color32 Dark = new Color32(45, 45, 58, 255);
        static readonly Color32 BowlRim = new Color32(235, 235, 235, 255);
        static readonly Color32 BowlBody = new Color32(200, 200, 205, 255);

        public static void Apply()
        {
            EnsureFolder(Root);
            EnsureFolder(CustomersDir);
            EnsureFolder(FoodIconsDir);

            // ── 고객 4종 (16×24) — 상의 색으로 archetype 구분, senior 는 회색 머리 ──
            WriteSprite($"{CustomersDir}/student.png", BuildCustomer(new Color32(88, 178, 82, 255), grayHair: false));
            WriteSprite($"{CustomersDir}/office_worker.png", BuildCustomer(new Color32(72, 120, 212, 255), grayHair: false));
            WriteSprite($"{CustomersDir}/family_parent.png", BuildCustomer(new Color32(222, 148, 64, 255), grayHair: false));
            WriteSprite($"{CustomersDir}/senior_regular.png", BuildCustomer(new Color32(158, 120, 199, 255), grayHair: true));

            // ── 음식 6종 (16×16) — 내용물 색으로 레시피 구분 ──
            WriteSprite($"{FoodIconsDir}/pork_gukbap.png", BuildFood(new Color32(194, 153, 107, 255)));
            WriteSprite($"{FoodIconsDir}/beef_gukbap.png", BuildFood(new Color32(140, 77, 51, 255)));
            WriteSprite($"{FoodIconsDir}/tteokbokki.png", BuildFood(new Color32(217, 51, 38, 255)));
            WriteSprite($"{FoodIconsDir}/gimbap.png", BuildFood(new Color32(38, 38, 38, 255)));
            WriteSprite($"{FoodIconsDir}/janchi_guksu.png", BuildFood(new Color32(237, 227, 199, 255)));
            WriteSprite($"{FoodIconsDir}/bibim_guksu.png", BuildFood(new Color32(230, 115, 38, 255)));

            AssetDatabase.Refresh();
            ApplyImportSettings();

            if (!File.Exists(ProvenancePath))
            {
                throw new FileNotFoundException(
                    "PLACEHOLDER-PROVENANCE.md 가 없다 — 플레이스홀더 출처 기록은 필수 (task-108 설계)", ProvenancePath);
            }
            Debug.Log("[PlaceholderArtBuilder] placeholder sprites ready (customers 4, food icons 6)");
        }

        /// <summary>생성된 모든 스프라이트 경로 (임포트 설정/테스트 공용).</summary>
        public static IEnumerable<string> AllSpritePaths()
        {
            yield return $"{CustomersDir}/student.png";
            yield return $"{CustomersDir}/office_worker.png";
            yield return $"{CustomersDir}/family_parent.png";
            yield return $"{CustomersDir}/senior_regular.png";
            yield return $"{FoodIconsDir}/pork_gukbap.png";
            yield return $"{FoodIconsDir}/beef_gukbap.png";
            yield return $"{FoodIconsDir}/tteokbokki.png";
            yield return $"{FoodIconsDir}/gimbap.png";
            yield return $"{FoodIconsDir}/janchi_guksu.png";
            yield return $"{FoodIconsDir}/bibim_guksu.png";
        }

        static void EnsureFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        // ── 픽셀 패턴 (결정론) ──────────────────────────────────────────────

        /// <summary>16×24 픽셀 인물 — 머리/눈/상의/팔/다리.</summary>
        static Color32[] BuildCustomer(Color32 shirt, bool grayHair)
        {
            const int w = 16, h = 24;
            var px = NewCanvas(w * h);
            var hair = grayHair ? new Color32(190, 190, 195, 255) : new Color32(58, 42, 33, 255);

            FillRect(px, w, 5, 16, 10, 21, Skin);            // 머리
            FillRect(px, w, 5, 20, 10, 21, hair);            // 머리카락
            Set(px, w, 6, 18, Dark); Set(px, w, 9, 18, Dark); // 눈
            FillRect(px, w, 4, 6, 11, 15, shirt);            // 몸통(상의)
            FillRect(px, w, 3, 9, 3, 14, shirt);             // 왼팔
            FillRect(px, w, 12, 9, 12, 14, shirt);           // 오른팔
            FillRect(px, w, 5, 1, 7, 5, Dark);               // 왼다리
            FillRect(px, w, 8, 1, 10, 5, Dark);              // 오른다리
            return px;
        }

        /// <summary>16×16 음식 그릇 — 림/그릇/내용물.</summary>
        static Color32[] BuildFood(Color32 content)
        {
            const int w = 16;
            var px = NewCanvas(w * 16);
            FillRect(px, w, 3, 3, 12, 4, BowlBody);   // 그릇 바닥
            FillRect(px, w, 2, 5, 13, 8, BowlBody);   // 그릇 몸통
            FillRect(px, w, 2, 9, 13, 9, BowlRim);    // 림
            FillRect(px, w, 3, 10, 12, 12, content);  // 내용물
            Set(px, w, 5, 13, content); Set(px, w, 8, 13, content); Set(px, w, 11, 13, content); // 고명
            return px;
        }

        static Color32[] NewCanvas(int size)
        {
            var px = new Color32[size];
            for (int i = 0; i < px.Length; i++)
            {
                px[i] = Transparent;
            }
            return px;
        }

        static void FillRect(Color32[] px, int width, int x0, int y0, int x1, int y1, Color32 color)
        {
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    px[y * width + x] = color;
                }
            }
        }

        static void Set(Color32[] px, int width, int x, int y, Color32 color)
        {
            px[y * width + x] = color;
        }

        // ── 파일 쓰기/임포트 ────────────────────────────────────────────────

        static void WriteSprite(string path, Color32[] pixels)
        {
            int height = pixels.Length / 16;
            var tex = new Texture2D(16, height, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            // 바이트 동일하면 재기록하지 않는다 (임포트/GUID 안정)
            if (File.Exists(path))
            {
                var existing = File.ReadAllBytes(path);
                if (existing.Length == bytes.Length && System.MemoryExtensions.SequenceEqual<byte>(existing, bytes))
                {
                    return;
                }
            }
            File.WriteAllBytes(path, bytes);
        }

        /// <summary>픽셀 표준 고정: Sprite · PPU 32 · Point · 무압축 · mipmap off (테스트가 검증).</summary>
        static void ApplyImportSettings()
        {
            foreach (var path in AllSpritePaths())
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    throw new FileNotFoundException("TextureImporter 를 찾을 수 없다", path);
                }
                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
                if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
                if (!Mathf.Approximately(importer.spritePixelsPerUnit, 32f)) { importer.spritePixelsPerUnit = 32f; dirty = true; }
                if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; dirty = true; }
                if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
                if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
                if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
                if (dirty)
                {
                    importer.SaveAndReimport();
                }
            }
        }
    }
}
