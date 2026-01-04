using ClipFlow.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ClipFlow.Core.Interfaces
{
    public interface IClipboardWatcher : IDisposable
    {
        // 定义一个标准事件
        event Action<ClipboardData>? ClipboardChanged;

        // 可以在这里定义启动和停止方法
        void Start();
        void Stop();

    }
}
