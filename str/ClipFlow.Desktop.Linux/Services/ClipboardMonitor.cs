using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ClipFlow.Models;
using ClipFlow.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Path = System.IO.Path;
using System.Timers;
using ClipFlow.Desktop.Interfaces;

namespace ClipFlow.Desktop.Win.Services
{
    public class ClipboardMonitor : IClipboardMonitor
    {
        private readonly IClipboardHandler _clipboardHandler;
        private bool _isMonitoring;
        private Timer? _timer;

        public event EventHandler<Exception>? OnError;
        public event IClipboardMonitor.ClipboardChangedEventHandler? OnClipboardChanged;

        public ClipboardMonitor(IClipboardHandler clipboardHandler)
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

            try
            {
                var clipData = await _clipboardHandler.GetContentAsync();
                if (clipData != null)
                {
                    OnClipboardChanged?.Invoke(clipData);
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, ex);
                LogService.Instance.AddLog("错误", $"监控剪贴板失败: {ex.Message}");
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