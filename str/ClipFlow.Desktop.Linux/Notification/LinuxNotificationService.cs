using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ClipFlow.Desktop.Services;
using ClipFlow.Interfaces;
using ClipFlow.Services;

namespace ClipFlow.Desktop.Linux.Notification
{
    public class LinuxNotificationService : INotification
    {
        private bool _isInitialized;
        private const string APP_NAME = "ClipFlow";
        private string? _notifySendPath;

        public void Initialize()
        {
            try
            {
                // 检查是否安装了 notify-send
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "notify-send",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (process != null)
                {
                    _notifySendPath = process.StandardOutput.ReadToEnd()?.Trim();
                    process.WaitForExit();
                    _isInitialized = process.ExitCode == 0 && !string.IsNullOrEmpty(_notifySendPath);

                    if (!_isInitialized)
                    {
                        FileLogService._.Error("未找到notify-send命令，请安装libnotify-bin包");
                    }
                    else
                    {
                        // 测试通知
                        using var testProcess = Process.Start(new ProcessStartInfo
                        {
                            FileName = _notifySendPath,
                            Arguments = $"--app-name=\"{APP_NAME}\" \"初始化成功\" \"通知服务已准备就绪\" --icon=dialog-information",
                            UseShellExecute = false,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        });

                        if (testProcess != null)
                        {
                            var error = testProcess.StandardError.ReadToEnd();
                            testProcess.WaitForExit();

                            if (!string.IsNullOrEmpty(error))
                            {
                                _isInitialized = false;
                                FileLogService._.Error($"通知服务测试失败: {error}");
                            }
                            else
                            {
                                FileLogService._.Info("Linux通知服务初始化成功");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FileLogService._.Error("初始化 Linux 通知失败", ex);
            }
        }

        public async Task ShowNotificationAsync(string title, string message)
        {
            if (!_isInitialized || string.IsNullOrEmpty(_notifySendPath))
            {
                FileLogService._.Error("通知服务未初始化");
                return;
            }

            try
            {
                var escapedTitle = title.Replace("\"", "\\\"");
                var escapedMessage = message.Replace("\"", "\\\"");

                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = _notifySendPath,
                    Arguments = $"--app-name=\"{APP_NAME}\" \"{escapedTitle}\" \"{escapedMessage}\" --icon=dialog-information --urgency=normal",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                });

                if (process != null)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (!string.IsNullOrEmpty(error))
                    {
                        FileLogService._.Error($"显示Linux通知失败: {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                FileLogService._.Error("显示 Linux 通知失败", ex);
            }
        }

        public void Dispose()
        {
            _isInitialized = false;
            _notifySendPath = null;
        }
    }
} 