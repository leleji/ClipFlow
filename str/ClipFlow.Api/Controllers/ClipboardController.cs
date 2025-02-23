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
    public class ClipboardController : ControllerBase
    {
        private readonly ClipboardWebSocketManager _webSocketManager;
        private readonly ClipboardDataManager _clipboardManager;
        private readonly ILogger<ClipboardController> _logger;
        private readonly string _fileStoragePath;
        private readonly AppSettings _appSettings;
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
        public async Task<ActionResult<ApiResponse<object>>> Upload(string type)
        {
            try
            {
                var token = Request.Headers["X-Auth-Token"].ToString();
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
                record.DataLength = contentLength;

                // 保存文件时使用 UUID 作为前缀
                var physicalFileName = $"{record.Uuid}.dat";
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
                    }

                    await fileStream.FlushAsync();
                }
                // 添加到历史记录
                _clipboardManager.AddRecord(token, record);
                if (_clipboardManager.GetHistory(token).Count > 20)
                {
                    var oldRecord = _clipboardManager.GetHistory(token).Dequeue();
                    if (oldRecord.FileName != null)
                    {
                        var oldFile = Path.Combine(_fileStoragePath, oldRecord.FileName);
                        if (System.IO.File.Exists(oldFile))
                        {
                            System.IO.File.Delete(oldFile);
                        }
                    }
                }

                // 获取当前连接的客户端ID并记录日志
                var currentClientId = Request.Headers["X-Client-Id"].ToString();
                _logger.LogInformation($"Upload request from client: {currentClientId}, Token: {token}");

                // 通知其他客户端
                var json = JsonSerializer.Serialize(record);
                var jsonbuffer = Encoding.UTF8.GetBytes(json);
                
                _logger.LogInformation($"Broadcasting to other clients. Current client: {currentClientId}, Token: {token}");
                await _webSocketManager.BroadcastToUserAsync(token, currentClientId, jsonbuffer);

                return Ok(ApiResponse<object>.Success(new { uuid = record.Uuid }, "数据已同步"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<object>.Error(401, ex.Message));
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
            try 
            {
                var token = Request.Headers["X-Auth-Token"].ToString();
                var record = _clipboardManager.GetByUuid(token, uuid);
                if (record == null)
                {
                    return NotFound(ApiResponse<object>.Error(404, "数据未找到"));
                }

                var physicalFileName = $"{uuid}.dat";
                var filePath = Path.Combine(_fileStoragePath, physicalFileName);
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(ApiResponse<object>.Error(404, "文件未找到"));
                }
                return PhysicalFile(filePath, "application/octet-stream", record.FileName);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<object>.Error(401, ex.Message));
            }
        }

        [HttpGet]
        public ActionResult<ApiResponse<ClipboardData>> GetLatest([FromQuery] bool onlyText = false)
        {
            try
            {
                var token = Request.Headers["X-Auth-Token"].ToString();
                var latest = onlyText 
                    ? _clipboardManager.GetLatestText(token)
                    : _clipboardManager.GetLatest(token);
                
                if (latest == null)
                {
                    return NotFound(ApiResponse<object>.Error(404, "暂无数据"));
                }
                return ApiResponse<ClipboardData>.Success(latest);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<object>.Error(401, ex.Message));
            }
        }

        [HttpGet("ws")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status101SwitchingProtocols)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task Socket()
        {
            var connectionId = Request.Headers["X-Client-Id"].ToString();
            var token = Request.Headers["X-Auth-Token"].ToString();
            if (HttpContext.WebSockets.IsWebSocketRequest && !string.IsNullOrEmpty(connectionId) && !string.IsNullOrEmpty(token))
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                try
                {
                    if (!_appSettings.Tokens.Contains(token))
                    {
                        throw new UnauthorizedAccessException("无效的访问令牌");
                    }

                    _webSocketManager.AddSocket(connectionId, webSocket, token);

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
    }
} 