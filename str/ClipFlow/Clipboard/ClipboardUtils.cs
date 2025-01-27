using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ClipFlow.Services;

namespace ClipFlow.Clipboard
{
    public static class ClipboardUtils
    {
        public static string GetMd5Hash(string input)
        {
            using var md5Hash = MD5.Create();
            var bytes = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).ToLower();
        }

        public static async Task AddItemToArchive(ZipArchive archive, string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    await AddFolderToArchive(archive, path);
                }
                else
                {
                    await AddFileToArchive(archive, path);
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.AddLog("警告", $"压缩文件失败: {path} - {ex.Message}");
            }
        }

        private static async Task AddFolderToArchive(ZipArchive archive, string baseFolder)
        {
            var files = Directory.GetFiles(baseFolder, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(baseFolder, file);
                var entry = archive.CreateEntry(Path.Combine(Path.GetFileName(baseFolder), relativePath));
                using var entryStream = entry.Open();
                using var fileStream = File.OpenRead(file);
                await fileStream.CopyToAsync(entryStream);
            }
        }

        private static async Task AddFileToArchive(ZipArchive archive, string baseFolder)
        {
            var entry = archive.CreateEntry(Path.GetFileName(baseFolder));
            using var entryStream = entry.Open();
            using var fileStream = File.OpenRead(baseFolder);
            await fileStream.CopyToAsync(entryStream);
        }
    }
} 