using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using ClipFlow.Models;
using SkiaSharp;
using Avalonia.Markup.Xaml.Templates;
using HarfBuzzSharp;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Diagnostics;


namespace ClipFlow.Desktop.Utilities
{
    public class CompressionEncryptor
    {
        private const int BufferSize = 4096;

        private static readonly Aes AesInstance = Aes.Create();
        



        // 对字符串进行加密，并保存为临时文件
        public static byte[] EncryptTextToTemporaryFile(ClipboardData data, string password)
        {
            using var stream = new MemoryStream();
            stream.Write(BitConverter.GetBytes((int)data.Type));
            stream.Write(BitConverter.GetBytes(data.Data.Length));
            stream.Write(data.Data);
            if (string.IsNullOrEmpty(password))
            {
                return stream.ToArray();
            }
            else {
                return EncryptWithEcb(stream.ToArray(), password);
            }
        }
        public static void EncryptClipboardDataToTemporaryFile(string path, ClipboardData data, string password)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"ClipFlow{Path.DirectorySeparatorChar}{data.FileName}");
            using FileStream fs = new($"{tempPath}.tmp", FileMode.Create, FileAccess.ReadWrite);
            // 打开 path 指定的文件流，避免占用大量内存
            using FileStream fileStream = new($"{path}", FileMode.Open, FileAccess.Read);
            // 获取文件大小
            var fileSize = fileStream.Length;
            // 写入类型
            fs.Write(BitConverter.GetBytes((int)data.Type));
            // 写入数据大小（long -> 8 字节）
            fs.Write(BitConverter.GetBytes(fileSize));
            // 写入文件名长度
            var fileNameBytes = Encoding.UTF8.GetBytes(data.FileName);
            fs.Write(BitConverter.GetBytes(fileNameBytes.Length));
            // 写入文件名
            fs.Write(fileNameBytes);
            // 以缓冲区方式读取文件并写入目标文件
            var buffer = new byte[81920]; // 80KB 缓冲区，优化大文件处理
            int bytesRead;
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                fs.Write(buffer, 0, bytesRead);
            }
            if (string.IsNullOrEmpty(password))
            {
                fs.Close();
                File.Move($"{tempPath}.tmp", $"{tempPath}.dat");
            }
            else
            {
                EncryptFileStream(fs, password, $"{tempPath}.dat");
            }


            
          
        }

        // 从临时文件解密为文本
        public static async Task<ClipboardData> DecryptTemporaryFileToClipboardDataAsync(string tempPathFile, string password)
        {
            try
            {
                using FileStream fileStream = new FileStream(tempPathFile, FileMode.Open, FileAccess.ReadWrite);
                using var decryptedStream = DecryptWithEcb(fileStream, password);
                using var reader = new BinaryReader(decryptedStream, Encoding.UTF8, leaveOpen: true);
                // 读取类型
                var type = (ClipboardType)reader.ReadInt32();
                // 读取数据大小
                long fileSize = reader.ReadInt64();
                if (type == ClipboardType.Text)
                {
                    byte[] textBytes = reader.ReadBytes((int)fileSize);
                    string text = Encoding.UTF8.GetString(textBytes);
                    return new ClipboardData { Text = text, Type = type };
                }
                else
                {
                    // 读取文件名长度 & 文件名
                    var fileNameLength = reader.ReadInt32();
                    var fileNameBytes = reader.ReadBytes(fileNameLength);
                    var fileName = Encoding.UTF8.GetString(fileNameBytes);

                    var outputPath = Path.Combine(Path.GetDirectoryName(tempPathFile), fileName);
                    using (var outputFileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                    {
                        using var bufferedStream = new BufferedStream(outputFileStream, 131072); // 128KB 缓冲
                        var fileData = new byte[131072];
                        long totalBytesRead = 0;
                        while (totalBytesRead < fileSize)
                        {
                            int bytesRead = await decryptedStream.ReadAsync(fileData, 0, fileData.Length);
                            if (bytesRead == 0) break; // 读取完毕
                            await bufferedStream.WriteAsync(fileData, 0, bytesRead);
                            totalBytesRead += bytesRead;
                        }
                    }
                    var data= new ClipboardData { FileName = fileName, Type = type};
                    if (type == ClipboardType.File)
                    {
                        data.CopyFiles=[outputPath] ;
                    }
                    else if (type== ClipboardType.FileList)
                    {
                        data.CopyFiles= ClipboardUtils.ExtractZipArchive(Path.Combine(outputPath));
                        File.Delete(outputPath);
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("处理原始数据错误。");
            }
            finally
            {
                if (File.Exists(tempPathFile))
                    File.Delete(tempPathFile);
            }
            


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

        // 直接返回一个加密后的 FileStream
        public static void EncryptFileStream(FileStream inputFileStream, string password, string outputFilePath)
        {
            // 设置加密模式和填充方式
            AesInstance.Mode = CipherMode.ECB;  // 使用 ECB 模式
            AesInstance.Key = DeriveKey(password, AesInstance.KeySize / 8);
            AesInstance.Padding = PaddingMode.PKCS7;  // 填充模式
            // 创建一个 FileStream 来保存加密后的字节数据
            using (FileStream outputFileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
            {
                using (ICryptoTransform encryptor = AesInstance.CreateEncryptor())
                using (CryptoStream cryptoStream = new CryptoStream(outputFileStream, encryptor, CryptoStreamMode.Write))
                {
                    // 重置输入流的位置指针
                    inputFileStream.Position = 0;
                    // 通过 CryptoStream 进行流加密，将输入流中的数据写入到加密流
                    inputFileStream.CopyTo(cryptoStream);
                    // 等待加密操作完成
                    cryptoStream.FlushFinalBlock();
                    cryptoStream.Flush();
                    cryptoStream.Close();
                }
            } ;
        }
        private static Stream DecryptWithEcb(Stream encryptedData, string password)
        {
            Aes aes = Aes.Create();
            aes.Mode = CipherMode.ECB;
            aes.Key = DeriveKey(password, aes.KeySize / 8);
            aes.Padding = PaddingMode.PKCS7;

            ICryptoTransform decryptor = aes.CreateDecryptor();
            return new CryptoStream(encryptedData, decryptor, CryptoStreamMode.Read, leaveOpen: false);
        }


        // 根据密码派生密钥
        private static byte[] DeriveKey(string password, int keySize)
        {
            using var deriveBytes = new Rfc2898DeriveBytes(password, "salt"u8.ToArray(), 100000, HashAlgorithmName.SHA256);
            return deriveBytes.GetBytes(keySize);
        }



      
        
    }


}
