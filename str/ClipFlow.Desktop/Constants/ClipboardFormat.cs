using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipFlow.Desktop.Constants
{
    public static class ClipboardFormat
    {
        public const string LinuxFile = "text/uri-list";
        public const string GnomeFiles = "x-special/gnome-copied-files";

        public const string Text = "TEXT";
        public const string Html = "HTML Format";
        public const string TextHtml = "text/html";
        public const string ImagePng = "image/png";
        public const string ImageJpeg = "image/jpeg";
        public const string ImageBmp = "image/bmp";


        public const string WindowsFile = "FileDrop";
        
        public const string MacFile = "public.file-url";
        public const string MacText = "public.utf8-plain-text";
        public const string MacImagePng = "public.png";
        public const string MacImageJpeg = "public.jpeg";
        public const string MacImageTiff = "public.tiff";
    }
}
