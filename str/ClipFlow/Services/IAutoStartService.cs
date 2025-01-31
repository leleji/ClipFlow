namespace ClipFlow.Desktop.Services
{
    public interface IAutoStartService
    {
        bool IsEnabled { get; }
        void Enable();
        void Disable();
    }
} 