# 콘셉트 아트 원본 — Codex Visual North Star (아카이브 전용)

> **Status**: 아카이브 (런타임 미연결). 오너 승인 최종 아트 방향의 콘셉트 **원본 보존본**이다.
> 런타임 적용(최종 경로·Asset Map·슬라이싱·픽셀 규격·AI 아트 정책·provenance 형식)은
> **task-116 (Codex 설계)** 에서 확정하며, 그 계약이 ready 되기 전엔 이 파일들을 런타임에 연결하지 않는다.

## 오너 결정 (2026-07-12)

Codex 가 생성한 **Visual North Star + 파생 콘셉트 6종**의 비주얼 방향을 Client is King 의
**최종 아트 방향 = 현대 뉴욕 코리아타운(푸드트럭)** 으로 승인했다.

**제약 (엄수)**:
- 현재 파일은 **콘셉트 시트**(RGB·**알파 없음**·1536~1672px 대형) — 자동 축소·임의 크롭으로
  런타임 스프라이트로 쓰지 않는다. 캐릭터 외형·아이콘 모양·색·레이아웃을 임의 재디자인하지 않는다.
- **task-114 에 끼워 넣지 않는다** — task-114 설계는 CC0/OFL 플레이스홀더만 허용하고 NYC AI 아트
  오버홀을 제외한다(완료·커밋됨). 현재 데모 기준선·테스트를 보존한다.
- **task-116 이 확정할 것**: AI 아트 사용 정책 + provenance / 최종 파일 경로 + Asset Map /
  캐릭터·음식·UI 아이콘별 픽셀 규격 / 투명 배경·스프라이트 프레임·레이어 분리 규칙 /
  `PlaceholderArtBuilder` 유지·교체 범위 / Unity 임포트 + 회귀 테스트 기준.
- task-116 design.md 가 ready 되면 **그 계약만** 구현한다. 구현 중 디자인 판단이 필요하면 추측하지
  않고 Codex 검수로 돌린다.

## 파일 (`kb/concepts/art-originals/` — `game/Assets` 밖, Unity 미임포트)

| 파일 | 라벨 | 규격 | 크기 | md5 (바이트 불변 핀) | 원본 (Codex 캐시 exec-id) |
|------|------|------|------|----------------------|---------------------------|
| `visual-north-star.png` | Visual North Star (합성 시트) | 1672×941 RGB | 1,976,738 B | `1690a993f1bf6f466d7195217255c66b` | `exec-0a6fe4ec-aefb-4c53-ad4d-24d1a6db13d1` |
| `protagonist.png` | 주인공 (턴어라운드·표정·액션) | 1536×1024 RGB | 1,963,120 B | `8ffb0cfa6a947f17babb54bfb1e7fc02` | `exec-0789662e-a39a-47e2-8ff3-9cbf98b0d08b` |
| `customers.png` | 손님 4종 (걷기·카운터·표정) | 1672×941 RGB | 1,855,895 B | `c4201012c3d8dd1285759fe5ceb4d864` | `exec-7ee53b4a-55b0-4705-a183-54c813c33cc5` |
| `food.png` | 음식 6종 (+소형) | 1536×1024 RGB | 1,930,775 B | `b16ecb4cfcee1ab78d95f85eb6cedd5d` | `exec-1b1439ea-a436-4240-b7d5-b398f13fa618` |
| `ui-icons.png` | UI 아이콘 (음식+시스템) | 1672×941 RGB | 1,438,672 B | `f3c409d40b442cb40e38f52d077256b7` | `exec-0c7a5160-1faa-4c42-af4e-da0fe72f2e36` |
| `foodtruck-environment.png` | 푸드트럭 환경 (씬+타일셋) | 1672×941 RGB | 1,853,461 B | `d4167d3aa43d9fce25f6a770fc59b4e3` | `exec-09ecc2a0-66aa-4f6b-acf1-7e5f8fe7a72a` |

> **md5 핀**: 위 6개 md5 는 콘셉트 원본의 바이트 불변 계약이다 — task-116 `NycArtTests` H7 이 이 값과
> 실제 파일 해시의 일치를 검증한다(콘셉트 재디자인·손상 방지). 원본 변경은 이 표 갱신을 동반해야 한다.

## 백업 근거

- **원본 소스** (transient — 소실 위험): `C:\Users\wkdwo\.codex\generated_images\019f4b99-7b81-7c42-a89c-6cd9c524cc12\`
  (codex 생성 이미지 캐시, 재실행·정리 시 사라질 수 있음).
- **백업 방식**: 비파괴 **바이트 동일 복사**(6종 전부 md5 일치 확인), 2026-07-12. 이미지 자체는
  일절 변형하지 않았다(리사이즈·크롭·색보정·알파 처리 없음). 파일명만 라벨로 리네임(위 표가 exec-id 매핑).
- 이 디렉터리는 `game/Assets` 밖이라 Unity 가 자동 임포트하지 않는다 — 런타임에 영향 0.

## 출처 표기

Codex(gpt-image, OpenAI) 로 생성된 **AI 아트**다. 공개 사용·크레딧·라이선스 표기의 최종 정책은
task-116 이 확정한다(그전까지 아카이브 보존만).
