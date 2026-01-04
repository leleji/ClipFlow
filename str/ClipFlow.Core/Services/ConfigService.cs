using ClipFlow.Core.Models;
using ClipFlow.Core.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using NLog;
using NLog.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace ClipFlow.Core.Services
{
    public class ConfigService
    {
        private readonly string _configPath;
        private readonly AppConfig _currentConfig;

        /// <summary>
        /// 错误上传开关变化事件（给 Infrastructure 用）
        /// </summary>
        public event Action<bool>? AllowErrorUploadChanged;

        public AppConfig CurrentConfig => _currentConfig;
        public ConfigService()
        {
            _configPath = GetConfigFilePath();
            _currentConfig = LoadConfig();
            _currentConfig.PropertyChanged += SettingsViewModel_PropertyChanged;
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
                FileLogService.Instance.Error($"创建配置目录失败: {configDir}", ex);
            }

            return Path.Combine(configDir, "config.json");
        }

        private AppConfig LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json, AppConfigJsonContext.Default.AppConfig);
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
                            var config = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig);
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
                            FileLogService.Instance.Error("迁移旧配置失败", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FileLogService.Instance.Error($"加载配置失败: {_configPath}", ex);
            }

            return new AppConfig();
        }

        public void SaveConfig()
        {
            SaveConfig(_currentConfig);
        }

        private void SaveConfig(AppConfig config)
        {
            try
            {
                var json = JsonSerializer.Serialize(config, AppConfigJsonContext.Default.AppConfig);

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
                FileLogService.Instance.Error($"保存配置失败: {_configPath}", ex);
                throw;
            }
        }
        public void SettingsViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var propertyName = e.PropertyName;
            if (string.IsNullOrEmpty(propertyName))
                return;
            if (e.PropertyName == nameof(AppConfig.AllowErrorUpload))
            {
                AllowErrorUploadChanged?.Invoke(CurrentConfig.AllowErrorUpload);
            }
            SaveConfig();
        }
    }
} 