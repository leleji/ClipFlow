using Microsoft.AspNetCore.Mvc;
using ClipFlow.Models;
using ClipFlow.Api.Models;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ClipFlow.Api.Services;
using ClipFlow.Api.Filters;
using System.IO;
using System.IO.Compression;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace ClipFlow.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [TokenAuthorization]
    public class ClipboardController : ControllerBase
    {
        private readonly ClipboardWebSocketManager _webSocketManager;
        private readonly ClipboardDataManager _clipboardManager;
        private readonly ILogger<ClipboardController> _logger;
        private readonly string _fileStoragePath;
        private readonly AppSettings _appSettings;
        private static readonly ConcurrentDictionary<string, WebSocket> _sockets = new();
        private static readonly ConcurrentDictionary<string, DateTime> _lastPingTime = new();

        public ClipboardController(
            ClipboardWebSocketManager webSocketManager,
            ClipboardDataManager clipboardManager,
            ILogger<ClipboardController> logger,
            IOptions<AppSettings> appSettings)
        {
            _webSocketManager = webSocketManager;
            _clipboardManager = clipboardManager;
            _logger = logger;
            _appSettings = appSettings.Value;
            
            _fileStoragePath = Path.Combine(AppContext.BaseDirectory, "files");
            if (!Directory.Exists(_fileStoragePath))
            {
                Directory.CreateDirectory(_fileStoragePath);
            }
        }

        [HttpPost("{type}")]
        [RequestSizeLimit(524288000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 524288000)]
        public async Task<ActionResult<ApiResponse<object>>> Upload(string type, [FromQuery] string? filename)
        {
            try
            {
                var userKey = Request.Headers["X-User-Key"].ToString();
                var contentLength = (ulong)(Request.ContentLength ?? 0);

                // 检查文件大小限制（仅当 MaxFileSize > 0 时）
                if (_appSettings.MaxFileSize > 0)
                {
                    var maxSizeInBytes = _appSettings.MaxFileSize * 1024 * 1024; // 转换为字节
                    if (contentLength > maxSizeInBytes)
                    {
                        return BadRequest(ApiResponse<object>.Error(413, $"文件大小超过限制。最大允许: {_appSettings.MaxFileSize}MB，当前文件: {contentLength / 1024.0 / 1024.0:F2}MB"));
                    }
                }

                var record = new ClipboardData
                {
                    Uuid = Guid.NewGuid().ToString(),
                    Type = Enum.Parse<ClipboardType>(type, true)
                };

                if (record.Type == ClipboardType.Text)
                {
                    using var memoryStream = new MemoryStream();
                    await Request.Body.CopyToAsync(memoryStream);
                    record.Data = memoryStream.ToArray();
                }
                else
                {
                    record.DataLength = contentLength;
                    record.Filename = SanitizeFileName(filename);
                    
                    // 保存文件时使用 UUID 作为前缀
                    var physicalFileName = $"{record.Uuid}_{record.Filename}";
                    var filePath = Path.Combine(_fileStoragePath, physicalFileName);

                    const int bufferSize = 81920; // 80KB 缓冲区
                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.Asynchronous))
                    {
                        var buffer = new byte[bufferSize];
                        int bytesRead;
                        long totalBytesRead = 0;
                        var body = Request.Body;

                        while ((bytesRead = await body.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;

                            // 可选：报告进度
                            //if (contentLength > 0)
                            //{
                            //    var progress = (double)totalBytesRead / contentLength * 100;
                            //    _logger.LogDebug($"Upload progress: {progress:F2}% ({totalBytesRead}/{contentLength} bytes)");
                            //}
                        }

                        await fileStream.FlushAsync();
                    }
                }

                // 添加到历史记录
                _clipboardManager.AddRecord(userKey, record);
                if (_clipboardManager.GetHistory(userKey).Count > 20)
                {
                    var oldRecord = _clipboardManager.GetHistory(userKey).Dequeue();
                    if (oldRecord.Filename != null)
                    {
                        var oldFile = Path.Combine(_fileStoragePath, oldRecord.Filename);
                        if (System.IO.File.Exists(oldFile))
                        {
                            System.IO.File.Delete(oldFile);
                        }
                    }
                }

                // 获取当前连接的客户端ID并记录日志
                var currentClientId = Request.Headers["X-Client-Id"].ToString();
                _logger.LogInformation($"Upload request from client: {currentClientId}, UserKey: {userKey}");

                // 通知其他客户端
                var json = JsonSerializer.Serialize(record);
                var jsonbuffer = Encoding.UTF8.GetBytes(json);
                
                _logger.LogInformation($"Broadcasting to other clients. Current client: {currentClientId}, UserKey: {userKey}");
                await _webSocketManager.BroadcastToUserAsync(userKey, currentClientId, jsonbuffer);

                return Ok(ApiResponse<object>.Success(new { uuid = record.Uuid }, "数据已同步"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "上传剪贴板数据失败");
                if (ex is BadHttpRequestException badRequestEx)
                {
                    return BadRequest(ApiResponse<object>.Error(400, $"请求体太大: {badRequestEx.Message}"));
                }
                else if (ex is IOException ioEx)
                {
                    _logger.LogError(ioEx, "文件IO错误");
                    return StatusCode(500, ApiResponse<object>.Error(500, "文件保存失败"));
                }
                return StatusCode(500, ApiResponse<object>.Error(500, "服务器内部错误"));
            }
        }

        [HttpGet("file/{uuid}")]
        public ActionResult<ApiResponse<object>> GetFile(string uuid)
        {
            var userKey = Request.Headers["X-User-Key"].ToString();
            var record = _clipboardManager.GetByUuid(userKey, uuid);
            if (record == null || record.Filename == null)
            {
                return NotFound(ApiResponse<object>.Error(404, "文件未找到"));
            }

            var physicalFileName = $"{uuid}_{record.Filename}";
            var filePath = Path.Combine(_fileStoragePath, physicalFileName);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(ApiResponse<object>.Error(404, "文件未找到"));
            }
            return PhysicalFile(filePath, "application/octet-stream", record.Filename);
        }

        [HttpGet]
        public ActionResult<ApiResponse<ClipboardData>> GetLatest([FromQuery] bool onlyText = false)
        {
            var userKey = Request.Headers["X-User-Key"].ToString();
            var latest = onlyText 
                ? _clipboardManager.GetLatestText(userKey)
                : _clipboardManager.GetLatest(userKey);
            
            if (latest == null)
            {
                return NotFound(ApiResponse<ClipboardData>.Error(404, "暂无数据"));
            }
            if (onlyText) {
                latest.Text = Encoding.UTF8.GetString(latest.Data);
            }

            return ApiResponse<ClipboardData>.Success(latest);
        }

        [HttpGet("ws")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status101SwitchingProtocols)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task Socket()
        {
            var connectionId = Request.Headers["X-Client-Id"].ToString();
            var userKey = Request.Headers["X-User-Key"].ToString();
            if (HttpContext.WebSockets.IsWebSocketRequest && !string.IsNullOrEmpty(connectionId) && !string.IsNullOrEmpty(userKey))
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                var token = Request.Headers["X-Auth-Token"].ToString();
                try
                {
                    _webSocketManager.AddSocket(connectionId, webSocket, token, userKey);

                    var buffer = new byte[1024 * 4];
                    while (webSocket.State == WebSocketState.Open)
                    {
                        var result = await webSocket.ReceiveAsync(
                            new ArraySegment<byte>(buffer), CancellationToken.None);

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            
                            if (message == "ping")
                            {
                                _webSocketManager.UpdatePing(connectionId);
                                await webSocket.SendAsync(
                                    Encoding.UTF8.GetBytes("pong"),
                                    WebSocketMessageType.Text,
                                    true,
                                    CancellationToken.None);
                            }
                            else
                            {
                                _logger.LogWarning("收到意外的WebSocket消息");
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
                    _logger.LogError(ex, "WebSocket连接异常");
                }
                finally
                {
                    _webSocketManager.RemoveSocket(connectionId);
                }
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return fileName;

            // 替换 Windows 和 Linux 中的非法字符
            var invalidChars = Path.GetInvalidFileNameChars()
                .Concat(new[] { '\\', '/' })  // 添加额外的路径分隔符
                .ToArray();
            
            // 替换非法字符为下划线
            var sanitizedName = invalidChars.Aggregate(fileName, (current, invalid) => 
                current.Replace(invalid, '_'));

            // 处理以点或空格开头的文件名
            sanitizedName = sanitizedName.TrimStart('.', ' ');
            
            // 如果文件名为空（比如全是非法字符），生成一个默认名称
            if (string.IsNullOrWhiteSpace(sanitizedName))
            {
                sanitizedName = $"file_{DateTime.UtcNow:yyyyMMddHHmmss}";
            }

            // 确保文件名不超过最大长度（考虑到不同文件系统的限制）
            const int maxFileNameLength = 200; // 设置一个安全的最大长度
            if (sanitizedName.Length > maxFileNameLength)
            {
                var extension = Path.GetExtension(sanitizedName);
                sanitizedName = sanitizedName.Substring(0, maxFileNameLength - extension.Length) + extension;
            }

            return sanitizedName;
        }
    }
} 