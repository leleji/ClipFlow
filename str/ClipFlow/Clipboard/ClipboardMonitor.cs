using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ClipFlow.Models;
using ClipFlow.Services;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace ClipFlow.Clipboard
{
    public class ClipboardMonitor : IDisposable
    {
        private string? _lastHash;
        private bool _isMonitoring;
        private System.Timers.Timer? _timer;
        // 添加一个标志来指示是否是程序自己修改的剪贴板
        private bool _isSettingClipboard;
        // 添加一个标志来指示是否是来自服务器的更新
        private bool _isServerUpdate;
        private const string TempFolderName = "ClipFlow";

        // 错误事件
        public event EventHandler<Exception> OnError;
        // 剪贴板变化事件委托和事件
        public delegate void ClipboardChangedEventHandler(ClipboardData data);
        public event ClipboardChangedEventHandler OnClipboardChanged;

        public ClipboardMonitor()
        {
            // 在构造函数中初始化 UI 相关的内容可能为空，因为窗口还未创建
        }

        public void Start()
        {
            if (_isMonitoring) return;

            _isMonitoring = true;
            _timer = new System.Timers.Timer(1000); // 每秒检查一次
            _timer.Elapsed += (s, e) =>
            {
                // 在 UI 线程上执行检查
                Dispatcher.UIThread.Post(async () =>
                {
                    await CheckClipboardContent();
                });
            };
            _timer.Start();
        }

        public void Stop()
        {
            _isMonitoring = false;
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
        }

        private async Task<ClipboardData?> ProcessTextContent(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;

            var textHash = GetMd5Hash(text);
            if (textHash == _lastHash) return null;

            _lastHash = textHash;
            return new ClipboardData
            {
                Type = ClipboardType.Text,
                Data = Encoding.UTF8.GetBytes(text),
                Description = "文本: "+(text.Length > 30 ? text[..30] + "..." : text)
            };
        }

        private async Task<ClipboardData?> ProcessFileContent(IEnumerable<IStorageItem> files)
        {
            var fileList = files.ToList();
            if (!fileList.Any()) return null;

            var filesHash = GetMd5Hash(string.Join("|", fileList.Select(f => f.Path.LocalPath)));
            if (filesHash == _lastHash) return null;
            _lastHash = filesHash;

            return fileList.Count == 1 && !Directory.Exists(fileList[0].Path.LocalPath)
                ? await ProcessSingleFile(fileList[0])
                : await ProcessMultipleItems(fileList);
        }

        private async Task<ClipboardData> ProcessSingleFile(IStorageItem file)
        {
            return new ClipboardData
            {
                Type = ClipboardType.File,
                Filename = file.Name,
                FilenameList = new List<string> { file.Path.LocalPath },
                DataLength = (await file.GetBasicPropertiesAsync()).Size,
                Description = $"单文件: {file.Name}"
            };
        }

        private async Task<ClipboardData> ProcessMultipleItems(List<IStorageItem> items)
        {
            // 计算总大小
            var sizes = await Task.WhenAll(items.Select(async file => (await file.GetBasicPropertiesAsync()).Size));
            ulong totalSize = 0;
            foreach (var size in sizes)
            {
                if (size.HasValue)
                {
                    totalSize += size.Value;
                }
            }
            return new ClipboardData
            {
                Type = ClipboardType.FileList,
                FilenameList = items.Select(v => v.Path.LocalPath).ToList(),
                Filename = $"files_{DateTime.Now:yyyyMMddHHmmss}.zip",
                DataLength = totalSize,
                Description = $"{items.Count} 个文件: {string.Join(", ", items.Select(v => v.Name))}"
            };
        }

        public async Task AddItemToArchive(ZipArchive archive, string path)
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
                OnError?.Invoke(this, ex);
                LogService.Instance.AddLog("警告", $"压缩文件失败: {path} - {ex.Message}");
            }
        }

        private async Task AddFolderToArchive(ZipArchive archive, string baseFolder)
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

        private async Task AddFileToArchive(ZipArchive archive, string baseFolder)
        {
            var entry = archive.CreateEntry(Path.GetFileName(baseFolder));
            using var entryStream = entry.Open();
            using var fileStream = File.OpenRead(baseFolder);
            await fileStream.CopyToAsync(entryStream);
        }

        private async Task CheckClipboardContent()
        {
            if (!_isMonitoring || _isSettingClipboard) return;

            try
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                    desktop.MainWindow?.Clipboard != null)
                {
                    var clipboard = desktop.MainWindow.Clipboard;
                    ClipboardData clipData = null;
                    var frmats = await clipboard.GetFormatsAsync();
                    if (frmats.Contains("Files"))
                    {
                        // 检查文件内容
                        var clipboardFiles = await clipboard.GetDataAsync("Files") as IEnumerable<IStorageItem>;
                        if (clipboardFiles != null)
                        {
                            clipData = await ProcessFileContent(clipboardFiles);
                        }
                    }
                    else
                    {
                        // 检查文本内容
                        var text = await clipboard.GetTextAsync();
                        clipData = await ProcessTextContent(text);
                    }
                    
                    if (clipData != null)
                    {
                        OnClipboardChanged?.Invoke(clipData);
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.AddLog("错误", $"监控剪贴板失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置剪贴板内容（来自服务器的更新）
        /// </summary>
        public async Task<bool> SetClipboardContentAsync(ClipboardData data, bool isServerUpdate = true)
        {

            try
            {
                _isSettingClipboard = true;
                _isServerUpdate = isServerUpdate;

                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                    desktop.MainWindow?.Clipboard != null)
                {
                    var clipboard = desktop.MainWindow.Clipboard;

                    switch (data.Type)
                    {
                        case ClipboardType.Text:
                            var text = Encoding.UTF8.GetString(data.Data);
                            await clipboard.SetTextAsync(text);
                            _lastHash = GetMd5Hash(text);
                            data.Description = "文本: " + (text.Length > 30 ? text[..30] + "..." : text);
                            LogService.Instance.AddLog("已接收", data.Description);

                            break;

                        case ClipboardType.File:
                        case ClipboardType.FileList:


                            if (data.FilenameList.Any())
                            {
                                var provider = desktop.MainWindow.StorageProvider;
                                var storageItems = data.FilenameList.Select<string, IStorageItem?>(file =>
                                {
                                    if (Directory.Exists(file))
                                    {
                                        return provider.TryGetFolderFromPathAsync(file).Result;
                                    }
                                    return provider.TryGetFileFromPathAsync(file).Result;
                                }).Where(item => item is not null);
                                var dataObject = new DataObject();
                                dataObject.Set("Files", storageItems);
                                await clipboard.SetDataObjectAsync(dataObject);
                                _lastHash = GetMd5Hash(string.Join("|", data.FilenameList));
                                data.Description = $"{storageItems.Count()} 个文件: {string.Join(", ", storageItems.Select(v => v.Name))}";
                                LogService.Instance.AddLog("已接收", data.Description);
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, ex);
                LogService.Instance.AddLog("错误", $"设置剪贴板内容失败: {ex.Message}");
                return false;
            }
            finally
            {
                _isSettingClipboard = false;
                _ = Task.Delay(1000).ContinueWith(_ =>
                {
                    _isServerUpdate = false;
                });
            }
            return true;
        }
        private string GetNameFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            // 移除末尾的路径分隔符（如果有的话）
            path = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // 获取最后一段路径名
            return Path.GetFileName(path);
        }
        private FileType GetFileType(string extension)
        {
            if (new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".ico", ".tiff" }
                .Contains(extension))
            {
                return FileType.Image;
            }
            if (new[] { ".doc", ".docx", ".pdf", ".txt", ".xls", ".xlsx", ".ppt", ".pptx" }
                .Contains(extension))
            {
                return FileType.Document;
            }
            if (new[] { ".zip", ".rar", ".7z", ".tar", ".gz" }
                .Contains(extension))
            {
                return FileType.Archive;
            }
            if (new[] { ".mp3", ".wav", ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv" }
                .Contains(extension))
            {
                return FileType.Media;
            }
            return FileType.Other;
        }

        public void Dispose()
        {
            Stop();
        }
        public static string GetMd5Hash(string input)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] bytes = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(bytes).ToLower();
            }
        }

        private bool IsImageFile(string filename)
        {
            var extension = Path.GetExtension(filename).ToLower();
            return new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".ico", ".tiff" }.Contains(extension);
        }



    }

    public class ClipboardChangedEventArgs : EventArgs
    {
        public ClipboardType ClipboardType { get; set; }
        public string? Text { get; set; }
        public IList<ClipboardFileInfo>? Files { get; set; }
    }

    public class ClipboardFileInfo
    {
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Path { get; set; } = string.Empty;
        public string? Extension { get; set; }
    }
}