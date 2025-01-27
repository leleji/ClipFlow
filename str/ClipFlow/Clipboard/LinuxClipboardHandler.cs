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
                    var ph = pathList.Skip(1).Select(v =>
                    {
                        try { return new System.Uri(v).LocalPath; }
                        catch { }
                        return "";
                    })
                    .Where(x => !string.IsNullOrEmpty(x));
                    foreach (var path in ph)
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

        protected override async Task<DataObject?> SetStorageItemsToClipboard(ClipboardData data)
        {
            var dataObject = new DataObject();
            dataObject.Set("Text", Encoding.UTF8.GetBytes(string.Join('\n', data.FilenameList.Select(v => v))));
            var uriEnum = data.FilenameList.Select(file => new System.Uri(file).GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped));
            var uris = string.Join("\n", uriEnum);
            dataObject.Set("text/uri-list", Encoding.UTF8.GetBytes(uris));
            var nautilus = $"x-special/nautilus-clipboard\ncopy\n{uris}\n";
            dataObject.Set(FileFormat, nautilus);
            return await Task.FromResult(dataObject); 
        }
    }
} 