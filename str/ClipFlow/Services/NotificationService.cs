using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Win32;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ClipFlow.Services
{
    public class NotificationService : IDisposable
    {
        private static NotificationService? _instance;
        public static NotificationService Instance => _instance ??= new NotificationService();

        private readonly bool _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private readonly bool _isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        private readonly bool _isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        private bool _isWindowsNotificationEnabled = false;

        private NotificationService()
        {
            if (_isWindows)
            {
                try
                {
                    
                    // 尝试初始化通知
                    var notifier = ToastNotificationManager.CreateToastNotifier("ClipFlow");
                    _isWindowsNotificationEnabled = true;

                    try
                    {
                        // 清理旧的通知，但不影响服务的可用性
                        ToastNotificationManager.History.Clear();
                    }
                    catch (Exception ex)
                    {
                        FileLogService._.Error("清理旧通知失败，但不影响使用", ex);
                    }
                }
                catch (Exception ex)
                {
                    FileLogService._.Error("初始化 Windows 通知失败，将使用备用通知方式", ex);
                    _isWindowsNotificationEnabled = false;
                }
            }
            else if (_isMacOS)
            {
                InitializeMacOSNotifications();
            }
            else if (_isLinux)
            {
                InitializeLinuxNotifications();
            }
        }


        private void CreateShortcut(string shortcutPath)
        {
            try
            {
                //var shell = new IWshRuntimeLibrary.WshShell();
                //var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutPath);
                //shortcut.TargetPath = Process.GetCurrentProcess().MainModule?.FileName;
                //shortcut.Description = "ClipFlow";
                //shortcut.Save();
            }
            catch (Exception ex)
            {
                FileLogService._.Error("创建快捷方式失败", ex);
                throw;
            }
        }

        [SupportedOSPlatform("windows")]
        private async Task ShowWindowsNotificationAsync(string title, string message)
        {
            if (!_isWindowsNotificationEnabled)
            {
                // 如果 Windows 通知不可用，使用备用方案
                await ShowFallbackNotificationAsync(title, message);
                return;
            }

            try
            {
                var template = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
                var textNodes = template.GetElementsByTagName("text");
                textNodes[0].AppendChild(template.CreateTextNode(title));
                textNodes[1].AppendChild(template.CreateTextNode(message));

                var toast = new ToastNotification(template)
                {
                    ExpirationTime = DateTime.Now.AddSeconds(5)
                };

                var notifier = ToastNotificationManager.CreateToastNotifier("ClipFlow");
                notifier.Show(toast);
            }
            catch (Exception ex)
            {
                FileLogService._.Error("显示 Windows 通知失败，切换到备用通知", ex);
                await ShowFallbackNotificationAsync(title, message);
            }
        }

        private async Task ShowFallbackNotificationAsync(string title, string message)
        {
            try
            {
                // 使用 Avalonia 的窗口作为备用通知方式
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        var mainWindow = desktop.MainWindow;
                        if (mainWindow != null)
                        {
                            // TODO: 在主窗口显示一个临时的通知提示
                            // 可以使用 Toast 控件或自定义的通知控件
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                FileLogService._.Error("显示备用通知失败", ex);
            }
        }

        public async Task ShowNotificationAsync(string title, string message)
        {
            if (_isWindows)
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await ShowWindowsNotificationAsync(title, message);
                });
            }
            else if (_isMacOS)
            {
                await ShowMacOSNotificationAsync(title, message);
            }
            else if (_isLinux)
            {
                await ShowLinuxNotificationAsync(title, message);
            }
        }

        private void InitializeMacOSNotifications()
        {
            try
            {
                // macOS 通知实现
                // 使用 terminal-notifier 或 osascript
            }
            catch (Exception ex)
            {
                FileLogService._.Error("初始化 macOS 通知失败", ex);
            }
        }

        private void InitializeLinuxNotifications()
        {
            try
            {
                // 检查是否安装了 notify-send
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "notify-send",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                });
                process?.WaitForExit();
            }
            catch (Exception ex)
            {
                FileLogService._.Error("初始化 Linux 通知失败", ex);
            }
        }

        private async Task ShowMacOSNotificationAsync(string title, string message)
        {
            try
            {
                var escapedTitle = title.Replace("\"", "\\\"");
                var escapedMessage = message.Replace("\"", "\\\"");
                var script = $"display notification \"{escapedMessage}\" with title \"{escapedTitle}\"";

                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "osascript",
                    Arguments = $"-e '{script}'",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });

                if (process != null)
                {
                    await process.WaitForExitAsync();
                }
            }
            catch (Exception ex)
            {
                FileLogService._.Error("显示 macOS 通知失败", ex);
            }
        }

        private async Task ShowLinuxNotificationAsync(string title, string message)
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "notify-send",
                    Arguments = $"\"{title}\" \"{message}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });

                if (process != null)
                {
                    await process.WaitForExitAsync();
                }
            }
            catch (Exception ex)
            {
                FileLogService._.Error("显示 Linux 通知失败", ex);
            }
        }

        public void Dispose()
        {
            if (_isWindows && _isWindowsNotificationEnabled)
            {
                try
                {
                    ToastNotificationManager.History.Clear();
                }
                catch (Exception ex)
                {
                    FileLogService._.Error("清理 Windows 通知失败", ex);
                }
            }
            GC.SuppressFinalize(this);
        }
    }
} 