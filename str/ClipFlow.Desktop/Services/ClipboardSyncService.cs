using System.Net.Http;
using System.Net.Http.Json;
using System.IO.Compression;
using System.Net.WebSockets;
using ClipFlow.Models;
using ClipFlow.Desktop.Utilities;
using System.Threading;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClipFlow.Desktop.Interfaces;

namespace ClipFlow.Desktop.Services
{
    public class ClipboardSyncService : IClipboardSyncService
    {
        private readonly ConfigService _configService;
        private readonly INotificationService _notificationService;
        private readonly HttpClient _httpClient;
        private readonly string _clientId = Guid.NewGuid().ToString();
        private CancellationTokenSource? _uploadCancellationTokenSource;
        private readonly IClipboardMonitor _clipboardMonitor;
        private string _baseUrl;
        private string _wsUrl;
        private WebSocketService? _webSocketService;

        public event Action<WebSocketState> OnWebSocketStateChanged;

        public ClipboardSyncService(ConfigService configService,INotificationService notificationService, IClipboardMonitor clipboardMonitor)
        {
            _configService = configService;
            _notificationService= notificationService;
            _clipboardMonitor = clipboardMonitor;
            var handler = new HttpClientHandler
            {
                MaxRequestContentBufferSize = 524288000 // 500MB
            };
            
            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMinutes(30)
            };

            clipboardMonitor.OnClipboardChanged += ClipboardMonitor_OnClipboardChanged;
        }

        public void Start()
        {
            UpdateBaseUrl(_configService.CurrentConfig.Host);
            UpdateHeaders(_configService.CurrentConfig.Token, _configService.CurrentConfig.UserKey);
            if (string.IsNullOrEmpty(_baseUrl))
            {
                LogService.Instance.AddLog("错误", "服务器地址未设置");
                return;
            }

            _clipboardMonitor.Start();
            StartWebSocket();
        }

        public void Stop()
        {
            _clipboardMonitor.Stop();
            StopWebSocket();
        }

        private void StartWebSocket()
        {
            if (string.IsNullOrEmpty(_wsUrl)) return;
            StopWebSocket();
            _webSocketService = new WebSocketService(
                _wsUrl,
                _clientId,
                _httpClient.DefaultRequestHeaders.GetValues("X-Auth-Token").FirstOrDefault() ?? string.Empty,
                _httpClient.DefaultRequestHeaders.GetValues("X-User-Key").FirstOrDefault() ?? string.Empty,
                HandleWebSocketNotificationAsync);

            _webSocketService.StateChanged += (sender, state) =>
            {
                OnWebSocketStateChanged?.Invoke(state);
            };

            _webSocketService.ErrorOccurred += (sender, ex) =>
            {
                LogService.Instance.AddLog("错误", $"WebSocket错误: {ex.Message}");
            };

            _ = _webSocketService.StartAsync();
        }

        private async void StopWebSocket()
        {
            if (_webSocketService != null)
            {
                await _webSocketService.StopAsync();
                _webSocketService = null;
            }
        }

        private async Task HandleWebSocketNotificationAsync(ClipboardData data)
        {
            try
            {
                var config = _configService.CurrentConfig;
                if (!CheckSyncDownloadRestrictions(data))
                {
                    return;
                };
                var tempPath = Path.Combine(Path.GetTempPath(), $"ClipFlow{Path.DirectorySeparatorChar}{data.Uuid}{Path.DirectorySeparatorChar}");
                Directory.CreateDirectory(tempPath);
                // 下载ZIP文件
                var response = await _httpClient.GetAsync($"{_baseUrl}/file/{data.Uuid}", HttpCompletionOption.ResponseHeadersRead);
                if (response.IsSuccessStatusCode)
                {
                    using var responseStream = await response.Content.ReadAsStreamAsync();
                    using (FileStream fileStream = new FileStream($"{tempPath}{data.Uuid}.dat", FileMode.Create))
                    {
                        await responseStream.CopyToAsync(fileStream);
                    };
                     var clipboardInfo = await CompressionEncryptor.DecryptTemporaryFileToClipboardDataAsync($"{tempPath}{data.Uuid}.dat", _configService.CurrentConfig.DataKey);
                    if (await _clipboardMonitor.SetClipboardContentAsync(clipboardInfo, true))
                    {
                        if (config.EnableDownloadNotification)
                        {
                            await _notificationService.ShowNotificationAsync(
                                "接收成功",
                                $"已接收: {clipboardInfo.Description}"
                            );
                        }
                        return;
                    };
                    throw new Exception("设置剪贴板错误。");
                }
                else
                {
                    throw new Exception("下载文件错误。");
                }
                
            }
            catch (Exception ex)
            {
                LogService.Instance.AddLog("错误", $"处理通知失败: {ex.Message}");
                await _notificationService.ShowNotificationAsync(
                "接收异常",
                $"{ex.Message}"
            );
            }
        }
        public void UpdateBaseUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return;

            try
            {
                var uri = new Uri(url.TrimEnd('/'));
                var baseUri = new UriBuilder(uri)
                {
                    Path = uri.AbsolutePath.TrimEnd('/'),
                }.Uri;

                if (!baseUri.AbsolutePath.Contains("api/Clipboard", StringComparison.OrdinalIgnoreCase))
                {
                    _baseUrl = $"{baseUri.ToString().TrimEnd('/')}/api/Clipboard";
                }
                else
                {
                    _baseUrl = baseUri.ToString().TrimEnd('/');
                }

                // 更新WebSocket URL
                var scheme = uri.Scheme == "https" ? "wss" : "ws";
                var wsUri = new UriBuilder(uri)
                {
                    Scheme = scheme,
                    Path = $"{baseUri.AbsolutePath.TrimEnd('/')}/api/Clipboard/ws"
                };
                _wsUrl = wsUri.Uri.ToString();
            }
            catch (Exception ex)
            {
                LogService.Instance.AddLog("错误", $"URL格式错误: {ex.Message}");
            }
        }

        public void UpdateHeaders(string token, string userKey)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-Auth-Token", token);
            _httpClient.DefaultRequestHeaders.Add("X-User-Key", userKey);
            _httpClient.DefaultRequestHeaders.Add("X-Client-Id", _clientId);

            // 如果WebSocket已连接，需要重新连接以使用新的凭证
            if (_webSocketService != null)
            {
                StopWebSocket();
                StartWebSocket();
            }
        }

        private bool CheckSyncDownloadRestrictions(ClipboardData data)
        {
            var config = _configService.CurrentConfig;
            if (!config.EnableDownload)
            {
                LogService.Instance.AddLog("提示", "下载功能已禁用");
                return false;
            }

            switch (data.Type)
            {
                case ClipboardType.Text:
                    if (!config.EnableDownloadText)
                    {
                        LogService.Instance.AddLog("提示", "文本下载已禁用");
                        return false;
                    }
                    break;

                case ClipboardType.File:
                case ClipboardType.FileList:
                    //if (IsImageFile(data.FileName))
                    //{
                    //    if (!config.EnableDownloadImage)
                    //    {
                    //        LogService.Instance.AddLog("提示", "图片下载已禁用");
                    //        return false;
                    //    }
                    //}
                    //else
                    if (!config.EnableDownloadFile)
                    {
                        LogService.Instance.AddLog("提示", "文件下载已禁用");
                        return false;
                    }
                    if (config.MaxDownloadFileSize > 0 && data.DataLength > config.MaxDownloadFileSize * 1024 * 1024)
                    {
                        LogService.Instance.AddLog("提示", $"文件大小超过限制: {data.DataLength / 1024 / 1024}MB/{config.MaxDownloadFileSize}MB");
                        return false;
                    }
                    break;
            }
            return true;
        }

        private bool CheckSyncUploadsRestrictions(ClipboardData data)
        {
            var config = _configService.CurrentConfig;

            if (!config.EnableUpload)
            {
                LogService.Instance.AddLog("提示", "上传功能已禁用");
                return false;
            }

            // 进程名筛选
            if (!string.IsNullOrEmpty(config.ProcessNames))
            {
                var processNames = config.ProcessNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var isProcessAllowed = processNames.Contains(data.ProcessName, StringComparer.OrdinalIgnoreCase);
                if (config.IsProcessNameWhitelist && isProcessAllowed)
                {
                    LogService.Instance.AddLog("提示", $"进程 {data.ProcessName} 在黑名单中");
                    return false;
                }
                else if (!config.IsProcessNameWhitelist && !isProcessAllowed)
                {
                    LogService.Instance.AddLog("提示", $"进程 {data.ProcessName} 不在白名单中");
                    return false;
                }
            }

            switch (data.Type)
            {
                case ClipboardType.Text:
                    if (!config.EnableUploadText)
                    {
                        LogService.Instance.AddLog("提示", "文本上传已禁用");
                        return false;
                    }
                    if (config.MaxTextLength > 0 && data.Text.Length > config.MaxTextLength)
                    {
                        LogService.Instance.AddLog("提示", $"文本长度超过限制: {data.Text.Length}/{config.MaxTextLength}");
                        return false;
                    }
                    break;

                case ClipboardType.File:
                    // 文件后缀筛选
                    if (!string.IsNullOrEmpty(config.FileExtensions))
                    {
                        var extensions = config.FileExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(ext => ext.StartsWith(".") ? ext.ToLower() : "." + ext.ToLower())
                            .ToList();
                        
                        var fileExtension = Path.GetExtension(data.FileName).ToLower();
                        var isExtensionAllowed = extensions.Contains(fileExtension);

                        if (config.IsFileExtensionWhitelist && isExtensionAllowed)
                        {
                            LogService.Instance.AddLog("提示", $"文件后缀 {fileExtension} 在黑名单中");
                            return false;
                        }
                        else if (!config.IsFileExtensionWhitelist && !isExtensionAllowed)
                        {
                            LogService.Instance.AddLog("提示", $"文件后缀 {fileExtension} 不在白名单中");
                            return false;
                        }
                    }

                    if (IsImageFile(data.FileName))
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

                    if (config.MaxUploadFileSize > 0 && data.DataLength> config.MaxUploadFileSize * 1024 * 1024)
                    {
                        LogService.Instance.AddLog("提示", $"文件大小超过限制: {data.DataLength / 1024 / 1024}MB/{config.MaxUploadFileSize}MB");
                        return false;
                    }
                    break;

                case ClipboardType.FileList:
                    if (!config.EnableUploadMultiple)
                    {
                        LogService.Instance.AddLog("提示", "多文件上传已禁用");
                        return false;
                    }
                    if (config.MaxUploadFileSize > 0 && data.DataLength > config.MaxUploadFileSize * 1024 * 1024)
                    {
                        LogService.Instance.AddLog("提示", $"压缩包大小超过限制: {data.DataLength / 1024 / 1024}MB/{config.MaxUploadFileSize}MB");
                        return false;
                    }
                    break;
            }

            const long maxServerSize = 500 * 1024 * 1024;
            if (data.DataLength > maxServerSize)
            {
                LogService.Instance.AddLog("错误", $"文件大小超过服务器限制: {data.DataLength / 1024.0 / 1024.0:F2}MB/500MB");
                return false;
            }
            return true;
        }

        private bool IsImageFile(string filename)
        {
            var extension = Path.GetExtension(filename).ToLower();
            return new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".ico", ".tiff" }.Contains(extension);
        }

        private async void ClipboardMonitor_OnClipboardChanged(ClipboardData data)
        {
            if (_uploadCancellationTokenSource != null)
            {
                try
                {
                    _uploadCancellationTokenSource.Cancel();
                    _uploadCancellationTokenSource.Dispose();
                }
                catch  {}
            }
            if (!CheckSyncUploadsRestrictions(data))
            {
                return;
            }
            _uploadCancellationTokenSource = new CancellationTokenSource();
            var tempPath = Path.Combine(Path.GetTempPath(), $"ClipFlow{Path.DirectorySeparatorChar}{data.FileName}");

            try
            {
                if (!Directory.Exists(Path.Combine(Path.GetTempPath(), $"ClipFlow")))
                {
                    Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"ClipFlow"));
                }
                LogService.Instance.AddLog("上传", data.Description);
                const int maxRetries = 3;
                int currentRetry = 0;

                while (currentRetry < maxRetries && !_uploadCancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        var url = $"{_baseUrl}/{data.Type.ToString().ToLower()}";
                        FileStream fileStream1=null ;
                        HttpContent content;
                        switch (data.Type)
                        {
                            case ClipboardType.Text:
                                content = new ByteArrayContent(CompressionEncryptor.EncryptTextToTemporaryFile(data, _configService.CurrentConfig.DataKey));
                                break;

                            case ClipboardType.File:
                                CompressionEncryptor.EncryptClipboardDataToTemporaryFile(data.FilenameList[0], data, _configService.CurrentConfig.DataKey);
                                content = new StreamContent(new FileStream($"{tempPath}.dat", FileMode.Open, FileAccess.Read));
                                break;

                            case ClipboardType.FileList:
                                using (var archive = ZipFile.Open($"{tempPath}", ZipArchiveMode.Create))
                                {
                                    await ClipboardUtils.CreateZipArchive(archive, data.FilenameList);
                                }
                                CompressionEncryptor.EncryptClipboardDataToTemporaryFile($"{tempPath}", data, _configService.CurrentConfig.DataKey);
                                content = new StreamContent(new FileStream($"{tempPath}.dat", FileMode.Open, FileAccess.Read));
                                break;

                            default:
                                throw new NotSupportedException($"不支持的类型: {data.Type}");
                        }
                        using (content)
                        {
                            var response = await _httpClient.PostAsync(url, content, _uploadCancellationTokenSource.Token);
                            var resjson = await response.Content.ReadFromJsonAsync<ApiResponse<ClipboardData>>();
                            if (!resjson.IsSuccessStatusCode)
                            {
                                if (response.StatusCode == System.Net.HttpStatusCode.RequestEntityTooLarge)
                                {
                                    LogService.Instance.AddLog("错误", $"文件太大: {resjson.Message}");
                                    break;
                                }
                                throw new HttpRequestException($"{resjson.Code} - {resjson.Message}");
                            }
                            LogService.Instance.AddLog("上传",$"{resjson.Message} {data.ProcessName}" );

                            if (_configService.CurrentConfig.EnableUploadNotification)
                            {
                                await _notificationService.ShowNotificationAsync(
                                    "上传成功",
                                    $"已上传: {data.Description}"
                                );
                            }
                            fileStream1?.Dispose();
                            break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        LogService.Instance.AddLog("提示", "有新同步当前任务已打断");
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
                            LogService.Instance.AddLog("警告", $"上传失败，{currentRetry}/{maxRetries} 次重试...");
                            await Task.Delay((int)delay, _uploadCancellationTokenSource.Token);
                        }
                    }
                    finally
                    {
                        try
                        {
                            if (File.Exists(tempPath))
                                File.Delete(tempPath);
                            if (File.Exists($"{tempPath}.dat"))
                                File.Delete($"{tempPath}.dat");
                            if (File.Exists($"{tempPath}.tmp"))
                                File.Delete($"{tempPath}.tmp");
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    LogService.Instance.AddLog("提示", "上传已取消");
                }
                else
                {
                    LogService.Instance.AddLog("错误", $"上传失败: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            _uploadCancellationTokenSource?.Cancel();
            _uploadCancellationTokenSource?.Dispose();
            _clipboardMonitor?.Dispose();
            _webSocketService?.Dispose();
            _httpClient?.Dispose();
        }

        public WebSocketState GetWebSocketState()
        {
            return _webSocketService?.GetState() ?? WebSocketState.None;
        }
    }
} 