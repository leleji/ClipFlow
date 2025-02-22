using Avalonia.Platform.Storage;
using ClipFlow.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipFlow.Desktop.Utilities
{
    public class ClipboardProcess
    {
        public static ClipboardData? ProcessText(string text,string processName="")
        {
            if (string.IsNullOrEmpty(text)) return null;

            return new ClipboardData
            {
                Type = ClipboardType.Text,
                Text = text,
                ProcessName= processName,
                Description = "文本: " + (text.Length > 30 ? text[..30] + "..." : text)
            };
        }

        public static ClipboardData ProcessSingleFile(string file, string processName = "")
        {
            var fileInfo = new FileInfo(file);
            return new ClipboardData
            {
                Type = ClipboardType.File,
                FileName = fileInfo.Name,
                ProcessName = processName,
                FilenameList = new List<string> { file },
                DataLength = (ulong)file.Length,
                Description = $"单文件: {fileInfo.Name}"
            };
        }
        public async Task<ClipboardData> ProcessSingleFile(IStorageItem file)
        {
            return new ClipboardData
            {
                Type = ClipboardType.File,
                FileName = file.Name,
                FilenameList = new List<string> { file.Path.LocalPath },
                DataLength = (await file.GetBasicPropertiesAsync()).Size,
                Description = $"单文件: {file.Name}"
            };
        }
        public static ClipboardData? ProcessMultipleItems(List<string> files, string processName = "")
        {
            if (!files.Any()) return null;
            ulong totalSize = (ulong)ClipboardUtils.GetTotalSize(files);
            return new ClipboardData
            {
                Type = ClipboardType.FileList,
                FilenameList = files,
                ProcessName = processName,
                FileName = $"files_{DateTime.Now:yyyyMMddHHmmss}.zip",
                DataLength = totalSize,
                Description = $"{files.Count} 个文件: {string.Join(", ", files.Select(path => Path.GetFileName(path.TrimEnd('\\'))).Take(5))}"
            };
        }

        public static ClipboardData? ProcessFiles(IEnumerable<string> files, string processName)
        {
            var fileList = files.ToList();
            if (!files.Any()) return null;
            return fileList.Count == 1 && !Directory.Exists(fileList[0])
                ? ProcessSingleFile(fileList[0], processName)
                : ProcessMultipleItems(fileList, processName);
        }

    }
}
