using ClipFlow.Core.Interfaces;
using ClipFlow.Core.Services;
using ClipFlow.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipFlow.Core
{
    public class AppServices
    {
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
        public static IServiceProvider ServiceProvider { get; private set; }
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
        public static void ConfigureServices(IServiceCollection services)
        {
            var configService = new ConfigService();
            if (configService.CurrentConfig.ClipboardMonitorMode == 1)
            {
                services.AddSingleton<IClipboardMonitor, ClipboardMonitorTimer>();
            }


            // 注册所有服务
            services.AddSingleton<ConfigService>();
            services.AddSingleton<IAutoStartService, AutoStartService>();
            services.AddSingleton<IClipboardSyncService, ClipboardSyncService>();


            // 注册所有ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<SyncSettingsViewModel>();
            services.AddTransient<UploadSettingsViewModel>();
            services.AddTransient<DownloadSettingsViewModel>();
            services.AddTransient<LogViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<AboutViewModel>();

            ServiceProvider = services.BuildServiceProvider();
        }
    }
}
