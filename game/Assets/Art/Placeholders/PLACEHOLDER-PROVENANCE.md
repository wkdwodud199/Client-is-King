# Placeholder Art Provenance (task-108)

모든 스프라이트는 **프로젝트 생성형 플레이스홀더**다 — `PlaceholderArtBuilder`(에디터 스크립트)가
결정론적 픽셀 패턴으로 생성한 자체 산출물이며, 외부 에셋을 포함하지 않는다 (CC0 상당 — 저작권 주장 없음).
재생성: `Unity.exe -batchmode -quit -nographics -projectPath game -executeMethod ClientIsKing.EditorTools.SceneBuilder.Apply`
(SceneBuilder.Apply 가 PlaceholderArtBuilder.Apply 를 선행 호출). task-113 아트 마감 패스에서 교체 예정.

## Customers/ — 고객 archetype 4종 (24×32px v2 — 문자열 픽셀맵, PPU 32, Point, 무압축)

공용 인체 맵(아웃라인·3톤 셰이딩) + archetype 팔레트/액세서리 오버레이 방식.

| 파일 | 생성 방식 |
|------|-----------|
| Customers/student.png | PlaceholderArtBuilder 픽셀맵 (초록 후드+캡 학생) |
| Customers/office_worker.png | PlaceholderArtBuilder 픽셀맵 (파랑 정장+빨간 넥타이 직장인) |
| Customers/family_parent.png | PlaceholderArtBuilder 픽셀맵 (주황 상의+크림 앞치마 가족) |
| Customers/senior_regular.png | PlaceholderArtBuilder 픽셀맵 (보라 조끼+회머리+안경 어르신) |

## FoodIcons/ — 레시피 6종 (20×16px v2 — 문자열 픽셀맵, PPU 32, Point, 무압축)

국그릇(림 하이라이트·김·고명)/접시(단면·소스) 맵 + 내용물 팔레트.

| 파일 | 생성 방식 |
|------|-----------|
| FoodIcons/pork_gukbap.png | PlaceholderArtBuilder 픽셀맵 (뽀얀 국물+파·고기 고명) |
| FoodIcons/beef_gukbap.png | PlaceholderArtBuilder 픽셀맵 (진한 국물+고기 고명) |
| FoodIcons/tteokbokki.png | PlaceholderArtBuilder 픽셀맵 (접시+빨간 소스+흰 떡) |
| FoodIcons/gimbap.png | PlaceholderArtBuilder 픽셀맵 (접시+김 단면·밥심·속재료) |
| FoodIcons/janchi_guksu.png | PlaceholderArtBuilder 픽셀맵 (크림 면+지단·파 고명) |
| FoodIcons/bibim_guksu.png | PlaceholderArtBuilder 픽셀맵 (양념 면+야채 고명) |

## 폰트

- `Assets/Art/Fonts/Galmuri11.ttf` — 외부 OFL 폰트, 출처/라이선스는 `Assets/Art/Fonts/Galmuri-LICENSE.txt` (OFL-1.1, quiple/galmuri).
