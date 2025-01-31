using System;
using System.Threading.Tasks;
using System.Net.WebSockets;
using ClipFlow.Models;
using System.Text.Json;
using System.Threading;
using System.Text;

namespace ClipFlow.Desktop.Services
{
    public class WebSocketService : IDisposable
    {
        private ClientWebSocket _webSocket;
        private readonly string _clientId;
        private readonly string _token;
        private readonly string _userKey;
        private readonly string _wsUrl;
        private readonly Func<ClipboardData, Task> _onMessageReceived;
        private CancellationTokenSource _cancellationTokenSource;
        
        private const int ReconnectDelay = 5000; // 5秒后重连
        private const int BufferSize = 1024 * 4;
        private const int PingInterval = 10000; // 10秒发送一次ping

        public event EventHandler<WebSocketState> StateChanged;
        public event EventHandler<Exception> ErrorOccurred;

        private WebSocketState _state;
        public WebSocketState State
        {
            get => _state;
            private set
            {
                if (_state != value)
                {
                    _state = value;
                    StateChanged?.Invoke(this, _state);
                }
            }
        }

        public WebSocketService(
            string wsUrl,
            string clientId,
            string token,
            string userKey,
            Func<ClipboardData, Task> onMessageReceived)
        {
            _wsUrl = wsUrl ?? throw new ArgumentNullException(nameof(wsUrl));
            _clientId = clientId;
            _token = token;
            _userKey = userKey;
            _onMessageReceived = onMessageReceived ?? throw new ArgumentNullException(nameof(onMessageReceived));
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    await CleanupAsync();
                    State = WebSocketState.Connecting;

                    _webSocket = new ClientWebSocket();
                    ConfigureWebSocket(_webSocket);

                    await _webSocket.ConnectAsync(new Uri(_wsUrl), _cancellationTokenSource.Token);
                    State = WebSocketState.Open;
                    LogService.Instance.AddLog("提示", "WebSocket连接成功");

                    // 启动消息接收和心跳
                    var receiveTask = ReceiveMessages();
                    var pingTask = StartPingAsync();
                    
                    // 等待任意一个任务完成
                    await Task.WhenAny(receiveTask, pingTask);

                    // 如果连接断开，等待重连
                    if (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        State = WebSocketState.Connecting;
                        await Task.Delay(ReconnectDelay, _cancellationTokenSource.Token);
                    }
                }
                catch (Exception ex) when (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    HandleError(ex);
                    await Task.Delay(ReconnectDelay, _cancellationTokenSource.Token);
                }
            }
        }

        private void ConfigureWebSocket(ClientWebSocket webSocket)
        {
            webSocket.Options.SetRequestHeader("X-Auth-Token", _token);
            webSocket.Options.SetRequestHeader("X-Client-Id", _clientId);
            webSocket.Options.SetRequestHeader("X-User-Key", _userKey);
            webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
        }

        private async Task ReceiveMessages()
        {
            var buffer = new byte[BufferSize];
            var messageBuffer = new StringBuilder();

            while (_webSocket.State == WebSocketState.Open && 
                   !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        _cancellationTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        if (message != "pong")
                        {
                            await HandleMessageAsync(message);
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        State = WebSocketState.Closed;
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    HandleError(ex);
                    break;
                }
            }
        }

        private async Task HandleMessageAsync(string message)
        {
            try
            {
                var clipboardData = JsonSerializer.Deserialize<ClipboardData>(message);
                if (clipboardData != null)
                {
                    await _onMessageReceived(clipboardData);
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }

        private async Task StartPingAsync()
        {
            while (_webSocket?.State == WebSocketState.Open)
            {
                try
                {
                    await Task.Delay(PingInterval, _cancellationTokenSource.Token);
                    await SendMessageAsync("ping");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    HandleError(ex);
                    break;
                }
            }
        }

        private async Task SendMessageAsync(string message)
        {
            if (_webSocket?.State != WebSocketState.Open) return;

            var bytes = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                _cancellationTokenSource.Token);
        }

        private void HandleError(Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
        }

        public async Task StopAsync()
        {
            try
            {
                _cancellationTokenSource.Cancel();

                if (_webSocket?.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "User disconnected",
                        CancellationToken.None);
                }

                await CleanupAsync();
                State = WebSocketState.Closed;
                LogService.Instance.AddLog("提示", "WebSocket连接已关闭");
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }

        private async Task CleanupAsync()
        {
            if (_webSocket != null)
            {
                try
                {
                    if (_webSocket.State == WebSocketState.Open)
                    {
                        await _webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Cleanup",
                            CancellationToken.None);
                    }
                }
                catch
                {
                    // 忽略清理过程中的错误
                }
                finally
                {
                    _webSocket.Dispose();
                    _webSocket = null;
                }
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _webSocket?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
} 