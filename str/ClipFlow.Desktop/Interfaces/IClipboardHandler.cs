using ClipFlow.Models;
using System.Threading.Tasks;

namespace ClipFlow.Desktop.Interfaces
{
    public interface IClipboardHandler
    {
        Task<bool> SetContentAsync(ClipboardData data, bool isServerUpdate = true);
        Task<ClipboardData> GetContentAsync();
        void Initialize();
        void Cleanup();
    }
}