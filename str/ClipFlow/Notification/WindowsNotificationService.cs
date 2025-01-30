using System;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using System.Runtime.Versioning;
using Avalonia.Threading;
using ClipFlow.Services;

namespace ClipFlow.Notification
{
    [SupportedOSPlatform("windows10.0.10240")]
    public class WindowsNotificationService : INotificationService
    {
        private ToastNotifier? _notifier;
        private bool _isInitialized;

        public void Initialize()
        {
            try
            {
                _notifier = ToastNotificationManager.CreateToastNotifier("ClipFlow");

                try
                {
                    ToastNotificationManager.History.Clear();
                }
                catch (Exception ex)
                {
                    FileLogService._.Error("清理旧通知失败，但不影响使用", ex);
                }

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                FileLogService._.Error("初始化 Windows 通知失败", ex);
            }
        }

        public async Task ShowNotificationAsync(string title, string message)
        {
            if (!_isInitialized || _notifier == null)
            {
                FileLogService._.Error("通知服务未初始化");
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
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

                    _notifier.Show(toast);
                }
                catch (Exception ex)
                {
                    FileLogService._.Error("显示 Windows 通知失败", ex);
                }
            });
        }

        public void Dispose()
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
    }
}