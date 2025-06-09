using Avalonia;
using Avalonia.Media;
using ClipFlow.Core;
using ClipFlow.Core.Interfaces;
using ClipFlow.Core.Services;
using ClipFlow.Desktop.Windows.Services;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;

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
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure(() => new App())
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .With(new FontManagerOptions
            {
                DefaultFamilyName = "avares://Avalonia.Fonts.Inter/Assets#Inter",
                FontFallbacks = new[]
                {
                        new FontFallback { FontFamily = "Microsoft YaHei UI" },
                        new FontFallback { FontFamily = "Noto Sans CJK SC" },
                        new FontFallback { FontFamily = "PingFang SC" },
                        new FontFallback { FontFamily = "Source Han Sans SC" },
                        new FontFallback { FontFamily = "WenQuanYi Micro Hei" }
                }
            });

}