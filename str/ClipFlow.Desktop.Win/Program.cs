using System;
using Avalonia;
using Avalonia.Media;
using ClipFlow.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using ClipFlow.Desktop.Interfaces;
using ClipFlow.Desktop.Win.Services;
using NLog.Config;
using NLog;
using System.IO;
using NLog.Targets;

namespace ClipFlow.Desktop.Win;

internal class Program
{


    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {

        try
        {


            // 注册平台特定服务
            ServiceCollection services = new();
            services.AddSingleton<IClipboardHandler, WindowsClipboardService>();

            // 注册 Windows 通知服务
            var notificationService = new WindowsNotificationService();
            notificationService.Initialize();
            NotificationService.RegisterPlatformService(notificationService);
            
            FileLogService._.Info("Windows通知服务注册成功");


           
            BuildAvaloniaApp(services)
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            FileLogService._.Error("注册通知服务失败", ex);
            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(IServiceCollection services)
        => AppBuilder.Configure(() => new App(services))
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
