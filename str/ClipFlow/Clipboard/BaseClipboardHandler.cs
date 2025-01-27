using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using ClipFlow.Models;
using ClipFlow.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClipFlow.Clipboard
{
    public abstract class BaseClipboardHandler : IClipboardHandler
    {
        protected readonly ClipboardTextHandler _textHandler;
        protected readonly ClipboardFileHandler _fileHandler;
        protected bool _isSettingClipboard;
        protected bool _isServerUpdate;
        private string? _lastHash;

        protected BaseClipboardHandler()
        {
            _textHandler = new ClipboardTextHandler();
            _fileHandler = new ClipboardFileHandler();
        }

        public virtual void Initialize() 
        {
            _lastHash = null;
        }

        public virtual void Cleanup() { }

        protected abstract string FileFormat { get; }

        public async Task<bool> SetContentAsync(ClipboardData data, bool isServerUpdate = true)
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
                            var text = System.Text.Encoding.UTF8.GetString(data.Data);
                            await clipboard.SetTextAsync(text);
                            _lastHash = ClipboardUtils.GetMd5Hash(text);
                            LogService.Instance.AddLog("已接收", data.Description);
                            break;

                        case ClipboardType.File:
                        case ClipboardType.FileList:
                            if (data.FilenameList.Count > 0)
                            {
                                var provider = desktop.MainWindow.StorageProvider;
                                var storageItems = new List<IStorageItem>();

                                foreach (var file in data.FilenameList)
                                {
                                    try
                                    {
                                        IStorageItem? item = null;
                                        if (System.IO.Directory.Exists(file))
                                        {
                                            item = await provider.TryGetFolderFromPathAsync(file);
                                        }
                                        else if (System.IO.File.Exists(file))
                                        {
                                            item = await provider.TryGetFileFromPathAsync(file);
                                        }
                                        else
                                        {
                                            LogService.Instance.AddLog("警告", $"文件不存在: {file}");
                                            continue;
                                        }

                                        if (item != null)
                                        {
                                            storageItems.Add(item);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        LogService.Instance.AddLog("警告", $"处理文件失败: {file} - {ex.Message}");
                                    }
                                }

                                if (storageItems.Count > 0)
                                {
                                    var dataObject = new DataObject();
                                    dataObject.Set(FileFormat, storageItems);
                                    await clipboard.SetDataObjectAsync(dataObject);
                                    _lastHash = ClipboardUtils.GetMd5Hash(string.Join("|", data.FilenameList));
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

        public async Task<ClipboardData?> GetContentAsync()
        {
            if (_isSettingClipboard) return null;

            try
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                    desktop.MainWindow?.Clipboard != null)
                {
                    var clipboard = desktop.MainWindow.Clipboard;
                    var formats = await clipboard.GetFormatsAsync();

                    if (formats.Contains(FileFormat))
                    {
                        var clipboardFiles = await clipboard.GetDataAsync(FileFormat) as IEnumerable<IStorageItem>;
                        if (clipboardFiles != null)
                        {
                            var filesHash = ClipboardUtils.GetMd5Hash(string.Join("|", clipboardFiles.Select(f => f.Path.LocalPath)));
                            if (filesHash == _lastHash) return null;
                            _lastHash = filesHash;
                            return await _fileHandler.ProcessFiles(clipboardFiles);
                        }
                    }
                    else
                    {
                        var text = await clipboard.GetTextAsync();
                        if (string.IsNullOrEmpty(text)) return null;
                        
                        var textHash = ClipboardUtils.GetMd5Hash(text);
                        if (textHash == _lastHash) return null;
                        _lastHash = textHash;
                        return _textHandler.ProcessText(text);
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.AddLog("错误", $"获取剪贴板内容失败: {ex.Message}");
            }

            return null;
        }
    }
} 