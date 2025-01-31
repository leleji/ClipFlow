using CommunityToolkit.Mvvm.ComponentModel;
using ClipFlow.Desktop.Services;

namespace ClipFlow.Desktop.ViewModels
{
    public partial class UploadSettingsViewModel : ViewModelBase
    {
        private readonly ConfigService _configService = ConfigService.Instance;

        [ObservableProperty]
        private bool enableUpload;

        [ObservableProperty]
        private bool enableUploadText;

        [ObservableProperty]
        private bool enableUploadImage;

        [ObservableProperty]
        private bool enableUploadFile;

        [ObservableProperty]
        private bool enableUploadMultiple;

        [ObservableProperty]
        private int maxTextLength;

        [ObservableProperty]
        private int maxUploadFileSize;

        [ObservableProperty]
        private bool _enableUploadNotification;

        public UploadSettingsViewModel()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            EnableUpload = _configService.CurrentConfig.EnableUpload;
            EnableUploadText = _configService.CurrentConfig.EnableUploadText;
            EnableUploadImage = _configService.CurrentConfig.EnableUploadImage;
            EnableUploadFile = _configService.CurrentConfig.EnableUploadFile;
            EnableUploadMultiple = _configService.CurrentConfig.EnableUploadMultiple;
            MaxTextLength = _configService.CurrentConfig.MaxTextLength;
            MaxUploadFileSize = _configService.CurrentConfig.MaxUploadFileSize;
            _enableUploadNotification = _configService.CurrentConfig.EnableUploadNotification;
        }

        partial void OnEnableUploadChanged(bool value)
        {
            _configService.CurrentConfig.EnableUpload = value;
            _configService.SaveConfig();
        }

        partial void OnEnableUploadTextChanged(bool value)
        {
            _configService.CurrentConfig.EnableUploadText = value;
            _configService.SaveConfig();
        }

        partial void OnEnableUploadImageChanged(bool value)
        {
            _configService.CurrentConfig.EnableUploadImage = value;
            _configService.SaveConfig();
        }

        partial void OnEnableUploadFileChanged(bool value)
        {
            _configService.CurrentConfig.EnableUploadFile = value;
            _configService.SaveConfig();
        }

        partial void OnEnableUploadMultipleChanged(bool value)
        {
            _configService.CurrentConfig.EnableUploadMultiple = value;
            _configService.SaveConfig();
        }

        partial void OnMaxTextLengthChanged(int value)
        {
            _configService.CurrentConfig.MaxTextLength = value;
            _configService.SaveConfig();
        }

        partial void OnMaxUploadFileSizeChanged(int value)
        {
            _configService.CurrentConfig.MaxUploadFileSize = value;
            _configService.SaveConfig();
        }

        partial void OnEnableUploadNotificationChanged(bool value)
        {
            _configService.CurrentConfig.EnableUploadNotification = value;
            _configService.SaveConfig();
        }
    }
} 