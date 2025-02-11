using ClipFlow.Models;
using System.Text;
using System.Security.Cryptography;
using System;

namespace ClipFlow.Desktop.ClipboardHandler
{
    public class ClipboardTextHandler
    {
        public ClipboardData? ProcessText(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;

            return new ClipboardData
            {
                Type = ClipboardType.Text,
                Text =text,
                Description = "文本: " + (text.Length > 30 ? text[..30] + "..." : text)
            };
        }

    }
} 