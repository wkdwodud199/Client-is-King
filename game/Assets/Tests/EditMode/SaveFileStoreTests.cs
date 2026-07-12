using System.IO;
using ClientIsKing.Save;
using NUnit.Framework;
using UnityEngine;

namespace ClientIsKing.Tests.EditMode
{
    /// <summary>
    /// task-113 U1 자체검증: SaveFileStore 원자적 파일 I/O — 정상 쓰기·tmp 무해성·읽기/삭제 실패 사유.
    /// 실사용 persistentDataPath 를 건드리지 않도록 Application.temporaryCachePath 하위에서만 동작한다.
    /// </summary>
    public class SaveFileStoreTests
    {
        string testDir;
        string savePath;

        [SetUp]
        public void SetUp()
        {
            testDir = Path.Combine(Application.temporaryCachePath, "task113-savefilestore-tests-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDir);
            savePath = Path.Combine(testDir, "save.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, recursive: true);
            }
        }

        [Test]
        public void Exists_Returns_False_When_No_File()
        {
            Assert.IsFalse(SaveFileStore.Exists(savePath));
        }

        [Test]
        public void TryWriteAtomic_Then_TryRead_RoundTrips_Content()
        {
            Assert.IsTrue(SaveFileStore.TryWriteAtomic(savePath, "{\"day\":1}", out var writeReason), writeReason);
            Assert.IsTrue(SaveFileStore.Exists(savePath));

            Assert.IsTrue(SaveFileStore.TryRead(savePath, out var json, out var readReason), readReason);
            Assert.AreEqual("{\"day\":1}", json);
        }

        [Test]
        public void TryWriteAtomic_Leaves_No_Tmp_File_After_Success()
        {
            Assert.IsTrue(SaveFileStore.TryWriteAtomic(savePath, "{}", out _));
            Assert.IsFalse(File.Exists(savePath + SaveFileStore.TempSuffix), "정상 쓰기 후 tmp 가 남으면 안 된다");
        }

        [Test]
        public void TryWriteAtomic_Overwrites_Existing_File_Successfully()
        {
            Assert.IsTrue(SaveFileStore.TryWriteAtomic(savePath, "{\"v\":1}", out _));
            Assert.IsTrue(SaveFileStore.TryWriteAtomic(savePath, "{\"v\":2}", out var reason), reason);

            Assert.IsTrue(SaveFileStore.TryRead(savePath, out var json, out _));
            Assert.AreEqual("{\"v\":2}", json);
            Assert.IsFalse(File.Exists(savePath + SaveFileStore.TempSuffix));
        }

        [Test]
        public void TryRead_Ignores_Orphaned_Tmp_And_Reads_Existing_Save()
        {
            Assert.IsTrue(SaveFileStore.TryWriteAtomic(savePath, "{\"v\":1}", out _));
            // 중간 크래시 모사 — tmp 만 남기고 실제 save.json 은 이전 값 그대로.
            File.WriteAllText(savePath + SaveFileStore.TempSuffix, "{\"v\":999-partial");

            Assert.IsTrue(SaveFileStore.TryRead(savePath, out var json, out var reason), reason);
            Assert.AreEqual("{\"v\":1}", json, "로드는 tmp 를 읽지 않고 기존 save.json 을 읽어야 한다");
        }

        [Test]
        public void TryWriteAtomic_Replaces_Orphaned_Tmp_Harmlessly_On_Next_Write()
        {
            Assert.IsTrue(SaveFileStore.TryWriteAtomic(savePath, "{\"v\":1}", out _));
            File.WriteAllText(savePath + SaveFileStore.TempSuffix, "stale-tmp");

            Assert.IsTrue(SaveFileStore.TryWriteAtomic(savePath, "{\"v\":2}", out var reason), reason);
            Assert.IsFalse(File.Exists(savePath + SaveFileStore.TempSuffix));
            Assert.IsTrue(SaveFileStore.TryRead(savePath, out var json, out _));
            Assert.AreEqual("{\"v\":2}", json);
        }

        [Test]
        public void TryRead_Fails_With_Reason_When_File_Missing()
        {
            Assert.IsFalse(SaveFileStore.TryRead(savePath, out var json, out var reason));
            Assert.IsNull(json);
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void TryDelete_Removes_Existing_File()
        {
            Assert.IsTrue(SaveFileStore.TryWriteAtomic(savePath, "{}", out _));
            Assert.IsTrue(SaveFileStore.TryDelete(savePath, out var reason), reason);
            Assert.IsFalse(SaveFileStore.Exists(savePath));
        }

        [Test]
        public void TryDelete_Succeeds_When_File_Already_Absent()
        {
            Assert.IsTrue(SaveFileStore.TryDelete(savePath, out var reason), reason);
        }

        [Test]
        public void Written_File_Is_Utf8_Without_Bom()
        {
            Assert.IsTrue(SaveFileStore.TryWriteAtomic(savePath, "가나다", out _));
            byte[] bytes = File.ReadAllBytes(savePath);
            bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            Assert.IsFalse(hasBom, "UTF-8 BOM 이 없어야 한다");
        }
    }
}
