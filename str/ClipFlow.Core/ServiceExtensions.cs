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
    public static class ServiceExtensions
    {
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            // 1. 基础配置服务 (单例)
            services.AddSingleton<ConfigService>();

            // 2. 核心业务逻辑 (单例)
            services.AddSingleton<IClipboardSyncService, ClipboardSyncService>();


            //开启启动
            services.AddSingleton<IAutoStartService, AutoStartService>();

            // 3. 所有 ViewModels (瞬时)
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<SyncSettingsViewModel>();
            services.AddTransient<UploadSettingsViewModel>();
            services.AddTransient<DownloadSettingsViewModel>();
            services.AddTransient<LogViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<AboutViewModel>();

            return services;
        }
    }
}
