using System.Collections.Generic;
using ClientIsKing.Data;
using ClientIsKing.DayCycle;
using ClientIsKing.Events;
using ClientIsKing.Save;
using ClientIsKing.Service;
using ClientIsKing.Settlement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClientIsKing.Managers
{
    /// <summary>
    /// 게임 전역 매니저 (싱글턴 MonoBehaviour 8종 중 첫 번째 — 브리프 규약).
    /// task-104 시점에는 상태 보관·phase 진행·씬 로드의 얇은 래퍼만 제공한다.
    /// 경제/인벤토리/서비스 로직은 후속 매니저(task-105+)가 담당한다.
    /// task-110: 정렬된 GenreDef catalog 의 유일한 런타임 소유자이며, Market→Service 전환 직전
    /// ServiceManager.TryStartServiceDay 를 원자적으로 성공시킨 경우에만 phase event 를 발행한다.
    /// task-113: 저장/불러오기 API(SaveGame/TryLoadGame/TryPeekSave/StartNewRun)와 자동 저장 트리거
    /// 배선을 이 매니저의 얇은 확장으로 제공한다(신규 SaveManager singleton 없음 — F1/F4).
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private List<GenreDef> genreCatalog = new List<GenreDef>();
        [SerializeField] private List<GameEventDef> eventCatalog = new List<GameEventDef>();

        private GameState state;
        private DayPhaseMachine machine;

        /// <summary>현재 런타임 상태 (Awake 이후 항상 non-null).</summary>
        public GameState State => state;

        /// <summary>정렬된 genre 4종 catalog (SceneBuilder 가 MainMenu/Shop 양쪽에 동일하게 주입).</summary>
        public IReadOnlyList<GenreDef> GenreCatalog => genreCatalog;

        /// <summary>정렬된 이벤트 4종 catalog (SceneBuilder 가 MainMenu/Shop 양쪽에 동일하게 주입, task-112 E1).</summary>
        public IReadOnlyList<GameEventDef> EventCatalog => eventCatalog;

        // ── 저장/불러오기 (task-113 F1) ──────────────────────────────────────

        /// <summary>테스트 전용 경로 override (IVT) — null/빈 문자열이면 기본 경로를 사용한다.</summary>
        internal static string SaveFilePathOverride;

        string ResolveSavePath()
        {
            return string.IsNullOrEmpty(SaveFilePathOverride) ? SaveFileStore.DefaultPath : SaveFilePathOverride;
        }

        /// <summary>마지막 저장 시도의 실패 사유 ("" = 마지막 저장 성공).</summary>
        public string LastAutoSaveFailReason { get; private set; } = "";

        /// <summary>마지막 저장이 기록된 일차 (Night 표시용).</summary>
        public int LastAutoSaveDay { get; private set; }

        /// <summary>마지막 저장이 기록된 phase (Night 표시용).</summary>
        public DayPhase LastAutoSavePhase { get; private set; }

        /// <summary>현재 경로에 저장 파일이 존재하는가.</summary>
        public bool HasSaveFile => SaveFileStore.Exists(ResolveSavePath());

        private void Awake()
        {
            // 두 씬 모두 GameManager 부트스트랩을 포함하므로(어느 씬에서 시작해도 동작),
            // 이미 살아있는 인스턴스가 있으면 중복 쪽을 제거한다.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (Application.isPlaying)
            {
                // EditMode 테스트(FirstPlayableLoopTests)에서도 생성 가능하도록 Play 모드에서만 적용.
                DontDestroyOnLoad(gameObject);
            }
            // 암묵적 부트스트랩(state==null) 은 저장하지 않는다 — 앱 부팅이 기존 세이브를 덮어쓰는
            // 사고를 구조적으로 차단한다. 저장을 동반한 새 런은 StartNewRun() 하나뿐이다.
            if (state == null)
            {
                StartNewGame();
            }
            GameEvents.GenreSelected += OnGenreSelected;
            GameEvents.SNSCampaignExecuted += OnSnsCampaignExecuted;
        }

        private void OnDestroy()
        {
            GameEvents.GenreSelected -= OnGenreSelected;
            GameEvents.SNSCampaignExecuted -= OnSnsCampaignExecuted;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void OnGenreSelected(string genreId)
        {
            AutoSave(); // F2 트리거 4 — 장르 확정
        }

        void OnSnsCampaignExecuted(string campaignId)
        {
            AutoSave(); // F2 트리거 5 — SNS 집행
        }

        /// <summary>새 게임 상태로 초기화한다 (day 1, Market). 저장은 하지 않는다 — MainMenu 이어하기가
        /// 아닌 부트스트랩·테스트 경로가 쓴다.</summary>
        public void StartNewGame()
        {
            state = new GameState();
            machine = new DayPhaseMachine(state);
        }

        /// <summary>새 런을 시작하고 즉시 저장한다 (F2 트리거 1) — MainMenu `게임 시작` 버튼 전용.</summary>
        public void StartNewRun()
        {
            StartNewGame();
            AutoSave();
        }

        /// <summary>catalog 에서 id 로 GenreDef 를 조회한다 (ordinal 비교).</summary>
        public bool TryGetGenre(string id, out GenreDef def)
        {
            if (!string.IsNullOrEmpty(id))
            {
                for (int i = 0; i < genreCatalog.Count; i++)
                {
                    if (genreCatalog[i] != null && genreCatalog[i].Id == id)
                    {
                        def = genreCatalog[i];
                        return true;
                    }
                }
            }
            def = null;
            return false;
        }

        /// <summary>catalog 에서 id 로 GameEventDef 를 조회한다 (ordinal 비교, task-112 E1).</summary>
        public bool TryGetEventDef(string id, out GameEventDef def)
        {
            if (!string.IsNullOrEmpty(id))
            {
                for (int i = 0; i < eventCatalog.Count; i++)
                {
                    if (eventCatalog[i] != null && eventCatalog[i].Id == id)
                    {
                        def = eventCatalog[i];
                        return true;
                    }
                }
            }
            def = null;
            return false;
        }

        internal static GameEventDefInput ToEventInput(GameEventDef def)
        {
            return new GameEventDefInput
            {
                Id = def.Id,
                DisplayName = def.DisplayName,
                Kind = def.Kind,
                BaseWeight = def.BaseWeight,
                DurationDays = def.DurationDays,
                PercentEffect = def.PercentEffect,
                FlatEffect = def.FlatEffect,
            };
        }

        internal static List<GameEventDefInput> ToEventInputs(IReadOnlyList<GameEventDef> defs)
        {
            var result = new List<GameEventDefInput>();
            foreach (var def in defs)
            {
                if (def != null)
                {
                    result.Add(ToEventInput(def));
                }
            }
            return result;
        }

        /// <summary>오늘(state.day) 활성 이벤트로부터 축 분리 효과를 계산한다 (task-112 E1).</summary>
        public bool TryBuildTodayEventEffects(out EventDayEffects fx, out string reason)
        {
            var defs = ToEventInputs(eventCatalog);
            return EventOps.TryBuildDayEffects(state.activeEvents, state.day, defs, out fx, out reason);
        }

        /// <summary>내일(state.day + 1) 예고를 계산한다 (task-112 E1). UI 는 이 DTO 를 재계산하지 않는다.</summary>
        public bool TryBuildEventForecast(out EventForecast forecast, out string reason)
        {
            var defs = ToEventInputs(eventCatalog);
            return EventOps.TryBuildForecast(state.activeEvents, state.day + 1, defs, out forecast, out reason);
        }

        /// <summary>
        /// 현재 phase 에서 다음 phase 로 진행 가능한지 도메인 규칙으로 판정한다 (design.md G3/H9).
        /// Market→Service: 선택 genre 가 catalog 에 존재하고 TryBuildDayPlan 이 성공해야 한다.
        /// Service→Settlement: serviceDay==day, 생성 주문 수==plan.orderCount, 열린 주문 없음.
        /// 그 외 phase 전환은 항상 허용한다.
        /// </summary>
        public bool CanAdvancePhase(out string reason)
        {
            reason = "";
            if (state == null)
            {
                reason = "게임 상태가 초기화되지 않았습니다.";
                return false;
            }
            if (state.isBankrupt)
            {
                reason = "파산 상태에서는 진행할 수 없습니다.";
                return false;
            }
            if (EndingOps.GetStatus(state) == RunEndingStatus.Cleared)
            {
                reason = "데모 클리어 상태에서는 진행할 수 없습니다.";
                return false;
            }

            switch (state.currentPhase)
            {
                case DayPhase.Market:
                    return CanAdvanceFromMarket(out reason);
                case DayPhase.Service:
                    return CanAdvanceFromService(out reason);
                case DayPhase.Settlement:
                    return CanAdvanceFromSettlement(out reason);
                case DayPhase.Night:
                    return CanAdvanceFromNight(out reason);
                default:
                    return true;
            }
        }

        /// <summary>Settlement 이탈 게이트(task-112 E1) — 오늘 이벤트 효과 계산 실패 시 손상 데이터로 차단.</summary>
        bool CanAdvanceFromSettlement(out string reason)
        {
            if (!TryBuildTodayEventEffects(out _, out reason))
            {
                return false;
            }
            reason = "";
            return true;
        }

        /// <summary>Night 이탈 게이트(task-112 E1) — 내일 활성 이벤트 전이 실패 시 손상 데이터로 차단.</summary>
        bool CanAdvanceFromNight(out string reason)
        {
            var defs = ToEventInputs(eventCatalog);
            if (!EventOps.TryBuildNextDayActiveEvents(state.activeEvents, state.day + 1, defs, out _, out _, out reason))
            {
                return false;
            }
            reason = "";
            return true;
        }

        bool CanAdvanceFromMarket(out string reason)
        {
            if (!TryGetGenre(state.selectedGenreId, out var genre))
            {
                reason = "먼저 전문 분야를 선택하세요.";
                return false;
            }
            var service = ServiceManager.Instance;
            if (service == null)
            {
                reason = "서비스 매니저가 초기화되지 않았습니다.";
                return false;
            }
            if (!service.TryBuildDayPlan(genre, out _, out reason))
            {
                return false;
            }
            reason = "";
            return true;
        }

        bool CanAdvanceFromService(out string reason)
        {
            if (state.serviceDay != state.day)
            {
                reason = "오늘 영업이 아직 시작되지 않았습니다.";
                return false;
            }
            var service = ServiceManager.Instance;
            if (service != null && TryGetGenre(state.selectedGenreId, out var genre)
                && service.TryBuildDayPlan(genre, out var plan, out _)
                && state.serviceOrders.Count != plan.OrderCount)
            {
                reason = "생성된 주문 수가 예상과 다릅니다.";
                return false;
            }
            if (ServiceOps.HasOpenOrders(state))
            {
                reason = "아직 처리하지 않은 주문이 있습니다.";
                return false;
            }
            reason = "";
            return true;
        }

        /// <summary>
        /// 다음 phase 로 진행한다 (이벤트 발행은 상태 머신이 담당).
        /// task-107 게이트: 파산이면 진행/이벤트 없이 현재 phase 를 유지하고,
        /// Settlement 이탈 전에 오늘 정산이 미적용이면 먼저 적용한다 (그 결과 파산이면 머문다).
        /// task-110 게이트: Market→Service 직전에 CanAdvancePhase 를 만족하고 TryStartServiceDay 가
        /// 원자적으로 성공한 경우에만 진행한다. 그 외 phase 는 기존 규칙을 유지한다.
        /// task-112 E1: Settlement 자동 정산은 오늘 이벤트 효과(fx) 반영 overload 로 계산하고(fx 실패
        /// 시 현재 phase 유지), Night→Market 경계는 machine.Advance() 직전에 activeEvents 를 원자
        /// 교체한다(DayPhaseChanged 발행 전 완료 — 별도 GameEvents 불필요).
        /// </summary>
        public DayPhase AdvancePhase()
        {
            if (EndingOps.IsRunEnded(state))
            {
                return state.currentPhase;
            }
            if (state.currentPhase == DayPhase.Settlement && !SettlementOps.IsSettlementApplied(state))
            {
                if (!TryBuildTodayEventEffects(out var fx, out _))
                {
                    return state.currentPhase;
                }
                var result = SettlementOps.ApplyDailySettlement(state, fx.OperatingCostMilli, fx.OperatingCostFlat);
                if (result.Applied)
                {
                    AutoSave(); // F2 트리거 3 — 정산 신규 적용 (파산 확정 포함, 클리어 상태도 여기서 저장된다)
                }
                if (result.Bankrupt)
                {
                    return state.currentPhase;
                }
                if (EndingOps.GetStatus(state) == RunEndingStatus.Cleared)
                {
                    return state.currentPhase;
                }
            }

            if (state.currentPhase == DayPhase.Market)
            {
                if (!CanAdvanceFromMarket(out _))
                {
                    return state.currentPhase;
                }
                var service = ServiceManager.Instance;
                if (service == null || !TryGetGenre(state.selectedGenreId, out var genre)
                    || !service.TryStartServiceDay(genre, out _))
                {
                    return state.currentPhase;
                }
            }
            else if (state.currentPhase == DayPhase.Service)
            {
                if (!CanAdvanceFromService(out _))
                {
                    return state.currentPhase;
                }
            }
            else if (state.currentPhase == DayPhase.Night)
            {
                var defs = ToEventInputs(eventCatalog);
                if (!EventOps.TryBuildNextDayActiveEvents(state.activeEvents, state.day + 1, defs, out var next, out _, out _))
                {
                    return state.currentPhase;
                }
                state.activeEvents = next;
            }

            var before = state.currentPhase;
            var after = machine.Advance();
            if (after != before)
            {
                AutoSave(); // F2 트리거 2 — phase 전환이 실제 발생한 경우만
            }
            return after;
        }

        /// <summary>Shop 씬을 로드한다 (Build Settings 등록 전제 — SceneBuilder 가 보장).</summary>
        public void LoadShopScene()
        {
            SceneManager.LoadScene("Shop");
        }

        /// <summary>MainMenu 씬을 로드한다 (Build Settings 등록 전제 — SceneBuilder 가 보장).
        /// task-115: 엔딩 오버레이 "메인 메뉴로 ▶" 버튼 전용 (LoadShopScene 미러).</summary>
        public void LoadMainMenuScene()
        {
            SceneManager.LoadScene("MainMenu");
        }

        // ── 저장/불러오기 API (task-113 F1/F4) ──────────────────────────────

        /// <summary>catalog 조립 — SaveOps 는 SO 를 모르므로 이 매니저가 문자열 ID 목록으로 투영한다.</summary>
        bool TryBuildSaveCatalogs(out SaveOps.SaveCatalogInputs catalogs, out string failReason)
        {
            catalogs = null;
            failReason = "";
            var service = ServiceManager.Instance;
            if (service == null)
            {
                failReason = "서비스 매니저가 초기화되지 않았습니다.";
                return false;
            }

            catalogs = new SaveOps.SaveCatalogInputs
            {
                GenreIds = ToIds(genreCatalog),
                RecipeIds = ToIds(service.RecipeDefs),
                CustomerIds = ToIds(service.CustomerDefs),
                SnsCampaignIds = ToIds(service.SnsCampaignDefs),
                EventDefs = ToEventInputs(eventCatalog),
            };
            return true;
        }

        static List<string> ToIds(IReadOnlyList<GenreDef> defs)
        {
            var result = new List<string>();
            foreach (var def in defs)
            {
                if (def != null) result.Add(def.Id);
            }
            return result;
        }

        static List<string> ToIds(IReadOnlyList<RecipeDef> defs)
        {
            var result = new List<string>();
            foreach (var def in defs)
            {
                if (def != null) result.Add(def.Id);
            }
            return result;
        }

        static List<string> ToIds(IReadOnlyList<CustomerArchetypeDef> defs)
        {
            var result = new List<string>();
            foreach (var def in defs)
            {
                if (def != null) result.Add(def.Id);
            }
            return result;
        }

        static List<string> ToIds(IReadOnlyList<SNSCampaignDef> defs)
        {
            var result = new List<string>();
            foreach (var def in defs)
            {
                if (def != null) result.Add(def.Id);
            }
            return result;
        }

        /// <summary>현재 상태를 저장한다 (항상 동작 — isPlaying 가드는 AutoSave 몫). 손상 상태는 저장하지 않는다.</summary>
        public bool SaveGame(out string failReason)
        {
            failReason = "";
            if (!TryBuildSaveCatalogs(out var catalogs, out failReason))
            {
                return false;
            }
            if (!SaveOps.TrySerialize(state, catalogs, out var json, out failReason))
            {
                return false;
            }
            return SaveFileStore.TryWriteAtomic(ResolveSavePath(), json, out failReason);
        }

        /// <summary>
        /// 자동 저장 (F2 트리거 공통 경로). Application.isPlaying 일 때만 실제로 저장을 시도한다 —
        /// EditMode 테스트의 AdvancePhase 호출 등이 파일을 쓰지 않도록 한다(332개 무회귀 전제).
        /// 성공/실패 모두 GameEvents.SaveStateChanged 를 발행한다. 저장 실패는 진행을 차단하지 않는다.
        /// </summary>
        internal void AutoSave()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (SaveGame(out var reason))
            {
                LastAutoSaveFailReason = "";
                LastAutoSaveDay = state.day;
                LastAutoSavePhase = state.currentPhase;
            }
            else
            {
                LastAutoSaveFailReason = reason;
                Debug.LogWarning($"자동 저장 실패: {reason}");
            }
            GameEvents.RaiseSaveStateChanged();
        }

        /// <summary>
        /// 저장 파일을 dry-run 으로 읽어 요약을 만든다 — TryLoadGame 과 동일하게 V11(주문 identity)
        /// 까지 포함한 검증을 거치며, 성공/실패 관계없이 상태를 원상복구한다(관찰 가능한 부작용 0).
        /// </summary>
        public bool TryPeekSave(out SaveSummary summary, out string failReason)
        {
            summary = null;
            var prevState = state;
            var prevMachine = machine;

            bool ok = TryLoadInternal(out var loaded, out failReason);
            state = prevState;
            machine = prevMachine;

            if (!ok)
            {
                return false;
            }
            if (!TryValidateOrderIdentity(loaded, out failReason))
            {
                return false;
            }
            summary = SaveOps.BuildSummary(loaded);
            return true;
        }

        /// <summary>
        /// 저장 파일을 읽어 상태를 설치한다. V11(주문 identity) 사후 검증까지 통과해야 확정되며,
        /// 실패하면 설치 전 상태로 완전히 롤백한다(관찰 가능한 부작용 0 — 동기 블록, 이벤트 미발행).
        /// </summary>
        public bool TryLoadGame(out string failReason)
        {
            var prevState = state;
            var prevMachine = machine;

            if (!TryLoadInternal(out var loaded, out failReason))
            {
                state = prevState;
                machine = prevMachine;
                return false;
            }

            state = loaded;
            machine = new DayPhaseMachine(state);

            if (!TryValidateOrderIdentity(state, out failReason))
            {
                state = prevState;
                machine = prevMachine;
                return false;
            }

            LastAutoSaveFailReason = "";
            LastAutoSaveDay = state.day;
            LastAutoSavePhase = state.currentPhase;
            return true;
        }

        /// <summary>공유 로드 절차 — 파일 읽기·역직렬화·V1~V10 검증까지. state/machine 은 건드리지 않는다.</summary>
        bool TryLoadInternal(out GameState loaded, out string failReason)
        {
            loaded = null;
            failReason = "";

            string path = ResolveSavePath();
            if (!SaveFileStore.Exists(path))
            {
                failReason = "저장 파일이 없습니다.";
                return false;
            }
            if (!SaveFileStore.TryRead(path, out var json, out failReason))
            {
                return false;
            }
            if (!TryBuildSaveCatalogs(out var catalogs, out failReason))
            {
                return false;
            }
            return SaveOps.TryDeserialize(json, catalogs, out loaded, out failReason);
        }

        /// <summary>
        /// V11 — 설치 후 주문 identity 재검증(design.md D절). 장르 미선택/파산이면 생략(통과).
        /// 오늘 주문을 보유(serviceDay==day)하면 TryBuildDayPlan + ServiceOps.BuildOrders(plan, customerDefs)
        /// 재생성 결과와 인덱스별 recipeId/customerId/partySize/snsInflow/eventInflow 를 비교한다.
        /// served/missed/serviceCurrentOrderIndex 는 저장값을 존중 — 비교 대상이 아니다.
        /// TryLoadGame(설치된 state) 과 TryPeekSave(dry-run loaded state) 양쪽에서 호출하므로 검증 대상
        /// 상태를 매개변수 <paramref name="s"/> 로 받는다 — this.state 를 직접 참조하지 않는다.
        /// </summary>
        bool TryValidateOrderIdentity(GameState s, out string failReason)
        {
            failReason = "";
            if (s.isBankrupt || string.IsNullOrEmpty(s.selectedGenreId))
            {
                return true;
            }
            if (!TryGetGenre(s.selectedGenreId, out var genre))
            {
                failReason = "저장 상태로 수요 계획을 재구성할 수 없습니다: 알 수 없는 전문 분야입니다.";
                return false;
            }
            var service = ServiceManager.Instance;
            if (service == null)
            {
                failReason = "저장 상태로 수요 계획을 재구성할 수 없습니다: 서비스 매니저가 초기화되지 않았습니다.";
                return false;
            }
            if (!service.TryBuildDayPlan(genre, out var plan, out var reason))
            {
                failReason = $"저장 상태로 수요 계획을 재구성할 수 없습니다: {reason}";
                return false;
            }
            if (s.serviceDay != s.day)
            {
                return true; // 오늘 주문을 보유하지 않음 — 재생성 비교 대상 아님
            }

            var rebuilt = ServiceOps.BuildOrders(plan, service.CustomerDefs);
            var saved = s.serviceOrders;
            if (rebuilt.Count != saved.Count)
            {
                failReason = "저장된 주문이 수요 계획과 일치하지 않습니다 (주문 개수).";
                return false;
            }
            for (int i = 0; i < rebuilt.Count; i++)
            {
                if (!string.Equals(rebuilt[i].recipeId, saved[i].recipeId, System.StringComparison.Ordinal)
                    || !string.Equals(rebuilt[i].customerId, saved[i].customerId, System.StringComparison.Ordinal)
                    || rebuilt[i].partySize != saved[i].partySize
                    || rebuilt[i].snsInflow != saved[i].snsInflow
                    || rebuilt[i].eventInflow != saved[i].eventInflow)
                {
                    failReason = $"저장된 주문이 수요 계획과 일치하지 않습니다 (주문 {i}).";
                    return false;
                }
            }
            return true;
        }

#if UNITY_EDITOR
        /// <summary>SceneBuilder 전용 참조 주입 — 정렬된 genre 4종을 MainMenu/Shop 양쪽에 동일하게 주입한다.</summary>
        internal void EditorInit(List<GenreDef> genreCatalog)
        {
            this.genreCatalog = genreCatalog;
        }

        /// <summary>SceneBuilder 전용 참조 주입(task-112) — 정렬된 이벤트 4종을 포함한 2-인자 overload.</summary>
        internal void EditorInit(List<GenreDef> genreCatalog, List<GameEventDef> eventCatalog)
        {
            this.genreCatalog = genreCatalog;
            this.eventCatalog = eventCatalog;
        }
#endif
    }
}
