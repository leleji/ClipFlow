using ClipFlow.Desktop.Interfaces;
using ClipFlow.Desktop.Services;
using ClipFlow.Desktop.Utilities;
using ClipFlow.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Windows.Forms.DataFormats;


namespace ClipFlow.Desktop.Win.Services
{
    public class WindowsClipboardService : IClipboardHandler
    {
        private string? _lastHash;
        private bool _isSettingClipboard;
        private bool _isServerUpdate;

        public async Task<ClipboardData?> GetContentAsync()
        {
            if (_isSettingClipboard) return null;

            try
            {
               
                //包含FileDrop的都是文件，包括有些图片
                if (Clipboard.ContainsData("FileDrop"))
                {
                    var list = new List<string>(Clipboard.GetFileDropList().Cast<string>());
                    var filesHash = ClipboardUtils.GetMd5Hash(string.Join("|", list));
                    if (filesHash == _lastHash) return null;
                    _lastHash = filesHash;
                    if (list.Count == 1 && !Directory.Exists(list[0]))
                    {
                        return ProcessSingleFile(list[0]);
                    }
                    return ProcessMultipleItems(list);

                }else if (Clipboard.ContainsText())
                {
                    var text =  Clipboard.GetText();
                    if (string.IsNullOrEmpty(text)) return null;
                    var textHash = ClipboardUtils.GetMd5Hash(text);
                    if (textHash == _lastHash) return null;
                    _lastHash = textHash;
                    return ProcessText(text);
                }
                else if (Clipboard.ContainsImage())
                {
                    //var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
                    ////本地没有存储的图片
                    //using var img = Clipboard.GetImage();
                    //img?.Save(tempPath, ImageFormat.Png);
                    //return ProcessSingleFile(tempPath);
                }
                else
                {
                    var formats = Clipboard.GetDataObject()?.GetFormats();
                    FileLogService._.Error($"无法获取剪贴板数据对象:{string.Join(",", formats)}");
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.AddLog("错误", $"获取剪贴板内容失败: {ex.Message}");
            }

            return null;
        }

        public async Task<bool> SetContentAsync(ClipboardData data, bool isServerUpdate = true)
        {

            ;
            try
            {
                _isSettingClipboard = true;
                _isServerUpdate = isServerUpdate;
                switch (data.Type)
                {
                    case ClipboardType.Text:
                        var text = Encoding.UTF8.GetString(data.Data);
                        Clipboard.SetText(text);
                        _lastHash = ClipboardUtils.GetMd5Hash(text);
                        data.Description = "文本: " + (text.Length > 30 ? text[..30] + "..." : text);
                        LogService.Instance.AddLog("已接收", data.Description);
                        break;
                    case ClipboardType.File:
                    case ClipboardType.FileList:
                        data.Description = $"{data.CopyFiles.Count} 个文件: {string.Join(", ", data.CopyFiles.Cast<string>()
                           .Select(path => Path.GetFileName(path.TrimEnd('\\')))
                           .Take(5))}";
                        _lastHash = ClipboardUtils.GetMd5Hash(string.Join("|", data.CopyFiles.Cast<string>()));
                        Clipboard.SetFileDropList(data.CopyFiles);
                        LogService.Instance.AddLog("已接收", data.Description);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.AddLog("错误", $"设置剪贴板内容失败: {ex.Message}");
                return false;
            }
            finally
            {
                _isSettingClipboard = false;
                _ = Task.Delay(1000).ContinueWith(_ =>
                {
                    _isServerUpdate = false;
                });
            }
            return true;
        }

        private ClipboardData ProcessSingleFile(string file)
        {
            FileInfo fileInfo = new FileInfo(file);
            return new ClipboardData
            {
                Type = ClipboardType.File,
                FileName = fileInfo.Name,
                FilenameList = new List<string> { file },
                DataLength = (ulong)file.Length,
                Description = $"单文件: {fileInfo.Name}"
            };
        }

        private ClipboardData ProcessMultipleItems(List<string> files)
        {
            ulong totalSize = (ulong)ClipboardUtils.GetTotalSize(files);
            return new ClipboardData
            {
                Type = ClipboardType.FileList,
                FilenameList = files,
                FileName = $"files_{DateTime.Now:yyyyMMddHHmmss}.zip",
                DataLength = totalSize,
                Description = $"{files.Count} 个文件: {string.Join(", ", files.Select(path => Path.GetFileName(path.TrimEnd('\\'))).Take(5))}"
            };
        }

        private ClipboardData? ProcessText(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;

            return new ClipboardData
            {
                Type = ClipboardType.Text,
                Text = text,
                Data= Encoding.UTF8.GetBytes(text),
                Description = "文本: " + (text.Length > 30 ? text[..30] + "..." : text)
            };
        }


        public void Initialize()
        {
            
        }

        public void Cleanup()
        {
            
        }
    }
} 