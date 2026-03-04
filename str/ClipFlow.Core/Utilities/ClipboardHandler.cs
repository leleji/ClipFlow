using Avalonia.Platform.Storage;
using ClipFlow.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ClipFlow.Core.Utilities
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
                DataLength = (ulong)text.Length,
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
                CopyFiles = [file],
                DataLength = (ulong)fileInfo.Length,
                Description = $"单文件: {fileInfo.Name}"
            };
        }
        public static ClipboardData? ProcessMultipleItems(List<string> files, string processName = "")
        {
            if (!files.Any()) return null;
            ulong totalSize = (ulong)ClipboardUtils.GetTotalSize(files);
            var cpfile=new StringCollection();
            cpfile.AddRange(files.ToArray());
            return new ClipboardData
            {
                Type = ClipboardType.FileList,
                CopyFiles = cpfile,
                ProcessName = processName,
                FileName = $"files_{DateTime.Now:yyyyMMddHHmmss}.zip",
                DataLength = totalSize,
                Description = $"{files.Count} 个文件: {string.Join(", ", files.Select(path => Path.GetFileName(path.TrimEnd('\\'))).Take(5))}"
            };
        }

        public static ClipboardData? ProcessFiles(IEnumerable<string> files, string processName="")
        {
            var fileList = files.ToList();
            if (!files.Any()) return null;
            return fileList.Count == 1 && !Directory.Exists(fileList[0])
                ? ProcessSingleFile(fileList[0], processName)
                : ProcessMultipleItems(fileList, processName);
        }

    }
}
