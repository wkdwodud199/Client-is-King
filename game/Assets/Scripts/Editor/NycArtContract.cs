using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ClientIsKing.EditorTools
{
    /// <summary>
    /// task-116 NYC 코리아타운 아트 오버홀 — 임포트·검증 계약 (design.md B~H절, A안 확정).
    ///
    /// 콘셉트 6종(kb/concepts/art-originals/)은 레퍼런스 성경일 뿐, 이 클래스가 픽셀을 생성하지 않는다.
    /// 스프라이트 해상도 재생산(A안)은 오너/Codex가 입력 패키지(배치 1 — 32파일)로 전달하고,
    /// 이 클래스는 그 입력의 **경로 상수·Asset Map·임포트 표준 적용**만 소유한다(D/H절).
    /// PlaceholderArtBuilder 와 달리 생성 빌더가 아니다 — 원본 팩에서 잘라 파생하는 단계가 없다
    /// (F3 exact-match 키잉 채택 시에만 최소 파생 로직이 U2에서 추가될 수 있다).
    ///
    /// 컴파일 안전: NYC 자산이 하나도 없어도(U1 시점) 이 파일은 상수·경로 나열·임포트 메서드로만
    /// 구성되어 정상 컴파일된다. 자산 존재를 전제하는 최상위 코드가 없다.
    /// 임포트 표준: Sprite · PPU 32 · Point · 무압축 · mipmap off · alphaIsTransparency (H절 — 테스트가 고정).
    /// </summary>
    public static class NycArtContract
    {
        public const string Root = "Assets/Art/NYC";
        public const string CustomersDir = Root + "/Customers";
        public const string FoodIconsDir = Root + "/FoodIcons";
        public const string UiIconsDir = Root + "/UiIcons";
        public const string StageDir = Root + "/Stage";
        public const string ProvenancePath = Root + "/NYC-ART-PROVENANCE.md";

        const int WalkFrameCount = 4; // 우향 걷기 프레임 수 (idle 1 별도 — D/F5절)

        // archetype id (기존 catalog id 재사용 — 콘셉트↔런타임↔코드 1:1 대응, D절)
        static readonly string[] CustomerIds =
        {
            "student",
            "office_worker",
            "family_parent",
            "senior_regular",
        };

        // recipeId (기존 catalog id 재사용 — D절)
        static readonly string[] FoodIconIds =
        {
            "pork_gukbap",
            "beef_gukbap",
            "janchi_guksu",
            "bibim_guksu",
            "tteokbokki",
            "gimbap",
        };

        // 장르 id (기존 FoodIcons 재활용 슬롯을 전용 UI 아이콘으로 승격 — D절)
        static readonly string[] GenreIds =
        {
            "gukbap",
            "bunsik",
            "noodle",
            "generalist",
        };

        /// <summary>
        /// 배치 1 Asset Map 전수(32파일) — 존재/규격/임포트 검증 공용 (D절).
        /// 손님 4종×(idle 1 + walk 4) = 20, 음식 아이콘 6, 장르 UI 아이콘 4, 무대 2.
        /// </summary>
        public static IEnumerable<string> AllSpritePaths()
        {
            foreach (var id in CustomerIds)
            {
                yield return $"{CustomersDir}/{id}.png"; // idle
                for (int f = 0; f < WalkFrameCount; f++)
                {
                    yield return $"{CustomersDir}/{id}_walk{f}.png";
                }
            }
            foreach (var id in FoodIconIds)
            {
                yield return $"{FoodIconsDir}/{id}.png";
            }
            foreach (var id in GenreIds)
            {
                yield return $"{UiIconsDir}/genre_{id}.png";
            }
            yield return $"{StageDir}/backdrop.png";
            yield return $"{StageDir}/counter.png";
        }

        /// <summary>손님 idle 스프라이트 경로만 (카탈로그 배선 공용 — U3).</summary>
        public static IEnumerable<string> CustomerIdlePaths()
        {
            foreach (var id in CustomerIds)
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

        public static string FoodIconPath(string recipeId) => $"{FoodIconsDir}/{recipeId}.png";
        public static string GenreIconPath(string genreId) => $"{UiIconsDir}/genre_{genreId}.png";
        public static string BackdropPath => $"{StageDir}/backdrop.png";
        public static string CounterPath => $"{StageDir}/counter.png";

        /// <summary>
        /// 픽셀 표준 고정: Sprite · PPU 32 · Point · 무압축 · mipmap off · alphaIsTransparency (H절).
        /// 자산이 아직 임포트되지 않은 경로(AssetImporter 조회 실패)는 예외로 실패 — U2 수입 이후에만 호출된다.
        /// </summary>
        public static void ApplyImportSettings(string path)
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

        /// <summary>배치 1 전수(32파일)에 임포트 표준을 적용 (U2 수입 이후 호출 — 자산 부재 시 예외).</summary>
        public static void ApplyImportSettingsToAll()
        {
            foreach (var path in AllSpritePaths())
            {
                ApplyImportSettings(path);
            }
        }
    }
}
