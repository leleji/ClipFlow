using Avalonia.Threading;
using ClipFlow.Core.Services;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ClipFlow.Core.Interfaces;
using ClipFlow.Core.Models;

namespace ClipFlow.Desktop.Linux.Services
{
    public class ClipboardMonitor : IClipboardMonitor
    {
        private const string libX11 = "libX11.so.6";
        private const string libXfixes = "libXfixes.so.3";

        [DllImport(libX11)]
        private static extern IntPtr XOpenDisplay(string? display);

        [DllImport(libX11)]
        private static extern int XCloseDisplay(IntPtr display);

        [DllImport(libX11)]
        private static extern IntPtr XDefaultRootWindow(IntPtr display);

        [DllImport(libX11)]
        private static extern IntPtr XInternAtom(IntPtr display, string atom_name, bool only_if_exists);

        [DllImport(libX11)]
        private static extern IntPtr XNextEvent(IntPtr display, out XEvent xevent);

        [DllImport(libX11)]
        private static extern void XConvertSelection(IntPtr display, IntPtr selection, IntPtr target, IntPtr property, IntPtr requestor, IntPtr time);

        [DllImport(libX11)]
        private static extern int XGetWindowProperty(IntPtr display, IntPtr w, IntPtr property, IntPtr long_offset, IntPtr long_length, bool delete, IntPtr req_type, out IntPtr actual_type, out int actual_format, out IntPtr nitems, out IntPtr bytes_after, out IntPtr prop);

        [DllImport(libX11)]
        private static extern int XFree(IntPtr data);

        [DllImport(libXfixes)]
        private static extern void XFixesSelectSelectionInput(IntPtr display, IntPtr window, IntPtr selection, uint event_mask);

        [StructLayout(LayoutKind.Sequential)]
        private struct XEvent
        {
            public int type;
            public IntPtr serial;
            public bool send_event;
            public IntPtr display;
            public IntPtr window;
            public IntPtr root;
            public IntPtr subwindow;
            public IntPtr time;
            public int x;
            public int y;
            public int x_root;
            public int y_root;
            public uint state;
            public uint keycode;
            public bool same_screen;
        }

        
        private readonly IClipboardHandler _clipboardHandler;
        private bool _isMonitoring;

        private IntPtr _display;
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
            _display = XOpenDisplay(null);
            if (_display == IntPtr.Zero)
            {
                Console.WriteLine("无法连接x11");
                return;
            }

            // 获取必要的原子
            IntPtr clipboardAtom = XInternAtom(_display, "CLIPBOARD", false);
            // 获取根窗口
            IntPtr rootWindow = XDefaultRootWindow(_display);

            // 使用 XFixesSelectSelectionInput 来监听剪贴板选择变化
            XFixesSelectSelectionInput(_display, rootWindow, clipboardAtom, 0x01); // 0x01 表示 SelectionNotify 事件

            _clipboardHandler.Initialize();
            Task.Run(async () =>
            {
                while (_isMonitoring)
                {
                    XEvent xevent;
                    XNextEvent(_display, out xevent);
                    // 检查是否是 SelectionNotify 事件
                    if (xevent.type == 86||xevent.type == 31) // 31 是 SelectionNotify 事件类型
                    {
                        Dispatcher.UIThread.Post(async () =>
                        {
                            await CheckClipboardContent();
                        });
                    }
                }

            });
            
            
        }

        public void Stop()
        {
            _isMonitoring = false;
            XCloseDisplay(_display);
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