using Avalonia;
using Avalonia.Media;
using ClipFlow.Core;
using ClipFlow.Core.Interfaces;
using ClipFlow.Core.Services;
using ClipFlow.Desktop.Services;
using ClipFlow.Infrastructure.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace ClipFlow.Desktop
{
    internal class Program
    {



      


        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            using var singleInstance = new SingleInstanceService("ClipFlow.Unique.ID");

            if (!singleInstance.CheckIsFirstInstance())
            {
                // 异步发送激活信号并退出
                singleInstance.SendMessageToFirstInstanceAsync("ACTIVATE").GetAwaiter().GetResult();
                return;
            }

            // 监听来自其他实例的信号
            singleInstance.MessageReceived += (msg) =>
            {
                if (msg == "ACTIVATE")
                {
                    //  提示软件开启
                    return;
                }
            };

            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            finally
            {
                singleInstance.Dispose();
                ClipFlowSentry.Shutdown();
            }

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
                    })
                .With(new MacOSPlatformOptions
                    {
                        ShowInDock = false
                    });
    }
}
