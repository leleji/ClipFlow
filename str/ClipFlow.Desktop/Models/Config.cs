namespace ClipFlow.Desktop.Models
{
    public class Config
    {
        /// <summary>
        /// 服务器地址
        /// </summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>
        /// 用户认证令牌
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// 用户唯一标识
        /// </summary>
        public string UserKey { get; set; } = string.Empty;

        /// <summary>
        /// 数据加密密钥
        /// </summary>
        public string DataKey { get; set; } = string.Empty;

        /// <summary>
        /// 是否启用同步功能
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 主题模式：0-跟随系统，1-浅色，2-深色
        /// </summary>
        public int ThemeMode { get; set; }

        /// <summary>
        /// 是否最小化到托盘
        /// </summary>
        public bool MinimizeToTray { get; set; } = true;

        /// <summary>
        /// 是否开机自启动
        /// </summary>
        public bool AutoStart { get; set; }

        /// <summary>
        /// 是否启用上传功能
        /// </summary>
        public bool EnableUpload { get; set; } = true;

        /// <summary>
        /// 是否启用文本上传
        /// </summary>
        public bool EnableUploadText { get; set; } = true;

        /// <summary>
        /// 是否启用图片上传
        /// </summary>
        public bool EnableUploadImage { get; set; } = true;

        /// <summary>
        /// 是否启用文件上传
        /// </summary>
        public bool EnableUploadFile { get; set; } = true;

        /// <summary>
        /// 是否启用多文件上传
        /// </summary>
        public bool EnableUploadMultiple { get; set; } = false;

        /// <summary>
        /// 是否启用下载功能
        /// </summary>
        public bool EnableDownload { get; set; } = true;

        /// <summary>
        /// 是否启用文本下载
        /// </summary>
        public bool EnableDownloadText { get; set; } = true;

        /// <summary>
        /// 是否启用文件下载
        /// </summary>
        public bool EnableDownloadFile { get; set; } = true;


        /// <summary>
        /// 文本最大长度限制（0表示不限制）
        /// </summary>
        public int MaxTextLength { get; set; } = 0;

        /// <summary>
        /// 上传文件大小限制，单位：Mb（0表示不限制）
        /// </summary>
        public ulong MaxUploadFileSize { get; set; } = 20;

        /// <summary>
        /// 下载文件大小限制，单位：Mb（0表示不限制）
        /// </summary>
        public uint MaxDownloadFileSize { get; set; } = 0;

        /// <summary>
        /// 是否启用上传通知
        /// </summary>
        public bool EnableUploadNotification { get; set; } = true;

        /// <summary>
        /// 是否启用下载通知
        /// </summary>
        public bool EnableDownloadNotification { get; set; } = true;

        /// <summary>
        /// 文件后缀过滤模式：true-黑名单，false-白名单
        /// </summary>
        public bool IsFileExtensionWhitelist { get; set; } = true;

        /// <summary>
        /// 文件后缀列表，以逗号分隔
        /// </summary>
        public string FileExtensions { get; set; } = string.Empty;

        /// <summary>
        /// 进程名过滤模式：true-黑名单，false-白名单
        /// </summary>
        public bool IsProcessNameWhitelist { get; set; } = true;

        /// <summary>
        /// 进程名列表，以逗号分隔
        /// </summary>
        public string ProcessNames { get; set; } = string.Empty;
    }
} 