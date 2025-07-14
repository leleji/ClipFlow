using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ClipFlow.Desktop.MacOs.Utilities;

public static class Frontmost
{
    [DllImport("/System/Library/Frameworks/Carbon.framework/Carbon")]
    private static extern IntPtr GetFrontProcess(out ProcessSerialNumber psn);

    [DllImport("/System/Library/Frameworks/Carbon.framework/Carbon")]
    private static extern IntPtr CopyProcessName(ref ProcessSerialNumber psn, out IntPtr processName);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern bool CFStringGetCString(IntPtr theString, byte[] buffer, long bufferSize, int encoding);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr cf);

    public static string GetApplicationName()
    {
        try
        {
            if (GetFrontProcess(out var psn) == IntPtr.Zero)
                if (CopyProcessName(ref psn, out var namePtr) == IntPtr.Zero && namePtr != IntPtr.Zero)
                {
                    var buffer = new byte[1024];
                    if (CFStringGetCString(namePtr, buffer, buffer.Length, 0x08000100))
                    {
                        CFRelease(namePtr);
                        var name = Encoding.UTF8.GetString(buffer).TrimEnd('\0');
                        return name;
                    }

                    CFRelease(namePtr);
                }

            return string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取前台应用时出错: {ex.Message}");
            return string.Empty;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessSerialNumber
    {
        public ulong highLongOfPSN;
        public ulong lowLongOfPSN;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInfoRec
    {
        public uint processInfoLength;
        public uint processSignature;
        public uint processBackgroundOnly;
        public uint processAppSpec;
    }
}