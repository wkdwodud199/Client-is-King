using System;
using System.IO;
using System.Text;

namespace ClientIsKing.Save
{
    /// <summary>
    /// 세이브 파일 I/O 의 단일 원천 (task-113 E절) — System.IO 는 이 클래스와 테스트만 사용한다.
    /// 원자적 쓰기: {path}.tmp 에 UTF-8(BOM 없음)로 전체 기록 → File.Replace/Move 로 원자 교체.
    /// 모든 예외는 포착해 (false, 사유) 로 변환한다 — 호출자(GameManager)가 진행 비차단 정책을 적용한다.
    /// </summary>
    internal static class SaveFileStore
    {
        internal const string FileName = "save.json";
        internal const string TempSuffix = ".tmp";

        static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        /// <summary>기본 경로 (Application.persistentDataPath/save.json). 접근 시점에 조회한다
        /// (EditMode 에서도 안전하게 호출 가능 — Application.persistentDataPath 는 lifecycle 이 없다).</summary>
        internal static string DefaultPath => Path.Combine(UnityEngine.Application.persistentDataPath, FileName);

        internal static bool Exists(string path)
        {
            return !string.IsNullOrEmpty(path) && File.Exists(path);
        }

        internal static bool TryRead(string path, out string json, out string failReason)
        {
            json = null;
            failReason = "";
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                failReason = "저장 파일이 없습니다.";
                return false;
            }
            try
            {
                json = File.ReadAllText(path, Encoding.UTF8);
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                failReason = $"저장 파일을 읽을 수 없습니다: {e.Message}";
                return false;
            }
        }

        /// <summary>{path}.tmp 에 전체 기록 후 원자 교체. 대상이 있으면 File.Replace, 없으면 File.Move.</summary>
        internal static bool TryWriteAtomic(string path, string json, out string failReason)
        {
            failReason = "";
            if (string.IsNullOrEmpty(path))
            {
                failReason = "저장 경로가 없습니다.";
                return false;
            }
            string tempPath = path + TempSuffix;
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(tempPath, json, Utf8NoBom);

                if (File.Exists(path))
                {
                    File.Replace(tempPath, path, null);
                }
                else
                {
                    File.Move(tempPath, path);
                }
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                failReason = $"저장 파일을 쓸 수 없습니다: {e.Message}";
                return false;
            }
        }

        internal static bool TryDelete(string path, out string failReason)
        {
            failReason = "";
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                failReason = $"저장 파일을 삭제할 수 없습니다: {e.Message}";
                return false;
            }
        }
    }
}
