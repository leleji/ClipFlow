using NLog;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ClipFlow.Desktop.Services
{
    public class FileLogService
    {
        private readonly Logger _logger;

        private static readonly Lazy<FileLogService> _lazyInstance = new Lazy<FileLogService>(() => new FileLogService());

        public static FileLogService Instance => _lazyInstance.Value;

        private FileLogService()
        {
            string dynamicLogDirectory = GetLogDirectory();
            GlobalDiagnosticsContext.Set("logDirectory", dynamicLogDirectory);
            _logger = LogManager.GetCurrentClassLogger();
        }

        private string GetLogDirectory()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ClipFlow/logs");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library/Application Support/ClipFlow/logs");
            }
            else
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".config/clipflow/logs");
            }
        }

        private void Log(LogLevel level, string msg, Exception? err = null)
        {
            if (err == null)
            {
                _logger.Log(level, msg);
            }
            else
            {
                _logger.Log(level, err, msg);
            }
        }

        public void Debug(string msg, Exception? err = null) => Log(LogLevel.Debug, msg, err);
        public void Info(string msg, Exception? err = null) => Log(LogLevel.Info, msg, err);
        public void Warn(string msg, Exception? err = null) => Log(LogLevel.Warn, msg, err);
        public void Error(string msg, Exception? err = null) => Log(LogLevel.Error, msg, err);
        public void Fatal(string msg, Exception? err = null) => Log(LogLevel.Fatal, msg, err);
    }
}