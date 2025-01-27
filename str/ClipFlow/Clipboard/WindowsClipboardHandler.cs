using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ClipFlow.Models;
using ClipFlow.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClipFlow.Clipboard
{
    public class WindowsClipboardHandler : BaseClipboardHandler
    {
        protected override string FileFormat => "Files";

    }
} 