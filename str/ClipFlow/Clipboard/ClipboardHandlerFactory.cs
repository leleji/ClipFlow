using System;
using System.Runtime.InteropServices;

namespace ClipFlow.Clipboard
{
    public static class ClipboardHandlerFactory
    {
        public static IClipboardHandler Create()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new WindowsClipboardHandler();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new MacOSClipboardHandler();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new LinuxClipboardHandler();
            }
            else
            {
                throw new PlatformNotSupportedException("当前操作系统不支持剪贴板操作");
            }
        }
    }
} 