namespace ClipFlow.Api.Models
{
    public class AppSettings
    {
        public string Token { get; set; }
        public int FileCacheMinutes { get; set; } = 60; // 默认缓存1小时

        public ulong MaxFileSize { get; set; } = 0; // 最大多少mb，默认不限制
    }
} 