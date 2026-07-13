## Notice Review (README/AI-ART-NOTICE) — 2026-07-13
- Reviewer: Codex
- Review status: request-changes
- Feedback:
  1. [approved] AI 공개문 KO/EN 자체는 task-116 C절 확정문과 문구가 일치한다. `README.md:168-170`은 `kb/tasks/task-116/design.md:91`의 KO 공개문과 동일하고, `README.en.md:168-170`은 `kb/tasks/task-116/design.md:92`의 EN 공개문과 동일하다. `game/Assets/StreamingAssets/AI-ART-NOTICE.txt:4-9`, `game/Assets/StreamingAssets/AI-ART-NOTICE.txt:14-19`도 개행만 다를 뿐 같은 공개문이다.
  2. [request-changes] `game/Assets/Art/NYC/**` 경로 표기가 현재 워크트리와 맞지 않는다. `README.md:163`, `README.en.md:163`은 프로젝트 고유 아트 경로로 `game/Assets/Art/NYC/**`를 고지하지만, 실제 `game/Assets/Art/NYC` 디렉터리는 존재하지 않는다. 코드 계약은 `game/Assets/Scripts/Editor/NycArtContract.cs:23-28`에서 Unity 내부 경로 `Assets/Art/NYC`와 `NYC-ART-PROVENANCE.md`를 예정하고 있으나, 현재 공개 리포 기준의 실제 고유 런타임 아트 경로로는 성립하지 않는다. 반면 `kb/concepts/art-originals/**`는 존재한다.
  3. [request-changes] `AI-ART-NOTICE.txt`의 라이선스 요약이 확정 정책의 법적 caveat를 누락한다. task-116 C절은 AI 요소의 저작권 성립·보호 범위가 관할권별로 다를 수 있고 독점성·고유성·비침해성을 보증하지 않는다고 정했다(`kb/tasks/task-116/design.md:86`). README 양쪽은 이를 반영한다(`README.md:163`, `README.en.md:163`). 그러나 `game/Assets/StreamingAssets/AI-ART-NOTICE.txt:11-12`, `game/Assets/StreamingAssets/AI-ART-NOTICE.txt:21-22`는 MIT 제외·CC0 아님·재사용 미허가만 요약하고 관할권 상이·비보증 문구를 빠뜨린다.
  4. [request-changes] `StreamingAssets/Licenses/`의 "full license texts" 참조가 현재 번들 구성보다 넓게 읽힌다. `README.md:166`, `README.en.md:166`, `game/Assets/StreamingAssets/AI-ART-NOTICE.txt:24`는 라이선스 전문이 `StreamingAssets/Licenses/`에 있다고 안내한다. 실제 폴더에는 MIT `LICENSE.txt`, `Galmuri-LICENSE.txt`, `THIRD-PARTY-NOTICES.md`가 있으나, 플레이스홀더 CC0 팩의 라이선스 근거는 `game/Assets/Art/Placeholders/PLACEHOLDER-PROVENANCE.md:18-25` 및 각 `OpenSource/**` 파일에 남아 있고 `StreamingAssets/Licenses/`에는 동봉되어 있지 않다. 공개 문구를 좁히거나 CC0 관련 전문/근거를 번들에 추가해야 한다.
  5. [approved] 코드 MIT 표기는 `LICENSE:1-3`의 MIT 및 `© 2026 P0t4t0`와 맞다(`README.md:162`, `README.en.md:162`). 폰트 OFL 1.1 표기도 `game/Assets/StreamingAssets/Licenses/Galmuri-LICENSE.txt:1-7` 및 `README.md:165`, `README.en.md:165`와 맞다. 플레이스홀더 CC0 표기도 `game/Assets/Art/Placeholders/PLACEHOLDER-PROVENANCE.md:18-25`와 맞다.
  6. [approved] THIRD-PARTY-NOTICES 및 Unity 상표 고지는 실제 파일과 맞다. 루트와 번들 양쪽의 `THIRD-PARTY-NOTICES.md:3-8`은 Unity 6 사용, Unity 상표, 비제휴·비후원·비보증 고지를 포함한다.
  7. [approved] README KO/EN은 라이선스 절 기준으로 상호 정합하다. `README.md:160-166`과 `README.en.md:160-166`은 코드 MIT, 프로젝트 고유 아트 MIT 제외, 재사용·재배포·2차 저작물 미허가, 관할권 상이·비보증, Not CC0, 플레이스홀더 CC0, Galmuri11 OFL, 서드파티 고지를 같은 범위로 고지한다. 저작권 성립 보증 같은 위험한 단정은 보이지 않는다.
  8. [approved with caveat] 정책 밖 신규 법적 단정은 README에는 보이지 않는다. `AI-ART-NOTICE.txt`의 별도 라이선스 요약도 확정 정책을 요약하려는 범위이나, 위 3번처럼 핵심 caveat가 빠져 현재 상태로는 축약이 불완전하다.
- Action required:
  1. `game/Assets/Art/NYC/**` 실제 자산/프로비넌스가 아직 없다면 공개 고지에서 현재 존재 경로처럼 쓰지 말고 "예정 경로"로 조정하거나, 공개 전 해당 디렉터리와 `NYC-ART-PROVENANCE.md`를 실제로 추가한다.
  2. `AI-ART-NOTICE.txt` KO/EN 라이선스 요약에 관할권별 저작권 성립·보호 범위 상이 및 독점성·고유성·비침해성 비보증 문구를 추가한다.
  3. `StreamingAssets/Licenses/` 안내를 현재 포함된 전문 범위로 좁히거나, 플레이스홀더 CC0 팩 관련 라이선스 근거/전문도 번들에 포함한다.
