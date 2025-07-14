using Avalonia;
using System;
using Microsoft.Extensions.DependencyInjection;
using ClipFlow.Core.Interfaces;
using ClipFlow.Desktop.Linux.Services;
using ClipFlow.Core;

namespace ClipFlow.Desktop.Linux;

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

        Core.Program.Build(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() => Core.Program.BuildAvaloniaApp();
}