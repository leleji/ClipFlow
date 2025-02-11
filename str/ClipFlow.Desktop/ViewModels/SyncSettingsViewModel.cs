using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;
using ClipFlow.Desktop.Services;
using System.Net.WebSockets;
using ClipFlow.Desktop.Interfaces;
using System.Data;

namespace ClipFlow.Desktop.ViewModels
{
    public partial class SyncSettingsViewModel : ViewModelBase, IDisposable
    {
        private readonly ConfigService _configService;
        private readonly IClipboardSyncService _clipboardSyncService;
        private Action<WebSocketState>? _webSocketStateHandler;

        [ObservableProperty]
        private bool _isEnabled;

        [ObservableProperty]
        private string _host;

        [ObservableProperty]
        private string _token;

        [ObservableProperty]
        private string _userKey;

        [ObservableProperty]
        private string _dataKey;

        [ObservableProperty]
        private string _serverStatus;

        public SyncSettingsViewModel(
            ConfigService configService,
            IClipboardSyncService clipboardSyncService)
        {
            _configService = configService;
            _clipboardSyncService = clipboardSyncService;
            
            // 加载配置
            _host = _configService.CurrentConfig.Host;
            _token = _configService.CurrentConfig.Token;
            _userKey = _configService.CurrentConfig.UserKey;
            _dataKey = _configService.CurrentConfig.DataKey;
            // 订阅WebSocket状态变化
            _webSocketStateHandler = state =>
            {
                ServerStatus = state switch
                {
                    WebSocketState.Connecting => "服务器状态：正在连接",
                    WebSocketState.Open => "服务器状态：连接成功",
                    WebSocketState.Closed => "服务器状态：连接已关闭",
                    WebSocketState.Aborted => "服务器状态：连接断开，准备重连",
                    WebSocketState.None => "服务器状态：未连接",
                    _ => $"服务器状态：{state}"
                };
            };
            _clipboardSyncService.OnWebSocketStateChanged += _webSocketStateHandler;

            // 设置初始状态
            var currentState = _clipboardSyncService.GetWebSocketState();
            _webSocketStateHandler(currentState);

            // 设置启用状态，但不触发OnIsEnabledChanged
            _isEnabled = _configService.CurrentConfig.IsEnabled;

        }

        #region 配置保存
        partial void OnIsEnabledChanged(bool value)
        {
            if (value)
            {
                _clipboardSyncService.Start();
            }
            else
            {
                _clipboardSyncService.Stop();
            }

            _configService.CurrentConfig.IsEnabled = value;
            _configService.SaveConfig();
        }

        partial void OnHostChanged(string value)
        {
            _configService.CurrentConfig.Host = value;
            _configService.SaveConfig();
        }

        partial void OnTokenChanged(string value)
        {
            _configService.CurrentConfig.Token = value;
            _configService.SaveConfig();
        }

        partial void OnUserKeyChanged(string value)
        {
            _configService.CurrentConfig.UserKey = value;
            _configService.SaveConfig();
        }
        partial void OnDataKeyChanged(string value)
        {
            _configService.CurrentConfig.DataKey = value;
            _configService.SaveConfig();
        }
        #endregion

        public void Dispose()
        {

            if (_webSocketStateHandler != null)
            {
                _clipboardSyncService.OnWebSocketStateChanged -= _webSocketStateHandler;
                _webSocketStateHandler = null;
            }

        }
    }
} 