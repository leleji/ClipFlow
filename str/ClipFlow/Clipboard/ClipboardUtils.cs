using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ClipFlow.Desktop.Services;

namespace ClipFlow.Desktop.Clipboard
{
    public static class ClipboardUtils
    {
        public static string GetMd5Hash(string input)
        {
            using var md5Hash = MD5.Create();
            var bytes = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).ToLower();
        }

        public static async Task<List<string>> ExtractZipArchive(string zipFilePath)
        {
            var extractedPaths = new List<string>();
            var extractPath = Path.GetDirectoryName(zipFilePath)!;
            var processedFirstLevelDirs = new HashSet<string>();
            var processedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (var archive = ZipFile.OpenRead(zipFilePath))
                {
                    // 先处理所有条目，创建完整的目录结构
                    foreach (var entry in archive.Entries)
                    {
                        // 标准化路径分隔符
                        var normalizedEntryName = entry.FullName.Replace('\\', '/');
                        var fullPath = Path.GetFullPath(Path.Combine(extractPath, normalizedEntryName));

                        // 确保解压路径在目标目录内
                        if (!fullPath.StartsWith(extractPath))
                        {
                            LogService.Instance.AddLog("警告", $"检测到潜在的路径遍历攻击: {entry.FullName}");
                            continue;
                        }

                        // 检查是否已处理过此路径
                        if (processedPaths.Contains(fullPath))
                        {
                            continue;
                        }

                        if (entry.Name == "") // 文件夹
                        {
                            Directory.CreateDirectory(fullPath);
                        }
                        else // 文件
                        {
                            var dirPath = Path.GetDirectoryName(fullPath)!;
                            Directory.CreateDirectory(dirPath);
                            entry.ExtractToFile(fullPath, true);
                            processedPaths.Add(fullPath);
                        }

                        // 只处理第一层的路径
                        var pathParts = normalizedEntryName.Split('/', StringSplitOptions.RemoveEmptyEntries);
                        if (pathParts.Length == 1) // 第一层的文件
                        {
                            if (!string.IsNullOrEmpty(entry.Name)) // 是文件
                            {
                                extractedPaths.Add(fullPath);
                            }
                        }
                        else if (pathParts.Length > 1) // 可能是子文件夹
                        {
                            var firstLevelDir = Path.Combine(extractPath, pathParts[0]);
                            if (!processedFirstLevelDirs.Contains(firstLevelDir))
                            {
                                extractedPaths.Add(firstLevelDir + Path.DirectorySeparatorChar);
                                processedFirstLevelDirs.Add(firstLevelDir);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.AddLog("错误", $"解压文件失败: {ex.Message}");
                throw;
            }

            return extractedPaths;
        }

        public static async Task CreateZipArchive(ZipArchive archive, IEnumerable<string> paths)
        {
            // 找到所有路径的共同父目录
            var commonParent = GetCommonParentPath(paths);
            var processedDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var path in paths)
            {
                try
                {
                    if (Directory.Exists(path))
                    {
                        // 创建文件夹及其内容
                        await AddDirectoryToArchive(archive, path, commonParent, processedDirs);
                    }
                    else if (File.Exists(path))
                    {
                        // 添加单个文件
                        var relativePath = Path.GetRelativePath(commonParent, path);
                        await AddFileToArchive(archive, path, relativePath);
                    }
                    else
                    {
                        LogService.Instance.AddLog("警告", $"路径不存在: {path}");
                    }
                }
                catch (Exception ex)
                {
                    LogService.Instance.AddLog("警告", $"压缩失败: {path} - {ex.Message}");
                }
            }
        }

        private static async Task AddDirectoryToArchive(ZipArchive archive, string dirPath, string basePath, HashSet<string> processedDirs)
        {
            // 获取所有文件
            var files = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories);
            
            if (files.Length > 0)
            {
                // 如果有文件，创建必要的文件夹结构
                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(basePath, file).Replace('\\', '/');
                    var dirName = Path.GetDirectoryName(relativePath);
                    
                    if (!string.IsNullOrEmpty(dirName) && !processedDirs.Contains(dirName))
                    {
                        // 创建文件所在的文件夹及其父文件夹
                        var pathParts = dirName.Split('/', '\\');
                        var currentPath = "";
                        foreach (var part in pathParts)
                        {
                            currentPath = string.IsNullOrEmpty(currentPath) ? part : currentPath + "/" + part;
                            if (!processedDirs.Contains(currentPath))
                            {
                                archive.CreateEntry(currentPath + "/");
                                processedDirs.Add(currentPath);
                            }
                        }
                    }
                    
                    // 添加文件
                    await AddFileToArchive(archive, file, relativePath);
                }
            }
            else
            {
                // 如果是空文件夹，只创建一个文件夹条目
                var relativePath = Path.GetRelativePath(basePath, dirPath).Replace('\\', '/');
                if (!processedDirs.Contains(relativePath))
                {
                    archive.CreateEntry(relativePath + "/");
                    processedDirs.Add(relativePath);
                }
            }
        }

        private static string GetCommonParentPath(IEnumerable<string> paths)
        {
            var pathArray = paths.ToArray();
            if (pathArray.Length == 0) return string.Empty;
            if (pathArray.Length == 1) return Path.GetDirectoryName(pathArray[0])!;

            var firstPath = pathArray[0];
            var commonParent = Path.GetDirectoryName(firstPath)!;

            while (!string.IsNullOrEmpty(commonParent))
            {
                if (pathArray.All(p => p.StartsWith(commonParent, StringComparison.OrdinalIgnoreCase)))
                {
                    return commonParent;
                }
                commonParent = Path.GetDirectoryName(commonParent)!;
            }

            return commonParent;
        }

        private static async Task AddFileToArchive(ZipArchive archive, string filePath, string entryName)
        {
            try
            {
                var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var fileStream = File.OpenRead(filePath);
                await fileStream.CopyToAsync(entryStream);
            }
            catch (Exception ex)
            {
                LogService.Instance.AddLog("警告", $"添加文件到压缩包失败: {filePath} - {ex.Message}");
            }
        }
    }
} 