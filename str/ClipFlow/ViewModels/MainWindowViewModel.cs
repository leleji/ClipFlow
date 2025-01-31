using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using Avalonia.Media;

namespace ClipFlow.Desktop.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase, IDisposable
    {
        [ObservableProperty]
        private ViewModelBase? _currentPage;

        [ObservableProperty]
        private NavigationItem? _selectedItem;

        public ObservableCollection<NavigationItem> NavigationItems { get; } = new();

        // 使用字典来缓存已创建的页面
        private readonly Dictionary<string, ViewModelBase> _pageCache = new();
        private bool _disposed = false;

        public MainWindowViewModel()
        {
            var resources = Application.Current!.Resources;
            // 只添加导航项，不立即创建 ViewModel
            NavigationItems.Add(new NavigationItem(this) { Icon = resources["home_icon"] as StreamGeometry, Name = "同步配置", ViewModelType = typeof(SyncSettingsViewModel) });
            NavigationItems.Add(new NavigationItem(this) { Icon = resources["upload_icon"] as StreamGeometry, Name = "上传设置", ViewModelType = typeof(UploadSettingsViewModel) });
            NavigationItems.Add(new NavigationItem(this) { Icon = resources["download_icon"] as StreamGeometry, Name = "下载设置", ViewModelType = typeof(DownloadSettingsViewModel) });
            NavigationItems.Add(new NavigationItem(this) { Icon = resources["log_icon"] as StreamGeometry, Name = "日志记录", ViewModelType = typeof(LogViewModel) });
            NavigationItems.Add(new NavigationItem(this) { Icon = resources["settings_icon"] as StreamGeometry, Name = "基础设置", ViewModelType = typeof(SettingsViewModel) });
            NavigationItems.Add(new NavigationItem(this) { Icon = resources["about_icon"] as StreamGeometry, Name = "关于", ViewModelType = typeof(AboutViewModel) });

            // 默认选择主页
            SelectedItem = NavigationItems[0];
            CurrentPage = GetOrCreateViewModel(SelectedItem);
        }

        partial void OnSelectedItemChanged(NavigationItem? value)
        {
            if (value != null)
            {
                CurrentPage = GetOrCreateViewModel(value);
            }
        }

        private ViewModelBase GetOrCreateViewModel(NavigationItem item)
        {
            // 如果缓存中存在，直接返回
            if (_pageCache.TryGetValue(item.Name!, out var existingViewModel))
            {
                return existingViewModel;
            }

            // 否则创建新的实例并缓存
            var newViewModel = (ViewModelBase)System.Activator.CreateInstance(item.ViewModelType!)!;
            _pageCache[item.Name!] = newViewModel;
            return newViewModel;
        }

        // 为 NavigationItem 提供获取 ViewModel 的方法
        internal ViewModelBase? GetViewModel(string name)
        {
            return _pageCache.GetValueOrDefault(name);
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
                    // 清理所有缓存的 ViewModel
                    foreach (var viewModel in _pageCache.Values)
                    {
                        if (viewModel is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }
                    _pageCache.Clear();
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
        private readonly MainWindowViewModel _mainViewModel;

        public NavigationItem(MainWindowViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        public StreamGeometry? Icon { get; set; }
        public string? Name { get; set; }
        public System.Type? ViewModelType { get; set; }
        public ViewModelBase? ViewModel => _mainViewModel.GetViewModel(Name!);
    }
}
