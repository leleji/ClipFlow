using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using ClipFlow.Core.Interfaces;
using ClipFlow.Core.Services;
using ClipFlow.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AppKit;
using ClipFlow.Core.Utilities;
using Foundation;
using ObjCRuntime;

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
            // if (_isSettingClipboard) return null;
            //NSApplication.Init();
            try
            {
                var pasteboard = NSPasteboard.GeneralPasteboard;
                var types = pasteboard.Types;
                if (types.Contains(NSPasteboard.NSFilenamesType))
                {
                    var files = pasteboard.GetPropertyListForType(NSPasteboard.NSFilenamesType) as NSArray;
                    var filesHash = GetMd5Hash(string.Join("|", files.Select(f => f)));
                    if (filesHash == _lastHash) return null;
                    _lastHash = filesHash;
                    return ProcessFiles(files.Cast<NSString>().Select(ns => ns.ToString()).ToList());
                }
                else if (types.Contains(NSPasteboard.NSStringType))
                {
                    var text = pasteboard.GetDataForType(NSPasteboard.NSStringType).ToString();
                    if (string.IsNullOrEmpty(text)) return null;
                    var textHash = GetMd5Hash(text);
                    if (textHash == _lastHash) return null;
                    _lastHash = textHash;
                    return ProcessText(text);
                }
                else if (types.Contains(NSPasteboard.NSPictType) || types.Contains(NSPasteboard.NSTiffType))
                {
                    // 从剪贴板获取图片
                    var images = pasteboard.ReadObjectsForClasses(new Class[] { new Class(typeof(NSImage)) }, null);
                    if (images != null && images.Length > 0)
                    {
                        NSImage image = images[0] as NSImage;
                        if (image != null)
                        {
                            string savePath = Path.Combine(Path.GetTempPath(), $"ClipFlow/{Guid.NewGuid()}.png");
                            var textHash = GetMd5Hash(savePath);
                            if (textHash == _lastHash) return null;
                            if (SaveImage(image, savePath))
                            {
                                Console.WriteLine($"Image saved to: {savePath}");
                                _lastHash = textHash;
                                return ProcessSingleFile(savePath);
                            }
                            else
                            {
                                Console.WriteLine("Failed to save image.");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("No valid image found on clipboard.");
                    }
                }
        }catch (Exception ex) {
             LogService.Instance.AddLog("错误", $"获取剪贴板内容失败: {ex.Message}"); 
            }

        return null;
    }
        /// <summary>
        /// 将 NSImage 保存为 PNG 文件
        /// </summary>
        public static bool SaveImage(NSImage image, string filePath)
        {
            if (image == null) return false;

            var tiffData = image.AsTiff();
            if (tiffData == null) return false;

            var bitmap = new NSBitmapImageRep(tiffData);
            var pngData = bitmap.RepresentationUsingTypeProperties(NSBitmapImageFileType.Png);
            if (pngData == null) return false;

            return pngData.Save(filePath, true);
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

        private ClipboardData? ProcessFiles(IEnumerable<string> files)
        {
            var fileList = files.ToList();
            if (!files.Any()) return null;
            return fileList.Count == 1 && !Directory.Exists(fileList[0])
                ?  ProcessSingleFile(fileList[0])
                :  ProcessMultipleItems(fileList);
        }

        private ClipboardData ProcessSingleFile(string file)
        {
            FileInfo fileInfo = new FileInfo(file);
            return new ClipboardData
            {
                Type = ClipboardType.File,
                FileName = fileInfo.Name,
                FilenameList = new List<string> { file },
                DataLength = (ulong)file.Length,
                Description = $"单文件: {fileInfo.Name}"
            };
        }

        private ClipboardData ProcessMultipleItems(List<string> files)
        {
            ulong totalSize = (ulong)ClipboardUtils.GetTotalSize(files);
            return new ClipboardData
            {
                Type = ClipboardType.FileList,
                FilenameList = files,
                FileName = $"files_{DateTime.Now:yyyyMMddHHmmss}.zip",
                DataLength = totalSize,
                Description = $"{files.Count} 个文件: {string.Join(", ", files.Select(path => Path.GetFileName(path.TrimEnd('\\'))).Take(5))}"
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