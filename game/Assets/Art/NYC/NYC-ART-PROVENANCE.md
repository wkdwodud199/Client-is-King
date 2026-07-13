# NYC 코리아타운 런타임 아트 — Provenance (task-116 배치 1)

> **Status**: U2 수입 완료(기계 검증 통과). **최종 640×360 오너/Codex 시각 승인 대기 중** —
> Claude self-approve 금지(design.md H절). 이 문서는 `game/Assets/Art/NYC/**` 32개 런타임
> 스프라이트의 생성 계보·규격·후처리·승인 상태를 파일별로 고정한다(design.md C절 형식).

## 승인·역할

- **방향 승인**: Project Owner, 2026-07-12 — Codex Visual North Star + 파생 콘셉트 6종을
  최종 아트 방향(현대 뉴욕 코리아타운 푸드트럭)으로 승인(원본: `kb/concepts/art-originals/`).
- **생산**: 오너 / Codex 이미지 생성 워크플로우(A안 — 타깃 해상도 재생산), 2026-07-13.
- **기계 검증 + 수입**: Claude — 경로 집합·PNG IHDR/CRC·8-bit RGBA type 6·규격·이진 알파·
  투명 모서리·색 상한·캐릭터 baseline·SHA-256 대조를 독립 재검증한 뒤 무수정 수입. Claude는
  아트를 생성·리터칭·리사이즈·크롭·재디자인하지 않았고 Unity 임포트 설정만 적용했다.
- **최종 시각 판정**: 오너/Codex(대기 중). Claude는 640×360 캡처만 제출하며 톤·식별성을 판정하지 않는다.

## 생성 도구·계보

- **생성 도구**: OpenAI 내장 이미지 생성(Codex 워크플로우). 별도 API 키·유료 서드파티 도구 미사용.
  정확한 백엔드 모델 식별자는 도구에서 노출되지 않아 **추정하여 표기하지 않는다**.
- **참조 콘셉트(계보)**: `kb/concepts/art-originals/`의 오너 승인 6종(바이트 불변 — PROVENANCE.md md5 핀).
  콘셉트는 레퍼런스이며 축소·크롭으로 런타임 자산을 만들지 않았다.

## 후처리 내역 (생산 측)

1. 자산/프레임마다 개별 생성 프롬프트로 32-logical-pixel 확대 픽셀 아트를 생성했다.
2. 생성 raw 프리뷰(단색 크로마 배경)는 `source/raw/`에만 남기고 런타임에 반입하지 않았다.
3. raw 크로마에 미세한 톤 편차가 있어, raw를 실루엣/시각 소스로만 쓰고 **타깃 캔버스에서 새로
   저작**했다(32×32 / 640×160 / 320×32, nearest logical-pixel, 경계 정리, 발 기준선 고정,
   안티에일리어싱 없음). 최종 파일은 **신규 이진 알파 마스크**를 받았으며 raw 크로마 픽셀은
   런타임 패키지에 포함되지 않는다(F3 exact-match 키잉 산출물 아님 — 배경색 키잉 후처리 없음).
4. 캐릭터 ≤24색 / 음식 ≤32색 / 장르 심볼 ≤12색 / 무대 ≤64색 팔레트. 디더링·반투명 픽셀 없음.
5. 전 런타임 PNG는 8-bit truecolor RGBA(PNG IHDR color type 6). `Stage/backdrop.png`는 설계상 완전 불투명.
6. **Claude 후처리 0건** — 픽셀 무수정. Unity 임포트 표준(Sprite·PPU32·Point·무압축·mipmap off·
   alphaIsTransparency)만 적용했다.

## 라이선스·권리 (CC0 아님)

코드는 MIT를 적용하되 **프로젝트 고유 아트는 별도 라이선스**로 둔다. `kb/concepts/art-originals/**`와
`game/Assets/Art/NYC/**`는 리포 MIT 적용에서 **제외**한다. 재사용·재배포·2차 저작물 작성 허가를
부여하지 않는다. AI 요소의 저작권 성립·보호 범위는 관할권별로 다를 수 있으며 독점성·고유성·비침해성은
보증하지 않는다. **CC0로 표기하지 않는다.** CC0 계보(`PLACEHOLDER-PROVENANCE.md`)와는 파일·디렉터리
수준에서 분리한다.

## 파일별 기록 (32)

규격·생성일·참조 콘셉트·후처리·승인. 생성 도구 = OpenAI 내장 이미지 생성(Codex), 생성일 = 2026-07-13,
후처리 = 타깃 캔버스 재저작 + 신규 이진 알파 마스크(Claude 픽셀 무수정), 승인 = 방향 2026-07-12 /
최종 시각 게이트 대기(오너).

### 손님 (Customers/ — 32×32 RGBA, 참조 `customers.png`)

| 파일 | 규격 | 정체성(참조 콘셉트) |
|------|------|----------------------|
| `Customers/student.png` | 32×32 | 백팩+회색 후디 청년 (idle) |
| `Customers/student_walk0.png` | 32×32 | 〃 걷기 0 |
| `Customers/student_walk1.png` | 32×32 | 〃 걷기 1 |
| `Customers/student_walk2.png` | 32×32 | 〃 걷기 2 |
| `Customers/student_walk3.png` | 32×32 | 〃 걷기 3 |
| `Customers/office_worker.png` | 32×32 | 브라운 코트+토트백 직장인 (idle) |
| `Customers/office_worker_walk0.png` | 32×32 | 〃 걷기 0 |
| `Customers/office_worker_walk1.png` | 32×32 | 〃 걷기 1 |
| `Customers/office_worker_walk2.png` | 32×32 | 〃 걷기 2 |
| `Customers/office_worker_walk3.png` | 32×32 | 〃 걷기 3 |
| `Customers/family_parent.png` | 32×32 | 그린 패딩 부모 + 옐로 패딩 아이 페어 (idle) |
| `Customers/family_parent_walk0.png` | 32×32 | 〃 걷기 0 |
| `Customers/family_parent_walk1.png` | 32×32 | 〃 걷기 1 |
| `Customers/family_parent_walk2.png` | 32×32 | 〃 걷기 2 |
| `Customers/family_parent_walk3.png` | 32×32 | 〃 걷기 3 |
| `Customers/senior_regular.png` | 32×32 | 헌팅캡+브라운 재킷 노신사 (idle) |
| `Customers/senior_regular_walk0.png` | 32×32 | 〃 걷기 0 |
| `Customers/senior_regular_walk1.png` | 32×32 | 〃 걷기 1 |
| `Customers/senior_regular_walk2.png` | 32×32 | 〃 걷기 2 |
| `Customers/senior_regular_walk3.png` | 32×32 | 〃 걷기 3 |

### 음식 아이콘 (FoodIcons/ — 32×32 RGBA, 참조 `food.png`)

| 파일 | 규격 | 정체성 |
|------|------|--------|
| `FoodIcons/pork_gukbap.png` | 32×32 | 뽀얀 돼지국밥 |
| `FoodIcons/beef_gukbap.png` | 32×32 | 얼큰 소고기국밥 |
| `FoodIcons/janchi_guksu.png` | 32×32 | 맑은 잔치국수 |
| `FoodIcons/bibim_guksu.png` | 32×32 | 비빔국수 |
| `FoodIcons/tteokbokki.png` | 32×32 | 떡볶이 |
| `FoodIcons/gimbap.png` | 32×32 | 김밥 |

### 장르 UI 아이콘 (UiIcons/ — 32×32 RGBA, 참조 `ui-icons.png`, 버튼 프레임 없음)

| 파일 | 규격 | 정체성(장르 매핑) |
|------|------|--------------------|
| `UiIcons/genre_gukbap.png` | 32×32 | 국그릇 = `gukbap` |
| `UiIcons/genre_bunsik.png` | 32×32 | 꼬치 = `bunsik` |
| `UiIcons/genre_noodles.png` | 32×32 | 면기 = `noodles` |
| `UiIcons/genre_generalist.png` | 32×32 | 도시락 = `generalist` |

### 무대 (Stage/ — 참조 `foodtruck-environment.png`, 단판)

| 파일 | 규격 | 정체성 | 후처리 비고 |
|------|------|--------|-------------|
| `Stage/backdrop.png` | 640×160 | 야간 벽돌 거리 + 트럭 단판 합성 | 완전 불투명(설계 예외) |
| `Stage/counter.png` | 320×32 | 트럭 서빙 카운터 띠 | 이진 알파 |

## 공개문 (README·AI-ART-NOTICE.txt와 동일 원천)

**[KO]** Client is King의 NYC 코리아타운 배경, 캐릭터, 음식 및 UI 아이콘 일부는 프로젝트 오너가
승인한 비주얼 콘셉트를 바탕으로 OpenAI의 Codex 내 이미지 생성 도구를 사용해 사전 생성하고, 프로젝트
팀이 방향을 정하고 선택·검수·통합한 AI 보조 아트입니다. 정확한 백엔드 모델 식별자는 도구에서
노출되지 않아 추정하여 표기하지 않습니다. 게임 실행 중에는 생성형 AI 또는 외부 AI 서비스를 사용하지
않습니다.

**[EN]** Some NYC Koreatown backgrounds, characters, food art, and UI icons in Client is King are
pre-generated, AI-assisted artwork created with OpenAI image generation through Codex from the
owner-approved visual concepts, then directed, selected, reviewed, and integrated by the project
team. The exact backend model identifier is not exposed by the tool and is therefore not guessed.
No generative AI or external AI service runs while the game is being played.

## 패키지 경계·검증 근거

- 런타임 반입: 스테이징 `handoff/NYC/**`의 32파일만. `review/**`(contact sheet·GIF)·`source/raw/**`
  (생성 프리뷰)는 Unity에 수입하지 않았다.
- 오너 preflight(전수 PASS, 0 error/warning) + Claude 독립 재검증(32파일 규격·IHDR6·이진 알파·
  투명 모서리·색 상한·아키타입별 발 baseline y30 일치) + SHA-256 32/32 일치로 수입 게이트를 통과했다.
- 검증 자료 원천(스테이징): `control/validation-report.txt`·`asset-manifest.csv`·`SHA256SUMS.txt`·
  `provenance-input.md`.
