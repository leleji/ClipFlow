using System;
using Avalonia;
using Avalonia.Media;
using ClipFlow.Desktop.Interfaces;
using ClipFlow.Desktop.MacOs.Services;
using ClipFlow.Desktop.MacOS.Services;
using ClipFlow.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClipFlow.Desktop.MacOs;

internal class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // 注册 MacOS 通知服务
        var notificationService = new MacOSNotificationService();
        notificationService.Initialize();
        NotificationService.RegisterPlatformService(notificationService);

        // 注册平台特定服务
        ServiceCollection services = new();
        services.AddSingleton<IClipboardHandler, MacOSClipboardService>();
        
        BuildAvaloniaApp(services)
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(IServiceCollection services)
        => AppBuilder.Configure(() => new App(services))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .With(new MacOSPlatformOptions { ShowInDock = false })
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