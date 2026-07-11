using System.Collections.Generic;
using System.IO;
using ClientIsKing.Data;
using UnityEditor;
using UnityEngine;

namespace ClientIsKing.EditorTools
{
    /// <summary>
    /// task-103: 초기(시드) 데이터 에셋을 멱등 생성/갱신하는 배치 진입점.
    ///
    /// 실행(리포 루트 기준):
    ///   Unity.exe -batchmode -quit -nographics -projectPath game
    ///     -executeMethod ClientIsKing.EditorTools.InitialDataBuilder.Apply -logFile [log]
    ///
    /// 멱등 규약: 같은 경로의 asset 이 있으면 값만 갱신한다 (삭제 후 재생성 금지 — GUID 안정).
    /// 수치는 데모 밸런스 초안 — 하드캡/참조 무결성이 계약이며 수치는 playtest 에서 조정한다.
    /// </summary>
    public static class InitialDataBuilder
    {
        const string Root = "Assets/Data/Definitions";
        const float QualityC = 0.5f;
        const float QualityB = 0.75f;

        public static void Apply()
        {
            EnsureFolders();

            BuildIngredients();
            var customers = BuildCustomerArchetypes();
            var genres = BuildGenres(customers);
            BuildRecipes(genres);
            BuildSNSCampaigns();
            BuildGameEvents();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[InitialDataBuilder] seed data applied (ingredients 18, recipes 6, genres 4, customers 4, sns 3, events 4)");
        }

        static void EnsureFolders()
        {
            string[] dirs =
            {
                Root,
                Root + "/Ingredients",
                Root + "/Recipes",
                Root + "/Genres",
                Root + "/Customers",
                Root + "/SNS",
                Root + "/Events",
            };
            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            AssetDatabase.Refresh();
        }

        /// <summary>경로의 asset 을 로드하거나 없으면 생성한다 (GUID 안정 — 삭제/재생성 금지).</summary>
        static T Upsert<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        static string Slug(IngredientKind kind)
        {
            switch (kind)
            {
                case IngredientKind.Rice: return "rice";
                case IngredientKind.RiceCake: return "rice_cake";
                case IngredientKind.Noodle: return "noodle";
                case IngredientKind.Pork: return "pork";
                case IngredientKind.Beef: return "beef";
                case IngredientKind.FishCake: return "fish_cake";
                case IngredientKind.Seaweed: return "seaweed";
                case IngredientKind.Vegetable: return "vegetable";
                case IngredientKind.Gochujang: return "gochujang";
                default: throw new System.ArgumentOutOfRangeException(nameof(kind), kind, "unknown ingredient kind");
            }
        }

        // ── 재료 18 = 9종 × C/B ─────────────────────────────────────────────
        static void BuildIngredients()
        {
            // (종류, 표시명, C등급 단가, B등급 단가) — 원 단위
            (IngredientKind kind, string name, int costC, int costB)[] rows =
            {
                (IngredientKind.Rice, "쌀", 300, 500),
                (IngredientKind.RiceCake, "떡", 400, 650),
                (IngredientKind.Noodle, "소면", 350, 550),
                (IngredientKind.Pork, "돼지고기", 900, 1400),
                (IngredientKind.Beef, "소고기", 1200, 1900),
                (IngredientKind.FishCake, "어묵", 400, 600),
                (IngredientKind.Seaweed, "김", 250, 400),
                (IngredientKind.Vegetable, "채소", 200, 350),
                (IngredientKind.Gochujang, "고추장", 150, 250),
            };
            foreach (var r in rows)
            {
                UpsertIngredient(r.kind, r.name, IngredientGrade.C, r.costC, QualityC);
                UpsertIngredient(r.kind, r.name, IngredientGrade.B, r.costB, QualityB);
            }
        }

        static void UpsertIngredient(IngredientKind kind, string korName, IngredientGrade grade, int cost, float quality)
        {
            string slug = Slug(kind);
            string gradeSlug = grade == IngredientGrade.C ? "c" : "b";
            var asset = Upsert<IngredientDef>($"{Root}/Ingredients/{slug}_{gradeSlug}.asset");
            asset.EditorInit($"{slug}_{gradeSlug}", $"{korName} ({grade}급)", kind, grade, cost, quality);
            EditorUtility.SetDirty(asset);
        }

        // ── 고객 archetype 4 ────────────────────────────────────────────────
        static Dictionary<string, CustomerArchetypeDef> BuildCustomerArchetypes()
        {
            var map = new Dictionary<string, CustomerArchetypeDef>();
            (string id, string name, AgeBand age, GenderTarget gender,
             float weight, float patience, float sensitivity, int minParty, int maxParty)[] rows =
            {
                ("student", "학생 단골", AgeBand.Teens, GenderTarget.All, 1.0f, 60f, 0.8f, 1, 3),
                ("office_worker", "직장인", AgeBand.Twenties, GenderTarget.All, 1.2f, 45f, 0.5f, 1, 2),
                ("family_parent", "가족 손님", AgeBand.ThirtiesForties, GenderTarget.All, 0.9f, 75f, 0.55f, 2, 4),
                ("senior_regular", "동네 어르신", AgeBand.FiftiesPlus, GenderTarget.All, 0.7f, 90f, 0.35f, 1, 2),
            };
            foreach (var r in rows)
            {
                var asset = Upsert<CustomerArchetypeDef>($"{Root}/Customers/{r.id}.asset");
                asset.EditorInit(r.id, r.name, r.age, r.gender, r.weight, r.patience, r.sensitivity,
                    new IntRange(r.minParty, r.maxParty));
                EditorUtility.SetDirty(asset);
                map[r.id] = asset;
            }
            return map;
        }

        // ── 장르 4 = 3종 + 제네럴리스트 ─────────────────────────────────────
        // task-110 승인 seed (design.md D5 고정 — 구현 중 임의 재밸런싱 금지):
        // 국밥 (1.15, 1.20, 0.95) / 분식 (0.85, 0.80, 1.05) / 면류 (0.95, 1.00, 0.95) / 제네럴리스트 (1, 1, 1).
        // price 배수가 직관과 반대로 보이는 이유는 recipe base price 가 이미 장르별로 다르기 때문이다.
        // description 은 E3 headline/비교/forecast 문구와 정합하게 유지한다.
        static Dictionary<GenreKind, GenreDef> BuildGenres(Dictionary<string, CustomerArchetypeDef> customers)
        {
            var map = new Dictionary<GenreKind, GenreDef>();
            (string id, string name, GenreKind kind, float cost, float time, float price,
             (string archetypeId, float mult)[] affinities, string desc)[] rows =
            {
                ("gukbap", "국밥", GenreKind.Gukbap, 1.15f, 1.2f, 0.95f,
                    new[] { ("student", 0.7f), ("office_worker", 1.0f), ("family_parent", 1.0f), ("senior_regular", 1.5f) },
                    "진한 국물 한 그릇 — 원가와 조리가 무겁지만 1인 가격이 높고 중장년 단골이 탄탄하다."),
                ("bunsik", "분식", GenreKind.Bunsik, 0.85f, 0.8f, 1.05f,
                    new[] { ("student", 1.5f), ("office_worker", 1.1f), ("family_parent", 1.0f), ("senior_regular", 0.6f) },
                    "싸고 빠르게 도는 분식 — 원가가 낮고 주문이 많아 젊은 층 유입이 강하다."),
                ("noodles", "면류", GenreKind.Noodles, 0.95f, 1.0f, 0.95f,
                    new[] { ("student", 0.9f), ("office_worker", 1.0f), ("family_parent", 1.15f), ("senior_regular", 1.2f) },
                    "균형 잡힌 면 요리 — 원가·가격·회전이 모두 중간이고 가족·직장인에게 고루 어필한다."),
                ("generalist", "제네럴리스트", GenreKind.Generalist, 1f, 1f, 1f,
                    new[] { ("student", 1.0f), ("office_worker", 1.0f), ("family_parent", 1.0f), ("senior_regular", 1.0f) },
                    "특화 없는 균형 선택지 — 장르 배수 없이 데모 레시피 6종 전체를 다룬다. 레시피의 직접 장르로는 쓰지 않는다."),
            };
            foreach (var r in rows)
            {
                var affinities = new List<CustomerGenreAffinity>();
                foreach (var a in r.affinities)
                {
                    affinities.Add(new CustomerGenreAffinity(customers[a.archetypeId], a.mult));
                }
                var asset = Upsert<GenreDef>($"{Root}/Genres/{r.id}.asset");
                asset.EditorInit(r.id, r.name, r.kind, r.cost, r.time, r.price, affinities, r.desc);
                EditorUtility.SetDirty(asset);
                map[r.kind] = asset;
            }
            return map;
        }

        // ── 레시피 6 = 국밥/분식/면류 × 2 ───────────────────────────────────
        static void BuildRecipes(Dictionary<GenreKind, GenreDef> genres)
        {
            (string id, string name, GenreKind genre,
             (IngredientKind kind, int qty)[] reqs, float cook, int price)[] rows =
            {
                ("pork_gukbap", "돼지국밥", GenreKind.Gukbap,
                    new[] { (IngredientKind.Pork, 2), (IngredientKind.Rice, 1), (IngredientKind.Vegetable, 1) }, 14f, 9000),
                ("beef_gukbap", "소고기국밥", GenreKind.Gukbap,
                    new[] { (IngredientKind.Beef, 2), (IngredientKind.Rice, 1), (IngredientKind.Vegetable, 1) }, 16f, 11000),
                ("tteokbokki", "떡볶이", GenreKind.Bunsik,
                    new[] { (IngredientKind.RiceCake, 2), (IngredientKind.Gochujang, 1), (IngredientKind.FishCake, 1) }, 10f, 5000),
                ("gimbap", "김밥", GenreKind.Bunsik,
                    new[] { (IngredientKind.Rice, 1), (IngredientKind.Seaweed, 1), (IngredientKind.Vegetable, 1) }, 8f, 4500),
                ("janchi_guksu", "잔치국수", GenreKind.Noodles,
                    new[] { (IngredientKind.Noodle, 2), (IngredientKind.Vegetable, 1) }, 9f, 6000),
                ("bibim_guksu", "비빔국수", GenreKind.Noodles,
                    new[] { (IngredientKind.Noodle, 2), (IngredientKind.Gochujang, 1), (IngredientKind.Vegetable, 1) }, 11f, 6500),
            };
            foreach (var r in rows)
            {
                var reqs = new List<RecipeIngredientRequirement>();
                foreach (var q in r.reqs)
                {
                    reqs.Add(new RecipeIngredientRequirement(q.kind, q.qty));
                }
                var asset = Upsert<RecipeDef>($"{Root}/Recipes/{r.id}.asset");
                asset.EditorInit(r.id, r.name, genres[r.genre], reqs, r.cook, r.price);
                EditorUtility.SetDirty(asset);
            }
        }

        // ── SNS 채널 3 (가상 채널명) ────────────────────────────────────────
        // 비용은 task-111 design.md B1 승인 seed (밸런스 가드 G 근거 — 구현 중 임의 재밸런싱 금지).
        // 도달률·감쇠율·친화 배수·문구는 task-103 값 유지, GUID 보존 upsert.
        static void BuildSNSCampaigns()
        {
            (string id, string name, SNSChannel channel, int cost, float reach, float decay,
             (AgeBand age, GenderTarget gender, float mult)[] affinities, string desc)[] rows =
            {
                ("photo_feed", "픽쳐그램", SNSChannel.PhotoFeed, 15000, 0.25f, 0.85f,
                    new[] { (AgeBand.Twenties, GenderTarget.Female, 1.5f), (AgeBand.ThirtiesForties, GenderTarget.Female, 1.2f), (AgeBand.Teens, GenderTarget.All, 1.1f) },
                    "사진 중심 피드. 비주얼 좋은 메뉴가 20~30대 여성에게 잘 퍼진다."),
                ("short_form", "숏핑", SNSChannel.ShortForm, 12000, 0.30f, 0.80f,
                    new[] { (AgeBand.Teens, GenderTarget.All, 1.6f), (AgeBand.Twenties, GenderTarget.All, 1.3f) },
                    "숏폼 영상. 도달은 넓고 빠르지만 반복 사용 시 피로도가 크다."),
                ("local_board", "동네게시판", SNSChannel.LocalBoard, 7000, 0.15f, 0.90f,
                    new[] { (AgeBand.FiftiesPlus, GenderTarget.All, 1.5f), (AgeBand.ThirtiesForties, GenderTarget.All, 1.25f) },
                    "지역 커뮤니티. 도달은 좁지만 중장년 단골 전환이 좋고 감쇠가 완만하다."),
            };
            foreach (var r in rows)
            {
                var affinities = new List<SNSAudienceAffinity>();
                foreach (var a in r.affinities)
                {
                    affinities.Add(new SNSAudienceAffinity(a.age, a.gender, a.mult));
                }
                var asset = Upsert<SNSCampaignDef>($"{Root}/SNS/{r.id}.asset");
                asset.EditorInit(r.id, r.name, r.channel, r.cost, r.reach, r.decay, affinities, r.desc);
                EditorUtility.SetDirty(asset);
            }
        }

        // ── 이벤트 4 (하드캡) ───────────────────────────────────────────────
        // task-112 승인 seed (design.md B1 고정 — 구현 중 임의 재밸런싱 금지): 위생 flat 80,000 →
        // 8,000원(단일 이벤트 파산 강제 금지), 단체 flat 6 → 4(파티 크기 규약 전환), "불시"/"예고 없이"
        // 문구 2건을 전날 예고 규약에 맞게 교체. weight·duration·percent·타 이벤트는 불변 (GUID 보존 upsert).
        static void BuildGameEvents()
        {
            (string id, string name, GameEventKind kind, float weight, int days, float percent, int flat, string desc)[] rows =
            {
                ("ingredient_price_surge", "재료값 폭등", GameEventKind.IngredientPriceSurge, 1.0f, 2, 0.35f, 0,
                    "시장 파동으로 재료 구매가가 +35% 오른다 (2일)."),
                ("hygiene_inspection", "위생 점검", GameEventKind.HygieneInspection, 0.8f, 1, 0f, 8000,
                    "위생 점검이 예고됐다. 대응 비용 8,000원이 정산에 더해진다 (1일)."),
                ("rent_increase", "임대료 인상", GameEventKind.RentIncrease, 0.6f, 0, 0.15f, 0,
                    "건물주가 임대료를 +15% 올린다 (영구, durationDays 0 = 영구 규약)."),
                ("group_customers", "단체 손님", GameEventKind.GroupCustomers, 0.9f, 1, 0f, 4,
                    "회식 단체 1팀(4인)이 예약 방문한다. 재료를 넉넉히 준비하자 (1일)."),
            };
            foreach (var r in rows)
            {
                var asset = Upsert<GameEventDef>($"{Root}/Events/{r.id}.asset");
                asset.EditorInit(r.id, r.name, r.kind, r.weight, r.days, r.percent, r.flat, r.desc);
                EditorUtility.SetDirty(asset);
            }
        }
    }
}
