using System;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ClipFlow.Core.Interfaces;
using ClipFlow.Core.Models;

namespace ClipFlow.Infrastructure.Platforms.Windows
{
    public class WindowsClipboardHandler : IClipboardHandler
    {
        public void Initialize() { /* Windows 原生不需要特殊初始化 */ }
        public void Cleanup() { /* 释放资源 */ }

        public async Task<ClipboardData?> GetContentAsync()
        {
            return await Task.Run(() =>
            {
                if (!Win32Native.OpenClipboard(IntPtr.Zero)) return null;
                
                try
                {
                    //获取原始数据
                    var formats = new List<(uint Id, string Name)>();
                    uint format = 0;
                    while ((format = Win32Native.EnumClipboardFormats(format)) != 0)
                    {
                        string name = GetFormatFriendlyName(format);
                        formats.Add((format, name));
                    }


                    // 1. 优先处理文件格式
                    if (Win32Native.IsClipboardFormatAvailable(Win32Native.CF_HDROP))
                    {
                        IntPtr hGlobal = Win32Native.GetClipboardData(Win32Native.CF_HDROP);
                        if (hGlobal != IntPtr.Zero)
                        {
                            var files = GetFilesFromHandle(hGlobal);
                            if (files.Count > 0)
                            {
                                return new ClipboardData
                                {
                                    Type = ClipboardType.File,
                                    CopyFiles = files,
                                    Description = $"文件: {files[0]} 等 {files.Count} 个项目"
                                };
                            }
                        }
                    }
                    // 2. 处理图片 (CF_DIB)
                    if (Win32Native.IsClipboardFormatAvailable(Win32Native.CF_DIB))
                    {
                        IntPtr hGlobal = Win32Native.GetClipboardData(Win32Native.CF_DIB);
                        if (hGlobal != IntPtr.Zero)
                        {
                            byte[]? imageBytes = GetImageBytesFromHandle(hGlobal);
                            if (imageBytes != null)
                            {
                                return new ClipboardData
                                {
                                    Type = ClipboardType.File,
                                    Text = Convert.ToBase64String(imageBytes),
                                    Description = "图片数据",
                                    DataLength = (ulong)imageBytes.Length
                                };
                            }
                        }
                    }
                    // 3. 处理文本格式
                    if (Win32Native.IsClipboardFormatAvailable(Win32Native.CF_UNICODETEXT))
                    {
                        IntPtr hGlobal = Win32Native.GetClipboardData(Win32Native.CF_UNICODETEXT);
                        if (hGlobal != IntPtr.Zero)
                        {
                            string text = GetTextFromHandle(hGlobal);
                            return new ClipboardData
                            {
                                Type = ClipboardType.Text,
                                Text = text,
                                DataLength = (ulong)text.Length
                            };
                        }
                    }

                    return null;
                }
                finally
                {
                    Win32Native.CloseClipboard();
                }
            });
        }

        public async Task<bool> SetContentAsync(ClipboardData data, bool isServerUpdate = true)
        {
            return await Task.Run(() =>
            {
                if (!Win32Native.OpenClipboard(IntPtr.Zero)) return false;
                Win32Native.EmptyClipboard();

                try
                {
                    if (data.Type == ClipboardType.Text && !string.IsNullOrEmpty(data.Text))
                    {
                        return SetTextToClipboard(data.Text);
                    }
                    else if (data.Type == ClipboardType.File && data.CopyFiles?.Count > 0)
                    {
                        return SetFilesToClipboard(data.CopyFiles);
                    }
                    return false;
                }
                finally
                {
                    Win32Native.CloseClipboard();
                }
            });
        }

        #region 私有辅助方法
        private static readonly Dictionary<uint, string> StandardFormats = new()
        {
            { 1, "CF_TEXT" },
            { 2, "CF_BITMAP" },
            { 3, "CF_METAFILEPICT" },
            { 8, "CF_DIB" },
            { 13, "CF_UNICODETEXT" },
            { 15, "CF_HDROP" },
            { 17, "CF_DIBV5" }
        };
        private byte[]? GetImageBytesFromHandle(IntPtr hGlobal)
        {
            IntPtr pointer = Win32Native.GlobalLock(hGlobal);
            if (pointer == IntPtr.Zero) return null;

            try
            {
                // 1. 获取 DIB 数据的大小
                // 注意：在 Win32 中，hGlobal 的大小可以通过 GlobalSize 获取
                UIntPtr sizeInBytes = Win32Native.GlobalSize(hGlobal);
                int dibSize = (int)sizeInBytes.ToUInt32();

                // 2. 准备位图文件头 (BITMAPFILEHEADER) - 14 字节
                int fileHeaderSize = 14;
                int totalSize = fileHeaderSize + dibSize;
                byte[] bitmapData = new byte[totalSize];

                // 3. 填充位图文件头
                // 'BM' 标志
                bitmapData[0] = 0x42;
                bitmapData[1] = 0x4D;
                // 文件总大小
                BitConverter.GetBytes(totalSize).CopyTo(bitmapData, 2);
                // 保留字 (0)
                BitConverter.GetBytes((short)0).CopyTo(bitmapData, 6);
                BitConverter.GetBytes((short)0).CopyTo(bitmapData, 8);

                // 计算像素数据的偏移量 (FileHeader + DIB Header 中的前 4 字节是 DIBHeader 大小)
                int dibHeaderSize = Marshal.ReadInt32(pointer);
                int offset = fileHeaderSize + dibHeaderSize;
                BitConverter.GetBytes(offset).CopyTo(bitmapData, 10);

                // 4. 拷贝 DIB 数据到文件头之后
                Marshal.Copy(pointer, bitmapData, fileHeaderSize, dibSize);

                return bitmapData;
            }
            finally
            {
                Win32Native.GlobalUnlock(hGlobal);
            }
        }
        public string GetFormatFriendlyName(uint id)
        {
            // 1. 查找标准格式
            if (StandardFormats.TryGetValue(id, out var name))
                return name;

            // 2. 查找注册格式名称
            StringBuilder sb = new StringBuilder(256);
            if (Win32Native.GetClipboardFormatName(id, sb, sb.Capacity) > 0)
            {
                return sb.ToString();
            }

            return $"Private/Unknown ({id})";
        }
        private string GetTextFromHandle(IntPtr hGlobal)
        {
            IntPtr lpstr = Win32Native.GlobalLock(hGlobal);
            try
            {
                return Marshal.PtrToStringUni(lpstr) ?? string.Empty;
            }
            finally
            {
                Win32Native.GlobalUnlock(hGlobal);
            }
        }

        private StringCollection GetFilesFromHandle(IntPtr hGlobal)
        {
            var files = new StringCollection();
            // 使用 Shell32 或手动解析 DROPFILES 结构
            // 这里为了 AOT 安全，手动调用 DragQueryFile
            uint count = Win32Native.DragQueryFile(hGlobal, 0xFFFFFFFF, null, 0);
            for (uint i = 0; i < count; i++)
            {
                uint size = Win32Native.DragQueryFile(hGlobal, i, null, 0);
                StringBuilder sb = new StringBuilder((int)size + 1);
                Win32Native.DragQueryFile(hGlobal, i, sb, (uint)sb.Capacity);
                files.Add(sb.ToString());
            }
            return files;
        }

        private bool SetTextToClipboard(string text)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(text + "\0");
            IntPtr hGlobal = Win32Native.GlobalAlloc(Win32Native.GMEM_MOVEABLE, (UIntPtr)bytes.Length);
            if (hGlobal == IntPtr.Zero) return false;

            IntPtr target = Win32Native.GlobalLock(hGlobal);
            Marshal.Copy(bytes, 0, target, bytes.Length);
            Win32Native.GlobalUnlock(hGlobal);

            return Win32Native.SetClipboardData(Win32Native.CF_UNICODETEXT, hGlobal) != IntPtr.Zero;
        }

        private bool SetFilesToClipboard(StringCollection files)
        {
            // 构建文件列表：以 \0 分隔，以 \0\0 结尾
            StringBuilder sb = new StringBuilder();
            foreach (string file in files) { sb.Append(file).Append('\0'); }
            sb.Append('\0');
            byte[] fileListBytes = Encoding.Unicode.GetBytes(sb.ToString());

            int structSize = Marshal.SizeOf<Win32Native.DROPFILES>();
            IntPtr hGlobal = Win32Native.GlobalAlloc(Win32Native.GMEM_MOVEABLE, (UIntPtr)(structSize + fileListBytes.Length));
            if (hGlobal == IntPtr.Zero) return false;

            IntPtr pointer = Win32Native.GlobalLock(hGlobal);
            try
            {
                Win32Native.DROPFILES df = new Win32Native.DROPFILES
                {
                    pFiles = (uint)structSize,
                    fWide = true // 必须为 Unicode
                };
                Marshal.StructureToPtr(df, pointer, false);
                Marshal.Copy(fileListBytes, 0, pointer + structSize, fileListBytes.Length);
            }
            finally
            {
                Win32Native.GlobalUnlock(hGlobal);
            }

            return Win32Native.SetClipboardData(Win32Native.CF_HDROP, hGlobal) != IntPtr.Zero;
        }

        #endregion
    }
}