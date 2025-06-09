using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System;
using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;

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
            NavigationItems.Add(new NavigationItem { Icon = resources["home_icon"] as StreamGeometry, Name = "同步配置", ViewModelType = typeof(SyncSettingsViewModel) });
            NavigationItems.Add(new NavigationItem { Icon = resources["upload_icon"] as StreamGeometry, Name = "上传设置", ViewModelType = typeof(UploadSettingsViewModel) });
            NavigationItems.Add(new NavigationItem { Icon = resources["download_icon"] as StreamGeometry, Name = "下载设置", ViewModelType = typeof(DownloadSettingsViewModel) });
            NavigationItems.Add(new NavigationItem { Icon = resources["log_icon"] as StreamGeometry, Name = "日志记录", ViewModelType = typeof(LogViewModel) });
            NavigationItems.Add(new NavigationItem { Icon = resources["settings_icon"] as StreamGeometry, Name = "基础设置", ViewModelType = typeof(SettingsViewModel) });
            NavigationItems.Add(new NavigationItem { Icon = resources["about_icon"] as StreamGeometry, Name = "关于", ViewModelType = typeof(AboutViewModel) });

            // 默认选择主页
            SelectedItem = NavigationItems[0];
        }

        partial void OnSelectedItemChanged(NavigationItem? value)
        {
            if (value?.ViewModelType != null)
            {
                // 如果当前页面实现了IDisposable，则销毁它
                if (CurrentPage is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                // 创建新的ViewModel实例
                CurrentPage = (ViewModelBase)_serviceProvider.GetRequiredService(value.ViewModelType);
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
        public Type? ViewModelType { get; set; }
    }
}
