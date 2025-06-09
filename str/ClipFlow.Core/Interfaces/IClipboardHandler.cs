using ClipFlow.Core.Models;
using System.Threading.Tasks;

namespace ClipFlow.Core.Interfaces
{
    public interface IClipboardHandler
    {
        Task<bool> SetContentAsync(ClipboardData data, bool isServerUpdate = true);
        Task<ClipboardData?> GetContentAsync();
        
        void Initialize();
        void Cleanup();
    }
}