using Avalonia.Threading;
using ClipFlow.Core.Interfaces;
using ClipFlow.Core.Services;
using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace ClipFlow.Infrastructure.Platforms.Windows
{
    //[SupportedOSPlatform("windows10.0.19041.0")]
    public class WindowsNotificationService : INotificationService
    {
        private bool _isInitialized;
        private const string APP_NAME = "ClipFlow";


        public void Initialize()
        {

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

           
        }

        public void Dispose()
        {
            try
            {
                if (_isInitialized)
                {
                  
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