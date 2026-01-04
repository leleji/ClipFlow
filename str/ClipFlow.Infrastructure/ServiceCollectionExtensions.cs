using ClipFlow.Core;
using ClipFlow.Core.Interfaces;
using ClipFlow.Infrastructure.Platforms.Windows;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
#if WINDOWS
using ClipFlow.Infrastructure.Platforms.Windows;
#endif


namespace ClipFlow.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPlatformServices(this IServiceCollection services)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // 注入 Windows 平台的具体实现
                services.AddSingleton<IClipboardWatcher, WindowsClipboardWatcher>();
                services.AddSingleton<INotificationService, WindowsNotificationService>();
                services.AddSingleton<IClipboardHandler, WindowsClipboardHandler1>();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // TODO: 注入 MacOS 实现
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // TODO: 注入 Linux 实现
            }

            return services;
        }
    }
}
