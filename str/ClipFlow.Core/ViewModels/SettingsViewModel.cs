using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using ClipFlow.Core.Services;
using ClipFlow.Core.Interfaces;

namespace ClipFlow.Core.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly ConfigService _configService;
        private readonly IAutoStartService _autoStartService;

        [ObservableProperty]
        private bool _autoStart;

        [ObservableProperty]
        private bool _minimizeToTray;

        [ObservableProperty]
        private int _themeMode;

        [ObservableProperty]
        private int _clipboardMonitorMode;

        [ObservableProperty]
        private string _title = "设置";

        [ObservableProperty]
        private string _description = "在这里管理您的设置";

        public SettingsViewModel(ConfigService configService, IAutoStartService autoStartService)
        {
            _configService = configService;
            _autoStartService = autoStartService;

            // 加载配置
            _autoStart = _autoStartService.IsEnabled;
            _minimizeToTray = _configService.CurrentConfig.MinimizeToTray;
            _themeMode = _configService.CurrentConfig.ThemeMode;
            _clipboardMonitorMode = _configService.CurrentConfig.ClipboardMonitorMode;
        }

        partial void OnAutoStartChanged(bool value)
        {
            if (value)
            {
                _autoStartService.Enable();
            }
            else
            {
                _autoStartService.Disable();
            }
        }

        partial void OnMinimizeToTrayChanged(bool value)
        {
            _configService.CurrentConfig.MinimizeToTray = value;
            _configService.SaveConfig();
        }

        partial void OnThemeModeChanged(int value)
        {
            _configService.CurrentConfig.ThemeMode = value;
            _configService.SaveConfig();

            if (Application.Current != null)
            {
                Application.Current.RequestedThemeVariant = value switch
                {
                    0 => null, // 跟随系统
                    1 => ThemeVariant.Light,
                    2 => ThemeVariant.Dark,
                    _ => null
                };
            }
        }

        partial void OnClipboardMonitorModeChanged(int value)
        {
            _configService.CurrentConfig.ClipboardMonitorMode = value;
            _configService.SaveConfig();
        }
    }
} 