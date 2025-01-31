using Avalonia;
using Avalonia.Media;
using System;
using ClipFlow.Desktop.MacOs.Notification;
using ClipFlow.Services;

namespace ClipFlow.Desktop.MacOs;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // 注册 MacOS 通知服务
        var notificationService = new Notification.MacOSNotificationService();
        notificationService.Initialize();
        NotificationService.RegisterPlatformService(notificationService);

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

