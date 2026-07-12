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
    /// - 음식: karsiori Food Pack(CC0)·Henry Software Pixel Food(CC0) 개별 스프라이트 매핑.
    ///
    /// task-114 아트 마감 패스: 파생 단계에 결정론적 팔레트 스왑을 추가한다 (design.md B절).
    ///   슬라이스/트림 → ①정수 사전 확대(음식, 트림 w≤16∧h≤16 만 ×2) → ②exact-match 팔레트 스왑
    ///   (PaletteMaps 단일 원천 — 불투명 픽셀만, alpha 보존, 미등재 색 통과) → ③32×32 캔버스 패딩(음식만)
    ///   → WritePng. 손님(student 5파일)·무대 타일 2종은 ②스왑만 적용. 비대상 손님 3종은 매핑이 없어
    ///   바이트 불변. HSV/색상환 회전/디더링 등 부동소수 변환은 결정론 훼손 위험으로 금지.
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

        // task-114: 음식 아이콘 공통 캔버스 한 변(px)과 정수 사전 확대 기준 (트림 결과 w≤16 ∧ h≤16 → ×2).
        const int FoodCanvas = 32;
        const int SmallSourceMax = 16;

        // archetype id → Ninja Adventure 캐릭터 폴더명
        static readonly (string id, string character)[] CustomerMap =
        {
            ("student", "Boy"),
            ("office_worker", "ManGreen"),
            ("family_parent", "Woman"),
            ("senior_regular", "OldMan"),
        };

        // recipeId → OpenSource 소스 PNG 경로 (한식 톤 정합은 PaletteMaps 스왑이 담당 — task-114)
        static readonly (string id, string source)[] FoodMap =
        {
            ("pork_gukbap",  KarsioriRoot + "/Carrot stew.png"),
            ("beef_gukbap",  KarsioriRoot + "/Pumpkin soup.png"),
            ("janchi_guksu", KarsioriRoot + "/Mushroom Stew.png"),
            ("bibim_guksu",  KarsioriRoot + "/Tomato stew.png"),
            ("tteokbokki",   KarsioriRoot + "/Meatballs.png"),
            ("gimbap",       HenryRoot + "/Sushi.png"),
        };

        /// <summary>
        /// task-114 리컬러 매핑 표 — **단일 원천** (테스트·provenance 가 이 상수를 공유한다, design.md B1).
        /// key = 파생 PNG 경로. from = 2026-07-12 파생 PNG 실측색, to = 설계 시드(D2 방향 앵커 포함).
        /// 그릇·식기 계열(국그릇 공통 #501C07/#B3400F/#3B0A09/#2C0302/#8D2D04/#D15C2B/#6F2709/#A74116 등)은
        /// 매핑하지 않는다 — 원본 팩의 식기 세트 일관성 보존 (B2).
        /// </summary>
        public static readonly IReadOnlyDictionary<string, (Color32 from, Color32 to)[]> PaletteMaps =
            BuildPaletteMaps();

        static IReadOnlyDictionary<string, (Color32 from, Color32 to)[]> BuildPaletteMaps()
        {
            // hex 리터럴 → 불투명 Color32 (표 가독용).
            static Color32 C(uint rgb) =>
                new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 0xFF);

            var maps = new Dictionary<string, (Color32 from, Color32 to)[]>
            {
                // ── 음식 6종 (B2) ──────────────────────────────────────────
                // 뽀얀 돼지국밥 — 사골 국물 + 편육 (허브 green #73AD48/#568931 은 파 고명으로 유지)
                [$"{FoodIconsDir}/pork_gukbap.png"] = new[]
                {
                    (C(0xE3C77B), C(0xEDE3D2)), // 국물 밝음
                    (C(0xD1B770), C(0xE2D6C0)), // 국물 중간
                    (C(0xB79661), C(0xCBBBA0)), // 국물 음영
                    (C(0xD1671D), C(0x8A5B3F)), // 편육
                    (C(0xE5792B), C(0xA6714C)), // 고기 밝음
                    (C(0xA25119), C(0x6E4630)), // 고기 음영
                },
                // 얼큰 소고기국밥 — 주황빛 국물 (red 3종 분화: #C24A22 주색)
                [$"{FoodIconsDir}/beef_gukbap.png"] = new[]
                {
                    (C(0xC14202), C(0xC24A22)), // 국물 주
                    (C(0xFF6F00), C(0xE67A3C)), // 기름 하이라이트
                    (C(0xC0E400), C(0x6FA04E)), // 대파 밝음
                    (C(0x8EA800), C(0x4E7A33)), // 대파 음영
                    (C(0xF7E0A0), C(0xF2D9A6)), // 기름방울
                },
                // 맑은 잔치국수 — 맑은 국물 + 흰 소면 + 지단
                [$"{FoodIconsDir}/janchi_guksu.png"] = new[]
                {
                    (C(0xEAD374), C(0xF2E9CF)), // 맑은 국물
                    (C(0xBFAD67), C(0xE3D8B8)), // 국물 중간
                    (C(0xA59244), C(0xC9BC94)), // 국물 음영
                    (C(0xB9B9B9), C(0xFBF7EB)), // 소면 밝음
                    (C(0x9B9B9B), C(0xEFE7D4)), // 소면 중간
                    (C(0x7E7881), C(0xCFC5AE)), // 소면 음영
                    (C(0xCBB660), C(0xE8C86A)), // 지단
                },
                // 비빔국수 — 진한 고추장 양념 + 노란 면 (red 3종 분화: #A83226 주색)
                [$"{FoodIconsDir}/bibim_guksu.png"] = new[]
                {
                    (C(0xC30500), C(0xA83226)), // 양념 주
                    (C(0xAA110D), C(0x9E2D1F)), // 양념 중간
                    (C(0x880300), C(0x6E1C12)), // 양념 음영
                    (C(0xE3C77B), C(0xE8C86A)), // 면 하이라이트
                    (C(0x28B3BC), C(0x79A84C)), // 오이채 밝음 (실측: 그릇 장식 무늬 — tteokbokki 와 공유)
                    (C(0x0B696F), C(0x4E7A33)), // 오이 음영
                    (C(0x174E74), C(0x3D6626)), // 고명 정리
                    (C(0x0B325D), C(0x2F5220)), // 고명 정리
                },
                // 밝은 떡볶이 — 구체/바닥 역할은 픽셀 레이아웃 실측으로 확정 (설계 F-2, 시드와 반대).
                // 실측: 구체(미트볼) = #890502(밝음)/#730907(중간)/#4B0807(음영) → 흰 떡,
                //        바닥 소스 = #5B0503(지배 75px)/#881F00(밝은 영역), #400002 = 구체 접촉 그림자.
                // 방향 앵커(밝은 고추장 #D34A3A + 흰 떡 #F2E6D8)는 불변 — 같은 from/to 집합 내 역할 재배정.
                [$"{FoodIconsDir}/tteokbokki.png"] = new[]
                {
                    (C(0x5B0503), C(0xD34A3A)), // 바닥 소스 주 — F2 Gochujang Red (red 3종 분화 주색)
                    (C(0x881F00), C(0xC74534)), // 바닥 소스 중간 (좌측 밝은 영역)
                    (C(0x400002), C(0xA93325)), // 구체 접촉 그림자 → 소스 음영
                    (C(0x630200), C(0x8C2A1E)), // 깊은 소스 점
                    (C(0x890502), C(0xF2E6D8)), // 구체 밝음 → 떡 밝음
                    (C(0x730907), C(0xE7D6C2)), // 구체 중간 → 떡 중간
                    (C(0x4B0807), C(0xD9C7B2)), // 구체 음영 → 떡 음영
                    (C(0x28B3BC), C(0x6FA04E)), // 파 밝음 (실측: 그릇 장식 무늬 — bibim_guksu 와 공유)
                    (C(0x0B696F), C(0x4E7A33)),
                    (C(0x174E74), C(0x3D6626)),
                    (C(0x0B325D), C(0x2F5220)),
                },
                // 김밥 — 김 흑녹 정합 (밥 #DFD9C8/#FFFEE8/#B4A8A0/#8B7F7E · 속재료 소색상은 유지)
                [$"{FoodIconsDir}/gimbap.png"] = new[]
                {
                    (C(0x170625), C(0x1B2A1A)), // 김 주
                    (C(0x1C1030), C(0x223420)), // 김 결
                    (C(0x362939), C(0x2C4128)), // 김 음영
                    (C(0x524654), C(0x3A4F36)), // 김 경계
                },
                // ── 무대 타일 (B4 — F2 방향 보정) ──────────────────────────
                [$"{StageDir}/floor.png"] = new[]
                {
                    (C(0xE6DABF), C(0xF4E5C2)), // Steam Cream 실내 바닥
                    (C(0xD9CAA9), C(0xE4D0A6)),
                },
                [$"{StageDir}/counter.png"] = new[]
                {
                    (C(0xB48355), C(0xBE8A4E)), // warm amber 나무 카운터
                    (C(0xC58F5C), C(0xD3A462)),
                    (C(0xA07247), C(0xA2713F)),
                    (C(0x8F673F), C(0x7C5A34)),
                },
            };

            // ── student 의상 (B3) — 단일 쌍, idle+walk0..3 5파일 공통 적용 ──
            // 실측 #D14B34 는 Gochujang Red #D34A3A(음식 accent·primary action 예약색)와 사실상 동일
            // (ΔRGB 2,1,10) → 청 데님 전환으로 4종 의상 hue 를 파랑/올리브/골드/그레이로 분리.
            // office_worker/family_parent/senior_regular 는 매핑 없음 — 바이트 불변 (B3).
            var studentSwap = new[] { (C(0xD14B34), C(0x3F6FA6)) };
            maps[$"{CustomersDir}/student.png"] = studentSwap;
            for (int f = 0; f < WalkFrameCount; f++)
            {
                maps[$"{CustomersDir}/student_walk{f}.png"] = studentSwap;
            }

            return maps;
        }

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
            Debug.Log("[PlaceholderArtBuilder] OpenSource-derived sprites ready (customers 4×(idle+walk4), food icons 6, recolored — task-114)");
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
        // 손님 — Ninja Adventure 우향 idle + 걷기 4프레임 파생 (student 는 의상 스왑 — task-114 B3)
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
                string idleOut = $"{CustomersDir}/{id}.png";
                WritePng(idleOut,
                    SwapPalette(Region(idlePx, idleW, idleH, 3 * CustomerFrame, 0, CustomerFrame, CustomerFrame), idleOut),
                    CustomerFrame, CustomerFrame);

                // 우향 걷기 = Walk.png 이미지 행3(우향). Texture2D 는 bottom-origin 이라 시각 맨 아래 행(우향)은
                // 텍스처 y∈[0,16). 4프레임 x=0,16,32,48.
                for (int f = 0; f < WalkFrameCount; f++)
                {
                    string walkOut = $"{CustomersDir}/{id}_walk{f}.png";
                    WritePng(walkOut,
                        SwapPalette(Region(walkPx, walkW, walkH, f * CustomerFrame, 0, CustomerFrame, CustomerFrame), walkOut),
                        CustomerFrame, CustomerFrame);
                }
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // 음식 — karsiori/Henry 매핑 + 한식 리컬러 + 32×32 캔버스 표준화 (task-114 B1 순서 고정)
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
                    pixels = trimmed;
                    w = tw;
                    h = th;
                }

                // task-114 파생 확장 — ①정수 사전 확대 → ②팔레트 스왑 → ③32×32 캔버스 패딩 (B1 순서).
                string path = $"{FoodIconsDir}/{id}.png";
                pixels = PrescaleIfSmall(pixels, ref w, ref h);
                pixels = SwapPalette(pixels, path);
                pixels = PadToCanvas(pixels, w, h, FoodCanvas, FoodCanvas);
                WritePng(path, pixels, FoodCanvas, FoodCanvas);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // 무대 타일 — Kenney Roguelike/RPG 시트에서 바닥/카운터 16×16 타일 추출 (순수 장식)
        // task-114: Steam Cream / warm amber 팔레트 보정 (B4)
        // ════════════════════════════════════════════════════════════════════
        static void BuildStageTiles()
        {
            var sheet = LoadReadableTexture(KenneySheet);
            var px = sheet.GetPixels32();
            int w = sheet.width, h = sheet.height;
            Object.DestroyImmediate(sheet);

            WritePng(FloorTilePath,
                SwapPalette(KenneyTilePixels(px, w, h, FloorTileCell), FloorTilePath), KenneyTile, KenneyTile);
            WritePng(CounterTilePath,
                SwapPalette(KenneyTilePixels(px, w, h, CounterTileCell), CounterTilePath), KenneyTile, KenneyTile);
        }

        /// <summary>Kenney 시트(bottom-origin 픽셀)에서 (col,row) 타일을 잘라낸다. row 는 PIL top-origin 그리드.</summary>
        static Color32[] KenneyTilePixels(Color32[] px, int w, int h, (int col, int row) cell)
        {
            int x = cell.col * KenneyStride;
            int topY = cell.row * KenneyStride;              // PIL top-origin y
            int y = h - topY - KenneyTile;                   // bottom-origin y
            return Region(px, w, h, x, y, KenneyTile, KenneyTile);
        }

        // ── task-114 리컬러/캔버스 헬퍼 (순수 결정론 — B1) ──────────────────

        /// <summary>
        /// PaletteMaps 의 exact-match 팔레트 스왑 — 불투명 픽셀만, alpha 보존, 미등재 색 통과 (단일 패스).
        /// 매핑이 없는 파생 경로는 그대로 통과한다 (비대상 손님 3종 바이트 불변의 근거).
        /// </summary>
        static Color32[] SwapPalette(Color32[] px, string derivedPath)
        {
            if (!PaletteMaps.TryGetValue(derivedPath, out var pairs))
            {
                return px;
            }
            for (int i = 0; i < px.Length; i++)
            {
                var p = px[i];
                if (p.a == 0)
                {
                    continue;
                }
                foreach (var (from, to) in pairs)
                {
                    if (p.r == from.r && p.g == from.g && p.b == from.b)
                    {
                        px[i] = new Color32(to.r, to.g, to.b, p.a);
                        break;
                    }
                }
            }
            return px;
        }

        /// <summary>
        /// 트림 결과 w≤16 ∧ h≤16 인 소스만 nearest-neighbor ×2 정수 사전 확대 (현재 gimbap 13×13 만 해당 —
        /// 규칙 기반, 이름 하드코딩 아님). 축소·비정수 리샘플링은 금지 (제약).
        /// </summary>
        static Color32[] PrescaleIfSmall(Color32[] src, ref int w, ref int h)
        {
            if (w > SmallSourceMax || h > SmallSourceMax)
            {
                return src;
            }
            int w2 = w * 2, h2 = h * 2;
            var outPx = new Color32[w2 * h2];
            for (int y = 0; y < h2; y++)
            {
                for (int x = 0; x < w2; x++)
                {
                    outPx[y * w2 + x] = src[(y / 2) * w + (x / 2)];
                }
            }
            w = w2;
            h = h2;
            return outPx;
        }

        /// <summary>
        /// 투명 캔버스 중앙 패딩 — offset = floor((canvas−w)/2), floor((canvas−h)/2) (bottom-origin,
        /// 홀수 잔여는 내용이 좌/하로 붙는다 — 결정론 고정). w/h 가 캔버스를 넘으면 Region 가드와 동일하게 예외.
        /// </summary>
        static Color32[] PadToCanvas(Color32[] src, int w, int h, int canvasW, int canvasH)
        {
            if (w > canvasW || h > canvasH)
            {
                throw new System.InvalidOperationException(
                    $"[PlaceholderArtBuilder] 캔버스 초과: {w}×{h} > {canvasW}×{canvasH}");
            }
            int ox = (canvasW - w) / 2;
            int oy = (canvasH - h) / 2;
            var outPx = new Color32[canvasW * canvasH]; // Color32 기본값 = (0,0,0,0) 투명
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    outPx[(oy + y) * canvasW + (ox + x)] = src[y * w + x];
                }
            }
            return outPx;
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
