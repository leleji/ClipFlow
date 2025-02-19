using System;
using Avalonia;
using Avalonia.Media;
using ClipFlow.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using ClipFlow.Desktop.Interfaces;
using ClipFlow.Desktop.Win.Services;
using NLog;

namespace ClipFlow.Desktop.Win;

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
        services.AddSingleton<IClipboardMonitor, ClipboardMonitor>();
        
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
