using Avalonia;
using Avalonia.Media;
using ClipFlow.Core;
using ClipFlow.Core.Interfaces;
using ClipFlow.Core.Services;
using ClipFlow.Desktop.Windows.Services;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Threading;

namespace ClipFlow.Desktop.Windows;

internal class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // 注册平台特定服务
        ServiceCollection services = new();
        services.AddSingleton<IClipboardHandler, WindowsClipboardService>();
        services.AddSingleton<INotificationService, WindowsNotificationService>();
        // 根据配置选择剪贴板监控实现
        var configService = new ConfigService();
        if (configService.CurrentConfig.ClipboardMonitorMode == 1)
        {
            services.AddSingleton<IClipboardMonitor, ClipboardMonitorTimer>();
        }
        else
        {
            services.AddSingleton<IClipboardMonitor, ClipboardMonitor>();
        }
        AppServices.ConfigureServices(services);

        Core.Program.Build(args);
    }
    //给 Designer 用
    public static AppBuilder BuildAvaloniaApp() => Core.Program.BuildAvaloniaApp();
}