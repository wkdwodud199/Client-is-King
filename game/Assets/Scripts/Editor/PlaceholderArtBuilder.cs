using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ClientIsKing.EditorTools
{
    /// <summary>
    /// task-109 아트 도입 패스: 손님·음식 스프라이트를 CC0 오픈소스 팩 서브셋에서 파생한다.
    /// 원본 팩은 Assets/Art/OpenSource/ 아래 무수정 보존(source-of-truth)하고, 여기서는 그 시트의
    /// 특정 프레임을 잘라(GetPixels) 개별 PNG(Customers/·FoodIcons/)로 산출한다. 출처·파생 규약은
    /// PLACEHOLDER-PROVENANCE.md 에 기록한다. (이전 v2 절차적 픽셀맵 생성을 대체.)
    ///
    /// - 손님: Ninja Adventure(CC0) 4캐릭터의 우향 idle 1프레임 + 우향 걷기 4프레임.
    ///   archetype→character: student→Boy, office_worker→ManGreen, family_parent→Woman, senior_regular→OldMan.
    ///   Walk.png 64×64(4방향×4프레임, 프레임 16×16), Idle.png 64×16(4프레임). 우향 = 이미지 맨 아래 행(행3).
    ///   Texture2D.LoadImage 는 bottom-origin 이므로 이미지 행3(우향)은 텍스처 y∈[0,16).
    /// - 음식: karsiori Food Pack(CC0)·Henry Software Pixel Food(CC0) 개별 스프라이트를 직접 매핑(리컬러 없음 — task-114 이월).
    ///
    /// 멱등 규약: PNG 를 읽어 자른 뒤 인코딩한 바이트가 기존 파일과 같으면 다시 쓰지 않는다 (GUID 안정).
    /// 임포트 표준: Sprite · PPU 32 · Point · 무압축 · mipmap off (테스트가 고정).
    /// </summary>
    public static class PlaceholderArtBuilder
    {
        public const string Root = "Assets/Art/Placeholders";
        public const string CustomersDir = Root + "/Customers";
        public const string FoodIconsDir = Root + "/FoodIcons";
        public const string StageDir = Root + "/Stage";
        public const string ProvenancePath = Root + "/PLACEHOLDER-PROVENANCE.md";

        const string OpenSourceRoot = "Assets/Art/OpenSource";
        const string NinjaRoot = OpenSourceRoot + "/NinjaAdventure/Character";
        const string KarsioriRoot = OpenSourceRoot + "/karsiori-FoodPack";
        const string HenryRoot = OpenSourceRoot + "/HenrySoftware-PixelFood/Food";
        const string KenneySheet = OpenSourceRoot + "/Kenney-RoguelikeRPG/roguelikeSheet_transparent.png";

        // Kenney Roguelike/RPG 시트: 16×16 타일 + 타일 간 1px 여백 → 타일 (col,row) 원점 = (col*17, row*17).
        const int KenneyTile = 16;
        const int KenneyStride = 17;
        // 무대 타일 좌표 (PIL top-origin 그리드 기준 — 실측: 아래 provenance 참조).
        static readonly (int col, int row) FloorTileCell = (8, 1);   // 크림 벽/바닥 plaster (밝은 톤)
        static readonly (int col, int row) CounterTileCell = (6, 10); // 나무 판자 카운터 (갈색 톤)

        const int CustomerFrame = 16; // Ninja Adventure 프레임 한 변(px)
        const int WalkFrameCount = 4; // 우향 걷기 프레임 수

        // archetype id → Ninja Adventure 캐릭터 폴더명
        static readonly (string id, string character)[] CustomerMap =
        {
            ("student", "Boy"),
            ("office_worker", "ManGreen"),
            ("family_parent", "Woman"),
            ("senior_regular", "OldMan"),
        };

        // recipeId → OpenSource 소스 PNG 경로 (직접 매핑, 리컬러 없음)
        static readonly (string id, string source)[] FoodMap =
        {
            ("pork_gukbap",  KarsioriRoot + "/Carrot stew.png"),
            ("beef_gukbap",  KarsioriRoot + "/Pumpkin soup.png"),
            ("janchi_guksu", KarsioriRoot + "/Mushroom Stew.png"),
            ("bibim_guksu",  KarsioriRoot + "/Tomato stew.png"),
            ("tteokbokki",   KarsioriRoot + "/Meatballs.png"),
            ("gimbap",       HenryRoot + "/Sushi.png"),
        };

        public static void Apply()
        {
            EnsureFolder(Root);
            EnsureFolder(CustomersDir);
            EnsureFolder(FoodIconsDir);
            EnsureFolder(StageDir);

            BuildCustomers();
            BuildFoods();
            BuildStageTiles();

            AssetDatabase.Refresh();
            ApplyImportSettings();

            if (!File.Exists(ProvenancePath))
            {
                throw new FileNotFoundException(
                    "PLACEHOLDER-PROVENANCE.md 가 없다 — 플레이스홀더/오픈소스 출처 기록은 필수 (task-108/109 설계)", ProvenancePath);
            }
            Debug.Log("[PlaceholderArtBuilder] OpenSource-derived sprites ready (customers 4×(idle+walk4), food icons 6)");
        }

        /// <summary>산출된 모든 스프라이트 경로 (임포트 설정/테스트 공용). idle + walk0..3 + 음식 6.</summary>
        public static IEnumerable<string> AllSpritePaths()
        {
            foreach (var (id, _) in CustomerMap)
            {
                yield return $"{CustomersDir}/{id}.png"; // idle / fallback 겸용
                for (int f = 0; f < WalkFrameCount; f++)
                {
                    yield return $"{CustomersDir}/{id}_walk{f}.png";
                }
            }
            foreach (var (id, _) in FoodMap)
            {
                yield return $"{FoodIconsDir}/{id}.png";
            }
            yield return $"{StageDir}/floor.png";
            yield return $"{StageDir}/counter.png";
        }

        /// <summary>손님 idle 스프라이트 경로만 (테스트/카탈로그 공용).</summary>
        public static IEnumerable<string> CustomerIdlePaths()
        {
            foreach (var (id, _) in CustomerMap)
            {
                yield return $"{CustomersDir}/{id}.png";
            }
        }

        /// <summary>주어진 archetype id 의 우향 걷기 프레임 경로 4종 (순서 보장).</summary>
        public static IEnumerable<string> WalkFramePaths(string customerId)
        {
            for (int f = 0; f < WalkFrameCount; f++)
            {
                yield return $"{CustomersDir}/{customerId}_walk{f}.png";
            }
        }

        public static string FloorTilePath => $"{StageDir}/floor.png";
        public static string CounterTilePath => $"{StageDir}/counter.png";

        // ════════════════════════════════════════════════════════════════════
        // 손님 — Ninja Adventure 우향 idle + 걷기 4프레임 파생
        // ════════════════════════════════════════════════════════════════════
        static void BuildCustomers()
        {
            foreach (var (id, character) in CustomerMap)
            {
                string walkPath = $"{NinjaRoot}/{character}/SeparateAnim/Walk.png";
                string idlePath = $"{NinjaRoot}/{character}/SeparateAnim/Idle.png";

                var walkTex = LoadReadableTexture(walkPath);
                var idleTex = LoadReadableTexture(idlePath);
                var walkPx = walkTex.GetPixels32();
                var idlePx = idleTex.GetPixels32();
                int walkW = walkTex.width, walkH = walkTex.height;
                int idleW = idleTex.width, idleH = idleTex.height;
                Object.DestroyImmediate(walkTex);
                Object.DestroyImmediate(idleTex);

                // 우향 idle = Idle.png 프레임 index 3 (x∈[48,64)). Idle.png 는 64×16 → 텍스처 y∈[0,16).
                WritePng($"{CustomersDir}/{id}.png",
                    Region(idlePx, idleW, idleH, 3 * CustomerFrame, 0, CustomerFrame, CustomerFrame),
                    CustomerFrame, CustomerFrame);

                // 우향 걷기 = Walk.png 이미지 행3(우향). Texture2D 는 bottom-origin 이라 시각 맨 아래 행(우향)은
                // 텍스처 y∈[0,16). 4프레임 x=0,16,32,48.
                for (int f = 0; f < WalkFrameCount; f++)
                {
                    WritePng($"{CustomersDir}/{id}_walk{f}.png",
                        Region(walkPx, walkW, walkH, f * CustomerFrame, 0, CustomerFrame, CustomerFrame),
                        CustomerFrame, CustomerFrame);
                }
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // 음식 — karsiori/Henry 개별 스프라이트 직접 매핑 (소스 크기 유지 + 투명 트림)
        // ════════════════════════════════════════════════════════════════════
        static void BuildFoods()
        {
            foreach (var (id, source) in FoodMap)
            {
                var tex = LoadReadableTexture(source);
                var pixels = tex.GetPixels32();
                int w = tex.width;
                int h = tex.height;
                Object.DestroyImmediate(tex);

                // 투명 여백 트림 (bottom-origin 픽셀 배열 기준 bounding box).
                if (TryTrim(pixels, w, h, out var trimmed, out int tw, out int th))
                {
                    WritePng($"{FoodIconsDir}/{id}.png", trimmed, tw, th);
                }
                else
                {
                    WritePng($"{FoodIconsDir}/{id}.png", pixels, w, h);
                }
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // 무대 타일 — Kenney Roguelike/RPG 시트에서 바닥/카운터 16×16 타일 추출 (순수 장식)
        // ════════════════════════════════════════════════════════════════════
        static void BuildStageTiles()
        {
            var sheet = LoadReadableTexture(KenneySheet);
            var px = sheet.GetPixels32();
            int w = sheet.width, h = sheet.height;
            Object.DestroyImmediate(sheet);

            WritePng(FloorTilePath, KenneyTilePixels(px, w, h, FloorTileCell), KenneyTile, KenneyTile);
            WritePng(CounterTilePath, KenneyTilePixels(px, w, h, CounterTileCell), KenneyTile, KenneyTile);
        }

        /// <summary>Kenney 시트(bottom-origin 픽셀)에서 (col,row) 타일을 잘라낸다. row 는 PIL top-origin 그리드.</summary>
        static Color32[] KenneyTilePixels(Color32[] px, int w, int h, (int col, int row) cell)
        {
            int x = cell.col * KenneyStride;
            int topY = cell.row * KenneyStride;              // PIL top-origin y
            int y = h - topY - KenneyTile;                   // bottom-origin y
            return Region(px, w, h, x, y, KenneyTile, KenneyTile);
        }

        // ── 텍스처 로드/픽셀 유틸 ────────────────────────────────────────────

        /// <summary>bottom-origin 픽셀 배열에서 (x,y) 오프셋의 w×h 영역을 잘라 새 배열로.</summary>
        static Color32[] Region(Color32[] src, int srcW, int srcH, int x, int y, int w, int h)
        {
            if (x < 0 || y < 0 || x + w > srcW || y + h > srcH)
            {
                throw new System.InvalidOperationException(
                    $"[PlaceholderArtBuilder] Region 범위 초과: ({x},{y},{w},{h}) in {srcW}×{srcH}");
            }
            var outPx = new Color32[w * h];
            for (int row = 0; row < h; row++)
            {
                for (int col = 0; col < w; col++)
                {
                    outPx[row * w + col] = src[(y + row) * srcW + (x + col)];
                }
            }
            return outPx;
        }

        /// <summary>임포트 설정과 무관하게 readable 텍스처를 얻는다 (원본 PNG 바이트 직접 디코딩).</summary>
        static Texture2D LoadReadableTexture(string assetPath)
        {
            string full = Path.GetFullPath(assetPath);
            if (!File.Exists(full))
            {
                throw new FileNotFoundException($"[PlaceholderArtBuilder] OpenSource 소스 PNG 누락: {assetPath}", full);
            }
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(File.ReadAllBytes(full)))
            {
                Object.DestroyImmediate(tex);
                throw new System.InvalidOperationException($"[PlaceholderArtBuilder] PNG 디코딩 실패: {assetPath}");
            }
            return tex;
        }

        /// <summary>투명 픽셀을 제거한 최소 bounding box 를 반환. 전부 투명이면 false.</summary>
        static bool TryTrim(Color32[] src, int w, int h, out Color32[] outPx, out int outW, out int outH)
        {
            int minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (src[y * w + x].a != 0)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }
            if (maxX < 0)
            {
                outPx = null; outW = 0; outH = 0;
                return false;
            }
            outW = maxX - minX + 1;
            outH = maxY - minY + 1;
            if (outW == w && outH == h)
            {
                outPx = null;
                return false; // 트림 불필요
            }
            outPx = new Color32[outW * outH];
            for (int y = 0; y < outH; y++)
            {
                for (int x = 0; x < outW; x++)
                {
                    outPx[y * outW + x] = src[(minY + y) * w + (minX + x)];
                }
            }
            return true;
        }

        // ── 파일 쓰기/임포트 ────────────────────────────────────────────────

        static void EnsureFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        static void WritePng(string path, Color32[] pixels, int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            // 바이트 동일하면 재기록하지 않는다 (임포트/GUID 안정 — 멱등)
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
