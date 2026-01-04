using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using ClipFlow.Core.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;

namespace ClipFlow.Core.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private ViewModelBase? _currentPage;

        [ObservableProperty]
        private NavigationItem? _selectedItem;

        public ObservableCollection<NavigationItem> NavigationItems { get; } = new();
        private bool _disposed = false;


        public MainWindowViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            var resources = Application.Current!.Resources;
            // 只添加导航项，不立即创建 ViewModel
            NavigationItems.Add(new NavigationItem { Icon = resources["home_icon"] as StreamGeometry, Name = "同步配置", CreateViewModel = sp => sp.GetRequiredService<SyncSettingsViewModel>() });
            NavigationItems.Add(new NavigationItem { Icon = resources["upload_icon"] as StreamGeometry, Name = "上传设置", CreateViewModel = sp => sp.GetRequiredService < UploadSettingsViewModel>() });
            NavigationItems.Add(new NavigationItem { Icon = resources["download_icon"] as StreamGeometry, Name = "下载设置", CreateViewModel = sp => sp.GetRequiredService<DownloadSettingsViewModel>() });
            NavigationItems.Add(new NavigationItem { Icon = resources["log_icon"] as StreamGeometry, Name = "日志记录", CreateViewModel = sp => sp.GetRequiredService<LogViewModel>() });
            NavigationItems.Add(new NavigationItem { Icon = resources["settings_icon"] as StreamGeometry, Name = "基础设置", CreateViewModel = sp => sp.GetRequiredService < SettingsViewModel>() });
            NavigationItems.Add(new NavigationItem { Icon = resources["about_icon"] as StreamGeometry, Name = "关于", CreateViewModel = sp => sp.GetRequiredService < AboutViewModel>() });

            // 默认选择主页
            SelectedItem = NavigationItems[0];
        }

        partial void OnSelectedItemChanged(NavigationItem? value)
        {
            if (value?.CreateViewModel != null)
            {
                if (CurrentPage is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                // 使用工厂方法创建，不依赖运行时反射 Type
                CurrentPage = value.CreateViewModel(_serviceProvider);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 销毁当前页面
                    if (CurrentPage is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }

                _disposed = true;
            }
        }

        ~MainWindowViewModel()
        {
            Dispose(false);
        }
    }

    public class NavigationItem
    {
        public StreamGeometry? Icon { get; set; }
        public string? Name { get; set; }
        // 使用 Func 替代 Type，这样 DI 可以在编译时确定类型
        public Func<IServiceProvider, ViewModelBase>? CreateViewModel { get; set; }
    }
}
