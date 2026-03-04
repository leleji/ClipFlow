using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace ClipFlow.Common.Models
{
    public partial class LogItem : ObservableObject
    {
        public DateTime Timestamp { get; init; }
        public string Type { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;

        [ObservableProperty]
        private bool isSelected;
    }
} 