using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClipFlow.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClipFlow.ViewModels
{
    public partial class LogViewModel : ViewModelBase
    {
        private readonly LogService _logService;
        public event EventHandler? LogItemsChanged;

        [ObservableProperty]
        private Services.LogItem _selectedLogItem;

        public IEnumerable<Services.LogItem> LogItems => _logService.LogItems;

        public IBrush TimestampBrush => Application.Current!.ActualThemeVariant == ThemeVariant.Dark 
            ? new SolidColorBrush(Color.FromRgb(180, 180, 180))
            : new SolidColorBrush(Color.FromRgb(100, 100, 100));

        public IBrush TypeBrush => Application.Current!.ActualThemeVariant == ThemeVariant.Dark
            ? new SolidColorBrush(Color.FromRgb(77, 179, 255))
            : new SolidColorBrush(Color.FromRgb(0, 120, 215));

        public IBrush MessageBrush => Application.Current!.ActualThemeVariant == ThemeVariant.Dark
            ? new SolidColorBrush(Color.FromRgb(220, 220, 220))
            : new SolidColorBrush(Color.FromRgb(30, 30, 30));

        public LogViewModel()
        {
            _logService = LogService.Instance;
            _logService.LogAdded += (s, e) => LogItemsChanged?.Invoke(this, EventArgs.Empty);
            
            // 监听主题变化
            if (Application.Current != null)
            {
                Application.Current.ActualThemeVariantChanged += (s, e) =>
                {
                    OnPropertyChanged(nameof(TimestampBrush));
                    OnPropertyChanged(nameof(TypeBrush));
                    OnPropertyChanged(nameof(MessageBrush));
                };
            }
        }

        [RelayCommand]
        private async void CopySelectedLog()
        {
            if (_selectedLogItem != null)
            {
                var text = $"{_selectedLogItem.Timestamp:yyyy-MM-dd HH:mm:ss} {_selectedLogItem.Type}: {_selectedLogItem.Message}";
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var clipboard = desktop.MainWindow?.Clipboard;
                    if (clipboard != null)
                    {
                        await clipboard.SetTextAsync(text);
                    }
                }
            }
        }

        [RelayCommand]
        private async void CopyAllLogs()
        {
            var sb = new StringBuilder();
            foreach (var log in LogItems)
            {
                sb.AppendLine($"{log.Timestamp:yyyy-MM-dd HH:mm:ss} {log.Type}: {log.Message}");
            }

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var clipboard = desktop.MainWindow?.Clipboard;
                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(sb.ToString());
                }
            }
        }

        [RelayCommand]
        private void ClearLog()
        {
            _logService.Clear();
        }
    }
} 