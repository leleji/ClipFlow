using System;
using System.Net.WebSockets;

namespace ClipFlow.Desktop.Interfaces
{
    public interface IClipboardSyncService : IDisposable
    {
        event Action<WebSocketState> OnWebSocketStateChanged;

        void Start();
        void Stop();
        void UpdateBaseUrl(string url);
        void UpdateHeaders(string token);
        WebSocketState GetWebSocketState();
    }
}