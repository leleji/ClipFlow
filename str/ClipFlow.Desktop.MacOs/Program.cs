using System;
using System.Linq;
using AppKit;
using Avalonia;
using Avalonia.Media;
using ClipFlow.Desktop.Interfaces;
using ClipFlow.Desktop.MacOs.Services;
using ClipFlow.Desktop.MacOS.Services;
using ClipFlow.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Foundation;

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
        // // 确保在 UI 线程中运行
        // NSApplication.Init();
        // NSApplication.SharedApplication.InvokeOnMainThread(() =>
        // {
        //     // 访问剪贴板
        //     var pasteboard = NSPasteboard.GeneralPasteboard;
        //
        //     // 获取剪贴板上的所有类型
        //     var types = pasteboard.Types;
        //     // 如果剪贴板包含文件
        //     if (types.Contains(NSPasteboard.NSFilenamesType)){
        //         var files = pasteboard.GetPropertyListForType(NSPasteboard.NSFilenamesType) as NSArray;
        //
        //         }
        //     // 设置剪贴板内容
        //     pasteboard.ClearContents();
        //     pasteboard.SetStringForType(new NSString("Hello from .NET using AppKit!"), NSPasteboard.NSStringType);
        //
        //     // 获取剪贴板内容
        //     var clipboardContent = pasteboard.GetDataForType(NSPasteboard.NSStringType);
        //     Console.WriteLine($"Clipboard content: {clipboardContent}");
        // });

        // 运行应用程序的主事件循环
        //NSApplication.SharedApplication.Run();
        
     
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