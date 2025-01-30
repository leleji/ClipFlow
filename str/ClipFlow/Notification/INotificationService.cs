using System.Threading.Tasks;

namespace ClipFlow.Notification
{
    public interface INotificationService
    {
        void Initialize();
        Task ShowNotificationAsync(string title, string message);
        void Dispose();
    }
}