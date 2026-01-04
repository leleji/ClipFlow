using Avalonia.Controls;
using Avalonia.Input.Platform;
using ClipFlow.Core.Interfaces;
using ClipFlow.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ClipFlow.Core.Services
{
    public static class ClipboardManager
    {
        // 这里使用接口，以便在 Windows/Linux/Android 下注入不同的处理逻辑
        private static IClipboardHandler? _handler;

        public static IClipboard Clipboard;

        private static string? _lastHash;

        /// <summary>
        /// 由各平台（如 Windows 端的 App.axaml.cs）在初始化时注入
        /// </summary>
        public static void RegisterHandler(IClipboardHandler handler)
        {
            _handler = handler;
            var hiddenWindow = new Window
            {
                IsVisible = false,
                ShowInTaskbar = false,
                SystemDecorations = SystemDecorations.None,
                Width = 0,
                Height = 0
            };
            Clipboard = hiddenWindow.Clipboard;
        }

        // 静态包装方法，方便全局调用
        public static Task<ClipboardData?> GetContentAsync() =>
            _handler?.GetContentAsync() ?? Task.FromResult<ClipboardData?>(null);

        public static Task<bool> SetContentAsync(ClipboardData data) =>
            _handler?.SetContentAsync(data) ?? Task.FromResult(false);
    }
}
