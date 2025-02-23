using ClipFlow.Models;
using ClipFlow.Desktop.Services;
using System;
using System.Threading.Tasks;
using ClipFlow.Desktop.Interfaces;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ClipFlow.Desktop.Windows.Services
{
    public class ClipboardMonitor : Form, IClipboardMonitor
    {

        // 导入 Windows API
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        // 定义消息常量
        private const int WM_CLIPBOARDUPDATE = 0x031D;


        private readonly IClipboardHandler _clipboardHandler;
        private bool _isMonitoring;

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
            AddClipboardFormatListener(this.Handle);



            _clipboardHandler.Initialize();
        }

        public void Stop()
        {
            _isMonitoring = false;
            bool result = RemoveClipboardFormatListener(this.Handle);
            if (!result)
            {
                Console.WriteLine("Failed to remove clipboard listener.");
            }

            _clipboardHandler.Cleanup();
        }


        protected override void WndProc(ref Message m)
        {
            // 处理消息
            if (m.Msg == WM_CLIPBOARDUPDATE)
            {
                var clipData = _clipboardHandler.GetContentAsync().Result;
                if (clipData != null)
                {
                    OnClipboardChanged?.Invoke(clipData);
                }
            }

            base.WndProc(ref m);
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // 注销剪贴板监听
            RemoveClipboardFormatListener(this.Handle);
            base.OnFormClosed(e);
        }



        public async Task<bool> SetClipboardContentAsync(ClipboardData data, bool isServerUpdate = true)
        {
            return await _clipboardHandler.SetContentAsync(data, isServerUpdate);
        }
    }
}