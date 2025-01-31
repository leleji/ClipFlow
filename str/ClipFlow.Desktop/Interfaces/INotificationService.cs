using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipFlow.Interfaces
{
    public interface INotification: IDisposable
    {
        void Initialize();
        Task ShowNotificationAsync(string title, string message);
    }
}
