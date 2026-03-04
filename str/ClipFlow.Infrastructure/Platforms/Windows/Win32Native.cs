using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ClipFlow.Infrastructure.Platforms.Windows
{
    internal static partial class Win32Native
    {

        public const int WM_CLIPBOARDUPDATE = 0x031D;
        public static readonly IntPtr HWND_MESSAGE = new IntPtr(-3);



        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public int pt_x;
            public int pt_y;
        }


        [DllImport("user32.dll")]
        public static extern void PostQuitMessage(int nExitCode);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        [LibraryImport("user32.dll", EntryPoint = "CreateWindowExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        public static partial IntPtr CreateWindowEx(
            int dwExStyle, string lpClassName, string lpWindowName,
            int dwStyle, int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [LibraryImport("user32.dll", EntryPoint = "GetMessageW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        // 同样建议为这几个也指定 Unicode 版本，防止潜在的字符集问题
        [LibraryImport("user32.dll", EntryPoint = "TranslateMessage")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool TranslateMessage(ref MSG lpMsg);

        [LibraryImport("user32.dll", EntryPoint = "DispatchMessageW")]
        public static partial IntPtr DispatchMessage(ref MSG lpMsg);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool PostMessage(
            IntPtr hWnd,
            int Msg,
            IntPtr wParam,
            IntPtr lParam);


        // --- User32.dll 剪贴板相关 ---
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsClipboardFormatAvailable(uint uFormat);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        // --- Kernel32.dll 内存管理相关 ---
        public const uint GMEM_MOVEABLE = 0x0002;
        public const uint GMEM_ZEROINIT = 0x0040;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GlobalFree(IntPtr hMem);

        // 获取当前剪贴板中现有的格式数量
        [DllImport("user32.dll")]
        public static extern int CountClipboardFormats();

        // 枚举剪贴板格式：传入上一个格式 ID，返回下一个格式 ID
        // 第一次调用传 0
        [DllImport("user32.dll")]
        public static extern uint EnumClipboardFormats(uint format);


        // 获取格式的名称（针对自定义格式，如 "HTML Format", "FileGroupDescriptorW"）
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetClipboardFormatName(uint format, [Out] StringBuilder lpszFormatName, int cchMaxCount);

        // --- Shell32.dll 文件拖拽相关 ---
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern uint DragQueryFile(IntPtr hDrop, uint iFile, StringBuilder? lpszFile, uint cch);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern UIntPtr GlobalSize(IntPtr hMem);

        // --- 常量定义 ---
        public const uint CF_UNICODETEXT = 13;
        public const uint CF_HDROP = 15;
        public const uint CF_DIB = 8;
        public const uint CF_BITMAP = 2;

        [StructLayout(LayoutKind.Sequential)]
        public struct DROPFILES
        {
            public uint pFiles;
            public int pt_x;
            public int pt_y;
            public bool fNC;
            public bool fWide;
        }

    }
}
