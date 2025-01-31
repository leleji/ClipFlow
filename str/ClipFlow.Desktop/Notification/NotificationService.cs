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
using ClipFlow.Desktop.Services;

namespace ClipFlow.Desktop.Notification
{
    public class NotificationService : IDisposable
    {
        private static NotificationService? _instance;
        public static NotificationService Instance => _instance ??= new NotificationService();

        private readonly INotificationService _platformService;

        private NotificationService()
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240))
            {
                _platformService = new WindowsNotificationService();
            }
            else if (OperatingSystem.IsMacOS())
            {
                _platformService = new MacOSNotificationService();
            }
            else if (OperatingSystem.IsLinux())
            {
                _platformService = new LinuxNotificationService();
            }
            else
            {
                throw new PlatformNotSupportedException("当前平台不支持通知功能");
            }

            _platformService.Initialize();
        }

        public async Task ShowNotificationAsync(string title, string message)
        {
            await _platformService.ShowNotificationAsync(title, message);
        }

        public void Dispose()
        {
            _platformService.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}