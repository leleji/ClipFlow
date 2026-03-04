using Avalonia.Input.Platform;
using Avalonia.Threading;
using ClipFlow.Common.Helpers;
using ClipFlow.Core.Constants;
using ClipFlow.Core.Interfaces;
using ClipFlow.Core.Models;
using ClipFlow.Core.Services;
using ClipFlow.Core.Utilities;
using System;
using System.Linq;
using System.Text;
using System.Threading;

namespace ClipFlow.Infrastructure.Platforms.Windows
{
    public partial class WindowsClipboardWatcher(IClipboardHandler handler) : IClipboardWatcher
    {
        private IntPtr _hwnd;
        private Thread? _windowThread;
        private readonly AutoResetEvent _windowCreated = new(false);
        private bool _isRunning;
        private bool _isSettingClipboard;
        public event Action<ClipboardData>? ClipboardChanged;



        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;

            _windowThread = new Thread(RunMessageLoop)
            {
                IsBackground = true,
                Name = "ClipboardWatcherLoop"
            };
            _windowThread.Start();

            // 等待窗口句柄创建完成，超时时间 2 秒
            if (!_windowCreated.WaitOne(2000))
            {
                System.Diagnostics.Debug.WriteLine("警告：剪贴板监听窗口创建超时");
            }
        }

        public void Stop()
        {
            _isRunning = false;
            if (_hwnd != IntPtr.Zero)
            {
                // 停止监听
                Win32Native.RemoveClipboardFormatListener(_hwnd);
                // 关键：向消息循环发送退出指令，否则 GetMessage 会一直阻塞线程
                Win32Native.PostMessage(_hwnd, 0x0010, IntPtr.Zero, IntPtr.Zero);
            }
        }

        private void RunMessageLoop()
        {
            // 1. 创建仅消息窗口 (Message-Only Window)
            _hwnd = Win32Native.CreateWindowEx(
                0, "Message", "ClipFlowMsgWindow",
                0, 0, 0, 0, 0,
                Win32Native.HWND_MESSAGE, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            if (_hwnd == IntPtr.Zero) return;

            // 2. 注册剪贴板监听
            Win32Native.AddClipboardFormatListener(_hwnd);
            _windowCreated.Set();

            // 3. 消息循环
            Win32Native.MSG msg;
            // GetMessage 会在收到 WM_QUIT 时返回 false，从而正常结束循环
            while (Win32Native.GetMessage(out msg, IntPtr.Zero, 0, 0))
            {
                if (msg.message == Win32Native.WM_CLIPBOARDUPDATE)
                {
                    _ = HandleClipboardAsync();
                }
                Win32Native.TranslateMessage(ref msg);
                Win32Native.DispatchMessage(ref msg);
            }

            _hwnd = IntPtr.Zero;
        }
        private async Task HandleClipboardAsync()
        {
            var clipData = await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                return await handler.GetContentAsync();
            });
            if (clipData != null)
            {
                ClipboardChanged?.Invoke(clipData);
            }
        }
        public async Task<bool> SetClipboardContentAsync(ClipboardData data, bool isServerUpdate = true)
        {
            //return await clipboardHandler.SetContentAsync(data, isServerUpdate);

            return false;
        }

        public void Dispose()
        {
            Stop();
            _windowCreated.Dispose();
        }

       
    }
}