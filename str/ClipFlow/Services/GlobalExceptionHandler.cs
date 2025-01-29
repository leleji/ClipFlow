using System;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace ClipFlow.Services
{
    public static class GlobalExceptionHandler
    {
        public static void Setup()
        {
            // 处理未捕获的异常
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = (Exception)args.ExceptionObject;
                FileLogService._.Fatal($"未处理的异常: {ex.Message}", ex);
            };

            // 处理未观察到的任务异常
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                FileLogService._.Fatal($"未观察到的任务异常: {args.Exception.Message}", args.Exception);
                args.SetObserved(); // 标记异常已被观察，防止程序崩溃
            };

            // 处理UI线程异常
            Dispatcher.UIThread.UnhandledException += (sender, args) =>
            {
                FileLogService._.Fatal($"UI线程异常: {args.Exception.Message}", args.Exception);
                args.Handled = true; // 标记异常已处理
            };
        }
    }
} 