namespace ClipFlow.Services
{
    public interface IAutoStartService
    {
        bool IsEnabled { get; }
        void Enable();
        void Disable();
    }
} 