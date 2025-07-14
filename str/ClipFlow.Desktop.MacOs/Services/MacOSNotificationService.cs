using System;
using System.Threading.Tasks;
using ClipFlow.Core.Interfaces;
using ClipFlow.Core.Services;
using Foundation;

namespace ClipFlow.Desktop.MacOs.Services;

public class MacOSNotificationService : INotificationService
{
    private bool _isInitialized;
    private NSUserNotificationCenter? _notificationCenter;

    public void Initialize()
    {
        _notificationCenter = NSUserNotificationCenter.DefaultUserNotificationCenter;

        if (_notificationCenter != null)
        {
            _isInitialized = true;
            FileLogService.Instance.Info("macOS通知服务初始化成功");
        }
        else
        {
            FileLogService.Instance.Error("macOS通知服务初始化失败");
        }
    }

    public async Task ShowNotificationAsync(string title, string message)
    {
        if (!_isInitialized)
        {
            FileLogService.Instance.Error("通知服务未初始化");
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
                ActionButtonTitle = "确定"
            };
            _notificationCenter.ShouldPresentNotification = (c, n) => true; // 允许前台通知
            _notificationCenter.DeliverNotification(notification);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            FileLogService.Instance.Error("显示 macOS 通知失败", ex);
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