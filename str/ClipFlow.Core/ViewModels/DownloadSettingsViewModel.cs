using CommunityToolkit.Mvvm.ComponentModel;
using ClipFlow.Core.Services;

namespace ClipFlow.Core.ViewModels
{
    public partial class DownloadSettingsViewModel : ViewModelBase
    {
        private readonly ConfigService _configService;

        [ObservableProperty]
        private bool _enableDownload;

        [ObservableProperty]
        private bool _enableDownloadText;

        [ObservableProperty]
        private bool _enableDownloadFile;

        [ObservableProperty]
        private uint _maxDownloadFileSize;

        [ObservableProperty]
        private bool _enableDownloadNotification;

        public DownloadSettingsViewModel(ConfigService configService)
        {
            _configService = configService;
            LoadSettings();
            PropertyChanged += _configService.SettingsViewModel_PropertyChanged;
        }

        private void LoadSettings()
        {
            EnableDownload = _configService.CurrentConfig.EnableDownload;
            EnableDownloadText = _configService.CurrentConfig.EnableDownloadText;
            EnableDownloadFile = _configService.CurrentConfig.EnableDownloadFile;
            MaxDownloadFileSize = _configService.CurrentConfig.MaxDownloadFileSize;
            EnableDownloadNotification = _configService.CurrentConfig.EnableDownloadNotification;
        }

    
    }
} 