using System;
using System.Net.WebSockets;

namespace ClipFlow.Core.Interfaces
{
    public interface IClipboardSyncService : IDisposable
    {
        event Action<WebSocketState> OnWebSocketStateChanged;

        void Start();
        void Stop();
        WebSocketState GetWebSocketState();
    }
}