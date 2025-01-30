using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ClipFlow.Services;

namespace ClipFlow.Notification
{
    public class LinuxNotificationService : INotificationService
    {
        private bool _isInitialized;

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
                    UseShellExecute = false
                });

                if (process != null)
                {
                    process.WaitForExit();
                    _isInitialized = process.ExitCode == 0;

                    if (!_isInitialized)
                    {
                        FileLogService._.Error("未找到notify-send命令，请安装libnotify-bin包");
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
            if (!_isInitialized)
            {
                FileLogService._.Error("通知服务未初始化");
                return;
            }

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
            // Linux通知不需要特别的清理
        }
    }
}