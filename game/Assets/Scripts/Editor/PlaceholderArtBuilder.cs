using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ClientIsKing.EditorTools
{
    /// <summary>
    /// task-108(+M1.5 피드백 v2): 플레이스홀더 스프라이트를 문자열 픽셀맵으로 생성하는 에디터 유틸.
    /// 외부 에셋 없이 재현 가능(프로젝트 생성형 CC0 상당) — 출처 기록은 PLACEHOLDER-PROVENANCE.md.
    /// task-113 아트 마감에서 교체 예정.
    ///
    /// v2: 고객 24×32 (아웃라인·3톤 셰이딩·archetype 액세서리), 음식 20×16 (그릇/접시·고명·김).
    /// 멱등 규약: 픽셀이 결정론적이므로 기존 파일과 바이트가 같으면 다시 쓰지 않는다.
    /// </summary>
    public static class PlaceholderArtBuilder
    {
        public const string Root = "Assets/Art/Placeholders";
        public const string CustomersDir = Root + "/Customers";
        public const string FoodIconsDir = Root + "/FoodIcons";
        public const string ProvenancePath = Root + "/PLACEHOLDER-PROVENANCE.md";

        // ── 공용 팔레트 ─────────────────────────────────────────────────────
        static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        static readonly Color32 Outline = new Color32(33, 29, 42, 255);
        static readonly Color32 Skin = new Color32(245, 204, 176, 255);
        static readonly Color32 SkinSh = new Color32(214, 166, 136, 255);
        static readonly Color32 Eye = new Color32(33, 29, 42, 255);
        static readonly Color32 Pants = new Color32(72, 66, 90, 255);
        static readonly Color32 PantsSh = new Color32(55, 50, 70, 255);
        static readonly Color32 Shoe = new Color32(42, 38, 52, 255);
        static readonly Color32 White = new Color32(248, 246, 240, 255);
        static readonly Color32 Steam = new Color32(255, 255, 255, 130);
        static readonly Color32 BowlLight = new Color32(226, 226, 232, 255);
        static readonly Color32 BowlBase = new Color32(196, 198, 208, 255);
        static readonly Color32 BowlSh = new Color32(158, 160, 174, 255);

        public static void Apply()
        {
            EnsureFolder(Root);
            EnsureFolder(CustomersDir);
            EnsureFolder(FoodIconsDir);

            BuildCustomers();
            BuildFoods();

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

        // ════════════════════════════════════════════════════════════════════
        // 고객 4종 — 공용 24×32 인체 맵 + archetype 팔레트/액세서리
        // 문자: . 투명 / O 외곽선 / S,s 피부 / H,h 머리 / C,c 상의 / P,p 하의 / B 신발 / E 눈 / W 하이라이트
        // ════════════════════════════════════════════════════════════════════
        static readonly string[] HumanoidMap =
        {
            //         111111111122223
            //123456789012345678901234 (24 cols)
            "........OOOOOOOO........", // 0  머리 꼭대기
            ".......OHHHHHHHHO.......", // 1
            "......OHHHHHHHHHHO......", // 2
            "......OHHHHHHHHHHO......", // 3
            "......OHhHHHHHHhHO......", // 4  헤어라인
            "......OSSSSSSSSSsO......", // 5  이마
            "......OSSSSSSSSSsO......", // 6
            "......OSESSSSSSEsO......", // 7  눈
            "......OSSSSSSSSSsO......", // 8
            "......OSSssssssSsO......", // 9  입가 음영
            ".......OSSSSSSSsO.......", // 10 턱
            "........OssssssO........", // 11 목
            ".......OCCCCCCCCO.......", // 12 어깨
            "......OCCCCCCCCCcO......", // 13
            ".....OCcCCCCCCCCcCO.....", // 14 팔 시작
            ".....OCcCCCCCCCCcCO.....", // 15
            ".....OCcCCCCCCCCcCO.....", // 16
            ".....OCcCCCCCCCCcCO.....", // 17
            ".....OScCCCCCCCCcSO.....", // 18 손
            "......OcCCCCCCCCcO......", // 19
            "......OccCCCCCCccO......", // 20 밑단
            ".......OPPPPPPPPO.......", // 21 하의
            ".......OPPPppPPPO.......", // 22
            ".......OPPOOOOPPO.......", // 23 다리 갈라짐
            ".......OPPO..OPPO.......", // 24
            ".......OPPO..OPPO.......", // 25
            ".......OppO..OppO.......", // 26
            ".......OppO..OppO.......", // 27
            ".......OBBO..OBBO.......", // 28 신발
            "......OBBBO..OBBBO......", // 29
            "......OOOOO..OOOOO......", // 30
            "........................", // 31 바닥 여백
        };

        static Dictionary<char, Color32> HumanoidPalette(Color32 hair, Color32 hairSh, Color32 cloth, Color32 clothSh)
        {
            return new Dictionary<char, Color32>
            {
                ['O'] = Outline,
                ['S'] = Skin, ['s'] = SkinSh, ['E'] = Eye,
                ['H'] = hair, ['h'] = hairSh,
                ['C'] = cloth, ['c'] = clothSh,
                ['P'] = Pants, ['p'] = PantsSh,
                ['B'] = Shoe, ['W'] = White,
            };
        }

        static void BuildCustomers()
        {
            const int w = 24;

            // 학생 — 초록 후드 + 초록 캡 (머리 위 4줄을 상의색 모자로)
            {
                var cloth = new Color32(96, 172, 98, 255);
                var clothSh = new Color32(66, 132, 72, 255);
                var px = FromMap(HumanoidMap, w, HumanoidPalette(
                    new Color32(46, 42, 52, 255), new Color32(34, 31, 40, 255), cloth, clothSh));
                for (int row = 0; row <= 3; row++) // 캡: 윗머리를 상의색으로
                {
                    RecolorRow(px, w, HumanoidMap, row, 'H', cloth);
                    RecolorRow(px, w, HumanoidMap, row, 'h', clothSh);
                }
                WriteSprite($"{CustomersDir}/student.png", px, w);
            }

            // 직장인 — 갈색 머리 + 파랑 정장 + 빨간 넥타이
            {
                var px = FromMap(HumanoidMap, w, HumanoidPalette(
                    new Color32(94, 66, 48, 255), new Color32(72, 50, 38, 255),
                    new Color32(76, 118, 198, 255), new Color32(54, 88, 158, 255)));
                var tie = new Color32(198, 62, 58, 255);
                for (int row = 12; row <= 16; row++) // 넥타이 (가슴 중앙 2px)
                {
                    SetTop(px, w, 11, row, tie);
                    SetTop(px, w, 12, row, tie);
                }
                WriteSprite($"{CustomersDir}/office_worker.png", px, w);
            }

            // 가족 손님 — 짙은 갈색 머리 + 주황 상의 + 크림 앞치마
            {
                var px = FromMap(HumanoidMap, w, HumanoidPalette(
                    new Color32(70, 50, 40, 255), new Color32(54, 39, 32, 255),
                    new Color32(224, 142, 66, 255), new Color32(182, 108, 46, 255)));
                var apron = new Color32(238, 228, 206, 255);
                var apronSh = new Color32(212, 200, 176, 255);
                for (int row = 15; row <= 20; row++) // 앞치마 (몸통 중앙)
                {
                    for (int x = 9; x <= 14; x++)
                    {
                        SetTop(px, w, x, row, row >= 19 ? apronSh : apron);
                    }
                }
                WriteSprite($"{CustomersDir}/family_parent.png", px, w);
            }

            // 동네 어르신 — 회색 머리 + 보라 조끼 + 안경
            {
                var px = FromMap(HumanoidMap, w, HumanoidPalette(
                    new Color32(184, 184, 192, 255), new Color32(152, 152, 162, 255),
                    new Color32(152, 114, 192, 255), new Color32(118, 86, 154, 255)));
                for (int x = 8; x <= 9; x++) SetTop(px, w, x, 7, Outline);   // 안경 왼 렌즈
                for (int x = 14; x <= 15; x++) SetTop(px, w, x, 7, Outline); // 안경 오른 렌즈
                SetTop(px, w, 11, 7, Outline); SetTop(px, w, 12, 7, Outline); // 브릿지
                WriteSprite($"{CustomersDir}/senior_regular.png", px, w);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // 음식 6종 — 국그릇/접시 20×16 맵 + 내용물 팔레트/고명 오버레이
        // 문자: T 김(스팀) / G,g 내용물 / W 림 하이라이트 / B,b 그릇 / L 접시 / O 외곽선
        // ════════════════════════════════════════════════════════════════════
        static readonly string[] SoupBowlMap =
        {
            //         1111111111
            //1234567890123456789 (20 cols)
            ".....T....T.........", // 0 김
            "....T....T..........", // 1
            "....................", // 2
            "..OOOOOOOOOOOOOO....", // 3 내용물 상단
            ".OGGGGGGGGGGGGGGO...", // 4
            ".OGgGGgGGGgGGgGGO...", // 5
            "..OWWWWWWWWWWWWO....", // 6 림
            "..OBBBBBBBBBBBBO....", // 7 그릇
            "..OBBBBBBBBBBbbO....", // 8
            "...OBBBBBBBBbbO.....", // 9
            "...ObbbbbbbbbbO.....", // 10
            "....OOOOOOOOOO......", // 11
            "......OBBBBO........", // 12 굽
            "......ObbbbO........", // 13
            ".......OOOO.........", // 14
            "....................", // 15
        };

        static readonly string[] PlateMap =
        {
            //1234567890123456789 (20 cols)
            "....................", // 0
            "....................", // 1
            "....OOOOOOOOOOOO....", // 2 음식 두둑
            "...OGGGGGGGGGGGGO...", // 3
            "...OGgGGgGGgGGgGO...", // 4
            "...OGGGGGGGGGGGGO...", // 5
            "..OWGgGGgGGgGGgGWO..", // 6 접시 안쪽 림
            ".OLLWWWWWWWWWWWWLLO.", // 7 접시
            ".OLLLLLLLLLLLLLLLLO.", // 8
            "..OLLLLLLLLLLLLLLO..", // 9
            "...OOOOOOOOOOOOOO...", // 10
            "....................", // 11
            "....................", // 12
            "....................", // 13
            "....................", // 14
            "....................", // 15
        };

        static Dictionary<char, Color32> FoodPalette(Color32 content, Color32 contentSh)
        {
            return new Dictionary<char, Color32>
            {
                ['O'] = Outline, ['T'] = Steam,
                ['G'] = content, ['g'] = contentSh,
                ['W'] = BowlLight, ['B'] = BowlBase, ['b'] = BowlSh,
                ['L'] = BowlBase,
            };
        }

        static void BuildFoods()
        {
            const int w = 20;

            // 돼지국밥 — 뽀얀 국물 + 파 + 고기
            {
                var px = FromMap(SoupBowlMap, w, FoodPalette(
                    new Color32(233, 214, 184, 255), new Color32(210, 186, 150, 255)));
                Garnish(px, w, 5, new Color32(98, 172, 82, 255), 5, 12);      // 파
                Garnish(px, w, 4, new Color32(168, 122, 86, 255), 8, 14);     // 고기
                WriteSprite($"{FoodIconsDir}/pork_gukbap.png", px, w);
            }
            // 소고기국밥 — 진한 국물 + 고기 + 파
            {
                var px = FromMap(SoupBowlMap, w, FoodPalette(
                    new Color32(158, 92, 54, 255), new Color32(128, 70, 40, 255)));
                Garnish(px, w, 4, new Color32(110, 58, 34, 255), 6, 12);      // 고기
                Garnish(px, w, 5, new Color32(98, 172, 82, 255), 9, 14);      // 파
                WriteSprite($"{FoodIconsDir}/beef_gukbap.png", px, w);
            }
            // 떡볶이 — 접시 + 빨간 소스 + 흰 떡
            {
                var px = FromMap(PlateMap, w, FoodPalette(
                    new Color32(214, 54, 40, 255), new Color32(176, 38, 30, 255)));
                var rice = new Color32(246, 240, 228, 255);
                Garnish(px, w, 3, rice, 6, 10); Garnish(px, w, 3, rice, 13, 0);
                Garnish(px, w, 4, rice, 8, 15); Garnish(px, w, 5, rice, 5, 11);
                WriteSprite($"{FoodIconsDir}/tteokbokki.png", px, w);
            }
            // 김밥 — 접시 + 김 단면 (검정 링 + 흰 밥심)
            {
                var px = FromMap(PlateMap, w, FoodPalette(
                    new Color32(52, 50, 50, 255), new Color32(38, 36, 36, 255)));
                var rice = new Color32(246, 240, 228, 255);
                var filling = new Color32(224, 142, 66, 255);
                Garnish(px, w, 3, rice, 6, 10); Garnish(px, w, 3, rice, 14, 0);
                Garnish(px, w, 4, rice, 8, 12); Garnish(px, w, 5, rice, 6, 15);
                Garnish(px, w, 4, filling, 10, 0); Garnish(px, w, 5, filling, 11, 0);
                WriteSprite($"{FoodIconsDir}/gimbap.png", px, w);
            }
            // 잔치국수 — 크림 면 + 지단 고명
            {
                var px = FromMap(SoupBowlMap, w, FoodPalette(
                    new Color32(240, 230, 202, 255), new Color32(220, 206, 172, 255)));
                Garnish(px, w, 4, new Color32(240, 202, 92, 255), 7, 12);     // 지단
                Garnish(px, w, 5, new Color32(98, 172, 82, 255), 10, 15);     // 파
                WriteSprite($"{FoodIconsDir}/janchi_guksu.png", px, w);
            }
            // 비빔국수 — 주황 양념 면 + 야채
            {
                var px = FromMap(SoupBowlMap, w, FoodPalette(
                    new Color32(232, 118, 42, 255), new Color32(196, 92, 30, 255)));
                Garnish(px, w, 4, new Color32(98, 172, 82, 255), 6, 13);      // 오이
                Garnish(px, w, 5, new Color32(214, 54, 40, 255), 9, 15);      // 고추장 포인트
                WriteSprite($"{FoodIconsDir}/bibim_guksu.png", px, w);
            }
        }

        // ── 픽셀맵 유틸 ─────────────────────────────────────────────────────

        /// <summary>문자열 맵(top→bottom) → 픽셀 배열(bottom-origin). 폭 불일치는 즉시 예외.</summary>
        static Color32[] FromMap(string[] rows, int width, Dictionary<char, Color32> palette)
        {
            int height = rows.Length;
            var px = new Color32[width * height];
            for (int row = 0; row < height; row++)
            {
                if (rows[row].Length != width)
                {
                    throw new System.InvalidOperationException(
                        $"[PlaceholderArtBuilder] 픽셀맵 폭 불일치: row {row} = {rows[row].Length} (기대 {width})");
                }
                int y = height - 1 - row;
                for (int x = 0; x < width; x++)
                {
                    char ch = rows[row][x];
                    px[y * width + x] = ch == '.' ? Clear
                        : palette.TryGetValue(ch, out var color) ? color
                        : throw new System.InvalidOperationException(
                            $"[PlaceholderArtBuilder] 팔레트에 없는 문자 '{ch}' (row {row}, col {x})");
                }
            }
            return px;
        }

        /// <summary>top 기준 row 좌표로 픽셀 설정 (맵 좌표계와 동일하게 액세서리를 얹는다).</summary>
        static void SetTop(Color32[] px, int width, int x, int topRow, Color32 color)
        {
            int height = px.Length / width;
            px[(height - 1 - topRow) * width + x] = color;
        }

        /// <summary>맵의 특정 row 에서 문자가 있던 자리만 다른 색으로 (모자 등 부분 recolor).</summary>
        static void RecolorRow(Color32[] px, int width, string[] rows, int topRow, char target, Color32 color)
        {
            for (int x = 0; x < width; x++)
            {
                if (rows[topRow][x] == target)
                {
                    SetTop(px, width, x, topRow, color);
                }
            }
        }

        /// <summary>내용물 위 고명 점 2px (x, x2). x2=0 이면 한 점만.</summary>
        static void Garnish(Color32[] px, int width, int topRow, Color32 color, int x, int x2)
        {
            SetTop(px, width, x, topRow, color);
            if (x2 > 0)
            {
                SetTop(px, width, x2, topRow, color);
            }
        }

        // ── 파일 쓰기/임포트 ────────────────────────────────────────────────

        static void EnsureFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        static void WriteSprite(string path, Color32[] pixels, int width)
        {
            int height = pixels.Length / width;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
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
