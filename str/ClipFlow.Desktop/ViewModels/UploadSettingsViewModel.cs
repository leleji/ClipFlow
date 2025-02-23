using CommunityToolkit.Mvvm.ComponentModel;
using ClipFlow.Desktop.Services;
using System.Runtime.InteropServices;

namespace ClipFlow.Desktop.ViewModels
{
    public partial class UploadSettingsViewModel : ViewModelBase
    {
        private readonly ConfigService _configService;

        [ObservableProperty]
        private bool isProcessNameFilterVisible;

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
        private ulong maxUploadFileSize;

        [ObservableProperty]
        private bool enableUploadNotification;

        [ObservableProperty]
        private bool isFileExtensionWhitelist;

        [ObservableProperty]
        private string fileExtensions = string.Empty;

        [ObservableProperty]
        private bool isProcessNameWhitelist;

        [ObservableProperty]
        private string processNames = string.Empty;

        public UploadSettingsViewModel(ConfigService configService)
        {
            _configService = configService;
            // 在Linux系统下隐藏进程名过滤功能
            isProcessNameFilterVisible = !RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
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
            EnableUploadNotification = _configService.CurrentConfig.EnableUploadNotification;
            IsFileExtensionWhitelist = _configService.CurrentConfig.IsFileExtensionWhitelist;
            FileExtensions = _configService.CurrentConfig.FileExtensions;
            IsProcessNameWhitelist = _configService.CurrentConfig.IsProcessNameWhitelist;
            ProcessNames = _configService.CurrentConfig.ProcessNames;
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

        partial void OnMaxUploadFileSizeChanged(ulong value)
        {
            _configService.CurrentConfig.MaxUploadFileSize = value;
            _configService.SaveConfig();
        }

        partial void OnEnableUploadNotificationChanged(bool value)
        {
            _configService.CurrentConfig.EnableUploadNotification = value;
            _configService.SaveConfig();
        }

        partial void OnIsFileExtensionWhitelistChanged(bool value)
        {
            _configService.CurrentConfig.IsFileExtensionWhitelist = value;
            _configService.SaveConfig();
        }

        partial void OnFileExtensionsChanged(string value)
        {
            _configService.CurrentConfig.FileExtensions = value;
            _configService.SaveConfig();
        }

        partial void OnIsProcessNameWhitelistChanged(bool value)
        {
            _configService.CurrentConfig.IsProcessNameWhitelist = value;
            _configService.SaveConfig();
        }

        partial void OnProcessNamesChanged(string value)
        {
            _configService.CurrentConfig.ProcessNames = value;
            _configService.SaveConfig();
        }
    }
} 