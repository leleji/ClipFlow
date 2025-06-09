using System;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Runtime.Versioning;
using ClipFlow.Core.Services;
using Avalonia.Threading;
using ClipFlow.Core.Interfaces;

namespace ClipFlow.Core.Windows.Services
{
    [SupportedOSPlatform("windows10.0.19041.0")]
    public class WindowsNotificationService : INotificationService
    {
        private bool _isInitialized;
        private const string APP_NAME = "ClipFlow";
        

        public void Initialize()
        {
            // 清理旧通知
            try
            {
                ToastNotificationManagerCompat.History.Clear();
            }
            catch (Exception ex)
            {
                FileLogService.Instance.Error("清理旧通知失败，但不影响使用", ex);
            }

            _isInitialized = true;
            FileLogService.Instance.Info("Windows通知服务初始化成功");
        }

        public async Task ShowNotificationAsync(string title, string message)
        {
            if (!_isInitialized)
            {
                FileLogService.Instance.Error("通知服务未初始化");
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    new ToastContentBuilder()
                        .AddText(title)
                        .AddText(message)
                        .Show(toast =>
                        {
                            toast.ExpirationTime = DateTime.Now.AddSeconds(5);
                        });
                }
                catch (Exception ex)
                {
                    FileLogService.Instance.Error("显示 Windows 通知失败", ex);
                }
            });
        }

        public void Dispose()
        {
            try
            {
                if (_isInitialized)
                {
                    ToastNotificationManagerCompat.History.Clear();
                }
            }
            catch (Exception ex)
            {
                FileLogService.Instance.Error("清理 Windows 通知失败", ex);
            }
            finally
            {
                _isInitialized = false;
            }
        }
    }
}