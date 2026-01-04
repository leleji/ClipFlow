using ClipFlow.Core.Services;
using Sentry.Protocol;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ClipFlow.Infrastructure.Telemetry
{
    public static class ClipFlowSentry
    {
        private static bool _initialized;
        private static volatile bool _enabled = true;
        public static void Init(
            string dsn,
            bool allowUpload,
            string environment = "production",
            double tracesSampleRate = 0)
        {
            if (_initialized)
                return;
            _enabled = allowUpload;
            SentrySdk.Init(o =>
            {
                o.Dsn = dsn;
                o.Environment = environment;
                o.Release = $"clipflow@{Assembly.GetEntryAssembly()?.GetName().Version}";
                o.TracesSampleRate = tracesSampleRate;
                o.AutoSessionTracking = true;
                o.MaxBreadcrumbs = 50;

                //// 全局事件过滤
                o.SetBeforeSend(@event =>
                {
                    if (!_enabled)
                        return null;
                    var ex = @event.Exception;

                    if (ex is TaskCanceledException)
                        return null;

                    if (ex?.Message?.Contains("Operation canceled", StringComparison.OrdinalIgnoreCase) == true)
                        return null;

                    return @event;
                });
                // ⛔ Breadcrumb 过滤
                o.SetBeforeBreadcrumb (breadcrumb =>
                {
                    return _enabled ? breadcrumb : null;
                });
            });

            RegisterGlobalHandlers();
            SetDeviceContext();

            _initialized = true;
        }

        public static void SetUser(string clientId, string? username = null)
        {
            SentrySdk.ConfigureScope(scope =>
            {
                scope.User = new SentryUser
                {
                    Id = clientId,
                    Username = username,
                    IpAddress = "auto"
                };
            });
        }

        public static void Capture(Exception ex, string? module = null)
        {
            if (!_enabled)
                return;
            using (SentrySdk.PushScope())
            {
                SentrySdk.ConfigureScope(scope =>
                {
                    if (!string.IsNullOrWhiteSpace(module))
                        scope.SetTag("module", module);
                });

                SentrySdk.CaptureException(ex);

            
            }
        }

        public static void Capture(Exception ex, Action<Scope> configureScope)
        {
            if (!_enabled)
                return;
            using (SentrySdk.PushScope())
            {
                SentrySdk.ConfigureScope(configureScope);
                SentrySdk.CaptureException(ex);
            }
        }

        public static void Breadcrumb(
            string message,
            string category = "app",
            BreadcrumbLevel level = BreadcrumbLevel.Info)
        {
            if (!_enabled)
                return;
            SentrySdk.AddBreadcrumb(message, category, level: level);
        }

        public static void Shutdown()
        {
            try
            {
                SentrySdk.Flush(TimeSpan.FromSeconds(2));
            }
            finally
            {
                SentrySdk.Close();
            }
        }
        public static void SetEnabled(bool enabled)
        {
            _enabled = enabled;
        }
        // ----------------------

        private static void RegisterGlobalHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    SentrySdk.CaptureException(ex);
            };

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                SentrySdk.CaptureException(e.Exception);
                e.SetObserved();
            };
        }

        private static void SetDeviceContext()
        {
            SentrySdk.ConfigureScope(scope =>
            {
                scope.SetTag("platform", "avalonia");
                scope.SetTag("os", RuntimeInformation.OSDescription);
                scope.SetTag("arch", RuntimeInformation.OSArchitecture.ToString());
                scope.SetTag("framework", RuntimeInformation.FrameworkDescription);
            });
        }
    }
}
