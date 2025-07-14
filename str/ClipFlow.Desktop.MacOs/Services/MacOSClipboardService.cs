using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AppKit;
using ClipFlow.Core.Constants;
using ClipFlow.Core.Interfaces;
using ClipFlow.Core.Models;
using ClipFlow.Core.Services;
using ClipFlow.Core.Utilities;
using ClipFlow.Desktop.MacOs.Utilities;
using Foundation;
using ObjCRuntime;

namespace ClipFlow.Desktop.MacOS.Services;

public class MacOSClipboardService : IClipboardHandler
{
    private bool _isServerUpdate;
    private bool _isSettingClipboard;

    private string? _lastHash;

    public async Task<ClipboardData?> GetContentAsync()
    {
        if (_isSettingClipboard) return null;
        try
        {
            var pasteboard = NSPasteboard.GeneralPasteboard;
            var types = pasteboard.Types;

            if (types.Contains(ClipboardFormat.MacFile))
            {
                var files = pasteboard.ReadObjectsForClasses([new Class(typeof(NSUrl))],
                        null)
                    .OfType<NSUrl>()
                    .Select(f => f.Path)
                    .ToArray();
                var filesHash = ClipboardUtils.GetMd5Hash(string.Join("|", files));
                if (filesHash == _lastHash) return null;
                _lastHash = filesHash;
                return ClipboardProcess.ProcessFiles(files, Frontmost.GetApplicationName());
            }

            if (types.Contains(ClipboardFormat.MacText))
            {
                var text = pasteboard.GetStringForType(ClipboardFormat.MacText);
                if (string.IsNullOrEmpty(text)) return null;
                var textHash = ClipboardUtils.GetMd5Hash(text);
                if (textHash == _lastHash) return null;
                _lastHash = textHash;
                return ClipboardProcess.ProcessText(text, Frontmost.GetApplicationName());
            }

            if (types.Contains(ClipboardFormat.MacImageJpeg) || types.Contains(ClipboardFormat.MacImagePng) ||
                types.Contains(ClipboardFormat.MacImageTiff))
            {
                var imageData = pasteboard.GetDataForType(ClipboardFormat.MacImagePng);
                var textHash = ClipboardUtils.GetMd5Hash(imageData.ToString());
                if (textHash == _lastHash) return null;
                _lastHash = textHash;
                var savePath = Path.Combine(Path.GetTempPath(), $"ClipFlow/{Guid.NewGuid()}.png");
                if (imageData.Save(new NSUrl($"file://{savePath}"), true))
                    return ClipboardProcess.ProcessSingleFile(savePath, Frontmost.GetApplicationName());
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
            var pasteboard = NSPasteboard.GeneralPasteboard;
            // 清除剪贴板中的现有内容
            pasteboard.ClearContents();
            switch (data.Type)
            {
                case ClipboardType.Text:
                    pasteboard.SetStringForType(data.Text, ClipboardFormat.MacText);
                    _lastHash = ClipboardUtils.GetMd5Hash(data.Text);
                    data.Description = "文本: " + (data.Text.Length > 30 ? data.Text[..30] + "..." : data.Text);
                    LogService.Instance.AddLog("已接收", data.Description);
                    break;
                case ClipboardType.File:
                case ClipboardType.FileList:
                    if (data.CopyFiles.Count > 0)
                    {
                        data.Description =
                            $"{data.CopyFiles.Count} 个文件: {string.Join(", ", data.CopyFiles.Cast<string>().Select(path => Path.GetFileName(path.TrimEnd('\\'))).Take(5))}";
                        var fileUrls = data.CopyFiles.Cast<string>()
                            .Select(path => new NSUrl(path, false) as INSPasteboardWriting)
                            .ToArray();
                        var success = pasteboard.WriteObjects(fileUrls);
                        _lastHash = ClipboardUtils.GetMd5Hash(string.Join("|", data.CopyFiles.Cast<string>()));
                        LogService.Instance.AddLog("已接收", data.Description);
                    }

                    break;
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
            _ = Task.Delay(1000).ContinueWith(_ => { _isServerUpdate = false; });
        }

        return true;
    }

    public void Initialize()
    {
        if (!AXIsProcessTrusted())
        {
            Console.WriteLine("当前应用没有辅助功能权限！请前往系统偏好设置 -> 安全性与隐私 -> 隐私 -> 辅助功能，启用权限。");
            Process.Start("open", "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility");
        }
    }

    public void Cleanup()
    {
    }

    [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
    public static extern bool AXIsProcessTrusted();
}