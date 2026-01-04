using CommunityToolkit.Mvvm.ComponentModel;
using ClipFlow.Core.Services;

namespace ClipFlow.Core.ViewModels
{
    public partial class DownloadSettingsViewModel : ViewModelBase
    {
        public ConfigService ConfigService { get; }


        public DownloadSettingsViewModel(ConfigService configService)
        {
            ConfigService = configService;
        }

 

    
    }
} 