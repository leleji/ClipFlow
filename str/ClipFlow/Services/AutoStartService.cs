using System;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Win32;

namespace ClipFlow.Services
{
    public class AutoStartService : IAutoStartService
    {
        private static readonly string AppName = "ClipFlow";
        private static readonly string ExePath = Environment.ProcessPath ?? string.Empty;

        public bool IsEnabled
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
                    return key?.GetValue(AppName)?.ToString() == ExePath;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var plistPath = GetMacStartupPlistPath();
                    return File.Exists(plistPath);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    var desktopPath = GetLinuxDesktopFilePath();
                    return File.Exists(desktopPath);
                }
                return false;
            }
        }

        public void Enable()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                    key?.SetValue(AppName, ExePath);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var plistContent = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>Label</key>
    <string>{AppName}</string>
    <key>ProgramArguments</key>
    <array>
        <string>{ExePath}</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
</dict>
</plist>";
                    File.WriteAllText(GetMacStartupPlistPath(), plistContent);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    var desktopContent = $@"[Desktop Entry]
Type=Application
Version=1.0
Name={AppName}
Comment=Sync clipboard across devices
Exec={ExePath}
Terminal=false
Categories=Utility;";
                    var desktopPath = GetLinuxDesktopFilePath();
                    var autoStartDir = Path.GetDirectoryName(desktopPath);
                    if (!string.IsNullOrEmpty(autoStartDir) && !Directory.Exists(autoStartDir))
                    {
                        Directory.CreateDirectory(autoStartDir);
                    }
                    File.WriteAllText(desktopPath, desktopContent);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启用自启动失败: {ex.Message}");
            }
        }

        public void Disable()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                    key?.DeleteValue(AppName, false);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var plistPath = GetMacStartupPlistPath();
                    if (File.Exists(plistPath))
                    {
                        File.Delete(plistPath);
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    var desktopPath = GetLinuxDesktopFilePath();
                    if (File.Exists(desktopPath))
                    {
                        File.Delete(desktopPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"禁用自启动失败: {ex.Message}");
            }
        }

        private static string GetMacStartupPlistPath()
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(homeDir, "Library", "LaunchAgents", $"com.{AppName.ToLower()}.plist");
        }

        private static string GetLinuxDesktopFilePath()
        {
            var configDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(configDir, "autostart", $"{AppName.ToLower()}.desktop");
        }
    }
} 