using Avalonia.Threading;
using ClipFlow.Core.Models;
using System;
using System.Threading.Tasks;
using System.Timers;
using ClipFlow.Core.Interfaces;

namespace ClipFlow.Core
{
    public class ClipboardMonitorTimer(IClipboardHandler clipboardHandler) : IClipboardMonitor
    {
        private bool _isMonitoring;
        private bool _isChecking = false;
        private Timer? _timer;

        public event IClipboardMonitor.ClipboardChangedEventHandler? OnClipboardChanged;

        public void Start()
        {
            if (_isMonitoring) return;

            _isMonitoring = true;
            _timer = new Timer(1000); // 每秒检查一次
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
                    OnClipboardChanged?.Invoke(clipData);
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
    }
}