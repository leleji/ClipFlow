using CommunityToolkit.Mvvm.ComponentModel;
using ClipFlow.Desktop.Services;

namespace ClipFlow.Desktop.ViewModels
{
    public partial class DownloadSettingsViewModel : ViewModelBase
    {
        private readonly ConfigService _configService = ConfigService.Instance;

        [ObservableProperty]
        private bool enableDownload;

        [ObservableProperty]
        private bool enableDownloadText;

        [ObservableProperty]
        private bool enableDownloadImage;

        [ObservableProperty]
        private bool enableDownloadFile;

        [ObservableProperty]
        private uint maxDownloadFileSize;

        [ObservableProperty]
        private bool _enableDownloadNotification;

        public DownloadSettingsViewModel()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            EnableDownload = _configService.CurrentConfig.EnableDownload;
            EnableDownloadText = _configService.CurrentConfig.EnableDownloadText;
            EnableDownloadImage = _configService.CurrentConfig.EnableDownloadImage;
            EnableDownloadFile = _configService.CurrentConfig.EnableDownloadFile;
            MaxDownloadFileSize = _configService.CurrentConfig.MaxDownloadFileSize;
            EnableDownloadNotification = _configService.CurrentConfig.EnableDownloadNotification;
        }

        partial void OnEnableDownloadChanged(bool value)
        {
            _configService.CurrentConfig.EnableDownload = value;
            _configService.SaveConfig();
        }

        partial void OnEnableDownloadTextChanged(bool value)
        {
            _configService.CurrentConfig.EnableDownloadText = value;
            _configService.SaveConfig();
        }

        partial void OnEnableDownloadImageChanged(bool value)
        {
            _configService.CurrentConfig.EnableDownloadImage = value;
            _configService.SaveConfig();
        }

        partial void OnEnableDownloadFileChanged(bool value)
        {
            _configService.CurrentConfig.EnableDownloadFile = value;
            _configService.SaveConfig();
        }

        partial void OnMaxDownloadFileSizeChanged(uint value)
        {
            _configService.CurrentConfig.MaxDownloadFileSize = value;
            _configService.SaveConfig();
        }

        partial void OnEnableDownloadNotificationChanged(bool value)
        {
            _configService.CurrentConfig.EnableDownloadNotification = value;
            _configService.SaveConfig();
        }
    }
} 