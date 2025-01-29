using CommunityToolkit.Mvvm.ComponentModel;

namespace ClipFlow.ViewModels
{
    public partial class AboutViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _title = "关于";

        [ObservableProperty]
        private string _description = "ClipFlow 版本 1.0";
    }
} 