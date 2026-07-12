# NYC 스프라이트 생산 규격서 — task-116 배치 1 (입력 게이트)

> **누가**: 오너/Codex(이미지 생성 또는 작가) — A안(타깃 해상도 재생산) 확정.
> **무엇을**: 아래 **32개 PNG**를 타깃 해상도에서 직접 생산해 전달한다.
> **참조**: `kb/concepts/art-originals/`(승인 콘셉트 6종)를 **비주얼 레퍼런스**로만 쓴다 —
> 콘셉트를 축소·크롭하지 말고, 같은 정체성을 **타깃 해상도에서 새로 그린다**(픽셀 아트는 목표
> 해상도에서 저작해야 성립). 캐릭터 외형·아이콘·색·구도는 **승인 콘셉트를 재현**하되 재디자인 금지.
> 전달되면 Claude가 검증(기계)→수입→씬 배선→테스트를 구현한다. 불합격 파일은 반려 목록으로 회신한다(수정 생성 안 함).

## 공통 규칙 (전 파일)

- **포맷**: PNG · **RGBA** · 실제 투명 배경(콘셉트의 크림색 배경 금지).
- **알파**: **이진** — 모든 픽셀 alpha ∈ {0, 255}(반투명 금지). 예외: `Stage/backdrop.png`는 완전 불투명 허용.
  - 생산 도구가 알파를 못 내면: **단일 평면 배경색**으로 출력하고 그 배경색 hex를 함께 알려달라(Claude가 exact-match 키잉으로 alpha 0 처리 — 그 색만, 나머지 무변경). 경계 헤일로가 남으면 시각 게이트에서 반려.
- **픽셀 표준**: PPU 32 · **Point 필터 전제**(경계 선명·안티에일리어싱 헤일로 금지) · 무압축 · mipmap 없음. 즉 각 픽셀이 또렷한 도트여야 한다.
- **정체성 고정**: 아래 "정체성" 열은 승인 콘셉트에서 온 **고정 사양**이다. 그대로 재현한다.
- **전달 형식**: **프레임당 개별 PNG 1파일**(스프라이트 시트/스트립 아님 — 슬라이싱 리스크 0). 파일명 정확히 일치.
- **불투명 고유색 상한**(기계 검증): 손님 ≤24 · 음식 ≤32 · 장르 아이콘 ≤12 · backdrop/counter ≤64.
- **원본 PNG 진짜 RGBA**(기계 검증): 납품 PNG 는 원본 바이트 기준 **IHDR color type == 6**(truecolor+alpha)이어야 한다 — Unity 변환 Texture2D 만이 아니라 raw PNG 헤더를 확인한다. (RGB 납품 + 키잉 폴백을 택한 파일만 예외로, provenance 에 배경색 기록.)

## 1) 손님 — 20파일 · 각 32×32 RGBA

4 아키타입 × (idle 1 + 걷기 4프레임). **우향 기준**(좌향은 런타임이 좌우 반전 — 백팩·토트백 등 비대칭 디테일이 반전돼 보이는 점 유의). 프레임 간 **발 기준선·캔버스 크기 고정**(점프 방지). 발밑 그림자는 파일에 포함 가능(배경 요소는 금지).

| 파일 (Customers/) | 정체성 (customers.png 참조) |
|---|---|
| `student.png`, `student_walk0..3.png` | 백팩 + 회색 후디 청년 |
| `office_worker.png`, `office_worker_walk0..3.png` | 브라운 코트 + 토트백 직장인 |
| `family_parent.png`, `family_parent_walk0..3.png` | 그린 패딩 부모 + 옐로 패딩 아이 — **부모+아이 페어 우선**(승인 콘셉트대로). 1× 크기에서 식별 불가할 때만 오너/Codex 재승인 후 부모 단독. 캔버스 32×32 고정 |
| `senior_regular.png`, `senior_regular_walk0..3.png` | 헌팅캡 + 브라운 재킷 노신사 |

## 2) 음식 아이콘 — 6파일 · 각 32×32 RGBA

투명 배경 위 중앙 배치. `food.png`의 대형 6종 재현(소형 변형은 배치 1 미사용).

| 파일 (FoodIcons/) | 정체성 |
|---|---|
| `pork_gukbap.png` | 뽀얀 돼지국밥 |
| `beef_gukbap.png` | 얼큰 소고기국밥 |
| `janchi_guksu.png` | 맑은 잔치국수 |
| `bibim_guksu.png` | 비빔국수 |
| `tteokbokki.png` | 떡볶이(빨간 양념 + 흰 떡) |
| `gimbap.png` | 김밥 |

## 3) 장르 UI 아이콘 — 4파일 · 각 32×32 RGBA

`ui-icons.png`의 장르 4종. **버튼 프레임 없이 투명 배경 심볼만**(콘셉트의 프레임 미포함 — 프레임은 SceneBuilder UI가 담당). 매핑 **확정**(오너 2026-07-12): 국그릇=gukbap·꼬치=bunsik·면기=noodles·도시락=generalist. **파일명은 실제 도메인 장르 id 사용** — 면류는 `genre_noodles.png`(단수 `noodle` 아님).

| 파일 (UiIcons/) | 정체성 |
|---|---|
| `genre_gukbap.png` | 국그릇(국밥) |
| `genre_bunsik.png` | 꼬치(분식) |
| `genre_noodles.png` | 면기(면류) |
| `genre_generalist.png` | 도시락(제네럴리스트) |

## 4) 무대 — 2파일

`foodtruck-environment.png` 참조. 단판 1장(타일 반복 아님).

| 파일 (Stage/) | 규격 | 정체성 |
|---|---|---|
| `backdrop.png` | **640×160** RGBA(불투명 허용) | 야간 벽돌 거리 + 트럭 합성 전경 |
| `counter.png` | **320×32** RGBA | 트럭 서빙 카운터 띠 |

## 생산 순서 — 파일럿 9파일 먼저 (오너 확정)

전체 32파일을 바로 생산하지 않는다. 아래 **9파일을 파일럿으로 먼저** 생산·검증한다:

- `student.png` + `student_walk0..3.png` (5)
- `pork_gukbap.png` · `genre_gukbap.png` · `backdrop.png` · `counter.png` (4)

오너/Codex 가 **1× · 4× contact sheet + 걷기 애니메이션 + 좌우 반전**을 승인한 뒤 나머지 23파일을 생산한다.
**파일럿은 U2 부분 납품이 아니다** — 최종 32파일이 전부 기계 검증을 통과한 뒤에만 U2(수입)를 시작한다.

## 전달 후 절차

1. Claude가 `NycArtTests`(존재·규격·**IHDR color type 6**·알파·색상한·임포트 표준·provenance)로 **기계 검증** → 불합격분은 **수정 없이 파일별 반려 사유만 보고**(Claude 아트 수정 금지).
2. 합격분을 `game/Assets/Art/NYC/`에 수입(+`.meta`) + `NYC-ART-PROVENANCE.md` 작성. **U2 자산 수입 커밋**.
3. `SceneBuilder` 스프라이트 소스 경로 전환 — **U3 별도 커밋**(롤백 지점) + 씬 재생성.
4. 640×360 시각 승인(오너/Codex — **Claude self-approve 금지**) → 반려 시 재생산 요청.

## AI 아트 공개·라이선스 정책 (오너 확정 2026-07-12)

리포가 **public**이라 스프라이트를 커밋하면 즉시 게시된다. 정책 **확정**(전체 문구는 design.md C절):
- 라이선스: 코드 MIT / NYC·art-originals **아트는 MIT 제외·별도 라이선스**. 재사용·재배포·2차 저작물 미허가. **CC0 표기 금지**.
- U2 커밋 시 함께 생성할 공개문 위치: `README.md`+`README.en.md`(요약+MIT 제외 범위) · `game/Assets/Art/NYC/NYC-ART-PROVENANCE.md`(전체 문구+파일별 생성/후처리/승인) · `game/Assets/StreamingAssets/AI-ART-NOTICE.txt`(빌드 포함 KO/EN). Steam Content Survey=Pre-Generated AI 신고. 인게임 Credits UI=별도 release task(공개 출시 전 필수).
