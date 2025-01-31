using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using System;
using System.IO;
using ClipFlow.Desktop.ViewModels;
using Avalonia.Controls.ApplicationLifetimes;
using ClipFlow.Desktop.Services;

namespace ClipFlow.Desktop.Views
{
    public partial class MainWindow : Window
    {
        private TrayIcon? _trayIcon;
        private readonly MainWindowViewModel _viewModel;
        private bool _isExiting = false;
        private bool _isFirstOpen = true;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;

            // 初始化托盘图标
            InitializeTrayIcon();

            // 订阅窗口关闭事件
            Closing += MainWindow_Closing;

            // 订阅窗口打开事件
            Opened += MainWindow_Opened;
        }

        private void MainWindow_Opened(object? sender, EventArgs e)
        {
            // 只在首次打开时检查是否需要隐藏
            if (_isFirstOpen && ConfigService.Instance.CurrentConfig.HideOnStartup)
            {
                Hide();
                _isFirstOpen = false;
            }
        }

        private void InitializeTrayIcon()
        {
            var menu = new NativeMenu();
            var showItem = new NativeMenuItem("显示窗口");
            showItem.Click += ShowWindow_Click;
            menu.Add(showItem);

            var exitItem = new NativeMenuItem("退出");
            exitItem.Click += Exit_Click;
            menu.Add(exitItem);

            // 使用资源路径加载图标
            var uri = new Uri("avares://ClipFlow/Assets/trayiicon.ico");
            _trayIcon = new TrayIcon
            {
                Icon = new WindowIcon(AssetLoader.Open(uri)),
                ToolTipText = "ClipFlow",
                Menu = menu,
                IsVisible = true
            };

            _trayIcon.Clicked += TrayIcon_Clicked;
        }

        private void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
        {
            if (ConfigService.Instance.CurrentConfig?.MinimizeToTray == true && !_isExiting)
            {
                e.Cancel = true;  // 取消关闭操作
                Hide();          // 隐藏窗口
            }
            else
            {
                // 清理资源
                _trayIcon?.Dispose();
                _viewModel.Dispose(); // 直接调用 MainWindowViewModel 的 Dispose
            }
        }

        private void TrayIcon_Clicked(object? sender, EventArgs e)
        {
            if (!IsVisible)
            {
                ShowWindow();
            }
        }

        private void ShowWindow_Click(object? sender, EventArgs e)
        {
            ShowWindow();
        }

        private void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void Exit_Click(object? sender, EventArgs e)
        {
            _isExiting = true;
            
            // 清理资源
            _trayIcon?.Dispose();
            _viewModel.Dispose(); // 直接调用 MainWindowViewModel 的 Dispose

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }
    }
}