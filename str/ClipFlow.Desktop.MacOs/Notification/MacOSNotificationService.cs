using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ClipFlow.Desktop.Services;
using ClipFlow.Interfaces;
using ClipFlow.Services;

namespace ClipFlow.Desktop.MacOs.Notification
{
    public class MacOSNotificationService : INotification
    {
        private bool _isInitialized;
        private const string APP_NAME = "ClipFlow";

        public void Initialize()
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "osascript",
                    Arguments = $"-e 'display notification \"正在初始化...\" with title \"{APP_NAME}\" subtitle \"欢迎使用\"'",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                });

                if (process != null)
                {
                    process.WaitForExit();
                    var error = process.StandardError.ReadToEnd();

                    if (string.IsNullOrEmpty(error))
                    {
                        _isInitialized = true;
                        FileLogService._.Info("macOS通知服务初始化成功");
                    }
                    else
                    {
                        if (error.Contains("not allowed"))
                        {
                            FileLogService._.Error("ClipFlow没有通知权限，请在系统偏好设置 > 通知与专注模式中授予权限");
                        }
                        else
                        {
                            FileLogService._.Error($"macOS通知初始化失败: {error}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FileLogService._.Error("初始化 macOS 通知失败", ex);
            }
        }

        public async Task ShowNotificationAsync(string title, string message)
        {
            if (!_isInitialized)
            {
                FileLogService._.Error("通知服务未初始化");
                return;
            }

            try
            {
                var escapedTitle = title.Replace("\"", "\\\"").Replace("'", "\\'");
                var escapedMessage = message.Replace("\"", "\\\"").Replace("'", "\\'");

                var script = $@"
                    display notification ""{escapedMessage}"" with title ""{APP_NAME}"" subtitle ""{escapedTitle}""
                    sound name ""Submarine""
                ";

                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "osascript",
                    Arguments = $"-e '{script}'",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                });

                if (process != null)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (!string.IsNullOrEmpty(error))
                    {
                        if (error.Contains("not allowed"))
                        {
                            FileLogService._.Error("ClipFlow没有通知权限，请在系统偏好设置 > 通知与专注模式中授予权限");
                        }
                        else
                        {
                            FileLogService._.Error($"显示macOS通知失败: {error}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FileLogService._.Error("显示 macOS 通知失败", ex);
            }
        }

        public void Dispose()
        {
            _isInitialized = false;
        }
    }
} 