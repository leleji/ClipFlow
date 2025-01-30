using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using ClipFlow.Services;
using ClipFlow.Models;
using System.Net.Http;
using Avalonia.Threading;
using ClipFlow.Clipboard;
using System.Net.WebSockets;
using System.Collections.Generic;
using System.IO.Compression;
using System.Threading;
using System.Net.Http.Json;
using static System.Net.Mime.MediaTypeNames;
using ClipFlow.Notification;

namespace ClipFlow.ViewModels
{
    public partial class SyncSettingsViewModel : ViewModelBase, IDisposable
    {
        private readonly ConfigService _configService = ConfigService.Instance;
        private readonly ClipboardMonitor _clipboardMonitor;
        private WebSocketService _webSocketService;

        [ObservableProperty]
        private bool _isEnabled;

        [ObservableProperty]
        private string _host;

        [ObservableProperty]
        private string _token;

        [ObservableProperty]
        private string _userKey;

        [ObservableProperty]
        private string _serverStatus;

        [ObservableProperty]
        private bool _isConnectToWebSocket;

        private  string _baseUrl;
        private string _wsUrl;
        private readonly string _clientId = Guid.NewGuid().ToString();
        private readonly HttpClient _httpClient;

        // 添加取消令牌源
        private CancellationTokenSource? _uploadCancellationTokenSource;

        public SyncSettingsViewModel()
        {
            _clipboardMonitor = new ClipboardMonitor();
            _clipboardMonitor.OnClipboardChanged += ClipboardMonitor_OnClipboardChanged;
            
            // 配置 HttpClient
            var handler = new HttpClientHandler
            {
                MaxRequestContentBufferSize = 524288000 // 500MB
            };
            
            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMinutes(30)
            };
            
            // 加载配置
            _host = _configService.CurrentConfig.Host;
            _token = _configService.CurrentConfig.Token;
            _userKey = _configService.CurrentConfig.UserKey;
            UpdateUrls(Host);
            UpdateHeaders();
            IsEnabled = _configService.CurrentConfig.IsEnabled;
        }

        private void UpdateHeaders()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-Auth-Token", _configService.CurrentConfig.Token);
            _httpClient.DefaultRequestHeaders.Add("X-User-Key", _configService.CurrentConfig.UserKey);
            _httpClient.DefaultRequestHeaders.Add("X-Client-Id", _clientId);
        }

        private void UpdateUrls(string url)
        {
            if (string.IsNullOrEmpty(url)) return;

            try
            {
                var uri = new Uri(url.TrimEnd('/'));
                var scheme = uri.Scheme;
                var wsScheme = scheme == "https" ? "wss" : "ws";

                // 构建基础URL
                var baseUri = new UriBuilder(uri)
                {
                    Path = uri.AbsolutePath.TrimEnd('/'),
                }.Uri;

                // 如果路径不包含 api/Clipboard，添加它
                if (!baseUri.AbsolutePath.Contains("api/Clipboard", StringComparison.OrdinalIgnoreCase))
                {
                    _baseUrl = $"{baseUri.ToString().TrimEnd('/')}/api/Clipboard";
                }
                else
                {
                    _baseUrl = baseUri.ToString().TrimEnd('/');
                }

                // 构建WebSocket URL
                var wsUri = new UriBuilder(baseUri)
                {
                    Scheme = wsScheme,
                    Path = $"{baseUri.AbsolutePath.TrimEnd('/')}/api/Clipboard/ws".Replace("//", "/")
                };
                _wsUrl = wsUri.Uri.ToString();

            }
            catch (Exception ex)
            {
                AddLog("错误", $"URL格式错误: {ex.Message}");
            }
        }

        private async void AddLog(string type, string message)
        {
            LogService.Instance.AddLog(type, message);
        }

        private async Task StartWebSocketConnection()
        {
            ServerStatus = "服务器状态：正在连接";

            _webSocketService = new WebSocketService(
                _wsUrl,
                _clientId,
                Token,
                UserKey,
                HandleNotificationAsync
            );

            // 订阅状态变化事件
            _webSocketService.StateChanged += (sender, state) =>
            {
                ServerStatus = state switch
                {
                    WebSocketState.Connecting => "服务器状态：正在连接",
                    WebSocketState.Open => "服务器状态：连接成功",
                    WebSocketState.Closed => "服务器状态：连接已关闭",
                    WebSocketState.Aborted => "服务器状态：连接断开，准备重连",
                    _ => $"服务器状态：{state}"
                };
            };

            // 订阅错误事件
            _webSocketService.ErrorOccurred += (sender, ex) =>
            {
                AddLog("错误", $"WebSocket错误: {ex.Message}");
            };

            try 
            {
                await _webSocketService.StartAsync();
            }
            catch (Exception ex)
            {
                ServerStatus = "服务器状态：连接失败，准备重连";
                AddLog("错误", $"启动WebSocket连接失败: {ex.Message}");
            }
        }

        private bool CheckSyncRestrictions(ClipboardData data)
        {
            var config = ConfigService.Instance.CurrentConfig;

            // 检查总开关
            if (!config.EnableUpload)
            {
                LogService.Instance.AddLog("提示", "上传功能已禁用");
                return false;
            }

            // 根据类型检查具体限制
            switch (data.Type)
            {
                case ClipboardType.Text:
                    if (!config.EnableUploadText)
                    {
                        LogService.Instance.AddLog("提示", "文本上传已禁用");
                        return false;
                    }
                    if (config.MaxTextLength > 0 && data.Data.Length > config.MaxTextLength)
                    {
                        LogService.Instance.AddLog("提示", $"文本长度超过限制: {data.Data.Length}/{config.MaxTextLength}");
                        return false;
                    }
                    break;

                case ClipboardType.File:
                    // 检查是否是图片
                    if (IsImageFile(data.Filename))
                    {
                        if (!config.EnableUploadImage)
                        {
                            LogService.Instance.AddLog("提示", "图片上传已禁用");
                            return false;
                        }
                    }
                    else if (!config.EnableUploadFile)
                    {
                        LogService.Instance.AddLog("提示", "文件上传已禁用");
                        return false;
                    }

                    if (config.MaxUploadFileSize > 0 && data.Data.Length > config.MaxUploadFileSize * 1024 * 1024)
                    {
                        LogService.Instance.AddLog("提示", $"文件大小超过限制: {data.Data.Length / 1024 / 1024}MB/{config.MaxUploadFileSize}MB");
                        return false;
                    }
                    break;

                case ClipboardType.FileList:
                    if (!config.EnableUploadMultiple)
                    {
                        LogService.Instance.AddLog("提示", "多文件上传已禁用");
                        return false;
                    }
                    if (config.MaxUploadFileSize > 0 && data.Data.Length > config.MaxUploadFileSize * 1024 * 1024)
                    {
                        LogService.Instance.AddLog("提示", $"压缩包大小超过限制: {data.Data.Length / 1024 / 1024}MB/{config.MaxUploadFileSize}MB");
                        return false;
                    }
                    break;
            }

            return true;
        }

        private bool IsImageFile(string filename)
        {
            var extension = Path.GetExtension(filename).ToLower();
            return new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".ico", ".tiff" }.Contains(extension);
        }

        private async Task HandleNotificationAsync(ClipboardData data)
        {
            try
            {
                var config = ConfigService.Instance.CurrentConfig;

                // 检查下载限制
                if (!config.EnableDownload)
                {
                    LogService.Instance.AddLog("提示", "下载功能已禁用");
                    return;
                }

                switch (data.Type)
                {
                    case ClipboardType.Text:
                        if (!config.EnableDownloadText)
                        {
                            LogService.Instance.AddLog("提示", "文本下载已禁用");
                            return;
                        }
                        break;

                    case ClipboardType.File:
                    case ClipboardType.FileList:
                        if (IsImageFile(data.Filename))
                        {
                            if (!config.EnableDownloadImage)
                            {
                                LogService.Instance.AddLog("提示", "图片下载已禁用");
                                return;
                            }
                        }
                        else if (!config.EnableDownloadFile)
                        {
                            LogService.Instance.AddLog("提示", "文件下载已禁用");
                            return;
                        }
                        if (config.MaxDownloadFileSize > 0)
                        {
                            if (data.DataLength > config.MaxDownloadFileSize * 1024 * 1024)
                            {
                                LogService.Instance.AddLog("提示", $"文件大小超过限制: {data.DataLength / 1024 / 1024}MB/{config.MaxDownloadFileSize}MB");
                                return;
                            }
                        }
                       
                        await DownloadFile(data);
                        break;
                }

                Dispatcher.UIThread.Post(async () =>
                {
                    // 处理剪贴板内容
                   var success =  await _clipboardMonitor.SetClipboardContentAsync(data, true);
                    // 下载成功后发送通知
                    if (_configService.CurrentConfig.EnableDownloadNotification)
                    {
                        if (success)
                        {
                            await NotificationService.Instance.ShowNotificationAsync(
                              "接收成功",
                              $"已接收: {data.Description}"
                          );
                        }
                        else
                        {
                            await NotificationService.Instance.ShowNotificationAsync(
                              "接收错误",
                              $"未知错误请检查剪贴板"
                          );
                        }
                       

                    }
                });
                
                        
            }
            catch (Exception ex)
            {
                AddLog("错误", $"处理通知失败: {ex.Message}");
            }
        }

        private async Task DownloadFile(ClipboardData data)
        {
            string tempfilePath = string.Empty;
            try
            {
                // 下载ZIP文件
                var response = await _httpClient.GetAsync($"{_baseUrl}/file/{data.Uuid}");
                if (response.IsSuccessStatusCode)
                {

                    var fileData = await response.Content.ReadAsByteArrayAsync();

                    // 创建临时目录
                    var tempPath = Path.Combine(Path.GetTempPath(), "ClipFlow");
                    Directory.CreateDirectory(tempPath);

                    // 创建一个临时文件来存储
                    tempfilePath = Path.Combine(tempPath, data.Filename);
                    await File.WriteAllBytesAsync(tempfilePath, fileData);
                    data.FilenameList = new List<string>();
                    if (data.Type == ClipboardType.FileList)
                    {

                        var extractedFiles = await ClipboardUtils.ExtractZipArchive(tempfilePath);
                        data.FilenameList.AddRange(extractedFiles);
                    }
                    else
                    {
                        data.FilenameList.Add(tempfilePath);
                    }
                }
                else
                {
                    //错误
                    var res = await response.Content.ReadFromJsonAsync<ApiResponse<ClipboardData>>();
                    AddLog("错误", $"文件下载错误: {res?.Message}");
                }
            }
            catch (Exception ex)
            {
                AddLog("错误", $"文件下载或解压文件失败: {ex.Message}");
            }
            finally
            {
                if (data.Type == ClipboardType.FileList&&tempfilePath != string.Empty)
                {
                    // 清理临时ZIP文件
                    File.Delete(tempfilePath);
                }

            }
        }

        private async void ClipboardMonitor_OnClipboardChanged(ClipboardData data)
        {

            // 取消之前的上传操作
            if (_uploadCancellationTokenSource != null)
            {
                _uploadCancellationTokenSource.Cancel();
                _uploadCancellationTokenSource.Dispose();
            }
            _uploadCancellationTokenSource = new CancellationTokenSource();
            var tempZipPath = Path.Combine(Path.GetTempPath(), $"ClipFlow_{Guid.NewGuid()}.zip");

            try
            {
                if (!CheckSyncRestrictions(data))
                {
                    return;
                }

                const long maxServerSize = 500 * 1024 * 1024;
                if (data.DataLength > maxServerSize)
                {
                    AddLog("错误", $"文件大小超过服务器限制: {data.DataLength / 1024.0 / 1024.0:F2}MB/500MB");
                    return;
                }

                AddLog("上传", data.Description);

                const int maxRetries = 3;
                int currentRetry = 0;

                while (currentRetry < maxRetries && !_uploadCancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        var url = $"{_baseUrl}/{data.Type.ToString().ToLower()}";
                        if (data.Type != ClipboardType.Text)
                        {
                            url += $"?filename={Uri.EscapeDataString(data.Filename)}";
                        }

                        HttpContent content;
                        switch (data.Type)
                        {
                            case ClipboardType.Text:
                                content = new ByteArrayContent(data.Data);
                                break;

                            case ClipboardType.File:
                                var fileStream = new FileStream(data.FilenameList[0], FileMode.Open, FileAccess.Read);
                                content = new StreamContent(fileStream);
                                break;

                            case ClipboardType.FileList:
                                using (var archive = ZipFile.Open(tempZipPath, ZipArchiveMode.Create))
                                {
                                    await ClipboardUtils.CreateZipArchive(archive, data.FilenameList);
                                }
                                var zipStream = new FileStream(tempZipPath, FileMode.Open, FileAccess.Read);
                                content = new StreamContent(zipStream);
                                break;

                            default:
                                throw new NotSupportedException($"不支持的类型: {data.Type}");
                        }

                        using (content)
                        {
                            // 使用取消令牌发送请求
                            var response = await _httpClient.PostAsync(url, content, _uploadCancellationTokenSource.Token);
                            var resjson = await response.Content.ReadFromJsonAsync<ApiResponse<ClipboardData>>();
                            if (!resjson.IsSuccessStatusCode)
                            {
                                if (response.StatusCode == System.Net.HttpStatusCode.RequestEntityTooLarge)
                                {
                                    AddLog("错误", $"文件太大: {resjson.Message}");
                                    break;
                                }
                                throw new HttpRequestException($"{resjson.Code} - {resjson.Message}");
                            }
                            AddLog("上传", resjson.Message);

                            // 上传成功后发送通知
                            if (_configService.CurrentConfig.EnableUploadNotification)
                            {
                                await NotificationService.Instance.ShowNotificationAsync(
                                    "上传成功",
                                    $"已上传: {data.Description}"
                                );
                            }
                            break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        AddLog("提示", "有新同步当前任务已打断");
                        break;
                    }
                    catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
                    {
                        currentRetry++;
                        if (currentRetry >= maxRetries)
                        {
                            throw;
                        }

                        if (!_uploadCancellationTokenSource.Token.IsCancellationRequested)
                        {
                            var delay = Math.Pow(2, currentRetry) * 1000;
                            AddLog("警告", $"上传失败，{currentRetry}/{maxRetries} 次重试...");
                            await Task.Delay((int)delay, _uploadCancellationTokenSource.Token);
                        }
                    }
                    finally
                    {
                        if (File.Exists(tempZipPath))
                        {
                            try
                            {
                                File.Delete(tempZipPath);
                            }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    AddLog("提示", "上传已取消");
                }
                else
                {
                    AddLog("错误", $"上传失败: {ex.Message}");
                }
            }
        }

        #region 配置保存
        partial void OnIsEnabledChanged(bool value)
        {
            if (value)
            {
                    if (string.IsNullOrEmpty(_wsUrl))
                    {
                            ServerStatus = "服务器状态：服务器地址未设置";
                            LogService.Instance.AddLog("错误", "WebSocket URL未设置");
                        return;
                    }
                    _clipboardMonitor.Start();
                _ = Task.Run(async () => {
                    try 
                    {
                        await StartWebSocketConnection();
                    }
                    catch (Exception ex)
                    {
                        AddLog("错误", $"启动WebSocket连接失败: {ex.Message}");
                    }
                });
            }
            else
            {
                _clipboardMonitor.Stop();
                _ = Task.Run(async () => {
                    try 
                    {
                        await StopWebSocketConnection();
                    }
                    catch (Exception ex)
                    {
                        AddLog("错误", $"停止WebSocket连接失败: {ex.Message}");
                    }
                });
            }

            _configService.CurrentConfig.IsEnabled = value;
            _configService.SaveConfig();
        }

        partial void OnHostChanged(string value)
        {
            _configService.CurrentConfig.Host = value;
            _configService.SaveConfig();
            UpdateUrls(value);
        }

        partial void OnTokenChanged(string value)
        {
            _configService.CurrentConfig.Token = value;
            _configService.SaveConfig();
            UpdateHeaders();
        }

        partial void OnUserKeyChanged(string value)
        {
            _configService.CurrentConfig.UserKey = value;
            _configService.SaveConfig();
            UpdateHeaders();
        }

        #endregion

        private async Task StopWebSocketConnection()
        {
            if (_webSocketService != null)
            {
                await _webSocketService.StopAsync();
                ServerStatus = "服务器状态：已断开";
            }
        }

     
        public void Dispose()
        {
            _uploadCancellationTokenSource?.Cancel();
            _uploadCancellationTokenSource?.Dispose();
            _clipboardMonitor.Dispose();
            _webSocketService?.Dispose();
            _httpClient?.Dispose();
        }
    }
} 