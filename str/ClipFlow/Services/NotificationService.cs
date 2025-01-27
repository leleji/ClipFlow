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


        private NotificationService()
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240))
            {
                try
                {
                    
                    // 尝试初始化通知
                    var notifier = ToastNotificationManager.CreateToastNotifier("ClipFlow");

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
                    FileLogService._.Error("初始化 Windows 通知失败", ex);
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                InitializeMacOSNotifications();
            }
            else if (OperatingSystem.IsLinux())
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

        [SupportedOSPlatform("windows10.0.10240")]
        private async Task ShowWindowsNotificationAsync(string title, string message)
        {
            await Dispatcher.UIThread.InvokeAsync( () =>
            {
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
                    FileLogService._.Error("显示 Windows 通知失败", ex);
                }
            });
           
        }


        public async Task ShowNotificationAsync(string title, string message)
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240))
            {
               
                await ShowWindowsNotificationAsync(title, message);
            }
            else if (OperatingSystem.IsMacOS())
            {
                await ShowMacOSNotificationAsync(title, message);
            }
            else if (OperatingSystem.IsLinux())
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
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240))
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