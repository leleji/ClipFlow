using System;

namespace ClipFlow.Desktop.Models
{
    public class LogItem
    {
        public DateTime Timestamp { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }

        public LogItem(string type, string message)
        {
            Timestamp = DateTime.Now;
            Type = type;
            Message = message;
        }
    }
} 