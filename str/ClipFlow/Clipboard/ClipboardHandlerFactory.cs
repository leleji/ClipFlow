using System;
using System.Runtime.InteropServices;

namespace ClipFlow.Clipboard
{
    public static class ClipboardHandlerFactory
    {
        public static IClipboardHandler Create()
        {
            if (OperatingSystem.IsWindows())
            {
                return new WindowsClipboardHandler();
            }
            else if (OperatingSystem.IsMacOS())
            {
                return new MacOSClipboardHandler();
            }
            else if (OperatingSystem.IsLinux())
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