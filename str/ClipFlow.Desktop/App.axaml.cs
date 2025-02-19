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
using Avalonia.Data.Core.Plugins;
using System.Linq;
namespace ClipFlow.Desktop
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
        public override void OnFrameworkInitializationCompleted()
        {
           
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();
                // 初始化托盘图标
                InitializeTrayIcon(desktop);
            }
            // 获取服务
            var configService = AppServices.ServiceProvider.GetRequiredService<ConfigService>();
            var clipboardSyncService = AppServices.ServiceProvider.GetRequiredService<IClipboardSyncService>();
            // 如果需要启动服务
            if (configService.CurrentConfig.IsEnabled)
            {
                clipboardSyncService.Start();
            }
            Show();
            base.OnFrameworkInitializationCompleted();
        }
        bool isLoadingShow = false;
        private void Show()
        {
            if (isLoadingShow) {
                return;
            }
            isLoadingShow = true;
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow != null)
                {
                    desktop.MainWindow.Activate();
                    isLoadingShow = false;
                    return;
                }
                desktop.MainWindow = new MainWindow
                {
                    DataContext = AppServices.ServiceProvider.GetRequiredService<MainWindowViewModel>()
                };
                // 订阅窗口事件
                desktop.MainWindow.Closing += MainWindow_Closing;
                //mainWindow.Opened += MainWindow_Opened;

                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                desktop.MainWindow.Show();
            }
            isLoadingShow = false;
        }


        private void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
        {
 
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow.Closing -= MainWindow_Closing;
                desktop.MainWindow = null; // 释放窗口
                var configService = AppServices.ServiceProvider.GetRequiredService<ConfigService>();
                if (!configService.CurrentConfig.MinimizeToTray)
                {
                    desktop.Shutdown();
                }
                GC.Collect();
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
        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}