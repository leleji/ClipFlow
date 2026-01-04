using ClipFlow.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ClipFlow.Core.ViewModels
{
    public partial class UploadSettingsViewModel : ViewModelBase
    {
        public ConfigService ConfigService { get; }


        [ObservableProperty]
        private bool isProcessNameFilterVisible;


        public UploadSettingsViewModel(ConfigService configService)
        {
            ConfigService = configService;
            // 在Linux系统下隐藏进程名过滤功能
            isProcessNameFilterVisible = !RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        }


     
    }
} 