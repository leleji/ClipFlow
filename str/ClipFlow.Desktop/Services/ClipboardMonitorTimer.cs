using Avalonia.Threading;
using ClipFlow.Desktop.Models;
using System;
using System.Threading.Tasks;
using System.Timers;
using ClipFlow.Desktop.Interfaces;

namespace ClipFlow.Desktop
{
    public class ClipboardMonitorTimer : IClipboardMonitor
    {
        private readonly IClipboardHandler _clipboardHandler;
        private bool _isMonitoring;
        private Timer? _timer;

        public event IClipboardMonitor.ClipboardChangedEventHandler? OnClipboardChanged;

        public ClipboardMonitorTimer(IClipboardHandler clipboardHandler)
        {
            _clipboardHandler = clipboardHandler;
        }

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
            _clipboardHandler.Initialize();
        }

        public void Stop()
        {
            _isMonitoring = false;
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
            _clipboardHandler.Cleanup();
        }

        private async Task CheckClipboardContent()
        {
            if (!_isMonitoring) return;
            var clipData =await _clipboardHandler.GetContentAsync();
            if (clipData != null)
            {
                OnClipboardChanged?.Invoke(clipData);
            }
        }

        public async Task<bool> SetClipboardContentAsync(ClipboardData data, bool isServerUpdate = true)
        {
            return await _clipboardHandler.SetContentAsync(data, isServerUpdate);
        }

        public void Dispose()
        {
            Stop();
            if (_clipboardHandler is IDisposable disposable)
            {
                disposable.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}