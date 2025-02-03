using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ProtoBuf;
using K4os.Compression.LZ4;
using System.Linq;
using System.Threading.Tasks;

namespace ClipFlow.Desktop.Utilities
{
    public class CompressionEncryptor
    {
        private const int BufferSize = 4096;

        private static readonly Aes AesInstance = Aes.Create();
        
        // 处理文件打包和加密
        public static async Task<byte[]> PackageAndEncryptFilesAsync(string baseDir, List<string> files, string encryptedFilePath, string password)
        {
            var fileData = new ClipboardData
            {
                Files = new List<FileEntry>()
            };

            // 处理文件
            foreach (var file in files)
            {
                string relativePath = Path.GetRelativePath(baseDir, file);
                byte[] content = await File.ReadAllBytesAsync(file);
                fileData.Files.Add(new FileEntry
                {
                    Path = relativePath,
                    Content = content
                });
            }

            using var fileStream = new MemoryStream();
            // 使用 protobuf-net 序列化
            Serializer.Serialize(fileStream, fileData);
            byte[] serializedData = fileStream.ToArray();

            byte[] compressedData = Compress(serializedData);
            byte[] encryptedData = EncryptWithEcb(compressedData, password);
            await File.WriteAllBytesAsync(encryptedFilePath, encryptedData);
            return encryptedData;
        }

        // 解密并解压文件
        public static async Task DecryptAndExtractFilesAsync(string encryptedFilePath, string outputDir, string password)
        {
            byte[] encryptedData = await File.ReadAllBytesAsync(encryptedFilePath);
            byte[] decompressedData = DecryptWithEcb(encryptedData, password);
            byte[] serializedData = Decompress(decompressedData);

            using (var memoryStream = new MemoryStream(serializedData))
            {
                var fileData = Serializer.Deserialize<ClipboardData>(memoryStream);
                foreach (var entry in fileData.Files)
                {
                    string filePath = Path.Combine(outputDir, entry.Path);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    await File.WriteAllBytesAsync(filePath, entry.Content);
                }
            }
        }

        // 对字符串进行加密，并保存为临时文件
        public static byte[] EncryptTextToTemporaryFile(string text, string password)
        {
            var fileData = new ClipboardData
            {
                Text = text
            };
            using var fileStream = new MemoryStream();
            // 使用 protobuf-net 序列化
            Serializer.Serialize(fileStream, fileData);
            byte[] serializedData = fileStream.ToArray();
            byte[] compressedData = Compress(serializedData);
            byte[] encryptedData = EncryptWithEcb(compressedData, password);
            return encryptedData;
        }

        // 从临时文件解密为文本
        public static string DecryptTemporaryFileToText(string tempFilePath, string password)
        {
            // 读取加密数据
            byte[] encryptedData = File.ReadAllBytes(tempFilePath);

            // 解密数据
            byte[] decryptedData = DecryptWithEcb(encryptedData, password);

            // 转换回原文本
            return Encoding.UTF8.GetString(decryptedData);
        }

        // 使用 AES-ECB 模式加密数据
        private static byte[] EncryptWithEcb(byte[] data, string password)
        {
            AesInstance.Mode = CipherMode.ECB;  // 使用 ECB 模式
            AesInstance.Key = DeriveKey(password, AesInstance.KeySize / 8);
            AesInstance.Padding = PaddingMode.PKCS7;  // 填充模式
            using var encryptor = AesInstance.CreateEncryptor();
            return encryptor.TransformFinalBlock(data, 0, data.Length);
        }

        // 使用 AES-ECB 模式解密数据
        private static byte[] DecryptWithEcb(byte[] data, string password)
        {
            AesInstance.Mode = CipherMode.ECB;  // 使用 ECB 模式
            AesInstance.Key = DeriveKey(password, AesInstance.KeySize / 8);
            AesInstance.Padding = PaddingMode.PKCS7;  // 填充模式
            using var decryptor = AesInstance.CreateDecryptor();
            return decryptor.TransformFinalBlock(data, 0, data.Length);
        }

        // 根据密码派生密钥
        private static byte[] DeriveKey(string password, int keySize)
        {
            using var deriveBytes = new Rfc2898DeriveBytes(password, "salt"u8.ToArray(), 100000, HashAlgorithmName.SHA256);
            return deriveBytes.GetBytes(keySize);
        }

        // LZ4 压缩数据
        private static byte[] Compress(byte[] data)
        {
            int maxCompressedSize = LZ4Codec.MaximumOutputSize(data.Length);
            byte[] compressedData = new byte[maxCompressedSize];
            int compressedLength = LZ4Codec.Encode(data, 0, data.Length, compressedData, 0, compressedData.Length);
            return compressedData.Take(compressedLength).ToArray(); // 使用 LINQ 来避免 Array.Resize
        }

        // LZ4 解压数据
        private static byte[] Decompress(byte[] data)
        {
            int decompressedLength = LZ4Codec.Decode(data, 0, data.Length, null, 0, 0); // 获取解压后的数据大小
            byte[] decompressedData = new byte[decompressedLength];
            LZ4Codec.Decode(data, 0, data.Length, decompressedData, 0, decompressedData.Length); // 解压
            return decompressedData;
        }
    }

    [ProtoContract]
    public class FileEntry
    {
        [ProtoMember(1)]
        public string Path { get; set; }

        [ProtoMember(2)]
        public byte[] Content { get; set; }
    }

    [ProtoContract]
    public class ClipboardData
    {
        [ProtoMember(1)]
        public List<FileEntry> Files { get; set; }
        
        [ProtoMember(2)]
        public string Text { get; set; }
        
        
    }
}
