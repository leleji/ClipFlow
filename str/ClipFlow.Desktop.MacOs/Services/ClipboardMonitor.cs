using System;
using System.Threading.Tasks;
using ClipFlow.Core.Interfaces;
using ClipFlow.Core.Models;
using ClipFlow.Desktop.MacOs.Utilities;

namespace ClipFlow.Desktop.MacOs.Services;

public class ClipboardMonitor(IClipboardHandler clipboardHandler) : IClipboardMonitor
{
    private bool _isMonitoring;
    private PasteboardMonitor? _pasteboardMonitor;
    public event IClipboardMonitor.ClipboardChangedEventHandler? OnClipboardChanged;

    public void Start()
    {
        if (_isMonitoring) return;
        _isMonitoring = true;
        _pasteboardMonitor = new PasteboardMonitor();
        _pasteboardMonitor.Changed += async (sender, e) =>
        {
            Console.WriteLine($"剪贴板已更改 (计数从 {e.OldCount} 变为 {e.NewCount})");
            await CheckClipboardContent();
        };
        if (!_pasteboardMonitor.StartAsync()) Console.WriteLine("启动监控失败");
        clipboardHandler.Initialize();
    }

    public void Stop()
    {
        _isMonitoring = false;
        Task.Run(async () =>
        {
            if (_pasteboardMonitor != null) await _pasteboardMonitor.StopAsync();
        });
        clipboardHandler.Cleanup();
    }

    public async Task<bool> SetClipboardContentAsync(ClipboardData data, bool isServerUpdate = true)
    {
        return await clipboardHandler.SetContentAsync(data, isServerUpdate);
    }

    public void Dispose()
    {
        Stop();
        if (clipboardHandler is IDisposable disposable) disposable.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task CheckClipboardContent()
    {
        if (!_isMonitoring) return;
        var clipData = await clipboardHandler.GetContentAsync();
        if (clipData != null) OnClipboardChanged?.Invoke(clipData);
    }
}