using Avalonia.Threading;
using ClipFlow.Core.Interfaces;
using ClipFlow.Core.Models;
using System;
using System.Threading.Tasks;
using System.Timers;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ClipFlow.Infrastructure.Platforms.General
{
    public class ClipboardMonitorTimer(IClipboardHandler clipboardHandler) : IClipboardWatcher
    {
        private bool _isMonitoring;
        private bool _isChecking = false;
        private System.Timers.Timer? _timer;

        public event Action<ClipboardData>? ClipboardChanged;

        public void Start()
        {
            if (_isMonitoring) return;

            _isMonitoring = true;
            _timer = new System.Timers.Timer(1000); // 每秒检查一次
            _timer.Elapsed += (s, e) =>
            {
                // 在 UI 线程上执行检查
                Dispatcher.UIThread.Post(async () =>
                {
                    await CheckClipboardContent();
                });
            };
            _timer.Start();
            clipboardHandler.Initialize();
        }

        public void Stop()
        {
            _isMonitoring = false;
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
            clipboardHandler.Cleanup();
        }

        private async Task CheckClipboardContent()
        {
            if (!_isMonitoring || _isChecking) return;
            try
            {
                var clipData = await clipboardHandler.GetContentAsync();
                if (clipData != null)
                {
                    ClipboardChanged?.Invoke(clipData);
                }
            }
            finally
            {
                _isChecking = false;
            }
        }

        public async Task<bool> SetClipboardContentAsync(ClipboardData data, bool isServerUpdate = true)
        {
            return await clipboardHandler.SetContentAsync(data, isServerUpdate);
        }

        public void Dispose()
        {
            Stop();
            if (clipboardHandler is IDisposable disposable)
            {
                disposable.Dispose();
            }
            GC.SuppressFinalize(this);
        }

        public Task<ClipboardData?> GetClipboardContentAsync()
        {
            throw new NotImplementedException();
        }
    }
}