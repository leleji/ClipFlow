using CommunityToolkit.Mvvm.ComponentModel;

namespace ClipFlow.ViewModels
{
    public partial class AboutViewModel : ViewModelBase
    {

        [ObservableProperty]
        private string _description = "ClipFlow 版本 0.0.1";
    }
} 