using ClipFlow.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ClipFlow.Core.Services
{
    public static class ClipboardProcessingService
    {
        private const int AesKeySize = 32; // 256 bits
        private const int IvSize = 16;     // 128 bits

        #region 公开 API

        /// <summary>
        /// 处理小数据（文本、图片）：内存中完成
        /// </summary>
        public static (string Fingerprint, byte[] EncryptedData) ProcessData(ClipboardType type, byte[] rawData, byte[] key)
        {
            string fingerprint = CalculateHash(rawData);

            // 文本和图片通常需要 Brotli 压缩
            byte[] compressed = Compress(rawData);
            byte[] encrypted = EncryptBytes(compressed, key);

            return (fingerprint, encrypted);
        }

        /// <summary>
        /// 处理大数据（文件/文件夹）：流式写入临时文件
        /// </summary>
        public static async Task<(string Fingerprint, string TempFilePath)> ProcessFilesAsync(List<string> paths, byte[] key)
        {
            // 1. 基于路径计算指纹（秒传判断用）
            string fingerprint = CalculatePathsHash(paths);

            // 2. 准备临时文件路径
            string tempDir = Path.Combine(Path.GetTempPath(), "ClipFlow");
            Directory.CreateDirectory(tempDir);

            string tempZip = Path.Combine(tempDir, Guid.NewGuid() + ".tmp");
            string finalEnc = Path.Combine(tempDir, Guid.NewGuid() + ".enc");

            // 3. 打包文件 (流式)
            using (var zipFs = new FileStream(tempZip, FileMode.Create))
            {
                using var archive = new ZipArchive(zipFs, ZipArchiveMode.Create);
                foreach (var path in FilterSubPaths(paths))
                {
                    if (File.Exists(path)) AddFileToZip(archive, path, Path.GetFileName(path));
                    else if (Directory.Exists(path)) AddDirectoryToZip(archive, path, new DirectoryInfo(path).Name);
                }
            }

            // 4. 加密文件 (流式)
            await EncryptFileAsync(tempZip, finalEnc, key);

            // 5. 清理中间 Zip
            File.Delete(tempZip);

            return (fingerprint, finalEnc);
        }

        /// <summary>
        /// 统一解密逻辑：返回解密后的字节流（适配内存或文件）
        /// </summary>
        public static async Task<byte[]> DecryptDataAsync(byte[] encryptedData, byte[] key)
        {
            return await Task.Run(() => DecryptBytes(encryptedData, key));
        }

        #endregion

        #region 私有核心逻辑 (AES-CBC)

        private static byte[] EncryptBytes(byte[] plainBytes, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using var ms = new MemoryStream();
            ms.Write(aes.IV, 0, IvSize); // 写入 IV

            using (var cryptoStream = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cryptoStream.Write(plainBytes, 0, plainBytes.Length);
            }
            return ms.ToArray();
        }

        private static byte[] DecryptBytes(byte[] encryptedData, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            byte[] iv = new byte[IvSize];
            Array.Copy(encryptedData, 0, iv, 0, IvSize);
            aes.IV = iv;

            using var msInput = new MemoryStream(encryptedData, IvSize, encryptedData.Length - IvSize);
            using var cryptoStream = new CryptoStream(msInput, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var msOutput = new MemoryStream();
            cryptoStream.CopyTo(msOutput);
            return msOutput.ToArray();
        }

        private static async Task EncryptFileAsync(string src, string dest, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using var destFs = new FileStream(dest, FileMode.Create);
            await destFs.WriteAsync(aes.IV, 0, IvSize); // 写入 IV

            using var cryptoStream = new CryptoStream(destFs, aes.CreateEncryptor(), CryptoStreamMode.Write);
            using var srcFs = new FileStream(src, FileMode.Open);
            await srcFs.CopyToAsync(cryptoStream);
        }

        #endregion

        #region 辅助方法 (指纹、压缩、Zip)

        private static string CalculateHash(byte[] data) =>
            Convert.ToHexString(SHA256.HashData(data)).ToLower();

        private static string CalculatePathsHash(List<string> paths)
        {
            using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            foreach (var p in paths.OrderBy(x => x)) sha.AppendData(Encoding.UTF8.GetBytes(p));
            return Convert.ToHexString(sha.GetHashAndReset()).ToLower();
        }

        private static byte[] Compress(byte[] data)
        {
            using var ms = new MemoryStream();
            using (var br = new BrotliStream(ms, CompressionLevel.Fastest)) br.Write(data, 0, data.Length);
            return ms.ToArray();
        }

        private static void AddFileToZip(ZipArchive arc, string src, string entry)
        {
            var e = arc.CreateEntry(entry, CompressionLevel.Fastest);
            using var es = e.Open();
            using var fs = File.OpenRead(src);
            fs.CopyTo(es);
        }

        private static void AddDirectoryToZip(ZipArchive arc, string src, string root)
        {
            foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
            {
                string rel = Path.GetRelativePath(src, file);
                AddFileToZip(arc, file, Path.Combine(root, rel));
            }
        }

        private static List<string> FilterSubPaths(List<string> paths)
        {
            var sorted = paths.OrderBy(p => p.Length).ToList();
            var result = new List<string>();
            foreach (var path in sorted)
                if (!result.Any(r => path.StartsWith(r + Path.DirectorySeparatorChar) || path == r))
                    result.Add(path);
            return result;
        }

        #endregion
    }
}