using System;
using AppKit;
using Avalonia;
using Avalonia.Media;
using ClipFlow.Desktop.Interfaces;
using ClipFlow.Desktop.MacOs.Services;
using ClipFlow.Desktop.MacOS.Services;
using Foundation;
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
        NSApplication.Init();
        ServiceCollection services = new();
        services.AddSingleton<IClipboardHandler, MacOsClipboardService>();
        services.AddSingleton<INotificationService, MacOSNotificationService>();
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