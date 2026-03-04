using ClipFlow.Common.Models;
using System;
using System.Collections.ObjectModel;


namespace ClipFlow.Common.Helpers
{
    public class LogService
    {
        private const int MaxLogItems = 100;  // 设置最大日志条数 
        private static LogService? _instance;
        public static LogService Instance => _instance ??= new LogService();

        public ObservableCollection<LogItem> LogItems { get; } = new();

        public event EventHandler? LogAdded;

        public void AddLog(string type, string message)
        {
            var log = new LogItem
            {
                Type = type,
                Message = message,
                Timestamp = DateTime.Now
            };

            // 从末尾添加日志
            LogItems.Add(log);

            // 如果超过最大条数，移除最早的日志
            while (LogItems.Count > MaxLogItems)
            {
                LogItems.RemoveAt(0);
            }

            // 触发日志添加事件，用于滚动到底部
            LogAdded?.Invoke(this, EventArgs.Empty);
        }

        public void Clear()
        {
            LogItems.Clear();

        }
    }


} 