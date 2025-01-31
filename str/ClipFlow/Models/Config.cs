namespace ClipFlow.Desktop.Models
{
    public class Config
    {
        public string Host { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string UserKey { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public int ThemeMode { get; set; }
        public bool MinimizeToTray { get; set; } = true;
        public bool HideOnStartup { get; set; }
        public bool AutoStart { get; set; }
        public bool EnableUpload { get; set; } = true;
        public bool EnableUploadText { get; set; } = true;
        public bool EnableUploadFile { get; set; } = true;
        public bool EnableUploadImage { get; set; } = true;
        public bool EnableUploadMultiple { get; set; } = true;
        public bool EnableDownload { get; set; } = true;
        public bool EnableDownloadText { get; set; } = true;
        public bool EnableDownloadFile { get; set; } = true;
        public bool EnableDownloadImage { get; set; } = true;
        public int MaxTextLength { get; set; } = 0;
        public int MaxUploadFileSize { get; set; } = 0;
        public uint MaxDownloadFileSize { get; set; } = 0;
        public bool EnableUploadNotification { get; set; } = true;
        public bool EnableDownloadNotification { get; set; } = true;
    }
} 