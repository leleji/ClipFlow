using System;
using System.Runtime.InteropServices;

namespace ClipFlow.Desktop.MacOs.Utilities;

public static class Frontmost
{
    
    [DllImport("/System/Library/Frameworks/Carbon.framework/Carbon")]
    static extern IntPtr GetFrontProcess(out ProcessSerialNumber psn);

    [DllImport("/System/Library/Frameworks/Carbon.framework/Carbon")]
    static extern IntPtr CopyProcessName(ref ProcessSerialNumber psn, out IntPtr processName);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    static extern bool CFStringGetCString(IntPtr theString, byte[] buffer, long bufferSize, int encoding);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    static extern void CFRelease(IntPtr cf);

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
    public static string GetApplicationName()
    {
        try
        {
            if (GetFrontProcess(out var psn) == IntPtr.Zero)
            {
                if (CopyProcessName(ref psn, out var namePtr) == IntPtr.Zero && namePtr != IntPtr.Zero)
                {
                    byte[] buffer = new byte[1024];
                    if (CFStringGetCString(namePtr, buffer, buffer.Length, 0x08000100))
                    {
                        CFRelease(namePtr);
                        string? name = System.Text.Encoding.UTF8.GetString(buffer).TrimEnd('\0');
                        return name;
                    }
                    CFRelease(namePtr);
                }
            }
            return string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取前台应用时出错: {ex.Message}");
            return string.Empty;
        }
    }

}