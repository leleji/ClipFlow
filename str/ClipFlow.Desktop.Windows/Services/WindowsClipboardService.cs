using ClipFlow.Desktop.Constants;
using ClipFlow.Desktop.Interfaces;
using ClipFlow.Desktop.Services;
using ClipFlow.Desktop.Utilities;
using ClipFlow.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;


namespace ClipFlow.Desktop.Windows.Services
{
    public class WindowsClipboardService : IClipboardHandler
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);




        private string? _lastHash;
        private bool _isSettingClipboard;
        private bool _isServerUpdate;

        public async Task<ClipboardData?> GetContentAsync()
        {
            if (_isSettingClipboard) return null;

            try
            {
                //包含FileDrop的都是文件，包括有些图片
                if (Clipboard.ContainsFileDropList())
                {
                    var list = new List<string>(Clipboard.GetFileDropList().Cast<string>());
                    var filesHash = ClipboardUtils.GetMd5Hash(string.Join("|", list));
                    if (filesHash == _lastHash) return null;
                    _lastHash = filesHash;
                    if (list.Count == 1 && !Directory.Exists(list[0]))
                    {
                        return ClipboardProcess.ProcessSingleFile(list[0], GetProcessName());
                    }
                    return ClipboardProcess.ProcessMultipleItems(list, GetProcessName());

                }else if (Clipboard.ContainsText())
                {
                    var text = Clipboard.GetText();
                    if (string.IsNullOrEmpty(text)) return null;
                    var textHash = ClipboardUtils.GetMd5Hash(text);
                    if (textHash == _lastHash) return null;
                    _lastHash = textHash;
                    return ClipboardProcess.ProcessText(text, GetProcessName());
                }
                else if (Clipboard.ContainsImage())
                {
                    if (Clipboard.ContainsData(ClipboardFormat.Html)) {
                        var format = Clipboard.GetData(ClipboardFormat.Html)?.ToString();
                        var textHash = ClipboardUtils.GetMd5Hash(format);
                        if (textHash == _lastHash) return null;
                        _lastHash = textHash;
                        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
                        //本地没有存储的图片
                        using var img = Clipboard.GetImage();
                        img?.Save(tempPath, ImageFormat.Png);
                        return ClipboardProcess.ProcessSingleFile(tempPath, GetProcessName());
                    }
                    else
                    {
                        var formats = Clipboard.GetDataObject()?.GetFormats();
                        FileLogService.Instance.Error($"图片无法生成Hash:{string.Join(",", formats)}");
                    }
                }
                else
                {
                    var formats = Clipboard.GetDataObject()?.GetFormats();
                    FileLogService.Instance.Error($"无法获取剪贴板数据对象:{string.Join(",", formats)}");
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.AddLog("错误", $"获取剪贴板内容失败: {ex.Message}");
            }

            return null;
        }

        public string GetProcessName()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow(); // 获取当前窗口句柄
                GetWindowThreadProcessId(hwnd, out uint processId); // 获取进程ID
                Process process = Process.GetProcessById((int)processId); // 通过ID获取进程
                return process.ProcessName;
            }
            catch (Exception ex)
            {
                FileLogService.Instance.Error("获取进程信息异常",ex);
                return string.Empty;
            }
        }

        public async Task<bool> SetContentAsync(ClipboardData data, bool isServerUpdate = true)
        {
            try
            {
                _isSettingClipboard = true;
                _isServerUpdate = isServerUpdate;
                switch (data.Type)
                {
                    case ClipboardType.Text:
                        Clipboard.SetText(data.Text);
                        _lastHash = ClipboardUtils.GetMd5Hash(data.Text);
                        data.Description = "文本: " + (data.Text.Length > 30 ? data.Text[..30] + "..." : data.Text);
                        LogService.Instance.AddLog("已接收", data.Description);
                        break;
                    case ClipboardType.File:
                    case ClipboardType.FileList:
                        data.Description = $"{data.CopyFiles.Count} 个文件: {string.Join(", ", data.CopyFiles.Cast<string>()
                           .Select(path => Path.GetFileName(path.TrimEnd('\\')))
                           .Take(5))}";
                        _lastHash = ClipboardUtils.GetMd5Hash(string.Join("|", data.CopyFiles.Cast<string>()));
                        Clipboard.SetFileDropList(data.CopyFiles);
                        LogService.Instance.AddLog("已接收", data.Description);
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
                _ = Task.Delay(1000).ContinueWith(_ =>
                {
                    _isServerUpdate = false;
                });
            }
            return true;
        }

  



        public void Initialize()
        {
            
        }

        public void Cleanup()
        {
            
        }
    }
} 