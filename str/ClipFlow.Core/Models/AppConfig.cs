using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Text.Json.Serialization;

namespace ClipFlow.Core.Models
{
    public partial class AppConfig : ObservableObject
    {


        #region 基础配置

        private string _host = string.Empty;
        /// <summary>服务器地址</summary>
        [JsonPropertyName("Host")]
        public string Host
        {
            get => _host;
            set => SetProperty(ref _host, value);
        }

        private string _token = string.Empty;
        /// <summary>用户认证令牌</summary>
        [JsonPropertyName("Token")]
        public string Token
        {
            get => _token;
            set => SetProperty(ref _token, value);
        }

        private string _dataKey = string.Empty;
        /// <summary>数据加密密钥</summary>
        [JsonPropertyName("DataKey")]
        public string DataKey
        {
            get => _dataKey;
            set => SetProperty(ref _dataKey, value);
        }

        private string _userKey = string.Empty;
        /// <summary>userKey 用于标识用户的唯一键</summary>
        [JsonPropertyName("UserKey")]
        public string UserKey
        {
            get => _userKey;
            set => SetProperty(ref _userKey, value);
        }

        #endregion

        #region 系统状态

        private bool _isEnabled;
        /// <summary>是否启用同步功能</summary>
        [JsonPropertyName("IsEnabled")]
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        private int _themeMode;
        /// <summary>主题模式：0-跟随系统，1-浅色，2-深色</summary>
        [JsonPropertyName("ThemeMode")]
        public int ThemeMode
        {
            get => _themeMode;
            set { 
                SetProperty(ref _themeMode, value);
                if (Application.Current != null)
                {
                    Application.Current.RequestedThemeVariant = value switch
                    {
                        0 => null, // 跟随系统
                        1 => ThemeVariant.Light,
                        2 => ThemeVariant.Dark,
                        _ => null
                    };
                }
            }
        }




        private bool _minimizeToTray = true;
        /// <summary>是否最小化到托盘</summary>
        [JsonPropertyName("MinimizeToTray")]
        public bool MinimizeToTray
        {
            get => _minimizeToTray;
            set => SetProperty(ref _minimizeToTray, value);
        }

        private bool _allowErrorUpload = true;
        /// <summary> 是否允许上传错误 / 诊断数据默认 true</summary>
        [JsonPropertyName("AllowErrorUpload")]
        public bool AllowErrorUpload
        {
            get => _allowErrorUpload;
            set => SetProperty(ref _allowErrorUpload, value);
        }


        #endregion

        #region 上传配置

        private bool _enableUpload = true;
        /// <summary>是否启用上传功能</summary>
        [JsonPropertyName("EnableUpload")]
        public bool EnableUpload
        {
            get => _enableUpload;
            set => SetProperty(ref _enableUpload, value);
        }

        private bool _enableUploadText = true;
        /// <summary>是否启用文本上传</summary>
        [JsonPropertyName("EnableUploadText")]
        public bool EnableUploadText
        {
            get => _enableUploadText;
            set => SetProperty(ref _enableUploadText, value);
        }

        private bool _enableUploadImage = true;
        /// <summary>是否启用图片上传</summary>
        [JsonPropertyName("EnableUploadImage")]
        public bool EnableUploadImage
        {
            get => _enableUploadImage;
            set => SetProperty(ref _enableUploadImage, value);
        }

        private bool _enableUploadFile = true;
        /// <summary>是否启用文件上传</summary>
        [JsonPropertyName("EnableUploadFile")]
        public bool EnableUploadFile
        {
            get => _enableUploadFile;
            set => SetProperty(ref _enableUploadFile, value);
        }

        private bool _enableUploadMultiple = false;
        /// <summary>是否启用多文件上传</summary>
        [JsonPropertyName("EnableUploadMultiple")]
        public bool EnableUploadMultiple
        {
            get => _enableUploadMultiple;
            set => SetProperty(ref _enableUploadMultiple, value);
        }

        #endregion

        #region 下载配置

        private bool _enableDownload = true;
        /// <summary>是否启用下载功能</summary>
        [JsonPropertyName("EnableDownload")]
        public bool EnableDownload
        {
            get => _enableDownload;
            set => SetProperty(ref _enableDownload, value);
        }

        private bool _enableDownloadText = true;
        /// <summary>是否启用文本下载</summary>
        [JsonPropertyName("EnableDownloadText")]
        public bool EnableDownloadText
        {
            get => _enableDownloadText;
            set => SetProperty(ref _enableDownloadText, value);
        }

        private bool _enableDownloadFile = true;
        /// <summary>是否启用文件下载</summary>
        [JsonPropertyName("EnableDownloadFile")]
        public bool EnableDownloadFile
        {
            get => _enableDownloadFile;
            set => SetProperty(ref _enableDownloadFile, value);
        }

        #endregion

        #region 限制与通知

        private int _maxTextLength = 0;
        /// <summary>文本最大长度限制（0表示不限制）</summary>
        [JsonPropertyName("MaxTextLength")]
        public int MaxTextLength
        {
            get => _maxTextLength;
            set => SetProperty(ref _maxTextLength, value);
        }

        private ulong _maxUploadFileSize = 20;
        /// <summary>上传文件大小限制，单位：Mb（0表示不限制）</summary>
        [JsonPropertyName("MaxUploadFileSize")]
        public ulong MaxUploadFileSize
        {
            get => _maxUploadFileSize;
            set => SetProperty(ref _maxUploadFileSize, value);
        }

        private uint _maxDownloadFileSize = 0;
        /// <summary>下载文件大小限制，单位：Mb（0表示不限制）</summary>
        [JsonPropertyName("MaxDownloadFileSize")]
        public uint MaxDownloadFileSize
        {
            get => _maxDownloadFileSize;
            set => SetProperty(ref _maxDownloadFileSize, value);
        }

        private bool _enableUploadNotification = true;
        /// <summary>是否启用上传通知</summary>
        [JsonPropertyName("EnableUploadNotification")]
        public bool EnableUploadNotification
        {
            get => _enableUploadNotification;
            set => SetProperty(ref _enableUploadNotification, value);
        }

        private bool _enableDownloadNotification = true;
        /// <summary>是否启用下载通知</summary>
        [JsonPropertyName("EnableDownloadNotification")]
        public bool EnableDownloadNotification
        {
            get => _enableDownloadNotification;
            set => SetProperty(ref _enableDownloadNotification, value);
        }

        #endregion

        #region 过滤与模式

        private bool _isFileExtensionWhitelist = true;
        /// <summary>文件后缀过滤模式：true-黑名单，false-白名单</summary>
        [JsonPropertyName("IsFileExtensionWhitelist")]
        public bool IsFileExtensionWhitelist
        {
            get => _isFileExtensionWhitelist;
            set => SetProperty(ref _isFileExtensionWhitelist, value);
        }

        private string _fileExtensions = string.Empty;
        /// <summary>文件后缀列表，以逗号分隔</summary>
        [JsonPropertyName("FileExtensions")]
        public string FileExtensions
        {
            get => _fileExtensions;
            set => SetProperty(ref _fileExtensions, value);
        }

        private bool _isProcessNameWhitelist = true;
        /// <summary>进程名过滤模式：true-黑名单，false-白名单</summary>
        [JsonPropertyName("IsProcessNameWhitelist")]
        public bool IsProcessNameWhitelist
        {
            get => _isProcessNameWhitelist;
            set => SetProperty(ref _isProcessNameWhitelist, value);
        }

        private string _processNames = string.Empty;
        /// <summary>进程名列表，以逗号分隔</summary>
        [JsonPropertyName("ProcessNames")]
        public string ProcessNames
        {
            get => _processNames;
            set => SetProperty(ref _processNames, value);
        }

        private int _clipboardMonitorMode = 0;
        /// <summary>剪贴板监控模式：0-高效模式，1-兼容模式</summary>
        [JsonPropertyName("ClipboardMonitorMode")]
        public int ClipboardMonitorMode
        {
            get => _clipboardMonitorMode;
            set => SetProperty(ref _clipboardMonitorMode, value);
        }

        #endregion

    }
}