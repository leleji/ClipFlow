using Avalonia.Platform.Storage;
using ClipFlow.Models;
using ClipFlow.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ClipFlow.Clipboard
{
    public class ClipboardFileHandler
    {
        public async Task<ClipboardData?> ProcessFiles(IEnumerable<IStorageItem> files)
        {
            var fileList = files.ToList();
            if (!fileList.Any()) return null;

            return fileList.Count == 1 && !Directory.Exists(fileList[0].Path.LocalPath)
                ? await ProcessSingleFile(fileList[0])
                : await ProcessMultipleItems(fileList);
        }

        private async Task<ClipboardData> ProcessSingleFile(IStorageItem file)
        {
            return new ClipboardData
            {
                Type = ClipboardType.File,
                Filename = file.Name,
                FilenameList = new List<string> { file.Path.LocalPath },
                DataLength = (await file.GetBasicPropertiesAsync()).Size,
                Description = $"单文件: {file.Name}"
            };
        }

        private async Task<ClipboardData> ProcessMultipleItems(List<IStorageItem> items)
        {
            var sizes = await Task.WhenAll(items.Select(async file => (await file.GetBasicPropertiesAsync()).Size));
            ulong totalSize = 0;
            foreach (var size in sizes)
            {
                if (size.HasValue)
                {
                    totalSize += size.Value;
                }
            }
            return new ClipboardData
            {
                Type = ClipboardType.FileList,
                FilenameList = items.Select(v => v.Path.LocalPath).ToList(),
                Filename = $"files_{DateTime.Now:yyyyMMddHHmmss}.zip",
                DataLength = totalSize,
                Description = $"{items.Count} 个文件: {string.Join(", ", items.Select(path => Path.GetFileName(path.Path.LocalPath.TrimEnd('\\'))).Take(5))}"
            };
        }
    }
} 