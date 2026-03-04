using Avalonia.Input;
using Avalonia.Input.Platform;
using ClipFlow.Common.Utilities;
using ClipFlow.Core.Constants;
using ClipFlow.Core.Interfaces;
using ClipFlow.Core.Models;
using ClipFlow.Core.Services;
using ClipFlow.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ClipFlow.Infrastructure.Platforms.Windows
{
    public class WindowsClipboardHandler1 : IClipboardHandler
    {


        private string? _lastHash;
        public void Initialize() { /* Windows 原生不需要特殊初始化 */ }
        public void Cleanup() { /* 释放资源 */ }

        public async Task<ClipboardData?> GetContentAsync()
        {
            var formats=await ClipboardManager.Clipboard.GetDataFormatsAsync();

            //foreach (var format in formats)
            //{

            //    if (format is DataFormat<string> stringFormat)
            //    {
            //        var text = await ClipboardManager.Clipboard.TryGetValueAsync(stringFormat);
            //        Console.WriteLine($"格式: {format.Identifier}, 内容: {text}");
            //    }
            //    else
            //    {
            //        var stringFormaat = DataFormat.CreateBytesPlatformFormat(format.Identifier);
            //        // 对于未知格式，通常可以使用 object 尝试读取

            //        var data = await ClipboardManager.Clipboard.TryGetValueAsync(stringFormaat);
            //        string str = System.Text.Encoding.UTF8.GetString(data);
            //    }
            //}

            if (formats.Contains(DataFormat.File))
            {
                var list = await ClipboardManager.Clipboard.TryGetFilesAsync();
                var filesHash = Common.Utilities.ClipboardUtils.GetMd5Hash(string.Join("|", list.Select(v=>v.Path.LocalPath)));
                if (filesHash == _lastHash) return null;
                _lastHash = filesHash;

            }
            else if (formats.Contains(DataFormat.Bitmap)) 
            {
                var bytes = await ClipboardManager.Clipboard.TryGetBitmapAsync();

                bytes.Save("clipboard_image.png");
                var stringFormaat = DataFormat.CreateBytesPlatformFormat("CF_DIBV5");
            


                var data = await ClipboardManager.Clipboard.TryGetValueAsync(stringFormaat);
            }
            else if (formats.Contains(DataFormat.Text))
            {
                var text = await ClipboardManager.Clipboard.TryGetTextAsync();
                if (string.IsNullOrEmpty(text)) return null;
                var textHash = Common.Utilities.ClipboardUtils.GetMd5Hash(text);
                if (textHash == _lastHash) return null;
                _lastHash = textHash;
                return ClipboardProcess.ProcessText(text);
            }


 

            return null;
        }

        public async Task<bool> SetContentAsync(ClipboardData data, bool isServerUpdate = true)
        {
            return false;
        }

    }
}