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
    public class LinuxClipboardHandler : BaseClipboardHandler
    {

        protected override string FileFormat => "x-special/gnome-copied-files";

       // protected override string TextFormat => "text/plain";
    }
} 