using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ClipFlow.Models;
using ClipFlow.Services;
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

namespace ClipFlow.Clipboard
{
    public class ClipboardMonitor : IDisposable
    {
        private readonly IClipboardHandler _clipboardHandler;
        private bool _isMonitoring;
        private Timer? _timer;
        private const string TempFolderName = "ClipFlow";

        // 错误事件
        public event EventHandler<Exception>? OnError;
        // 剪贴板变化事件委托和事件
        public delegate void ClipboardChangedEventHandler(ClipboardData data);
        public event ClipboardChangedEventHandler? OnClipboardChanged;

        public ClipboardMonitor()
        {
            _clipboardHandler = ClipboardHandlerFactory.Create();
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
        }
    }
}