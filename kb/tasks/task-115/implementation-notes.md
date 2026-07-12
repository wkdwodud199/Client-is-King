# 구현 노트 — task-115 (밸런싱 + 엔딩 + Windows 빌드, M3 최종)

> Status: done
>
> G1(도메인+밸런스)·G2(엔딩 표현+빌드)·G3(통합검증+기록) 전부 완료. 자동 검증 게이트(컴파일·
> EditMode·PlayMode·BuildWindows) 전부 통과. **Codex 코드리뷰 approved**(2026-07-12, reviews/001.md —
> Action required 없음). 오너 플레이테스트·640×360 시각 승인 3종·빌드 exe 스모크는 done 게이트
> 조건으로 별도 대기(미결 — Claude self-approve 금지).

## Codex 코드 리뷰 결과 (2026-07-12 · reviews/001.md)

- **판정 approved · Action required 없음.** Codex 확인: `GameState` 무필드추가·`SaveSchemaVersion`
  1 유지·엔딩 파생(isBankrupt 우선/daysCompleted≥7 클리어)·GameManager 클리어 게이트 대칭 차단·
  운영비 15,000 동기화·`BalanceEndingGuardTests` 핀·엔딩 오버레이(Canvas 폴링·640×360 모달·최상단
  sibling)·MainMenu 클리어 분기·`BuildTool` 2씬 검증+실패 예외·`ClientIsKing.exe` 존재+`.gitignore`
  적용. 리뷰 중 validator/generate-status --check/--check-done 전부 통과 확인. 설계 원안 28,000 →
  15,000 재확정도 "밴드 절차가 깨진 이유를 재실측으로 설명 + 새 승인값을 테스트에 반영" 근거로 수용.
- Codex 는 Unity 전체 테스트를 재실행하지 않음(EditMode 486/PlayMode 9 는 Claude 독립 재실행으로 확인).
- **approved 는 코드/자동검증/문서일관성 승인이지, 오너 수동 게이트 통과나 M3 최종 선언을 대체하지 않는다**(Codex 명시).

## 설계 수정 (오너 승인 2026-07-12)

design.md B3 원안(운영비 12,000→28,000, 조정 밴드 [22,000, 30,000])은 **전제 오류**로 폐기되었다 —
B1 진단 표의 "100일 평균 기여이익"(~37k~52k)을 곧 "일 순마진의 하한"으로 오인해 밴드를 산정했으나,
실제로는 100일 FNV 스케줄 중 **최악의 개별 날**(`gukbap` day32, 임대료 인상 영구 활성 상태)의
순이익이 원래(12,000 기준) **+5,031원**밖에 되지 않는다. 이 값이 `EventBalanceTests.Guard1`
("전 장르 100일 매일 순이익 > 0")의 실질적 상한을 결정하며, 그 상한은 약 16,373원(여유 0)으로
design.md 가 지정한 밴드와 전혀 겹치지 않는다(실측 근거는 아래 "발견 과정" 절 참조).

오너가 이 사실을 확인하고 **"온건(~15,000)"** 노선으로 재확정했다 — 원래 12,000 대비 +3,000(25%
인상)으로 데모 긴장감은 확보하되, gukbap day32 같은 최악 케이스에도 여유(net=+1,581)를 남긴다.

**최종 확정값**: `SettlementOps.DailyOperatingCost` / `EventOps.BaseDailyOperatingCost` = **15,000**.

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

## 스코프 밖 fixture 2개 재확인 결과 — 수정 불필요 (G1)

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

## G1 — 도메인 + 밸런스 코어 (완료)

**U2 — EndingOps 도메인 (시드와 무관):**
- `game/Assets/Scripts/Runtime/DayCycle/EndingOps.cs`, `EndingSummary.cs` 신규 — 설계 C2 그대로
  구현(분기 순서 `isBankrupt→Bankrupt`, `daysCompleted≥7→Cleared`, else `None`; `ClearTargetDays=7`;
  `BuildSummary` 는 `SNSCampaignOps.CalculateFollowerDisplay` 위임).
- `GameManager.cs`: `CanAdvancePhase` 파산 게이트 직후 클리어 차단 추가(사유 문자열 정확 일치),
  `AdvancePhase` 최상단 가드를 `EndingOps.IsRunEnded`로 확장 + 정산 인라인 적용 후 클리어 반환 분기
  추가, `LoadMainMenuScene()` 신설(`LoadShopScene` 미러).
- 테스트: `EndingOpsTests.cs`(파생 매트릭스·BuildSummary 전수·null 예외), `GameManagerEndingGateTests.cs`
  (차단·사유·이벤트 0·상태 불변·게이트 미발동 조건·파산 무회귀).

**U1/U3 — 밸런스 상수·가드:**
- `EventOps.BaseDailyOperatingCost` 명명 상수 신설(=15,000) + 리터럴 2곳(원래 EventOps.cs:444,508)
  치환 완료 + `BalanceEndingGuardTests.Constant_Sync_Pins`로 동기 핀.
- B4 락스텝 목록 전수를 최종 시드(15,000) 기준으로 재계산·동기화(GenreBalanceTests/SNSBalanceTests/
  EventBalanceTests 상수, EventOpsTests/NightPanelEventFlowTests 파생 문자열, Guard1/2/3 실측값).
- Whitelist 8지점(SNS 숏폼 12,000·SaveOpsTests V3 fixture)은 **byte-unchanged** 확인함.
- `BalanceEndingGuardTests.cs` 신규: 전 장르 7일 실루프 클리어 도달(조건 1), 무위 플레이 Day≤3
  파산(조건 3), 상수 동기 핀(조건 3), 클리어 저장 왕복(조건 4) — 전부 15,000 기준 통과.

## G2 — 엔딩 표현 + Windows 빌드 (완료, 커밋 79231d4)

**엔딩 표현 (design D):**
- `EndingOverlayController.cs` 신규 — Canvas 탑재 상태 폴링(`Update`→`RefreshNow`), `None`이 아니고
  숨김이면 `Render`+표시+버튼 focus, `None`인데 표시 중이면 숨김. 클리어="데모 클리어!"(Brass Amber)
  / 파산="게임 오버"(Warning Plum), 버튼→`GameManager.LoadMainMenuScene()`.
- `SceneBuilder.BuildEndingOverlay` — `Panel_Ending`(640×360 Ink Navy 모달, raycastTarget true)
  + Title/Stats/Message/MainMenuButton 5종, 장르 modal 이후 생성해 최상단 sibling, 초기 비활성.
- `MainMenuController.RefreshSaveUi` 4→5분기 — 클리어 분기 추가(파산 앞, `EndingOps.IsCleared`
  단일 판정, 이어하기 잠금 + Brass Amber 문구).
- FIX: `SceneBuilder.cs` 정산 패널 정적 플레이스홀더 `"운영비 -12,000원"` → `"운영비 -0원"`
  중립값(SNS `"숏핑 12,000원"`은 보존 — whitelist 무변경).

**Windows 빌드 (design E1):**
- `game/Assets/Scripts/Editor/BuildTool.cs` 신규 — `ScenePaths()`(활성 씬 정확히 [MainMenu,Shop]
  검증) + `BuildWindows()`(`BuildPipeline.BuildPlayer` StandaloneWindows64, 실패 시 예외로 exit
  비제로, 성공 시 exe 경로·크기 로그). `-nographics` 미사용(셔이더 처리 안전 우선, E1 명시).

**테스트:** `EndingOverlaySceneTests.cs`(D1 표 전수 — 좌표·색·sibling·raycastTarget·3분기 RefreshNow),
`BuildToolTests.cs`(ScenePaths 순수부), `SceneBuilderTests.cs`/`MainMenuSaveFlowTests.cs` 갱신.
독립 재확인: EditMode 486/486(G1 470 + 신규 16).

## G3 — 통합 검증 + 기록 (완료, 이 커밋)

**PlayMode (신규 `EndingPlayModeTests.cs`):**
- 클리어 세이브 fixture 를 Day1~7 실루프(전량 서빙·C급 실요구량 구매)로 실제 도메인 경로 완주해
  준비한다(EditMode `BalanceEndingGuardTests.PlayOneFullDay` 전례의 PlayMode 판 — 정산 필드·
  `serviceOrders`가 프로덕션 코드 산출물이라 V3~V11(주문 identity 재검증 포함)이 자동 일관된다).
  Day7 정산 성공 시 자동 저장(트리거3)된 파일을 그대로 사용 — 별도 SetUpFixture 는 걸지 않는다
  (`SaveLoadPlayModeTests`의 것이 같은 네임스페이스라 이미 커버).
- 검증: `TryLoadGame`→클리어 상태 확인→`LoadShopScene`→`Panel_Ending` 활성+"데모 클리어!"→
  `CanAdvancePhase` false(사유 정확 일치)+`AdvancePhase` phase 유지→`LoadMainMenuScene`→MainMenu
  `ContinueButton` 비활성+`SaveStatusText` "데모 클리어!·영업 7일 달성"+Brass Amber.

**통합 검증 중 발견·수정한 회귀 (G1/G2 스코프 밖, 밸런스 시드 변경의 실제 collateral):**
- `GenrePersistencePlayModeTests.Day1_To_3_No_Ui_Advances_Through_EventFree_Day2_Into_Surge_Active_Day3`
  가 15,000 시드에서 실패했다(Day1~2 전부포기로 cash=0 소진 후 Day3 구매 실패 — "자금이 부족합니다
  (필요 1,397원, 보유 0원)"). 이 fixture 의 목적은 이벤트 예고/폭등 메커니즘 검증이지 밸런스 압박
  검증이 아니므로, Day3 구매 직전 `gm.State.cash += 5000` 소액 보충으로 최소 수정했다(리터럴 재작성
  아님 — fixture 의도는 그대로 보존).

**BuildWindows 배치 게이트:**
- 최초(target 미보유) 빌드 — 소요 **≈2분 31초**(19:13:03 시작 → 19:15:34 exit 0, `-batchmode -quit
  -buildTarget StandaloneWindows64 -executeMethod BuildTool.BuildWindows`, `-nographics` 미사용).
- 산출: `game/Build/Windows/ClientIsKing.exe`(런처 스텁 667,648 bytes) + `ClientIsKing_Data/` 등
  — `BuildTool` 로그 기준 총 산출물 크기 **102,490,858 bytes(≈97.7MB)**.
- `git check-ignore game/Build/Windows/ClientIsKing.exe` 성공(`.gitignore:38` 기존 커버, 변경 없음).
- 부가 관찰: 최초 StandaloneWindows64 타깃 노출로 `ProjectSettings.asset`(m_BuildTargetBatching
  Standalone 엔트리 자동 추가, companyName/productName 무변경 확인)·`GraphicsSettings.asset`·
  `UniversalRenderPipelineGlobalSettings.asset`·`URP-Pipeline.asset` 에 Unity 자동 생성 diff 발생
  (URP 셰이더 스트리핑/런타임 설정 목록 채움 — 수동 편집 아님, 빌드 파이프라인 1회 실행의 정상 부작용).
  design.md E2 가 예견한 "최초 1회 재임포트" 항목과 일치.

**MainMenu.unity/Shop.unity 씬 재직렬화 노이즈** — G1/G2/G3 세 단계 모두 EditMode 배치 실행이 씬을
열고(`SceneBuilder`/테스트 fixture 경유) `SaveScene`을 호출하는 기존 동작 때문에 git status 에 계속
나타난다(삽입/삭제 대칭, 내용 손상 아님) — G1 단계부터 반복 관찰, 코드 저작 변경은 없음.

## 검증 결과 (G3 게이트)

| 게이트 | 결과 |
|--------|------|
| EditMode (`-quit` 없이) | **486/486 green** (G1 기준선 439 + G2 신규 16 + G1 자체 EndingOps/GameManagerEndingGate/BalanceEndingGuard 31 → 470, G2 재확인 486, G3 재확인 486 무회귀) |
| PlayMode (`-quit` 없이) | **9/9 green** (기존 8 + 신규 `EndingPlayModeTests` 1 — 첫 시도 실패 2건 모두 수정 후 green) |
| BuildWindows (`-batchmode -quit`) | **exit 0**, `ClientIsKing.exe` 생성 확인, 소요 ≈2분31초, 산출물 총 ≈97.7MB |
| git 청정 | Build 산출물 gitignore 확인, ProjectSettings/URP diff 는 빌드 파이프라인 1회성 부작용(위 기재), 씬 재직렬화 노이즈(내용 손상 아님) |

## 미결 (오너/Codex 게이트 — done 게이트 조건, 자동 검증과 별도)

- 오너 플레이테스트 — 밸런스 시드 15,000 체감 확인(N=7 데모 완주).
- 640×360 시각 승인 3종(장르 modal/Night/무대 등 기존 + 엔딩 오버레이 클리어/게임오버 신규) — Claude
  self-approve 금지.
- 빌드 exe 실행 스모크(오너 더블클릭) — 새 게임→7일→클리어/파산→이어하기 전체 플레이.
- Codex 코드리뷰 — G1/G2/G3 전체 diff.
