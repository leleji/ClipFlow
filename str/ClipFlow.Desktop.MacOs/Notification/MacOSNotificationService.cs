using System;
using System.Threading.Tasks;
using AppKit;
using Foundation;
using ClipFlow.Desktop.Services;
using ClipFlow.Interfaces;
using ClipFlow.Services;

namespace ClipFlow.Desktop.MacOs.Notification
{
    public class MacOSNotificationService : INotification
    {
        private bool _isInitialized;
        private NSUserNotificationCenter _notificationCenter;

        public void Initialize()
        {
            try
            {
                _notificationCenter = NSUserNotificationCenter.DefaultUserNotificationCenter;
                
                if (_notificationCenter != null)
                {
                    _isInitialized = true;
                    FileLogService._.Info("macOS通知服务初始化成功");
                }
                else
                {
                    FileLogService._.Error("macOS通知服务初始化失败");
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
                var notification = new NSUserNotification
                {
                    Title = title,
                    InformativeText = message,
                    DeliveryDate = NSDate.Now,
                    HasActionButton = true,
                    ActionButtonTitle = "确定",
                };
                _notificationCenter.ShouldPresentNotification = (c, n) => true;  // 允许前台通知
                _notificationCenter.DeliverNotification(notification);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                FileLogService._.Error("显示 macOS 通知失败", ex);
            }
        }

        public void Dispose()
        {
            if (_notificationCenter != null)
            {
                _notificationCenter.Dispose();
                _notificationCenter = null;
            }
            _isInitialized = false;
        }
    }
} 