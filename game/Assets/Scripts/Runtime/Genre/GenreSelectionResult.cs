namespace ClientIsKing.Genre
{
    /// <summary>
    /// 장르(전문 분야) 선택 시도 결과 — UI 와 테스트가 같은 계약을 쓴다 (task-110 U1).
    /// 실패 경로는 GameState 를 절대 변경하지 않는다 (design.md D5/H5 계약).
    /// </summary>
    public readonly struct GenreSelectionResult
    {
        public GenreSelectionResult(bool success, string message, string genreId)
        {
            Success = success;
            Message = message;
            GenreId = genreId;
        }

        public bool Success { get; }
        /// <summary>UI 에 그대로 표시하는 한국어 메시지.</summary>
        public string Message { get; }
        /// <summary>성공 시 확정된 genre id (실패 시 빈 문자열).</summary>
        public string GenreId { get; }
    }
}
