using ClipFlow.Core.Models;
using ClipFlow.Core.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ClipFlow.Test
{
    [TestClass]
    public class CompressionEncryptorTests
    {


        [TestMethod]
        public async Task DecryptTemporaryFileToClipboardDataAsync_FileType_ReturnsCorrectFileName()
        {
            Aes AesInstance = Aes.Create();
            var dsd= DeriveKey("123", AesInstance.KeySize / 8);

            var tempPathFile = "C:\\Users\\3996\\AppData\\Local\\Temp\\ClipFlow\\fe2dc93d-91b3-449a-9f87-e67e0ea7f7c7\\fe2dc93d-91b3-449a-9f87-e67e0ea7f7c7.dat";
            var password = "123";
            using FileStream fileStream = new FileStream(tempPathFile, FileMode.Open, FileAccess.ReadWrite);
            using var decryptedStream = fileStream;
            using var reader = new BinaryReader(decryptedStream, Encoding.UTF8, leaveOpen: true);
            // 读取类型
            var type = (ClipboardType)reader.ReadInt32();
            // 读取数据大小
            long fileSize = reader.ReadInt64();
            if (type == ClipboardType.Text)
            {
                byte[] textBytes = reader.ReadBytes((int)fileSize);
                string text = Encoding.UTF8.GetString(textBytes);

            }
            else
            {
                // 读取文件名长度 & 文件名
                var fileNameLength = reader.ReadInt32();
                var fileNameBytes = reader.ReadBytes(fileNameLength);
                var fileName = Encoding.UTF8.GetString(fileNameBytes);
            }
        }
        private static byte[] DeriveKey(string password, int keySize)
        {
            using var deriveBytes = new Rfc2898DeriveBytes(password, "salt"u8.ToArray(), 1000, HashAlgorithmName.SHA256);
            return deriveBytes.GetBytes(keySize);
        }

    }
}