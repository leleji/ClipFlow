using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ClipFlow.Desktop.ViewModels;
using ClipFlow.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using ClipFlow.Desktop.Services;
using System;
using Avalonia.Controls;
using Avalonia.Platform;
using ClipFlow.Desktop.Interfaces;
using System.Net.WebSockets;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Input.Platform;
namespace ClipFlow.Desktop
{
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;

        private IServiceCollection _services;
        public static IClipboard Clipboard { get; set; }
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
        public App(IServiceCollection services)
        {
            _services = services;
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // 配置依赖注入
            ConfigureServices(_services);
            _serviceProvider = _services.BuildServiceProvider();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // 初始化托盘图标
                InitializeTrayIcon(desktop);
            }

            // 获取服务
            var configService = _serviceProvider.GetRequiredService<ConfigService>();
            var clipboardSyncService = _serviceProvider.GetRequiredService<IClipboardSyncService>();
            // 如果需要启动服务
            if (configService.CurrentConfig.IsEnabled)
            {
                clipboardSyncService.Start();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void Show()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {

                var mainWindow = ActivatorUtilities.CreateInstance<MainWindow>(_serviceProvider);
                mainWindow.DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>();
                Clipboard = mainWindow.Clipboard;
                desktop.MainWindow = mainWindow;
                // 订阅窗口事件
                mainWindow.Closing += MainWindow_Closing;
                //mainWindow.Opened += MainWindow_Opened;

                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                // 注册程序退出事件
                desktop.Exit += (s, e) =>
                {
                    if (_serviceProvider is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                };
                desktop.MainWindow.Show();
            }
        }
        private void ConfigureServices(IServiceCollection services)
        {
            // 注册所有服务
            services.AddSingleton<ConfigService>();
            services.AddSingleton<LogService>();
            services.AddSingleton<NotificationService>();
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
        }

        private void MainWindow_Opened(object? sender, EventArgs e)
        {
            if (sender is Window window)
            {
                var config = _serviceProvider!.GetRequiredService<ConfigService>();
                if (config.CurrentConfig.HideOnStartup)
                {
                    window.Hide();
                }
            }
        }

        private void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
        {
            if (sender is Window window)
            {
                var config = _serviceProvider!.GetRequiredService<ConfigService>();
                if (config.CurrentConfig.MinimizeToTray)
                {
                    e.Cancel = true;
                    window.Hide();
                }
            }
        }

        private void InitializeTrayIcon(IClassicDesktopStyleApplicationLifetime mainWindow)
        {
            // 使用资源路径加载图标
            var uri = new Uri("avares://ClipFlow.Desktop/Assets/trayiicon.ico");
            var trayIcon = new TrayIcon
            {
                Icon = new WindowIcon(AssetLoader.Open(uri)),
                ToolTipText = "ClipFlow",
                IsVisible = true
            };

            var showMenuItem = new NativeMenuItem("显示");
            showMenuItem.Click += (s, e) => Show();
            
            var exitMenuItem = new NativeMenuItem("退出");
            exitMenuItem.Click += (s, e) =>
            {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown();
                }
            };

            trayIcon.Menu = new NativeMenu();
            trayIcon.Menu.Items.Add(showMenuItem);
            trayIcon.Menu.Items.Add(exitMenuItem);

            trayIcon.Clicked += (s, e) => Show();


        }
    }
} 