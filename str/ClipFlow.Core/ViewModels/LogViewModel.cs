using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClipFlow.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ClipFlow.Common.Helpers;
using ClipFlow.Common.Models;

namespace ClipFlow.Core.ViewModels
{
    public partial class LogViewModel : ViewModelBase
    {
        private readonly LogService _logService = LogService.Instance;

        public event EventHandler LogItemsChanged;

        [ObservableProperty]
        private LogItem _selectedLogItem;

        public IEnumerable<LogItem> LogItems => _logService.LogItems;



        public LogViewModel()
        {
            _logService.LogAdded += (s, e) => LogItemsChanged?.Invoke(this, EventArgs.Empty);
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