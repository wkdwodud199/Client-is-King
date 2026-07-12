namespace ClientIsKing.UI
{
    /// <summary>
    /// task-117 (B절): 크레딧 표시 문구의 단일 C# 원천 — SceneBuilder 가 빌드 타임에 씬에 굽고,
    /// 원 문서와의 정합은 EditMode 동기화 테스트(CreditsPanelSceneTests)가 고정한다.
    /// AI 공개문·라이선스 문장은 오너 확정본 verbatim 이며 임의 수정 금지(법적 문구 = 오너 게이트).
    /// 제작 저작자명(P0t4t0)·엔진 표기(THIRD-PARTY-NOTICES 방식)는 오너 확정(2026-07-13).
    /// </summary>
    public static class CreditsCopy
    {
        /// <summary>
        /// AI 보조 아트 공개문 [KO] — task-116 design.md C절 확정문 verbatim (한 글자도 바꾸지 않는다,
        /// 개행 미삽입 — 줄바꿈은 TMP 자동 wrap 소유). 동기화 테스트가 원문 부분 문자열 일치를 검증한다.
        /// </summary>
        public const string AiArtNoticeKo =
            "Client is King의 NYC 코리아타운 배경, 캐릭터, 음식 및 UI 아이콘 일부는 프로젝트 오너가 승인한 비주얼 콘셉트를 바탕으로 OpenAI의 Codex 내 이미지 생성 도구를 사용해 사전 생성하고, 프로젝트 팀이 방향을 정하고 선택·검수·통합한 AI 보조 아트입니다. 정확한 백엔드 모델 식별자는 도구에서 노출되지 않아 추정하여 표기하지 않습니다. 게임 실행 중에는 생성형 AI 또는 외부 AI 서비스를 사용하지 않습니다.";

        /// <summary>
        /// 좌측 컬럼 (A절) — 수동 \n 줄바꿈, 각 줄 폭 ≤300px (G절 fit 테스트).
        /// 제작 © 표기 = 루트 LICENSE 권리자(P0t4t0), 엔진 줄 = 사실 서술만(권리 표기 없음 —
        /// Unity 상표 귀속·비제휴 고지는 THIRD-PARTY-NOTICES.md 동봉이 담당, 오너 확정 2026-07-13).
        /// </summary>
        public const string LeftColumn =
            "<color=#E5A84B>제작</color>\n" +
            "Client is King — © 2026 P0t4t0\n" +
            "\n" +
            "<color=#E5A84B>엔진</color>\n" +
            "Unity 6 (6000.3.8f1)\n" +
            "\n" +
            "<color=#E5A84B>폰트</color>\n" +
            "Galmuri11 — SIL Open Font License 1.1\n" +
            "© 2019–2025 Lee Minseo (quiple@quiple.dev)\n" +
            "\n" +
            "<color=#E5A84B>플레이스홀더 아트 — CC0 1.0</color>\n" +
            "Ninja Adventure Asset Pack — Pixel-Boy / AAA\n" +
            "Pixel Art Food Pack — karsiori\n" +
            "Free Pixel Food — Henry Software\n" +
            "Roguelike/RPG pack — Kenney (kenney.nl)";

        /// <summary>
        /// 우측 컬럼 (A절) — AI 공개문·라이선스 문장은 자동 wrap(개행 미삽입), 전체 높이 ≤240px.
        /// 라이선스 요약은 task-116 C절 정책의 확정 표현(Codex P0-2) — CC0 표기 금지 규칙 승계.
        /// </summary>
        public const string RightColumn =
            "<color=#E5A84B>AI 보조 아트</color>\n" +
            AiArtNoticeKo + "\n" +
            "\n" +
            "<color=#E5A84B>라이선스</color>\n" +
            "코드: MIT License\n" +
            "프로젝트 고유 아트(AI 보조 아트 포함): MIT 적용 제외, CC0 아님. 프로젝트 오너는 별도의 재사용·재배포·2차 저작물 작성 허가를 부여하지 않습니다. AI 요소의 저작권 성립과 보호 범위는 관할권에 따라 달라질 수 있습니다.";
    }
}
