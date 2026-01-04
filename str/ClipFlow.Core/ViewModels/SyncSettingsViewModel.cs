using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;
using ClipFlow.Core.Services;
using System.Net.WebSockets;
using ClipFlow.Core.Interfaces;
using System.Data;

namespace ClipFlow.Core.ViewModels
{
    public partial class SyncSettingsViewModel : ViewModelBase, IDisposable
    {
        public ConfigService ConfigService { get; }


        private readonly IClipboardSyncService _clipboardSyncService;
        private Action<WebSocketState>? _webSocketStateHandler;

        [ObservableProperty]
        private bool _isEnabled;

        [ObservableProperty]
        private string? _serverStatus;

        public SyncSettingsViewModel(
            ConfigService configService,
            IClipboardSyncService clipboardSyncService)
        {
            ConfigService = configService;
            _clipboardSyncService = clipboardSyncService;
            
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
            _isEnabled = ConfigService.CurrentConfig.IsEnabled;

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

            ConfigService.CurrentConfig.IsEnabled = value;
            ConfigService.SaveConfig();
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