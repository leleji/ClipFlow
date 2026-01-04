using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using ClipFlow.Core;
using ClipFlow.Core.Interfaces;
using ClipFlow.Core.Services;
using ClipFlow.Core.Utilities;
using ClipFlow.Core.ViewModels;
using ClipFlow.Desktop.Views;
using ClipFlow.Infrastructure;
using ClipFlow.Infrastructure.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Sentry;
using System;
using System.Linq;

namespace ClipFlow.Desktop
{
    public partial class App : Application
    {
        public static new App Current => (App)Application.Current!;
        public IServiceProvider? Services { get; private set; }

        private bool _isShowingWindow = false;

        // 点击托盘图标（比如双击或单点）直接显示窗口
        private void OnTrayIconClicked(object? sender, EventArgs e) => ShowMainWindow();

        // 菜单项：打开
        private void OnTrayOpenClick(object? sender, EventArgs e) => ShowMainWindow();

        // 菜单项：退出
        private void OnTrayExitClick(object? sender, EventArgs e) => Exit();

        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            // 避免在 Avalonia Designer 模式下访问服务
            if (Design.IsDesignMode)
            {
                BuildServiceProvider();
                base.OnFrameworkInitializationCompleted();
                return;
            }



            Services = BuildServiceProvider();
            var configService = Services.GetRequiredService<ConfigService>();
            var clipboardSyncService = Services.GetRequiredService<IClipboardSyncService>();


            ClipFlowSentry.Init(
                dsn: "https://be22c13467f44d198f4e3a8aa21b367b@glitchtip.1999111.xyz/1",
                allowUpload: configService.CurrentConfig.AllowErrorUpload,
                environment: "production"
            );
            configService.AllowErrorUploadChanged += ClipFlowSentry.SetEnabled;

            // 如果需要启动服务
            if (configService.CurrentConfig.IsEnabled)
            {
                clipboardSyncService.Start();
            }
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode =ShutdownMode.OnExplicitShutdown;
                ClipboardManager.RegisterHandler(Services.GetRequiredService<IClipboardHandler>());
                ClipboardManager.GetContentAsync();
#if DEBUG
                ShowMainWindow();
#endif
            }
            base.OnFrameworkInitializationCompleted();
        }

        private IServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddCoreServices();       // Core 层通用注入
            services.AddPlatformServices();   // Infrastructure 层平台注入
            return services.BuildServiceProvider();
        }

        private void ShowMainWindow()
        {
            if (_isShowingWindow) return;
            _isShowingWindow = true;

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow == null)
                {
                    desktop.MainWindow = new MainWindow
                    {
                        DataContext = Services?.GetRequiredService<MainWindowViewModel>()
                    };

                    // 关闭窗口时销毁引用，释放内存
                    desktop.MainWindow.Closed += MainWindow_Closed;
                    desktop.MainWindow.Show();
                }
                else
                {
                    desktop.MainWindow.Activate();
                }
            }

            Dispatcher.UIThread.Post(() => _isShowingWindow = false, DispatcherPriority.Background);
        }
        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow != null)
                {
                    var configService = Services?.GetRequiredService<ConfigService>();
                    if (!configService.CurrentConfig.MinimizeToTray)
                    {
                        //如果的是不是最小化就关闭窗口
                        Exit();
                    }
                    else
                    {
                        desktop.MainWindow.Closed -= MainWindow_Closed;
                        desktop.MainWindow = null;
                    }

                }
            }

        }
        public void Exit()
        {
            (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
        }
    }
}
