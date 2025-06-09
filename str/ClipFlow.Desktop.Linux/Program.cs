using Avalonia;
using System;
using System.Runtime.InteropServices;
using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using ClipFlow.Core.Interfaces;
using ClipFlow.Core.Linux.Services;
using ClipFlow.Core.Win.Services;

namespace ClipFlow.Core.Linux;

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
        services.AddSingleton<IClipboardHandler, LinuxClipboardService>();
        services.AddSingleton<INotificationService, LinuxNotificationService>();
        services.AddSingleton<IClipboardMonitor, ClipboardMonitor>();
        AppServices.ConfigureServices(services);


        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .With(new FontManagerOptions
            {
                DefaultFamilyName = "avares://Avalonia.Fonts.Inter/Assets#Inter",
                FontFallbacks =
                [
                    new FontFallback { FontFamily = "Microsoft YaHei UI" },
                    new FontFallback { FontFamily = "Noto Sans CJK SC" },
                    new FontFallback { FontFamily = "PingFang SC" },
                    new FontFallback { FontFamily = "Source Han Sans SC" },
                    new FontFallback { FontFamily = "WenQuanYi Micro Hei" }
                ]
            });
}