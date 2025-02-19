using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipFlow.Desktop.Interfaces
{
    public interface INotificationService : IDisposable
    {
        void Initialize();
        Task ShowNotificationAsync(string title, string message);
    }
}
