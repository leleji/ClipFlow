using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace ClipFlow.Desktop.MacOs.Utilities;


public sealed class PasteboardMonitor : IAsyncDisposable
{
    private readonly record struct NativeMethods
    {
        [DllImport("/usr/lib/libobjc.dylib")]
        public static extern IntPtr objc_getClass(string name);

        [DllImport("/usr/lib/libobjc.dylib")]
        public static extern IntPtr sel_registerName(string name);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public static extern IntPtr objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        public static extern ulong objc_msgSend_UInt64(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libSystem.dylib")]
        public static extern IntPtr dlopen(string path, int mode);

        [DllImport("/usr/lib/libSystem.dylib")]
        public static extern IntPtr dlerror();
    }

    private const int RtldNow = 2;
    private IntPtr _pasteboard;
    private readonly CancellationTokenSource _cts = new();
    private Task? _monitorTask;
    private bool _isDisposed;

    public event EventHandler<PasteboardChangeEventArgs>? Changed;

    private bool InitializeAppKit()
    {
        var appKitLib = NativeMethods.dlopen("/System/Library/Frameworks/AppKit.framework/AppKit", RtldNow);
        if (appKitLib == IntPtr.Zero)
        {
            var error = NativeMethods.dlerror();
            if (error != IntPtr.Zero)
            {
                var errorMsg = Marshal.PtrToStringAnsi(error);
                Console.WriteLine($"加载 AppKit 出错: {errorMsg}");
            }
            return false;
        }
        return true;
    }

    private static IntPtr GetPasteboard()
    {
        try
        {
            var nsPasteboardClass = NativeMethods.objc_getClass("NSPasteboard");
            if (nsPasteboardClass == IntPtr.Zero)
            {
                Console.WriteLine("无法获取 NSPasteboard 类");
                return IntPtr.Zero;
            }

            var generalPasteboardSelector = NativeMethods.sel_registerName("generalPasteboard");
            return NativeMethods.objc_msgSend_IntPtr(nsPasteboardClass, generalPasteboardSelector);
        }
        catch (Exception ex)
        {
            Console.WriteLine( "获取剪贴板时出错");
            return IntPtr.Zero;
        }
    }

    private static ulong GetPasteboardChangeCount(IntPtr pasteboard)
    {
        try
        {
            if (pasteboard == IntPtr.Zero) return 0;
            var changeCountSelector = NativeMethods.sel_registerName("changeCount");
            return NativeMethods.objc_msgSend_UInt64(pasteboard, changeCountSelector);
        }
        catch (Exception ex)
        {
            Console.WriteLine( "获取剪贴板计数时出错");
            return 0;
        }
    }

    public bool StartAsync()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_monitorTask is not null)
        {
            Console.WriteLine("监控已经在运行中");
            return false;
        }

        if (!InitializeAppKit())
        {
            Console.WriteLine("初始化 AppKit 失败");
            return false;
        }

        _pasteboard = GetPasteboard();
        if (_pasteboard == IntPtr.Zero)
        {
            Console.WriteLine("无法获取剪贴板引用");
            return false;
        }

        var lastChangeCount = GetPasteboardChangeCount(_pasteboard);
        Console.WriteLine($"开始监控剪贴板，初始计数: {lastChangeCount}");

        _monitorTask = Task.Run(async () =>
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    var currentChangeCount = GetPasteboardChangeCount(_pasteboard);
                    if (currentChangeCount != lastChangeCount)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            OnChanged(new PasteboardChangeEventArgs(lastChangeCount, currentChangeCount));
                        });
                       
                        lastChangeCount = currentChangeCount;
                    }
                    await Task.Delay(100, _cts.Token);
                }
            }
            catch (OperationCanceledException) when (_cts.Token.IsCancellationRequested)
            {
                Console.WriteLine("监控已取消");
            }
            catch (Exception ex)
            {
                Console.WriteLine("监控过程中发生错误");
            }
        });

        return true;
    }

    public async Task StopAsync()
    {
        if (_monitorTask is null)
        {
            Console.WriteLine("监控未在运行");
            return;
        }

        try
        {
            await _cts.CancelAsync();
            await _monitorTask;
            Console.WriteLine("监控已停止");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("监控已取消");
        }
        catch (Exception ex)
        {
            Console.WriteLine( "停止监控时出错");
        }
        finally
        {
            _monitorTask = null;
        }
    }

    private void OnChanged(PasteboardChangeEventArgs e)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        try
        {
            Changed?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            Console.WriteLine("处理剪贴板变化事件时出错");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;

        try
        {
            await StopAsync();
        }
        finally
        {
            _cts.Dispose();
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
public class PasteboardChangeEventArgs : EventArgs
{
    public ulong OldCount { get; }
    public ulong NewCount { get; }

    public PasteboardChangeEventArgs(ulong oldCount, ulong newCount)
    {
        OldCount = oldCount;
        NewCount = newCount;
    }
}