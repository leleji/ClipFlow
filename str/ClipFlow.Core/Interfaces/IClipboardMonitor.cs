using ClipFlow.Core.Models;
using System;
using System.Threading.Tasks;

namespace ClipFlow.Core.Interfaces
{
    public interface IClipboardMonitor : IDisposable
    {
        // 定义委托类型
        delegate void ClipboardChangedEventHandler(ClipboardData data);
        event ClipboardChangedEventHandler? OnClipboardChanged;
        void Start();
        void Stop();
        Task<bool> SetClipboardContentAsync(ClipboardData data, bool isServerUpdate = true);
    }
}
