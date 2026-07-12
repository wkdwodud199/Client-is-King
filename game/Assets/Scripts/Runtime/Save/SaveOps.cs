using System;
using System.Collections.Generic;
using ClientIsKing.DayCycle;
using ClientIsKing.Events;
using UnityEngine;

namespace ClientIsKing.Save
{
    /// <summary>
    /// 세이브 규칙의 단일 원천 (순수 — System.IO/씬/SO 금지, JsonUtility 만 예외 허용, task-113 C/D 절).
    ///
    /// 파이프라인 (로드): (1) json 공백 검사 → (2) SaveVersionProbe → (3) 버전 확인/마이그레이션 훅 →
    /// (4) FromJson&lt;GameState&gt; → (5) schemaVersion 재확인 → (6) V2b 정규형 검사(ToJson 재직렬화가
    /// 입력과 바이트 동일) → (7) TryValidateState(V3~V10). V11(주문 identity)은 manager 계층 몫이다.
    /// 파이프라인 (저장): schemaVersion 확인 → TryValidateState → ToJson(prettyPrint).
    /// </summary>
    public static class SaveOps
    {
        /// <summary>검증에 필요한 catalog 투영 입력 (manager 가 조립 — SaveOps 는 SO 를 모른다).</summary>
        public sealed class SaveCatalogInputs
        {
            public IReadOnlyList<string> GenreIds;
            public IReadOnlyList<string> RecipeIds;
            public IReadOnlyList<string> CustomerIds;
            public IReadOnlyList<string> SnsCampaignIds;
            public IReadOnlyList<GameEventDefInput> EventDefs;
        }

        [Serializable]
        internal sealed class SaveVersionProbe
        {
            public int schemaVersion = 0; // 필드 부재 = 0 = 명시적 실패 (v1 이전 배포 세이브는 존재하지 않는다)
        }

        // ── 진입 검증 (모든 공개 API 공유) ──────────────────────────────────

        static bool TryValidateCatalogs(SaveCatalogInputs catalogs, out string failReason)
        {
            failReason = "";
            if (catalogs == null)
            {
                failReason = "카탈로그 입력이 없습니다.";
                return false;
            }
            if (!TryValidateIdList(catalogs.GenreIds, "장르", out failReason)) return false;
            if (!TryValidateIdList(catalogs.RecipeIds, "레시피", out failReason)) return false;
            if (!TryValidateIdList(catalogs.CustomerIds, "고객", out failReason)) return false;
            if (!TryValidateIdList(catalogs.SnsCampaignIds, "SNS 캠페인", out failReason)) return false;
            if (!EventOps.TryValidateCatalog(catalogs.EventDefs, out failReason))
            {
                return false;
            }
            return true;
        }

        static bool TryValidateIdList(IReadOnlyList<string> ids, string label, out string failReason)
        {
            failReason = "";
            if (ids == null || ids.Count == 0)
            {
                failReason = $"{label} 카탈로그가 없습니다.";
                return false;
            }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in ids)
            {
                if (string.IsNullOrEmpty(id))
                {
                    failReason = $"{label} 카탈로그에 잘못된 ID 가 있습니다.";
                    return false;
                }
                if (!seen.Add(id))
                {
                    failReason = $"{label} 카탈로그에 중복된 ID '{id}' 가 있습니다.";
                    return false;
                }
            }
            return true;
        }

        // ── 저장 방향 ────────────────────────────────────────────────────────

        /// <summary>
        /// 상태를 검증(V3~V10)한 뒤에만 JSON 을 만든다 — 손상 상태를 디스크로 내보내지 않는다.
        /// prettyPrint 확정(사람이 읽는 디버깅 가치, 결정론 무관).
        /// </summary>
        public static bool TrySerialize(GameState state, SaveCatalogInputs catalogs, out string json, out string failReason)
        {
            json = null;
            failReason = "";

            if (state == null)
            {
                failReason = "게임 상태가 없습니다.";
                return false;
            }
            if (!TryValidateCatalogs(catalogs, out failReason))
            {
                return false;
            }
            if (state.schemaVersion != GameState.SaveSchemaVersion)
            {
                failReason = $"손상된 상태는 저장하지 않습니다: 지원하지 않는 저장 버전입니다 (v{state.schemaVersion}).";
                return false;
            }
            if (!TryValidateState(state, catalogs, out var stateReason))
            {
                failReason = $"손상된 상태는 저장하지 않습니다: {stateReason}";
                return false;
            }

            json = JsonUtility.ToJson(state, prettyPrint: true);
            return true;
        }

        // ── 로드 방향 ────────────────────────────────────────────────────────

        /// <summary>
        /// V1 구조 → V2 버전/마이그레이션 → FromJson&lt;GameState&gt; → V2b 정규형 → V3~V10 검증.
        /// 실패해도 out state 는 항상 null 이며 호출자(GameManager)의 현재 상태는 건드리지 않는다.
        /// </summary>
        public static bool TryDeserialize(string json, SaveCatalogInputs catalogs, out GameState state, out string failReason)
        {
            state = null;
            failReason = "";

            if (!TryValidateCatalogs(catalogs, out failReason))
            {
                return false;
            }

            // V1 — 구조
            if (string.IsNullOrWhiteSpace(json))
            {
                failReason = "저장 파일이 손상되었습니다 (JSON 파싱 실패).";
                return false;
            }

            SaveVersionProbe probe;
            try
            {
                probe = JsonUtility.FromJson<SaveVersionProbe>(json);
            }
            catch (ArgumentException)
            {
                failReason = "저장 파일이 손상되었습니다 (JSON 파싱 실패).";
                return false;
            }
            if (probe == null)
            {
                failReason = "저장 파일이 손상되었습니다 (JSON 파싱 실패).";
                return false;
            }

            // V2 — 버전
            string workingJson = json;
            if (probe.schemaVersion != GameState.SaveSchemaVersion)
            {
                if (!TryMigrateToCurrent(probe.schemaVersion, ref workingJson, out failReason))
                {
                    failReason = $"지원하지 않는 저장 버전입니다 (v{probe.schemaVersion}).";
                    return false;
                }
            }

            GameState loaded;
            try
            {
                loaded = JsonUtility.FromJson<GameState>(workingJson);
            }
            catch (ArgumentException)
            {
                failReason = "저장 파일이 손상되었습니다 (JSON 파싱 실패).";
                return false;
            }
            if (loaded == null)
            {
                failReason = "저장 파일이 손상되었습니다 (JSON 파싱 실패).";
                return false;
            }
            if (loaded.schemaVersion != probe.schemaVersion)
            {
                failReason = "저장 데이터 검증 실패: 스키마 버전이 일치하지 않습니다.";
                return false;
            }

            // V2b — 정규형 검사 (필드 누락·명시 null·잉여/미지 키·순서 변조·중복 키를 단일 규칙으로 감지)
            string canonical = JsonUtility.ToJson(loaded, prettyPrint: true);
            if (!string.Equals(canonical, workingJson, StringComparison.Ordinal))
            {
                failReason = "저장 파일이 정규 직렬화 형식과 일치하지 않습니다 (필드 누락 또는 변조).";
                return false;
            }

            // V3~V10
            if (!TryValidateState(loaded, catalogs, out failReason))
            {
                return false;
            }

            state = loaded;
            return true;
        }

        /// <summary>
        /// 마이그레이션 훅 — v1 단일: fileVersion == SaveSchemaVersion 만 통과. task-113 시점에는 v1 이전
        /// 배포 세이브가 존재하지 않으므로 모든 불일치(0 = 필드 누락 포함)가 실패다.
        /// 미래: case 1: MigrateV1ToV2(ref json); 체인을 여기에 추가한다.
        /// </summary>
        internal static bool TryMigrateToCurrent(int fileVersion, ref string json, out string failReason)
        {
            failReason = "";
            if (fileVersion == GameState.SaveSchemaVersion)
            {
                return true;
            }
            failReason = $"지원하지 않는 저장 버전입니다 (v{fileVersion}).";
            return false;
        }

        // ── 검증 매트릭스 V3~V10 (저장·peek·로드 공유 — 단일 함수) ───────────

        /// <summary>
        /// 저장·peek·로드 세 경로가 공유하는 단일 검증 (V3~V10 전부). 실패 시 첫 위반 사유를 반환한다.
        /// 어느 필드도 기본값 보정으로 통과시키지 않는다 — null/범위 밖/불일치는 전부 명시적 실패.
        /// </summary>
        public static bool TryValidateState(GameState state, SaveCatalogInputs catalogs, out string failReason)
        {
            failReason = "";
            if (state == null)
            {
                failReason = "게임 상태가 없습니다.";
                return false;
            }
            if (!TryValidateCatalogs(catalogs, out failReason))
            {
                return false;
            }

            if (!ValidateCommon(state, out failReason)) return false;         // V3
            if (!ValidateBankruptcy(state, out failReason)) return false;     // V4
            if (!ValidateGenre(state, catalogs, out failReason)) return false; // V5
            if (!ValidateInventory(state, out failReason)) return false;     // V6
            if (!ValidateService(state, catalogs, out failReason)) return false; // V7
            if (!ValidatePurchase(state, out failReason)) return false;      // V8
            if (!ValidateSns(state, catalogs, out failReason)) return false; // V9
            if (!ValidateEvents(state, catalogs, out failReason)) return false; // V10

            return true;
        }

        // V3 — 상태 공통 (non-null 총칙 + 구조 불변식)
        static bool ValidateCommon(GameState state, out string failReason)
        {
            failReason = "";
            if (state.ingredientStocks == null)
            {
                failReason = "저장 데이터 검증 실패: 재료 인벤토리 목록이 없습니다.";
                return false;
            }
            if (state.serviceOrders == null)
            {
                failReason = "저장 데이터 검증 실패: 주문 목록이 없습니다.";
                return false;
            }
            if (state.snsCampaignHistory == null)
            {
                failReason = "저장 데이터 검증 실패: SNS 집행 기록 목록이 없습니다.";
                return false;
            }
            if (state.activeEvents == null)
            {
                failReason = "저장 데이터 검증 실패: 활성 이벤트 목록이 없습니다.";
                return false;
            }
            if (state.selectedGenreId == null)
            {
                failReason = "저장 데이터 검증 실패: 전문 분야 ID 가 없습니다.";
                return false;
            }
            if (state.bankruptcyReason == null)
            {
                failReason = "저장 데이터 검증 실패: 파산 사유 필드가 없습니다.";
                return false;
            }
            if (state.day < 1)
            {
                failReason = "저장 데이터 검증 실패: 일차가 잘못되었습니다.";
                return false;
            }
            if (state.currentPhase < DayPhase.Market || state.currentPhase > DayPhase.Night)
            {
                failReason = "저장 데이터 검증 실패: 알 수 없는 phase 값입니다.";
                return false;
            }
            if (state.cash < 0)
            {
                failReason = "저장 데이터 검증 실패: 보유 자금이 음수입니다.";
                return false;
            }
            if (state.settlementDay < 0 || state.settlementDay > state.day)
            {
                failReason = "저장 데이터 검증 실패: 정산 일차가 잘못되었습니다.";
                return false;
            }
            bool settledToday = state.settlementDay == state.day;
            int expectedDaysCompleted = (settledToday && !state.isBankrupt) ? state.day : state.day - 1;
            if (state.daysCompleted != expectedDaysCompleted)
            {
                failReason = "저장 데이터 검증 실패: 완료 일수가 정산 기록과 일치하지 않습니다.";
                return false;
            }
            if (state.settlementDay >= 1)
            {
                int expectedNet = state.settlementGrossRevenue - state.settlementIngredientSpend - state.settlementOperatingCost;
                if (state.settlementNetProfit != expectedNet)
                {
                    failReason = "저장 데이터 검증 실패: 정산 순이익이 매출/지출과 일치하지 않습니다.";
                    return false;
                }
                if (state.settlementGrossRevenue < 0 || state.settlementIngredientSpend < 0 || state.settlementOperatingCost < 0)
                {
                    failReason = "저장 데이터 검증 실패: 정산 금액이 음수입니다.";
                    return false;
                }
            }
            if (state.currentPhase == DayPhase.Night && state.settlementDay != state.day)
            {
                failReason = "저장 데이터 검증 실패: Night phase 인데 오늘 정산이 적용되지 않았습니다.";
                return false;
            }
            return true;
        }

        // V4 — 파산 정합
        static bool ValidateBankruptcy(GameState state, out string failReason)
        {
            failReason = "";
            if (!state.isBankrupt)
            {
                if (state.bankruptcyDay != 0 || state.bankruptcyReason != "")
                {
                    failReason = "저장 데이터 검증 실패: 파산 기록이 일관되지 않습니다.";
                    return false;
                }
                return true;
            }
            if (state.bankruptcyDay != state.day || state.settlementDay != state.day
                || state.cash != 0 || string.IsNullOrEmpty(state.bankruptcyReason))
            {
                failReason = "저장 데이터 검증 실패: 파산 기록이 일관되지 않습니다.";
                return false;
            }
            return true;
        }

        // V5 — 장르
        static bool ValidateGenre(GameState state, SaveCatalogInputs catalogs, out string failReason)
        {
            failReason = "";
            if (state.selectedGenreId == "")
            {
                if (state.day == 1 && state.currentPhase == DayPhase.Market)
                {
                    return true;
                }
                failReason = "저장 데이터 검증 실패: 전문 분야가 선택되지 않은 상태가 잘못되었습니다.";
                return false;
            }
            if (!Contains(catalogs.GenreIds, state.selectedGenreId))
            {
                failReason = $"저장 데이터 검증 실패: 알 수 없는 전문 분야 ID '{state.selectedGenreId}' 입니다.";
                return false;
            }
            return true;
        }

        // V6 — 인벤토리
        static bool ValidateInventory(GameState state, out string failReason)
        {
            failReason = "";
            var seen = new HashSet<(int, int)>();
            foreach (var stock in state.ingredientStocks)
            {
                if (stock == null)
                {
                    failReason = "저장 데이터 검증 실패: 인벤토리 항목이 잘못되었습니다.";
                    return false;
                }
                int kind = (int)stock.kind;
                int grade = (int)stock.grade;
                if (kind < 0 || kind > 8)
                {
                    failReason = "저장 데이터 검증 실패: 알 수 없는 재료 종류입니다.";
                    return false;
                }
                if (grade < 0 || grade > 1)
                {
                    failReason = "저장 데이터 검증 실패: 알 수 없는 재료 등급입니다.";
                    return false;
                }
                if (stock.quantity < 0)
                {
                    failReason = "저장 데이터 검증 실패: 재료 수량이 음수입니다.";
                    return false;
                }
                if (!seen.Add((kind, grade)))
                {
                    failReason = "저장 데이터 검증 실패: 인벤토리에 중복된 (종류,등급) 항목이 있습니다.";
                    return false;
                }
            }
            return true;
        }

        // V7 — 서비스
        static bool ValidateService(GameState state, SaveCatalogInputs catalogs, out string failReason)
        {
            failReason = "";
            if (state.serviceDay < 0 || state.serviceDay > state.day)
            {
                failReason = "저장 데이터 검증 실패: 영업 일차가 잘못되었습니다.";
                return false;
            }

            int servedCount = 0, missedCount = 0, custServed = 0, custMissed = 0;
            int snsServed = 0, snsMissed = 0, snsRevenue = 0;
            int evtServed = 0, evtMissed = 0, evtRevenue = 0;

            var orders = state.serviceOrders;
            bool sawOpen = false;
            for (int i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                if (order == null || order.recipeId == null || order.customerId == null)
                {
                    failReason = "저장 데이터 검증 실패: 주문 항목이 잘못되었습니다.";
                    return false;
                }
                if (string.IsNullOrEmpty(order.recipeId) || !Contains(catalogs.RecipeIds, order.recipeId))
                {
                    failReason = $"저장 데이터 검증 실패: 알 수 없는 레시피 ID '{order.recipeId}' 입니다.";
                    return false;
                }
                if (string.IsNullOrEmpty(order.customerId) || !Contains(catalogs.CustomerIds, order.customerId))
                {
                    failReason = $"저장 데이터 검증 실패: 알 수 없는 고객 ID '{order.customerId}' 입니다.";
                    return false;
                }
                if (order.partySize < 1)
                {
                    failReason = "저장 데이터 검증 실패: 주문 파티 크기가 잘못되었습니다.";
                    return false;
                }
                if (order.served && order.missed)
                {
                    failReason = "저장 데이터 검증 실패: 주문이 서빙과 포기 상태를 동시에 갖고 있습니다.";
                    return false;
                }

                bool isOpen = !order.served && !order.missed;
                if (isOpen)
                {
                    sawOpen = true;
                }
                else if (sawOpen)
                {
                    failReason = "저장 데이터 검증 실패: 처리된 주문이 열린 주문 뒤에 있습니다.";
                    return false;
                }

                if (order.served)
                {
                    servedCount++;
                    custServed += order.partySize;
                    if (order.snsInflow) snsServed++;
                    if (order.eventInflow) evtServed++;
                }
                if (order.missed)
                {
                    missedCount++;
                    custMissed += order.partySize;
                    if (order.snsInflow) snsMissed++;
                    if (order.eventInflow) evtMissed++;
                }
            }

            if (state.serviceCurrentOrderIndex < 0 || state.serviceCurrentOrderIndex > orders.Count)
            {
                failReason = "저장 데이터 검증 실패: 현재 주문 인덱스가 범위를 벗어났습니다.";
                return false;
            }
            for (int i = 0; i < state.serviceCurrentOrderIndex; i++)
            {
                if (orders[i].IsOpen)
                {
                    failReason = "저장 데이터 검증 실패: 인덱스 앞의 주문이 아직 열려 있습니다.";
                    return false;
                }
            }
            for (int i = state.serviceCurrentOrderIndex; i < orders.Count; i++)
            {
                if (!orders[i].IsOpen)
                {
                    failReason = "저장 데이터 검증 실패: 인덱스 뒤의 주문이 이미 처리되었습니다.";
                    return false;
                }
            }

            if (state.serviceOrdersServedToday != servedCount || state.serviceOrdersMissedToday != missedCount
                || state.serviceCustomersServedToday != custServed || state.serviceCustomersMissedToday != custMissed)
            {
                failReason = "저장 데이터 검증 실패: 서비스 통계가 주문 목록과 일치하지 않습니다.";
                return false;
            }
            if (state.serviceSnsOrdersServedToday != snsServed || state.serviceSnsOrdersMissedToday != snsMissed)
            {
                failReason = "저장 데이터 검증 실패: SNS 유입 통계가 주문 목록과 일치하지 않습니다.";
                return false;
            }
            if (state.serviceEventOrdersServedToday != evtServed || state.serviceEventOrdersMissedToday != evtMissed)
            {
                failReason = "저장 데이터 검증 실패: 단체 손님 통계가 주문 목록과 일치하지 않습니다.";
                return false;
            }
            if (state.serviceRevenueToday < 0 || state.serviceSnsRevenueToday < 0 || state.serviceEventRevenueToday < 0)
            {
                failReason = "저장 데이터 검증 실패: 서비스 매출이 음수입니다.";
                return false;
            }
            _ = snsRevenue; _ = evtRevenue; // 매출 3종은 ≥0 만 검증(태그 매출은 집계 대상 아님)

            if ((state.currentPhase == DayPhase.Settlement || state.currentPhase == DayPhase.Night)
                && (state.serviceDay != state.day || sawOpenOrdersRemain(state)))
            {
                failReason = "저장 데이터 검증 실패: 정산/밤 phase 인데 처리되지 않은 주문이 남아 있습니다.";
                return false;
            }
            if (state.currentPhase == DayPhase.Service && state.serviceDay != state.day)
            {
                failReason = "저장 데이터 검증 실패: 영업 phase 인데 오늘 영업이 시작되지 않았습니다.";
                return false;
            }

            return true;
        }

        static bool sawOpenOrdersRemain(GameState state)
        {
            foreach (var order in state.serviceOrders)
            {
                if (order.IsOpen)
                {
                    return true;
                }
            }
            return false;
        }

        // V8 — 구매
        static bool ValidatePurchase(GameState state, out string failReason)
        {
            failReason = "";
            if (state.marketSpendDay < 0 || state.marketSpendDay > state.day)
            {
                failReason = "저장 데이터 검증 실패: 구매 지출 일차가 잘못되었습니다.";
                return false;
            }
            if (state.marketSpendToday < 0)
            {
                failReason = "저장 데이터 검증 실패: 구매 지출액이 음수입니다.";
                return false;
            }
            if (state.marketEventSurchargeToday < 0 || state.marketEventSurchargeToday > state.marketSpendToday)
            {
                failReason = "저장 데이터 검증 실패: 이벤트 할증액이 구매 지출액과 일치하지 않습니다.";
                return false;
            }
            return true;
        }

        // V9 — SNS
        static bool ValidateSns(GameState state, SaveCatalogInputs catalogs, out string failReason)
        {
            failReason = "";
            var seenDays = new HashSet<int>();
            SNSCampaignRecordLite todayRecord = null;
            foreach (var record in state.snsCampaignHistory)
            {
                if (record == null || record.campaignId == null)
                {
                    failReason = "저장 데이터 검증 실패: SNS 집행 기록이 잘못되었습니다.";
                    return false;
                }
                if (string.IsNullOrEmpty(record.campaignId) || !Contains(catalogs.SnsCampaignIds, record.campaignId))
                {
                    failReason = $"저장 데이터 검증 실패: 알 수 없는 SNS 캠페인 ID '{record.campaignId}' 입니다.";
                    return false;
                }
                if (record.executedOnDay < 1 || record.executedOnDay > state.day)
                {
                    failReason = "저장 데이터 검증 실패: SNS 집행 일차가 잘못되었습니다.";
                    return false;
                }
                if (record.costPaid < 0 || record.effectiveMilliReach < 0 || record.followerGain < 0)
                {
                    failReason = "저장 데이터 검증 실패: SNS 집행 기록 값이 음수입니다.";
                    return false;
                }
                if (record.bonusOrderCount < 0 || record.bonusOrderCount > 2)
                {
                    failReason = "저장 데이터 검증 실패: SNS 보너스 주문 수가 잘못되었습니다.";
                    return false;
                }
                if (!seenDays.Add(record.executedOnDay))
                {
                    failReason = "저장 데이터 검증 실패: 같은 날 SNS 집행 기록이 중복되었습니다.";
                    return false;
                }
                if (record.executedOnDay == state.day)
                {
                    todayRecord = new SNSCampaignRecordLite(record.costPaid);
                }
            }

            if (state.currentPhase == DayPhase.Night && !state.isBankrupt)
            {
                int expectedCash = todayRecord != null
                    ? state.settlementCashAfter - todayRecord.CostPaid
                    : state.settlementCashAfter;
                if (state.cash != expectedCash)
                {
                    failReason = "저장 데이터 검증 실패: Night 잔액이 정산/SNS 집행과 일치하지 않습니다.";
                    return false;
                }
            }
            return true;
        }

        sealed class SNSCampaignRecordLite
        {
            public readonly int CostPaid;
            public SNSCampaignRecordLite(int costPaid) { CostPaid = costPaid; }
        }

        // V10 — 이벤트 (EventOps 기존 검증 재사용 — 제2 구현 금지)
        static bool ValidateEvents(GameState state, SaveCatalogInputs catalogs, out string failReason)
        {
            return EventOps.TryBuildDayEffects(state.activeEvents, state.day, catalogs.EventDefs, out _, out failReason);
        }

        static bool Contains(IReadOnlyList<string> ids, string id)
        {
            if (ids == null)
            {
                return false;
            }
            for (int i = 0; i < ids.Count; i++)
            {
                if (string.Equals(ids[i], id, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        // ── 표시 요약 ────────────────────────────────────────────────────────

        /// <summary>검증 통과 상태의 표시 요약 (MainMenu 이어하기 라벨용 — UI 재계산 금지).</summary>
        public static SaveSummary BuildSummary(GameState state)
        {
            return new SaveSummary
            {
                Day = state.day,
                Phase = state.currentPhase,
                Cash = state.cash,
                DaysCompleted = state.daysCompleted,
                IsBankrupt = state.isBankrupt,
                SelectedGenreId = state.selectedGenreId,
            };
        }
    }
}
