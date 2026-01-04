using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using ClipFlow.Core.Services;
using ClipFlow.Core.Interfaces;

namespace ClipFlow.Core.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {

        public ConfigService ConfigService { get; }

        private readonly IAutoStartService _autoStartService;

        [ObservableProperty]
        private bool _autoStart;

        public SettingsViewModel(ConfigService configService, IAutoStartService autoStartService)
        {
            ConfigService= configService;
            _autoStartService = autoStartService;

            // 加载配置
            _autoStart = _autoStartService.IsEnabled;
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
    }
} 