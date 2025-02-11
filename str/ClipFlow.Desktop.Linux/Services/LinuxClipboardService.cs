using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using ClipFlow.Desktop.Interfaces;
using ClipFlow.Desktop.Services;
using ClipFlow.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipFlow.Desktop.Linux.Services
{
    public class LinuxClipboardService : IClipboardHandler
    {
        private const string FileFormat = "x-special/gnome-copied-files";
        private string? _lastHash;
        private bool _isSettingClipboard;
        private bool _isServerUpdate;

        public async Task<ClipboardData?> GetContentAsync()
        {
            if (_isSettingClipboard) return null;

            try
            {
                if (App.Clipboard != null)
                {
                    var clipboard = App.Clipboard;
                    var formats = await clipboard.GetFormatsAsync();
                    
                    if (formats.Contains(FileFormat))
                    {
                        var bytes = await clipboard.GetDataAsync(FileFormat) as byte[];
                        if (bytes != null)
                        {
                            var str = Encoding.UTF8.GetString(bytes);
                            var pathList = str.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                                            .Where(x => !string.IsNullOrEmpty(x))
                                            .ToArray();

                            if (pathList.Length > 1)
                            {
                                var files = await GetStorageItems(pathList.Skip(1));
                                if (files.Any())
                                {
                                    var filesHash = GetMd5Hash(string.Join("|", files.Select(f => f.Path.LocalPath)));
                                    if (filesHash == _lastHash) return null;
                                    _lastHash = filesHash;
                                    return await ProcessFiles(files);
                                }
                            }
                        }
                    }
                    else
                    {
                        var text = await clipboard.GetTextAsync();
                        if (string.IsNullOrEmpty(text)) return null;
                        
                        var textHash = GetMd5Hash(text);
                        if (textHash == _lastHash) return null;
                        _lastHash = textHash;
                        return ProcessText(text);
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.AddLog("错误", $"获取剪贴板内容失败: {ex.Message}");
            }

            return null;
        }

        public async Task<bool> SetContentAsync(ClipboardData data, bool isServerUpdate = true)
        {
            try
            {
                _isSettingClipboard = true;
                _isServerUpdate = isServerUpdate;

                if (App.Clipboard != null)
                {
                    var clipboard = App.Clipboard;
                    switch (data.Type)
                    {
                        case ClipboardType.Text:
                            var text = System.Text.Encoding.UTF8.GetString(data.Data);
                            await clipboard.SetTextAsync(text);
                            _lastHash = GetMd5Hash(text);
                            data.Description = "文本: " + (text.Length > 30 ? text[..30] + "..." : text);
                            LogService.Instance.AddLog("已接收", data.Description);
                            break;

                        case ClipboardType.File:
                        case ClipboardType.FileList:
                            if (data.FilenameList.Count > 0)
                            {
                                data.Description = $"{data.FilenameList.Count} 个文件: {string.Join(", ", data.FilenameList.Select(path => Path.GetFileName(path.TrimEnd('\\'))).Take(5))}";
                                var dataObject = await CreateDataObject(data);
                                if (dataObject != null)
                                {
                                    await clipboard.SetDataObjectAsync(dataObject);
                                    _lastHash = GetMd5Hash(string.Join("|", data.FilenameList));
                                    LogService.Instance.AddLog("已接收", data.Description);
                                }
                                else
                                {
                                    LogService.Instance.AddLog("错误", "没有可用的文件可以设置到剪贴板");
                                    return false;
                                }
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
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

        private async Task<IEnumerable<IStorageItem>> GetStorageItems(IEnumerable<string> paths)
        {
            var storageItems = new List<IStorageItem>();
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var provider = desktop.MainWindow?.StorageProvider;
                if (provider != null)
                {
                    foreach (var path in paths)
                    {
                        try
                        {
                            var localPath = new Uri(path).LocalPath;
                            IStorageItem? item = null;

                            if (Directory.Exists(localPath))
                            {
                                item = await provider.TryGetFolderFromPathAsync(localPath);
                            }
                            else if (File.Exists(localPath))
                            {
                                item = await provider.TryGetFileFromPathAsync(localPath);
                            }

                            if (item != null)
                            {
                                storageItems.Add(item);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogService.Instance.AddLog("警告", $"处理文件路径失败: {path} - {ex.Message}");
                        }
                    }
                }
            }
            return storageItems;
        }

        private async Task<DataObject?> CreateDataObject(ClipboardData data)
        {
            var dataObject = new DataObject();
            
            // 设置纯文本格式
            dataObject.Set("Text", Encoding.UTF8.GetBytes(string.Join('\n', data.FilenameList)));
            
            // 设置URI列表格式
            var uriEnum = data.FilenameList.Select(file => new Uri(file).GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped));
            var uris = string.Join("\n", uriEnum);
            dataObject.Set("text/uri-list", Encoding.UTF8.GetBytes(uris));
            
            // 设置GNOME格式
            var nautilus = $"x-special/nautilus-clipboard\ncopy\n{uris}\n";
            dataObject.Set(FileFormat, Encoding.UTF8.GetBytes(nautilus));
            
            return dataObject;
        }

        private async Task<ClipboardData?> ProcessFiles(IEnumerable<IStorageItem> files)
        {
            var fileList = files.ToList();
            if (!fileList.Any()) return null;

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
                Description = $"{items.Count} 个文件: {string.Join(", ", items.Select(path => Path.GetFileName(path.Path.LocalPath.TrimEnd('\\'))).Take(5))}"
            };
        }

        private ClipboardData? ProcessText(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;

            return new ClipboardData
            {
                Type = ClipboardType.Text,
                Text = text,
                Description = "文本: " + (text.Length > 30 ? text[..30] + "..." : text)
            };
        }

        private string GetMd5Hash(string input)
        {
            using var md5Hash = System.Security.Cryptography.MD5.Create();
            var bytes = md5Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).ToLower();
        }

        public void Initialize()
        {
           
        }

        public void Cleanup()
        {
            
        }
    }
} 