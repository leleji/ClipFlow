using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using ClipFlow.Desktop.Services;

namespace ClipFlow.Desktop.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly ConfigService _configService = ConfigService.Instance;
        private readonly IAutoStartService _autoStartService = new AutoStartService();

        [ObservableProperty]
        private bool _autoStart;

        [ObservableProperty]
        private bool _minimizeToTray;

        [ObservableProperty]
        private int _themeMode;

        [ObservableProperty]
        private string _title = "设置";

        [ObservableProperty]
        private string _description = "在这里管理您的设置";

        [ObservableProperty]
        private bool _hideOnStartup;

        public SettingsViewModel()
        {
            // 加载配置
            _autoStart = _autoStartService.IsEnabled;
            _minimizeToTray = _configService.CurrentConfig.MinimizeToTray;
            _hideOnStartup = _configService.CurrentConfig.HideOnStartup;
            _themeMode = _configService.CurrentConfig.ThemeMode;
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

            var app = Application.Current;
            if (app != null)
            {
                app.RequestedThemeVariant = value switch
                {
                    0 => null, // 跟随系统
                    1 => ThemeVariant.Light,
                    2 => ThemeVariant.Dark,
                    _ => null
                };
            }
        }

        partial void OnHideOnStartupChanged(bool value)
        {
            _configService.CurrentConfig.HideOnStartup = value;
            _configService.SaveConfig();
        }
    }
} 