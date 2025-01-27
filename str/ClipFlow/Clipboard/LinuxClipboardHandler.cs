using ABI.System;
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipFlow.Clipboard
{
    public class LinuxClipboardHandler : BaseClipboardHandler
    {
        protected override string FileFormat => "x-special/gnome-copied-files";

        protected override async Task<IEnumerable<IStorageItem>?> GetStorageItemsFromClipboard(IClipboard clipboard)
        {
            var storageItems = new List<IStorageItem>();
            var bytes = await clipboard.GetDataAsync(FileFormat) as byte[];
            var str = Encoding.UTF8.GetString(bytes!);
            var pathList = str.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                                .Where(x => !string.IsNullOrEmpty(x))
                                .ToArray();
            if (pathList.Length > 1)
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                  desktop.MainWindow?.Clipboard != null)
                {
                    var provider = desktop.MainWindow.StorageProvider;
                    foreach (var path in pathList.Skip(1).Select(v => { return (new System.Uri(v)).LocalPath; }))
                    {
                        IStorageItem? item = null;
                        if (System.IO.Directory.Exists(path))
                        {
                            item = await provider.TryGetFolderFromPathAsync(path);
                        }
                        else if (System.IO.File.Exists(path))
                        {
                            item = await provider.TryGetFileFromPathAsync(path);
                        }
                        if (item != null)
                        {
                            storageItems.Add(item);
                        }
                    }
                }
 
            }

            return storageItems;
        }

        protected override Task<bool> SetStorageItemsToClipboard(DataObject dataObject, IEnumerable<IStorageItem> items)
        {
            dataObject.Set(FileFormat, items);
            return Task.FromResult(true);
        }
    }
} 