using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClipFlow.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ClipFlow.Desktop.ViewModels
{
    public partial class LogViewModel : ViewModelBase
    {
        private readonly LogService _logService;
        public event EventHandler LogItemsChanged;

        [ObservableProperty]
        private Services.LogItem _selectedLogItem;

        public IEnumerable<Services.LogItem> LogItems => _logService.LogItems;

        public static IBrush TimestampBrush => Application.Current!.ActualThemeVariant == ThemeVariant.Dark 
            ? new SolidColorBrush(Color.FromRgb(180, 180, 180))
            : new SolidColorBrush(Color.FromRgb(100, 100, 100));

        public static IBrush TypeBrush => Application.Current!.ActualThemeVariant == ThemeVariant.Dark
            ? new SolidColorBrush(Color.FromRgb(77, 179, 255))
            : new SolidColorBrush(Color.FromRgb(0, 120, 215));

        public static IBrush MessageBrush => Application.Current!.ActualThemeVariant == ThemeVariant.Dark
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
                    OnPropertyChanged(nameof(ViewModels.LogViewModel.TimestampBrush));
                    OnPropertyChanged(nameof(ViewModels.LogViewModel.TypeBrush));
                    OnPropertyChanged(nameof(ViewModels.LogViewModel.MessageBrush));
                };
            }
        }

        [RelayCommand]
        private async Task CopySelectedLog()
        {
            var text = $"{SelectedLogItem.Timestamp:yyyy-MM-dd HH:mm:ss} {SelectedLogItem.Type}: {SelectedLogItem.Message}";
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var clipboard = desktop.MainWindow?.Clipboard;
                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(text);
                }
            }
        }

        [RelayCommand]
        private async Task CopyAllLogs()
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