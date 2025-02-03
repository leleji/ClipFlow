using System;
using System.Threading.Tasks;
using ClipFlow.Desktop.Services;
using ClipFlow.Interfaces;

namespace ClipFlow.Desktop.Services
{


    public class NotificationService : IDisposable
    {
        private static NotificationService? _instance;
        private static readonly object _lock = new object();

        public static NotificationService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new NotificationService();
                    }
                }
                return _instance;
            }
        }

        private INotification? _platformService;
        private bool _isDisposed;

        private NotificationService()
        {
            FileLogService._.Info("通知服务实例已创建");
        }

        public static void RegisterPlatformService(INotification service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            lock (_lock)
            {
                if (Instance._platformService != null)
                {
                    FileLogService._.Info("正在替换现有的通知服务");
                    Instance._platformService.Dispose();
                }

                Instance._platformService = service;
                FileLogService._.Info($"已注册平台通知服务: {service.GetType().Name}");
            }
        }

        public async Task ShowNotificationAsync(string title, string message)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(NotificationService));
            }

            if (_platformService == null)
            {
                FileLogService._.Error("通知服务未初始化，请确保已调用 RegisterPlatformService");
                return;
            }

            try
            {
                await _platformService.ShowNotificationAsync(title, message);
            }
            catch (Exception ex)
            {
                FileLogService._.Error($"显示通知失败: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                FileLogService._.Info("正在释放通知服务");

                if (_platformService != null)
                {
                    _platformService.Dispose();
                    _platformService = null;
                }

                _isDisposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}