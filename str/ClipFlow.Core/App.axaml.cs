using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using ClipFlow.Core.Interfaces;
using ClipFlow.Core.Services;
using ClipFlow.Core.ViewModels;
using ClipFlow.Core.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.InteropServices.JavaScript;
namespace ClipFlow.Core
{
    public partial class App : Application
    {
        private TrayIcon? _trayIcon;
        private IClassicDesktopStyleApplicationLifetime? _desktop;
        private bool _isShowingWindow = false;
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
        public override void OnFrameworkInitializationCompleted()
        {
            // 避免在 Avalonia Designer 模式下访问服务，因为不会调用Main()
            if (Design.IsDesignMode)
            {
                base.OnFrameworkInitializationCompleted();
                return;
            }

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();
                _desktop = desktop;
                _desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                // 初始化托盘图标
                InitializeTrayIcon(_desktop);
            }
            // 获取服务
            var configService = AppServices.ServiceProvider.GetRequiredService<ConfigService>();
            var clipboardSyncService = AppServices.ServiceProvider.GetRequiredService<IClipboardSyncService>();
            var notificationService = AppServices.ServiceProvider.GetRequiredService<INotificationService>();
            // 初始化服务
            notificationService.Initialize();
            // 如果需要启动服务
            if (configService.CurrentConfig.IsEnabled)
            {
                clipboardSyncService.Start();
            }
#if DEBUG
            //Show();
#endif

            base.OnFrameworkInitializationCompleted();
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
                        DataContext = AppServices.ServiceProvider.GetRequiredService<MainWindowViewModel>()
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
                    var configService = AppServices.ServiceProvider.GetRequiredService<ConfigService>();
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
        private void TrayIcon_Clicked(object? sender, EventArgs e)
        {
            ShowMainWindow();
        }
        private void Exit()
        {
            if (_trayIcon != null)
            {
                _trayIcon.Clicked -= TrayIcon_Clicked;
                _trayIcon.Menu = null;
                _trayIcon.Dispose();
                _trayIcon = null;
            }

            if (_desktop?.MainWindow != null)
            {
                _desktop.MainWindow.Closed -= MainWindow_Closed;
                _desktop.MainWindow.Close();
                _desktop.MainWindow = null;
            }

            _desktop?.Shutdown();
        }


        private void InitializeTrayIcon(IClassicDesktopStyleApplicationLifetime mainWindow)
        {
            // 使用资源路径加载图标
            var uri = new Uri("avares://ClipFlow.Core/Assets/trayiicon.ico");
            _trayIcon = new TrayIcon
            {
                Icon = new WindowIcon(AssetLoader.Open(uri)),
                ToolTipText = nameof(ClipFlow),
                IsVisible = true
            };
       
            var showMenuItem = new NativeMenuItem("显示");
            showMenuItem.Click += (s, e) => ShowMainWindow();

            var exitMenuItem = new NativeMenuItem("退出");
            exitMenuItem.Click += (s, e) => Exit();

            _trayIcon.Menu = new NativeMenu();
            _trayIcon.Menu.Items.Add(showMenuItem);
            _trayIcon.Menu.Items.Add(exitMenuItem);
            _trayIcon.Clicked += (s, e) => ShowMainWindow();


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