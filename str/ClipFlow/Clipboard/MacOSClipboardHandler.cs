using System;

namespace ClipFlow.Clipboard
{
    public class MacOSClipboardHandler : BaseClipboardHandler
    {
        protected override string FileFormat => "public.file-url";
    }
} 