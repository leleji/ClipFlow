using Avalonia;
using System;
using Microsoft.Extensions.DependencyInjection;
using ClipFlow.Desktop.Interfaces;
using ClipFlow.Desktop.Linux.Services;
using ClipFlow.Desktop.Services;

namespace ClipFlow.Desktop.Linux;

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
            services.AddSingleton<IClipboardHandler, LinuxClipboardService>();

            var notificationService = new LinuxNotificationService();
            notificationService.Initialize();
            NotificationService.RegisterPlatformService(notificationService);

            BuildAvaloniaApp(services)
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(IServiceCollection services)
        => AppBuilder.Configure(() => new App(services))
            .WithInterFont()
            .LogToTrace();
} 