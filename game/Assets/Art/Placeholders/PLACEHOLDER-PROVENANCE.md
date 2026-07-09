# Placeholder Art Provenance (task-108)

모든 스프라이트는 **프로젝트 생성형 플레이스홀더**다 — `PlaceholderArtBuilder`(에디터 스크립트)가
결정론적 픽셀 패턴으로 생성한 자체 산출물이며, 외부 에셋을 포함하지 않는다 (CC0 상당 — 저작권 주장 없음).
재생성: `Unity.exe -batchmode -quit -nographics -projectPath game -executeMethod ClientIsKing.EditorTools.SceneBuilder.Apply`
(SceneBuilder.Apply 가 PlaceholderArtBuilder.Apply 를 선행 호출). task-113 아트 마감 패스에서 교체 예정.

## Customers/ — 고객 archetype 4종 (16×24px, PPU 32, Point, 무압축)

| 파일 | 생성 방식 |
|------|-----------|
| Customers/student.png | PlaceholderArtBuilder 코드 생성 (초록 상의 픽셀 인물) |
| Customers/office_worker.png | PlaceholderArtBuilder 코드 생성 (파랑 정장 픽셀 인물) |
| Customers/family_parent.png | PlaceholderArtBuilder 코드 생성 (주황 상의 픽셀 인물) |
| Customers/senior_regular.png | PlaceholderArtBuilder 코드 생성 (보라 상의·회색 머리 픽셀 인물) |

## FoodIcons/ — 레시피 6종 (16×16px, PPU 32, Point, 무압축)

| 파일 | 생성 방식 |
|------|-----------|
| FoodIcons/pork_gukbap.png | PlaceholderArtBuilder 코드 생성 (뽀얀 갈색 국그릇) |
| FoodIcons/beef_gukbap.png | PlaceholderArtBuilder 코드 생성 (진갈색 국그릇) |
| FoodIcons/tteokbokki.png | PlaceholderArtBuilder 코드 생성 (빨간 접시) |
| FoodIcons/gimbap.png | PlaceholderArtBuilder 코드 생성 (검정 김말이 접시) |
| FoodIcons/janchi_guksu.png | PlaceholderArtBuilder 코드 생성 (크림색 국수 그릇) |
| FoodIcons/bibim_guksu.png | PlaceholderArtBuilder 코드 생성 (주황 비빔 그릇) |

## 폰트

- `Assets/Art/Fonts/Galmuri11.ttf` — 외부 OFL 폰트, 출처/라이선스는 `Assets/Art/Fonts/Galmuri-LICENSE.txt` (OFL-1.1, quiple/galmuri).
