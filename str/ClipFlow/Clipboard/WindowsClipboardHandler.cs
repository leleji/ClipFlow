using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using ClipFlow.Models;
using ClipFlow.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ClipFlow.Clipboard
{
    public class WindowsClipboardHandler : BaseClipboardHandler
    {
        protected override string FileFormat => "Files";

        protected override async Task<IEnumerable<IStorageItem>?> GetStorageItemsFromClipboard(IClipboard clipboard)
        {
            return await clipboard.GetDataAsync(FileFormat) as IEnumerable<IStorageItem>;
        }

        protected override async Task<DataObject?> SetStorageItemsToClipboard(ClipboardData data)
        {
            
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                   desktop.MainWindow?.Clipboard != null)
            {
                var provider = desktop.MainWindow.StorageProvider;
                var storageItems = new List<IStorageItem>();
                foreach (var file in data.FilenameList)
                {
                    IStorageItem? item = null;
                    if (System.IO.Directory.Exists(file))
                    {
                        item = await provider.TryGetFolderFromPathAsync(file);
                    }
                    else if (System.IO.File.Exists(file))
                    {
                        item = await provider.TryGetFileFromPathAsync(file);
                    }
                    else
                    {
                        LogService.Instance.AddLog("警告", $"文件不存在: {file}");
                        continue;
                    }

                    if (item != null)
                    {
                        storageItems.Add(item);
                    }
                }

                if (storageItems.Count > 0)
                {
                    var dataObject = new DataObject();
                    dataObject.Set(FileFormat, storageItems);
                    return dataObject;
                }
            }
            return null;

        }
    }
} 