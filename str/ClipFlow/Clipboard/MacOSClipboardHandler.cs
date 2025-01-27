using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClipFlow.Clipboard
{
    public class MacOSClipboardHandler : BaseClipboardHandler
    {
        protected override string FileFormat => "public.file-url";

        protected override async Task<IEnumerable<IStorageItem>?> GetStorageItemsFromClipboard(IClipboard clipboard)
        {
            return await clipboard.GetDataAsync(FileFormat) as IEnumerable<IStorageItem>;
        }

        protected override Task<bool> SetStorageItemsToClipboard(DataObject dataObject, IEnumerable<IStorageItem> items)
        {
            dataObject.Set(FileFormat, items);
            return Task.FromResult(true);
        }
    }
} 