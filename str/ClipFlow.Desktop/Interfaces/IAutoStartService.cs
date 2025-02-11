namespace ClipFlow.Desktop.Interfaces
{
    public interface IAutoStartService
    {
        bool IsEnabled { get; }
        void Enable();
        void Disable();
    }
}