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
using System.Threading.Tasks;

namespace ClipFlow.Desktop.MacOS.Services
{
    public class MacOSClipboardService : IClipboardHandler
    {
        private const string FileFormat = "public.file-url";
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
                        var clipboardFiles = await clipboard.GetDataAsync("Files") as IEnumerable<IStorageItem>;
                        if (clipboardFiles != null)
                        {
                            var filesHash = GetMd5Hash(string.Join("|", clipboardFiles.Select(f => f.Path.LocalPath)));
                            if (filesHash == _lastHash) return null;
                            _lastHash = filesHash;
                            return await ProcessFiles(clipboardFiles);
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

        private async Task<DataObject?> CreateDataObject(ClipboardData data)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow?.StorageProvider != null)
            {
                var provider = desktop.MainWindow.StorageProvider;
                var storageItems = new List<IStorageItem>();

                foreach (var file in data.FilenameList)
                {
                    IStorageItem? item = null;
                    if (Directory.Exists(file))
                    {
                        item = await provider.TryGetFolderFromPathAsync(file);
                    }
                    else if (File.Exists(file))
                    {
                        item = await provider.TryGetFileFromPathAsync(file);
                    }
                    else
                    {
                        LogService.Instance.AddLog("错误", $"文件不存在: {file}");
                        continue;
                    }

                    if (item != null)
                    {
                        storageItems.Add(item);
                    }
                }

                if (storageItems.Count > 0)
                {
                    var dataObject = new DataObject();
                    dataObject.Set(FileFormat, storageItems);
                    return dataObject;
                }
            }
            return null;
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