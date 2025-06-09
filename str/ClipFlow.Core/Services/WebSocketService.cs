using System;
using System.Threading.Tasks;
using System.Net.WebSockets;
using ClipFlow.Core.Models;
using System.Text.Json;
using System.Threading;
using System.Text;

namespace ClipFlow.Core.Services
{
    public class WebSocketService : IDisposable
    {
        private readonly string _wsUrl;
        private readonly string _clientId;
        private readonly string _token;
        private readonly Func<ClipboardData, Task> _notificationHandler;
        private ClientWebSocket? _webSocket;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isManualClosed;
        private const int ReconnectDelay = 5000; // 5秒后重连
        private const int PingInterval = 10000; // 10秒发送一次ping

        public event EventHandler<WebSocketState>? StateChanged;
        public event EventHandler<Exception>? ErrorOccurred;

        public WebSocketService(
            string wsUrl,
            string clientId,
            string token,
            Func<ClipboardData, Task> notificationHandler)
        {
            _wsUrl = wsUrl;
            _clientId = clientId;
            _token = token;
            _notificationHandler = notificationHandler;
        }

        public async Task StartAsync()
        {
            _isManualClosed = false;
            await ConnectAsync();
        }

        private async Task ConnectAsync()
        {
            while (!_isManualClosed)
            {
                try
                {
                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource = new CancellationTokenSource();
                    _webSocket?.Dispose();
                    _webSocket = new ClientWebSocket();

                    _webSocket.Options.SetRequestHeader("X-Auth-Token", _token);
                    _webSocket.Options.SetRequestHeader("X-Client-Id", _clientId);

                    await _webSocket.ConnectAsync(new Uri(_wsUrl), _cancellationTokenSource.Token);
                    StateChanged?.Invoke(this, _webSocket.State);

                    _ = StartReceivingAsync();
                    _ = StartPingAsync();
                    
                    // 连接成功，退出重连循环
                    return;
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, ex);
                    StateChanged?.Invoke(this, WebSocketState.Aborted);

                    // 等待5秒后重试
                    if (!_isManualClosed)
                    {
                        await Task.Delay(ReconnectDelay, _cancellationTokenSource.Token);
                    }
                }
            }
        }

        private async Task StartReceivingAsync()
        {
            var buffer = new byte[1024 * 4];
            try
            {
                while (!_isManualClosed && _webSocket?.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        _cancellationTokenSource?.Token ?? CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        if (message != "pong")
                        {
                            try
                            {
                                var data = JsonSerializer.Deserialize<ClipboardData>(message);
                                if (data != null)
                                {
                                    await _notificationHandler(data);
                                }
                            }
                            catch (Exception ex)
                            {
                                ErrorOccurred?.Invoke(this, ex);
                            }
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
            finally
            {
                if (!_isManualClosed)
                {
                    StateChanged?.Invoke(this, WebSocketState.Aborted);
                    await ConnectAsync();
                }
            }
        }

        private async Task StartPingAsync()
        {
            try
            {
                while (!_isManualClosed && _webSocket?.State == WebSocketState.Open)
                {
                    await Task.Delay(PingInterval, _cancellationTokenSource?.Token ?? CancellationToken.None);
                    if (_webSocket?.State == WebSocketState.Open)
                    {
                        await _webSocket.SendAsync(
                            Encoding.UTF8.GetBytes("ping"),
                            WebSocketMessageType.Text,
                            true,
                            _cancellationTokenSource?.Token ?? CancellationToken.None);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
        }

        public async Task StopAsync()
        {
            _isManualClosed = true;
            
            try
            {
                if (_webSocket?.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
            finally
            {
                _cancellationTokenSource?.Cancel();
                _webSocket?.Dispose();
                _webSocket = null;
                StateChanged?.Invoke(this, WebSocketState.Closed);
            }
        }

        public void Dispose()
        {
            _isManualClosed = true;
            _ = StopAsync();
            _cancellationTokenSource?.Dispose();
            _webSocket?.Dispose();
        }

        public WebSocketState GetState()
        {
            return _webSocket?.State ?? WebSocketState.None;
        }
    }
} 