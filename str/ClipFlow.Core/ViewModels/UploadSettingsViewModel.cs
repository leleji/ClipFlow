using ClipFlow.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ClipFlow.Core.ViewModels
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

            PropertyChanged += _configService.SettingsViewModel_PropertyChanged;
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

     
    }
} 