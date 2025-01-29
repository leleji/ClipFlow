using System;
using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices;
using ClipFlow.Models;

namespace ClipFlow.Services
{
    public class ConfigService
    {
        private static ConfigService? _instance;
        public static ConfigService Instance => _instance ??= new ConfigService();

        private readonly string _configPath;
        private Config _currentConfig;

        public Config CurrentConfig => _currentConfig;

        private ConfigService()
        {
            _configPath = GetConfigFilePath();
            _currentConfig = LoadConfig();
        }

        private string GetConfigFilePath()
        {
            string configDir;
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: %APPDATA%\ClipFlow
                configDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ClipFlow");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // macOS: ~/Library/Application Support/ClipFlow
                configDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library/Application Support/ClipFlow");
            }
            else
            {
                // Linux: ~/.config/clipflow
                configDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".config/clipflow");
            }

            // 确保配置目录存在
            try
            {
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }
            }
            catch (Exception ex)
            {
                FileLogService._.Error($"创建配置目录失败: {configDir}", ex);
            }

            return Path.Combine(configDir, "config.json");
        }

        private Config LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var config = JsonSerializer.Deserialize<Config>(json);
                    if (config != null)
                    {
                        return config;
                    }
                }
                else
                {
                    // 如果新位置不存在配置文件，尝试从旧位置迁移
                    var oldConfigPath = Path.Combine(AppContext.BaseDirectory, "config.json");
                    if (File.Exists(oldConfigPath))
                    {
                        try
                        {
                            var json = File.ReadAllText(oldConfigPath);
                            var config = JsonSerializer.Deserialize<Config>(json);
                            if (config != null)
                            {
                                // 保存到新位置
                                SaveConfig(config);
                                // 尝试删除旧配置文件
                                try { File.Delete(oldConfigPath); } catch { }
                                return config;
                            }
                        }
                        catch (Exception ex)
                        {
                            FileLogService._.Error("迁移旧配置失败", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FileLogService._.Error($"加载配置失败: {_configPath}", ex);
            }

            return new Config
            {
                // 基本设置默认值
                MinimizeToTray = true,
                HideOnStartup = false,
                AutoStart = false,

                // 上传设置默认值
                EnableUpload = true,
                EnableUploadText = true,
                EnableUploadFile = true,
                EnableUploadImage = true,
                EnableUploadMultiple = true,

                // 下载设置默认值
                EnableDownload = true,
                EnableDownloadText = true,
                EnableDownloadFile = true,
                EnableDownloadImage = true,

                // 限制设置默认值
                MaxTextLength = 0,
                MaxUploadFileSize = 0,
                MaxDownloadFileSize = 0,

                // 通知设置默认值
                EnableUploadNotification = true,
                EnableDownloadNotification = true
            };
        }

        public void SaveConfig()
        {
            SaveConfig(_currentConfig);
        }

        private void SaveConfig(Config config)
        {
            try
            {
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // 使用临时文件来保存，以防保存过程中出错导致配置文件损坏
                var tempPath = _configPath + ".tmp";
                File.WriteAllText(tempPath, json);
                
                // 如果存在旧文件，先备份
                if (File.Exists(_configPath))
                {
                    var backupconPath = _configPath + ".bak";
                    try { File.Copy(_configPath, backupconPath, true); } catch { }
                }

                // 将临时文件移动到正式位置
                File.Move(tempPath, _configPath, true);
                
                // 成功保存后删除备份
                var backupPath = _configPath + ".bak";
                if (File.Exists(backupPath))
                {
                    try { File.Delete(backupPath); } catch { }
                }
            }
            catch (Exception ex)
            {
                FileLogService._.Error($"保存配置失败: {_configPath}", ex);
                throw;
            }
        }
    }
} 