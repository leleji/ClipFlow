using Avalonia;
using Avalonia.Media;
using ClipFlow.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClipFlow.Core
{
    public class Program
    {
        private static Mutex? _mutex;

        const string mutexName = "ClipFlow.Desktop_Mutex";
        private const string PipeName = "ClipFlow.Desktop_Pipe";

        private static bool EnsureSingleInstance()
        {
            bool createdNew;
            _mutex = new Mutex(true, mutexName, out createdNew);
            if (!createdNew)
            {
                SendActivateSignal();
            }else
            {
                StartPipeListener();
            }
            return createdNew;
        }
        private static void ReleaseMutex()
        {

            try
            {
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
            }
            catch
            {
                
            }
        }
        private static void SendActivateSignal()
        {
            try
            {
                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                client.Connect(500); // 最多等待 0.5 秒
                using var writer = new StreamWriter(client, Encoding.UTF8);
                writer.WriteLine("ACTIVATE");
                writer.Flush();
            }
            catch
            {
               
            }
        }
        private static void StartPipeListener()
        {
            var thread = new Thread(async () =>
            {
                while (true)
                {
                    try
                    {
                        using var server = new NamedPipeServerStream(PipeName, PipeDirection.In);
                        server.WaitForConnection();

                        using var reader = new StreamReader(server, Encoding.UTF8);
                        var msg = reader.ReadLine();
                        if (msg == "ACTIVATE")
                        {
                            var notificationService = AppServices.ServiceProvider.GetRequiredService<INotificationService>();
                            await notificationService.ShowNotificationAsync("提示", "软件已经开启，请勿重复打开");
                        }
                    }
                    catch
                    {
                        // 忽略异常
                    }
                }
            });

            thread.IsBackground = true;
            thread.Start();
        }


        public static void Build(string[] args)
        {
            //启动判断
            if (!EnsureSingleInstance())
            {
                return;
            }
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            // 应用退出时释放 Mutex
            ReleaseMutex();
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            var builder = AppBuilder.Configure(() => new App())
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
                if (OperatingSystem.IsMacOS())
                {
                    builder = builder.With(new MacOSPlatformOptions
                    {
                        ShowInDock = false
                    });
                }
             return builder;
        }

    }
}
