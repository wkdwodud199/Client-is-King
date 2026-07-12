# 구현 노트 — task-115 (Group 1: 도메인 + 밸런스 코어)

> 작성자: Claude (G1 담당). Status: **완료 — 오너 승인 시드 15,000 확정, 전체 EditMode 검증 완료.**

## 설계 수정 (오너 승인 2026-07-12)

design.md B3 원안(운영비 12,000→28,000, 조정 밴드 [22,000, 30,000])은 **전제 오류**로 폐기되었다 —
B1 진단 표의 "100일 평균 기여이익"(~37k~52k)을 곧 "일 순마진의 하한"으로 오인해 밴드를 산정했으나,
실제로는 100일 FNV 스케줄 중 **최악의 개별 날**(`gukbap` day32, 임대료 인상 영구 활성 상태)의
순이익이 원래(12,000 기준) **+5,031원**밖에 되지 않는다. 이 값이 `EventBalanceTests.Guard1`
("전 장르 100일 매일 순이익 > 0")의 실질적 상한을 결정하며, 그 상한은 약 16,373원(여유 0)으로
design.md 가 지정한 밴드와 전혀 겹치지 않는다(실측 근거는 아래 "발견 과정" 절 참조).

오너가 이 사실을 확인하고 **"온건(~15,000)"** 노선으로 재확정했다 — 원래 12,000 대비 +3,000(25%
인상)으로 데모 긴장감은 확보하되, gukbap day32 같은 최악 케이스에도 여유(net=+1,581)를 남긴다.

**최종 확정값**: `SettlementOps.DailyOperatingCost` / `EventOps.BaseDailyOperatingCost` = **15,000**
(전체 EditMode 배치 실행으로 `Guard1`(전 장르×100일 순이익>0) green 확인 완료 — 아래 "실측 결과" 참조).

## 발견 과정 (요약 — 밴드 폐기의 실측 근거)

`EventBalanceTests.Guard1`은 `Assert.Greater`로 첫 실패에서 멈추는 구조라 장르 순회 순서(ordinal:
bunsik→generalist→gukbap→noodles)상 뒤 장르의 더 나쁜 값이 가려진다. early-exit 없는 임시 진단
테스트(확인 후 제거)로 전 장르×100일을 전수 조사한 결과, **`gukbap day32, net=+5,031원
(OperatingCost=12,000)`이 전역 최저값**임을 확정했다. 이 지점 하나만으로 밴드 전체가 조건 1을
만족하지 못한다(정수 역산: `net(S) = 5031 - (MulMilliHalfUp(S,1150) - MulMilliHalfUp(12000,1150))`
— 밴드 하한 22,000에서도 net=-6,469, 밴드 전체에서 실패). 조건 1을 만족하는 정수 최댓값은 16,373
(여유 0)이며, 안전 마진을 둔 15,000(여유 1,581)을 오너가 최종 채택했다.

## 최종 실측 — 100일 실루프 최저 순이익 날 (시드 15,000)

`EventBalanceTests.Guard1_Every_Day_Net_Profit_Is_Positive_For_All_Genres` 전수 실측(early-exit
없는 임시 진단으로 확인, 이후 정식 테스트로 복원):

| 장르 | 최저 일일 순이익 | 100일 평균 |
|------|------------------|------------|
| gukbap | **+1,581** (day32, 임대료 인상 영구 활성) | 31,966.41 |
| bunsik | +11,623 | 31,299.10 |
| noodles | +12,541 | 33,051.60 |
| generalist | +9,900 | 34,519.49 |

전 장르·전 100일에서 순이익이 양수임을 확인했다(무위 플레이만 파산 가능, 성실 플레이는 항상 흑자
— B3 목표 그대로 유지).

Guard2(3중첩 최악 조합, day13 가정): gukbap=24,401 / bunsik=12,239 / noodles=17,088 /
generalist=16,028 (전부 양수). Guard3(Day1~3 무이벤트~폭등): bunsik Day3 마감 잔액=112,568,
Day3 폭등 순이익 gukbap=23,725/bunsik=22,489/noodles=27,621/generalist=33,546 (전부 양수, 표
그대로 흑자 유지).

## 스코프 밖 fixture 2개 재확인 결과 — 수정 불필요

애초 22,000~28,000대 시드에서는 깨졌던 아래 두 파일이, **최종 확정값 15,000에서는 재설계 없이
그대로 통과**함을 확인했다(경계값이 우연히 살아남는 지점):

- `SettlementPanelSceneTests.cs`(`EventEffectText_Shows_Cause_Line_When_Surge_Active_On_Day3`):
  Day1~2 "전부 포기" fixture 는 시작 자금 30,000원에서 운영비 15,000원×2일=30,000원을 정확히
  소진한다. `SettlementOps.ApplyDailySettlement`의 파산 판정이 `cashBefore >= cost`(엄격한 `>`가
  아님)라 Day2 정산 후 cash=0 이지만 파산이 아니다 — Day3 진입이 그대로 성립한다.
- `SettlementPresentationTests.cs`(`Final_Texts_Match_SettlementResult_Exactly`): fixture
  `cash=40000, revenue=20000, spend=5000` 의 `NetProfit = 20000-5000-15000 = 0`. `SettlementPanel`
  netText 규약("≥0 이면 +")과 하드코딩된 `+{NetProfit:N0}` 리터럴이 "0"에서도 부호 충돌 없이
  일치한다(음수로 뒤집히지 않음).

두 파일 모두 **byte-unchanged**로 남겨뒀다 — 리터럴/fixture 변경을 하지 않았다.

## U2 — EndingOps 도메인 (완료, 시드와 무관)

- `game/Assets/Scripts/Runtime/DayCycle/EndingOps.cs`, `EndingSummary.cs` 신규 — 설계 C2 그대로
  구현(분기 순서 `isBankrupt→Bankrupt`, `daysCompleted≥7→Cleared`, else `None`; `ClearTargetDays=7`;
  `BuildSummary` 는 `SNSCampaignOps.CalculateFollowerDisplay` 위임).
- `GameManager.cs`: `CanAdvancePhase` 파산 게이트 직후 클리어 차단 추가(사유 문자열 정확 일치),
  `AdvancePhase` 최상단 가드를 `EndingOps.IsRunEnded`로 확장 + 정산 인라인 적용 후 클리어 반환 분기
  추가, `LoadMainMenuScene()` 신설(`LoadShopScene` 미러).
- 테스트: `EndingOpsTests.cs`(파생 매트릭스·BuildSummary 전수·null 예외), `GameManagerEndingGateTests.cs`
  (차단·사유·이벤트 0·상태 불변·게이트 미발동 조건·파산 무회귀).

## U1/U3 — 밸런스 상수·가드 (완료)

- `EventOps.BaseDailyOperatingCost` 명명 상수 신설(=15,000) + 리터럴 2곳(원래 EventOps.cs:444,508)
  치환 완료 + `BalanceEndingGuardTests.Constant_Sync_Pins`로 동기 핀.
- B4 락스텝 목록 전수를 최종 시드(15,000) 기준으로 재계산·동기화(GenreBalanceTests/SNSBalanceTests/
  EventBalanceTests 상수, EventOpsTests/NightPanelEventFlowTests 파생 문자열, Guard1/2/3 실측값).
- Whitelist 8지점(SNS 숏폼 12,000·SaveOpsTests V3 fixture)은 **byte-unchanged** 확인함(`git diff
  --stat` 무변화).
- `BalanceEndingGuardTests.cs` 신규: 전 장르 7일 실루프 클리어 도달(조건 1), 무위 플레이 Day≤3
  파산(조건 3), 상수 동기 핀(조건 3), 클리어 저장 왕복(조건 4) — 전부 15,000 기준 통과.

## 검증

전체 EditMode 배치 실행(기준선 439 + 신규 EndingOpsTests/GameManagerEndingGateTests/
BalanceEndingGuardTests) — 결과는 `kb/artifacts/task-115-summary.md` 참조.
