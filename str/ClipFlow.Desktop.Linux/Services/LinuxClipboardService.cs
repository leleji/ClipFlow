using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using ClipFlow.Core.Constants;
using ClipFlow.Core.Interfaces;
using ClipFlow.Core.Services;
using ClipFlow.Core.Utilities;
using ClipFlow.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace ClipFlow.Core.Linux.Services
{
    public class LinuxClipboardService : IClipboardHandler
    {

        private string? _lastHash;
        private bool _isSettingClipboard;
        private bool _isServerUpdate;

        private IClipboard? _clipboard;

        public async Task<ClipboardData?> GetContentAsync()
        {
            
            if (_isSettingClipboard) return null;
            _isSettingClipboard=true;
            try
            {
                if (_clipboard != null)
                {
                    var formats = await _clipboard.GetFormatsAsync();

                    if (formats.Contains(ClipboardFormat.LinuxFile))
                    {
                        var bytes = await _clipboard.GetDataAsync(ClipboardFormat.LinuxFile) as byte[];
                        if (bytes != null)
                        {
                            var content =System.Web.HttpUtility.UrlDecode( Encoding.UTF8.GetString(bytes));
                            var pathList = content.Split(["\r\n", "\r", "\n"], StringSplitOptions.None)
                                .Select(v => v.Trim().Replace("file://", ""))
                                .Where(x => !string.IsNullOrEmpty(x))
                                .ToList();
                            var filesHash = ClipboardUtils.GetMd5Hash(string.Join("|", pathList));
                            if (filesHash == _lastHash) return null;
                            _lastHash = filesHash;
                            if (pathList.Count == 1 && !Directory.Exists(pathList[0]))
                            {
                                return ClipboardProcess.ProcessSingleFile(pathList[0]);
                            }
                            return ClipboardProcess.ProcessMultipleItems(pathList);
                        }

                    }
                    else if (formats.Contains(ClipboardFormat.ImagePng) || formats.Contains(ClipboardFormat.ImageJpegt) || formats.Contains(ClipboardFormat.ImageBmp))
                    {
                        var imgformat = formats.FirstOrDefault(f => f.StartsWith("image/"));
                        if (!string.IsNullOrEmpty(imgformat))
                        {
                            var imageData = await _clipboard.GetDataAsync(imgformat) as byte[];
                            if (imageData != null && imageData.Length > 0)
                            {
                                var textHash = ClipboardUtils.GetMd5Hash(imageData);
                                if (textHash == _lastHash) return null;
                                _lastHash = textHash;
                                string extension = imgformat.Split('/')[1]; // png, jpeg, bmp
                                var tempPath = Path.Combine(Path.GetTempPath(), $"ClipFlow{Path.DirectorySeparatorChar}{Guid.NewGuid()}.{extension}");
                                try
                                {
                                    File.WriteAllBytes(tempPath, imageData);
                                    Console.WriteLine($"图片已保存为: {Path.GetFullPath(tempPath)}");
                                    return ClipboardProcess.ProcessSingleFile(tempPath, "");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"保存图片失败: {ex.Message}");
                                }
                            }
                        }
                    }else if (formats.Contains("UTF8_STRING") || formats.Contains(ClipboardFormat.Text) || formats.Contains("STRING"))
                    {
                        var text = await _clipboard.GetTextAsync();
                        if (string.IsNullOrEmpty(text)) return null;

                        var textHash = ClipboardUtils.GetMd5Hash(text);
                        if (textHash == _lastHash) return null;
                        _lastHash = textHash;
                        return ClipboardProcess.ProcessText(text);
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.AddLog("错误", $"获取剪贴板内容失败: {ex.Message}");
            }finally
            {
                _isSettingClipboard = false;
            }
            return null;
        }

        public async Task<bool> SetContentAsync(ClipboardData data, bool isServerUpdate = true)
        {
            try
            {
                _isSettingClipboard = true;
                _isServerUpdate = isServerUpdate;

                if (_clipboard != null)
                {
                    switch (data.Type)
                    {
                        case ClipboardType.Text:
                            await _clipboard.SetTextAsync(data.Text);
                            _lastHash = ClipboardUtils.GetMd5Hash(data.Text);
                            data.Description = "文本: " + (data.Text.Length > 30 ? data.Text[..30] + "..." : data.Text);
                            LogService.Instance.AddLog("已接收", data.Description);
                            break;

                        case ClipboardType.File:
                        case ClipboardType.FileList:
                            if (data.CopyFiles.Count > 0)
                            {
                                data.Description = $"{data.CopyFiles.Count} 个文件: {string.Join(", ", data.CopyFiles.Cast<string>().Select(path => System.IO.Path.GetFileName(path.TrimEnd('\\'))).Take(5))}";
                                var dataObject = CreateDataObject(data);
                                await _clipboard.SetDataObjectAsync(dataObject);
                                _lastHash = ClipboardUtils.GetMd5Hash(string.Join("|", data.CopyFiles));
                                LogService.Instance.AddLog("已接收", data.Description);
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



        private DataObject CreateDataObject(ClipboardData data)
        {
            var dataObject = new DataObject();

            // 设置纯文本格式
            dataObject.Set(ClipboardFormat.Text, Encoding.UTF8.GetBytes(string.Join('\n', data.CopyFiles)));

            // 设置URI列表格式
            var uriEnum = data.CopyFiles.Cast<string>().Select(file => new Uri(file).GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped));
            var uris = string.Join("\n", uriEnum);
            dataObject.Set(ClipboardFormat.LinuxFile, Encoding.UTF8.GetBytes(uris));

            // 设置GNOME格式
            var nautilus = $"x-special/nautilus-clipboard\ncopy\n{uris}\n";
            dataObject.Set(ClipboardFormat.GnomeFiles, Encoding.UTF8.GetBytes(nautilus));

            return dataObject;
        }



        public void Initialize()
        {
            if (_clipboard==null)
            {
                // 创建一个隐藏的顶级窗口
                var hiddenWindow = new Window
                {
                    Width = 0,
                    Height = 0,
                    IsVisible = false
                };
                // 获取剪贴板实例
                if (hiddenWindow.Clipboard!=null)
                {
                    _clipboard = hiddenWindow.Clipboard;
                }
                else
                {
                    LogService.Instance.AddLog("警告", $"剪贴板初始化错误。");
                }
            }
        }

        public void Cleanup()
        {
            
        }
    }
} 