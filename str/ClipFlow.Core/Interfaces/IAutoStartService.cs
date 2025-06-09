namespace ClipFlow.Core.Interfaces
{
    public interface IAutoStartService
    {
        bool IsEnabled { get; }
        void Enable();
        void Disable();
    }
}