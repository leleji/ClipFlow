using Avalonia;
using Avalonia.Media;
using System;
using ClipFlow.Desktop.Linux.Notification;
using ClipFlow.Desktop.Services;
using ClipFlow.Services;

namespace ClipFlow.Desktop.Linux;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            // 注册 Linux 通知服务
            var notificationService = new LinuxNotificationService();
            notificationService.Initialize();
            NotificationService.RegisterPlatformService(notificationService);
            
            FileLogService._.Info("Linux通知服务注册成功");
        }
        catch (Exception ex)
        {
            FileLogService._.Error("注册通知服务失败", ex);
        }

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

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